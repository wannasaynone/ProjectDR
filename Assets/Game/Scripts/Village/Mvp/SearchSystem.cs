// SearchSystem — 搜索系統。
// 玩家點擊「搜索附近」→ 啟動冷卻並進入 pending 狀態。
// Tick 推進，冷卻結束當幀結算：加木材 + 發布 MvpSearchCompletedEvent。

using System;

namespace ProjectDR.Village.Mvp
{
    /// <summary>
    /// 搜索系統：點擊時啟動冷卻，冷卻結束才加木材（模擬「翻找中 → 找到」的時序）。
    /// 不擁有冷卻計時本體，仰賴 ActionTimeManager 統一處理寒冷倍率。
    /// </summary>
    public class SearchSystem
    {
        private readonly ResourceManager _resourceManager;
        private readonly ActionTimeManager _actionTime;
        private readonly MvpConfig _config;
        private readonly IRandomSource _random;

        private bool _pending;
        private string _pendingFeedback;

        /// <summary>
        /// 建構搜索系統。
        /// </summary>
        /// <param name="resourceManager">資源管理器（不可 null）。</param>
        /// <param name="actionTime">冷卻計時管理器（不可 null）。</param>
        /// <param name="config">MVP 配置（不可 null）。</param>
        /// <param name="random">隨機來源（不可 null，測試時可替換）。</param>
        public SearchSystem(
            ResourceManager resourceManager,
            ActionTimeManager actionTime,
            MvpConfig config,
            IRandomSource random)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _actionTime = actionTime ?? throw new ArgumentNullException(nameof(actionTime));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>目前是否可以搜索（未在冷卻中且無 pending）。</summary>
        public bool CanSearch => !_pending && !_actionTime.IsOnCooldown(MvpActionKeys.Search);

        /// <summary>是否有 pending 的搜索（冷卻中）。</summary>
        public bool IsPending => _pending;

        /// <summary>
        /// 嘗試發起一次搜索。
        /// 若仍在冷卻中或有 pending 回傳 false；
        /// 否則啟動冷卻，登記 pending，待 Tick 結算時才加木材並發事件。
        /// </summary>
        public bool TrySearch()
        {
            if (_pending) return false;
            if (!_actionTime.TryStartCooldown(MvpActionKeys.Search, _config.SearchCooldownSeconds))
            {
                return false;
            }

            _pending = true;
            _pendingFeedback = PickRandomLine();
            return true;
        }

        /// <summary>
        /// 每幀推進：若 pending 且冷卻已結束，則結算（加木材 + 發事件）。
        /// MvpEntryPoint Update 順序須在 ActionTimeManager.Tick 之後呼叫。
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f) throw new ArgumentException("deltaSeconds 不可為負。", nameof(deltaSeconds));
            if (!_pending) return;
            if (_actionTime.IsOnCooldown(MvpActionKeys.Search)) return;

            _resourceManager.Add(MvpResourceIds.Wood, _config.SearchWoodGainPerSearch);
            KahaGameCore.GameEvent.EventBus.Publish(new MvpSearchCompletedEvent
            {
                FeedbackLine = _pendingFeedback,
                WoodGained = _config.SearchWoodGainPerSearch
            });
            _pending = false;
            _pendingFeedback = null;
        }

        private string PickRandomLine()
        {
            int count = _config.SearchFeedbackLines.Count;
            if (count == 0) return string.Empty;
            int idx = _random.Range(0, count);
            if (idx < 0) idx = 0;
            if (idx >= count) idx = count - 1;
            return _config.SearchFeedbackLines[idx];
        }
    }

    /// <summary>隨機來源介面（測試可替換為確定性實作）。</summary>
    public interface IRandomSource
    {
        /// <summary>回傳 [minInclusive, maxExclusive) 範圍內的整數。</summary>
        int Range(int minInclusive, int maxExclusive);
    }

    /// <summary>使用 System.Random 的隨機來源。</summary>
    public class SystemRandomSource : IRandomSource
    {
        private readonly Random _rng;
        public SystemRandomSource() { _rng = new Random(); }
        public SystemRandomSource(int seed) { _rng = new Random(seed); }
        public int Range(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);
    }
}
