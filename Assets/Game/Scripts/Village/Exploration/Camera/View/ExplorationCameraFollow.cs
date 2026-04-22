using UnityEngine;

namespace ProjectDR.Village.Exploration.Camera
{
    /// <summary>
    /// 探索模式攝影機跟隨。
    /// 進入探索時切換攝影機為正交模式（2D 格子地圖需要），
    /// 在 LateUpdate 中平滑跟隨玩家 token，
    /// 銷毀時還原攝影機至初始狀態。
    /// </summary>
    public class ExplorationCameraFollow : MonoBehaviour
    {
        private Transform _target;
        private UnityEngine.Camera _camera;
        private Vector3 _originalCameraPosition;
        private bool _originalOrthographic;
        private float _originalOrthographicSize;
        private float _originalFieldOfView;
        private float _smoothSpeed;
        private float _cameraZ;

        /// <summary>
        /// 初始化攝影機跟隨。
        /// </summary>
        /// <param name="target">跟隨目標（玩家 token 的 Transform）。</param>
        /// <param name="orthographicSize">正交攝影機的 size（控制可視範圍，越小越近）。</param>
        /// <param name="smoothSpeed">平滑跟隨速度（越大越快，建議 5~10）。</param>
        public void Initialize(Transform target, float orthographicSize = 5f, float smoothSpeed = 8f)
        {
            _target = target;
            _smoothSpeed = smoothSpeed;
            _camera = UnityEngine.Camera.main;

            if (_camera != null)
            {
                // 儲存攝影機原始狀態
                _originalCameraPosition = _camera.transform.position;
                _originalOrthographic = _camera.orthographic;
                _originalOrthographicSize = _camera.orthographicSize;
                _originalFieldOfView = _camera.fieldOfView;
                _cameraZ = _originalCameraPosition.z;

                // 切換為正交模式（2D 格子地圖必須使用正交攝影機）
                _camera.orthographic = true;
                _camera.orthographicSize = orthographicSize;

                // 立即將攝影機移到目標位置（避免開場滑動）
                Vector3 targetPos = _target.position;
                _camera.transform.position = new Vector3(targetPos.x, targetPos.y, _cameraZ);
            }
        }

        private void LateUpdate()
        {
            if (_camera == null || _target == null) return;

            Vector3 currentPos = _camera.transform.position;
            Vector3 targetPos = new Vector3(_target.position.x, _target.position.y, _cameraZ);

            _camera.transform.position = Vector3.Lerp(currentPos, targetPos, _smoothSpeed * Time.deltaTime);
        }

        private void OnDestroy()
        {
            // 還原攝影機至初始狀態
            if (_camera != null)
            {
                _camera.transform.position = _originalCameraPosition;
                _camera.orthographic = _originalOrthographic;
                _camera.orthographicSize = _originalOrthographicSize;
                _camera.fieldOfView = _originalFieldOfView;
            }
        }
    }
}
