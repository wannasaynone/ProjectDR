// VillageEntryPoint partial — Installer 序列部分（Sprint 7 VillageEntryPoint 瘦身抽出）
// RunInstallers() 為 ADR-003 B5 核心：6 個 IVillageInstaller 的組裝邏輯。
// 此 partial 獨立管理，方便後續新增 Installer 時修改單一聚焦的檔案。

using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.TimeProvider;
using ProjectDR.Village.ItemType;
using ProjectDR.Village.Dialogue;
using ProjectDR.Village.CG;
using ProjectDR.Village.CharacterIntro;
using ProjectDR.Village.CharacterQuestions;
using ProjectDR.Village.IdleChat;
using ProjectDR.Village.Greeting;
using ProjectDR.Village.Affinity;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Commission;
using ProjectDR.Village.Progression;
using ProjectDR.Village.CharacterUnlock;
using ProjectDR.Village.OpeningSequence;
using ProjectDR.Village.Guard;
using ProjectDR.Village.Gift;
using ProjectDR.Village.Farm;
using ProjectDR.Village.Storage;
using ProjectDR.Village.Backpack;
using UnityEngine;

namespace ProjectDR.Village.Core
{
    public partial class VillageEntryPoint
    {
        // ===== B5 核心：Installer 序列（取代原 InitializeManagers / B8 確認）=====

        private void RunInstallers()
        {
            // --- JSON 解析 ---
            StorageExpansionConfigData expansionData = _storageExpansionConfigJson != null
                ? JsonUtility.FromJson<StorageExpansionConfigData>(_storageExpansionConfigJson.text)
                : new StorageExpansionConfigData { initial_capacity = StorageManager.DefaultInitialCapacity, stages = new StorageExpansionStageData[0] };

            int storageInitialCapacity = expansionData.initial_capacity > 0
                ? expansionData.initial_capacity : StorageManager.DefaultInitialCapacity;

            MainQuestConfigData mainQuestData = _mainQuestConfigJson != null
                ? JsonUtility.FromJson<MainQuestConfigData>(_mainQuestConfigJson.text)
                : new MainQuestConfigData { main_quests = new MainQuestConfigEntry[0] };

            InitialResourcesConfigData initialResourcesData = _initialResourcesConfigJson != null
                ? JsonUtility.FromJson<InitialResourcesConfigData>(_initialResourcesConfigJson.text)
                : new InitialResourcesConfigData { grants = new InitialResourceGrantData[0] };

            AffinityConfigData affinityData = _affinityConfigJson != null
                ? JsonUtility.FromJson<AffinityConfigData>(_affinityConfigJson.text)
                : new AffinityConfigData { characters = new AffinityCharacterConfigData[0], defaultThresholds = new int[] { 5 } };

            CGSceneConfigData cgSceneData = _cgSceneConfigJson != null
                ? JsonUtility.FromJson<CGSceneConfigData>(_cgSceneConfigJson.text)
                : new CGSceneConfigData { scenes = new CGSceneConfigEntry[0] };

            CommissionRecipesConfigData commissionData = _commissionRecipesConfigJson != null
                ? JsonUtility.FromJson<CommissionRecipesConfigData>(_commissionRecipesConfigJson.text)
                : new CommissionRecipesConfigData { recipes = new CommissionRecipeEntry[0] };

            CharacterQuestionsConfigData cqData = _characterQuestionsConfigJson != null
                ? JsonUtility.FromJson<CharacterQuestionsConfigData>(_characterQuestionsConfigJson.text)
                : new CharacterQuestionsConfigData { questions = new CharacterQuestionEntryData[0] };

            GreetingConfigData greetingData = _greetingConfigJson != null
                ? JsonUtility.FromJson<GreetingConfigData>(_greetingConfigJson.text)
                : new GreetingConfigData { greetings = new GreetingEntryData[0] };

            IdleChatConfigData idleChatData = _idleChatConfigJson != null
                ? JsonUtility.FromJson<IdleChatConfigData>(_idleChatConfigJson.text)
                : new IdleChatConfigData { topics = new IdleChatTopicData[0] };

            // --- #1 CoreStorageInstaller ---
            _coreStorageInstaller = new CoreStorageInstaller(20, 99, storageInitialCapacity, StorageManager.DefaultMaxStackValue);
            // TODO Sprint 8：ADR-002 退出後，此處改傳真正的 GameDataQuery delegate，例：
            //   GameDataQuery<KahaGameCore.GameData.IGameData> gda = (id) => _gameStaticDataManager.GetGameData<KahaGameCore.GameData.IGameData>(id);
            // Sprint 7 期間所有 Installer 仍直接接受 ConfigData constructor 注入，尚無 Installer 消費 ctx.GameDataAccess，故暫傳 null。
            VillageContext ctx = new VillageContext(_villageCanvas, _uiContainer, gameDataAccess: null);
            _coreStorageInstaller.Install(ctx);
            _backpackManager = _coreStorageInstaller.BackpackManager;
            _storageManager  = _coreStorageInstaller.StorageManager;
            _transferManager = _coreStorageInstaller.StorageTransferManager;

            // --- #2 ProgressionInstaller ---
            _progressionInstaller = new ProgressionInstaller(mainQuestData, initialResourcesData, _backpackManager, _storageManager);
            _progressionInstaller.Install(ctx);
            _mainQuestManager      = _progressionInstaller.GetMainQuestManager();
            _mainQuestConfig       = new MainQuestConfig(mainQuestData);
            _characterUnlockManager = _progressionInstaller.GetCharacterUnlockManager();
            _redDotManager         = _progressionInstaller.GetRedDotManager();
            _progressionManager    = _progressionInstaller.GetVillageProgressionManager();
            _progressionManager.ForceUnlock(CharacterIds.VillageChiefWife);
            _initialResourcesConfig    = new InitialResourcesConfig(initialResourcesData);
            _initialResourceDispatcher = new InitialResourceDispatcher(_backpackManager, _storageManager);

            // --- #3 AffinityInstaller ---
            _affinityInstaller = new AffinityInstaller(affinityData);
            _affinityInstaller.Install(ctx);
            _affinityManager = _affinityInstaller.AffinityManager;
            _giftManager = new GiftManager(_affinityManager, _backpackManager, _storageManager);

            // --- #4 CGInstaller ---
            _cgInstaller = new CGInstaller(cgSceneData);
            _cgInstaller.Install(ctx);
            _cgSceneConfig  = _cgInstaller.CgSceneConfig;
            _cgUnlockManager = _cgInstaller.CgUnlockManager;
            if (_kgcDialogueViewPrefab != null && _villageCanvas != null)
                _hcgDialogueSetup = new HCGDialogueSetup(_kgcDialogueViewPrefab, _villageCanvas.transform);

            // --- #5 CommissionInstaller ---
            string[] allowedCommissionCharacters = new string[] { CharacterIds.Witch, CharacterIds.Guard };
            _commissionInstaller = new CommissionInstaller(commissionData, expansionData, _backpackManager, _storageManager, allowedCommissionCharacters);
            _commissionInstaller.Install(ctx);
            _commissionManager    = _commissionInstaller.GetCommissionManager();
            _commissionRecipesConfig = new CommissionRecipesConfig(commissionData);

            // --- #6 DialogueFlowInstaller ---
            _dialogueFlowInstaller = new DialogueFlowInstaller(cqData, greetingData, idleChatData, _redDotManager, _characterQuestionCountdownSeconds, _dialogueCooldownBaseSeconds);
            _dialogueFlowInstaller.Install(ctx);
            _characterQuestionsManager      = _dialogueFlowInstaller.CharacterQuestionsManager;
            _characterQuestionCountdownManager = _dialogueFlowInstaller.CharacterQuestionCountdownManager;
            _greetingPresenter  = _dialogueFlowInstaller.GreetingPresenter;
            _idleChatPresenter  = _dialogueFlowInstaller.IdleChatPresenter;
            _dialogueCooldownManager = _dialogueFlowInstaller.DialogueCooldownManager;
            _staminaManager     = _dialogueFlowInstaller.StaminaManager;
            _characterQuestionsConfig = new CharacterQuestionsConfig(cqData);

            // --- 非 Installer 化子系統（VEP 自管） ---
            _navigationManager  = new VillageNavigationManager(_progressionManager);
            _explorationManager = new ExplorationEntryManager(_backpackManager);
            _questManager       = new QuestManager(_storageManager);
            _dialogueManager    = new DialogueManager();

            ITimeProvider timeProvider = _coreStorageInstaller.TimeProvider;
            _itemTypeResolver = new ItemTypeResolver();
            _itemTypeResolver.Register("seed_wheat",  ItemTypes.Seed);
            _itemTypeResolver.Register("seed_carrot", ItemTypes.Seed);
            _itemTypeResolver.Register("seed_herb",   ItemTypes.Seed);
            _itemTypeResolver.Register("wheat",  ItemTypes.Ingredient);
            _itemTypeResolver.Register("carrot", ItemTypes.Ingredient);
            _itemTypeResolver.Register("herb",   ItemTypes.Ingredient);
            var seedDataMap = new Dictionary<string, SeedData>
            {
                { "seed_wheat",  new SeedData("seed_wheat",  "wheat",  300f) },
                { "seed_carrot", new SeedData("seed_carrot", "carrot", 600f) },
                { "seed_herb",   new SeedData("seed_herb",   "herb",   180f) }
            };
            _farmManager = new FarmManager(3, seedDataMap, _itemTypeResolver, _storageManager, timeProvider);

            PlayerQuestionsConfigData questionsData = _playerQuestionsConfigJson != null
                ? JsonUtility.FromJson<PlayerQuestionsConfigData>(_playerQuestionsConfigJson.text)
                : new PlayerQuestionsConfigData { questions = new PlayerQuestionData[0] };
            _playerQuestionsConfig  = new PlayerQuestionsConfig(questionsData);
            _playerQuestionsManager = new PlayerQuestionsManager(_playerQuestionsConfig);

            CharacterIntroConfigData introData = _characterIntroConfigJson != null
                ? JsonUtility.FromJson<CharacterIntroConfigData>(_characterIntroConfigJson.text)
                : new CharacterIntroConfigData { character_intros = new CharacterIntroData[0], character_intro_lines = new CharacterIntroLineData[0] };
            _characterIntroConfig = new CharacterIntroConfig(introData);

            NodeDialogueConfigData nodeData = _nodeDialogueConfigJson != null
                ? JsonUtility.FromJson<NodeDialogueConfigData>(_nodeDialogueConfigJson.text)
                : new NodeDialogueConfigData { node_dialogue_lines = new NodeDialogueLineData[0] };
            _nodeDialogueConfig = new NodeDialogueConfig(nodeData);

            if (_characterIntroCGViewPrefab != null && _uiContainer != null)
                _cgPlayer = new CharacterIntroCGPlayer(_characterIntroConfig, _characterIntroCGViewPrefab, _uiContainer, _typewriterCharsPerSecond);
            else
                _cgPlayer = new PlaceholderCGPlayer(_characterIntroConfig);

            _nodeDialogueController   = new NodeDialogueController(_dialogueManager, _nodeDialogueConfig);
            _openingSequenceController = new OpeningSequenceController(_cgPlayer, _nodeDialogueController);
            _guardReturnEventController = new GuardReturnEventController(_cgPlayer);
            _explorationInterceptor = new ExplorationDepartureInterceptorAdapter(_guardReturnEventController, _characterUnlockManager);
            _explorationManager.SetDepartureInterceptor(_explorationInterceptor);

            // _guardFirstMeetDialogueConfig 已移除（A08 併入 NodeDialogueConfig，2026-04-22）

            // B6：跨域事件訂閱
            SubscribeCrossDomainEvents();
        }
    }
}
