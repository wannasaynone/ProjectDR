using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village.Mvp;

namespace ProjectDR.Tests.Village.Mvp
{
    [TestFixture]
    public class ResourceManagerTests
    {
        private ResourceManager _sut;
        private List<MvpResourceChangedEvent> _events;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _sut = new ResourceManager();
            _events = new List<MvpResourceChangedEvent>();
            EventBus.Subscribe<MvpResourceChangedEvent>(OnEvent);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Unsubscribe<MvpResourceChangedEvent>(OnEvent);
            EventBus.ForceClearAll();
        }

        private void OnEvent(MvpResourceChangedEvent e) => _events.Add(e);

        [Test]
        public void GetAmount_Initial_ReturnsZero()
        {
            Assert.AreEqual(0, _sut.GetAmount(MvpResourceIds.Wood));
        }

        [Test]
        public void Add_Positive_Increases()
        {
            _sut.Add(MvpResourceIds.Wood, 3);
            Assert.AreEqual(3, _sut.GetAmount(MvpResourceIds.Wood));
        }

        [Test]
        public void Add_PublishesEvent_WithDelta()
        {
            _sut.Add(MvpResourceIds.Wood, 5);
            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual(MvpResourceIds.Wood, _events[0].ResourceId);
            Assert.AreEqual(5, _events[0].NewAmount);
            Assert.AreEqual(5, _events[0].Delta);
        }

        [Test]
        public void Add_NonPositive_Throws()
        {
            Assert.Throws<ArgumentException>(() => _sut.Add(MvpResourceIds.Wood, 0));
            Assert.Throws<ArgumentException>(() => _sut.Add(MvpResourceIds.Wood, -1));
        }

        [Test]
        public void Add_NullId_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.Add(null, 1));
        }

        [Test]
        public void Add_EmptyId_Throws()
        {
            Assert.Throws<ArgumentException>(() => _sut.Add("", 1));
        }

        [Test]
        public void TrySpend_EnoughResource_Success()
        {
            _sut.Add(MvpResourceIds.Wood, 5);
            bool ok = _sut.TrySpend(MvpResourceIds.Wood, 3);
            Assert.IsTrue(ok);
            Assert.AreEqual(2, _sut.GetAmount(MvpResourceIds.Wood));
        }

        [Test]
        public void TrySpend_InsufficientResource_FailsAndDoesNotDeduct()
        {
            _sut.Add(MvpResourceIds.Wood, 2);
            bool ok = _sut.TrySpend(MvpResourceIds.Wood, 5);
            Assert.IsFalse(ok);
            Assert.AreEqual(2, _sut.GetAmount(MvpResourceIds.Wood));
            // 只有 Add 的事件，沒有 Spend 事件
            Assert.AreEqual(1, _events.Count);
        }

        [Test]
        public void TrySpend_PublishesEventWithNegativeDelta()
        {
            _sut.Add(MvpResourceIds.Wood, 5);
            _events.Clear();
            _sut.TrySpend(MvpResourceIds.Wood, 2);
            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual(-2, _events[0].Delta);
            Assert.AreEqual(3, _events[0].NewAmount);
        }

        [Test]
        public void Has_ReturnsTrueWhenEnough()
        {
            _sut.Add(MvpResourceIds.Wood, 5);
            Assert.IsTrue(_sut.Has(MvpResourceIds.Wood, 5));
            Assert.IsTrue(_sut.Has(MvpResourceIds.Wood, 3));
            Assert.IsFalse(_sut.Has(MvpResourceIds.Wood, 6));
        }

        [Test]
        public void Has_NegativeRequired_Throws()
        {
            Assert.Throws<ArgumentException>(() => _sut.Has(MvpResourceIds.Wood, -1));
        }
    }
}
