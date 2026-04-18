// CharacterIntroCGView — 登場 CG + 短劇情播放 View（B13）。
// 全螢幕 overlay：上半 CG 圖、下半對話框，點擊任意位置推進對話。
//
// 佈局驗證（3840x2160，中心 0,0）：
//   PnlBackground   cx=0    cy=0      w=3840  h=2160  [全螢幕黑底]
//   ImgCG           cx=0    cy=+432   w=3840  h=1296  T=+1080 B=-216
//   PnlDialogue     cx=0    cy=-756   w=3840  h=648   T=-432  B=-1080
//     TxtSpeaker    cx=-1680 cy=-448  w=320   h=48    L=-1840 R=-1520
//     TxtDialogue   cx=0    cy=-540   w=3280  h=384   L=-1640 R=+1640 ✓
//     TxtTapHint    cx=0    cy=-864   w=400   h=40    B=-884 ✓
//   ImgCG.B=-216 vs PnlDialogue.T=-432 → 間距 216px ✓

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 登場 CG + 短劇情播放畫面（全螢幕 overlay）。
    /// 上半顯示 CG 圖片，下半顯示對話框（打字機效果），點擊任意位置推進對話。
    /// </summary>
    public class CharacterIntroCGView : ViewBase
    {
        [Header("CG 圖片區")]
        [SerializeField] private Image _cgImage;

        [Header("對話框")]
        [SerializeField] private TMP_Text _speakerLabel;
        [SerializeField] private TMP_Text _dialogueText;
        [SerializeField] private TMP_Text _tapHintLabel;

        [Header("點擊區域（全螢幕）")]
        [SerializeField] private Button _fullScreenClickArea;

        // 播放狀態
        private IReadOnlyList<CharacterIntroLineData> _lines;
        private int _lineIndex;
        private TypewriterEffect _typewriter;
        private float _charsPerSecond;
        private Action _onComplete;
        private bool _isActive;

        /// <summary>
        /// 由 CharacterIntroCGPlayer 在 Show 前呼叫，設定播放內容。
        /// </summary>
        /// <param name="cgSprite">CG Sprite（null 時顯示紫色 placeholder 色塊）。</param>
        /// <param name="lines">依 sequence 排序的對話行清單。</param>
        /// <param name="charsPerSecond">打字機速度。</param>
        /// <param name="onComplete">播放完成後的回呼。</param>
        public void SetContent(
            Sprite cgSprite,
            IReadOnlyList<CharacterIntroLineData> lines,
            float charsPerSecond,
            Action onComplete)
        {
            _lines = lines;
            _charsPerSecond = charsPerSecond > 0f ? charsPerSecond : 20f;
            _onComplete = onComplete;
            _lineIndex = 0;
            _isActive = false;

            if (_cgImage != null)
            {
                if (cgSprite != null)
                {
                    _cgImage.sprite = cgSprite;
                    _cgImage.color = Color.white;
                }
                else
                {
                    // IT 階段 placeholder：深紫色色塊
                    _cgImage.sprite = null;
                    _cgImage.color = new Color(0.2f, 0.1f, 0.3f, 1f);
                }
            }
        }

        protected override void OnShow()
        {
            // 建立打字機
            if (_dialogueText != null)
            {
                _typewriter = _dialogueText.GetComponent<TypewriterEffect>();
                if (_typewriter == null)
                    _typewriter = _dialogueText.gameObject.AddComponent<TypewriterEffect>();
                _typewriter.Initialize(_dialogueText);
                _typewriter.OnComplete += OnTypewriterDone;
            }

            if (_fullScreenClickArea != null)
                _fullScreenClickArea.onClick.AddListener(OnClicked);

            if (_tapHintLabel != null)
            {
                _tapHintLabel.text = "點擊繼續";
                _tapHintLabel.gameObject.SetActive(false);
            }

            _isActive = true;
            PlayCurrentLine();
        }

        protected override void OnHide()
        {
            if (_typewriter != null)
                _typewriter.OnComplete -= OnTypewriterDone;

            if (_fullScreenClickArea != null)
                _fullScreenClickArea.onClick.RemoveListener(OnClicked);
        }

        private void PlayCurrentLine()
        {
            int count = _lines != null ? _lines.Count : 0;
            if (_lineIndex >= count)
            {
                InvokeComplete();
                return;
            }

            CharacterIntroLineData line = _lines[_lineIndex];
            bool narration = line.line_type == CharacterIntroLineTypes.Narration
                          || line.speaker == "narrator";

            if (_speakerLabel != null)
            {
                _speakerLabel.gameObject.SetActive(!narration);
                _speakerLabel.text = narration ? string.Empty : ToDisplayName(line.speaker);
            }

            if (_tapHintLabel != null)
                _tapHintLabel.gameObject.SetActive(false);

            string text = line.text ?? string.Empty;
            if (_typewriter != null)
                _typewriter.Play(text, _charsPerSecond);
            else if (_dialogueText != null)
            {
                _dialogueText.text = text;
                OnTypewriterDone();
            }
        }

        private void OnTypewriterDone()
        {
            if (_tapHintLabel != null)
                _tapHintLabel.gameObject.SetActive(true);
        }

        private void OnClicked()
        {
            if (!_isActive) return;

            if (_typewriter != null && _typewriter.IsPlaying)
            {
                _typewriter.Skip();
                return;
            }

            _lineIndex++;
            int count = _lines != null ? _lines.Count : 0;
            if (_lineIndex >= count)
                InvokeComplete();
            else
                PlayCurrentLine();
        }

        // CTRL 快轉：按住 LeftCtrl / RightCtrl 時連續跳過打字機並自動推進到下一行
        private const float FastForwardIntervalSeconds = 0.05f;
        private float _fastForwardAccumulator;

        private void Update()
        {
            if (!_isActive) return;

            bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!ctrlHeld)
            {
                _fastForwardAccumulator = 0f;
                return;
            }

            if (_typewriter != null && _typewriter.IsPlaying)
            {
                _typewriter.Skip();
                _fastForwardAccumulator = 0f;
                return;
            }

            _fastForwardAccumulator += Time.unscaledDeltaTime;
            if (_fastForwardAccumulator < FastForwardIntervalSeconds) return;
            _fastForwardAccumulator = 0f;
            OnClicked();
        }

        private void InvokeComplete()
        {
            _isActive = false;
            Action cb = _onComplete;
            _onComplete = null;
            cb?.Invoke();
        }

        private static string ToDisplayName(string speakerId)
        {
            if (speakerId == CharacterIds.VillageChiefWife) return "艾薇";
            if (speakerId == CharacterIds.Guard)            return "卡塞拉";
            if (speakerId == CharacterIds.Witch)            return "席薇雅";
            if (speakerId == CharacterIds.FarmGirl)         return "蘿";
            return speakerId ?? string.Empty;
        }
    }
}
