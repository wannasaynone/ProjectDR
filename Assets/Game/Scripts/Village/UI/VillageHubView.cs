using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 村莊主畫面（Hub）。
    /// 顯示可導航區域的按鈕清單，玩家點擊後觸發導航。
    /// </summary>
    public class VillageHubView : ViewBase
    {
        [SerializeField] private Transform _areaButtonContainer;
        [SerializeField] private Button _areaButtonPrefab;

        private VillageNavigationManager _navigationManager;

        public void Initialize(VillageNavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
            EventBus.Subscribe<AreaUnlockedEvent>(OnAreaUnlocked);
        }

        protected override void OnShow()
        {
            RefreshAreaButtons();
        }

        protected override void OnHide()
        {
            // 無需額外清理
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<AreaUnlockedEvent>(OnAreaUnlocked);
        }

        private void OnAreaUnlocked(AreaUnlockedEvent e)
        {
            if (gameObject.activeInHierarchy)
            {
                RefreshAreaButtons();
            }
        }

        private void RefreshAreaButtons()
        {
            if (_areaButtonContainer == null) return;

            // 清除現有按鈕
            for (int i = _areaButtonContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_areaButtonContainer.GetChild(i).gameObject);
            }

            IReadOnlyList<string> areas = _navigationManager.GetNavigableAreas();
            foreach (string areaId in areas)
            {
                string capturedAreaId = areaId;
                Button button = Instantiate(_areaButtonPrefab, _areaButtonContainer);
                button.gameObject.SetActive(true);

                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = GetAreaDisplayName(capturedAreaId);
                }

                button.onClick.AddListener(() => _navigationManager.NavigateTo(capturedAreaId));
            }
        }

        private string GetAreaDisplayName(string areaId)
        {
            switch (areaId)
            {
                case AreaIds.Storage: return "倉庫（村長夫人）";
                case AreaIds.Exploration: return "探索（獵人）";
                case AreaIds.Alchemy: return "煉藥坊（魔女）";
                case AreaIds.Farm: return "農場（農女）";
                default: return areaId;
            }
        }
    }
}
