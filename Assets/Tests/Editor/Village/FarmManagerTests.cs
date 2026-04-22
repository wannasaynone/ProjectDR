using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Storage;
using ProjectDR.Village.Farm;
using ProjectDR.Village.ItemType;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.TimeProvider;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// FarmManager 的單元測試。
    /// 測試對象：建構驗證、格子查詢、種植流程、收穫流程、批次收穫、事件發布、時間計算。
    /// 此測試不依賴 MonoBehaviour 或 Unity 場景，為純邏輯測試。
    /// </summary>
    [TestFixture]
    public class FarmManagerTests
    {
        // ===== 假實作：可控制時間的 ITimeProvider =====

        /// <summary>測試用時間提供者，可在測試中手動設定當前時間。</summary>
        private class FakeTimeProvider : ITimeProvider
        {
            public long CurrentTimestamp { get; set; }
            public long GetCurrentTimestampUtc() => CurrentTimestamp;
        }

        // ===== 常數與共用設定 =====

        private const string WheatSeedId = "wheat_seed";
        private const string WheatId = "wheat";
        private const float WheatGrowthSeconds = 100f;

        private const string CarrotSeedId = "carrot_seed";
        private const string CarrotId = "carrot";
        private const float CarrotGrowthSeconds = 200f;

        private FakeTimeProvider _timeProvider;
        private StorageManager _storageManager;
        private ItemTypeResolver _itemTypeResolver;
        private Dictionary<string, SeedData> _seedDataMap;
        private FarmManager _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            _timeProvider = new FakeTimeProvider { CurrentTimestamp = 1000L };
            _storageManager = new StorageManager();

            _itemTypeResolver = new ItemTypeResolver();
            _itemTypeResolver.Register(WheatSeedId, ItemTypes.Seed);
            _itemTypeResolver.Register(CarrotSeedId, ItemTypes.Seed);
            _itemTypeResolver.Register("wood", ItemTypes.Material);

            _seedDataMap = new Dictionary<string, SeedData>
            {
                { WheatSeedId, new SeedData(WheatSeedId, WheatId, WheatGrowthSeconds) },
                { CarrotSeedId, new SeedData(CarrotSeedId, CarrotId, CarrotGrowthSeconds) },
            };

            // 預先在倉庫放入種子
            _storageManager.AddItem(WheatSeedId, 10);
            _storageManager.AddItem(CarrotSeedId, 10);

            _sut = new FarmManager(3, _seedDataMap, _itemTypeResolver, _storageManager, _timeProvider);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== 建構參數驗證 =====

        [Test]
        public void Constructor_ZeroPlotCount_ThrowsArgumentException()
        {
            // plotCount 為 0 應拋出 ArgumentException
            Assert.Throws<ArgumentException>(() =>
                new FarmManager(0, _seedDataMap, _itemTypeResolver, _storageManager, _timeProvider));
        }

        [Test]
        public void Constructor_NegativePlotCount_ThrowsArgumentException()
        {
            // plotCount 為負數應拋出 ArgumentException
            Assert.Throws<ArgumentException>(() =>
                new FarmManager(-1, _seedDataMap, _itemTypeResolver, _storageManager, _timeProvider));
        }

        [Test]
        public void Constructor_NullSeedDataMap_ThrowsArgumentNullException()
        {
            // seedDataMap 為 null 應拋出 ArgumentNullException
            Assert.Throws<ArgumentNullException>(() =>
                new FarmManager(3, null, _itemTypeResolver, _storageManager, _timeProvider));
        }

        [Test]
        public void Constructor_NullItemTypeResolver_ThrowsArgumentNullException()
        {
            // itemTypeResolver 為 null 應拋出 ArgumentNullException
            Assert.Throws<ArgumentNullException>(() =>
                new FarmManager(3, _seedDataMap, null, _storageManager, _timeProvider));
        }

        [Test]
        public void Constructor_NullStorageManager_ThrowsArgumentNullException()
        {
            // storageManager 為 null 應拋出 ArgumentNullException
            Assert.Throws<ArgumentNullException>(() =>
                new FarmManager(3, _seedDataMap, _itemTypeResolver, null, _timeProvider));
        }

        [Test]
        public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
        {
            // timeProvider 為 null 應拋出 ArgumentNullException
            Assert.Throws<ArgumentNullException>(() =>
                new FarmManager(3, _seedDataMap, _itemTypeResolver, _storageManager, null));
        }

        // ===== PlotCount =====

        [Test]
        public void PlotCount_AfterConstruction_ReturnsCorrectValue()
        {
            // PlotCount 應與建構時傳入的 plotCount 相符
            Assert.AreEqual(3, _sut.PlotCount);
        }

        // ===== GetPlot =====

        [Test]
        public void GetPlot_ValidIndex_ReturnsEmptyPlot()
        {
            // 初始狀態下所有格子應為空
            FarmPlot plot = _sut.GetPlot(0);

            Assert.IsTrue(plot.IsEmpty);
        }

        [Test]
        public void GetPlot_NegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            // 負數索引應拋出 ArgumentOutOfRangeException
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GetPlot(-1));
        }

        [Test]
        public void GetPlot_IndexEqualToPlotCount_ThrowsArgumentOutOfRangeException()
        {
            // 等於 PlotCount 的索引應拋出 ArgumentOutOfRangeException（超出 0-based 範圍）
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GetPlot(_sut.PlotCount));
        }

        [Test]
        public void GetPlot_IndexGreaterThanPlotCount_ThrowsArgumentOutOfRangeException()
        {
            // 超過 PlotCount 的索引應拋出 ArgumentOutOfRangeException
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GetPlot(100));
        }

        // ===== GetAllPlots =====

        [Test]
        public void GetAllPlots_InitialState_AllPlotsAreEmpty()
        {
            // 初始狀態下 GetAllPlots 應全部為空格子
            IReadOnlyList<FarmPlot> plots = _sut.GetAllPlots();

            Assert.AreEqual(3, plots.Count);
            foreach (FarmPlot plot in plots)
            {
                Assert.IsTrue(plot.IsEmpty);
            }
        }

        // ===== Plant 成功情境 =====

        [Test]
        public void Plant_ValidSeedInStorage_ReturnsSuccess()
        {
            // 正常種植應回傳成功結果
            PlantResult result = _sut.Plant(0, WheatSeedId);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(PlantError.None, result.Error);
        }

        [Test]
        public void Plant_Success_PlotBecomesNonEmpty()
        {
            // 種植後格子應不再為空
            _sut.Plant(0, WheatSeedId);

            FarmPlot plot = _sut.GetPlot(0);

            Assert.IsFalse(plot.IsEmpty);
            Assert.AreEqual(WheatSeedId, plot.SeedItemId);
            Assert.AreEqual(WheatId, plot.HarvestItemId);
        }

        [Test]
        public void Plant_Success_StorageDeductsOneSeed()
        {
            // 種植成功後，倉庫應扣除一顆種子
            int beforeCount = _storageManager.GetItemCount(WheatSeedId);

            _sut.Plant(0, WheatSeedId);

            int afterCount = _storageManager.GetItemCount(WheatSeedId);

            Assert.AreEqual(beforeCount - 1, afterCount);
        }

        [Test]
        public void Plant_Success_PlotRecordsPlantedTimestamp()
        {
            // 種植時應記錄當前時間戳記
            _timeProvider.CurrentTimestamp = 5000L;

            _sut.Plant(0, WheatSeedId);

            FarmPlot plot = _sut.GetPlot(0);

            Assert.AreEqual(5000L, plot.PlantedTimestampUtc);
        }

        [Test]
        public void Plant_Success_PublishesFarmPlotPlantedEvent()
        {
            // 種植成功後應發布 FarmPlotPlantedEvent
            FarmPlotPlantedEvent receivedEvent = null;
            Action<FarmPlotPlantedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<FarmPlotPlantedEvent>(handler);

            _sut.Plant(0, WheatSeedId);

            EventBus.Unsubscribe<FarmPlotPlantedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
        }

        [Test]
        public void Plant_Success_EventContainsCorrectPlotIndex()
        {
            // 事件應包含正確的格子索引
            FarmPlotPlantedEvent receivedEvent = null;
            Action<FarmPlotPlantedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<FarmPlotPlantedEvent>(handler);

            _sut.Plant(2, WheatSeedId);

            EventBus.Unsubscribe<FarmPlotPlantedEvent>(handler);

            Assert.AreEqual(2, receivedEvent.PlotIndex);
        }

        [Test]
        public void Plant_Success_EventContainsCorrectSeedItemId()
        {
            // 事件應包含正確的種子 ID
            FarmPlotPlantedEvent receivedEvent = null;
            Action<FarmPlotPlantedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<FarmPlotPlantedEvent>(handler);

            _sut.Plant(0, WheatSeedId);

            EventBus.Unsubscribe<FarmPlotPlantedEvent>(handler);

            Assert.AreEqual(WheatSeedId, receivedEvent.SeedItemId);
        }

        [Test]
        public void Plant_Success_EventContainsCorrectExpectedHarvestTimestamp()
        {
            // 事件中的預計收穫時間 = 種植時間 + 成長秒數
            _timeProvider.CurrentTimestamp = 1000L;

            FarmPlotPlantedEvent receivedEvent = null;
            Action<FarmPlotPlantedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<FarmPlotPlantedEvent>(handler);

            _sut.Plant(0, WheatSeedId);

            EventBus.Unsubscribe<FarmPlotPlantedEvent>(handler);

            long expectedHarvestTs = 1000L + (long)WheatGrowthSeconds;
            Assert.AreEqual(expectedHarvestTs, receivedEvent.ExpectedHarvestTimestampUtc);
        }

        // ===== Plant 失敗情境 =====

        [Test]
        public void Plant_InvalidPlotIndex_ReturnsInvalidPlotIndexError()
        {
            // 超出範圍的格子索引應回傳 InvalidPlotIndex 錯誤
            PlantResult result = _sut.Plant(99, WheatSeedId);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PlantError.InvalidPlotIndex, result.Error);
        }

        [Test]
        public void Plant_NegativePlotIndex_ReturnsInvalidPlotIndexError()
        {
            // 負數格子索引應回傳 InvalidPlotIndex 錯誤
            PlantResult result = _sut.Plant(-1, WheatSeedId);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PlantError.InvalidPlotIndex, result.Error);
        }

        [Test]
        public void Plant_OccupiedPlot_ReturnsPlotNotEmptyError()
        {
            // 已種植的格子再次種植應回傳 PlotNotEmpty 錯誤
            _sut.Plant(0, WheatSeedId);

            PlantResult result = _sut.Plant(0, WheatSeedId);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PlantError.PlotNotEmpty, result.Error);
        }

        [Test]
        public void Plant_NonSeedItem_ReturnsItemNotSeedError()
        {
            // 非種子物品應回傳 ItemNotSeed 錯誤
            _storageManager.AddItem("wood", 5);

            PlantResult result = _sut.Plant(0, "wood");

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PlantError.ItemNotSeed, result.Error);
        }

        [Test]
        public void Plant_UnknownSeed_ReturnsUnknownSeedError()
        {
            // 已是種子類型但無對應 SeedData 時，應回傳 UnknownSeed 錯誤
            _itemTypeResolver.Register("mystery_seed", ItemTypes.Seed);
            _storageManager.AddItem("mystery_seed", 1);

            PlantResult result = _sut.Plant(0, "mystery_seed");

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PlantError.UnknownSeed, result.Error);
        }

        [Test]
        public void Plant_SeedNotInStorage_ReturnsSeedNotInStorageError()
        {
            // 倉庫中沒有該種子應回傳 SeedNotInStorage 錯誤
            StorageManager emptyStorage = new StorageManager();
            FarmManager sut = new FarmManager(3, _seedDataMap, _itemTypeResolver, emptyStorage, _timeProvider);

            PlantResult result = sut.Plant(0, WheatSeedId);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PlantError.SeedNotInStorage, result.Error);
        }

        // ===== Harvest 成功情境 =====

        [Test]
        public void Harvest_ReadyPlot_ReturnsSuccess()
        {
            // 成熟後收穫應回傳成功
            _timeProvider.CurrentTimestamp = 1000L;
            _sut.Plant(0, WheatSeedId);

            // 時間推進到成熟後
            _timeProvider.CurrentTimestamp = 1000L + (long)WheatGrowthSeconds;

            HarvestResult result = _sut.Harvest(0);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(HarvestError.None, result.Error);
        }

        [Test]
        public void Harvest_Success_PlotBecomesEmpty()
        {
            // 收穫後格子應變回空格
            _timeProvider.CurrentTimestamp = 1000L;
            _sut.Plant(0, WheatSeedId);
            _timeProvider.CurrentTimestamp = 1000L + (long)WheatGrowthSeconds;

            _sut.Harvest(0);

            Assert.IsTrue(_sut.GetPlot(0).IsEmpty);
        }

        [Test]
        public void Harvest_Success_HarvestedItemAddedToStorage()
        {
            // 收穫後作物應加入倉庫
            _timeProvider.CurrentTimestamp = 1000L;
            _sut.Plant(0, WheatSeedId);
            _timeProvider.CurrentTimestamp = 1000L + (long)WheatGrowthSeconds;

            _sut.Harvest(0);

            Assert.IsTrue(_storageManager.GetItemCount(WheatId) > 0);
        }

        [Test]
        public void Harvest_Success_PublishesFarmPlotHarvestedEvent()
        {
            // 收穫成功後應發布 FarmPlotHarvestedEvent
            _timeProvider.CurrentTimestamp = 1000L;
            _sut.Plant(0, WheatSeedId);
            _timeProvider.CurrentTimestamp = 1000L + (long)WheatGrowthSeconds;

            FarmPlotHarvestedEvent receivedEvent = null;
            Action<FarmPlotHarvestedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<FarmPlotHarvestedEvent>(handler);

            _sut.Harvest(0);

            EventBus.Unsubscribe<FarmPlotHarvestedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
        }

        [Test]
        public void Harvest_Success_EventContainsCorrectPlotIndex()
        {
            // 收穫事件應包含正確的格子索引
            _timeProvider.CurrentTimestamp = 1000L;
            _sut.Plant(1, WheatSeedId);
            _timeProvider.CurrentTimestamp = 1000L + (long)WheatGrowthSeconds;

            FarmPlotHarvestedEvent receivedEvent = null;
            Action<FarmPlotHarvestedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<FarmPlotHarvestedEvent>(handler);

            _sut.Harvest(1);

            EventBus.Unsubscribe<FarmPlotHarvestedEvent>(handler);

            Assert.AreEqual(1, receivedEvent.PlotIndex);
        }

        [Test]
        public void Harvest_Success_EventContainsCorrectHarvestedItemId()
        {
            // 收穫事件應包含正確的作物 ID
            _timeProvider.CurrentTimestamp = 1000L;
            _sut.Plant(0, WheatSeedId);
            _timeProvider.CurrentTimestamp = 1000L + (long)WheatGrowthSeconds;

            FarmPlotHarvestedEvent receivedEvent = null;
            Action<FarmPlotHarvestedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<FarmPlotHarvestedEvent>(handler);

            _sut.Harvest(0);

            EventBus.Unsubscribe<FarmPlotHarvestedEvent>(handler);

            Assert.AreEqual(WheatId, receivedEvent.HarvestedItemId);
        }

        // ===== Harvest 時間邊界 =====

        [Test]
        public void Harvest_ExactlyAtMaturityTime_ReturnsSuccess()
        {
            // 時間剛好等於種植時間 + 成長秒數時，應可收穫
            _timeProvider.CurrentTimestamp = 1000L;
            _sut.Plant(0, WheatSeedId);

            // 精確到成熟那一刻
            _timeProvider.CurrentTimestamp = 1000L + (long)WheatGrowthSeconds;

            HarvestResult result = _sut.Harvest(0);

            Assert.IsTrue(result.IsSuccess);
        }

        [Test]
        public void Harvest_OneSecondBeforeMaturity_ReturnsNotReadyError()
        {
            // 距成熟還差一秒時，應無法收穫
            _timeProvider.CurrentTimestamp = 1000L;
            _sut.Plant(0, WheatSeedId);

            // 差一秒
            _timeProvider.CurrentTimestamp = 1000L + (long)WheatGrowthSeconds - 1L;

            HarvestResult result = _sut.Harvest(0);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(HarvestError.NotReady, result.Error);
        }

        // ===== Harvest 失敗情境 =====

        [Test]
        public void Harvest_InvalidPlotIndex_ReturnsInvalidPlotIndexError()
        {
            // 超出範圍的格子索引應回傳 InvalidPlotIndex 錯誤
            HarvestResult result = _sut.Harvest(99);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(HarvestError.InvalidPlotIndex, result.Error);
        }

        [Test]
        public void Harvest_NegativePlotIndex_ReturnsInvalidPlotIndexError()
        {
            // 負數格子索引應回傳 InvalidPlotIndex 錯誤
            HarvestResult result = _sut.Harvest(-1);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(HarvestError.InvalidPlotIndex, result.Error);
        }

        [Test]
        public void Harvest_EmptyPlot_ReturnsPlotEmptyError()
        {
            // 空格子收穫應回傳 PlotEmpty 錯誤
            HarvestResult result = _sut.Harvest(0);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(HarvestError.PlotEmpty, result.Error);
        }

        [Test]
        public void Harvest_PlantedButNotReady_ReturnsNotReadyError()
        {
            // 已種植但尚未成熟，應回傳 NotReady 錯誤
            _timeProvider.CurrentTimestamp = 1000L;
            _sut.Plant(0, WheatSeedId);

            // 不推進時間，立刻嘗試收穫
            HarvestResult result = _sut.Harvest(0);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(HarvestError.NotReady, result.Error);
        }

        // ===== HarvestAll =====

        [Test]
        public void HarvestAll_MultipleReadyPlots_HarvestsAll()
        {
            // 多個成熟格子應全部收穫
            _timeProvider.CurrentTimestamp = 1000L;
            _sut.Plant(0, WheatSeedId);
            _sut.Plant(1, WheatSeedId);
            _sut.Plant(2, CarrotSeedId);

            // 推進時間到兩種作物都成熟（取較長的 CarrotGrowthSeconds）
            _timeProvider.CurrentTimestamp = 1000L + (long)CarrotGrowthSeconds;

            HarvestAllResult result = _sut.HarvestAll();

            Assert.AreEqual(3, result.HarvestedCount);
        }

        [Test]
        public void HarvestAll_SomeReadySomeNot_HarvestsOnlyReady()
        {
            // 只有部分格子成熟時，只收成熟的
            _timeProvider.CurrentTimestamp = 1000L;
            _sut.Plant(0, WheatSeedId);     // 100 秒成熟
            _sut.Plant(1, CarrotSeedId);    // 200 秒成熟

            // 只讓小麥成熟
            _timeProvider.CurrentTimestamp = 1000L + (long)WheatGrowthSeconds;

            HarvestAllResult result = _sut.HarvestAll();

            Assert.AreEqual(1, result.HarvestedCount);
            Assert.IsTrue(_sut.GetPlot(0).IsEmpty);   // 小麥格已收
            Assert.IsFalse(_sut.GetPlot(1).IsEmpty);  // 紅蘿蔔格未收
        }

        [Test]
        public void HarvestAll_NoPlotsReady_ReturnsZeroHarvestedCount()
        {
            // 無任何成熟格子時，回傳 HarvestedCount = 0
            _timeProvider.CurrentTimestamp = 1000L;
            _sut.Plant(0, WheatSeedId);

            // 不推進時間
            HarvestAllResult result = _sut.HarvestAll();

            Assert.AreEqual(0, result.HarvestedCount);
        }

        [Test]
        public void HarvestAll_AllPlotsEmpty_ReturnsZeroHarvestedCount()
        {
            // 全部格子為空時，回傳 HarvestedCount = 0
            HarvestAllResult result = _sut.HarvestAll();

            Assert.AreEqual(0, result.HarvestedCount);
        }

        [Test]
        public void HarvestAll_ReadyPlots_PublishesHarvestedEventForEachPlot()
        {
            // 批次收穫應對每個成功的格子發布 FarmPlotHarvestedEvent
            _timeProvider.CurrentTimestamp = 1000L;
            _sut.Plant(0, WheatSeedId);
            _sut.Plant(1, WheatSeedId);
            _timeProvider.CurrentTimestamp = 1000L + (long)WheatGrowthSeconds;

            int eventCount = 0;
            Action<FarmPlotHarvestedEvent> handler = (e) => { eventCount++; };
            EventBus.Subscribe<FarmPlotHarvestedEvent>(handler);

            _sut.HarvestAll();

            EventBus.Unsubscribe<FarmPlotHarvestedEvent>(handler);

            Assert.AreEqual(2, eventCount);
        }

        [Test]
        public void HarvestAll_Success_AllReadyPlotsBecomesEmpty()
        {
            // 批次收穫後所有成熟格子應變回空格
            _timeProvider.CurrentTimestamp = 1000L;
            _sut.Plant(0, WheatSeedId);
            _sut.Plant(2, WheatSeedId);
            _timeProvider.CurrentTimestamp = 1000L + (long)WheatGrowthSeconds;

            _sut.HarvestAll();

            Assert.IsTrue(_sut.GetPlot(0).IsEmpty);
            Assert.IsTrue(_sut.GetPlot(2).IsEmpty);
        }
    }
}
