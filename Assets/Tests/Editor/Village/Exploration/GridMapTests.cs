using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village.Exploration;

namespace ProjectDR.Tests.Village.Exploration
{
    /// <summary>
    /// GridMap unit tests.
    /// Covers: initialization, exploration state, reveal logic, monster counting,
    /// evacuation points, event publishing, and edge cases.
    /// Pure logic tests with no MonoBehaviour or Unity scene dependency.
    /// </summary>
    [TestFixture]
    public class GridMapTests
    {
        // ===== Mock: IMonsterPositionProvider =====

        /// <summary>Test mock that returns a configurable list of monster positions.</summary>
        private class MockMonsterPositionProvider : IMonsterPositionProvider
        {
            private readonly List<Vector2Int> _positions;

            public MockMonsterPositionProvider(params Vector2Int[] positions)
            {
                _positions = new List<Vector2Int>(positions);
            }

            public IReadOnlyList<Vector2Int> GetMonsterPositions() => _positions.AsReadOnly();

            public void SetPositions(params Vector2Int[] positions)
            {
                _positions.Clear();
                _positions.AddRange(positions);
            }
        }

        // ===== Helper methods =====

        private static CellType[] CreateAllExplorableCells(int width, int height)
        {
            CellType[] cells = new CellType[width * height];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = CellType.Explorable;
            }
            return cells;
        }

        private static CellType[] CreateCellsWithBlocked(int width, int height, params Vector2Int[] blockedPositions)
        {
            CellType[] cells = CreateAllExplorableCells(width, height);
            foreach (Vector2Int pos in blockedPositions)
            {
                cells[pos.y * width + pos.x] = CellType.Blocked;
            }
            return cells;
        }

        /// <summary>Creates a standard 5x5 all-explorable MapData with spawn at center.</summary>
        private static MapData CreateStandard5x5Map(
            Vector2Int? spawn = null,
            List<List<Vector2Int>> evacuationGroups = null,
            Vector2Int[] blocked = null)
        {
            Vector2Int spawnPos = spawn ?? new Vector2Int(2, 2);
            List<List<Vector2Int>> groups = evacuationGroups ?? new List<List<Vector2Int>>();
            CellType[] cells = blocked != null
                ? CreateCellsWithBlocked(5, 5, blocked)
                : CreateAllExplorableCells(5, 5);
            return new MapData(5, 5, cells, spawnPos, groups);
        }

        private MockMonsterPositionProvider _monsterProvider;
        private MapData _defaultMapData;
        private GridMap _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            _monsterProvider = new MockMonsterPositionProvider();
            _defaultMapData = CreateStandard5x5Map();
            _sut = new GridMap(_defaultMapData, _monsterProvider);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== Constructor =====

        [Test]
        public void Constructor_ValidParameters_StoresMapData()
        {
            Assert.AreEqual(_defaultMapData.Width, _sut.Width);
            Assert.AreEqual(_defaultMapData.Height, _sut.Height);
        }

        // ===== InitializeExplored: valid cases =====

        [Test]
        public void InitializeExplored_RadiusZero_OnlySpawnIsExplored()
        {
            // revealRadius=0 means only the spawn cell itself is explored
            _sut.InitializeExplored(0, -1);

            Assert.IsTrue(_sut.IsExplored(2, 2));  // spawn
            Assert.IsFalse(_sut.IsExplored(1, 2)); // adjacent
            Assert.IsFalse(_sut.IsExplored(3, 2)); // adjacent
        }

        [Test]
        public void InitializeExplored_RadiusOne_SpawnAndManhattanNeighborsExplored()
        {
            // revealRadius=1 reveals cells within Manhattan distance 1 from spawn
            _sut.InitializeExplored(1, -1);

            // Spawn itself
            Assert.IsTrue(_sut.IsExplored(2, 2));
            // Manhattan distance 1
            Assert.IsTrue(_sut.IsExplored(1, 2));
            Assert.IsTrue(_sut.IsExplored(3, 2));
            Assert.IsTrue(_sut.IsExplored(2, 1));
            Assert.IsTrue(_sut.IsExplored(2, 3));
            // Manhattan distance 2 should NOT be explored
            Assert.IsFalse(_sut.IsExplored(0, 2));
            Assert.IsFalse(_sut.IsExplored(4, 2));
        }

        [Test]
        public void InitializeExplored_RadiusTwo_RevealsLargerArea()
        {
            _sut.InitializeExplored(2, -1);

            // Spawn
            Assert.IsTrue(_sut.IsExplored(2, 2));
            // Manhattan distance 2
            Assert.IsTrue(_sut.IsExplored(0, 2));
            Assert.IsTrue(_sut.IsExplored(4, 2));
            Assert.IsTrue(_sut.IsExplored(2, 0));
            Assert.IsTrue(_sut.IsExplored(2, 4));
            // Manhattan distance 3 should NOT be explored (if in bounds)
            Assert.IsFalse(_sut.IsExplored(0, 0)); // distance = 4
        }

        [Test]
        public void InitializeExplored_WithEvacuationGroup_RevealsAroundEvacuationPoints()
        {
            // Create map with evacuation group at (4,4)
            List<List<Vector2Int>> groups = new List<List<Vector2Int>>
            {
                new List<Vector2Int> { new Vector2Int(4, 4) },
            };
            MapData mapData = CreateStandard5x5Map(new Vector2Int(0, 0), groups);
            GridMap sut = new GridMap(mapData, _monsterProvider);

            sut.InitializeExplored(1, 0); // group index 0

            // Spawn (0,0) area
            Assert.IsTrue(sut.IsExplored(0, 0));
            Assert.IsTrue(sut.IsExplored(1, 0));
            Assert.IsTrue(sut.IsExplored(0, 1));

            // Evacuation point (4,4) area
            Assert.IsTrue(sut.IsExplored(4, 4));
            Assert.IsTrue(sut.IsExplored(3, 4));
            Assert.IsTrue(sut.IsExplored(4, 3));
        }

        [Test]
        public void InitializeExplored_NegativeOneGroupIndex_WithNonEmptyGroups_ThrowsArgumentOutOfRangeException()
        {
            // When evacuation groups exist, -1 is NOT allowed (only valid when groups list is empty)
            List<List<Vector2Int>> groups = new List<List<Vector2Int>>
            {
                new List<Vector2Int> { new Vector2Int(4, 4) },
            };
            MapData mapData = CreateStandard5x5Map(new Vector2Int(0, 0), groups);
            GridMap sut = new GridMap(mapData, _monsterProvider);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                sut.InitializeExplored(1, -1));
        }

        [Test]
        public void InitializeExplored_BlockedCellsInRadius_NotExplored()
        {
            // Blocked cells within reveal radius should NOT be explored
            MapData mapData = CreateStandard5x5Map(
                new Vector2Int(2, 2),
                blocked: new Vector2Int[] { new Vector2Int(3, 2) });
            GridMap sut = new GridMap(mapData, _monsterProvider);

            sut.InitializeExplored(1, -1);

            Assert.IsTrue(sut.IsExplored(2, 2));  // spawn
            Assert.IsTrue(sut.IsExplored(1, 2));  // walkable neighbor
            Assert.IsFalse(sut.IsExplored(3, 2)); // blocked neighbor
        }

        [Test]
        public void InitializeExplored_DoesNotPublishCellRevealedEvent()
        {
            // InitializeExplored should be silent (no events)
            int eventCount = 0;
            Action<CellRevealedEvent> handler = (e) => { eventCount++; };
            EventBus.Subscribe<CellRevealedEvent>(handler);

            _sut.InitializeExplored(2, -1);

            EventBus.Unsubscribe<CellRevealedEvent>(handler);

            Assert.AreEqual(0, eventCount);
        }

        // ===== InitializeExplored: error cases =====

        [Test]
        public void InitializeExplored_CalledTwice_ThrowsInvalidOperationException()
        {
            _sut.InitializeExplored(1, -1);

            Assert.Throws<InvalidOperationException>(() =>
                _sut.InitializeExplored(1, -1));
        }

        [Test]
        public void InitializeExplored_NegativeRadius_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _sut.InitializeExplored(-1, -1));
        }

        [Test]
        public void InitializeExplored_GroupIndexOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // No evacuation groups but group index 0 requested
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _sut.InitializeExplored(1, 0));
        }

        [Test]
        public void InitializeExplored_GroupIndexTooLarge_ThrowsArgumentOutOfRangeException()
        {
            List<List<Vector2Int>> groups = new List<List<Vector2Int>>
            {
                new List<Vector2Int> { new Vector2Int(4, 4) },
            };
            MapData mapData = CreateStandard5x5Map(new Vector2Int(0, 0), groups);
            GridMap sut = new GridMap(mapData, _monsterProvider);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                sut.InitializeExplored(1, 1)); // only index 0 is valid
        }

        [Test]
        public void InitializeExplored_EmptyGroupsAllowsNegativeOneIndex()
        {
            // When evacuation groups list is empty, -1 is allowed
            Assert.DoesNotThrow(() =>
                _sut.InitializeExplored(1, -1));
        }

        // ===== IsExplored =====

        [Test]
        public void IsExplored_BeforeInitialize_ReturnsFalse()
        {
            // Before InitializeExplored, all cells are unexplored
            Assert.IsFalse(_sut.IsExplored(2, 2));
        }

        [Test]
        public void IsExplored_OutOfBounds_ReturnsFalse()
        {
            _sut.InitializeExplored(1, -1);

            Assert.IsFalse(_sut.IsExplored(-1, 0));
            Assert.IsFalse(_sut.IsExplored(10, 10));
        }

        // ===== RevealCell =====

        [Test]
        public void RevealCell_UnexploredWalkableCell_ReturnsTrue()
        {
            _sut.InitializeExplored(0, -1);

            bool result = _sut.RevealCell(3, 2);

            Assert.IsTrue(result);
        }

        [Test]
        public void RevealCell_Success_CellBecomesExplored()
        {
            _sut.InitializeExplored(0, -1);

            _sut.RevealCell(3, 2);

            Assert.IsTrue(_sut.IsExplored(3, 2));
        }

        [Test]
        public void RevealCell_Success_PublishesCellRevealedEvent()
        {
            _sut.InitializeExplored(0, -1);

            CellRevealedEvent receivedEvent = null;
            Action<CellRevealedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<CellRevealedEvent>(handler);

            _sut.RevealCell(3, 2);

            EventBus.Unsubscribe<CellRevealedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(3, receivedEvent.X);
            Assert.AreEqual(2, receivedEvent.Y);
        }

        [Test]
        public void RevealCell_AlreadyExplored_ReturnsFalse()
        {
            _sut.InitializeExplored(1, -1);

            // (2,2) is spawn, already explored
            bool result = _sut.RevealCell(2, 2);

            Assert.IsFalse(result);
        }

        [Test]
        public void RevealCell_AlreadyExplored_DoesNotPublishEvent()
        {
            _sut.InitializeExplored(1, -1);

            int eventCount = 0;
            Action<CellRevealedEvent> handler = (e) => { eventCount++; };
            EventBus.Subscribe<CellRevealedEvent>(handler);

            _sut.RevealCell(2, 2); // already explored

            EventBus.Unsubscribe<CellRevealedEvent>(handler);

            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void RevealCell_BlockedCell_ReturnsFalse()
        {
            MapData mapData = CreateStandard5x5Map(
                new Vector2Int(2, 2),
                blocked: new Vector2Int[] { new Vector2Int(3, 3) });
            GridMap sut = new GridMap(mapData, _monsterProvider);
            sut.InitializeExplored(0, -1);

            bool result = sut.RevealCell(3, 3);

            Assert.IsFalse(result);
        }

        [Test]
        public void RevealCell_OutOfBounds_ReturnsFalse()
        {
            _sut.InitializeExplored(0, -1);

            Assert.IsFalse(_sut.RevealCell(10, 10));
            Assert.IsFalse(_sut.RevealCell(-1, 0));
        }

        [Test]
        public void RevealCell_OutOfBounds_DoesNotPublishEvent()
        {
            _sut.InitializeExplored(0, -1);

            int eventCount = 0;
            Action<CellRevealedEvent> handler = (e) => { eventCount++; };
            EventBus.Subscribe<CellRevealedEvent>(handler);

            _sut.RevealCell(10, 10);

            EventBus.Unsubscribe<CellRevealedEvent>(handler);

            Assert.AreEqual(0, eventCount);
        }

        // ===== GetAdjacentMonsterCount =====

        [Test]
        public void GetAdjacentMonsterCount_NoMonsters_ReturnsZero()
        {
            _sut.InitializeExplored(0, -1);

            int count = _sut.GetAdjacentMonsterCount(2, 2);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void GetAdjacentMonsterCount_OneMonsterAdjacent_ReturnsOne()
        {
            _monsterProvider.SetPositions(new Vector2Int(3, 2)); // right of spawn
            GridMap sut = new GridMap(_defaultMapData, _monsterProvider);
            sut.InitializeExplored(0, -1);

            int count = sut.GetAdjacentMonsterCount(2, 2);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void GetAdjacentMonsterCount_MonsterOnDiagonal_Counted()
        {
            // 8-neighbor includes diagonals
            _monsterProvider.SetPositions(new Vector2Int(3, 3)); // diagonal from spawn
            GridMap sut = new GridMap(_defaultMapData, _monsterProvider);
            sut.InitializeExplored(0, -1);

            int count = sut.GetAdjacentMonsterCount(2, 2);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void GetAdjacentMonsterCount_AllEightNeighborsHaveMonsters_ReturnsEight()
        {
            _monsterProvider.SetPositions(
                new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
                new Vector2Int(1, 2),                       new Vector2Int(3, 2),
                new Vector2Int(1, 3), new Vector2Int(2, 3), new Vector2Int(3, 3));
            GridMap sut = new GridMap(_defaultMapData, _monsterProvider);
            sut.InitializeExplored(0, -1);

            int count = sut.GetAdjacentMonsterCount(2, 2);

            Assert.AreEqual(8, count);
        }

        [Test]
        public void GetAdjacentMonsterCount_MonsterOnSameCell_NotCounted()
        {
            // Monster on the cell itself, not adjacent
            _monsterProvider.SetPositions(new Vector2Int(2, 2));
            GridMap sut = new GridMap(_defaultMapData, _monsterProvider);
            sut.InitializeExplored(0, -1);

            int count = sut.GetAdjacentMonsterCount(2, 2);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void GetAdjacentMonsterCount_MonsterTwoCellsAway_NotCounted()
        {
            _monsterProvider.SetPositions(new Vector2Int(4, 2)); // distance 2 from spawn
            GridMap sut = new GridMap(_defaultMapData, _monsterProvider);
            sut.InitializeExplored(0, -1);

            int count = sut.GetAdjacentMonsterCount(2, 2);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void GetAdjacentMonsterCount_NullProvider_ReturnsZero()
        {
            // When IMonsterPositionProvider is null, always returns 0
            GridMap sut = new GridMap(_defaultMapData, null);
            sut.InitializeExplored(0, -1);

            int count = sut.GetAdjacentMonsterCount(2, 2);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void GetAdjacentMonsterCount_CornerCell_CountsOnlyValidNeighbors()
        {
            // Corner cell (0,0) has only 3 neighbors: (1,0), (0,1), (1,1)
            _monsterProvider.SetPositions(
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1));
            GridMap sut = new GridMap(_defaultMapData, _monsterProvider);
            sut.InitializeExplored(0, -1);

            int count = sut.GetAdjacentMonsterCount(0, 0);

            Assert.AreEqual(3, count);
        }

        // ===== RecalculateAllMonsterCounts =====

        [Test]
        public void RecalculateAllMonsterCounts_PublishesMonsterCountsChangedEvent()
        {
            _sut.InitializeExplored(0, -1);

            MonsterCountsChangedEvent receivedEvent = null;
            Action<MonsterCountsChangedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<MonsterCountsChangedEvent>(handler);

            _sut.RecalculateAllMonsterCounts();

            EventBus.Unsubscribe<MonsterCountsChangedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
        }

        // ===== GetActiveEvacuationPoints =====

        [Test]
        public void GetActiveEvacuationPoints_NoGroupSelected_ReturnsEmptyList()
        {
            _sut.InitializeExplored(0, -1);

            IReadOnlyList<Vector2Int> points = _sut.GetActiveEvacuationPoints();

            Assert.AreEqual(0, points.Count);
        }

        [Test]
        public void GetActiveEvacuationPoints_GroupSelected_ReturnsGroupPoints()
        {
            List<List<Vector2Int>> groups = new List<List<Vector2Int>>
            {
                new List<Vector2Int> { new Vector2Int(4, 0), new Vector2Int(4, 1) },
            };
            MapData mapData = CreateStandard5x5Map(new Vector2Int(0, 0), groups);
            GridMap sut = new GridMap(mapData, _monsterProvider);
            sut.InitializeExplored(0, 0);

            IReadOnlyList<Vector2Int> points = sut.GetActiveEvacuationPoints();

            Assert.AreEqual(2, points.Count);
            bool hasPoint0 = false;
            bool hasPoint1 = false;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] == new Vector2Int(4, 0)) hasPoint0 = true;
                if (points[i] == new Vector2Int(4, 1)) hasPoint1 = true;
            }
            Assert.IsTrue(hasPoint0);
            Assert.IsTrue(hasPoint1);
        }

        // ===== IsEvacuationPoint =====

        [Test]
        public void IsEvacuationPoint_ActiveEvacuationPoint_ReturnsTrue()
        {
            List<List<Vector2Int>> groups = new List<List<Vector2Int>>
            {
                new List<Vector2Int> { new Vector2Int(4, 0) },
            };
            MapData mapData = CreateStandard5x5Map(new Vector2Int(0, 0), groups);
            GridMap sut = new GridMap(mapData, _monsterProvider);
            sut.InitializeExplored(0, 0);

            Assert.IsTrue(sut.IsEvacuationPoint(4, 0));
        }

        [Test]
        public void IsEvacuationPoint_NonEvacuationPoint_ReturnsFalse()
        {
            List<List<Vector2Int>> groups = new List<List<Vector2Int>>
            {
                new List<Vector2Int> { new Vector2Int(4, 0) },
            };
            MapData mapData = CreateStandard5x5Map(new Vector2Int(0, 0), groups);
            GridMap sut = new GridMap(mapData, _monsterProvider);
            sut.InitializeExplored(0, 0);

            Assert.IsFalse(sut.IsEvacuationPoint(2, 2));
        }

        [Test]
        public void IsEvacuationPoint_NoGroupSelected_ReturnsFalse()
        {
            _sut.InitializeExplored(0, -1);

            Assert.IsFalse(_sut.IsEvacuationPoint(0, 0));
        }

        // ===== Multiple evacuation groups =====

        [Test]
        public void InitializeExplored_MultipleGroups_OnlySelectedGroupActive()
        {
            List<List<Vector2Int>> groups = new List<List<Vector2Int>>
            {
                new List<Vector2Int> { new Vector2Int(4, 0) }, // group 0
                new List<Vector2Int> { new Vector2Int(0, 4) }, // group 1
            };
            MapData mapData = CreateStandard5x5Map(new Vector2Int(2, 2), groups);
            GridMap sut = new GridMap(mapData, _monsterProvider);

            sut.InitializeExplored(1, 1); // select group 1

            // Group 1 point should be evacuation
            Assert.IsTrue(sut.IsEvacuationPoint(0, 4));
            // Group 0 point should NOT be evacuation
            Assert.IsFalse(sut.IsEvacuationPoint(4, 0));
        }
    }
}
