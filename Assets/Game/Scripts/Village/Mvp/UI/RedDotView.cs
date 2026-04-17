// RedDotView — 紅點 UI 元件。
// 掛在需要顯示紅點提示的子 GameObject 上，簡單的 SetActive 控制顯示。

using UnityEngine;
using UnityEngine.UI;

namespace ProjectDR.Village.Mvp.UI
{
    /// <summary>
    /// 紅點 UI 元件。
    /// 內含一個 Image（紅色圓點），SetVisible(true/false) 切換顯示。
    /// </summary>
    [DisallowMultipleComponent]
    public class RedDotView : MonoBehaviour
    {
        [Header("紅點圖像")]
        [Tooltip("紅點的 Image 元件（子物件）。")]
        [SerializeField] private Image _dotImage;

        /// <summary>
        /// 切換紅點顯示狀態。
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_dotImage != null)
            {
                _dotImage.enabled = visible;
            }
            gameObject.SetActive(visible);
        }

        /// <summary>當前是否可見。</summary>
        public bool IsVisible => gameObject.activeSelf && (_dotImage == null || _dotImage.enabled);
    }
}
