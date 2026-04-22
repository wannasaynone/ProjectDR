using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Exploration;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.Exploration.Core;
using ProjectDR.Village.Exploration.Collection;

namespace ProjectDR.Tests.Village.Exploration
{
    /// <summary>
    /// CollectiblePointState unit tests.
    /// Covers: state machine transitions, two-layer timers, item pickup, edge cases, events,
    /// fixed 6-slot item box, store/remove player items, GetAllSlots.
    /// GDD rules: 8-11, 44, 46.
    /// </summary>
    [TestFixture]
    public class CollectiblePointStateTests
    {
        private CollectiblePointData _defaultData;
        private CollectiblePointState _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            List<CollectibleItemEntry> items = new List<CollectibleItemEntry>
            {
                new CollectibleItemEntry("Wood", 2, 3.0f),
                new CollectibleItemEntry("Stone", 1, 5.0f)
            };
            _defaultData = new CollectiblePointData(1, 2, 4.0f, items);
            _sut = new CollectiblePointState(_defaultData);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== Constructor =====

        [Test]
        public void Constructor_ValidData_InitialPhaseIsIdle()
        {
            Assert.AreEqual(GatheringPhase.Idle, _sut.Phase);
        }

        [Test]
        public void Constructor_ValidData_SlotCountIsMaxSlots()
        {
            Assert.AreEqual(CollectiblePointState.MaxSlots, _sut.SlotCount);
        }

        [Test]
        public void Constructor_ValidData_MapItemCountMatchesItems()
        {
            Assert.AreEqual(2, _sut.MapItemCount);
        }

        [Test]
        public void Constructor_ValidData_MapItemSlotsAreLocked()
        {
            Assert.AreEqual(CollectibleSlotState.Locked, _sut.GetSlotState(0));
            Assert.AreEqual(CollectibleSlotState.Locked, _sut.GetSlotState(1));
        }

        [Test]
        public void Constructor_ValidData_RemainingSlotsAreEmpty()
        {
            Assert.AreEqual(CollectibleSlotState.Empty, _sut.GetSlotState(2));
            Assert.AreEqual(CollectibleSlotState.Empty, _sut.GetSlotState(3));
            Assert.AreEqual(CollectibleSlotState.Empty, _sut.GetSlotState(4));
            Assert.AreEqual(CollectibleSlotState.Empty, _sut.GetSlotState(5));
        }

        [Test]
        public void Constructor_NullData_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CollectiblePointState(null));
        }

        [Test]
        public void Constructor_TooManyItems_ThrowsArgumentException()
        {
            List<CollectibleItemEntry> items = new List<CollectibleItemEntry>();
            for (int i = 0; i < 7; i++)
            {
                items.Add(new CollectibleItemEntry($"Item{i}", 1, 0f));
            }
            CollectiblePointData data = new CollectiblePointData(0, 0, 0f, items);
            Assert.Throws<ArgumentException>(() => new CollectiblePointState(data));
        }

        [Test]
        public void Constructor_MaxItems_AllSlotsLocked()
        {
            List<CollectibleItemEntry> items = new List<CollectibleItemEntry>();
            for (int i = 0; i < 6; i++)
            {
                items.Add(new CollectibleItemEntry($"Item{i}", 1, 1.0f));
            }
            CollectiblePointData data = new CollectiblePointData(0, 0, 1.0f, items);
            CollectiblePointState sut = new CollectiblePointState(data);

            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(CollectibleSlotState.Locked, sut.GetSlotState(i));
            }
        }

        [Test]
        public void Constructor_GatheringProgressIsZero()
        {
            Assert.AreEqual(0f, _sut.GatheringProgress, 0.001f);
        }

        // ===== StartGathering =====

        [Test]
        public void StartGathering_FromIdle_TransitionsToGathering()
        {
            _sut.StartGathering();

            Assert.AreEqual(GatheringPhase.Gathering, _sut.Phase);
        }

        [Test]
        public void StartGathering_FromGathering_ThrowsInvalidOperationException()
        {
            _sut.StartGathering();

            Assert.Throws<InvalidOperationException>(() => _sut.StartGathering());
        }

        [Test]
        public void StartGathering_FromUnlocking_ThrowsInvalidOperationException()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            Assert.Throws<InvalidOperationException>(() => _sut.StartGathering());
        }

        [Test]
        public void StartGathering_ZeroGatherDuration_DirectlyEntersUnlocking()
        {
            List<CollectibleItemEntry> items = new List<CollectibleItemEntry>
            {
                new CollectibleItemEntry("Wood", 1, 2.0f)
            };
            CollectiblePointData data = new CollectiblePointData(0, 0, 0f, items);
            CollectiblePointState sut = new CollectiblePointState(data);

            sut.StartGathering();

            Assert.AreEqual(GatheringPhase.Unlocking, sut.Phase);
        }

        // ===== CancelGathering (GDD rule 44) =====

        [Test]
        public void CancelGathering_FromGathering_TransitionsToIdle()
        {
            _sut.StartGathering();
            _sut.Update(2.0f);

            _sut.CancelGathering();

            Assert.AreEqual(GatheringPhase.Idle, _sut.Phase);
        }

        [Test]
        public void CancelGathering_ClearsAccumulatedTime()
        {
            _sut.StartGathering();
            _sut.Update(2.0f);

            _sut.CancelGathering();

            _sut.StartGathering();
            Assert.AreEqual(0f, _sut.GatheringProgress, 0.001f);
        }

        [Test]
        public void CancelGathering_FromIdle_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _sut.CancelGathering());
        }

        [Test]
        public void CancelGathering_FromUnlocking_ThrowsInvalidOperationException()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            Assert.Throws<InvalidOperationException>(() => _sut.CancelGathering());
        }

        // ===== Update: First layer timer (Gathering) =====

        [Test]
        public void Update_DuringGathering_IncreasesProgress()
        {
            _sut.StartGathering();

            _sut.Update(2.0f);

            Assert.AreEqual(0.5f, _sut.GatheringProgress, 0.01f);
        }

        [Test]
        public void Update_DuringGathering_RemainingTimeDecreases()
        {
            _sut.StartGathering();

            _sut.Update(1.0f);

            Assert.AreEqual(3.0f, _sut.GatheringRemainingTime, 0.01f);
        }

        [Test]
        public void Update_GatheringCompletes_TransitionsToUnlocking()
        {
            _sut.StartGathering();

            _sut.Update(4.0f);

            Assert.AreEqual(GatheringPhase.Unlocking, _sut.Phase);
        }

        [Test]
        public void Update_GatheringCompletes_PublishesGatheringCompletedEvent()
        {
            _sut.StartGathering();

            GatheringCompletedEvent receivedEvent = null;
            Action<GatheringCompletedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<GatheringCompletedEvent>(handler);

            _sut.Update(4.0f);

            EventBus.Unsubscribe<GatheringCompletedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(1, receivedEvent.X);
            Assert.AreEqual(2, receivedEvent.Y);
        }

        [Test]
        public void Update_GatheringCompletes_MapItemSlotsTransitionToUnlocking()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            Assert.AreEqual(CollectibleSlotState.Unlocking, _sut.GetSlotState(0));
            Assert.AreEqual(CollectibleSlotState.Unlocking, _sut.GetSlotState(1));
        }

        [Test]
        public void Update_GatheringCompletes_EmptySlotsRemainEmpty()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            Assert.AreEqual(CollectibleSlotState.Empty, _sut.GetSlotState(2));
            Assert.AreEqual(CollectibleSlotState.Empty, _sut.GetSlotState(3));
            Assert.AreEqual(CollectibleSlotState.Empty, _sut.GetSlotState(4));
            Assert.AreEqual(CollectibleSlotState.Empty, _sut.GetSlotState(5));
        }

        [Test]
        public void Update_ZeroDeltaTime_NoChange()
        {
            _sut.StartGathering();
            float progressBefore = _sut.GatheringProgress;

            _sut.Update(0f);

            Assert.AreEqual(progressBefore, _sut.GatheringProgress, 0.001f);
        }

        [Test]
        public void Update_NegativeDeltaTime_NoChange()
        {
            _sut.StartGathering();
            float progressBefore = _sut.GatheringProgress;

            _sut.Update(-1f);

            Assert.AreEqual(progressBefore, _sut.GatheringProgress, 0.001f);
        }

        [Test]
        public void Update_IdlePhase_NoEffect()
        {
            _sut.Update(5.0f);

            Assert.AreEqual(GatheringPhase.Idle, _sut.Phase);
        }

        // ===== Update: Second layer timer (Unlocking) =====

        [Test]
        public void Update_DuringUnlocking_IncreasesSlotProgress()
        {
            _sut.StartGathering();
            _sut.Update(5.0f); // complete first layer

            _sut.Update(1.5f); // partial second layer

            // Wood: 1.5 / 3.0 = 0.5
            Assert.AreEqual(0.5f, _sut.GetSlotUnlockProgress(0), 0.01f);
            // Stone: 1.5 / 5.0 = 0.3
            Assert.AreEqual(0.3f, _sut.GetSlotUnlockProgress(1), 0.01f);
        }

        [Test]
        public void Update_SlotUnlockCompletes_TransitionsToUnlocked()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            _sut.Update(3.0f); // Wood (3s) completes, Stone (5s) still unlocking

            Assert.AreEqual(CollectibleSlotState.Unlocked, _sut.GetSlotState(0));
            Assert.AreEqual(CollectibleSlotState.Unlocking, _sut.GetSlotState(1));
        }

        [Test]
        public void Update_SlotUnlockCompletes_PublishesItemSlotUnlockedEvent()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            ItemSlotUnlockedEvent receivedEvent = null;
            Action<ItemSlotUnlockedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<ItemSlotUnlockedEvent>(handler);

            _sut.Update(3.0f);

            EventBus.Unsubscribe<ItemSlotUnlockedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(0, receivedEvent.SlotIndex);
            Assert.AreEqual("Wood", receivedEvent.ItemId);
        }

        [Test]
        public void Update_AllSlotsUnlock_AllBecomesUnlocked()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(5.0f);

            Assert.AreEqual(CollectibleSlotState.Unlocked, _sut.GetSlotState(0));
            Assert.AreEqual(CollectibleSlotState.Unlocked, _sut.GetSlotState(1));
        }

        [Test]
        public void Update_ZeroUnlockDuration_ImmediatelyUnlocked()
        {
            List<CollectibleItemEntry> items = new List<CollectibleItemEntry>
            {
                new CollectibleItemEntry("Wood", 1, 0f)
            };
            CollectiblePointData data = new CollectiblePointData(0, 0, 1.0f, items);
            CollectiblePointState sut = new CollectiblePointState(data);

            sut.StartGathering();
            sut.Update(1.0f);

            Assert.AreEqual(CollectibleSlotState.Unlocked, sut.GetSlotState(0));
        }

        // ===== TryPickItem (GDD rule 46) =====

        [Test]
        public void TryPickItem_UnlockedSlot_ReturnsQuantity()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(3.0f);

            BackpackManager backpack = new BackpackManager(5, 10);
            int picked = _sut.TryPickItem(0, backpack);

            Assert.AreEqual(2, picked);
        }

        [Test]
        public void TryPickItem_UnlockedSlot_TransitionsToTaken()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(3.0f);

            BackpackManager backpack = new BackpackManager(5, 10);
            _sut.TryPickItem(0, backpack);

            Assert.AreEqual(CollectibleSlotState.Taken, _sut.GetSlotState(0));
        }

        [Test]
        public void TryPickItem_UnlockedSlot_AddsToBackpack()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(3.0f);

            BackpackManager backpack = new BackpackManager(5, 10);
            _sut.TryPickItem(0, backpack);

            Assert.AreEqual(2, backpack.GetItemCount("Wood"));
        }

        [Test]
        public void TryPickItem_LockedSlot_ReturnsZero()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            BackpackManager backpack = new BackpackManager(5, 10);
            int picked = _sut.TryPickItem(0, backpack);

            Assert.AreEqual(0, picked);
        }

        [Test]
        public void TryPickItem_TakenSlot_ReturnsZero()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(3.0f);

            BackpackManager backpack = new BackpackManager(5, 10);
            _sut.TryPickItem(0, backpack);

            int secondPick = _sut.TryPickItem(0, backpack);
            Assert.AreEqual(0, secondPick);
        }

        [Test]
        public void TryPickItem_BackpackFull_ReturnsZero()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(3.0f);

            BackpackManager backpack = new BackpackManager(1, 1);
            backpack.AddItem("Other", 1);

            int picked = _sut.TryPickItem(0, backpack);
            Assert.AreEqual(0, picked);
        }

        [Test]
        public void TryPickItem_BackpackFull_SlotRemainsUnlocked()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(3.0f);

            BackpackManager backpack = new BackpackManager(1, 1);
            backpack.AddItem("Other", 1);

            _sut.TryPickItem(0, backpack);
            Assert.AreEqual(CollectibleSlotState.Unlocked, _sut.GetSlotState(0));
        }

        [Test]
        public void TryPickItem_PublishesItemPickedUpEvent()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(3.0f);

            BackpackManager backpack = new BackpackManager(5, 10);

            ItemPickedUpEvent receivedEvent = null;
            Action<ItemPickedUpEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<ItemPickedUpEvent>(handler);

            _sut.TryPickItem(0, backpack);

            EventBus.Unsubscribe<ItemPickedUpEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual("Wood", receivedEvent.ItemId);
            Assert.AreEqual(2, receivedEvent.Quantity);
        }

        [Test]
        public void TryPickItem_NotInUnlockingPhase_ThrowsInvalidOperationException()
        {
            BackpackManager backpack = new BackpackManager(5, 10);

            Assert.Throws<InvalidOperationException>(() => _sut.TryPickItem(0, backpack));
        }

        [Test]
        public void TryPickItem_InvalidSlotIndex_ThrowsArgumentOutOfRangeException()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            BackpackManager backpack = new BackpackManager(5, 10);

            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.TryPickItem(-1, backpack));
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.TryPickItem(6, backpack));
        }

        [Test]
        public void TryPickItem_NullBackpack_ThrowsArgumentNullException()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            Assert.Throws<ArgumentNullException>(() => _sut.TryPickItem(0, null));
        }

        [Test]
        public void TryPickItem_EmptySlot_ReturnsZero()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            BackpackManager backpack = new BackpackManager(5, 10);
            int picked = _sut.TryPickItem(2, backpack); // empty slot

            Assert.AreEqual(0, picked);
        }

        // ===== CloseItemPanel =====

        [Test]
        public void CloseItemPanel_FromUnlocking_TransitionsToIdle()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            _sut.CloseItemPanel();

            Assert.AreEqual(GatheringPhase.Idle, _sut.Phase);
        }

        [Test]
        public void CloseItemPanel_ResetsMapItemSlotsToLocked()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(3.0f);

            _sut.CloseItemPanel();

            Assert.AreEqual(CollectibleSlotState.Locked, _sut.GetSlotState(0));
            Assert.AreEqual(CollectibleSlotState.Locked, _sut.GetSlotState(1));
        }

        [Test]
        public void CloseItemPanel_ResetsRemainingSlotsToEmpty()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            // Store an item in empty slot
            _sut.StoreItem(2, "Potion", 1);

            _sut.CloseItemPanel();

            Assert.AreEqual(CollectibleSlotState.Empty, _sut.GetSlotState(2));
            Assert.AreEqual(CollectibleSlotState.Empty, _sut.GetSlotState(3));
        }

        [Test]
        public void CloseItemPanel_FromIdle_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _sut.CloseItemPanel());
        }

        [Test]
        public void CloseItemPanel_FromGathering_ThrowsInvalidOperationException()
        {
            _sut.StartGathering();

            Assert.Throws<InvalidOperationException>(() => _sut.CloseItemPanel());
        }

        // ===== AllItemsTaken =====

        [Test]
        public void AllItemsTaken_NoItemsTaken_ReturnsFalse()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(5.0f);

            Assert.IsFalse(_sut.AllItemsTaken);
        }

        [Test]
        public void AllItemsTaken_SomeItemsTaken_ReturnsFalse()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(5.0f);

            BackpackManager backpack = new BackpackManager(5, 10);
            _sut.TryPickItem(0, backpack);

            Assert.IsFalse(_sut.AllItemsTaken);
        }

        [Test]
        public void AllItemsTaken_AllMapItemsTaken_ReturnsTrue()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(5.0f);

            BackpackManager backpack = new BackpackManager(5, 10);
            _sut.TryPickItem(0, backpack);
            _sut.TryPickItem(1, backpack);

            Assert.IsTrue(_sut.AllItemsTaken);
        }

        [Test]
        public void AllItemsTaken_IgnoresEmptyAndPlayerStoredSlots()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(5.0f);

            BackpackManager backpack = new BackpackManager(5, 10);
            _sut.TryPickItem(0, backpack);
            _sut.TryPickItem(1, backpack);

            // Store item in empty slot - should not affect AllItemsTaken
            _sut.StoreItem(2, "Potion", 1);

            Assert.IsTrue(_sut.AllItemsTaken);
        }

        // ===== GetSlotState / GetSlotUnlockProgress =====

        [Test]
        public void GetSlotState_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GetSlotState(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GetSlotState(6));
        }

        [Test]
        public void GetSlotUnlockProgress_Locked_ReturnsZero()
        {
            Assert.AreEqual(0f, _sut.GetSlotUnlockProgress(0), 0.001f);
        }

        [Test]
        public void GetSlotUnlockProgress_Taken_ReturnsOne()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(3.0f);

            BackpackManager backpack = new BackpackManager(5, 10);
            _sut.TryPickItem(0, backpack);

            Assert.AreEqual(1f, _sut.GetSlotUnlockProgress(0), 0.001f);
        }

        [Test]
        public void GetSlotUnlockProgress_Empty_ReturnsOne()
        {
            Assert.AreEqual(1f, _sut.GetSlotUnlockProgress(2), 0.001f); // empty slot
        }

        [Test]
        public void GetSlotUnlockProgress_PlayerStored_ReturnsOne()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.StoreItem(2, "Potion", 1);

            Assert.AreEqual(1f, _sut.GetSlotUnlockProgress(2), 0.001f);
        }

        // ===== GDD rule 44: Cancel resets time =====

        [Test]
        public void CancelAndRestart_TimeResetsCompletely()
        {
            _sut.StartGathering();
            _sut.Update(3.0f);

            _sut.CancelGathering();
            _sut.StartGathering();

            Assert.AreEqual(4.0f, _sut.GatheringRemainingTime, 0.01f);
        }

        // ===== Reuse: CloseItemPanel then StartGathering again =====

        [Test]
        public void CloseItemPanel_ThenRestart_WorksCorrectly()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.CloseItemPanel();

            _sut.StartGathering();
            Assert.AreEqual(GatheringPhase.Gathering, _sut.Phase);
        }

        // ===== StoreItem =====

        [Test]
        public void StoreItem_EmptySlot_ReturnsTrue()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            bool result = _sut.StoreItem(2, "Potion", 3);

            Assert.IsTrue(result);
        }

        [Test]
        public void StoreItem_EmptySlot_TransitionsToPlayerStored()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            _sut.StoreItem(2, "Potion", 3);

            Assert.AreEqual(CollectibleSlotState.PlayerStored, _sut.GetSlotState(2));
        }

        [Test]
        public void StoreItem_NonEmptySlot_ReturnsFalse()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            // Slot 0 is a map item slot (Unlocking), not Empty
            bool result = _sut.StoreItem(0, "Potion", 1);

            Assert.IsFalse(result);
        }

        [Test]
        public void StoreItem_AlreadyStoredSlot_ReturnsFalse()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            _sut.StoreItem(2, "Potion", 1);

            // Slot 2 is now PlayerStored, not Empty
            bool result = _sut.StoreItem(2, "Herb", 1);

            Assert.IsFalse(result);
        }

        [Test]
        public void StoreItem_NotInUnlockingPhase_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _sut.StoreItem(2, "Potion", 1));
        }

        [Test]
        public void StoreItem_NullItemId_ThrowsArgumentException()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            Assert.Throws<ArgumentException>(() => _sut.StoreItem(2, null, 1));
        }

        [Test]
        public void StoreItem_EmptyItemId_ThrowsArgumentException()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            Assert.Throws<ArgumentException>(() => _sut.StoreItem(2, "", 1));
        }

        [Test]
        public void StoreItem_ZeroQuantity_ThrowsArgumentOutOfRangeException()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.StoreItem(2, "Potion", 0));
        }

        [Test]
        public void StoreItem_NegativeQuantity_ThrowsArgumentOutOfRangeException()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.StoreItem(2, "Potion", -1));
        }

        [Test]
        public void StoreItem_InvalidSlotIndex_ThrowsArgumentOutOfRangeException()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.StoreItem(-1, "Potion", 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.StoreItem(6, "Potion", 1));
        }

        [Test]
        public void StoreItem_PublishesItemStoredInBoxEvent()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            ItemStoredInBoxEvent receivedEvent = null;
            Action<ItemStoredInBoxEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<ItemStoredInBoxEvent>(handler);

            _sut.StoreItem(2, "Potion", 3);

            EventBus.Unsubscribe<ItemStoredInBoxEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(2, receivedEvent.SlotIndex);
            Assert.AreEqual("Potion", receivedEvent.ItemId);
            Assert.AreEqual(3, receivedEvent.Quantity);
        }

        // ===== RemoveStoredItem =====

        [Test]
        public void RemoveStoredItem_PlayerStoredSlot_ReturnsTrue()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.StoreItem(2, "Potion", 3);

            bool result = _sut.RemoveStoredItem(2);

            Assert.IsTrue(result);
        }

        [Test]
        public void RemoveStoredItem_PlayerStoredSlot_TransitionsToEmpty()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.StoreItem(2, "Potion", 3);

            _sut.RemoveStoredItem(2);

            Assert.AreEqual(CollectibleSlotState.Empty, _sut.GetSlotState(2));
        }

        [Test]
        public void RemoveStoredItem_EmptySlot_ReturnsFalse()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            bool result = _sut.RemoveStoredItem(2); // already empty

            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveStoredItem_MapItemSlot_ReturnsFalse()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            bool result = _sut.RemoveStoredItem(0); // map item slot

            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveStoredItem_NotInUnlockingPhase_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _sut.RemoveStoredItem(2));
        }

        [Test]
        public void RemoveStoredItem_InvalidSlotIndex_ThrowsArgumentOutOfRangeException()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.RemoveStoredItem(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.RemoveStoredItem(6));
        }

        [Test]
        public void RemoveStoredItem_PublishesItemRemovedFromBoxEvent()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.StoreItem(2, "Potion", 3);

            ItemRemovedFromBoxEvent receivedEvent = null;
            Action<ItemRemovedFromBoxEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<ItemRemovedFromBoxEvent>(handler);

            _sut.RemoveStoredItem(2);

            EventBus.Unsubscribe<ItemRemovedFromBoxEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(2, receivedEvent.SlotIndex);
            Assert.AreEqual("Potion", receivedEvent.ItemId);
            Assert.AreEqual(3, receivedEvent.Quantity);
        }

        // ===== TryPickItem: PlayerStored items =====

        [Test]
        public void TryPickItem_PlayerStoredSlot_ReturnsQuantity()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.StoreItem(2, "Potion", 3);

            BackpackManager backpack = new BackpackManager(5, 10);
            int picked = _sut.TryPickItem(2, backpack);

            Assert.AreEqual(3, picked);
        }

        [Test]
        public void TryPickItem_PlayerStoredSlot_TransitionsToEmpty()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.StoreItem(2, "Potion", 3);

            BackpackManager backpack = new BackpackManager(5, 10);
            _sut.TryPickItem(2, backpack);

            Assert.AreEqual(CollectibleSlotState.Empty, _sut.GetSlotState(2));
        }

        [Test]
        public void TryPickItem_PlayerStoredSlot_AddsToBackpack()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.StoreItem(2, "Potion", 3);

            BackpackManager backpack = new BackpackManager(5, 10);
            _sut.TryPickItem(2, backpack);

            Assert.AreEqual(3, backpack.GetItemCount("Potion"));
        }

        [Test]
        public void TryPickItem_PlayerStoredSlot_PublishesItemRemovedFromBoxEvent()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.StoreItem(2, "Potion", 3);

            BackpackManager backpack = new BackpackManager(5, 10);

            ItemRemovedFromBoxEvent receivedEvent = null;
            Action<ItemRemovedFromBoxEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<ItemRemovedFromBoxEvent>(handler);

            _sut.TryPickItem(2, backpack);

            EventBus.Unsubscribe<ItemRemovedFromBoxEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(2, receivedEvent.SlotIndex);
            Assert.AreEqual("Potion", receivedEvent.ItemId);
            Assert.AreEqual(3, receivedEvent.Quantity);
        }

        [Test]
        public void TryPickItem_PlayerStoredSlot_BackpackFull_ReturnsZero()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.StoreItem(2, "Potion", 3);

            BackpackManager backpack = new BackpackManager(1, 1);
            backpack.AddItem("Other", 1);

            int picked = _sut.TryPickItem(2, backpack);
            Assert.AreEqual(0, picked);
        }

        [Test]
        public void TryPickItem_PlayerStoredSlot_BackpackFull_SlotRemainsPlayerStored()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.StoreItem(2, "Potion", 3);

            BackpackManager backpack = new BackpackManager(1, 1);
            backpack.AddItem("Other", 1);

            _sut.TryPickItem(2, backpack);
            Assert.AreEqual(CollectibleSlotState.PlayerStored, _sut.GetSlotState(2));
        }

        // ===== GetAllSlots =====

        [Test]
        public void GetAllSlots_ReturnsMaxSlots()
        {
            BoxSlotInfo[] slots = _sut.GetAllSlots();

            Assert.AreEqual(CollectiblePointState.MaxSlots, slots.Length);
        }

        [Test]
        public void GetAllSlots_MapItemSlots_HaveCorrectData()
        {
            BoxSlotInfo[] slots = _sut.GetAllSlots();

            Assert.AreEqual("Wood", slots[0].ItemId);
            Assert.AreEqual(2, slots[0].Quantity);
            Assert.AreEqual(CollectibleSlotState.Locked, slots[0].State);
            Assert.IsFalse(slots[0].IsPlayerStored);

            Assert.AreEqual("Stone", slots[1].ItemId);
            Assert.AreEqual(1, slots[1].Quantity);
        }

        [Test]
        public void GetAllSlots_EmptySlots_HaveNullItemId()
        {
            BoxSlotInfo[] slots = _sut.GetAllSlots();

            for (int i = 2; i < 6; i++)
            {
                Assert.IsNull(slots[i].ItemId);
                Assert.AreEqual(0, slots[i].Quantity);
                Assert.AreEqual(CollectibleSlotState.Empty, slots[i].State);
                Assert.IsFalse(slots[i].IsPlayerStored);
            }
        }

        [Test]
        public void GetAllSlots_PlayerStoredSlot_HasCorrectData()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.StoreItem(3, "Herb", 5);

            BoxSlotInfo[] slots = _sut.GetAllSlots();

            Assert.AreEqual("Herb", slots[3].ItemId);
            Assert.AreEqual(5, slots[3].Quantity);
            Assert.AreEqual(CollectibleSlotState.PlayerStored, slots[3].State);
            Assert.IsTrue(slots[3].IsPlayerStored);
        }

        // ===== FindFirstEmptySlot =====

        [Test]
        public void FindFirstEmptySlot_HasEmptySlots_ReturnsFirstIndex()
        {
            int index = _sut.FindFirstEmptySlot();

            Assert.AreEqual(2, index); // slots 0,1 are map items, 2 is first empty
        }

        [Test]
        public void FindFirstEmptySlot_AllSlotsOccupied_ReturnsNegativeOne()
        {
            List<CollectibleItemEntry> items = new List<CollectibleItemEntry>();
            for (int i = 0; i < 6; i++)
            {
                items.Add(new CollectibleItemEntry($"Item{i}", 1, 1.0f));
            }
            CollectiblePointData data = new CollectiblePointData(0, 0, 1.0f, items);
            CollectiblePointState sut = new CollectiblePointState(data);

            int index = sut.FindFirstEmptySlot();

            Assert.AreEqual(-1, index);
        }

        [Test]
        public void FindFirstEmptySlot_SomeStoredItems_SkipsThem()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            _sut.StoreItem(2, "Potion", 1);

            int index = _sut.FindFirstEmptySlot();

            Assert.AreEqual(3, index); // slot 2 is PlayerStored, 3 is next empty
        }

        // ===== CloseItemPanel: clears stored items =====

        [Test]
        public void CloseItemPanel_ClearsStoredItems()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.StoreItem(2, "Potion", 1);
            _sut.StoreItem(3, "Herb", 2);

            _sut.CloseItemPanel();

            // After close, all non-map slots should be Empty
            BoxSlotInfo[] slots = _sut.GetAllSlots();
            for (int i = 2; i < 6; i++)
            {
                Assert.AreEqual(CollectibleSlotState.Empty, slots[i].State);
                Assert.IsNull(slots[i].ItemId);
            }
        }
    }
}
