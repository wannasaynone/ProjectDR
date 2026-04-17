using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village.Mvp;

namespace ProjectDR.Tests.Village.Mvp
{
    [TestFixture]
    public class ColdStatusSystemTests
    {
        private ColdStatusSystem _sut;
        private List<MvpColdStateChangedEvent> _events;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _sut = new ColdStatusSystem();
            _events = new List<MvpColdStateChangedEvent>();
            EventBus.Subscribe<MvpColdStateChangedEvent>(OnEvt);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Unsubscribe<MvpColdStateChangedEvent>(OnEvt);
            _sut.Dispose();
            EventBus.ForceClearAll();
        }

        private void OnEvt(MvpColdStateChangedEvent e) => _events.Add(e);

        [Test]
        public void Initial_NotCold()
        {
            Assert.IsFalse(_sut.IsCold);
        }

        [Test]
        public void FireExtinguished_BecomesCold()
        {
            EventBus.Publish(new MvpFireStateChangedEvent { IsLit = false, RemainingSeconds = 0f });
            Assert.IsTrue(_sut.IsCold);
            Assert.AreEqual(1, _events.Count);
            Assert.IsTrue(_events[0].IsCold);
        }

        [Test]
        public void FireLit_ClearsCold()
        {
            // 先變寒冷
            EventBus.Publish(new MvpFireStateChangedEvent { IsLit = false, RemainingSeconds = 0f });
            Assert.IsTrue(_sut.IsCold);
            _events.Clear();

            // 再次點燃
            EventBus.Publish(new MvpFireStateChangedEvent { IsLit = true, RemainingSeconds = 60f });
            Assert.IsFalse(_sut.IsCold);
            Assert.AreEqual(1, _events.Count);
            Assert.IsFalse(_events[0].IsCold);
        }

        [Test]
        public void NoDuplicateEvent_IfSameState()
        {
            // 連續兩次熄滅，只發一次事件
            EventBus.Publish(new MvpFireStateChangedEvent { IsLit = false });
            EventBus.Publish(new MvpFireStateChangedEvent { IsLit = false });
            Assert.AreEqual(1, _events.Count);
        }

        [Test]
        public void Dispose_UnsubscribesFromEvents()
        {
            _sut.Dispose();
            _events.Clear();
            EventBus.Publish(new MvpFireStateChangedEvent { IsLit = false });
            Assert.AreEqual(0, _events.Count);
        }
    }
}
