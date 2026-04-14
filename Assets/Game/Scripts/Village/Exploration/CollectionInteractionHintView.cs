// CollectionInteractionHintView — 採集互動提示 View。
// 玩家站在有採集點的格子上時，在玩家頭上顯示「按 E 採集」提示文字。
// 採集進行中改顯示「按 E 取消」，Unlocking 階段不顯示任何提示。
// 使用 WorldSpace TextMeshPro。

using System;
using KahaGameCore.GameEvent;
using TMPro;
using UnityEngine;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// 在玩家頭上顯示採集互動提示（WorldSpace TextMeshPro）。
    /// 根據當前採集狀態動態切換顯示文字。
    /// </summary>
    public class CollectionInteractionHintView : MonoBehaviour
    {
        private CollectionManager _collectionManager;
        private ExplorationMapView _mapView;
        private TextMeshPro _text;

        private Action<PlayerMoveCompletedEvent> _onPlayerMoveCompleted;
        private Action<CollectionStartedEvent> _onCollectionStarted;
        private Action<CollectionCancelledEvent> _onCollectionCancelled;
        private Action<GatheringCompletedEvent> _onGatheringCompleted;
        private Action<CollectionPanelClosedEvent> _onPanelClosed;

        private const string HintInteract = "[E] 採集";
        private const string HintCancel = "[E] 取消";

        /// <summary>
        /// 初始化提示 View，建立 TMP 文字並訂閱事件。
        /// </summary>
        public void Initialize(CollectionManager collectionManager, ExplorationMapView mapView)
        {
            _collectionManager = collectionManager;
            _mapView = mapView;

            // 建立 WorldSpace TMP
            _text = gameObject.AddComponent<TextMeshPro>();
            _text.alignment = TextAlignmentOptions.Center;
            _text.fontSize = 2.5f;
            _text.color = new Color(0.2f, 0.9f, 1f);
            _text.sortingOrder = 25;
            _text.enabled = false;

            RectTransform rt = _text.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(3f, 0.8f);

            _onPlayerMoveCompleted = (e) => RefreshHint(e.Position);
            _onCollectionStarted = (e) => UpdateHintText();
            _onCollectionCancelled = (e) => UpdateHintText();
            _onGatheringCompleted = (e) => HideHint();
            _onPanelClosed = (e) => UpdateHintBasedOnCurrentPosition();

            EventBus.Subscribe<PlayerMoveCompletedEvent>(_onPlayerMoveCompleted);
            EventBus.Subscribe<CollectionStartedEvent>(_onCollectionStarted);
            EventBus.Subscribe<CollectionCancelledEvent>(_onCollectionCancelled);
            EventBus.Subscribe<GatheringCompletedEvent>(_onGatheringCompleted);
            EventBus.Subscribe<CollectionPanelClosedEvent>(_onPanelClosed);
        }

        private void RefreshHint(Vector2Int position)
        {
            Vector3 worldPos = _mapView.GridToWorldPosition(position.x, position.y)
                + new Vector3(0f, 0.8f, -0.1f);
            transform.position = worldPos;
            UpdateHintText();
        }

        private void UpdateHintText()
        {
            if (_collectionManager == null)
            {
                HideHint();
                return;
            }

            // Unlocking 階段：不顯示提示
            if (_collectionManager.ActivePointState != null &&
                _collectionManager.ActivePointState.Phase == GatheringPhase.Unlocking)
            {
                HideHint();
                return;
            }

            // Gathering 階段：顯示「按 E 取消」
            if (_collectionManager.ActivePointState != null &&
                _collectionManager.ActivePointState.Phase == GatheringPhase.Gathering)
            {
                ShowHint(HintCancel);
                return;
            }

            // Idle 階段且站在採集點上：顯示「按 E 採集」
            if (_collectionManager.CanInteract())
            {
                ShowHint(HintInteract);
                return;
            }

            HideHint();
        }

        private void UpdateHintBasedOnCurrentPosition()
        {
            UpdateHintText();
        }

        private void ShowHint(string message)
        {
            _text.text = message;
            _text.enabled = true;
        }

        private void HideHint()
        {
            _text.enabled = false;
        }

        private void OnDestroy()
        {
            if (_onPlayerMoveCompleted != null)
                EventBus.Unsubscribe<PlayerMoveCompletedEvent>(_onPlayerMoveCompleted);
            if (_onCollectionStarted != null)
                EventBus.Unsubscribe<CollectionStartedEvent>(_onCollectionStarted);
            if (_onCollectionCancelled != null)
                EventBus.Unsubscribe<CollectionCancelledEvent>(_onCollectionCancelled);
            if (_onGatheringCompleted != null)
                EventBus.Unsubscribe<GatheringCompletedEvent>(_onGatheringCompleted);
            if (_onPanelClosed != null)
                EventBus.Unsubscribe<CollectionPanelClosedEvent>(_onPanelClosed);
        }
    }
}
