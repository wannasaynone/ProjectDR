// CollectiblePointState — 單一探索點的運行時狀態。
// 管理兩層計時狀態機：Idle -> Gathering -> Unlocking。
// 對應 GDD 規則 8-11, 44, 46。

using System;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// 採集階段列舉。
    /// </summary>
    public enum GatheringPhase
    {
        /// <summary>未開始採集。</summary>
        Idle,
        /// <summary>正在採集中（第一層計時）。</summary>
        Gathering,
        /// <summary>採集完成，物品欄已出現（第二層計時進行中或已完成）。</summary>
        Unlocking
    }

    /// <summary>
    /// 物品欄位狀態列舉。
    /// </summary>
    public enum CollectibleSlotState
    {
        /// <summary>尚未開始解鎖。</summary>
        Locked,
        /// <summary>正在解鎖中。</summary>
        Unlocking,
        /// <summary>已解鎖，等待玩家拾取。</summary>
        Unlocked,
        /// <summary>已被玩家拾取。</summary>
        Taken,
        /// <summary>空格（物品箱中無物品的格子，可供玩家存放物品）。</summary>
        Empty,
        /// <summary>玩家從背包存入的物品（不需解鎖計時，可立即取回）。</summary>
        PlayerStored
    }

    /// <summary>
    /// 物品箱單格資訊（用於 GetAllSlots 回傳）。
    /// </summary>
    public struct BoxSlotInfo
    {
        /// <summary>物品 ID。空格為 null。</summary>
        public string ItemId { get; }
        /// <summary>物品數量。空格為 0。</summary>
        public int Quantity { get; }
        /// <summary>格子狀態。</summary>
        public CollectibleSlotState State { get; }
        /// <summary>是否為玩家存放的物品。</summary>
        public bool IsPlayerStored { get; }

        public BoxSlotInfo(string itemId, int quantity, CollectibleSlotState state, bool isPlayerStored)
        {
            ItemId = itemId;
            Quantity = quantity;
            State = state;
            IsPlayerStored = isPlayerStored;
        }

        /// <summary>建立空格資訊。</summary>
        public static BoxSlotInfo CreateEmpty()
        {
            return new BoxSlotInfo(null, 0, CollectibleSlotState.Empty, false);
        }
    }

    /// <summary>
    /// 單一探索點（物品箱）的運行時狀態。管理兩層計時狀態機。
    /// 物品箱固定 6 格（2x3），地圖定義的物品佔前幾格，其餘為空格。
    /// 支援雙向操作：物品箱→背包（拾取）、背包→物品箱空格（存放）。
    /// </summary>
    public class CollectiblePointState
    {
        /// <summary>物品箱固定格數。</summary>
        public const int MaxSlots = 6;

        private readonly CollectiblePointData _data;
        private readonly float[] _slotElapsedTime;
        private readonly CollectibleSlotState[] _slotStates;

        // 玩家存放物品追蹤（僅 Empty 格子可存放）
        private readonly string[] _storedItemIds;
        private readonly int[] _storedQuantities;

        private GatheringPhase _phase;
        private float _gatheringElapsed;
        private bool _hasBeenOpened;

        /// <summary>當前採集階段。</summary>
        public GatheringPhase Phase => _phase;

        /// <summary>物品箱總格數（固定為 6）。</summary>
        public int SlotCount => MaxSlots;

        /// <summary>地圖定義的物品數量。</summary>
        public int MapItemCount => _data.Items.Count;

        /// <summary>採集進度 (0~1)。Idle 時為 0，Gathering 時為已經過時間/總時間。</summary>
        public float GatheringProgress
        {
            get
            {
                if (_phase != GatheringPhase.Gathering) return _phase == GatheringPhase.Idle ? 0f : 1f;
                if (_data.GatherDurationSeconds <= 0f) return 1f;
                return Math.Min(_gatheringElapsed / _data.GatherDurationSeconds, 1f);
            }
        }

        /// <summary>第一層計時剩餘時間。非 Gathering 狀態時回傳 0。</summary>
        public float GatheringRemainingTime
        {
            get
            {
                if (_phase != GatheringPhase.Gathering) return 0f;
                float remaining = _data.GatherDurationSeconds - _gatheringElapsed;
                return remaining > 0f ? remaining : 0f;
            }
        }

        /// <summary>探索點靜態資料。</summary>
        public CollectiblePointData Data => _data;

        /// <summary>
        /// 建立探索點運行時狀態。物品箱固定 6 格。
        /// </summary>
        /// <param name="data">探索點靜態資料。不可為 null。物品數量不可超過 MaxSlots。</param>
        public CollectiblePointState(CollectiblePointData data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            if (data.Items.Count > MaxSlots)
                throw new ArgumentException(
                    $"Item count ({data.Items.Count}) exceeds MaxSlots ({MaxSlots}).",
                    nameof(data));

            _slotElapsedTime = new float[MaxSlots];
            _slotStates = new CollectibleSlotState[MaxSlots];
            _storedItemIds = new string[MaxSlots];
            _storedQuantities = new int[MaxSlots];

            // 地圖定義的物品佔前 N 格，設為 Locked
            for (int i = 0; i < data.Items.Count; i++)
            {
                _slotStates[i] = CollectibleSlotState.Locked;
            }

            // 其餘格子設為 Empty
            for (int i = data.Items.Count; i < MaxSlots; i++)
            {
                _slotStates[i] = CollectibleSlotState.Empty;
            }

            _phase = GatheringPhase.Idle;
            _gatheringElapsed = 0f;
        }

        /// <summary>
        /// 取得指定欄位的狀態。
        /// </summary>
        public CollectibleSlotState GetSlotState(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            return _slotStates[slotIndex];
        }

        /// <summary>
        /// 取得指定欄位的解鎖進度 (0~1)。
        /// Locked 時為 0，Taken/Empty/PlayerStored 時為 1。
        /// 僅對地圖物品格有意義。
        /// </summary>
        public float GetSlotUnlockProgress(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            CollectibleSlotState state = _slotStates[slotIndex];
            if (state == CollectibleSlotState.Locked) return 0f;
            if (state == CollectibleSlotState.Unlocked ||
                state == CollectibleSlotState.Taken ||
                state == CollectibleSlotState.Empty ||
                state == CollectibleSlotState.PlayerStored) return 1f;

            // Unlocking state — only for map item slots
            if (slotIndex >= _data.Items.Count) return 1f;
            float duration = _data.Items[slotIndex].UnlockDurationSeconds;
            if (duration <= 0f) return 1f;
            return Math.Min(_slotElapsedTime[slotIndex] / duration, 1f);
        }

        /// <summary>
        /// 取得所有 6 格的狀態資訊。
        /// </summary>
        public BoxSlotInfo[] GetAllSlots()
        {
            BoxSlotInfo[] result = new BoxSlotInfo[MaxSlots];
            for (int i = 0; i < MaxSlots; i++)
            {
                CollectibleSlotState state = _slotStates[i];
                if (i < _data.Items.Count)
                {
                    // 地圖物品格
                    CollectibleItemEntry entry = _data.Items[i];
                    result[i] = new BoxSlotInfo(entry.ItemId, entry.Quantity, state, false);
                }
                else if (state == CollectibleSlotState.PlayerStored)
                {
                    // 玩家存放的物品
                    result[i] = new BoxSlotInfo(_storedItemIds[i], _storedQuantities[i], state, true);
                }
                else
                {
                    // 空格
                    result[i] = BoxSlotInfo.CreateEmpty();
                }
            }
            return result;
        }

        /// <summary>
        /// 開始採集（Idle -> Gathering）。
        /// 若 GatherDurationSeconds 為 0，直接進入 Unlocking 階段。
        /// </summary>
        /// <exception cref="InvalidOperationException">非 Idle 狀態時呼叫。</exception>
        public void StartGathering()
        {
            if (_phase != GatheringPhase.Idle)
                throw new InvalidOperationException("Can only start gathering from Idle phase.");

            // 已開啟過的物品箱直接進入 Unlocking，不需再等開啟計時
            if (_hasBeenOpened || _data.GatherDurationSeconds <= 0f)
            {
                _phase = GatheringPhase.Unlocking;
                _gatheringElapsed = 0f;
                _hasBeenOpened = true;
                TransitionAllSlotsToUnlocking();

                EventBus.Publish(new GatheringCompletedEvent
                {
                    X = _data.X,
                    Y = _data.Y
                });
                return;
            }

            _phase = GatheringPhase.Gathering;
            _gatheringElapsed = 0f;
        }

        /// <summary>
        /// 取消採集（Gathering -> Idle）。累積時間清空。對應 GDD 規則 44。
        /// </summary>
        /// <exception cref="InvalidOperationException">非 Gathering 狀態時呼叫。</exception>
        public void CancelGathering()
        {
            if (_phase != GatheringPhase.Gathering)
                throw new InvalidOperationException("Can only cancel gathering during Gathering phase.");

            _phase = GatheringPhase.Idle;
            _gatheringElapsed = 0f;
        }

        /// <summary>
        /// 關閉物品欄，回到 Idle（從 Unlocking 狀態）。
        /// 未拾取的物品視為放棄。玩家存放的物品也會清除。
        /// </summary>
        /// <exception cref="InvalidOperationException">非 Unlocking 狀態時呼叫。</exception>
        public void CloseItemPanel()
        {
            if (_phase != GatheringPhase.Unlocking)
                throw new InvalidOperationException("Can only close item panel during Unlocking phase.");

            // 只切換 phase 回 Idle，保留所有格子狀態（已解鎖、已存放、空格）
            // 重新開啟時會直接進入 Unlocking（因為 _hasBeenOpened = true）
            _phase = GatheringPhase.Idle;
        }

        /// <summary>
        /// 推進計時。
        /// Gathering 階段：推進第一層計時，完成後自動進入 Unlocking。
        /// Unlocking 階段：推進所有尚在 Unlocking 的物品欄位計時。
        /// </summary>
        public void Update(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            if (_phase == GatheringPhase.Gathering)
            {
                _gatheringElapsed += deltaTime;
                if (_gatheringElapsed >= _data.GatherDurationSeconds)
                {
                    _gatheringElapsed = _data.GatherDurationSeconds;
                    _phase = GatheringPhase.Unlocking;
                    _hasBeenOpened = true;
                    TransitionAllSlotsToUnlocking();

                    EventBus.Publish(new GatheringCompletedEvent
                    {
                        X = _data.X,
                        Y = _data.Y
                    });
                }
            }
            else if (_phase == GatheringPhase.Unlocking)
            {
                for (int i = 0; i < _data.Items.Count; i++)
                {
                    if (_slotStates[i] != CollectibleSlotState.Unlocking)
                        continue;

                    _slotElapsedTime[i] += deltaTime;
                    if (_slotElapsedTime[i] >= _data.Items[i].UnlockDurationSeconds)
                    {
                        _slotElapsedTime[i] = _data.Items[i].UnlockDurationSeconds;
                        _slotStates[i] = CollectibleSlotState.Unlocked;

                        EventBus.Publish(new ItemSlotUnlockedEvent
                        {
                            X = _data.X,
                            Y = _data.Y,
                            SlotIndex = i,
                            ItemId = _data.Items[i].ItemId
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 拾取已解鎖的物品到背包。對應 GDD 規則 46（手動拾取）。
        /// 也支援拾取玩家存放的物品。
        /// </summary>
        /// <param name="slotIndex">物品欄位索引。</param>
        /// <param name="backpack">目標背包。</param>
        /// <returns>實際放入背包的數量。0 表示背包已滿或欄位狀態不允許拾取。</returns>
        /// <exception cref="InvalidOperationException">非 Unlocking 階段時呼叫。</exception>
        public int TryPickItem(int slotIndex, BackpackManager backpack)
        {
            if (_phase != GatheringPhase.Unlocking)
                throw new InvalidOperationException("Can only pick items during Unlocking phase.");
            if (backpack == null)
                throw new ArgumentNullException(nameof(backpack));
            if (slotIndex < 0 || slotIndex >= MaxSlots)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            if (_slotStates[slotIndex] == CollectibleSlotState.Unlocked)
            {
                // 地圖物品拾取
                if (slotIndex >= _data.Items.Count) return 0;
                CollectibleItemEntry item = _data.Items[slotIndex];
                int added = backpack.AddItem(item.ItemId, item.Quantity);

                if (added > 0)
                {
                    _slotStates[slotIndex] = CollectibleSlotState.Empty;

                    EventBus.Publish(new ItemPickedUpEvent
                    {
                        ItemId = item.ItemId,
                        Quantity = added
                    });
                }

                return added;
            }
            else if (_slotStates[slotIndex] == CollectibleSlotState.PlayerStored)
            {
                // 玩家存放物品取回
                string itemId = _storedItemIds[slotIndex];
                int quantity = _storedQuantities[slotIndex];
                if (string.IsNullOrEmpty(itemId) || quantity <= 0) return 0;

                int added = backpack.AddItem(itemId, quantity);

                if (added > 0)
                {
                    _slotStates[slotIndex] = CollectibleSlotState.Empty;
                    _storedItemIds[slotIndex] = null;
                    _storedQuantities[slotIndex] = 0;

                    EventBus.Publish(new ItemRemovedFromBoxEvent
                    {
                        SlotIndex = slotIndex,
                        ItemId = itemId,
                        Quantity = added
                    });
                }

                return added;
            }

            return 0;
        }

        /// <summary>
        /// 將物品存入物品箱的指定空格。
        /// 僅 Empty 狀態的格子可存放。不需解鎖計時，立即可取回。
        /// </summary>
        /// <param name="slotIndex">物品箱格子索引。</param>
        /// <param name="itemId">物品 ID。</param>
        /// <param name="quantity">數量。</param>
        /// <returns>true 表示存放成功。</returns>
        /// <exception cref="InvalidOperationException">非 Unlocking 階段時呼叫。</exception>
        public bool StoreItem(int slotIndex, string itemId, int quantity)
        {
            if (_phase != GatheringPhase.Unlocking)
                throw new InvalidOperationException("Can only store items during Unlocking phase.");
            if (string.IsNullOrEmpty(itemId))
                throw new ArgumentException("itemId must not be null or empty.", nameof(itemId));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "quantity must be greater than 0.");
            if (slotIndex < 0 || slotIndex >= MaxSlots)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            if (_slotStates[slotIndex] != CollectibleSlotState.Empty)
                return false;

            _slotStates[slotIndex] = CollectibleSlotState.PlayerStored;
            _storedItemIds[slotIndex] = itemId;
            _storedQuantities[slotIndex] = quantity;

            EventBus.Publish(new ItemStoredInBoxEvent
            {
                SlotIndex = slotIndex,
                ItemId = itemId,
                Quantity = quantity
            });

            return true;
        }

        /// <summary>
        /// 從物品箱移除玩家存放的物品（不放回背包，僅清除格子）。
        /// 拾取玩家存放物品請使用 TryPickItem。
        /// </summary>
        /// <param name="slotIndex">物品箱格子索引。</param>
        /// <returns>true 表示移除成功。</returns>
        /// <exception cref="InvalidOperationException">非 Unlocking 階段時呼叫。</exception>
        public bool RemoveStoredItem(int slotIndex)
        {
            if (_phase != GatheringPhase.Unlocking)
                throw new InvalidOperationException("Can only remove stored items during Unlocking phase.");
            if (slotIndex < 0 || slotIndex >= MaxSlots)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            if (_slotStates[slotIndex] != CollectibleSlotState.PlayerStored)
                return false;

            string itemId = _storedItemIds[slotIndex];
            int quantity = _storedQuantities[slotIndex];

            _slotStates[slotIndex] = CollectibleSlotState.Empty;
            _storedItemIds[slotIndex] = null;
            _storedQuantities[slotIndex] = 0;

            EventBus.Publish(new ItemRemovedFromBoxEvent
            {
                SlotIndex = slotIndex,
                ItemId = itemId,
                Quantity = quantity
            });

            return true;
        }

        /// <summary>
        /// 是否所有地圖物品欄位都已被拾取。
        /// 僅檢查地圖定義的物品格（前 N 格），不包含空格與玩家存放格。
        /// </summary>
        public bool AllItemsTaken
        {
            get
            {
                for (int i = 0; i < _data.Items.Count; i++)
                {
                    if (_slotStates[i] != CollectibleSlotState.Taken)
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// 取得第一個可用的空格索引。無空格時回傳 -1。
        /// </summary>
        public int FindFirstEmptySlot()
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                if (_slotStates[i] == CollectibleSlotState.Empty)
                    return i;
            }
            return -1;
        }

        // ------------------------------------------------------------------
        // Private helpers
        // ------------------------------------------------------------------

        private void TransitionAllSlotsToUnlocking()
        {
            for (int i = 0; i < _data.Items.Count; i++)
            {
                if (_slotStates[i] == CollectibleSlotState.Locked)
                {
                    // 解鎖時間為 0 的物品直接 Unlocked
                    if (_data.Items[i].UnlockDurationSeconds <= 0f)
                    {
                        _slotStates[i] = CollectibleSlotState.Unlocked;
                        EventBus.Publish(new ItemSlotUnlockedEvent
                        {
                            X = _data.X,
                            Y = _data.Y,
                            SlotIndex = i,
                            ItemId = _data.Items[i].ItemId
                        });
                    }
                    else
                    {
                        _slotStates[i] = CollectibleSlotState.Unlocking;
                    }
                }
            }
            // Empty slots (indices >= _data.Items.Count) remain Empty
        }
    }
}
