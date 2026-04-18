// PlayerQuestionsView — 玩家主動發問 UI（B14）。
// overlay 模式，顯示依好感度分批解鎖的問題清單。
//
// 佈局驗證（3840x2160，中心 0,0，overlay 右側 w=1600）：
//   PnlRoot      cx=800   cy=0      w=1600  h=2160   L=0    R=+1600
//   TxtTitle     cx=800   cy=+928   w=880   h=72     L=360  R=1240  T=+964 B=+892
//   ScrollRect   cx=800   cy=+8     w=1520  h=1600   L=40   R=1560  T=+808 B=-792
//   BtnClose     cx=800   cy=-944   w=480   h=80     L=560  R=1040  T=-904 B=-984
//
//   TxtTitle.B=+892 vs ScrollRect.T=+808 → 間距 84px ✓
//   ScrollRect.B=-792 vs BtnClose.T=-904 → 間距 112px ✓
//   BtnClose.B=-984 ≥ -1080（全覆蓋 overlay 可接受）✓

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 玩家主動發問 overlay View（B14）。
    /// 顯示依好感度分批解鎖的問題清單，
    /// 點擊題目 → 打字機播放角色回答 → 返回清單。
    /// </summary>
    public class PlayerQuestionsView : ViewBase
    {
        // ── SerializeField ──────────────────────────────────────────────

        [Header("標題")]
        [SerializeField] private TMP_Text _titleLabel;

        [Header("問題清單")]
        [SerializeField] private Transform _questionListContainer;
        [SerializeField] private Button _questionRowPrefab;

        [Header("回答區（問題被點擊後顯示）")]
        [SerializeField] private GameObject _answerPanel;
        [SerializeField] private TMP_Text _questionDisplayLabel;
        [SerializeField] private TMP_Text _answerText;
        [SerializeField] private Button _backToListButton;

        [Header("導航")]
        [SerializeField] private Button _closeButton;

        // ── 注入相依 ────────────────────────────────────────────────────

        private PlayerQuestionsConfig _questionsConfig;
        private AffinityManager _affinityManager;
        private DialogueManager _dialogueManager;
        private RedDotManager _redDotManager; // C2：清除 L2 紅點
        private TypewriterEffect _typewriter;
        private string _characterId;
        private float _charsPerSecond;

        private System.Action _returnAction;

        // ── 執行時狀態 ──────────────────────────────────────────────────

        private bool _isShowingAnswer;

        // ── 公開 API ────────────────────────────────────────────────────

        /// <summary>
        /// 由 VillageEntryPoint 注入相依。
        /// </summary>
        public void Initialize(
            PlayerQuestionsConfig questionsConfig,
            AffinityManager affinityManager,
            DialogueManager dialogueManager,
            string characterId,
            float charsPerSecond)
        {
            Initialize(questionsConfig, affinityManager, dialogueManager, null, characterId, charsPerSecond);
        }

        /// <summary>
        /// C2（Sprint 4）擴充：支援 RedDotManager 注入，打開發問清單即清除 L2 紅點。
        /// </summary>
        public void Initialize(
            PlayerQuestionsConfig questionsConfig,
            AffinityManager affinityManager,
            DialogueManager dialogueManager,
            RedDotManager redDotManager,
            string characterId,
            float charsPerSecond)
        {
            _questionsConfig = questionsConfig;
            _affinityManager = affinityManager;
            _dialogueManager = dialogueManager;
            _redDotManager   = redDotManager;
            _characterId     = characterId;
            _charsPerSecond  = charsPerSecond > 0f ? charsPerSecond : 20f;
        }

        /// <summary>設定關閉按鈕的回呼（overlay 模式）。</summary>
        public void SetReturnAction(System.Action action)
        {
            _returnAction = action;
        }

        // ── ViewBase 生命週期 ────────────────────────────────────────────

        protected override void OnShow()
        {
            // 建立打字機（用於回答區）
            if (_answerText != null)
            {
                _typewriter = _answerText.GetComponent<TypewriterEffect>();
                if (_typewriter == null)
                    _typewriter = _answerText.gameObject.AddComponent<TypewriterEffect>();
                _typewriter.Initialize(_answerText);
            }

            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);

            if (_backToListButton != null)
                _backToListButton.onClick.AddListener(OnBackToList);

            // 設定標題
            if (_titleLabel != null)
                _titleLabel.text = "發問";

            // 初始狀態：顯示清單，隱藏回答面板
            ShowListPanel();
            RefreshQuestionList();

            // C2（Sprint 4）：打開發問清單 → 清除該角色 L2 角色發問紅點
            if (_redDotManager != null && !string.IsNullOrEmpty(_characterId))
            {
                _redDotManager.SetCharacterQuestionFlag(_characterId, false);
            }
        }

        protected override void OnHide()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);

            if (_backToListButton != null)
                _backToListButton.onClick.RemoveListener(OnBackToList);

            ClearQuestionList();
        }

        // ── 清單邏輯 ────────────────────────────────────────────────────

        private void RefreshQuestionList()
        {
            ClearQuestionList();

            if (_questionsConfig == null || _questionListContainer == null) return;

            // 計算當前好感度階段
            int affinityValue = _affinityManager != null ? _affinityManager.GetAffinity(_characterId) : 0;
            int currentStage  = CalculateAffinityStage(affinityValue);

            IReadOnlyList<PlayerQuestionInfo> questions =
                _questionsConfig.GetQuestionsForCharacter(_characterId);

            foreach (PlayerQuestionInfo q in questions)
            {
                CreateQuestionRow(q, currentStage);
            }
        }

        private void CreateQuestionRow(PlayerQuestionInfo question, int currentStage)
        {
            if (_questionRowPrefab == null) return;

            Button row = Instantiate(_questionRowPrefab, _questionListContainer);
            row.gameObject.SetActive(true);

            TMP_Text label = row.GetComponentInChildren<TMP_Text>();
            bool unlocked = question.UnlockAffinityStage <= currentStage;

            if (label != null)
            {
                label.text = unlocked
                    ? question.QuestionText
                    : $"[好感度階段 {question.UnlockAffinityStage} 解鎖] {question.QuestionText}";
                label.color = unlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
            }

            row.interactable = unlocked;
            if (unlocked)
            {
                PlayerQuestionInfo captured = question;
                row.onClick.AddListener(() => OnQuestionSelected(captured));
            }
        }

        private void ClearQuestionList()
        {
            if (_questionListContainer == null) return;
            for (int i = _questionListContainer.childCount - 1; i >= 0; i--)
                Destroy(_questionListContainer.GetChild(i).gameObject);
        }

        // ── 回答邏輯 ────────────────────────────────────────────────────

        private void OnQuestionSelected(PlayerQuestionInfo question)
        {
            if (_isShowingAnswer) return;

            _isShowingAnswer = true;
            ShowAnswerPanel(question);
        }

        private void ShowAnswerPanel(PlayerQuestionInfo question)
        {
            // 隱藏清單，顯示回答區
            if (_questionListContainer != null)
                _questionListContainer.gameObject.SetActive(false);

            if (_answerPanel != null)
                _answerPanel.SetActive(true);

            if (_questionDisplayLabel != null)
                _questionDisplayLabel.text = question.QuestionText;

            // 打字機播放回答
            if (_typewriter != null && !string.IsNullOrEmpty(question.ResponseText))
            {
                _typewriter.Play(question.ResponseText, _charsPerSecond);
            }
            else if (_answerText != null)
            {
                _answerText.text = question.ResponseText;
            }
        }

        private void OnBackToList()
        {
            _isShowingAnswer = false;
            ShowListPanel();
        }

        private void ShowListPanel()
        {
            if (_answerPanel != null)
                _answerPanel.SetActive(false);

            if (_questionListContainer != null)
                _questionListContainer.gameObject.SetActive(true);
        }

        // ── 導航 ────────────────────────────────────────────────────────

        private void OnCloseClicked()
        {
            _returnAction?.Invoke();
        }

        // ── 工具 ────────────────────────────────────────────────────────

        /// <summary>
        /// 依好感度數值計算當前所屬的好感度門檻索引（階段）。
        /// IT 階段簡化：每 5 點好感度算一個階段（stage 0 = 0-4, stage 1 = 5-9, …）。
        /// </summary>
        private static int CalculateAffinityStage(int affinityValue)
        {
            if (affinityValue < 0) return 0;
            return affinityValue / 5;
        }
    }
}
