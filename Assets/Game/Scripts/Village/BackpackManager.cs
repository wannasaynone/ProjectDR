// BackpackManager — 格子制背包管理器。
// IT 階段例外：物品以字串 ID 識別，無需 IGameData（物品 metadata 在後續 Sprint 定義）。
// maxSlots 與 defaultMaxStack 由外部傳入，不寫死於程式碼中。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// 格子制背包管理器。
    /// 每個格子存放一種物品，有獨立的數量與堆疊上限。
    /// 背包有最大格子數限制。
    /// </summary>
    public class BackpackManager
    {
        private readonly int _maxSlots;
        private readonly int _defaultMaxStack;
        private readonly BackpackSlot[] _slots;

        /// <summary>背包是否已滿（所有格子都有物品且均達最大堆疊數）。</summary>
        public bool IsFull
        {
            get
            {
                for (int i = 0; i < _maxSlots; i++)
                {
                    if (_slots[i].IsEmpty || _slots[i].Quantity < _defaultMaxStack)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>背包是否為空（所有格子都沒有物品）。</summary>
        public bool IsEmpty
        {
            get
            {
                for (int i = 0; i < _maxSlots; i++)
                {
                    if (!_slots[i].IsEmpty)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>最大格子數。</summary>
        public int MaxSlots => _maxSlots;

        /// <summary>預設最大堆疊數。</summary>
        public int DefaultMaxStack => _defaultMaxStack;

        /// <summary>
        /// 建立背包管理器。
        /// </summary>
        /// <param name="maxSlots">最大格子數（必須大於 0）。</param>
        /// <param name="defaultMaxStack">預設最大堆疊數（必須大於 0）。</param>
        public BackpackManager(int maxSlots, int defaultMaxStack)
        {
            if (maxSlots <= 0)
            {
                throw new ArgumentException("最大格子數必須大於 0。", nameof(maxSlots));
            }

            if (defaultMaxStack <= 0)
            {
                throw new ArgumentException("預設最大堆疊數必須大於 0。", nameof(defaultMaxStack));
            }

            _maxSlots = maxSlots;
            _defaultMaxStack = defaultMaxStack;
            _slots = new BackpackSlot[maxSlots];

            for (int i = 0; i < maxSlots; i++)
            {
                _slots[i] = BackpackSlot.Empty;
            }
        }

        /// <summary>
        /// 新增物品至背包。
        /// 優先堆疊至已有相同物品的格子，再使用空格子。
        /// 回傳實際加入的數量（可能因背包空間不足而少於 quantity）。
        /// 若 quantity 小於等於 0，拋出 ArgumentException。
        /// </summary>
        public int AddItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentException("物品 ID 不可為空。", nameof(itemId));
            }

            if (quantity <= 0)
            {
                throw new ArgumentException("數量必須大於 0。", nameof(quantity));
            }

            int remaining = quantity;

            // 第一輪：堆疊至已有相同物品的格子
            for (int i = 0; i < _maxSlots && remaining > 0; i++)
            {
                if (_slots[i].ItemId == itemId && _slots[i].Quantity < _defaultMaxStack)
                {
                    int canAdd = _defaultMaxStack - _slots[i].Quantity;
                    int toAdd = remaining < canAdd ? remaining : canAdd;
                    _slots[i] = new BackpackSlot(itemId, _slots[i].Quantity + toAdd);
                    remaining -= toAdd;
                }
            }

            // 第二輪：使用空格子
            for (int i = 0; i < _maxSlots && remaining > 0; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    int toAdd = remaining < _defaultMaxStack ? remaining : _defaultMaxStack;
                    _slots[i] = new BackpackSlot(itemId, toAdd);
                    remaining -= toAdd;
                }
            }

            int actualAdded = quantity - remaining;

            if (actualAdded > 0)
            {
                EventBus.Publish(new BackpackChangedEvent
                {
                    ItemId = itemId,
                    TotalQuantity = GetItemCount(itemId)
                });
            }

            return actualAdded;
        }

        /// <summary>
        /// 從背包移除物品。
        /// 從後往前搜尋含有該物品的格子進行移除。
        /// 回傳實際移除的數量（可能因庫存不足而少於 quantity）。
        /// 若 quantity 小於等於 0，拋出 ArgumentException。
        /// </summary>
        public int RemoveItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentException("物品 ID 不可為空。", nameof(itemId));
            }

            if (quantity <= 0)
            {
                throw new ArgumentException("數量必須大於 0。", nameof(quantity));
            }

            int remaining = quantity;

            // 從後往前移除，優先清空後面的格子
            for (int i = _maxSlots - 1; i >= 0 && remaining > 0; i--)
            {
                if (_slots[i].ItemId == itemId)
                {
                    int canRemove = _slots[i].Quantity;
                    int toRemove = remaining < canRemove ? remaining : canRemove;
                    int newQuantity = _slots[i].Quantity - toRemove;

                    _slots[i] = newQuantity > 0
                        ? new BackpackSlot(itemId, newQuantity)
                        : BackpackSlot.Empty;

                    remaining -= toRemove;
                }
            }

            int actualRemoved = quantity - remaining;

            if (actualRemoved > 0)
            {
                EventBus.Publish(new BackpackChangedEvent
                {
                    ItemId = itemId,
                    TotalQuantity = GetItemCount(itemId)
                });
            }

            return actualRemoved;
        }

        /// <summary>
        /// 從指定格子移除指定數量的物品。
        /// 回傳實際移除的數量。
        /// </summary>
        public int RemoveFromSlot(int slotIndex, int quantity)
        {
            if (slotIndex < 0 || slotIndex >= _maxSlots)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "格子索引超出範圍。");
            }

            if (quantity <= 0)
            {
                throw new ArgumentException("數量必須大於 0。", nameof(quantity));
            }

            if (_slots[slotIndex].IsEmpty)
            {
                return 0;
            }

            string itemId = _slots[slotIndex].ItemId;
            int canRemove = _slots[slotIndex].Quantity;
            int toRemove = quantity < canRemove ? quantity : canRemove;
            int newQuantity = canRemove - toRemove;

            _slots[slotIndex] = newQuantity > 0
                ? new BackpackSlot(itemId, newQuantity)
                : BackpackSlot.Empty;

            if (toRemove > 0)
            {
                EventBus.Publish(new BackpackChangedEvent
                {
                    ItemId = itemId,
                    TotalQuantity = GetItemCount(itemId)
                });
            }

            return toRemove;
        }

        /// <summary>取得所有格子的唯讀快照。</summary>
        public IReadOnlyList<BackpackSlot> GetSlots()
        {
            BackpackSlot[] copy = new BackpackSlot[_maxSlots];
            Array.Copy(_slots, copy, _maxSlots);
            return copy;
        }

        /// <summary>查詢指定物品在背包中的總數量。</summary>
        public int GetItemCount(string itemId)
        {
            int total = 0;
            for (int i = 0; i < _maxSlots; i++)
            {
                if (_slots[i].ItemId == itemId)
                {
                    total += _slots[i].Quantity;
                }
            }
            return total;
        }

        /// <summary>拍攝背包快照。</summary>
        public BackpackSnapshot TakeSnapshot()
        {
            return new BackpackSnapshot(GetSlots());
        }

        /// <summary>
        /// 從快照回溯背包狀態。
        /// 快照的格子數必須等於背包的最大格子數。
        /// </summary>
        public void RestoreSnapshot(BackpackSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.SlotCount != _maxSlots)
            {
                throw new ArgumentException(
                    $"快照格子數 ({snapshot.SlotCount}) 與背包格子數 ({_maxSlots}) 不一致。",
                    nameof(snapshot));
            }

            IReadOnlyList<BackpackSlot> snapshotSlots = snapshot.GetSlots();
            for (int i = 0; i < _maxSlots; i++)
            {
                _slots[i] = snapshotSlots[i];
            }

            EventBus.Publish(new BackpackChangedEvent
            {
                ItemId = null,
                TotalQuantity = 0
            });
        }
    }
}
