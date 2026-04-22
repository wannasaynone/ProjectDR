// DeathManager — 死亡管理器。
// 監聽 PlayerDiedEvent（由 PlayerCombatStats 在 HP <= 0 時發布），
// 觸發背包回溯與探索結束流程。
// GDD 規則 27-30：HP 歸零 → 時間回溯 → 背包回溯至出發快照 → 結束探索。

using ProjectDR.Village.Exploration.Core;
using ProjectDR.Village.Backpack;
using System;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// 死亡管理器。
    /// 訂閱 PlayerDiedEvent，執行死亡流程：
    /// 1. 設定死亡狀態（防重複觸發）
    /// 2. 回溯背包至出發前快照
    /// 3. 發布 PlayerDeathEvent（通知 View 層開始死亡演出）
    /// 4. 發布 ExplorationCompletedEvent（結束探索回村）
    /// </summary>
    public class DeathManager
    {
        private readonly BackpackManager _backpackManager;
        private readonly ExplorationEntryManager _explorationEntryManager;
        private Action<PlayerDiedEvent> _onPlayerDied;
        private bool _isDead;

        /// <summary>玩家是否已死亡（死亡流程已觸發）。</summary>
        public bool IsDead => _isDead;

        /// <summary>
        /// 建立死亡管理器。
        /// </summary>
        /// <param name="backpackManager">背包管理器（用於回溯）。</param>
        /// <param name="explorationEntryManager">探索進入管理器（用於取得出發快照）。</param>
        public DeathManager(BackpackManager backpackManager, ExplorationEntryManager explorationEntryManager)
        {
            _backpackManager = backpackManager ?? throw new ArgumentNullException(nameof(backpackManager));
            _explorationEntryManager = explorationEntryManager ?? throw new ArgumentNullException(nameof(explorationEntryManager));

            _onPlayerDied = HandlePlayerDied;
            EventBus.Subscribe<PlayerDiedEvent>(_onPlayerDied);
        }

        /// <summary>
        /// 取消訂閱所有事件。銷毀時呼叫。
        /// </summary>
        public void Dispose()
        {
            if (_onPlayerDied != null)
            {
                EventBus.Unsubscribe<PlayerDiedEvent>(_onPlayerDied);
                _onPlayerDied = null;
            }
        }

        /// <summary>
        /// 重置死亡狀態。用於重新開始探索時。
        /// </summary>
        public void Reset()
        {
            _isDead = false;
        }

        private void HandlePlayerDied(PlayerDiedEvent e)
        {
            if (_isDead) return;

            _isDead = true;

            // Step 1: 回溯背包至出發前快照（邏輯先於視覺）
            BackpackSnapshot snapshot = _explorationEntryManager.GetDepartureSnapshot();
            if (snapshot != null)
            {
                _backpackManager.RestoreSnapshot(snapshot);
            }

            // Step 2: 發布 PlayerDeathEvent（通知 View 層）
            EventBus.Publish(new PlayerDeathEvent());

            // Step 3: 發布 ExplorationCompletedEvent（結束探索回村）
            EventBus.Publish(new ExplorationCompletedEvent());
        }
    }
}
