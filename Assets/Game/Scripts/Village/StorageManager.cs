// StorageManager — 玩家物品庫存管理器。
// IT 階段例外：物品以字串 ID 識別，無需 IGameData（物品 metadata 在後續 Sprint 定義）。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// 玩家物品庫存管理器。
    /// 管理玩家持有的物品數量，支援新增、移除、查詢與事件通知。
    /// </summary>
    public class StorageManager
    {
        private readonly Dictionary<string, int> _items = new Dictionary<string, int>();

        /// <summary>
        /// 新增物品至庫存。
        /// 若 quantity 小於等於 0，拋出 ArgumentException。
        /// 成功新增後發布 StorageChangedEvent。
        /// </summary>
        public void AddItem(string itemId, int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentException("數量必須大於 0。", nameof(quantity));
            }

            if (_items.ContainsKey(itemId))
            {
                _items[itemId] += quantity;
            }
            else
            {
                _items[itemId] = quantity;
            }

            EventBus.Publish(new StorageChangedEvent
            {
                ItemId = itemId,
                NewQuantity = _items[itemId]
            });
        }

        /// <summary>
        /// 從庫存移除物品。
        /// 若 quantity 小於等於 0，拋出 ArgumentException。
        /// 若庫存不足，回傳 false 且不改變狀態。
        /// 成功移除後發布 StorageChangedEvent。
        /// </summary>
        public bool RemoveItem(string itemId, int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentException("數量必須大於 0。", nameof(quantity));
            }

            if (!_items.ContainsKey(itemId) || _items[itemId] < quantity)
            {
                return false;
            }

            _items[itemId] -= quantity;

            EventBus.Publish(new StorageChangedEvent
            {
                ItemId = itemId,
                NewQuantity = _items[itemId]
            });

            return true;
        }

        /// <summary>查詢指定物品的數量。若不存在，回傳 0。</summary>
        public int GetItemCount(string itemId)
        {
            return _items.TryGetValue(itemId, out int count) ? count : 0;
        }

        /// <summary>取得所有庫存物品的快照（唯讀）。</summary>
        public IReadOnlyDictionary<string, int> GetAllItems()
        {
            return _items;
        }

        /// <summary>檢查是否擁有指定數量（含）以上的物品。</summary>
        public bool HasItem(string itemId, int quantity)
        {
            return GetItemCount(itemId) >= quantity;
        }
    }
}
