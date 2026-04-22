using ProjectDR.Village.Exploration.Core;
using ProjectDR.Village.Exploration.Camera;
using System;
using KahaGameCore.GameEvent;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Movement
{
    /// <summary>
    /// Visual representation of the player token on the exploration map.
    /// Listens to <see cref="PlayerMoveStartedEvent"/> and drives a manual Lerp animation
    /// between grid cells. Notifies <see cref="PlayerGridMovement"/> when the animation ends.
    /// </summary>
    public class ExplorationPlayerView : MonoBehaviour
    {
        private PlayerGridMovement _playerMovement;
        private ExplorationMapView _mapView;
        private SpriteRenderer _spriteRenderer;

        private bool _isLerping;
        private Vector3 _lerpFrom;
        private Vector3 _lerpTo;
        private float _lerpDuration;
        private float _lerpElapsed;

        private Action<PlayerMoveStartedEvent> _onMoveStarted;

        /// <summary>
        /// Initializes the player token, places it at <paramref name="startPosition"/>,
        /// and subscribes to move events.
        /// </summary>
        public void Initialize(PlayerGridMovement playerMovement, ExplorationMapView mapView,
            Vector2Int startPosition)
        {
            _playerMovement = playerMovement;
            _mapView = mapView;

            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sprite = CreateWhiteSprite();
            _spriteRenderer.color = Color.white;
            _spriteRenderer.sortingOrder = 10;

            // Render the player token smaller than a full cell so it is visually distinct.
            transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            transform.position = mapView.GridToWorldPosition(startPosition.x, startPosition.y);

            _onMoveStarted = (e) =>
            {
                _lerpFrom = _mapView.GridToWorldPosition(e.From.x, e.From.y);
                _lerpTo = _mapView.GridToWorldPosition(e.To.x, e.To.y);
                _lerpDuration = e.MoveDuration;
                _lerpElapsed = 0f;
                _isLerping = true;
            };
            EventBus.Subscribe<PlayerMoveStartedEvent>(_onMoveStarted);
        }

        private void Update()
        {
            if (!_isLerping)
                return;

            _lerpElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_lerpElapsed / _lerpDuration);
            transform.position = Vector3.Lerp(_lerpFrom, _lerpTo, t);

            if (t >= 1f)
            {
                _isLerping = false;
                _playerMovement.CompleteMoveAnimation();
            }
        }

        private void OnDestroy()
        {
            if (_onMoveStarted != null)
                EventBus.Unsubscribe<PlayerMoveStartedEvent>(_onMoveStarted);
        }

        // Creates a plain white 4x4 texture sprite used as the player token.
        private static Sprite CreateWhiteSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}
