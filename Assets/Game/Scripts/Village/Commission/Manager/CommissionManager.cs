// CommissionManager — 委託系統管理器。
// 統一管理所有委託型角色（FarmGirl / Witch / Guard）的工作台 slots，
// 提供 StartCommission / ClaimCommission / GetSlot / Tick 等 API。
// 純邏輯類別（非 MonoBehaviour），透過建構子注入相依。
//
// 依據 GDD `commission-system.md` v1.1：
// - 單一委託 Manager 服務多角色（character_id 分群）
// - 單物品配方（單一輸入 → 單一產出 + 倒數時間）
// - 多格子並行（每角色 N 格）
// - 現實時間倒數
// - 背包/倉庫整合：開始時先背包後倉庫扣除輸入；領取時先背包後倉庫入庫
//
// 事件發布：
// - CommissionStartedEvent：開始委託時
// - CommissionCompletedEvent：slot 從 InProgress 跨越到 Completed 邊界時（每次 Tick 偵測，僅發一次）
// - CommissionClaimedEvent：產出領取時（物品已進倉儲）
// - CommissionTickEvent：倒數中每秒（整秒剩餘值變化）發布一次
//
// 注意：FarmManager 仍並存，農女的「耕種」UI（FarmAreaView）暫時繼續走 FarmManager。
// 為避免雙重管理，VillageEntryPoint 組裝時以 allowedCharacterIds 過濾，本 IT 階段僅啟用 Witch 與 Guard。

using ProjectDR.Village.Farm;
using ProjectDR.Village.Storage;
using ProjectDR.Village.Backpack;
using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.TimeProvider;

namespace ProjectDR.Village.Commission
{
    // ===== 開始委託結果 =====

    /// <summary>開始委託的失敗原因。</summary>
    public enum StartCommissionError
    {
        /// <summary>無錯誤（成功）。</summary>
        None,

        /// <summary>指定的角色 ID 未在 CommissionManager 管理範圍內（未配置或未啟用）。</summary>
        UnknownCharacter,

        /// <summary>Slot 索引超出該角色的 slot 範圍。</summary>
        InvalidSlotIndex,

        /// <summary>Slot 目前非 Idle，無法開始新委託。</summary>
        SlotNotIdle,

        /// <summary>找不到指定的配方 ID。</summary>
        UnknownRecipe,

        /// <summary>配方不屬於該角色（config 中的 character_id 不一致）。</summary>
        RecipeCharacterMismatch,

        /// <summary>輸入物品在背包+倉庫中總量不足。</summary>
        InsufficientInput,
    }

    /// <summary>開始委託的結果。</summary>
    public class StartCommissionResult
    {
        public bool IsSuccess { get; }
        public StartCommissionError Error { get; }

        private StartCommissionResult(bool isSuccess, StartCommissionError error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static StartCommissionResult Success() => new StartCommissionResult(true, StartCommissionError.None);
        public static StartCommissionResult Failure(StartCommissionError error) => new StartCommissionResult(false, error);
    }

    // ===== 領取結果 =====

    /// <summary>領取委託的失敗原因。</summary>
    public enum ClaimCommissionError
    {
        /// <summary>無錯誤（成功）。</summary>
        None,

        /// <summary>角色 ID 未管理。</summary>
        UnknownCharacter,

        /// <summary>Slot 索引超出範圍。</summary>
        InvalidSlotIndex,

        /// <summary>Slot 為空，無可領取內容。</summary>
        SlotEmpty,

        /// <summary>Slot 尚未完成倒數。</summary>
        NotReady,

        /// <summary>背包與倉庫皆無法容納全部產出。</summary>
        NoSpaceForOutput,
    }

    /// <summary>領取委託的結果。</summary>
    public class ClaimCommissionResult
    {
        public bool IsSuccess { get; }
        public ClaimCommissionError Error { get; }

        /// <summary>實際入倉儲的數量（成功時 = 配方 output_quantity）。</summary>
        public int ClaimedQuantity { get; }

        /// <summary>實際入倉儲的物品 ID。</summary>
        public string ClaimedItemId { get; }

        private ClaimCommissionResult(bool isSuccess, ClaimCommissionError error, string claimedItemId, int claimedQuantity)
        {
            IsSuccess = isSuccess;
            Error = error;
            ClaimedItemId = claimedItemId;
            ClaimedQuantity = claimedQuantity;
        }

        public static ClaimCommissionResult Success(string itemId, int quantity)
            => new ClaimCommissionResult(true, ClaimCommissionError.None, itemId, quantity);

        public static ClaimCommissionResult Failure(ClaimCommissionError error)
            => new ClaimCommissionResult(false, error, null, 0);
    }

    // ===== Slot 狀態機 =====

    /// <summary>委託 slot 的狀態。</summary>
    public enum CommissionSlotState
    {
        /// <summary>閒置（可開始新委託）。</summary>
        Idle,

        /// <summary>工作中（倒數未結束）。</summary>
        InProgress,

        /// <summary>完成（倒數結束、可領取）。</summary>
        Completed,
    }

    // ===== Slot 唯讀資訊 =====

    /// <summary>
    /// 委託 slot 的唯讀快照。
    /// UI 層透過此結構查詢單一 slot 的狀態、配方、剩餘時間等。
    /// </summary>
    public readonly struct CommissionSlotInfo
    {
        /// <summary>所屬角色 ID。</summary>
        public string CharacterId { get; }

        /// <summary>Slot 索引（0-based）。</summary>
        public int SlotIndex { get; }

        /// <summary>當前狀態。</summary>
        public CommissionSlotState State { get; }

        /// <summary>當前配方 ID；Idle 時為 null。</summary>
        public string RecipeId { get; }

        /// <summary>預計產出物品 ID；Idle 時為 null。</summary>
        public string OutputItemId { get; }

        /// <summary>預計產出數量；Idle 時為 0。</summary>
        public int OutputQuantity { get; }

        /// <summary>開始時的 UTC 時間戳記；Idle 時為 0。</summary>
        public long StartedTimestampUtc { get; }

        /// <summary>倒數總長（秒）；Idle 時為 0。</summary>
        public float DurationSeconds { get; }

        /// <summary>剩餘秒數（0 表已完成）。</summary>
        public int RemainingSeconds { get; }

        public CommissionSlotInfo(
            string characterId,
            int slotIndex,
            CommissionSlotState state,
            string recipeId,
            string outputItemId,
            int outputQuantity,
            long startedTimestampUtc,
            float durationSeconds,
            int remainingSeconds)
        {
            CharacterId = characterId;
            SlotIndex = slotIndex;
            State = state;
            RecipeId = recipeId;
            OutputItemId = outputItemId;
            OutputQuantity = outputQuantity;
            StartedTimestampUtc = startedTimestampUtc;
            DurationSeconds = durationSeconds;
            RemainingSeconds = remainingSeconds;
        }
    }

    // ===== 內部 Slot 狀態（可變，但封裝在 Manager 內） =====

    internal class CommissionSlot
    {
        public CommissionSlotState State;
        public string RecipeId;
        public string OutputItemId;
        public int OutputQuantity;
        public long StartedTimestampUtc;
        public float DurationSeconds;

        /// <summary>上次發布 TickEvent 的剩餘秒數（整秒），用於去重。初始值 -1 表尚未發布。</summary>
        public int LastPublishedRemainingSeconds;

        public static CommissionSlot NewIdle()
        {
            return new CommissionSlot
            {
                State = CommissionSlotState.Idle,
                RecipeId = null,
                OutputItemId = null,
                OutputQuantity = 0,
                StartedTimestampUtc = 0,
                DurationSeconds = 0f,
                LastPublishedRemainingSeconds = -1,
            };
        }

        public void Reset()
        {
            State = CommissionSlotState.Idle;
            RecipeId = null;
            OutputItemId = null;
            OutputQuantity = 0;
            StartedTimestampUtc = 0;
            DurationSeconds = 0f;
            LastPublishedRemainingSeconds = -1;
        }
    }

    // ===== CommissionManager =====

    /// <summary>
    /// 委託系統管理器。
    /// 統一管理所有啟用角色的 slots，提供委託生命週期操作與倒數推進。
    /// 背包/倉庫策略：輸入扣除與產出入庫皆為「先背包後倉庫」。
    /// </summary>
    public class CommissionManager
    {
        private readonly CommissionRecipesConfig _config;
        private readonly BackpackManager _backpack;
        private readonly StorageManager _storage;
        private readonly ITimeProvider _timeProvider;

        /// <summary>依角色 ID → slot 陣列。</summary>
        private readonly Dictionary<string, CommissionSlot[]> _slotsByCharacter;

        /// <summary>本 Manager 實際啟用的角色 ID 集合（即實際配置 slots 的角色）。</summary>
        private readonly HashSet<string> _managedCharacters;

        /// <summary>
        /// 建構 CommissionManager。
        /// </summary>
        /// <param name="config">委託配方配置（不可為 null）。</param>
        /// <param name="backpack">背包管理器（不可為 null）。</param>
        /// <param name="storage">倉庫管理器（不可為 null）。</param>
        /// <param name="timeProvider">時間提供者（不可為 null）。</param>
        /// <param name="allowedCharacterIds">
        /// 啟用的角色 ID 白名單。為 null 代表啟用 config 中的所有角色；
        /// 若指定，則僅啟用交集部分（避免與 FarmManager 雙重管理農女時可傳入 [Witch, Guard]）。
        /// </param>
        /// <exception cref="ArgumentNullException">任一必要參數為 null 時拋出。</exception>
        public CommissionManager(
            CommissionRecipesConfig config,
            BackpackManager backpack,
            StorageManager storage,
            ITimeProvider timeProvider,
            IEnumerable<string> allowedCharacterIds = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _backpack = backpack ?? throw new ArgumentNullException(nameof(backpack));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

            _slotsByCharacter = new Dictionary<string, CommissionSlot[]>();
            _managedCharacters = new HashSet<string>();

            HashSet<string> allowed = null;
            if (allowedCharacterIds != null)
            {
                allowed = new HashSet<string>();
                foreach (string id in allowedCharacterIds)
                {
                    if (!string.IsNullOrEmpty(id)) allowed.Add(id);
                }
            }

            foreach (string characterId in _config.GetConfiguredCharacterIds())
            {
                if (allowed != null && !allowed.Contains(characterId)) continue;

                int slotCount = _config.GetWorkbenchSlotCount(characterId);
                if (slotCount <= 0) continue;

                CommissionSlot[] slots = new CommissionSlot[slotCount];
                for (int i = 0; i < slotCount; i++)
                {
                    slots[i] = CommissionSlot.NewIdle();
                }
                _slotsByCharacter[characterId] = slots;
                _managedCharacters.Add(characterId);
            }
        }

        /// <summary>取得管理中的所有角色 ID（只讀）。</summary>
        public IReadOnlyCollection<string> GetManagedCharacterIds() => _managedCharacters;

        /// <summary>取得指定角色的 slot 數。未管理的角色回傳 0。</summary>
        public int GetSlotCount(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return 0;
            return _slotsByCharacter.TryGetValue(characterId, out CommissionSlot[] slots) ? slots.Length : 0;
        }

        /// <summary>
        /// 查詢指定角色指定 slot 的當前資訊快照。
        /// 未管理的角色或無效索引回傳 default(CommissionSlotInfo)（State=Idle 且 CharacterId=null）。
        /// </summary>
        public CommissionSlotInfo GetSlot(string characterId, int slotIndex)
        {
            if (!TryGetSlot(characterId, slotIndex, out CommissionSlot slot))
            {
                return default(CommissionSlotInfo);
            }
            return ToInfo(characterId, slotIndex, slot);
        }

        /// <summary>
        /// 取得指定角色所有 slots 的快照清單。
        /// 未管理的角色回傳空清單。
        /// </summary>
        public IReadOnlyList<CommissionSlotInfo> GetSlots(string characterId)
        {
            if (!_slotsByCharacter.TryGetValue(characterId, out CommissionSlot[] slots))
            {
                return Array.AsReadOnly(Array.Empty<CommissionSlotInfo>());
            }
            CommissionSlotInfo[] snapshot = new CommissionSlotInfo[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                snapshot[i] = ToInfo(characterId, i, slots[i]);
            }
            return Array.AsReadOnly(snapshot);
        }

        /// <summary>
        /// 開始委託：扣除輸入物品（先背包後倉庫）、slot 轉 InProgress、發布 Started 事件。
        /// </summary>
        public StartCommissionResult StartCommission(string characterId, string recipeId, int slotIndex)
        {
            if (!_slotsByCharacter.TryGetValue(characterId, out CommissionSlot[] slots))
            {
                return StartCommissionResult.Failure(StartCommissionError.UnknownCharacter);
            }
            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                return StartCommissionResult.Failure(StartCommissionError.InvalidSlotIndex);
            }
            CommissionSlot slot = slots[slotIndex];
            if (slot.State != CommissionSlotState.Idle)
            {
                return StartCommissionResult.Failure(StartCommissionError.SlotNotIdle);
            }

            CommissionRecipeInfo recipe = _config.GetRecipe(recipeId);
            if (recipe == null)
            {
                return StartCommissionResult.Failure(StartCommissionError.UnknownRecipe);
            }
            if (recipe.CharacterId != characterId)
            {
                return StartCommissionResult.Failure(StartCommissionError.RecipeCharacterMismatch);
            }

            // 扣除輸入物品（僅有輸入時才扣）
            if (!recipe.IsEmptyHanded)
            {
                int totalAvailable = _backpack.GetItemCount(recipe.InputItemId)
                                     + _storage.GetItemCount(recipe.InputItemId);
                if (totalAvailable < recipe.InputQuantity)
                {
                    return StartCommissionResult.Failure(StartCommissionError.InsufficientInput);
                }

                int remaining = recipe.InputQuantity;

                // 先從背包扣
                int backpackHas = _backpack.GetItemCount(recipe.InputItemId);
                if (backpackHas > 0)
                {
                    int toRemoveFromBackpack = remaining < backpackHas ? remaining : backpackHas;
                    int actuallyRemoved = _backpack.RemoveItem(recipe.InputItemId, toRemoveFromBackpack);
                    remaining -= actuallyRemoved;
                }

                // 剩餘從倉庫扣
                if (remaining > 0)
                {
                    bool ok = _storage.RemoveItem(recipe.InputItemId, remaining);
                    if (!ok)
                    {
                        // 理論上進到這裡代表第一步扣背包後庫存發生變化，極罕見。
                        // 回滾：把已扣的背包量補回。保持 slot 狀態不變、不發布事件。
                        int rollback = recipe.InputQuantity - remaining;
                        if (rollback > 0)
                        {
                            _backpack.AddItem(recipe.InputItemId, rollback);
                        }
                        return StartCommissionResult.Failure(StartCommissionError.InsufficientInput);
                    }
                }
            }

            // 更新 slot 狀態
            long now = _timeProvider.GetCurrentTimestampUtc();
            slot.State = CommissionSlotState.InProgress;
            slot.RecipeId = recipe.RecipeId;
            slot.OutputItemId = recipe.OutputItemId;
            slot.OutputQuantity = recipe.OutputQuantity;
            slot.StartedTimestampUtc = now;
            slot.DurationSeconds = recipe.DurationSeconds;
            slot.LastPublishedRemainingSeconds = (int)Math.Ceiling(recipe.DurationSeconds);

            // 發布事件
            long expectedCompletion = now + (long)Math.Ceiling(recipe.DurationSeconds);
            EventBus.Publish(new CommissionStartedEvent
            {
                CharacterId = characterId,
                SlotIndex = slotIndex,
                RecipeId = recipe.RecipeId,
                ExpectedCompletionTimestampUtc = expectedCompletion,
            });

            return StartCommissionResult.Success();
        }

        /// <summary>
        /// 領取已完成的委託。
        /// 將產出先進背包，滿則進倉庫；兩者都無法完整容納時失敗、保留 slot 可再次嘗試。
        /// 成功時清空 slot、發布 Claimed 事件。
        /// </summary>
        public ClaimCommissionResult ClaimCommission(string characterId, int slotIndex)
        {
            if (!_slotsByCharacter.TryGetValue(characterId, out CommissionSlot[] slots))
            {
                return ClaimCommissionResult.Failure(ClaimCommissionError.UnknownCharacter);
            }
            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                return ClaimCommissionResult.Failure(ClaimCommissionError.InvalidSlotIndex);
            }

            CommissionSlot slot = slots[slotIndex];
            if (slot.State == CommissionSlotState.Idle)
            {
                return ClaimCommissionResult.Failure(ClaimCommissionError.SlotEmpty);
            }

            // 若仍在倒數中，先嘗試轉為 Completed（呼叫者可能沒 Tick）
            if (slot.State == CommissionSlotState.InProgress)
            {
                long now = _timeProvider.GetCurrentTimestampUtc();
                long endTime = slot.StartedTimestampUtc + (long)Math.Ceiling(slot.DurationSeconds);
                if (now >= endTime)
                {
                    TransitionToCompleted(characterId, slotIndex, slot);
                }
                else
                {
                    return ClaimCommissionResult.Failure(ClaimCommissionError.NotReady);
                }
            }

            // 此時 slot.State 必為 Completed
            string itemId = slot.OutputItemId;
            int quantity = slot.OutputQuantity;
            string recipeId = slot.RecipeId;

            // 入庫策略：先背包後倉庫。
            // BackpackManager.AddItem 回傳實際加入數量；剩餘用 StorageManager.TryAddItem 嘗試。
            // 若合計仍不足，執行回滾（背包部分移除、倉庫部分移除），返回 NoSpaceForOutput。
            int intoBackpack = _backpack.AddItem(itemId, quantity);
            int remainingForStorage = quantity - intoBackpack;
            int intoStorage = 0;
            if (remainingForStorage > 0)
            {
                intoStorage = _storage.TryAddItem(itemId, remainingForStorage);
            }

            int totalClaimed = intoBackpack + intoStorage;
            if (totalClaimed < quantity)
            {
                // 回滾：把已加入的部分還原（保留 slot 為 Completed 狀態，讓玩家清出空間後再試）
                if (intoBackpack > 0)
                {
                    _backpack.RemoveItem(itemId, intoBackpack);
                }
                if (intoStorage > 0)
                {
                    _storage.RemoveItem(itemId, intoStorage);
                }
                return ClaimCommissionResult.Failure(ClaimCommissionError.NoSpaceForOutput);
            }

            // 清空 slot
            slot.Reset();

            // 發布事件
            EventBus.Publish(new CommissionClaimedEvent
            {
                CharacterId = characterId,
                SlotIndex = slotIndex,
                RecipeId = recipeId,
                OutputItemId = itemId,
                OutputQuantity = quantity,
            });

            return ClaimCommissionResult.Success(itemId, quantity);
        }

        /// <summary>
        /// 推進所有 slots 的倒數。
        /// 依當前時間判斷各 InProgress slot 是否該轉 Completed（邊界僅觸發一次 CompletedEvent），
        /// 並在整秒剩餘秒數變化時發布 TickEvent。
        /// deltaSeconds 目前不直接使用（採現實時間戳記差值），但保留介面供後續擴充。
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            long now = _timeProvider.GetCurrentTimestampUtc();
            foreach (KeyValuePair<string, CommissionSlot[]> kvp in _slotsByCharacter)
            {
                string characterId = kvp.Key;
                CommissionSlot[] slots = kvp.Value;
                for (int i = 0; i < slots.Length; i++)
                {
                    CommissionSlot slot = slots[i];
                    if (slot.State != CommissionSlotState.InProgress) continue;

                    long endTime = slot.StartedTimestampUtc + (long)Math.Ceiling(slot.DurationSeconds);
                    long remainingLong = endTime - now;
                    int remainingInt = remainingLong > 0 ? (int)remainingLong : 0;

                    if (remainingInt <= 0)
                    {
                        TransitionToCompleted(characterId, i, slot);
                        continue;
                    }

                    // 整秒剩餘發生變化 → 發布 TickEvent
                    if (remainingInt != slot.LastPublishedRemainingSeconds)
                    {
                        slot.LastPublishedRemainingSeconds = remainingInt;
                        EventBus.Publish(new CommissionTickEvent
                        {
                            CharacterId = characterId,
                            SlotIndex = i,
                            RemainingSeconds = remainingInt,
                        });
                    }
                }
            }
        }

        // ===== 輔助方法 =====

        private bool TryGetSlot(string characterId, int slotIndex, out CommissionSlot slot)
        {
            slot = null;
            if (string.IsNullOrEmpty(characterId)) return false;
            if (!_slotsByCharacter.TryGetValue(characterId, out CommissionSlot[] slots)) return false;
            if (slotIndex < 0 || slotIndex >= slots.Length) return false;
            slot = slots[slotIndex];
            return true;
        }

        private CommissionSlotInfo ToInfo(string characterId, int slotIndex, CommissionSlot slot)
        {
            int remainingInt = 0;
            if (slot.State == CommissionSlotState.InProgress)
            {
                long now = _timeProvider.GetCurrentTimestampUtc();
                long endTime = slot.StartedTimestampUtc + (long)Math.Ceiling(slot.DurationSeconds);
                long remainingLong = endTime - now;
                remainingInt = remainingLong > 0 ? (int)remainingLong : 0;
            }
            return new CommissionSlotInfo(
                characterId,
                slotIndex,
                slot.State,
                slot.RecipeId,
                slot.OutputItemId,
                slot.OutputQuantity,
                slot.StartedTimestampUtc,
                slot.DurationSeconds,
                remainingInt);
        }

        private void TransitionToCompleted(string characterId, int slotIndex, CommissionSlot slot)
        {
            slot.State = CommissionSlotState.Completed;
            slot.LastPublishedRemainingSeconds = 0;

            EventBus.Publish(new CommissionCompletedEvent
            {
                CharacterId = characterId,
                SlotIndex = slotIndex,
                RecipeId = slot.RecipeId,
                OutputItemId = slot.OutputItemId,
                OutputQuantity = slot.OutputQuantity,
            });

            // 先發布一次 Tick=0 讓倒數 UI 顯示「00:00」（保持與中間 Tick 行為一致）
            EventBus.Publish(new CommissionTickEvent
            {
                CharacterId = characterId,
                SlotIndex = slotIndex,
                RemainingSeconds = 0,
            });
        }
    }
}
