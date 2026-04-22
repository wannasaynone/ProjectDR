// ICGPlayer — 登場 CG 播放介面（B9 預留、B13 實作）。
// 作用：把「播放角色登場 CG」這件事抽象化，讓 B9 開場劇情演出系統可以在流程中
// 插入 CG 播放環節，同時 B13 登場 CG 播放系統可以替換成真正的 CG 視覺實作。
//
// 使用時機：
// - B9 開場劇情：OpeningSequenceController 呼叫 PlayIntroCG(villageChiefWife) 作為第一步
// - B10 守衛歸來：GuardReturnEventController 在事件流程中呼叫 PlayIntroCG(guard)
// - B13 首次進入流程：CharacterInteractionView（或 NodeDialogueController）呼叫 PlayIntroCG
//
// IT 階段以 PlaceholderCGPlayer 實作，立即回呼 onComplete 以跑通流程。

using System;

namespace ProjectDR.Village.CG
{
    /// <summary>
    /// 登場 CG 播放介面。
    /// 呼叫 PlayIntroCG 開始播放，完成時透過 onComplete 回呼通知。
    /// 實作方須在播放期間發布 CGPlaybackStartedEvent 與 CGPlaybackCompletedEvent
    /// 供其他系統（UI 遮罩、音訊等）協調。
    /// </summary>
    public interface ICGPlayer
    {
        /// <summary>
        /// 播放指定角色的登場 CG。
        /// </summary>
        /// <param name="characterId">角色 ID（對應 CharacterIds；不可為 null/空）。</param>
        /// <param name="onComplete">播放完成時的回呼（不可為 null）。</param>
        void PlayIntroCG(string characterId, Action onComplete);
    }
}
