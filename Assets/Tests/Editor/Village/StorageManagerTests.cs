using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// StorageManager 的單元測試。
    /// 測試對象：物品新增、移除、數量查詢、全部取得、擁有判斷、事件發布。
    /// 此測試不依賴 MonoBehaviour 或 Unity 場景，為純邏輯測試。
    /// </summary>
    [TestFixture]
    public class StorageManagerTests
    {
        private StorageManager _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _sut = new StorageManager();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== AddItem =====

        [Test]
        public void AddItem_ValidItem_IncreasesItemCount()
        {
            // 新增物品後，數量應增加
            _sut.AddItem("Wood", 5);

            Assert.AreEqual(5, _sut.GetItemCount("Wood"));
        }

        [Test]
        public void AddItem_SameItemTwice_AccumulatesQuantity()
        {
            // 多次新增同種物品，數量應累加
            _sut.AddItem("Wood", 3);
            _sut.AddItem("Wood", 7);

            Assert.AreEqual(10, _sut.GetItemCount("Wood"));
        }

        [Test]
        public void AddItem_ZeroQuantity_ThrowsArgumentException()
        {
            // 數量 <= 0 應拋出 ArgumentException
            Assert.Throws<ArgumentException>(() => _sut.AddItem("Wood", 0));
        }

        [Test]
        public void AddItem_NegativeQuantity_ThrowsArgumentException()
        {
            // 負數數量應拋出 ArgumentException
            Assert.Throws<ArgumentException>(() => _sut.AddItem("Wood", -1));
        }

        [Test]
        public void AddItem_DifferentItems_StoredSeparately()
        {
            // 不同物品應分開儲存
            _sut.AddItem("Wood", 5);
            _sut.AddItem("Stone", 3);

            Assert.AreEqual(5, _sut.GetItemCount("Wood"));
            Assert.AreEqual(3, _sut.GetItemCount("Stone"));
        }

        // ===== RemoveItem =====

        [Test]
        public void RemoveItem_SufficientQuantity_ReturnsTrue()
        {
            // 庫存足夠時移除應回傳 true
            _sut.AddItem("Wood", 5);

            bool result = _sut.RemoveItem("Wood", 3);

            Assert.IsTrue(result);
        }

        [Test]
        public void RemoveItem_SufficientQuantity_DecreasesItemCount()
        {
            // 移除後，數量應減少
            _sut.AddItem("Wood", 5);
            _sut.RemoveItem("Wood", 3);

            Assert.AreEqual(2, _sut.GetItemCount("Wood"));
        }

        [Test]
        public void RemoveItem_ExactQuantity_ReturnsTrue()
        {
            // 移除剛好等於庫存的數量應回傳 true
            _sut.AddItem("Wood", 5);

            bool result = _sut.RemoveItem("Wood", 5);

            Assert.IsTrue(result);
        }

        [Test]
        public void RemoveItem_ExactQuantity_ItemCountBecomesZero()
        {
            // 移除所有庫存後，數量應為 0
            _sut.AddItem("Wood", 5);
            _sut.RemoveItem("Wood", 5);

            Assert.AreEqual(0, _sut.GetItemCount("Wood"));
        }

        [Test]
        public void RemoveItem_InsufficientQuantity_ReturnsFalse()
        {
            // 庫存不足時移除應回傳 false
            _sut.AddItem("Wood", 3);

            bool result = _sut.RemoveItem("Wood", 5);

            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveItem_InsufficientQuantity_DoesNotChangeItemCount()
        {
            // 移除失敗後，庫存數量不應改變
            _sut.AddItem("Wood", 3);
            _sut.RemoveItem("Wood", 5);

            Assert.AreEqual(3, _sut.GetItemCount("Wood"));
        }

        [Test]
        public void RemoveItem_NonExistentItem_ReturnsFalse()
        {
            // 移除不存在的物品應回傳 false
            bool result = _sut.RemoveItem("Stone", 1);

            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveItem_ZeroQuantity_ThrowsArgumentException()
        {
            // 移除數量 <= 0 應拋出 ArgumentException
            _sut.AddItem("Wood", 5);

            Assert.Throws<ArgumentException>(() => _sut.RemoveItem("Wood", 0));
        }

        [Test]
        public void RemoveItem_NegativeQuantity_ThrowsArgumentException()
        {
            // 移除負數數量應拋出 ArgumentException
            _sut.AddItem("Wood", 5);

            Assert.Throws<ArgumentException>(() => _sut.RemoveItem("Wood", -1));
        }

        // ===== GetItemCount =====

        [Test]
        public void GetItemCount_NonExistentItem_ReturnsZero()
        {
            // 查詢不存在的物品應回傳 0
            int count = _sut.GetItemCount("Potion");

            Assert.AreEqual(0, count);
        }

        [Test]
        public void GetItemCount_ExistingItem_ReturnsCorrectCount()
        {
            // 查詢已存在物品應回傳正確數量
            _sut.AddItem("Wood", 7);

            Assert.AreEqual(7, _sut.GetItemCount("Wood"));
        }

        // ===== GetAllItems =====

        [Test]
        public void GetAllItems_EmptyStorage_ReturnsEmptyDictionary()
        {
            // 空庫存應回傳空字典（非 null）
            IReadOnlyDictionary<string, int> items = _sut.GetAllItems();

            Assert.IsNotNull(items);
            Assert.AreEqual(0, items.Count);
        }

        [Test]
        public void GetAllItems_AfterAddingItems_ContainsAllItems()
        {
            // 新增多種物品後，GetAllItems 應包含所有物品
            _sut.AddItem("Wood", 5);
            _sut.AddItem("Stone", 3);
            _sut.AddItem("Herb", 10);

            IReadOnlyDictionary<string, int> items = _sut.GetAllItems();

            Assert.AreEqual(3, items.Count);
            Assert.AreEqual(5, items["Wood"]);
            Assert.AreEqual(3, items["Stone"]);
            Assert.AreEqual(10, items["Herb"]);
        }

        // ===== HasItem =====

        [Test]
        public void HasItem_SufficientQuantity_ReturnsTrue()
        {
            // 擁有足夠數量時應回傳 true
            _sut.AddItem("Wood", 5);

            bool result = _sut.HasItem("Wood", 3);

            Assert.IsTrue(result);
        }

        [Test]
        public void HasItem_ExactQuantity_ReturnsTrue()
        {
            // 擁有剛好等於查詢數量時應回傳 true
            _sut.AddItem("Wood", 5);

            bool result = _sut.HasItem("Wood", 5);

            Assert.IsTrue(result);
        }

        [Test]
        public void HasItem_InsufficientQuantity_ReturnsFalse()
        {
            // 數量不足時應回傳 false
            _sut.AddItem("Wood", 2);

            bool result = _sut.HasItem("Wood", 5);

            Assert.IsFalse(result);
        }

        [Test]
        public void HasItem_NonExistentItem_ReturnsFalse()
        {
            // 物品不存在時應回傳 false
            bool result = _sut.HasItem("Diamond", 1);

            Assert.IsFalse(result);
        }

        // ===== StorageChangedEvent 事件 =====

        [Test]
        public void AddItem_ValidItem_PublishesStorageChangedEvent()
        {
            // 新增物品時應發布 StorageChangedEvent
            StorageChangedEvent receivedEvent = null;
            Action<StorageChangedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<StorageChangedEvent>(handler);

            _sut.AddItem("Wood", 5);

            EventBus.Unsubscribe<StorageChangedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
        }

        [Test]
        public void AddItem_ValidItem_EventContainsCorrectItemId()
        {
            // 發布的事件應包含正確的 itemId
            StorageChangedEvent receivedEvent = null;
            Action<StorageChangedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<StorageChangedEvent>(handler);

            _sut.AddItem("Wood", 5);

            EventBus.Unsubscribe<StorageChangedEvent>(handler);

            Assert.AreEqual("Wood", receivedEvent.ItemId);
        }

        [Test]
        public void RemoveItem_Success_PublishesStorageChangedEvent()
        {
            // 成功移除物品時應發布 StorageChangedEvent
            _sut.AddItem("Wood", 5);

            EventBus.ForceClearAll();

            StorageChangedEvent receivedEvent = null;
            Action<StorageChangedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<StorageChangedEvent>(handler);

            _sut.RemoveItem("Wood", 3);

            EventBus.Unsubscribe<StorageChangedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
        }

        [Test]
        public void RemoveItem_Failure_DoesNotPublishStorageChangedEvent()
        {
            // 移除失敗時不應發布事件
            bool eventPublished = false;
            Action<StorageChangedEvent> handler = (e) => { eventPublished = true; };
            EventBus.Subscribe<StorageChangedEvent>(handler);

            _sut.RemoveItem("Wood", 5);

            EventBus.Unsubscribe<StorageChangedEvent>(handler);

            Assert.IsFalse(eventPublished);
        }
    }
}
