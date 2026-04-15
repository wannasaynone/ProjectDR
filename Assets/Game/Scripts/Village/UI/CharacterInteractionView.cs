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
        [SerializeField] private Button _fullScreenDialogueArea;

        [Header("功能選單")]
        [SerializeField] private Transform _menuContainer;
        [SerializeField] private Button _menuButtonPrefab;

        [Header("Overlay")]
        [SerializeField] private Transform _overlayContainer;

        [Header("好感度特效")]
        [SerializeField] private Transform _affinityEffectContainer;

        [Header("好感度")]
        [SerializeField] private TMP_Text _affinityLabel;

        [Header("導航")]
        [SerializeField] private Button _returnButton;

        private DialogueManager _dialogueManager;
        private VillageNavigationManager _navigationManager;
        private AffinityManager _affinityManager;

        /// <summary>取得當前顯示的角色 ID。若未設定角色資料則回傳 null。</summary>
        public string CurrentCharacterId => _characterData?.CharacterId;
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

        // IT 階段感謝台詞（硬編碼，後續改為外部資料）
        // 例外說明：IT 階段為最小成本驗證，感謝台詞暫時硬編碼於 View 層
        private static readonly Dictionary<string, string[]> ThankYouLines
            = new Dictionary<string, string[]>
            {
                { CharacterIds.VillageChiefWife, new[] { "謝謝你的禮物，真貼心。" } },
                { CharacterIds.Guard, new[] { "哦...謝了。" } },
                { CharacterIds.Witch, new[] { "嗯，這個有點意思。" } },
                { CharacterIds.FarmGirl, new[] { "哇，給我的嗎？好開心！" } }
            };

        /// <summary>
        /// 由 VillageEntryPoint 注入相依。
        /// </summary>
        /// <param name="dialogueManager">對話管理器。</param>
        /// <param name="navigationManager">導航管理器。</param>
        /// <param name="affinityManager">好感度管理器。</param>
        /// <param name="typewriterCharsPerSecond">打字機每秒顯示字元數。</param>
        public void Initialize(
            DialogueManager dialogueManager,
            VillageNavigationManager navigationManager,
            AffinityManager affinityManager,
            float typewriterCharsPerSecond)
        {
            _dialogueManager = dialogueManager;
            _navigationManager = navigationManager;
            _affinityManager = affinityManager;
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

            if (_fullScreenDialogueArea != null)
            {
                _fullScreenDialogueArea.onClick.AddListener(OnDialogueClicked);
            }

            if (_returnButton != null)
            {
                _returnButton.onClick.AddListener(OnReturnClicked);
            }

            // 訂閱好感度變更事件以即時刷新顯示
            EventBus.Subscribe<AffinityChangedEvent>(OnAffinityChanged);

            // 訂閱送禮成功事件以播放感謝對話
            EventBus.Subscribe<GiftSuccessEvent>(OnGiftSuccess);

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

            // 啟用全螢幕對話點擊區域
            if (_fullScreenDialogueArea != null)
            {
                _fullScreenDialogueArea.gameObject.SetActive(true);
            }

            // 關閉任何 overlay
            CloseOverlay();

            // 刷新好感度顯示
            RefreshAffinityDisplay();

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

            if (_fullScreenDialogueArea != null)
            {
                _fullScreenDialogueArea.onClick.RemoveListener(OnDialogueClicked);
                _fullScreenDialogueArea.gameObject.SetActive(false);
            }

            if (_returnButton != null)
            {
                _returnButton.onClick.RemoveListener(OnReturnClicked);
            }

            // 清理事件訂閱
            EventBus.Unsubscribe<DialogueCompletedEvent>(OnDialogueCompleted);
            EventBus.Unsubscribe<AffinityChangedEvent>(OnAffinityChanged);
            EventBus.Unsubscribe<GiftSuccessEvent>(OnGiftSuccess);

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
            EventBus.Unsubscribe<AffinityChangedEvent>(OnAffinityChanged);
            EventBus.Unsubscribe<GiftSuccessEvent>(OnGiftSuccess);
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

            // 隱藏全螢幕對話點擊區域
            if (_fullScreenDialogueArea != null)
            {
                _fullScreenDialogueArea.gameObject.SetActive(false);
            }

            // 顯示功能選單
            SetMenuVisible(true);
            RefreshMenu();
        }

        /// <summary>
        /// 收到送禮成功事件時：關閉 overlay → 播放感謝對話。
        /// 若 HCG 對話已觸發（UI 被隱藏），activeInHierarchy 為 false，自動跳過。
        /// </summary>
        private void OnGiftSuccess(GiftSuccessEvent e)
        {
            if (_characterData == null || e.CharacterId != _characterData.CharacterId) return;
            if (!gameObject.activeInHierarchy) return;

            // 關閉 overlay（GiftAreaView 可能已觸發 returnAction 關閉，此為保險）
            CloseOverlay();

            // 刷新好感度顯示
            RefreshAffinityDisplay();

            // 播放感謝對話
            string[] lines;
            if (!ThankYouLines.TryGetValue(e.CharacterId, out lines))
            {
                lines = new[] { "謝謝你。" };
            }

            DialogueData thankYouDialogue = new DialogueData(lines);

            // 隱藏功能選單
            SetMenuVisible(false);

            // 啟用全螢幕對話點擊區域
            if (_fullScreenDialogueArea != null)
            {
                _fullScreenDialogueArea.gameObject.SetActive(true);
            }

            // 確保不重複訂閱
            EventBus.Unsubscribe<DialogueCompletedEvent>(OnDialogueCompleted);
            EventBus.Subscribe<DialogueCompletedEvent>(OnDialogueCompleted);

            _dialogueManager.StartDialogue(thankYouDialogue);

            // 播放打字機效果
            string firstLine = _dialogueManager.GetCurrentLine();
            if (firstLine != null && _typewriterEffect != null)
            {
                _typewriterEffect.OnComplete += OnTypewriterLineComplete;
                _typewriterEffect.Play(firstLine, _typewriterCharsPerSecond);
            }
        }

        private void SetMenuVisible(bool visible)
        {
            if (_menuContainer != null)
            {
                _menuContainer.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 隱藏/顯示對話系統以外的 UI 元素（立繪、對話面板、好感度、返回按鈕）。
        /// overlay 開啟時隱藏，關閉時恢復。
        /// </summary>
        private void SetNonDialogueUIVisible(bool visible)
        {
            // PortraitImage(Animator) → PortraitPanel
            if (_portraitAnimator != null)
            {
                _portraitAnimator.transform.parent.gameObject.SetActive(visible);
            }
            // DialogueText → DialogueClickArea → DialoguePanel
            if (_dialogueText != null)
            {
                _dialogueText.transform.parent.parent.gameObject.SetActive(visible);
            }
            if (_returnButton != null)
            {
                _returnButton.gameObject.SetActive(visible);
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

            // overlay 開啟時隱藏其他 UI 元素
            SetMenuVisible(false);
            SetNonDialogueUIVisible(false);
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

                // overlay 關閉時恢復 UI 元素
                SetMenuVisible(true);
                RefreshMenu();
                SetNonDialogueUIVisible(true);
            }
        }

        private void OnReturnClicked()
        {
            // 先關閉 overlay
            CloseOverlay();

            _navigationManager.ReturnToHub();
        }

        // ===== 好感度顯示 =====

        private void RefreshAffinityDisplay()
        {
            if (_affinityLabel == null || _affinityManager == null || _characterData == null) return;

            int current = _affinityManager.GetAffinity(_characterData.CharacterId);
            _affinityLabel.text = $"好感度：{current}";
        }

        private void OnAffinityChanged(AffinityChangedEvent e)
        {
            if (_characterData == null || e.CharacterId != _characterData.CharacterId) return;
            if (!gameObject.activeInHierarchy) return;

            RefreshAffinityDisplay();
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
                case FunctionIds.Gift: return "送禮";
                case FunctionIds.Gallery: return "回憶";
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
        public const string Gift = "Gift";
        public const string Gallery = "Gallery";
    }
}
