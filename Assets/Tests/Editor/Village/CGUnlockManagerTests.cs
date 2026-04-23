using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.CG;
using JsonFx.Json;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// CGUnlockManager 的單元測試。
    /// 測試對象：建構驗證、解鎖邏輯、事件發布、配置反序列化。
    /// Sprint 8 Wave 2.5：配合純陣列 DTO 重構（CGSceneData[]，廢棄 CGSceneConfigData 包裹類）。
    /// </summary>
    [TestFixture]
    public class CGUnlockManagerTests
    {
        private CGUnlockManager _sut;
        private CGSceneConfig _defaultConfig;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            _defaultConfig = CreateDefaultConfig();
            _sut = new CGUnlockManager(_defaultConfig);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        private static CGSceneConfig CreateDefaultConfig()
        {
            CGSceneData[] entries = new CGSceneData[]
            {
                new CGSceneData
                {
                    id = 1,
                    cg_scene_id = "vcw_scene_1",
                    character_id = "VillageChiefWife",
                    required_threshold = 5,
                    dialogue_id = 1001,
                    display_name = "溫柔的夜晚"
                },
                new CGSceneData
                {
                    id = 2,
                    cg_scene_id = "guard_scene_1",
                    character_id = "Guard",
                    required_threshold = 5,
                    dialogue_id = 1002,
                    display_name = "月下的告白"
                },
                new CGSceneData
                {
                    id = 3,
                    cg_scene_id = "witch_scene_1",
                    character_id = "Witch",
                    required_threshold = 5,
                    dialogue_id = 1003,
                    display_name = "實驗室的秘密"
                },
                new CGSceneData
                {
                    id = 4,
                    cg_scene_id = "farmgirl_scene_1",
                    character_id = "FarmGirl",
                    required_threshold = 5,
                    dialogue_id = 1004,
                    display_name = "田間的午後"
                }
            };
            return new CGSceneConfig(entries);
        }

        // ===== 建構驗證 =====

        [Test]
        public void Constructor_NullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CGUnlockManager(null));
        }

        [Test]
        public void Constructor_ValidConfig_NoException()
        {
            Assert.DoesNotThrow(() => new CGUnlockManager(_defaultConfig));
        }

        // ===== IsUnlocked =====

        [Test]
        public void IsUnlocked_NotUnlockedScene_ReturnsFalse()
        {
            Assert.IsFalse(_sut.IsUnlocked("vcw_scene_1"));
        }

        [Test]
        public void IsUnlocked_NullId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.IsUnlocked(null));
        }

        [Test]
        public void IsUnlocked_EmptyId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.IsUnlocked(""));
        }

        // ===== UnlockScene =====

        [Test]
        public void UnlockScene_ValidId_SetsUnlocked()
        {
            _sut.UnlockScene("vcw_scene_1");

            Assert.IsTrue(_sut.IsUnlocked("vcw_scene_1"));
        }

        [Test]
        public void UnlockScene_AlreadyUnlocked_NoError()
        {
            _sut.UnlockScene("vcw_scene_1");
            _sut.UnlockScene("vcw_scene_1");

            Assert.IsTrue(_sut.IsUnlocked("vcw_scene_1"));
        }

        [Test]
        public void UnlockScene_NullId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.UnlockScene(null));
        }

        [Test]
        public void UnlockScene_EmptyId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.UnlockScene(""));
        }

        [Test]
        public void UnlockScene_PublishesCGUnlockedEvent()
        {
            CGUnlockedEvent receivedEvent = null;
            EventBus.Subscribe<CGUnlockedEvent>(e => receivedEvent = e);

            _sut.UnlockScene("vcw_scene_1");

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual("vcw_scene_1", receivedEvent.CgSceneId);
            Assert.AreEqual("VillageChiefWife", receivedEvent.CharacterId);
        }

        [Test]
        public void UnlockScene_AlreadyUnlocked_DoesNotPublishEvent()
        {
            _sut.UnlockScene("vcw_scene_1");

            int eventCount = 0;
            EventBus.Subscribe<CGUnlockedEvent>(e => eventCount++);

            _sut.UnlockScene("vcw_scene_1");

            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void UnlockScene_UnknownSceneId_DoesNotPublishEvent()
        {
            // 未在配置中的場景 ID，不發布事件（無法取得 characterId）
            int eventCount = 0;
            EventBus.Subscribe<CGUnlockedEvent>(e => eventCount++);

            _sut.UnlockScene("unknown_scene");

            // 仍然標記為已解鎖
            Assert.IsTrue(_sut.IsUnlocked("unknown_scene"));
            // 但不發布事件（找不到 characterId）
            Assert.AreEqual(0, eventCount);
        }

        // ===== GetUnlockedScenes =====

        [Test]
        public void GetUnlockedScenes_NoUnlocks_ReturnsEmpty()
        {
            IReadOnlyList<CGSceneInfo> scenes = _sut.GetUnlockedScenes("VillageChiefWife");

            Assert.IsNotNull(scenes);
            Assert.AreEqual(0, scenes.Count);
        }

        [Test]
        public void GetUnlockedScenes_WithUnlock_ReturnsMatchingScenes()
        {
            _sut.UnlockScene("vcw_scene_1");

            IReadOnlyList<CGSceneInfo> scenes = _sut.GetUnlockedScenes("VillageChiefWife");

            Assert.AreEqual(1, scenes.Count);
            Assert.AreEqual("vcw_scene_1", scenes[0].CgSceneId);
            Assert.AreEqual(1001, scenes[0].DialogueId);
            Assert.AreEqual("溫柔的夜晚", scenes[0].DisplayName);
        }

        [Test]
        public void GetUnlockedScenes_OtherCharacter_DoesNotInclude()
        {
            _sut.UnlockScene("vcw_scene_1");

            IReadOnlyList<CGSceneInfo> scenes = _sut.GetUnlockedScenes("Guard");

            Assert.AreEqual(0, scenes.Count);
        }

        [Test]
        public void GetUnlockedScenes_NullCharacterId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.GetUnlockedScenes(null));
        }

        // ===== AffinityThresholdReachedEvent 監聽 =====

        [Test]
        public void OnAffinityThresholdReached_MatchingConfig_UnlocksScene()
        {
            // 模擬好感度達標事件
            EventBus.Publish(new AffinityThresholdReachedEvent
            {
                CharacterId = "VillageChiefWife",
                ThresholdValue = 5
            });

            Assert.IsTrue(_sut.IsUnlocked("vcw_scene_1"));
        }

        [Test]
        public void OnAffinityThresholdReached_NonMatchingThreshold_DoesNotUnlock()
        {
            EventBus.Publish(new AffinityThresholdReachedEvent
            {
                CharacterId = "VillageChiefWife",
                ThresholdValue = 10  // 配置中門檻是 5，不是 10
            });

            Assert.IsFalse(_sut.IsUnlocked("vcw_scene_1"));
        }

        [Test]
        public void OnAffinityThresholdReached_UnknownCharacter_DoesNotUnlock()
        {
            EventBus.Publish(new AffinityThresholdReachedEvent
            {
                CharacterId = "UnknownChar",
                ThresholdValue = 5
            });

            Assert.IsFalse(_sut.IsUnlocked("vcw_scene_1"));
        }

        // ===== Dispose =====

        [Test]
        public void Dispose_UnsubscribesFromEvents()
        {
            _sut.Dispose();

            // Dispose 後事件不應再觸發解鎖
            EventBus.Publish(new AffinityThresholdReachedEvent
            {
                CharacterId = "VillageChiefWife",
                ThresholdValue = 5
            });

            Assert.IsFalse(_sut.IsUnlocked("vcw_scene_1"));
        }

        // ===== HasUnlockedScenes =====

        [Test]
        public void HasUnlockedScenes_NoUnlocks_ReturnsFalse()
        {
            Assert.IsFalse(_sut.HasUnlockedScenes("VillageChiefWife"));
        }

        [Test]
        public void HasUnlockedScenes_WithUnlock_ReturnsTrue()
        {
            _sut.UnlockScene("vcw_scene_1");

            Assert.IsTrue(_sut.HasUnlockedScenes("VillageChiefWife"));
        }

        [Test]
        public void HasUnlockedScenes_NullCharacterId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.HasUnlockedScenes(null));
        }
    }

    // ===== CGSceneConfig 反序列化測試 =====

    [TestFixture]
    public class CGSceneConfigTests
    {
        [Test]
        public void Constructor_NullEntries_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CGSceneConfig(null));
        }

        [Test]
        public void Constructor_ValidEntries_ParsesScenes()
        {
            CGSceneData[] entries = new CGSceneData[]
            {
                new CGSceneData
                {
                    id = 1,
                    cg_scene_id = "test_scene",
                    character_id = "TestChar",
                    required_threshold = 10,
                    dialogue_id = 2001,
                    display_name = "測試場景"
                }
            };

            CGSceneConfig config = new CGSceneConfig(entries);

            IReadOnlyList<CGSceneInfo> scenes = config.GetScenesForCharacter("TestChar");
            Assert.AreEqual(1, scenes.Count);
            Assert.AreEqual("test_scene", scenes[0].CgSceneId);
            Assert.AreEqual(10, scenes[0].RequiredThreshold);
        }

        // ===== ADR-001 / ADR-002 A02：IGameData 契約斷言 =====

        [Test]
        public void CGSceneData_ImplementsIGameData()
        {
            CGSceneData entry = new CGSceneData
            {
                id = 7,
                cg_scene_id = "vcw_scene_1",
                character_id = "VillageChiefWife",
                required_threshold = 5,
                dialogue_id = 1001,
                display_name = "溫柔的夜晚"
            };

            // IGameData 介面斷言
            Assert.IsInstanceOf<KahaGameCore.GameData.IGameData>(entry,
                "CGSceneData 必須實作 IGameData（ADR-001 / ADR-002 A02）");
            // ID 非 0
            Assert.AreNotEqual(0, entry.ID,
                "CGSceneData.ID 不得為 0（ADR-002 A02 反序列化要求）");
            // Key 與 cg_scene_id 一致
            Assert.AreEqual(entry.cg_scene_id, entry.Key,
                "CGSceneData.Key 應回傳與 cg_scene_id 相同的語意字串");
        }

        [Test]
        public void Constructor_EmptyEntries_NoException()
        {
            CGSceneConfig config = new CGSceneConfig(new CGSceneData[0]);

            IReadOnlyList<CGSceneInfo> scenes = config.GetScenesForCharacter("AnyChar");
            Assert.AreEqual(0, scenes.Count);
        }

        [Test]
        public void GetScenesByThreshold_ReturnsMatchingScenes()
        {
            CGSceneData[] entries = new CGSceneData[]
            {
                new CGSceneData
                {
                    id = 1,
                    cg_scene_id = "scene_1",
                    character_id = "Char1",
                    required_threshold = 5,
                    dialogue_id = 1001,
                    display_name = "場景一"
                },
                new CGSceneData
                {
                    id = 2,
                    cg_scene_id = "scene_2",
                    character_id = "Char1",
                    required_threshold = 10,
                    dialogue_id = 1002,
                    display_name = "場景二"
                }
            };

            CGSceneConfig config = new CGSceneConfig(entries);

            IReadOnlyList<CGSceneInfo> scenes = config.GetScenesByThreshold("Char1", 5);
            Assert.AreEqual(1, scenes.Count);
            Assert.AreEqual("scene_1", scenes[0].CgSceneId);
        }

        [Test]
        public void GetSceneInfo_ValidId_ReturnsInfo()
        {
            CGSceneData[] entries = new CGSceneData[]
            {
                new CGSceneData
                {
                    id = 1,
                    cg_scene_id = "test_scene",
                    character_id = "TestChar",
                    required_threshold = 5,
                    dialogue_id = 1001,
                    display_name = "測試"
                }
            };

            CGSceneConfig config = new CGSceneConfig(entries);

            CGSceneInfo info = config.GetSceneInfo("test_scene");
            Assert.IsNotNull(info);
            Assert.AreEqual("TestChar", info.CharacterId);
        }

        [Test]
        public void GetSceneInfo_UnknownId_ReturnsNull()
        {
            CGSceneConfig config = new CGSceneConfig(new CGSceneData[0]);

            Assert.IsNull(config.GetSceneInfo("unknown"));
        }

        [Test]
        public void JsonDeserialization_ValidJson_ParsesCorrectly()
        {
            // Sprint 8 Wave 2.5：純陣列格式，使用 JsonFx 反序列化
            string json = @"[
                {
                    ""id"": 1,
                    ""cg_scene_id"": ""vcw_scene_1"",
                    ""character_id"": ""VillageChiefWife"",
                    ""required_threshold"": 5,
                    ""dialogue_id"": 1001,
                    ""display_name"": ""溫柔的夜晚""
                }
            ]";

            CGSceneData[] entries = JsonReader.Deserialize<CGSceneData[]>(json);
            CGSceneConfig config = new CGSceneConfig(entries);

            CGSceneInfo info = config.GetSceneInfo("vcw_scene_1");
            Assert.IsNotNull(info);
            Assert.AreEqual("VillageChiefWife", info.CharacterId);
            Assert.AreEqual(5, info.RequiredThreshold);
            Assert.AreEqual(1001, info.DialogueId);
            Assert.AreEqual("溫柔的夜晚", info.DisplayName);
        }
    }
}
