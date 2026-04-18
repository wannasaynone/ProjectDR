// CharacterQuestionsView — 角色發問 UI View（Sprint 5 B6）。
//
// 行為（依 character-interaction.md v2.3 §5.1、character-content-template.md v1.4 §3.2）：
// - overlay 開啟時自動抽題（由注入的 CharacterQuestionsManager）
// - 打字機播放 Prompt → 完成後顯示 4 個選項（UI 只顯示文字，不顯示 +N 數值）
// - 玩家點選項 → 呼叫 Manager.SubmitAnswer → 透過 responseAction 將 response 交回
//   主角色互動畫面播放（避免在 overlay 內以類似 CG 的形式顯示）
//
// 此 View 僅為 overlay（在 CharacterInteractionView 的 overlayContainer 內出現），
// 不涉及角色選單切換邏輯。由 CharacterInteractionView.OnFunctionClicked 分流決定開啟。

using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectDR.Village.UI
{
    public class CharacterQuestionsView : ViewBase
    {
        [Header("Prompt / Response")]
        [SerializeField] private TMP_Text _dialogueText;

        [Header("選項容器")]
        [SerializeField] private Transform _optionsContainer;
        [SerializeField] private Button _optionButtonPrefab;

        [Header("關閉 / 返回")]
        [SerializeField] private Button _closeButton;

        private CharacterQuestionsManager _manager;
        private CharacterQuestionsConfig _config;
        private AffinityManager _affinityManager;
        private CharacterQuestionCountdownManager _countdownManager;
        private RedDotManager _redDotManager;
        private string _characterId;
        private int _level;
        private float _charsPerSecond;

        private TypewriterEffect _typewriter;
        private CharacterQuestionInfo _currentQuestion;
        private System.Action _returnAction;
        private System.Action<string> _responseAction;

        public void Initialize(
            CharacterQuestionsManager manager,
            CharacterQuestionsConfig config,
            AffinityManager affinityManager,
            CharacterQuestionCountdownManager countdownManager,
            RedDotManager redDotManager,
            string characterId,
            int level,
            float charsPerSecond)
        {
            _manager = manager;
            _config = config;
            _affinityManager = affinityManager;
            _countdownManager = countdownManager;
            _redDotManager = redDotManager;
            _characterId = characterId;
            _level = level > 0 ? level : 1;
            _charsPerSecond = charsPerSecond > 0f ? charsPerSecond : 20f;
        }

        public void SetReturnAction(System.Action action) { _returnAction = action; }

        /// <summary>
        /// 設定選擇答案後的回應處理回呼。
        /// 回呼會收到被選項目對應的 response 文字；實作端應由主角色互動畫面播放該對話
        /// 並關閉本 overlay（避免在 overlay 內以類似 CG 的形式顯示）。
        /// </summary>
        public void SetResponseAction(System.Action<string> action) { _responseAction = action; }

        protected override void OnShow()
        {
            // 打字機
            if (_dialogueText != null)
            {
                _typewriter = _dialogueText.GetComponent<TypewriterEffect>();
                if (_typewriter == null) _typewriter = _dialogueText.gameObject.AddComponent<TypewriterEffect>();
                _typewriter.Initialize(_dialogueText);
            }

            if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);

            // 進入 = 觸發角色發問：清 L2 + ClearReady
            if (_redDotManager != null && !string.IsNullOrEmpty(_characterId))
                _redDotManager.SetCharacterQuestionFlag(_characterId, false);
            if (_countdownManager != null && !string.IsNullOrEmpty(_characterId))
                _countdownManager.ClearReady(_characterId);

            // 抽題
            if (_manager != null)
            {
                _currentQuestion = _manager.PickNextQuestion(_characterId, _level);
            }

            if (_currentQuestion == null)
            {
                // 題目池耗盡 fallback：直接關閉（GDD 規格此情境罕見，因為 10 題/級很多）
                if (_dialogueText != null) _dialogueText.text = "（目前沒有更多問題。）";
                ClearOptions();
                return;
            }

            // 播放 Prompt
            ClearOptions();
            if (_typewriter != null)
            {
                _typewriter.OnComplete -= OnPromptTypewriterComplete;
                _typewriter.OnComplete += OnPromptTypewriterComplete;
                _typewriter.Play(_currentQuestion.Prompt ?? string.Empty, _charsPerSecond);
            }
            else if (_dialogueText != null)
            {
                _dialogueText.text = _currentQuestion.Prompt ?? string.Empty;
                ShowOptions();
            }

            // 重新啟動倒數（本次角色發問觸發結束後重新計時）
            if (_countdownManager != null && !string.IsNullOrEmpty(_characterId))
                _countdownManager.StartCountdown(_characterId);
        }

        protected override void OnHide()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
            if (_typewriter != null) _typewriter.OnComplete -= OnPromptTypewriterComplete;
            ClearOptions();
        }

        // ===== Prompt 打字機完成 → 顯示選項 =====

        private void OnPromptTypewriterComplete()
        {
            if (_typewriter != null) _typewriter.OnComplete -= OnPromptTypewriterComplete;
            ShowOptions();
        }

        private void ShowOptions()
        {
            if (_currentQuestion == null || _optionsContainer == null || _optionButtonPrefab == null) return;

            ClearOptions();

            foreach (CharacterQuestionOption opt in _currentQuestion.Options)
            {
                string capturedPersonality = opt.Personality;
                string capturedResponse = opt.Response ?? string.Empty;

                Button btn = Instantiate(_optionButtonPrefab, _optionsContainer);
                btn.gameObject.SetActive(true);

                TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
                // 規則層：UI 只顯示選項文字，不顯示 +N 數值
                if (label != null) label.text = opt.Text ?? string.Empty;

                btn.onClick.AddListener(() => OnOptionClicked(capturedPersonality, capturedResponse));
            }
        }

        private void ClearOptions()
        {
            if (_optionsContainer == null) return;
            for (int i = _optionsContainer.childCount - 1; i >= 0; i--)
                Destroy(_optionsContainer.GetChild(i).gameObject);
        }

        private void OnOptionClicked(string personalityId, string response)
        {
            if (_currentQuestion == null || _manager == null) return;

            _manager.SubmitAnswer(_characterId, _currentQuestion.QuestionId, personalityId);
            ClearOptions();

            // 選擇後將 response 交由主角色互動畫面播放（依規格：返回角色互動頁面顯示對話，
            // 不在此 overlay 中以類似 CG 的形式顯示）。未設定 responseAction 時退回僅關閉 overlay。
            if (_responseAction != null)
            {
                _responseAction.Invoke(response ?? string.Empty);
            }
            else
            {
                _returnAction?.Invoke();
            }
        }

        private void OnCloseClicked()
        {
            _returnAction?.Invoke();
        }
    }
}
