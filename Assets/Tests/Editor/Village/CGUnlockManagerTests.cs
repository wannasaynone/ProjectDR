using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using UnityEngine;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// CGUnlockManager 的單元測試。
    /// 測試對象：建構驗證、解鎖邏輯、事件發布、配置反序列化。
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
            CGSceneConfigData data = new CGSceneConfigData
            {
                scenes = new CGSceneConfigEntry[]
                {
                    new CGSceneConfigEntry
                    {
                        cgSceneId = "vcw_scene_1",
                        characterId = "VillageChiefWife",
                        requiredThreshold = 5,
                        dialogueId = 1001,
                        displayName = "溫柔的夜晚"
                    },
                    new CGSceneConfigEntry
                    {
                        cgSceneId = "guard_scene_1",
                        characterId = "Guard",
                        requiredThreshold = 5,
                        dialogueId = 1002,
                        displayName = "月下的告白"
                    },
                    new CGSceneConfigEntry
                    {
                        cgSceneId = "witch_scene_1",
                        characterId = "Witch",
                        requiredThreshold = 5,
                        dialogueId = 1003,
                        displayName = "實驗室的秘密"
                    },
                    new CGSceneConfigEntry
                    {
                        cgSceneId = "farmgirl_scene_1",
                        characterId = "FarmGirl",
                        requiredThreshold = 5,
                        dialogueId = 1004,
                        displayName = "田間的午後"
                    }
                }
            };
            return new CGSceneConfig(data);
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
        public void Constructor_NullData_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CGSceneConfig(null));
        }

        [Test]
        public void Constructor_ValidData_ParsesScenes()
        {
            CGSceneConfigData data = new CGSceneConfigData
            {
                scenes = new CGSceneConfigEntry[]
                {
                    new CGSceneConfigEntry
                    {
                        cgSceneId = "test_scene",
                        characterId = "TestChar",
                        requiredThreshold = 10,
                        dialogueId = 2001,
                        displayName = "測試場景"
                    }
                }
            };

            CGSceneConfig config = new CGSceneConfig(data);

            IReadOnlyList<CGSceneInfo> scenes = config.GetScenesForCharacter("TestChar");
            Assert.AreEqual(1, scenes.Count);
            Assert.AreEqual("test_scene", scenes[0].CgSceneId);
            Assert.AreEqual(10, scenes[0].RequiredThreshold);
        }

        [Test]
        public void Constructor_NullScenes_NoException()
        {
            CGSceneConfigData data = new CGSceneConfigData { scenes = null };

            CGSceneConfig config = new CGSceneConfig(data);

            IReadOnlyList<CGSceneInfo> scenes = config.GetScenesForCharacter("AnyChar");
            Assert.AreEqual(0, scenes.Count);
        }

        [Test]
        public void GetScenesByThreshold_ReturnsMatchingScenes()
        {
            CGSceneConfigData data = new CGSceneConfigData
            {
                scenes = new CGSceneConfigEntry[]
                {
                    new CGSceneConfigEntry
                    {
                        cgSceneId = "scene_1",
                        characterId = "Char1",
                        requiredThreshold = 5,
                        dialogueId = 1001,
                        displayName = "場景一"
                    },
                    new CGSceneConfigEntry
                    {
                        cgSceneId = "scene_2",
                        characterId = "Char1",
                        requiredThreshold = 10,
                        dialogueId = 1002,
                        displayName = "場景二"
                    }
                }
            };

            CGSceneConfig config = new CGSceneConfig(data);

            IReadOnlyList<CGSceneInfo> scenes = config.GetScenesByThreshold("Char1", 5);
            Assert.AreEqual(1, scenes.Count);
            Assert.AreEqual("scene_1", scenes[0].CgSceneId);
        }

        [Test]
        public void GetSceneInfo_ValidId_ReturnsInfo()
        {
            CGSceneConfigData data = new CGSceneConfigData
            {
                scenes = new CGSceneConfigEntry[]
                {
                    new CGSceneConfigEntry
                    {
                        cgSceneId = "test_scene",
                        characterId = "TestChar",
                        requiredThreshold = 5,
                        dialogueId = 1001,
                        displayName = "測試"
                    }
                }
            };

            CGSceneConfig config = new CGSceneConfig(data);

            CGSceneInfo info = config.GetSceneInfo("test_scene");
            Assert.IsNotNull(info);
            Assert.AreEqual("TestChar", info.CharacterId);
        }

        [Test]
        public void GetSceneInfo_UnknownId_ReturnsNull()
        {
            CGSceneConfigData data = new CGSceneConfigData
            {
                scenes = new CGSceneConfigEntry[0]
            };

            CGSceneConfig config = new CGSceneConfig(data);

            Assert.IsNull(config.GetSceneInfo("unknown"));
        }

        [Test]
        public void JsonDeserialization_ValidJson_ParsesCorrectly()
        {
            string json = @"{
                ""scenes"": [
                    {
                        ""cgSceneId"": ""vcw_scene_1"",
                        ""characterId"": ""VillageChiefWife"",
                        ""requiredThreshold"": 5,
                        ""dialogueId"": 1001,
                        ""displayName"": ""溫柔的夜晚""
                    }
                ]
            }";

            CGSceneConfigData data = JsonUtility.FromJson<CGSceneConfigData>(json);
            CGSceneConfig config = new CGSceneConfig(data);

            CGSceneInfo info = config.GetSceneInfo("vcw_scene_1");
            Assert.IsNotNull(info);
            Assert.AreEqual("VillageChiefWife", info.CharacterId);
            Assert.AreEqual(5, info.RequiredThreshold);
            Assert.AreEqual(1001, info.DialogueId);
            Assert.AreEqual("溫柔的夜晚", info.DisplayName);
        }
    }
}
