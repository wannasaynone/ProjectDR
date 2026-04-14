using System;
using NUnit.Framework;
using ProjectDR.Village.Exploration.Combat;

namespace ProjectDR.Tests.Village.Exploration.Combat
{
    [TestFixture]
    public class SpdMoveSpeedCalculatorTests
    {
        [Test]
        public void Constructor_ValidParams_CalculatesDuration()
        {
            // 0.2 - 10 * 0.005 = 0.15
            var calc = new SpdMoveSpeedCalculator(0.2f, 0.005f, 10);
            Assert.AreEqual(0.15f, calc.CalculateMoveDuration(), 0.001f);
        }

        [Test]
        public void Constructor_HighSpd_ClampsToMinDuration()
        {
            // 0.2 - 100 * 0.005 = -0.3 -> clamp to 0.05
            var calc = new SpdMoveSpeedCalculator(0.2f, 0.005f, 100);
            Assert.AreEqual(0.05f, calc.CalculateMoveDuration(), 0.001f);
        }

        [Test]
        public void Constructor_ZeroSpd_ReturnsBase()
        {
            var calc = new SpdMoveSpeedCalculator(0.2f, 0.005f, 0);
            Assert.AreEqual(0.2f, calc.CalculateMoveDuration(), 0.001f);
        }

        [Test]
        public void Constructor_ZeroBase_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SpdMoveSpeedCalculator(0f, 0.005f, 10));
        }

        [Test]
        public void Constructor_NegativeBase_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SpdMoveSpeedCalculator(-0.1f, 0.005f, 10));
        }

        [Test]
        public void CalculateMoveDuration_ReturnsConsistentValue()
        {
            var calc = new SpdMoveSpeedCalculator(0.2f, 0.005f, 10);
            float d1 = calc.CalculateMoveDuration();
            float d2 = calc.CalculateMoveDuration();
            Assert.AreEqual(d1, d2, 0.0001f);
        }
    }
}
