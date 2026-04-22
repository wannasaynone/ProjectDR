// OpeningSequenceController — 開場劇情演出控制器（B9 Sprint 4）。
// 純邏輯 IDisposable（非 MonoBehaviour）。
//
// 依據 GDD：
// - `character-unlock-system.md` v1.2 § 1.2 節點 0 UX 流程、§ 1.2.1 強制模式
// - `character-interaction.md` v2.2 § 1.4 強制模式
//
// 流程（節點 0）：
// 1. 遊戲啟動進入 Village scene
// 2. 呼叫 StartOpeningSequence() 觸發：
//    a. PlayIntroCG(villageChiefWife) — 播放村長夫人登場 CG
//    b. CG 完成後，播放節點 0 劇情（NodeDialogueController.PlayNode("node_0")）
//    c. NodeDialogueCompletedEvent 觸發 → 節點 0 結束（選項已透過 CharacterUnlockManager 處理解鎖）
//    d. 發布 OpeningSequenceCompletedEvent（UI 收到後可關閉強制模式、開啟返回按鈕）
//
// 節點 1/2 不由本 controller 負責（那是 MainQuestCompleted 觸發後由上層再呼叫 NodeDialogueController.PlayNode
// 播放；本 controller 僅處理開場節點 0 + CG）。
//
// 與 UI 層的協調：
// - UI（CharacterInteractionView）訂閱 OpeningSequenceStartedEvent 進入強制模式 + 自動 push 村長夫人畫面
// - UI 訂閱 CGPlaybackStartedEvent/CompletedEvent 處理 CG 播放遮罩
// - UI 訂閱 NodeDialogueStartedEvent/CompletedEvent 同步強制模式顯示/關閉
// - UI 訂閱 OpeningSequenceCompletedEvent 解除強制模式

using ProjectDR.Village.Dialogue;
using ProjectDR.Village.CG;
using System;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Village.OpeningSequence
{
    /// <summary>
    /// 開場劇情演出控制器。
    /// 協調 ICGPlayer、NodeDialogueController、CharacterUnlockManager 完成節點 0 強制流程。
    /// </summary>
    public class OpeningSequenceController : IDisposable
    {
        private readonly ICGPlayer _cgPlayer;
        private readonly NodeDialogueController _nodeDialogueController;

        private readonly Action<NodeDialogueCompletedEvent> _onNodeCompleted;

        /// <summary>節點 0 的識別字串。</summary>
        public const string Node0Id = "node_0";

        private bool _isRunning;
        private bool _disposed;

        /// <summary>目前是否在開場流程中。</summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 建構開場劇情控制器。
        /// </summary>
        /// <param name="cgPlayer">CG 播放器（不可為 null；IT 階段使用 PlaceholderCGPlayer）。</param>
        /// <param name="nodeDialogueController">節點劇情控制器（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">任一參數為 null 時拋出。</exception>
        public OpeningSequenceController(
            ICGPlayer cgPlayer,
            NodeDialogueController nodeDialogueController)
        {
            _cgPlayer = cgPlayer ?? throw new ArgumentNullException(nameof(cgPlayer));
            _nodeDialogueController = nodeDialogueController ?? throw new ArgumentNullException(nameof(nodeDialogueController));

            _onNodeCompleted = OnNodeDialogueCompleted;
            EventBus.Subscribe(_onNodeCompleted);
        }

        /// <summary>
        /// 啟動開場劇情演出。
        /// 若已在執行中，重複呼叫會被忽略（不拋例外）。
        /// </summary>
        public void StartOpeningSequence()
        {
            if (_isRunning) return;
            _isRunning = true;

            EventBus.Publish(new OpeningSequenceStartedEvent());

            // Step 1：播放村長夫人登場 CG
            // IT 階段 PlaceholderCGPlayer 立即完成並觸發回呼
            _cgPlayer.PlayIntroCG(CharacterIds.VillageChiefWife, OnCGComplete);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            EventBus.Unsubscribe(_onNodeCompleted);
        }

        // ===== 流程步驟 =====

        private void OnCGComplete()
        {
            if (!_isRunning || _disposed) return;

            // Step 2：播放節點 0 對話
            _nodeDialogueController.PlayNode(Node0Id);
        }

        private void OnNodeDialogueCompleted(NodeDialogueCompletedEvent e)
        {
            if (!_isRunning) return;
            if (e == null || e.NodeId != Node0Id) return;

            // Step 3：節點 0 完成 → 開場流程結束
            // 玩家在 NodeDialogueController 中已透過 DialogueChoiceSelectedEvent
            // 讓 CharacterUnlockManager 處理了角色解鎖與初始資源發放
            _isRunning = false;
            EventBus.Publish(new OpeningSequenceCompletedEvent());
        }
    }
}
