// DialogueCooldownManager 單元測試（Sprint 5 B10）。

using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.Dialogue;

namespace ProjectDR.Village.Tests
{
    [TestFixture]
    public class DialogueCooldownManagerTests
    {
        private const float BaseCD = 60f;
        private const string VCW = "village_chief_wife";

        private List<DialogueCooldownStartedEvent> _started;
        private List<DialogueCooldownCompletedEvent> _completed;
        private System.Action<DialogueCooldownStartedEvent> _startedHandler;
        private System.Action<DialogueCooldownCompletedEvent> _completedHandler;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _started = new List<DialogueCooldownStartedEvent>();
            _completed = new List<DialogueCooldownCompletedEvent>();
            _startedHandler = e => _started.Add(e);
            _completedHandler = e => _completed.Add(e);
            EventBus.Subscribe(_startedHandler);
            EventBus.Subscribe(_completedHandler);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Unsubscribe(_startedHandler);
            EventBus.Unsubscribe(_completedHandler);
        }

        [Test]
        public void Constructor_InvalidDuration_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new DialogueCooldownManager(0f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new DialogueCooldownManager(-1f));
        }

        [Test]
        public void StartCooldown_PublishesStartedEvent()
        {
            DialogueCooldownManager m = new DialogueCooldownManager(BaseCD);
            m.StartCooldown(VCW);

            Assert.AreEqual(1, _started.Count);
            Assert.AreEqual(VCW, _started[0].CharacterId);
            Assert.AreEqual(BaseCD, _started[0].DurationSeconds, 0.01f);
        }

        [Test]
        public void IsOnCooldown_WhileActive_ReturnsTrue()
        {
            DialogueCooldownManager m = new DialogueCooldownManager(BaseCD);
            m.StartCooldown(VCW);
            Assert.IsTrue(m.IsOnCooldown(VCW));

            m.Tick(30f);
            Assert.IsTrue(m.IsOnCooldown(VCW));
        }

        [Test]
        public void Tick_FinishesAfterBaseDuration()
        {
            DialogueCooldownManager m = new DialogueCooldownManager(BaseCD);
            m.StartCooldown(VCW);
            m.Tick(60f);
            Assert.IsFalse(m.IsOnCooldown(VCW));
            Assert.AreEqual(1, _completed.Count);
            Assert.AreEqual(VCW, _completed[0].CharacterId);
        }

        [Test]
        public void Tick_Working_DoublesActualDuration()
        {
            DialogueCooldownManager m = new DialogueCooldownManager(BaseCD);
            m.SetWorking(VCW, true);
            m.StartCooldown(VCW);

            // 工作中 → 60 秒只扣 30 秒剩餘，還需再 60 秒
            m.Tick(60f);
            Assert.IsTrue(m.IsOnCooldown(VCW));
            Assert.AreEqual(30f, m.GetRemainingSeconds(VCW), 0.01f);

            m.Tick(60f);
            Assert.IsFalse(m.IsOnCooldown(VCW));
        }

        [Test]
        public void Tick_SwitchWorking_AppliesNewRate()
        {
            DialogueCooldownManager m = new DialogueCooldownManager(BaseCD);
            m.StartCooldown(VCW);

            m.Tick(30f); // remain 30
            m.SetWorking(VCW, true);
            m.Tick(30f); // working: 30s 扣 15 → remain 15
            Assert.AreEqual(15f, m.GetRemainingSeconds(VCW), 0.01f);

            m.SetWorking(VCW, false);
            m.Tick(15f);
            Assert.IsFalse(m.IsOnCooldown(VCW));
        }

        [Test]
        public void StartCooldown_Working_StartedEventCarriesDoubledDuration()
        {
            DialogueCooldownManager m = new DialogueCooldownManager(BaseCD);
            m.SetWorking(VCW, true);
            m.StartCooldown(VCW);
            Assert.AreEqual(120f, _started[0].DurationSeconds, 0.01f);
        }

        [Test]
        public void StartCooldown_WhileActive_Resets()
        {
            DialogueCooldownManager m = new DialogueCooldownManager(BaseCD);
            m.StartCooldown(VCW);
            m.Tick(30f);
            m.StartCooldown(VCW); // reset
            Assert.AreEqual(BaseCD, m.GetRemainingSeconds(VCW), 0.01f);
            Assert.AreEqual(2, _started.Count);
        }

        [Test]
        public void MultipleCharacters_Independent()
        {
            DialogueCooldownManager m = new DialogueCooldownManager(BaseCD);
            m.StartCooldown("A");
            m.StartCooldown("B");
            m.Tick(60f);
            Assert.AreEqual(2, _completed.Count);
        }

        [Test]
        public void Dispose_StopsTicking()
        {
            DialogueCooldownManager m = new DialogueCooldownManager(BaseCD);
            m.StartCooldown(VCW);
            m.Dispose();
            Assert.DoesNotThrow(() => m.Tick(60f));
            Assert.AreEqual(0, _completed.Count);
        }

        [Test]
        public void Tick_NegativeDelta_Ignored()
        {
            DialogueCooldownManager m = new DialogueCooldownManager(BaseCD);
            m.StartCooldown(VCW);
            m.Tick(-10f);
            Assert.AreEqual(BaseCD, m.GetRemainingSeconds(VCW), 0.01f);
        }

        [Test]
        public void IsOnCooldown_UnknownCharacter_False()
        {
            DialogueCooldownManager m = new DialogueCooldownManager(BaseCD);
            Assert.IsFalse(m.IsOnCooldown("nope"));
        }
    }
}
