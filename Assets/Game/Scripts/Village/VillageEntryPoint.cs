using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Exploration;
using ProjectDR.Village.UI;
using UnityEngine;

namespace ProjectDR.Village
{
    /// <summary>
    /// 村莊場景的進入點（MonoBehaviour）。
    /// 負責建立所有村莊模組、注入相依關係、初始化 UI 並監聽導航事件。
    /// 掛載在場景的 VillageEntryPoint GameObject 上。
    /// </summary>
    public class VillageEntryPoint : MonoBehaviour
    {
        [Header("View Prefabs")]
        [SerializeField] private VillageHubView _hubViewPrefab;
        [SerializeField] private CharacterInteractionView _characterInteractionViewPrefab;
        [SerializeField] private StorageAreaView _storageViewPrefab;
        [SerializeField] private ExplorationAreaView _explorationViewPrefab;
        [SerializeField] private AlchemyAreaView _alchemyViewPrefab;
        [SerializeField] private FarmAreaView _farmViewPrefab;

        [Header("Village Canvas")]
        [SerializeField] private Canvas _villageCanvas;

        [Header("Exploration Config")]
        [SerializeField] private TextAsset _mapJson;
        [SerializeField] private TextAsset _combatConfigJson;
        [SerializeField] private TextAsset _monsterConfigJson;

        [Header("Affinity Config")]
        [SerializeField] private TextAsset _affinityConfigJson;

        [Header("Gift View")]
        [SerializeField] private GiftAreaView _giftViewPrefab;

        [Header("CG System")]
        [SerializeField] private TextAsset _cgSceneConfigJson;
        [SerializeField] private CGGalleryView _galleryViewPrefab;
        [SerializeField] private ProjectBSR.DialogueSystem.View.DialogueView _kgcDialogueViewPrefab;

        [Header("Progression Config (Sprint 4 B3/B4/B6)")]
        [SerializeField] private TextAsset _initialResourcesConfigJson;
        [SerializeField] private TextAsset _mainQuestConfigJson;
        [SerializeField] private TextAsset _storageExpansionConfigJson;

        [Header("Commission Config (Sprint 4 B5)")]
        [SerializeField] private TextAsset _commissionRecipesConfigJson;

        [Header("Commission UI (Sprint 4 B11)")]
        [SerializeField] private UI.CraftWorkbenchView _craftWorkbenchPrefab;
        [SerializeField] private UI.CraftItemSelectorView _craftItemSelectorPrefab;

        [Header("Opening & Guard Return Config (Sprint 4 B9/B10)")]
        [SerializeField] private TextAsset _characterIntroConfigJson;
        [SerializeField] private TextAsset _nodeDialogueConfigJson;
        [SerializeField] private TextAsset _guardReturnConfigJson;

        [Header("CG Intro View (Sprint 4 B13)")]
        [Tooltip("CharacterIntroCGView Prefab，B13 真正的 CG 播放器使用")]
        [SerializeField] private UI.CharacterIntroCGView _characterIntroCGViewPrefab;

        [Header("Player Questions Config (Sprint 4 B14)")]
        [SerializeField] private TextAsset _playerQuestionsConfigJson;
        [SerializeField] private UI.PlayerQuestionsView _playerQuestionsViewPrefab;

        [Header("UI Container")]
        [SerializeField] private Transform _uiContainer;

        [Header("打字機設定")]
        [Tooltip("打字機每秒顯示的字元數")]
        [SerializeField] private float _typewriterCharsPerSecond = 20f;

        private StorageManager _storageManager;
        private BackpackManager _backpackManager;
        private StorageTransferManager _transferManager;
        private VillageProgressionManager _progressionManager;
        private VillageNavigationManager _navigationManager;
        private ExplorationEntryManager _explorationManager;
        private QuestManager _questManager;
        private DialogueManager _dialogueManager;

        // V3 農田系統
        private ItemTypeResolver _itemTypeResolver;
        private FarmManager _farmManager;
        private ITimeProvider _timeProvider;

        // 好感度系統
        private AffinityManager _affinityManager;
        private GiftManager _giftManager;

        // CG 解鎖與回憶系統
        private CGSceneConfig _cgSceneConfig;
        private CGUnlockManager _cgUnlockManager;
        private HCGDialogueSetup _hcgDialogueSetup;

        // Sprint 4 B3/B4/B6 新系統
        private StorageExpansionConfig _storageExpansionConfig;
        private StorageExpansionManager _storageExpansionManager;
        private InitialResourcesConfig _initialResourcesConfig;
        private MainQuestConfig _mainQuestConfig;
        private MainQuestManager _mainQuestManager;
        private CharacterUnlockManager _characterUnlockManager;
        private InitialResourceDispatcher _initialResourceDispatcher;

        // Sprint 4 B5 委託系統
        private CommissionRecipesConfig _commissionRecipesConfig;
        private CommissionManager _commissionManager;

        // Sprint 4 B11/B12 委託 UI
        private CommissionInteractionPresenter _commissionPresenter;

        // Sprint 4 B7 紅點系統
        private RedDotManager _redDotManager;

        // Sprint 4 B9 開場劇情演出系統
        private CharacterIntroConfig _characterIntroConfig;
        private NodeDialogueConfig _nodeDialogueConfig;
        private ICGPlayer _cgPlayer;
        private NodeDialogueController _nodeDialogueController;
        private OpeningSequenceController _openingSequenceController;

        // Sprint 4 B10 守衛歸來事件系統
        private GuardReturnConfig _guardReturnConfig;
        private GuardReturnEventController _guardReturnEventController;
        private ExplorationDepartureInterceptorAdapter _explorationInterceptor;

        // Sprint 4 B14 玩家發問系統
        private PlayerQuestionsConfig _playerQuestionsConfig;

        private ViewStackController _stackController;
        private readonly HashSet<string> _initializedViews = new HashSet<string>();

        // 探索切換用
        private GameObject _explorationRoot;

        // 角色資料（IT 階段在此定義 placeholder）
        private List<CharacterMenuData> _characters;

        private void Start()
        {
            InitializeManagers();
            InitializeCharacterData();
            InitializeUI();
            SubscribeToNavigationEvents();
            SubscribeToExplorationEvents();
            EventBus.Subscribe<CGUnlockedEvent>(OnCGUnlocked);

            // C2（Sprint 4）：啟動開場劇情序列（節點 0 強制流程）。
            // 條件：_openingSequenceController 建構成功（需 _characterIntroConfigJson + _nodeDialogueConfigJson）。
            // 啟動後玩家會進入強制模式的村長夫人畫面，無法返回 Hub 直到選擇 1 完成。
            TryStartOpeningSequence();

            // C2：自動完成 T0（auto 類型），發布主線事件訊號連動紅點 L3/L4。
            if (_mainQuestManager != null)
            {
                _mainQuestManager.TryAutoCompleteFirstAutoQuest();
            }
        }

        private void Update()
        {
            // B5：推進委託系統倒數。採現實時間戳記差值計算，deltaSeconds 僅為語意傳遞。
            if (_commissionManager != null)
            {
                _commissionManager.Tick(Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromNavigationEvents();
            UnsubscribeFromExplorationEvents();
            EventBus.Unsubscribe<CGUnlockedEvent>(OnCGUnlocked);
            EventBus.Unsubscribe<CommissionClaimedEvent>(OnCommissionClaimedForMainQuest);

            // C2（Sprint 4）：取消訂閱
            EventBus.Unsubscribe<CharacterUnlockedEvent>(OnCharacterUnlockedForProgression);
            EventBus.Unsubscribe<NodeDialogueCompletedEvent>(OnNodeDialogueCompletedForMainQuest);
            EventBus.Unsubscribe<ExplorationDepartedEvent>(OnExplorationDepartedForMainQuest);
            EventBus.Unsubscribe<GuardReturnEventCompletedEvent>(OnGuardReturnForMainQuest);
            EventBus.Unsubscribe<MainQuestCompletedEvent>(OnMainQuestCompletedForNodeDialogue);
            EventBus.Unsubscribe<StorageExpansionCompletedEvent>(OnStorageExpansionCompletedForMainQuest);

            if (_explorationManager != null)
            {
                _explorationManager.Dispose();
            }

            if (_cgUnlockManager != null)
            {
                _cgUnlockManager.Dispose();
            }

            if (_hcgDialogueSetup != null)
            {
                _hcgDialogueSetup.Dispose();
            }

            if (_characterUnlockManager != null)
            {
                _characterUnlockManager.Dispose();
            }

            if (_commissionPresenter != null)
            {
                _commissionPresenter.Dispose();
                _commissionPresenter = null;
            }

            // Sprint 4 第六波清理
            if (_redDotManager != null)
            {
                _redDotManager.Dispose();
                _redDotManager = null;
            }
            if (_openingSequenceController != null)
            {
                _openingSequenceController.Dispose();
                _openingSequenceController = null;
            }
            if (_nodeDialogueController != null)
            {
                _nodeDialogueController.Dispose();
                _nodeDialogueController = null;
            }
            if (_guardReturnEventController != null)
            {
                _guardReturnEventController.Dispose();
                _guardReturnEventController = null;
            }
        }

        private void InitializeManagers()
        {
            // 依照相依順序建立模組（被依賴的先建立）

            // 倉庫擴建配置：先載入，以便決定 StorageManager 初始容量
            StorageExpansionConfigData expansionData = _storageExpansionConfigJson != null
                ? JsonUtility.FromJson<StorageExpansionConfigData>(_storageExpansionConfigJson.text)
                : new StorageExpansionConfigData { initial_capacity = StorageManager.DefaultInitialCapacity, stages = new StorageExpansionStageData[0] };
            _storageExpansionConfig = new StorageExpansionConfig(expansionData);

            int storageInitialCapacity = _storageExpansionConfig.InitialCapacity > 0
                ? _storageExpansionConfig.InitialCapacity
                : StorageManager.DefaultInitialCapacity;
            _storageManager = new StorageManager(storageInitialCapacity, StorageManager.DefaultMaxStackValue);

            // IT 階段暫用硬編碼數值，正式版本應從外部資料源載入
            // TODO: 將 maxSlots 與 defaultMaxStack 移至外部配置
            int backpackMaxSlots = 20;
            int backpackDefaultMaxStack = 99;
            _backpackManager = new BackpackManager(backpackMaxSlots, backpackDefaultMaxStack);
            _transferManager = new StorageTransferManager(_backpackManager, _storageManager);

            _progressionManager = new VillageProgressionManager();
            _navigationManager = new VillageNavigationManager(_progressionManager);
            _explorationManager = new ExplorationEntryManager(_backpackManager);
            _questManager = new QuestManager(_storageManager);
            _dialogueManager = new DialogueManager();

            // V3 農田系統
            _timeProvider = new SystemTimeProvider();
            _itemTypeResolver = new ItemTypeResolver();

            // IT 階段：手動註冊物品分類
            // TODO: 正式版本應從外部資料表載入
            _itemTypeResolver.Register("seed_wheat", ItemTypes.Seed);
            _itemTypeResolver.Register("seed_carrot", ItemTypes.Seed);
            _itemTypeResolver.Register("seed_herb", ItemTypes.Seed);
            _itemTypeResolver.Register("wheat", ItemTypes.Ingredient);
            _itemTypeResolver.Register("carrot", ItemTypes.Ingredient);
            _itemTypeResolver.Register("herb", ItemTypes.Ingredient);

            var seedDataMap = new Dictionary<string, SeedData>
            {
                { "seed_wheat", new SeedData("seed_wheat", "wheat", 300f) },    // 5 分鐘
                { "seed_carrot", new SeedData("seed_carrot", "carrot", 600f) }, // 10 分鐘
                { "seed_herb", new SeedData("seed_herb", "herb", 180f) }       // 3 分鐘
            };

            int farmPlotCount = 3;
            _farmManager = new FarmManager(
                farmPlotCount, seedDataMap, _itemTypeResolver,
                _storageManager, _timeProvider);

            // 好感度系統
            AffinityConfigData affinityConfigData = _affinityConfigJson != null
                ? JsonUtility.FromJson<AffinityConfigData>(_affinityConfigJson.text)
                : new AffinityConfigData
                {
                    characters = new AffinityCharacterConfigData[0],
                    defaultThresholds = new int[] { 5 }
                };
            AffinityConfig affinityConfig = new AffinityConfig(affinityConfigData);
            _affinityManager = new AffinityManager(affinityConfig);
            _giftManager = new GiftManager(_affinityManager, _backpackManager, _storageManager);

            // CG 解鎖與回憶系統
            CGSceneConfigData cgConfigData = _cgSceneConfigJson != null
                ? JsonUtility.FromJson<CGSceneConfigData>(_cgSceneConfigJson.text)
                : new CGSceneConfigData { scenes = new CGSceneConfigEntry[0] };
            _cgSceneConfig = new CGSceneConfig(cgConfigData);
            _cgUnlockManager = new CGUnlockManager(_cgSceneConfig);

            // HCG 劇情播放（需要 KGC DialogueView Prefab）
            if (_kgcDialogueViewPrefab != null && _villageCanvas != null)
            {
                _hcgDialogueSetup = new HCGDialogueSetup(
                    _kgcDialogueViewPrefab,
                    _villageCanvas.transform);
            }

            // C2（Sprint 4）：移除 IT 階段的「強制解鎖四位角色」，
            // 漸進解鎖由 CharacterUnlockManager 主導。
            // 僅村長夫人在 VillageProgressionManager 層級保持可導航
            // （節點 0/1/2 流程中玩家只能互動村長夫人，直到選項解鎖其他角色）。
            _progressionManager.ForceUnlock(CharacterIds.VillageChiefWife);

            // Sprint 4 B3/B4/B6 系統組裝 —
            // 注意：B8（VillageHubView 漸進解鎖）由 ui-ux-designer 並行處理，
            // 此處僅建立管理器實例，UI 層的實際連接留給後續整合（C1 端到端整合）。

            _initialResourcesConfig = new InitialResourcesConfig(
                _initialResourcesConfigJson != null
                    ? JsonUtility.FromJson<InitialResourcesConfigData>(_initialResourcesConfigJson.text)
                    : new InitialResourcesConfigData { grants = new InitialResourceGrantData[0] });

            _mainQuestConfig = new MainQuestConfig(
                _mainQuestConfigJson != null
                    ? JsonUtility.FromJson<MainQuestConfigData>(_mainQuestConfigJson.text)
                    : new MainQuestConfigData { main_quests = new MainQuestConfigEntry[0] });

            _storageExpansionManager = new StorageExpansionManager(
                _storageManager, _backpackManager, _storageExpansionConfig);

            _initialResourceDispatcher = new InitialResourceDispatcher(_backpackManager, _storageManager);

            _mainQuestManager = new MainQuestManager(_mainQuestConfig);

            _characterUnlockManager = new CharacterUnlockManager(
                _initialResourcesConfig, _initialResourceDispatcher);

            // B5 委託系統組裝
            CommissionRecipesConfigData commissionData = _commissionRecipesConfigJson != null
                ? JsonUtility.FromJson<CommissionRecipesConfigData>(_commissionRecipesConfigJson.text)
                : new CommissionRecipesConfigData { recipes = new CommissionRecipeEntry[0] };
            _commissionRecipesConfig = new CommissionRecipesConfig(commissionData);

            // IT 階段：僅啟用魔女與守衛，農女暫時繼續走 FarmManager（見 dev-log 2026-04-18-3）
            string[] allowedCommissionCharacters = new string[]
            {
                CharacterIds.Witch,
                CharacterIds.Guard,
            };
            _commissionManager = new CommissionManager(
                _commissionRecipesConfig, _backpackManager, _storageManager,
                _timeProvider, allowedCommissionCharacters);

            // 連線委託完成 → 主線任務 commission_count 訊號
            EventBus.Subscribe<CommissionClaimedEvent>(OnCommissionClaimedForMainQuest);

            // C2（Sprint 4）：連線角色解鎖 → VillageProgressionManager 對應導航 ID 解鎖。
            // 讓漸進解鎖的角色同時能通過 NavigationManager 的 IsAreaUnlocked 檢查。
            EventBus.Subscribe<CharacterUnlockedEvent>(OnCharacterUnlockedForProgression);

            // C2：連線節點劇情完成 → MainQuest 訊號（T1 首次角色 intro 完成；
            // 實際節點 1 / 2 由主線任務完成事件觸發播放）。
            EventBus.Subscribe<NodeDialogueCompletedEvent>(OnNodeDialogueCompletedForMainQuest);

            // C2：連線探索出發 → MainQuest first_explore；守衛歸來完成另行訊號。
            EventBus.Subscribe<ExplorationDepartedEvent>(OnExplorationDepartedForMainQuest);
            EventBus.Subscribe<GuardReturnEventCompletedEvent>(OnGuardReturnForMainQuest);

            // C2：連線 MainQuestCompletedEvent → 節點 1/2 播放（T1 → node_1、T3 → node_2）
            EventBus.Subscribe<MainQuestCompletedEvent>(OnMainQuestCompletedForNodeDialogue);

            // C2：連線擴建完成 → MainQuest first_storage_expand 訊號
            EventBus.Subscribe<StorageExpansionCompletedEvent>(OnStorageExpansionCompletedForMainQuest);

            // ===== Sprint 4 B7 紅點系統 =====
            _redDotManager = new RedDotManager(_mainQuestConfig, _mainQuestManager);

            // ===== Sprint 4 B9 開場劇情演出系統 =====
            CharacterIntroConfigData introData = _characterIntroConfigJson != null
                ? JsonUtility.FromJson<CharacterIntroConfigData>(_characterIntroConfigJson.text)
                : new CharacterIntroConfigData
                {
                    character_intros = new CharacterIntroData[0],
                    character_intro_lines = new CharacterIntroLineData[0],
                };
            _characterIntroConfig = new CharacterIntroConfig(introData);

            NodeDialogueConfigData nodeData = _nodeDialogueConfigJson != null
                ? JsonUtility.FromJson<NodeDialogueConfigData>(_nodeDialogueConfigJson.text)
                : new NodeDialogueConfigData { node_dialogue_lines = new NodeDialogueLineData[0] };
            _nodeDialogueConfig = new NodeDialogueConfig(nodeData);

            // B13：若有提供 CharacterIntroCGView Prefab，使用真正的 CG 播放器；
            //       否則 fallback 至 PlaceholderCGPlayer（保持向後相容）
            if (_characterIntroCGViewPrefab != null && _uiContainer != null)
            {
                _cgPlayer = new CharacterIntroCGPlayer(
                    _characterIntroConfig,
                    _characterIntroCGViewPrefab,
                    _uiContainer,
                    _typewriterCharsPerSecond);
            }
            else
            {
                _cgPlayer = new PlaceholderCGPlayer(_characterIntroConfig);
            }

            _nodeDialogueController = new NodeDialogueController(_dialogueManager, _nodeDialogueConfig);
            _openingSequenceController = new OpeningSequenceController(_cgPlayer, _nodeDialogueController);

            // ===== Sprint 4 B10 守衛歸來事件系統 =====
            GuardReturnConfigData guardData = _guardReturnConfigJson != null
                ? JsonUtility.FromJson<GuardReturnConfigData>(_guardReturnConfigJson.text)
                : new GuardReturnConfigData { guard_return_lines = new GuardReturnLineData[0] };
            _guardReturnConfig = new GuardReturnConfig(guardData);

            _guardReturnEventController = new GuardReturnEventController(
                _cgPlayer, _dialogueManager, _guardReturnConfig);

            // 注入探索出發攔截器：當探索功能已解鎖 + 守衛未解鎖 + 事件未觸發過 → 攔截首次探索
            _explorationInterceptor = new ExplorationDepartureInterceptorAdapter(
                _guardReturnEventController, _characterUnlockManager);
            _explorationManager.SetDepartureInterceptor(_explorationInterceptor);

            // ===== Sprint 4 B14 玩家發問配置 =====
            PlayerQuestionsConfigData questionsData = _playerQuestionsConfigJson != null
                ? JsonUtility.FromJson<PlayerQuestionsConfigData>(_playerQuestionsConfigJson.text)
                : new PlayerQuestionsConfigData { questions = new PlayerQuestionData[0] };
            _playerQuestionsConfig = new PlayerQuestionsConfig(questionsData);
        }

        /// <summary>
        /// 探索出發攔截器：守衛歸來事件的整合層。
        /// 條件：探索功能已解鎖 + 守衛尚未解鎖 + 事件未觸發過 → 攔截本次出發，改觸發守衛歸來事件。
        /// </summary>
        private class ExplorationDepartureInterceptorAdapter : IExplorationDepartureInterceptor
        {
            private readonly GuardReturnEventController _guardReturnController;
            private readonly CharacterUnlockManager _unlockManager;

            public ExplorationDepartureInterceptorAdapter(
                GuardReturnEventController guardReturnController,
                CharacterUnlockManager unlockManager)
            {
                _guardReturnController = guardReturnController;
                _unlockManager = unlockManager;
            }

            public bool TryIntercept()
            {
                if (_guardReturnController == null || _unlockManager == null) return false;
                if (_guardReturnController.HasTriggered) return false;
                if (_unlockManager.IsUnlocked(CharacterIds.Guard)) return false;

                return _guardReturnController.TriggerEvent();
            }
        }

        /// <summary>
        /// 委託領取時通知主線任務管理器：累積該角色的 commission_count 訊號。
        /// 注意：目前 main-quest-config.json 的 T2/T3 completion_condition_value 為
        /// placeholder（choice1_character|1），實際匹配由 B7/C1 整合階段釐清。
        /// 本處傳送原始 characterId 作為 signalValue，避免遺漏訊號來源。
        /// </summary>
        private void OnCommissionClaimedForMainQuest(CommissionClaimedEvent e)
        {
            if (_mainQuestManager == null) return;
            _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.CommissionCount, e.CharacterId);
        }

        // ===== C2（Sprint 4）新增接線：漸進解鎖 + 主線訊號 =====

        /// <summary>
        /// 角色解鎖事件 → VillageProgressionManager 強制解鎖對應 areaId，
        /// 讓 VillageNavigationManager.IsAreaUnlocked 檢查通過。
        /// </summary>
        private void OnCharacterUnlockedForProgression(CharacterUnlockedEvent e)
        {
            if (_progressionManager == null || e == null || string.IsNullOrEmpty(e.CharacterId)) return;
            _progressionManager.ForceUnlock(e.CharacterId);
        }

        /// <summary>
        /// 節點劇情完成 → 對 MainQuestManager 發送對應訊號：
        /// - node_0 完成：無動作（T0 為 auto 已自動完成）
        /// - node_1 完成：dialogue_end + first_char_intro_complete（推進 T1）
        /// - node_2 完成：無動作（T2/T3 由委託完成訊號推進）
        /// 注意：此處採簡化策略，將節點 1 視為「首次角色引導完成」訊號。
        /// </summary>
        private void OnNodeDialogueCompletedForMainQuest(NodeDialogueCompletedEvent e)
        {
            if (e == null) return;

            // 節點 1 完成：解鎖剩下那位角色（由 Node0ChosenBranch 判斷對側）。
            // 說明：node-dialogue-config.json 的節點 1 單一確認型選項 branch 為空字串，
            // CharacterUnlockManager 的 OnDialogueChoiceSelected 無法用 branch 判斷，
            // 故在此以節點 1 完成事件為觸發點補上解鎖邏輯。
            if (e.NodeId == NodeDialogueController.NodeIdNode1 && _characterUnlockManager != null)
            {
                string chosen = _characterUnlockManager.Node0ChosenBranch;
                string remainingBranch = null;
                if (chosen == NodeDialogueBranchIds.FarmGirl) remainingBranch = NodeDialogueBranchIds.Witch;
                else if (chosen == NodeDialogueBranchIds.Witch) remainingBranch = NodeDialogueBranchIds.FarmGirl;

                if (remainingBranch == NodeDialogueBranchIds.FarmGirl
                    && !_characterUnlockManager.IsUnlocked(CharacterIds.FarmGirl))
                {
                    _characterUnlockManager.ForceUnlock(CharacterIds.FarmGirl);
                    DispatchInitialResourceGrants(InitialResourcesTriggerIds.UnlockFarmGirl);
                }
                else if (remainingBranch == NodeDialogueBranchIds.Witch
                    && !_characterUnlockManager.IsUnlocked(CharacterIds.Witch))
                {
                    _characterUnlockManager.ForceUnlock(CharacterIds.Witch);
                    DispatchInitialResourceGrants(InitialResourcesTriggerIds.UnlockWitch);
                }
            }

            // 節點 1/2 播完後將 VCW view 從 Forced 切回 Normal 並顯示選單/返回按鈕
            if (e.NodeId == NodeDialogueController.NodeIdNode1
                || e.NodeId == NodeDialogueController.NodeIdNode2)
            {
                RevertVCWFromExternalNodeMode();

                // 清除 L4 主線事件紅點（此節點已播完）
                if (_redDotManager != null)
                {
                    _redDotManager.SetMainQuestEventFlag(CharacterIds.VillageChiefWife, false);
                }
            }
        }

        /// <summary>
        /// 將 VCW 的 CharacterInteractionView 從「Forced + 外部驅動對話」模式切回 Normal。
        /// 用於開場節點 0 以外的節點劇情（node_1/2）播放完畢後收尾。
        /// </summary>
        private void RevertVCWFromExternalNodeMode()
        {
            if (_stackController == null) return;
            ViewBase vcwView = _stackController.GetOrCreateInstance(CharacterIds.VillageChiefWife);
            CharacterInteractionView interactionView = vcwView as CharacterInteractionView;
            if (interactionView == null) return;

            interactionView.SetExternalDialogueMode(false);
            interactionView.SetState(CharacterInteractionState.Normal);
            interactionView.ShowFunctionMenu();
        }

        /// <summary>
        /// 依 trigger_id 派發初始資源 grant（呼叫 InitialResourceDispatcher）。
        /// 用於節點 1 補發解鎖角色對應的物資（因 CharacterUnlockManager 的 node_1 分支判斷失效）。
        /// </summary>
        private void DispatchInitialResourceGrants(string triggerId)
        {
            if (_initialResourcesConfig == null || _initialResourceDispatcher == null) return;
            System.Collections.Generic.IReadOnlyList<InitialResourceGrant> grants
                = _initialResourcesConfig.GetGrantsByTrigger(triggerId);
            foreach (InitialResourceGrant grant in grants)
            {
                _initialResourceDispatcher.Dispatch(grant);
            }
        }

        /// <summary>
        /// 玩家出發探索時發送 first_explore 訊號推進 T4。
        /// T4 的 completion_condition_value 為 "guard_return_event_complete"，
        /// 仍會在守衛歸來事件完成時透過 OnGuardReturnForMainQuest 另送一次，
        /// 兩訊號擇一匹配即可完成（MainQuestManager 僅完成匹配者）。
        /// </summary>
        private void OnExplorationDepartedForMainQuest(ExplorationDepartedEvent e)
        {
            if (_mainQuestManager == null) return;
            _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.FirstExplore, null);
        }

        /// <summary>守衛歸來事件完成 → 傳送「guard_return_event_complete」匹配 T4。</summary>
        private void OnGuardReturnForMainQuest(GuardReturnEventCompletedEvent e)
        {
            if (_mainQuestManager == null) return;
            _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.FirstExplore,
                MainQuestSignalValues.GuardReturnEventComplete);
        }

        /// <summary>
        /// 主線任務完成 → 若為 T1（觸發節點 1）或 T3（觸發節點 2），播放對應節點對話。
        /// 節點播放必須等 DialogueManager 空閒（T1 完成時 node_1 對話剛好播完 completed，此時已空閒）。
        /// </summary>
        private void OnMainQuestCompletedForNodeDialogue(MainQuestCompletedEvent e)
        {
            if (_nodeDialogueController == null || e == null) return;
            // 注意：T1 完成 = 節點 1 劇情已經播完（T1 由 node_1 完成觸發），
            // 所以這裡僅在 T2 完成時播放 node_2（GDD 第十八輪 QC-D：節點 2 由 T3 觸發；
            // 本實作採 T2 完成觸發 node_2，因為 T3 的 commission_count 訊號是由委託直接推進）。
            // 最終：何時觸發節點 1 / 2 在 main-quest-config.json 的 completion_condition 之外，
            // 透過「T1 完成 = 節點 1 結束」「T3 完成 = 節點 2 結束」推導。本方法不主動播放節點，
            // 節點播放的時機完全由上層驅動（OpeningSequenceController 播 node_0、
            // 玩家互動觸發 node_1/2 由後續版本決定）。本處保留 hook 供擴展。
        }

        /// <summary>
        /// 倉庫擴建完成 → 推進 first_storage_expand 訊號（對應 MainQuestCompletionTypes.FirstStorageExpand）。
        /// </summary>
        private void OnStorageExpansionCompletedForMainQuest(StorageExpansionCompletedEvent e)
        {
            if (_mainQuestManager == null) return;
            _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.FirstStorageExpand, null);
        }

        /// <summary>
        /// 啟動開場劇情序列。
        /// IT 階段：不使用存檔旗標，每次進入場景都重新播放開場（方便驗證流程）。
        ///
        /// 流程（依 GDD character-unlock-system.md v1.2）：
        /// 1. 先 push VCW 的 CharacterInteractionView 進入 Forced 模式（無返回按鈕）+ 外部驅動對話模式
        /// 2. 啟動開場 → CG 全螢幕 overlay 覆蓋 VCW view 播放登場 CG + intro_lines
        /// 3. CG 完成後 node_0 對話在 VCW view 上播放（DialogueStartedEvent 由外部驅動觸發打字機）
        /// 4. 玩家選擇 VN 選項 → 解鎖角色 + 發放資源
        /// 5. node_0 完成 → OpeningSequenceCompletedEvent → VCW 切至 Normal + 顯示返回按鈕與功能選單
        /// </summary>
        private void TryStartOpeningSequence()
        {
            if (_openingSequenceController == null) return;

            // 訂閱完成事件以收尾 VCW view
            EventBus.Subscribe<OpeningSequenceCompletedEvent>(OnOpeningSequenceCompletedMarkPlayed);

            // 先以 openingMode 初始化 VCW view（設定 Forced + 外部驅動），再經由 NavigationManager 導航過去。
            // 必須走 NavigationManager（而非直接 PushView）才能設定 _currentArea，讓返回按鈕的
            // ReturnToHub 可以正常發 ReturnedToHubEvent。NavigateTo 會透過 OnNavigatedToArea 事件
            // 間接觸發 PushView — 由於 _initializedViews 已經包含 VCW，第二次 Initialize 會 early return。
            InitializeCharacterView(CharacterIds.VillageChiefWife, openingMode: true);
            _navigationManager.NavigateTo(CharacterIds.VillageChiefWife);

            _openingSequenceController.StartOpeningSequence();
        }

        // 已播過的主線節點集合，用於 GetPendingMainQuestNodeId 判斷。
        private readonly HashSet<string> _playedMainQuestNodes = new HashSet<string>();

        private void OnOpeningSequenceCompletedMarkPlayed(OpeningSequenceCompletedEvent e)
        {
            // 開場序列已播放村長夫人 CG → 本 session 內標記該角色 CG 已播過，避免玩家進入村長夫人畫面時重播
            MarkIntroCGPlayed(CharacterIds.VillageChiefWife);
            EventBus.Unsubscribe<OpeningSequenceCompletedEvent>(OnOpeningSequenceCompletedMarkPlayed);

            // 將 VCW view 從 Forced 切回 Normal：開啟返回按鈕 + 顯示功能選單
            ViewBase vcwView = _stackController != null
                ? _stackController.GetOrCreateInstance(CharacterIds.VillageChiefWife)
                : null;
            CharacterInteractionView interactionView = vcwView as CharacterInteractionView;
            if (interactionView != null)
            {
                interactionView.SetExternalDialogueMode(false);
                interactionView.SetState(CharacterInteractionState.Normal);
                interactionView.ShowFunctionMenu();
            }
        }

        /// <summary>
        /// 初始化 IT 階段的角色資料（placeholder 對話與功能選單）。
        /// 正式版本應從外部資料源載入。
        /// </summary>
        private void InitializeCharacterData()
        {
            _characters = new List<CharacterMenuData>
            {
                new CharacterMenuData(
                    CharacterIds.VillageChiefWife,
                    "村長夫人",
                    new DialogueData(new string[]
                    {
                        "歡迎回來，今天辛苦了。",
                        "倉庫裡的物資我已經整理好了，需要什麼儘管拿。"
                    }),
                    new string[] { AreaIds.Storage, FunctionIds.Gift, FunctionIds.Gallery, FunctionIds.Dialogue }
                ),
                new CharacterMenuData(
                    CharacterIds.Guard,
                    "守衛",
                    new DialogueData(new string[]
                    {
                        "又要出門嗎？小心點。",
                        "森林裡最近不太安寧。"
                    }),
                    // B11：守衛改用 CommissionScout 委託按鈕（探索周圍）
                    new string[] { FunctionIds.CommissionScout, FunctionIds.Gift, FunctionIds.Gallery, FunctionIds.Dialogue }
                ),
                new CharacterMenuData(
                    CharacterIds.Witch,
                    "魔女",
                    new DialogueData(new string[]
                    {
                        "嗯...你來了啊。",
                        "需要藥水的話，自己看著辦吧。"
                    }),
                    // B11：魔女改用 CommissionAlchemy 委託按鈕（煉製）
                    new string[] { FunctionIds.CommissionAlchemy, FunctionIds.Gift, FunctionIds.Gallery, FunctionIds.Dialogue }
                ),
                new CharacterMenuData(
                    CharacterIds.FarmGirl,
                    "農女",
                    new DialogueData(new string[]
                    {
                        "啊！你來得正好！",
                        "今天的作物長得可好了！"
                    }),
                    // B11：農女改用 CommissionFarm 委託按鈕（耕種）
                    new string[] { FunctionIds.CommissionFarm, FunctionIds.Gift, FunctionIds.Gallery, FunctionIds.Dialogue }
                )
            };
        }

        private void InitializeUI()
        {
            _stackController = new ViewStackController(_uiContainer);

            // 註冊 View Prefab（延遲 Instantiate）
            _stackController.RegisterPrefab(AreaIds.Hub, _hubViewPrefab);

            // 所有角色共用同一個 CharacterInteractionView Prefab，
            // 但以不同角色 ID 註冊，讓每個角色有獨立的 View 實例
            foreach (CharacterMenuData character in _characters)
            {
                _stackController.RegisterPrefab(character.CharacterId, _characterInteractionViewPrefab);
            }

            // 先建立 Hub 實例並注入相依，再顯示
            InitializeViewDependencies();
            _stackController.SetRoot(AreaIds.Hub);
        }

        private void InitializeViewDependencies()
        {
            // 取得已建立的 Hub 實例並注入相依
            // B8：新增 _characterUnlockManager 注入，支援漸進解鎖顯示
            VillageHubView hubView = _stackController.GetOrCreateInstance(AreaIds.Hub) as VillageHubView;
            if (hubView != null)
            {
                hubView.Initialize(_navigationManager, _characters.AsReadOnly(), _characterUnlockManager, _redDotManager);
            }
        }

        // 登場 CG 已播放記憶（session 內，不持久化 — IT 階段每次進遊戲都重新播放開場）
        private readonly HashSet<string> _introCgPlayedCharacters = new HashSet<string>();

        private bool HasPlayedIntroCG(string characterId)
        {
            return _introCgPlayedCharacters.Contains(characterId);
        }

        private void MarkIntroCGPlayed(string characterId)
        {
            _introCgPlayedCharacters.Add(characterId);
        }

        private void InitializeCharacterView(string characterId, bool openingMode = false)
        {
            if (_initializedViews.Contains(characterId)) return;

            ViewBase view = _stackController.GetOrCreateInstance(characterId);
            if (view == null) return;

            _initializedViews.Add(characterId);

            CharacterInteractionView interactionView = view as CharacterInteractionView;
            if (interactionView == null) return;

            interactionView.Initialize(_dialogueManager, _navigationManager, _affinityManager, _typewriterCharsPerSecond);

            // 註冊功能 View Prefab 與初始化回呼
            RegisterFunctionPrefabs(interactionView);

            // 找到對應的角色資料並設定
            CharacterMenuData characterData = FindCharacterData(characterId);
            if (characterData != null)
            {
                interactionView.SetCharacter(characterData);
            }

            if (openingMode)
            {
                // 開場流程：VCW 進入強制模式 + 外部驅動對話，
                // CG 由 OpeningSequenceController 以 overlay 播放、node_0 對話由 NodeDialogueController 推進
                interactionView.SetState(CharacterInteractionState.Forced);
                interactionView.SetExternalDialogueMode(true);
            }
            else
            {
                // B13：首次進入流程（GDD § 1.5）
                // 若此角色尚未播放過登場 CG → 設定 FirstEntry 狀態 → 播放 CG → 播完後切回 Normal
                if (!HasPlayedIntroCG(characterId) && _cgPlayer != null)
                {
                    interactionView.SetState(CharacterInteractionState.FirstEntry);
                    string capturedCharId = characterId;
                    CharacterInteractionView capturedView = interactionView;
                    _cgPlayer.PlayIntroCG(characterId, () =>
                    {
                        MarkIntroCGPlayed(capturedCharId);
                        // CG 播放完成後切回 Normal 狀態
                        if (capturedView != null && capturedView.gameObject != null)
                        {
                            capturedView.SetState(CharacterInteractionState.Normal);
                        }

                        // 清除 FirstMeet 紅點（玩家已完成首次登場 CG）
                        if (_redDotManager != null)
                        {
                            _redDotManager.SetFirstMeetFlag(capturedCharId, false);
                        }

                        // C2（Sprint 4）：若此首次進入的是非村長夫人（農女/魔女/守衛）→ 推進 T1 dialogue_end 訊號
                        if (capturedCharId != CharacterIds.VillageChiefWife && _mainQuestManager != null)
                        {
                            _mainQuestManager.NotifyCompletionSignal(
                                MainQuestCompletionTypes.DialogueEnd,
                                MainQuestSignalValues.FirstCharIntroComplete);
                        }
                    });
                }
                // 節點 1/2 的播放已移至 OnNavigatedToArea（配合 openingMode 進入 Forced+External），
                // 此處不再處理 VCW 待播節點。
            }

            // B12：通知 CommissionInteractionPresenter 進入角色
            // 僅在委託型角色（Witch / Guard）才執行
            if (_commissionManager != null
                && _commissionManager.GetManagedCharacterIds().Contains(characterId))
            {
                if (_commissionPresenter == null)
                {
                    _commissionPresenter = new CommissionInteractionPresenter(_commissionManager);
                }
                _commissionPresenter.OnEnterCharacter(characterId, interactionView);
            }
        }

        /// <summary>
        /// 為 CharacterInteractionView 註冊所有功能 View 的 Prefab 與初始化回呼。
        /// </summary>
        private void RegisterFunctionPrefabs(CharacterInteractionView interactionView)
        {
            // 倉庫
            if (_storageViewPrefab != null)
            {
                interactionView.RegisterFunctionPrefab(
                    AreaIds.Storage,
                    _storageViewPrefab,
                    (ViewBase view) =>
                    {
                        StorageAreaView storageView = view as StorageAreaView;
                        if (storageView != null)
                        {
                            storageView.Initialize(
                                _storageManager, _backpackManager,
                                _transferManager, _navigationManager);
                            storageView.SetReturnAction(() => interactionView.CloseOverlay());
                        }
                    }
                );
            }

            // 探索
            if (_explorationViewPrefab != null)
            {
                interactionView.RegisterFunctionPrefab(
                    AreaIds.Exploration,
                    _explorationViewPrefab,
                    (ViewBase view) =>
                    {
                        ExplorationAreaView explorationView = view as ExplorationAreaView;
                        if (explorationView != null)
                        {
                            explorationView.Initialize(_explorationManager, _navigationManager);
                            explorationView.SetReturnAction(() => interactionView.CloseOverlay());
                        }
                    }
                );
            }

            // 煉金（Placeholder）
            if (_alchemyViewPrefab != null)
            {
                interactionView.RegisterFunctionPrefab(
                    AreaIds.Alchemy,
                    _alchemyViewPrefab,
                    (ViewBase view) =>
                    {
                        AlchemyAreaView alchemyView = view as AlchemyAreaView;
                        if (alchemyView != null)
                        {
                            alchemyView.Initialize(_navigationManager);
                            alchemyView.SetReturnAction(() => interactionView.CloseOverlay());
                        }
                    }
                );
            }

            // 農場（V3 完整實作）
            if (_farmViewPrefab != null)
            {
                interactionView.RegisterFunctionPrefab(
                    AreaIds.Farm,
                    _farmViewPrefab,
                    (ViewBase view) =>
                    {
                        FarmAreaView farmView = view as FarmAreaView;
                        if (farmView != null)
                        {
                            farmView.Initialize(
                                _farmManager, _storageManager,
                                _itemTypeResolver, _navigationManager);
                            farmView.SetReturnAction(() => interactionView.CloseOverlay());
                        }
                    }
                );
            }

            // 回憶（CG Gallery）
            if (_galleryViewPrefab != null)
            {
                interactionView.RegisterFunctionPrefab(
                    FunctionIds.Gallery,
                    _galleryViewPrefab,
                    (ViewBase view) =>
                    {
                        CGGalleryView galleryView = view as CGGalleryView;
                        if (galleryView != null)
                        {
                            string characterId = interactionView.CurrentCharacterId;
                            galleryView.Initialize(_cgUnlockManager, _hcgDialogueSetup, characterId);
                            galleryView.SetReturnAction(() => interactionView.CloseOverlay());
                        }
                    }
                );
            }

            // 送禮
            if (_giftViewPrefab != null)
            {
                interactionView.RegisterFunctionPrefab(
                    FunctionIds.Gift,
                    _giftViewPrefab,
                    (ViewBase view) =>
                    {
                        GiftAreaView giftView = view as GiftAreaView;
                        if (giftView != null)
                        {
                            string characterId = interactionView.CurrentCharacterId;
                            giftView.Initialize(
                                _giftManager, _affinityManager,
                                _backpackManager, _storageManager,
                                characterId);
                            giftView.SetReturnAction(() => interactionView.CloseOverlay());
                        }
                    }
                );
            }

            // B11 委託工作台（耕種/煉製/探索周圍）
            // 三個功能共用同一個 CraftWorkbenchView Prefab，差別在於設定的角色 ID
            if (_craftWorkbenchPrefab != null)
            {
                RegisterCraftWorkbenchForFunction(interactionView, FunctionIds.CommissionFarm);
                RegisterCraftWorkbenchForFunction(interactionView, FunctionIds.CommissionAlchemy);
                RegisterCraftWorkbenchForFunction(interactionView, FunctionIds.CommissionScout);
            }

            // B14 玩家發問（[對話] 按鈕 → PlayerQuestionsView overlay）
            if (_playerQuestionsViewPrefab != null && _playerQuestionsConfig != null)
            {
                interactionView.RegisterFunctionPrefab(
                    FunctionIds.Dialogue,
                    _playerQuestionsViewPrefab,
                    (ViewBase view) =>
                    {
                        UI.PlayerQuestionsView questionsView = view as UI.PlayerQuestionsView;
                        if (questionsView != null)
                        {
                            string charId = interactionView.CurrentCharacterId;
                            questionsView.Initialize(
                                _playerQuestionsConfig,
                                _affinityManager,
                                _dialogueManager,
                                _redDotManager,
                                charId,
                                _typewriterCharsPerSecond);
                            questionsView.SetReturnAction(() => interactionView.CloseOverlay());
                        }
                    }
                );
            }
        }

        /// <summary>為指定委託功能 ID 註冊 CraftWorkbenchView Prefab。</summary>
        private void RegisterCraftWorkbenchForFunction(CharacterInteractionView interactionView, string functionId)
        {
            if (_craftWorkbenchPrefab == null || _commissionManager == null) return;

            interactionView.RegisterFunctionPrefab(
                functionId,
                _craftWorkbenchPrefab,
                (ViewBase view) =>
                {
                    UI.CraftWorkbenchView workbenchView = view as UI.CraftWorkbenchView;
                    if (workbenchView != null)
                    {
                        workbenchView.Initialize(
                            _commissionManager, _commissionRecipesConfig,
                            _backpackManager, _storageManager);
                        string charId = interactionView.CurrentCharacterId;
                        workbenchView.SetCharacter(charId);
                        workbenchView.SetReturnAction(() => interactionView.CloseOverlay());
                    }
                }
            );
        }

        private CharacterMenuData FindCharacterData(string characterId)
        {
            foreach (CharacterMenuData data in _characters)
            {
                if (data.CharacterId == characterId)
                {
                    return data;
                }
            }
            return null;
        }

        private void SubscribeToNavigationEvents()
        {
            EventBus.Subscribe<NavigatedToAreaEvent>(OnNavigatedToArea);
            EventBus.Subscribe<ReturnedToHubEvent>(OnReturnedToHub);
        }

        private void UnsubscribeFromNavigationEvents()
        {
            EventBus.Unsubscribe<NavigatedToAreaEvent>(OnNavigatedToArea);
            EventBus.Unsubscribe<ReturnedToHubEvent>(OnReturnedToHub);
        }

        private void SubscribeToExplorationEvents()
        {
            EventBus.Subscribe<ExplorationDepartedEvent>(OnExplorationDeparted);
            EventBus.Subscribe<ExplorationCompletedEvent>(OnExplorationCompleted);
        }

        private void UnsubscribeFromExplorationEvents()
        {
            EventBus.Unsubscribe<ExplorationDepartedEvent>(OnExplorationDeparted);
            EventBus.Unsubscribe<ExplorationCompletedEvent>(OnExplorationCompleted);
        }

        private void OnNavigatedToArea(NavigatedToAreaEvent e)
        {
            // 判斷是角色 ID 還是舊的區域 ID
            if (!IsCharacterId(e.AreaId)) return;

            // 進入 VCW 時若有待播主線節點，採用開場同樣的 Forced + 外部驅動對話模式，
            // 讓節點對話能正確顯示（避免被 VCW 一般打招呼對話覆蓋）。
            string pendingNode = GetPendingMainQuestNodeId();
            bool isVCWPendingNode = e.AreaId == CharacterIds.VillageChiefWife
                && !string.IsNullOrEmpty(pendingNode);

            InitializeCharacterView(e.AreaId, openingMode: isVCWPendingNode);
            _stackController.PushView(e.AreaId);

            if (isVCWPendingNode)
            {
                // View 已啟動、訂閱 DialogueStartedEvent，現在播放節點對話
                _playedMainQuestNodes.Add(pendingNode);
                try
                {
                    _nodeDialogueController.PlayNode(pendingNode);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[VillageEntryPoint] 節點 {pendingNode} 播放失敗：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 取得目前等待播放的 VCW 主線節點 ID（node_1 或 node_2）。
        /// 未有待播節點時回傳 null。
        /// </summary>
        private string GetPendingMainQuestNodeId()
        {
            if (_nodeDialogueController == null || _mainQuestManager == null) return null;
            if (_dialogueManager != null && _dialogueManager.IsActive) return null;

            if (!_playedMainQuestNodes.Contains(NodeDialogueController.NodeIdNode1)
                && _mainQuestManager.IsQuestCompleted("T1"))
            {
                return NodeDialogueController.NodeIdNode1;
            }
            if (!_playedMainQuestNodes.Contains(NodeDialogueController.NodeIdNode2)
                && _mainQuestManager.IsQuestCompleted("T3"))
            {
                return NodeDialogueController.NodeIdNode2;
            }
            return null;
        }

        private void OnReturnedToHub(ReturnedToHubEvent e)
        {
            // 清除角色 View 的初始化標記，讓下次進入時重新設定角色資料
            foreach (CharacterMenuData character in _characters)
            {
                _initializedViews.Remove(character.CharacterId);
            }

            // 重置 DialogueManager 狀態，避免上一個角色未結束的對話影響下一次 GetPendingMainQuestNodeId 判斷
            if (_dialogueManager != null)
            {
                _dialogueManager.Reset();
            }

            _stackController.SetRoot(AreaIds.Hub);
        }

        private bool IsCharacterId(string id)
        {
            return id == CharacterIds.VillageChiefWife
                || id == CharacterIds.Guard
                || id == CharacterIds.Witch
                || id == CharacterIds.FarmGirl;
        }

        // ===== 探索切換 =====

        private void OnExplorationDeparted(ExplorationDepartedEvent e)
        {
            // 隱藏村莊 Canvas
            if (_villageCanvas != null)
            {
                _villageCanvas.gameObject.SetActive(false);
            }

            // 動態建立探索根物件與 ExplorationEntryPoint
            _explorationRoot = new GameObject("ExplorationRoot");
            ExplorationEntryPoint explorationEntry = _explorationRoot.AddComponent<ExplorationEntryPoint>();

            // 注入配置 TextAsset（透過反射設定 SerializeField，因為無法直接存取 private field）
            // 改用 Initialize 注入村莊依賴，TextAsset 透過公開方法設定
            explorationEntry.SetConfigAssets(_mapJson, _combatConfigJson, _monsterConfigJson);
            explorationEntry.Initialize(_backpackManager, _explorationManager);
        }

        private void OnExplorationCompleted(ExplorationCompletedEvent e)
        {
            // 延遲一小段時間讓死亡動畫播放完畢
            float returnDelay = 1.5f;
            StartCoroutine(ReturnToVillageAfterDelay(returnDelay));
        }

        private IEnumerator ReturnToVillageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // 銷毀探索根物件（連同所有子物件一起清理）
            if (_explorationRoot != null)
            {
                Destroy(_explorationRoot);
                _explorationRoot = null;
            }

            // 顯示村莊 Canvas
            if (_villageCanvas != null)
            {
                _villageCanvas.gameObject.SetActive(true);
            }

            // 回到 Hub
            _navigationManager.ReturnToHub();
        }

        // ===== CG 解鎖自動播放 =====

        private void OnCGUnlocked(CGUnlockedEvent e)
        {
            if (_hcgDialogueSetup == null || _cgSceneConfig == null) return;

            CGSceneInfo sceneInfo = _cgSceneConfig.GetSceneInfo(e.CgSceneId);
            if (sceneInfo == null) return;

            // HCG 對話觸發時隱藏對話系統以外的 UI
            if (_uiContainer != null)
            {
                _uiContainer.gameObject.SetActive(false);
            }

            // 好感度達標時直接播放 HCG 劇情
            _hcgDialogueSetup.PlayCGScene(sceneInfo.DialogueId, () =>
            {
                // 播放完成，恢復 UI
                if (_uiContainer != null)
                {
                    _uiContainer.gameObject.SetActive(true);
                }
            });
        }
    }
}
