// CollectionManager — 管理採集互動的核心邏輯。
// 協調 PlayerGridMovement（移動鎖定）、CollectiblePointState（兩層計時）、BackpackManager（物品存放）。
// 對應 GDD 規則 8-13, 44-46。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// 管理採集互動的核心邏輯。
    /// 負責：查詢可互動探索點、開始/取消採集、計時推進、物品拾取。
    /// </summary>
    public class CollectionManager
    {
        private readonly GridMap _gridMap;
        private readonly PlayerGridMovement _playerMovement;
        private readonly BackpackManager _backpack;
        private readonly Dictionary<long, CollectiblePointState> _pointStates;

        private CollectiblePointState _activePointState;

        /// <summary>當前是否正在採集中（Gathering 或 Unlocking 階段）。</summary>
        public bool IsCollecting => _activePointState != null &&
            _activePointState.Phase != GatheringPhase.Idle;

        /// <summary>取得當前互動的探索點狀態。無互動時回傳 null。</summary>
        public CollectiblePointState ActivePointState => _activePointState;

        /// <summary>
        /// 建立採集管理器。
        /// </summary>
        /// <param name="gridMap">格子地圖。</param>
        /// <param name="playerMovement">玩家移動邏輯（用於鎖定移動）。</param>
        /// <param name="backpack">背包（用於物品拾取）。</param>
        public CollectionManager(GridMap gridMap, PlayerGridMovement playerMovement, BackpackManager backpack)
        {
            _gridMap = gridMap ?? throw new ArgumentNullException(nameof(gridMap));
            _playerMovement = playerMovement ?? throw new ArgumentNullException(nameof(playerMovement));
            _backpack = backpack ?? throw new ArgumentNullException(nameof(backpack));
            _pointStates = new Dictionary<long, CollectiblePointState>();
        }

        /// <summary>
        /// 玩家當前位置是否有可互動的探索點。
        /// </summary>
        public bool CanInteract()
        {
            if (IsCollecting) return false;
            if (_playerMovement.IsMoving) return false;

            CollectiblePointData pointData = _gridMap.GetCollectiblePointAt(
                _playerMovement.CurrentPosition.x,
                _playerMovement.CurrentPosition.y);

            return pointData != null;
        }

        /// <summary>
        /// 開始採集。鎖定玩家移動，啟動第一層計時。
        /// </summary>
        /// <returns>true 表示成功開始；false 表示無法開始（無探索點、正在移動等）。</returns>
        public bool TryStartGathering()
        {
            if (IsCollecting) return false;
            if (_playerMovement.IsMoving) return false;

            int x = _playerMovement.CurrentPosition.x;
            int y = _playerMovement.CurrentPosition.y;

            CollectiblePointData pointData = _gridMap.GetCollectiblePointAt(x, y);
            if (pointData == null) return false;

            _activePointState = GetOrCreatePointState(pointData);
            _activePointState.StartGathering();
            _playerMovement.SetMovementLock(true);

            EventBus.Publish(new CollectionStartedEvent
            {
                X = x,
                Y = y,
                GatherDuration = pointData.GatherDurationSeconds
            });

            return true;
        }

        /// <summary>
        /// 取消採集。解鎖玩家移動，累積時間清空。對應 GDD 規則 44。
        /// </summary>
        public void CancelGathering()
        {
            if (_activePointState == null) return;
            if (_activePointState.Phase != GatheringPhase.Gathering) return;

            int x = _activePointState.Data.X;
            int y = _activePointState.Data.Y;

            _activePointState.CancelGathering();
            _activePointState = null;
            _playerMovement.SetMovementLock(false);

            EventBus.Publish(new CollectionCancelledEvent
            {
                X = x,
                Y = y
            });
        }

        /// <summary>
        /// 拾取指定欄位的已解鎖物品到背包。對應 GDD 規則 46。
        /// </summary>
        /// <returns>實際放入背包的數量。</returns>
        public int TryPickItem(int slotIndex)
        {
            if (_activePointState == null) return 0;
            if (_activePointState.Phase != GatheringPhase.Unlocking) return 0;

            return _activePointState.TryPickItem(slotIndex, _backpack);
        }

        /// <summary>
        /// 關閉物品欄。解鎖玩家移動。
        /// </summary>
        public void CloseItemPanel()
        {
            if (_activePointState == null) return;
            if (_activePointState.Phase != GatheringPhase.Unlocking) return;

            _activePointState.CloseItemPanel();
            _activePointState = null;
            _playerMovement.SetMovementLock(false);

            EventBus.Publish(new CollectionPanelClosedEvent());
        }

        /// <summary>
        /// 推進當前採集點的計時。
        /// </summary>
        public void Update(float deltaTime)
        {
            if (_activePointState == null) return;
            _activePointState.Update(deltaTime);
        }

        // ------------------------------------------------------------------
        // Private helpers
        // ------------------------------------------------------------------

        private long PositionKey(int x, int y)
        {
            return ((long)x << 32) | (uint)y;
        }

        private CollectiblePointState GetOrCreatePointState(CollectiblePointData data)
        {
            long key = PositionKey(data.X, data.Y);
            if (!_pointStates.TryGetValue(key, out CollectiblePointState state))
            {
                state = new CollectiblePointState(data);
                _pointStates[key] = state;
            }
            return state;
        }
    }
}
