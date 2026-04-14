using UnityEngine;
using UnityEngine.UI;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 煉金工坊畫面（IT 階段 Placeholder）。
    /// 目前只提供返回 Hub 的功能，藥水製作系統將在後續 Sprint 實作。
    /// </summary>
    public class AlchemyAreaView : ViewBase
    {
        [SerializeField] private Button _returnButton;

        private VillageNavigationManager _navigationManager;

        // Overlay 模式：若有設定 returnAction，return 按鈕觸發此回呼而非 ReturnToHub
        private System.Action _returnAction;

        public void Initialize(VillageNavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
            _returnAction = null;
        }

        /// <summary>
        /// 設定 overlay 模式的返回行為。
        /// 設定後，return 按鈕會觸發此回呼而非 ReturnToHub()。
        /// </summary>
        /// <param name="returnAction">返回時要執行的回呼。</param>
        public void SetReturnAction(System.Action returnAction)
        {
            _returnAction = returnAction;
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
            if (_returnAction != null)
            {
                _returnAction.Invoke();
                return;
            }

            _navigationManager.ReturnToHub();
        }
    }
}
