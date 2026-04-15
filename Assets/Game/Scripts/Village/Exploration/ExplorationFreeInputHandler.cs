using UnityEngine;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// Polls keyboard input each frame and drives continuous free movement via
    /// <see cref="PlayerFreeMovement"/>. Also handles interaction commands (E key)
    /// for the collection system.
    /// Replaces ExplorationInputHandler for the free-movement model.
    /// </summary>
    public class ExplorationFreeInputHandler : MonoBehaviour
    {
        private PlayerFreeMovement _playerMovement;
        private CollectionManager _collectionManager;

        /// <summary>
        /// Binds this handler to the given movement and collection instances.
        /// Must be called once before the component starts receiving Update calls.
        /// </summary>
        public void Initialize(PlayerFreeMovement playerMovement, CollectionManager collectionManager = null)
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

            // Movement (disabled while collecting)
            if (_collectionManager != null && _collectionManager.IsCollecting)
                return;

            // Continuous WASD input
            float h = 0f;
            float v = 0f;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                v -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                h += 1f;

            Vector2 input = new Vector2(h, v);
            if (input.sqrMagnitude > 0.001f)
            {
                _playerMovement.Move(input, Time.deltaTime);
            }
        }
    }
}
