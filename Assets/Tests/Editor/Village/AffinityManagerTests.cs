using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Affinity;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// AffinityManager 的單元測試。
    /// 測試對象：建構驗證、好感度增減、門檻檢查、事件發布、JSON 配置反序列化。
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

            // 預設配置：每個角色門檻 [5]，使用 defaultThresholds
            AffinityConfigData configData = new AffinityConfigData
            {
                characters = new AffinityCharacterConfigData[]
                {
                    new AffinityCharacterConfigData
                    {
                        characterId = CharacterIds.VillageChiefWife,
                        thresholds = new int[] { 5 }
                    },
                    new AffinityCharacterConfigData
                    {
                        characterId = CharacterIds.Guard,
                        thresholds = new int[] { 3, 7 }
                    }
                },
                defaultThresholds = new int[] { 10 }
            };

            _defaultConfig = new AffinityConfig(configData);
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
            // Witch 沒有在 config 中明確配置，應回傳 defaultThresholds
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
            // Witch 未配置，使用 defaultThresholds [10]
            AffinityThresholdReachedEvent receivedEvent = null;
            EventBus.Subscribe<AffinityThresholdReachedEvent>(e => receivedEvent = e);

            _sut.AddAffinity(CharacterIds.Witch, 10);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(CharacterIds.Witch, receivedEvent.CharacterId);
            Assert.AreEqual(10, receivedEvent.ThresholdValue);
        }

        // ===== AffinityConfig 與 AffinityConfigData JSON 反序列化 =====

        [Test]
        public void AffinityConfigData_Deserialization_CorrectValues()
        {
            string json = @"{
                ""characters"": [
                    {
                        ""characterId"": ""TestChar"",
                        ""thresholds"": [3, 6, 9]
                    }
                ],
                ""defaultThresholds"": [5]
            }";

            AffinityConfigData data = UnityEngine.JsonUtility.FromJson<AffinityConfigData>(json);

            Assert.IsNotNull(data);
            Assert.IsNotNull(data.characters);
            Assert.AreEqual(1, data.characters.Length);
            Assert.AreEqual("TestChar", data.characters[0].characterId);
            Assert.AreEqual(3, data.characters[0].thresholds.Length);
            Assert.AreEqual(3, data.characters[0].thresholds[0]);
            Assert.AreEqual(6, data.characters[0].thresholds[1]);
            Assert.AreEqual(9, data.characters[0].thresholds[2]);
            Assert.AreEqual(1, data.defaultThresholds.Length);
            Assert.AreEqual(5, data.defaultThresholds[0]);
        }

        [Test]
        public void AffinityConfig_FromData_CorrectThresholdsMapping()
        {
            AffinityConfigData data = new AffinityConfigData
            {
                characters = new AffinityCharacterConfigData[]
                {
                    new AffinityCharacterConfigData
                    {
                        characterId = "A",
                        thresholds = new int[] { 2, 4 }
                    }
                },
                defaultThresholds = new int[] { 8 }
            };

            AffinityConfig config = new AffinityConfig(data);

            Assert.AreEqual(2, config.GetThresholds("A").Count);
            Assert.AreEqual(2, config.GetThresholds("A")[0]);
            Assert.AreEqual(4, config.GetThresholds("A")[1]);

            // 未配置角色使用 defaultThresholds
            Assert.AreEqual(1, config.GetThresholds("B").Count);
            Assert.AreEqual(8, config.GetThresholds("B")[0]);
        }

        [Test]
        public void AffinityConfig_NullData_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AffinityConfig(null));
        }

        // ===== ADR-001 IGameData 契約驗證（ADR-002 A01 改造後新增）=====

        [Test]
        public void AffinityCharacterConfigData_ImplementsIGameData()
        {
            // AffinityCharacterConfigData 必須實作 IGameData（ADR-001）
            var data = new AffinityCharacterConfigData
            {
                id = 1,
                characterId = "test_char",
                thresholds = new int[] { 5 }
            };

            Assert.IsInstanceOf<KahaGameCore.GameData.IGameData>(data,
                "AffinityCharacterConfigData 必須實作 IGameData（ADR-001）");
        }

        [Test]
        public void AffinityCharacterConfigData_ID_IsNonZero_WhenSetToPositive()
        {
            // IGameData.ID 非 0 斷言（ADR-001：ID 必須可區分資料行）
            const int EXPECTED_ID = 1;
            var data = new AffinityCharacterConfigData
            {
                id = EXPECTED_ID,
                characterId = "village_chief_wife",
                thresholds = new int[] { 5 }
            };

            Assert.AreNotEqual(0, data.ID,
                "AffinityCharacterConfigData.ID 不可為 0（ADR-001 IGameData 契約）");
            Assert.AreEqual(EXPECTED_ID, data.ID);
        }

        [Test]
        public void AffinityCharacterConfigData_ID_MapsToIdField()
        {
            // 確認 IGameData.ID property 對應 int id 欄位（雙欄位規則：ID + characterId）
            var data = new AffinityCharacterConfigData
            {
                id = 42,
                characterId = "witch",
                thresholds = new int[] { 3, 7 }
            };

            Assert.AreEqual(42, data.ID, "ID property 應對應 int id 欄位");
            Assert.AreEqual("witch", data.characterId, "characterId 語意字串外鍵應獨立保留");
        }

        [Test]
        public void AffinityConfigData_Deserialization_WithId_CorrectValues()
        {
            // 驗證包含 id 欄位的 JSON 可正確反序列化（ADR-001 改造後 JSON 格式）
            string json = @"{
                ""characters"": [
                    {
                        ""id"": 1,
                        ""characterId"": ""TestChar"",
                        ""thresholds"": [3, 6, 9]
                    }
                ],
                ""defaultThresholds"": [5]
            }";

            AffinityConfigData data = UnityEngine.JsonUtility.FromJson<AffinityConfigData>(json);

            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.characters.Length);
            Assert.AreEqual(1, data.characters[0].id);
            Assert.AreEqual(1, data.characters[0].ID);
            Assert.AreEqual("TestChar", data.characters[0].characterId);
            Assert.AreNotEqual(0, data.characters[0].ID,
                "反序列化後 ID 不可為 0（ADR-001 IGameData 契約）");
        }
    }
}
