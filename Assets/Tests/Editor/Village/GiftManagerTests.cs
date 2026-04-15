using System;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// GiftManager 的單元測試。
    /// 測試對象：建構驗證、送禮流程（扣物品先背包後倉庫、加好感度）、邊界條件、事件發布。
    /// 此測試不依賴 MonoBehaviour 或 Unity 場景，為純邏輯測試。
    /// </summary>
    [TestFixture]
    public class GiftManagerTests
    {
        private GiftManager _sut;
        private AffinityManager _affinityManager;
        private BackpackManager _backpackManager;
        private StorageManager _storageManager;

        private const int BackpackMaxSlots = 10;
        private const int BackpackDefaultMaxStack = 99;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            AffinityConfigData configData = new AffinityConfigData
            {
                characters = new AffinityCharacterConfigData[]
                {
                    new AffinityCharacterConfigData
                    {
                        characterId = CharacterIds.VillageChiefWife,
                        thresholds = new int[] { 3, 6 }
                    }
                },
                defaultThresholds = new int[] { 5 }
            };

            AffinityConfig config = new AffinityConfig(configData);
            _affinityManager = new AffinityManager(config);
            _backpackManager = new BackpackManager(BackpackMaxSlots, BackpackDefaultMaxStack);
            _storageManager = new StorageManager();

            _sut = new GiftManager(_affinityManager, _backpackManager, _storageManager);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== 建構參數驗證 =====

        [Test]
        public void Constructor_NullAffinityManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GiftManager(null, _backpackManager, _storageManager));
        }

        [Test]
        public void Constructor_NullBackpackManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GiftManager(_affinityManager, null, _storageManager));
        }

        [Test]
        public void Constructor_NullStorageManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GiftManager(_affinityManager, _backpackManager, null));
        }

        // ===== GiveGift 參數驗證 =====

        [Test]
        public void GiveGift_NullCharacterId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _sut.GiveGift(null, "item_a"));
        }

        [Test]
        public void GiveGift_EmptyCharacterId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _sut.GiveGift("", "item_a"));
        }

        [Test]
        public void GiveGift_NullItemId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _sut.GiveGift(CharacterIds.VillageChiefWife, null));
        }

        [Test]
        public void GiveGift_EmptyItemId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _sut.GiveGift(CharacterIds.VillageChiefWife, ""));
        }

        // ===== 送禮成功：優先從背包扣除 =====

        [Test]
        public void GiveGift_ItemInBackpack_RemovesFromBackpack()
        {
            _backpackManager.AddItem("item_a", 3);

            GiftResult result = _sut.GiveGift(CharacterIds.VillageChiefWife, "item_a");

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2, _backpackManager.GetItemCount("item_a"));
        }

        [Test]
        public void GiveGift_ItemInBackpack_DoesNotTouchStorage()
        {
            _backpackManager.AddItem("item_a", 3);
            _storageManager.AddItem("item_a", 5);

            _sut.GiveGift(CharacterIds.VillageChiefWife, "item_a");

            // 倉庫數量不應改變
            Assert.AreEqual(5, _storageManager.GetItemCount("item_a"));
        }

        [Test]
        public void GiveGift_ItemInBackpack_IncreasesAffinity()
        {
            _backpackManager.AddItem("item_a", 3);

            _sut.GiveGift(CharacterIds.VillageChiefWife, "item_a");

            Assert.AreEqual(1, _affinityManager.GetAffinity(CharacterIds.VillageChiefWife));
        }

        // ===== 送禮成功：背包沒有則從倉庫扣除 =====

        [Test]
        public void GiveGift_ItemOnlyInStorage_RemovesFromStorage()
        {
            _storageManager.AddItem("item_b", 5);

            GiftResult result = _sut.GiveGift(CharacterIds.VillageChiefWife, "item_b");

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(4, _storageManager.GetItemCount("item_b"));
        }

        [Test]
        public void GiveGift_ItemOnlyInStorage_IncreasesAffinity()
        {
            _storageManager.AddItem("item_b", 5);

            _sut.GiveGift(CharacterIds.VillageChiefWife, "item_b");

            Assert.AreEqual(1, _affinityManager.GetAffinity(CharacterIds.VillageChiefWife));
        }

        // ===== 送禮失敗：物品不足 =====

        [Test]
        public void GiveGift_ItemNotInBackpackOrStorage_Fails()
        {
            GiftResult result = _sut.GiveGift(CharacterIds.VillageChiefWife, "nonexistent");

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(GiftError.ItemNotAvailable, result.Error);
        }

        [Test]
        public void GiveGift_ItemNotAvailable_DoesNotChangeAffinity()
        {
            _sut.GiveGift(CharacterIds.VillageChiefWife, "nonexistent");

            Assert.AreEqual(0, _affinityManager.GetAffinity(CharacterIds.VillageChiefWife));
        }

        // ===== 多次送禮累計好感度 =====

        [Test]
        public void GiveGift_MultipleTimes_AccumulatesAffinity()
        {
            _backpackManager.AddItem("item_a", 5);

            _sut.GiveGift(CharacterIds.VillageChiefWife, "item_a");
            _sut.GiveGift(CharacterIds.VillageChiefWife, "item_a");
            _sut.GiveGift(CharacterIds.VillageChiefWife, "item_a");

            Assert.AreEqual(3, _affinityManager.GetAffinity(CharacterIds.VillageChiefWife));
            Assert.AreEqual(2, _backpackManager.GetItemCount("item_a"));
        }

        // ===== 背包用完後自動切倉庫 =====

        [Test]
        public void GiveGift_BackpackRunsOut_FallsBackToStorage()
        {
            _backpackManager.AddItem("item_a", 1);
            _storageManager.AddItem("item_a", 5);

            // 第一次：從背包扣
            GiftResult result1 = _sut.GiveGift(CharacterIds.VillageChiefWife, "item_a");
            Assert.IsTrue(result1.IsSuccess);
            Assert.AreEqual(0, _backpackManager.GetItemCount("item_a"));
            Assert.AreEqual(5, _storageManager.GetItemCount("item_a"));

            // 第二次：背包沒了，從倉庫扣
            GiftResult result2 = _sut.GiveGift(CharacterIds.VillageChiefWife, "item_a");
            Assert.IsTrue(result2.IsSuccess);
            Assert.AreEqual(4, _storageManager.GetItemCount("item_a"));
        }

        // ===== 結果物件 =====

        [Test]
        public void GiveGift_Success_ResultContainsCharacterIdAndItemId()
        {
            _backpackManager.AddItem("item_a", 1);

            GiftResult result = _sut.GiveGift(CharacterIds.VillageChiefWife, "item_a");

            Assert.AreEqual(CharacterIds.VillageChiefWife, result.CharacterId);
            Assert.AreEqual("item_a", result.ItemId);
        }

        [Test]
        public void GiveGift_Failure_ResultContainsCharacterIdAndItemId()
        {
            GiftResult result = _sut.GiveGift(CharacterIds.VillageChiefWife, "item_a");

            Assert.AreEqual(CharacterIds.VillageChiefWife, result.CharacterId);
            Assert.AreEqual("item_a", result.ItemId);
        }

        // ===== 好感度事件在送禮成功時觸發 =====

        [Test]
        public void GiveGift_Success_PublishesAffinityChangedEvent()
        {
            _backpackManager.AddItem("item_a", 1);

            AffinityChangedEvent receivedEvent = null;
            EventBus.Subscribe<AffinityChangedEvent>(e => receivedEvent = e);

            _sut.GiveGift(CharacterIds.VillageChiefWife, "item_a");

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(CharacterIds.VillageChiefWife, receivedEvent.CharacterId);
            Assert.AreEqual(1, receivedEvent.NewValue);
        }

        [Test]
        public void GiveGift_Failure_DoesNotPublishAffinityChangedEvent()
        {
            AffinityChangedEvent receivedEvent = null;
            EventBus.Subscribe<AffinityChangedEvent>(e => receivedEvent = e);

            _sut.GiveGift(CharacterIds.VillageChiefWife, "nonexistent");

            Assert.IsNull(receivedEvent);
        }

        // ===== L1a: GiftSuccessEvent 發布測試 =====

        [Test]
        public void GiveGift_Success_PublishesGiftSuccessEvent()
        {
            _backpackManager.AddItem("item_a", 1);

            GiftSuccessEvent receivedEvent = null;
            EventBus.Subscribe<GiftSuccessEvent>(e => receivedEvent = e);

            _sut.GiveGift(CharacterIds.VillageChiefWife, "item_a");

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(CharacterIds.VillageChiefWife, receivedEvent.CharacterId);
            Assert.AreEqual("item_a", receivedEvent.ItemId);
        }

        [Test]
        public void GiveGift_Failure_DoesNotPublishGiftSuccessEvent()
        {
            GiftSuccessEvent receivedEvent = null;
            EventBus.Subscribe<GiftSuccessEvent>(e => receivedEvent = e);

            _sut.GiveGift(CharacterIds.VillageChiefWife, "nonexistent");

            Assert.IsNull(receivedEvent);
        }

        [Test]
        public void GiveGift_SuccessFromStorage_PublishesGiftSuccessEvent()
        {
            _storageManager.AddItem("item_b", 5);

            GiftSuccessEvent receivedEvent = null;
            EventBus.Subscribe<GiftSuccessEvent>(e => receivedEvent = e);

            _sut.GiveGift(CharacterIds.VillageChiefWife, "item_b");

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(CharacterIds.VillageChiefWife, receivedEvent.CharacterId);
            Assert.AreEqual("item_b", receivedEvent.ItemId);
        }
    }
}
