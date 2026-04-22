using ProjectDR.Village.Exploration.Collection;
using ProjectDR.Village.Exploration.Movement;
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
        private PlayerFreeMovement _playerMovement;
        private CollectionManager _collectionManager;
        private Transform _playerVisualTransform;
        private UnityEngine.Camera _mainCamera;

        public void Initialize(SwordAttack swordAttack, PlayerFreeMovement playerMovement,
            CollectionManager collectionManager = null,
            Transform playerVisualTransform = null)
        {
            _swordAttack = swordAttack;
            _playerMovement = playerMovement;
            _collectionManager = collectionManager;
            _playerVisualTransform = playerVisualTransform;
            _mainCamera = UnityEngine.Camera.main;
        }

        private void Update()
        {
            if (_swordAttack == null || _playerMovement == null)
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
                _mainCamera = UnityEngine.Camera.main;
            if (_mainCamera == null)
                return;

            // Get player world position from free movement
            Vector2 playerWorldPos;
            if (_playerVisualTransform != null)
            {
                playerWorldPos = new Vector2(_playerVisualTransform.position.x, _playerVisualTransform.position.y);
            }
            else
            {
                playerWorldPos = _playerMovement.WorldPosition;
            }

            // Get mouse world position
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
