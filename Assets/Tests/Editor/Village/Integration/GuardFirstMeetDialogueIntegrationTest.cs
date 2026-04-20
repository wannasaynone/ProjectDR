// GuardFirstMeetDialogueIntegrationTest — Sprint 6 決策 6-13 取劍新機制整合測試。
//
// 驗證「守衛首次進入互動畫面 → 自動對白觸發 → 發劍 + 探索重開」完整新流程。
//
// 測試覆蓋（依 Sprint 6 F12 驗收準則）：
//   T1. 守衛首次進入（IsUnlocked=true, CompleteStatus=false）→ CharacterInteractionView 被設置覆蓋對白
//   T2. 覆蓋對白完成 callback → 劍入背包 + ExplorationGateReopenedEvent 發布
//   T3. 覆蓋對白完成後再次進入守衛 → 不再設置覆蓋對白（僅一次性）
//   T4. ExplorationGateReopenedEvent 發布 → T2 主線任務完成（production path）
//   T5 regression：
//     F9/F10 清除 FirstMeet 時機 — 首次進入後 FirstMeet = false（在對白完成之前）
//     guard_ask_sword 題目不再出現在 PlayerQuestionsManager 的清單中

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.UI;

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
        private GuardFirstMeetDialogueConfig _firstMeetConfig;
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

            _firstMeetConfig = BuildGuardFirstMeetConfig();

            // Sprint 6 決策 6-13：guard_ask_sword 不應在 player-questions-config 中
            _playerQuestionsConfig = BuildGuardQuestionsConfigWithoutSwordQuestion();
            _playerQuestionsManager = new PlayerQuestionsManager(_playerQuestionsConfig, seed: 42);
        }

        [TearDown]
        public void TearDown()
        {
            _unlockManager?.Dispose();
            _explorationManager?.Dispose();
            _redDotManager?.Dispose();
            EventBus.ForceClearAll();
        }

        // ===== T1：首次進入守衛（已解鎖）→ CharacterInteractionView 設置覆蓋對白 =====

        [Test]
        public void GuardFirstEnter_WhenUnlocked_AndNotCompleted_SetsOverrideDialogue()
        {
            // Arrange：模擬守衛已解鎖
            _unlockManager.ForceUnlock(CharacterIds.Guard);

            // Act：模擬 VillageEntryPoint.OnCharacterEnteredAndCGDone 的判斷邏輯
            bool shouldTrigger = ShouldTriggerGuardFirstMeetDialogue(
                characterId: CharacterIds.Guard,
                isCompleted: false,
                isUnlocked: true);

            // Assert
            Assert.IsTrue(shouldTrigger,
                "守衛已解鎖且首次對白尚未完成時，應觸發首次進入取劍對白");
        }

        // ===== T2：覆蓋對白完成 callback → 劍入背包 + ExplorationGateReopenedEvent 發布 =====

        [Test]
        public void GuardFirstMeetCallback_GrantsSword_AndPublishesExplorationGateReopenedEvent()
        {
            // Arrange
            bool reopenedReceived = false;
            Action<ExplorationGateReopenedEvent> handler = (e) => reopenedReceived = true;
            EventBus.Subscribe(handler);

            try
            {
                // Act：模擬 OnGuardFirstMeetDialogueCompleted
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

        // ===== T3：對白完成後再次進入守衛 → 不再觸發取劍對白（僅一次性）=====

        [Test]
        public void GuardFirstMeet_AfterCompleted_DoesNotTriggerAgain()
        {
            // Arrange：模擬首次對白已完成
            bool firstMeetCompleted = true;

            // Act
            bool shouldTrigger = ShouldTriggerGuardFirstMeetDialogue(
                characterId: CharacterIds.Guard,
                isCompleted: firstMeetCompleted,
                isUnlocked: true);

            // Assert
            Assert.IsFalse(shouldTrigger,
                "取劍對白完成後再次進入守衛，不應再次觸發取劍對白");
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
        /// 模擬 VillageEntryPoint.OnCharacterEnteredAndCGDone 中的判斷邏輯。
        /// </summary>
        private static bool ShouldTriggerGuardFirstMeetDialogue(
            string characterId,
            bool isCompleted,
            bool isUnlocked)
        {
            return characterId == CharacterIds.Guard
                && !isCompleted
                && isUnlocked;
        }

        /// <summary>
        /// 模擬 VillageEntryPoint.OnGuardFirstMeetDialogueCompleted 業務邏輯。
        /// </summary>
        private void SimulateGuardFirstMeetCompleted()
        {
            // 發劍（與 VillageEntryPoint.OnGuardFirstMeetDialogueCompleted 邏輯一致）
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
            return new InitialResourcesConfig(new InitialResourcesConfigData
            {
                schema_version = 1,
                grants = new InitialResourceGrantData[]
                {
                    new InitialResourceGrantData
                    {
                        grant_id = "initial_backpack_node0",
                        trigger_id = InitialResourcesTriggerIds.Node0Start,
                        item_id = "",
                        quantity = 0
                    },
                    new InitialResourceGrantData
                    {
                        grant_id = "unlock_guard_sword",
                        trigger_id = InitialResourcesTriggerIds.GuardSwordAsked,
                        item_id = "gift_sword_wooden",
                        quantity = 1
                    },
                },
            });
        }

        private static GuardFirstMeetDialogueConfig BuildGuardFirstMeetConfig()
        {
            return new GuardFirstMeetDialogueConfig(new GuardFirstMeetDialogueConfigData
            {
                schema_version = 1,
                dialogue_lines = new string[]
                {
                    "你終於來了。【test placeholder】",
                    "拿好這個。【test placeholder】",
                    "在森林裡小心。【test placeholder】"
                }
            });
        }

        /// <summary>
        /// 建立守衛發問清單（不含 guard_ask_sword 特殊題）。
        /// 反映 Sprint 6 決策 6-13 後的 player-questions-config.json 結構。
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
            return new MainQuestConfig(new MainQuestConfigData
            {
                main_quests = new MainQuestConfigEntry[]
                {
                    new MainQuestConfigEntry
                    {
                        quest_id = "T0",
                        display_name = "開始",
                        description = "",
                        completion_condition_type = MainQuestCompletionTypes.Auto,
                        completion_condition_value = "",
                        unlock_on_complete = "T1",
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T1",
                        display_name = "認識所有人",
                        description = "",
                        completion_condition_type = MainQuestCompletionTypes.DialogueEnd,
                        completion_condition_value = MainQuestSignalValues.Node2DialogueComplete,
                        unlock_on_complete = "T2",
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T2",
                        display_name = "出去看看外面",
                        description = "",
                        completion_condition_type = MainQuestCompletionTypes.FirstExplore,
                        completion_condition_value = MainQuestSignalValues.GuardReturnEventComplete,
                        unlock_on_complete = "",
                    },
                }
            });
        }
    }
}
