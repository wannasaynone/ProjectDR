using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Affinity;
using ProjectDR.Village.Navigation;
using JsonFx.Json;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// AffinityManager 的單元測試。
    /// 測試對象：建構驗證、好感度增減、門檻檢查、事件發布、JSON 配置反序列化。
    /// Sprint 8 Wave 2.5：配合純陣列 DTO 重構（AffinityCharacterData[]，廢棄 AffinityConfigData 包裹類）。
    /// 此測試不依賴 MonoBehaviour 或 Unity 場景，為純邏輯測試。
    /// </summary>
    [TestFixture]
    public class AffinityManagerTests
    {
        private AffinityManager _sut;
        private AffinityConfig _defaultConfig;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            // 預設配置：使用純陣列 AffinityCharacterData
            // VillageChiefWife: 門檻 [5], Guard: 門檻 [3, 7], __default__: 門檻 [10]
            AffinityCharacterData[] entries = new AffinityCharacterData[]
            {
                new AffinityCharacterData { id = 1, character_id = CharacterIds.VillageChiefWife, thresholds = "5" },
                new AffinityCharacterData { id = 2, character_id = CharacterIds.Guard, thresholds = "3,7" },
                new AffinityCharacterData { id = 3, character_id = "__default__", thresholds = "10" }
            };

            _defaultConfig = new AffinityConfig(entries);
            _sut = new AffinityManager(_defaultConfig);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== 建構參數驗證 =====

        [Test]
        public void Constructor_NullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AffinityManager(null));
        }

        // ===== GetAffinity 初始狀態 =====

        [Test]
        public void GetAffinity_InitialValue_ReturnsZero()
        {
            // 任何角色的初始好感度應為 0
            int result = _sut.GetAffinity(CharacterIds.VillageChiefWife);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetAffinity_UnknownCharacter_ReturnsZero()
        {
            // 未在 config 中定義的角色也應回傳 0（使用 defaultThresholds）
            int result = _sut.GetAffinity("UnknownCharacter");
            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetAffinity_NullCharacterId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.GetAffinity(null));
        }

        [Test]
        public void GetAffinity_EmptyCharacterId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.GetAffinity(""));
        }

        // ===== AddAffinity 基本功能 =====

        [Test]
        public void AddAffinity_PositiveAmount_IncreasesValue()
        {
            _sut.AddAffinity(CharacterIds.VillageChiefWife, 3);
            Assert.AreEqual(3, _sut.GetAffinity(CharacterIds.VillageChiefWife));
        }

        [Test]
        public void AddAffinity_MultipleCalls_Accumulates()
        {
            _sut.AddAffinity(CharacterIds.VillageChiefWife, 2);
            _sut.AddAffinity(CharacterIds.VillageChiefWife, 3);
            Assert.AreEqual(5, _sut.GetAffinity(CharacterIds.VillageChiefWife));
        }

        [Test]
        public void AddAffinity_ZeroAmount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _sut.AddAffinity(CharacterIds.VillageChiefWife, 0));
        }

        [Test]
        public void AddAffinity_NegativeAmount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _sut.AddAffinity(CharacterIds.VillageChiefWife, -1));
        }

        [Test]
        public void AddAffinity_NullCharacterId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.AddAffinity(null, 1));
        }

        [Test]
        public void AddAffinity_EmptyCharacterId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.AddAffinity("", 1));
        }

        [Test]
        public void AddAffinity_DifferentCharacters_IndependentValues()
        {
            _sut.AddAffinity(CharacterIds.VillageChiefWife, 3);
            _sut.AddAffinity(CharacterIds.Guard, 1);

            Assert.AreEqual(3, _sut.GetAffinity(CharacterIds.VillageChiefWife));
            Assert.AreEqual(1, _sut.GetAffinity(CharacterIds.Guard));
        }

        // ===== AffinityChangedEvent 事件發布 =====

        [Test]
        public void AddAffinity_PublishesAffinityChangedEvent()
        {
            AffinityChangedEvent receivedEvent = null;
            EventBus.Subscribe<AffinityChangedEvent>(e => receivedEvent = e);

            _sut.AddAffinity(CharacterIds.VillageChiefWife, 2);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(CharacterIds.VillageChiefWife, receivedEvent.CharacterId);
            Assert.AreEqual(2, receivedEvent.NewValue);
            Assert.AreEqual(2, receivedEvent.Amount);
        }

        [Test]
        public void AddAffinity_SecondCall_EventReflectsAccumulatedValue()
        {
            _sut.AddAffinity(CharacterIds.VillageChiefWife, 2);

            AffinityChangedEvent receivedEvent = null;
            EventBus.Subscribe<AffinityChangedEvent>(e => receivedEvent = e);

            _sut.AddAffinity(CharacterIds.VillageChiefWife, 3);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(5, receivedEvent.NewValue);
            Assert.AreEqual(3, receivedEvent.Amount);
        }

        // ===== 門檻觸發 =====

        [Test]
        public void AddAffinity_ReachesThreshold_PublishesThresholdReachedEvent()
        {
            AffinityThresholdReachedEvent receivedEvent = null;
            EventBus.Subscribe<AffinityThresholdReachedEvent>(e => receivedEvent = e);

            // VillageChiefWife 門檻為 [5]，加到 5 應觸發
            _sut.AddAffinity(CharacterIds.VillageChiefWife, 5);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(CharacterIds.VillageChiefWife, receivedEvent.CharacterId);
            Assert.AreEqual(5, receivedEvent.ThresholdValue);
        }

        [Test]
        public void AddAffinity_ExceedsThreshold_PublishesThresholdReachedEvent()
        {
            AffinityThresholdReachedEvent receivedEvent = null;
            EventBus.Subscribe<AffinityThresholdReachedEvent>(e => receivedEvent = e);

            // 直接加到超過門檻值也應觸發
            _sut.AddAffinity(CharacterIds.VillageChiefWife, 6);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(5, receivedEvent.ThresholdValue);
        }

        [Test]
        public void AddAffinity_BelowThreshold_DoesNotPublishThresholdEvent()
        {
            AffinityThresholdReachedEvent receivedEvent = null;
            EventBus.Subscribe<AffinityThresholdReachedEvent>(e => receivedEvent = e);

            // VillageChiefWife 門檻為 [5]，加 4 不應觸發
            _sut.AddAffinity(CharacterIds.VillageChiefWife, 4);

            Assert.IsNull(receivedEvent);
        }

        [Test]
        public void AddAffinity_ThresholdAlreadyReached_DoesNotPublishAgain()
        {
            // 先達到門檻
            _sut.AddAffinity(CharacterIds.VillageChiefWife, 5);

            // 再次訂閱，確認不會重複觸發
            AffinityThresholdReachedEvent receivedEvent = null;
            EventBus.Subscribe<AffinityThresholdReachedEvent>(e => receivedEvent = e);

            _sut.AddAffinity(CharacterIds.VillageChiefWife, 1);

            Assert.IsNull(receivedEvent);
        }

        [Test]
        public void AddAffinity_MultipleThresholds_FirstReached_PublishesOnlyFirst()
        {
            // Guard 有門檻 [3, 7]
            AffinityThresholdReachedEvent receivedEvent = null;
            EventBus.Subscribe<AffinityThresholdReachedEvent>(e => receivedEvent = e);

            _sut.AddAffinity(CharacterIds.Guard, 3);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(3, receivedEvent.ThresholdValue);
        }

        [Test]
        public void AddAffinity_MultipleThresholds_SecondReached_PublishesSecond()
        {
            // Guard 有門檻 [3, 7]，先達到第一個
            _sut.AddAffinity(CharacterIds.Guard, 3);

            AffinityThresholdReachedEvent receivedEvent = null;
            EventBus.Subscribe<AffinityThresholdReachedEvent>(e => receivedEvent = e);

            // 再加到第二個門檻
            _sut.AddAffinity(CharacterIds.Guard, 4);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(7, receivedEvent.ThresholdValue);
        }

        [Test]
        public void AddAffinity_MultipleThresholds_SkipMiddle_PublishesBoth()
        {
            // Guard 有門檻 [3, 7]，一次跳過兩個門檻
            List<AffinityThresholdReachedEvent> receivedEvents = new List<AffinityThresholdReachedEvent>();
            EventBus.Subscribe<AffinityThresholdReachedEvent>(e => receivedEvents.Add(e));

            _sut.AddAffinity(CharacterIds.Guard, 8);

            Assert.AreEqual(2, receivedEvents.Count);
            Assert.AreEqual(3, receivedEvents[0].ThresholdValue);
            Assert.AreEqual(7, receivedEvents[1].ThresholdValue);
        }

        // ===== GetThresholds =====

        [Test]
        public void GetThresholds_ConfiguredCharacter_ReturnsConfiguredThresholds()
        {
            System.Collections.Generic.IReadOnlyList<int> thresholds =
                _sut.GetThresholds(CharacterIds.Guard);

            Assert.AreEqual(2, thresholds.Count);
            Assert.AreEqual(3, thresholds[0]);
            Assert.AreEqual(7, thresholds[1]);
        }

        [Test]
        public void GetThresholds_UnconfiguredCharacter_ReturnsDefaultThresholds()
        {
            // Witch 沒有在 config 中明確配置，應回傳 __default__ 的門檻
            System.Collections.Generic.IReadOnlyList<int> thresholds =
                _sut.GetThresholds(CharacterIds.Witch);

            Assert.AreEqual(1, thresholds.Count);
            Assert.AreEqual(10, thresholds[0]);
        }

        [Test]
        public void GetThresholds_NullCharacterId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.GetThresholds(null));
        }

        // ===== GetReachedThresholds =====

        [Test]
        public void GetReachedThresholds_NoAffinityAdded_ReturnsEmpty()
        {
            System.Collections.Generic.IReadOnlyList<int> reached =
                _sut.GetReachedThresholds(CharacterIds.VillageChiefWife);

            Assert.AreEqual(0, reached.Count);
        }

        [Test]
        public void GetReachedThresholds_AfterThresholdReached_ReturnsReachedValues()
        {
            _sut.AddAffinity(CharacterIds.Guard, 4); // 達到門檻 3

            System.Collections.Generic.IReadOnlyList<int> reached =
                _sut.GetReachedThresholds(CharacterIds.Guard);

            Assert.AreEqual(1, reached.Count);
            Assert.AreEqual(3, reached[0]);
        }

        [Test]
        public void GetReachedThresholds_NullCharacterId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.GetReachedThresholds(null));
        }

        // ===== defaultThresholds 應用於未配置角色 =====

        [Test]
        public void AddAffinity_UnconfiguredCharacter_UsesDefaultThresholds()
        {
            // Witch 未配置，使用 __default__ entry 的門檻 [10]
            AffinityThresholdReachedEvent receivedEvent = null;
            EventBus.Subscribe<AffinityThresholdReachedEvent>(e => receivedEvent = e);

            _sut.AddAffinity(CharacterIds.Witch, 10);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(CharacterIds.Witch, receivedEvent.CharacterId);
            Assert.AreEqual(10, receivedEvent.ThresholdValue);
        }

        // ===== AffinityConfig 與 AffinityCharacterData JSON 反序列化 =====

        [Test]
        public void AffinityCharacterData_Deserialization_CorrectValues()
        {
            // Sprint 8 Wave 2.5：純陣列格式，使用 JsonFx 反序列化
            string json = @"[
                { ""id"": 1, ""character_id"": ""TestChar"", ""thresholds"": ""3,6,9"" }
            ]";

            AffinityCharacterData[] entries = JsonReader.Deserialize<AffinityCharacterData[]>(json);

            Assert.IsNotNull(entries);
            Assert.AreEqual(1, entries.Length);
            Assert.AreEqual("TestChar", entries[0].character_id);
            Assert.AreEqual("3,6,9", entries[0].thresholds);
        }

        [Test]
        public void AffinityConfig_FromEntries_CorrectThresholdsMapping()
        {
            AffinityCharacterData[] entries = new AffinityCharacterData[]
            {
                new AffinityCharacterData { id = 1, character_id = "A", thresholds = "2,4" },
                new AffinityCharacterData { id = 2, character_id = "__default__", thresholds = "8" }
            };

            AffinityConfig config = new AffinityConfig(entries);

            Assert.AreEqual(2, config.GetThresholds("A").Count);
            Assert.AreEqual(2, config.GetThresholds("A")[0]);
            Assert.AreEqual(4, config.GetThresholds("A")[1]);

            // 未配置角色使用 __default__
            Assert.AreEqual(1, config.GetThresholds("B").Count);
            Assert.AreEqual(8, config.GetThresholds("B")[0]);
        }

        [Test]
        public void AffinityConfig_NullEntries_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AffinityConfig(null));
        }

        // ===== ADR-001 IGameData 契約驗證 =====

        [Test]
        public void AffinityCharacterData_ImplementsIGameData()
        {
            AffinityCharacterData data = new AffinityCharacterData
            {
                id = 1,
                character_id = "test_char",
                thresholds = "5"
            };

            Assert.IsInstanceOf<KahaGameCore.GameData.IGameData>(data,
                "AffinityCharacterData 必須實作 IGameData（ADR-001）");
        }

        [Test]
        public void AffinityCharacterData_ID_IsNonZero_WhenSetToPositive()
        {
            const int EXPECTED_ID = 1;
            AffinityCharacterData data = new AffinityCharacterData
            {
                id = EXPECTED_ID,
                character_id = "village_chief_wife",
                thresholds = "5"
            };

            Assert.AreNotEqual(0, data.ID,
                "AffinityCharacterData.ID 不可為 0（ADR-001 IGameData 契約）");
            Assert.AreEqual(EXPECTED_ID, data.ID);
        }

        [Test]
        public void AffinityCharacterData_ID_MapsToIdField()
        {
            AffinityCharacterData data = new AffinityCharacterData
            {
                id = 42,
                character_id = "witch",
                thresholds = "3,7"
            };

            Assert.AreEqual(42, data.ID, "ID property 應對應 int id 欄位");
            Assert.AreEqual("witch", data.character_id, "character_id 語意字串外鍵應獨立保留");
        }
    }
}
