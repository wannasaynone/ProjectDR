// StorageManager — 玩家倉庫管理器（格子制 + 堆疊）。
// 依據 GDD `storage-expansion.md`：倉庫規格與背包一致，初始 100 格，每次擴建 +50 格。
// IT 階段例外：物品以字串 ID 識別，無需 IGameData（物品 metadata 在後續 Sprint 定義）。
// maxSlots 與 defaultMaxStack 由外部傳入，不寫死於程式碼中。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// 玩家倉庫管理器（格子制 + 堆疊）。
    /// 每個格子存放一種物品，有獨立的數量與堆疊上限。
    /// 倉庫有容量上限（格子數），可透過 ExpandCapacity 擴建。
    /// 與 BackpackManager 結構相同，以利 UI 與轉移邏輯共用。
    /// </summary>
    public class StorageManager
    {
        /// <summary>
        /// 無參數建構函式，採用既定預設值（100 格容量 + 99 堆疊上限）。
        /// 保留此建構函式以向下相容既有測試與舊有呼叫端。
        /// </summary>
        public const int DefaultInitialCapacity = 100;

        /// <summary>預設堆疊上限，與背包相同（99）。</summary>
        public const int DefaultMaxStackValue = 99;

        private readonly int _defaultMaxStack;
        private readonly List<BackpackSlot> _slots;
        private int _capacity;

        /// <summary>倉庫當前容量（格子數）。</summary>
        public int Capacity => _capacity;

        /// <summary>預設堆疊上限。</summary>
        public int DefaultMaxStack => _defaultMaxStack;

        /// <summary>倉庫是否已滿（所有格子都有物品且均達堆疊上限）。</summary>
        public bool IsFull
        {
            get
            {
                if (_capacity == 0)
                {
                    return true;
                }

                int occupied = 0;
                for (int i = 0; i < _capacity; i++)
                {
                    BackpackSlot slot = _slots[i];
                    if (!slot.IsEmpty && slot.Quantity >= _defaultMaxStack)
                    {
                        occupied++;
                    }
                }
                return occupied == _capacity;
            }
        }

        /// <summary>
        /// 無參數建構函式（既有相容）：容量 100 格、堆疊上限 99。
        /// </summary>
        public StorageManager() : this(DefaultInitialCapacity, DefaultMaxStackValue)
        {
        }

        /// <summary>
        /// 建立倉庫管理器（容量與堆疊上限由外部指定）。
        /// </summary>
        /// <param name="initialCapacity">初始格子數（必須大於 0）。</param>
        /// <param name="defaultMaxStack">預設堆疊上限（必須大於 0）。</param>
        public StorageManager(int initialCapacity, int defaultMaxStack)
        {
            if (initialCapacity <= 0)
            {
                throw new ArgumentException("初始容量必須大於 0。", nameof(initialCapacity));
            }

            if (defaultMaxStack <= 0)
            {
                throw new ArgumentException("預設堆疊上限必須大於 0。", nameof(defaultMaxStack));
            }

            _capacity = initialCapacity;
            _defaultMaxStack = defaultMaxStack;
            _slots = new List<BackpackSlot>(initialCapacity);

            for (int i = 0; i < initialCapacity; i++)
            {
                _slots.Add(BackpackSlot.Empty);
            }
        }

        /// <summary>
        /// 新增物品至倉庫（保留既有 API 語意）。
        /// 優先堆疊至已有相同物品的格子，再使用空格子。
        /// 若 quantity 小於等於 0，拋出 ArgumentException。
        /// 若倉庫空間不足以容納全部 quantity，拋出 InvalidOperationException（GDD 超出容量策略 A：完全拒絕放入）。
        /// 呼叫端若需要部分加入語意請改用 TryAddItem。
        /// 成功新增後發布 StorageChangedEvent。
        /// </summary>
        public void AddItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentException("物品 ID 不可為空。", nameof(itemId));
            }

            if (quantity <= 0)
            {
                throw new ArgumentException("數量必須大於 0。", nameof(quantity));
            }

            if (!CanAddItem(itemId, quantity))
            {
                throw new InvalidOperationException(
                    $"倉庫空間不足以放入 {itemId} ×{quantity}（當前容量 {_capacity} 格）。");
            }

            int added = AddItemInternal(itemId, quantity);
            if (added > 0)
            {
                EventBus.Publish(new StorageChangedEvent
                {
                    ItemId = itemId,
                    NewQuantity = GetItemCount(itemId)
                });
            }
        }

        /// <summary>
        /// 嘗試新增物品至倉庫，容量不足時部分加入。
        /// 回傳實際加入的數量（可能少於 quantity）。
        /// 若實際加入量 &gt; 0 則發布 StorageChangedEvent。
        /// </summary>
        public int TryAddItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentException("物品 ID 不可為空。", nameof(itemId));
            }

            if (quantity <= 0)
            {
                throw new ArgumentException("數量必須大於 0。", nameof(quantity));
            }

            int added = AddItemInternal(itemId, quantity);
            if (added > 0)
            {
                EventBus.Publish(new StorageChangedEvent
                {
                    ItemId = itemId,
                    NewQuantity = GetItemCount(itemId)
                });
            }
            return added;
        }

        /// <summary>
        /// 檢查是否可完整加入 quantity 個 itemId。
        /// 考慮既有相同物品格子的剩餘堆疊與空格的可用容量。
        /// </summary>
        public bool CanAddItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
            {
                return false;
            }

            int remaining = quantity;

            for (int i = 0; i < _capacity && remaining > 0; i++)
            {
                BackpackSlot slot = _slots[i];
                if (slot.IsEmpty)
                {
                    remaining -= _defaultMaxStack;
                }
                else if (slot.ItemId == itemId && slot.Quantity < _defaultMaxStack)
                {
                    remaining -= (_defaultMaxStack - slot.Quantity);
                }

                if (remaining <= 0)
                {
                    return true;
                }
            }

            return remaining <= 0;
        }

        /// <summary>
        /// 從倉庫移除物品。
        /// 若 quantity 小於等於 0，拋出 ArgumentException。
        /// 若倉庫不足，回傳 false 且不改變狀態。
        /// 成功移除後發布 StorageChangedEvent。
        /// </summary>
        public bool RemoveItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentException("物品 ID 不可為空。", nameof(itemId));
            }

            if (quantity <= 0)
            {
                throw new ArgumentException("數量必須大於 0。", nameof(quantity));
            }

            if (GetItemCount(itemId) < quantity)
            {
                return false;
            }

            int remaining = quantity;

            // 從後往前移除，優先清空後面的格子
            for (int i = _capacity - 1; i >= 0 && remaining > 0; i--)
            {
                BackpackSlot slot = _slots[i];
                if (slot.ItemId == itemId)
                {
                    int canRemove = slot.Quantity;
                    int toRemove = remaining < canRemove ? remaining : canRemove;
                    int newQuantity = slot.Quantity - toRemove;

                    _slots[i] = newQuantity > 0
                        ? new BackpackSlot(itemId, newQuantity)
                        : BackpackSlot.Empty;

                    remaining -= toRemove;
                }
            }

            EventBus.Publish(new StorageChangedEvent
            {
                ItemId = itemId,
                NewQuantity = GetItemCount(itemId)
            });

            return true;
        }

        /// <summary>查詢指定物品的數量。若不存在，回傳 0。</summary>
        public int GetItemCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < _capacity; i++)
            {
                if (_slots[i].ItemId == itemId)
                {
                    total += _slots[i].Quantity;
                }
            }
            return total;
        }

        /// <summary>取得所有庫存物品的快照（唯讀）。由格子資料聚合而成。</summary>
        public IReadOnlyDictionary<string, int> GetAllItems()
        {
            Dictionary<string, int> aggregated = new Dictionary<string, int>();
            for (int i = 0; i < _capacity; i++)
            {
                BackpackSlot slot = _slots[i];
                if (slot.IsEmpty)
                {
                    continue;
                }

                if (aggregated.ContainsKey(slot.ItemId))
                {
                    aggregated[slot.ItemId] += slot.Quantity;
                }
                else
                {
                    aggregated[slot.ItemId] = slot.Quantity;
                }
            }
            return aggregated;
        }

        /// <summary>取得所有格子的唯讀快照。</summary>
        public IReadOnlyList<BackpackSlot> GetSlots()
        {
            BackpackSlot[] copy = new BackpackSlot[_capacity];
            for (int i = 0; i < _capacity; i++)
            {
                copy[i] = _slots[i];
            }
            return copy;
        }

        /// <summary>檢查是否擁有指定數量（含）以上的物品。</summary>
        public bool HasItem(string itemId, int quantity)
        {
            return GetItemCount(itemId) >= quantity;
        }

        /// <summary>取得目前已使用的格子數量（含有物品的格子）。</summary>
        public int GetUsedSlots()
        {
            int used = 0;
            for (int i = 0; i < _capacity; i++)
            {
                if (!_slots[i].IsEmpty)
                {
                    used++;
                }
            }
            return used;
        }

        /// <summary>
        /// 擴建容量：增加 delta 個格子。
        /// 若 delta 小於等於 0，拋出 ArgumentException。
        /// 擴建後發布 StorageCapacityChangedEvent。
        /// </summary>
        public void ExpandCapacity(int delta)
        {
            if (delta <= 0)
            {
                throw new ArgumentException("擴建格數必須大於 0。", nameof(delta));
            }

            int previousCapacity = _capacity;
            int newCapacity = _capacity + delta;

            for (int i = 0; i < delta; i++)
            {
                _slots.Add(BackpackSlot.Empty);
            }

            _capacity = newCapacity;

            EventBus.Publish(new StorageCapacityChangedEvent
            {
                PreviousCapacity = previousCapacity,
                NewCapacity = newCapacity
            });
        }

        // ===== 內部邏輯 =====

        /// <summary>
        /// 內部新增邏輯：優先堆疊至既有相同物品，再使用空格子。
        /// 回傳實際加入的數量。不發布事件（由呼叫端發布）。
        /// </summary>
        private int AddItemInternal(string itemId, int quantity)
        {
            int remaining = quantity;

            // 第一輪：堆疊至既有相同物品的格子
            for (int i = 0; i < _capacity && remaining > 0; i++)
            {
                BackpackSlot slot = _slots[i];
                if (slot.ItemId == itemId && slot.Quantity < _defaultMaxStack)
                {
                    int canAdd = _defaultMaxStack - slot.Quantity;
                    int toAdd = remaining < canAdd ? remaining : canAdd;
                    _slots[i] = new BackpackSlot(itemId, slot.Quantity + toAdd);
                    remaining -= toAdd;
                }
            }

            // 第二輪：使用空格子
            for (int i = 0; i < _capacity && remaining > 0; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    int toAdd = remaining < _defaultMaxStack ? remaining : _defaultMaxStack;
                    _slots[i] = new BackpackSlot(itemId, toAdd);
                    remaining -= toAdd;
                }
            }

            return quantity - remaining;
        }
    }
}
