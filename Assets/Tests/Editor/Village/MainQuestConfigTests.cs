using System;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// MainQuestConfig / MainQuestData 單元測試。
    /// Sprint 8 Wave 2.5：配合純陣列 DTO 重構（廢棄包裹類 MainQuestConfigData / MainQuestConfigEntry）。
    /// </summary>
    [TestFixture]
    public class MainQuestConfigTests
    {
        [Test]
        public void Constructor_NullQuestEntries_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MainQuestConfig(null, new MainQuestUnlockData[0]));
        }

        [Test]
        public void Constructor_NullUnlockEntries_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MainQuestConfig(new MainQuestData[0], null));
        }

        [Test]
        public void Constructor_EmptyQuests_NoEntries()
        {
            MainQuestConfig config = new MainQuestConfig(new MainQuestData[0], new MainQuestUnlockData[0]);
            Assert.AreEqual(0, config.OrderedQuests.Count);
            Assert.IsNull(config.GetQuest("T0"));
        }

        [Test]
        public void GetQuest_ReturnsExpectedInfo()
        {
            MainQuestConfig config = BuildSimpleConfig();
            MainQuestInfo t1 = config.GetQuest("T1");
            Assert.IsNotNull(t1);
            Assert.AreEqual("node_2_dialogue_complete", t1.CompletionConditionValue);
            Assert.AreEqual(MainQuestCompletionTypes.DialogueEnd, t1.CompletionConditionType);
        }

        [Test]
        public void T1_RewardGrantIds_IsEmpty()
        {
            MainQuestConfig config = BuildSimpleConfig();
            MainQuestInfo t1 = config.GetQuest("T1");
            Assert.AreEqual(0, t1.RewardGrantIds.Count);
        }

        [Test]
        public void T1_UnlockOnComplete_ContainsT2AndNode2AndExploration()
        {
            MainQuestConfig config = BuildSimpleConfig();
            MainQuestInfo t1 = config.GetQuest("T1");
            Assert.AreEqual(3, t1.UnlockOnComplete.Count);
            Assert.Contains("T2", (System.Collections.ICollection)t1.UnlockOnComplete);
            Assert.Contains("node_2_complete", (System.Collections.ICollection)t1.UnlockOnComplete);
            Assert.Contains("exploration_open", (System.Collections.ICollection)t1.UnlockOnComplete);
        }

        [Test]
        public void T2_SortOrder_IsTwo()
        {
            MainQuestConfig config = BuildSimpleConfig();
            MainQuestInfo t2 = config.GetQuest("T2");
            Assert.IsNotNull(t2);
            Assert.AreEqual(2, t2.SortOrder);
        }

        [Test]
        public void OldT2T3_DoNotExist()
        {
            MainQuestConfig config = BuildSimpleConfig();
            foreach (var quest in config.OrderedQuests)
            {
                Assert.AreNotEqual(MainQuestCompletionTypes.CommissionCount, quest.CompletionConditionType,
                    $"Quest {quest.QuestId} 不應有 CommissionCount 完成條件（委託強制教學已移除）");
            }
        }

        [Test]
        public void EmptyPipeString_ResultsInEmptyList()
        {
            MainQuestConfig config = BuildSimpleConfig();
            MainQuestInfo t0 = config.GetQuest("T0");
            Assert.AreEqual(0, t0.RewardGrantIds.Count);
        }

        [Test]
        public void OrderedQuests_SortedBySortOrderAscending()
        {
            MainQuestData[] quests = new MainQuestData[]
            {
                new MainQuestData { id = 3, quest_id = "T3", sort_order = 3 },
                new MainQuestData { id = 1, quest_id = "T1", sort_order = 1 },
                new MainQuestData { id = 2, quest_id = "T2", sort_order = 2 }
            };
            MainQuestConfig config = new MainQuestConfig(quests, new MainQuestUnlockData[0]);
            Assert.AreEqual("T1", config.OrderedQuests[0].QuestId);
            Assert.AreEqual("T2", config.OrderedQuests[1].QuestId);
            Assert.AreEqual("T3", config.OrderedQuests[2].QuestId);
        }

        [Test]
        public void QuestWithEmptyId_Skipped()
        {
            MainQuestData[] quests = new MainQuestData[]
            {
                new MainQuestData { id = 0, quest_id = "", sort_order = 0 },
                new MainQuestData { id = 1, quest_id = "T1", sort_order = 1 }
            };
            MainQuestConfig config = new MainQuestConfig(quests, new MainQuestUnlockData[0]);
            Assert.AreEqual(1, config.OrderedQuests.Count);
            Assert.AreEqual("T1", config.OrderedQuests[0].QuestId);
        }

        // ===== IGameData 契約斷言（ADR-001 / ADR-002 A12）=====

        [Test]
        public void MainQuestData_ImplementsIGameData()
        {
            MainQuestData entry = new MainQuestData { id = 1, quest_id = "T0" };
            KahaGameCore.GameData.IGameData iGameData = entry;
            Assert.AreEqual(1, iGameData.ID, "IGameData.ID 必須等於 id 欄位");
        }

        [Test]
        public void MainQuestUnlockData_ImplementsIGameData()
        {
            MainQuestUnlockData entry = new MainQuestUnlockData { id = 1, main_quest_id = "T0" };
            KahaGameCore.GameData.IGameData iGameData = entry;
            Assert.AreEqual(1, iGameData.ID, "IGameData.ID 必須等於 id 欄位");
        }

        [Test]
        public void MainQuestInfo_HasIdAndKey()
        {
            MainQuestConfig config = BuildSimpleConfig();
            MainQuestInfo t0 = config.GetQuest("T0");
            Assert.IsNotNull(t0);
            Assert.AreNotEqual(0, t0.ID, "MainQuestInfo.ID 不應為 0");
            Assert.AreEqual("T0", t0.Key, "Key 應等於 quest_id");
            Assert.AreEqual(t0.Key, t0.QuestId, "QuestId 應等於 Key");
        }

        [Test]
        public void MainQuestInfo_UnlockEntries_LinkedFromSubTable()
        {
            MainQuestConfig config = BuildSimpleConfig();
            MainQuestInfo t1 = config.GetQuest("T1");
            Assert.IsNotNull(t1.UnlockEntries);
            Assert.Greater(t1.UnlockEntries.Count, 0, "T1 應有至少一筆 UnlockEntries");
        }

        // ===== 輔助：建立測試用配置 =====

        /// <summary>
        /// Sprint 6 重構後的測試用配置（T0/T1/T2 三條序列）。
        /// Sprint 8：改用純陣列 + 子表格式。
        /// </summary>
        private static MainQuestConfig BuildSimpleConfig()
        {
            MainQuestData[] quests = new MainQuestData[]
            {
                new MainQuestData
                {
                    id = 1,
                    quest_id = "T0",
                    display_name = "醒來的地方",
                    description = "開局任務",
                    owner_character_id = CharacterIds.VillageChiefWife,
                    completion_condition_type = MainQuestCompletionTypes.Auto,
                    completion_condition_value = "node0_dialogue_complete",
                    reward_grant_ids = "",
                    sort_order = 0
                },
                new MainQuestData
                {
                    id = 2,
                    quest_id = "T1",
                    display_name = "認識所有人",
                    description = "任務 1",
                    owner_character_id = CharacterIds.VillageChiefWife,
                    completion_condition_type = MainQuestCompletionTypes.DialogueEnd,
                    completion_condition_value = "node_2_dialogue_complete",
                    reward_grant_ids = "",
                    sort_order = 1
                },
                new MainQuestData
                {
                    id = 3,
                    quest_id = "T2",
                    display_name = "出去看看外面",
                    description = "任務 2（原 T4）",
                    owner_character_id = CharacterIds.VillageChiefWife,
                    completion_condition_type = MainQuestCompletionTypes.FirstExplore,
                    completion_condition_value = "guard_return_event_complete",
                    reward_grant_ids = "unlock_guard_sword",
                    sort_order = 2
                }
            };

            MainQuestUnlockData[] unlocks = new MainQuestUnlockData[]
            {
                new MainQuestUnlockData { id = 1, main_quest_id = "T0", unlock_type = "quest", unlock_value = "T1", sort_order = 0 },
                new MainQuestUnlockData { id = 2, main_quest_id = "T0", unlock_type = "event", unlock_value = "node_0_complete", sort_order = 1 },
                new MainQuestUnlockData { id = 3, main_quest_id = "T1", unlock_type = "quest", unlock_value = "T2", sort_order = 0 },
                new MainQuestUnlockData { id = 4, main_quest_id = "T1", unlock_type = "event", unlock_value = "node_2_complete", sort_order = 1 },
                new MainQuestUnlockData { id = 5, main_quest_id = "T1", unlock_type = "feature", unlock_value = "exploration_open", sort_order = 2 },
                new MainQuestUnlockData { id = 6, main_quest_id = "T2", unlock_type = "character", unlock_value = "guard_unlock", sort_order = 0 },
                new MainQuestUnlockData { id = 7, main_quest_id = "T2", unlock_type = "feature", unlock_value = "exploration_full_open", sort_order = 1 }
            };

            return new MainQuestConfig(quests, unlocks);
        }
    }
}
