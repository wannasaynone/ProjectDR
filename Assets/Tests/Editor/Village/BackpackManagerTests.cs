using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// BackpackManager 的單元測試。
    /// 測試對象：格子制物品新增/移除、容量限制、堆疊邏輯、快照/回溯、事件發布。
    /// 此測試不依賴 MonoBehaviour 或 Unity 場景，為純邏輯測試。
    /// </summary>
    [TestFixture]
    public class BackpackManagerTests
    {
        private const int TestMaxSlots = 3;
        private const int TestMaxStack = 5;

        private BackpackManager _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _sut = new BackpackManager(TestMaxSlots, TestMaxStack);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== 建構 =====

        [Test]
        public void Constructor_ZeroMaxSlots_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new BackpackManager(0, TestMaxStack));
        }

        [Test]
        public void Constructor_NegativeMaxSlots_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new BackpackManager(-1, TestMaxStack));
        }

        [Test]
        public void Constructor_ZeroMaxStack_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new BackpackManager(TestMaxSlots, 0));
        }

        [Test]
        public void Constructor_NegativeMaxStack_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new BackpackManager(TestMaxSlots, -1));
        }

        [Test]
        public void Constructor_ValidParams_CreatesEmptyBackpack()
        {
            Assert.IsTrue(_sut.IsEmpty);
            Assert.IsFalse(_sut.IsFull);
            Assert.AreEqual(TestMaxSlots, _sut.MaxSlots);
            Assert.AreEqual(TestMaxStack, _sut.DefaultMaxStack);
        }

        // ===== AddItem =====

        [Test]
        public void AddItem_ValidItem_ReturnsAddedQuantity()
        {
            int added = _sut.AddItem("Wood", 3);

            Assert.AreEqual(3, added);
        }

        [Test]
        public void AddItem_ValidItem_IncreasesItemCount()
        {
            _sut.AddItem("Wood", 3);

            Assert.AreEqual(3, _sut.GetItemCount("Wood"));
        }

        [Test]
        public void AddItem_SameItemTwice_StacksInSameSlot()
        {
            _sut.AddItem("Wood", 2);
            _sut.AddItem("Wood", 2);

            Assert.AreEqual(4, _sut.GetItemCount("Wood"));

            // 應只佔一個格子
            IReadOnlyList<BackpackSlot> slots = _sut.GetSlots();
            Assert.AreEqual("Wood", slots[0].ItemId);
            Assert.AreEqual(4, slots[0].Quantity);
            Assert.IsTrue(slots[1].IsEmpty);
        }

        [Test]
        public void AddItem_ExceedsMaxStack_SpillsToNextSlot()
        {
            // maxStack=5，加入 8 應分兩格：5+3
            int added = _sut.AddItem("Wood", 8);

            Assert.AreEqual(8, added);
            Assert.AreEqual(8, _sut.GetItemCount("Wood"));

            IReadOnlyList<BackpackSlot> slots = _sut.GetSlots();
            Assert.AreEqual(5, slots[0].Quantity);
            Assert.AreEqual(3, slots[1].Quantity);
        }

        [Test]
        public void AddItem_BackpackFull_ReturnsPartialQuantity()
        {
            // 3 格 x maxStack 5 = 最多 15
            _sut.AddItem("Wood", 15);

            // 背包已滿，再加應回傳 0
            int added = _sut.AddItem("Wood", 5);

            Assert.AreEqual(0, added);
        }

        [Test]
        public void AddItem_PartialSpace_ReturnsActualAdded()
        {
            // 佔 2 格（10 個），第 3 格剩 5 空間
            _sut.AddItem("Wood", 10);

            // 再加 8，只能放 5
            int added = _sut.AddItem("Stone", 8);

            Assert.AreEqual(5, added);
        }

        [Test]
        public void AddItem_DifferentItems_OccupySeparateSlots()
        {
            _sut.AddItem("Wood", 3);
            _sut.AddItem("Stone", 2);

            Assert.AreEqual(3, _sut.GetItemCount("Wood"));
            Assert.AreEqual(2, _sut.GetItemCount("Stone"));

            IReadOnlyList<BackpackSlot> slots = _sut.GetSlots();
            Assert.AreEqual("Wood", slots[0].ItemId);
            Assert.AreEqual("Stone", slots[1].ItemId);
        }

        [Test]
        public void AddItem_ZeroQuantity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.AddItem("Wood", 0));
        }

        [Test]
        public void AddItem_NegativeQuantity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.AddItem("Wood", -1));
        }

        [Test]
        public void AddItem_NullItemId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.AddItem(null, 1));
        }

        [Test]
        public void AddItem_EmptyItemId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.AddItem("", 1));
        }

        [Test]
        public void AddItem_StackExistingBeforeNewSlot_FillsExistingFirst()
        {
            // 格子 0: Wood x3, 格子 1: Stone x2
            _sut.AddItem("Wood", 3);
            _sut.AddItem("Stone", 2);

            // 再加 Wood x4，應先填滿格子 0 (3->5)，剩 2 放新格子 2
            _sut.AddItem("Wood", 4);

            IReadOnlyList<BackpackSlot> slots = _sut.GetSlots();
            Assert.AreEqual(5, slots[0].Quantity); // Wood 填滿
            Assert.AreEqual("Stone", slots[1].ItemId); // Stone 不動
            Assert.AreEqual("Wood", slots[2].ItemId); // Wood 溢出到新格
            Assert.AreEqual(2, slots[2].Quantity);
        }

        // ===== RemoveItem =====

        [Test]
        public void RemoveItem_SufficientQuantity_ReturnsRemovedQuantity()
        {
            _sut.AddItem("Wood", 5);

            int removed = _sut.RemoveItem("Wood", 3);

            Assert.AreEqual(3, removed);
        }

        [Test]
        public void RemoveItem_SufficientQuantity_DecreasesItemCount()
        {
            _sut.AddItem("Wood", 5);
            _sut.RemoveItem("Wood", 3);

            Assert.AreEqual(2, _sut.GetItemCount("Wood"));
        }

        [Test]
        public void RemoveItem_AllQuantity_ClearsSlot()
        {
            _sut.AddItem("Wood", 3);
            _sut.RemoveItem("Wood", 3);

            Assert.AreEqual(0, _sut.GetItemCount("Wood"));
            Assert.IsTrue(_sut.IsEmpty);
        }

        [Test]
        public void RemoveItem_InsufficientQuantity_ReturnsActualRemoved()
        {
            _sut.AddItem("Wood", 3);

            int removed = _sut.RemoveItem("Wood", 5);

            Assert.AreEqual(3, removed);
        }

        [Test]
        public void RemoveItem_NonExistentItem_ReturnsZero()
        {
            int removed = _sut.RemoveItem("Stone", 1);

            Assert.AreEqual(0, removed);
        }

        [Test]
        public void RemoveItem_ZeroQuantity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.RemoveItem("Wood", 0));
        }

        [Test]
        public void RemoveItem_NegativeQuantity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.RemoveItem("Wood", -1));
        }

        // ===== RemoveFromSlot =====

        [Test]
        public void RemoveFromSlot_ValidSlot_ReturnsRemovedQuantity()
        {
            _sut.AddItem("Wood", 3);

            int removed = _sut.RemoveFromSlot(0, 2);

            Assert.AreEqual(2, removed);
        }

        [Test]
        public void RemoveFromSlot_EmptySlot_ReturnsZero()
        {
            int removed = _sut.RemoveFromSlot(0, 1);

            Assert.AreEqual(0, removed);
        }

        [Test]
        public void RemoveFromSlot_ExceedsSlotQuantity_RemovesAll()
        {
            _sut.AddItem("Wood", 3);

            int removed = _sut.RemoveFromSlot(0, 10);

            Assert.AreEqual(3, removed);
            Assert.IsTrue(_sut.GetSlots()[0].IsEmpty);
        }

        [Test]
        public void RemoveFromSlot_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.RemoveFromSlot(-1, 1));
        }

        [Test]
        public void RemoveFromSlot_IndexExceedsMax_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.RemoveFromSlot(TestMaxSlots, 1));
        }

        [Test]
        public void RemoveFromSlot_ZeroQuantity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.RemoveFromSlot(0, 0));
        }

        // ===== IsFull / IsEmpty =====

        [Test]
        public void IsFull_AllSlotsFilled_ReturnsTrue()
        {
            // 3 格 x 5 = 15
            _sut.AddItem("Wood", 15);

            Assert.IsTrue(_sut.IsFull);
        }

        [Test]
        public void IsFull_PartiallyFilled_ReturnsFalse()
        {
            _sut.AddItem("Wood", 10);

            Assert.IsFalse(_sut.IsFull);
        }

        [Test]
        public void IsEmpty_InitialState_ReturnsTrue()
        {
            Assert.IsTrue(_sut.IsEmpty);
        }

        [Test]
        public void IsEmpty_AfterAddItem_ReturnsFalse()
        {
            _sut.AddItem("Wood", 1);

            Assert.IsFalse(_sut.IsEmpty);
        }

        [Test]
        public void IsEmpty_AfterRemoveAll_ReturnsTrue()
        {
            _sut.AddItem("Wood", 3);
            _sut.RemoveItem("Wood", 3);

            Assert.IsTrue(_sut.IsEmpty);
        }

        // ===== GetSlots =====

        [Test]
        public void GetSlots_ReturnsCorrectSlotCount()
        {
            IReadOnlyList<BackpackSlot> slots = _sut.GetSlots();

            Assert.AreEqual(TestMaxSlots, slots.Count);
        }

        [Test]
        public void GetSlots_ReturnsCopy_ModifyingDoesNotAffectBackpack()
        {
            _sut.AddItem("Wood", 3);

            // GetSlots 回傳的是副本，不應影響原始資料
            IReadOnlyList<BackpackSlot> slots = _sut.GetSlots();
            Assert.AreEqual(3, _sut.GetItemCount("Wood"));
        }

        // ===== GetItemCount =====

        [Test]
        public void GetItemCount_NonExistentItem_ReturnsZero()
        {
            Assert.AreEqual(0, _sut.GetItemCount("Potion"));
        }

        [Test]
        public void GetItemCount_MultipleSlots_ReturnsTotalAcrossSlots()
        {
            // 加入 8，分佈在兩個格子 (5+3)
            _sut.AddItem("Wood", 8);

            Assert.AreEqual(8, _sut.GetItemCount("Wood"));
        }

        // ===== TakeSnapshot / RestoreSnapshot =====

        [Test]
        public void TakeSnapshot_ReturnsNonNull()
        {
            BackpackSnapshot snapshot = _sut.TakeSnapshot();

            Assert.IsNotNull(snapshot);
        }

        [Test]
        public void TakeSnapshot_CapturesCurrentState()
        {
            _sut.AddItem("Wood", 3);
            _sut.AddItem("Stone", 2);

            BackpackSnapshot snapshot = _sut.TakeSnapshot();

            Assert.AreEqual(TestMaxSlots, snapshot.SlotCount);
            IReadOnlyList<BackpackSlot> snapshotSlots = snapshot.GetSlots();
            Assert.AreEqual("Wood", snapshotSlots[0].ItemId);
            Assert.AreEqual(3, snapshotSlots[0].Quantity);
            Assert.AreEqual("Stone", snapshotSlots[1].ItemId);
            Assert.AreEqual(2, snapshotSlots[1].Quantity);
        }

        [Test]
        public void RestoreSnapshot_RestoresPreviousState()
        {
            _sut.AddItem("Wood", 3);
            BackpackSnapshot snapshot = _sut.TakeSnapshot();

            // 修改背包內容
            _sut.AddItem("Stone", 2);
            _sut.RemoveItem("Wood", 1);

            // 回溯
            _sut.RestoreSnapshot(snapshot);

            Assert.AreEqual(3, _sut.GetItemCount("Wood"));
            Assert.AreEqual(0, _sut.GetItemCount("Stone"));
        }

        [Test]
        public void RestoreSnapshot_NullSnapshot_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.RestoreSnapshot(null));
        }

        [Test]
        public void RestoreSnapshot_MismatchedSlotCount_ThrowsArgumentException()
        {
            // 建立一個不同格子數的背包來製造不匹配的快照
            BackpackManager otherBackpack = new BackpackManager(TestMaxSlots + 1, TestMaxStack);
            BackpackSnapshot mismatchedSnapshot = otherBackpack.TakeSnapshot();

            Assert.Throws<ArgumentException>(() => _sut.RestoreSnapshot(mismatchedSnapshot));
        }

        [Test]
        public void RestoreSnapshot_SnapshotIsIndependentOfLaterChanges()
        {
            _sut.AddItem("Wood", 3);
            BackpackSnapshot snapshot = _sut.TakeSnapshot();

            // 拍完快照後修改背包
            _sut.AddItem("Wood", 2);

            // 快照不應受後續修改影響
            IReadOnlyList<BackpackSlot> snapshotSlots = snapshot.GetSlots();
            Assert.AreEqual(3, snapshotSlots[0].Quantity);
        }

        // ===== BackpackChangedEvent 事件 =====

        [Test]
        public void AddItem_Success_PublishesBackpackChangedEvent()
        {
            BackpackChangedEvent receivedEvent = null;
            Action<BackpackChangedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<BackpackChangedEvent>(handler);

            _sut.AddItem("Wood", 3);

            EventBus.Unsubscribe<BackpackChangedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual("Wood", receivedEvent.ItemId);
            Assert.AreEqual(3, receivedEvent.TotalQuantity);
        }

        [Test]
        public void AddItem_NoSpaceAvailable_DoesNotPublishEvent()
        {
            _sut.AddItem("Wood", 15); // 填滿

            EventBus.ForceClearAll();

            bool eventPublished = false;
            Action<BackpackChangedEvent> handler = (e) => { eventPublished = true; };
            EventBus.Subscribe<BackpackChangedEvent>(handler);

            _sut.AddItem("Stone", 1);

            EventBus.Unsubscribe<BackpackChangedEvent>(handler);

            Assert.IsFalse(eventPublished);
        }

        [Test]
        public void RemoveItem_Success_PublishesBackpackChangedEvent()
        {
            _sut.AddItem("Wood", 5);
            EventBus.ForceClearAll();

            BackpackChangedEvent receivedEvent = null;
            Action<BackpackChangedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<BackpackChangedEvent>(handler);

            _sut.RemoveItem("Wood", 3);

            EventBus.Unsubscribe<BackpackChangedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual("Wood", receivedEvent.ItemId);
            Assert.AreEqual(2, receivedEvent.TotalQuantity);
        }

        [Test]
        public void RemoveItem_NonExistent_DoesNotPublishEvent()
        {
            bool eventPublished = false;
            Action<BackpackChangedEvent> handler = (e) => { eventPublished = true; };
            EventBus.Subscribe<BackpackChangedEvent>(handler);

            _sut.RemoveItem("Stone", 1);

            EventBus.Unsubscribe<BackpackChangedEvent>(handler);

            Assert.IsFalse(eventPublished);
        }

        [Test]
        public void RestoreSnapshot_PublishesBackpackChangedEvent()
        {
            BackpackSnapshot snapshot = _sut.TakeSnapshot();
            _sut.AddItem("Wood", 3);

            EventBus.ForceClearAll();

            bool eventPublished = false;
            Action<BackpackChangedEvent> handler = (e) => { eventPublished = true; };
            EventBus.Subscribe<BackpackChangedEvent>(handler);

            _sut.RestoreSnapshot(snapshot);

            EventBus.Unsubscribe<BackpackChangedEvent>(handler);

            Assert.IsTrue(eventPublished);
        }
    }
}
