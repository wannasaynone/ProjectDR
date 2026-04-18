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
            MainQuestConfig config = BuildSimpleConfig();
            MainQuestInfo t1 = config.GetQuest("T1");
            Assert.IsNotNull(t1);
            Assert.AreEqual("first_char_intro_complete", t1.CompletionConditionValue);
            Assert.AreEqual(MainQuestCompletionTypes.DialogueEnd, t1.CompletionConditionType);
        }

        [Test]
        public void RewardGrantIds_ParsedFromPipeSeparated()
        {
            MainQuestConfig config = BuildSimpleConfig();
            MainQuestInfo t1 = config.GetQuest("T1");
            Assert.AreEqual(2, t1.RewardGrantIds.Count);
            Assert.AreEqual("unlock_farm_girl_seed", t1.RewardGrantIds[0]);
            Assert.AreEqual("unlock_witch_herb", t1.RewardGrantIds[1]);
        }

        [Test]
        public void UnlockOnComplete_ParsedFromPipeSeparated()
        {
            MainQuestConfig config = BuildSimpleConfig();
            MainQuestInfo t3 = config.GetQuest("T3");
            Assert.AreEqual(3, t3.UnlockOnComplete.Count);
            Assert.Contains("T4", (System.Collections.ICollection)t3.UnlockOnComplete);
            Assert.Contains("node_2_complete", (System.Collections.ICollection)t3.UnlockOnComplete);
            Assert.Contains("exploration_open", (System.Collections.ICollection)t3.UnlockOnComplete);
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
            Assert.IsNotNull(config.GetQuest("T4"));
            Assert.AreEqual(5, config.OrderedQuests.Count);
        }

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
                        display_name = "開局",
                        description = "開局任務",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.Auto,
                        completion_condition_value = "",
                        reward_grant_ids = "",
                        unlock_on_complete = "T1",
                        sort_order = 0
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T1",
                        display_name = "先去認識她們",
                        description = "任務 1",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.DialogueEnd,
                        completion_condition_value = "first_char_intro_complete",
                        reward_grant_ids = "unlock_farm_girl_seed|unlock_witch_herb",
                        unlock_on_complete = "T2",
                        sort_order = 1
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T2",
                        display_name = "幫她一次",
                        description = "任務 2",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.CommissionCount,
                        completion_condition_value = "choice1_character|1",
                        reward_grant_ids = "",
                        unlock_on_complete = "T3",
                        sort_order = 2
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T3",
                        display_name = "再去認識另一個人",
                        description = "任務 3",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.CommissionCount,
                        completion_condition_value = "choice2_character|1",
                        reward_grant_ids = "",
                        unlock_on_complete = "T4|node_2_complete|exploration_open",
                        sort_order = 3
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T4",
                        display_name = "出去看看外面",
                        description = "任務 4",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.FirstExplore,
                        completion_condition_value = "guard_return_event_complete",
                        reward_grant_ids = "unlock_guard_sword",
                        unlock_on_complete = "guard_unlock",
                        sort_order = 4
                    }
                }
            };
            return new MainQuestConfig(data);
        }
    }
}
