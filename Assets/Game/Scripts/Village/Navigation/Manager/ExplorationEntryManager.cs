// ExplorationEntryManager — 探索進入管理器。
// 管理玩家出發與返回探索的狀態轉換。
// V2：戰利品進入背包而非直接進倉庫；出發時自動拍攝背包快照。
// V5：監聽 ExplorationCompletedEvent 自動觸發 CompleteExploration。
// V6（B10 Sprint 4）：新增 IDepartureInterceptor 機制，供守衛歸來事件攔截首次探索。

using ProjectDR.Village.Exploration.Core;
using ProjectDR.Village.Backpack;
using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village;
using ProjectDR.Village.Exploration;

namespace ProjectDR.Village.Navigation
{
    /// <summary>
    /// 出發攔截器介面。
    /// ExplorationEntryManager.Depart() 前會詢問攔截器是否「此次出發應先改為觸發劇情事件」。
    /// 實作方（B10 GuardReturnEventController）判斷條件後，若要攔截則回傳 true 並觸發對應劇情。
    /// </summary>
    public interface IExplorationDepartureInterceptor
    {
        /// <summary>
        /// 嘗試攔截一次「出發探索」呼叫。
        /// 若回傳 true，ExplorationEntryManager 不會實際出發，呼叫端（UI）應等待該攔截流程完成後再重新呼叫 Depart()。
        /// 若回傳 false，ExplorationEntryManager 繼續正常出發流程。
        /// </summary>
        bool TryIntercept();
    }

    /// <summary>
    /// 探索進入管理器。
    /// 管理玩家出發與返回探索的狀態轉換。
    /// 監聽 ExplorationCompletedEvent（由撤離完成或死亡觸發），
    /// 自動呼叫 CompleteExploration() 結束探索狀態並發布 ExplorationReturnedEvent。
    /// 出發時自動拍攝背包快照，供死亡回溯使用。
    /// 支援單一 IExplorationDepartureInterceptor 攔截出發，供守衛歸來事件等劇情攔截首次探索。
    /// </summary>
    public class ExplorationEntryManager
    {
        private readonly BackpackManager _backpackManager;
        private bool _isExploring = false;
        private bool _isLocked = false; // Sprint 6 擴張：守衛歸來後鎖定探索入口
        private BackpackSnapshot _departureSnapshot;
        private Action<ExplorationCompletedEvent> _onExplorationCompleted;

        /// <summary>
        /// 出發攔截器。由上層注入。預設為 null（無攔截）。
        /// </summary>
        private IExplorationDepartureInterceptor _interceptor;

        public ExplorationEntryManager(BackpackManager backpackManager)
        {
            _backpackManager = backpackManager;
            _onExplorationCompleted = OnExplorationCompleted;
            EventBus.Subscribe<ExplorationCompletedEvent>(_onExplorationCompleted);
        }

        /// <summary>
        /// 設定出發攔截器。傳入 null 可移除攔截器。
        /// 守衛歸來事件完成後，上層應呼叫 SetDepartureInterceptor(null) 移除。
        /// </summary>
        public void SetDepartureInterceptor(IExplorationDepartureInterceptor interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>
        /// 設定探索入口鎖定狀態（Sprint 6 擴張）。
        /// 鎖定後 Depart() 回傳 false，VillageHubView 顯示「要去找守衛對話拿劍...」提示。
        /// 玩家發問「要拿劍」成功後呼叫 SetExplorationLocked(false) 解鎖。
        /// </summary>
        public void SetExplorationLocked(bool locked)
        {
            _isLocked = locked;
        }

        /// <summary>探索入口是否處於鎖定狀態。</summary>
        public bool IsExplorationLocked => _isLocked;

        /// <summary>檢查目前是否可以出發探索（未在探索中且未鎖定）。</summary>
        public bool CanDepart()
        {
            return !_isExploring && !_isLocked;
        }

        /// <summary>
        /// 出發探索。若已在探索中或入口鎖定，回傳 false。
        /// 若有攔截器且攔截成功，不實際出發、回傳 false。
        /// 成功出發時，拍攝背包快照並發布 ExplorationDepartedEvent。
        /// </summary>
        public bool Depart()
        {
            if (_isLocked)
            {
                // 鎖定狀態：不出發，發布提示事件（VillageHubView 處理每次點擊都顯示的 modal）
                EventBus.Publish(new ExplorationGateLockedClickedEvent());
                return false;
            }

            if (!CanDepart())
            {
                return false;
            }

            // 先給攔截器機會（守衛歸來事件）
            if (_interceptor != null && _interceptor.TryIntercept())
            {
                return false;
            }

            // 出發前拍攝背包快照（死亡回溯用）
            _departureSnapshot = _backpackManager.TakeSnapshot();

            _isExploring = true;
            EventBus.Publish(new ExplorationDepartedEvent());
            return true;
        }

        /// <summary>
        /// 模擬探索返回（IT 階段使用）。
        /// 若目前不在探索中，此方法無效（不新增物品、不發布事件）。
        /// 成功返回時，將戰利品新增至背包並發布 ExplorationReturnedEvent。
        /// </summary>
        public void SimulateReturn(Dictionary<string, int> loot)
        {
            if (!_isExploring)
            {
                return;
            }

            _isExploring = false;

            // 將戰利品新增至背包
            foreach (KeyValuePair<string, int> item in loot)
            {
                _backpackManager.AddItem(item.Key, item.Value);
            }

            EventBus.Publish(new ExplorationReturnedEvent { Loot = loot });
        }

        /// <summary>
        /// 結束探索。將 _isExploring 設為 false 並發布 ExplorationReturnedEvent。
        /// 若目前不在探索中，此方法無效。
        /// </summary>
        public void CompleteExploration()
        {
            if (!_isExploring)
            {
                return;
            }

            _isExploring = false;
            EventBus.Publish(new ExplorationReturnedEvent { Loot = new Dictionary<string, int>() });
        }

        /// <summary>
        /// 取消訂閱所有事件。銷毀時呼叫。
        /// </summary>
        public void Dispose()
        {
            if (_onExplorationCompleted != null)
            {
                EventBus.Unsubscribe<ExplorationCompletedEvent>(_onExplorationCompleted);
                _onExplorationCompleted = null;
            }
        }

        /// <summary>
        /// 取得出發時拍攝的背包快照。
        /// 若尚未出發過，回傳 null。
        /// </summary>
        public BackpackSnapshot GetDepartureSnapshot()
        {
            return _departureSnapshot;
        }

        private void OnExplorationCompleted(ExplorationCompletedEvent e)
        {
            CompleteExploration();
        }
    }
}
