using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Dialogue;
using ProjectDR.Village.CharacterUnlock;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Progression;
using ProjectDR.Village.Navigation;

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
        public void DialogueChoiceSelected_FarmGirlBranch_NoSeedGrant_AfterSprint6()
        {
            // Sprint 6：移除農女解鎖時的種子發放（initial-resources-config.json 已刪除 unlock_farm_girl_seed）
            // Config 中不含 unlock_farm_girl 的 grant 條目，所以 dispatcher 不應收到任何 grant。
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                _dispatcher.Dispatched.Clear();
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });
                // 新 config 無農女 grant，dispatcher 不應收到任何派發
                Assert.AreEqual(0, _dispatcher.Dispatched.Count);
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

        // ===== 節點 1 剩下那位（C7：改由 NodeDialogueCompletedEvent 路徑解鎖）=====
        // 注意：原測試「第二次 DialogueChoiceSelected 解鎖剩下那位」已被 C7 bugfix 取代。
        //       真實 config 節點 1 選項 branch = ""，OnDialogueChoiceSelected 不會觸發第二位解鎖。
        //       解鎖改由 OnNodeDialogueCompleted(node_1) 依 _node0ChosenBranch 推算並解鎖。

        [Test]
        public void Node1_AfterNode0ChoseFarmGirl_NodeDialogueCompleted_UnlocksWitch()
        {
            // C7 修復後的正確路徑：節點 1 對話完成 → 解鎖魔女（不依賴第二次 DialogueChoiceSelected）
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });
                EventBus.Publish(new NodeDialogueCompletedEvent
                {
                    NodeId = NodeDialogueController.NodeIdNode1,
                    SelectedBranchId = string.Empty
                });

                Assert.IsTrue(sut.IsUnlocked(CharacterIds.FarmGirl));
                Assert.IsTrue(sut.IsUnlocked(CharacterIds.Witch));
            }
        }

        [Test]
        public void Node1_AfterNode0ChoseWitch_NodeDialogueCompleted_UnlocksFarmGirl()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.Witch });
                EventBus.Publish(new NodeDialogueCompletedEvent
                {
                    NodeId = NodeDialogueController.NodeIdNode1,
                    SelectedBranchId = string.Empty
                });

                Assert.IsTrue(sut.IsUnlocked(CharacterIds.FarmGirl));
                Assert.IsTrue(sut.IsUnlocked(CharacterIds.Witch));
            }
        }

        [Test]
        public void Node1_SecondDialogueChoiceSelected_SameBranch_DoesNothing()
        {
            // 同 branch 的第二次 DialogueChoiceSelected（例如 UI 意外重複觸發）不解鎖任何人
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });
                _dispatcher.Dispatched.Clear();
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });
                Assert.AreEqual(0, _dispatcher.Dispatched.Count);
                Assert.IsFalse(sut.IsUnlocked(CharacterIds.Witch));
            }
        }

        // ===== 節點 1 NodeDialogueCompletedEvent 路徑（C7 bugfix）=====

        [Test]
        public void Node1DialogueCompleted_AfterFarmGirlChoice_UnlocksWitch()
        {
            // 根因復現：真實 config 節點 1 選項 branch 為空，OnDialogueChoiceSelected 不觸發解鎖。
            // 修復後，NodeDialogueCompletedEvent(node_1) 應觸發剩下那位解鎖。
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                // 節點 0：選農女（設定 _node0ChosenBranch）
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });
                Assert.IsFalse(sut.IsUnlocked(CharacterIds.Witch));

                // 節點 1 完成（空 branch 選項）
                EventBus.Publish(new NodeDialogueCompletedEvent
                {
                    NodeId = NodeDialogueController.NodeIdNode1,
                    SelectedBranchId = string.Empty
                });

                Assert.IsTrue(sut.IsUnlocked(CharacterIds.Witch),
                    "節點 1 完成後應解鎖魔女（節點 0 選農女情境）");
            }
        }

        [Test]
        public void Node1DialogueCompleted_AfterWitchChoice_UnlocksFarmGirl()
        {
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.Witch });
                Assert.IsFalse(sut.IsUnlocked(CharacterIds.FarmGirl));

                EventBus.Publish(new NodeDialogueCompletedEvent
                {
                    NodeId = NodeDialogueController.NodeIdNode1,
                    SelectedBranchId = string.Empty
                });

                Assert.IsTrue(sut.IsUnlocked(CharacterIds.FarmGirl),
                    "節點 1 完成後應解鎖農女（節點 0 選魔女情境）");
            }
        }

        [Test]
        public void Node1DialogueCompleted_WithoutNode0Choice_DoesNothing()
        {
            // 若 _node0ChosenBranch 為 null（異常情況），節點 1 完成不應解鎖任何人
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new NodeDialogueCompletedEvent
                {
                    NodeId = NodeDialogueController.NodeIdNode1,
                    SelectedBranchId = string.Empty
                });

                Assert.IsFalse(sut.IsUnlocked(CharacterIds.FarmGirl));
                Assert.IsFalse(sut.IsUnlocked(CharacterIds.Witch));
            }
        }

        [Test]
        public void Node1DialogueCompleted_IsIdempotent()
        {
            // 節點 1 完成事件重複發布不應解鎖已解鎖角色兩次（事件數 = 1）
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = NodeDialogueBranchIds.FarmGirl });

                int count = 0;
                Action<CharacterUnlockedEvent> handler = (e) => { if (e.CharacterId == CharacterIds.Witch) count++; };
                EventBus.Subscribe(handler);
                try
                {
                    EventBus.Publish(new NodeDialogueCompletedEvent { NodeId = NodeDialogueController.NodeIdNode1, SelectedBranchId = string.Empty });
                    EventBus.Publish(new NodeDialogueCompletedEvent { NodeId = NodeDialogueController.NodeIdNode1, SelectedBranchId = string.Empty });
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }

                Assert.AreEqual(1, count, "魔女解鎖事件只應發布一次（ForceUnlock 已處理冪等性）");
            }
        }

        [Test]
        public void Node2DialogueCompleted_DoesNotUnlockCharacters()
        {
            // node_2 完成不應觸發角色解鎖（確保 C7 修復不影響 node_2 路徑）
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new NodeDialogueCompletedEvent
                {
                    NodeId = NodeDialogueController.NodeIdNode2,
                    SelectedBranchId = string.Empty
                });

                Assert.IsFalse(sut.IsUnlocked(CharacterIds.FarmGirl));
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
        public void GuardReturnEventCompleted_DoesNotDispatchSwordGrant()
        {
            // Sprint 6 C9：守衛歸來事件完成不直接贈劍，劍由玩家主動發問「要拿劍」時派發。
            // 此測試更新自舊版 GuardReturnEventCompleted_DispatchesSwordGrant（已過時），
            // 舊版斷言 dispatch count = 1（unlock_guard_sword），C9 後正確值為 0。
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                _dispatcher.Dispatched.Clear();
                EventBus.Publish(new GuardReturnEventCompletedEvent());
                Assert.AreEqual(0, _dispatcher.Dispatched.Count,
                    "守衛歸來事件完成後不應派發任何 grant（劍由玩家主動發問取得）");
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
        public void MainQuestCompletedEvent_T1_UnlocksExplorationFeature()
        {
            // Sprint 6：新 T1（認識所有人）完成時解鎖探索功能，不再是舊 T3
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                ExplorationFeatureUnlockedEvent received = null;
                Action<ExplorationFeatureUnlockedEvent> handler = (e) => { received = e; };
                EventBus.Subscribe(handler);
                try
                {
                    EventBus.Publish(new MainQuestCompletedEvent { QuestId = "T1" });
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
        public void MainQuestCompletedEvent_T0_DoesNotUnlockExploration()
        {
            // T0 完成不應解鎖探索
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new MainQuestCompletedEvent { QuestId = "T0" });
                Assert.IsFalse(sut.IsExplorationFeatureUnlocked);
            }
        }

        [Test]
        public void MainQuestCompletedEvent_T2_DoesNotUnlockExploration()
        {
            // T2 完成不應解鎖探索（探索已由 T1 解鎖，T2 是守衛歸來）
            using (CharacterUnlockManager sut = new CharacterUnlockManager(_config, _dispatcher))
            {
                EventBus.Publish(new MainQuestCompletedEvent { QuestId = "T2" });
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
            // Sprint 6 B2：移除 unlock_farm_girl_seed、unlock_witch_herb；保留 initial_backpack_node0 與 unlock_guard_sword
            // A11 改造（2026-04-22）：加 id 欄位，改 GuardReturnEvent → GuardSwordAsked
            InitialResourcesConfigData data = new InitialResourcesConfigData
            {
                schema_version = 2,
                grants = new InitialResourceGrantData[]
                {
                    new InitialResourceGrantData
                    {
                        id = 1,
                        grant_id = "initial_backpack_node0",
                        trigger_id = InitialResourcesTriggerIds.Node0Start,
                        item_id = "",
                        quantity = 0
                    },
                    new InitialResourceGrantData
                    {
                        id = 2,
                        grant_id = "unlock_guard_sword",
                        trigger_id = InitialResourcesTriggerIds.GuardSwordAsked,
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
