using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 村莊主畫面（Hub）。
    /// 顯示可導航區域的按鈕清單，玩家點擊後觸發導航。
    /// </summary>
    public class VillageHubView : UIToolkitViewBase
    {
        private VillageNavigationManager _navigationManager;
        private VisualElement _areaButtonContainer;

        public void Initialize(VillageNavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
            EventBus.Subscribe<AreaUnlockedEvent>(OnAreaUnlocked);
        }

        protected override void OnShow()
        {
            _areaButtonContainer = Root.Q<VisualElement>("area-button-container");
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

            _areaButtonContainer.Clear();

            IReadOnlyList<string> areas = _navigationManager.GetNavigableAreas();
            foreach (string areaId in areas)
            {
                string capturedAreaId = areaId;
                Button button = new Button(() => _navigationManager.NavigateTo(capturedAreaId))
                {
                    text = GetAreaDisplayName(capturedAreaId),
                    name = $"btn-{capturedAreaId.ToLower()}"
                };
                button.AddToClassList("area-button");
                _areaButtonContainer.Add(button);
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
