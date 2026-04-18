// StorageTransferManager — 背包與倉庫之間的雙向物品轉移管理器。
// 負責協調 BackpackManager 與 StorageManager 之間的物品轉移操作。

using System;

namespace ProjectDR.Village
{
    /// <summary>
    /// 背包與倉庫之間的雙向物品轉移管理器。
    /// 提供從背包指定格子轉移至倉庫，以及從倉庫轉移至背包的功能。
    /// </summary>
    public class StorageTransferManager
    {
        private readonly BackpackManager _backpack;
        private readonly StorageManager _warehouse;

        public StorageTransferManager(BackpackManager backpack, StorageManager warehouse)
        {
            _backpack = backpack ?? throw new ArgumentNullException(nameof(backpack));
            _warehouse = warehouse ?? throw new ArgumentNullException(nameof(warehouse));
        }

        /// <summary>
        /// 將背包指定格子的物品轉移至倉庫。
        /// 回傳實際轉移的數量。
        /// </summary>
        /// <param name="slotIndex">背包格子索引。</param>
        /// <param name="quantity">欲轉移的數量（必須大於 0）。</param>
        public int TransferToWarehouse(int slotIndex, int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentException("數量必須大於 0。", nameof(quantity));
            }

            // 取得該格子的物品資訊
            var slots = _backpack.GetSlots();

            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "格子索引超出範圍。");
            }

            BackpackSlot slot = slots[slotIndex];

            if (slot.IsEmpty)
            {
                return 0;
            }

            // 從背包預計移除的量不可超過實際持有
            int plannedRemove = quantity < slot.Quantity ? quantity : slot.Quantity;

            // 倉庫容量可能不足，僅能接受部分，實際轉移量以倉庫可容納為準
            // 先嘗試加入倉庫，回傳實際加入量
            int addedToWarehouse = _warehouse.TryAddItem(slot.ItemId, plannedRemove);

            if (addedToWarehouse <= 0)
            {
                return 0;
            }

            // 自背包移除對應的實際加入量
            int removed = _backpack.RemoveFromSlot(slotIndex, addedToWarehouse);
            return removed;
        }

        /// <summary>
        /// 將倉庫的物品轉移至背包。
        /// 回傳實際轉移的數量（可能因背包空間不足而少於 quantity）。
        /// </summary>
        /// <param name="itemId">物品 ID。</param>
        /// <param name="quantity">欲轉移的數量（必須大於 0）。</param>
        public int TransferToBackpack(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentException("物品 ID 不可為空。", nameof(itemId));
            }

            if (quantity <= 0)
            {
                throw new ArgumentException("數量必須大於 0。", nameof(quantity));
            }

            // 檢查倉庫實際有多少
            int warehouseCount = _warehouse.GetItemCount(itemId);
            if (warehouseCount == 0)
            {
                return 0;
            }

            // 實際可轉移量不超過倉庫持有量
            int toTransfer = quantity < warehouseCount ? quantity : warehouseCount;

            // 嘗試加入背包（可能只能加入部分）
            int actualAdded = _backpack.AddItem(itemId, toTransfer);

            if (actualAdded > 0)
            {
                // 從倉庫移除已成功加入背包的數量
                _warehouse.RemoveItem(itemId, actualAdded);
            }

            return actualAdded;
        }
    }
}
