// RedDotManager — 紅點 4 層系統（B7 Sprint 4）。
// 純邏輯管理器，不依賴 MonoBehaviour。
//
// 依據 GDD：
// - `commission-system.md` v1.1 § 四、紅點觸發四層
// - `character-interaction.md` v2.2 § 七、紅點系統整合
// - `base-management.md` v2.0
//
// 四層定義：
// - L1 委託完成：某 slot 有已完成、未領取的委託（監聽 CommissionCompleted / Claimed）
// - L2 角色發問：角色有未閱讀的主動發問（placeholder：依 AffinityThresholdReached 觸發）
// - L3 新任務：該角色擁有尚未 InProgress 的 Available 主線任務（監聽 MainQuestAvailable / Started / Completed）
// - L4 主線事件：節點 1/2 劇情待推進（監聽 MainQuestCompleted 中的 T1/T3 等觸發節點的任務）
//
// 優先序：L1 > L4 > L3 > L2（GDD § 4.3）。
// UI 層訂閱 RedDotUpdatedEvent 取得 HighestLayer 顯示對應紅點。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// 紅點 Hub 呈現資訊（不可變）。
    /// UI 層查詢時取得「是否顯示紅點」與「最高層級」。
    /// </summary>
    public readonly struct HubRedDotInfo
    {
        /// <summary>對應的角色 ID。</summary>
        public string CharacterId { get; }

        /// <summary>當前最高優先序層級（None 表示不顯示紅點）。</summary>
        public RedDotLayer HighestLayer { get; }

        /// <summary>是否應顯示紅點。</summary>
        public bool ShouldShow => HighestLayer != RedDotLayer.None;

        public HubRedDotInfo(string characterId, RedDotLayer highestLayer)
        {
            CharacterId = characterId;
            HighestLayer = highestLayer;
        }
    }

    /// <summary>
    /// 紅點管理器。
    /// 訂閱多個事件源（Commission、Affinity、MainQuest、CharacterUnlock）
    /// 更新內部每角色的 4 層紅點狀態，並在最高層級變化時發布 RedDotUpdatedEvent。
    ///
    /// 實作 IDisposable 以取消事件訂閱。
    /// </summary>
    public class RedDotManager : IDisposable
    {
        // ===== 優先序查找表（L1 > L4 > L3 > L2 > None） =====
        // 使用陣列存放四層啟用狀態，依索引對應 Layer，以 GetHighestLayer 自訂優先序判斷。

        private readonly MainQuestConfig _mainQuestConfig;
        private readonly MainQuestManager _mainQuestManager;

        // 每角色的各層啟用狀態。Key = characterId。
        private readonly Dictionary<string, CharacterRedDotState> _statesByCharacter;

        // 訂閱 handler（保留欄位以供 Dispose 取消）
        private readonly Action<CommissionCompletedEvent> _onCommissionCompleted;
        private readonly Action<CommissionClaimedEvent> _onCommissionClaimed;
        private readonly Action<MainQuestAvailableEvent> _onMainQuestAvailable;
        private readonly Action<MainQuestStartedEvent> _onMainQuestStarted;
        private readonly Action<MainQuestCompletedEvent> _onMainQuestCompleted;
        private readonly Action<AffinityThresholdReachedEvent> _onAffinityThresholdReached;
        private readonly Action<CharacterUnlockedEvent> _onCharacterUnlocked;

        private bool _disposed;

        /// <summary>
        /// 建構紅點管理器。
        /// </summary>
        /// <param name="mainQuestConfig">主線任務配置（不可為 null；用於查詢 quest 的 owner_character_id）。</param>
        /// <param name="mainQuestManager">主線任務管理器（不可為 null；用於初始化時掃描 Available 任務）。</param>
        /// <exception cref="ArgumentNullException">任一參數為 null 時拋出。</exception>
        public RedDotManager(
            MainQuestConfig mainQuestConfig,
            MainQuestManager mainQuestManager)
        {
            _mainQuestConfig = mainQuestConfig ?? throw new ArgumentNullException(nameof(mainQuestConfig));
            _mainQuestManager = mainQuestManager ?? throw new ArgumentNullException(nameof(mainQuestManager));

            _statesByCharacter = new Dictionary<string, CharacterRedDotState>();

            _onCommissionCompleted = OnCommissionCompleted;
            _onCommissionClaimed = OnCommissionClaimed;
            _onMainQuestAvailable = OnMainQuestAvailable;
            _onMainQuestStarted = OnMainQuestStarted;
            _onMainQuestCompleted = OnMainQuestCompleted;
            _onAffinityThresholdReached = OnAffinityThresholdReached;
            _onCharacterUnlocked = OnCharacterUnlocked;

            EventBus.Subscribe(_onCommissionCompleted);
            EventBus.Subscribe(_onCommissionClaimed);
            EventBus.Subscribe(_onMainQuestAvailable);
            EventBus.Subscribe(_onMainQuestStarted);
            EventBus.Subscribe(_onMainQuestCompleted);
            EventBus.Subscribe(_onAffinityThresholdReached);
            EventBus.Subscribe(_onCharacterUnlocked);

            // 建構時掃描 MainQuestManager 現有 Available 任務以同步初始狀態
            // （MainQuestManager 建構時的 Available 事件可能早於 RedDotManager 建構）
            InitialSyncFromMainQuestManager();
        }

        // ===== 公開 API =====

        /// <summary>
        /// 取得指定角色的紅點資訊（最高層級與是否顯示）。
        /// </summary>
        public HubRedDotInfo GetHubRedDot(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return new HubRedDotInfo(characterId, RedDotLayer.None);
            }
            CharacterRedDotState state = GetOrCreateState(characterId);
            return new HubRedDotInfo(characterId, state.GetHighestLayer());
        }

        /// <summary>判斷指定角色是否有任一層紅點啟用。</summary>
        public bool HasAnyRedDot(string characterId)
        {
            return GetHubRedDot(characterId).ShouldShow;
        }

        /// <summary>取得所有目前有紅點的角色 ID 清單（不保證排序）。</summary>
        public IReadOnlyList<string> GetCharactersWithRedDot()
        {
            List<string> result = new List<string>();
            foreach (KeyValuePair<string, CharacterRedDotState> kvp in _statesByCharacter)
            {
                if (kvp.Value.GetHighestLayer() != RedDotLayer.None)
                {
                    result.Add(kvp.Key);
                }
            }
            return result.AsReadOnly();
        }

        /// <summary>
        /// 手動標記某角色的 L2 角色發問層。
        /// 供上層（例如 DialogueController 在完成一次角色主動發問對話後）清除狀態。
        /// </summary>
        public void SetCharacterQuestionFlag(string characterId, bool enabled)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            UpdateLayer(characterId, RedDotLayer.CharacterQuestion, enabled);
        }

        /// <summary>
        /// 手動標記某角色的 L4 主線事件層。
        /// 供上層（例如 OpeningSequenceController/NodeDialogueController 在播放完節點劇情後清除狀態）。
        /// </summary>
        public void SetMainQuestEventFlag(string characterId, bool enabled)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            UpdateLayer(characterId, RedDotLayer.MainQuestEvent, enabled);
        }

        /// <summary>
        /// 手動標記某角色的「首次登場」紅點。
        /// 角色剛解鎖、玩家尚未進入其互動畫面（未播放登場 CG）時顯示。
        /// 進入互動畫面並播放 CG 後，由上層呼叫本方法並傳 false 清除。
        /// </summary>
        public void SetFirstMeetFlag(string characterId, bool enabled)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            UpdateLayer(characterId, RedDotLayer.FirstMeet, enabled);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            EventBus.Unsubscribe(_onCommissionCompleted);
            EventBus.Unsubscribe(_onCommissionClaimed);
            EventBus.Unsubscribe(_onMainQuestAvailable);
            EventBus.Unsubscribe(_onMainQuestStarted);
            EventBus.Unsubscribe(_onMainQuestCompleted);
            EventBus.Unsubscribe(_onAffinityThresholdReached);
            EventBus.Unsubscribe(_onCharacterUnlocked);
        }

        // ===== 事件處理 =====

        /// <summary>L1 委託完成：任何 slot 進入 Completed → 該角色紅點亮。</summary>
        private void OnCommissionCompleted(CommissionCompletedEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.CharacterId)) return;
            UpdateLayer(e.CharacterId, RedDotLayer.CommissionCompleted, true);
        }

        /// <summary>
        /// L1 委託領取：若該角色已無任何 Completed slot 則關閉 L1。
        /// 注意：此事件只告知領取結束，仍可能有其他 Completed slot，
        /// 但我們無法直接查詢。採保守策略 — 直接關閉 L1；
        /// 若還有其他 slot 已完成，下一次 Tick 仍會透過 CommissionCompletedEvent 重新點亮。
        /// （Tick 已針對每個 slot 邊界發布一次 CompletedEvent）
        /// </summary>
        private void OnCommissionClaimed(CommissionClaimedEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.CharacterId)) return;
            UpdateLayer(e.CharacterId, RedDotLayer.CommissionCompleted, false);
        }

        /// <summary>L3 新任務：某任務從 Locked → Available → 對應角色紅點亮。</summary>
        private void OnMainQuestAvailable(MainQuestAvailableEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.QuestId)) return;
            string ownerId = GetQuestOwner(e.QuestId);
            if (string.IsNullOrEmpty(ownerId)) return;
            UpdateLayer(ownerId, RedDotLayer.NewQuest, true);
        }

        /// <summary>L3 新任務：任務承接後（Available → InProgress），L3 關閉。</summary>
        private void OnMainQuestStarted(MainQuestStartedEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.QuestId)) return;
            string ownerId = GetQuestOwner(e.QuestId);
            if (string.IsNullOrEmpty(ownerId)) return;

            // 該角色是否還有其他 Available 任務？若有則維持 L3；否則關閉。
            if (!HasAnyAvailableQuestFor(ownerId))
            {
                UpdateLayer(ownerId, RedDotLayer.NewQuest, false);
            }
        }

        /// <summary>
        /// L4 主線事件：特定任務完成時，對應的節點角色 L4 紅點亮起。
        /// 目前採簡化規則：
        /// - T1 完成（character-unlock-system.md QC-D：T1 觸發節點 1）→ VillageChiefWife L4 亮
        /// - T3 完成（節點 2）→ VillageChiefWife L4 亮
        /// - 其他任務完成後 → 依 owner_character_id 決定是否需要清除 L4（保守做法：任務 Completed → 該 owner 的 L4 清除）
        /// </summary>
        private void OnMainQuestCompleted(MainQuestCompletedEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.QuestId)) return;

            // 任務完成時，先關閉該 owner 的 L3（該任務已不是 Available）
            // 注意：MainQuestStarted 事件已處理從 Available 開始 → InProgress 時的 L3 關閉，
            // 但某些自動完成任務（Auto 類型）會跳過 Started，所以此處再保守清一次。
            string ownerId = GetQuestOwner(e.QuestId);
            if (!string.IsNullOrEmpty(ownerId) && !HasAnyAvailableQuestFor(ownerId))
            {
                UpdateLayer(ownerId, RedDotLayer.NewQuest, false);
            }

            // L4 節點觸發：T1 → 節點 1；T3 → 節點 2（character-unlock-system.md § 6.2）
            if (e.QuestId == QuestIdsTriggersNode1 || e.QuestId == QuestIdsTriggersNode2)
            {
                UpdateLayer(CharacterIds.VillageChiefWife, RedDotLayer.MainQuestEvent, true);
            }
        }

        /// <summary>L2 角色發問：好感度門檻達成 → 該角色 L2 亮起（placeholder）。</summary>
        private void OnAffinityThresholdReached(AffinityThresholdReachedEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.CharacterId)) return;
            UpdateLayer(e.CharacterId, RedDotLayer.CharacterQuestion, true);
        }

        /// <summary>
        /// 角色解鎖事件：建立該角色的空狀態 + 點亮 FirstMeet 紅點（玩家尚未進入其互動畫面）。
        /// FirstMeet 紅點會在玩家首次進入該角色互動畫面、CG 播放完成後由上層呼叫
        /// SetFirstMeetFlag(charId, false) 清除（見 VillageEntryPoint.InitializeCharacterView 的 CG 回呼）。
        /// </summary>
        private void OnCharacterUnlocked(CharacterUnlockedEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.CharacterId)) return;
            GetOrCreateState(e.CharacterId);
            UpdateLayer(e.CharacterId, RedDotLayer.FirstMeet, true);
        }

        // ===== 私有工具 =====

        /// <summary>
        /// 節點 1 的觸發任務 ID（character-unlock-system.md § 6.2）。
        /// GDD 定義：T1 = 節點 1 觸發。
        /// </summary>
        public const string QuestIdsTriggersNode1 = "T1";

        /// <summary>節點 2 的觸發任務 ID（T3 = 節點 2 觸發）。</summary>
        public const string QuestIdsTriggersNode2 = "T3";

        private void InitialSyncFromMainQuestManager()
        {
            foreach (MainQuestInfo quest in _mainQuestConfig.OrderedQuests)
            {
                if (_mainQuestManager.GetState(quest.QuestId) == MainQuestState.Available)
                {
                    if (!string.IsNullOrEmpty(quest.OwnerCharacterId))
                    {
                        UpdateLayer(quest.OwnerCharacterId, RedDotLayer.NewQuest, true);
                    }
                }
            }
        }

        private string GetQuestOwner(string questId)
        {
            MainQuestInfo info = _mainQuestConfig.GetQuest(questId);
            return info?.OwnerCharacterId;
        }

        private bool HasAnyAvailableQuestFor(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return false;
            foreach (MainQuestInfo quest in _mainQuestConfig.OrderedQuests)
            {
                if (quest.OwnerCharacterId == characterId
                    && _mainQuestManager.GetState(quest.QuestId) == MainQuestState.Available)
                {
                    return true;
                }
            }
            return false;
        }

        private CharacterRedDotState GetOrCreateState(string characterId)
        {
            if (!_statesByCharacter.TryGetValue(characterId, out CharacterRedDotState state))
            {
                state = new CharacterRedDotState();
                _statesByCharacter[characterId] = state;
            }
            return state;
        }

        /// <summary>
        /// 更新某角色某層的啟用狀態。
        /// 若變更導致 HighestLayer 改變，則發布 RedDotUpdatedEvent。
        /// </summary>
        private void UpdateLayer(string characterId, RedDotLayer layer, bool enabled)
        {
            CharacterRedDotState state = GetOrCreateState(characterId);
            RedDotLayer before = state.GetHighestLayer();
            state.SetLayer(layer, enabled);
            RedDotLayer after = state.GetHighestLayer();

            if (before != after)
            {
                EventBus.Publish(new RedDotUpdatedEvent
                {
                    CharacterId = characterId,
                    HighestLayer = after,
                });
            }
        }

        // ===== 內部狀態物件 =====

        /// <summary>
        /// 單一角色的紅點層啟用狀態。
        /// 以 bool 陣列索引對應各 RedDotLayer（見 ToIndex）。
        /// </summary>
        private class CharacterRedDotState
        {
            // 0=L1CommissionCompleted, 1=L2CharacterQuestion, 2=L3NewQuest, 3=L4MainQuestEvent, 4=FirstMeet
            private readonly bool[] _flags = new bool[5];

            public void SetLayer(RedDotLayer layer, bool enabled)
            {
                int index = ToIndex(layer);
                if (index < 0) return;
                _flags[index] = enabled;
            }

            public bool GetLayer(RedDotLayer layer)
            {
                int index = ToIndex(layer);
                if (index < 0) return false;
                return _flags[index];
            }

            /// <summary>
            /// 依優先序 L1 > L4 > FirstMeet > L3 > L2 找出最高啟用層。
            /// 全部關閉時回傳 None。
            /// </summary>
            public RedDotLayer GetHighestLayer()
            {
                if (_flags[0]) return RedDotLayer.CommissionCompleted;  // L1
                if (_flags[3]) return RedDotLayer.MainQuestEvent;        // L4
                if (_flags[4]) return RedDotLayer.FirstMeet;             // 首次登場
                if (_flags[2]) return RedDotLayer.NewQuest;              // L3
                if (_flags[1]) return RedDotLayer.CharacterQuestion;     // L2
                return RedDotLayer.None;
            }

            private static int ToIndex(RedDotLayer layer)
            {
                switch (layer)
                {
                    case RedDotLayer.CommissionCompleted: return 0;
                    case RedDotLayer.CharacterQuestion:   return 1;
                    case RedDotLayer.NewQuest:            return 2;
                    case RedDotLayer.MainQuestEvent:      return 3;
                    case RedDotLayer.FirstMeet:           return 4;
                    default: return -1;
                }
            }
        }
    }
}
