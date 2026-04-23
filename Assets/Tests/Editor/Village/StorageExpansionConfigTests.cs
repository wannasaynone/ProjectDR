using System;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Storage;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// StorageExpansionConfig / StorageExpansionStageData / StorageExpansionRequirementData 單元測試。
    /// Sprint 8 Wave 2.5：配合純陣列 DTO 重構（廢棄包裹類 StorageExpansionConfigData、移除 required_items 管道符字串）。
    /// 驗證 DTO 反序列化、子表需求分組、邊界條件。
    /// </summary>
    [TestFixture]
    public class StorageExpansionConfigTests
    {
        [Test]
        public void Constructor_NullStageEntries_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new StorageExpansionConfig(null, new StorageExpansionRequirementData[0]));
        }

        [Test]
        public void Constructor_NullRequirementEntries_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new StorageExpansionConfig(new StorageExpansionStageData[0], null));
        }

        [Test]
        public void Constructor_EmptyStages_NoStages()
        {
            StorageExpansionConfig config = new StorageExpansionConfig(
                new StorageExpansionStageData[0],
                new StorageExpansionRequirementData[0]);
            Assert.AreEqual(0, config.Stages.Count);
            Assert.IsNull(config.GetStage(1));
        }

        [Test]
        public void Constructor_Level0Entry_SetsInitialCapacity()
        {
            // level=0 entry 的 capacity_after 作為 InitialCapacity（Q7 拍板）
            StorageExpansionStageData[] stages = new StorageExpansionStageData[]
            {
                new StorageExpansionStageData
                {
                    id = 0, level = 0, capacity_before = 0, capacity_after = 100,
                    duration_seconds = 0
                }
            };
            StorageExpansionConfig config = new StorageExpansionConfig(stages, new StorageExpansionRequirementData[0]);
            Assert.AreEqual(100, config.InitialCapacity);
            // level=0 不進入擴建清單
            Assert.AreEqual(0, config.Stages.Count);
        }

        [Test]
        public void GetStage_WithRequirements_ParsesSubTableCorrectly()
        {
            StorageExpansionStageData[] stages = new StorageExpansionStageData[]
            {
                new StorageExpansionStageData
                {
                    id = 1, level = 1,
                    capacity_before = 100, capacity_after = 150,
                    duration_seconds = 90,
                    description = "first stage"
                }
            };
            StorageExpansionRequirementData[] requirements = new StorageExpansionRequirementData[]
            {
                new StorageExpansionRequirementData { id = 1, stage_level = 1, item_id = "material_wood", quantity = 10 },
                new StorageExpansionRequirementData { id = 2, stage_level = 1, item_id = "material_cloth", quantity = 5 },
            };
            StorageExpansionConfig config = new StorageExpansionConfig(stages, requirements);
            StorageExpansionStage stage = config.GetStage(1);
            Assert.IsNotNull(stage);
            Assert.AreEqual(2, stage.RequiredItems.Count);
            Assert.AreEqual(10, stage.RequiredItems["material_wood"]);
            Assert.AreEqual(5, stage.RequiredItems["material_cloth"]);
            Assert.AreEqual(50, stage.CapacityDelta);
        }

        [Test]
        public void GetStage_NoRequirements_ReturnsEmptyDictionary()
        {
            StorageExpansionStageData[] stages = new StorageExpansionStageData[]
            {
                new StorageExpansionStageData
                {
                    id = 1, level = 1,
                    capacity_before = 100, capacity_after = 150,
                    duration_seconds = 90,
                    description = ""
                }
            };
            StorageExpansionConfig config = new StorageExpansionConfig(stages, new StorageExpansionRequirementData[0]);
            StorageExpansionStage stage = config.GetStage(1);
            Assert.AreEqual(0, stage.RequiredItems.Count);
        }

        [Test]
        public void Stages_OrderedByLevel()
        {
            StorageExpansionStageData[] stages = new StorageExpansionStageData[]
            {
                new StorageExpansionStageData { id = 3, level = 3, capacity_before = 200, capacity_after = 250, duration_seconds = 0 },
                new StorageExpansionStageData { id = 1, level = 1, capacity_before = 100, capacity_after = 150, duration_seconds = 0 },
                new StorageExpansionStageData { id = 2, level = 2, capacity_before = 150, capacity_after = 200, duration_seconds = 0 }
            };
            StorageExpansionConfig config = new StorageExpansionConfig(stages, new StorageExpansionRequirementData[0]);
            Assert.AreEqual(3, config.Stages.Count);
            Assert.AreEqual(1, config.Stages[0].Level);
            Assert.AreEqual(2, config.Stages[1].Level);
            Assert.AreEqual(3, config.Stages[2].Level);
        }

        // ===== ADR-001 / ADR-002 A16：IGameData 契約斷言 =====

        [Test]
        public void StorageExpansionStageData_ImplementsIGameData()
        {
            StorageExpansionStageData entry = new StorageExpansionStageData
            {
                id = 1,
                level = 1,
                capacity_before = 100,
                capacity_after = 150,
                duration_seconds = 90
            };

            Assert.That(entry, Is.AssignableTo<KahaGameCore.GameData.IGameData>(),
                "StorageExpansionStageData 必須實作 IGameData（ADR-001 / ADR-002 A16）");
            Assert.That(entry.ID, Is.Not.Zero,
                "StorageExpansionStageData.ID（=id）不得為 0（ADR-002 A16 反序列化要求）");
        }

        [Test]
        public void StorageExpansionRequirementData_ImplementsIGameData()
        {
            StorageExpansionRequirementData entry = new StorageExpansionRequirementData
            {
                id = 1,
                stage_level = 1,
                item_id = "material_wood",
                quantity = 5
            };

            Assert.That(entry, Is.AssignableTo<KahaGameCore.GameData.IGameData>(),
                "StorageExpansionRequirementData 必須實作 IGameData（ADR-001）");
            Assert.That(entry.ID, Is.Not.Zero,
                "StorageExpansionRequirementData.ID 不得為 0");
        }
    }
}
