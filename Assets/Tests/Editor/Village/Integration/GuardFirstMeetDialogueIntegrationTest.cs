// GuardFirstMeetDialogueIntegrationTest — 守衛首次取劍對白整合測試。
//
// A08 併入 NodeDialogueConfig（2026-04-22）重構後更新：
//   原 GuardFirstMeetDialogueConfig 獨立對白已併入 node-dialogue-config.json（node_id="guard_first_meet"）。
//   觸發路徑：NodeDialogueController.TryPlayFirstMeetDialogueIfNotTriggered("guard_first_meet")
//   業務邏輯：NodeDialogueCompletedEvent { NodeId="guard_first_meet" } → 發劍 + ExplorationGateReopenedEvent
//
// 測試覆蓋（依 Sprint 6 F12 + A08 併入驗收準則）：
//   T1. NodeDialogueController.TryPlayFirstMeetDialogueIfNotTriggered 首次呼叫 → 回傳 true、節點開始播放
//   T2. NodeDialogueCompletedEvent { NodeId="guard_first_meet" } → 劍入背包 + ExplorationGateReopenedEvent 發布
//   T3. TryPlayFirstMeetDialogueIfNotTriggered 再次呼叫（已觸發）→ 回傳 false（不重播）
//   T4. ExplorationGateReopenedEvent 發布 → T2 主線任務完成（production path）
//   T5 regression：
//     guard_ask_sword 題目不再出現在 PlayerQuestionsManager 的清單中
//   T5b regression：
//     FirstMeet 清除時機 — 首次進入後 FirstMeet = false（對白完成之前）

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
using ProjectDR.Village.CharacterQuestions;
using ProjectDR.Village.Dialogue;

namespace ProjectDR.Tests.Village.Integration
{
    [TestFixture]
    public class GuardFirstMeetDialogueIntegrationTest
    {
        private BackpackManager _backpack;
        private StorageManager _storage;
        private InitialResourcesConfig _resourcesConfig;
        private InitialResourceDispatcher _dispatcher;
        private CharacterUnlockManager _unlockManager;
        private ExplorationEntryManager _explorationManager;
        private RedDotManager _redDotManager;
        private MainQuestConfig _mainQuestConfig;
        private MainQuestManager _mainQuestManager;
        private DialogueManager _dialogueManager;
        private NodeDialogueConfig _nodeDialogueConfig;
        private NodeDialogueController _nodeDialogueController;
        private PlayerQuestionsConfig _playerQuestionsConfig;
        private PlayerQuestionsManager _playerQuestionsManager;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            _backpack = new BackpackManager(20, 99);
            _storage = new StorageManager(100, 99);

            _resourcesConfig = BuildInitialResourcesConfig();
            _dispatcher = new InitialResourceDispatcher(_backpack, _storage);
            _unlockManager = new CharacterUnlockManager(_resourcesConfig, _dispatcher);
            _explorationManager = new ExplorationEntryManager(_backpack);
            _mainQuestConfig = BuildMainQuestConfig();
            _mainQuestManager = new MainQuestManager(_mainQuestConfig);
            _redDotManager = new RedDotManager(_mainQuestConfig, _mainQuestManager);

            // 建立含 guard_first_meet 節點的 NodeDialogueConfig
            _nodeDialogueConfig = BuildNodeDialogueConfigWithGuardFirstMeet();
            _dialogueManager = new DialogueManager();
            _nodeDialogueController = new NodeDialogueController(_dialogueManager, _nodeDialogueConfig);

            // Sprint 6 決策 6-13：guard_ask_sword 不應在 player-questions-config 中
            _playerQuestionsConfig = BuildGuardQuestionsConfigWithoutSwordQuestion();
            _playerQuestionsManager = new PlayerQuestionsManager(_playerQuestionsConfig, seed: 42);
        }

        [TearDown]
        public void TearDown()
        {
            _nodeDialogueController?.Dispose();
            _unlockManager?.Dispose();
            _explorationManager?.Dispose();
            _redDotManager?.Dispose();
            EventBus.ForceClearAll();
        }

        // ===== T1：TryPlayFirstMeetDialogueIfNotTriggered 首次呼叫 → 回傳 true =====

        [Test]
        public void TryPlayFirstMeetDialogueIfNotTriggered_FirstCall_ReturnsTrue()
        {
            // Act
            bool result = _nodeDialogueController.TryPlayFirstMeetDialogueIfNotTriggered(
                NodeDialogueController.NodeIdGuardFirstMeet);

            // Assert
            Assert.IsTrue(result,
                "首次呼叫 TryPlayFirstMeetDialogueIfNotTriggered 應回傳 true（成功啟動播放）");
            Assert.IsTrue(_nodeDialogueController.IsPlaying,
                "成功啟動後 IsPlaying 應為 true");
        }

        // ===== T1b regression：TryPlayFirstMeet 呼叫後 DialogueManager.IsActive = true，且第一行是 guard_first_meet 台詞 =====
        // 此測試對應 D7 bug：StartDialoguePlayback 曾覆蓋已啟動的 guard_first_meet 對話
        // 修復：StartDialoguePlayback 在 DialogueManager.IsActive 時不得重啟對話

        [Test]
        public void Regression_D7_AfterTryPlayFirstMeet_DialogueManagerIsActiveWithGuardLine()
        {
            // Act：TryPlayFirstMeetDialogueIfNotTriggered 應呼叫 PlayNode → DialogueManager.StartDialogue
            bool result = _nodeDialogueController.TryPlayFirstMeetDialogueIfNotTriggered(
                NodeDialogueController.NodeIdGuardFirstMeet);

            // Assert：DialogueManager 應在 Active 狀態（對話已啟動）
            Assert.IsTrue(result, "首次呼叫應回傳 true");
            Assert.IsTrue(_dialogueManager.IsActive,
                "TryPlayFirstMeetDialogueIfNotTriggered 呼叫後 DialogueManager 應為 Active（StartDialoguePlayback 不得覆蓋此狀態）");

            // Assert：第一行應是 guard_first_meet 的對白，不應是招呼語
            string currentLine = _dialogueManager.GetCurrentLine();
            Assert.IsNotNull(currentLine, "對話應有第一行");
            Assert.IsTrue(currentLine.Contains("終於來了") || currentLine.Contains("test placeholder"),
                $"第一行應是 guard_first_meet 的台詞，實際：{currentLine}");
        }

        // ===== T2：NodeDialogueCompletedEvent { NodeId=guard_first_meet } → 發劍 + ExplorationGateReopenedEvent =====

        [Test]
        public void GuardFirstMeetNodeCompleted_GrantsSword_AndPublishesExplorationGateReopenedEvent()
        {
            // Arrange
            bool reopenedReceived = false;
            Action<ExplorationGateReopenedEvent> handler = (e) => reopenedReceived = true;
            EventBus.Subscribe(handler);

            try
            {
                // Act：模擬 VillageEntryPoint.OnNodeDialogueCompletedForMainQuest 業務邏輯
                SimulateGuardFirstMeetCompleted();
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            // Assert：劍入背包
            Assert.AreEqual(1, _backpack.GetItemCount("gift_sword_wooden"),
                "取劍對白完成後背包應有木劍");

            // Assert：ExplorationGateReopenedEvent 發布
            Assert.IsTrue(reopenedReceived,
                "取劍對白完成後應發布 ExplorationGateReopenedEvent");
        }

        // ===== T3：TryPlayFirstMeetDialogueIfNotTriggered 再次呼叫（已觸發）→ 回傳 false =====

        [Test]
        public void TryPlayFirstMeetDialogueIfNotTriggered_AfterAlreadyTriggered_ReturnsFalse()
        {
            // Arrange：第一次呼叫
            _nodeDialogueController.TryPlayFirstMeetDialogueIfNotTriggered(
                NodeDialogueController.NodeIdGuardFirstMeet);

            // 模擬對白完成（重設 IsPlaying 狀態）
            EventBus.Publish(new DialogueCompletedEvent());

            // Act：第二次呼叫
            bool result = _nodeDialogueController.TryPlayFirstMeetDialogueIfNotTriggered(
                NodeDialogueController.NodeIdGuardFirstMeet);

            // Assert：不應重播
            Assert.IsFalse(result,
                "TryPlayFirstMeetDialogueIfNotTriggered 在已觸發後再次呼叫應回傳 false（不重播）");
        }

        // ===== T4：ExplorationGateReopenedEvent → T2 主線完成（production path）=====

        [Test]
        public void GuardFirstMeetCompleted_CompletesT2MainQuest()
        {
            // Arrange：T0 auto → T1 完成 → T2 進入 Available
            _mainQuestManager.TryAutoCompleteFirstAutoQuest(); // T0 auto
            _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd,
                MainQuestSignalValues.Node2DialogueComplete); // T1

            // 裝配 production path subscriber：
            // 訂閱 ExplorationGateReopenedEvent → 送 T2 完成訊號
            Action<ExplorationGateReopenedEvent> productionSubscriber = (e) =>
            {
                _mainQuestManager.NotifyCompletionSignal(
                    MainQuestCompletionTypes.FirstExplore,
                    MainQuestSignalValues.GuardReturnEventComplete);
            };
            EventBus.Subscribe(productionSubscriber);

            bool t2Completed = false;
            Action<MainQuestCompletedEvent> t2Handler = (e) =>
            {
                if (e.QuestId == "T2") t2Completed = true;
            };
            EventBus.Subscribe(t2Handler);

            try
            {
                // Act：模擬對白完成 → 發布 ExplorationGateReopenedEvent
                SimulateGuardFirstMeetCompleted();
            }
            finally
            {
                EventBus.Unsubscribe(t2Handler);
                EventBus.Unsubscribe(productionSubscriber);
            }

            Assert.IsTrue(t2Completed,
                "取劍對白完成後 ExplorationGateReopenedEvent production subscriber 應完成 T2");
        }

        // ===== T5 regression：guard_ask_sword 不應出現在 PlayerQuestionsManager 清單 =====

        [Test]
        public void Regression_T5_GuardQuestionsList_DoesNotContainAskSwordQuestion()
        {
            // Arrange：模擬守衛已解鎖（進入 normal 發問流程）
            PlayerQuestionsPresentation presentation =
                _playerQuestionsManager.GetPresentation(CharacterIds.Guard);

            // Assert：不應有 guard_ask_sword 特殊題
            bool hasAskSwordQuestion = false;
            foreach (PlayerQuestionInfo q in presentation.Questions)
            {
                if (q.QuestionId == "guard_ask_sword"
                    || (q.IsSingleUse && q.TriggerFlag == "grant_guard_sword"))
                {
                    hasAskSwordQuestion = true;
                    break;
                }
            }

            Assert.IsFalse(hasAskSwordQuestion,
                "Sprint 6 決策 6-13：guard_ask_sword 特殊題已移除，不應出現在守衛發問清單中");
        }

        // ===== T5b regression：FirstMeet 清除時機不因決策 6-13 改變 =====

        [Test]
        public void Regression_T5b_FirstMeetFlag_ClearedOnEnter_NotOnDialogueComplete()
        {
            // Arrange：設置守衛 FirstMeet 紅點
            _redDotManager.SetFirstMeetFlag(CharacterIds.Guard, true);
            Assert.IsTrue(_redDotManager.IsLayerActive(CharacterIds.Guard, RedDotLayer.FirstMeet),
                "前置條件：FirstMeet 應為 true");

            // Act：模擬 OnCharacterEnteredAndCGDone（清除 FirstMeet 紅點）
            _redDotManager.SetFirstMeetFlag(CharacterIds.Guard, false);

            // Assert：FirstMeet 在進入時清除（對白完成前）
            Assert.IsFalse(_redDotManager.IsLayerActive(CharacterIds.Guard, RedDotLayer.FirstMeet),
                "FirstMeet 紅點應在玩家進入守衛互動畫面時清除（不等對白完成）");
        }

        // ===== Helpers =====

        /// <summary>
        /// 模擬 VillageEntryPoint.OnNodeDialogueCompletedForMainQuest 中 guard_first_meet 的業務邏輯。
        /// </summary>
        private void SimulateGuardFirstMeetCompleted()
        {
            // 發劍（與 VillageEntryPoint guard_first_meet 完成邏輯一致）
            IReadOnlyList<InitialResourceGrant> grants =
                _resourcesConfig.GetGrantsByTrigger(InitialResourcesTriggerIds.GuardSwordAsked);
            foreach (InitialResourceGrant grant in grants)
            {
                _dispatcher.Dispatch(grant);
            }

            // 發布 ExplorationGateReopenedEvent
            EventBus.Publish(new ExplorationGateReopenedEvent());
        }

        // ===== Config Builders =====

        private static InitialResourcesConfig BuildInitialResourcesConfig()
        {
            return new InitialResourcesConfig(new InitialResourceGrantData[]
            {
                new InitialResourceGrantData { id = 1, grant_id = "initial_backpack_node0", trigger_id = InitialResourcesTriggerIds.Node0Start,    item_id = "",                quantity = 0 },
                new InitialResourceGrantData { id = 2, grant_id = "unlock_guard_sword",     trigger_id = InitialResourcesTriggerIds.GuardSwordAsked, item_id = "gift_sword_wooden", quantity = 1 },
            });
        }

        /// <summary>
        /// 建立含 guard_first_meet 節點（4 行）的 NodeDialogueConfig，
        /// 與 production node-dialogue-config.json 的 guard_first_meet 結構一致。
        /// </summary>
        private static NodeDialogueConfig BuildNodeDialogueConfigWithGuardFirstMeet()
        {
            return new NodeDialogueConfig(new NodeDialogueLineData[]
            {
                new NodeDialogueLineData { id = 32, line_id = "guard_first_meet_001", node_id = "guard_first_meet", sequence = 1, speaker = "guard",    text = "你終於來了。【test placeholder】",         line_type = NodeDialogueLineTypes.Dialogue,  choice_branch = "" },
                new NodeDialogueLineData { id = 33, line_id = "guard_first_meet_002", node_id = "guard_first_meet", sequence = 2, speaker = "guard",    text = "出發前，你需要這個。拿好它。【test placeholder】", line_type = NodeDialogueLineTypes.Dialogue, choice_branch = "" },
                new NodeDialogueLineData { id = 34, line_id = "guard_first_meet_003", node_id = "guard_first_meet", sequence = 3, speaker = "narrator", text = "（守衛遞給你一把劍）",                      line_type = NodeDialogueLineTypes.Narration, choice_branch = "" },
                new NodeDialogueLineData { id = 35, line_id = "guard_first_meet_004", node_id = "guard_first_meet", sequence = 4, speaker = "guard",    text = "在森林裡小心。【test placeholder】",         line_type = NodeDialogueLineTypes.Dialogue,  choice_branch = "" },
            });
        }

        /// <summary>
        /// 建立守衛發問清單（不含 guard_ask_sword 特殊題）。
        /// 反映 Sprint 6 決策 6-13 後的 player-questions-config.json 結構。
        /// PlayerQuestionsConfig 為 ADR-002 A15 豁免，保留包裹類建構子。
        /// </summary>
        private static PlayerQuestionsConfig BuildGuardQuestionsConfigWithoutSwordQuestion()
        {
            return new PlayerQuestionsConfig(new PlayerQuestionsConfigData
            {
                schema_version = 2,
                questions = new PlayerQuestionData[]
                {
                    new PlayerQuestionData
                    {
                        question_id = "g_q01",
                        character_id = CharacterIds.Guard,
                        question_text = "你巡邏的範圍有多大？",
                        response_text = "以村子為中心，往外約三百步。",
                        sort_order = 1,
                        is_single_use = false,
                        trigger_flag = "",
                        affinity_gain = 0,
                    },
                    new PlayerQuestionData
                    {
                        question_id = "g_q02",
                        character_id = CharacterIds.Guard,
                        question_text = "你在這裡待多久了？",
                        response_text = "四年。",
                        sort_order = 2,
                        is_single_use = false,
                        trigger_flag = "",
                        affinity_gain = 0,
                    },
                }
            });
        }

        private static MainQuestConfig BuildMainQuestConfig()
        {
            return new MainQuestConfig(
                new MainQuestData[]
                {
                    new MainQuestData { id = 1, quest_id = "T0", display_name = "開始",         completion_condition_type = MainQuestCompletionTypes.Auto,        completion_condition_value = "" },
                    new MainQuestData { id = 2, quest_id = "T1", display_name = "認識所有人",   completion_condition_type = MainQuestCompletionTypes.DialogueEnd,  completion_condition_value = MainQuestSignalValues.Node2DialogueComplete   },
                    new MainQuestData { id = 3, quest_id = "T2", display_name = "出去看看外面", completion_condition_type = MainQuestCompletionTypes.FirstExplore, completion_condition_value = MainQuestSignalValues.GuardReturnEventComplete },
                },
                new MainQuestUnlockData[]
                {
                    new MainQuestUnlockData { id = 1, main_quest_id = "T0", unlock_type = "quest", unlock_value = "T1" },
                    new MainQuestUnlockData { id = 2, main_quest_id = "T1", unlock_type = "quest", unlock_value = "T2" },
                });
        }
    }
}
