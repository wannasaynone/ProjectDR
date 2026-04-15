using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// Displays a line from the player position toward the mouse cursor,
    /// indicating the current aim direction. Uses a LineRenderer.
    /// </summary>
    public class AimIndicatorView : MonoBehaviour
    {
        private Transform _playerTransform;
        private LineRenderer _lineRenderer;
        private Camera _mainCamera;

        private const float LineLength = 2.0f;
        private const float LineWidth = 0.05f;

        public void Initialize(Transform playerTransform)
        {
            _playerTransform = playerTransform;
            _mainCamera = Camera.main;

            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth = LineWidth;
            _lineRenderer.endWidth = LineWidth * 0.5f;
            _lineRenderer.sortingOrder = 20;
            _lineRenderer.useWorldSpace = true;

            // Simple white material
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = new Color(1f, 1f, 1f, 0.6f);
            _lineRenderer.endColor = new Color(1f, 1f, 1f, 0.2f);
        }

        private void Update()
        {
            if (_playerTransform == null) return;

            if (_mainCamera == null)
                _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Vector3 playerPos = _playerTransform.position;

            // Get mouse world position
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = Mathf.Abs(_mainCamera.transform.position.z);
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(mouseScreenPos);

            Vector2 direction = new Vector2(mouseWorldPos.x - playerPos.x, mouseWorldPos.y - playerPos.y);
            if (direction.sqrMagnitude < 0.001f) return;

            direction = direction.normalized;

            Vector3 startPos = playerPos;
            Vector3 endPos = playerPos + new Vector3(direction.x, direction.y, 0f) * LineLength;

            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);
        }
    }
}
