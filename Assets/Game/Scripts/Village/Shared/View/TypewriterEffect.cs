using System.Collections;
using UnityEngine;
using TMPro;

namespace ProjectDR.Village.Shared
{
    /// <summary>
    /// 打字機效果元件。
    /// 掛在持有 TMP_Text 的 GameObject 上，逐字顯示文字。
    /// 點擊/呼叫 Skip() 可跳過動畫直接顯示完整文字。
    /// </summary>
    public class TypewriterEffect : MonoBehaviour
    {
        private TMP_Text _textComponent;
        private Coroutine _currentCoroutine;
        private string _fullText;

        /// <summary>打字機是否正在播放中。</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>打字完成時觸發的回呼。</summary>
        public event System.Action OnComplete;

        /// <summary>
        /// 初始化打字機效果元件。
        /// </summary>
        /// <param name="textComponent">要控制的 TMP_Text 元件。</param>
        public void Initialize(TMP_Text textComponent)
        {
            _textComponent = textComponent;
        }

        /// <summary>
        /// 開始打字機效果播放。
        /// </summary>
        /// <param name="text">要顯示的完整文字。</param>
        /// <param name="charsPerSecond">每秒顯示的字元數。</param>
        public void Play(string text, float charsPerSecond)
        {
            if (_textComponent == null) return;

            // 停止先前的播放
            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
            }

            _fullText = text;
            IsPlaying = true;
            _currentCoroutine = StartCoroutine(TypewriteCoroutine(text, charsPerSecond));
        }

        /// <summary>
        /// 跳過打字機效果，直接顯示完整文字。
        /// </summary>
        public void Skip()
        {
            if (!IsPlaying) return;

            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
                _currentCoroutine = null;
            }

            if (_textComponent != null)
            {
                _textComponent.text = _fullText;
            }

            IsPlaying = false;
            OnComplete?.Invoke();
        }

        private IEnumerator TypewriteCoroutine(string text, float charsPerSecond)
        {
            _textComponent.text = "";

            if (charsPerSecond <= 0f)
            {
                _textComponent.text = text;
                IsPlaying = false;
                OnComplete?.Invoke();
                yield break;
            }

            float interval = 1f / charsPerSecond;

            for (int i = 0; i < text.Length; i++)
            {
                _textComponent.text = text.Substring(0, i + 1);
                yield return new WaitForSeconds(interval);
            }

            IsPlaying = false;
            _currentCoroutine = null;
            OnComplete?.Invoke();
        }

        private void OnDisable()
        {
            // 停止所有 Coroutine 以避免元件被停用後仍在播放
            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
                _currentCoroutine = null;
                IsPlaying = false;
            }
        }
    }
}
