using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village.Mvp;

namespace ProjectDR.Tests.Village.Mvp
{
    [TestFixture]
    public class FireSystemTests
    {
        private ResourceManager _resource;
        private MvpConfig _config;
        private FireSystem _sut;

        private List<MvpFireStateChangedEvent> _stateEvents;
        private List<MvpFireRemainingChangedEvent> _remainEvents;
        private List<MvpFireExtendedEvent> _extendEvents;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _resource = new ResourceManager();
            _config = new MvpConfig(MvpTestConfig.MakeDefault());
            _sut = new FireSystem(_resource, _config);

            _stateEvents = new List<MvpFireStateChangedEvent>();
            _remainEvents = new List<MvpFireRemainingChangedEvent>();
            _extendEvents = new List<MvpFireExtendedEvent>();
            EventBus.Subscribe<MvpFireStateChangedEvent>(OnState);
            EventBus.Subscribe<MvpFireRemainingChangedEvent>(OnRemain);
            EventBus.Subscribe<MvpFireExtendedEvent>(OnExtend);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Unsubscribe<MvpFireStateChangedEvent>(OnState);
            EventBus.Unsubscribe<MvpFireRemainingChangedEvent>(OnRemain);
            EventBus.Unsubscribe<MvpFireExtendedEvent>(OnExtend);
            EventBus.ForceClearAll();
        }

        private void OnState(MvpFireStateChangedEvent e) => _stateEvents.Add(e);
        private void OnRemain(MvpFireRemainingChangedEvent e) => _remainEvents.Add(e);
        private void OnExtend(MvpFireExtendedEvent e) => _extendEvents.Add(e);

        [Test]
        public void Initial_NotLit_NotUnlocked()
        {
            Assert.IsFalse(_sut.IsLit);
            Assert.IsFalse(_sut.IsUnlocked);
            Assert.IsFalse(_sut.HasEverBeenLit);
        }

        [Test]
        public void IsUnlocked_AfterWoodReachesThreshold()
        {
            _resource.Add(MvpResourceIds.Wood, 5);
            Assert.IsTrue(_sut.IsUnlocked);
        }

        [Test]
        public void TryLight_NotUnlocked_Fails()
        {
            Assert.IsFalse(_sut.TryLight());
        }

        [Test]
        public void TryLight_Unlocked_Success()
        {
            _resource.Add(MvpResourceIds.Wood, 5);
            bool ok = _sut.TryLight();
            Assert.IsTrue(ok);
            Assert.IsTrue(_sut.IsLit);
            Assert.IsTrue(_sut.HasEverBeenLit);
            Assert.AreEqual(60f, _sut.RemainingSeconds, 0.001f);
            Assert.AreEqual(4, _resource.GetAmount(MvpResourceIds.Wood)); // 扣 1
            Assert.AreEqual(1, _stateEvents.Count);
            Assert.IsTrue(_stateEvents[0].IsLit);
        }

        [Test]
        public void TryLight_AlreadyLit_Fails()
        {
            _resource.Add(MvpResourceIds.Wood, 5);
            _sut.TryLight();
            Assert.IsFalse(_sut.TryLight());
        }

        [Test]
        public void Tick_DecreasesRemaining()
        {
            _resource.Add(MvpResourceIds.Wood, 5);
            _sut.TryLight();
            _sut.Tick(10f);
            Assert.AreEqual(50f, _sut.RemainingSeconds, 0.001f);
            Assert.IsTrue(_sut.IsLit);
        }

        [Test]
        public void Tick_Extinguishes_AtZero()
        {
            _resource.Add(MvpResourceIds.Wood, 5);
            _sut.TryLight();
            _stateEvents.Clear();

            _sut.Tick(60f);

            Assert.IsFalse(_sut.IsLit);
            Assert.AreEqual(0f, _sut.RemainingSeconds, 0.001f);
            Assert.AreEqual(1, _stateEvents.Count);
            Assert.IsFalse(_stateEvents[0].IsLit);
        }

        [Test]
        public void Tick_Exceeds_ClampsToZero()
        {
            _resource.Add(MvpResourceIds.Wood, 5);
            _sut.TryLight();
            _sut.Tick(120f);
            Assert.AreEqual(0f, _sut.RemainingSeconds, 0.001f);
        }

        [Test]
        public void TryExtend_NotLit_Fails()
        {
            Assert.IsFalse(_sut.TryExtend());
        }

        [Test]
        public void TryExtend_Lit_AddsDurationAndConsumesWood()
        {
            _resource.Add(MvpResourceIds.Wood, 10); // 5 + 額外 5
            _sut.TryLight(); // 扣 1 → 9
            _sut.TryExtend(); // 扣 1 → 8，+60 秒
            Assert.AreEqual(120f, _sut.RemainingSeconds, 0.001f);
            Assert.AreEqual(8, _resource.GetAmount(MvpResourceIds.Wood));
            Assert.AreEqual(1, _extendEvents.Count);
            Assert.AreEqual(120f, _extendEvents[0].NewRemainingSeconds, 0.001f);
        }

        [Test]
        public void TryExtend_InsufficientWood_Fails()
        {
            _resource.Add(MvpResourceIds.Wood, 5);
            _sut.TryLight(); // 扣 1 → 4
            _resource.TrySpend(MvpResourceIds.Wood, 4); // 歸 0
            bool ok = _sut.TryExtend();
            Assert.IsFalse(ok);
        }
    }
}
