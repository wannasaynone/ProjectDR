using UnityEngine;
using UnityEngine.UI;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 農場畫面（IT 階段 Placeholder）。
    /// 目前只提供返回 Hub 的功能，農務系統將在後續 Sprint 實作。
    /// </summary>
    public class FarmAreaView : ViewBase
    {
        [SerializeField] private Button _returnButton;

        private VillageNavigationManager _navigationManager;

        public void Initialize(VillageNavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        protected override void OnShow()
        {
            if (_returnButton != null)
            {
                _returnButton.onClick.AddListener(OnReturnClicked);
            }
        }

        protected override void OnHide()
        {
            if (_returnButton != null)
            {
                _returnButton.onClick.RemoveListener(OnReturnClicked);
            }
        }

        private void OnReturnClicked()
        {
            _navigationManager.ReturnToHub();
        }
    }
}
