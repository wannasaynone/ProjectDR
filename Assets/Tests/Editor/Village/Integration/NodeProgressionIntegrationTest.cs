// NodeProgressionIntegrationTest — Sprint 4 C1 端到端 Loop 整合測試。
// 驗證 TEST 2（節點 1 流程）與 TEST 3（節點 2 + 探索解鎖）：
//
// TEST 2：
//   MainQuestManager.NotifyCompletionSignal(DialogueEnd, FirstCharIntroComplete)
//   → T1 完成 → 玩家進村長夫人 → NodeDialogueController.PlayNode("node_1")
//   → VN 選項（剩下那位）→ 選擇後 CharacterUnlockedEvent + InitialResourceDispatcher
//
// TEST 3：
//   NotifyCompletionSignal(CommissionCount, <choice2_character|1>) 觸發 T3 完成（若 value 符合）
//   或直接 Complete T3 → CharacterUnlockManager 解鎖探索功能 → ExplorationFeatureUnlockedEvent

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village.Integration
{
    [TestFixture]
    public class NodeProgressionIntegrationTest
    {
        private BackpackManager _backpack;
        private StorageManager _storage;
        private DialogueManager _dialogueManager;
        private InitialResourcesConfig _resourcesConfig;
        private InitialResourceDispatcher _dispatcher;
        private CharacterUnlockManager _unlockManager;
        private NodeDialogueConfig _nodeConfig;
        private NodeDialogueController _nodeController;
        private MainQuestConfig _mainQuestConfig;
        private MainQuestManager _mainQuestManager;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            _backpack = new BackpackManager(20, 99);
            _storage = new StorageManager(100, 99);
            _dialogueManager = new DialogueManager();

            _resourcesConfig = BuildInitialResourcesConfig();
            _dispatcher = new InitialResourceDispatcher(_backpack, _storage);
            _unlockManager = new CharacterUnlockManager(_resourcesConfig, _dispatcher);

            _nodeConfig = BuildNodeDialogueConfig();
            _nodeController = new NodeDialogueController(_dialogueManager, _nodeConfig);

            _mainQuestConfig = BuildMainQuestConfig();
            _mainQuestManager = new MainQuestManager(_mainQuestConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _nodeController?.Dispose();
            _unlockManager?.Dispose();
            EventBus.ForceClearAll();
        }

        // ===== TEST 2：節點 1 流程 =====

        [Test]
        public void Node1_PlaysAfterFirstChoiceMade_AndUnlocksRemainingCharacter()
        {
            // 玩家先在節點 0 選擇農女
            EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });
            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.FarmGirl));
            Assert.IsFalse(_unlockManager.IsUnlocked(CharacterIds.Witch));

            // 節點 1 開始播放
            _nodeController.PlayNode(NodeDialogueController.NodeIdNode1);
            Assert.IsTrue(_nodeController.IsPlaying);

            // 玩家選擇「剩下那位」= 魔女
            _dialogueManager.SelectChoice(NodeDialogueBranchIds.Witch);

            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.Witch));
        }

        [Test]
        public void Node1_WitchFirstThenFarmGirl_UnlocksFarmGirl()
        {
            EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.Witch });
            _nodeController.PlayNode(NodeDialogueController.NodeIdNode1);
            _dialogueManager.SelectChoice(NodeDialogueBranchIds.FarmGirl);

            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.FarmGirl));
        }

        [Test]
        public void T1_NotifyCompletion_ViaDialogueEndSignal_CompletesT1()
        {
            // T0 先 auto 完成 → T1 變 Available
            _mainQuestManager.TryAutoCompleteFirstAutoQuest();
            Assert.AreEqual(MainQuestState.Available, _mainQuestManager.GetState("T1"));

            // 發送 dialogue_end + first_char_intro_complete 訊號
            var completed = _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd,
                MainQuestSignalValues.FirstCharIntroComplete);

            Assert.Contains("T1", (System.Collections.ICollection)completed);
            Assert.AreEqual(MainQuestState.Completed, _mainQuestManager.GetState("T1"));
            Assert.AreEqual(MainQuestState.Available, _mainQuestManager.GetState("T2"));
        }

        // ===== TEST 3：節點 2 + 探索解鎖 =====

        [Test]
        public void T3_Complete_UnlocksExplorationFeature()
        {
            // 先手動驅動 T0→T1→T2→T3 的線性推進
            _mainQuestManager.TryAutoCompleteFirstAutoQuest();  // T0
            _mainQuestManager.NotifyCompletionSignal(MainQuestCompletionTypes.DialogueEnd, MainQuestSignalValues.FirstCharIntroComplete); // T1
            // T2/T3 completion_condition_type = commission_count；因 T2 value 為 placeholder，
            // 本測試直接從 Available → InProgress → Completed 走 StartQuest / CompleteQuest
            _mainQuestManager.StartQuest("T2");
            _mainQuestManager.CompleteQuest("T2");
            _mainQuestManager.StartQuest("T3");
            _mainQuestManager.CompleteQuest("T3");

            // T3 完成 → CharacterUnlockManager 監聽 MainQuestCompletedEvent 解鎖探索
            Assert.IsTrue(_unlockManager.IsExplorationFeatureUnlocked);
        }

        [Test]
        public void T3_Complete_PublishesExplorationFeatureUnlockedEvent()
        {
            ExplorationFeatureUnlockedEvent received = null;
            Action<ExplorationFeatureUnlockedEvent> handler = (e) => received = e;
            EventBus.Subscribe(handler);
            try
            {
                _mainQuestManager.TryAutoCompleteFirstAutoQuest();
                _mainQuestManager.NotifyCompletionSignal(MainQuestCompletionTypes.DialogueEnd, MainQuestSignalValues.FirstCharIntroComplete);
                _mainQuestManager.StartQuest("T2");
                _mainQuestManager.CompleteQuest("T2");
                _mainQuestManager.StartQuest("T3");
                _mainQuestManager.CompleteQuest("T3");
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            Assert.IsNotNull(received);
        }

        [Test]
        public void Node2_PlayAndComplete_PublishesNodeDialogueCompleted()
        {
            NodeDialogueCompletedEvent received = null;
            Action<NodeDialogueCompletedEvent> handler = (e) => received = e;
            EventBus.Subscribe(handler);
            try
            {
                _nodeController.PlayNode(NodeDialogueController.NodeIdNode2);
                while (_dialogueManager.IsActive && _dialogueManager.Advance()) { }
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            Assert.IsNotNull(received);
            Assert.AreEqual(NodeDialogueController.NodeIdNode2, received.NodeId);
        }

        // ===== TEST 2-X：主線任務序列完整性 =====

        [Test]
        public void MainQuestSequence_T0ToT3_AllAvailableInOrder()
        {
            Assert.AreEqual(MainQuestState.Available, _mainQuestManager.GetState("T0"));

            _mainQuestManager.TryAutoCompleteFirstAutoQuest();
            Assert.AreEqual(MainQuestState.Completed, _mainQuestManager.GetState("T0"));
            Assert.AreEqual(MainQuestState.Available, _mainQuestManager.GetState("T1"));

            _mainQuestManager.NotifyCompletionSignal(MainQuestCompletionTypes.DialogueEnd, MainQuestSignalValues.FirstCharIntroComplete);
            Assert.AreEqual(MainQuestState.Completed, _mainQuestManager.GetState("T1"));
            Assert.AreEqual(MainQuestState.Available, _mainQuestManager.GetState("T2"));
        }

        // ===== Helpers =====

        private static InitialResourcesConfig BuildInitialResourcesConfig()
        {
            return new InitialResourcesConfig(new InitialResourcesConfigData
            {
                schema_version = 1,
                grants = new InitialResourceGrantData[]
                {
                    new InitialResourceGrantData { grant_id = "initial_backpack_node0", trigger_id = InitialResourcesTriggerIds.Node0Start, item_id = "", quantity = 0 },
                    new InitialResourceGrantData { grant_id = "unlock_farm_girl_seed", trigger_id = InitialResourcesTriggerIds.UnlockFarmGirl, item_id = "seed_tomato", quantity = 3 },
                    new InitialResourceGrantData { grant_id = "unlock_witch_herb", trigger_id = InitialResourcesTriggerIds.UnlockWitch, item_id = "herb_green", quantity = 3 },
                    new InitialResourceGrantData { grant_id = "unlock_guard_sword", trigger_id = InitialResourcesTriggerIds.GuardReturnEvent, item_id = "gift_sword_wooden", quantity = 1 },
                },
            });
        }

        private static NodeDialogueConfig BuildNodeDialogueConfig()
        {
            return new NodeDialogueConfig(new NodeDialogueConfigData
            {
                schema_version = 1,
                node_dialogue_lines = new NodeDialogueLineData[]
                {
                    new NodeDialogueLineData { line_id = "n0_1", node_id = "node_0", sequence = 1, text = "intro 0", line_type = "dialogue", choice_branch = "" },
                    new NodeDialogueLineData { line_id = "n0_c1", node_id = "node_0", sequence = 2, text = "選農女", line_type = "choice_option", choice_branch = "farm_girl" },
                    new NodeDialogueLineData { line_id = "n0_c2", node_id = "node_0", sequence = 3, text = "選魔女", line_type = "choice_option", choice_branch = "witch" },

                    new NodeDialogueLineData { line_id = "n1_1", node_id = "node_1", sequence = 1, text = "intro 1", line_type = "dialogue", choice_branch = "" },
                    new NodeDialogueLineData { line_id = "n1_c1", node_id = "node_1", sequence = 2, text = "剩下農女", line_type = "choice_option", choice_branch = "farm_girl" },
                    new NodeDialogueLineData { line_id = "n1_c2", node_id = "node_1", sequence = 3, text = "剩下魔女", line_type = "choice_option", choice_branch = "witch" },

                    new NodeDialogueLineData { line_id = "n2_1", node_id = "node_2", sequence = 1, text = "探索即將開啟", line_type = "dialogue", choice_branch = "" },
                    new NodeDialogueLineData { line_id = "n2_2", node_id = "node_2", sequence = 2, text = "小心森林", line_type = "dialogue", choice_branch = "" },
                },
            });
        }

        private static MainQuestConfig BuildMainQuestConfig()
        {
            return new MainQuestConfig(new MainQuestConfigData
            {
                schema_version = 1,
                main_quests = new MainQuestConfigEntry[]
                {
                    new MainQuestConfigEntry
                    {
                        quest_id = "T0",
                        display_name = "醒來",
                        description = "醒來",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.Auto,
                        completion_condition_value = MainQuestSignalValues.Node0DialogueComplete,
                        reward_grant_ids = "",
                        unlock_on_complete = "T1",
                        sort_order = 0,
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T1",
                        display_name = "先去認識她們",
                        description = "認識",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.DialogueEnd,
                        completion_condition_value = MainQuestSignalValues.FirstCharIntroComplete,
                        reward_grant_ids = "",
                        unlock_on_complete = "T2",
                        sort_order = 1,
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T2",
                        display_name = "幫她一次",
                        description = "委託",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.CommissionCount,
                        completion_condition_value = "choice1_character|1",
                        reward_grant_ids = "",
                        unlock_on_complete = "T3",
                        sort_order = 2,
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T3",
                        display_name = "再認識一個",
                        description = "再委託",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.CommissionCount,
                        completion_condition_value = "choice2_character|1",
                        reward_grant_ids = "",
                        unlock_on_complete = "T4",
                        sort_order = 3,
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T4",
                        display_name = "出去",
                        description = "探索",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.FirstExplore,
                        completion_condition_value = MainQuestSignalValues.GuardReturnEventComplete,
                        reward_grant_ids = "unlock_guard_sword",
                        unlock_on_complete = "",
                        sort_order = 4,
                    },
                },
            });
        }
    }
}
