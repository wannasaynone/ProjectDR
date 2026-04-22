using System;
using NUnit.Framework;
using ProjectDR.Village.Exploration.Combat;
using ProjectDR.Village.Exploration.MoveSpeed;

namespace ProjectDR.Tests.Village.Exploration.Combat
{
    [TestFixture]
    public class SpdMoveSpeedProviderTests
    {
        [Test]
        public void Constructor_ValidParams_CalculatesSpeed()
        {
            // 3.0 + 10 * 0.1 = 4.0
            var provider = new SpdMoveSpeedProvider(3.0f, 0.1f, 10);
            Assert.AreEqual(4.0f, provider.GetMoveSpeed(), 0.001f);
        }

        [Test]
        public void Constructor_HighSpdNegativeResult_ClampsToMinSpeed()
        {
            // Pathological: baseSpeed=0.5, factor=-0.1, spd=100 => 0.5 + 100*(-0.1) = -9.5 => clamp to 0.5
            var provider = new SpdMoveSpeedProvider(0.5f, -0.1f, 100);
            Assert.AreEqual(0.5f, provider.GetMoveSpeed(), 0.001f);
        }

        [Test]
        public void Constructor_ZeroSpd_ReturnsBase()
        {
            var provider = new SpdMoveSpeedProvider(3.0f, 0.1f, 0);
            Assert.AreEqual(3.0f, provider.GetMoveSpeed(), 0.001f);
        }

        [Test]
        public void Constructor_ZeroBase_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SpdMoveSpeedProvider(0f, 0.1f, 10));
        }

        [Test]
        public void Constructor_NegativeBase_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SpdMoveSpeedProvider(-1.0f, 0.1f, 10));
        }

        [Test]
        public void GetMoveSpeed_ReturnsConsistentValue()
        {
            var provider = new SpdMoveSpeedProvider(3.0f, 0.1f, 10);
            float s1 = provider.GetMoveSpeed();
            float s2 = provider.GetMoveSpeed();
            Assert.AreEqual(s1, s2, 0.0001f);
        }
    }
}
