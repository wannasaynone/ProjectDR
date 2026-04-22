// DeathView — 死亡視覺回饋。
// 訂閱 PlayerDeathEvent，顯示畫面變暗 + 時間回溯文字提示。
// IT 階段使用簡易 UGUI overlay 實作。

using System;
using KahaGameCore.GameEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// 死亡視覺回饋 View。
    /// 訂閱 PlayerDeathEvent，播放：
    /// 1. 全螢幕紅色閃爍
    /// 2. 畫面漸暗
    /// 3.「時間回溯...」文字提示
    /// 4. 短暫延遲後發布 DeathRewindCompletedEvent
    /// </summary>
    public class DeathView : MonoBehaviour
    {
        private Canvas _canvas;
        private Image _overlayImage;
        private TextMeshProUGUI _rewindText;

        private Action<PlayerDeathEvent> _onPlayerDeath;

        // Animation state
        private bool _isPlaying;
        private float _timer;
        private int _phase; // 0=red flash, 1=fade to dark, 2=show text, 3=hold, 4=done

        // Timing constants (IT stage — simple fixed durations)
        private const float RedFlashDuration = 0.3f;
        private const float FadeToDarkDuration = 0.5f;
        private const float TextShowDuration = 0.3f;
        private const float HoldDuration = 1.0f;

        /// <summary>
        /// 初始化 DeathView。建立 Canvas overlay 元件。
        /// </summary>
        public void Initialize()
        {
            CreateOverlay();

            _onPlayerDeath = HandlePlayerDeath;
            EventBus.Subscribe<PlayerDeathEvent>(_onPlayerDeath);
        }

        private void Update()
        {
            if (!_isPlaying) return;

            _timer += Time.deltaTime;

            switch (_phase)
            {
                case 0: // Red flash
                    UpdateRedFlash();
                    break;
                case 1: // Fade to dark
                    UpdateFadeToDark();
                    break;
                case 2: // Show text
                    UpdateShowText();
                    break;
                case 3: // Hold
                    UpdateHold();
                    break;
            }
        }

        private void OnDestroy()
        {
            if (_onPlayerDeath != null)
            {
                EventBus.Unsubscribe<PlayerDeathEvent>(_onPlayerDeath);
            }
        }

        private void HandlePlayerDeath(PlayerDeathEvent e)
        {
            _isPlaying = true;
            _timer = 0f;
            _phase = 0;

            _overlayImage.color = new Color(1f, 0f, 0f, 0f);
            _overlayImage.gameObject.SetActive(true);
            _rewindText.gameObject.SetActive(false);
            _canvas.gameObject.SetActive(true);
        }

        private void UpdateRedFlash()
        {
            float t = Mathf.Clamp01(_timer / RedFlashDuration);
            // Flash red, peak at mid-point
            float alpha = t < 0.5f
                ? Mathf.Lerp(0f, 0.6f, t * 2f)
                : Mathf.Lerp(0.6f, 0.3f, (t - 0.5f) * 2f);
            _overlayImage.color = new Color(1f, 0f, 0f, alpha);

            if (_timer >= RedFlashDuration)
            {
                _timer = 0f;
                _phase = 1;
            }
        }

        private void UpdateFadeToDark()
        {
            float t = Mathf.Clamp01(_timer / FadeToDarkDuration);
            // Transition from red(0.3) to black(0.8)
            float r = Mathf.Lerp(1f, 0f, t);
            float alpha = Mathf.Lerp(0.3f, 0.8f, t);
            _overlayImage.color = new Color(r, 0f, 0f, alpha);

            if (_timer >= FadeToDarkDuration)
            {
                _overlayImage.color = new Color(0f, 0f, 0f, 0.8f);
                _timer = 0f;
                _phase = 2;
            }
        }

        private void UpdateShowText()
        {
            float t = Mathf.Clamp01(_timer / TextShowDuration);
            _rewindText.gameObject.SetActive(true);
            Color textColor = _rewindText.color;
            textColor.a = t;
            _rewindText.color = textColor;

            if (_timer >= TextShowDuration)
            {
                _timer = 0f;
                _phase = 3;
            }
        }

        private void UpdateHold()
        {
            if (_timer >= HoldDuration)
            {
                _isPlaying = false;
                _phase = 4;
                EventBus.Publish(new DeathRewindCompletedEvent());
            }
        }

        private void CreateOverlay()
        {
            // Create a screen-space overlay Canvas
            GameObject canvasObj = new GameObject("DeathOverlayCanvas");
            canvasObj.transform.SetParent(transform);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 999; // On top of everything
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Full-screen overlay image
            GameObject imgObj = new GameObject("DeathOverlay");
            imgObj.transform.SetParent(canvasObj.transform, false);
            _overlayImage = imgObj.AddComponent<Image>();
            _overlayImage.color = new Color(0f, 0f, 0f, 0f);

            RectTransform imgRt = imgObj.GetComponent<RectTransform>();
            imgRt.anchorMin = Vector2.zero;
            imgRt.anchorMax = Vector2.one;
            imgRt.offsetMin = Vector2.zero;
            imgRt.offsetMax = Vector2.zero;

            // Rewind text
            GameObject textObj = new GameObject("RewindText");
            textObj.transform.SetParent(canvasObj.transform, false);
            _rewindText = textObj.AddComponent<TextMeshProUGUI>();
            _rewindText.text = "時間回溯...";
            _rewindText.fontSize = 48f;
            _rewindText.alignment = TextAlignmentOptions.Center;
            _rewindText.color = new Color(1f, 1f, 1f, 0f);

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0.3f);
            textRt.anchorMax = new Vector2(1f, 0.7f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            canvasObj.SetActive(false);
        }
    }
}
