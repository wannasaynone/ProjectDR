using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 角色互動畫面的五種狀態（對應 character-interaction.md v2.2）。
    /// </summary>
    public enum CharacterInteractionState
    {
        /// <summary>正常模式：既有互動畫面，對話 + 功能選單 + 返回按鈕。</summary>
        Normal = 0,

        /// <summary>強制模式：無返回按鈕，節點 0 / 強制劇情事件使用。</summary>
        Forced = 1,

        /// <summary>首次進入：播放登場 CG + 短劇情，每角色只觸發一次。</summary>
        FirstEntry = 2,

        /// <summary>委託中：功能選單位置顯示「工作中 mm:ss」倒數 + 認真工作對話。</summary>
        CommissionInProgress = 3,

        /// <summary>完成領取：功能選單位置顯示「領取」按鈕。</summary>
        CommissionReady = 4,
    }

    /// <summary>
    /// 角色互動畫面。
    /// 左側為立繪區（Animator 預留），右上為對話區（打字機效果），
    /// 右下為功能選單（對話結束後顯示），並支援 VN 式選項容器。
    /// 支援在 overlay container 中顯示功能 View（懸浮覆蓋）。
    ///
    /// 五種狀態（見 CharacterInteractionState）：
    /// Normal / Forced / FirstEntry / CommissionInProgress / CommissionReady。
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

        [Header("選項容器（VN 式選項）")]
        [Tooltip("對話中呈現 VN 選項時使用的容器，內容動態生成於此。")]
        [SerializeField] private Transform _choiceContainer;
        [Tooltip("選項按鈕 Prefab（可與 _menuButtonPrefab 同一份，或獨立設計）。")]
        [SerializeField] private Button _choiceButtonPrefab;

        [Header("委託中狀態")]
        [Tooltip("「工作中 mm:ss」倒數計時文字，位於功能選單位置。")]
        [SerializeField] private TMP_Text _commissionCountdownText;

        [Header("完成領取狀態")]
        [Tooltip("委託完成時顯示的「領取」按鈕，位於功能選單位置。")]
        [SerializeField] private Button _commissionClaimButton;

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

        // Sprint 5 B3：紅點下沉（L2 → 對話按鈕、L3 → 任務按鈕）
        private RedDotManager _redDotManager;
        private System.Action<RedDotUpdatedEvent> _onRedDotUpdated;

        // Sprint 5 B17：招呼語系統
        private GreetingPresenter _greetingPresenter;
        /// <summary>
        /// Sprint 5 B17：對每個角色，本 session 內的「當前招呼語等級」（placeholder = 1）。
        /// 未來與 AffinityManager 整合後，由實際好感度等級計算取代。
        /// </summary>
        private int _greetingLevel = 1;

        // 對話按鈕暫時隱藏條件（ex：女角解鎖後到村長夫人家回報前，隱藏該角色的對話按鈕與 L2 紅點）。
        // 若為 null 或回傳 false，視為不隱藏（正常流程）。
        private System.Func<bool> _dialogueSuppressionProvider;

        // 訂閱主線事件用的委派快取
        private System.Action<MainQuestCompletedEvent> _onMainQuestCompleted;
        private System.Action<NodeDialogueCompletedEvent> _onNodeDialogueCompleted;

        // 角色發問（a 路徑）inline 流程相依（Sprint 5 dialogue-flow-correction 第四輪修正）
        private CharacterQuestionsManager _characterQuestionsManager;
        private CharacterQuestionCountdownManager _characterQuestionCountdownManager;
        private CharacterQuestionInfo _currentCharacterQuestion;
        private int _characterQuestionLevel = 1;
        // 延後 SubmitAnswer 用：選完答案後要等 response 播完才提交，
        // 避免 SubmitAnswer → AddAffinity → AffinityThresholdReached → CGUnlockedEvent
        // → HCGDialogueSetup.PlayCGScene 立刻把 UI 蓋掉，response 還沒播完就被搶走。
        private bool _pendingCharacterQuestionSubmit;
        private string _pendingSubmitQuestionId;
        private string _pendingSubmitPersonality;

        /// <summary>取得當前顯示的角色 ID。若未設定角色資料則回傳 null。</summary>
        public string CurrentCharacterId => _characterData?.CharacterId;

        /// <summary>目前的狀態（預設 Normal）。</summary>
        public CharacterInteractionState CurrentState => _currentState;

        private CharacterInteractionState _currentState = CharacterInteractionState.Normal;

        // 委託中倒數剩餘秒數（由 SetState 或 SetCommissionRemainingSeconds 更新）
        private int _commissionRemainingSeconds;

        // 領取按鈕的外部回呼
        private System.Action _commissionClaimCallback;

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

        // IT 階段委託中 placeholder 台詞（B12，硬編碼）
        // 例外說明：IT 階段為最小成本驗證，工作中台詞暫時硬編碼於 View 層
        private static readonly Dictionary<string, string[]> WorkingLines
            = new Dictionary<string, string[]>
            {
                { CharacterIds.Guard, new[] { "……（正在巡邏中，請稍候。）" } },
                { CharacterIds.Witch, new[] { "……（正在煉製中，請勿打擾。）" } },
                { CharacterIds.FarmGirl, new[] { "……（正在耕種中，努力中！）" } }
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
            Initialize(dialogueManager, navigationManager, affinityManager, null, typewriterCharsPerSecond);
        }

        /// <summary>
        /// Sprint 5 B3 擴充：加入 RedDotManager 注入以支援紅點下沉顯示。
        /// </summary>
        public void Initialize(
            DialogueManager dialogueManager,
            VillageNavigationManager navigationManager,
            AffinityManager affinityManager,
            RedDotManager redDotManager,
            float typewriterCharsPerSecond)
        {
            _dialogueManager = dialogueManager;
            _navigationManager = navigationManager;
            _affinityManager = affinityManager;
            _redDotManager = redDotManager;
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
        /// Sprint 5 B17：注入招呼語表示器。
        /// 進入 Normal 狀態時會以此產生招呼語取代既有硬編碼對話。
        /// 若傳 null，行為退回 Sprint 4（使用 CharacterMenuData.Dialogue）。
        /// </summary>
        public void SetGreetingPresenter(GreetingPresenter presenter)
        {
            _greetingPresenter = presenter;
        }

        /// <summary>
        /// Sprint 5 B17：設定當前招呼語等級（placeholder = 1）。
        /// 未來與 AffinityManager 整合後由好感度計算取代。
        /// </summary>
        public void SetGreetingLevel(int level)
        {
            if (level > 0) _greetingLevel = level;
        }

        /// <summary>
        /// 注入角色發問（a 路徑）所需相依，之後對話按鈕 L2 紅點觸發的流程就由本 View inline 呈現，
        /// 不再開 overlay。流程：按對話 → 隱藏選單 → 主對話區播 prompt → 選項容器顯示 4 個選項 →
        /// 選後清選項 → 主對話區播 response → 選單恢復。
        /// </summary>
        public void SetCharacterQuestionDependencies(
            CharacterQuestionsManager questionsManager,
            CharacterQuestionCountdownManager countdownManager)
        {
            _characterQuestionsManager = questionsManager;
            _characterQuestionCountdownManager = countdownManager;
        }

        /// <summary>
        /// 設定對話按鈕隱藏判定。回傳 true 時：
        /// - RefreshMenu 不會建立「對話」按鈕（避免玩家在此時段進入角色發問 / 玩家發問）
        /// - ApplyButtonRedDots 自動不會在對話按鈕上顯示 L2 紅點（因為按鈕不存在）
        /// 典型用途：女角剛解鎖後，必須先回村長夫人家觸發下一個主線節點的期間，
        /// 暫時關閉該角色的對話互動以引導玩家回到 VCW。
        /// </summary>
        public void SetDialogueSuppressionProvider(System.Func<bool> provider)
        {
            _dialogueSuppressionProvider = provider;
            // 立刻套用（例如玩家剛從外部切入，或主線狀態剛變更）
            if (gameObject.activeInHierarchy && _menuContainer != null
                && _menuContainer.gameObject.activeSelf)
            {
                RefreshMenu();
            }
        }

        private bool IsDialogueSuppressed()
        {
            return _dialogueSuppressionProvider != null && _dialogueSuppressionProvider();
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
            // 預設以內部驅動模式啟動；opening flow 會在 SetCharacter 之後再呼叫
            // SetExternalDialogueMode(true) 改為外部驅動，避免殘留上一輪的狀態
            _externalDialogueMode = false;
        }

        /// <summary>
        /// 設定是否由外部驅動 DialogueManager（而非由本 View 呼叫 StartDialogue）。
        /// 開場劇情（OpeningSequenceController）等由外部推進對話時使用，
        /// 外部模式下 OnShow 僅準備 UI，不自動啟動角色預設對話；
        /// 打字機會在 DialogueStartedEvent 到達時自動播放當前行。
        /// </summary>
        public void SetExternalDialogueMode(bool enabled)
        {
            _externalDialogueMode = enabled;
            if (enabled)
            {
                _pendingDialogueStart = false;
            }
        }

        /// <summary>
        /// 設定當前狀態。
        /// 呼叫時立即更新 UI 顯示（返回按鈕、選單、倒數、領取按鈕）。
        /// </summary>
        /// <param name="state">目標狀態。</param>
        public void SetState(CharacterInteractionState state)
        {
            _currentState = state;
            ApplyStateToUI();
        }

        /// <summary>
        /// 更新委託中狀態的剩餘秒數（mm:ss 格式顯示）。
        /// 僅在 CommissionInProgress 狀態時有效。
        /// </summary>
        public void SetCommissionRemainingSeconds(int remainingSeconds)
        {
            _commissionRemainingSeconds = remainingSeconds < 0 ? 0 : remainingSeconds;
            RefreshCommissionCountdownText();
        }

        /// <summary>
        /// 設定「領取」按鈕被按下時的回呼。
        /// 僅在 CommissionReady 狀態下按鈕才會顯示。
        /// </summary>
        public void SetCommissionClaimCallback(System.Action callback)
        {
            _commissionClaimCallback = callback;
        }

        private bool _pendingDialogueStart;
        private bool _externalDialogueMode;

        // CTRL 快轉：按住 LeftCtrl / RightCtrl 時連續跳過打字機並自動推進到下一行
        // 在等待 VN 選項時暫停（必須由玩家選擇）。頻率控制：每 0.05s 推進一次。
        private const float FastForwardIntervalSeconds = 0.05f;
        private float _fastForwardAccumulator;

        private void Update()
        {
            if (_dialogueManager == null) return;
            if (!_dialogueManager.IsActive) return;
            if (_dialogueManager.IsWaitingForChoice) return;

            bool ctrlHeld = UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftControl)
                || UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightControl);
            if (!ctrlHeld)
            {
                _fastForwardAccumulator = 0f;
                return;
            }

            // 打字機播放中 → 立即跳到當前行完整顯示
            if (_typewriterEffect != null && _typewriterEffect.IsPlaying)
            {
                _typewriterEffect.Skip();
                _fastForwardAccumulator = 0f;
                return;
            }

            // 打字機已完成 → 以固定間隔推進到下一行
            _fastForwardAccumulator += UnityEngine.Time.unscaledDeltaTime;
            if (_fastForwardAccumulator < FastForwardIntervalSeconds) return;
            _fastForwardAccumulator = 0f;
            AdvanceDialogue();
        }

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

            if (_commissionClaimButton != null)
            {
                _commissionClaimButton.onClick.AddListener(OnCommissionClaimClicked);
            }

            // 訂閱好感度變更事件以即時刷新顯示
            EventBus.Subscribe<AffinityChangedEvent>(OnAffinityChanged);

            // 訂閱送禮成功事件以播放感謝對話
            EventBus.Subscribe<GiftSuccessEvent>(OnGiftSuccess);

            // 訂閱 VN 選項事件
            EventBus.Subscribe<DialogueChoicePresentedEvent>(OnDialogueChoicePresented);

            // 訂閱 DialogueStartedEvent — 外部驅動模式下由此觸發打字機播放首行
            EventBus.Subscribe<DialogueStartedEvent>(OnExternalDialogueStarted);

            // Sprint 5 B3：訂閱紅點更新事件，L2/L3 變化時重新套用按鈕紅點
            if (_onRedDotUpdated == null) _onRedDotUpdated = OnRedDotUpdated;
            EventBus.Subscribe(_onRedDotUpdated);

            // 主線事件：完成主線任務或播放完節點對話時，重新評估對話按鈕隱藏條件
            if (_onMainQuestCompleted == null) _onMainQuestCompleted = OnMainQuestCompletedRefreshMenu;
            if (_onNodeDialogueCompleted == null) _onNodeDialogueCompleted = OnNodeDialogueCompletedRefreshMenu;
            EventBus.Subscribe(_onMainQuestCompleted);
            EventBus.Subscribe(_onNodeDialogueCompleted);

            // 初次套用狀態到 UI（Normal 為預設）
            ApplyStateToUI();

            if (_externalDialogueMode)
            {
                // 外部驅動：不呼叫 StartDialogue，只準備 UI 等 DialogueStartedEvent 觸發打字機
                PrepareDialogueUI();
            }
            else if (_pendingDialogueStart && _characterData != null)
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
            PrepareDialogueUI();

            // 委託進行中：改用 placeholder 工作中台詞（B12）
            DialogueData dialogueToPlay = _characterData.Dialogue;
            if (_currentState == CharacterInteractionState.CommissionInProgress
                && _characterData != null)
            {
                string[] workingLinesForChar;
                if (WorkingLines.TryGetValue(_characterData.CharacterId, out workingLinesForChar))
                {
                    dialogueToPlay = new DialogueData(workingLinesForChar);
                }
            }
            // Sprint 5 B17：Normal 狀態下用招呼語取代硬編碼對話（若 Presenter 可用且不被 L1/L4 壓制）
            else if (_currentState == CharacterInteractionState.Normal
                     && _greetingPresenter != null
                     && _characterData != null)
            {
                GreetingInfo greeting = _greetingPresenter.TryGreet(_characterData.CharacterId, _greetingLevel);
                if (greeting != null)
                {
                    dialogueToPlay = new DialogueData(new string[] { greeting.Text });
                }
            }

            // 開始對話
            _dialogueManager.StartDialogue(dialogueToPlay);

            // 播放第一行打字機效果
            string firstLine = _dialogueManager.GetCurrentLine();
            if (firstLine != null && _typewriterEffect != null)
            {
                _typewriterEffect.OnComplete += OnTypewriterLineComplete;
                _typewriterEffect.Play(firstLine, _typewriterCharsPerSecond);
            }
        }

        /// <summary>
        /// 準備對話區的 UI（角色名、隱藏選單、清除選項、啟用全螢幕點擊、訂閱 Completed）。
        /// 共用於內部啟動對話（StartDialoguePlayback）與外部驅動模式（OnShow）。
        /// </summary>
        private void PrepareDialogueUI()
        {
            // 設定角色名稱
            if (_characterNameLabel != null && _characterData != null)
            {
                _characterNameLabel.text = _characterData.DisplayName;
            }

            // 隱藏功能選單 / 倒數 / 領取按鈕（對話開始時全部隱藏）
            SetMenuVisible(false);
            SetCommissionCountdownVisible(false);
            SetCommissionClaimButtonVisible(false);

            // 清除選項容器（重新對話前清除）
            ClearChoiceContainer();

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
        }

        /// <summary>
        /// 外部驅動模式下，DialogueManager 被上層啟動時觸發打字機播放首行。
        /// 非外部驅動模式或非本 View 的對話忽略（例：View 未顯示時）。
        /// </summary>
        private void OnExternalDialogueStarted(DialogueStartedEvent e)
        {
            if (!_externalDialogueMode) return;
            if (!gameObject.activeInHierarchy) return;
            if (_typewriterEffect == null) return;

            string firstLine = e?.FirstLine;
            if (string.IsNullOrEmpty(firstLine) && _dialogueManager != null)
            {
                firstLine = _dialogueManager.GetCurrentLine();
            }
            if (string.IsNullOrEmpty(firstLine)) return;

            _typewriterEffect.OnComplete -= OnTypewriterLineComplete;
            _typewriterEffect.OnComplete += OnTypewriterLineComplete;
            _typewriterEffect.Play(firstLine, _typewriterCharsPerSecond);
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

            if (_commissionClaimButton != null)
            {
                _commissionClaimButton.onClick.RemoveListener(OnCommissionClaimClicked);
            }

            // 清理事件訂閱
            EventBus.Unsubscribe<DialogueCompletedEvent>(OnDialogueCompleted);
            EventBus.Unsubscribe<AffinityChangedEvent>(OnAffinityChanged);
            EventBus.Unsubscribe<GiftSuccessEvent>(OnGiftSuccess);
            EventBus.Unsubscribe<DialogueChoicePresentedEvent>(OnDialogueChoicePresented);
            EventBus.Unsubscribe<DialogueStartedEvent>(OnExternalDialogueStarted);
            if (_onRedDotUpdated != null) EventBus.Unsubscribe(_onRedDotUpdated);
            if (_onMainQuestCompleted != null) EventBus.Unsubscribe(_onMainQuestCompleted);
            if (_onNodeDialogueCompleted != null) EventBus.Unsubscribe(_onNodeDialogueCompleted);

            if (_typewriterEffect != null)
            {
                _typewriterEffect.OnComplete -= OnTypewriterLineComplete;
            }

            // 關閉 overlay
            CloseOverlay();

            // 清除選項容器
            ClearChoiceContainer();

            // 若還有延後的角色發問答案，保險提交（例如玩家 response 沒播完就返回 Hub）
            SubmitPendingCharacterQuestionAnswerIfAny();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<DialogueCompletedEvent>(OnDialogueCompleted);
            EventBus.Unsubscribe<AffinityChangedEvent>(OnAffinityChanged);
            EventBus.Unsubscribe<GiftSuccessEvent>(OnGiftSuccess);
            EventBus.Unsubscribe<DialogueChoicePresentedEvent>(OnDialogueChoicePresented);
            EventBus.Unsubscribe<DialogueStartedEvent>(OnExternalDialogueStarted);
        }

        /// <summary>
        /// 對話區域被點擊時的處理。
        /// 若打字機正在播放 → 跳過（直接顯示完整文字）。
        /// 若打字機已完成 → 前進到下一行。
        /// 若正在等待玩家選擇選項 → 不做任何事（點擊被選項吸收）。
        /// </summary>
        private void OnDialogueClicked()
        {
            // 等待選擇中：忽略對話區點擊
            if (_dialogueManager != null && _dialogueManager.IsWaitingForChoice)
            {
                return;
            }

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

            // 先提交延後的角色發問答案（可能觸發 Affinity 門檻 → HCG 播放）。
            // HCG 非同步接手 UI，完成後 UI 還原，此時選單已顯示（下方 SetMenuVisible(true) 先設定）。
            SubmitPendingCharacterQuestionAnswerIfAny();

            // 對話結束後依據當前狀態呈現對應 UI
            // Normal / FirstEntry 完成 → 顯示功能選單
            // CommissionInProgress → 顯示倒數（維持委託中）
            // CommissionReady → 顯示領取按鈕
            // Forced → 不顯示選單（等外部 SetState 切回 Normal / 觸發節點劇情完成流程）
            ApplyStateToUI();

            // 在 Normal / FirstEntry 狀態下，對話完成 → 顯示功能選單
            if (_currentState == CharacterInteractionState.Normal
                || _currentState == CharacterInteractionState.FirstEntry)
            {
                SetMenuVisible(true);
                RefreshMenu();
            }
        }

        /// <summary>
        /// DialogueManager 發布選項事件 → 動態生成選項按鈕於 _choiceContainer。
        /// 點擊後呼叫 DialogueManager.SelectChoice。
        /// </summary>
        private void OnDialogueChoicePresented(DialogueChoicePresentedEvent e)
        {
            if (!gameObject.activeInHierarchy) return;
            if (_choiceContainer == null || _choiceButtonPrefab == null)
            {
                Debug.LogWarning("[CharacterInteractionView] 未設定選項容器或選項按鈕 Prefab，無法呈現 VN 選項。");
                return;
            }
            if (e == null || e.Choices == null) return;

            ClearChoiceContainer();

            // 對話呈現選項期間：隱藏全螢幕對話點擊（避免誤點跳過）
            if (_fullScreenDialogueArea != null)
            {
                _fullScreenDialogueArea.gameObject.SetActive(false);
            }

            // 選項容器顯示
            _choiceContainer.gameObject.SetActive(true);

            foreach (DialogueChoice choice in e.Choices)
            {
                string capturedId = choice.ChoiceId;
                string capturedText = choice.Text;

                Button button = Instantiate(_choiceButtonPrefab, _choiceContainer);
                button.gameObject.SetActive(true);

                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = capturedText;
                }

                button.onClick.AddListener(() => OnChoiceSelected(capturedId));
            }
        }

        private void OnChoiceSelected(string choiceId)
        {
            if (_dialogueManager == null) return;
            if (!_dialogueManager.IsWaitingForChoice) return;

            // 通知 DialogueManager
            _dialogueManager.SelectChoice(choiceId);

            // 清除選項 UI
            ClearChoiceContainer();

            // 重新啟用全螢幕對話點擊（進入分支回應時推進對話）
            if (_fullScreenDialogueArea != null)
            {
                _fullScreenDialogueArea.gameObject.SetActive(true);
            }

            // 若 SelectChoice 後呼叫端已 AppendLines，繼續推進到下一行
            // 若沒有附加，Advance 會直接發布 Completed
            AdvanceDialogue();
        }

        private void ClearChoiceContainer()
        {
            if (_choiceContainer == null) return;
            for (int i = _choiceContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_choiceContainer.GetChild(i).gameObject);
            }
            _choiceContainer.gameObject.SetActive(false);
        }

        /// <summary>
        /// 收到送禮成功事件時：關閉 overlay → 播放感謝對話。
        /// 若 HCG 對話已觸發（UI 被隱藏），activeInHierarchy 為 false，自動跳過。
        /// </summary>
        private void OnGiftSuccess(GiftSuccessEvent e)
        {
            if (_characterData == null || e.CharacterId != _characterData.CharacterId) return;
            if (!gameObject.activeInHierarchy) return;

            string[] lines;
            if (!ThankYouLines.TryGetValue(e.CharacterId, out lines))
            {
                lines = new[] { "謝謝你。" };
            }

            PlayDialogue(lines);
        }

        /// <summary>
        /// 由外部呼叫，於主角色互動畫面的對話區播放一段對話。
        /// 會關閉當前 overlay、隱藏功能選單、啟用全螢幕點擊區，
        /// 對話完成後由 OnDialogueCompleted 自然恢復選單。
        /// </summary>
        /// <param name="lines">要播放的對話行。空值或空陣列會被忽略。</param>
        public void PlayDialogue(string[] lines)
        {
            if (lines == null || lines.Length == 0) return;
            if (!gameObject.activeInHierarchy) return;
            if (_dialogueManager == null) return;

            CloseOverlay();

            RefreshAffinityDisplay();

            SetMenuVisible(false);

            if (_fullScreenDialogueArea != null)
            {
                _fullScreenDialogueArea.gameObject.SetActive(true);
            }

            EventBus.Unsubscribe<DialogueCompletedEvent>(OnDialogueCompleted);
            EventBus.Subscribe<DialogueCompletedEvent>(OnDialogueCompleted);

            _dialogueManager.StartDialogue(new DialogueData(lines));

            string firstLine = _dialogueManager.GetCurrentLine();
            if (firstLine != null && _typewriterEffect != null)
            {
                _typewriterEffect.OnComplete -= OnTypewriterLineComplete;
                _typewriterEffect.OnComplete += OnTypewriterLineComplete;
                _typewriterEffect.Play(firstLine, _typewriterCharsPerSecond);
            }
        }

        // ===== 狀態套用 =====

        /// <summary>
        /// 依 _currentState 更新 UI 可見性：返回按鈕、選單、倒數、領取按鈕。
        /// </summary>
        private void ApplyStateToUI()
        {
            // 返回按鈕：僅在非強制模式下可見
            bool returnButtonVisible = _currentState != CharacterInteractionState.Forced;
            if (_returnButton != null)
            {
                _returnButton.gameObject.SetActive(returnButtonVisible);
            }

            switch (_currentState)
            {
                case CharacterInteractionState.Normal:
                case CharacterInteractionState.FirstEntry:
                case CharacterInteractionState.Forced:
                    // 功能選單由對話完成事件觸發顯示（StartDialoguePlayback 先隱藏）
                    SetCommissionCountdownVisible(false);
                    SetCommissionClaimButtonVisible(false);
                    break;

                case CharacterInteractionState.CommissionInProgress:
                    // 委託中：隱藏功能選單，顯示倒數
                    SetMenuVisible(false);
                    SetCommissionClaimButtonVisible(false);
                    SetCommissionCountdownVisible(true);
                    RefreshCommissionCountdownText();
                    break;

                case CharacterInteractionState.CommissionReady:
                    // 完成領取：隱藏功能選單與倒數，顯示領取按鈕
                    SetMenuVisible(false);
                    SetCommissionCountdownVisible(false);
                    SetCommissionClaimButtonVisible(true);
                    break;
            }
        }

        private void OnCommissionClaimClicked()
        {
            _commissionClaimCallback?.Invoke();
        }

        private void SetMenuVisible(bool visible)
        {
            if (_menuContainer != null)
            {
                _menuContainer.gameObject.SetActive(visible);
            }
        }

        private void SetCommissionCountdownVisible(bool visible)
        {
            if (_commissionCountdownText != null)
            {
                _commissionCountdownText.gameObject.SetActive(visible);
            }
        }

        private void SetCommissionClaimButtonVisible(bool visible)
        {
            if (_commissionClaimButton != null)
            {
                _commissionClaimButton.gameObject.SetActive(visible);
            }
        }

        private void RefreshCommissionCountdownText()
        {
            if (_commissionCountdownText == null) return;
            int seconds = _commissionRemainingSeconds;
            int mm = seconds / 60;
            int ss = seconds % 60;
            _commissionCountdownText.text = $"工作中... {mm:00}:{ss:00}";
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
            // 返回按鈕僅在非強制模式下可見（overlay 開啟時一律隱藏）
            if (_returnButton != null)
            {
                bool effective = visible && _currentState != CharacterInteractionState.Forced;
                _returnButton.gameObject.SetActive(effective);
            }
        }

        private void RefreshMenu()
        {
            if (_menuContainer == null || _characterData == null) return;

            // 清除現有按鈕（Destroy 為延遲銷毀，當幀結束才真正消失 → 下方 ApplyButtonRedDots
            // 不可再以 _menuContainer.childCount 迭代，否則會同時看到舊+新按鈕，索引錯位導致
            // 新按鈕拿不到紅點。改用 _visibleButtons 直接追蹤本輪 Instantiate 的 Button 實例。）
            for (int i = _menuContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_menuContainer.GetChild(i).gameObject);
            }

            bool dialogueSuppressed = IsDialogueSuppressed();

            _visibleFunctionIds.Clear();
            _visibleButtons.Clear();
            foreach (string functionId in _characterData.FunctionIds)
            {
                // 對話按鈕在指定時段暫時隱藏（例：女角解鎖後回 VCW 報到前）
                if (dialogueSuppressed && functionId == FunctionIds.Dialogue) continue;

                string capturedId = functionId;

                Button button = Instantiate(_menuButtonPrefab, _menuContainer);
                button.gameObject.SetActive(true);

                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = GetFunctionDisplayName(capturedId);
                }

                button.onClick.AddListener(() => OnFunctionClicked(capturedId));

                _visibleFunctionIds.Add(capturedId);
                _visibleButtons.Add(button);
            }

            // Sprint 5 B3：套用紅點下沉顯示（L2 → 對話按鈕、L3 → 任務按鈕）
            ApplyButtonRedDots();
        }

        // 保留當下實際生成的功能按鈕 ID 順序，供 ApplyButtonRedDots 依索引對應
        private readonly List<string> _visibleFunctionIds = new List<string>();
        // 對應的 Button 實例（不依賴 _menuContainer.childCount，避免 Destroy 延遲造成舊按鈕殘留干擾）
        private readonly List<Button> _visibleButtons = new List<Button>();

        /// <summary>
        /// Sprint 5 B3：依當前角色的紅點層級，在「對話」/「任務」按鈕上顯示紅點子物件。
        /// - L2（CharacterQuestion）→ 對話按鈕紅點
        /// - L3（NewQuest）→ 任務按鈕紅點
        /// 紅點子物件約定名稱為 "RedDot"，由 UI Prefab 預留。
        /// </summary>
        private void ApplyButtonRedDots()
        {
            if (_characterData == null) return;

            bool hasL2 = false;
            bool hasL3 = false;
            if (_redDotManager != null)
            {
                // L2/L3 下沉顯示與當前最高層的優先序比較無關：即便 L1/L4 同時存在，
                // 下沉到按鈕的紅點仍需顯示（GDD §8：L1/L4 播完後 L2/L3 保留）。
                hasL2 = _redDotManager.IsLayerActive(_characterData.CharacterId, RedDotLayer.CharacterQuestion);
                hasL3 = _redDotManager.IsLayerActive(_characterData.CharacterId, RedDotLayer.NewQuest);
            }

            // 直接迭代本輪 RefreshMenu 建立的 Button 實例，避免 Destroy 延遲造成索引錯位
            for (int i = 0; i < _visibleButtons.Count; i++)
            {
                Button btn = _visibleButtons[i];
                if (btn == null) continue; // Unity overload：若 GameObject 已銷毀，這裡會是 true
                string functionId = _visibleFunctionIds[i];

                Transform redDot = btn.transform.Find("RedDot");
                if (redDot == null) continue;

                bool shouldShow;
                if (functionId == FunctionIds.Dialogue) shouldShow = hasL2;
                else if (functionId == FunctionIds.Quest) shouldShow = hasL3;
                else shouldShow = false;

                redDot.gameObject.SetActive(shouldShow);
            }
        }

        /// <summary>Sprint 5 B3：紅點狀態變更時重新套用按鈕紅點顯示。</summary>
        private void OnRedDotUpdated(RedDotUpdatedEvent e)
        {
            if (!gameObject.activeInHierarchy) return;
            if (_characterData == null || e == null) return;
            if (e.CharacterId != _characterData.CharacterId) return;

            ApplyButtonRedDots();
        }

        /// <summary>主線任務完成時重新評估對話按鈕顯示條件（例：T2 完成 → node_1 pending）。</summary>
        private void OnMainQuestCompletedRefreshMenu(MainQuestCompletedEvent e)
        {
            if (!gameObject.activeInHierarchy) return;
            if (_menuContainer == null || !_menuContainer.gameObject.activeSelf) return;
            RefreshMenu();
        }

        /// <summary>節點對話完成時重新評估對話按鈕顯示條件（pending node 清除後恢復對話按鈕）。</summary>
        private void OnNodeDialogueCompletedRefreshMenu(NodeDialogueCompletedEvent e)
        {
            if (!gameObject.activeInHierarchy) return;
            if (_menuContainer == null || !_menuContainer.gameObject.activeSelf) return;
            RefreshMenu();
        }

        private void OnFunctionClicked(string functionId)
        {
            if (functionId == FunctionIds.Dialogue)
            {
                // Sprint 5 B8 / dialogue-flow-correction 第四輪修正：
                // - L2 紅點亮 → 角色發問（a 路徑，**inline 於本 View**，不再開 overlay）
                // - 無紅點  → 玩家發問（b 路徑，PlayerQuestionsView overlay，消耗體力）
                bool hasL2 = false;
                if (_redDotManager != null && _characterData != null)
                {
                    hasL2 = _redDotManager.IsLayerActive(
                        _characterData.CharacterId, RedDotLayer.CharacterQuestion);
                }

                if (hasL2 && _characterQuestionsManager != null)
                {
                    StartCharacterQuestionInline();
                }
                else if (_functionPrefabs.ContainsKey(FunctionIds.Dialogue))
                {
                    OpenOverlay(FunctionIds.Dialogue);
                }
                else
                {
                    OnDialogueOptionClicked();
                }
                return;
            }

            // 委託功能（耕種/煉製/探索周圍）：開啟格子式工作台
            if (functionId == FunctionIds.CommissionFarm
                || functionId == FunctionIds.CommissionAlchemy
                || functionId == FunctionIds.CommissionScout)
            {
                OpenCraftWorkbench(functionId);
                return;
            }

            // 其他功能：開啟 overlay
            OpenOverlay(functionId);
        }

        /// <summary>
        /// 開啟格子式工作台（CraftWorkbenchView），依 GDD character-interaction.md v2.2 § 6：
        /// 點擊委託功能按鈕時，右側對話框與功能選單消失，顯示格子工作台。
        /// </summary>
        private void OpenCraftWorkbench(string functionId)
        {
            CloseOverlay();

            if (!_functionPrefabs.TryGetValue(functionId, out ViewBase prefab))
            {
                Debug.LogWarning(string.Format("[CharacterInteractionView] 未註冊委託工作台 Prefab: {0}", functionId));
                return;
            }

            if (_overlayContainer == null) return;

            ViewBase overlayView = Instantiate(prefab, _overlayContainer);
            _currentOverlayInstance = overlayView.gameObject;

            if (_functionInitializers.TryGetValue(functionId, out System.Action<ViewBase> initializer))
            {
                initializer(overlayView);
            }

            overlayView.Show();

            // 工作台開啟時：隱藏選單，保留立繪，對話面板消失（依 GDD §6 規則 6）
            SetMenuVisible(false);
            if (_dialogueText != null)
                _dialogueText.transform.parent.parent.gameObject.SetActive(false);
        }

        private void OnDialogueOptionClicked()
        {
            // 重新播放對話（此時 GameObject 必定 active）
            if (_characterData != null)
            {
                StartDialoguePlayback();
            }
        }

        // ===== 角色發問 inline 流程（a 路徑，取代 overlay） =====

        /// <summary>
        /// 啟動角色發問的 inline 流程（dialogue-flow-correction 第四輪修正）：
        /// 1. 隱藏選單
        /// 2. 主對話區以打字機播放 prompt
        /// 3. prompt 播完後於選項容器顯示 4 個選項
        /// 4. 選擇後清選項、主對話區以打字機播放 response
        /// 5. response 播完後 OnDialogueCompleted 路徑自然讓選單恢復
        /// </summary>
        private void StartCharacterQuestionInline()
        {
            if (_characterQuestionsManager == null || _characterData == null) return;
            if (_typewriterEffect == null || _dialogueManager == null) return;

            // 清 L2 + ClearReady（由 CharacterQuestionsView 原先行為移植）
            if (_redDotManager != null)
            {
                _redDotManager.SetCharacterQuestionFlag(_characterData.CharacterId, false);
            }
            if (_characterQuestionCountdownManager != null)
            {
                _characterQuestionCountdownManager.ClearReady(_characterData.CharacterId);
            }

            CharacterQuestionInfo question = _characterQuestionsManager.PickNextQuestion(
                _characterData.CharacterId, _characterQuestionLevel);

            if (question == null)
            {
                // 題目池耗盡 fallback：播一行訊息，流程結束後 menu 自然恢復
                PlayDialogue(new[] { "（目前沒有更多問題。）" });
                // 倒數重啟
                if (_characterQuestionCountdownManager != null)
                {
                    _characterQuestionCountdownManager.StartCountdown(_characterData.CharacterId);
                }
                return;
            }

            _currentCharacterQuestion = question;

            // 1. 隱藏選單（右下關閉）
            SetMenuVisible(false);

            // 2. 主對話區準備播 prompt（右上顯示對話）
            //   - 清除選項容器（防守）
            //   - 禁用全螢幕對話點擊區域（prompt 播放中點擊會被當 Skip，打字機完成後自然進入選項階段，不讓玩家點擊跳過）
            //   - 不透過 DialogueManager（避免 DialogueCompletedEvent 立刻把選單打開）
            ClearChoiceContainer();
            if (_fullScreenDialogueArea != null)
            {
                _fullScreenDialogueArea.gameObject.SetActive(false);
            }

            // 訂閱打字機完成事件以推進到選項階段
            _typewriterEffect.OnComplete -= OnCharacterQuestionPromptComplete;
            _typewriterEffect.OnComplete += OnCharacterQuestionPromptComplete;
            _typewriterEffect.Play(question.Prompt ?? string.Empty, _typewriterCharsPerSecond);

            // 3. 倒數重啟（本次角色發問觸發後重新計時 60s）
            if (_characterQuestionCountdownManager != null)
            {
                _characterQuestionCountdownManager.StartCountdown(_characterData.CharacterId);
            }
        }

        private void OnCharacterQuestionPromptComplete()
        {
            if (_typewriterEffect != null)
            {
                _typewriterEffect.OnComplete -= OnCharacterQuestionPromptComplete;
            }
            if (_currentCharacterQuestion == null) return;

            // 4. prompt 播完 → 於選項容器顯示 4 個選項（右下顯示回答）
            ShowCharacterQuestionChoices(_currentCharacterQuestion);
        }

        private void ShowCharacterQuestionChoices(CharacterQuestionInfo question)
        {
            if (_choiceContainer == null || _choiceButtonPrefab == null)
            {
                Debug.LogWarning("[CharacterInteractionView] 未設定選項容器或選項按鈕 Prefab，無法呈現角色發問選項。");
                // 缺資源時仍讓流程完成：播 fallback 結束
                PlayDialogue(new[] { "（選項無法顯示。）" });
                _currentCharacterQuestion = null;
                return;
            }

            ClearChoiceContainer();
            _choiceContainer.gameObject.SetActive(true);

            foreach (CharacterQuestionOption opt in question.Options)
            {
                string capturedPersonality = opt.Personality;
                string capturedResponse = opt.Response ?? string.Empty;
                string capturedText = opt.Text ?? string.Empty;

                Button btn = Instantiate(_choiceButtonPrefab, _choiceContainer);
                btn.gameObject.SetActive(true);

                TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
                // 規則層：UI 只顯示選項文字，不顯示 +N 數值
                if (label != null) label.text = capturedText;

                btn.onClick.AddListener(() =>
                    OnCharacterQuestionOptionSelected(capturedPersonality, capturedResponse));
            }
        }

        private void OnCharacterQuestionOptionSelected(string personalityId, string response)
        {
            if (_currentCharacterQuestion == null || _characterQuestionsManager == null) return;
            if (_characterData == null) return;

            // 延後 SubmitAnswer（加好感度）到 response 播完後才執行，避免 AffinityThresholdReached →
            // CGUnlockedEvent → HCG 立刻接手 UI 導致 response 沒播出來。
            _pendingCharacterQuestionSubmit = true;
            _pendingSubmitQuestionId = _currentCharacterQuestion.QuestionId;
            _pendingSubmitPersonality = personalityId;

            string responseText = response ?? string.Empty;
            _currentCharacterQuestion = null;

            // 5. 清選項（右下關閉）→ 主對話區播 response（右上顯示對話）
            //    PlayDialogue 會負責恢復 _fullScreenDialogueArea、啟動 Dialogue 序列、
            //    完成後觸發 OnDialogueCompleted → SubmitPendingCharacterQuestionAnswer → SetMenuVisible(true) + RefreshMenu
            ClearChoiceContainer();
            PlayDialogue(new[] { responseText });
        }

        /// <summary>
        /// 將延後的角色發問答案提交至 CharacterQuestionsManager。
        /// 於 OnDialogueCompleted（response 播完）與 OnHide（玩家中途離開）時呼叫以確保答案必被計入。
        /// </summary>
        private void SubmitPendingCharacterQuestionAnswerIfAny()
        {
            if (!_pendingCharacterQuestionSubmit) return;
            _pendingCharacterQuestionSubmit = false;

            if (_characterQuestionsManager == null || _characterData == null)
            {
                _pendingSubmitQuestionId = null;
                _pendingSubmitPersonality = null;
                return;
            }

            _characterQuestionsManager.SubmitAnswer(
                _characterData.CharacterId,
                _pendingSubmitQuestionId,
                _pendingSubmitPersonality);

            _pendingSubmitQuestionId = null;
            _pendingSubmitPersonality = null;
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

                // overlay 關閉時恢復 UI 元素（含對話面板）
                SetMenuVisible(true);
                RefreshMenu();
                SetNonDialogueUIVisible(true);

                // 恢復對話面板（工作台開啟時曾隱藏）
                if (_dialogueText != null)
                    _dialogueText.transform.parent.parent.gameObject.SetActive(true);
            }
        }

        private void OnReturnClicked()
        {
            // 強制模式下忽略（按鈕本應被隱藏，防禦性檢查）
            if (_currentState == CharacterInteractionState.Forced) return;

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
                case FunctionIds.CommissionFarm: return "耕種";
                case FunctionIds.CommissionAlchemy: return "煉製";
                case FunctionIds.CommissionScout: return "探索周圍";
                default: return functionId;
            }
        }

        /// <summary>
        /// 讓呼叫端在 DialogueCompleted 後顯示功能選單（Normal / FirstEntry 狀態）。
        /// 分離此 API 讓外部系統（節點劇情控制器）在特定狀態下控制菜單呈現。
        /// </summary>
        public void ShowFunctionMenu()
        {
            if (_currentState == CharacterInteractionState.CommissionInProgress) return;
            if (_currentState == CharacterInteractionState.CommissionReady) return;

            SetMenuVisible(true);
            RefreshMenu();
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

        /// <summary>Sprint 5 B3：任務按鈕（L3 紅點下沉至此），功能連結至 main-quest-system。</summary>
        public const string Quest = "Quest";

        /// <summary>
        /// Sprint 5 B8：角色發問路徑 ID（內部分流使用）。
        /// 角色選單不暴露此 ID，由 OnFunctionClicked 針對「對話」按鈕依 L2 紅點狀態分流至此。
        /// </summary>
        public const string CharacterQuestion = "CharacterQuestion";

        // 委託功能 ID（B11/B12，對應委託型三角色的專業功能按鈕）
        // 依 character-interaction.md v2.2 § 3：耕種/煉製/探索周圍
        public const string CommissionFarm    = "CommissionFarm";    // 農女 [耕種]
        public const string CommissionAlchemy = "CommissionAlchemy"; // 魔女 [煉製]
        public const string CommissionScout   = "CommissionScout";   // 守衛 [探索周圍]
    }
}
