// FarmPlot — 農田格子資料結構（readonly struct）。
// 代表單一農田格子的狀態，包含種植資訊與收穫計算。

namespace ProjectDR.Village.Farm
{
    /// <summary>
    /// 農田格子資料。
    /// 不可變的值型別，代表單一格子的種植狀態。
    /// </summary>
    public readonly struct FarmPlot
    {
        /// <summary>種植的種子 ID；為 null 代表空格。</summary>
        public string SeedItemId { get; }

        /// <summary>收穫後產出的物品 ID。</summary>
        public string HarvestItemId { get; }

        /// <summary>種植時的 UTC 時間戳記（秒）。</summary>
        public long PlantedTimestampUtc { get; }

        /// <summary>成長所需的秒數。</summary>
        public float GrowthDurationSeconds { get; }

        /// <summary>空格子：SeedItemId 為 null。</summary>
        public static FarmPlot Empty => new FarmPlot(null, null, 0, 0);

        /// <summary>是否為空格（SeedItemId 為 null）。</summary>
        public bool IsEmpty => SeedItemId == null;

        /// <summary>
        /// 建構 FarmPlot。
        /// </summary>
        /// <param name="seedItemId">種子 ID。</param>
        /// <param name="harvestItemId">收穫物 ID。</param>
        /// <param name="plantedTimestampUtc">種植 UTC 時間戳記（秒）。</param>
        /// <param name="growthDurationSeconds">成長秒數。</param>
        public FarmPlot(string seedItemId, string harvestItemId, long plantedTimestampUtc, float growthDurationSeconds)
        {
            SeedItemId = seedItemId;
            HarvestItemId = harvestItemId;
            PlantedTimestampUtc = plantedTimestampUtc;
            GrowthDurationSeconds = growthDurationSeconds;
        }

        /// <summary>
        /// 判斷格子是否已可收穫。
        /// 空格子永遠回傳 false。
        /// </summary>
        /// <param name="currentTimestampUtc">當前 UTC 時間戳記（秒）。</param>
        /// <returns>是否已達收穫條件。</returns>
        public bool IsReadyToHarvest(long currentTimestampUtc)
        {
            if (IsEmpty)
                return false;

            return currentTimestampUtc >= PlantedTimestampUtc + (long)GrowthDurationSeconds;
        }

        /// <summary>
        /// 取得剩餘成長秒數。
        /// 已可收穫時回傳 0；currentTimestamp &lt; PlantedTimestamp 時回傳 GrowthDurationSeconds。
        /// </summary>
        /// <param name="currentTimestampUtc">當前 UTC 時間戳記（秒）。</param>
        /// <returns>剩餘秒數（最小為 0）。</returns>
        public float GetRemainingSeconds(long currentTimestampUtc)
        {
            if (IsEmpty)
                return 0f;

            if (currentTimestampUtc < PlantedTimestampUtc)
                return GrowthDurationSeconds;

            long endTime = PlantedTimestampUtc + (long)GrowthDurationSeconds;
            long remaining = endTime - currentTimestampUtc;
            return remaining > 0 ? (float)remaining : 0f;
        }
    }
}
