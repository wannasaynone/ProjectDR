// VillageEntryPoint partial — Installer 序列部分（Sprint 7 VillageEntryPoint 瘦身抽出）
// RunInstallers() 為 ADR-003 B5 核心：6 個 IVillageInstaller 的組裝邏輯。
// 此 partial 獨立管理，方便後續新增 Installer 時修改單一聚焦的檔案。
//
// Sprint 8 Wave 2.5 重構：
//   - 所有 JsonUtility.FromJson<包裹類>(...) 改為 JsonFx JsonReader.Deserialize<T[]>(...)
//   - 廢棄舊包裹類引用（AffinityConfigData、CGSceneConfigData 等）
//   - Installer 建構子改傳純陣列 DTO
//   - 新增子表 TextAsset 解析（MainQuestUnlocks / StorageExpansionRequirements /
//     CharacterQuestionOptions / CharacterProfiles / PersonalityAffinityRules /
//     CharacterIntroLines / IdleChatAnswers）
// ADR-001 / ADR-002

using System.Collections.Generic;
using JsonFx.Json;
using KahaGameCore.GameData;
using KahaGameCore.GameData.Implemented;
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
using ProjectDR.Village.Exploration.Combat;
using UnityEngine;

namespace ProjectDR.Village.Core
{
    public partial class VillageEntryPoint
    {
        // ===== B5 核心：Installer 序列（取代原 InitializeManagers / B8 確認）=====

        private void RunInstallers()
        {
            // ===== 反序列化各分頁純陣列 =====
            // JsonFx.Json.JsonReader.Deserialize<T> 是靜態方法，不可用 new JsonReader()

            // Progression
            MainQuestData[] mainQuestEntries = _mainQuestConfigJson != null
                ? JsonReader.Deserialize<MainQuestData[]>(_mainQuestConfigJson.text)
                : new MainQuestData[0];

            MainQuestUnlockData[] mainQuestUnlockEntries = _mainQuestUnlocksConfigJson != null
                ? JsonReader.Deserialize<MainQuestUnlockData[]>(_mainQuestUnlocksConfigJson.text)
                : new MainQuestUnlockData[0];

            InitialResourceGrantData[] initialResourceEntries = _initialResourcesConfigJson != null
                ? JsonReader.Deserialize<InitialResourceGrantData[]>(_initialResourcesConfigJson.text)
                : new InitialResourceGrantData[0];

            StorageExpansionStageData[] storageExpansionStageEntries = _storageExpansionConfigJson != null
                ? JsonReader.Deserialize<StorageExpansionStageData[]>(_storageExpansionConfigJson.text)
                : new StorageExpansionStageData[0];

            StorageExpansionRequirementData[] storageExpansionRequirementEntries = _storageExpansionRequirementsConfigJson != null
                ? JsonReader.Deserialize<StorageExpansionRequirementData[]>(_storageExpansionRequirementsConfigJson.text)
                : new StorageExpansionRequirementData[0];

            // Affinity
            AffinityCharacterData[] affinityEntries = _affinityConfigJson != null
                ? JsonReader.Deserialize<AffinityCharacterData[]>(_affinityConfigJson.text)
                : new AffinityCharacterData[0];

            // CG
            CGSceneData[] cgSceneEntries = _cgSceneConfigJson != null
                ? JsonReader.Deserialize<CGSceneData[]>(_cgSceneConfigJson.text)
                : new CGSceneData[0];

            // Commission
            CommissionRecipeData[] commissionEntries = _commissionRecipesConfigJson != null
                ? JsonReader.Deserialize<CommissionRecipeData[]>(_commissionRecipesConfigJson.text)
                : new CommissionRecipeData[0];

            // Dialogue Flow
            CharacterQuestionData[] cqEntries = _characterQuestionsConfigJson != null
                ? JsonReader.Deserialize<CharacterQuestionData[]>(_characterQuestionsConfigJson.text)
                : new CharacterQuestionData[0];

            CharacterQuestionOptionData[] cqOptionEntries = _characterQuestionOptionsConfigJson != null
                ? JsonReader.Deserialize<CharacterQuestionOptionData[]>(_characterQuestionOptionsConfigJson.text)
                : new CharacterQuestionOptionData[0];

            CharacterProfileData[] profileEntries = _characterProfilesConfigJson != null
                ? JsonReader.Deserialize<CharacterProfileData[]>(_characterProfilesConfigJson.text)
                : new CharacterProfileData[0];

            PersonalityAffinityRuleData[] affinityRuleEntries = _personalityAffinityRulesConfigJson != null
                ? JsonReader.Deserialize<PersonalityAffinityRuleData[]>(_personalityAffinityRulesConfigJson.text)
                : new PersonalityAffinityRuleData[0];

            GreetingData[] greetingEntries = _greetingConfigJson != null
                ? JsonReader.Deserialize<GreetingData[]>(_greetingConfigJson.text)
                : new GreetingData[0];

            IdleChatTopicData[] idleChatTopicEntries = _idleChatConfigJson != null
                ? JsonReader.Deserialize<IdleChatTopicData[]>(_idleChatConfigJson.text)
                : new IdleChatTopicData[0];

            IdleChatAnswerData[] idleChatAnswerEntries = _idleChatAnswersConfigJson != null
                ? JsonReader.Deserialize<IdleChatAnswerData[]>(_idleChatAnswersConfigJson.text)
                : new IdleChatAnswerData[0];

            // Opening
            CharacterIntroData[] characterIntroEntries = _characterIntroConfigJson != null
                ? JsonReader.Deserialize<CharacterIntroData[]>(_characterIntroConfigJson.text)
                : new CharacterIntroData[0];

            CharacterIntroLineData[] characterIntroLineEntries = _characterIntroLinesConfigJson != null
                ? JsonReader.Deserialize<CharacterIntroLineData[]>(_characterIntroLinesConfigJson.text)
                : new CharacterIntroLineData[0];

            NodeDialogueLineData[] nodeDialogueEntries = _nodeDialogueConfigJson != null
                ? JsonReader.Deserialize<NodeDialogueLineData[]>(_nodeDialogueConfigJson.text)
                : new NodeDialogueLineData[0];

            // Combat（singleton 純陣列，取 array[0]）
            CombatConfigData combatConfigDto = null;
            if (_combatConfigJson != null)
            {
                CombatConfigData[] combatArr = JsonReader.Deserialize<CombatConfigData[]>(_combatConfigJson.text);
                if (combatArr != null && combatArr.Length > 0)
                    combatConfigDto = combatArr[0];
            }

            // StorageExpansionConfig 的初始容量（由 level=0 entry 推導）
            StorageExpansionConfig expansionConfigForCapacity = new StorageExpansionConfig(
                storageExpansionStageEntries, storageExpansionRequirementEntries);
            int storageInitialCapacity = expansionConfigForCapacity.InitialCapacity > 0
                ? expansionConfigForCapacity.InitialCapacity
                : StorageManager.DefaultInitialCapacity;

            // --- #1 CoreStorageInstaller ---
            // GameStaticDataManager — runtime IGameData 查詢的唯一入口（ADR-001）。
            // 各 Installer 的 TextAsset 資料在此批次已反序列化完畢；GameStaticDataManager 本身不需要
            // 重新載入，ctx.GameDataAccess delegate 僅供 Installer 依 id 查詢已注冊的 IGameData 使用。
            GameStaticDataManager gameStaticDataManager = new GameStaticDataManager();
            GameDataQuery<IGameData> gameDataAccess = (int id) => gameStaticDataManager.GetGameData<IGameData>(id);

            _coreStorageInstaller = new CoreStorageInstaller(20, 99, storageInitialCapacity, StorageManager.DefaultMaxStackValue);
            VillageContext ctx = new VillageContext(_villageCanvas, _uiContainer, gameDataAccess);
            _coreStorageInstaller.Install(ctx);
            _backpackManager = _coreStorageInstaller.BackpackManager;
            _storageManager  = _coreStorageInstaller.StorageManager;
            _transferManager = _coreStorageInstaller.StorageTransferManager;

            // --- #2 ProgressionInstaller ---
            _progressionInstaller = new ProgressionInstaller(
                mainQuestEntries, mainQuestUnlockEntries,
                initialResourceEntries,
                _backpackManager, _storageManager);
            _progressionInstaller.Install(ctx);
            _mainQuestManager       = _progressionInstaller.GetMainQuestManager();
            _mainQuestConfig        = new MainQuestConfig(mainQuestEntries, mainQuestUnlockEntries);
            _characterUnlockManager = _progressionInstaller.GetCharacterUnlockManager();
            _redDotManager          = _progressionInstaller.GetRedDotManager();
            _progressionManager     = _progressionInstaller.GetVillageProgressionManager();
            _progressionManager.ForceUnlock(CharacterIds.VillageChiefWife);
            _initialResourcesConfig    = new InitialResourcesConfig(initialResourceEntries);
            _initialResourceDispatcher = new InitialResourceDispatcher(_backpackManager, _storageManager);

            // --- #3 AffinityInstaller ---
            _affinityInstaller = new AffinityInstaller(affinityEntries);
            _affinityInstaller.Install(ctx);
            _affinityManager = _affinityInstaller.AffinityManager;
            _giftManager = new GiftManager(_affinityManager, _backpackManager, _storageManager);

            // --- #4 CGInstaller ---
            _cgInstaller = new CGInstaller(cgSceneEntries);
            _cgInstaller.Install(ctx);
            _cgSceneConfig  = _cgInstaller.CgSceneConfig;
            _cgUnlockManager = _cgInstaller.CgUnlockManager;
            if (_kgcDialogueViewPrefab != null && _villageCanvas != null)
                _hcgDialogueSetup = new HCGDialogueSetup(_kgcDialogueViewPrefab, _villageCanvas.transform);

            // --- #5 CommissionInstaller ---
            string[] allowedCommissionCharacters = new string[] { CharacterIds.Witch, CharacterIds.Guard };
            _commissionInstaller = new CommissionInstaller(
                commissionEntries,
                storageExpansionStageEntries, storageExpansionRequirementEntries,
                _backpackManager, _storageManager, allowedCommissionCharacters);
            _commissionInstaller.Install(ctx);
            _commissionManager      = _commissionInstaller.GetCommissionManager();
            _commissionRecipesConfig = new CommissionRecipesConfig(commissionEntries);

            // --- #6 DialogueFlowInstaller ---
            _dialogueFlowInstaller = new DialogueFlowInstaller(
                cqEntries, cqOptionEntries, profileEntries, affinityRuleEntries,
                greetingEntries,
                idleChatTopicEntries, idleChatAnswerEntries,
                _redDotManager, _characterQuestionCountdownSeconds, _dialogueCooldownBaseSeconds);
            _dialogueFlowInstaller.Install(ctx);
            _characterQuestionsManager        = _dialogueFlowInstaller.CharacterQuestionsManager;
            _characterQuestionCountdownManager = _dialogueFlowInstaller.CharacterQuestionCountdownManager;
            _greetingPresenter   = _dialogueFlowInstaller.GreetingPresenter;
            _idleChatPresenter   = _dialogueFlowInstaller.IdleChatPresenter;
            _dialogueCooldownManager = _dialogueFlowInstaller.DialogueCooldownManager;
            _staminaManager      = _dialogueFlowInstaller.StaminaManager;
            _characterQuestionsConfig = new CharacterQuestionsConfig(
                cqEntries, cqOptionEntries, profileEntries, affinityRuleEntries);

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

            // PlayerQuestionsConfig — ADR-002 A15 豁免（本 Sprint 暫不改）
            PlayerQuestionsConfigData questionsData = _playerQuestionsConfigJson != null
                ? JsonUtility.FromJson<PlayerQuestionsConfigData>(_playerQuestionsConfigJson.text)
                : new PlayerQuestionsConfigData { questions = new PlayerQuestionData[0] };
            _playerQuestionsConfig  = new PlayerQuestionsConfig(questionsData);
            _playerQuestionsManager = new PlayerQuestionsManager(_playerQuestionsConfig);

            // CharacterIntroConfig（兩個獨立陣列）
            _characterIntroConfig = new CharacterIntroConfig(characterIntroEntries, characterIntroLineEntries);

            // NodeDialogueConfig（純陣列）
            _nodeDialogueConfig = new NodeDialogueConfig(nodeDialogueEntries);

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
