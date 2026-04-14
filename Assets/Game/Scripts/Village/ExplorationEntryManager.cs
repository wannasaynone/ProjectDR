// ExplorationEntryManager — 探索進入管理器（IT 階段版本）。
// IT 階段使用 SimulateReturn 模擬探索返回，不實際連接探索系統。
// V2：戰利品進入背包而非直接進倉庫；出發時自動拍攝背包快照。

using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// 探索進入管理器（IT 階段版本）。
    /// 管理玩家出發與返回探索的狀態轉換。
    /// 使用 SimulateReturn 模擬探索返回，並將戰利品新增至 BackpackManager。
    /// 出發時自動拍攝背包快照，供死亡回溯使用。
    /// </summary>
    public class ExplorationEntryManager
    {
        private readonly BackpackManager _backpackManager;
        private bool _isExploring = false;
        private BackpackSnapshot _departureSnapshot;

        public ExplorationEntryManager(BackpackManager backpackManager)
        {
            _backpackManager = backpackManager;
        }

        /// <summary>檢查目前是否可以出發探索（未在探索中即可出發）。</summary>
        public bool CanDepart()
        {
            return !_isExploring;
        }

        /// <summary>
        /// 出發探索。若已在探索中，回傳 false。
        /// 成功出發時，拍攝背包快照並發布 ExplorationDepartedEvent。
        /// </summary>
        public bool Depart()
        {
            if (!CanDepart())
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
        /// 取得出發時拍攝的背包快照。
        /// 若尚未出發過，回傳 null。
        /// </summary>
        public BackpackSnapshot GetDepartureSnapshot()
        {
            return _departureSnapshot;
        }
    }
}
