using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village.Exploration;

namespace ProjectDR.Tests.Village.Exploration
{
    /// <summary>
    /// PlayerGridMovement and FixedMoveSpeedCalculator unit tests.
    /// Covers: movement initiation, animation completion, state transitions,
    /// boundary conditions, event publishing, and speed calculator validation.
    /// Pure logic tests with no MonoBehaviour or Unity scene dependency.
    /// </summary>
    [TestFixture]
    public class PlayerGridMovementTests
    {
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

        // ===== Mock: IMoveSpeedCalculator =====

        private class MockMoveSpeedCalculator : IMoveSpeedCalculator
        {
            public float Duration { get; set; }

            public MockMoveSpeedCalculator(float duration)
            {
                Duration = duration;
            }

            public float CalculateMoveDuration() => Duration;
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

        private MockMonsterPositionProvider _monsterProvider;
        private MockMoveSpeedCalculator _speedCalculator;
        private GridMap _gridMap;
        private PlayerGridMovement _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            _monsterProvider = new MockMonsterPositionProvider();
            _speedCalculator = new MockMoveSpeedCalculator(0.5f);

            // Create a 5x5 all-explorable map with spawn at (2,2)
            CellType[] cells = CreateAllExplorableCells(5, 5);
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            _gridMap = new GridMap(mapData, _monsterProvider);
            _gridMap.InitializeExplored(1, -1);

            _sut = new PlayerGridMovement(_gridMap, new Vector2Int(2, 2), _speedCalculator);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== Constructor =====

        [Test]
        public void Constructor_ValidParameters_SetsCurrentPosition()
        {
            Assert.AreEqual(new Vector2Int(2, 2), _sut.CurrentPosition);
        }

        [Test]
        public void Constructor_ValidParameters_IsNotMoving()
        {
            Assert.IsFalse(_sut.IsMoving);
        }

        [Test]
        public void Constructor_StartOnBlockedCell_ThrowsArgumentException()
        {
            CellType[] cells = CreateCellsWithBlocked(5, 5, new Vector2Int(1, 1));
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, _monsterProvider);
            gridMap.InitializeExplored(0, -1);

            Assert.Throws<ArgumentException>(() =>
                new PlayerGridMovement(gridMap, new Vector2Int(1, 1), _speedCalculator));
        }

        [Test]
        public void Constructor_StartOutOfBounds_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new PlayerGridMovement(_gridMap, new Vector2Int(10, 10), _speedCalculator));
        }

        [Test]
        public void Constructor_StartNegativeCoords_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new PlayerGridMovement(_gridMap, new Vector2Int(-1, 0), _speedCalculator));
        }

        // ===== TryMove: success cases =====

        [Test]
        public void TryMove_Up_ReturnsTrue()
        {
            bool result = _sut.TryMove(MoveDirection.Up);

            Assert.IsTrue(result);
        }

        [Test]
        public void TryMove_Down_ReturnsTrue()
        {
            bool result = _sut.TryMove(MoveDirection.Down);

            Assert.IsTrue(result);
        }

        [Test]
        public void TryMove_Left_ReturnsTrue()
        {
            bool result = _sut.TryMove(MoveDirection.Left);

            Assert.IsTrue(result);
        }

        [Test]
        public void TryMove_Right_ReturnsTrue()
        {
            bool result = _sut.TryMove(MoveDirection.Right);

            Assert.IsTrue(result);
        }

        [Test]
        public void TryMove_Success_SetsIsMovingTrue()
        {
            _sut.TryMove(MoveDirection.Right);

            Assert.IsTrue(_sut.IsMoving);
        }

        [Test]
        public void TryMove_Success_SetsTargetPosition()
        {
            // From (2,2) moving Right should target (3,2)
            _sut.TryMove(MoveDirection.Right);

            Assert.AreEqual(new Vector2Int(3, 2), _sut.TargetPosition);
        }

        [Test]
        public void TryMove_Up_TargetPositionDecreasesY()
        {
            // Up decreases Y (assuming Up = -Y in grid coords)
            _sut.TryMove(MoveDirection.Up);

            Assert.AreEqual(new Vector2Int(2, 1), _sut.TargetPosition);
        }

        [Test]
        public void TryMove_Down_TargetPositionIncreasesY()
        {
            _sut.TryMove(MoveDirection.Down);

            Assert.AreEqual(new Vector2Int(2, 3), _sut.TargetPosition);
        }

        [Test]
        public void TryMove_Left_TargetPositionDecreasesX()
        {
            _sut.TryMove(MoveDirection.Left);

            Assert.AreEqual(new Vector2Int(1, 2), _sut.TargetPosition);
        }

        [Test]
        public void TryMove_Success_CurrentPositionDoesNotChangeYet()
        {
            // During movement, CurrentPosition remains the origin until animation completes
            _sut.TryMove(MoveDirection.Right);

            Assert.AreEqual(new Vector2Int(2, 2), _sut.CurrentPosition);
        }

        [Test]
        public void TryMove_Success_SetsCurrentMoveDuration()
        {
            _speedCalculator.Duration = 0.75f;

            _sut.TryMove(MoveDirection.Right);

            Assert.AreEqual(0.75f, _sut.CurrentMoveDuration, 0.001f);
        }

        [Test]
        public void TryMove_Success_PublishesPlayerMoveStartedEvent()
        {
            PlayerMoveStartedEvent receivedEvent = null;
            Action<PlayerMoveStartedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<PlayerMoveStartedEvent>(handler);

            _sut.TryMove(MoveDirection.Right);

            EventBus.Unsubscribe<PlayerMoveStartedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(new Vector2Int(2, 2), receivedEvent.From);
            Assert.AreEqual(new Vector2Int(3, 2), receivedEvent.To);
            Assert.AreEqual(0.5f, receivedEvent.MoveDuration, 0.001f);
        }

        // ===== TryMove: failure cases =====

        [Test]
        public void TryMove_WhileMoving_ReturnsFalse()
        {
            _sut.TryMove(MoveDirection.Right);

            bool result = _sut.TryMove(MoveDirection.Left);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryMove_WhileMoving_DoesNotPublishEvent()
        {
            _sut.TryMove(MoveDirection.Right);

            int eventCount = 0;
            Action<PlayerMoveStartedEvent> handler = (e) => { eventCount++; };
            EventBus.Subscribe<PlayerMoveStartedEvent>(handler);

            _sut.TryMove(MoveDirection.Left);

            EventBus.Unsubscribe<PlayerMoveStartedEvent>(handler);

            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void TryMove_IntoBlockedCell_ReturnsFalse()
        {
            // Create map with blocked cell to the right of spawn
            CellType[] cells = CreateCellsWithBlocked(5, 5, new Vector2Int(3, 2));
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, _monsterProvider);
            gridMap.InitializeExplored(0, -1);
            PlayerGridMovement sut = new PlayerGridMovement(gridMap, new Vector2Int(2, 2), _speedCalculator);

            bool result = sut.TryMove(MoveDirection.Right);

            Assert.IsFalse(result);
            Assert.IsFalse(sut.IsMoving);
        }

        [Test]
        public void TryMove_OutOfBounds_ReturnsFalse()
        {
            // Position player at edge (0,0) and try to move Up (out of bounds)
            PlayerGridMovement sut = new PlayerGridMovement(_gridMap, new Vector2Int(0, 0), _speedCalculator);

            bool result = sut.TryMove(MoveDirection.Up);

            Assert.IsFalse(result);
            Assert.IsFalse(sut.IsMoving);
        }

        [Test]
        public void TryMove_OutOfBoundsLeft_ReturnsFalse()
        {
            PlayerGridMovement sut = new PlayerGridMovement(_gridMap, new Vector2Int(0, 0), _speedCalculator);

            bool result = sut.TryMove(MoveDirection.Left);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryMove_OutOfBoundsDown_ReturnsFalse()
        {
            PlayerGridMovement sut = new PlayerGridMovement(_gridMap, new Vector2Int(4, 4), _speedCalculator);

            bool result = sut.TryMove(MoveDirection.Down);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryMove_OutOfBoundsRight_ReturnsFalse()
        {
            PlayerGridMovement sut = new PlayerGridMovement(_gridMap, new Vector2Int(4, 4), _speedCalculator);

            bool result = sut.TryMove(MoveDirection.Right);

            Assert.IsFalse(result);
        }

        // ===== CompleteMoveAnimation =====

        [Test]
        public void CompleteMoveAnimation_AfterTryMove_UpdatesCurrentPosition()
        {
            _sut.TryMove(MoveDirection.Right);

            _sut.CompleteMoveAnimation();

            Assert.AreEqual(new Vector2Int(3, 2), _sut.CurrentPosition);
        }

        [Test]
        public void CompleteMoveAnimation_AfterTryMove_SetsIsMovingFalse()
        {
            _sut.TryMove(MoveDirection.Right);

            _sut.CompleteMoveAnimation();

            Assert.IsFalse(_sut.IsMoving);
        }

        [Test]
        public void CompleteMoveAnimation_PublishesPlayerMoveCompletedEvent()
        {
            _sut.TryMove(MoveDirection.Right);

            PlayerMoveCompletedEvent receivedEvent = null;
            Action<PlayerMoveCompletedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<PlayerMoveCompletedEvent>(handler);

            _sut.CompleteMoveAnimation();

            EventBus.Unsubscribe<PlayerMoveCompletedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(new Vector2Int(3, 2), receivedEvent.Position);
        }

        [Test]
        public void CompleteMoveAnimation_WhenNotMoving_DoesNothing()
        {
            // Should not throw, should not change state
            Vector2Int positionBefore = _sut.CurrentPosition;

            _sut.CompleteMoveAnimation();

            Assert.AreEqual(positionBefore, _sut.CurrentPosition);
            Assert.IsFalse(_sut.IsMoving);
        }

        [Test]
        public void CompleteMoveAnimation_WhenNotMoving_DoesNotPublishEvent()
        {
            int eventCount = 0;
            Action<PlayerMoveCompletedEvent> handler = (e) => { eventCount++; };
            EventBus.Subscribe<PlayerMoveCompletedEvent>(handler);

            _sut.CompleteMoveAnimation();

            EventBus.Unsubscribe<PlayerMoveCompletedEvent>(handler);

            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void CompleteMoveAnimation_UnexploredTarget_TriggersRevealCell()
        {
            // Move to an unexplored cell; CompleteMoveAnimation should reveal it
            // Set up with radius 0 so only spawn is explored
            CellType[] cells = CreateAllExplorableCells(5, 5);
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, _monsterProvider);
            gridMap.InitializeExplored(0, -1); // only (2,2) explored
            PlayerGridMovement sut = new PlayerGridMovement(gridMap, new Vector2Int(2, 2), _speedCalculator);

            sut.TryMove(MoveDirection.Right); // target (3,2) is unexplored

            CellRevealedEvent revealEvent = null;
            Action<CellRevealedEvent> handler = (e) => { revealEvent = e; };
            EventBus.Subscribe<CellRevealedEvent>(handler);

            sut.CompleteMoveAnimation();

            EventBus.Unsubscribe<CellRevealedEvent>(handler);

            Assert.IsNotNull(revealEvent);
            Assert.AreEqual(3, revealEvent.X);
            Assert.AreEqual(2, revealEvent.Y);
            Assert.IsTrue(gridMap.IsExplored(3, 2));
        }

        [Test]
        public void CompleteMoveAnimation_AlreadyExploredTarget_DoesNotReveal()
        {
            // Move to an already explored cell; no CellRevealedEvent expected
            // radius 1 -> (3,2) is already explored
            _sut.TryMove(MoveDirection.Right);

            int revealCount = 0;
            Action<CellRevealedEvent> handler = (e) => { revealCount++; };
            EventBus.Subscribe<CellRevealedEvent>(handler);

            _sut.CompleteMoveAnimation();

            EventBus.Unsubscribe<CellRevealedEvent>(handler);

            Assert.AreEqual(0, revealCount);
        }

        // ===== Sequential movement =====

        [Test]
        public void TryMove_AfterCompleteMoveAnimation_CanMoveAgain()
        {
            _sut.TryMove(MoveDirection.Right);
            _sut.CompleteMoveAnimation();

            bool result = _sut.TryMove(MoveDirection.Right);

            Assert.IsTrue(result);
            Assert.AreEqual(new Vector2Int(4, 2), _sut.TargetPosition);
        }

        [Test]
        public void TryMove_MultipleSequentialMoves_PositionUpdatesCorrectly()
        {
            // Move right twice
            _sut.TryMove(MoveDirection.Right);
            _sut.CompleteMoveAnimation();
            Assert.AreEqual(new Vector2Int(3, 2), _sut.CurrentPosition);

            _sut.TryMove(MoveDirection.Down);
            _sut.CompleteMoveAnimation();
            Assert.AreEqual(new Vector2Int(3, 3), _sut.CurrentPosition);
        }

        // ===== FixedMoveSpeedCalculator =====

        [Test]
        public void FixedMoveSpeedCalculator_ValidDuration_ReturnsCorrectValue()
        {
            FixedMoveSpeedCalculator calc = new FixedMoveSpeedCalculator(0.3f);

            Assert.AreEqual(0.3f, calc.CalculateMoveDuration(), 0.001f);
        }

        [Test]
        public void FixedMoveSpeedCalculator_ZeroDuration_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FixedMoveSpeedCalculator(0f));
        }

        [Test]
        public void FixedMoveSpeedCalculator_NegativeDuration_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FixedMoveSpeedCalculator(-1f));
        }

        [Test]
        public void FixedMoveSpeedCalculator_VerySmallPositiveDuration_Allowed()
        {
            FixedMoveSpeedCalculator calc = new FixedMoveSpeedCalculator(0.001f);

            Assert.AreEqual(0.001f, calc.CalculateMoveDuration(), 0.0001f);
        }

        [Test]
        public void FixedMoveSpeedCalculator_LargeDuration_Allowed()
        {
            FixedMoveSpeedCalculator calc = new FixedMoveSpeedCalculator(100f);

            Assert.AreEqual(100f, calc.CalculateMoveDuration(), 0.001f);
        }
    }
}
