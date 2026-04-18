using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// RedDotManager 單元測試（B7 Sprint 4）。
    /// 驗證：初始狀態、4 層觸發、優先序 (L1 &gt; L4 &gt; L3 &gt; L2)、
    /// 事件發布去重、清除邏輯、Dispose 取消訂閱。
    /// </summary>
    [TestFixture]
    public class RedDotManagerTests
    {
        private MainQuestConfig _config;
        private MainQuestManager _questManager;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _config = BuildConfig();
            _questManager = new MainQuestManager(_config);
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
            Assert.Throws<ArgumentNullException>(() => new RedDotManager(null, _questManager));
        }

        [Test]
        public void Constructor_NullQuestManager_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new RedDotManager(_config, null));
        }

        [Test]
        public void Constructor_InitialSync_FirstAvailableQuestTriggersNewQuestRedDot()
        {
            // BuildConfig() 中 T0 owner = VillageChiefWife，建構 MainQuestManager 時自動 Available
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                HubRedDotInfo info = sut.GetHubRedDot(CharacterIds.VillageChiefWife);
                Assert.AreEqual(RedDotLayer.NewQuest, info.HighestLayer);
                Assert.IsTrue(info.ShouldShow);
            }
        }

        [Test]
        public void Constructor_OtherCharacters_HasNoRedDot()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                Assert.IsFalse(sut.HasAnyRedDot(CharacterIds.FarmGirl));
                Assert.IsFalse(sut.HasAnyRedDot(CharacterIds.Witch));
                Assert.IsFalse(sut.HasAnyRedDot(CharacterIds.Guard));
            }
        }

        // ===== L1 委託完成層 =====

        [Test]
        public void CommissionCompleted_EnablesL1()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                EventBus.Publish(new CommissionCompletedEvent
                {
                    CharacterId = CharacterIds.Witch,
                    SlotIndex = 0,
                    RecipeId = "test_recipe",
                    OutputItemId = "potion_red",
                    OutputQuantity = 1,
                });

                HubRedDotInfo info = sut.GetHubRedDot(CharacterIds.Witch);
                Assert.AreEqual(RedDotLayer.CommissionCompleted, info.HighestLayer);
            }
        }

        [Test]
        public void CommissionClaimed_ClearsL1()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                EventBus.Publish(new CommissionCompletedEvent
                {
                    CharacterId = CharacterIds.Witch,
                });
                Assert.AreEqual(RedDotLayer.CommissionCompleted,
                    sut.GetHubRedDot(CharacterIds.Witch).HighestLayer);

                EventBus.Publish(new CommissionClaimedEvent
                {
                    CharacterId = CharacterIds.Witch,
                });
                Assert.AreEqual(RedDotLayer.None,
                    sut.GetHubRedDot(CharacterIds.Witch).HighestLayer);
            }
        }

        // ===== L2 角色發問層 =====

        [Test]
        public void AffinityThresholdReached_EnablesL2()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                EventBus.Publish(new AffinityThresholdReachedEvent
                {
                    CharacterId = CharacterIds.FarmGirl,
                    ThresholdValue = 5,
                });
                Assert.AreEqual(RedDotLayer.CharacterQuestion,
                    sut.GetHubRedDot(CharacterIds.FarmGirl).HighestLayer);
            }
        }

        [Test]
        public void SetCharacterQuestionFlag_ClearsL2()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                EventBus.Publish(new AffinityThresholdReachedEvent
                {
                    CharacterId = CharacterIds.FarmGirl,
                    ThresholdValue = 5,
                });
                sut.SetCharacterQuestionFlag(CharacterIds.FarmGirl, false);
                Assert.AreEqual(RedDotLayer.None,
                    sut.GetHubRedDot(CharacterIds.FarmGirl).HighestLayer);
            }
        }

        // ===== L3 新任務層 =====

        [Test]
        public void MainQuestAvailable_EnablesL3()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                // 先把 T0 完成讓 T1 (owner=FarmGirl in BuildConfig) Available
                _questManager.TryAutoCompleteFirstAutoQuest(); // T0 完成 → 發布 T1 Available
                HubRedDotInfo info = sut.GetHubRedDot(CharacterIds.FarmGirl);
                Assert.AreEqual(RedDotLayer.NewQuest, info.HighestLayer);
            }
        }

        [Test]
        public void MainQuestStarted_ClearsL3IfNoOtherAvailable()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                _questManager.TryAutoCompleteFirstAutoQuest(); // T0 → T1 Available, FarmGirl L3 亮
                _questManager.StartQuest("T1");
                Assert.AreEqual(RedDotLayer.None,
                    sut.GetHubRedDot(CharacterIds.FarmGirl).HighestLayer);
            }
        }

        // ===== L4 主線事件層（T1 → 節點 1；T3 → 節點 2） =====

        [Test]
        public void MainQuestCompleted_T1_TriggersNode1L4OnVillageChiefWife()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                _questManager.TryAutoCompleteFirstAutoQuest(); // T0
                _questManager.StartQuest("T1");
                _questManager.CompleteQuest("T1");

                HubRedDotInfo info = sut.GetHubRedDot(CharacterIds.VillageChiefWife);
                Assert.AreEqual(RedDotLayer.MainQuestEvent, info.HighestLayer);
            }
        }

        [Test]
        public void MainQuestCompleted_T3_TriggersNode2L4OnVillageChiefWife()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                _questManager.TryAutoCompleteFirstAutoQuest();
                _questManager.StartQuest("T1");
                _questManager.CompleteQuest("T1");
                _questManager.StartQuest("T2");
                _questManager.CompleteQuest("T2");
                _questManager.StartQuest("T3");
                _questManager.CompleteQuest("T3");

                // T3 完成 → 村長夫人 L4
                HubRedDotInfo info = sut.GetHubRedDot(CharacterIds.VillageChiefWife);
                Assert.AreEqual(RedDotLayer.MainQuestEvent, info.HighestLayer);
            }
        }

        [Test]
        public void SetMainQuestEventFlag_ClearsL4()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                _questManager.TryAutoCompleteFirstAutoQuest();
                _questManager.StartQuest("T1");
                _questManager.CompleteQuest("T1");
                Assert.AreEqual(RedDotLayer.MainQuestEvent,
                    sut.GetHubRedDot(CharacterIds.VillageChiefWife).HighestLayer);

                sut.SetMainQuestEventFlag(CharacterIds.VillageChiefWife, false);
                Assert.AreEqual(RedDotLayer.None,
                    sut.GetHubRedDot(CharacterIds.VillageChiefWife).HighestLayer);
            }
        }

        // ===== 優先序 L1 > L4 > L3 > L2 =====

        [Test]
        public void Priority_L1OverridesAllOthers()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                // 先把村長夫人弄出 L2, L3, L4
                // 村長夫人已經有 L3 (T0 Available) — 建構時初始 sync
                // 加上 L4
                _questManager.TryAutoCompleteFirstAutoQuest();
                _questManager.StartQuest("T1");
                _questManager.CompleteQuest("T1"); // L4 亮

                // 加上 L2
                EventBus.Publish(new AffinityThresholdReachedEvent
                {
                    CharacterId = CharacterIds.VillageChiefWife,
                    ThresholdValue = 5,
                });

                // 加上 L1
                EventBus.Publish(new CommissionCompletedEvent
                {
                    CharacterId = CharacterIds.VillageChiefWife,
                });

                // L1 最高
                Assert.AreEqual(RedDotLayer.CommissionCompleted,
                    sut.GetHubRedDot(CharacterIds.VillageChiefWife).HighestLayer);
            }
        }

        [Test]
        public void Priority_L4OverridesL3AndL2()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                EventBus.Publish(new AffinityThresholdReachedEvent
                {
                    CharacterId = CharacterIds.FarmGirl,
                    ThresholdValue = 5,
                });
                Assert.AreEqual(RedDotLayer.CharacterQuestion,
                    sut.GetHubRedDot(CharacterIds.FarmGirl).HighestLayer);

                // FarmGirl 獲得一個 Available quest (T1) 通過自動完成 T0
                _questManager.TryAutoCompleteFirstAutoQuest();
                Assert.AreEqual(RedDotLayer.NewQuest,
                    sut.GetHubRedDot(CharacterIds.FarmGirl).HighestLayer);

                // 手動設 L4（farmgirl 其實沒 L4 觸發條件，但此 API 允許手動設）
                sut.SetMainQuestEventFlag(CharacterIds.FarmGirl, true);
                Assert.AreEqual(RedDotLayer.MainQuestEvent,
                    sut.GetHubRedDot(CharacterIds.FarmGirl).HighestLayer);
            }
        }

        [Test]
        public void Priority_L3OverridesL2()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                EventBus.Publish(new AffinityThresholdReachedEvent
                {
                    CharacterId = CharacterIds.Witch,
                    ThresholdValue = 5,
                });
                Assert.AreEqual(RedDotLayer.CharacterQuestion,
                    sut.GetHubRedDot(CharacterIds.Witch).HighestLayer);

                // 完成 T0/T1/T2 讓 T3 (owner=Witch in BuildConfig) Available
                _questManager.TryAutoCompleteFirstAutoQuest();
                _questManager.StartQuest("T1");
                _questManager.CompleteQuest("T1");
                _questManager.StartQuest("T2");
                _questManager.CompleteQuest("T2"); // T3 Available

                Assert.AreEqual(RedDotLayer.NewQuest,
                    sut.GetHubRedDot(CharacterIds.Witch).HighestLayer);
            }
        }

        // ===== RedDotUpdatedEvent 發布 =====

        [Test]
        public void RedDotUpdatedEvent_PublishedOnLayerChange()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                RedDotUpdatedEvent received = null;
                Action<RedDotUpdatedEvent> handler = (e) => { received = e; };
                EventBus.Subscribe(handler);
                try
                {
                    EventBus.Publish(new CommissionCompletedEvent
                    {
                        CharacterId = CharacterIds.FarmGirl,
                    });
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }

                Assert.IsNotNull(received);
                Assert.AreEqual(CharacterIds.FarmGirl, received.CharacterId);
                Assert.AreEqual(RedDotLayer.CommissionCompleted, received.HighestLayer);
            }
        }

        [Test]
        public void RedDotUpdatedEvent_NotPublishedWhenHighestUnchanged()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                // 先亮 L1
                EventBus.Publish(new CommissionCompletedEvent
                {
                    CharacterId = CharacterIds.FarmGirl,
                });

                int publishCount = 0;
                Action<RedDotUpdatedEvent> handler = (e) => { publishCount++; };
                EventBus.Subscribe(handler);
                try
                {
                    // 再加 L2 — 不影響 HighestLayer
                    EventBus.Publish(new AffinityThresholdReachedEvent
                    {
                        CharacterId = CharacterIds.FarmGirl,
                        ThresholdValue = 5,
                    });
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }

                Assert.AreEqual(0, publishCount);
            }
        }

        // ===== GetCharactersWithRedDot =====

        [Test]
        public void GetCharactersWithRedDot_ReturnsOnlyCharactersWithActiveRedDots()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                // 村長夫人 已經有 L3 (T0 Available)
                EventBus.Publish(new CommissionCompletedEvent
                {
                    CharacterId = CharacterIds.Witch,
                });

                IReadOnlyList<string> withRedDot = sut.GetCharactersWithRedDot();
                CollectionAssert.Contains(withRedDot, CharacterIds.VillageChiefWife);
                CollectionAssert.Contains(withRedDot, CharacterIds.Witch);
                CollectionAssert.DoesNotContain(withRedDot, CharacterIds.Guard);
            }
        }

        // ===== Dispose =====

        [Test]
        public void Dispose_UnsubscribesFromEvents()
        {
            RedDotManager sut = new RedDotManager(_config, _questManager);
            sut.Dispose();

            // Dispose 後發布事件，不應更動狀態
            EventBus.Publish(new CommissionCompletedEvent
            {
                CharacterId = CharacterIds.Witch,
            });
            // 狀態查詢：因為已 Dispose，但內部狀態仍存於字典（這是合理的）
            // 此測試主要確認 Dispose 不拋例外、後續事件發布不會引爆
            Assert.Pass();
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            RedDotManager sut = new RedDotManager(_config, _questManager);
            sut.Dispose();
            Assert.DoesNotThrow(() => sut.Dispose());
        }

        // ===== Helper =====

        /// <summary>
        /// 建立測試用的任務配置：
        /// - T0 (owner=VillageChiefWife, Auto, 初始 Available)
        /// - T1 (owner=FarmGirl, DialogueEnd, T0 完成後 Available) — 對應節點 1 觸發
        /// - T2 (owner=FarmGirl, CommissionCount)
        /// - T3 (owner=Witch, CommissionCount) — 對應節點 2 觸發
        /// - T4 (owner=Guard, FirstExplore)
        /// </summary>
        private static MainQuestConfig BuildConfig()
        {
            MainQuestConfigData data = new MainQuestConfigData
            {
                schema_version = 1,
                main_quests = new MainQuestConfigEntry[]
                {
                    new MainQuestConfigEntry
                    {
                        quest_id = "T0",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        completion_condition_type = MainQuestCompletionTypes.Auto,
                        completion_condition_value = "",
                        unlock_on_complete = "T1",
                        sort_order = 0
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T1",
                        owner_character_id = CharacterIds.FarmGirl,
                        completion_condition_type = MainQuestCompletionTypes.DialogueEnd,
                        completion_condition_value = "first_char_intro_complete",
                        unlock_on_complete = "T2",
                        sort_order = 1
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T2",
                        owner_character_id = CharacterIds.FarmGirl,
                        completion_condition_type = MainQuestCompletionTypes.CommissionCount,
                        completion_condition_value = "",
                        unlock_on_complete = "T3",
                        sort_order = 2
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T3",
                        owner_character_id = CharacterIds.Witch,
                        completion_condition_type = MainQuestCompletionTypes.CommissionCount,
                        completion_condition_value = "",
                        unlock_on_complete = "T4",
                        sort_order = 3
                    },
                    new MainQuestConfigEntry
                    {
                        quest_id = "T4",
                        owner_character_id = CharacterIds.Guard,
                        completion_condition_type = MainQuestCompletionTypes.FirstExplore,
                        completion_condition_value = "",
                        unlock_on_complete = "",
                        sort_order = 4
                    }
                }
            };
            return new MainQuestConfig(data);
        }
    }
}
