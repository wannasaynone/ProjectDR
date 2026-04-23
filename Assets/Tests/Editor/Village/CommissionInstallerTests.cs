// CommissionInstallerTests — CommissionInstaller 單元測試。
// 驗證：建構防護、Install 後 CommissionManager / StorageExpansionManager 實例建立、
// Install 時 ctx.TimeProvider 未就位拋出例外、Tick / Uninstall 不拋例外。

using System;
using NUnit.Framework;
using UnityEngine;
using KahaGameCore.GameData;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.Commission;
using ProjectDR.Village.Core;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.Storage;
using ProjectDR.Village.TimeProvider;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// CommissionInstaller 單元測試。
    /// </summary>
    [TestFixture]
    public class CommissionInstallerTests
    {
        // ===== Fake time provider =====

        private class FakeTimeProvider : ITimeProvider
        {
            public long CurrentTimestamp { get; set; } = 1000L;
            public long GetCurrentTimestampUtc() => CurrentTimestamp;
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

        // ===== 建構防護 =====

        [Test]
        public void Constructor_NullCommissionEntries_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CommissionInstaller(
                null,
                BuildStorageExpansionStageEntries(),
                BuildStorageExpansionRequirementEntries(),
                BuildBackpackManager(),
                BuildStorageManager()));
        }

        [Test]
        public void Constructor_NullStorageExpansionStageEntries_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CommissionInstaller(
                BuildCommissionEntries(),
                null,
                BuildStorageExpansionRequirementEntries(),
                BuildBackpackManager(),
                BuildStorageManager()));
        }

        [Test]
        public void Constructor_NullStorageExpansionRequirementEntries_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CommissionInstaller(
                BuildCommissionEntries(),
                BuildStorageExpansionStageEntries(),
                null,
                BuildBackpackManager(),
                BuildStorageManager()));
        }

        [Test]
        public void Constructor_NullBackpackManager_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CommissionInstaller(
                BuildCommissionEntries(),
                BuildStorageExpansionStageEntries(),
                BuildStorageExpansionRequirementEntries(),
                null,
                BuildStorageManager()));
        }

        [Test]
        public void Constructor_NullStorageManager_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CommissionInstaller(
                BuildCommissionEntries(),
                BuildStorageExpansionStageEntries(),
                BuildStorageExpansionRequirementEntries(),
                BuildBackpackManager(),
                null));
        }

        // ===== Install 行為 =====

        [Test]
        public void Install_NullCtx_ThrowsInvalidOperationException()
        {
            CommissionInstaller installer = BuildInstaller();
            Assert.Throws<InvalidOperationException>(() => installer.Install(null));
        }

        [Test]
        public void Install_TimeProviderNull_ThrowsInvalidOperationException()
        {
            CommissionInstaller installer = BuildInstaller();
            VillageContext ctx = BuildContext(timeProvider: null);

            Assert.Throws<InvalidOperationException>(() => installer.Install(ctx),
                "ctx.TimeProvider 未就位時 Install 應拋出 InvalidOperationException");
        }

        [Test]
        public void Install_CreatesCommissionManager()
        {
            CommissionInstaller installer = BuildInstaller();
            VillageContext ctx = BuildContextWithTimeProvider();

            installer.Install(ctx);

            Assert.IsNotNull(installer.GetCommissionManager(),
                "Install 後 GetCommissionManager 應回傳實例");
        }

        [Test]
        public void Install_CreatesStorageExpansionManager()
        {
            CommissionInstaller installer = BuildInstaller();
            VillageContext ctx = BuildContextWithTimeProvider();

            installer.Install(ctx);

            Assert.IsNotNull(installer.GetStorageExpansionManager(),
                "Install 後 GetStorageExpansionManager 應回傳實例");
        }

        [Test]
        public void Tick_AfterInstall_DoesNotThrow()
        {
            CommissionInstaller installer = BuildInstaller();
            VillageContext ctx = BuildContextWithTimeProvider();
            installer.Install(ctx);

            Assert.DoesNotThrow(() => installer.Tick(0.016f),
                "Tick 不應拋出例外");
        }

        [Test]
        public void Uninstall_AfterInstall_DoesNotThrow()
        {
            CommissionInstaller installer = BuildInstaller();
            VillageContext ctx = BuildContextWithTimeProvider();
            installer.Install(ctx);

            Assert.DoesNotThrow(() => installer.Uninstall(),
                "Uninstall 不應拋出例外");
        }

        // ===== 輔助：建立測試依賴 =====

        private static CommissionRecipeData[] BuildCommissionEntries()
        {
            return new CommissionRecipeData[]
            {
                new CommissionRecipeData
                {
                    id = 1,
                    recipe_id = "witch_heal",
                    character_id = "Witch",
                    workbench_slot_index_max = 2,
                    input_item_id = "herb_green",
                    input_quantity = 1,
                    output_item_id = "potion_heal",
                    output_quantity = 1,
                    duration_seconds = 60
                }
            };
        }

        private static StorageExpansionStageData[] BuildStorageExpansionStageEntries()
        {
            return new StorageExpansionStageData[]
            {
                new StorageExpansionStageData
                {
                    id = 0,
                    level = 0,
                    capacity_before = 0,
                    capacity_after = 10,
                    duration_seconds = 0
                },
                new StorageExpansionStageData
                {
                    id = 1,
                    level = 1,
                    capacity_before = 10,
                    capacity_after = 20,
                    duration_seconds = 30,
                    description = "first expansion"
                }
            };
        }

        private static StorageExpansionRequirementData[] BuildStorageExpansionRequirementEntries()
        {
            return new StorageExpansionRequirementData[]
            {
                new StorageExpansionRequirementData
                {
                    id = 1,
                    stage_level = 1,
                    item_id = "material_wood",
                    quantity = 5
                }
            };
        }

        private static BackpackManager BuildBackpackManager()
        {
            return new BackpackManager(maxSlots: 10, defaultMaxStack: 99);
        }

        private static StorageManager BuildStorageManager()
        {
            return new StorageManager(initialCapacity: 20, defaultMaxStack: 99);
        }

        private static CommissionInstaller BuildInstaller()
        {
            return new CommissionInstaller(
                BuildCommissionEntries(),
                BuildStorageExpansionStageEntries(),
                BuildStorageExpansionRequirementEntries(),
                BuildBackpackManager(),
                BuildStorageManager());
        }

        /// <summary>建立 ctx 並設定 TimeProvider（可選）。</summary>
        private static VillageContext BuildContext(ITimeProvider timeProvider = null)
        {
            GameObject go = new GameObject("TestCtx");
            Canvas canvas = go.AddComponent<Canvas>();
            GameDataQuery<KahaGameCore.GameData.IGameData> gameDataAccess =
                new GameDataQuery<KahaGameCore.GameData.IGameData>(id => null);
            VillageContext ctx = new VillageContext(canvas, go.transform, gameDataAccess);
            ctx.TimeProvider = timeProvider;
            return ctx;
        }

        private static VillageContext BuildContextWithTimeProvider()
        {
            return BuildContext(timeProvider: new FakeTimeProvider());
        }
    }
}
