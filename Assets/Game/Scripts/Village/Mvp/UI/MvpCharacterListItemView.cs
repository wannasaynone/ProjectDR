// MvpCharacterListItemView — 角色清單單一項目 UI 元件。
// 顯示角色名字、好感度、紅點，點擊時觸發回呼進入角色互動 View。

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDR.Village.Mvp.UI
{
    /// <summary>
    /// 角色清單單一項目：名字 + 好感度 + 紅點 + 點擊按鈕。
    /// 由 MvpMainView 動態生成，透過 Bind 注入資料。
    /// </summary>
    [DisallowMultipleComponent]
    public class MvpCharacterListItemView : MonoBehaviour
    {
        [Header("顯示元件")]
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _affinityLabel;
        [SerializeField] private Button _itemButton;
        [SerializeField] private RedDotView _redDot;

        private string _characterId;
        private Action<string> _onClickCallback;

        /// <summary>當前綁定的角色 ID。</summary>
        public string CharacterId => _characterId;

        /// <summary>
        /// 由 MvpMainView 呼叫綁定資料與點擊回呼。
        /// </summary>
        public void Bind(string characterId, string displayName, int affinity, Action<string> onClickCallback)
        {
            _characterId = characterId;
            _onClickCallback = onClickCallback;

            if (_nameLabel != null) _nameLabel.text = displayName;
            if (_affinityLabel != null) _affinityLabel.text = $"好感度：{affinity}";
            if (_redDot != null) _redDot.SetVisible(false);

            if (_itemButton != null)
            {
                _itemButton.onClick.RemoveAllListeners();
                _itemButton.onClick.AddListener(OnClicked);
            }
        }

        /// <summary>更新好感度顯示。</summary>
        public void SetAffinity(int affinity)
        {
            if (_affinityLabel != null) _affinityLabel.text = $"好感度：{affinity}";
        }

        /// <summary>設定紅點顯示狀態。</summary>
        public void SetRedDotVisible(bool visible)
        {
            if (_redDot != null) _redDot.SetVisible(visible);
        }

        private void OnClicked()
        {
            _onClickCallback?.Invoke(_characterId);
        }

        private void OnDestroy()
        {
            if (_itemButton != null)
            {
                _itemButton.onClick.RemoveListener(OnClicked);
            }
        }
    }
}
