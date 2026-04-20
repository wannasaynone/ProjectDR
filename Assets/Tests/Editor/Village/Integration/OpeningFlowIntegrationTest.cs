// OpeningFlowIntegrationTest — Sprint 4 C1 端到端 Loop 整合測試。
// 驗證 TEST 1 的完整開場流程：
//   OpeningSequenceController.StartOpeningSequence()
//   → CGPlayer.PlayIntroCG("VillageChiefWife") 完成
//   → NodeDialogueController 播節點 0 intro + dialogue
//   → DialogueChoicePresentedEvent 發布（2 個選項）
//   → DialogueManager.SelectChoice("farm_girl") 或 "witch"
//   → CharacterUnlockManager.CharacterUnlockedEvent 發布（選的那位）
//   → InitialResourceDispatcher 發放初始資源（3 顆種子或 3 顆綠藥草）
//   → OpeningSequenceCompletedEvent 發布
//
// 使用真實 Manager（不 mock）— 透過 FakeCGPlayer 讓 CG 即時完成，其餘流程全實際跑。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village.Integration
{
    [TestFixture]
    public class OpeningFlowIntegrationTest
    {
        private BackpackManager _backpack;
        private StorageManager _storage;
        private DialogueManager _dialogueManager;
        private InitialResourcesConfig _resourcesConfig;
        private InitialResourceDispatcher _dispatcher;
        private CharacterUnlockManager _unlockManager;
        private NodeDialogueConfig _nodeConfig;
        private NodeDialogueController _nodeController;
        private FakeCGPlayer _cgPlayer;
        private OpeningSequenceController _openingController;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            _backpack = new BackpackManager(20, 99);
            _storage = new StorageManager(100, 99);
            _dialogueManager = new DialogueManager();

            _resourcesConfig = BuildInitialResourcesConfig();
            _dispatcher = new InitialResourceDispatcher(_backpack, _storage);
            _unlockManager = new CharacterUnlockManager(_resourcesConfig, _dispatcher);

            _nodeConfig = BuildNodeDialogueConfig();
            _nodeController = new NodeDialogueController(_dialogueManager, _nodeConfig);

            _cgPlayer = new FakeCGPlayer();
            _openingController = new OpeningSequenceController(_cgPlayer, _nodeController);
        }

        [TearDown]
        public void TearDown()
        {
            _openingController?.Dispose();
            _nodeController?.Dispose();
            _unlockManager?.Dispose();
            EventBus.ForceClearAll();
        }

        // ===== TEST 1-A：初始狀態 =====

        [Test]
        public void InitialState_OnlyVillageChiefWifeUnlocked()
        {
            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.VillageChiefWife));
            Assert.IsFalse(_unlockManager.IsUnlocked(CharacterIds.FarmGirl));
            Assert.IsFalse(_unlockManager.IsUnlocked(CharacterIds.Witch));
            Assert.IsFalse(_unlockManager.IsUnlocked(CharacterIds.Guard));
            Assert.IsFalse(_unlockManager.IsExplorationFeatureUnlocked);
        }

        // ===== TEST 1-B：CG 播放 → 節點對話啟動 =====

        [Test]
        public void StartOpeningSequence_TriggersCGPlayback_WithVillageChiefWife()
        {
            _cgPlayer.AutoComplete = false;
            _openingController.StartOpeningSequence();
            Assert.AreEqual(CharacterIds.VillageChiefWife, _cgPlayer.LastCharacterId);
            Assert.IsTrue(_openingController.IsRunning);
        }

        [Test]
        public void CGComplete_TriggersNodeDialogue()
        {
            _cgPlayer.AutoComplete = true;
            _openingController.StartOpeningSequence();
            Assert.IsTrue(_nodeController.IsPlaying);
            Assert.AreEqual(NodeDialogueController.NodeIdNode0, _nodeController.ActiveNodeId);
        }

        // ===== TEST 1-C：選項發布與玩家選擇 =====

        [Test]
        public void OpeningSequence_PublishesChoicePresentedEventWithTwoOptions()
        {
            DialogueChoicePresentedEvent received = null;
            Action<DialogueChoicePresentedEvent> handler = (e) => received = e;
            EventBus.Subscribe(handler);
            try
            {
                _cgPlayer.AutoComplete = true;
                _openingController.StartOpeningSequence();
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            Assert.IsNotNull(received, "未收到 DialogueChoicePresentedEvent");
            Assert.AreEqual(2, received.Choices.Count);
        }

        [Test]
        public void SelectChoice_FarmGirl_UnlocksFarmGirl()
        {
            _cgPlayer.AutoComplete = true;
            _openingController.StartOpeningSequence();
            _dialogueManager.SelectChoice(NodeDialogueBranchIds.FarmGirl);

            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.FarmGirl));
            Assert.IsFalse(_unlockManager.IsUnlocked(CharacterIds.Witch));
        }

        [Test]
        public void SelectChoice_Witch_UnlocksWitch()
        {
            _cgPlayer.AutoComplete = true;
            _openingController.StartOpeningSequence();
            _dialogueManager.SelectChoice(NodeDialogueBranchIds.Witch);

            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.Witch));
            Assert.IsFalse(_unlockManager.IsUnlocked(CharacterIds.FarmGirl));
        }

        // ===== TEST 1-D：選擇後初始資源（Sprint 6：農女/魔女無初始資源）=====

        [Test]
        public void SelectFarmGirl_NoSeedDispatch_AfterSprint6()
        {
            // Sprint 6 B2：農女解鎖不再發放種子，物資改為依賴探索
            _cgPlayer.AutoComplete = true;
            _openingController.StartOpeningSequence();
            _dialogueManager.SelectChoice(NodeDialogueBranchIds.FarmGirl);

            int seedsInBackpack = _backpack.GetItemCount("seed_tomato");
            Assert.AreEqual(0, seedsInBackpack, "Sprint 6 後農女分支不應發放番茄種子");
        }

        [Test]
        public void SelectWitch_NoHerbDispatch_AfterSprint6()
        {
            // Sprint 6 B2：魔女解鎖不再發放藥草，物資改為依賴探索
            _cgPlayer.AutoComplete = true;
            _openingController.StartOpeningSequence();
            _dialogueManager.SelectChoice(NodeDialogueBranchIds.Witch);

            int herbs = _backpack.GetItemCount("herb_green");
            Assert.AreEqual(0, herbs, "Sprint 6 後魔女分支不應發放綠藥草");
        }

        // ===== TEST 1-E：CharacterUnlockedEvent 發布 =====

        [Test]
        public void SelectFarmGirl_PublishesCharacterUnlockedEvent()
        {
            CharacterUnlockedEvent captured = null;
            Action<CharacterUnlockedEvent> handler = (e) => captured = e;
            EventBus.Subscribe(handler);
            try
            {
                _cgPlayer.AutoComplete = true;
                _openingController.StartOpeningSequence();
                _dialogueManager.SelectChoice(NodeDialogueBranchIds.FarmGirl);
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            Assert.IsNotNull(captured);
            Assert.AreEqual(CharacterIds.FarmGirl, captured.CharacterId);
        }

        // ===== TEST 1-F：完整流程 → OpeningSequenceCompletedEvent =====

        [Test]
        public void FullOpeningFlow_FarmGirlBranch_PublishesOpeningSequenceCompleted()
        {
            bool completedReceived = false;
            Action<OpeningSequenceCompletedEvent> handler = (e) => completedReceived = true;
            EventBus.Subscribe(handler);
            try
            {
                _cgPlayer.AutoComplete = true;
                _openingController.StartOpeningSequence();
                _dialogueManager.SelectChoice(NodeDialogueBranchIds.FarmGirl);

                // 推進對話至結束
                while (_dialogueManager.IsActive && _dialogueManager.Advance()) { }
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            Assert.IsTrue(completedReceived, "完整流程結束後未收到 OpeningSequenceCompletedEvent");
            Assert.IsFalse(_openingController.IsRunning);
        }

        // ===== TEST 1-G：OpeningSequenceStartedEvent =====

        [Test]
        public void StartOpeningSequence_PublishesStartedEvent()
        {
            bool startedReceived = false;
            Action<OpeningSequenceStartedEvent> handler = (e) => startedReceived = true;
            EventBus.Subscribe(handler);
            try
            {
                _cgPlayer.AutoComplete = false;
                _openingController.StartOpeningSequence();
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
            Assert.IsTrue(startedReceived);
        }

        // ===== Helpers =====

        private static InitialResourcesConfig BuildInitialResourcesConfig()
        {
            // Sprint 6 B2：移除 unlock_farm_girl_seed、unlock_witch_herb；角色解鎖時不再發放物資
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
                        quantity = 0,
                    },
                },
            });
        }

        private static NodeDialogueConfig BuildNodeDialogueConfig()
        {
            return new NodeDialogueConfig(new NodeDialogueConfigData
            {
                schema_version = 1,
                node_dialogue_lines = new NodeDialogueLineData[]
                {
                    // 節點 0
                    new NodeDialogueLineData { line_id = "n0_1", node_id = "node_0", sequence = 1, text = "開場：村長夫人出現", line_type = "narration", choice_branch = "" },
                    new NodeDialogueLineData { line_id = "n0_2", node_id = "node_0", sequence = 2, text = "你醒來了", line_type = "dialogue", choice_branch = "" },
                    new NodeDialogueLineData { line_id = "n0_3", node_id = "node_0", sequence = 3, text = "你想先見誰？", line_type = "choice_prompt", choice_branch = "" },
                    new NodeDialogueLineData { line_id = "n0_c1", node_id = "node_0", sequence = 4, text = "農女", line_type = "choice_option", choice_branch = "farm_girl" },
                    new NodeDialogueLineData { line_id = "n0_c2", node_id = "node_0", sequence = 5, text = "魔女", line_type = "choice_option", choice_branch = "witch" },
                    new NodeDialogueLineData { line_id = "n0_r1", node_id = "node_0", sequence = 6, text = "那去農場吧", line_type = "choice_response", choice_branch = "farm_girl" },
                    new NodeDialogueLineData { line_id = "n0_r2", node_id = "node_0", sequence = 7, text = "那去煉金台吧", line_type = "choice_response", choice_branch = "witch" },

                    // 節點 1
                    new NodeDialogueLineData { line_id = "n1_1", node_id = "node_1", sequence = 1, text = "再介紹下一位", line_type = "dialogue", choice_branch = "" },
                    new NodeDialogueLineData { line_id = "n1_c1", node_id = "node_1", sequence = 2, text = "好", line_type = "choice_option", choice_branch = "farm_girl" },
                    new NodeDialogueLineData { line_id = "n1_c2", node_id = "node_1", sequence = 3, text = "好", line_type = "choice_option", choice_branch = "witch" },

                    // 節點 2
                    new NodeDialogueLineData { line_id = "n2_1", node_id = "node_2", sequence = 1, text = "探索開了", line_type = "dialogue", choice_branch = "" },
                },
            });
        }

        /// <summary>測試用 FakeCGPlayer — AutoComplete=true 立即完成，否則保留回呼供手動觸發。</summary>
        private class FakeCGPlayer : ICGPlayer
        {
            public bool AutoComplete { get; set; } = true;
            public string LastCharacterId { get; private set; }
            public int CallCount { get; private set; }
            private Action _storedComplete;

            public void PlayIntroCG(string characterId, Action onComplete)
            {
                LastCharacterId = characterId;
                CallCount++;
                if (AutoComplete)
                {
                    onComplete?.Invoke();
                }
                else
                {
                    _storedComplete = onComplete;
                }
            }

            public void InvokeStoredComplete()
            {
                _storedComplete?.Invoke();
                _storedComplete = null;
            }
        }
    }
}
