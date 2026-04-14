// FarmManager — 農田管理器。
// 管理農田格子的種植、收穫流程，透過 ITimeProvider 取得時間，
// 並透過 StorageManager 處理種子扣除與作物入庫。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    // ===== 種植結果列舉 =====

    /// <summary>種植失敗原因。</summary>
    public enum PlantError
    {
        /// <summary>無錯誤（成功）。</summary>
        None,

        /// <summary>格子索引超出範圍。</summary>
        InvalidPlotIndex,

        /// <summary>格子已有作物，非空。</summary>
        PlotNotEmpty,

        /// <summary>指定物品非種子類型。</summary>
        ItemNotSeed,

        /// <summary>種子不在倉庫中（數量不足）。</summary>
        SeedNotInStorage,

        /// <summary>未知的種子（無對應 SeedData）。</summary>
        UnknownSeed,
    }

    /// <summary>種植操作結果。</summary>
    public class PlantResult
    {
        /// <summary>是否成功。</summary>
        public bool IsSuccess { get; }

        /// <summary>失敗原因；成功時為 None。</summary>
        public PlantError Error { get; }

        private PlantResult(bool isSuccess, PlantError error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        /// <summary>建立成功結果。</summary>
        public static PlantResult Success() => new PlantResult(true, PlantError.None);

        /// <summary>建立失敗結果。</summary>
        public static PlantResult Failure(PlantError error) => new PlantResult(false, error);
    }

    // ===== 收穫結果列舉 =====

    /// <summary>收穫失敗原因。</summary>
    public enum HarvestError
    {
        /// <summary>無錯誤（成功）。</summary>
        None,

        /// <summary>格子索引超出範圍。</summary>
        InvalidPlotIndex,

        /// <summary>格子為空，沒有作物可收穫。</summary>
        PlotEmpty,

        /// <summary>作物尚未成熟。</summary>
        NotReady,
    }

    /// <summary>收穫操作結果。</summary>
    public class HarvestResult
    {
        /// <summary>是否成功。</summary>
        public bool IsSuccess { get; }

        /// <summary>失敗原因；成功時為 None。</summary>
        public HarvestError Error { get; }

        private HarvestResult(bool isSuccess, HarvestError error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        /// <summary>建立成功結果。</summary>
        public static HarvestResult Success() => new HarvestResult(true, HarvestError.None);

        /// <summary>建立失敗結果。</summary>
        public static HarvestResult Failure(HarvestError error) => new HarvestResult(false, error);
    }

    /// <summary>批次收穫（HarvestAll）結果。</summary>
    public class HarvestAllResult
    {
        /// <summary>本次成功收穫的格子數量。</summary>
        public int HarvestedCount { get; }

        public HarvestAllResult(int harvestedCount)
        {
            HarvestedCount = harvestedCount;
        }
    }

    // ===== FarmManager =====

    /// <summary>
    /// 農田管理器。
    /// 管理固定數量的農田格子，提供種植與收穫操作，並透過 EventBus 發布事件。
    /// </summary>
    public class FarmManager
    {
        /// <summary>農田格子總數。</summary>
        public int PlotCount { get; }

        private readonly FarmPlot[] _plots;
        private readonly IReadOnlyDictionary<string, SeedData> _seedDataMap;
        private readonly ItemTypeResolver _itemTypeResolver;
        private readonly StorageManager _storageManager;
        private readonly ITimeProvider _timeProvider;

        /// <summary>
        /// 建構 FarmManager。
        /// </summary>
        /// <param name="plotCount">農田格子數，必須大於 0。</param>
        /// <param name="seedDataMap">種子資料查詢表，不可為 null。</param>
        /// <param name="itemTypeResolver">物品分類解析器，不可為 null。</param>
        /// <param name="storageManager">倉庫管理器，不可為 null。</param>
        /// <param name="timeProvider">時間提供者，不可為 null。</param>
        /// <exception cref="ArgumentException">plotCount &lt;= 0 時拋出。</exception>
        /// <exception cref="ArgumentNullException">任一參數為 null 時拋出。</exception>
        public FarmManager(
            int plotCount,
            IReadOnlyDictionary<string, SeedData> seedDataMap,
            ItemTypeResolver itemTypeResolver,
            StorageManager storageManager,
            ITimeProvider timeProvider)
        {
            if (plotCount <= 0)
                throw new ArgumentException("plotCount 必須大於 0。", nameof(plotCount));
            if (seedDataMap == null)
                throw new ArgumentNullException(nameof(seedDataMap));
            if (itemTypeResolver == null)
                throw new ArgumentNullException(nameof(itemTypeResolver));
            if (storageManager == null)
                throw new ArgumentNullException(nameof(storageManager));
            if (timeProvider == null)
                throw new ArgumentNullException(nameof(timeProvider));

            PlotCount = plotCount;
            _seedDataMap = seedDataMap;
            _itemTypeResolver = itemTypeResolver;
            _storageManager = storageManager;
            _timeProvider = timeProvider;

            _plots = new FarmPlot[plotCount];
            for (int i = 0; i < plotCount; i++)
                _plots[i] = FarmPlot.Empty;
        }

        /// <summary>
        /// 取得指定索引的農田格子。
        /// </summary>
        /// <param name="plotIndex">格子索引（0-based）。</param>
        /// <returns>對應的 FarmPlot。</returns>
        /// <exception cref="ArgumentOutOfRangeException">索引超出範圍時拋出。</exception>
        public FarmPlot GetPlot(int plotIndex)
        {
            if (plotIndex < 0 || plotIndex >= PlotCount)
                throw new ArgumentOutOfRangeException(nameof(plotIndex), $"索引 {plotIndex} 超出範圍 [0, {PlotCount - 1}]。");

            return _plots[plotIndex];
        }

        /// <summary>取得所有農田格子的唯讀副本。</summary>
        public IReadOnlyList<FarmPlot> GetAllPlots()
        {
            return new List<FarmPlot>(_plots);
        }

        /// <summary>
        /// 在指定格子種植種子。
        /// 成功時從倉庫扣除一顆種子，記錄種植狀態，並發布 FarmPlotPlantedEvent。
        /// </summary>
        /// <param name="plotIndex">要種植的格子索引。</param>
        /// <param name="seedItemId">種子物品 ID。</param>
        /// <returns>操作結果。</returns>
        public PlantResult Plant(int plotIndex, string seedItemId)
        {
            // 1. 驗證 plotIndex 範圍
            if (plotIndex < 0 || plotIndex >= PlotCount)
                return PlantResult.Failure(PlantError.InvalidPlotIndex);

            // 2. 格子必須是空的
            if (!_plots[plotIndex].IsEmpty)
                return PlantResult.Failure(PlantError.PlotNotEmpty);

            // 3. 必須是種子類型
            if (!_itemTypeResolver.IsType(seedItemId, ItemTypes.Seed))
                return PlantResult.Failure(PlantError.ItemNotSeed);

            // 4. seedDataMap 必須有該種子的資料
            if (!_seedDataMap.TryGetValue(seedItemId, out SeedData seedData))
                return PlantResult.Failure(PlantError.UnknownSeed);

            // 5. 倉庫必須有庫存
            if (!_storageManager.HasItem(seedItemId, 1))
                return PlantResult.Failure(PlantError.SeedNotInStorage);

            // 6. 扣除庫存
            _storageManager.RemoveItem(seedItemId, 1);

            // 7. 記錄種植狀態
            long plantedAt = _timeProvider.GetCurrentTimestampUtc();
            _plots[plotIndex] = new FarmPlot(seedItemId, seedData.HarvestItemId, plantedAt, seedData.GrowthDurationSeconds);

            // 8. 發布事件
            EventBus.Publish(new FarmPlotPlantedEvent
            {
                PlotIndex = plotIndex,
                SeedItemId = seedItemId,
                ExpectedHarvestTimestampUtc = plantedAt + (long)seedData.GrowthDurationSeconds
            });

            return PlantResult.Success();
        }

        /// <summary>
        /// 收穫指定格子的作物。
        /// 成功時將作物加入倉庫，清空格子，並發布 FarmPlotHarvestedEvent。
        /// </summary>
        /// <param name="plotIndex">要收穫的格子索引。</param>
        /// <returns>操作結果。</returns>
        public HarvestResult Harvest(int plotIndex)
        {
            // 1. 驗證 plotIndex 範圍
            if (plotIndex < 0 || plotIndex >= PlotCount)
                return HarvestResult.Failure(HarvestError.InvalidPlotIndex);

            // 2. 格子必須非空
            FarmPlot plot = _plots[plotIndex];
            if (plot.IsEmpty)
                return HarvestResult.Failure(HarvestError.PlotEmpty);

            // 3. 必須已成熟
            long currentTime = _timeProvider.GetCurrentTimestampUtc();
            if (!plot.IsReadyToHarvest(currentTime))
                return HarvestResult.Failure(HarvestError.NotReady);

            // 4. 將作物加入倉庫
            _storageManager.AddItem(plot.HarvestItemId, 1);

            // 5. 清空格子
            string harvestedItemId = plot.HarvestItemId;
            _plots[plotIndex] = FarmPlot.Empty;

            // 6. 發布事件
            EventBus.Publish(new FarmPlotHarvestedEvent
            {
                PlotIndex = plotIndex,
                HarvestedItemId = harvestedItemId,
                Quantity = 1
            });

            return HarvestResult.Success();
        }

        /// <summary>
        /// 一次收穫所有已成熟的格子。
        /// 對每個成功收穫的格子發布 FarmPlotHarvestedEvent。
        /// 無可收穫格子時 HarvestedCount 為 0。
        /// </summary>
        /// <returns>批次收穫結果。</returns>
        public HarvestAllResult HarvestAll()
        {
            int harvestedCount = 0;
            long currentTime = _timeProvider.GetCurrentTimestampUtc();

            for (int i = 0; i < PlotCount; i++)
            {
                FarmPlot plot = _plots[i];
                if (plot.IsEmpty)
                    continue;
                if (!plot.IsReadyToHarvest(currentTime))
                    continue;

                // 加入倉庫
                _storageManager.AddItem(plot.HarvestItemId, 1);

                // 紀錄 harvestedItemId 再清空格子
                string harvestedItemId = plot.HarvestItemId;
                _plots[i] = FarmPlot.Empty;

                // 發布事件
                EventBus.Publish(new FarmPlotHarvestedEvent
                {
                    PlotIndex = i,
                    HarvestedItemId = harvestedItemId,
                    Quantity = 1
                });

                harvestedCount++;
            }

            return new HarvestAllResult(harvestedCount);
        }
    }
}
