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
//    b. 透過 ICGPlayer 播放守衛登場 CG
//    c. CG 完成後，啟動 DialogueManager 播放 guard-return-config 的所有行
//    d. DialogueCompletedEvent 到達 → 解鎖守衛 + 贈劍（透過 CharacterUnlockManager.OnGuardReturnCompleted）
//    e. 發布 GuardReturnEventCompletedEvent
//
// 整合：ExplorationEntryManager 可在 Depart() 前檢查 CanTriggerGuardReturn()，若尚未觸發則優先觸發本事件。

using System;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// 守衛歸來事件控制器。
    /// 協調 ICGPlayer、DialogueManager、CharacterUnlockManager 完成純劇情演出。
    /// 一次性觸發：完成後 HasTriggered = true，後續呼叫 TriggerEvent 會被忽略。
    /// </summary>
    public class GuardReturnEventController : IDisposable
    {
        private readonly ICGPlayer _cgPlayer;
        private readonly DialogueManager _dialogueManager;
        private readonly GuardReturnConfig _config;

        private readonly Action<DialogueCompletedEvent> _onDialogueCompleted;

        private bool _isRunning;
        private bool _hasTriggered;
        private bool _dialoguePhase;
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
        /// <param name="dialogueManager">對話管理器（不可為 null）。</param>
        /// <param name="config">守衛歸來事件配置（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">任一參數為 null 時拋出。</exception>
        public GuardReturnEventController(
            ICGPlayer cgPlayer,
            DialogueManager dialogueManager,
            GuardReturnConfig config)
        {
            _cgPlayer = cgPlayer ?? throw new ArgumentNullException(nameof(cgPlayer));
            _dialogueManager = dialogueManager ?? throw new ArgumentNullException(nameof(dialogueManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _onDialogueCompleted = OnDialogueCompleted;
            EventBus.Subscribe(_onDialogueCompleted);
        }

        /// <summary>
        /// 判斷現在是否可以觸發守衛歸來事件。
        /// 條件：未曾觸發過 + 非執行中 + DialogueManager 空閒。
        /// </summary>
        public bool CanTriggerGuardReturn()
        {
            if (_hasTriggered) return false;
            if (_isRunning) return false;
            if (_dialogueManager.IsActive) return false;
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

            EventBus.Unsubscribe(_onDialogueCompleted);
        }

        // ===== 流程步驟 =====

        private void OnCGComplete()
        {
            if (!_isRunning || _disposed) return;

            // Step 2：播放整段守衛歸來對話（純線性）
            string[] texts = _config.GetAllLineTexts();
            if (texts.Length == 0)
            {
                // 無對話 → 直接跳到完成
                CompleteEvent();
                return;
            }

            _dialoguePhase = true;
            _dialogueManager.StartDialogue(new DialogueData(texts));
        }

        private void OnDialogueCompleted(DialogueCompletedEvent e)
        {
            if (!_isRunning) return;
            if (!_dialoguePhase) return;

            _dialoguePhase = false;
            CompleteEvent();
        }

        private void CompleteEvent()
        {
            _isRunning = false;

            // Step 3：發布完成事件
            // CharacterUnlockManager 已訂閱 GuardReturnEventCompletedEvent，
            // 自動處理守衛解鎖 + 贈劍 grant 派發。
            EventBus.Publish(new GuardReturnEventCompletedEvent());
        }
    }
}
