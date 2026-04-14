using KahaGameCore.GameEvent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 探索入口畫面（IT 階段 Placeholder）。
    /// 提供出發探索與模擬返回的按鈕。
    /// </summary>
    public class ExplorationAreaView : ViewBase
    {
        [SerializeField] private Button _departButton;
        [SerializeField] private Button _simulateReturnButton;
        [SerializeField] private Button _returnButton;
        [SerializeField] private TMP_Text _statusLabel;

        private ExplorationEntryManager _explorationManager;
        private VillageNavigationManager _navigationManager;

        // Overlay 模式：若有設定 returnAction，return 按鈕觸發此回呼而非 ReturnToHub
        private System.Action _returnAction;

        public void Initialize(ExplorationEntryManager explorationManager, VillageNavigationManager navigationManager)
        {
            _explorationManager = explorationManager;
            _navigationManager = navigationManager;
            _returnAction = null;
            EventBus.Subscribe<ExplorationDepartedEvent>(OnDeparted);
            EventBus.Subscribe<ExplorationReturnedEvent>(OnReturned);
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
            if (_departButton != null) _departButton.onClick.AddListener(OnDepartClicked);
            if (_simulateReturnButton != null) _simulateReturnButton.onClick.AddListener(OnSimulateReturnClicked);
            if (_returnButton != null) _returnButton.onClick.AddListener(OnReturnClicked);

            RefreshStatus();
        }

        protected override void OnHide()
        {
            if (_departButton != null) _departButton.onClick.RemoveListener(OnDepartClicked);
            if (_simulateReturnButton != null) _simulateReturnButton.onClick.RemoveListener(OnSimulateReturnClicked);
            if (_returnButton != null) _returnButton.onClick.RemoveListener(OnReturnClicked);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ExplorationDepartedEvent>(OnDeparted);
            EventBus.Unsubscribe<ExplorationReturnedEvent>(OnReturned);
        }

        private void OnDeparted(ExplorationDepartedEvent e)
        {
            if (gameObject.activeInHierarchy) RefreshStatus();
        }

        private void OnReturned(ExplorationReturnedEvent e)
        {
            if (gameObject.activeInHierarchy) RefreshStatus();
        }

        private void RefreshStatus()
        {
            bool canDepart = _explorationManager.CanDepart();

            if (_departButton != null)
            {
                _departButton.interactable = canDepart;
            }

            if (_simulateReturnButton != null)
            {
                _simulateReturnButton.interactable = !canDepart;
            }

            if (_statusLabel != null)
            {
                _statusLabel.text = canDepart ? "準備出發" : "探索中...";
            }
        }

        private void OnDepartClicked()
        {
            _explorationManager.Depart();
        }

        private void OnSimulateReturnClicked()
        {
            // IT 階段：模擬帶著固定戰利品返回
            System.Collections.Generic.Dictionary<string, int> loot
                = new System.Collections.Generic.Dictionary<string, int>
                {
                    { "Wood", 5 },
                    { "Meat", 2 }
                };
            _explorationManager.SimulateReturn(loot);
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
