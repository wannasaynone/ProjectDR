using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.ItemType;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// ItemTypeResolver 的單元測試。
    /// 測試對象：物品分類註冊、查詢、類型判斷、依類型過濾。
    /// 此測試不依賴 MonoBehaviour 或 Unity 場景，為純邏輯測試。
    /// </summary>
    [TestFixture]
    public class ItemTypeResolverTests
    {
        private ItemTypeResolver _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new ItemTypeResolver();
        }

        // ===== Register + GetItemType 正常流程 =====

        [Test]
        public void Register_ValidItemAndType_GetItemTypeReturnsRegisteredType()
        {
            // 正常註冊後可用 GetItemType 取得分類
            _sut.Register("wheat_seed", ItemTypes.Seed);

            string result = _sut.GetItemType("wheat_seed");

            Assert.AreEqual(ItemTypes.Seed, result);
        }

        [Test]
        public void Register_MultipleItems_EachReturnsOwnType()
        {
            // 多個物品各自有獨立分類
            _sut.Register("wheat_seed", ItemTypes.Seed);
            _sut.Register("carrot", ItemTypes.Ingredient);
            _sut.Register("bread", ItemTypes.Food);

            Assert.AreEqual(ItemTypes.Seed, _sut.GetItemType("wheat_seed"));
            Assert.AreEqual(ItemTypes.Ingredient, _sut.GetItemType("carrot"));
            Assert.AreEqual(ItemTypes.Food, _sut.GetItemType("bread"));
        }

        // ===== Register 覆寫既有 itemId =====

        [Test]
        public void Register_DuplicateItemId_OverwritesPreviousType()
        {
            // 重複註冊同一 itemId，後者覆寫前者
            _sut.Register("herb", ItemTypes.Ingredient);
            _sut.Register("herb", ItemTypes.Material);

            string result = _sut.GetItemType("herb");

            Assert.AreEqual(ItemTypes.Material, result);
        }

        // ===== Register null/empty 驗證 =====

        [Test]
        public void Register_NullItemId_ThrowsArgumentException()
        {
            // itemId 為 null 應拋出 ArgumentException
            Assert.Throws<ArgumentException>(() => _sut.Register(null, ItemTypes.Seed));
        }

        [Test]
        public void Register_EmptyItemId_ThrowsArgumentException()
        {
            // itemId 為空字串應拋出 ArgumentException
            Assert.Throws<ArgumentException>(() => _sut.Register("", ItemTypes.Seed));
        }

        [Test]
        public void Register_NullType_ThrowsArgumentException()
        {
            // type 為 null 應拋出 ArgumentException
            Assert.Throws<ArgumentException>(() => _sut.Register("wheat_seed", null));
        }

        [Test]
        public void Register_EmptyType_ThrowsArgumentException()
        {
            // type 為空字串應拋出 ArgumentException
            Assert.Throws<ArgumentException>(() => _sut.Register("wheat_seed", ""));
        }

        // ===== GetItemType 邊界 =====

        [Test]
        public void GetItemType_UnregisteredItem_ReturnsNull()
        {
            // 未註冊的物品查詢應回傳 null
            string result = _sut.GetItemType("unknown_item");

            Assert.IsNull(result);
        }

        [Test]
        public void GetItemType_NullItemId_ThrowsArgumentException()
        {
            // itemId 為 null 應拋出 ArgumentException
            Assert.Throws<ArgumentException>(() => _sut.GetItemType(null));
        }

        [Test]
        public void GetItemType_EmptyItemId_ThrowsArgumentException()
        {
            // itemId 為空字串應拋出 ArgumentException
            Assert.Throws<ArgumentException>(() => _sut.GetItemType(""));
        }

        // ===== IsType =====

        [Test]
        public void IsType_CorrectType_ReturnsTrue()
        {
            // 正確分類查詢應回傳 true
            _sut.Register("wheat_seed", ItemTypes.Seed);

            bool result = _sut.IsType("wheat_seed", ItemTypes.Seed);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsType_WrongType_ReturnsFalse()
        {
            // 錯誤分類查詢應回傳 false
            _sut.Register("wheat_seed", ItemTypes.Seed);

            bool result = _sut.IsType("wheat_seed", ItemTypes.Food);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsType_UnregisteredItem_ReturnsFalse()
        {
            // 未註冊的物品查詢應回傳 false（不拋出例外）
            bool result = _sut.IsType("unknown_item", ItemTypes.Seed);

            Assert.IsFalse(result);
        }

        // ===== GetItemsByType =====

        [Test]
        public void GetItemsByType_WithMatchingItems_ReturnsAllMatchingItemIds()
        {
            // 同一分類下有多個物品，應全部回傳
            _sut.Register("wheat_seed", ItemTypes.Seed);
            _sut.Register("carrot_seed", ItemTypes.Seed);
            _sut.Register("bread", ItemTypes.Food);

            IReadOnlyList<string> seeds = _sut.GetItemsByType(ItemTypes.Seed);

            Assert.AreEqual(2, seeds.Count);
            Assert.IsTrue(seeds.Contains("wheat_seed"));
            Assert.IsTrue(seeds.Contains("carrot_seed"));
        }

        [Test]
        public void GetItemsByType_NoMatchingItems_ReturnsEmptyCollection()
        {
            // 無任何物品屬於該分類，應回傳空集合（非 null）
            _sut.Register("wheat_seed", ItemTypes.Seed);

            IReadOnlyList<string> potions = _sut.GetItemsByType(ItemTypes.Potion);

            Assert.IsNotNull(potions);
            Assert.AreEqual(0, potions.Count);
        }

        [Test]
        public void GetItemsByType_EmptyResolver_ReturnsEmptyCollection()
        {
            // 完全空的 resolver 查詢任何分類，都應回傳空集合
            IReadOnlyList<string> result = _sut.GetItemsByType(ItemTypes.Material);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }
    }
}
