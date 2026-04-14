using UnityEngine;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// Polls keyboard input each frame and forwards directional commands to
    /// <see cref="PlayerGridMovement"/> and interaction commands to
    /// <see cref="CollectionManager"/>. Supports WASD, arrow keys, and E key.
    /// </summary>
    public class ExplorationInputHandler : MonoBehaviour
    {
        private PlayerGridMovement _playerMovement;
        private CollectionManager _collectionManager;

        /// <summary>
        /// Binds this handler to the given movement and collection instances.
        /// Must be called once before the component starts receiving Update calls.
        /// </summary>
        public void Initialize(PlayerGridMovement playerMovement, CollectionManager collectionManager = null)
        {
            _playerMovement = playerMovement;
            _collectionManager = collectionManager;
        }

        private void Update()
        {
            if (_playerMovement == null)
                return;

            // E key: interact with collectible point or cancel gathering
            if (_collectionManager != null && Input.GetKeyDown(KeyCode.E))
            {
                if (_collectionManager.IsCollecting)
                {
                    // During Gathering phase: E cancels gathering (GDD rule 44)
                    if (_collectionManager.ActivePointState != null &&
                        _collectionManager.ActivePointState.Phase == GatheringPhase.Gathering)
                    {
                        _collectionManager.CancelGathering();
                        return;
                    }
                }
                else if (_collectionManager.CanInteract())
                {
                    _collectionManager.TryStartGathering();
                    return;
                }
            }

            // Escape key: close item panel during Unlocking phase
            if (_collectionManager != null && Input.GetKeyDown(KeyCode.Escape))
            {
                if (_collectionManager.IsCollecting &&
                    _collectionManager.ActivePointState != null &&
                    _collectionManager.ActivePointState.Phase == GatheringPhase.Unlocking)
                {
                    _collectionManager.CloseItemPanel();
                    return;
                }
            }

            // Movement keys (disabled while collecting)
            if (_collectionManager != null && _collectionManager.IsCollecting)
                return;

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                _playerMovement.TryMove(MoveDirection.Up);
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                _playerMovement.TryMove(MoveDirection.Down);
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                _playerMovement.TryMove(MoveDirection.Left);
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                _playerMovement.TryMove(MoveDirection.Right);
        }
    }
}
