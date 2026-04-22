using UnityEngine;

namespace ProjectDR.Village.Shared
{
    /// <summary>
    /// 所有村莊 UI 畫面的抽象基類（基於 UGUI）。
    /// 每個 View 掛載在 GameObject 上，透過 Initialize() 接收相依注入。
    /// </summary>
    public abstract class ViewBase : MonoBehaviour
    {
        public void Show()
        {
            gameObject.SetActive(true);
            OnShow();
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
