using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village;
using ProjectDR.Village.Progression;

namespace ProjectDR.Village.Navigation
{
    /// <summary>
    /// 村莊導航管理器。
    /// 依賴 VillageProgressionManager 取得可導航的已解鎖區域清單。
    /// 管理目前所在區域與返回主畫面操作。
    /// </summary>
    public class VillageNavigationManager
    {
        private readonly VillageProgressionManager _progressionManager;
        private string _currentArea = null;

        public VillageNavigationManager(VillageProgressionManager progressionManager)
        {
            _progressionManager = progressionManager;
        }

        /// <summary>目前所在區域。若位於主畫面（Hub），則為 null。</summary>
        public string CurrentArea => _currentArea;

        /// <summary>取得目前可導航的區域清單（等同於已解鎖的區域）。</summary>
        public IReadOnlyList<string> GetNavigableAreas()
        {
            return _progressionManager.GetUnlockedAreas();
        }

        /// <summary>
        /// 導航至指定區域。
        /// 失敗條件：區域未解鎖、不存在、或目前已在該區域。
        /// 成功後發布 NavigatedToAreaEvent。
        /// </summary>
        public bool NavigateTo(string areaId)
        {
            if (!_progressionManager.IsAreaUnlocked(areaId))
            {
                return false;
            }

            if (_currentArea == areaId)
            {
                return false;
            }

            _currentArea = areaId;
            EventBus.Publish(new NavigatedToAreaEvent { AreaId = areaId });
            return true;
        }

        /// <summary>
        /// 返回主畫面（Hub）。
        /// 若已在 Hub 則不執行任何動作，也不發布事件。
        /// 成功返回後發布 ReturnedToHubEvent。
        /// </summary>
        public void ReturnToHub()
        {
            if (_currentArea == null)
            {
                return;
            }

            _currentArea = null;
            EventBus.Publish(new ReturnedToHubEvent());
        }
    }
}
