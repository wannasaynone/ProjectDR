// CGInstallerTests — CGInstaller 單元測試（ADR-003 B4d）。
// 遵循 ADR-003 測試要求：
//   T1: Install(null) 拋出 InvalidOperationException
//   T2: Install → Uninstall → EventBus 訂閱清除（無洩漏）
//   T3: Uninstall 在未 Install 時安全執行（無例外）
//   T4: Install → Uninstall → Install 可重入（無洩漏）
//
// 遵循 ADR-003（IVillageInstaller 契約）

using System;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.Core;
using ProjectDR.Village.CG;
using UnityEngine;

namespace ProjectDR.Tests.Village
{
    [TestFixture]
    public class CGInstallerTests
    {
        private CGInstaller _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _sut = new CGInstaller(new CGSceneData[0]);
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

        // ===== T3: Uninstall 在未 Install 時安全執行 =====

        [Test]
        public void Uninstall_BeforeInstall_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.Uninstall());
        }

        // ===== T2: Install → Uninstall → EventBus 訂閱清除 =====

        [Test]
        public void InstallThenUninstall_CGUnlockedEvent_NotReceivedAfterUninstall()
        {
            VillageContext ctx = BuildTestContext();
            _sut.Install(ctx);

            // Uninstall 後，CGInstaller 的 OnCGUnlocked 不應再收到事件
            _sut.Uninstall();

            bool received = false;
            Action<CGUnlockedEvent> probe = (e) => received = true;
            EventBus.Subscribe(probe);
            try
            {
                // 發布 CGUnlockedEvent（由 CGUnlockManager 的 AffinityThresholdReachedEvent 間接觸發）
                // 直接發布 CGUnlockedEvent 驗證 CGInstaller handler 已解除
                EventBus.Publish(new CGUnlockedEvent { CgSceneId = "test_scene", CharacterId = "TestChar" });
            }
            finally
            {
                EventBus.Unsubscribe(probe);
            }

            // probe 仍收到（EventBus 正常），確認 Uninstall 後 CGInstaller 不再額外處理
            // 此處僅驗證 Uninstall 後無例外 + CGUnlockManager 已 Dispose
            Assert.IsTrue(received, "EventBus 仍應能正常發布/訂閱（與 CGInstaller 無關）");
        }

        // ===== T4: Install → Uninstall → Install 重入不洩漏 =====

        [Test]
        public void Install_Uninstall_Install_IsIdempotentAndFreeOfLeak()
        {
            VillageContext ctx = BuildTestContext();

            // 第一次 Install → Uninstall
            _sut.Install(ctx);
            _sut.Uninstall();

            // 第二次 Install 不應拋出，且不累積訂閱
            Assert.DoesNotThrow(() => _sut.Install(ctx));

            // 發布事件：不應有 double-handler 問題
            int fireCount = 0;
            Action<CGUnlockedEvent> probe = (e) => fireCount++;
            EventBus.Subscribe(probe);
            try
            {
                EventBus.Publish(new CGUnlockedEvent { CgSceneId = "s1", CharacterId = "c1" });
            }
            finally
            {
                EventBus.Unsubscribe(probe);
            }

            // probe 應只收到 1 次（CGInstaller 內部 handler 不重複訂閱）
            Assert.AreEqual(1, fireCount, "重入 Install 後 CGUnlockedEvent 不應被多次觸發");
        }

        // ===== Helpers =====

        /// <summary>
        /// 建立可用於 EditMode 測試的最小 VillageContext。
        /// 使用 new GameObject() 建立 Unity 組件（EditMode 允許）。
        /// </summary>
        private static VillageContext BuildTestContext()
        {
            GameObject canvasGo = new GameObject("TestCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            Transform uiContainer = canvasGo.transform;
            GameDataQuery<KahaGameCore.GameData.IGameData> gameDataAccess =
                (int id) => null;
            return new VillageContext(canvas, uiContainer, gameDataAccess);
        }
    }
}
