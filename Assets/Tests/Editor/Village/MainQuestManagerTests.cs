using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// MainQuestManager 單元測試。
    /// 驗證：狀態機轉換、事件發布、Auto 任務自動完成、完成訊號通知、解鎖後續任務。
    /// </summary>
    [TestFixture]
    public class MainQuestManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== 建構 =====

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MainQuestManager(null));
        }

        [Test]
        public void Constructor_EmptyConfig_NoThrow()
        {
            MainQuestConfig config = new MainQuestConfig(new MainQuestConfigData
            {
                schema_version = 1,
                main_quests = new MainQuestConfigEntry[0]
            });
            MainQuestManager sut = new MainQuestManager(config);
            Assert.IsNull(sut.GetActiveQuest());
        }

        [Test]
        public void Constructor_InitialState_FirstQuestIsAvailable()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            Assert.AreEqual(MainQuestState.Available, sut.GetState("T0"));
            Assert.AreEqual(MainQuestState.Locked, sut.GetState("T1"));
            Assert.AreEqual(MainQuestState.Locked, sut.GetState("T2"));
        }

        [Test]
        public void Constructor_PublishesFirstQuestAvailableEvent()
        {
            MainQuestAvailableEvent received = null;
            Action<MainQuestAvailableEvent> handler = (e) => { received = e; };
            EventBus.Subscribe(handler);
            try
            {
                MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
            Assert.IsNotNull(received);
            Assert.AreEqual("T0", received.QuestId);
        }

        // ===== StartQuest =====

        [Test]
        public void StartQuest_Available_SucceedsAndPublishesEvent()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());

            MainQuestStartedEvent received = null;
            Action<MainQuestStartedEvent> handler = (e) => { received = e; };
            EventBus.Subscribe(handler);
            try
            {
                bool ok = sut.StartQuest("T0");
                Assert.IsTrue(ok);
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            Assert.IsNotNull(received);
            Assert.AreEqual("T0", received.QuestId);
            Assert.AreEqual(MainQuestState.InProgress, sut.GetState("T0"));
        }

        [Test]
        public void StartQuest_Locked_Fails()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            Assert.IsFalse(sut.StartQuest("T1"));
            Assert.AreEqual(MainQuestState.Locked, sut.GetState("T1"));
        }

        [Test]
        public void StartQuest_Unknown_Fails()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            Assert.IsFalse(sut.StartQuest("T99"));
        }

        // ===== CompleteQuest =====

        [Test]
        public void CompleteQuest_InProgress_SucceedsAndPublishesEvent()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            sut.StartQuest("T0");

            MainQuestCompletedEvent received = null;
            Action<MainQuestCompletedEvent> handler = (e) => { received = e; };
            EventBus.Subscribe(handler);
            try
            {
                bool ok = sut.CompleteQuest("T0");
                Assert.IsTrue(ok);
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
            Assert.IsNotNull(received);
            Assert.AreEqual("T0", received.QuestId);
            Assert.AreEqual(MainQuestState.Completed, sut.GetState("T0"));
        }

        [Test]
        public void CompleteQuest_UnlocksNextViaUnlockOnComplete()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            sut.StartQuest("T0");
            sut.CompleteQuest("T0");
            Assert.AreEqual(MainQuestState.Available, sut.GetState("T1"));
        }

        [Test]
        public void CompleteQuest_NotInProgress_Fails()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            Assert.IsFalse(sut.CompleteQuest("T1")); // Locked
        }

        [Test]
        public void IsQuestCompleted_AfterCompletion_True()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            sut.StartQuest("T0");
            sut.CompleteQuest("T0");
            Assert.IsTrue(sut.IsQuestCompleted("T0"));
            Assert.IsFalse(sut.IsQuestCompleted("T1"));
        }

        // ===== GetActiveQuest / GetQuestsInState =====

        [Test]
        public void GetActiveQuest_NoneInProgress_ReturnsNull()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            Assert.IsNull(sut.GetActiveQuest());
        }

        [Test]
        public void GetActiveQuest_InProgress_ReturnsQuest()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            sut.StartQuest("T0");
            MainQuestInfo active = sut.GetActiveQuest();
            Assert.IsNotNull(active);
            Assert.AreEqual("T0", active.QuestId);
        }

        [Test]
        public void GetQuestsInState_ReturnsMatchingIds()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            sut.StartQuest("T0");
            sut.CompleteQuest("T0");

            IReadOnlyList<string> completed = sut.GetQuestsInState(MainQuestState.Completed);
            Assert.AreEqual(1, completed.Count);
            Assert.AreEqual("T0", completed[0]);

            IReadOnlyList<string> available = sut.GetQuestsInState(MainQuestState.Available);
            Assert.AreEqual(1, available.Count);
            Assert.AreEqual("T1", available[0]);
        }

        // ===== TryAutoCompleteFirstAutoQuest =====

        [Test]
        public void TryAutoCompleteFirstAutoQuest_T0IsAuto_Completes()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            bool ok = sut.TryAutoCompleteFirstAutoQuest();
            Assert.IsTrue(ok);
            Assert.IsTrue(sut.IsQuestCompleted("T0"));
            Assert.AreEqual(MainQuestState.Available, sut.GetState("T1"));
        }

        [Test]
        public void TryAutoCompleteFirstAutoQuest_NoAutoAvailable_ReturnsFalse()
        {
            MainQuestConfig config = new MainQuestConfig(new MainQuestConfigData
            {
                schema_version = 1,
                main_quests = new MainQuestConfigEntry[]
                {
                    new MainQuestConfigEntry
                    {
                        id = 1,
                        quest_id = "T_dialogue",
                        completion_condition_type = MainQuestCompletionTypes.DialogueEnd,
                        sort_order = 0
                    }
                }
            });
            MainQuestManager sut = new MainQuestManager(config);
            Assert.IsFalse(sut.TryAutoCompleteFirstAutoQuest());
        }

        // ===== NotifyCompletionSignal =====

        [Test]
        public void NotifyCompletionSignal_InProgressMatches_Completes()
        {
            // 新 T1 完成條件 = node_2_dialogue_complete
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            sut.TryAutoCompleteFirstAutoQuest(); // T0 complete, T1 Available
            sut.StartQuest("T1"); // T1 InProgress

            IReadOnlyList<string> completed = sut.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd,
                MainQuestSignalValues.Node2DialogueComplete);

            Assert.AreEqual(1, completed.Count);
            Assert.AreEqual("T1", completed[0]);
            Assert.IsTrue(sut.IsQuestCompleted("T1"));
        }

        [Test]
        public void NotifyCompletionSignal_ValueMismatch_DoesNotComplete()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            sut.TryAutoCompleteFirstAutoQuest();
            sut.StartQuest("T1");

            IReadOnlyList<string> completed = sut.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd, "not_matching_value");

            Assert.AreEqual(0, completed.Count);
            Assert.IsFalse(sut.IsQuestCompleted("T1"));
        }

        [Test]
        public void NotifyCompletionSignal_TypeMismatch_NoEffect()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            sut.TryAutoCompleteFirstAutoQuest();
            sut.StartQuest("T1");

            IReadOnlyList<string> completed = sut.NotifyCompletionSignal(
                MainQuestCompletionTypes.FirstExplore, null);

            Assert.AreEqual(0, completed.Count);
            Assert.IsFalse(sut.IsQuestCompleted("T1"));
        }

        [Test]
        public void NotifyCompletionSignal_AvailableTaskWithMatchingType_AutoStartsAndCompletes()
        {
            // T1 Available（未手動 Start）— 應自動 Start 再 Complete
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            sut.TryAutoCompleteFirstAutoQuest(); // T1 變為 Available，但還沒 Start

            IReadOnlyList<string> completed = sut.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd,
                MainQuestSignalValues.Node2DialogueComplete);

            Assert.AreEqual(1, completed.Count);
            Assert.IsTrue(sut.IsQuestCompleted("T1"));
        }

        [Test]
        public void NotifyCompletionSignal_EmptySignalType_NoOp()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            IReadOnlyList<string> completed = sut.NotifyCompletionSignal("", "x");
            Assert.AreEqual(0, completed.Count);
        }

        [Test]
        public void NotifyCompletionSignal_NullValueMatchesAnyValue()
        {
            // 當 signalValue 為 null，只需類型匹配即可
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            sut.TryAutoCompleteFirstAutoQuest();
            sut.StartQuest("T1");

            IReadOnlyList<string> completed = sut.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd, null);

            Assert.AreEqual(1, completed.Count);
        }

        // ===== 跨多關卡完整流程（新 T0/T1/T2 三條結構）=====

        [Test]
        public void FullFlow_NewStructure_T0ToT2_CompletesInOrder()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());

            // T0 自動完成 → T1 Available
            sut.TryAutoCompleteFirstAutoQuest();
            Assert.IsTrue(sut.IsQuestCompleted("T0"));
            Assert.AreEqual(MainQuestState.Available, sut.GetState("T1"));

            // T1 完成條件 = node_2_dialogue_complete（魔女對話結束）
            sut.NotifyCompletionSignal(
                MainQuestCompletionTypes.DialogueEnd,
                MainQuestSignalValues.Node2DialogueComplete);
            Assert.IsTrue(sut.IsQuestCompleted("T1"));
            Assert.AreEqual(MainQuestState.Available, sut.GetState("T2"));

            // T2 完成條件 = first_explore（首次探索守衛歸來）
            sut.NotifyCompletionSignal(
                MainQuestCompletionTypes.FirstExplore,
                MainQuestSignalValues.GuardReturnEventComplete);
            Assert.IsTrue(sut.IsQuestCompleted("T2"));
        }

        [Test]
        public void Constructor_InitialState_ThreeQuests_FirstQuestIsAvailable()
        {
            MainQuestManager sut = new MainQuestManager(BuildThreeQuestConfig());
            Assert.AreEqual(MainQuestState.Available, sut.GetState("T0"));
            Assert.AreEqual(MainQuestState.Locked, sut.GetState("T1"));
            Assert.AreEqual(MainQuestState.Locked, sut.GetState("T2"));
        }

        // ===== Helper（舊 BuildFiveQuestConfig 已移除，改為 BuildThreeQuestConfig）=====

        private static MainQuestConfig BuildThreeQuestConfig()
        {
            // 對應新 main-quest-config.json（Sprint 6 B1）：T0/T1/T2 三條任務
            MainQuestConfigData data = new MainQuestConfigData
            {
                schema_version = 1,
                main_quests = new MainQuestConfigEntry[]
                {
                    new MainQuestConfigEntry
                    {
                        id = 1,
                        quest_id = "T0",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.Auto,
                        completion_condition_value = MainQuestSignalValues.Node0DialogueComplete,
                        unlock_on_complete = "T1|node_0_complete",
                        sort_order = 0
                    },
                    new MainQuestConfigEntry
                    {
                        id = 2,
                        quest_id = "T1",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.DialogueEnd,
                        completion_condition_value = MainQuestSignalValues.Node2DialogueComplete,
                        unlock_on_complete = "T2|node_2_complete|exploration_open",
                        sort_order = 1
                    },
                    new MainQuestConfigEntry
                    {
                        id = 3,
                        quest_id = "T2",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.FirstExplore,
                        completion_condition_value = MainQuestSignalValues.GuardReturnEventComplete,
                        unlock_on_complete = "guard_unlock|exploration_full_open",
                        sort_order = 2
                    }
                }
            };
            return new MainQuestConfig(data);
        }
    }
}
