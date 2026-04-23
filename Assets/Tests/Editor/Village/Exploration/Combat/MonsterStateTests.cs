using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village.Exploration.Combat;

namespace ProjectDR.Tests.Village.Exploration.Combat
{
    [TestFixture]
    public class MonsterStateTests
    {
        private MonsterTypeData CreateSlimeType()
        {
            var data = new MonsterData
            {
                id = 1,
                type_id = "Slime",
                max_hp = 6,
                atk = 3,
                def = 1,
                spd = 4,
                move_cooldown_seconds = 2.0f,
                vision_range = 3,
                attack_range = 1,
                attack_angle_degrees_half = 45f,
                attack_prepare_seconds = 1.0f,
                attack_cooldown_seconds = 1.5f,
                color_r = 0.2f,
                color_g = 0.8f,
                color_b = 0.2f,
                color_a = 1f
            };
            return new MonsterTypeData(data);
        }

        [SetUp]
        public void SetUp()
        {
            MonsterState.ResetIdCounter();
        }

        [Test]
        public void Constructor_SetsInitialValues()
        {
            var type = CreateSlimeType();
            var state = new MonsterState(type, new Vector2Int(3, 4));

            Assert.AreEqual("Slime", state.TypeData.TypeId);
            Assert.AreEqual(new Vector2Int(3, 4), state.Position);
            Assert.AreEqual(6, state.CurrentHp);
            Assert.IsFalse(state.IsDead);
            Assert.AreEqual(MonsterAIState.Idle, state.AIState);
        }

        [Test]
        public void Constructor_AssignsUniqueIds()
        {
            var type = CreateSlimeType();
            var s1 = new MonsterState(type, Vector2Int.zero);
            var s2 = new MonsterState(type, Vector2Int.zero);

            Assert.AreNotEqual(s1.Id, s2.Id);
        }

        [Test]
        public void TakeDamage_ReducesHp()
        {
            var type = CreateSlimeType();
            var state = new MonsterState(type, Vector2Int.zero);

            int actual = state.TakeDamage(3);

            Assert.AreEqual(3, actual);
            Assert.AreEqual(3, state.CurrentHp);
        }

        [Test]
        public void TakeDamage_ExceedsHp_ClampsToZero()
        {
            var type = CreateSlimeType();
            var state = new MonsterState(type, Vector2Int.zero);

            int actual = state.TakeDamage(10);

            Assert.AreEqual(6, actual);
            Assert.AreEqual(0, state.CurrentHp);
            Assert.IsTrue(state.IsDead);
        }

        [Test]
        public void TakeDamage_ZeroDamage_NoChange()
        {
            var type = CreateSlimeType();
            var state = new MonsterState(type, Vector2Int.zero);

            int actual = state.TakeDamage(0);

            Assert.AreEqual(0, actual);
            Assert.AreEqual(6, state.CurrentHp);
        }

        [Test]
        public void TakeDamage_NegativeDamage_NoChange()
        {
            var type = CreateSlimeType();
            var state = new MonsterState(type, Vector2Int.zero);

            int actual = state.TakeDamage(-5);

            Assert.AreEqual(0, actual);
            Assert.AreEqual(6, state.CurrentHp);
        }
    }
}
