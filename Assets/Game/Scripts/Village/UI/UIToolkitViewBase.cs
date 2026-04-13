using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 所有村莊 UI 畫面的抽象基類（基於 UI Toolkit）。
    /// 每個 View 掛載在帶有 UIDocument 的 GameObject 上，
    /// 透過 Initialize() 接收相依注入。
    /// </summary>
    public abstract class UIToolkitViewBase : MonoBehaviour
    {
        private UIDocument _uiDocument;

        protected VisualElement Root { get; private set; }

        protected virtual void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
            {
                Root = _uiDocument.rootVisualElement;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            EnsureRoot();
            OnShow();
        }

        private void EnsureRoot()
        {
            // UIDocument 在 GameObject deactivate/reactivate 後會重建 rootVisualElement，
            // 因此每次 Show 都必須重新取得，不可快取舊引用。
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
            {
                Root = _uiDocument.rootVisualElement;
            }
        }

        public void Hide()
        {
            if (!gameObject.activeSelf) return;
            OnHide();
            gameObject.SetActive(false);
        }

        /// <summary>在畫面顯示時呼叫，子類別覆寫以處理顯示邏輯。</summary>
        protected virtual void OnShow() { }

        /// <summary>在畫面隱藏時呼叫，子類別覆寫以處理清理邏輯。</summary>
        protected virtual void OnHide() { }
    }
}
