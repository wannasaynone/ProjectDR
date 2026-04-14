using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 角色互動畫面。
    /// 左側為立繪區（Animator 預留），右上為對話區（打字機效果），
    /// 右下為功能選單（對話結束後顯示）。
    /// 支援在 overlay container 中顯示功能 View（懸浮覆蓋）。
    /// </summary>
    public class CharacterInteractionView : ViewBase
    {
        [Header("立繪區")]
        [SerializeField] private Animator _portraitAnimator;

        [Header("對話區")]
        [SerializeField] private TMP_Text _dialogueText;
        [SerializeField] private TMP_Text _characterNameLabel;
        [SerializeField] private Button _dialogueClickArea;

        [Header("功能選單")]
        [SerializeField] private Transform _menuContainer;
        [SerializeField] private Button _menuButtonPrefab;

        [Header("Overlay")]
        [SerializeField] private Transform _overlayContainer;

        [Header("導航")]
        [SerializeField] private Button _returnButton;

        private DialogueManager _dialogueManager;
        private VillageNavigationManager _navigationManager;
        private TypewriterEffect _typewriterEffect;
        private CharacterMenuData _characterData;

        // overlay 相關
        private GameObject _currentOverlayInstance;

        // 功能 View Prefab 對照表（由 VillageEntryPoint 設定）
        private readonly Dictionary<string, ViewBase> _functionPrefabs
            = new Dictionary<string, ViewBase>();

        // 功能 View 初始化回呼對照表（由 VillageEntryPoint 設定）
        private readonly Dictionary<string, System.Action<ViewBase>> _functionInitializers
            = new Dictionary<string, System.Action<ViewBase>>();

        // 打字機速度（由外部配置傳入）
        private float _typewriterCharsPerSecond;

        /// <summary>
        /// 由 VillageEntryPoint 注入相依。
        /// </summary>
        /// <param name="dialogueManager">對話管理器。</param>
        /// <param name="navigationManager">導航管理器。</param>
        /// <param name="typewriterCharsPerSecond">打字機每秒顯示字元數。</param>
        public void Initialize(
            DialogueManager dialogueManager,
            VillageNavigationManager navigationManager,
            float typewriterCharsPerSecond)
        {
            _dialogueManager = dialogueManager;
            _navigationManager = navigationManager;
            _typewriterCharsPerSecond = typewriterCharsPerSecond;

            // 建立 TypewriterEffect 元件
            if (_dialogueText != null)
            {
                _typewriterEffect = _dialogueText.gameObject.GetComponent<TypewriterEffect>();
                if (_typewriterEffect == null)
                {
                    _typewriterEffect = _dialogueText.gameObject.AddComponent<TypewriterEffect>();
                }
                _typewriterEffect.Initialize(_dialogueText);
            }
        }

        /// <summary>
        /// 註冊功能 View 的 Prefab 與初始化回呼。
        /// 由 VillageEntryPoint 呼叫。
        /// </summary>
        /// <param name="functionId">功能 ID（如 AreaIds.Storage）。</param>
        /// <param name="prefab">功能 View 的 Prefab。</param>
        /// <param name="initializer">功能 View 初始化回呼（接收 instantiate 後的實例）。</param>
        public void RegisterFunctionPrefab(string functionId, ViewBase prefab, System.Action<ViewBase> initializer)
        {
            _functionPrefabs[functionId] = prefab;
            _functionInitializers[functionId] = initializer;
        }

        /// <summary>
        /// 設定當前角色資料。實際的對話播放延遲到 OnShow 時執行。
        /// </summary>
        public void SetCharacter(CharacterMenuData characterData)
        {
            _characterData = characterData;
            _pendingDialogueStart = true;
        }

        private bool _pendingDialogueStart;

        protected override void OnShow()
        {
            if (_dialogueClickArea != null)
            {
                _dialogueClickArea.onClick.AddListener(OnDialogueClicked);
            }

            if (_returnButton != null)
            {
                _returnButton.onClick.AddListener(OnReturnClicked);
            }

            // 在 GameObject active 後才啟動對話與打字機
            if (_pendingDialogueStart && _characterData != null)
            {
                _pendingDialogueStart = false;
                StartDialoguePlayback();
            }
        }

        /// <summary>
        /// 啟動對話播放（設定 UI、開始打字機）。
        /// 必須在 GameObject active 時呼叫。
        /// </summary>
        private void StartDialoguePlayback()
        {
            // 設定角色名稱
            if (_characterNameLabel != null)
            {
                _characterNameLabel.text = _characterData.DisplayName;
            }

            // 隱藏功能選單
            SetMenuVisible(false);

            // 關閉任何 overlay
            CloseOverlay();

            // 確保不重複訂閱（重新對話時仍在 Show 狀態，OnHide 不會觸發取消訂閱）
            EventBus.Unsubscribe<DialogueCompletedEvent>(OnDialogueCompleted);
            EventBus.Subscribe<DialogueCompletedEvent>(OnDialogueCompleted);

            // 開始對話
            _dialogueManager.StartDialogue(_characterData.Dialogue);

            // 播放第一行打字機效果
            string firstLine = _dialogueManager.GetCurrentLine();
            if (firstLine != null && _typewriterEffect != null)
            {
                _typewriterEffect.OnComplete += OnTypewriterLineComplete;
                _typewriterEffect.Play(firstLine, _typewriterCharsPerSecond);
            }
        }

        protected override void OnHide()
        {
            if (_dialogueClickArea != null)
            {
                _dialogueClickArea.onClick.RemoveListener(OnDialogueClicked);
            }

            if (_returnButton != null)
            {
                _returnButton.onClick.RemoveListener(OnReturnClicked);
            }

            // 清理事件訂閱
            EventBus.Unsubscribe<DialogueCompletedEvent>(OnDialogueCompleted);

            if (_typewriterEffect != null)
            {
                _typewriterEffect.OnComplete -= OnTypewriterLineComplete;
            }

            // 關閉 overlay
            CloseOverlay();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<DialogueCompletedEvent>(OnDialogueCompleted);
        }

        /// <summary>
        /// 對話區域被點擊時的處理。
        /// 若打字機正在播放 → 跳過（直接顯示完整文字）。
        /// 若打字機已完成 → 前進到下一行。
        /// </summary>
        private void OnDialogueClicked()
        {
            if (_typewriterEffect != null && _typewriterEffect.IsPlaying)
            {
                _typewriterEffect.Skip();
                return;
            }

            // 打字機已完成，嘗試前進
            AdvanceDialogue();
        }

        private void OnTypewriterLineComplete()
        {
            // 打字機完成一行的播放，等待玩家點擊
        }

        private void AdvanceDialogue()
        {
            if (_dialogueManager == null) return;

            bool hasNext = _dialogueManager.Advance();
            if (hasNext)
            {
                // 播放下一行
                string nextLine = _dialogueManager.GetCurrentLine();
                if (nextLine != null && _typewriterEffect != null)
                {
                    _typewriterEffect.Play(nextLine, _typewriterCharsPerSecond);
                }
            }
            // 若 hasNext 為 false，DialogueManager 會發布 DialogueCompletedEvent
        }

        private void OnDialogueCompleted(DialogueCompletedEvent e)
        {
            // 清理打字機事件
            if (_typewriterEffect != null)
            {
                _typewriterEffect.OnComplete -= OnTypewriterLineComplete;
            }

            // 顯示功能選單
            SetMenuVisible(true);
            RefreshMenu();
        }

        private void SetMenuVisible(bool visible)
        {
            if (_menuContainer != null)
            {
                _menuContainer.gameObject.SetActive(visible);
            }
        }

        private void RefreshMenu()
        {
            if (_menuContainer == null || _characterData == null) return;

            // 清除現有按鈕
            for (int i = _menuContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_menuContainer.GetChild(i).gameObject);
            }

            foreach (string functionId in _characterData.FunctionIds)
            {
                string capturedId = functionId;
                Button button = Instantiate(_menuButtonPrefab, _menuContainer);
                button.gameObject.SetActive(true);

                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = GetFunctionDisplayName(capturedId);
                }

                button.onClick.AddListener(() => OnFunctionClicked(capturedId));
            }
        }

        private void OnFunctionClicked(string functionId)
        {
            if (functionId == FunctionIds.Dialogue)
            {
                // 「對話」功能：重新播放對話
                OnDialogueOptionClicked();
                return;
            }

            // 其他功能：開啟 overlay
            OpenOverlay(functionId);
        }

        private void OnDialogueOptionClicked()
        {
            // 重新播放對話（此時 GameObject 必定 active）
            if (_characterData != null)
            {
                StartDialoguePlayback();
            }
        }

        private void OpenOverlay(string functionId)
        {
            // 關閉現有 overlay
            CloseOverlay();

            if (!_functionPrefabs.TryGetValue(functionId, out ViewBase prefab))
            {
                Debug.LogWarning($"[CharacterInteractionView] 未註冊的功能 Prefab: {functionId}");
                return;
            }

            if (_overlayContainer == null) return;

            // Instantiate 功能 View 到 overlay container
            ViewBase overlayView = Instantiate(prefab, _overlayContainer);
            _currentOverlayInstance = overlayView.gameObject;

            // 初始化功能 View
            if (_functionInitializers.TryGetValue(functionId, out System.Action<ViewBase> initializer))
            {
                initializer(overlayView);
            }

            overlayView.Show();
        }

        /// <summary>
        /// 關閉目前的 overlay。
        /// 由 overlay 內的 return 按鈕透過回呼觸發。
        /// </summary>
        public void CloseOverlay()
        {
            if (_currentOverlayInstance != null)
            {
                Destroy(_currentOverlayInstance);
                _currentOverlayInstance = null;
            }
        }

        private void OnReturnClicked()
        {
            // 先關閉 overlay
            CloseOverlay();

            _navigationManager.ReturnToHub();
        }

        private string GetFunctionDisplayName(string functionId)
        {
            switch (functionId)
            {
                case AreaIds.Storage: return "倉庫";
                case AreaIds.Exploration: return "探索";
                case AreaIds.Alchemy: return "藥水";
                case AreaIds.Farm: return "農場";
                case FunctionIds.Dialogue: return "對話";
                default: return functionId;
            }
        }
    }

    /// <summary>
    /// 功能選單 ID 常數定義（非區域的功能）。
    /// </summary>
    public static class FunctionIds
    {
        public const string Dialogue = "Dialogue";
    }
}
