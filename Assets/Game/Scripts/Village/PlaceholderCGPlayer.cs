// PlaceholderCGPlayer — ICGPlayer 的 IT 階段 placeholder 實作（B9）。
//
// 用途：B9 批次先建立此 placeholder 實作，讓 OpeningSequenceController / GuardReturnEventController
// 的流程可以跑通。B13 批次會建立真正的 CG 播放視覺系統取代此實作。
//
// 行為：
// - 收到 PlayIntroCG 呼叫後，發布 CGPlaybackStartedEvent
// - 接著立即發布 CGPlaybackCompletedEvent 並呼叫 onComplete（不做視覺播放）
// - IT 期間可觀察事件日誌確認流程正確

using System;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// ICGPlayer 的 placeholder 實作。
    /// 立即完成以跑通上游流程。發布 CGPlaybackStartedEvent/CGPlaybackCompletedEvent
    /// 供 UI 層預留偵聽。
    /// </summary>
    public class PlaceholderCGPlayer : ICGPlayer
    {
        private readonly CharacterIntroConfig _introConfig;

        /// <summary>
        /// 建構 placeholder CG 播放器。
        /// </summary>
        /// <param name="introConfig">登場 CG 配置（可為 null；為 null 時無法查詢 intro_id，事件中 IntroId 為空字串）。</param>
        public PlaceholderCGPlayer(CharacterIntroConfig introConfig)
        {
            _introConfig = introConfig;
        }

        /// <inheritdoc />
        public void PlayIntroCG(string characterId, Action onComplete)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                // 無效輸入也立刻回呼，避免卡流程
                onComplete?.Invoke();
                return;
            }

            string introId = string.Empty;
            if (_introConfig != null)
            {
                CharacterIntroInfo info = _introConfig.GetIntroByCharacter(characterId);
                if (info != null)
                {
                    introId = info.IntroId;
                }
            }

            EventBus.Publish(new CGPlaybackStartedEvent
            {
                IntroId = introId,
                CharacterId = characterId,
            });

            // Placeholder：不做視覺播放，直接完成
            EventBus.Publish(new CGPlaybackCompletedEvent
            {
                IntroId = introId,
                CharacterId = characterId,
            });

            onComplete?.Invoke();
        }
    }
}
