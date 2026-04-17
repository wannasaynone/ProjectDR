using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village.Mvp;

namespace ProjectDR.Tests.Village.Mvp
{
    [TestFixture]
    public class PopulationManagerTests
    {
        private PopulationManager _sut;
        private List<MvpPopulationCapIncreasedEvent> _capEvents;
        private List<MvpPopulationChangedEvent> _countEvents;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _sut = new PopulationManager(0);
            _capEvents = new List<MvpPopulationCapIncreasedEvent>();
            _countEvents = new List<MvpPopulationChangedEvent>();
            EventBus.Subscribe<MvpPopulationCapIncreasedEvent>(OnCap);
            EventBus.Subscribe<MvpPopulationChangedEvent>(OnCount);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Unsubscribe<MvpPopulationCapIncreasedEvent>(OnCap);
            EventBus.Unsubscribe<MvpPopulationChangedEvent>(OnCount);
            EventBus.ForceClearAll();
        }

        private void OnCap(MvpPopulationCapIncreasedEvent e) => _capEvents.Add(e);
        private void OnCount(MvpPopulationChangedEvent e) => _countEvents.Add(e);

        [Test]
        public void Ctor_NegativeInitial_Throws()
        {
            Assert.Throws<ArgumentException>(() => new PopulationManager(-1));
        }

        [Test]
        public void Initial_CapAndCountZero()
        {
            Assert.AreEqual(0, _sut.Cap);
            Assert.AreEqual(0, _sut.Count);
            Assert.IsFalse(_sut.HasVacancy);
        }

        [Test]
        public void IncreaseCap_RaisesEventAndUpdatesCap()
        {
            _sut.IncreaseCap(1);
            Assert.AreEqual(1, _sut.Cap);
            Assert.IsTrue(_sut.HasVacancy);
            Assert.AreEqual(1, _capEvents.Count);
            Assert.AreEqual(1, _capEvents[0].Increment);
            Assert.AreEqual(1, _capEvents[0].NewCap);
        }

        [Test]
        public void IncreaseCap_NonPositive_Throws()
        {
            Assert.Throws<ArgumentException>(() => _sut.IncreaseCap(0));
            Assert.Throws<ArgumentException>(() => _sut.IncreaseCap(-1));
        }

        [Test]
        public void TryIncrementCount_NoVacancy_Fails()
        {
            Assert.IsFalse(_sut.TryIncrementCount());
            Assert.AreEqual(0, _sut.Count);
        }

        [Test]
        public void TryIncrementCount_WithVacancy_Succeeds()
        {
            _sut.IncreaseCap(2);
            bool ok = _sut.TryIncrementCount();
            Assert.IsTrue(ok);
            Assert.AreEqual(1, _sut.Count);
            Assert.AreEqual(1, _countEvents.Count);
            Assert.AreEqual(1, _countEvents[0].NewCount);
            Assert.AreEqual(2, _countEvents[0].CurrentCap);
        }

        [Test]
        public void TryIncrementCount_AtCap_Fails()
        {
            _sut.IncreaseCap(1);
            _sut.TryIncrementCount();
            bool ok = _sut.TryIncrementCount();
            Assert.IsFalse(ok);
        }
    }
}
