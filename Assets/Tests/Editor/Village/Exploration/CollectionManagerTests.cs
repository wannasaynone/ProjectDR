using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village;
using ProjectDR.Village.Exploration;

namespace ProjectDR.Tests.Village.Exploration
{
    /// <summary>
    /// CollectionManager unit tests.
    /// Covers: interaction availability, start/cancel gathering, item pickup,
    /// movement locking, panel close, events, edge cases.
    /// GDD rules: 8-13, 44-46.
    /// </summary>
    [TestFixture]
    public class CollectionManagerTests
    {
        // ===== Mock =====

        private class MockMonsterPositionProvider : IMonsterPositionProvider
        {
            public IReadOnlyList<Vector2Int> GetMonsterPositions()
            {
                return new List<Vector2Int>().AsReadOnly();
            }
        }

        private class MockMoveSpeedCalculator : IMoveSpeedCalculator
        {
            public float CalculateMoveDuration() => 0.5f;
        }

        // ===== Test infrastructure =====

        private GridMap _gridMap;
        private PlayerGridMovement _playerMovement;
        private BackpackManager _backpack;
        private CollectionManager _sut;
        private MapData _mapData;

        private static CellType[] CreateAllExplorableCells(int width, int height)
        {
            CellType[] cells = new CellType[width * height];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = CellType.Explorable;
            }
            return cells;
        }

        /// <summary>
        /// Creates a 5x5 map with a collectible point at (2,2) (spawn position).
        /// </summary>
        private void SetUpWithCollectibleAtSpawn(
            float gatherDuration = 3.0f,
            float unlockDuration = 2.0f)
        {
            List<CollectibleItemEntry> items = new List<CollectibleItemEntry>
            {
                new CollectibleItemEntry("Wood", 2, unlockDuration),
                new CollectibleItemEntry("Stone", 1, unlockDuration)
            };
            CollectiblePointData cpData = new CollectiblePointData(2, 2, gatherDuration, items);

            CellType[] cells = CreateAllExplorableCells(5, 5);
            _mapData = new MapData(5, 5, cells, new Vector2Int(2, 2),
                new List<List<Vector2Int>>(),
                new List<CollectiblePointData> { cpData });

            _gridMap = new GridMap(_mapData, new MockMonsterPositionProvider());
            _gridMap.InitializeExplored(1, -1);

            _playerMovement = new PlayerGridMovement(
                _gridMap, new Vector2Int(2, 2), new MockMoveSpeedCalculator());

            _backpack = new BackpackManager(5, 10);
            _sut = new CollectionManager(_gridMap, _playerMovement, _backpack);
        }

        /// <summary>
        /// Creates a 5x5 map without collectible points.
        /// </summary>
        private void SetUpWithoutCollectible()
        {
            CellType[] cells = CreateAllExplorableCells(5, 5);
            _mapData = new MapData(5, 5, cells, new Vector2Int(2, 2),
                new List<List<Vector2Int>>());

            _gridMap = new GridMap(_mapData, new MockMonsterPositionProvider());
            _gridMap.InitializeExplored(1, -1);

            _playerMovement = new PlayerGridMovement(
                _gridMap, new Vector2Int(2, 2), new MockMoveSpeedCalculator());

            _backpack = new BackpackManager(5, 10);
            _sut = new CollectionManager(_gridMap, _playerMovement, _backpack);
        }

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== Constructor =====

        [Test]
        public void Constructor_NullGridMap_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CollectionManager(null,
                    new PlayerGridMovement(
                        new GridMap(
                            new MapData(3, 3, CreateAllExplorableCells(3, 3),
                                new Vector2Int(1, 1), new List<List<Vector2Int>>()),
                            null),
                        new Vector2Int(1, 1), new MockMoveSpeedCalculator()),
                    new BackpackManager(5, 10)));
        }

        [Test]
        public void Constructor_NullPlayerMovement_ThrowsArgumentNullException()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            MapData mapData = new MapData(3, 3, cells, new Vector2Int(1, 1), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, null);

            Assert.Throws<ArgumentNullException>(() =>
                new CollectionManager(gridMap, null, new BackpackManager(5, 10)));
        }

        [Test]
        public void Constructor_NullBackpack_ThrowsArgumentNullException()
        {
            CellType[] cells = CreateAllExplorableCells(3, 3);
            MapData mapData = new MapData(3, 3, cells, new Vector2Int(1, 1), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, null);
            gridMap.InitializeExplored(0, -1);
            PlayerGridMovement pm = new PlayerGridMovement(gridMap, new Vector2Int(1, 1), new MockMoveSpeedCalculator());

            Assert.Throws<ArgumentNullException>(() =>
                new CollectionManager(gridMap, pm, null));
        }

        // ===== CanInteract =====

        [Test]
        public void CanInteract_PlayerOnCollectiblePoint_ReturnsTrue()
        {
            SetUpWithCollectibleAtSpawn();

            Assert.IsTrue(_sut.CanInteract());
        }

        [Test]
        public void CanInteract_PlayerNotOnCollectiblePoint_ReturnsFalse()
        {
            SetUpWithoutCollectible();

            Assert.IsFalse(_sut.CanInteract());
        }

        [Test]
        public void CanInteract_WhileCollecting_ReturnsFalse()
        {
            SetUpWithCollectibleAtSpawn();

            _sut.TryStartGathering();

            Assert.IsFalse(_sut.CanInteract());
        }

        [Test]
        public void CanInteract_WhileMoving_ReturnsFalse()
        {
            SetUpWithCollectibleAtSpawn();

            _playerMovement.TryMove(MoveDirection.Right);

            Assert.IsFalse(_sut.CanInteract());
        }

        // ===== TryStartGathering =====

        [Test]
        public void TryStartGathering_OnCollectiblePoint_ReturnsTrue()
        {
            SetUpWithCollectibleAtSpawn();

            bool result = _sut.TryStartGathering();

            Assert.IsTrue(result);
        }

        [Test]
        public void TryStartGathering_Success_SetsIsCollectingTrue()
        {
            SetUpWithCollectibleAtSpawn();

            _sut.TryStartGathering();

            Assert.IsTrue(_sut.IsCollecting);
        }

        [Test]
        public void TryStartGathering_Success_LocksPlayerMovement()
        {
            SetUpWithCollectibleAtSpawn();

            _sut.TryStartGathering();

            Assert.IsTrue(_playerMovement.IsMovementLocked);
        }

        [Test]
        public void TryStartGathering_Success_PublishesCollectionStartedEvent()
        {
            SetUpWithCollectibleAtSpawn();

            CollectionStartedEvent receivedEvent = null;
            Action<CollectionStartedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<CollectionStartedEvent>(handler);

            _sut.TryStartGathering();

            EventBus.Unsubscribe<CollectionStartedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(2, receivedEvent.X);
            Assert.AreEqual(2, receivedEvent.Y);
        }

        [Test]
        public void TryStartGathering_NoCollectiblePoint_ReturnsFalse()
        {
            SetUpWithoutCollectible();

            bool result = _sut.TryStartGathering();

            Assert.IsFalse(result);
        }

        [Test]
        public void TryStartGathering_AlreadyCollecting_ReturnsFalse()
        {
            SetUpWithCollectibleAtSpawn();

            _sut.TryStartGathering();
            bool result = _sut.TryStartGathering();

            Assert.IsFalse(result);
        }

        [Test]
        public void TryStartGathering_WhileMoving_ReturnsFalse()
        {
            SetUpWithCollectibleAtSpawn();

            _playerMovement.TryMove(MoveDirection.Right);

            bool result = _sut.TryStartGathering();

            Assert.IsFalse(result);
        }

        // ===== TryStartGathering: movement lock (GDD rule 44) =====

        [Test]
        public void TryStartGathering_PlayerCannotMoveWhileGathering()
        {
            SetUpWithCollectibleAtSpawn();

            _sut.TryStartGathering();

            bool moveResult = _playerMovement.TryMove(MoveDirection.Right);

            Assert.IsFalse(moveResult);
        }

        // ===== CancelGathering (GDD rule 44) =====

        [Test]
        public void CancelGathering_DuringGathering_UnlocksMovement()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();

            _sut.CancelGathering();

            Assert.IsFalse(_playerMovement.IsMovementLocked);
        }

        [Test]
        public void CancelGathering_DuringGathering_PublishesEvent()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();

            CollectionCancelledEvent receivedEvent = null;
            Action<CollectionCancelledEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<CollectionCancelledEvent>(handler);

            _sut.CancelGathering();

            EventBus.Unsubscribe<CollectionCancelledEvent>(handler);

            Assert.IsNotNull(receivedEvent);
        }

        [Test]
        public void CancelGathering_SetsIsCollectingFalse()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();

            _sut.CancelGathering();

            Assert.IsFalse(_sut.IsCollecting);
        }

        [Test]
        public void CancelGathering_WhenNotGathering_DoesNothing()
        {
            SetUpWithCollectibleAtSpawn();

            // Should not throw when not gathering
            Assert.DoesNotThrow(() => _sut.CancelGathering());
        }

        [Test]
        public void CancelGathering_DuringUnlocking_DoesNothing()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();
            _sut.Update(4.0f); // complete first layer, now in Unlocking

            // CancelGathering should do nothing in Unlocking phase
            _sut.CancelGathering();

            Assert.IsTrue(_sut.IsCollecting); // still collecting (in Unlocking)
        }

        // ===== Update =====

        [Test]
        public void Update_CompletesFirstLayerTimer()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();

            _sut.Update(3.0f); // gatherDuration = 3.0

            Assert.AreEqual(GatheringPhase.Unlocking, _sut.ActivePointState.Phase);
        }

        [Test]
        public void Update_CompletesSecondLayerTimer()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();

            _sut.Update(3.0f); // complete first layer
            _sut.Update(2.0f); // complete second layer (both items have 2.0 unlock)

            Assert.AreEqual(CollectibleSlotState.Unlocked,
                _sut.ActivePointState.GetSlotState(0));
            Assert.AreEqual(CollectibleSlotState.Unlocked,
                _sut.ActivePointState.GetSlotState(1));
        }

        [Test]
        public void Update_WhenNotCollecting_DoesNothing()
        {
            SetUpWithCollectibleAtSpawn();

            Assert.DoesNotThrow(() => _sut.Update(1.0f));
        }

        // ===== TryPickItem (GDD rule 46) =====

        [Test]
        public void TryPickItem_UnlockedItem_AddsToBackpack()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();
            _sut.Update(3.0f); // complete first layer
            _sut.Update(2.0f); // complete second layer

            int picked = _sut.TryPickItem(0);

            Assert.AreEqual(2, picked); // Wood quantity = 2
            Assert.AreEqual(2, _backpack.GetItemCount("Wood"));
        }

        [Test]
        public void TryPickItem_WhenNotCollecting_ReturnsZero()
        {
            SetUpWithCollectibleAtSpawn();

            int picked = _sut.TryPickItem(0);

            Assert.AreEqual(0, picked);
        }

        // ===== CloseItemPanel =====

        [Test]
        public void CloseItemPanel_FromUnlocking_UnlocksMovement()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();
            _sut.Update(3.0f); // complete first layer

            _sut.CloseItemPanel();

            Assert.IsFalse(_playerMovement.IsMovementLocked);
        }

        [Test]
        public void CloseItemPanel_SetsIsCollectingFalse()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();
            _sut.Update(3.0f);

            _sut.CloseItemPanel();

            Assert.IsFalse(_sut.IsCollecting);
        }

        [Test]
        public void CloseItemPanel_PublishesCollectionPanelClosedEvent()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();
            _sut.Update(3.0f);

            CollectionPanelClosedEvent receivedEvent = null;
            Action<CollectionPanelClosedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<CollectionPanelClosedEvent>(handler);

            _sut.CloseItemPanel();

            EventBus.Unsubscribe<CollectionPanelClosedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
        }

        [Test]
        public void CloseItemPanel_WhenNotCollecting_DoesNothing()
        {
            SetUpWithCollectibleAtSpawn();

            Assert.DoesNotThrow(() => _sut.CloseItemPanel());
        }

        // ===== Full workflow =====

        [Test]
        public void FullWorkflow_GatherUnlockPickClose()
        {
            SetUpWithCollectibleAtSpawn();

            // 1. Start gathering
            Assert.IsTrue(_sut.TryStartGathering());
            Assert.IsTrue(_playerMovement.IsMovementLocked);

            // 2. Complete first layer timer
            _sut.Update(3.0f);
            Assert.AreEqual(GatheringPhase.Unlocking, _sut.ActivePointState.Phase);

            // 3. Complete second layer timer
            _sut.Update(2.0f);
            Assert.AreEqual(CollectibleSlotState.Unlocked, _sut.ActivePointState.GetSlotState(0));

            // 4. Pick items
            Assert.AreEqual(2, _sut.TryPickItem(0));
            Assert.AreEqual(1, _sut.TryPickItem(1));

            // 5. Close panel
            _sut.CloseItemPanel();
            Assert.IsFalse(_sut.IsCollecting);
            Assert.IsFalse(_playerMovement.IsMovementLocked);

            // 6. Verify backpack
            Assert.AreEqual(2, _backpack.GetItemCount("Wood"));
            Assert.AreEqual(1, _backpack.GetItemCount("Stone"));
        }

        [Test]
        public void FullWorkflow_CancelAndRestart()
        {
            SetUpWithCollectibleAtSpawn();

            // Start gathering
            _sut.TryStartGathering();
            _sut.Update(1.0f); // partial

            // Cancel (GDD rule 44: time resets)
            _sut.CancelGathering();
            Assert.IsFalse(_playerMovement.IsMovementLocked);

            // Restart
            Assert.IsTrue(_sut.TryStartGathering());
            Assert.IsTrue(_playerMovement.IsMovementLocked);

            // Should need full 3.0 seconds again
            _sut.Update(2.9f);
            Assert.AreEqual(GatheringPhase.Gathering, _sut.ActivePointState.Phase);
            _sut.Update(0.2f);
            Assert.AreEqual(GatheringPhase.Unlocking, _sut.ActivePointState.Phase);
        }

        // ===== GDD rule 12, 13: Backpack capacity =====

        [Test]
        public void TryPickItem_BackpackFull_ReturnsZero()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();
            _sut.Update(3.0f);
            _sut.Update(2.0f);

            // Fill backpack to max
            BackpackManager fullBackpack = new BackpackManager(1, 1);
            fullBackpack.AddItem("Other", 1);

            // Use a new CollectionManager with the full backpack
            CollectionManager sut2 = new CollectionManager(_gridMap, _playerMovement, fullBackpack);

            // Need to start gathering fresh since we need a new manager
            // Instead, verify via the existing _sut but with modified backpack
            // Let's fill the shared backpack
            for (int i = 0; i < 5; i++)
            {
                _backpack.AddItem($"Fill{i}", 10);
            }

            // Backpack should be full now (5 slots x 10 max stack)
            Assert.IsTrue(_backpack.IsFull);

            int picked = _sut.TryPickItem(0);
            Assert.AreEqual(0, picked);
        }

        // ===== MapData: collectible point at blocked position =====

        [Test]
        public void MapData_CollectibleOnBlockedCell_ThrowsArgumentException()
        {
            CellType[] cells = CreateAllExplorableCells(5, 5);
            cells[2 * 5 + 3] = CellType.Blocked; // (3,2) is blocked

            List<CollectibleItemEntry> items = new List<CollectibleItemEntry>
            {
                new CollectibleItemEntry("Wood", 1, 0f)
            };
            CollectiblePointData cpData = new CollectiblePointData(3, 2, 0f, items);

            Assert.Throws<ArgumentException>(() =>
                new MapData(5, 5, cells, new Vector2Int(2, 2),
                    new List<List<Vector2Int>>(),
                    new List<CollectiblePointData> { cpData }));
        }

        // ===== MapData: query collectible points =====

        [Test]
        public void MapData_GetCollectiblePointAt_ReturnsCorrectPoint()
        {
            SetUpWithCollectibleAtSpawn();

            CollectiblePointData point = _mapData.GetCollectiblePointAt(2, 2);

            Assert.IsNotNull(point);
            Assert.AreEqual(2, point.X);
            Assert.AreEqual(2, point.Y);
        }

        [Test]
        public void MapData_GetCollectiblePointAt_NoPoint_ReturnsNull()
        {
            SetUpWithCollectibleAtSpawn();

            CollectiblePointData point = _mapData.GetCollectiblePointAt(0, 0);

            Assert.IsNull(point);
        }

        [Test]
        public void MapData_HasCollectiblePoint_True()
        {
            SetUpWithCollectibleAtSpawn();

            Assert.IsTrue(_mapData.HasCollectiblePoint(2, 2));
        }

        [Test]
        public void MapData_HasCollectiblePoint_False()
        {
            SetUpWithCollectibleAtSpawn();

            Assert.IsFalse(_mapData.HasCollectiblePoint(0, 0));
        }

        // ===== GridMap: delegates to MapData =====

        [Test]
        public void GridMap_HasCollectiblePoint_DelegatesToMapData()
        {
            SetUpWithCollectibleAtSpawn();

            Assert.IsTrue(_gridMap.HasCollectiblePoint(2, 2));
            Assert.IsFalse(_gridMap.HasCollectiblePoint(0, 0));
        }

        // ===== PlayerGridMovement: movement lock =====

        [Test]
        public void PlayerMovement_SetMovementLock_True_PreventsMovement()
        {
            SetUpWithCollectibleAtSpawn();

            _playerMovement.SetMovementLock(true);

            Assert.IsTrue(_playerMovement.IsMovementLocked);
            Assert.IsFalse(_playerMovement.TryMove(MoveDirection.Right));
        }

        [Test]
        public void PlayerMovement_SetMovementLock_False_AllowsMovement()
        {
            SetUpWithCollectibleAtSpawn();

            _playerMovement.SetMovementLock(true);
            _playerMovement.SetMovementLock(false);

            Assert.IsFalse(_playerMovement.IsMovementLocked);
            Assert.IsTrue(_playerMovement.TryMove(MoveDirection.Right));
        }
    }
}
