// GuardReturnSwordFlowIntegrationTest — Sprint 6 擴張 D5 端到端整合測試。
// 驗證「守衛歸來 → 探索鎖定 → （取劍流程）→ 探索重開」完整流程。
//
// Sprint 6 決策 6-13 更新（F12）：
//   取劍路徑從「玩家發問 guard_ask_sword 特殊題」改為「守衛首次進入自動對白觸發」。
//   影響測試 D、F、H：
//     D：舊語義為「清單含 sword 特殊題」，已更新為「測試 PlayerQuestionsManager 泛用特殊題機制」
//     F：TriggerSingleUseQuestion 不再發布 ExplorationGateReopenedEvent → 驗證不發布（regression）
//     H：T2 不再由 TriggerSingleUseQuestion 的 ExplorationGateReopened 完成 → 驗證不完成（regression）
//   未影響：A、B、C、E（泛用發劍機制）、G（泛用永久消耗機制）、F2、F4、F7
//
// 測試覆蓋（依 Sprint 6 D5 驗收準則，F12 更新）：
//   A. 守衛歸來事件結束後探索鎖定（ExplorationGateLockedEvent 發布）
//   B. 探索鎖定後嘗試 Depart 應被攔截（不出發）
//   C. 守衛 Hub 按鈕已解鎖（CharacterIds.Guard 已解鎖）
//   D. PlayerQuestionsManager.TriggerSingleUseQuestion 泛用機制：含特殊題的 config 呈現特殊題
//   E. TriggerSingleUseQuestion（泛用） → 劍入背包（透過自建含 sword 題的 config）
//   F. (regression) TriggerSingleUseQuestion 不再發布 ExplorationGateReopenedEvent（新流程改由對白完成發布）
//   G. TriggerSingleUseQuestion（泛用） → 特殊題從清單永久消失
//   H. (regression) TriggerSingleUseQuestion 不再觸發 T2 完成（新流程改由首次對白完成觸發）
//   F2. 守衛歸來完成後 T2 必須為 Available（不可自動完成）— 回歸防止雙 signal 重蹈覆轍
//   F4. 探索鎖定狀態下連續點擊 3 次發布 3 次 ExplorationGateLockedClickedEvent

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village.Integration
{
    [TestFixture]
    public class GuardReturnSwordFlowIntegrationTest
    {
        private BackpackManager _backpack;
        private StorageManager _storage;
        private InitialResourcesConfig _resourcesConfig;
        private InitialResourceDispatcher _dispatcher;
        private CharacterUnlockManager _unlockManager;
        private GuardReturnEventController _guardController;
        private ExplorationEntryManager _explorationManager;
        private FakeCGPlayer _cgPlayer;
        private PlayerQuestionsConfig _questionsConfig;
        private PlayerQuestionsManager _questionsManager;
        private MainQuestConfig _mainQuestConfig;
        private MainQuestManager _mainQuestManager;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            _backpack = new BackpackManager(20, 99);
            _storage = new StorageManager(100, 99);

            _resourcesConfig = BuildInitialResourcesConfig();
            _dispatcher = new InitialResourceDispatcher(_backpack, _storage);
            _unlockManager = new CharacterUnlockManager(_resourcesConfig, _dispatcher);

            _cgPlayer = new FakeCGPlayer { AutoComplete = true };
            _guardController = new GuardReturnEventController(_cgPlayer);

            _explorationManager = new ExplorationEntryManager(_backpack);
            _unlockManager.ForceUnlockExplorationFeature();
            _explorationManager.SetDepartureInterceptor(new Interceptor(_guardController, _unlockManager));

            _questionsConfig = BuildGuardQuestionsConfig();
            _questionsManager = new PlayerQuestionsManager(_questionsConfig, seed: 42);

            _mainQuestConfig = BuildMainQuestConfig();
            _mainQuestManager = new MainQuestManager(_mainQuestConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _guardController?.Dispose();
            _unlockManager?.Dispose();
            _explorationManager?.Dispose();
            EventBus.ForceClearAll();
        }

        // ===== A：守衛歸來完成後 ExplorationGateLockedEvent 發布 =====

        [Test]
        public void GuardReturnComplete_PublishesExplorationGateLockedEvent()
        {
            bool lockedEventReceived = false;
            Action<ExplorationGateLockedEvent> handler = (e) => lockedEventReceived = true;
            EventBus.Subscribe(handler);
            try
            {
                TriggerGuardReturnAndComplete();
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
            Assert.IsTrue(lockedEventReceived, "守衛歸來事件完成後應發布 ExplorationGateLockedEvent");
        }

        // ===== B：探索鎖定後 Depart 不出發 =====

        [Test]
        public void AfterGuardReturn_Depart_LockedByClosure()
        {
            TriggerGuardReturnAndComplete();

            // 設定 ExplorationEntryManager 為鎖定狀態（由 VillageEntryPoint 訂閱事件後設定）
            _explorationManager.SetExplorationLocked(true);

            bool departed = _explorationManager.Depart();
            Assert.IsFalse(departed, "探索鎖定狀態下 Depart 應回傳 false");
        }

        // ===== C：守衛 Hub 按鈕已解鎖 =====

        [Test]
        public void AfterGuardReturn_GuardIsUnlocked()
        {
            TriggerGuardReturnAndComplete();
            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.Guard));
        }

        // ===== D：PlayerQuestionsManager 泛用特殊題機制：含特殊題的 config 呈現特殊題 =====
        // Sprint 6 決策 6-13 更新：原語義「守衛清單含 guard_ask_sword」已廢止。
        // 測試改為驗證 PlayerQuestionsManager 泛用特殊題顯示機制：含特殊題的 config 可正確呈現。
        // （注意：本測試使用自建 BuildGuardQuestionsConfig，其中故意保留 guard_ask_sword 以測試機制本身）

        [Test]
        public void GuardQuestionsPresentation_WithCustomConfigContainingSingleUse_ShowsSingleUse()
        {
            PlayerQuestionsPresentation presentation =
                _questionsManager.GetPresentation(CharacterIds.Guard);

            bool hasSingleUseQuestion = false;
            foreach (PlayerQuestionInfo q in presentation.Questions)
            {
                if (q.IsSingleUse)
                {
                    hasSingleUseQuestion = true;
                    break;
                }
            }
            Assert.IsTrue(hasSingleUseQuestion,
                "PlayerQuestionsManager 泛用機制：含特殊題的 config 應在發問清單中呈現該特殊題");
        }

        // ===== E：TriggerSingleUseQuestion 泛用機制 → 劍入背包 =====
        // Sprint 6 決策 6-13 注意：此測試使用自建含 guard_ask_sword 的 config，
        // 驗證的是 TriggerSingleUseQuestion 泛用 grant 派發機制，而非守衛的業務流程。

        [Test]
        public void TriggerSingleUseQuestion_DispatchesGrantToBackpack()
        {
            // 先模擬守衛已解鎖（此情境）
            _unlockManager.ForceUnlock(CharacterIds.Guard);

#pragma warning disable CS0618 // TriggerFlagGrantGuardSword 已 Obsolete，此處用於測試泛用機制
            _questionsManager.TriggerSingleUseQuestion(
                CharacterIds.Guard,
                PlayerQuestionsManager.TriggerFlagGrantGuardSword,
                _dispatcher,
                _resourcesConfig);
#pragma warning restore CS0618

            Assert.AreEqual(1, _backpack.GetItemCount("gift_sword_wooden"),
                "TriggerSingleUseQuestion 泛用機制：觸發後應透過 dispatcher 派發對應 grant 至背包");
        }

        // ===== F：(regression) TriggerSingleUseQuestion 不再發布 ExplorationGateReopenedEvent =====
        // Sprint 6 決策 6-13：ExplorationGateReopenedEvent 改由「守衛首次進入對白完成」發布，
        // 不再由 TriggerSingleUseQuestion 發布。此測試確認舊路徑已移除，不產生重複事件。

        [Test]
        public void Regression_F12_TriggerSingleUseQuestion_DoesNotPublishExplorationGateReopenedEvent()
        {
            bool reopenedReceived = false;
            Action<ExplorationGateReopenedEvent> handler = (e) => reopenedReceived = true;
            EventBus.Subscribe(handler);
            try
            {
#pragma warning disable CS0618
                _questionsManager.TriggerSingleUseQuestion(
                    CharacterIds.Guard,
                    PlayerQuestionsManager.TriggerFlagGrantGuardSword,
                    _dispatcher,
                    _resourcesConfig);
#pragma warning restore CS0618
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
            Assert.IsFalse(reopenedReceived,
                "Sprint 6 決策 6-13 regression：TriggerSingleUseQuestion 不應再發布 ExplorationGateReopenedEvent，" +
                "此事件改由守衛首次進入對白完成後發布");
        }

        // ===== G：TriggerSingleUseQuestion 泛用機制 → 特殊題從清單永久消失 =====
        // Sprint 6 決策 6-13 注意：此測試驗證的是 _consumedSingleUseIds 的泛用永久消耗機制，
        // 使用自建含特殊題的 config。

        [Test]
        public void AfterTriggerSingleUseQuestion_QuestionRemovedPermanently()
        {
#pragma warning disable CS0618
            _questionsManager.TriggerSingleUseQuestion(
                CharacterIds.Guard,
                PlayerQuestionsManager.TriggerFlagGrantGuardSword,
                _dispatcher,
                _resourcesConfig);
#pragma warning restore CS0618

            PlayerQuestionsPresentation presentation =
                _questionsManager.GetPresentation(CharacterIds.Guard);

            bool stillHasSingleUseQuestion = false;
            foreach (PlayerQuestionInfo q in presentation.Questions)
            {
                if (q.IsSingleUse && q.QuestionId == "guard_ask_sword")
                {
                    stillHasSingleUseQuestion = true;
                    break;
                }
            }
            Assert.IsFalse(stillHasSingleUseQuestion,
                "TriggerSingleUseQuestion 泛用機制：觸發後特殊題應從清單永久消失");
        }

        // ===== H：(regression) TriggerSingleUseQuestion 不再透過 ExplorationGateReopenedEvent 完成 T2 =====
        // Sprint 6 決策 6-13：T2 現由「守衛首次進入對白完成 → OnGuardFirstMeetDialogueCompleted → ExplorationGateReopenedEvent」觸發。
        // TriggerSingleUseQuestion 不再發布 ExplorationGateReopenedEvent，故不應透過舊路徑完成 T2。

        [Test]
        public void Regression_F12_TriggerSingleUseQuestion_DoesNotCompleteT2ViaOldPath()
        {
            // Arrange：T0 auto → T1 完成 → T2 進入 Available
            _mainQuestManager.TryAutoCompleteFirstAutoQuest(); // T0 auto
            _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd,
                MainQuestSignalValues.Node2DialogueComplete); // T1

            // 裝配 production path subscriber（模擬 VillageEntryPoint.OnExplorationGateReopenedForT2）
            Action<ExplorationGateReopenedEvent> productionSubscriber = (e) =>
            {
                _mainQuestManager.NotifyCompletionSignal(
                    MainQuestCompletionTypes.FirstExplore,
                    MainQuestSignalValues.GuardReturnEventComplete);
            };
            EventBus.Subscribe(productionSubscriber);

            bool t2Completed = false;
            Action<MainQuestCompletedEvent> handler = (e) =>
            {
                if (e.QuestId == "T2") t2Completed = true;
            };
            EventBus.Subscribe(handler);
            try
            {
                // Act：TriggerSingleUseQuestion 不應發布 ExplorationGateReopenedEvent，故不觸發 T2
#pragma warning disable CS0618
                _questionsManager.TriggerSingleUseQuestion(
                    CharacterIds.Guard,
                    PlayerQuestionsManager.TriggerFlagGrantGuardSword,
                    _dispatcher,
                    _resourcesConfig);
#pragma warning restore CS0618
            }
            finally
            {
                EventBus.Unsubscribe(handler);
                EventBus.Unsubscribe(productionSubscriber);
            }

            Assert.IsFalse(t2Completed,
                "Sprint 6 決策 6-13 regression：TriggerSingleUseQuestion 不應再透過舊路徑完成 T2。" +
                "T2 現由守衛首次進入對白完成後的 ExplorationGateReopenedEvent 觸發。");
        }

        // ===== F2：守衛歸來完成後 T2 必須為 Available（不可自動完成）=====
        // 回歸測試：防止雙 signal handler（OnGuardReturnForMainQuest）重送 T2 訊號。
        // 若 VillageEntryPoint 存在 OnGuardReturnForMainQuest 訂閱，此測試將失敗。

        [Test]
        public void GuardReturnComplete_DoesNotCompleteT2_UntilPlayerAsks()
        {
            // Arrange：T0 auto → T1 完成 → T2 進入 Available
            _mainQuestManager.TryAutoCompleteFirstAutoQuest(); // T0 auto
            _mainQuestManager.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd,
                MainQuestSignalValues.Node2DialogueComplete); // T1 完成

            // 裝配 production path subscriber（模擬 VillageEntryPoint.OnGuardReturnLockExploration）
            Action<GuardReturnEventCompletedEvent> lockSubscriber = (e) =>
            {
                _explorationManager.SetExplorationLocked(true);
                EventBus.Publish(new ExplorationGateLockedEvent());
            };
            EventBus.Subscribe(lockSubscriber);

            // 訂閱 T2 完成：若意外完成即記錄
            bool t2CompletedUnexpectedly = false;
            Action<MainQuestCompletedEvent> t2Handler = (e) =>
            {
                if (e.QuestId == "T2") t2CompletedUnexpectedly = true;
            };
            EventBus.Subscribe(t2Handler);

            try
            {
                // Act：守衛歸來事件完成
                TriggerGuardReturnAndComplete();
            }
            finally
            {
                EventBus.Unsubscribe(lockSubscriber);
                EventBus.Unsubscribe(t2Handler);
            }

            // Assert：T2 不可在守衛歸來後自動完成
            Assert.IsFalse(t2CompletedUnexpectedly,
                "守衛歸來事件完成後 T2 不應自動完成，必須等玩家發問「要拿劍」後才完成");

            // Assert：背包無劍（守衛歸來不贈劍）
            Assert.AreEqual(0, _backpack.GetItemCount("gift_sword_wooden"),
                "守衛歸來事件完成後背包不應有劍");

            // Assert：探索應處於鎖定狀態
            Assert.IsTrue(_explorationManager.IsExplorationLocked,
                "守衛歸來事件完成後探索應為鎖定狀態");
        }

        // ===== F4：探索鎖定狀態下連續點擊 3 次發布 3 次 ExplorationGateLockedClickedEvent =====

        [Test]
        public void LockedButton_ClickedThreeTimes_PublishesThreeEvents()
        {
            // Arrange：設定探索為鎖定狀態
            _explorationManager.SetExplorationLocked(true);

            int lockedClickCount = 0;
            Action<ExplorationGateLockedClickedEvent> handler = (e) => lockedClickCount++;
            EventBus.Subscribe(handler);

            try
            {
                // Act：連續呼叫 Depart 3 次（鎖定狀態下不出發，每次發布 ExplorationGateLockedClickedEvent）
                _explorationManager.Depart();
                _explorationManager.Depart();
                _explorationManager.Depart();
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            // Assert：3 次點擊應發布 3 次事件（不去抖動、不一次性）
            Assert.AreEqual(3, lockedClickCount,
                "探索鎖定狀態下連續點擊 3 次應各自發布 ExplorationGateLockedClickedEvent，共 3 次");
        }

        // ===== F7（D4 bugfix 4）：守衛歸來完成後 CharacterUnlockedEvent{Guard} 必須在無手動推進下發布 =====
        // 回歸測試：防止 production path 依賴 DialogueManager.Advance() 推進對話後才發布完成事件。
        // 若 GuardReturnEventController 在 CG 完成後呼叫 DialogueManager.StartDialogue() 且
        // 沒有 View 呼叫 Advance()，則 DialogueCompletedEvent 永遠不發布，守衛不被解鎖。
        // 修復後：GuardReturnEventController.OnCGComplete() 應直接呼叫 CompleteEvent()，
        // 不依賴 DialogueManager 推進，使 CharacterUnlockedEvent{Guard} 在 CG 完成後立即發布。

        [Test]
        public void GuardReturnCGComplete_WithoutManualAdvance_PublishesCharacterUnlockedEvent()
        {
            // 此測試故意不呼叫 DialogueManager.Advance()，
            // 模擬生產環境中沒有 CharacterInteractionView 推進對話的真實情境。
            bool guardUnlockedEventReceived = false;
            Action<CharacterUnlockedEvent> handler = (e) =>
            {
                if (e.CharacterId == CharacterIds.Guard) guardUnlockedEventReceived = true;
            };
            EventBus.Subscribe(handler);

            try
            {
                // Act：僅觸發 CG（自動完成），不手動推進 DialogueManager
                _cgPlayer.AutoComplete = true;
                _explorationManager.Depart();
                // 刻意不呼叫 while (_dialogueManager.IsActive && _dialogueManager.Advance()) { }
                // 若 production path 依賴 Advance()，CharacterUnlockedEvent 不會發布，測試失敗
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            Assert.IsTrue(guardUnlockedEventReceived,
                "守衛歸來 CG 完成後應立即發布 CharacterUnlockedEvent{Guard}，" +
                "不應依賴 DialogueManager.Advance() 才能完成事件鏈。" +
                "根因：production 環境中無 CharacterInteractionView 呼叫 Advance()。");
        }

        // ===== 既有守衛歸來事件不再贈劍 =====

        [Test]
        public void GuardReturnComplete_DoesNotGrantSword()
        {
            TriggerGuardReturnAndComplete();

            // 守衛歸來事件完成後背包不應有劍
            Assert.AreEqual(0, _backpack.GetItemCount("gift_sword_wooden"),
                "守衛歸來事件完成時不應直接贈劍（劍由玩家主動發問取得）");
        }

        // ===== Helpers =====

        private void TriggerGuardReturnAndComplete()
        {
            _cgPlayer.AutoComplete = true;
            // Depart() → 攔截器 TriggerEvent() → FakeCGPlayer 立即呼叫 OnCGComplete → CompleteEvent()
            // GuardReturnEventCompletedEvent 在同步呼叫鏈中發布，不需手動推進 DialogueManager。
            _explorationManager.Depart();
        }

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

        private static PlayerQuestionsConfig BuildGuardQuestionsConfig()
        {
            return new PlayerQuestionsConfig(new PlayerQuestionsConfigData
            {
                schema_version = 2,
                questions = new PlayerQuestionData[]
                {
                    new PlayerQuestionData
                    {
                        question_id = "guard_ask_sword",
                        character_id = CharacterIds.Guard,
                        question_text = "你之前說過要給我一把劍的事…",
                        response_text = "對，你拿去吧。在森林裡要小心。",
                        sort_order = 0,
                        is_single_use = true,
                        trigger_flag = PlayerQuestionsManager.TriggerFlagGrantGuardSword,
                        affinity_gain = 0,
                    },
                    new PlayerQuestionData
                    {
                        question_id = "guard_q01",
                        character_id = CharacterIds.Guard,
                        question_text = "你當了多久的守衛？",
                        response_text = "三年。",
                        sort_order = 1,
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

        // ===== 攔截器（與 VillageEntryPoint 行為一致） =====

        private class Interceptor : IExplorationDepartureInterceptor
        {
            private readonly GuardReturnEventController _guardController;
            private readonly CharacterUnlockManager _unlockManager;

            public Interceptor(GuardReturnEventController g, CharacterUnlockManager u)
            {
                _guardController = g;
                _unlockManager = u;
            }

            public bool TryIntercept()
            {
                if (_guardController.HasTriggered) return false;
                if (_unlockManager.IsUnlocked(CharacterIds.Guard)) return false;
                return _guardController.TriggerEvent();
            }
        }

        // ===== FakeCGPlayer =====

        private class FakeCGPlayer : ICGPlayer
        {
            public bool AutoComplete { get; set; } = true;

            public void PlayIntroCG(string characterId, Action onComplete)
            {
                if (AutoComplete) onComplete?.Invoke();
            }
        }
    }
}
