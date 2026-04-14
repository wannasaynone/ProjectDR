using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Exploration;

namespace ProjectDR.Tests.Village.Exploration
{
    /// <summary>
    /// CollectiblePointState unit tests.
    /// Covers: state machine transitions, two-layer timers, item pickup, edge cases, events.
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
        public void Constructor_ValidData_SlotCountMatchesItems()
        {
            Assert.AreEqual(2, _sut.SlotCount);
        }

        [Test]
        public void Constructor_ValidData_AllSlotsAreLocked()
        {
            Assert.AreEqual(CollectibleSlotState.Locked, _sut.GetSlotState(0));
            Assert.AreEqual(CollectibleSlotState.Locked, _sut.GetSlotState(1));
        }

        [Test]
        public void Constructor_NullData_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CollectiblePointState(null));
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
            // Get to Unlocking: start + complete gathering
            _sut.StartGathering();
            _sut.Update(5.0f); // exceeds gatherDuration

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
            _sut.Update(2.0f); // partial progress

            _sut.CancelGathering();

            Assert.AreEqual(GatheringPhase.Idle, _sut.Phase);
        }

        [Test]
        public void CancelGathering_ClearsAccumulatedTime()
        {
            _sut.StartGathering();
            _sut.Update(2.0f); // partial progress

            _sut.CancelGathering();

            // Restart and check progress is 0
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
            _sut.Update(5.0f); // complete gathering

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

            _sut.Update(4.0f); // exactly the gather duration

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
        public void Update_GatheringCompletes_SlotsTransitionToUnlocking()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            Assert.AreEqual(CollectibleSlotState.Unlocking, _sut.GetSlotState(0));
            Assert.AreEqual(CollectibleSlotState.Unlocking, _sut.GetSlotState(1));
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
            _sut.Update(5.0f); // complete first layer

            _sut.Update(3.0f); // Wood (3s) completes, Stone (5s) still unlocking

            Assert.AreEqual(CollectibleSlotState.Unlocked, _sut.GetSlotState(0));
            Assert.AreEqual(CollectibleSlotState.Unlocking, _sut.GetSlotState(1));
        }

        [Test]
        public void Update_SlotUnlockCompletes_PublishesItemSlotUnlockedEvent()
        {
            _sut.StartGathering();
            _sut.Update(5.0f); // complete first layer

            ItemSlotUnlockedEvent receivedEvent = null;
            Action<ItemSlotUnlockedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<ItemSlotUnlockedEvent>(handler);

            _sut.Update(3.0f); // Wood completes

            EventBus.Unsubscribe<ItemSlotUnlockedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(0, receivedEvent.SlotIndex);
            Assert.AreEqual("Wood", receivedEvent.ItemId);
        }

        [Test]
        public void Update_AllSlotsUnlock_AllBecomesUnlocked()
        {
            _sut.StartGathering();
            _sut.Update(5.0f); // complete first layer
            _sut.Update(5.0f); // complete all second layer slots

            Assert.AreEqual(CollectibleSlotState.Unlocked, _sut.GetSlotState(0));
            Assert.AreEqual(CollectibleSlotState.Unlocked, _sut.GetSlotState(1));
        }

        [Test]
        public void Update_ZeroUnlockDuration_ImmediatelyUnlocked()
        {
            List<CollectibleItemEntry> items = new List<CollectibleItemEntry>
            {
                new CollectibleItemEntry("Wood", 1, 0f) // zero unlock duration
            };
            CollectiblePointData data = new CollectiblePointData(0, 0, 1.0f, items);
            CollectiblePointState sut = new CollectiblePointState(data);

            sut.StartGathering();
            sut.Update(1.0f); // complete first layer

            // Slot should be immediately Unlocked (zero unlock duration)
            Assert.AreEqual(CollectibleSlotState.Unlocked, sut.GetSlotState(0));
        }

        // ===== TryPickItem (GDD rule 46) =====

        [Test]
        public void TryPickItem_UnlockedSlot_ReturnsQuantity()
        {
            _sut.StartGathering();
            _sut.Update(5.0f); // complete first layer
            _sut.Update(3.0f); // Wood (slot 0) unlocked

            BackpackManager backpack = new BackpackManager(5, 10);
            int picked = _sut.TryPickItem(0, backpack);

            Assert.AreEqual(2, picked); // Wood quantity = 2
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
            _sut.Update(5.0f); // into unlocking, but slots still unlocking

            BackpackManager backpack = new BackpackManager(5, 10);
            int picked = _sut.TryPickItem(0, backpack);

            // Slot 0 is still Unlocking (not yet Unlocked), should return 0
            // Wait, at 5s with 3s unlock duration, slot 0 should NOT be unlocked yet
            // because we only entered Unlocking at that moment, no additional time passed
            Assert.AreEqual(0, picked);
        }

        [Test]
        public void TryPickItem_TakenSlot_ReturnsZero()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(3.0f);

            BackpackManager backpack = new BackpackManager(5, 10);
            _sut.TryPickItem(0, backpack); // first pick

            int secondPick = _sut.TryPickItem(0, backpack); // already taken
            Assert.AreEqual(0, secondPick);
        }

        [Test]
        public void TryPickItem_BackpackFull_ReturnsZero()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(3.0f);

            BackpackManager backpack = new BackpackManager(1, 1);
            backpack.AddItem("Other", 1); // fill the backpack

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
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.TryPickItem(2, backpack));
        }

        [Test]
        public void TryPickItem_NullBackpack_ThrowsArgumentNullException()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);

            Assert.Throws<ArgumentNullException>(() => _sut.TryPickItem(0, null));
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
        public void CloseItemPanel_ResetsSlotStates()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(3.0f); // some slots unlocked

            _sut.CloseItemPanel();

            // All slots should be back to Locked
            Assert.AreEqual(CollectibleSlotState.Locked, _sut.GetSlotState(0));
            Assert.AreEqual(CollectibleSlotState.Locked, _sut.GetSlotState(1));
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
            _sut.Update(5.0f); // all unlocked

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
        public void AllItemsTaken_AllItemsTaken_ReturnsTrue()
        {
            _sut.StartGathering();
            _sut.Update(5.0f);
            _sut.Update(5.0f);

            BackpackManager backpack = new BackpackManager(5, 10);
            _sut.TryPickItem(0, backpack);
            _sut.TryPickItem(1, backpack);

            Assert.IsTrue(_sut.AllItemsTaken);
        }

        // ===== GetSlotState / GetSlotUnlockProgress =====

        [Test]
        public void GetSlotState_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GetSlotState(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GetSlotState(2));
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

        // ===== GDD rule 44: Cancel resets time =====

        [Test]
        public void CancelAndRestart_TimeResetsCompletely()
        {
            _sut.StartGathering();
            _sut.Update(3.0f); // 3 of 4 seconds

            _sut.CancelGathering();
            _sut.StartGathering();

            // After cancel and restart, remaining time should be full again
            Assert.AreEqual(4.0f, _sut.GatheringRemainingTime, 0.01f);
        }

        // ===== Reuse: CloseItemPanel then StartGathering again =====

        [Test]
        public void CloseItemPanel_ThenRestart_WorksCorrectly()
        {
            _sut.StartGathering();
            _sut.Update(5.0f); // gathering complete, unlocking
            _sut.CloseItemPanel(); // close

            // Should be able to start again
            _sut.StartGathering();
            Assert.AreEqual(GatheringPhase.Gathering, _sut.Phase);
        }
    }
}
