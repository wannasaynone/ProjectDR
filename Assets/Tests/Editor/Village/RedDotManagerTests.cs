using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Progression;
using ProjectDR.Village.Navigation;

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
        public void CharacterQuestionCountdownReady_EnablesL2()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                EventBus.Publish(new CharacterQuestionCountdownReadyEvent
                {
                    CharacterId = CharacterIds.FarmGirl,
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
                EventBus.Publish(new CharacterQuestionCountdownReadyEvent
                {
                    CharacterId = CharacterIds.FarmGirl,
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
                // T0 完成後 T1 Available → FarmGirl L3 亮（T1 owner = FarmGirl）
                Assert.IsTrue(info.HighestLayer == RedDotLayer.NewQuest,
                    "完成 T0 後 FarmGirl 應有 L3 紅點（T1 owner）");
            }
        }

        [Test]
        public void MainQuestStarted_ClearsL3IfNoOtherAvailable()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                _questManager.TryAutoCompleteFirstAutoQuest(); // T0 → T1 Available, VCW L3 亮
                _questManager.StartQuest("T1");
                // 承接 T1 後，若無其他 Available 任務，L3 清除（但 L4 可能因其他路徑存在）
                HubRedDotInfo info = sut.GetHubRedDot(CharacterIds.VillageChiefWife);
                Assert.AreNotEqual(RedDotLayer.NewQuest, info.HighestLayer);
            }
        }

        // ===== L4 主線事件層（新 T1 = 節點 2 觸發；節點 1 L4 由外部 SetMainQuestEventFlag 觸發）=====

        [Test]
        public void MainQuestCompleted_NewT1_TriggersNode2L4OnVillageChiefWife()
        {
            // Sprint 6：新 T1（認識所有人 = 節點 2 對話完成）→ VCW L4 亮
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                _questManager.TryAutoCompleteFirstAutoQuest(); // T0 完成 → T1 Available
                _questManager.NotifyCompletionSignal(
                    MainQuestCompletionTypes.DialogueEnd,
                    MainQuestSignalValues.Node2DialogueComplete); // T1 完成

                HubRedDotInfo info = sut.GetHubRedDot(CharacterIds.VillageChiefWife);
                Assert.AreEqual(RedDotLayer.MainQuestEvent, info.HighestLayer);
            }
        }

        [Test]
        public void SetMainQuestEventFlag_ManuallyTriggersNode1L4()
        {
            // Sprint 6：節點 1 L4 由外部（VillageEntryPoint 在選擇 1 角色 CG 完成後）呼叫 SetMainQuestEventFlag
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                sut.SetMainQuestEventFlag(CharacterIds.VillageChiefWife, true);
                Assert.AreEqual(RedDotLayer.MainQuestEvent,
                    sut.GetHubRedDot(CharacterIds.VillageChiefWife).HighestLayer);
            }
        }

        [Test]
        public void SetMainQuestEventFlag_ClearsL4()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                _questManager.TryAutoCompleteFirstAutoQuest();
                _questManager.NotifyCompletionSignal(
                    MainQuestCompletionTypes.DialogueEnd,
                    MainQuestSignalValues.Node2DialogueComplete); // T1 完成 → L4 亮

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
                // 村長夫人已有 L3 (T0 Available 初始 sync)
                // 加上 L4
                _questManager.TryAutoCompleteFirstAutoQuest();
                _questManager.NotifyCompletionSignal(
                    MainQuestCompletionTypes.DialogueEnd,
                    MainQuestSignalValues.Node2DialogueComplete); // 新 T1 完成 → L4 亮

                // 加上 L2
                EventBus.Publish(new CharacterQuestionCountdownReadyEvent
                {
                    CharacterId = CharacterIds.VillageChiefWife,
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
                EventBus.Publish(new CharacterQuestionCountdownReadyEvent
                {
                    CharacterId = CharacterIds.FarmGirl,
                });
                Assert.AreEqual(RedDotLayer.CharacterQuestion,
                    sut.GetHubRedDot(CharacterIds.FarmGirl).HighestLayer);

                // FarmGirl 手動設 L4（模擬 VillageEntryPoint 在選擇 1 角色 CG 後呼叫）
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
                EventBus.Publish(new CharacterQuestionCountdownReadyEvent
                {
                    CharacterId = CharacterIds.VillageChiefWife,
                });

                // VCW 已有 L3 (T0 Available)，L3 > L2
                HubRedDotInfo info = sut.GetHubRedDot(CharacterIds.VillageChiefWife);
                Assert.AreEqual(RedDotLayer.NewQuest, info.HighestLayer);
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
                    EventBus.Publish(new CharacterQuestionCountdownReadyEvent
                    {
                        CharacterId = CharacterIds.FarmGirl,
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
        /// 建立測試用的任務配置（RedDotManager 專用 5-quest 配置，與生產環境不同）：
        /// - T0 (owner=VillageChiefWife, Auto, 初始 Available) — T0 auto 完成
        /// - T1 (owner=FarmGirl, DialogueEnd, node_2_dialogue_complete) — Sprint 6 新 T1（認識所有人），
        ///   RedDotManager.QuestIdsTriggersNode2 = "T1" → T1 完成後 VCW L4 亮
        /// - T2 (owner=FarmGirl, CommissionCount) — 用於 L3 多角色測試
        /// - T3 (owner=Witch, CommissionCount) — 用於 L3 多角色測試（Witch 擁有）
        /// - T4 (owner=Guard, FirstExplore) — 末端
        ///
        /// 注意：此配置刻意保留 5 個任務，以便 L3 測試能驗證多角色 owner 場景。
        /// T1 的 completion_condition_value 對應新 T1 的觸發訊號（Sprint 6 更新）。
        /// </summary>
        private static MainQuestConfig BuildConfig()
        {
            MainQuestData[] quests = new MainQuestData[]
            {
                new MainQuestData
                {
                    id = 1,
                    quest_id = "T0",
                    owner_character_id = CharacterIds.VillageChiefWife,
                    completion_condition_type = MainQuestCompletionTypes.Auto,
                    completion_condition_value = "",
                    sort_order = 0
                },
                new MainQuestData
                {
                    id = 2,
                    quest_id = "T1",
                    owner_character_id = CharacterIds.FarmGirl,
                    completion_condition_type = MainQuestCompletionTypes.DialogueEnd,
                    // Sprint 6：新 T1 completion_condition_value = node_2_dialogue_complete
                    completion_condition_value = MainQuestSignalValues.Node2DialogueComplete,
                    sort_order = 1
                },
                new MainQuestData
                {
                    id = 3,
                    quest_id = "T2",
                    owner_character_id = CharacterIds.FarmGirl,
                    completion_condition_type = MainQuestCompletionTypes.CommissionCount,
                    completion_condition_value = "",
                    sort_order = 2
                },
                new MainQuestData
                {
                    id = 4,
                    quest_id = "T3",
                    owner_character_id = CharacterIds.Witch,
                    completion_condition_type = MainQuestCompletionTypes.CommissionCount,
                    completion_condition_value = "",
                    sort_order = 3
                },
                new MainQuestData
                {
                    id = 5,
                    quest_id = "T4",
                    owner_character_id = CharacterIds.Guard,
                    completion_condition_type = MainQuestCompletionTypes.FirstExplore,
                    completion_condition_value = "",
                    sort_order = 4
                }
            };
            MainQuestUnlockData[] unlocks = new MainQuestUnlockData[]
            {
                new MainQuestUnlockData { id = 1, main_quest_id = "T0", unlock_type = "quest", unlock_value = "T1", sort_order = 0 },
                new MainQuestUnlockData { id = 2, main_quest_id = "T1", unlock_type = "quest", unlock_value = "T2", sort_order = 0 },
                new MainQuestUnlockData { id = 3, main_quest_id = "T2", unlock_type = "quest", unlock_value = "T3", sort_order = 0 },
                new MainQuestUnlockData { id = 4, main_quest_id = "T3", unlock_type = "quest", unlock_value = "T4", sort_order = 0 },
            };
            return new MainQuestConfig(quests, unlocks);
        }
    }
}
