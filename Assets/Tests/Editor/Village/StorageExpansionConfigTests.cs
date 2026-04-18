using System;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// StorageExpansionConfig / StorageExpansionConfigData 單元測試。
    /// 驗證 JSON 反序列化、物資字串解析、邊界條件。
    /// </summary>
    [TestFixture]
    public class StorageExpansionConfigTests
    {
        [Test]
        public void Constructor_NullData_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new StorageExpansionConfig(null));
        }

        [Test]
        public void Constructor_EmptyStages_NoStages()
        {
            StorageExpansionConfigData data = new StorageExpansionConfigData
            {
                schema_version = 1,
                max_expansion_level = 0,
                initial_capacity = 100,
                stages = new StorageExpansionStageData[0]
            };
            StorageExpansionConfig config = new StorageExpansionConfig(data);
            Assert.AreEqual(0, config.Stages.Count);
            Assert.IsNull(config.GetStage(1));
        }

        [Test]
        public void GetStage_ParsesRequiredItemsCorrectly()
        {
            StorageExpansionConfigData data = new StorageExpansionConfigData
            {
                schema_version = 1,
                max_expansion_level = 1,
                initial_capacity = 100,
                stages = new StorageExpansionStageData[]
                {
                    new StorageExpansionStageData
                    {
                        level = 1,
                        capacity_before = 100,
                        capacity_after = 150,
                        required_items = "material_wood:10|material_cloth:5",
                        duration_seconds = 90,
                        description = "first stage"
                    }
                }
            };
            StorageExpansionConfig config = new StorageExpansionConfig(data);
            StorageExpansionStage stage = config.GetStage(1);
            Assert.IsNotNull(stage);
            Assert.AreEqual(2, stage.RequiredItems.Count);
            Assert.AreEqual(10, stage.RequiredItems["material_wood"]);
            Assert.AreEqual(5, stage.RequiredItems["material_cloth"]);
            Assert.AreEqual(50, stage.CapacityDelta);
        }

        [Test]
        public void GetStage_EmptyRequiredItems_ReturnsEmptyDictionary()
        {
            StorageExpansionConfigData data = new StorageExpansionConfigData
            {
                schema_version = 1,
                max_expansion_level = 1,
                initial_capacity = 100,
                stages = new StorageExpansionStageData[]
                {
                    new StorageExpansionStageData
                    {
                        level = 1,
                        capacity_before = 100,
                        capacity_after = 150,
                        required_items = "",
                        duration_seconds = 90,
                        description = ""
                    }
                }
            };
            StorageExpansionConfig config = new StorageExpansionConfig(data);
            StorageExpansionStage stage = config.GetStage(1);
            Assert.AreEqual(0, stage.RequiredItems.Count);
        }

        [Test]
        public void GetStage_MalformedRequiredItems_SkipsInvalidPairs()
        {
            StorageExpansionConfigData data = new StorageExpansionConfigData
            {
                schema_version = 1,
                max_expansion_level = 1,
                initial_capacity = 100,
                stages = new StorageExpansionStageData[]
                {
                    new StorageExpansionStageData
                    {
                        level = 1,
                        capacity_before = 100,
                        capacity_after = 150,
                        required_items = "material_wood:10|bad_pair|no_quantity:|:no_id|valid:5",
                        duration_seconds = 90,
                        description = ""
                    }
                }
            };
            StorageExpansionConfig config = new StorageExpansionConfig(data);
            StorageExpansionStage stage = config.GetStage(1);
            Assert.AreEqual(2, stage.RequiredItems.Count);
            Assert.IsTrue(stage.RequiredItems.ContainsKey("material_wood"));
            Assert.IsTrue(stage.RequiredItems.ContainsKey("valid"));
        }

        [Test]
        public void Stages_OrderedByLevel()
        {
            StorageExpansionConfigData data = new StorageExpansionConfigData
            {
                schema_version = 1,
                max_expansion_level = 3,
                initial_capacity = 100,
                stages = new StorageExpansionStageData[]
                {
                    new StorageExpansionStageData { level = 3, capacity_before = 200, capacity_after = 250, required_items = "", duration_seconds = 0 },
                    new StorageExpansionStageData { level = 1, capacity_before = 100, capacity_after = 150, required_items = "", duration_seconds = 0 },
                    new StorageExpansionStageData { level = 2, capacity_before = 150, capacity_after = 200, required_items = "", duration_seconds = 0 }
                }
            };
            StorageExpansionConfig config = new StorageExpansionConfig(data);
            Assert.AreEqual(3, config.Stages.Count);
            Assert.AreEqual(1, config.Stages[0].Level);
            Assert.AreEqual(2, config.Stages[1].Level);
            Assert.AreEqual(3, config.Stages[2].Level);
        }

        [Test]
        public void RealJsonFile_DeserializesSuccessfully()
        {
            string path = "Config/storage-expansion-config";
            TextAsset asset = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                Assert.Pass("storage-expansion-config 資源不存在，跳過真實 JSON 測試。");
                return;
            }

            StorageExpansionConfigData data = JsonUtility.FromJson<StorageExpansionConfigData>(asset.text);
            Assert.IsNotNull(data);
            Assert.GreaterOrEqual(data.stages.Length, 1);

            StorageExpansionConfig config = new StorageExpansionConfig(data);
            Assert.GreaterOrEqual(config.Stages.Count, 1);
            Assert.AreEqual(100, config.InitialCapacity);
        }
    }
}
