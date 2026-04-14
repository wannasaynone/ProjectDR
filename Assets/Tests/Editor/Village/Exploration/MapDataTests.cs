using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village.Exploration;

namespace ProjectDR.Tests.Village.Exploration
{
    /// <summary>
    /// MapData unit tests.
    /// Covers: construction validation, cell queries, bounds checking, spawn/evacuation validation.
    /// Pure logic tests with no MonoBehaviour or Unity scene dependency.
    /// </summary>
    [TestFixture]
    public class MapDataTests
    {
        // ===== Helper methods =====

        /// <summary>Creates a flat array of Explorable cells for the given dimensions.</summary>
        private static CellType[] CreateAllExplorableCells(int width, int height)
        {
            CellType[] cells = new CellType[width * height];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = CellType.Explorable;
            }
            return cells;
        }

        /// <summary>Creates a flat array of cells with specific blocked positions.</summary>
        private static CellType[] CreateCellsWithBlocked(int width, int height, params Vector2Int[] blockedPositions)
        {
            CellType[] cells = CreateAllExplorableCells(width, height);
            foreach (Vector2Int pos in blockedPositions)
            {
                cells[pos.y * width + pos.x] = CellType.Blocked;
            }
            return cells;
        }

        // ===== Constructor: valid cases =====

        [Test]
        public void Constructor_ValidParameters_CreatesMapData()
        {
            // Minimal valid map: 1x1, spawn at (0,0), no evacuation groups
            CellType[] cells = new CellType[] { CellType.Explorable };
            List<List<Vector2Int>> evacuationGroups = new List<List<Vector2Int>>();

            MapData sut = new MapData(1, 1, cells, new Vector2Int(0, 0), evacuationGroups);

            Assert.AreEqual(1, sut.Width);
            Assert.AreEqual(1, sut.Height);
            Assert.AreEqual(new Vector2Int(0, 0), sut.SpawnPosition);
        }

        [Test]
        public void Constructor_LargerMap_StoresDimensionsCorrectly()
        {
            CellType[] cells = CreateAllExplorableCells(5, 3);
            List<List<Vector2Int>> evacuationGroups = new List<List<Vector2Int>>();

            MapData sut = new MapData(5, 3, cells, new Vector2Int(0, 0), evacuationGroups);

            Assert.AreEqual(5, sut.Width);
            Assert.AreEqual(3, sut.Height);
        }

        [Test]
        public void Constructor_WithEvacuationGroups_StoresGroupsCorrectly()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            List<List<Vector2Int>> evacuationGroups = new List<List<Vector2Int>>
            {
                new List<Vector2Int> { new Vector2Int(2, 0), new Vector2Int(2, 1) },
                new List<Vector2Int> { new Vector2Int(0, 2) },
            };

            MapData sut = new MapData(3, 3, cells, new Vector2Int(0, 0), evacuationGroups);

            Assert.AreEqual(2, sut.EvacuationGroups.Count);
            Assert.AreEqual(2, sut.EvacuationGroups[0].Count);
            Assert.AreEqual(1, sut.EvacuationGroups[1].Count);
        }

        [Test]
        public void Constructor_EmptyEvacuationGroups_Allowed()
        {
            CellType[] cells = CreateAllExplorableCells(2, 2);
            List<List<Vector2Int>> evacuationGroups = new List<List<Vector2Int>>();

            MapData sut = new MapData(2, 2, cells, new Vector2Int(0, 0), evacuationGroups);

            Assert.AreEqual(0, sut.EvacuationGroups.Count);
        }

        // ===== Constructor: invalid dimensions =====

        [Test]
        public void Constructor_ZeroWidth_ThrowsArgumentOutOfRangeException()
        {
            CellType[] cells = new CellType[0];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MapData(0, 1, cells, Vector2Int.zero, new List<List<Vector2Int>>()));
        }

        [Test]
        public void Constructor_NegativeWidth_ThrowsArgumentOutOfRangeException()
        {
            CellType[] cells = new CellType[0];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MapData(-1, 1, cells, Vector2Int.zero, new List<List<Vector2Int>>()));
        }

        [Test]
        public void Constructor_ZeroHeight_ThrowsArgumentOutOfRangeException()
        {
            CellType[] cells = new CellType[0];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MapData(1, 0, cells, Vector2Int.zero, new List<List<Vector2Int>>()));
        }

        [Test]
        public void Constructor_NegativeHeight_ThrowsArgumentOutOfRangeException()
        {
            CellType[] cells = new CellType[0];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MapData(1, -1, cells, Vector2Int.zero, new List<List<Vector2Int>>()));
        }

        // ===== Constructor: invalid cells array length =====

        [Test]
        public void Constructor_CellsLengthTooSmall_ThrowsArgumentException()
        {
            // 3x3 map expects 9 cells, provide only 4
            CellType[] cells = new CellType[4];
            Assert.Throws<ArgumentException>(() =>
                new MapData(3, 3, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>()));
        }

        [Test]
        public void Constructor_CellsLengthTooLarge_ThrowsArgumentException()
        {
            // 2x2 map expects 4 cells, provide 10
            CellType[] cells = new CellType[10];
            Assert.Throws<ArgumentException>(() =>
                new MapData(2, 2, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>()));
        }

        // ===== Constructor: invalid spawn position =====

        [Test]
        public void Constructor_SpawnOutOfBoundsX_ThrowsArgumentException()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            Assert.Throws<ArgumentException>(() =>
                new MapData(3, 3, cells, new Vector2Int(3, 0), new List<List<Vector2Int>>()));
        }

        [Test]
        public void Constructor_SpawnOutOfBoundsY_ThrowsArgumentException()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            Assert.Throws<ArgumentException>(() =>
                new MapData(3, 3, cells, new Vector2Int(0, 3), new List<List<Vector2Int>>()));
        }

        [Test]
        public void Constructor_SpawnNegativeX_ThrowsArgumentException()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            Assert.Throws<ArgumentException>(() =>
                new MapData(3, 3, cells, new Vector2Int(-1, 0), new List<List<Vector2Int>>()));
        }

        [Test]
        public void Constructor_SpawnNegativeY_ThrowsArgumentException()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            Assert.Throws<ArgumentException>(() =>
                new MapData(3, 3, cells, new Vector2Int(0, -1), new List<List<Vector2Int>>()));
        }

        [Test]
        public void Constructor_SpawnOnBlockedCell_ThrowsArgumentException()
        {
            CellType[] cells = CreateCellsWithBlocked(3, 3, new Vector2Int(1, 1));
            Assert.Throws<ArgumentException>(() =>
                new MapData(3, 3, cells, new Vector2Int(1, 1), new List<List<Vector2Int>>()));
        }

        // ===== Constructor: invalid evacuation points =====

        [Test]
        public void Constructor_EvacuationPointOutOfBounds_ThrowsArgumentException()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            List<List<Vector2Int>> evacuationGroups = new List<List<Vector2Int>>
            {
                new List<Vector2Int> { new Vector2Int(5, 5) },
            };
            Assert.Throws<ArgumentException>(() =>
                new MapData(3, 3, cells, new Vector2Int(0, 0), evacuationGroups));
        }

        [Test]
        public void Constructor_EvacuationPointOnBlockedCell_ThrowsArgumentException()
        {
            CellType[] cells = CreateCellsWithBlocked(3, 3, new Vector2Int(2, 2));
            List<List<Vector2Int>> evacuationGroups = new List<List<Vector2Int>>
            {
                new List<Vector2Int> { new Vector2Int(2, 2) },
            };
            Assert.Throws<ArgumentException>(() =>
                new MapData(3, 3, cells, new Vector2Int(0, 0), evacuationGroups));
        }

        [Test]
        public void Constructor_EvacuationPointNegativeCoords_ThrowsArgumentException()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            List<List<Vector2Int>> evacuationGroups = new List<List<Vector2Int>>
            {
                new List<Vector2Int> { new Vector2Int(-1, 0) },
            };
            Assert.Throws<ArgumentException>(() =>
                new MapData(3, 3, cells, new Vector2Int(0, 0), evacuationGroups));
        }

        // ===== GetCellType =====

        [Test]
        public void GetCellType_ExplorableCell_ReturnsExplorable()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            MapData sut = new MapData(3, 3, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>());

            Assert.AreEqual(CellType.Explorable, sut.GetCellType(1, 1));
        }

        [Test]
        public void GetCellType_BlockedCell_ReturnsBlocked()
        {
            CellType[] cells = CreateCellsWithBlocked(3, 3, new Vector2Int(1, 1));
            MapData sut = new MapData(3, 3, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>());

            Assert.AreEqual(CellType.Blocked, sut.GetCellType(1, 1));
        }

        [Test]
        public void GetCellType_OutOfBoundsPositive_ReturnsBlocked()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            MapData sut = new MapData(3, 3, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>());

            Assert.AreEqual(CellType.Blocked, sut.GetCellType(5, 5));
        }

        [Test]
        public void GetCellType_OutOfBoundsNegative_ReturnsBlocked()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            MapData sut = new MapData(3, 3, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>());

            Assert.AreEqual(CellType.Blocked, sut.GetCellType(-1, 0));
        }

        [Test]
        public void GetCellType_XInBoundsYOutOfBounds_ReturnsBlocked()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            MapData sut = new MapData(3, 3, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>());

            Assert.AreEqual(CellType.Blocked, sut.GetCellType(1, 10));
        }

        // ===== IsInBounds =====

        [Test]
        public void IsInBounds_ValidPosition_ReturnsTrue()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            MapData sut = new MapData(3, 3, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>());

            Assert.IsTrue(sut.IsInBounds(0, 0));
            Assert.IsTrue(sut.IsInBounds(2, 2));
            Assert.IsTrue(sut.IsInBounds(1, 1));
        }

        [Test]
        public void IsInBounds_OutOfBoundsPositive_ReturnsFalse()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            MapData sut = new MapData(3, 3, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>());

            Assert.IsFalse(sut.IsInBounds(3, 0));
            Assert.IsFalse(sut.IsInBounds(0, 3));
        }

        [Test]
        public void IsInBounds_NegativeCoords_ReturnsFalse()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            MapData sut = new MapData(3, 3, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>());

            Assert.IsFalse(sut.IsInBounds(-1, 0));
            Assert.IsFalse(sut.IsInBounds(0, -1));
        }

        // ===== IsWalkable =====

        [Test]
        public void IsWalkable_ExplorableCell_ReturnsTrue()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            MapData sut = new MapData(3, 3, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>());

            Assert.IsTrue(sut.IsWalkable(1, 1));
        }

        [Test]
        public void IsWalkable_BlockedCell_ReturnsFalse()
        {
            CellType[] cells = CreateCellsWithBlocked(3, 3, new Vector2Int(1, 1));
            MapData sut = new MapData(3, 3, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>());

            Assert.IsFalse(sut.IsWalkable(1, 1));
        }

        [Test]
        public void IsWalkable_OutOfBounds_ReturnsFalse()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            MapData sut = new MapData(3, 3, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>());

            Assert.IsFalse(sut.IsWalkable(10, 10));
            Assert.IsFalse(sut.IsWalkable(-1, -1));
        }

        // ===== Corner / edge cell access =====

        [Test]
        public void GetCellType_AllCorners_ReturnsCorrectType()
        {
            // Verify corner cells are accessible on a 4x4 map
            CellType[] cells = CreateAllExplorableCells(4, 4);
            MapData sut = new MapData(4, 4, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>());

            Assert.AreEqual(CellType.Explorable, sut.GetCellType(0, 0));   // top-left
            Assert.AreEqual(CellType.Explorable, sut.GetCellType(3, 0));   // top-right
            Assert.AreEqual(CellType.Explorable, sut.GetCellType(0, 3));   // bottom-left
            Assert.AreEqual(CellType.Explorable, sut.GetCellType(3, 3));   // bottom-right
        }

        [Test]
        public void IsInBounds_BoundaryEdge_ReturnsCorrectResult()
        {
            // Width=3,Height=3 -> valid: 0..2, invalid: 3
            CellType[] cells = CreateAllExplorableCells(3, 3);
            MapData sut = new MapData(3, 3, cells, new Vector2Int(0, 0), new List<List<Vector2Int>>());

            Assert.IsTrue(sut.IsInBounds(2, 2));   // last valid
            Assert.IsFalse(sut.IsInBounds(3, 2));  // one past width
            Assert.IsFalse(sut.IsInBounds(2, 3));  // one past height
        }

        // ===== EvacuationGroups immutability check =====

        [Test]
        public void EvacuationGroups_ReturnsReadOnlyList()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            List<List<Vector2Int>> evacuationGroups = new List<List<Vector2Int>>
            {
                new List<Vector2Int> { new Vector2Int(2, 2) },
            };
            MapData sut = new MapData(3, 3, cells, new Vector2Int(0, 0), evacuationGroups);

            // Should be IReadOnlyList (cannot cast to mutable List and modify)
            Assert.AreEqual(1, sut.EvacuationGroups.Count);
            Assert.AreEqual(new Vector2Int(2, 2), sut.EvacuationGroups[0][0]);
        }
    }
}
