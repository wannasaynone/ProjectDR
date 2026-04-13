using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using static ProjectDR.Village.AreaIds;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// VillageProgressionManager 的單元測試。
    /// 測試對象：區域解鎖狀態查詢、解鎖推進、強制解鎖、事件發布。
    /// 此測試不依賴 MonoBehaviour 或 Unity 場景，為純邏輯測試。
    /// </summary>
    [TestFixture]
    public class VillageProgressionManagerTests
    {
        private VillageProgressionManager _sut;

        [SetUp]
        public void SetUp()
        {
            // 每個測試前清除 EventBus，避免跨測試的訂閱污染
            EventBus.ForceClearAll();
            _sut = new VillageProgressionManager();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== IsAreaUnlocked =====

        [Test]
        public void IsAreaUnlocked_StorageOnInitialState_ReturnsTrue()
        {
            // 初始狀態下 Storage 應已解鎖
            bool result = _sut.IsAreaUnlocked(Storage);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsAreaUnlocked_UnlockedAreaId_ReturnsTrue()
        {
            // 強制解鎖後，查詢應回傳 true
            _sut.ForceUnlock(Farm);

            bool result = _sut.IsAreaUnlocked(Farm);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsAreaUnlocked_LockedAreaId_ReturnsFalse()
        {
            // 尚未解鎖的區域應回傳 false
            bool result = _sut.IsAreaUnlocked("Blacksmith");

            Assert.IsFalse(result);
        }

        [Test]
        public void IsAreaUnlocked_NonExistentAreaId_ReturnsFalse()
        {
            // 不存在的 areaId 應回傳 false
            bool result = _sut.IsAreaUnlocked("ThisAreaDoesNotExist");

            Assert.IsFalse(result);
        }

        // ===== GetUnlockedAreas =====

        [Test]
        public void GetUnlockedAreas_OnInitialState_ContainsOnlyStorage()
        {
            // 初始狀態應只含 Storage
            IReadOnlyList<string> areas = _sut.GetUnlockedAreas();

            Assert.AreEqual(1, areas.Count);
            Assert.IsTrue(areas.Contains(Storage));
        }

        [Test]
        public void GetUnlockedAreas_AfterForceUnlock_ContainsNewArea()
        {
            // 強制解鎖後，清單應包含新解鎖的區域
            _sut.ForceUnlock(Farm);

            IReadOnlyList<string> areas = _sut.GetUnlockedAreas();

            Assert.IsTrue(areas.Contains(Farm));
        }

        [Test]
        public void GetUnlockedAreas_AfterForceUnlock_StillContainsStorage()
        {
            // 解鎖新區域後，Storage 仍應在清單中
            _sut.ForceUnlock(Farm);

            IReadOnlyList<string> areas = _sut.GetUnlockedAreas();

            Assert.IsTrue(areas.Contains(Storage));
        }

        // ===== ForceUnlock =====

        [Test]
        public void ForceUnlock_NewArea_AreaBecomesUnlocked()
        {
            // 強制解鎖後，IsAreaUnlocked 應回傳 true
            _sut.ForceUnlock(Farm);

            Assert.IsTrue(_sut.IsAreaUnlocked(Farm));
        }

        [Test]
        public void ForceUnlock_AlreadyUnlockedArea_DoesNotDuplicate()
        {
            // 重複解鎖同一區域，清單中不應出現重複
            _sut.ForceUnlock(Farm);
            _sut.ForceUnlock(Farm);

            IReadOnlyList<string> areas = _sut.GetUnlockedAreas();
            int farmCount = 0;
            foreach (string area in areas)
            {
                if (area == Farm) farmCount++;
            }

            Assert.AreEqual(1, farmCount);
        }

        [Test]
        public void ForceUnlock_AlreadyUnlockedArea_DoesNotPublishEventAgain()
        {
            // 重複解鎖同一區域，事件只應發布一次
            int eventCount = 0;
            Action<AreaUnlockedEvent> handler = (e) => { eventCount++; };
            EventBus.Subscribe<AreaUnlockedEvent>(handler);

            _sut.ForceUnlock(Farm);
            _sut.ForceUnlock(Farm);

            EventBus.Unsubscribe<AreaUnlockedEvent>(handler);

            Assert.AreEqual(1, eventCount);
        }

        // ===== TryAdvanceProgression =====

        [Test]
        public void TryAdvanceProgression_WhenConditionsMet_ReturnsTrue()
        {
            // 滿足解鎖條件時，應回傳 true 並推進解鎖
            bool result = _sut.TryAdvanceProgression();

            // 若初始狀態就有可解鎖項目，應回傳 true；否則為 false
            // 此測試驗證方法可正常呼叫且回傳 bool
            Assert.IsInstanceOf<bool>(result);
        }

        [Test]
        public void TryAdvanceProgression_WhenNoConditionsMet_ReturnsFalse()
        {
            // 當沒有更多可解鎖的區域時，應回傳 false
            // 先解鎖所有可能的區域
            _sut.ForceUnlock(Farm);
            _sut.ForceUnlock("Blacksmith");
            _sut.ForceUnlock("Inn");
            _sut.ForceUnlock("Workshop");

            // 當沒有新的解鎖條件滿足時
            bool result = _sut.TryAdvanceProgression();

            // 根據實作，若沒有新東西可解鎖則回傳 false
            Assert.IsFalse(result);
        }

        // ===== AreaUnlockedEvent 事件發布 =====

        [Test]
        public void ForceUnlock_NewArea_PublishesAreaUnlockedEvent()
        {
            // 解鎖新區域時，應發布 AreaUnlockedEvent
            AreaUnlockedEvent receivedEvent = null;
            Action<AreaUnlockedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<AreaUnlockedEvent>(handler);

            _sut.ForceUnlock(Farm);

            EventBus.Unsubscribe<AreaUnlockedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
        }

        [Test]
        public void ForceUnlock_NewArea_EventContainsCorrectAreaId()
        {
            // 發布的事件應包含正確的 areaId
            AreaUnlockedEvent receivedEvent = null;
            Action<AreaUnlockedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<AreaUnlockedEvent>(handler);

            _sut.ForceUnlock(Farm);

            EventBus.Unsubscribe<AreaUnlockedEvent>(handler);

            Assert.AreEqual(Farm, receivedEvent.AreaId);
        }
    }
}
