using System;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// MainQuestConfig / MainQuestConfigData 單元測試。
    /// </summary>
    [TestFixture]
    public class MainQuestConfigTests
    {
        [Test]
        public void Constructor_NullData_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MainQuestConfig(null));
        }

        [Test]
        public void Constructor_EmptyQuests_NoEntries()
        {
            MainQuestConfigData data = new MainQuestConfigData
            {
                schema_version = 1,
                main_quests = new MainQuestConfigEntry[0]
            };
            MainQuestConfig config = new MainQuestConfig(data);
            Assert.AreEqual(0, config.OrderedQuests.Count);
            Assert.IsNull(config.GetQuest("T0"));
        }

        [Test]
        public void GetQuest_ReturnsExpectedInfo()
        {
            // Sprint 6 重構後：T1 完成條件改為 node_2_dialogue_complete（魔女對話結束）
            MainQuestConfig config = BuildSimpleConfig();
            MainQuestInfo t1 = config.GetQuest("T1");
            Assert.IsNotNull(t1);
            Assert.AreEqual("node_2_dialogue_complete", t1.CompletionConditionValue);
            Assert.AreEqual(MainQuestCompletionTypes.DialogueEnd, t1.CompletionConditionType);
        }

        [Test]
        public void T1_RewardGrantIds_IsEmpty()
        {
            // Sprint 6 決策 6：T1 不發初始物資（農女/魔女 grant 已移除）
            MainQuestConfig config = BuildSimpleConfig();
            MainQuestInfo t1 = config.GetQuest("T1");
            Assert.AreEqual(0, t1.RewardGrantIds.Count);
        }

        [Test]
        public void T1_UnlockOnComplete_ContainsT2AndNode2AndExploration()
        {
            // Sprint 6：T1 完成後解鎖 T2 + node_2_complete + exploration_open
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
            // Sprint 6：原 T4 重編為 T2，sort_order 必須為 2
            MainQuestConfig config = BuildSimpleConfig();
            MainQuestInfo t2 = config.GetQuest("T2");
            Assert.IsNotNull(t2);
            Assert.AreEqual(2, t2.SortOrder);
        }

        [Test]
        public void OldT2T3_DoNotExist()
        {
            // Sprint 6：原 T2「幫她一次」、T3「再去認識另一個人」已刪除
            MainQuestConfig config = BuildSimpleConfig();
            // 在新的 3 條序列中，T2 代表原 T4（出去看看外面），不再是委託任務
            // 驗證 3 條任務中不含 commission_count 型別（原 T2/T3 特徵）
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
            MainQuestConfigData data = new MainQuestConfigData
            {
                schema_version = 1,
                main_quests = new MainQuestConfigEntry[]
                {
                    new MainQuestConfigEntry { quest_id = "T3", sort_order = 3 },
                    new MainQuestConfigEntry { quest_id = "T1", sort_order = 1 },
                    new MainQuestConfigEntry { quest_id = "T2", sort_order = 2 }
                }
            };
            MainQuestConfig config = new MainQuestConfig(data);
            Assert.AreEqual("T1", config.OrderedQuests[0].QuestId);
            Assert.AreEqual("T2", config.OrderedQuests[1].QuestId);
            Assert.AreEqual("T3", config.OrderedQuests[2].QuestId);
        }

        [Test]
        public void QuestWithEmptyId_Skipped()
        {
            MainQuestConfigData data = new MainQuestConfigData
            {
                schema_version = 1,
                main_quests = new MainQuestConfigEntry[]
                {
                    new MainQuestConfigEntry { quest_id = "", sort_order = 0 },
                    new MainQuestConfigEntry { quest_id = "T1", sort_order = 1 }
                }
            };
            MainQuestConfig config = new MainQuestConfig(data);
            Assert.AreEqual(1, config.OrderedQuests.Count);
            Assert.AreEqual("T1", config.OrderedQuests[0].QuestId);
        }

        [Test]
        public void RealJsonFile_DeserializesSuccessfully()
        {
            // Sprint 6 重構後：3 條任務（T0/T1/T2），原 T4 已重編為 T2
            TextAsset asset = Resources.Load<TextAsset>("Config/main-quest-config");
            if (asset == null)
            {
                Assert.Pass("main-quest-config 資源不存在，跳過真實 JSON 測試。");
                return;
            }

            MainQuestConfigData data = JsonUtility.FromJson<MainQuestConfigData>(asset.text);
            Assert.IsNotNull(data);
            MainQuestConfig config = new MainQuestConfig(data);
            Assert.IsNotNull(config.GetQuest("T0"));
            Assert.IsNotNull(config.GetQuest("T1"));
            Assert.IsNotNull(config.GetQuest("T2"));
            Assert.IsNull(config.GetQuest("T3"), "T3（幫她一次）應已移除");
            Assert.IsNull(config.GetQuest("T4"), "T4 應已重編為 T2，不再有 T4 條目");
            Assert.AreEqual(3, config.OrderedQuests.Count);
            // 驗 T1 完成條件
            MainQuestInfo t1 = config.GetQuest("T1");
            Assert.AreEqual(MainQuestCompletionTypes.DialogueEnd, t1.CompletionConditionType);
            Assert.AreEqual("node_2_dialogue_complete", t1.CompletionConditionValue);
            Assert.AreEqual(0, t1.RewardGrantIds.Count, "T1 不應有 reward_grant_ids");
            // 驗 T2 sort_order
            Assert.AreEqual(2, config.GetQuest("T2").SortOrder);
        }

        /// <summary>
        /// Sprint 6 重構後的測試用配置（T0/T1/T2 三條序列）。
        /// 原 T2「幫她一次」、T3「再去認識另一個人」已刪除；原 T4 重編為 T2。
        /// </summary>
        private static MainQuestConfig BuildSimpleConfig()
        {
            MainQuestConfigData data = new MainQuestConfigData
            {
                schema_version = 1,
                main_quests = new MainQuestConfigEntry[]
                {
                    new MainQuestConfigEntry
                    {
                        quest_id = "T0",
                        display_name = "醒來的地方",
                        description = "開局任務",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.Auto,
                        completion_condition_value = "node0_dialogue_complete",
                        reward_grant_ids = "",
                        unlock_on_complete = "T1|node_0_complete",
                        sort_order = 0
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T1",
                        display_name = "認識所有人",
                        description = "任務 1",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.DialogueEnd,
                        completion_condition_value = "node_2_dialogue_complete",
                        reward_grant_ids = "",
                        unlock_on_complete = "T2|node_2_complete|exploration_open",
                        sort_order = 1
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T2",
                        display_name = "出去看看外面",
                        description = "任務 2（原 T4）",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.FirstExplore,
                        completion_condition_value = "guard_return_event_complete",
                        reward_grant_ids = "unlock_guard_sword",
                        unlock_on_complete = "guard_unlock|exploration_full_open",
                        sort_order = 2
                    }
                }
            };
            return new MainQuestConfig(data);
        }
    }
}
