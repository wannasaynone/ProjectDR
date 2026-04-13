using System.Collections.Generic;
using UnityEngine;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 管理村莊場景中所有 UI 畫面的顯示切換。
    /// 採用排他式顯示（同時只顯示一個 View），
    /// 由 VillageEntryPoint 初始化並注入所有 View 實例。
    /// </summary>
    public class UIToolkitViewController
    {
        private readonly Dictionary<string, UIToolkitViewBase> _views
            = new Dictionary<string, UIToolkitViewBase>();

        private UIToolkitViewBase _currentView;

        /// <summary>註冊一個 View 到控制器，以 viewId 為鍵。</summary>
        public void RegisterView(string viewId, UIToolkitViewBase view)
        {
            _views[viewId] = view;
            view.Hide();
        }

        /// <summary>切換至指定 viewId 的畫面，隱藏當前畫面。</summary>
        public void ShowView(string viewId)
        {
            if (!_views.TryGetValue(viewId, out UIToolkitViewBase targetView))
            {
                Debug.LogWarning($"[UIToolkitViewController] View not found: {viewId}");
                return;
            }

            if (_currentView != null && _currentView != targetView)
            {
                _currentView.Hide();
            }

            _currentView = targetView;
            _currentView.Show();
        }

        /// <summary>隱藏所有畫面，回到空白狀態（Hub 主畫面）。</summary>
        public void HideAll()
        {
            if (_currentView != null)
            {
                _currentView.Hide();
                _currentView = null;
            }
        }

        public UIToolkitViewBase CurrentView => _currentView;
    }
}
