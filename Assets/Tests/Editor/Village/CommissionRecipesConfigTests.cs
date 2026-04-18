using System;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// CommissionRecipesConfig 單元測試。
    /// 驗證：建構 null 防護、配方查詢、依角色分組、依輸入物品查詢、
    /// slot 數聚合、空手委託、無效項目跳過、真實 JSON 反序列化。
    /// </summary>
    [TestFixture]
    public class CommissionRecipesConfigTests
    {
        // ===== 建構驗證 =====

        [Test]
        public void Constructor_NullData_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CommissionRecipesConfig(null));
        }

        [Test]
        public void Constructor_EmptyRecipes_ReturnsEmptyConfig()
        {
            CommissionRecipesConfig cfg = new CommissionRecipesConfig(
                new CommissionRecipesConfigData { recipes = Array.Empty<CommissionRecipeEntry>() });
            Assert.AreEqual(0, cfg.AllRecipes.Count);
        }

        [Test]
        public void Constructor_NullRecipes_TreatedAsEmpty()
        {
            CommissionRecipesConfig cfg = new CommissionRecipesConfig(
                new CommissionRecipesConfigData { recipes = null });
            Assert.AreEqual(0, cfg.AllRecipes.Count);
        }

        // ===== 基本查詢 =====

        [Test]
        public void GetRecipe_Existing_ReturnsInfo()
        {
            CommissionRecipesConfig cfg = BuildSampleConfig();
            CommissionRecipeInfo info = cfg.GetRecipe("farm_tomato");
            Assert.IsNotNull(info);
            Assert.AreEqual("FarmGirl", info.CharacterId);
            Assert.AreEqual("seed_tomato", info.InputItemId);
            Assert.AreEqual("crop_tomato", info.OutputItemId);
            Assert.AreEqual(30f, info.DurationSeconds);
        }

        [Test]
        public void GetRecipe_Unknown_ReturnsNull()
        {
            CommissionRecipesConfig cfg = BuildSampleConfig();
            Assert.IsNull(cfg.GetRecipe("non_existent"));
            Assert.IsNull(cfg.GetRecipe(null));
            Assert.IsNull(cfg.GetRecipe(""));
        }

        [Test]
        public void GetRecipesByCharacter_GroupsCorrectly()
        {
            CommissionRecipesConfig cfg = BuildSampleConfig();
            Assert.AreEqual(2, cfg.GetRecipesByCharacter("FarmGirl").Count);
            Assert.AreEqual(1, cfg.GetRecipesByCharacter("Witch").Count);
            Assert.AreEqual(1, cfg.GetRecipesByCharacter("Guard").Count);
            Assert.AreEqual(0, cfg.GetRecipesByCharacter("VillageChiefWife").Count);
        }

        [Test]
        public void GetRecipeByInputItem_FarmTomato_ReturnsTomatoRecipe()
        {
            CommissionRecipesConfig cfg = BuildSampleConfig();
            CommissionRecipeInfo info = cfg.GetRecipeByInputItem("FarmGirl", "seed_tomato");
            Assert.IsNotNull(info);
            Assert.AreEqual("farm_tomato", info.RecipeId);
        }

        [Test]
        public void GetRecipeByInputItem_WrongCharacter_ReturnsNull()
        {
            // seed_tomato 是 FarmGirl 的輸入；Witch 不處理它
            CommissionRecipesConfig cfg = BuildSampleConfig();
            Assert.IsNull(cfg.GetRecipeByInputItem("Witch", "seed_tomato"));
        }

        [Test]
        public void GetRecipeByInputItem_GuardEmptyHanded_ReturnsRecipe()
        {
            CommissionRecipesConfig cfg = BuildSampleConfig();
            // 守衛的空手委託：itemId 為 null 或空字串應都能查到
            CommissionRecipeInfo byNull = cfg.GetRecipeByInputItem("Guard", null);
            CommissionRecipeInfo byEmpty = cfg.GetRecipeByInputItem("Guard", "");
            Assert.IsNotNull(byNull);
            Assert.AreEqual("guard_patrol", byNull.RecipeId);
            Assert.AreSame(byNull, byEmpty);
            Assert.IsTrue(byNull.IsEmptyHanded);
        }

        [Test]
        public void CanCharacterProcessItem_MatchesLookup()
        {
            CommissionRecipesConfig cfg = BuildSampleConfig();
            Assert.IsTrue(cfg.CanCharacterProcessItem("FarmGirl", "seed_tomato"));
            Assert.IsFalse(cfg.CanCharacterProcessItem("FarmGirl", "herb_green"));
            Assert.IsTrue(cfg.CanCharacterProcessItem("Witch", "herb_green"));
            // 空手委託走 GetRecipeByInputItem(character, null) 查得，CanCharacterProcessItem 同理
            Assert.IsTrue(cfg.CanCharacterProcessItem("Guard", null));
        }

        // ===== Slot 數聚合 =====

        [Test]
        public void GetWorkbenchSlotCount_TakesMaxOfRecipes()
        {
            CommissionRecipesConfigData data = new CommissionRecipesConfigData
            {
                recipes = new[]
                {
                    BuildEntry("a1", "FarmGirl", "seed_a", "out_a", 10, 2),
                    BuildEntry("a2", "FarmGirl", "seed_b", "out_b", 10, 3),  // 取 max=3
                }
            };
            CommissionRecipesConfig cfg = new CommissionRecipesConfig(data);
            Assert.AreEqual(3, cfg.GetWorkbenchSlotCount("FarmGirl"));
        }

        [Test]
        public void GetWorkbenchSlotCount_UnconfiguredCharacter_ReturnsZero()
        {
            CommissionRecipesConfig cfg = BuildSampleConfig();
            Assert.AreEqual(0, cfg.GetWorkbenchSlotCount("NobodyAtAll"));
            Assert.AreEqual(0, cfg.GetWorkbenchSlotCount(null));
        }

        // ===== 無效項目處理 =====

        [Test]
        public void Constructor_SkipsInvalidEntries()
        {
            CommissionRecipesConfigData data = new CommissionRecipesConfigData
            {
                recipes = new[]
                {
                    null,                                                   // null
                    BuildEntry("", "X", "i", "o", 10, 1),                    // empty recipe_id
                    BuildEntry("r2", "", "i", "o", 10, 1),                   // empty character
                    BuildEntry("r3", "X", "i", "", 10, 1),                   // empty output
                    BuildEntry("r4", "X", "i", "o", 0f, 1),                  // duration 0
                    BuildEntry("r5", "X", "i", "o", -1f, 1),                 // duration negative
                    BuildOutputQtyEntry("r6", "X", "i", "o", 10, 1, 0),      // output_quantity 0
                    BuildEntry("valid", "Witch", "herb", "potion", 15, 2),   // 有效
                }
            };
            CommissionRecipesConfig cfg = new CommissionRecipesConfig(data);
            Assert.AreEqual(1, cfg.AllRecipes.Count);
            Assert.IsNotNull(cfg.GetRecipe("valid"));
        }

        // ===== 真實 JSON =====

        [Test]
        public void RealJson_CommissionRecipesConfig_ParsesCorrectly()
        {
            UnityEngine.TextAsset text = UnityEngine.Resources.Load<UnityEngine.TextAsset>(
                "Config/commission-recipes-config");
            Assert.IsNotNull(text, "commission-recipes-config.json 必須存在於 Resources/Config/");

            CommissionRecipesConfigData data = UnityEngine.JsonUtility.FromJson<CommissionRecipesConfigData>(text.text);
            CommissionRecipesConfig cfg = new CommissionRecipesConfig(data);

            Assert.Greater(cfg.AllRecipes.Count, 0);
            // 預期三位委託角色
            Assert.IsTrue(cfg.GetWorkbenchSlotCount("FarmGirl") > 0);
            Assert.IsTrue(cfg.GetWorkbenchSlotCount("Witch") > 0);
            Assert.IsTrue(cfg.GetWorkbenchSlotCount("Guard") > 0);
            // 守衛至少有一個空手委託
            bool hasEmptyHanded = false;
            foreach (CommissionRecipeInfo r in cfg.GetRecipesByCharacter("Guard"))
            {
                if (r.IsEmptyHanded) { hasEmptyHanded = true; break; }
            }
            Assert.IsTrue(hasEmptyHanded, "Guard 至少需有一個空手委託");
        }

        // ===== 輔助：建立測試資料 =====

        private static CommissionRecipeEntry BuildEntry(
            string recipeId, string charId, string input, string output, float dur, int slotMax)
        {
            return new CommissionRecipeEntry
            {
                recipe_id = recipeId,
                character_id = charId,
                input_item_id = input,
                input_quantity = 1,
                output_item_id = output,
                output_quantity = 1,
                duration_seconds = dur,
                workbench_slot_index_max = slotMax,
                description = "test"
            };
        }

        private static CommissionRecipeEntry BuildOutputQtyEntry(
            string recipeId, string charId, string input, string output, float dur, int slotMax, int outputQty)
        {
            CommissionRecipeEntry e = BuildEntry(recipeId, charId, input, output, dur, slotMax);
            e.output_quantity = outputQty;
            return e;
        }

        private static CommissionRecipesConfig BuildSampleConfig()
        {
            CommissionRecipesConfigData data = new CommissionRecipesConfigData
            {
                recipes = new[]
                {
                    new CommissionRecipeEntry
                    {
                        recipe_id = "farm_tomato",
                        character_id = "FarmGirl",
                        input_item_id = "seed_tomato",
                        input_quantity = 1,
                        output_item_id = "crop_tomato",
                        output_quantity = 1,
                        duration_seconds = 30f,
                        workbench_slot_index_max = 2,
                        description = "",
                    },
                    new CommissionRecipeEntry
                    {
                        recipe_id = "farm_carrot",
                        character_id = "FarmGirl",
                        input_item_id = "seed_carrot",
                        input_quantity = 1,
                        output_item_id = "crop_carrot",
                        output_quantity = 1,
                        duration_seconds = 60f,
                        workbench_slot_index_max = 2,
                        description = "",
                    },
                    new CommissionRecipeEntry
                    {
                        recipe_id = "witch_heal",
                        character_id = "Witch",
                        input_item_id = "herb_green",
                        input_quantity = 1,
                        output_item_id = "potion_heal",
                        output_quantity = 1,
                        duration_seconds = 45f,
                        workbench_slot_index_max = 2,
                        description = "",
                    },
                    new CommissionRecipeEntry
                    {
                        recipe_id = "guard_patrol",
                        character_id = "Guard",
                        input_item_id = "",
                        input_quantity = 0,
                        output_item_id = "seed_tomato",
                        output_quantity = 1,
                        duration_seconds = 90f,
                        workbench_slot_index_max = 2,
                        description = "",
                    },
                }
            };
            return new CommissionRecipesConfig(data);
        }
    }
}
