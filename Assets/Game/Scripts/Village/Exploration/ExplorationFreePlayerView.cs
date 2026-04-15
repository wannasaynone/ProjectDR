using UnityEngine;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// Visual representation of the player token for the free-movement model.
    /// Each frame, syncs the transform position to <see cref="PlayerFreeMovement.WorldPosition"/>.
    /// Replaces ExplorationPlayerView for the free-movement model.
    /// </summary>
    public class ExplorationFreePlayerView : MonoBehaviour
    {
        private PlayerFreeMovement _playerMovement;
        private SpriteRenderer _spriteRenderer;

        /// <summary>
        /// Initializes the player token, places it at the player's world position.
        /// </summary>
        public void Initialize(PlayerFreeMovement playerMovement)
        {
            _playerMovement = playerMovement;

            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sprite = CreateWhiteSprite();
            _spriteRenderer.color = Color.white;
            _spriteRenderer.sortingOrder = 10;

            // Render the player token smaller than a full cell so it is visually distinct.
            transform.localScale = new Vector3(0.6f, 0.6f, 1f);

            // Set initial position
            SyncPosition();
        }

        private void Update()
        {
            if (_playerMovement == null) return;
            SyncPosition();
        }

        private void SyncPosition()
        {
            Vector2 wp = _playerMovement.WorldPosition;
            transform.position = new Vector3(wp.x, wp.y, 0f);
        }

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
