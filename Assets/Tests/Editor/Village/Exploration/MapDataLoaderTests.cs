using System;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village.Exploration;
using ProjectDR.Village.Exploration.Map;

namespace ProjectDR.Tests.Village.Exploration
{
    /// <summary>
    /// MapDataLoader unit tests.
    /// Covers: JSON deserialization, dimension parsing, spawn position, cell conversion, evacuation groups,
    /// and error handling for null/empty/invalid input.
    /// Pure logic tests — no MonoBehaviour or Unity scene dependency.
    /// Note: JsonUtility is available in EditMode tests via UnityEngine.
    /// </summary>
    [TestFixture]
    public class MapDataLoaderTests
    {
        // ===== Shared valid JSON fixture =====

        // 3x3 map, spawn at (1,1), one evacuation group with two points.
        // cells: row-major, 0=Explorable, 1=Blocked
        // Layout (y=0 top):
        //   0 0 0
        //   0 0 0
        //   0 0 0
        private const string ValidJson3x3 =
            "{" +
            "\"width\":3,\"height\":3," +
            "\"cells\":[0,0,0,0,0,0,0,0,0]," +
            "\"spawnPosition\":{\"x\":1,\"y\":1}," +
            "\"evacuationGroups\":[{\"points\":[{\"x\":0,\"y\":0},{\"x\":2,\"y\":0}]}]" +
            "}";

        // ===== Test 1: Load_ValidJson_ReturnsMapData =====

        [Test]
        public void Load_ValidJson_ReturnsMapData()
        {
            MapData result = MapDataLoader.Load(ValidJson3x3);

            Assert.IsNotNull(result);
        }

        // ===== Test 2: Load_ValidJson_CorrectDimensions =====

        [Test]
        public void Load_ValidJson_CorrectDimensions()
        {
            MapData result = MapDataLoader.Load(ValidJson3x3);

            Assert.AreEqual(3, result.Width);
            Assert.AreEqual(3, result.Height);
        }

        // ===== Test 3: Load_ValidJson_CorrectSpawnPosition =====

        [Test]
        public void Load_ValidJson_CorrectSpawnPosition()
        {
            MapData result = MapDataLoader.Load(ValidJson3x3);

            Assert.AreEqual(new Vector2Int(1, 1), result.SpawnPosition);
        }

        // ===== Test 4: Load_ValidJson_CorrectCellTypes =====

        [Test]
        public void Load_ValidJson_CorrectCellTypes()
        {
            // Map with a mix of Explorable (0) and Blocked (1) cells.
            // 2x2 grid, spawn at (0,0):
            //   0 1
            //   0 0
            const string json =
                "{" +
                "\"width\":2,\"height\":2," +
                "\"cells\":[0,1,0,0]," +
                "\"spawnPosition\":{\"x\":0,\"y\":0}," +
                "\"evacuationGroups\":[]" +
                "}";

            MapData result = MapDataLoader.Load(json);

            Assert.AreEqual(CellType.Explorable, result.GetCellType(0, 0));
            Assert.AreEqual(CellType.Blocked, result.GetCellType(1, 0));
            Assert.AreEqual(CellType.Explorable, result.GetCellType(0, 1));
            Assert.AreEqual(CellType.Explorable, result.GetCellType(1, 1));
        }

        // ===== Test 5: Load_ValidJson_CorrectEvacuationGroups =====

        [Test]
        public void Load_ValidJson_CorrectEvacuationGroups()
        {
            MapData result = MapDataLoader.Load(ValidJson3x3);

            Assert.AreEqual(1, result.EvacuationGroups.Count);
            Assert.AreEqual(2, result.EvacuationGroups[0].Count);
            Assert.AreEqual(new Vector2Int(0, 0), result.EvacuationGroups[0][0]);
            Assert.AreEqual(new Vector2Int(2, 0), result.EvacuationGroups[0][1]);
        }

        // ===== Test 6: Load_NullJson_ThrowsArgumentException =====

        [Test]
        public void Load_NullJson_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => MapDataLoader.Load(null));
        }

        // ===== Test 7: Load_EmptyJson_ThrowsArgumentException =====

        [Test]
        public void Load_EmptyJson_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => MapDataLoader.Load(string.Empty));
        }

        // ===== Test 8: Load_InvalidCellValue_ThrowsArgumentException =====

        [Test]
        public void Load_InvalidCellValue_ThrowsArgumentException()
        {
            // Cell value 2 is not a valid CellType (only 0 and 1 are allowed).
            const string json =
                "{" +
                "\"width\":2,\"height\":2," +
                "\"cells\":[0,2,0,0]," +
                "\"spawnPosition\":{\"x\":0,\"y\":0}," +
                "\"evacuationGroups\":[]" +
                "}";

            Assert.Throws<ArgumentException>(() => MapDataLoader.Load(json));
        }
    }
}
