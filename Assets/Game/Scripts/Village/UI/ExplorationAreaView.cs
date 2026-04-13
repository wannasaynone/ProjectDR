using KahaGameCore.GameEvent;
using ProjectDR.Village;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 探索入口畫面（IT 階段 Placeholder）。
    /// 提供出發探索與模擬返回的按鈕。
    /// </summary>
    public class ExplorationAreaView : UIToolkitViewBase
    {
        private ExplorationEntryManager _explorationManager;
        private VillageNavigationManager _navigationManager;
        private Button _departButton;
        private Button _simulateReturnButton;
        private Button _returnButton;
        private Label _statusLabel;

        public void Initialize(ExplorationEntryManager explorationManager, VillageNavigationManager navigationManager)
        {
            _explorationManager = explorationManager;
            _navigationManager = navigationManager;
            EventBus.Subscribe<ExplorationDepartedEvent>(OnDeparted);
            EventBus.Subscribe<ExplorationReturnedEvent>(OnReturned);
        }

        protected override void OnShow()
        {
            _departButton = Root.Q<Button>("depart-button");
            _simulateReturnButton = Root.Q<Button>("simulate-return-button");
            _returnButton = Root.Q<Button>("return-button");
            _statusLabel = Root.Q<Label>("status-label");

            if (_departButton != null) _departButton.clicked += OnDepartClicked;
            if (_simulateReturnButton != null) _simulateReturnButton.clicked += OnSimulateReturnClicked;
            if (_returnButton != null) _returnButton.clicked += OnReturnClicked;

            RefreshStatus();
        }

        protected override void OnHide()
        {
            if (_departButton != null) _departButton.clicked -= OnDepartClicked;
            if (_simulateReturnButton != null) _simulateReturnButton.clicked -= OnSimulateReturnClicked;
            if (_returnButton != null) _returnButton.clicked -= OnReturnClicked;
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
                _departButton.SetEnabled(canDepart);
            }

            if (_simulateReturnButton != null)
            {
                _simulateReturnButton.SetEnabled(!canDepart);
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
            _navigationManager.ReturnToHub();
        }
    }
}
