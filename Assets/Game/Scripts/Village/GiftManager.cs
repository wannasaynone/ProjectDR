// GiftManager — 送禮業務邏輯管理器。
// 處理送禮流程：扣除物品（先背包後倉庫）→ 增加好感度。
// 純邏輯類別（非 MonoBehaviour），透過建構子注入相依。

using System;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// 送禮錯誤類型。
    /// </summary>
    public enum GiftError
    {
        /// <summary>無錯誤（成功）。</summary>
        None,

        /// <summary>物品在背包與倉庫中均不足。</summary>
        ItemNotAvailable
    }

    /// <summary>
    /// 送禮結果。
    /// </summary>
    public class GiftResult
    {
        /// <summary>送禮是否成功。</summary>
        public bool IsSuccess { get; }

        /// <summary>失敗時的錯誤類型。成功時為 None。</summary>
        public GiftError Error { get; }

        /// <summary>送禮的目標角色 ID。</summary>
        public string CharacterId { get; }

        /// <summary>送出的物品 ID。</summary>
        public string ItemId { get; }

        private GiftResult(bool isSuccess, GiftError error, string characterId, string itemId)
        {
            IsSuccess = isSuccess;
            Error = error;
            CharacterId = characterId;
            ItemId = itemId;
        }

        /// <summary>建立成功結果。</summary>
        public static GiftResult Success(string characterId, string itemId)
        {
            return new GiftResult(true, GiftError.None, characterId, itemId);
        }

        /// <summary>建立失敗結果。</summary>
        public static GiftResult Failure(GiftError error, string characterId, string itemId)
        {
            return new GiftResult(false, error, characterId, itemId);
        }
    }

    /// <summary>
    /// 送禮業務邏輯管理器。
    /// 處理：選物品 → 扣物品（先背包後倉庫） → 增加好感度（固定 +1）。
    /// 純邏輯類別，不依賴 MonoBehaviour。
    /// </summary>
    public class GiftManager
    {
        private readonly AffinityManager _affinityManager;
        private readonly BackpackManager _backpackManager;
        private readonly StorageManager _storageManager;

        /// <summary>每次送禮增加的好感度量。</summary>
        private const int AffinityPerGift = 1;

        /// <summary>
        /// 建構送禮管理器。
        /// </summary>
        /// <param name="affinityManager">好感度管理器（不可為 null）。</param>
        /// <param name="backpackManager">背包管理器（不可為 null）。</param>
        /// <param name="storageManager">倉庫管理器（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">任一參數為 null 時拋出。</exception>
        public GiftManager(
            AffinityManager affinityManager,
            BackpackManager backpackManager,
            StorageManager storageManager)
        {
            _affinityManager = affinityManager ?? throw new ArgumentNullException(nameof(affinityManager));
            _backpackManager = backpackManager ?? throw new ArgumentNullException(nameof(backpackManager));
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
        }

        /// <summary>
        /// 向指定角色送出一個物品。
        /// 先嘗試從背包扣除 1 個，背包不足則從倉庫扣除 1 個。
        /// 成功扣除後增加好感度 1 點。
        /// </summary>
        /// <param name="characterId">目標角色 ID（不可為 null 或空字串）。</param>
        /// <param name="itemId">物品 ID（不可為 null 或空字串）。</param>
        /// <returns>送禮結果。</returns>
        /// <exception cref="ArgumentNullException">characterId 或 itemId 為 null 時拋出。</exception>
        /// <exception cref="ArgumentException">characterId 或 itemId 為空字串時拋出。</exception>
        public GiftResult GiveGift(string characterId, string itemId)
        {
            if (characterId == null)
            {
                throw new ArgumentNullException(nameof(characterId));
            }
            if (characterId.Length == 0)
            {
                throw new ArgumentException("角色 ID 不可為空字串。", nameof(characterId));
            }
            if (itemId == null)
            {
                throw new ArgumentNullException(nameof(itemId));
            }
            if (itemId.Length == 0)
            {
                throw new ArgumentException("物品 ID 不可為空字串。", nameof(itemId));
            }

            // 嘗試從背包扣除 1 個
            bool removedFromBackpack = TryRemoveFromBackpack(itemId);
            if (removedFromBackpack)
            {
                _affinityManager.AddAffinity(characterId, AffinityPerGift);
                EventBus.Publish(new GiftSuccessEvent { CharacterId = characterId, ItemId = itemId });
                return GiftResult.Success(characterId, itemId);
            }

            // 背包不足，嘗試從倉庫扣除 1 個
            bool removedFromStorage = _storageManager.RemoveItem(itemId, 1);
            if (removedFromStorage)
            {
                _affinityManager.AddAffinity(characterId, AffinityPerGift);
                EventBus.Publish(new GiftSuccessEvent { CharacterId = characterId, ItemId = itemId });
                return GiftResult.Success(characterId, itemId);
            }

            // 兩處都不足
            return GiftResult.Failure(GiftError.ItemNotAvailable, characterId, itemId);
        }

        /// <summary>
        /// 嘗試從背包移除 1 個指定物品。
        /// BackpackManager.RemoveItem 回傳實際移除數量（int），
        /// 此處判斷移除數量是否 >= 1。
        /// </summary>
        private bool TryRemoveFromBackpack(string itemId)
        {
            if (_backpackManager.GetItemCount(itemId) <= 0)
            {
                return false;
            }

            int removed = _backpackManager.RemoveItem(itemId, 1);
            return removed >= 1;
        }
    }
}
