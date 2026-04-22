// GuardReturnEventController — 守衛歸來事件控制器（B10 Sprint 4）。
// 純邏輯 IDisposable（非 MonoBehaviour）。
//
// 依據 GDD：
// - `character-unlock-system.md` v1.2 § 三、守衛歸來事件（身分誤會純劇情演出）
// - `exploration-system.md` v1.1 § 探索功能解鎖 + 首次探索觸發
//
// 職責：
// 1. 提供一次性觸發檢查：HasTriggered（避免重複觸發）
// 2. 呼叫 TriggerEvent() 啟動守衛歸來事件：
//    a. 發布 GuardReturnEventStartedEvent
//    b. 透過 ICGPlayer 播放守衛登場 CG（台詞已整合於 CharacterIntroCGView 的 intro_lines）
//    c. CG 完成後直接發布 GuardReturnEventCompletedEvent
//       （F7 bugfix：原設計呼叫 DialogueManager.StartDialogue 但無 View 推進對話，事件鏈斷裂）
//
// 整合：ExplorationEntryManager 可在 Depart() 前檢查 CanTriggerGuardReturn()，若尚未觸發則優先觸發本事件。

using System;
using KahaGameCore.GameEvent;
using ProjectDR.Village.CG;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Village.Guard
{
    /// <summary>
    /// 守衛歸來事件控制器。
    /// 協調 ICGPlayer + CharacterUnlockManager 完成純劇情演出。
    /// 一次性觸發：完成後 HasTriggered = true，後續呼叫 TriggerEvent 會被忽略。
    /// </summary>
    public class GuardReturnEventController : IDisposable
    {
        private readonly ICGPlayer _cgPlayer;

        private bool _isRunning;
        private bool _hasTriggered;
        private bool _disposed;

        /// <summary>目前是否正在播放守衛歸來事件。</summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 事件是否已經觸發過一次（不論是否已完成）。
        /// 用於外部判斷「首次探索是否該走本事件」。
        /// </summary>
        public bool HasTriggered => _hasTriggered;

        /// <summary>
        /// 建構守衛歸來事件控制器。
        /// </summary>
        /// <param name="cgPlayer">CG 播放器（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">cgPlayer 為 null 時拋出。</exception>
        public GuardReturnEventController(ICGPlayer cgPlayer)
        {
            _cgPlayer = cgPlayer ?? throw new ArgumentNullException(nameof(cgPlayer));
        }

        /// <summary>
        /// 判斷現在是否可以觸發守衛歸來事件。
        /// 條件：未曾觸發過 + 非執行中。
        /// </summary>
        public bool CanTriggerGuardReturn()
        {
            if (_hasTriggered) return false;
            if (_isRunning) return false;
            return true;
        }

        /// <summary>
        /// 觸發守衛歸來事件。
        /// 若已觸發過或正在執行中，此呼叫會被忽略並回傳 false。
        /// </summary>
        /// <returns>成功啟動事件回傳 true；否則 false。</returns>
        public bool TriggerEvent()
        {
            if (!CanTriggerGuardReturn())
            {
                return false;
            }

            _isRunning = true;
            _hasTriggered = true;

            EventBus.Publish(new GuardReturnEventStartedEvent());

            // Step 1：播放守衛登場 CG
            _cgPlayer.PlayIntroCG(CharacterIds.Guard, OnCGComplete);

            return true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        // ===== 流程步驟 =====

        private void OnCGComplete()
        {
            if (!_isRunning || _disposed) return;

            // Step 2：守衛歸來對話台詞已整合於 CharacterIntroCGView 的 intro_lines 中播放（CG 期間）。
            // CG 結束後直接完成事件，不再透過 DialogueManager 重播 guard_return_lines。
            // 根因（F7 bugfix）：DialogueManager.Advance() 只存在於 CharacterInteractionView，
            // 守衛歸來觸發時 CharacterInteractionView 未顯示，對話永遠無法推進。
            CompleteEvent();
        }

        private void CompleteEvent()
        {
            _isRunning = false;

            // CharacterUnlockManager 已訂閱 GuardReturnEventCompletedEvent，
            // 自動處理守衛 Hub 按鈕解鎖（ForceUnlock(Guard) + 發布 CharacterUnlockedEvent）。
            EventBus.Publish(new GuardReturnEventCompletedEvent());
        }
    }
}
