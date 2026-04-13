// ExplorationEntryManager — 探索進入管理器（IT 階段版本）。
// IT 階段使用 SimulateReturn 模擬探索返回，不實際連接探索系統。

using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// 探索進入管理器（IT 階段版本）。
    /// 管理玩家出發與返回探索的狀態轉換。
    /// 使用 SimulateReturn 模擬探索返回，並將戰利品新增至 StorageManager。
    /// </summary>
    public class ExplorationEntryManager
    {
        private readonly StorageManager _storageManager;
        private bool _isExploring = false;

        public ExplorationEntryManager(StorageManager storageManager)
        {
            _storageManager = storageManager;
        }

        /// <summary>檢查目前是否可以出發探索（未在探索中即可出發）。</summary>
        public bool CanDepart()
        {
            return !_isExploring;
        }

        /// <summary>
        /// 出發探索。若已在探索中，回傳 false。
        /// 成功出發時，發布 ExplorationDepartedEvent。
        /// </summary>
        public bool Depart()
        {
            if (!CanDepart())
            {
                return false;
            }

            _isExploring = true;
            EventBus.Publish(new ExplorationDepartedEvent());
            return true;
        }

        /// <summary>
        /// 模擬探索返回（IT 階段使用）。
        /// 若目前不在探索中，此方法無效（不新增物品、不發布事件）。
        /// 成功返回時，將戰利品新增至 Storage 並發布 ExplorationReturnedEvent。
        /// </summary>
        public void SimulateReturn(Dictionary<string, int> loot)
        {
            if (!_isExploring)
            {
                return;
            }

            _isExploring = false;

            // 將戰利品新增至庫存
            foreach (KeyValuePair<string, int> item in loot)
            {
                _storageManager.AddItem(item.Key, item.Value);
            }

            EventBus.Publish(new ExplorationReturnedEvent { Loot = loot });
        }
    }
}
