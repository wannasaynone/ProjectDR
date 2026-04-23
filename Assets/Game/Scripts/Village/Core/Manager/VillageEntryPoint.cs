// VillageEntryPoint — 村莊場景進入點（ADR-003 B5+B6 重構，Sprint 7 E7）。
// B5：InitializeManagers() 改為依序呼叫 6 個 IVillageInstaller.Install(ctx)，精簡至 <300 行核心方法。
// B6：跨域事件訂閱保留於本 EntryPoint；各域事件訂閱已移入對應 Installer（ADR-003 分散表）。
// B8：InitializeManagers() 已從本檔完全移除，由 Installer 序列取代。

using ProjectDR.Village.Exploration.Core;
using ProjectDR.Village.Gift;
using ProjectDR.Village.Farm;
using ProjectDR.Village.Storage;
using ProjectDR.Village.Backpack;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.Shared;
using ProjectDR.Village.ItemType;
using ProjectDR.Village.Dialogue;
using ProjectDR.Village.CG;
using ProjectDR.Village.CharacterInteraction;
using ProjectDR.Village.CharacterIntro;
using ProjectDR.Village.CharacterQuestions;
using ProjectDR.Village.CharacterStamina;
using ProjectDR.Village.IdleChat;
using ProjectDR.Village.Greeting;
using ProjectDR.Village.Affinity;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Commission;
using ProjectDR.Village.Progression;
using ProjectDR.Village.CharacterUnlock;
using ProjectDR.Village.OpeningSequence;
using ProjectDR.Village.Guard;
using ProjectDR.Village.Alchemy;
using ProjectDR.Village.UI;
using UnityEngine;

namespace ProjectDR.Village.Core
{
    public partial class VillageEntryPoint : MonoBehaviour
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
        [Header("Progression Config")]
        [SerializeField] private TextAsset _initialResourcesConfigJson;
        [SerializeField] private TextAsset _mainQuestConfigJson;
        [SerializeField] private TextAsset _mainQuestUnlocksConfigJson;
        [SerializeField] private TextAsset _storageExpansionConfigJson;
        [SerializeField] private TextAsset _storageExpansionRequirementsConfigJson;
        [Header("Commission Config")]
        [SerializeField] private TextAsset _commissionRecipesConfigJson;
        [Header("Commission UI")]
        [SerializeField] private CraftWorkbenchView _craftWorkbenchPrefab;
        [SerializeField] private CraftItemSelectorView _craftItemSelectorPrefab;
        [Header("Opening Config")]
        [SerializeField] private TextAsset _characterIntroConfigJson;
        [SerializeField] private TextAsset _characterIntroLinesConfigJson;
        [SerializeField] private TextAsset _nodeDialogueConfigJson;
        // _guardReturnConfigJson 已移除（A09：GuardReturnConfigData dead code，E6 2026-04-22）
        [Header("CG Intro View")]
        [SerializeField] private UI.CharacterIntroCGView _characterIntroCGViewPrefab;
        [Header("Player Questions Config")]
        [SerializeField] private TextAsset _playerQuestionsConfigJson;
        [SerializeField] private PlayerQuestionsView _playerQuestionsViewPrefab;
        [Header("Dialogue Flow Config")]
        [SerializeField] private TextAsset _characterQuestionsConfigJson;
        [SerializeField] private TextAsset _characterQuestionOptionsConfigJson;
        [SerializeField] private TextAsset _characterProfilesConfigJson;
        [SerializeField] private TextAsset _personalityAffinityRulesConfigJson;
        [SerializeField] private TextAsset _greetingConfigJson;
        [SerializeField] private TextAsset _idleChatConfigJson;
        [SerializeField] private TextAsset _idleChatAnswersConfigJson;
        [SerializeField] private CharacterQuestionsView _characterQuestionsViewPrefab;
        [Header("Dialogue Timings")]
        [SerializeField] private float _characterQuestionCountdownSeconds = 60f;
        [SerializeField] private float _dialogueCooldownBaseSeconds = 60f;
        [Header("UI Container")]
        [SerializeField] private Transform _uiContainer;
        [Header("Typewriter")]
        [SerializeField] private float _typewriterCharsPerSecond = 20f;

        // ===== Installer 實例（ADR-003 B5）=====
        private CoreStorageInstaller _coreStorageInstaller;
        private ProgressionInstaller _progressionInstaller;
        private AffinityInstaller _affinityInstaller;
        private CGInstaller _cgInstaller;
        private CommissionInstaller _commissionInstaller;
        private DialogueFlowInstaller _dialogueFlowInstaller;

        // ===== Manager 快取（從 Installer 取出）=====
        private BackpackManager _backpackManager;
        private StorageManager _storageManager;
        private StorageTransferManager _transferManager;
        private VillageProgressionManager _progressionManager;
        private VillageNavigationManager _navigationManager;
        private ExplorationEntryManager _explorationManager;
        private QuestManager _questManager;
        private DialogueManager _dialogueManager;
        private ItemTypeResolver _itemTypeResolver;
        private FarmManager _farmManager;
        private AffinityManager _affinityManager;
        private GiftManager _giftManager;
        private CGSceneConfig _cgSceneConfig;
        private CGUnlockManager _cgUnlockManager;
        private HCGDialogueSetup _hcgDialogueSetup;
        private RedDotManager _redDotManager;
        private MainQuestManager _mainQuestManager;
        private CharacterUnlockManager _characterUnlockManager;
        private MainQuestConfig _mainQuestConfig;
        private InitialResourcesConfig _initialResourcesConfig;
        private InitialResourceDispatcher _initialResourceDispatcher;
        private CommissionManager _commissionManager;
        private CommissionInteractionPresenter _commissionPresenter;
        private CommissionRecipesConfig _commissionRecipesConfig;
        private CharacterIntroConfig _characterIntroConfig;
        private NodeDialogueConfig _nodeDialogueConfig;
        private ICGPlayer _cgPlayer;
        private NodeDialogueController _nodeDialogueController;
        private OpeningSequenceController _openingSequenceController;
        private GuardReturnEventController _guardReturnEventController;
        private ExplorationDepartureInterceptorAdapter _explorationInterceptor;
        private PlayerQuestionsConfig _playerQuestionsConfig;
        // _guardFirstMeetDialogueConfig 已移除（A08 併入 NodeDialogueConfig，2026-04-22）
        private CharacterQuestionsConfig _characterQuestionsConfig;
        private CharacterQuestionsManager _characterQuestionsManager;
        private CharacterQuestionCountdownManager _characterQuestionCountdownManager;
        private GreetingPresenter _greetingPresenter;
        private IdleChatPresenter _idleChatPresenter;
        private PlayerQuestionsManager _playerQuestionsManager;
        private DialogueCooldownManager _dialogueCooldownManager;
        private CharacterStaminaManager _staminaManager;
        private ViewStackController _stackController;
        private readonly HashSet<string> _initializedViews = new HashSet<string>();
        private GameObject _explorationRoot;
        private List<CharacterMenuData> _characters;

        // ===== 跨域事件 Handler 快取（ADR-003 B6）=====
        private System.Action<CommissionClaimedEvent> _onCommissionClaimedForMainQuest;
        private System.Action<CharacterUnlockedEvent> _onCharacterUnlockedForProgression;
        private System.Action<NodeDialogueCompletedEvent> _onNodeDialogueCompletedForMainQuest;
        private System.Action<ExplorationDepartedEvent> _onExplorationDepartedForMainQuest;
        private System.Action<GuardReturnEventCompletedEvent> _onGuardReturnLockExploration;
        private System.Action<ExplorationGateReopenedEvent> _onExplorationGateReopenedForT2;
        private System.Action<MainQuestCompletedEvent> _onMainQuestCompletedForNodeDialogue;
        private System.Action<StorageExpansionCompletedEvent> _onStorageExpansionCompletedForMainQuest;

        private void Start()
        {
            RunInstallers();
            InitializeCharacterData();
            InitializeUI();
            SubscribeToNavigationEvents();
            SubscribeToExplorationEvents();
            TryStartOpeningSequence();

            if (_mainQuestManager != null)
                _mainQuestManager.TryAutoCompleteFirstAutoQuest();

            if (_characterQuestionCountdownManager != null && _characters != null)
            {
                _characterQuestionCountdownManager.BlockCountdown(CharacterIds.Guard);
                foreach (CharacterMenuData ch in _characters)
                    _characterQuestionCountdownManager.StartCountdown(ch.CharacterId);
            }
        }

        private void Update()
        {
            _commissionInstaller?.Tick(Time.unscaledDeltaTime);
            _dialogueFlowInstaller?.Tick(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            UnsubscribeFromNavigationEvents();
            UnsubscribeFromExplorationEvents();
            UnsubscribeCrossDomainEvents();

            _explorationManager?.Dispose();
            _hcgDialogueSetup?.Dispose();

            if (_commissionPresenter != null) { _commissionPresenter.Dispose(); _commissionPresenter = null; }
            if (_openingSequenceController != null) { _openingSequenceController.Dispose(); _openingSequenceController = null; }
            if (_nodeDialogueController != null) { _nodeDialogueController.Dispose(); _nodeDialogueController = null; }
            if (_guardReturnEventController != null) { _guardReturnEventController.Dispose(); _guardReturnEventController = null; }

            // B5：逆安裝順序 Uninstall
            _dialogueFlowInstaller?.Uninstall();
            _commissionInstaller?.Uninstall();
            _cgInstaller?.Uninstall();
            _affinityInstaller?.Uninstall();
            _progressionInstaller?.Uninstall();
            _coreStorageInstaller?.Uninstall();
        }

        // ===== B6：跨域事件訂閱 =====

        private void SubscribeCrossDomainEvents()
        {
            _onCommissionClaimedForMainQuest      = OnCommissionClaimedForMainQuest;
            _onCharacterUnlockedForProgression     = OnCharacterUnlockedForProgression;
            _onNodeDialogueCompletedForMainQuest   = OnNodeDialogueCompletedForMainQuest;
            _onExplorationDepartedForMainQuest     = OnExplorationDepartedForMainQuest;
            _onGuardReturnLockExploration          = OnGuardReturnLockExploration;
            _onExplorationGateReopenedForT2        = OnExplorationGateReopenedForT2;
            _onMainQuestCompletedForNodeDialogue   = OnMainQuestCompletedForNodeDialogue;
            _onStorageExpansionCompletedForMainQuest = OnStorageExpansionCompletedForMainQuest;

            EventBus.Subscribe(_onCommissionClaimedForMainQuest);
            EventBus.Subscribe(_onCharacterUnlockedForProgression);
            EventBus.Subscribe(_onNodeDialogueCompletedForMainQuest);
            EventBus.Subscribe(_onExplorationDepartedForMainQuest);
            EventBus.Subscribe(_onGuardReturnLockExploration);
            EventBus.Subscribe(_onExplorationGateReopenedForT2);
            EventBus.Subscribe(_onMainQuestCompletedForNodeDialogue);
            EventBus.Subscribe(_onStorageExpansionCompletedForMainQuest);
        }

        private void UnsubscribeCrossDomainEvents()
        {
            if (_onCommissionClaimedForMainQuest      != null) EventBus.Unsubscribe(_onCommissionClaimedForMainQuest);
            if (_onCharacterUnlockedForProgression     != null) EventBus.Unsubscribe(_onCharacterUnlockedForProgression);
            if (_onNodeDialogueCompletedForMainQuest   != null) EventBus.Unsubscribe(_onNodeDialogueCompletedForMainQuest);
            if (_onExplorationDepartedForMainQuest     != null) EventBus.Unsubscribe(_onExplorationDepartedForMainQuest);
            if (_onGuardReturnLockExploration          != null) EventBus.Unsubscribe(_onGuardReturnLockExploration);
            if (_onExplorationGateReopenedForT2        != null) EventBus.Unsubscribe(_onExplorationGateReopenedForT2);
            if (_onMainQuestCompletedForNodeDialogue   != null) EventBus.Unsubscribe(_onMainQuestCompletedForNodeDialogue);
            if (_onStorageExpansionCompletedForMainQuest != null) EventBus.Unsubscribe(_onStorageExpansionCompletedForMainQuest);
        }

        private void OnCommissionClaimedForMainQuest(CommissionClaimedEvent e)
        {
            if (_mainQuestManager == null) return;
            _mainQuestManager.NotifyCompletionSignal(MainQuestCompletionTypes.CommissionCount, e.CharacterId);
        }

        private void OnCharacterUnlockedForProgression(CharacterUnlockedEvent e)
        {
            if (_progressionManager == null || e == null || string.IsNullOrEmpty(e.CharacterId)) return;
            _progressionManager.ForceUnlock(e.CharacterId);
        }

        private void OnNodeDialogueCompletedForMainQuest(NodeDialogueCompletedEvent e)
        {
            if (e == null) return;
            if (e.NodeId == NodeDialogueController.NodeIdNode2 && _mainQuestManager != null)
            {
                _mainQuestManager.NotifyCompletionSignal(
                    MainQuestCompletionTypes.DialogueEnd, MainQuestSignalValues.Node2DialogueComplete);
            }
            if (e.NodeId == NodeDialogueController.NodeIdNode1 || e.NodeId == NodeDialogueController.NodeIdNode2)
            {
                RevertVCWFromExternalNodeMode();
                _redDotManager?.SetMainQuestEventFlag(CharacterIds.VillageChiefWife, false);
            }
            // A08 併入（2026-04-22）：守衛首次對白完成 → 發劍 + 探索重開
            if (e.NodeId == NodeDialogueController.NodeIdGuardFirstMeet)
            {
                if (_initialResourceDispatcher != null && _initialResourcesConfig != null)
                {
                    var grants = _initialResourcesConfig.GetGrantsByTrigger(InitialResourcesTriggerIds.GuardSwordAsked);
                    foreach (InitialResourceGrant grant in grants)
                        _initialResourceDispatcher.Dispatch(grant);
                }
                EventBus.Publish(new ExplorationGateReopenedEvent());
            }
        }

        private void OnExplorationDepartedForMainQuest(ExplorationDepartedEvent e)
        {
            _mainQuestManager?.NotifyCompletionSignal(MainQuestCompletionTypes.FirstExplore, null);
        }

        private void OnGuardReturnLockExploration(GuardReturnEventCompletedEvent e)
        {
            MarkIntroCGPlayed(CharacterIds.Guard);
            if (_explorationManager == null) return;
            _explorationManager.SetExplorationLocked(true);
            EventBus.Publish(new ExplorationGateLockedEvent());
        }

        private void OnExplorationGateReopenedForT2(ExplorationGateReopenedEvent e)
        {
            _explorationManager?.SetExplorationLocked(false);
            _mainQuestManager?.NotifyCompletionSignal(
                MainQuestCompletionTypes.FirstExplore, MainQuestSignalValues.GuardReturnEventComplete);
            if (_characterQuestionCountdownManager != null)
            {
                _characterQuestionCountdownManager.UnblockCountdown(CharacterIds.Guard);
                _characterQuestionCountdownManager.StartCountdown(CharacterIds.Guard);
            }
        }

        private void OnMainQuestCompletedForNodeDialogue(MainQuestCompletedEvent e)
        {
            // 節點觸發旗標由 InitializeCharacterView CG callback 設定，此處無需額外處理。
        }

        private void OnStorageExpansionCompletedForMainQuest(StorageExpansionCompletedEvent e)
        {
            _mainQuestManager?.NotifyCompletionSignal(MainQuestCompletionTypes.FirstStorageExpand, null);
        }

        // ===== 開場劇情 =====

        private void TryStartOpeningSequence()
        {
            if (_openingSequenceController == null) return;
            EventBus.Subscribe<OpeningSequenceCompletedEvent>(OnOpeningSequenceCompletedMarkPlayed);
            InitializeCharacterView(CharacterIds.VillageChiefWife, openingMode: true);
            _navigationManager.NavigateTo(CharacterIds.VillageChiefWife);
            _openingSequenceController.StartOpeningSequence();
        }

        private readonly HashSet<string> _playedMainQuestNodes = new HashSet<string>();
        private bool _node1TriggerReady;
        private bool _node2TriggerReady;

        private void OnOpeningSequenceCompletedMarkPlayed(OpeningSequenceCompletedEvent e)
        {
            MarkIntroCGPlayed(CharacterIds.VillageChiefWife);
            EventBus.Unsubscribe<OpeningSequenceCompletedEvent>(OnOpeningSequenceCompletedMarkPlayed);
            ViewBase vcwView = _stackController?.GetOrCreateInstance(CharacterIds.VillageChiefWife);
            CharacterInteractionView interactionView = vcwView as CharacterInteractionView;
            if (interactionView != null)
            {
                interactionView.SetExternalDialogueMode(false);
                interactionView.SetState(CharacterInteractionState.Normal);
                interactionView.ShowFunctionMenu();
            }
        }

        private void InitializeCharacterData()
        {
            _characters = new List<CharacterMenuData>
            {
                new CharacterMenuData(CharacterIds.VillageChiefWife, "村長夫人",
                    new DialogueData(new string[] { "歡迎回來，今天辛苦了。", "倉庫裡的物資我已經整理好了，需要什麼儘管拿。" }),
                    new string[] { AreaIds.Storage, FunctionIds.Gift, FunctionIds.Gallery, FunctionIds.Dialogue }),
                new CharacterMenuData(CharacterIds.Guard, "守衛",
                    new DialogueData(new string[] { "又要出門嗎？小心點。", "森林裡最近不太安寧。" }),
                    new string[] { FunctionIds.CommissionScout, FunctionIds.Gift, FunctionIds.Gallery, FunctionIds.Dialogue }),
                new CharacterMenuData(CharacterIds.Witch, "魔女",
                    new DialogueData(new string[] { "嗯...你來了啊。", "需要藥水的話，自己看著辦吧。" }),
                    new string[] { FunctionIds.CommissionAlchemy, FunctionIds.Gift, FunctionIds.Gallery, FunctionIds.Dialogue }),
                new CharacterMenuData(CharacterIds.FarmGirl, "農女",
                    new DialogueData(new string[] { "啊！你來得正好！", "今天的作物長得可好了！" }),
                    new string[] { FunctionIds.CommissionFarm, FunctionIds.Gift, FunctionIds.Gallery, FunctionIds.Dialogue })
            };
        }

        private void InitializeUI()
        {
            _stackController = new ViewStackController(_uiContainer);
            _stackController.RegisterPrefab(AreaIds.Hub, _hubViewPrefab);
            foreach (CharacterMenuData character in _characters)
                _stackController.RegisterPrefab(character.CharacterId, _characterInteractionViewPrefab);
            InitializeViewDependencies();
            _stackController.SetRoot(AreaIds.Hub);
        }

        private void InitializeViewDependencies()
        {
            VillageHubView hubView = _stackController.GetOrCreateInstance(AreaIds.Hub) as VillageHubView;
            hubView?.Initialize(_navigationManager, _characters.AsReadOnly(), _characterUnlockManager, _redDotManager, _explorationManager);
        }

        private readonly HashSet<string> _introCgPlayedCharacters = new HashSet<string>();
        private bool HasPlayedIntroCG(string characterId) => _introCgPlayedCharacters.Contains(characterId);
        private void MarkIntroCGPlayed(string characterId) => _introCgPlayedCharacters.Add(characterId);

        private void InitializeCharacterView(string characterId, bool openingMode = false)
        {
            if (_initializedViews.Contains(characterId)) return;
            ViewBase view = _stackController.GetOrCreateInstance(characterId);
            if (view == null) return;
            _initializedViews.Add(characterId);

            CharacterInteractionView interactionView = view as CharacterInteractionView;
            if (interactionView == null) return;

            interactionView.Initialize(_dialogueManager, _navigationManager, _affinityManager, _redDotManager, _typewriterCharsPerSecond);
            if (_greetingPresenter != null) interactionView.SetGreetingPresenter(_greetingPresenter);
            if (_characterQuestionsManager != null)
                interactionView.SetCharacterQuestionDependencies(_characterQuestionsManager, _characterQuestionCountdownManager);

            string capturedId = characterId;
            interactionView.SetDialogueSuppressionProvider(() =>
                capturedId == CharacterIds.VillageChiefWife && !HasPlayedAllUnlockNodes());

            RegisterFunctionPrefabs(interactionView);

            CharacterMenuData characterData = FindCharacterData(characterId);
            if (characterData != null) interactionView.SetCharacter(characterData);

            if (openingMode)
            {
                interactionView.SetState(CharacterInteractionState.Forced);
                interactionView.SetExternalDialogueMode(true);
            }
            else
            {
                if (!HasPlayedIntroCG(characterId) && _cgPlayer != null)
                {
                    interactionView.SetState(CharacterInteractionState.FirstEntry);
                    string capturedCharId = characterId;
                    CharacterInteractionView capturedView = interactionView;
                    _cgPlayer.PlayIntroCG(characterId, () =>
                    {
                        MarkIntroCGPlayed(capturedCharId);
                        OnCharacterEnteredAndCGDone(capturedCharId, capturedView);
                    });
                }
                else
                {
                    OnCharacterEnteredAndCGDone(characterId, interactionView);
                }
            }

            if (_commissionManager != null && _commissionManager.GetManagedCharacterIds().Contains(characterId))
            {
                if (_commissionPresenter == null)
                    _commissionPresenter = new CommissionInteractionPresenter(_commissionManager);
                _commissionPresenter.OnEnterCharacter(characterId, interactionView);
            }
        }

        private void OnCharacterEnteredAndCGDone(string characterId, CharacterInteractionView view)
        {
            if (view != null && view.gameObject != null) view.SetState(CharacterInteractionState.Normal);
            _redDotManager?.SetFirstMeetFlag(characterId, false);

            if (characterId != CharacterIds.VillageChiefWife && characterId != CharacterIds.Guard && _redDotManager != null)
            {
                _redDotManager.SetMainQuestEventFlag(CharacterIds.VillageChiefWife, true);
                if (_playedMainQuestNodes.Contains(NodeDialogueController.NodeIdNode1))
                    _node2TriggerReady = true;
                else
                    _node1TriggerReady = true;
            }

            if (characterId == CharacterIds.Guard
                && _characterUnlockManager != null && _characterUnlockManager.IsUnlocked(CharacterIds.Guard)
                && _nodeDialogueController != null)
            {
                // A08 併入 NodeDialogueConfig（2026-04-22）：改走 NodeDialogueController 首次觸發機制。
                // NodeDialogueCompletedEvent { NodeId="guard_first_meet" } 的業務邏輯在 OnNodeDialogueCompletedForMainQuest 處理。
                _nodeDialogueController.TryPlayFirstMeetDialogueIfNotTriggered(NodeDialogueController.NodeIdGuardFirstMeet);
            }
        }

        private void RevertVCWFromExternalNodeMode()
        {
            if (_stackController == null) return;
            CharacterInteractionView iv = _stackController.GetOrCreateInstance(CharacterIds.VillageChiefWife) as CharacterInteractionView;
            if (iv == null) return;
            iv.SetExternalDialogueMode(false);
            iv.SetState(CharacterInteractionState.Normal);
            iv.ShowFunctionMenu();
        }

        private CharacterMenuData FindCharacterData(string characterId)
        {
            foreach (CharacterMenuData data in _characters)
                if (data.CharacterId == characterId) return data;
            return null;
        }

        // ===== 導航事件 =====

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
            if (!IsCharacterId(e.AreaId)) return;
            string pendingNode = GetPendingMainQuestNodeId();
            bool isVCWPendingNode = e.AreaId == CharacterIds.VillageChiefWife && !string.IsNullOrEmpty(pendingNode);
            InitializeCharacterView(e.AreaId, openingMode: isVCWPendingNode);
            _stackController.PushView(e.AreaId);
            if (isVCWPendingNode)
            {
                _playedMainQuestNodes.Add(pendingNode);
                try { _nodeDialogueController.PlayNode(pendingNode); }
                catch (System.Exception ex) { Debug.LogWarning($"[VillageEntryPoint] 節點 {pendingNode} 播放失敗：{ex.Message}"); }
            }
        }

        private bool HasPlayedAllUnlockNodes() =>
            _playedMainQuestNodes.Contains(NodeDialogueController.NodeIdNode1)
            && _playedMainQuestNodes.Contains(NodeDialogueController.NodeIdNode2);

        private string GetPendingMainQuestNodeId()
        {
            if (_nodeDialogueController == null) return null;
            if (_dialogueManager != null && _dialogueManager.IsActive) return null;
            if (!_playedMainQuestNodes.Contains(NodeDialogueController.NodeIdNode1) && _node1TriggerReady)
                return NodeDialogueController.NodeIdNode1;
            if (!_playedMainQuestNodes.Contains(NodeDialogueController.NodeIdNode2) && _node2TriggerReady)
                return NodeDialogueController.NodeIdNode2;
            return null;
        }

        private void OnReturnedToHub(ReturnedToHubEvent e)
        {
            foreach (CharacterMenuData character in _characters)
                _initializedViews.Remove(character.CharacterId);
            _dialogueManager?.Reset();
            _stackController.SetRoot(AreaIds.Hub);
        }

        private bool IsCharacterId(string id) =>
            id == CharacterIds.VillageChiefWife || id == CharacterIds.Guard
            || id == CharacterIds.Witch || id == CharacterIds.FarmGirl;

        // ===== 探索切換 =====

        private void OnExplorationDeparted(ExplorationDepartedEvent e)
        {
            _villageCanvas?.gameObject.SetActive(false);
            _explorationRoot = new GameObject("ExplorationRoot");
            ExplorationEntryPoint explorationEntry = _explorationRoot.AddComponent<ExplorationEntryPoint>();
            explorationEntry.SetConfigAssets(_mapJson, _combatConfigJson, _monsterConfigJson);
            explorationEntry.Initialize(_backpackManager, _explorationManager);
        }

        private void OnExplorationCompleted(ExplorationCompletedEvent e) =>
            StartCoroutine(ReturnToVillageAfterDelay(1.5f));

        private IEnumerator ReturnToVillageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_explorationRoot != null) { Destroy(_explorationRoot); _explorationRoot = null; }
            _villageCanvas?.gameObject.SetActive(true);
            _navigationManager.ReturnToHub();
        }

        // ===== CG 解鎖自動播放 =====

        private void OnCGUnlocked(CGUnlockedEvent e)
        {
            if (_hcgDialogueSetup == null || _cgSceneConfig == null) return;
            CGSceneInfo sceneInfo = _cgSceneConfig.GetSceneInfo(e.CgSceneId);
            if (sceneInfo == null) return;
            _uiContainer?.gameObject.SetActive(false);
            _hcgDialogueSetup.PlayCGScene(sceneInfo.DialogueId, () => _uiContainer?.gameObject.SetActive(true));
        }

    }
}
