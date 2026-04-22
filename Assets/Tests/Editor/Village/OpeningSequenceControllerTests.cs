using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.OpeningSequence;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.Dialogue;
using ProjectDR.Village.CG;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// OpeningSequenceController 單元測試（B9 Sprint 4）。
    /// 驗證：CG 播放 → 節點 0 對話 → 開場完成事件的完整流程。
    /// </summary>
    [TestFixture]
    public class OpeningSequenceControllerTests
    {
        private DialogueManager _dialogueManager;
        private NodeDialogueConfig _nodeConfig;
        private NodeDialogueController _nodeController;
        private FakeCGPlayer _cgPlayer;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _dialogueManager = new DialogueManager();
            _nodeConfig = BuildNodeConfig();
            _nodeController = new NodeDialogueController(_dialogueManager, _nodeConfig);
            _cgPlayer = new FakeCGPlayer();
        }

        [TearDown]
        public void TearDown()
        {
            _nodeController?.Dispose();
            EventBus.ForceClearAll();
        }

        // ===== 建構 =====

        [Test]
        public void Constructor_NullCGPlayer_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new OpeningSequenceController(null, _nodeController));
        }

        [Test]
        public void Constructor_NullNodeController_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new OpeningSequenceController(_cgPlayer, null));
        }

        [Test]
        public void Constructor_InitialState_NotRunning()
        {
            using (OpeningSequenceController sut = new OpeningSequenceController(_cgPlayer, _nodeController))
            {
                Assert.IsFalse(sut.IsRunning);
            }
        }

        // ===== StartOpeningSequence 流程 =====

        [Test]
        public void StartOpeningSequence_CallsCGPlayerWithVillageChiefWife()
        {
            using (OpeningSequenceController sut = new OpeningSequenceController(_cgPlayer, _nodeController))
            {
                _cgPlayer.AutoComplete = false; // 暫停自動完成
                sut.StartOpeningSequence();

                Assert.AreEqual(CharacterIds.VillageChiefWife, _cgPlayer.LastCharacterId);
                Assert.IsTrue(sut.IsRunning);
            }
        }

        [Test]
        public void StartOpeningSequence_PublishesStartedEvent()
        {
            using (OpeningSequenceController sut = new OpeningSequenceController(_cgPlayer, _nodeController))
            {
                _cgPlayer.AutoComplete = false;
                bool received = false;
                Action<OpeningSequenceStartedEvent> handler = (e) => { received = true; };
                EventBus.Subscribe(handler);
                try
                {
                    sut.StartOpeningSequence();
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }
                Assert.IsTrue(received);
            }
        }

        [Test]
        public void CGCompletion_TriggersNodeDialogue()
        {
            using (OpeningSequenceController sut = new OpeningSequenceController(_cgPlayer, _nodeController))
            {
                _cgPlayer.AutoComplete = true; // 自動完成 CG
                sut.StartOpeningSequence();

                // CG 自動完成後，節點 0 應已啟動
                Assert.IsTrue(_nodeController.IsPlaying);
                Assert.AreEqual("node_0", _nodeController.ActiveNodeId);
            }
        }

        [Test]
        public void FullFlow_CGCompleteAndNodeComplete_PublishesOpeningSequenceCompleted()
        {
            using (OpeningSequenceController sut = new OpeningSequenceController(_cgPlayer, _nodeController))
            {
                _cgPlayer.AutoComplete = true;

                bool completedReceived = false;
                Action<OpeningSequenceCompletedEvent> handler = (e) => { completedReceived = true; };
                EventBus.Subscribe(handler);

                try
                {
                    sut.StartOpeningSequence();

                    // 模擬玩家選擇 farm_girl 分支
                    _dialogueManager.SelectChoice("farm_girl");

                    // 推進對話至結束
                    while (_dialogueManager.IsActive && _dialogueManager.Advance()) { }
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }

                Assert.IsTrue(completedReceived);
                Assert.IsFalse(sut.IsRunning);
            }
        }

        [Test]
        public void StartOpeningSequence_CalledTwice_Ignored()
        {
            using (OpeningSequenceController sut = new OpeningSequenceController(_cgPlayer, _nodeController))
            {
                _cgPlayer.AutoComplete = false;
                sut.StartOpeningSequence();
                int callsBefore = _cgPlayer.CallCount;

                sut.StartOpeningSequence(); // 應被忽略
                Assert.AreEqual(callsBefore, _cgPlayer.CallCount);
            }
        }

        // ===== Dispose =====

        [Test]
        public void Dispose_DoesNotThrow()
        {
            OpeningSequenceController sut = new OpeningSequenceController(_cgPlayer, _nodeController);
            Assert.DoesNotThrow(() => sut.Dispose());
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            OpeningSequenceController sut = new OpeningSequenceController(_cgPlayer, _nodeController);
            sut.Dispose();
            Assert.DoesNotThrow(() => sut.Dispose());
        }

        // ===== Helper =====

        private static NodeDialogueConfig BuildNodeConfig()
        {
            return new NodeDialogueConfig(new NodeDialogueConfigData
            {
                schema_version = 1,
                node_dialogue_lines = new NodeDialogueLineData[]
                {
                    new NodeDialogueLineData { line_id = "n0_1", node_id = "node_0", sequence = 1, text = "intro", line_type = "dialogue", choice_branch = "" },
                    new NodeDialogueLineData { line_id = "n0_2", node_id = "node_0", sequence = 2, text = "pick", line_type = "choice_prompt", choice_branch = "" },
                    new NodeDialogueLineData { line_id = "n0_c1", node_id = "node_0", sequence = 3, text = "A", line_type = "choice_option", choice_branch = "farm_girl" },
                    new NodeDialogueLineData { line_id = "n0_c2", node_id = "node_0", sequence = 4, text = "B", line_type = "choice_option", choice_branch = "witch" },
                    new NodeDialogueLineData { line_id = "n0_r1", node_id = "node_0", sequence = 5, text = "resp A", line_type = "choice_response", choice_branch = "farm_girl" },
                    new NodeDialogueLineData { line_id = "n0_r2", node_id = "node_0", sequence = 6, text = "resp B", line_type = "choice_response", choice_branch = "witch" },
                },
            });
        }

        /// <summary>
        /// 測試用 Fake CG Player。
        /// AutoComplete = true 時，PlayIntroCG 會立即執行 onComplete。
        /// AutoComplete = false 時，可透過 InvokeStoredComplete 手動觸發完成。
        /// </summary>
        private class FakeCGPlayer : ICGPlayer
        {
            public bool AutoComplete { get; set; } = true;
            public int CallCount { get; private set; }
            public string LastCharacterId { get; private set; }
            private Action _storedComplete;

            public void PlayIntroCG(string characterId, Action onComplete)
            {
                CallCount++;
                LastCharacterId = characterId;
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
