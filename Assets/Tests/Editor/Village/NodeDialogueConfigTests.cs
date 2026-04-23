using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Dialogue;
using JsonFx.Json;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// NodeDialogueConfig / NodeDialogueLineData 的單元測試（B2）。
    /// Sprint 8 Wave 2.5：配合純陣列 DTO 重構（廢棄包裹類 NodeDialogueConfigData）。
    /// </summary>
    [TestFixture]
    public class NodeDialogueConfigTests
    {
        private NodeDialogueLineData MakeLine(
            string nodeId, int sequence, string lineType,
            string branch = "", string speaker = "narrator", string text = "text")
        {
            return new NodeDialogueLineData
            {
                line_id = $"{nodeId}_{sequence}",
                node_id = nodeId,
                sequence = sequence,
                speaker = speaker,
                text = text,
                line_type = lineType,
                choice_branch = branch
            };
        }

        [Test]
        public void Constructor_WithNullData_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new NodeDialogueConfig(null);
            });
        }

        [Test]
        public void Constructor_WithEmptyLines_ProducesNoNodes()
        {
            NodeDialogueConfig config = new NodeDialogueConfig(new NodeDialogueLineData[0]);
            Assert.AreEqual(0, config.NodeIds.Count);
        }

        [Test]
        public void GetNode_NonExistent_ReturnsNull()
        {
            NodeDialogueConfig config = new NodeDialogueConfig(new NodeDialogueLineData[0]);
            Assert.IsNull(config.GetNode("node_99"));
        }

        [Test]
        public void GetNode_Null_ReturnsNull()
        {
            NodeDialogueConfig config = new NodeDialogueConfig(new NodeDialogueLineData[0]);
            Assert.IsNull(config.GetNode(null));
        }

        [Test]
        public void GroupsLinesByNodeId()
        {
            NodeDialogueConfig config = new NodeDialogueConfig(new NodeDialogueLineData[]
            {
                MakeLine("node_0", 1, NodeDialogueLineTypes.Dialogue),
                MakeLine("node_0", 2, NodeDialogueLineTypes.Dialogue),
                MakeLine("node_1", 1, NodeDialogueLineTypes.Dialogue)
            });

            Assert.IsNotNull(config.GetNode("node_0"));
            Assert.IsNotNull(config.GetNode("node_1"));
            Assert.AreEqual(2, config.NodeIds.Count);
        }

        [Test]
        public void SortsIntroLinesBySequence()
        {
            NodeDialogueConfig config = new NodeDialogueConfig(new NodeDialogueLineData[]
            {
                MakeLine("node_0", 3, NodeDialogueLineTypes.Dialogue, text: "C"),
                MakeLine("node_0", 1, NodeDialogueLineTypes.Narration, text: "A"),
                MakeLine("node_0", 2, NodeDialogueLineTypes.Dialogue, text: "B")
            });

            NodeDialogueData node = config.GetNode("node_0");
            Assert.AreEqual(3, node.IntroLines.Count);
            Assert.AreEqual("A", node.IntroLines[0].text);
            Assert.AreEqual("B", node.IntroLines[1].text);
            Assert.AreEqual("C", node.IntroLines[2].text);
        }

        [Test]
        public void ChoicePromptIsTreatedAsIntroLine()
        {
            NodeDialogueConfig config = new NodeDialogueConfig(new NodeDialogueLineData[]
            {
                MakeLine("node_0", 1, NodeDialogueLineTypes.Dialogue),
                MakeLine("node_0", 2, NodeDialogueLineTypes.ChoicePrompt, text: "prompt")
            });

            NodeDialogueData node = config.GetNode("node_0");
            Assert.AreEqual(2, node.IntroLines.Count);
            Assert.AreEqual("prompt", node.IntroLines[1].text);
        }

        [Test]
        public void ChoiceOptionsAreCollectedAsChoices()
        {
            NodeDialogueConfig config = new NodeDialogueConfig(new NodeDialogueLineData[]
            {
                MakeLine("node_0", 1, NodeDialogueLineTypes.ChoicePrompt),
                MakeLine("node_0", 2, NodeDialogueLineTypes.ChoiceOption, "farm_girl", "player", "A"),
                MakeLine("node_0", 3, NodeDialogueLineTypes.ChoiceOption, "witch", "player", "B")
            });

            NodeDialogueData node = config.GetNode("node_0");
            Assert.IsTrue(node.HasChoices);
            Assert.AreEqual(2, node.Choices.Count);
            Assert.AreEqual("farm_girl", node.Choices[0].BranchId);
            Assert.AreEqual("A", node.Choices[0].Text);
            Assert.AreEqual("witch", node.Choices[1].BranchId);
            Assert.AreEqual("B", node.Choices[1].Text);
        }

        [Test]
        public void ChoiceResponsesAreGroupedByBranch()
        {
            NodeDialogueConfig config = new NodeDialogueConfig(new NodeDialogueLineData[]
            {
                MakeLine("node_0", 1, NodeDialogueLineTypes.ChoicePrompt),
                MakeLine("node_0", 2, NodeDialogueLineTypes.ChoiceOption, "farm_girl", "player", "A"),
                MakeLine("node_0", 3, NodeDialogueLineTypes.ChoiceOption, "witch", "player", "B"),
                MakeLine("node_0", 4, NodeDialogueLineTypes.ChoiceResponse, "farm_girl", "VCW", "response_fg"),
                MakeLine("node_0", 5, NodeDialogueLineTypes.ChoiceResponse, "witch", "VCW", "response_witch")
            });

            NodeDialogueData node = config.GetNode("node_0");

            IReadOnlyList<NodeDialogueLineData> fgLines = node.GetResponseLines("farm_girl");
            Assert.AreEqual(1, fgLines.Count);
            Assert.AreEqual("response_fg", fgLines[0].text);

            IReadOnlyList<NodeDialogueLineData> witchLines = node.GetResponseLines("witch");
            Assert.AreEqual(1, witchLines.Count);
            Assert.AreEqual("response_witch", witchLines[0].text);
        }

        [Test]
        public void GetResponseLines_UnknownBranch_ReturnsEmpty()
        {
            NodeDialogueConfig config = new NodeDialogueConfig(new NodeDialogueLineData[]
            {
                MakeLine("node_0", 1, NodeDialogueLineTypes.ChoicePrompt),
                MakeLine("node_0", 2, NodeDialogueLineTypes.ChoiceOption, "farm_girl", "player", "A")
            });

            NodeDialogueData node = config.GetNode("node_0");
            IReadOnlyList<NodeDialogueLineData> lines = node.GetResponseLines("unknown");
            Assert.AreEqual(0, lines.Count);
        }

        [Test]
        public void NodeWithoutChoices_HasChoicesIsFalse()
        {
            NodeDialogueConfig config = new NodeDialogueConfig(new NodeDialogueLineData[]
            {
                MakeLine("node_2", 1, NodeDialogueLineTypes.Dialogue),
                MakeLine("node_2", 2, NodeDialogueLineTypes.Dialogue)
            });

            NodeDialogueData node = config.GetNode("node_2");
            Assert.IsFalse(node.HasChoices);
            Assert.AreEqual(0, node.Choices.Count);
        }

        [Test]
        public void MalformedLine_WithEmptyNodeId_IsSkipped()
        {
            NodeDialogueConfig config = new NodeDialogueConfig(new NodeDialogueLineData[]
            {
                new NodeDialogueLineData
                {
                    node_id = "", sequence = 1, line_type = NodeDialogueLineTypes.Dialogue, text = "skipped"
                },
                MakeLine("node_0", 1, NodeDialogueLineTypes.Dialogue)
            });

            Assert.AreEqual(1, config.NodeIds.Count);
            Assert.IsNotNull(config.GetNode("node_0"));
        }

        [Test]
        public void UnknownLineType_IsIgnored()
        {
            NodeDialogueConfig config = new NodeDialogueConfig(new NodeDialogueLineData[]
            {
                MakeLine("node_0", 1, "unknown_type"),
                MakeLine("node_0", 2, NodeDialogueLineTypes.Dialogue)
            });

            NodeDialogueData node = config.GetNode("node_0");
            Assert.AreEqual(1, node.IntroLines.Count);
            Assert.AreEqual(0, node.Choices.Count);
        }

        // ===== 真實 JSON 反序列化 =====

        [Test]
        public void JsonRoundTrip_WithRealisticData_ParsesSuccessfully()
        {
            // Sprint 8 Wave 2.5：純陣列格式，使用 JsonFx 反序列化
            string json = @"[
                { ""id"": 1, ""line_id"": ""n0_1"", ""node_id"": ""node_0"", ""sequence"": 1, ""speaker"": ""narrator"", ""text"": ""旁白"", ""line_type"": ""narration"", ""choice_branch"": """" },
                { ""id"": 2, ""line_id"": ""n0_2"", ""node_id"": ""node_0"", ""sequence"": 2, ""speaker"": ""VillageChiefWife"", ""text"": ""台詞"", ""line_type"": ""dialogue"", ""choice_branch"": """" },
                { ""id"": 3, ""line_id"": ""n0_3"", ""node_id"": ""node_0"", ""sequence"": 3, ""speaker"": ""VillageChiefWife"", ""text"": ""選項提示"", ""line_type"": ""choice_prompt"", ""choice_branch"": """" },
                { ""id"": 4, ""line_id"": ""n0_a"", ""node_id"": ""node_0"", ""sequence"": 4, ""speaker"": ""player"", ""text"": ""A"", ""line_type"": ""choice_option"", ""choice_branch"": ""farm_girl"" },
                { ""id"": 5, ""line_id"": ""n0_b"", ""node_id"": ""node_0"", ""sequence"": 5, ""speaker"": ""player"", ""text"": ""B"", ""line_type"": ""choice_option"", ""choice_branch"": ""witch"" },
                { ""id"": 6, ""line_id"": ""n0_rfg"", ""node_id"": ""node_0"", ""sequence"": 6, ""speaker"": ""VillageChiefWife"", ""text"": ""回應A"", ""line_type"": ""choice_response"", ""choice_branch"": ""farm_girl"" },
                { ""id"": 7, ""line_id"": ""n0_rw"", ""node_id"": ""node_0"", ""sequence"": 7, ""speaker"": ""VillageChiefWife"", ""text"": ""回應B"", ""line_type"": ""choice_response"", ""choice_branch"": ""witch"" }
            ]";

            NodeDialogueLineData[] entries = JsonReader.Deserialize<NodeDialogueLineData[]>(json);
            Assert.IsNotNull(entries);
            Assert.AreEqual(7, entries.Length);

            // ADR-001 / ADR-002 A14：驗證 NodeDialogueLineData 實作 IGameData + id 非 0
            NodeDialogueLineData firstLine = entries[0];
            Assert.That(firstLine, Is.AssignableTo<KahaGameCore.GameData.IGameData>(),
                "NodeDialogueLineData 必須實作 IGameData（ADR-001）");
            Assert.That(firstLine.ID, Is.Not.Zero,
                "NodeDialogueLineData.ID 必須非 0（ADR-002 A14）");
            Assert.That(firstLine.Key, Is.EqualTo("n0_1"),
                "NodeDialogueLineData.Key 應回傳 line_id");

            NodeDialogueConfig config = new NodeDialogueConfig(entries);
            NodeDialogueData node = config.GetNode("node_0");

            Assert.IsNotNull(node);
            Assert.AreEqual(3, node.IntroLines.Count);
            Assert.AreEqual(2, node.Choices.Count);
            Assert.AreEqual("回應A", node.GetResponseLines("farm_girl")[0].text);
            Assert.AreEqual("回應B", node.GetResponseLines("witch")[0].text);
        }
    }
}
