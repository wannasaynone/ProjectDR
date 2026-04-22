// AffinityInstallerTests — AffinityInstaller 單元測試（ADR-003 B4c / Sprint 7 E4）。
//
// 遵循 ADR-003 測試要求（D7 L1 Installer 單元測試）：
//   T1: Install(null) 拋出 InvalidOperationException
//   T2: 建構子 affinityConfigData 為 null 拋出 ArgumentNullException
//   T3: Install 時 ctx.BackpackReadOnly 未就位拋出 InvalidOperationException
//   T4: Install 時 ctx.StorageReadOnly 未就位拋出 InvalidOperationException
//   T5: Install → ctx.AffinityReadOnly 已填入且為 AffinityManager 實例
//   T6: Uninstall 在未 Install 時安全執行（無例外）
//   T7: Install → Uninstall → AffinityManager Accessor 回 null
//
// 遵循 ADR-003（IVillageInstaller 契約）

using System;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village.Core;
using ProjectDR.Village.Affinity;
using UnityEngine;

namespace ProjectDR.Tests.Village
{
    [TestFixture]
    public class AffinityInstallerTests
    {
        private AffinityInstaller _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _sut = new AffinityInstaller(BuildAffinityConfigData());
        }

        [TearDown]
        public void TearDown()
        {
            _sut?.Uninstall();
            EventBus.ForceClearAll();
        }

        // ===== T1: Install(null) 拋出 InvalidOperationException =====

        [Test]
        public void Install_NullContext_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _sut.Install(null));
        }

        // ===== T2: 建構子 affinityConfigData 為 null 拋出 ArgumentNullException =====

        [Test]
        public void Constructor_NullAffinityConfigData_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AffinityInstaller(null));
        }

        // ===== T3: ctx.BackpackReadOnly 未就位時拋出 InvalidOperationException =====

        [Test]
        public void Install_CtxBackpackReadOnlyNull_ThrowsInvalidOperationException()
        {
            // 建構 ctx 但不安裝 CoreStorageInstaller（BackpackReadOnly / StorageReadOnly 均為 null）
            VillageContext ctx = BuildEmptyContext();
            Assert.Throws<InvalidOperationException>(() => _sut.Install(ctx));
        }

        // ===== T4: ctx.StorageReadOnly 未就位時拋出 InvalidOperationException =====

        [Test]
        public void Install_CtxStorageReadOnlyNull_ThrowsInvalidOperationException()
        {
            // 只安裝 Backpack，不安裝 Storage
            VillageContext ctx = BuildEmptyContext();
            ctx.BackpackReadOnly = new ProjectDR.Village.Backpack.BackpackManager(30, 99);
            Assert.Throws<InvalidOperationException>(() => _sut.Install(ctx));
        }

        // ===== T5: Install → ctx.AffinityReadOnly 已填入且為 AffinityManager =====

        [Test]
        public void Install_ValidContext_FillsAffinityReadOnly()
        {
            VillageContext ctx = BuildContextWithCoreStorage();
            _sut.Install(ctx);

            Assert.IsNotNull(ctx.AffinityReadOnly,
                "Install 後 ctx.AffinityReadOnly 應已填入");
        }

        [Test]
        public void Install_ValidContext_AffinityReadOnlyIsAffinityManagerInstance()
        {
            VillageContext ctx = BuildContextWithCoreStorage();
            _sut.Install(ctx);

            Assert.IsInstanceOf<AffinityManager>(ctx.AffinityReadOnly,
                "ctx.AffinityReadOnly 應為 AffinityManager 實例");
        }

        [Test]
        public void Install_ValidContext_AffinityManagerAccessorAvailable()
        {
            VillageContext ctx = BuildContextWithCoreStorage();
            _sut.Install(ctx);

            Assert.IsNotNull(_sut.AffinityManager,
                "AffinityManager Accessor 應在 Install 後可用");
        }

        // ===== T6: Uninstall 在未 Install 時安全執行 =====

        [Test]
        public void Uninstall_BeforeInstall_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.Uninstall());
        }

        // ===== T7: Install → Uninstall → AffinityManager Accessor 回 null =====

        [Test]
        public void Uninstall_AfterInstall_ClearsAffinityManagerAccessor()
        {
            VillageContext ctx = BuildContextWithCoreStorage();
            _sut.Install(ctx);
            _sut.Uninstall();

            Assert.IsNull(_sut.AffinityManager,
                "Uninstall 後 AffinityManager Accessor 應回 null");
        }

        // ===== Helpers =====

        private static AffinityConfigData BuildAffinityConfigData()
        {
            return new AffinityConfigData
            {
                characters = new AffinityCharacterConfigData[0]
            };
        }

        private static VillageContext BuildEmptyContext()
        {
            GameObject canvasGo = new GameObject("TestCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            Transform uiContainer = canvasGo.transform;
            GameDataQuery<KahaGameCore.GameData.IGameData> gameDataAccess = (int id) => null;
            return new VillageContext(canvas, uiContainer, gameDataAccess);
        }

        /// <summary>
        /// 建立已安裝 CoreStorageInstaller 的 VillageContext（ctx.BackpackReadOnly + StorageReadOnly 就位）。
        /// AffinityInstaller 依賴這兩個欄位才能 Install。
        /// </summary>
        private static VillageContext BuildContextWithCoreStorage()
        {
            VillageContext ctx = BuildEmptyContext();

            CoreStorageInstaller coreInstaller = new CoreStorageInstaller(30, 99, 100, 99);
            coreInstaller.Install(ctx);

            return ctx;
        }
    }
}
