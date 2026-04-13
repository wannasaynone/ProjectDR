using System;
using System.Linq;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using static ProjectDR.Village.AreaIds;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// VillageNavigationManager 的單元測試。
    /// 測試對象：可導航區域查詢、導航至區域、返回主畫面、CurrentArea 屬性、事件發布。
    /// 此測試不依賴 MonoBehaviour 或 Unity 場景，為純邏輯測試。
    /// </summary>
    [TestFixture]
    public class VillageNavigationManagerTests
    {
        private VillageProgressionManager _progressionManager;
        private VillageNavigationManager _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _progressionManager = new VillageProgressionManager();
            _sut = new VillageNavigationManager(_progressionManager);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== GetNavigableAreas =====

        [Test]
        public void GetNavigableAreas_OnInitialState_ContainsOnlyStorage()
        {
            // 初始狀態下，可導航區域應只有 Storage
            System.Collections.Generic.IReadOnlyList<string> areas = _sut.GetNavigableAreas();

            Assert.AreEqual(1, areas.Count);
            Assert.IsTrue(areas.Contains(Storage));
        }

        [Test]
        public void GetNavigableAreas_AfterAreaUnlocked_ContainsNewArea()
        {
            // 解鎖新區域後，可導航區域應包含該區域
            _progressionManager.ForceUnlock(Farm);

            System.Collections.Generic.IReadOnlyList<string> areas = _sut.GetNavigableAreas();

            Assert.IsTrue(areas.Contains(Farm));
        }

        // ===== CurrentArea =====

        [Test]
        public void CurrentArea_OnInitialState_IsNull()
        {
            // 初始狀態下，沒有當前區域（位於主畫面 Hub）
            Assert.IsNull(_sut.CurrentArea);
        }

        [Test]
        public void CurrentArea_AfterNavigateTo_ReturnsTargetArea()
        {
            // 導航至 Storage 後，CurrentArea 應為 Storage
            _sut.NavigateTo(Storage);

            Assert.AreEqual(Storage, _sut.CurrentArea);
        }

        [Test]
        public void CurrentArea_AfterReturnToHub_IsNull()
        {
            // 返回主畫面後，CurrentArea 應為 null
            _sut.NavigateTo(Storage);
            _sut.ReturnToHub();

            Assert.IsNull(_sut.CurrentArea);
        }

        // ===== NavigateTo =====

        [Test]
        public void NavigateTo_UnlockedArea_ReturnsTrue()
        {
            // 導航至已解鎖區域應回傳 true
            bool result = _sut.NavigateTo(Storage);

            Assert.IsTrue(result);
        }

        [Test]
        public void NavigateTo_LockedArea_ReturnsFalse()
        {
            // 導航至未解鎖區域應回傳 false
            bool result = _sut.NavigateTo("Blacksmith");

            Assert.IsFalse(result);
        }

        [Test]
        public void NavigateTo_LockedArea_CurrentAreaRemainsUnchanged()
        {
            // 導航至未解鎖區域失敗後，CurrentArea 不應改變
            _sut.NavigateTo(Storage);
            _sut.NavigateTo("Blacksmith");

            Assert.AreEqual(Storage, _sut.CurrentArea);
        }

        [Test]
        public void NavigateTo_CurrentArea_ReturnsFalse()
        {
            // 導航至目前已在的區域應回傳 false
            _sut.NavigateTo(Storage);
            bool result = _sut.NavigateTo(Storage);

            Assert.IsFalse(result);
        }

        [Test]
        public void NavigateTo_CurrentArea_DoesNotChangeCurrentArea()
        {
            // 重複導航至相同區域，CurrentArea 不應改變（仍為同一區域）
            _sut.NavigateTo(Storage);
            _sut.NavigateTo(Storage);

            Assert.AreEqual(Storage, _sut.CurrentArea);
        }

        [Test]
        public void NavigateTo_NonExistentArea_ReturnsFalse()
        {
            // 導航至不存在的 areaId 應回傳 false
            bool result = _sut.NavigateTo("ThisAreaDoesNotExist");

            Assert.IsFalse(result);
        }

        // ===== ReturnToHub =====

        [Test]
        public void ReturnToHub_WhenInArea_SetsCurrentAreaToNull()
        {
            // 從區域返回主畫面，CurrentArea 應為 null
            _sut.NavigateTo(Storage);
            _sut.ReturnToHub();

            Assert.IsNull(_sut.CurrentArea);
        }

        [Test]
        public void ReturnToHub_WhenAlreadyAtHub_DoesNotThrow()
        {
            // 在主畫面重複呼叫 ReturnToHub 不應拋出例外
            Assert.DoesNotThrow(() => _sut.ReturnToHub());
        }

        // ===== NavigatedToAreaEvent 事件 =====

        [Test]
        public void NavigateTo_SuccessfulNavigation_PublishesNavigatedToAreaEvent()
        {
            // 成功導航至區域應發布 NavigatedToAreaEvent
            NavigatedToAreaEvent receivedEvent = null;
            Action<NavigatedToAreaEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<NavigatedToAreaEvent>(handler);

            _sut.NavigateTo(Storage);

            EventBus.Unsubscribe<NavigatedToAreaEvent>(handler);

            Assert.IsNotNull(receivedEvent);
        }

        [Test]
        public void NavigateTo_SuccessfulNavigation_EventContainsCorrectAreaId()
        {
            // 發布的事件應包含正確的 areaId
            NavigatedToAreaEvent receivedEvent = null;
            Action<NavigatedToAreaEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<NavigatedToAreaEvent>(handler);

            _sut.NavigateTo(Storage);

            EventBus.Unsubscribe<NavigatedToAreaEvent>(handler);

            Assert.AreEqual(Storage, receivedEvent.AreaId);
        }

        [Test]
        public void NavigateTo_FailedNavigation_DoesNotPublishNavigatedToAreaEvent()
        {
            // 導航失敗時不應發布事件
            bool eventPublished = false;
            Action<NavigatedToAreaEvent> handler = (e) => { eventPublished = true; };
            EventBus.Subscribe<NavigatedToAreaEvent>(handler);

            _sut.NavigateTo("Blacksmith");

            EventBus.Unsubscribe<NavigatedToAreaEvent>(handler);

            Assert.IsFalse(eventPublished);
        }

        // ===== ReturnedToHubEvent 事件 =====

        [Test]
        public void ReturnToHub_WhenInArea_PublishesReturnedToHubEvent()
        {
            // 從區域返回主畫面應發布 ReturnedToHubEvent
            _sut.NavigateTo(Storage);

            bool eventPublished = false;
            Action<ReturnedToHubEvent> handler = (e) => { eventPublished = true; };
            EventBus.Subscribe<ReturnedToHubEvent>(handler);

            _sut.ReturnToHub();

            EventBus.Unsubscribe<ReturnedToHubEvent>(handler);

            Assert.IsTrue(eventPublished);
        }

        [Test]
        public void ReturnToHub_WhenAlreadyAtHub_DoesNotPublishReturnedToHubEvent()
        {
            // 在主畫面呼叫 ReturnToHub 不應發布事件
            bool eventPublished = false;
            Action<ReturnedToHubEvent> handler = (e) => { eventPublished = true; };
            EventBus.Subscribe<ReturnedToHubEvent>(handler);

            _sut.ReturnToHub();

            EventBus.Unsubscribe<ReturnedToHubEvent>(handler);

            Assert.IsFalse(eventPublished);
        }
    }
}
