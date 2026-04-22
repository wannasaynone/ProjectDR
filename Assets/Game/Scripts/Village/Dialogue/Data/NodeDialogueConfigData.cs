// NodeDialogueConfigData — 節點劇情對話外部配置的 JSON DTO 與不可變配置物件。
// 配置檔路徑：Assets/Game/Resources/Config/node-dialogue-config.json
// 資料結構對應 A2 設計師產出：每行帶 id, line_id, node_id, sequence, speaker, text,
// line_type, choice_branch。支援 VN 式選項分支（choice_prompt / choice_option / choice_response）。
//
// ADR-001 / ADR-002 A14 改造（2026-04-22）：
//   NodeDialogueLineData 實作 KahaGameCore.GameData.IGameData，
//   加 int id 欄位（流水號主鍵）+ 保留 line_id（語意字串外鍵）。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village.Dialogue
{
    // ===== 對話行類型常數 =====

    /// <summary>節點劇情對話行的類型常數（對應 JSON line_type 欄位）。</summary>
    public static class NodeDialogueLineTypes
    {
        /// <summary>旁白（narrator speaker，非角色台詞）。</summary>
        public const string Narration = "narration";

        /// <summary>角色台詞（一般對話行）。</summary>
        public const string Dialogue = "dialogue";

        /// <summary>選項提示（角色丟出問題後即將呈現選項）。</summary>
        public const string ChoicePrompt = "choice_prompt";

        /// <summary>選項（玩家可選的分支選項）。</summary>
        public const string ChoiceOption = "choice_option";

        /// <summary>選擇後的回應（由 choice_branch 決定播放哪一行）。</summary>
        public const string ChoiceResponse = "choice_response";
    }

    // ===== JSON DTO（供 JsonUtility.FromJson 使用） =====

    /// <summary>
    /// 節點劇情的單一對話行（JSON DTO）。
    /// 欄位命名對應 node-dialogue-config.json。
    /// 實作 IGameData：int id 為流水號主鍵，line_id 為語意字串外鍵。
    /// </summary>
    [Serializable]
    public class NodeDialogueLineData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。回傳 int id 流水號。</summary>
        public int ID => id;

        /// <summary>對話行語意字串識別（語意外鍵）。對應 JSON 欄位 "line_id"。</summary>
        public string Key => line_id;

        /// <summary>對話行語意唯一識別。</summary>
        public string line_id;

        /// <summary>所屬節點 ID（node_0, node_1, node_2）。</summary>
        public string node_id;

        /// <summary>節點內播放順序（升序）。</summary>
        public int sequence;

        /// <summary>說話者（角色 ID 或 narrator, player）。</summary>
        public string speaker;

        /// <summary>對話文字。</summary>
        public string text;

        /// <summary>行類型（NodeDialogueLineTypes）。</summary>
        public string line_type;

        /// <summary>
        /// 選項分支標記。
        /// - 空字串：共通行（與分支無關）。
        /// - 非空：僅在該分支被選中時播放（choice_option 與 choice_response 使用）。
        /// </summary>
        public string choice_branch;
    }

    /// <summary>節點劇情對話的完整外部配置（JSON DTO）。</summary>
    [Serializable]
    public class NodeDialogueConfigData
    {
        /// <summary>資料結構版本。</summary>
        public int schema_version;

        /// <summary>配置說明（撰寫者備註）。</summary>
        public string note;

        /// <summary>所有節點的對話行。</summary>
        public NodeDialogueLineData[] node_dialogue_lines;
    }

    // ===== 不可變配置物件 =====

    /// <summary>單一選項資訊（不可變）。</summary>
    public class NodeDialogueChoiceOption
    {
        /// <summary>選項對應的分支 ID（choice_branch 欄位值）。</summary>
        public string BranchId { get; }

        /// <summary>選項顯示文字。</summary>
        public string Text { get; }

        public NodeDialogueChoiceOption(string branchId, string text)
        {
            BranchId = branchId;
            Text = text;
        }
    }

    /// <summary>
    /// 單一節點的劇情資料（不可變）。
    /// 內含共通對話行、選項清單、與各分支的回應行。
    /// </summary>
    public class NodeDialogueData
    {
        /// <summary>節點 ID。</summary>
        public string NodeId { get; }

        /// <summary>選項呈現前的共通對話行（narration/dialogue/choice_prompt）。</summary>
        public IReadOnlyList<NodeDialogueLineData> IntroLines { get; }

        /// <summary>節點內的選項清單（choice_option，依 sequence 排序）。</summary>
        public IReadOnlyList<NodeDialogueChoiceOption> Choices { get; }

        /// <summary>
        /// 每個分支的回應行（choice_response）。
        /// Key = branch id，Value = 該分支被選擇後要播放的對話行。
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<NodeDialogueLineData>> ResponseLinesByBranch { get; }

        /// <summary>
        /// 此節點是否包含選項分支。
        /// </summary>
        public bool HasChoices => Choices.Count > 0;

        public NodeDialogueData(
            string nodeId,
            IReadOnlyList<NodeDialogueLineData> introLines,
            IReadOnlyList<NodeDialogueChoiceOption> choices,
            IReadOnlyDictionary<string, IReadOnlyList<NodeDialogueLineData>> responseLinesByBranch)
        {
            NodeId = nodeId;
            IntroLines = introLines;
            Choices = choices;
            ResponseLinesByBranch = responseLinesByBranch;
        }

        /// <summary>
        /// 取得指定分支的回應對話行。若分支不存在則回傳空陣列。
        /// </summary>
        public IReadOnlyList<NodeDialogueLineData> GetResponseLines(string branchId)
        {
            if (ResponseLinesByBranch.TryGetValue(branchId ?? string.Empty, out IReadOnlyList<NodeDialogueLineData> lines))
            {
                return lines;
            }
            return Array.Empty<NodeDialogueLineData>();
        }
    }

    /// <summary>
    /// 節點劇情對話的不可變配置。
    /// 從 NodeDialogueConfigData（JSON DTO）建構，依 node_id 分組並依 sequence 排序。
    /// </summary>
    public class NodeDialogueConfig
    {
        private readonly Dictionary<string, NodeDialogueData> _nodesById;

        /// <summary>所有節點的 ID 清單（建構順序，不保證排序）。</summary>
        public IReadOnlyCollection<string> NodeIds => _nodesById.Keys;

        /// <summary>
        /// 從 JSON DTO 建構不可變配置。
        /// </summary>
        /// <param name="data">JSON 反序列化後的 DTO。</param>
        /// <exception cref="ArgumentNullException">data 為 null 時拋出。</exception>
        public NodeDialogueConfig(NodeDialogueConfigData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _nodesById = new Dictionary<string, NodeDialogueData>();

            NodeDialogueLineData[] allLines = data.node_dialogue_lines ?? Array.Empty<NodeDialogueLineData>();

            // 依 node_id 分組
            Dictionary<string, List<NodeDialogueLineData>> grouped = new Dictionary<string, List<NodeDialogueLineData>>();
            foreach (NodeDialogueLineData line in allLines)
            {
                if (line == null || string.IsNullOrEmpty(line.node_id))
                {
                    continue;
                }

                if (!grouped.TryGetValue(line.node_id, out List<NodeDialogueLineData> bucket))
                {
                    bucket = new List<NodeDialogueLineData>();
                    grouped[line.node_id] = bucket;
                }
                bucket.Add(line);
            }

            // 逐節點整理為 NodeDialogueData
            foreach (KeyValuePair<string, List<NodeDialogueLineData>> entry in grouped)
            {
                string nodeId = entry.Key;
                List<NodeDialogueLineData> lines = entry.Value;
                lines.Sort((NodeDialogueLineData a, NodeDialogueLineData b) => a.sequence.CompareTo(b.sequence));

                List<NodeDialogueLineData> introLines = new List<NodeDialogueLineData>();
                List<NodeDialogueChoiceOption> choices = new List<NodeDialogueChoiceOption>();
                Dictionary<string, List<NodeDialogueLineData>> responsesByBranch = new Dictionary<string, List<NodeDialogueLineData>>();

                foreach (NodeDialogueLineData line in lines)
                {
                    string lineType = line.line_type ?? string.Empty;
                    switch (lineType)
                    {
                        case NodeDialogueLineTypes.Narration:
                        case NodeDialogueLineTypes.Dialogue:
                        case NodeDialogueLineTypes.ChoicePrompt:
                            introLines.Add(line);
                            break;

                        case NodeDialogueLineTypes.ChoiceOption:
                            choices.Add(new NodeDialogueChoiceOption(line.choice_branch ?? string.Empty, line.text ?? string.Empty));
                            break;

                        case NodeDialogueLineTypes.ChoiceResponse:
                            string branchId = line.choice_branch ?? string.Empty;
                            if (!responsesByBranch.TryGetValue(branchId, out List<NodeDialogueLineData> bucket))
                            {
                                bucket = new List<NodeDialogueLineData>();
                                responsesByBranch[branchId] = bucket;
                            }
                            bucket.Add(line);
                            break;
                    }
                }

                // 轉為不可變介面
                Dictionary<string, IReadOnlyList<NodeDialogueLineData>> readonlyResponses =
                    new Dictionary<string, IReadOnlyList<NodeDialogueLineData>>();
                foreach (KeyValuePair<string, List<NodeDialogueLineData>> r in responsesByBranch)
                {
                    readonlyResponses[r.Key] = r.Value.AsReadOnly();
                }

                _nodesById[nodeId] = new NodeDialogueData(
                    nodeId,
                    introLines.AsReadOnly(),
                    choices.AsReadOnly(),
                    readonlyResponses);
            }
        }

        /// <summary>
        /// 取得指定節點的劇情資料。若節點不存在則回傳 null。
        /// </summary>
        public NodeDialogueData GetNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;
            _nodesById.TryGetValue(nodeId, out NodeDialogueData node);
            return node;
        }
    }
}
