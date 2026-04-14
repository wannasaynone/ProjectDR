using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 村莊主畫面（Hub）。
    /// 顯示角色按鈕清單，玩家點擊後觸發導航至該角色的互動畫面。
    /// </summary>
    public class VillageHubView : ViewBase
    {
        [SerializeField] private Transform _areaButtonContainer;
        [SerializeField] private Button _areaButtonPrefab;

        private VillageNavigationManager _navigationManager;
        private IReadOnlyList<CharacterMenuData> _characters;

        /// <summary>
        /// 注入相依。
        /// </summary>
        /// <param name="navigationManager">導航管理器。</param>
        /// <param name="characters">角色資料清單。</param>
        public void Initialize(VillageNavigationManager navigationManager, IReadOnlyList<CharacterMenuData> characters)
        {
            _navigationManager = navigationManager;
            _characters = characters;
        }

        protected override void OnShow()
        {
            RefreshCharacterButtons();
        }

        protected override void OnHide()
        {
            // 無需額外清理
        }

        private void RefreshCharacterButtons()
        {
            if (_areaButtonContainer == null) return;

            // 清除現有按鈕
            for (int i = _areaButtonContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_areaButtonContainer.GetChild(i).gameObject);
            }

            if (_characters == null) return;

            foreach (CharacterMenuData character in _characters)
            {
                string capturedCharacterId = character.CharacterId;
                Button button = Instantiate(_areaButtonPrefab, _areaButtonContainer);
                button.gameObject.SetActive(true);

                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = character.DisplayName;
                }

                button.onClick.AddListener(() => _navigationManager.NavigateTo(capturedCharacterId));
            }
        }
    }
}
