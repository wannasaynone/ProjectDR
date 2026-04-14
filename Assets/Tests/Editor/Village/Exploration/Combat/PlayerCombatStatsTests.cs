using System;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village.Exploration.Combat;

namespace ProjectDR.Tests.Village.Exploration.Combat
{
    [TestFixture]
    public class PlayerCombatStatsTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        [Test]
        public void Constructor_ValidParams_SetsProperties()
        {
            var stats = new PlayerCombatStats(20, 5, 2, 10);

            Assert.AreEqual(20, stats.MaxHp);
            Assert.AreEqual(20, stats.CurrentHp);
            Assert.AreEqual(5, stats.Atk);
            Assert.AreEqual(2, stats.Def);
            Assert.AreEqual(10, stats.Spd);
            Assert.IsFalse(stats.IsDead);
        }

        [Test]
        public void Constructor_ZeroMaxHp_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PlayerCombatStats(0, 5, 2, 10));
        }

        [Test]
        public void Constructor_NegativeMaxHp_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PlayerCombatStats(-1, 5, 2, 10));
        }

        [Test]
        public void TakeDamage_PositiveDamage_ReducesHp()
        {
            var stats = new PlayerCombatStats(20, 5, 2, 10);

            int actual = stats.TakeDamage(5);

            Assert.AreEqual(5, actual);
            Assert.AreEqual(15, stats.CurrentHp);
        }

        [Test]
        public void TakeDamage_DamageExceedsHp_ClampsToZero()
        {
            var stats = new PlayerCombatStats(10, 5, 2, 10);

            int actual = stats.TakeDamage(15);

            Assert.AreEqual(10, actual);
            Assert.AreEqual(0, stats.CurrentHp);
            Assert.IsTrue(stats.IsDead);
        }

        [Test]
        public void TakeDamage_ZeroDamage_NoChange()
        {
            var stats = new PlayerCombatStats(20, 5, 2, 10);

            int actual = stats.TakeDamage(0);

            Assert.AreEqual(0, actual);
            Assert.AreEqual(20, stats.CurrentHp);
        }

        [Test]
        public void TakeDamage_NegativeDamage_NoChange()
        {
            var stats = new PlayerCombatStats(20, 5, 2, 10);

            int actual = stats.TakeDamage(-5);

            Assert.AreEqual(0, actual);
            Assert.AreEqual(20, stats.CurrentHp);
        }

        [Test]
        public void TakeDamage_PublishesPlayerHpChangedEvent()
        {
            var stats = new PlayerCombatStats(20, 5, 2, 10);
            PlayerHpChangedEvent received = null;
            Action<PlayerHpChangedEvent> handler = (e) => { received = e; };
            EventBus.Subscribe<PlayerHpChangedEvent>(handler);

            stats.TakeDamage(7);

            EventBus.Unsubscribe<PlayerHpChangedEvent>(handler);

            Assert.IsNotNull(received);
            Assert.AreEqual(13, received.CurrentHp);
            Assert.AreEqual(20, received.MaxHp);
            Assert.AreEqual(7, received.DamageDealt);
        }

        [Test]
        public void TakeDamage_LethalDamage_PublishesPlayerDiedEvent()
        {
            var stats = new PlayerCombatStats(5, 5, 2, 10);
            PlayerDiedEvent died = null;
            Action<PlayerDiedEvent> handler = (e) => { died = e; };
            EventBus.Subscribe<PlayerDiedEvent>(handler);

            stats.TakeDamage(5);

            EventBus.Unsubscribe<PlayerDiedEvent>(handler);

            Assert.IsNotNull(died);
            Assert.IsTrue(stats.IsDead);
        }

        [Test]
        public void TakeDamage_NonLethalDamage_DoesNotPublishPlayerDiedEvent()
        {
            var stats = new PlayerCombatStats(20, 5, 2, 10);
            bool diedFired = false;
            Action<PlayerDiedEvent> handler = (e) => { diedFired = true; };
            EventBus.Subscribe<PlayerDiedEvent>(handler);

            stats.TakeDamage(5);

            EventBus.Unsubscribe<PlayerDiedEvent>(handler);

            Assert.IsFalse(diedFired);
        }

        [Test]
        public void Heal_IncreasesHp()
        {
            var stats = new PlayerCombatStats(20, 5, 2, 10);
            stats.TakeDamage(10);

            int healed = stats.Heal(5);

            Assert.AreEqual(5, healed);
            Assert.AreEqual(15, stats.CurrentHp);
        }

        [Test]
        public void Heal_ClampsToMaxHp()
        {
            var stats = new PlayerCombatStats(20, 5, 2, 10);
            stats.TakeDamage(3);

            int healed = stats.Heal(10);

            Assert.AreEqual(3, healed);
            Assert.AreEqual(20, stats.CurrentHp);
        }

        [Test]
        public void Heal_AtFullHp_ReturnsZero()
        {
            var stats = new PlayerCombatStats(20, 5, 2, 10);

            int healed = stats.Heal(5);

            Assert.AreEqual(0, healed);
            Assert.AreEqual(20, stats.CurrentHp);
        }

        [Test]
        public void Heal_ZeroAmount_ReturnsZero()
        {
            var stats = new PlayerCombatStats(20, 5, 2, 10);
            stats.TakeDamage(5);

            int healed = stats.Heal(0);

            Assert.AreEqual(0, healed);
        }
    }
}
