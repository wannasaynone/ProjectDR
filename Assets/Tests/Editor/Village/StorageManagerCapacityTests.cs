using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// StorageManager 容量化（格子 + 堆疊）新功能測試。
    /// 補充原 StorageManagerTests（保留舊 API 語意驗證）之外的容量、格子、擴建、TryAddItem 等新功能。
    /// </summary>
    [TestFixture]
    public class StorageManagerCapacityTests
    {
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

        // ===== 預設容量 =====

        [Test]
        public void DefaultConstructor_CapacityIs100()
        {
            StorageManager sut = new StorageManager();
            Assert.AreEqual(100, sut.Capacity);
        }

        [Test]
        public void DefaultConstructor_DefaultMaxStackIs99()
        {
            StorageManager sut = new StorageManager();
            Assert.AreEqual(99, sut.DefaultMaxStack);
        }

        [Test]
        public void DefaultConstructor_UsedSlotsIsZero()
        {
            StorageManager sut = new StorageManager();
            Assert.AreEqual(0, sut.GetUsedSlots());
        }

        // ===== 自訂容量建構 =====

        [Test]
        public void Constructor_WithInitialCapacity_ReturnsCorrectCapacity()
        {
            StorageManager sut = new StorageManager(50, 10);
            Assert.AreEqual(50, sut.Capacity);
            Assert.AreEqual(10, sut.DefaultMaxStack);
        }

        [Test]
        public void Constructor_ZeroCapacity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new StorageManager(0, 10));
        }

        [Test]
        public void Constructor_NegativeCapacity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new StorageManager(-1, 10));
        }

        [Test]
        public void Constructor_ZeroMaxStack_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new StorageManager(10, 0));
        }

        // ===== CanAddItem =====

        [Test]
        public void CanAddItem_EmptyStorage_ReturnsTrueForSmallQuantity()
        {
            StorageManager sut = new StorageManager(2, 5);
            Assert.IsTrue(sut.CanAddItem("Wood", 5));
        }

        [Test]
        public void CanAddItem_QuantityExceedsTotalCapacity_ReturnsFalse()
        {
            StorageManager sut = new StorageManager(2, 5); // 2 slots * 5 stack = 10 total
            Assert.IsFalse(sut.CanAddItem("Wood", 11));
        }

        [Test]
        public void CanAddItem_EmptyString_ReturnsFalse()
        {
            StorageManager sut = new StorageManager();
            Assert.IsFalse(sut.CanAddItem("", 1));
        }

        [Test]
        public void CanAddItem_ZeroQuantity_ReturnsFalse()
        {
            StorageManager sut = new StorageManager();
            Assert.IsFalse(sut.CanAddItem("Wood", 0));
        }

        // ===== TryAddItem =====

        [Test]
        public void TryAddItem_WithinCapacity_ReturnsExactQuantity()
        {
            StorageManager sut = new StorageManager(2, 5);
            int added = sut.TryAddItem("Wood", 5);
            Assert.AreEqual(5, added);
            Assert.AreEqual(5, sut.GetItemCount("Wood"));
        }

        [Test]
        public void TryAddItem_ExceedsCapacity_PartialFill()
        {
            StorageManager sut = new StorageManager(2, 5); // total capacity 10
            int added = sut.TryAddItem("Wood", 15);
            Assert.AreEqual(10, added);
            Assert.AreEqual(10, sut.GetItemCount("Wood"));
        }

        [Test]
        public void TryAddItem_AfterFullyOccupied_ReturnsZero()
        {
            StorageManager sut = new StorageManager(1, 5);
            sut.TryAddItem("Wood", 5);
            int added = sut.TryAddItem("Stone", 1);
            Assert.AreEqual(0, added);
        }

        // ===== AddItem（嚴格模式）=====

        [Test]
        public void AddItem_ExceedsCapacity_ThrowsInvalidOperationException()
        {
            StorageManager sut = new StorageManager(1, 5);
            Assert.Throws<InvalidOperationException>(() => sut.AddItem("Wood", 6));
        }

        [Test]
        public void AddItem_WithinCapacity_DoesNotThrow()
        {
            StorageManager sut = new StorageManager(1, 5);
            Assert.DoesNotThrow(() => sut.AddItem("Wood", 5));
        }

        [Test]
        public void AddItem_EmptyItemId_ThrowsArgumentException()
        {
            StorageManager sut = new StorageManager();
            Assert.Throws<ArgumentException>(() => sut.AddItem("", 1));
        }

        // ===== 堆疊語意 =====

        [Test]
        public void AddItem_SameItemAcrossMultipleSlots_StacksBeforeNewSlot()
        {
            StorageManager sut = new StorageManager(3, 5);
            // 第一個格子填滿 5
            sut.AddItem("Wood", 5);
            // 再加 3 個 → 應該在新格子而非覆蓋
            sut.AddItem("Wood", 3);
            Assert.AreEqual(8, sut.GetItemCount("Wood"));

            IReadOnlyList<BackpackSlot> slots = sut.GetSlots();
            Assert.AreEqual(3, slots.Count);
            // 第一格應已滿 5，第二格為 3
            Assert.AreEqual("Wood", slots[0].ItemId);
            Assert.AreEqual(5, slots[0].Quantity);
            Assert.AreEqual("Wood", slots[1].ItemId);
            Assert.AreEqual(3, slots[1].Quantity);
        }

        [Test]
        public void AddItem_DifferentItems_OccupyDifferentSlots()
        {
            StorageManager sut = new StorageManager(3, 5);
            sut.AddItem("Wood", 2);
            sut.AddItem("Stone", 3);
            Assert.AreEqual(2, sut.GetUsedSlots());
            Assert.AreEqual(2, sut.GetItemCount("Wood"));
            Assert.AreEqual(3, sut.GetItemCount("Stone"));
        }

        [Test]
        public void IsFull_WhenEveryThingFilled_ReturnsTrue()
        {
            StorageManager sut = new StorageManager(2, 5);
            sut.AddItem("Wood", 10); // 兩格各 5，全滿
            Assert.IsTrue(sut.IsFull);
        }

        [Test]
        public void IsFull_EmptyStorage_ReturnsFalse()
        {
            StorageManager sut = new StorageManager();
            Assert.IsFalse(sut.IsFull);
        }

        // ===== RemoveItem 跨格子 =====

        [Test]
        public void RemoveItem_CrossMultipleSlots_Success()
        {
            StorageManager sut = new StorageManager(3, 5);
            sut.AddItem("Wood", 5);
            sut.AddItem("Wood", 3); // 8 total
            bool ok = sut.RemoveItem("Wood", 6);
            Assert.IsTrue(ok);
            Assert.AreEqual(2, sut.GetItemCount("Wood"));
        }

        [Test]
        public void RemoveItem_InsufficientTotal_ReturnsFalseAndNoChange()
        {
            StorageManager sut = new StorageManager(3, 5);
            sut.AddItem("Wood", 3);
            bool ok = sut.RemoveItem("Wood", 5);
            Assert.IsFalse(ok);
            Assert.AreEqual(3, sut.GetItemCount("Wood"));
        }

        // ===== ExpandCapacity =====

        [Test]
        public void ExpandCapacity_IncreasesCapacity()
        {
            StorageManager sut = new StorageManager(10, 5);
            sut.ExpandCapacity(5);
            Assert.AreEqual(15, sut.Capacity);
        }

        [Test]
        public void ExpandCapacity_PreservesExistingItems()
        {
            StorageManager sut = new StorageManager(2, 5);
            sut.AddItem("Wood", 5);
            sut.ExpandCapacity(3);
            Assert.AreEqual(5, sut.GetItemCount("Wood"));
        }

        [Test]
        public void ExpandCapacity_PublishesStorageCapacityChangedEvent()
        {
            StorageManager sut = new StorageManager(10, 5);
            StorageCapacityChangedEvent received = null;
            Action<StorageCapacityChangedEvent> handler = (e) => { received = e; };
            EventBus.Subscribe(handler);
            try
            {
                sut.ExpandCapacity(5);
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
            Assert.IsNotNull(received);
            Assert.AreEqual(10, received.PreviousCapacity);
            Assert.AreEqual(15, received.NewCapacity);
        }

        [Test]
        public void ExpandCapacity_ZeroDelta_ThrowsArgumentException()
        {
            StorageManager sut = new StorageManager();
            Assert.Throws<ArgumentException>(() => sut.ExpandCapacity(0));
        }

        [Test]
        public void ExpandCapacity_NegativeDelta_ThrowsArgumentException()
        {
            StorageManager sut = new StorageManager();
            Assert.Throws<ArgumentException>(() => sut.ExpandCapacity(-5));
        }

        [Test]
        public void ExpandCapacity_AllowsFurtherAddition()
        {
            StorageManager sut = new StorageManager(1, 5);
            sut.AddItem("Wood", 5);
            // 擴建後才有空間
            sut.ExpandCapacity(1);
            sut.AddItem("Stone", 5);
            Assert.AreEqual(5, sut.GetItemCount("Stone"));
            Assert.AreEqual(5, sut.GetItemCount("Wood"));
        }

        // ===== GetAllItems 驗證格子聚合 =====

        [Test]
        public void GetAllItems_AggregatesAcrossSlots()
        {
            StorageManager sut = new StorageManager(3, 5);
            sut.AddItem("Wood", 5);
            sut.AddItem("Wood", 2); // 跨格
            sut.AddItem("Stone", 3);

            IReadOnlyDictionary<string, int> all = sut.GetAllItems();
            Assert.AreEqual(2, all.Count);
            Assert.AreEqual(7, all["Wood"]);
            Assert.AreEqual(3, all["Stone"]);
        }
    }
}
