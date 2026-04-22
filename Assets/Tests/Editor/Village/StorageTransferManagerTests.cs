using System;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.Storage;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// StorageTransferManager 的單元測試。
    /// 測試對象：背包→倉庫轉移、倉庫→背包轉移、邊界條件。
    /// 此測試不依賴 MonoBehaviour 或 Unity 場景，為純邏輯測試。
    /// </summary>
    [TestFixture]
    public class StorageTransferManagerTests
    {
        private const int TestMaxSlots = 3;
        private const int TestMaxStack = 5;

        private BackpackManager _backpack;
        private StorageManager _warehouse;
        private StorageTransferManager _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _backpack = new BackpackManager(TestMaxSlots, TestMaxStack);
            _warehouse = new StorageManager();
            _sut = new StorageTransferManager(_backpack, _warehouse);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== 建構 =====

        [Test]
        public void Constructor_NullBackpack_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new StorageTransferManager(null, _warehouse));
        }

        [Test]
        public void Constructor_NullWarehouse_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new StorageTransferManager(_backpack, null));
        }

        // ===== TransferToWarehouse =====

        [Test]
        public void TransferToWarehouse_ValidTransfer_ReturnsTransferredQuantity()
        {
            _backpack.AddItem("Wood", 5);

            int transferred = _sut.TransferToWarehouse(0, 3);

            Assert.AreEqual(3, transferred);
        }

        [Test]
        public void TransferToWarehouse_ValidTransfer_RemovesFromBackpack()
        {
            _backpack.AddItem("Wood", 5);

            _sut.TransferToWarehouse(0, 3);

            Assert.AreEqual(2, _backpack.GetItemCount("Wood"));
        }

        [Test]
        public void TransferToWarehouse_ValidTransfer_AddsToWarehouse()
        {
            _backpack.AddItem("Wood", 5);

            _sut.TransferToWarehouse(0, 3);

            Assert.AreEqual(3, _warehouse.GetItemCount("Wood"));
        }

        [Test]
        public void TransferToWarehouse_EntireSlot_ClearsSlot()
        {
            _backpack.AddItem("Wood", 3);

            _sut.TransferToWarehouse(0, 3);

            Assert.AreEqual(0, _backpack.GetItemCount("Wood"));
            Assert.AreEqual(3, _warehouse.GetItemCount("Wood"));
        }

        [Test]
        public void TransferToWarehouse_MoreThanSlotHas_TransfersSlotAmount()
        {
            _backpack.AddItem("Wood", 3);

            int transferred = _sut.TransferToWarehouse(0, 10);

            Assert.AreEqual(3, transferred);
            Assert.AreEqual(0, _backpack.GetItemCount("Wood"));
            Assert.AreEqual(3, _warehouse.GetItemCount("Wood"));
        }

        [Test]
        public void TransferToWarehouse_EmptySlot_ReturnsZero()
        {
            int transferred = _sut.TransferToWarehouse(0, 1);

            Assert.AreEqual(0, transferred);
        }

        [Test]
        public void TransferToWarehouse_ZeroQuantity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.TransferToWarehouse(0, 0));
        }

        [Test]
        public void TransferToWarehouse_NegativeQuantity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.TransferToWarehouse(0, -1));
        }

        [Test]
        public void TransferToWarehouse_InvalidSlotIndex_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.TransferToWarehouse(-1, 1));
        }

        [Test]
        public void TransferToWarehouse_ExceedsMaxSlotIndex_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.TransferToWarehouse(TestMaxSlots, 1));
        }

        // ===== TransferToBackpack =====

        [Test]
        public void TransferToBackpack_ValidTransfer_ReturnsTransferredQuantity()
        {
            _warehouse.AddItem("Wood", 10);

            int transferred = _sut.TransferToBackpack("Wood", 3);

            Assert.AreEqual(3, transferred);
        }

        [Test]
        public void TransferToBackpack_ValidTransfer_AddsToBackpack()
        {
            _warehouse.AddItem("Wood", 10);

            _sut.TransferToBackpack("Wood", 3);

            Assert.AreEqual(3, _backpack.GetItemCount("Wood"));
        }

        [Test]
        public void TransferToBackpack_ValidTransfer_RemovesFromWarehouse()
        {
            _warehouse.AddItem("Wood", 10);

            _sut.TransferToBackpack("Wood", 3);

            Assert.AreEqual(7, _warehouse.GetItemCount("Wood"));
        }

        [Test]
        public void TransferToBackpack_WarehouseHasLess_TransfersAvailableAmount()
        {
            _warehouse.AddItem("Wood", 2);

            int transferred = _sut.TransferToBackpack("Wood", 5);

            Assert.AreEqual(2, transferred);
            Assert.AreEqual(2, _backpack.GetItemCount("Wood"));
            Assert.AreEqual(0, _warehouse.GetItemCount("Wood"));
        }

        [Test]
        public void TransferToBackpack_BackpackFull_ReturnsZero()
        {
            // 填滿背包
            _backpack.AddItem("Stone", 15);
            _warehouse.AddItem("Wood", 10);

            int transferred = _sut.TransferToBackpack("Wood", 5);

            Assert.AreEqual(0, transferred);
            // 倉庫不應減少
            Assert.AreEqual(10, _warehouse.GetItemCount("Wood"));
        }

        [Test]
        public void TransferToBackpack_BackpackPartialSpace_TransfersPartial()
        {
            // 佔 2 格（10 個），第 3 格可放 5
            _backpack.AddItem("Stone", 10);
            _warehouse.AddItem("Wood", 10);

            int transferred = _sut.TransferToBackpack("Wood", 8);

            Assert.AreEqual(5, transferred);
            Assert.AreEqual(5, _backpack.GetItemCount("Wood"));
            Assert.AreEqual(5, _warehouse.GetItemCount("Wood"));
        }

        [Test]
        public void TransferToBackpack_ItemNotInWarehouse_ReturnsZero()
        {
            int transferred = _sut.TransferToBackpack("Wood", 5);

            Assert.AreEqual(0, transferred);
        }

        [Test]
        public void TransferToBackpack_ZeroQuantity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.TransferToBackpack("Wood", 0));
        }

        [Test]
        public void TransferToBackpack_NegativeQuantity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.TransferToBackpack("Wood", -1));
        }

        [Test]
        public void TransferToBackpack_NullItemId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.TransferToBackpack(null, 1));
        }

        [Test]
        public void TransferToBackpack_EmptyItemId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.TransferToBackpack("", 1));
        }

        // ===== 雙向整合 =====

        [Test]
        public void RoundTrip_TransferToWarehouseThenBack_RestoresOriginalState()
        {
            _backpack.AddItem("Wood", 5);

            // 背包 -> 倉庫
            _sut.TransferToWarehouse(0, 5);
            Assert.AreEqual(0, _backpack.GetItemCount("Wood"));
            Assert.AreEqual(5, _warehouse.GetItemCount("Wood"));

            // 倉庫 -> 背包
            _sut.TransferToBackpack("Wood", 5);
            Assert.AreEqual(5, _backpack.GetItemCount("Wood"));
            Assert.AreEqual(0, _warehouse.GetItemCount("Wood"));
        }
    }
}
