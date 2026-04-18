// PlayerQuestionsView — 玩家主動發問 UI（Sprint 5 B11/B13/B14 重寫）。
//
// 行為規格（依 character-content-template.md v1.4 §3.3、character-interaction.md v2.3 §5.2）：
// - 開啟即依 PlayerQuestionsManager.GetPresentation 決定本次呈現
//     - ≥4 題  → 顯示 4 題
//     - 1~3 題 → 只顯示剩餘
//     - 0 題   → 只顯示 [閒聊]（IdleChatPresenter 觸發隨機問題+隨機回答）
// - 體力為 0 → 點開此 View 顯示「現在好累了」文字，不可操作
// - 選題目 → 扣體力（B13）→ 播打字機回答 → 啟動 CD（B14）
// - [閒聊] 項 → 呼叫 IdleChatPresenter.Trigger → 播放抽到的 prompt+answer（不扣體力）
// - 選擇後標記已看（由 PlayerQuestionsManager）

using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectDR.Village.UI
{
    public class PlayerQuestionsView : ViewBase
    {
        // ===== SerializeField =====

        [Header("標題")]
        [SerializeField] private TMP_Text _titleLabel;

        [Header("問題清單")]
        [SerializeField] private Transform _questionListContainer;
        [SerializeField] private Button _questionRowPrefab;

        [Header("回答區")]
        [SerializeField] private GameObject _answerPanel;
        [SerializeField] private TMP_Text _questionDisplayLabel;
        [SerializeField] private TMP_Text _answerText;
        [SerializeField] private Button _backToListButton;

        [Header("體力不足提示")]
        [SerializeField] private GameObject _tiredPanel;
        [SerializeField] private TMP_Text _tiredLabel;

        [Header("導航")]
        [SerializeField] private Button _closeButton;

        // ===== 注入相依 =====

        private PlayerQuestionsManager _questionsManager;
        private PlayerQuestionsConfig _questionsConfig;
        private IdleChatPresenter _idleChatPresenter;
        private CharacterStaminaManager _staminaManager;
        private DialogueCooldownManager _cooldownManager;
        private RedDotManager _redDotManager;
        private TypewriterEffect _typewriter;
        private string _characterId;
        private float _charsPerSecond;

        private System.Action _returnAction;
        private System.Action<string> _responseAction;
        private bool _isShowingAnswer;

        // ===== 公開 API =====

        /// <summary>Sprint 5 新版：完整依賴注入。</summary>
        public void Initialize(
            PlayerQuestionsManager questionsManager,
            PlayerQuestionsConfig questionsConfig,
            IdleChatPresenter idleChatPresenter,
            CharacterStaminaManager staminaManager,
            DialogueCooldownManager cooldownManager,
            RedDotManager redDotManager,
            string characterId,
            float charsPerSecond)
        {
            _questionsManager = questionsManager;
            _questionsConfig = questionsConfig;
            _idleChatPresenter = idleChatPresenter;
            _staminaManager = staminaManager;
            _cooldownManager = cooldownManager;
            _redDotManager = redDotManager;
            _characterId = characterId;
            _charsPerSecond = charsPerSecond > 0f ? charsPerSecond : 20f;
        }

        /// <summary>Sprint 4 向下相容建構子（保留舊簽名防止編譯破壞）。</summary>
        public void Initialize(
            PlayerQuestionsConfig questionsConfig,
            AffinityManager affinityManager,
            DialogueManager dialogueManager,
            RedDotManager redDotManager,
            string characterId,
            float charsPerSecond)
        {
            // 舊簽名：建立預設 Manager（測試友善）
            _questionsConfig = questionsConfig;
            _questionsManager = questionsConfig != null ? new PlayerQuestionsManager(questionsConfig) : null;
            _idleChatPresenter = null;
            _staminaManager = null;
            _cooldownManager = null;
            _redDotManager = redDotManager;
            _characterId = characterId;
            _charsPerSecond = charsPerSecond > 0f ? charsPerSecond : 20f;
        }

        public void SetReturnAction(System.Action action) { _returnAction = action; }

        /// <summary>
        /// 設定「選擇題目 / 閒聊」後的回應處理回呼。
        /// 注入時：選題 / 閒聊後關閉 overlay，由主角色互動畫面播放回應，避免 overlay 呈現類似 CG 的對話。
        /// 不注入時：保留舊行為（於 overlay 內以打字機播放 answer + 返回清單按鈕）。
        /// </summary>
        public void SetResponseAction(System.Action<string> action) { _responseAction = action; }

        // ===== 生命週期 =====

        protected override void OnShow()
        {
            if (_answerText != null)
            {
                _typewriter = _answerText.GetComponent<TypewriterEffect>();
                if (_typewriter == null)
                    _typewriter = _answerText.gameObject.AddComponent<TypewriterEffect>();
                _typewriter.Initialize(_answerText);
            }

            if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
            if (_backToListButton != null) _backToListButton.onClick.AddListener(OnBackToList);

            if (_titleLabel != null) _titleLabel.text = "發問";

            _isShowingAnswer = false;
            ShowListPanel();

            // B13：體力 = 0 → 顯示「現在好累了」
            if (_staminaManager != null && !_staminaManager.HasEnoughForDialogue(_characterId))
            {
                ShowTiredState();
                return;
            }

            HideTiredState();
            RefreshQuestionList();
        }

        protected override void OnHide()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
            if (_backToListButton != null) _backToListButton.onClick.RemoveListener(OnBackToList);
            ClearQuestionList();
        }

        // ===== 清單 =====

        private void RefreshQuestionList()
        {
            ClearQuestionList();
            if (_questionsManager == null || _questionListContainer == null) return;

            PlayerQuestionsPresentation pres = _questionsManager.GetPresentation(_characterId);

            if (pres.IsIdleChatFallback)
            {
                CreateIdleChatRow();
            }
            else
            {
                foreach (PlayerQuestionInfo q in pres.Questions)
                {
                    CreateQuestionRow(q);
                }
            }
        }

        private void CreateQuestionRow(PlayerQuestionInfo question)
        {
            if (_questionRowPrefab == null) return;
            Button row = Instantiate(_questionRowPrefab, _questionListContainer);
            row.gameObject.SetActive(true);
            TMP_Text label = row.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = question.QuestionText;
            PlayerQuestionInfo captured = question;
            row.onClick.AddListener(() => OnQuestionSelected(captured));
        }

        private void CreateIdleChatRow()
        {
            if (_questionRowPrefab == null) return;
            Button row = Instantiate(_questionRowPrefab, _questionListContainer);
            row.gameObject.SetActive(true);
            TMP_Text label = row.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = "[閒聊]";
            row.onClick.AddListener(OnIdleChatSelected);
        }

        private void ClearQuestionList()
        {
            if (_questionListContainer == null) return;
            for (int i = _questionListContainer.childCount - 1; i >= 0; i--)
                Destroy(_questionListContainer.GetChild(i).gameObject);
        }

        // ===== 選題（b 路徑 1 對 1）=====

        private void OnQuestionSelected(PlayerQuestionInfo question)
        {
            if (_isShowingAnswer) return;

            // B13：扣體力（失敗則顯示 tired panel）
            if (_staminaManager != null && !_staminaManager.TryConsumeForDialogue(_characterId))
            {
                ShowTiredState();
                return;
            }

            // 標記已看
            if (_questionsManager != null)
                _questionsManager.MarkSeen(_characterId, question.QuestionId);

            // B14：啟動 CD
            if (_cooldownManager != null)
                _cooldownManager.StartCooldown(_characterId);

            // 優先交由主角色互動畫面播放回應（不在此 overlay 內以 CG 式呈現）。
            // 未注入 responseAction 時維持舊行為（overlay 內 ShowAnswerPanel）。
            if (_responseAction != null)
            {
                _isShowingAnswer = true;
                _responseAction.Invoke(question.ResponseText ?? string.Empty);
            }
            else
            {
                _isShowingAnswer = true;
                ShowAnswerPanel(question.QuestionText, question.ResponseText);
            }
        }

        // ===== 閒聊 =====

        private void OnIdleChatSelected()
        {
            if (_isShowingAnswer) return;

            string prompt;
            string answer;

            // 閒聊不扣體力（GDD）— 但仍啟動 CD（GDD：CD 處理與一般發問一致）
            if (_idleChatPresenter == null)
            {
                prompt = "[閒聊]";
                answer = "（沒有可用的閒聊內容。）";
            }
            else
            {
                IdleChatResult r = _idleChatPresenter.Trigger(_characterId);
                if (r == null)
                {
                    prompt = "[閒聊]";
                    answer = "（這個角色沒有閒聊內容。）";
                }
                else
                {
                    prompt = r.Prompt;
                    answer = r.Answer;
                }
            }

            _isShowingAnswer = true;

            if (_cooldownManager != null)
                _cooldownManager.StartCooldown(_characterId);

            // 優先交由主角色互動畫面播放回應
            if (_responseAction != null)
            {
                _responseAction.Invoke(answer ?? string.Empty);
            }
            else
            {
                ShowAnswerPanel(prompt, answer);
            }
        }

        // ===== 顯示 =====

        private void ShowAnswerPanel(string questionText, string answerText)
        {
            if (_questionListContainer != null) _questionListContainer.gameObject.SetActive(false);
            if (_answerPanel != null) _answerPanel.SetActive(true);
            if (_questionDisplayLabel != null) _questionDisplayLabel.text = questionText ?? string.Empty;

            if (_typewriter != null && !string.IsNullOrEmpty(answerText))
                _typewriter.Play(answerText, _charsPerSecond);
            else if (_answerText != null)
                _answerText.text = answerText ?? string.Empty;
        }

        private void OnBackToList()
        {
            _isShowingAnswer = false;
            ShowListPanel();
            // 回清單時，CD 已啟動，只能靠重新 Open 才能再次取用
            // （若剩餘題目為 0 會自動顯示 [閒聊]；若還有題目，會顯示新抽的 4 題）
            RefreshQuestionList();
        }

        private void ShowListPanel()
        {
            if (_answerPanel != null) _answerPanel.SetActive(false);
            if (_questionListContainer != null) _questionListContainer.gameObject.SetActive(true);
        }

        private void ShowTiredState()
        {
            ClearQuestionList();
            if (_questionListContainer != null) _questionListContainer.gameObject.SetActive(false);
            if (_answerPanel != null) _answerPanel.SetActive(false);
            if (_tiredPanel != null) _tiredPanel.SetActive(true);
            if (_tiredLabel != null) _tiredLabel.text = "現在好累了";
        }

        private void HideTiredState()
        {
            if (_tiredPanel != null) _tiredPanel.SetActive(false);
        }

        // ===== 導航 =====

        private void OnCloseClicked()
        {
            _returnAction?.Invoke();
        }
    }
}
