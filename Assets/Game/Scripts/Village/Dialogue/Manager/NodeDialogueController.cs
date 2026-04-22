// NodeDialogueController — 節點 0/1/2 劇情對話播放控制器（B9 Sprint 4）。
// 純邏輯 IDisposable（非 MonoBehaviour）。
//
// 依據 GDD：
// - `character-unlock-system.md` v1.2 § 1.2 節點結構、§ 2.2 VN 選項流程
// - `character-interaction.md` v2.2 § 1.4 強制模式
//
// 職責：
// 1. 協調 DialogueManager + NodeDialogueConfig，播放節點 N 的完整劇情：
//    intro_lines → choice_prompt → present choices → 玩家選擇 → choice_response → 完成
// 2. 玩家選擇時，透過 DialogueManager.SelectChoice 通知 → DialogueChoiceSelectedEvent
//    由 CharacterUnlockManager 訂閱處理角色解鎖
// 3. 發布 NodeDialogueStartedEvent / NodeDialogueCompletedEvent
//
// 不直接處理 UI：所有 VN 選項呈現、強制模式切換由 CharacterInteractionView 訂閱
// DialogueChoicePresentedEvent 自行處理。

using ProjectDR.Village.CharacterIntro;
using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Village.Dialogue
{
    /// <summary>
    /// 節點劇情播放控制器。
    /// 呼叫 PlayNode(nodeId) 啟動，透過 DialogueManager 推進對話，
    /// 玩家選擇 VN 選項後附加 response 行，最後 DialogueCompletedEvent 觸發節點結束。
    /// </summary>
    public class NodeDialogueController : IDisposable
    {
        /// <summary>節點 0 ID（開場，村長夫人介紹 + 選擇 1）。</summary>
        public const string NodeIdNode0 = "node_0";

        /// <summary>節點 1 ID（首次角色 intro 完成後，村長夫人引導 + 選擇 2）。</summary>
        public const string NodeIdNode1 = "node_1";

        /// <summary>節點 2 ID（第二位角色委託完成後，村長夫人引導探索功能開放）。</summary>
        public const string NodeIdNode2 = "node_2";

        /// <summary>守衛首次進入互動畫面自動觸發的取劍對白節點 ID（原 A08 併入）。</summary>
        public const string NodeIdGuardFirstMeet = "guard_first_meet";

        private readonly DialogueManager _dialogueManager;
        private readonly NodeDialogueConfig _config;

        private readonly Action<DialogueChoiceSelectedEvent> _onChoiceSelected;
        private readonly Action<DialogueCompletedEvent> _onDialogueCompleted;

        private string _activeNodeId;
        private string _selectedBranchId;
        private bool _disposed;
        private bool _choiceHandled; // 防止重複 AppendLines

        /// <summary>已播放過（once-only）的節點 ID 集合（session 內記憶體旗標，禁用 PlayerPrefs）。</summary>
        private readonly HashSet<string> _playedOnceNodes = new HashSet<string>();

        /// <summary>目前播放中的節點 ID。未播放時為 null。</summary>
        public string ActiveNodeId => _activeNodeId;

        /// <summary>是否正在播放節點劇情。</summary>
        public bool IsPlaying => !string.IsNullOrEmpty(_activeNodeId);

        /// <summary>
        /// 建構節點劇情控制器。
        /// </summary>
        /// <param name="dialogueManager">對話管理器（不可為 null）。</param>
        /// <param name="config">節點對話配置（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">任一參數為 null 時拋出。</exception>
        public NodeDialogueController(
            DialogueManager dialogueManager,
            NodeDialogueConfig config)
        {
            _dialogueManager = dialogueManager ?? throw new ArgumentNullException(nameof(dialogueManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _onChoiceSelected = OnDialogueChoiceSelected;
            _onDialogueCompleted = OnDialogueCompleted;
            EventBus.Subscribe(_onChoiceSelected);
            EventBus.Subscribe(_onDialogueCompleted);
        }

        /// <summary>
        /// 播放指定節點的劇情。
        /// 步驟：
        /// 1. 啟動 DialogueManager（填入 intro_lines 的 text）
        /// 2. 設定狀態等待 UI 逐行 Advance
        /// 3. UI 層透過訂閱 DialogueStartedEvent/DialogueCompletedEvent 控制顯示
        /// 4. 若節點含 choices，UI 層必須在 intro 播完後呼叫 PresentNodeChoices（或由本 controller 自動呼叫）
        ///
        /// 為簡化流程，本 controller 在啟動時即呼叫 PresentChoices；UI 層在對話走到最後一行時
        /// 就會看到已呈現的選項。另一種方案為等 Advance 至 choice_prompt 再 PresentChoices，
        /// 實作上較複雜。IT 階段先採前者：播 intro_lines 時選項已準備好，DialogueChoicePresentedEvent
        /// 會立即發布，UI 可在選項呈現後自動等待。
        /// </summary>
        /// <param name="nodeId">節點 ID（node_0 / node_1 / node_2）。</param>
        /// <exception cref="ArgumentException">nodeId 為 null/空或未知時拋出。</exception>
        /// <exception cref="InvalidOperationException">目前已在播放節點或 DialogueManager 使用中。</exception>
        public void PlayNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                throw new ArgumentException("nodeId 不可為 null 或空。", nameof(nodeId));
            }
            NodeDialogueData node = _config.GetNode(nodeId);
            if (node == null)
            {
                throw new ArgumentException($"節點 ID '{nodeId}' 不存在於配置中。", nameof(nodeId));
            }
            if (IsPlaying)
            {
                throw new InvalidOperationException("已有節點劇情在播放中。");
            }
            if (_dialogueManager.IsActive)
            {
                throw new InvalidOperationException("DialogueManager 正在播放其他對話。");
            }

            _activeNodeId = nodeId;
            _selectedBranchId = null;
            _choiceHandled = false;

            // 從 intro_lines 取出 text 陣列 → 啟動對話
            string[] introTexts = ExtractTexts(node.IntroLines);
            if (introTexts.Length == 0 && !node.HasChoices)
            {
                // 空節點，直接當作完成
                string finishedNode = _activeNodeId;
                _activeNodeId = null;
                EventBus.Publish(new NodeDialogueCompletedEvent
                {
                    NodeId = finishedNode,
                    SelectedBranchId = null,
                });
                return;
            }

            EventBus.Publish(new NodeDialogueStartedEvent { NodeId = nodeId });

            _dialogueManager.StartDialogue(new DialogueData(introTexts));

            // 若節點有選項，立即呈現（UI 層可在 intro 播完後才顯示）
            // 此作法使 DialogueChoicePresentedEvent 與 StartDialogue 同步發布，
            // UI 接到後可選擇等 intro 播完再顯示選項按鈕；若 UI 不做此控制，
            // 選項按鈕會在 intro 播放時已可見。IT 階段接受此行為。
            if (node.HasChoices)
            {
                List<DialogueChoice> choices = new List<DialogueChoice>(node.Choices.Count);
                foreach (NodeDialogueChoiceOption opt in node.Choices)
                {
                    choices.Add(new DialogueChoice(opt.BranchId, opt.Text));
                }
                _dialogueManager.PresentChoices(choices.AsReadOnly());
            }
        }

        /// <summary>
        /// 若指定 nodeId 在本 session 內尚未觸發，則播放該節點的完整對話序列並標記為已觸發。
        /// 適用於「只應自動播一次」的對白（例：守衛首次進入取劍對白）。
        ///
        /// 若 nodeId 已觸發、目前正在播放其他節點、或 DialogueManager 使用中，則靜默忽略（回傳 false）。
        /// 節點不存在於配置時也靜默忽略（回傳 false）並記錄警告。
        ///
        /// fallback：若配置載入失敗導致節點不存在，此方法靜默跳過；
        ///           呼叫端應在 NodeDialogueCompletedEvent 中處理業務邏輯，不依賴 View callback。
        /// </summary>
        /// <param name="nodeId">節點 ID。</param>
        /// <returns>true 表示成功啟動播放，false 表示跳過。</returns>
        public bool TryPlayFirstMeetDialogueIfNotTriggered(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return false;
            if (_playedOnceNodes.Contains(nodeId)) return false;
            if (IsPlaying) return false;
            if (_dialogueManager.IsActive) return false;

            NodeDialogueData node = _config.GetNode(nodeId);
            if (node == null)
            {
                UnityEngine.Debug.LogWarning($"[NodeDialogueController] TryPlayFirstMeetDialogueIfNotTriggered: 節點 '{nodeId}' 不存在於配置，靜默忽略。");
                return false;
            }

            _playedOnceNodes.Add(nodeId);
            PlayNode(nodeId);
            return true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            EventBus.Unsubscribe(_onChoiceSelected);
            EventBus.Unsubscribe(_onDialogueCompleted);
        }

        // ===== 事件處理 =====

        private void OnDialogueChoiceSelected(DialogueChoiceSelectedEvent e)
        {
            if (e == null) return;
            if (!IsPlaying) return;
            if (_choiceHandled) return;

            _selectedBranchId = e.ChoiceId;
            _choiceHandled = true;

            NodeDialogueData node = _config.GetNode(_activeNodeId);
            if (node == null) return;

            // 取得該分支的 response 行，附加到對話尾端
            IReadOnlyList<NodeDialogueLineData> responseLines = node.GetResponseLines(_selectedBranchId);
            if (responseLines != null && responseLines.Count > 0)
            {
                string[] responseTexts = ExtractTexts(responseLines);
                _dialogueManager.AppendLines(responseTexts);
            }
            // 若無 response（例如節點 1 沒有分支 response），DialogueManager 繼續 Advance 至最後一行後自動結束
        }

        private void OnDialogueCompleted(DialogueCompletedEvent e)
        {
            if (!IsPlaying) return;

            string finishedNode = _activeNodeId;
            string finishedBranch = _selectedBranchId;
            _activeNodeId = null;
            _selectedBranchId = null;
            _choiceHandled = false;

            EventBus.Publish(new NodeDialogueCompletedEvent
            {
                NodeId = finishedNode,
                SelectedBranchId = finishedBranch,
            });
        }

        // ===== 私有工具 =====

        private static string[] ExtractTexts(IReadOnlyList<NodeDialogueLineData> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                return Array.Empty<string>();
            }
            string[] texts = new string[lines.Count];
            for (int i = 0; i < lines.Count; i++)
            {
                texts[i] = lines[i]?.text ?? string.Empty;
            }
            return texts;
        }

        private static string[] ExtractTexts(IReadOnlyList<CharacterIntroLineData> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                return Array.Empty<string>();
            }
            string[] texts = new string[lines.Count];
            for (int i = 0; i < lines.Count; i++)
            {
                texts[i] = lines[i]?.text ?? string.Empty;
            }
            return texts;
        }
    }
}
