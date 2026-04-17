using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village.Mvp;

namespace ProjectDR.Tests.Village.Mvp
{
    [TestFixture]
    public class NpcArrivalManagerTests
    {
        private PopulationManager _population;
        private MvpConfig _config;
        private NpcArrivalManager _sut;
        private List<MvpNpcArrivedEvent> _events;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _population = new PopulationManager(0);
            _config = new MvpConfig(MvpTestConfig.MakeDefault());
            _sut = new NpcArrivalManager(_population, _config, new ZeroRandomSource());
            _events = new List<MvpNpcArrivedEvent>();
            EventBus.Subscribe<MvpNpcArrivedEvent>(OnEvt);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Unsubscribe<MvpNpcArrivedEvent>(OnEvt);
            _sut.Dispose();
            EventBus.ForceClearAll();
        }

        private void OnEvt(MvpNpcArrivedEvent e) => _events.Add(e);

        [Test]
        public void NoArrivalsInitially()
        {
            Assert.AreEqual(0, _sut.ArrivedCharacters.Count);
        }

        [Test]
        public void CapIncrease_TriggersArrival()
        {
            _population.IncreaseCap(1);
            Assert.AreEqual(1, _sut.ArrivedCharacters.Count);
            Assert.AreEqual("A", _sut.ArrivedCharacters[0].characterId); // ZeroRandom 選第一位
            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual("A", _events[0].CharacterId);
            Assert.AreEqual(1, _population.Count);
        }

        [Test]
        public void CapIncreaseMultiple_TriggersMultipleArrivals()
        {
            _population.IncreaseCap(3);
            Assert.AreEqual(3, _sut.ArrivedCharacters.Count);
            Assert.AreEqual(3, _population.Count);
            Assert.AreEqual(3, _events.Count);
            // ZeroRandom 每次選第一個 (未到訪池中第一個)：A, B, C
            Assert.AreEqual("A", _events[0].CharacterId);
            Assert.AreEqual("B", _events[1].CharacterId);
            Assert.AreEqual("C", _events[2].CharacterId);
        }

        [Test]
        public void CapExceedsPoolSize_StopsAtPoolExhaustion()
        {
            _population.IncreaseCap(10); // 但池只有 3 位
            Assert.AreEqual(3, _sut.ArrivedCharacters.Count);
            Assert.AreEqual(3, _population.Count);
        }

        [Test]
        public void NoDuplicateArrivals()
        {
            _population.IncreaseCap(1);
            _population.IncreaseCap(1);
            _population.IncreaseCap(1);
            Assert.AreEqual(3, _events.Count);
            HashSet<string> ids = new HashSet<string>();
            foreach (MvpNpcArrivedEvent e in _events) ids.Add(e.CharacterId);
            Assert.AreEqual(3, ids.Count);
        }

        [Test]
        public void Dispose_StopsListening()
        {
            _sut.Dispose();
            _population.IncreaseCap(1);
            Assert.AreEqual(0, _events.Count);
        }
    }
}
