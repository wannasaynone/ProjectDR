// CollectionGatheringView — 採集進度條 View（第一層計時）。
// 採集開始後在玩家頭上顯示進度條，即時反映 CollectiblePointState.GatheringProgress。
// 採集取消時隱藏，採集完成後隱藏（由第二層 UI 接手）。
// 使用 WorldSpace SpriteRenderer 進度條。

using System;
using KahaGameCore.GameEvent;
using TMPro;
using UnityEngine;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// 採集第一層計時的進度條 View。
    /// 顯示在玩家位置上方，以 WorldSpace SpriteRenderer 繪製。
    /// </summary>
    public class CollectionGatheringView : MonoBehaviour
    {
        private CollectionManager _collectionManager;
        private ExplorationMapView _mapView;

        // 進度條組件
        private GameObject _barContainer;
        private SpriteRenderer _bgBar;
        private SpriteRenderer _fillBar;
        private TextMeshPro _labelText;

        private bool _isVisible;
        private Vector3 _targetPosition;

        private Action<CollectionStartedEvent> _onCollectionStarted;
        private Action<CollectionCancelledEvent> _onCollectionCancelled;
        private Action<GatheringCompletedEvent> _onGatheringCompleted;

        private const float BarWidth = 1.2f;
        private const float BarHeight = 0.15f;

        /// <summary>
        /// 初始化採集進度條，建立 UI 結構並訂閱事件。
        /// </summary>
        public void Initialize(CollectionManager collectionManager, ExplorationMapView mapView)
        {
            _collectionManager = collectionManager;
            _mapView = mapView;

            BuildBarUI();

            _onCollectionStarted = (e) =>
            {
                _targetPosition = _mapView.GridToWorldPosition(e.X, e.Y) + new Vector3(0f, 1.1f, -0.1f);
                transform.position = _targetPosition;
                ShowBar();
            };

            _onCollectionCancelled = (e) => HideBar();
            _onGatheringCompleted = (e) => HideBar();

            EventBus.Subscribe<CollectionStartedEvent>(_onCollectionStarted);
            EventBus.Subscribe<CollectionCancelledEvent>(_onCollectionCancelled);
            EventBus.Subscribe<GatheringCompletedEvent>(_onGatheringCompleted);

            HideBar();
        }

        private void BuildBarUI()
        {
            _barContainer = new GameObject("GatheringBarContainer");
            _barContainer.transform.SetParent(transform);
            _barContainer.transform.localPosition = Vector3.zero;

            // 背景條
            GameObject bgObj = new GameObject("BgBar");
            bgObj.transform.SetParent(_barContainer.transform);
            bgObj.transform.localPosition = Vector3.zero;
            bgObj.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);
            _bgBar = bgObj.AddComponent<SpriteRenderer>();
            _bgBar.sprite = CreateWhiteSprite();
            _bgBar.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            _bgBar.sortingOrder = 30;

            // 填充條（pivot 在左側，從左往右填充）
            GameObject fillObj = new GameObject("FillBar");
            fillObj.transform.SetParent(_barContainer.transform);
            // 左對齊：pivot 在左，localPosition 從左端開始
            fillObj.transform.localPosition = new Vector3(-BarWidth / 2f, 0f, -0.01f);
            fillObj.transform.localScale = new Vector3(0f, BarHeight * 0.7f, 1f);
            _fillBar = fillObj.AddComponent<SpriteRenderer>();
            _fillBar.sprite = CreateWhiteSprite();
            _fillBar.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            _fillBar.sortingOrder = 31;

            // 標籤文字
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(_barContainer.transform);
            labelObj.transform.localPosition = new Vector3(0f, BarHeight + 0.15f, -0.01f);
            labelObj.transform.localScale = Vector3.one;
            _labelText = labelObj.AddComponent<TextMeshPro>();
            _labelText.text = "採集中...";
            _labelText.alignment = TextAlignmentOptions.Center;
            _labelText.fontSize = 1.8f;
            _labelText.color = Color.white;
            _labelText.sortingOrder = 32;
            RectTransform rt = _labelText.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2f, 0.5f);
        }

        private void Update()
        {
            if (!_isVisible) return;
            if (_collectionManager == null) return;

            CollectiblePointState state = _collectionManager.ActivePointState;
            if (state == null || state.Phase != GatheringPhase.Gathering)
                return;

            float progress = state.GatheringProgress;
            UpdateFillBar(progress);
        }

        private void UpdateFillBar(float progress)
        {
            // 填充條 localScale.x 從 0 到 BarWidth
            Vector3 scale = _fillBar.transform.localScale;
            scale.x = BarWidth * Mathf.Clamp01(progress);
            _fillBar.transform.localScale = scale;

            // 位置跟著 scale 修正（pivot 在左端）
            Vector3 pos = _fillBar.transform.localPosition;
            pos.x = -BarWidth / 2f + scale.x / 2f;
            _fillBar.transform.localPosition = pos;
        }

        private void ShowBar()
        {
            _isVisible = true;
            _barContainer.SetActive(true);
        }

        private void HideBar()
        {
            _isVisible = false;
            if (_barContainer != null)
                _barContainer.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_onCollectionStarted != null)
                EventBus.Unsubscribe<CollectionStartedEvent>(_onCollectionStarted);
            if (_onCollectionCancelled != null)
                EventBus.Unsubscribe<CollectionCancelledEvent>(_onCollectionCancelled);
            if (_onGatheringCompleted != null)
                EventBus.Unsubscribe<GatheringCompletedEvent>(_onGatheringCompleted);
        }

        private static Sprite CreateWhiteSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 4f);
        }
    }
}
