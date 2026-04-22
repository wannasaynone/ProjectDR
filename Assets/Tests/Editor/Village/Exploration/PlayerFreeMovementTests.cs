using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village.Exploration;
using ProjectDR.Village.Exploration.Combat;
using ProjectDR.Village.Exploration.Core;
using ProjectDR.Village.Exploration.Movement;
using ProjectDR.Village.Exploration.MoveSpeed;
using ProjectDR.Village.Exploration.Map;

namespace ProjectDR.Tests.Village.Exploration
{
    [TestFixture]
    public class PlayerFreeMovementTests
    {
        // ===== Mock: IMoveSpeedProvider =====

        private class MockMoveSpeedProvider : IMoveSpeedProvider
        {
            public float Speed { get; set; }

            public MockMoveSpeedProvider(float speed)
            {
                Speed = speed;
            }

            public float GetMoveSpeed() => Speed;
        }

        // ===== Mock: IMonsterPositionProvider =====

        private class MockMonsterPositionProvider : IMonsterPositionProvider
        {
            private readonly List<Vector2Int> _positions;

            public MockMonsterPositionProvider(params Vector2Int[] positions)
            {
                _positions = new List<Vector2Int>(positions);
            }

            public IReadOnlyList<Vector2Int> GetMonsterPositions() => _positions.AsReadOnly();
        }

        // ===== Helpers =====

        private const float CellSize = 1.0f;
        private static readonly Vector3 MapOrigin = Vector3.zero;

        private static CellType[] CreateAllExplorableCells(int width, int height)
        {
            CellType[] cells = new CellType[width * height];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = CellType.Explorable;
            return cells;
        }

        private static CellType[] CreateCellsWithBlocked(int width, int height, params Vector2Int[] blocked)
        {
            CellType[] cells = CreateAllExplorableCells(width, height);
            foreach (Vector2Int pos in blocked)
                cells[pos.y * width + pos.x] = CellType.Blocked;
            return cells;
        }

        private MockMoveSpeedProvider _speedProvider;
        private MockMonsterPositionProvider _monsterProvider;
        private GridMap _gridMap;
        private PlayerFreeMovement _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            _speedProvider = new MockMoveSpeedProvider(5.0f);
            _monsterProvider = new MockMonsterPositionProvider();

            // 5x5 all explorable, spawn at (2,2)
            CellType[] cells = CreateAllExplorableCells(5, 5);
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            _gridMap = new GridMap(mapData, _monsterProvider);
            _gridMap.InitializeExplored(1, -1);

            _sut = new PlayerFreeMovement(_gridMap, new Vector2Int(2, 2), CellSize, MapOrigin, _speedProvider);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== Constructor =====

        [Test]
        public void Constructor_ValidParameters_SetsCurrentGridCell()
        {
            Assert.AreEqual(new Vector2Int(2, 2), _sut.CurrentGridCell);
        }

        [Test]
        public void Constructor_ValidParameters_SetsWorldPosition()
        {
            // Grid (2,2) -> World (2.0, -2.0) with origin at (0,0), cellSize 1
            Assert.AreEqual(2.0f, _sut.WorldPosition.x, 0.001f);
            Assert.AreEqual(-2.0f, _sut.WorldPosition.y, 0.001f);
        }

        [Test]
        public void Constructor_ValidParameters_IsNotLocked()
        {
            Assert.IsFalse(_sut.IsMovementLocked);
        }

        [Test]
        public void Constructor_NullGridMap_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PlayerFreeMovement(null, new Vector2Int(2, 2), CellSize, MapOrigin, _speedProvider));
        }

        [Test]
        public void Constructor_NullSpeedProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PlayerFreeMovement(_gridMap, new Vector2Int(2, 2), CellSize, MapOrigin, null));
        }

        [Test]
        public void Constructor_ZeroCellSize_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PlayerFreeMovement(_gridMap, new Vector2Int(2, 2), 0f, MapOrigin, _speedProvider));
        }

        [Test]
        public void Constructor_BlockedStartCell_ThrowsArgumentException()
        {
            CellType[] cells = CreateCellsWithBlocked(5, 5, new Vector2Int(1, 1));
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, _monsterProvider);
            gridMap.InitializeExplored(0, -1);

            Assert.Throws<ArgumentException>(() =>
                new PlayerFreeMovement(gridMap, new Vector2Int(1, 1), CellSize, MapOrigin, _speedProvider));
        }

        [Test]
        public void Constructor_OutOfBoundsStartCell_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new PlayerFreeMovement(_gridMap, new Vector2Int(10, 10), CellSize, MapOrigin, _speedProvider));
        }

        // ===== Move: basic movement =====

        [Test]
        public void Move_RightInput_IncreasesWorldPositionX()
        {
            Vector2 posBefore = _sut.WorldPosition;

            _sut.Move(Vector2.right, 0.1f);

            Assert.Greater(_sut.WorldPosition.x, posBefore.x);
        }

        [Test]
        public void Move_UpInput_IncreasesWorldPositionY()
        {
            Vector2 posBefore = _sut.WorldPosition;

            _sut.Move(Vector2.up, 0.1f);

            Assert.Greater(_sut.WorldPosition.y, posBefore.y);
        }

        [Test]
        public void Move_ZeroDirection_NoMovement()
        {
            Vector2 posBefore = _sut.WorldPosition;

            _sut.Move(Vector2.zero, 0.1f);

            Assert.AreEqual(posBefore.x, _sut.WorldPosition.x, 0.001f);
            Assert.AreEqual(posBefore.y, _sut.WorldPosition.y, 0.001f);
        }

        [Test]
        public void Move_ZeroDeltaTime_NoMovement()
        {
            Vector2 posBefore = _sut.WorldPosition;

            _sut.Move(Vector2.right, 0f);

            Assert.AreEqual(posBefore.x, _sut.WorldPosition.x, 0.001f);
        }

        [Test]
        public void Move_NegativeDeltaTime_NoMovement()
        {
            Vector2 posBefore = _sut.WorldPosition;

            _sut.Move(Vector2.right, -1f);

            Assert.AreEqual(posBefore.x, _sut.WorldPosition.x, 0.001f);
        }

        [Test]
        public void Move_LargeDirection_NormalizesToUnitLength()
        {
            // Large direction vector should be capped
            _speedProvider.Speed = 10f;

            _sut.Move(new Vector2(10f, 0f), 0.1f);

            // Expected displacement = 10 * 0.1 = 1.0 (since direction is normalized)
            float expected = 2.0f + 1.0f; // start x + displacement
            Assert.AreEqual(expected, _sut.WorldPosition.x, 0.01f);
        }

        [Test]
        public void Move_SpeedAffectsDisplacement()
        {
            _speedProvider.Speed = 2.0f;
            Vector2 posBefore = _sut.WorldPosition;

            _sut.Move(Vector2.right, 1.0f);

            // displacement = 2.0 * 1.0 = 2.0
            Assert.AreEqual(posBefore.x + 2.0f, _sut.WorldPosition.x, 0.01f);
        }

        // ===== Move: movement lock =====

        [Test]
        public void Move_WhenLocked_NoMovement()
        {
            _sut.SetMovementLock(true);
            Vector2 posBefore = _sut.WorldPosition;

            _sut.Move(Vector2.right, 0.5f);

            Assert.AreEqual(posBefore.x, _sut.WorldPosition.x, 0.001f);
        }

        [Test]
        public void SetMovementLock_True_SetsIsMovementLocked()
        {
            _sut.SetMovementLock(true);

            Assert.IsTrue(_sut.IsMovementLocked);
        }

        [Test]
        public void SetMovementLock_False_ClearsIsMovementLocked()
        {
            _sut.SetMovementLock(true);
            _sut.SetMovementLock(false);

            Assert.IsFalse(_sut.IsMovementLocked);
        }

        // ===== Move: cell change detection =====

        [Test]
        public void Move_EnteringNewCell_PublishesPlayerCellChangedEvent()
        {
            PlayerCellChangedEvent received = null;
            Action<PlayerCellChangedEvent> handler = (e) => { received = e; };
            EventBus.Subscribe<PlayerCellChangedEvent>(handler);

            // Move right enough to cross into cell (3,2)
            // Current world pos = (2.0, -2.0), need to reach x=2.5 to cross
            // speed=5, dt=0.2 => displacement=1.0 => world pos = (3.0, -2.0) = cell (3,2)
            _sut.Move(Vector2.right, 0.2f);

            EventBus.Unsubscribe<PlayerCellChangedEvent>(handler);

            Assert.IsNotNull(received);
            Assert.AreEqual(new Vector2Int(2, 2), received.PreviousCell);
            Assert.AreEqual(new Vector2Int(3, 2), received.NewCell);
        }

        [Test]
        public void Move_EnteringNewCell_UpdatesCurrentGridCell()
        {
            // Move far enough to enter cell (3,2)
            _sut.Move(Vector2.right, 0.2f);

            Assert.AreEqual(new Vector2Int(3, 2), _sut.CurrentGridCell);
        }

        [Test]
        public void Move_StayingInSameCell_DoesNotPublishEvent()
        {
            int eventCount = 0;
            Action<PlayerCellChangedEvent> handler = (e) => { eventCount++; };
            EventBus.Subscribe<PlayerCellChangedEvent>(handler);

            // Small movement, stay in same cell
            // speed=5, dt=0.01 => displacement=0.05
            _sut.Move(Vector2.right, 0.01f);

            EventBus.Unsubscribe<PlayerCellChangedEvent>(handler);

            Assert.AreEqual(0, eventCount);
            Assert.AreEqual(new Vector2Int(2, 2), _sut.CurrentGridCell);
        }

        [Test]
        public void Move_EnteringUnexploredCell_RevealsCell()
        {
            // Use a map with only spawn explored (radius 0)
            CellType[] cells = CreateAllExplorableCells(5, 5);
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, _monsterProvider);
            gridMap.InitializeExplored(0, -1);
            PlayerFreeMovement sut = new PlayerFreeMovement(gridMap, new Vector2Int(2, 2), CellSize, MapOrigin, _speedProvider);

            Assert.IsFalse(gridMap.IsExplored(3, 2));

            // Move right into (3,2)
            sut.Move(Vector2.right, 0.2f);

            Assert.IsTrue(gridMap.IsExplored(3, 2));
        }

        [Test]
        public void Move_EnteringUnexploredCell_PublishesCellRevealedEvent()
        {
            CellType[] cells = CreateAllExplorableCells(5, 5);
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, _monsterProvider);
            gridMap.InitializeExplored(0, -1);
            PlayerFreeMovement sut = new PlayerFreeMovement(gridMap, new Vector2Int(2, 2), CellSize, MapOrigin, _speedProvider);

            CellRevealedEvent revealEvent = null;
            Action<CellRevealedEvent> handler = (e) => { revealEvent = e; };
            EventBus.Subscribe<CellRevealedEvent>(handler);

            sut.Move(Vector2.right, 0.2f);

            EventBus.Unsubscribe<CellRevealedEvent>(handler);

            Assert.IsNotNull(revealEvent);
            Assert.AreEqual(3, revealEvent.X);
            Assert.AreEqual(2, revealEvent.Y);
        }

        // ===== Move: wall collision =====

        [Test]
        public void Move_IntoBlockedCell_DoesNotCrossIntoIt()
        {
            // Block cell (3,2) — right of start
            CellType[] cells = CreateCellsWithBlocked(5, 5, new Vector2Int(3, 2));
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, _monsterProvider);
            gridMap.InitializeExplored(0, -1);
            PlayerFreeMovement sut = new PlayerFreeMovement(gridMap, new Vector2Int(2, 2), CellSize, MapOrigin, _speedProvider);

            sut.Move(Vector2.right, 1.0f);

            // Should not enter cell (3,2)
            Assert.AreEqual(new Vector2Int(2, 2), sut.CurrentGridCell);
        }

        [Test]
        public void Move_DiagonalIntoBlockedCellX_SlidesAlongY()
        {
            // Block cell (3,2) — right. Move diagonally right+up. Should slide upward.
            CellType[] cells = CreateCellsWithBlocked(5, 5, new Vector2Int(3, 2));
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, _monsterProvider);
            gridMap.InitializeExplored(2, -1);
            PlayerFreeMovement sut = new PlayerFreeMovement(gridMap, new Vector2Int(2, 2), CellSize, MapOrigin, _speedProvider);

            Vector2 posBefore = sut.WorldPosition;

            // Move diagonally right+up
            sut.Move(new Vector2(1f, 1f).normalized, 0.1f);

            // X should not increase much (blocked), Y should increase
            Assert.Greater(sut.WorldPosition.y, posBefore.y);
        }

        [Test]
        public void Move_OutOfBoundsLeft_StopsAtEdge()
        {
            // Start at (0,2), move left
            PlayerFreeMovement sut = new PlayerFreeMovement(_gridMap, new Vector2Int(0, 2), CellSize, MapOrigin, _speedProvider);

            sut.Move(Vector2.left, 1.0f);

            // Should not go below grid cell 0
            Assert.GreaterOrEqual(sut.CurrentGridCell.x, 0);
        }

        // ===== ApplyKnockback =====

        [Test]
        public void ApplyKnockback_MovesPlayerInDirection()
        {
            Vector2 posBefore = _sut.WorldPosition;

            _sut.ApplyKnockback(Vector2.right, 0.5f);

            Assert.Greater(_sut.WorldPosition.x, posBefore.x);
        }

        [Test]
        public void ApplyKnockback_ZeroDistance_NoMovement()
        {
            Vector2 posBefore = _sut.WorldPosition;

            _sut.ApplyKnockback(Vector2.right, 0f);

            Assert.AreEqual(posBefore.x, _sut.WorldPosition.x, 0.001f);
        }

        [Test]
        public void ApplyKnockback_ZeroDirection_NoMovement()
        {
            Vector2 posBefore = _sut.WorldPosition;

            _sut.ApplyKnockback(Vector2.zero, 1.0f);

            Assert.AreEqual(posBefore.x, _sut.WorldPosition.x, 0.001f);
        }

        [Test]
        public void ApplyKnockback_IntoWall_StopsBeforeWall()
        {
            // Block cell (3,2) right of start
            CellType[] cells = CreateCellsWithBlocked(5, 5, new Vector2Int(3, 2));
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, _monsterProvider);
            gridMap.InitializeExplored(0, -1);
            PlayerFreeMovement sut = new PlayerFreeMovement(gridMap, new Vector2Int(2, 2), CellSize, MapOrigin, _speedProvider);

            sut.ApplyKnockback(Vector2.right, 5.0f);

            // Should not enter cell (3,2)
            Assert.AreEqual(new Vector2Int(2, 2), sut.CurrentGridCell);
        }

        [Test]
        public void ApplyKnockback_CrossingCells_PublishesPlayerCellChangedEvent()
        {
            PlayerCellChangedEvent received = null;
            Action<PlayerCellChangedEvent> handler = (e) => { received = e; };
            EventBus.Subscribe<PlayerCellChangedEvent>(handler);

            _sut.ApplyKnockback(Vector2.right, 1.5f);

            EventBus.Unsubscribe<PlayerCellChangedEvent>(handler);

            Assert.IsNotNull(received);
        }

        // ===== Coordinate conversion =====

        [Test]
        public void GridToWorld_ReturnsCorrectPosition()
        {
            Vector2 world = _sut.GridToWorld(3, 2);

            Assert.AreEqual(3.0f, world.x, 0.001f);
            Assert.AreEqual(-2.0f, world.y, 0.001f);
        }

        [Test]
        public void WorldToGrid_ReturnsCorrectCell()
        {
            Vector2Int cell = _sut.WorldToGrid(new Vector2(3.2f, -1.8f));

            Assert.AreEqual(new Vector2Int(3, 2), cell);
        }

        [Test]
        public void WorldToGrid_AtCellCenter_ReturnsExactCell()
        {
            Vector2Int cell = _sut.WorldToGrid(new Vector2(2.0f, -2.0f));

            Assert.AreEqual(new Vector2Int(2, 2), cell);
        }
    }
}
