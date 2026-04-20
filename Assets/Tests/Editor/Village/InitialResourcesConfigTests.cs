using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// InitialResourcesConfig / InitialResourcesConfigData 單元測試。
    /// </summary>
    [TestFixture]
    public class InitialResourcesConfigTests
    {
        [Test]
        public void Constructor_NullData_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new InitialResourcesConfig(null));
        }

        [Test]
        public void Constructor_EmptyGrants_NoEntries()
        {
            InitialResourcesConfigData data = new InitialResourcesConfigData
            {
                schema_version = 1,
                grants = new InitialResourceGrantData[0]
            };
            InitialResourcesConfig config = new InitialResourcesConfig(data);
            Assert.IsNull(config.GetGrant("any"));
            Assert.AreEqual(0, config.GetGrantsByTrigger("any").Count);
        }

        [Test]
        public void GetGrant_ExistingGrantId_ReturnsEntry()
        {
            // Sprint 6 重構後：unlock_farm_girl_seed 已移除，改測試仍存在的 unlock_guard_sword
            InitialResourcesConfig config = BuildConfig();
            InitialResourceGrant grant = config.GetGrant("unlock_guard_sword");
            Assert.IsNotNull(grant);
            Assert.AreEqual("gift_sword_wooden", grant.ItemId);
            Assert.AreEqual(1, grant.Quantity);
            Assert.IsTrue(grant.HasItem);
        }

        [Test]
        public void GetGrant_Unknown_ReturnsNull()
        {
            InitialResourcesConfig config = BuildConfig();
            Assert.IsNull(config.GetGrant("nonexistent"));
        }

        [Test]
        public void GetGrantsByTrigger_GroupsByTrigger()
        {
            InitialResourcesConfig config = BuildConfig();
            IReadOnlyList<InitialResourceGrant> node0Grants = config.GetGrantsByTrigger(InitialResourcesTriggerIds.Node0Start);
            Assert.AreEqual(1, node0Grants.Count);
            Assert.AreEqual("initial_backpack_node0", node0Grants[0].GrantId);
        }

        [Test]
        public void GetGrantsByTrigger_MultipleInOneTrigger()
        {
            InitialResourcesConfigData data = new InitialResourcesConfigData
            {
                schema_version = 1,
                grants = new InitialResourceGrantData[]
                {
                    new InitialResourceGrantData { grant_id = "g1", trigger_id = "shared_trigger", item_id = "a", quantity = 1 },
                    new InitialResourceGrantData { grant_id = "g2", trigger_id = "shared_trigger", item_id = "b", quantity = 2 }
                }
            };
            InitialResourcesConfig config = new InitialResourcesConfig(data);
            IReadOnlyList<InitialResourceGrant> grants = config.GetGrantsByTrigger("shared_trigger");
            Assert.AreEqual(2, grants.Count);
        }

        [Test]
        public void GrantWithEmptyItemId_HasItemIsFalse()
        {
            InitialResourcesConfigData data = new InitialResourcesConfigData
            {
                schema_version = 1,
                grants = new InitialResourceGrantData[]
                {
                    new InitialResourceGrantData { grant_id = "empty", trigger_id = "t", item_id = "", quantity = 0 }
                }
            };
            InitialResourcesConfig config = new InitialResourcesConfig(data);
            InitialResourceGrant grant = config.GetGrant("empty");
            Assert.IsFalse(grant.HasItem);
        }

        [Test]
        public void GrantWithNullItemId_IsTreatedAsEmpty()
        {
            InitialResourcesConfigData data = new InitialResourcesConfigData
            {
                schema_version = 1,
                grants = new InitialResourceGrantData[]
                {
                    new InitialResourceGrantData { grant_id = "null_item", trigger_id = "t", item_id = null, quantity = 5 }
                }
            };
            InitialResourcesConfig config = new InitialResourcesConfig(data);
            InitialResourceGrant grant = config.GetGrant("null_item");
            Assert.IsNotNull(grant);
            Assert.AreEqual("", grant.ItemId);
            Assert.IsFalse(grant.HasItem);
        }

        [Test]
        public void GrantWithNullGrantId_Skipped()
        {
            InitialResourcesConfigData data = new InitialResourcesConfigData
            {
                schema_version = 1,
                grants = new InitialResourceGrantData[]
                {
                    new InitialResourceGrantData { grant_id = null, trigger_id = "t", item_id = "a", quantity = 1 },
                    new InitialResourceGrantData { grant_id = "g1", trigger_id = "t", item_id = "a", quantity = 1 }
                }
            };
            InitialResourcesConfig config = new InitialResourcesConfig(data);
            Assert.IsNotNull(config.GetGrant("g1"));
            IReadOnlyList<InitialResourceGrant> grants = config.GetGrantsByTrigger("t");
            Assert.AreEqual(1, grants.Count);
        }

        [Test]
        public void Sprint6_FarmGirlSeedGrant_DoesNotExist()
        {
            // Sprint 6 決策 6：移除農女初始物資 grant
            InitialResourcesConfig config = BuildConfig();
            Assert.IsNull(config.GetGrant("unlock_farm_girl_seed"),
                "unlock_farm_girl_seed 應已從 config 移除（Sprint 6 決策 6）");
        }

        [Test]
        public void Sprint6_WitchHerbGrant_DoesNotExist()
        {
            // Sprint 6 決策 6：移除魔女初始物資 grant
            InitialResourcesConfig config = BuildConfig();
            Assert.IsNull(config.GetGrant("unlock_witch_herb"),
                "unlock_witch_herb 應已從 config 移除（Sprint 6 決策 6）");
        }

        [Test]
        public void Sprint6_GuardSwordGrant_StillExists()
        {
            // Sprint 6：守衛贈劍 grant 不受影響，應保留
            InitialResourcesConfig config = BuildConfig();
            InitialResourceGrant grant = config.GetGrant("unlock_guard_sword");
            Assert.IsNotNull(grant, "unlock_guard_sword 應保留（守衛贈劍為 T2 完成條件）");
            Assert.AreEqual("gift_sword_wooden", grant.ItemId);
            Assert.AreEqual(1, grant.Quantity);
        }

        [Test]
        public void Sprint6_InitialBackpackNode0_StillExists()
        {
            // Sprint 6：空背包初始化 grant 不受影響，應保留
            InitialResourcesConfig config = BuildConfig();
            Assert.IsNotNull(config.GetGrant("initial_backpack_node0"),
                "initial_backpack_node0 應保留");
        }

        [Test]
        public void RealJsonFile_DeserializesSuccessfully()
        {
            // Sprint 6 重構後：移除 unlock_farm_girl_seed / unlock_witch_herb；保留 unlock_guard_sword / initial_backpack_node0
            TextAsset asset = Resources.Load<TextAsset>("Config/initial-resources-config");
            if (asset == null)
            {
                Assert.Pass("initial-resources-config 資源不存在，跳過真實 JSON 測試。");
                return;
            }

            InitialResourcesConfigData data = JsonUtility.FromJson<InitialResourcesConfigData>(asset.text);
            Assert.IsNotNull(data);
            InitialResourcesConfig config = new InitialResourcesConfig(data);
            Assert.IsNull(config.GetGrant("unlock_farm_girl_seed"), "unlock_farm_girl_seed 應已從 JSON 中移除");
            Assert.IsNull(config.GetGrant("unlock_witch_herb"), "unlock_witch_herb 應已從 JSON 中移除");
            Assert.IsNotNull(config.GetGrant("unlock_guard_sword"), "unlock_guard_sword 應保留");
            Assert.IsNotNull(config.GetGrant("initial_backpack_node0"), "initial_backpack_node0 應保留");
        }

        /// <summary>
        /// Sprint 6 重構後的測試用配置。
        /// 移除 unlock_farm_girl_seed / unlock_witch_herb（決策 6：物資完全依賴探索）。
        /// 保留 initial_backpack_node0 / unlock_guard_sword。
        /// </summary>
        private static InitialResourcesConfig BuildConfig()
        {
            InitialResourcesConfigData data = new InitialResourcesConfigData
            {
                schema_version = 1,
                grants = new InitialResourceGrantData[]
                {
                    new InitialResourceGrantData
                    {
                        grant_id = "initial_backpack_node0",
                        trigger_id = InitialResourcesTriggerIds.Node0Start,
                        item_id = "",
                        quantity = 0
                    },
                    new InitialResourceGrantData
                    {
                        grant_id = "unlock_guard_sword",
                        trigger_id = InitialResourcesTriggerIds.GuardReturnEvent,
                        item_id = "gift_sword_wooden",
                        quantity = 1
                    }
                }
            };
            return new InitialResourcesConfig(data);
        }
    }
}
