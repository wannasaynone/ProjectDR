// MvpCharacterInteractionView — MVP 版角色互動畫面。
// 與既有 CharacterInteractionView 分離，以避免破壞既有村莊/探索流程。
// 簡化版選單：[對話][派遣]，送禮/回憶/設施功能全部移除。
// Sprint 4 不實作派遣；派遣按鈕 UI 存在但點擊僅顯示佔位訊息。

using System;
using KahaGameCore.GameEvent;
using ProjectDR.Village.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDR.Village.Mvp.UI
{
    /// <summary>
    /// MVP 版角色互動畫面（骨架）。
    /// 顯示角色名稱、當前對話行、好感度、[對話][派遣] 按鈕與返回按鈕。
    /// 點「對話」依紅點狀態決定 direction（紅點=角色主動；無紅點=玩家主動）。
    /// </summary>
    public class MvpCharacterInteractionView : ViewBase
    {
        [Header("角色資訊")]
        [SerializeField] private TMP_Text _characterNameLabel;
        [SerializeField] private TMP_Text _affinityLabel;

        [Header("對話區")]
        [SerializeField] private TMP_Text _dialogueText;

        [Tooltip("點擊推進對話（覆蓋整個對話區）。")]
        [SerializeField] private Button _dialogueClickArea;

        [Header("功能選單")]
        [SerializeField] private Button _dialogueButton;
        [SerializeField] private Button _dispatchButton;
        [SerializeField] private TMP_Text _dispatchButtonLabel;

        [Header("返回")]
        [SerializeField] private Button _returnButton;

        [Header("派遣 Placeholder 文字")]
        [Tooltip("Sprint 4 不實作派遣，點選時顯示此訊息。")]
        [SerializeField] private TMP_Text _dispatchPlaceholderLabel;

        // 注入
        private MvpDialogueSession _dialogueSession;
        private DialogueManager _dialogueManager;
        private AffinityManager _affinityManager;
        private NPCInitiativeManager _initiativeManager;
        private DialogueCooldownManager _cooldownManager;
        private Action _onReturn;

        private string _currentCharacterId;
        private string _currentDisplayName;

        private bool _eventsSubscribed;

        /// <summary>由 MvpEntryPoint 注入相依。</summary>
        public void Initialize(
            MvpDialogueSession session,
            DialogueManager dialogueManager,
            AffinityManager affinityManager,
            NPCInitiativeManager initiativeManager,
            DialogueCooldownManager cooldownManager,
            Action onReturn)
        {
            _dialogueSession = session;
            _dialogueManager = dialogueManager;
            _affinityManager = affinityManager;
            _initiativeManager = initiativeManager;
            _cooldownManager = cooldownManager;
            _onReturn = onReturn;

            HookButtons();
        }

        /// <summary>設定目前顯示的角色（由 MvpEntryPoint 在 PushView 前呼叫）。</summary>
        public void SetCharacter(string characterId, string displayName)
        {
            _currentCharacterId = characterId;
            _currentDisplayName = displayName;
            RefreshAll();
        }

        protected override void OnShow()
        {
            if (!_eventsSubscribed)
            {
                EventBus.Subscribe<AffinityChangedEvent>(OnAffinityChanged);
                EventBus.Subscribe<MvpDialogueSessionStartedEvent>(OnSessionStarted);
                EventBus.Subscribe<MvpDialogueSessionCompletedEvent>(OnSessionCompleted);
                EventBus.Subscribe<DialogueStartedEvent>(OnDialogueLineStarted);
                _eventsSubscribed = true;
            }
            RefreshAll();
        }

        protected override void OnHide()
        {
            UnsubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void UnsubscribeEvents()
        {
            if (!_eventsSubscribed) return;
            EventBus.Unsubscribe<AffinityChangedEvent>(OnAffinityChanged);
            EventBus.Unsubscribe<MvpDialogueSessionStartedEvent>(OnSessionStarted);
            EventBus.Unsubscribe<MvpDialogueSessionCompletedEvent>(OnSessionCompleted);
            EventBus.Unsubscribe<DialogueStartedEvent>(OnDialogueLineStarted);
            _eventsSubscribed = false;
        }

        private void HookButtons()
        {
            if (_dialogueButton != null)
            {
                _dialogueButton.onClick.RemoveAllListeners();
                _dialogueButton.onClick.AddListener(OnDialogueClicked);
            }
            if (_dispatchButton != null)
            {
                _dispatchButton.onClick.RemoveAllListeners();
                _dispatchButton.onClick.AddListener(OnDispatchClicked);
            }
            if (_returnButton != null)
            {
                _returnButton.onClick.RemoveAllListeners();
                _returnButton.onClick.AddListener(OnReturnClicked);
            }
            if (_dialogueClickArea != null)
            {
                _dialogueClickArea.onClick.RemoveAllListeners();
                _dialogueClickArea.onClick.AddListener(OnDialogueAreaClicked);
            }
        }

        private void RefreshAll()
        {
            if (_characterNameLabel != null) _characterNameLabel.text = _currentDisplayName ?? string.Empty;

            if (_affinityLabel != null && _affinityManager != null && !string.IsNullOrEmpty(_currentCharacterId))
            {
                _affinityLabel.text = $"好感度：{_affinityManager.GetAffinity(_currentCharacterId)}";
            }

            if (_dialogueText != null)
            {
                _dialogueText.text = string.Empty;
            }

            if (_dispatchPlaceholderLabel != null)
            {
                _dispatchPlaceholderLabel.gameObject.SetActive(false);
            }

            // 按鈕互動性
            if (_dialogueButton != null)
            {
                _dialogueButton.interactable = CanStartDialogue();
            }
            if (_dispatchButton != null)
            {
                _dispatchButton.interactable = false; // Sprint 4 不實作派遣
                if (_dispatchButtonLabel != null) _dispatchButtonLabel.text = "派遣（未開放）";
            }
        }

        private bool CanStartDialogue()
        {
            if (_dialogueSession == null) return false;
            if (string.IsNullOrEmpty(_currentCharacterId)) return false;
            // 紅點忽略玩家冷卻
            bool ready = _initiativeManager != null && _initiativeManager.IsReady(_currentCharacterId);
            if (ready) return true;
            bool onCd = _cooldownManager != null && _cooldownManager.IsOnCooldown(_currentCharacterId);
            return !onCd;
        }

        private void OnDialogueClicked()
        {
            if (_dialogueSession == null || string.IsNullOrEmpty(_currentCharacterId)) return;
            _dialogueSession.TryStartPlayerInitiatedDialogue(_currentCharacterId);
        }

        private void OnDispatchClicked()
        {
            // Sprint 4 不實作派遣：顯示佔位訊息
            if (_dispatchPlaceholderLabel != null)
            {
                _dispatchPlaceholderLabel.gameObject.SetActive(true);
                _dispatchPlaceholderLabel.text = "派遣系統尚未開放。";
            }
        }

        private void OnDialogueAreaClicked()
        {
            if (_dialogueSession == null) return;
            if (!_dialogueSession.IsActive) return;
            // 對話中點擊推進
            bool hasNext = _dialogueSession.AdvanceDialogue();
            if (hasNext && _dialogueManager != null && _dialogueText != null)
            {
                _dialogueText.text = _dialogueManager.GetCurrentLine() ?? string.Empty;
            }
        }

        private void OnReturnClicked()
        {
            _onReturn?.Invoke();
        }

        private void OnAffinityChanged(AffinityChangedEvent e)
        {
            if (e.CharacterId != _currentCharacterId) return;
            if (_affinityLabel != null)
            {
                _affinityLabel.text = $"好感度：{e.NewValue}";
            }
        }

        private void OnSessionStarted(MvpDialogueSessionStartedEvent e)
        {
            if (e.CharacterId != _currentCharacterId) return;
            if (_dialogueText != null && _dialogueManager != null)
            {
                _dialogueText.text = _dialogueManager.GetCurrentLine() ?? string.Empty;
            }
            if (_dialogueButton != null) _dialogueButton.interactable = false;
        }

        private void OnSessionCompleted(MvpDialogueSessionCompletedEvent e)
        {
            if (e.CharacterId != _currentCharacterId) return;
            if (_dialogueText != null) _dialogueText.text = string.Empty;
            if (_dialogueButton != null) _dialogueButton.interactable = CanStartDialogue();
        }

        private void OnDialogueLineStarted(DialogueStartedEvent e)
        {
            if (_dialogueText != null && _dialogueManager != null)
            {
                _dialogueText.text = _dialogueManager.GetCurrentLine() ?? string.Empty;
            }
        }
    }
}
