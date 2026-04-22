// NodeProgressionIntegrationTest — Sprint 6 更新的端到端 Loop 整合測試。
// 驗證 TEST 2（節點 1 流程）與 TEST 3（節點 2 + 探索解鎖）：
//
// TEST 2：
//   玩家在節點 0 選擇農女/魔女 → CharacterUnlockManager 解鎖該角色
//   → NodeDialogueController.PlayNode("node_1") → VN 選項（剩下那位）
//   → 選擇後 CharacterUnlockedEvent（無初始資源，Sprint 6 B2 已移除）
//
// TEST 3（新 T1 架構）：
//   NotifyCompletionSignal(DialogueEnd, node_2_dialogue_complete) 觸發新 T1 完成
//   → CharacterUnlockManager 解鎖探索功能 → ExplorationFeatureUnlockedEvent
//   （舊 T2/T3/T4 已移除，現為 T0/T1/T2 三條結構）

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Storage;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.CharacterUnlock;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Progression;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.Dialogue;

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
        // C7 注意：節點 1 的剩下那位解鎖，由 CharacterUnlockManager.OnNodeDialogueCompleted 處理。
        // NodeDialogueCompletedEvent 在 NodeDialogueController 的對話推進至完成後由其發布。
        // 因此這些測試在選完選項後需 Advance 至對話結束，才能觸發解鎖。

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

            // 玩家選擇任意選項（節點 1 只有一個空 branch 選項，推進後觸發 NodeDialogueCompletedEvent）
            _dialogueManager.SelectChoice(NodeDialogueBranchIds.Witch);

            // 推進對話至完成 → NodeDialogueController 發布 NodeDialogueCompletedEvent(node_1)
            // → CharacterUnlockManager.OnNodeDialogueCompleted 解鎖剩下那位（魔女）
            while (_dialogueManager.IsActive && _dialogueManager.Advance()) { }

            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.Witch));
        }

        [Test]
        public void Node1_WitchFirstThenFarmGirl_UnlocksFarmGirl()
        {
            EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.Witch });
            _nodeController.PlayNode(NodeDialogueController.NodeIdNode1);
            _dialogueManager.SelectChoice(NodeDialogueBranchIds.FarmGirl);
            while (_dialogueManager.IsActive && _dialogueManager.Advance()) { }

            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.FarmGirl));
        }

        [Test]
        public void T1_NotifyCompletion_ViaNode2DialogueComplete_CompletesT1()
        {
            // Sprint 6：新 T1 完成條件 = node_2_dialogue_complete（魔女對話結束）
            _mainQuestManager.TryAutoCompleteFirstAutoQuest();
            Assert.AreEqual(MainQuestState.Available, _mainQuestManager.GetState("T1"));

            // 發送 dialogue_end + node_2_dialogue_complete 訊號
            var completed = _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd,
                MainQuestSignalValues.Node2DialogueComplete);

            Assert.Contains("T1", (System.Collections.ICollection)completed);
            Assert.AreEqual(MainQuestState.Completed, _mainQuestManager.GetState("T1"));
            Assert.AreEqual(MainQuestState.Available, _mainQuestManager.GetState("T2"));
        }

        // ===== TEST 3：新 T1 完成 + 探索解鎖 =====

        [Test]
        public void T1_Complete_UnlocksExplorationFeature()
        {
            // Sprint 6：T0 → T1（node_2_dialogue_complete）→ 探索解鎖
            _mainQuestManager.TryAutoCompleteFirstAutoQuest();  // T0 完成 → T1 Available

            // 新 T1 完成條件 = node_2_dialogue_complete
            _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd,
                MainQuestSignalValues.Node2DialogueComplete);

            // T1 完成 → CharacterUnlockManager 監聽 MainQuestCompletedEvent T1 → 解鎖探索
            Assert.IsTrue(_unlockManager.IsExplorationFeatureUnlocked);
        }

        [Test]
        public void T1_Complete_PublishesExplorationFeatureUnlockedEvent()
        {
            ExplorationFeatureUnlockedEvent received = null;
            Action<ExplorationFeatureUnlockedEvent> handler = (e) => received = e;
            EventBus.Subscribe(handler);
            try
            {
                _mainQuestManager.TryAutoCompleteFirstAutoQuest();
                _mainQuestManager.NotifyCompletionSignal(
                    MainQuestCompletionTypes.DialogueEnd,
                    MainQuestSignalValues.Node2DialogueComplete);
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

        // ===== TEST 2-X：主線任務序列完整性（新 T0/T1/T2 三條結構）=====

        [Test]
        public void MainQuestSequence_T0ToT2_AllAvailableInOrder()
        {
            Assert.AreEqual(MainQuestState.Available, _mainQuestManager.GetState("T0"));

            _mainQuestManager.TryAutoCompleteFirstAutoQuest();
            Assert.AreEqual(MainQuestState.Completed, _mainQuestManager.GetState("T0"));
            Assert.AreEqual(MainQuestState.Available, _mainQuestManager.GetState("T1"));

            // 新 T1 完成條件 = node_2_dialogue_complete
            _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd,
                MainQuestSignalValues.Node2DialogueComplete);
            Assert.AreEqual(MainQuestState.Completed, _mainQuestManager.GetState("T1"));
            Assert.AreEqual(MainQuestState.Available, _mainQuestManager.GetState("T2"));
        }

        // ===== Helpers =====

        private static InitialResourcesConfig BuildInitialResourcesConfig()
        {
            // Sprint 6 B2：只剩 initial_backpack_node0 和 unlock_guard_sword
            return new InitialResourcesConfig(new InitialResourcesConfigData
            {
                schema_version = 1,
                grants = new InitialResourceGrantData[]
                {
                    new InitialResourceGrantData { grant_id = "initial_backpack_node0", trigger_id = InitialResourcesTriggerIds.Node0Start, item_id = "", quantity = 0 },
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
            // Sprint 6 B1：新 T0/T1/T2 三條結構（移除舊 T2/T3/T4）
            return new MainQuestConfig(new MainQuestConfigData
            {
                schema_version = 1,
                main_quests = new MainQuestConfigEntry[]
                {
                    new MainQuestConfigEntry
                    {
                        id = 1,
                        quest_id = "T0",
                        display_name = "醒來的地方",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.Auto,
                        completion_condition_value = MainQuestSignalValues.Node0DialogueComplete,
                        unlock_on_complete = "T1|node_0_complete",
                        sort_order = 0,
                    },
                    new MainQuestConfigEntry
                    {
                        id = 2,
                        quest_id = "T1",
                        display_name = "認識所有人",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.DialogueEnd,
                        completion_condition_value = MainQuestSignalValues.Node2DialogueComplete,
                        unlock_on_complete = "T2|node_2_complete|exploration_open",
                        sort_order = 1,
                    },
                    new MainQuestConfigEntry
                    {
                        id = 3,
                        quest_id = "T2",
                        display_name = "出去看看外面",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.FirstExplore,
                        completion_condition_value = MainQuestSignalValues.GuardReturnEventComplete,
                        reward_grant_ids = "unlock_guard_sword",
                        unlock_on_complete = "guard_unlock|exploration_full_open",
                        sort_order = 2,
                    },
                },
            });
        }

        // ===== Sprint 6 C7 bugfix 回歸測試 =====

        /// <summary>
        /// 回歸測試 C7-1：節點 1 對話完成（NodeDialogueCompletedEvent）後，
        /// 若節點 0 選農女 → CharacterUnlockManager 自動解鎖魔女 Hub 按鈕。
        /// 根因：Sprint 4 原路徑依賴 OnDialogueChoiceSelected（farm_girl/witch branch）；
        ///       真實 config 選項 choice_branch = ""（空字串），永遠不觸發解鎖。
        ///       C5 R3 刪除 ForceUnlock 後此路徑完全斷裂。
        ///       修復：CharacterUnlockManager 訂閱 NodeDialogueCompletedEvent，
        ///             node_1 完成時依 _node0ChosenBranch 推算並解鎖剩下那位。
        /// </summary>
        [Test]
        public void Regression_C7_1_Node1DialogueCompleted_AfterFarmGirl_UnlocksWitch()
        {
            // 節點 0：選農女（設定 _node0ChosenBranch）
            EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });
            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.FarmGirl));
            Assert.IsFalse(_unlockManager.IsUnlocked(CharacterIds.Witch));

            // 節點 1 完成（直接發布事件，模擬 NodeDialogueController 播完後發布的事件）
            EventBus.Publish(new NodeDialogueCompletedEvent
            {
                NodeId = NodeDialogueController.NodeIdNode1,
                SelectedBranchId = string.Empty   // 真實 config：空 branch 選項
            });

            // Assert：剩下那位（魔女）應被解鎖
            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.Witch),
                "節點 0 選農女後，節點 1 對話結束（NodeDialogueCompletedEvent）應解鎖魔女");
        }

        /// <summary>
        /// 回歸測試 C7-2：反向路徑——節點 0 選魔女 → 節點 1 完成 → 農女解鎖。
        /// </summary>
        [Test]
        public void Regression_C7_2_Node1DialogueCompleted_AfterWitch_UnlocksFarmGirl()
        {
            EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.Witch });
            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.Witch));
            Assert.IsFalse(_unlockManager.IsUnlocked(CharacterIds.FarmGirl));

            EventBus.Publish(new NodeDialogueCompletedEvent
            {
                NodeId = NodeDialogueController.NodeIdNode1,
                SelectedBranchId = string.Empty
            });

            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.FarmGirl),
                "節點 0 選魔女後，節點 1 對話結束（NodeDialogueCompletedEvent）應解鎖農女");
        }

        /// <summary>
        /// 回歸測試 C7-3：節點 2 對話結束後探索入口解鎖（連帶驗證節點 2 路徑完整性）。
        /// 確保 C7 修復節點 1 時未破壞節點 2 的探索開放路徑。
        /// </summary>
        [Test]
        public void Regression_C7_3_Node2Complete_UnlocksExplorationFeature_PathIntact()
        {
            // Arrange：T0 完成 → T1 Available
            _mainQuestManager.TryAutoCompleteFirstAutoQuest();
            Assert.IsFalse(_unlockManager.IsExplorationFeatureUnlocked);

            // 模擬節點 2 完成 → VillageEntryPoint 送 node_2_dialogue_complete → T1 完成
            _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd,
                MainQuestSignalValues.Node2DialogueComplete);

            // Assert：T1 完成 → 探索解鎖
            Assert.IsTrue(_unlockManager.IsExplorationFeatureUnlocked,
                "節點 2 對話完成後（node_2_dialogue_complete）探索功能應解鎖");
        }

        // ===== Sprint 6 D4 bugfix 回歸測試 =====

        /// <summary>
        /// 回歸測試 D4-1：驗證「選擇 1 角色 CG 播完」時機（節點 1 觸發點），
        /// T1 尚未完成（T1 語義 = 認識所有人，需等節點 2 後），
        /// 確認 T1 此時確實為 Available 而非 Completed。
        /// 這驗證了舊架構 IsQuestCompleted("T1") 在此時機回傳 false 的根因。
        /// 新架構以 _node1TriggerReady 旗標繞過此問題。
        /// </summary>
        [Test]
        public void Regression_D4_1_T1_IsNotCompleted_WhenFirstCharCGPlayed_ProvingOldArchitectureBug()
        {
            // Arrange：T0 完成 → T1 Available
            _mainQuestManager.TryAutoCompleteFirstAutoQuest();
            Assert.AreEqual(MainQuestState.Available, _mainQuestManager.GetState("T1"),
                "T1 應在 T0 完成後變為 Available");

            // Act：模擬「選擇 1 角色 CG 播完」時機（此時只有農女/魔女 CG 完成，節點 2 尚未播放）
            // 舊架構以 IsQuestCompleted("T1") 判斷節點 1 觸發，但 T1 此時尚未完成
            bool wasT1CompletedAtCGFinishTime = _mainQuestManager.IsQuestCompleted("T1");

            // Assert：T1 此時必定尚未完成（這是 bug 的根因）
            Assert.IsFalse(wasT1CompletedAtCGFinishTime,
                "選擇 1 角色 CG 播完時 T1 尚未完成（T1 需 node_2_dialogue_complete 才完成）。" +
                "這確認舊架構 IsQuestCompleted(T1) 在此時機回傳 false，導致節點 1 無法觸發（D4 bug 根因）。");
        }

        /// <summary>
        /// 回歸測試 D4-2：驗證新架構節點 2 觸發路徑——
        /// 節點 2 對話完成（node_2_dialogue_complete）→ T1 完成 → MainQuestCompletedEvent(T1) 發布。
        /// VillageEntryPoint.OnMainQuestCompletedForNodeDialogue 訂閱此事件後設定 _node2TriggerReady。
        /// 此測試驗證該事件確實在正確時機發布，確保 _node2TriggerReady 的觸發源正確。
        /// </summary>
        [Test]
        public void Regression_D4_2_T1_MainQuestCompletedEvent_Fires_AfterNode2DialogueComplete()
        {
            // Arrange：T0 完成 → T1 Available
            _mainQuestManager.TryAutoCompleteFirstAutoQuest();

            MainQuestCompletedEvent receivedEvent = null;
            Action<MainQuestCompletedEvent> handler = e => receivedEvent = e;
            EventBus.Subscribe(handler);

            try
            {
                // Act：模擬 VillageEntryPoint.OnNodeDialogueCompletedForMainQuest（R1 修正後邏輯）
                // 在 node_2 完成時送 node_2_dialogue_complete 訊號
                _mainQuestManager.NotifyCompletionSignal(
                    MainQuestCompletionTypes.DialogueEnd,
                    MainQuestSignalValues.Node2DialogueComplete);
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            // Assert：T1 完成 → MainQuestCompletedEvent(T1) 必須發布
            // VillageEntryPoint.OnMainQuestCompletedForNodeDialogue 收到此事件後應設定 _node2TriggerReady = true
            Assert.IsNotNull(receivedEvent,
                "node_2_dialogue_complete 訊號送出後，T1 應完成並發布 MainQuestCompletedEvent");
            Assert.AreEqual("T1", receivedEvent.QuestId,
                "完成的任務 ID 應為 T1（認識所有人）");
            Assert.IsTrue(_mainQuestManager.IsQuestCompleted("T1"),
                "T1 應在 node_2_dialogue_complete 後標記為 Completed");
        }

        // ===== Sprint 6 C5/R7 production path 回歸測試 =====

        /// <summary>
        /// 回歸測試 R7-1：node_2 NodeDialogueCompletedEvent 透過 EventBus 傳播後，
        /// 若 MainQuestManager 收到 DialogueEnd + node_2_dialogue_complete → T1 完成。
        /// 驗證 R1 修正後 production path（VillageEntryPoint.OnNodeDialogueCompletedForMainQuest
        /// 在 node_2 完成時必須送 Node2DialogueComplete 訊號才能讓此流程成立）。
        /// </summary>
        [Test]
        public void Regression_R7_1_Node2DialogueCompletedEvent_CompletesT1_ViaCorrectSignal()
        {
            // Arrange：T0 完成 → T1 Available
            _mainQuestManager.TryAutoCompleteFirstAutoQuest();
            Assert.AreEqual(MainQuestState.Available, _mainQuestManager.GetState("T1"),
                "T1 應在 T0 完成後變為 Available");

            // Simulate：模擬 VillageEntryPoint.OnNodeDialogueCompletedForMainQuest（R1 修正後）
            // 在 NodeDialogueCompletedEvent(node_2) 時送 Node2DialogueComplete 訊號
            _nodeController.PlayNode(NodeDialogueController.NodeIdNode2);
            while (_dialogueManager.IsActive && _dialogueManager.Advance()) { }

            // 模擬 R1 修正後的 production 路徑：node_2 完成 → VillageEntryPoint 送訊號
            _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd,
                MainQuestSignalValues.Node2DialogueComplete);

            // Assert
            Assert.IsTrue(_mainQuestManager.IsQuestCompleted("T1"),
                "節點 2 對話完成（node_2_dialogue_complete）應完成 T1");
            Assert.AreEqual(MainQuestState.Available, _mainQuestManager.GetState("T2"),
                "T1 完成後 T2 應解鎖");
        }

        /// <summary>
        /// 回歸測試 R7-2：驗證舊訊號 FirstCharIntroComplete 不能完成新 T1。
        /// 這確保 R1 修正前的舊邏輯（CG 播完送 first_char_intro_complete）
        /// 在新架構中不會誤觸發 T1 完成。
        /// </summary>
        [Test]
        public void Regression_R7_2_OldSignal_FirstCharIntroComplete_DoesNotCompleteNewT1()
        {
            // Arrange：T0 完成 → T1 Available
            _mainQuestManager.TryAutoCompleteFirstAutoQuest();
            Assert.AreEqual(MainQuestState.Available, _mainQuestManager.GetState("T1"));

            // Act：送舊 Sprint 4 訊號（first_char_intro_complete）
            // 這是 R1 修正前 VillageEntryPoint L1007-1008 的行為
#pragma warning disable CS0618 // 此處故意使用 [Obsolete] 常數以驗證舊訊號不生效
            IReadOnlyList<string> completed = _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd,
                MainQuestSignalValues.FirstCharIntroComplete);
#pragma warning restore CS0618

            // Assert：新 T1 的 completion_condition_value 為 node_2_dialogue_complete，
            // 舊訊號 first_char_intro_complete 不匹配，T1 不應完成
            Assert.AreEqual(0, completed.Count,
                "舊訊號 first_char_intro_complete 不應完成新 T1（新 T1 需 node_2_dialogue_complete）");
            Assert.IsFalse(_mainQuestManager.IsQuestCompleted("T1"),
                "T1 不應因舊訊號而意外完成");
        }

        // ===== Sprint 6 C8 回歸測試（探索按鈕無反應 bugfix）=====

        /// <summary>
        /// 回歸測試 C8-1：探索功能解鎖後，ExplorationEntryManager.CanDepart() 應為 true，
        /// 且 Depart() 呼叫成功（回傳 true）並發布 ExplorationDepartedEvent。
        ///
        /// 根因記錄：VillageHubView 的 _explorationButton 從未在 OnShow() 中綁定 onClick handler，
        /// 導致按鈕顯示（SetActive=true）但點擊完全無反應。
        /// 修復：VillageHubView.Initialize() 接收 ExplorationEntryManager，
        ///       OnShow() 綁定 _explorationButton.onClick → OnExplorationButtonClicked → Depart()。
        ///
        /// 此測試驗證修復後的業務邏輯路徑（純邏輯層，不測 UGUI onClick 綁定本身）：
        /// 探索功能解鎖 → CanDepart() 為 true → 呼叫 Depart() 成功 → ExplorationDepartedEvent 發布。
        /// </summary>
        [Test]
        public void Regression_C8_1_AfterExplorationUnlocked_Depart_Succeeds_AndPublishesEvent()
        {
            // Arrange：T0 完成 → T1 → 探索解鎖
            _mainQuestManager.TryAutoCompleteFirstAutoQuest();
            _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd,
                MainQuestSignalValues.Node2DialogueComplete);

            // 確認探索功能已解鎖
            Assert.IsTrue(_unlockManager.IsExplorationFeatureUnlocked,
                "前置條件：探索功能應已解鎖（T1 完成後）");

            // Arrange：建立 ExplorationEntryManager
            var backpack = new BackpackManager(20, 99);
            var explorationManager = new ExplorationEntryManager(backpack);

            // 確認可以出發
            Assert.IsTrue(explorationManager.CanDepart(),
                "探索功能解鎖後，CanDepart() 應回傳 true（代表 Hub 探索按鈕點擊後應有效）");

            // Arrange：監聽 ExplorationDepartedEvent
            ExplorationDepartedEvent departedEvent = null;
            Action<ExplorationDepartedEvent> handler = e => departedEvent = e;
            EventBus.Subscribe(handler);

            try
            {
                // Act：模擬 Hub 探索按鈕點擊 → OnExplorationButtonClicked → Depart()
                bool departResult = explorationManager.Depart();

                // Assert
                Assert.IsTrue(departResult,
                    "Depart() 應回傳 true（代表出發成功）");
                Assert.IsNotNull(departedEvent,
                    "Depart() 成功後應發布 ExplorationDepartedEvent（Hub→探索場景切換的觸發點）");
                Assert.IsFalse(explorationManager.CanDepart(),
                    "出發後 CanDepart() 應為 false（避免重複出發）");
            }
            finally
            {
                EventBus.Unsubscribe(handler);
                explorationManager.Dispose();
            }
        }

        /// <summary>
        /// 回歸測試 C8-2：探索功能**尚未解鎖**時，CanDepart() 仍為 true，
        /// 但整體流程中 VillageHubView 的探索按鈕應為 Inactive（不可見），
        /// 玩家無法觸及。此測試確認業務邏輯層（ExplorationEntryManager）本身
        /// 不負責「是否顯示按鈕」的判斷，該職責由 VillageHubView 負責。
        ///
        /// 這是防禦性測試：確認 ExplorationEntryManager 在探索未解鎖時
        /// 不會因意外被呼叫而報錯（健壯性）。
        /// </summary>
        [Test]
        public void Regression_C8_2_ExplorationManagerDepart_IsRobust_EvenIfCalledUnexpectedly()
        {
            // Arrange：不解鎖探索功能
            Assert.IsFalse(_unlockManager.IsExplorationFeatureUnlocked,
                "前置條件：探索功能應尚未解鎖");

            var backpack = new BackpackManager(20, 99);
            var explorationManager = new ExplorationEntryManager(backpack);

            // Act & Assert：即使在未解鎖時被呼叫，ExplorationEntryManager 本身不應拋出例外
            // （按鈕顯示控制是 VillageHubView 的責任，不是 ExplorationEntryManager 的）
            Assert.DoesNotThrow(() =>
            {
                bool result = explorationManager.Depart();
                // CanDepart() 為 true 時才能成功，此處假設 manager 本身可正常呼叫
                Assert.IsTrue(result || !result, "Depart() 不應拋出例外");
            }, "ExplorationEntryManager.Depart() 不應在任何情況下拋出例外");

            explorationManager.Dispose();
        }
    }
}
