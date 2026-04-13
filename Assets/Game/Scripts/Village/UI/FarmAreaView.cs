using UnityEngine;
using UnityEngine.UIElements;
using ProjectDR.Village;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 農場畫面（IT 階段 Placeholder）。
    /// 目前只提供返回 Hub 的功能，農務系統將在後續 Sprint 實作。
    /// </summary>
    public class FarmAreaView : UIToolkitViewBase
    {
        private VillageNavigationManager _navigationManager;
        private Button _returnButton;

        public void Initialize(VillageNavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        protected override void OnShow()
        {
            _returnButton = Root.Q<Button>("return-button");
            if (_returnButton != null)
            {
                _returnButton.clicked += OnReturnClicked;
            }
        }

        protected override void OnHide()
        {
            if (_returnButton != null)
            {
                _returnButton.clicked -= OnReturnClicked;
            }
        }

        private void OnReturnClicked()
        {
            _navigationManager.ReturnToHub();
        }
    }
}
