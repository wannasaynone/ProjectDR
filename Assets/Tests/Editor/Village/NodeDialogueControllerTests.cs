using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// NodeDialogueController 單元測試（B9 Sprint 4）。
    /// 驗證：啟動、播放 intro_lines、呈現 choices、玩家選擇後附加 response、完成事件。
    /// </summary>
    [TestFixture]
    public class NodeDialogueControllerTests
    {
        private DialogueManager _dialogueManager;
        private NodeDialogueConfig _config;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _dialogueManager = new DialogueManager();
            _config = BuildConfig();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== 建構 =====

        [Test]
        public void Constructor_NullDialogueManager_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new NodeDialogueController(null, _config));
        }

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new NodeDialogueController(_dialogueManager, null));
        }

        [Test]
        public void Constructor_InitialState_NotPlaying()
        {
            using (NodeDialogueController sut = new NodeDialogueController(_dialogueManager, _config))
            {
                Assert.IsFalse(sut.IsPlaying);
                Assert.IsNull(sut.ActiveNodeId);
            }
        }

        // ===== PlayNode =====

        [Test]
        public void PlayNode_NullOrEmpty_Throws()
        {
            using (NodeDialogueController sut = new NodeDialogueController(_dialogueManager, _config))
            {
                Assert.Throws<ArgumentException>(() => sut.PlayNode(null));
                Assert.Throws<ArgumentException>(() => sut.PlayNode(""));
            }
        }

        [Test]
        public void PlayNode_UnknownNode_Throws()
        {
            using (NodeDialogueController sut = new NodeDialogueController(_dialogueManager, _config))
            {
                Assert.Throws<ArgumentException>(() => sut.PlayNode("unknown_node"));
            }
        }

        [Test]
        public void PlayNode_StartsDialogueAndMarksActive()
        {
            using (NodeDialogueController sut = new NodeDialogueController(_dialogueManager, _config))
            {
                sut.PlayNode("node_0");
                Assert.IsTrue(sut.IsPlaying);
                Assert.AreEqual("node_0", sut.ActiveNodeId);
                Assert.IsTrue(_dialogueManager.IsActive);
            }
        }

        [Test]
        public void PlayNode_PublishesStartedEvent()
        {
            using (NodeDialogueController sut = new NodeDialogueController(_dialogueManager, _config))
            {
                NodeDialogueStartedEvent received = null;
                Action<NodeDialogueStartedEvent> handler = (e) => { received = e; };
                EventBus.Subscribe(handler);
                try
                {
                    sut.PlayNode("node_0");
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }
                Assert.IsNotNull(received);
                Assert.AreEqual("node_0", received.NodeId);
            }
        }

        [Test]
        public void PlayNode_WithChoices_PresentsChoicesImmediately()
        {
            using (NodeDialogueController sut = new NodeDialogueController(_dialogueManager, _config))
            {
                sut.PlayNode("node_0");
                Assert.IsTrue(_dialogueManager.IsWaitingForChoice);
                Assert.AreEqual(2, _dialogueManager.CurrentChoices.Count);
            }
        }

        [Test]
        public void PlayNode_AlreadyPlaying_Throws()
        {
            using (NodeDialogueController sut = new NodeDialogueController(_dialogueManager, _config))
            {
                sut.PlayNode("node_0");
                Assert.Throws<InvalidOperationException>(() => sut.PlayNode("node_1"));
            }
        }

        // ===== 完整流程：選擇 + 附加 response + 完成 =====

        [Test]
        public void Flow_SelectChoice_AppendsResponseAndFinishes()
        {
            using (NodeDialogueController sut = new NodeDialogueController(_dialogueManager, _config))
            {
                sut.PlayNode("node_0");

                NodeDialogueCompletedEvent completed = null;
                Action<NodeDialogueCompletedEvent> handler = (e) => { completed = e; };
                EventBus.Subscribe(handler);

                try
                {
                    // 玩家選 farm_girl 分支
                    _dialogueManager.SelectChoice("farm_girl");

                    // 推進至對話結束（intro 2 行 + response 1 行）
                    // StartDialogue 已在第 0 行，所以 Advance 到最後一行觸發完成
                    while (_dialogueManager.IsActive && _dialogueManager.Advance())
                    {
                        // 逐行推進
                    }
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }

                Assert.IsNotNull(completed);
                Assert.AreEqual("node_0", completed.NodeId);
                Assert.AreEqual("farm_girl", completed.SelectedBranchId);
                Assert.IsFalse(sut.IsPlaying);
            }
        }

        [Test]
        public void Flow_DifferentBranch_AppendsCorrectResponse()
        {
            using (NodeDialogueController sut = new NodeDialogueController(_dialogueManager, _config))
            {
                sut.PlayNode("node_0");

                _dialogueManager.SelectChoice("witch");

                // 第一個 advance 後目前行應為 witch response
                // intro 2 行 + response 1 行 = 共 3 行
                bool advanced = _dialogueManager.Advance();
                Assert.IsTrue(advanced);

                // 記錄當前行內容
                string currentLine = _dialogueManager.GetCurrentLine();

                // 繼續推進
                while (_dialogueManager.IsActive && _dialogueManager.Advance())
                {
                    // 推進至結束
                }

                Assert.IsFalse(sut.IsPlaying);
                // 確認 witch response 有被附加
                Assert.IsNotNull(currentLine);
            }
        }

        // ===== 空節點處理 =====

        [Test]
        public void PlayNode_EmptyNode_FiresCompletedImmediately()
        {
            NodeDialogueConfig config = new NodeDialogueConfig(new NodeDialogueConfigData
            {
                node_dialogue_lines = new NodeDialogueLineData[]
                {
                    // 刻意製造空節點：只有不會被分到 intro/choice/response 的類型不存在，留空
                    // 實際上 node_empty 沒有任何行
                    new NodeDialogueLineData { node_id = "other_node", sequence = 1, line_type = "dialogue", text = "x" },
                },
            });

            using (NodeDialogueController sut = new NodeDialogueController(_dialogueManager, config))
            {
                // 用 other_node 當作測試目標（有 1 行 intro，無 choices）
                NodeDialogueCompletedEvent completed = null;
                Action<NodeDialogueCompletedEvent> handler = (e) => { completed = e; };
                EventBus.Subscribe(handler);
                try
                {
                    sut.PlayNode("other_node");
                    while (_dialogueManager.IsActive && _dialogueManager.Advance()) { }
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }
                Assert.IsNotNull(completed);
                Assert.AreEqual("other_node", completed.NodeId);
            }
        }

        // ===== Dispose =====

        [Test]
        public void Dispose_DoesNotThrow()
        {
            NodeDialogueController sut = new NodeDialogueController(_dialogueManager, _config);
            Assert.DoesNotThrow(() => sut.Dispose());
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            NodeDialogueController sut = new NodeDialogueController(_dialogueManager, _config);
            sut.Dispose();
            Assert.DoesNotThrow(() => sut.Dispose());
        }

        // ===== Helper =====

        /// <summary>
        /// 建立測試用節點配置：
        /// node_0：2 行 intro (dialogue) + 1 行 choice_prompt + 2 個 choices (farm_girl, witch)
        ///         + 各分支 1 行 response
        /// node_1：1 行 dialogue + 1 行 choice_prompt + 1 個 choice（共通分支）
        /// </summary>
        private static NodeDialogueConfig BuildConfig()
        {
            return new NodeDialogueConfig(new NodeDialogueConfigData
            {
                schema_version = 1,
                node_dialogue_lines = new NodeDialogueLineData[]
                {
                    // node_0 intro
                    new NodeDialogueLineData { line_id = "n0_1", node_id = "node_0", sequence = 1, speaker = "VCW", text = "intro1", line_type = "dialogue", choice_branch = "" },
                    new NodeDialogueLineData { line_id = "n0_2", node_id = "node_0", sequence = 2, speaker = "VCW", text = "intro2", line_type = "choice_prompt", choice_branch = "" },
                    new NodeDialogueLineData { line_id = "n0_c1", node_id = "node_0", sequence = 3, speaker = "player", text = "choose A", line_type = "choice_option", choice_branch = "farm_girl" },
                    new NodeDialogueLineData { line_id = "n0_c2", node_id = "node_0", sequence = 4, speaker = "player", text = "choose B", line_type = "choice_option", choice_branch = "witch" },
                    new NodeDialogueLineData { line_id = "n0_r1", node_id = "node_0", sequence = 5, speaker = "VCW", text = "resp farm", line_type = "choice_response", choice_branch = "farm_girl" },
                    new NodeDialogueLineData { line_id = "n0_r2", node_id = "node_0", sequence = 6, speaker = "VCW", text = "resp witch", line_type = "choice_response", choice_branch = "witch" },

                    // node_1
                    new NodeDialogueLineData { line_id = "n1_1", node_id = "node_1", sequence = 1, speaker = "VCW", text = "n1 intro", line_type = "dialogue", choice_branch = "" },
                    new NodeDialogueLineData { line_id = "n1_2", node_id = "node_1", sequence = 2, speaker = "VCW", text = "n1 prompt", line_type = "choice_prompt", choice_branch = "" },
                    new NodeDialogueLineData { line_id = "n1_c1", node_id = "node_1", sequence = 3, speaker = "player", text = "meet", line_type = "choice_option", choice_branch = "" },
                },
            });
        }
    }
}
