// ProgressionInstallerTests — ProgressionInstaller 單元測試。
// 驗證：建構防護、Install 後 ctx 已填入 VillageProgressionReadOnly、
// 角色解鎖查詢正常、探索解鎖查詢正常、Uninstall 不拋例外、
// ctx 為 null 時 Install 拋出 InvalidOperationException。

using System;
using NUnit.Framework;
using UnityEngine;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.CharacterUnlock;
using ProjectDR.Village.Core;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.Progression;
using ProjectDR.Village.Storage;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// ProgressionInstaller 單元測試。
    /// </summary>
    [TestFixture]
    public class ProgressionInstallerTests
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

        // ===== 建構防護 =====

        [Test]
        public void Constructor_NullMainQuestConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ProgressionInstaller(
                null,
                BuildInitialResourcesConfigData(),
                BuildBackpackManager(),
                BuildStorageManager()));
        }

        [Test]
        public void Constructor_NullInitialResourcesConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ProgressionInstaller(
                BuildMainQuestConfigData(),
                null,
                BuildBackpackManager(),
                BuildStorageManager()));
        }

        [Test]
        public void Constructor_NullBackpackManager_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ProgressionInstaller(
                BuildMainQuestConfigData(),
                BuildInitialResourcesConfigData(),
                null,
                BuildStorageManager()));
        }

        [Test]
        public void Constructor_NullStorageManager_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ProgressionInstaller(
                BuildMainQuestConfigData(),
                BuildInitialResourcesConfigData(),
                BuildBackpackManager(),
                null));
        }

        // ===== Install 行為 =====

        [Test]
        public void Install_NullCtx_ThrowsInvalidOperationException()
        {
            ProgressionInstaller installer = BuildInstaller();
            Assert.Throws<InvalidOperationException>(() => installer.Install(null));
        }

        [Test]
        public void Install_FillsVillageProgressionReadOnly()
        {
            ProgressionInstaller installer = BuildInstaller();
            VillageContext ctx = BuildContext();

            installer.Install(ctx);

            Assert.IsNotNull(ctx.VillageProgressionReadOnly,
                "Install 後 ctx.VillageProgressionReadOnly 應已填入");
        }

        [Test]
        public void Install_VillageChiefWife_IsUnlockedByDefault()
        {
            ProgressionInstaller installer = BuildInstaller();
            VillageContext ctx = BuildContext();
            installer.Install(ctx);

            bool isUnlocked = ctx.VillageProgressionReadOnly.IsCharacterUnlocked(CharacterIds.VillageChiefWife);
            Assert.IsTrue(isUnlocked, "村長夫人應在開局即解鎖");
        }

        [Test]
        public void Install_FarmGirl_IsLockedInitially()
        {
            ProgressionInstaller installer = BuildInstaller();
            VillageContext ctx = BuildContext();
            installer.Install(ctx);

            bool isUnlocked = ctx.VillageProgressionReadOnly.IsCharacterUnlocked(CharacterIds.FarmGirl);
            Assert.IsFalse(isUnlocked, "農女初始應未解鎖");
        }

        [Test]
        public void Install_ExplorationUnlocked_IsFalseInitially()
        {
            ProgressionInstaller installer = BuildInstaller();
            VillageContext ctx = BuildContext();
            installer.Install(ctx);

            bool isUnlocked = ctx.VillageProgressionReadOnly.IsExplorationUnlocked();
            Assert.IsFalse(isUnlocked, "探索功能初始應未解鎖");
        }

        [Test]
        public void GetRedDotManager_AfterInstall_ReturnsInstance()
        {
            ProgressionInstaller installer = BuildInstaller();
            VillageContext ctx = BuildContext();
            installer.Install(ctx);

            Assert.IsNotNull(installer.GetRedDotManager(), "Install 後 GetRedDotManager 應回傳實例");
        }

        [Test]
        public void GetMainQuestManager_AfterInstall_ReturnsInstance()
        {
            ProgressionInstaller installer = BuildInstaller();
            VillageContext ctx = BuildContext();
            installer.Install(ctx);

            Assert.IsNotNull(installer.GetMainQuestManager(), "Install 後 GetMainQuestManager 應回傳實例");
        }

        [Test]
        public void Uninstall_AfterInstall_DoesNotThrow()
        {
            ProgressionInstaller installer = BuildInstaller();
            VillageContext ctx = BuildContext();
            installer.Install(ctx);

            Assert.DoesNotThrow(() => installer.Uninstall(),
                "Uninstall 不應拋出例外");
        }

        // ===== 輔助：建立測試依賴 =====

        private static MainQuestConfigData BuildMainQuestConfigData()
        {
            return new MainQuestConfigData
            {
                schema_version = 2,
                main_quests = new MainQuestConfigEntry[]
                {
                    new MainQuestConfigEntry
                    {
                        id = 1,
                        quest_id = "T0",
                        completion_condition_type = MainQuestCompletionTypes.Auto,
                        completion_condition_value = MainQuestSignalValues.Node0DialogueComplete,
                        unlock_on_complete = "T1",
                        sort_order = 0
                    },
                    new MainQuestConfigEntry
                    {
                        id = 2,
                        quest_id = "T1",
                        completion_condition_type = MainQuestCompletionTypes.DialogueEnd,
                        completion_condition_value = MainQuestSignalValues.Node2DialogueComplete,
                        unlock_on_complete = "T2|exploration_open",
                        sort_order = 1
                    }
                }
            };
        }

        private static InitialResourcesConfigData BuildInitialResourcesConfigData()
        {
            return new InitialResourcesConfigData
            {
                schema_version = 2,
                grants = new InitialResourceGrantData[]
                {
                    new InitialResourceGrantData
                    {
                        id = 1,
                        grant_id = "initial_backpack_node0",
                        trigger_id = InitialResourcesTriggerIds.Node0Start,
                        item_id = "",
                        quantity = 0
                    }
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

        private static ProgressionInstaller BuildInstaller()
        {
            return new ProgressionInstaller(
                BuildMainQuestConfigData(),
                BuildInitialResourcesConfigData(),
                BuildBackpackManager(),
                BuildStorageManager());
        }

        private static VillageContext BuildContext()
        {
            // 使用 fake Canvas / Transform（Editor 測試允許 null 在此情境）
            // VillageContext 要求 canvas/uiContainer 非 null，使用 new GameObject
            GameObject go = new GameObject("TestCtx");
            Canvas canvas = go.AddComponent<Canvas>();
            GameDataQuery<KahaGameCore.GameData.IGameData> gameDataAccess =
                new GameDataQuery<KahaGameCore.GameData.IGameData>(id => null);
            return new VillageContext(canvas, go.transform, gameDataAccess);
        }
    }
}
