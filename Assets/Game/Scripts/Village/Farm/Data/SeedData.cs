// SeedData — 種子資料結構。
// 定義種子 ID、收穫物 ID 與成長時間，供 FarmManager 查詢種植參數。

using System;

namespace ProjectDR.Village.Farm
{
    /// <summary>
    /// 種子資料。
    /// 包含種子 ID、收穫物 ID 與成長所需秒數。
    /// </summary>
    public class SeedData
    {
        /// <summary>種子的物品 ID。</summary>
        public string SeedItemId { get; }

        /// <summary>收穫後產出的物品 ID。</summary>
        public string HarvestItemId { get; }

        /// <summary>從種植到可收穫所需的秒數。</summary>
        public float GrowthDurationSeconds { get; }

        /// <summary>
        /// 建構 SeedData。
        /// </summary>
        /// <param name="seedItemId">種子 ID，不可為 null 或空字串。</param>
        /// <param name="harvestItemId">收穫物 ID，不可為 null 或空字串。</param>
        /// <param name="growthDurationSeconds">成長秒數，必須大於 0。</param>
        /// <exception cref="ArgumentException">任一字串為 null/empty 或秒數 &lt;= 0 時拋出。</exception>
        public SeedData(string seedItemId, string harvestItemId, float growthDurationSeconds)
        {
            if (string.IsNullOrEmpty(seedItemId))
                throw new ArgumentException("seedItemId 不可為 null 或空字串。", nameof(seedItemId));
            if (string.IsNullOrEmpty(harvestItemId))
                throw new ArgumentException("harvestItemId 不可為 null 或空字串。", nameof(harvestItemId));
            if (growthDurationSeconds <= 0f)
                throw new ArgumentException("growthDurationSeconds 必須大於 0。", nameof(growthDurationSeconds));

            SeedItemId = seedItemId;
            HarvestItemId = harvestItemId;
            GrowthDurationSeconds = growthDurationSeconds;
        }
    }
}
