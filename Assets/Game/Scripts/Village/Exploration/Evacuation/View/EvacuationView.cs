using ProjectDR.Village.Exploration.Core;
using System;
using KahaGameCore.GameEvent;
using TMPro;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Evacuation
{
    /// <summary>
    /// View layer for the evacuation countdown.
    /// Displays a WorldSpace TextMeshPro text showing the remaining time.
    /// Subscribes to evacuation events and updates the display accordingly.
    /// </summary>
    public class EvacuationView : MonoBehaviour
    {
        private EvacuationManager _evacuationManager;
        private TextMeshPro _text;

        private Action<EvacuationStartedEvent> _onStarted;
        private Action<EvacuationCancelledEvent> _onCancelled;
        private Action<ExplorationCompletedEvent> _onCompleted;

        private bool _showingCountdown;
        private bool _showingComplete;

        /// <summary>
        /// Initializes the view, creates the text display, and subscribes to events.
        /// </summary>
        /// <param name="evacuationManager">The manager to read countdown state from.</param>
        /// <param name="worldPosition">
        /// World position for the text display (typically above the map).
        /// </param>
        public void Initialize(EvacuationManager evacuationManager, Vector3 worldPosition)
        {
            _evacuationManager = evacuationManager;
            transform.position = worldPosition;

            _text = gameObject.AddComponent<TextMeshPro>();
            _text.alignment = TextAlignmentOptions.Center;
            _text.fontSize = 4f;
            _text.sortingOrder = 20;
            _text.enabled = false;

            _onStarted = (e) =>
            {
                _showingCountdown = true;
                _showingComplete = false;
                _text.enabled = true;
            };

            _onCancelled = (e) =>
            {
                _showingCountdown = false;
                _text.enabled = false;
            };

            _onCompleted = (e) =>
            {
                _showingCountdown = false;
                _showingComplete = true;
                _text.text = "Evacuation Complete!";
                _text.enabled = true;
            };

            EventBus.Subscribe<EvacuationStartedEvent>(_onStarted);
            EventBus.Subscribe<EvacuationCancelledEvent>(_onCancelled);
            EventBus.Subscribe<ExplorationCompletedEvent>(_onCompleted);
        }

        private void Update()
        {
            if (_showingCountdown && _evacuationManager != null)
            {
                float remaining = _evacuationManager.RemainingTime;
                _text.text = string.Format("Evacuating... {0:F1}s", remaining);
            }
        }

        private void OnDestroy()
        {
            if (_onStarted != null)
                EventBus.Unsubscribe<EvacuationStartedEvent>(_onStarted);
            if (_onCancelled != null)
                EventBus.Unsubscribe<EvacuationCancelledEvent>(_onCancelled);
            if (_onCompleted != null)
                EventBus.Unsubscribe<ExplorationCompletedEvent>(_onCompleted);
        }
    }
}
