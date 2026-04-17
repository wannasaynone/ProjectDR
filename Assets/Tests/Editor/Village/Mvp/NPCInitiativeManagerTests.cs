using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village.Mvp;

namespace ProjectDR.Tests.Village.Mvp
{
    [TestFixture]
    public class NPCInitiativeManagerTests
    {
        private MvpConfig _config;
        private NPCInitiativeManager _sut;
        private List<MvpNpcInitiativeReadyEvent> _readyEvents;
        private List<MvpNpcInitiativeConsumedEvent> _consumedEvents;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _config = new MvpConfig(MvpTestConfig.MakeDefault());
            _sut = new NPCInitiativeManager(_config);
            _readyEvents = new List<MvpNpcInitiativeReadyEvent>();
            _consumedEvents = new List<MvpNpcInitiativeConsumedEvent>();
            EventBus.Subscribe<MvpNpcInitiativeReadyEvent>(OnReady);
            EventBus.Subscribe<MvpNpcInitiativeConsumedEvent>(OnConsumed);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Unsubscribe<MvpNpcInitiativeReadyEvent>(OnReady);
            EventBus.Unsubscribe<MvpNpcInitiativeConsumedEvent>(OnConsumed);
            _sut.Dispose();
            EventBus.ForceClearAll();
        }

        private void OnReady(MvpNpcInitiativeReadyEvent e) => _readyEvents.Add(e);
        private void OnConsumed(MvpNpcInitiativeConsumedEvent e) => _consumedEvents.Add(e);

        [Test]
        public void RegisterCharacter_NotReady()
        {
            _sut.RegisterCharacter("A");
            Assert.IsFalse(_sut.IsReady("A"));
        }

        [Test]
        public void Tick_ReachesZero_BecomesReady()
        {
            _sut.RegisterCharacter("A");
            _sut.Tick(45f);
            Assert.IsTrue(_sut.IsReady("A"));
            Assert.AreEqual(1, _readyEvents.Count);
            Assert.AreEqual("A", _readyEvents[0].CharacterId);
        }

        [Test]
        public void Tick_AlreadyReady_NoExtraEvent()
        {
            _sut.RegisterCharacter("A");
            _sut.Tick(45f);
            _sut.Tick(10f);
            Assert.AreEqual(1, _readyEvents.Count);
        }

        [Test]
        public void ConsumeInitiative_ResetsTimerAndRaisesConsumedEvent()
        {
            _sut.RegisterCharacter("A");
            _sut.Tick(45f);
            _sut.ConsumeInitiative("A");
            Assert.IsFalse(_sut.IsReady("A"));
            Assert.AreEqual(1, _consumedEvents.Count);
            // 重新計時：tick 44 秒仍未 Ready
            _sut.Tick(44f);
            Assert.IsFalse(_sut.IsReady("A"));
            _sut.Tick(2f);
            Assert.IsTrue(_sut.IsReady("A"));
        }

        [Test]
        public void ConsumeInitiative_NotReady_NoConsumedEvent()
        {
            _sut.RegisterCharacter("A");
            _sut.ConsumeInitiative("A");
            Assert.AreEqual(0, _consumedEvents.Count);
        }

        [Test]
        public void NpcArrivedEvent_AutoRegisters()
        {
            EventBus.Publish(new MvpNpcArrivedEvent { CharacterId = "X", DisplayName = "X" });
            Assert.IsTrue(new HashSet<string>(_sut.RegisteredCharacterIds).Contains("X"));
        }

        [Test]
        public void MultipleCharacters_IndependentTimers()
        {
            _sut.RegisterCharacter("A");
            _sut.RegisterCharacter("B");
            _sut.Tick(45f);
            Assert.IsTrue(_sut.IsReady("A"));
            Assert.IsTrue(_sut.IsReady("B"));
            Assert.AreEqual(2, _readyEvents.Count);
        }
    }
}
