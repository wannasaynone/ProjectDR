using System.Collections.Generic;
using UnityEngine;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 支援返回操作與 Prefab Clone 加載的 UI Toolkit 畫面控制器。
    /// 以 Stack 管理導航歷史，PushView 推入新畫面、Back 返回上一層。
    /// View 以 Prefab 形式註冊，首次顯示時 Instantiate，之後快取重用。
    /// </summary>
    public class UIToolkitStackController
    {
        private readonly Dictionary<string, UIToolkitViewBase> _prefabs
            = new Dictionary<string, UIToolkitViewBase>();

        private readonly Dictionary<string, UIToolkitViewBase> _instances
            = new Dictionary<string, UIToolkitViewBase>();

        private readonly Stack<string> _history = new Stack<string>();
        private readonly Transform _container;

        private string _currentViewId;

        /// <summary>
        /// 建立 StackController。
        /// </summary>
        /// <param name="container">Instantiate 出來的 View 會放在此 Transform 下。</param>
        public UIToolkitStackController(Transform container)
        {
            _container = container;
        }

        /// <summary>目前顯示的 View ID，若無則為 null。</summary>
        public string CurrentViewId => _currentViewId;

        /// <summary>目前顯示的 View 實例，若無則為 null。</summary>
        public UIToolkitViewBase CurrentView =>
            _currentViewId != null && _instances.TryGetValue(_currentViewId, out UIToolkitViewBase view)
                ? view
                : null;

        /// <summary>導航歷史深度（不含目前畫面）。</summary>
        public int HistoryCount => _history.Count;

        /// <summary>
        /// 註冊一個 View Prefab，以 viewId 為鍵。
        /// 實際的 Instantiate 會延遲到第一次 PushView 時才執行。
        /// </summary>
        public void RegisterPrefab(string viewId, UIToolkitViewBase prefab)
        {
            _prefabs[viewId] = prefab;
        }

        /// <summary>
        /// 推入並顯示指定 viewId 的畫面。
        /// 若目前有畫面，會先隱藏並將其 ID 推入歷史 Stack。
        /// 若該 View 尚未 Instantiate，會從 Prefab Clone 一份。
        /// </summary>
        public void PushView(string viewId)
        {
            if (viewId == _currentViewId) return;

            if (!_prefabs.ContainsKey(viewId) && !_instances.ContainsKey(viewId))
            {
                Debug.LogWarning($"[UIToolkitStackController] View not registered: {viewId}");
                return;
            }

            // 隱藏目前畫面，推入歷史
            if (_currentViewId != null)
            {
                HideView(_currentViewId);
                _history.Push(_currentViewId);
            }

            _currentViewId = viewId;
            ShowView(viewId);
        }

        /// <summary>
        /// 返回上一個畫面。
        /// 隱藏目前畫面，從歷史 Stack 彈出上一個畫面並顯示。
        /// 若歷史為空，則回傳 false 不執行任何動作。
        /// </summary>
        public bool Back()
        {
            if (_history.Count == 0) return false;

            if (_currentViewId != null)
            {
                HideView(_currentViewId);
            }

            _currentViewId = _history.Pop();
            ShowView(_currentViewId);
            return true;
        }

        /// <summary>
        /// 清除歷史並直接顯示指定畫面（通常用於回到根畫面）。
        /// 隱藏目前畫面並清空 Stack，不觸發逐層 Back。
        /// </summary>
        public void SetRoot(string viewId)
        {
            if (_currentViewId != null)
            {
                HideView(_currentViewId);
            }

            _history.Clear();
            _currentViewId = viewId;
            ShowView(viewId);
        }

        /// <summary>
        /// 取得或建立指定 viewId 的 View 實例。
        /// 若已有快取實例則直接回傳；否則從 Prefab Clone。
        /// </summary>
        public UIToolkitViewBase GetOrCreateInstance(string viewId)
        {
            if (_instances.TryGetValue(viewId, out UIToolkitViewBase existing))
            {
                return existing;
            }

            if (!_prefabs.TryGetValue(viewId, out UIToolkitViewBase prefab))
            {
                return null;
            }

            UIToolkitViewBase instance = Object.Instantiate(prefab, _container);
            instance.gameObject.SetActive(false);
            _instances[viewId] = instance;
            return instance;
        }

        private void ShowView(string viewId)
        {
            UIToolkitViewBase view = GetOrCreateInstance(viewId);
            if (view != null)
            {
                view.Show();
            }
        }

        private void HideView(string viewId)
        {
            if (_instances.TryGetValue(viewId, out UIToolkitViewBase view))
            {
                view.Hide();
            }
        }
    }
}
