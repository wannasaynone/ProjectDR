using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village;
using ProjectDR.Village.Exploration;
using ProjectDR.Village.Exploration.Combat;
using ProjectDR.Village.Exploration.Core;
using ProjectDR.Village.Exploration.MoveSpeed;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.Exploration.Collection;
using ProjectDR.Village.Exploration.Movement;
using ProjectDR.Village.Exploration.Map;

namespace ProjectDR.Tests.Village.Exploration
{
    /// <summary>
    /// CollectionManager unit tests.
    /// Covers: interaction availability, start/cancel gathering, item pickup,
    /// movement locking, panel close, events, edge cases,
    /// item box transfer (TransferToBox, TransferToBackpack).
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

        private class MockMoveSpeedProvider : IMoveSpeedProvider
        {
            public float GetMoveSpeed() => 5.0f;
        }

        // ===== Test infrastructure =====

        private GridMap _gridMap;
        private PlayerFreeMovement _playerMovement;
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

            _playerMovement = new PlayerFreeMovement(
                _gridMap, new Vector2Int(2, 2), 1.0f, Vector3.zero, new MockMoveSpeedProvider());

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

            _playerMovement = new PlayerFreeMovement(
                _gridMap, new Vector2Int(2, 2), 1.0f, Vector3.zero, new MockMoveSpeedProvider());

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
            CellType[] cells = CreateAllExplorableCells(3, 3);
            MapData md = new MapData(3, 3, cells, new Vector2Int(1, 1), new List<List<Vector2Int>>());
            GridMap gm = new GridMap(md, null);
            gm.InitializeExplored(0, -1);
            PlayerFreeMovement pm = new PlayerFreeMovement(gm, new Vector2Int(1, 1), 1.0f, Vector3.zero, new MockMoveSpeedProvider());

            Assert.Throws<ArgumentNullException>(() =>
                new CollectionManager(null, pm, new BackpackManager(5, 10)));
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
            PlayerFreeMovement pm = new PlayerFreeMovement(gridMap, new Vector2Int(1, 1), 1.0f, Vector3.zero, new MockMoveSpeedProvider());

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
        public void CanInteract_WhileMovementLocked_ReturnsFalse()
        {
            SetUpWithCollectibleAtSpawn();

            _playerMovement.SetMovementLock(true);

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
        public void TryStartGathering_WhileMovementLocked_ReturnsFalse()
        {
            SetUpWithCollectibleAtSpawn();

            _playerMovement.SetMovementLock(true);

            bool result = _sut.TryStartGathering();

            Assert.IsFalse(result);
        }

        // ===== TryStartGathering: movement lock (GDD rule 44) =====

        [Test]
        public void TryStartGathering_PlayerCannotMoveWhileGathering()
        {
            SetUpWithCollectibleAtSpawn();

            _sut.TryStartGathering();

            // Movement should be locked during gathering
            Assert.IsTrue(_playerMovement.IsMovementLocked);

            // Trying to move should have no effect
            Vector2 posBefore = _playerMovement.WorldPosition;
            _playerMovement.Move(Vector2.right, 0.5f);
            Assert.AreEqual(posBefore.x, _playerMovement.WorldPosition.x, 0.001f);
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

            Assert.DoesNotThrow(() => _sut.CancelGathering());
        }

        [Test]
        public void CancelGathering_DuringUnlocking_DoesNothing()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();
            _sut.Update(4.0f); // complete first layer, now in Unlocking

            _sut.CancelGathering();

            Assert.IsTrue(_sut.IsCollecting); // still collecting (in Unlocking)
        }

        // ===== Update =====

        [Test]
        public void Update_CompletesFirstLayerTimer()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();

            _sut.Update(3.0f);

            Assert.AreEqual(GatheringPhase.Unlocking, _sut.ActivePointState.Phase);
        }

        [Test]
        public void Update_CompletesSecondLayerTimer()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();

            _sut.Update(3.0f); // complete first layer
            _sut.Update(2.0f); // complete second layer

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
            _sut.Update(3.0f);
            _sut.Update(2.0f);

            int picked = _sut.TryPickItem(0);

            Assert.AreEqual(2, picked);
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
            _sut.Update(3.0f);

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

        // ===== TransferToBox =====

        [Test]
        public void TransferToBox_ValidItem_ReturnsTrue()
        {
            SetUpWithCollectibleAtSpawn();
            _backpack.AddItem("Potion", 3);
            _sut.TryStartGathering();
            _sut.Update(3.0f); // enter Unlocking

            bool result = _sut.TransferToBox("Potion", 3);

            Assert.IsTrue(result);
        }

        [Test]
        public void TransferToBox_RemovesFromBackpack()
        {
            SetUpWithCollectibleAtSpawn();
            _backpack.AddItem("Potion", 3);
            _sut.TryStartGathering();
            _sut.Update(3.0f);

            _sut.TransferToBox("Potion", 3);

            Assert.AreEqual(0, _backpack.GetItemCount("Potion"));
        }

        [Test]
        public void TransferToBox_StoresInBox()
        {
            SetUpWithCollectibleAtSpawn();
            _backpack.AddItem("Potion", 3);
            _sut.TryStartGathering();
            _sut.Update(3.0f);

            _sut.TransferToBox("Potion", 3);

            // First empty slot should be index 2 (map items at 0,1)
            CollectiblePointState state = _sut.ActivePointState;
            Assert.AreEqual(CollectibleSlotState.PlayerStored, state.GetSlotState(2));
        }

        [Test]
        public void TransferToBox_WhenNotCollecting_ReturnsFalse()
        {
            SetUpWithCollectibleAtSpawn();
            _backpack.AddItem("Potion", 3);

            bool result = _sut.TransferToBox("Potion", 3);

            Assert.IsFalse(result);
        }

        [Test]
        public void TransferToBox_InsufficientBackpackQuantity_ReturnsFalse()
        {
            SetUpWithCollectibleAtSpawn();
            _backpack.AddItem("Potion", 1);
            _sut.TryStartGathering();
            _sut.Update(3.0f);

            bool result = _sut.TransferToBox("Potion", 3); // only 1 in backpack

            Assert.IsFalse(result);
        }

        [Test]
        public void TransferToBox_InsufficientBackpackQuantity_DoesNotRemoveFromBackpack()
        {
            SetUpWithCollectibleAtSpawn();
            _backpack.AddItem("Potion", 1);
            _sut.TryStartGathering();
            _sut.Update(3.0f);

            _sut.TransferToBox("Potion", 3);

            Assert.AreEqual(1, _backpack.GetItemCount("Potion")); // unchanged
        }

        [Test]
        public void TransferToBox_NoEmptySlots_ReturnsFalse()
        {
            // Create with 6 map items (no empty slots)
            List<CollectibleItemEntry> items = new List<CollectibleItemEntry>();
            for (int i = 0; i < 6; i++)
            {
                items.Add(new CollectibleItemEntry($"Item{i}", 1, 0f));
            }
            CollectiblePointData cpData = new CollectiblePointData(2, 2, 0f, items);

            CellType[] cells = CreateAllExplorableCells(5, 5);
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2),
                new List<List<Vector2Int>>(),
                new List<CollectiblePointData> { cpData });

            GridMap gridMap = new GridMap(mapData, new MockMonsterPositionProvider());
            gridMap.InitializeExplored(1, -1);

            PlayerFreeMovement pm = new PlayerFreeMovement(
                gridMap, new Vector2Int(2, 2), 1.0f, Vector3.zero, new MockMoveSpeedProvider());

            BackpackManager bp = new BackpackManager(5, 10);
            bp.AddItem("Potion", 1);
            CollectionManager sut = new CollectionManager(gridMap, pm, bp);

            sut.TryStartGathering(); // zero duration, goes straight to Unlocking

            bool result = sut.TransferToBox("Potion", 1);

            Assert.IsFalse(result);
        }

        [Test]
        public void TransferToBox_NullItemId_ReturnsFalse()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();
            _sut.Update(3.0f);

            bool result = _sut.TransferToBox(null, 1);

            Assert.IsFalse(result);
        }

        [Test]
        public void TransferToBox_EmptyItemId_ReturnsFalse()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();
            _sut.Update(3.0f);

            bool result = _sut.TransferToBox("", 1);

            Assert.IsFalse(result);
        }

        [Test]
        public void TransferToBox_ZeroQuantity_ReturnsFalse()
        {
            SetUpWithCollectibleAtSpawn();
            _backpack.AddItem("Potion", 3);
            _sut.TryStartGathering();
            _sut.Update(3.0f);

            bool result = _sut.TransferToBox("Potion", 0);

            Assert.IsFalse(result);
        }

        [Test]
        public void TransferToBox_NegativeQuantity_ReturnsFalse()
        {
            SetUpWithCollectibleAtSpawn();
            _backpack.AddItem("Potion", 3);
            _sut.TryStartGathering();
            _sut.Update(3.0f);

            bool result = _sut.TransferToBox("Potion", -1);

            Assert.IsFalse(result);
        }

        [Test]
        public void TransferToBox_PublishesItemStoredInBoxEvent()
        {
            SetUpWithCollectibleAtSpawn();
            _backpack.AddItem("Potion", 3);
            _sut.TryStartGathering();
            _sut.Update(3.0f);

            ItemStoredInBoxEvent receivedEvent = null;
            Action<ItemStoredInBoxEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<ItemStoredInBoxEvent>(handler);

            _sut.TransferToBox("Potion", 3);

            EventBus.Unsubscribe<ItemStoredInBoxEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual("Potion", receivedEvent.ItemId);
            Assert.AreEqual(3, receivedEvent.Quantity);
        }

        // ===== TransferToBackpack =====

        [Test]
        public void TransferToBackpack_UnlockedMapItem_AddsToBackpack()
        {
            SetUpWithCollectibleAtSpawn();
            _sut.TryStartGathering();
            _sut.Update(3.0f);
            _sut.Update(2.0f);

            int picked = _sut.TransferToBackpack(0);

            Assert.AreEqual(2, picked);
            Assert.AreEqual(2, _backpack.GetItemCount("Wood"));
        }

        [Test]
        public void TransferToBackpack_PlayerStoredItem_AddsToBackpack()
        {
            SetUpWithCollectibleAtSpawn();
            _backpack.AddItem("Potion", 3);
            _sut.TryStartGathering();
            _sut.Update(3.0f);

            _sut.TransferToBox("Potion", 3);

            // Now pick it back
            int picked = _sut.TransferToBackpack(2);

            Assert.AreEqual(3, picked);
            Assert.AreEqual(3, _backpack.GetItemCount("Potion"));
        }

        [Test]
        public void TransferToBackpack_WhenNotCollecting_ReturnsZero()
        {
            SetUpWithCollectibleAtSpawn();

            int picked = _sut.TransferToBackpack(0);

            Assert.AreEqual(0, picked);
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

        [Test]
        public void FullWorkflow_StoreAndRetrieve()
        {
            SetUpWithCollectibleAtSpawn();
            _backpack.AddItem("Potion", 5);

            // Start gathering and enter Unlocking
            _sut.TryStartGathering();
            _sut.Update(3.0f);

            // Store item from backpack to box
            Assert.IsTrue(_sut.TransferToBox("Potion", 5));
            Assert.AreEqual(0, _backpack.GetItemCount("Potion"));

            // Verify box has the item
            CollectiblePointState state = _sut.ActivePointState;
            Assert.AreEqual(CollectibleSlotState.PlayerStored, state.GetSlotState(2));

            // Retrieve item from box to backpack
            int picked = _sut.TransferToBackpack(2);
            Assert.AreEqual(5, picked);
            Assert.AreEqual(5, _backpack.GetItemCount("Potion"));

            // Box slot should be empty again
            Assert.AreEqual(CollectibleSlotState.Empty, state.GetSlotState(2));
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
            for (int i = 0; i < 5; i++)
            {
                _backpack.AddItem($"Fill{i}", 10);
            }

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

        // ===== PlayerFreeMovement: movement lock =====

        [Test]
        public void PlayerMovement_SetMovementLock_True_PreventsMovement()
        {
            SetUpWithCollectibleAtSpawn();

            _playerMovement.SetMovementLock(true);

            Assert.IsTrue(_playerMovement.IsMovementLocked);

            Vector2 posBefore = _playerMovement.WorldPosition;
            _playerMovement.Move(Vector2.right, 0.5f);
            Assert.AreEqual(posBefore.x, _playerMovement.WorldPosition.x, 0.001f);
        }

        [Test]
        public void PlayerMovement_SetMovementLock_False_AllowsMovement()
        {
            SetUpWithCollectibleAtSpawn();

            _playerMovement.SetMovementLock(true);
            _playerMovement.SetMovementLock(false);

            Assert.IsFalse(_playerMovement.IsMovementLocked);

            Vector2 posBefore = _playerMovement.WorldPosition;
            _playerMovement.Move(Vector2.right, 0.1f);
            Assert.Greater(_playerMovement.WorldPosition.x, posBefore.x);
        }
    }
}
