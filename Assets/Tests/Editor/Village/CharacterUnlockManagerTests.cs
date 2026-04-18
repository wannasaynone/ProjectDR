using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// CharacterUnlockManager 單元測試。
    /// 驗證：初始解鎖、VN 節點選擇解鎖、守衛事件解鎖、探索功能解鎖、
    /// grant 派發至 dispatcher、事件發布。
    /// </summary>
    [TestFixture]
    public class CharacterUnlockManagerTests
    {
        private InitialResourcesConfig _config;
        private RecordingDispatcher _dispatcher;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _config = BuildConfig();
            _dispatcher = new RecordingDispatcher();
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
            Assert.Throws<ArgumentNullException>(
                () => new CharacterUnlockManager(null, _dispatcher));
        }

        [Test]
        public void Constructor_NullDispatcher_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new CharacterUnlockManager(_config, null));
        }

        [Test]
        public void Constructor_InitialState_OnlyVillageChiefWifeUnlocked()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                Assert.IsTrue(sut.IsUnlocked(CharacterIds.VillageChiefWife));
                Assert.IsFalse(sut.IsUnlocked(CharacterIds.FarmGirl));
                Assert.IsFalse(sut.IsUnlocked(CharacterIds.Witch));
                Assert.IsFalse(sut.IsUnlocked(CharacterIds.Guard));
            }
        }

        [Test]
        public void Constructor_DispatchesNode0StartGrants()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                Assert.AreEqual(1, _dispatcher.Dispatched.Count);
                Assert.AreEqual("initial_backpack_node0", _dispatcher.Dispatched[0].GrantId);
            }
        }

        // ===== VN 節點 0 選擇 =====

        [Test]
        public void DialogueChoiceSelected_FarmGirlBranch_UnlocksFarmGirl()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });

                Assert.IsTrue(sut.IsUnlocked(CharacterIds.FarmGirl));
                Assert.IsFalse(sut.IsUnlocked(CharacterIds.Witch));
                Assert.AreEqual(NodeDialogueBranchIds.FarmGirl, sut.Node0ChosenBranch);
            }
        }

        [Test]
        public void DialogueChoiceSelected_FarmGirlBranch_DispatchesSeedGrant()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                _dispatcher.Dispatched.Clear();
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });
                Assert.AreEqual(1, _dispatcher.Dispatched.Count);
                Assert.AreEqual("unlock_farm_girl_seed", _dispatcher.Dispatched[0].GrantId);
            }
        }

        [Test]
        public void DialogueChoiceSelected_WitchBranch_UnlocksWitch()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.Witch });
                Assert.IsTrue(sut.IsUnlocked(CharacterIds.Witch));
                Assert.AreEqual(NodeDialogueBranchIds.Witch, sut.Node0ChosenBranch);
            }
        }

        [Test]
        public void DialogueChoiceSelected_PublishesCharacterUnlockedEvent()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                CharacterUnlockedEvent received = null;
                Action<CharacterUnlockedEvent> handler = (e) => { received = e; };
                EventBus.Subscribe(handler);
                try
                {
                    EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }
                Assert.IsNotNull(received);
                Assert.AreEqual(CharacterIds.FarmGirl, received.CharacterId);
            }
        }

        // ===== 節點 1 剩下那位 =====

        [Test]
        public void Node1_AfterNode0ChoseFarmGirl_Choose_Witch_UnlocksWitch()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.Witch });

                Assert.IsTrue(sut.IsUnlocked(CharacterIds.FarmGirl));
                Assert.IsTrue(sut.IsUnlocked(CharacterIds.Witch));
            }
        }

        [Test]
        public void Node1_AfterNode0ChoseWitch_Choose_FarmGirl_UnlocksFarmGirl()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.Witch });
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });

                Assert.IsTrue(sut.IsUnlocked(CharacterIds.FarmGirl));
                Assert.IsTrue(sut.IsUnlocked(CharacterIds.Witch));
            }
        }

        [Test]
        public void Node1_SameBranchAsNode0_DoesNothingAdditional()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });
                _dispatcher.Dispatched.Clear();
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });
                Assert.AreEqual(0, _dispatcher.Dispatched.Count);
                Assert.IsFalse(sut.IsUnlocked(CharacterIds.Witch));
            }
        }

        // ===== 未知 ChoiceId 不處理 =====

        [Test]
        public void DialogueChoiceSelected_UnknownChoiceId_NoEffect()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = "unrelated_choice" });
                Assert.IsFalse(sut.IsUnlocked(CharacterIds.FarmGirl));
                Assert.IsFalse(sut.IsUnlocked(CharacterIds.Witch));
            }
        }

        // ===== 守衛歸來事件 =====

        [Test]
        public void GuardReturnEventCompleted_UnlocksGuard()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new GuardReturnEventCompletedEvent());
                Assert.IsTrue(sut.IsUnlocked(CharacterIds.Guard));
            }
        }

        [Test]
        public void GuardReturnEventCompleted_DispatchesSwordGrant()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                _dispatcher.Dispatched.Clear();
                EventBus.Publish(new GuardReturnEventCompletedEvent());
                Assert.AreEqual(1, _dispatcher.Dispatched.Count);
                Assert.AreEqual("unlock_guard_sword", _dispatcher.Dispatched[0].GrantId);
            }
        }

        // ===== 探索功能解鎖 =====

        [Test]
        public void ExplorationFeatureUnlocked_InitiallyFalse()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                Assert.IsFalse(sut.IsExplorationFeatureUnlocked);
            }
        }

        [Test]
        public void MainQuestCompletedEvent_T3_UnlocksExplorationFeature()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                ExplorationFeatureUnlockedEvent received = null;
                Action<ExplorationFeatureUnlockedEvent> handler = (e) => { received = e; };
                EventBus.Subscribe(handler);
                try
                {
                    EventBus.Publish(new MainQuestCompletedEvent { QuestId = "T3" });
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }
                Assert.IsNotNull(received);
                Assert.IsTrue(sut.IsExplorationFeatureUnlocked);
            }
        }

        [Test]
        public void MainQuestCompletedEvent_T1_DoesNotUnlockExploration()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new MainQuestCompletedEvent { QuestId = "T1" });
                Assert.IsFalse(sut.IsExplorationFeatureUnlocked);
            }
        }

        [Test]
        public void ForceUnlockExplorationFeature_Idempotent()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                int count = 0;
                Action<ExplorationFeatureUnlockedEvent> handler = (e) => { count++; };
                EventBus.Subscribe(handler);
                try
                {
                    sut.ForceUnlockExplorationFeature();
                    sut.ForceUnlockExplorationFeature();
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }
                Assert.AreEqual(1, count);
            }
        }

        // ===== ForceUnlock =====

        [Test]
        public void ForceUnlock_NewCharacter_Unlocks()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                sut.ForceUnlock(CharacterIds.Guard);
                Assert.IsTrue(sut.IsUnlocked(CharacterIds.Guard));
            }
        }

        [Test]
        public void ForceUnlock_AlreadyUnlocked_NoSecondEvent()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                int count = 0;
                Action<CharacterUnlockedEvent> handler = (e) => { count++; };
                EventBus.Subscribe(handler);
                try
                {
                    sut.ForceUnlock(CharacterIds.VillageChiefWife);
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }
                Assert.AreEqual(0, count);
            }
        }

        // ===== Dispose =====

        [Test]
        public void Dispose_UnsubscribesFromEvents()
        {
            CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher);
            sut.Dispose();

            // dispose 後再 publish 不應生效
            EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });
            Assert.IsFalse(sut.IsUnlocked(CharacterIds.FarmGirl));
        }

        [Test]
        public void Dispose_Twice_NoThrow()
        {
            CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher);
            sut.Dispose();
            Assert.DoesNotThrow(() => sut.Dispose());
        }

        // ===== GetUnlockedCharacters =====

        [Test]
        public void GetUnlockedCharacters_ReflectsInternalState()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                IReadOnlyCollection<string> initial = sut.GetUnlockedCharacters();
                Assert.AreEqual(1, initial.Count);

                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });
                Assert.AreEqual(2, sut.GetUnlockedCharacters().Count);
            }
        }

        // ===== Helpers =====

        private static InitialResourcesConfig BuildConfig()
        {
            InitialResourcesConfigData data = new InitialResourcesConfigData
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
                        grant_id = "unlock_farm_girl_seed",
                        trigger_id = InitialResourcesTriggerIds.UnlockFarmGirl,
                        item_id = "seed_tomato",
                        quantity = 3
                    },
                    new InitialResourceGrantData
                    {
                        grant_id = "unlock_witch_herb",
                        trigger_id = InitialResourcesTriggerIds.UnlockWitch,
                        item_id = "herb_green",
                        quantity = 3
                    },
                    new InitialResourceGrantData
                    {
                        grant_id = "unlock_guard_sword",
                        trigger_id = InitialResourcesTriggerIds.GuardReturnEvent,
                        item_id = "gift_sword_wooden",
                        quantity = 1
                    }
                }
            };
            return new InitialResourcesConfig(data);
        }

        /// <summary>測試用的 IInitialResourceDispatcher，記錄所有派發的 grant。</summary>
        private class RecordingDispatcher : IInitialResourceDispatcher
        {
            public List<InitialResourceGrant> Dispatched { get; } = new List<InitialResourceGrant>();

            public void Dispatch(InitialResourceGrant grant)
            {
                Dispatched.Add(grant);
            }
        }
    }
}
