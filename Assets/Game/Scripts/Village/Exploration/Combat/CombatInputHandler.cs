using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// Handles mouse input for combat:
    /// - Left-click: sword attack in direction toward mouse cursor
    /// GDD rule 14: Attack direction follows mouse, left-click to attack.
    /// Disabled when player movement is locked (during collection, etc.)
    /// </summary>
    public class CombatInputHandler : MonoBehaviour
    {
        private SwordAttack _swordAttack;
        private PlayerGridMovement _playerMovement;
        private ExplorationMapView _mapView;
        private CollectionManager _collectionManager;
        private Transform _playerVisualTransform;
        private Camera _mainCamera;

        public void Initialize(SwordAttack swordAttack, PlayerGridMovement playerMovement,
            ExplorationMapView mapView, CollectionManager collectionManager = null,
            Transform playerVisualTransform = null)
        {
            _swordAttack = swordAttack;
            _playerMovement = playerMovement;
            _mapView = mapView;
            _collectionManager = collectionManager;
            _playerVisualTransform = playerVisualTransform;
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (_swordAttack == null || _playerMovement == null || _mapView == null)
                return;

            // Block attacks during collection interaction
            if (_collectionManager != null && _collectionManager.IsCollecting)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                TryAttack();
            }
        }

        private void TryAttack()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;
            if (_mainCamera == null)
                return;

            // Get player world position — use visual transform if available (accurate during lerp),
            // otherwise fall back to grid-based position.
            Vector3 playerWorldPos3;
            if (_playerVisualTransform != null)
            {
                playerWorldPos3 = _playerVisualTransform.position;
            }
            else
            {
                playerWorldPos3 = _mapView.GridToWorldPosition(
                    _playerMovement.CurrentPosition.x, _playerMovement.CurrentPosition.y);
            }
            Vector2 playerWorldPos = new Vector2(playerWorldPos3.x, playerWorldPos3.y);

            // Get mouse world position — z must be the distance from camera to game plane (z=0),
            // otherwise ScreenToWorldPoint returns the camera's own position for perspective cameras.
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = Mathf.Abs(_mainCamera.transform.position.z);
            Vector3 mouseWorldPos3 = _mainCamera.ScreenToWorldPoint(mouseScreenPos);
            Vector2 mouseWorldPos = new Vector2(mouseWorldPos3.x, mouseWorldPos3.y);

            // Direction from player to mouse
            Vector2 direction = (mouseWorldPos - playerWorldPos).normalized;

            _swordAttack.TryAttack(playerWorldPos, direction);
        }
    }
}
