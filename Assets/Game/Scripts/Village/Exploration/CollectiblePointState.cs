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
        Taken
    }

    /// <summary>
    /// 單一探索點的運行時狀態。管理兩層計時狀態機。
    /// </summary>
    public class CollectiblePointState
    {
        private readonly CollectiblePointData _data;
        private readonly float[] _slotElapsedTime;
        private readonly CollectibleSlotState[] _slotStates;

        private GatheringPhase _phase;
        private float _gatheringElapsed;

        /// <summary>當前採集階段。</summary>
        public GatheringPhase Phase => _phase;

        /// <summary>物品欄位數量。</summary>
        public int SlotCount => _data.Items.Count;

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
        /// 建立探索點運行時狀態。
        /// </summary>
        /// <param name="data">探索點靜態資料。不可為 null。</param>
        public CollectiblePointState(CollectiblePointData data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _slotElapsedTime = new float[data.Items.Count];
            _slotStates = new CollectibleSlotState[data.Items.Count];

            for (int i = 0; i < _slotStates.Length; i++)
            {
                _slotStates[i] = CollectibleSlotState.Locked;
            }

            _phase = GatheringPhase.Idle;
            _gatheringElapsed = 0f;
        }

        /// <summary>
        /// 取得指定欄位的狀態。
        /// </summary>
        public CollectibleSlotState GetSlotState(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotStates.Length)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            return _slotStates[slotIndex];
        }

        /// <summary>
        /// 取得指定欄位的解鎖進度 (0~1)。Locked 時為 0，Taken 時為 1。
        /// </summary>
        public float GetSlotUnlockProgress(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotStates.Length)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            if (_slotStates[slotIndex] == CollectibleSlotState.Locked) return 0f;
            if (_slotStates[slotIndex] == CollectibleSlotState.Unlocked ||
                _slotStates[slotIndex] == CollectibleSlotState.Taken) return 1f;

            float duration = _data.Items[slotIndex].UnlockDurationSeconds;
            if (duration <= 0f) return 1f;
            return Math.Min(_slotElapsedTime[slotIndex] / duration, 1f);
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

            if (_data.GatherDurationSeconds <= 0f)
            {
                // 不需要等待，直接進入 Unlocking
                _phase = GatheringPhase.Unlocking;
                _gatheringElapsed = 0f;
                TransitionAllSlotsToUnlocking();
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
        /// 未拾取的物品視為放棄。
        /// </summary>
        /// <exception cref="InvalidOperationException">非 Unlocking 狀態時呼叫。</exception>
        public void CloseItemPanel()
        {
            if (_phase != GatheringPhase.Unlocking)
                throw new InvalidOperationException("Can only close item panel during Unlocking phase.");

            _phase = GatheringPhase.Idle;
            _gatheringElapsed = 0f;

            // Reset all slots
            for (int i = 0; i < _slotStates.Length; i++)
            {
                _slotStates[i] = CollectibleSlotState.Locked;
                _slotElapsedTime[i] = 0f;
            }
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
                for (int i = 0; i < _slotStates.Length; i++)
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
        /// </summary>
        /// <param name="slotIndex">物品欄位索引。</param>
        /// <param name="backpack">目標背包。</param>
        /// <returns>實際放入背包的數量。0 表示背包已滿或欄位未解鎖。</returns>
        /// <exception cref="InvalidOperationException">非 Unlocking 階段時呼叫。</exception>
        public int TryPickItem(int slotIndex, BackpackManager backpack)
        {
            if (_phase != GatheringPhase.Unlocking)
                throw new InvalidOperationException("Can only pick items during Unlocking phase.");
            if (backpack == null)
                throw new ArgumentNullException(nameof(backpack));
            if (slotIndex < 0 || slotIndex >= _slotStates.Length)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            if (_slotStates[slotIndex] != CollectibleSlotState.Unlocked)
                return 0;

            CollectibleItemEntry item = _data.Items[slotIndex];
            int added = backpack.AddItem(item.ItemId, item.Quantity);

            if (added > 0)
            {
                _slotStates[slotIndex] = CollectibleSlotState.Taken;

                EventBus.Publish(new ItemPickedUpEvent
                {
                    ItemId = item.ItemId,
                    Quantity = added
                });
            }

            return added;
        }

        /// <summary>
        /// 是否所有欄位都已被拾取。
        /// </summary>
        public bool AllItemsTaken
        {
            get
            {
                for (int i = 0; i < _slotStates.Length; i++)
                {
                    if (_slotStates[i] != CollectibleSlotState.Taken)
                        return false;
                }
                return true;
            }
        }

        // ------------------------------------------------------------------
        // Private helpers
        // ------------------------------------------------------------------

        private void TransitionAllSlotsToUnlocking()
        {
            for (int i = 0; i < _slotStates.Length; i++)
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
        }
    }
}
