using System;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village.Exploration.Combat;

namespace ProjectDR.Tests.Village.Exploration.Combat
{
    [TestFixture]
    public class SwordAttackTests
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

        private SwordAttack CreateDefault()
        {
            // angleHalf=45, range=1.5, baseCooldown=0.8, spdFactor=0.02, spd=10
            return new SwordAttack(45f, 1.5f, 0.8f, 0.02f, 10);
        }

        [Test]
        public void Constructor_ValidParams_SetsCooldown()
        {
            var sword = CreateDefault();
            // cooldown = 0.8 - 10 * 0.02 = 0.6
            Assert.AreEqual(0.6f, sword.Cooldown, 0.001f);
        }

        [Test]
        public void Constructor_HighSpd_ClampsMinCooldown()
        {
            // baseCooldown=0.8, spdFactor=0.02, spd=100 -> 0.8-2.0=-1.2 -> clamp to 0.1
            var sword = new SwordAttack(45f, 1.5f, 0.8f, 0.02f, 100);
            Assert.AreEqual(0.1f, sword.Cooldown, 0.001f);
        }

        [Test]
        public void Constructor_ZeroRange_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SwordAttack(45f, 0f, 0.8f, 0.02f, 10));
        }

        [Test]
        public void Constructor_ZeroBaseCooldown_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SwordAttack(45f, 1.5f, 0f, 0.02f, 10));
        }

        [Test]
        public void TryAttack_WhenReady_ReturnsTrue()
        {
            var sword = CreateDefault();

            bool result = sword.TryAttack(Vector2.zero, Vector2.right);

            Assert.IsTrue(result);
        }

        [Test]
        public void TryAttack_PublishesPlayerAttackEvent()
        {
            var sword = CreateDefault();
            PlayerAttackEvent received = null;
            Action<PlayerAttackEvent> handler = (e) => { received = e; };
            EventBus.Subscribe<PlayerAttackEvent>(handler);

            sword.TryAttack(new Vector2(1f, 2f), Vector2.right);

            EventBus.Unsubscribe<PlayerAttackEvent>(handler);

            Assert.IsNotNull(received);
            Assert.AreEqual(1f, received.Origin.x, 0.001f);
            Assert.AreEqual(2f, received.Origin.y, 0.001f);
            Assert.AreEqual(45f, received.AngleHalf, 0.001f);
            Assert.AreEqual(1.5f, received.Range, 0.001f);
        }

        [Test]
        public void TryAttack_OnCooldown_ReturnsFalse()
        {
            var sword = CreateDefault();
            sword.TryAttack(Vector2.zero, Vector2.right);

            bool result = sword.TryAttack(Vector2.zero, Vector2.right);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryAttack_AfterCooldownExpires_CanAttackAgain()
        {
            var sword = CreateDefault();
            sword.TryAttack(Vector2.zero, Vector2.right);

            // Advance past cooldown
            sword.Update(sword.Cooldown + 0.1f);

            bool result = sword.TryAttack(Vector2.zero, Vector2.right);

            Assert.IsTrue(result);
        }

        [Test]
        public void TryAttack_ZeroDirection_ReturnsFalse()
        {
            var sword = CreateDefault();

            bool result = sword.TryAttack(Vector2.zero, Vector2.zero);

            Assert.IsFalse(result);
        }

        [Test]
        public void CanAttack_InitiallyTrue()
        {
            var sword = CreateDefault();
            Assert.IsTrue(sword.CanAttack);
        }

        [Test]
        public void CanAttack_FalseDuringCooldown()
        {
            var sword = CreateDefault();
            sword.TryAttack(Vector2.zero, Vector2.right);

            Assert.IsFalse(sword.CanAttack);
        }

        [Test]
        public void CanAttack_TrueAfterCooldown()
        {
            var sword = CreateDefault();
            sword.TryAttack(Vector2.zero, Vector2.right);
            sword.Update(sword.Cooldown + 0.1f);

            Assert.IsTrue(sword.CanAttack);
        }

        // --- IsInSector tests ---

        [Test]
        public void IsInSector_TargetDirectlyAhead_ReturnsTrue()
        {
            var sword = CreateDefault();
            Vector2 origin = Vector2.zero;
            Vector2 direction = Vector2.right;
            Vector2 target = new Vector2(1f, 0f); // directly right, within range

            Assert.IsTrue(sword.IsInSector(origin, direction, target));
        }

        [Test]
        public void IsInSector_TargetOutOfRange_ReturnsFalse()
        {
            var sword = CreateDefault();
            Vector2 origin = Vector2.zero;
            Vector2 direction = Vector2.right;
            Vector2 target = new Vector2(5f, 0f); // beyond range 1.5

            Assert.IsFalse(sword.IsInSector(origin, direction, target));
        }

        [Test]
        public void IsInSector_TargetOutsideAngle_ReturnsFalse()
        {
            var sword = CreateDefault();
            Vector2 origin = Vector2.zero;
            Vector2 direction = Vector2.right;
            Vector2 target = new Vector2(0f, 1f); // 90 degrees off, outside 45 degree half-angle

            Assert.IsFalse(sword.IsInSector(origin, direction, target));
        }

        [Test]
        public void IsInSector_TargetAtEdgeOfAngle_ReturnsTrue()
        {
            var sword = CreateDefault(); // angleHalf = 45
            Vector2 origin = Vector2.zero;
            Vector2 direction = Vector2.right;
            // Target at 44 degrees, within angle
            float rad = 44f * Mathf.Deg2Rad;
            Vector2 target = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            Assert.IsTrue(sword.IsInSector(origin, direction, target));
        }

        [Test]
        public void IsInSector_TargetBehind_ReturnsFalse()
        {
            var sword = CreateDefault();
            Vector2 origin = Vector2.zero;
            Vector2 direction = Vector2.right;
            Vector2 target = new Vector2(-1f, 0f); // behind

            Assert.IsFalse(sword.IsInSector(origin, direction, target));
        }

        [Test]
        public void IsInSector_TargetAtOrigin_ReturnsFalse()
        {
            var sword = CreateDefault();

            Assert.IsFalse(sword.IsInSector(Vector2.zero, Vector2.right, Vector2.zero));
        }

        [Test]
        public void Update_ReducesCooldown()
        {
            var sword = CreateDefault();
            sword.TryAttack(Vector2.zero, Vector2.right);

            float before = sword.CooldownRemaining;
            sword.Update(0.1f);

            Assert.Less(sword.CooldownRemaining, before);
        }

        [Test]
        public void Update_CooldownDoesNotGoBelowZero()
        {
            var sword = CreateDefault();
            sword.TryAttack(Vector2.zero, Vector2.right);

            sword.Update(100f);

            Assert.AreEqual(0f, sword.CooldownRemaining, 0.001f);
        }
    }
}
