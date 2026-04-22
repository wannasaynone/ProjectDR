// CoreStorageInstallerTests — CoreStorageInstaller 單元測試（ADR-003 B4a / Sprint 7 E4）。
//
// 遵循 ADR-003 測試要求（D7 L1 Installer 單元測試）：
//   T1: Install(null) 拋出 InvalidOperationException
//   T2: 建構子容量參數小於等於 0 拋出 ArgumentException
//   T3: Install → ctx.BackpackReadOnly 與 ctx.StorageReadOnly 均已填入
//   T4: Uninstall 在未 Install 時安全執行（無例外）
//   T5: Install → Uninstall → BackpackManager / StorageManager Accessor 回 null
//   T6: BackpackManager 實作 IBackpackQuery（GetItemCount / HasItem / IsFull）
//   T7: StorageManager 實作 IStorageQuery（GetItemCount / HasItem）
//
// 遵循 ADR-003（IVillageInstaller 契約）

using System;
using NUnit.Framework;
using ProjectDR.Village.Core;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.Storage;
using UnityEngine;

namespace ProjectDR.Tests.Village
{
    [TestFixture]
    public class CoreStorageInstallerTests
    {
        private CoreStorageInstaller _sut;

        private const int BackpackMaxSlots = 30;
        private const int BackpackMaxStack = 99;
        private const int StorageInitialCapacity = 100;
        private const int StorageMaxStack = 99;

        [SetUp]
        public void SetUp()
        {
            _sut = new CoreStorageInstaller(
                BackpackMaxSlots,
                BackpackMaxStack,
                StorageInitialCapacity,
                StorageMaxStack);
        }

        [TearDown]
        public void TearDown()
        {
            _sut?.Uninstall();
        }

        // ===== T1: Install(null) 拋出 InvalidOperationException =====

        [Test]
        public void Install_NullContext_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _sut.Install(null));
        }

        // ===== T2: 建構子容量參數無效時拋出 ArgumentException =====

        [Test]
        public void Constructor_BackpackMaxSlotsZero_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CoreStorageInstaller(0, BackpackMaxStack, StorageInitialCapacity, StorageMaxStack));
        }

        [Test]
        public void Constructor_BackpackMaxStackZero_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CoreStorageInstaller(BackpackMaxSlots, 0, StorageInitialCapacity, StorageMaxStack));
        }

        [Test]
        public void Constructor_StorageInitialCapacityZero_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CoreStorageInstaller(BackpackMaxSlots, BackpackMaxStack, 0, StorageMaxStack));
        }

        [Test]
        public void Constructor_StorageMaxStackZero_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CoreStorageInstaller(BackpackMaxSlots, BackpackMaxStack, StorageInitialCapacity, 0));
        }

        // ===== T3: Install → ctx.BackpackReadOnly 與 ctx.StorageReadOnly 均已填入 =====

        [Test]
        public void Install_ValidContext_FillsBackpackReadOnly()
        {
            VillageContext ctx = BuildTestContext();
            _sut.Install(ctx);

            Assert.IsNotNull(ctx.BackpackReadOnly,
                "Install 後 ctx.BackpackReadOnly 應已填入");
        }

        [Test]
        public void Install_ValidContext_FillsStorageReadOnly()
        {
            VillageContext ctx = BuildTestContext();
            _sut.Install(ctx);

            Assert.IsNotNull(ctx.StorageReadOnly,
                "Install 後 ctx.StorageReadOnly 應已填入");
        }

        [Test]
        public void Install_ValidContext_AccessorsAvailable()
        {
            VillageContext ctx = BuildTestContext();
            _sut.Install(ctx);

            Assert.IsNotNull(_sut.BackpackManager, "BackpackManager Accessor 應在 Install 後可用");
            Assert.IsNotNull(_sut.StorageManager, "StorageManager Accessor 應在 Install 後可用");
            Assert.IsNotNull(_sut.StorageTransferManager, "StorageTransferManager Accessor 應在 Install 後可用");
        }

        // ===== T4: Uninstall 在未 Install 時安全執行 =====

        [Test]
        public void Uninstall_BeforeInstall_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.Uninstall());
        }

        // ===== T5: Install → Uninstall → Accessor 回 null =====

        [Test]
        public void Uninstall_AfterInstall_ClearsAccessors()
        {
            VillageContext ctx = BuildTestContext();
            _sut.Install(ctx);
            _sut.Uninstall();

            Assert.IsNull(_sut.BackpackManager,
                "Uninstall 後 BackpackManager Accessor 應回 null");
            Assert.IsNull(_sut.StorageManager,
                "Uninstall 後 StorageManager Accessor 應回 null");
            Assert.IsNull(_sut.StorageTransferManager,
                "Uninstall 後 StorageTransferManager Accessor 應回 null");
        }

        // ===== T6: BackpackManager 實作 IBackpackQuery =====

        [Test]
        public void BackpackManager_ImplementsIBackpackQuery_GetItemCountReturnsZeroForMissingItem()
        {
            VillageContext ctx = BuildTestContext();
            _sut.Install(ctx);

            IBackpackQuery query = ctx.BackpackReadOnly;
            Assert.AreEqual(0, query.GetItemCount("nonexistent_item"),
                "空背包查詢不存在的物品應回傳 0");
        }

        [Test]
        public void BackpackManager_ImplementsIBackpackQuery_HasItemReturnsFalseForMissingItem()
        {
            VillageContext ctx = BuildTestContext();
            _sut.Install(ctx);

            IBackpackQuery query = ctx.BackpackReadOnly;
            Assert.IsFalse(query.HasItem("nonexistent_item"),
                "空背包 HasItem 應回傳 false");
        }

        [Test]
        public void BackpackManager_ImplementsIBackpackQuery_IsFullReturnsFalseForEmptyBackpack()
        {
            VillageContext ctx = BuildTestContext();
            _sut.Install(ctx);

            IBackpackQuery query = ctx.BackpackReadOnly;
            Assert.IsFalse(query.IsFull(),
                "空背包 IsFull 應回傳 false");
        }

        // ===== T7: StorageManager 實作 IStorageQuery =====

        [Test]
        public void StorageManager_ImplementsIStorageQuery_GetItemCountReturnsZeroForMissingItem()
        {
            VillageContext ctx = BuildTestContext();
            _sut.Install(ctx);

            IStorageQuery query = ctx.StorageReadOnly;
            Assert.AreEqual(0, query.GetItemCount("nonexistent_item"),
                "空倉庫查詢不存在的物品應回傳 0");
        }

        [Test]
        public void StorageManager_ImplementsIStorageQuery_HasItemReturnsFalseForMissingItem()
        {
            VillageContext ctx = BuildTestContext();
            _sut.Install(ctx);

            IStorageQuery query = ctx.StorageReadOnly;
            Assert.IsFalse(query.HasItem("nonexistent_item"),
                "空倉庫 HasItem 應回傳 false");
        }

        // ===== Helpers =====

        private static VillageContext BuildTestContext()
        {
            GameObject canvasGo = new GameObject("TestCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            Transform uiContainer = canvasGo.transform;
            GameDataQuery<KahaGameCore.GameData.IGameData> gameDataAccess = (int id) => null;
            return new VillageContext(canvas, uiContainer, gameDataAccess);
        }
    }
}
