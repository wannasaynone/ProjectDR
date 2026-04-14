using NUnit.Framework;
using ProjectDR.Village.Exploration.Combat;

namespace ProjectDR.Tests.Village.Exploration.Combat
{
    [TestFixture]
    public class DamageCalculatorTests
    {
        [Test]
        public void Calculate_AtkGreaterThanDef_ReturnsDifference()
        {
            int dmg = DamageCalculator.Calculate(10, 3);
            Assert.AreEqual(7, dmg);
        }

        [Test]
        public void Calculate_AtkEqualsDef_ReturnsOne()
        {
            // Minimum damage is 1
            int dmg = DamageCalculator.Calculate(5, 5);
            Assert.AreEqual(1, dmg);
        }

        [Test]
        public void Calculate_AtkLessThanDef_ReturnsOne()
        {
            int dmg = DamageCalculator.Calculate(2, 10);
            Assert.AreEqual(1, dmg);
        }

        [Test]
        public void Calculate_ZeroAtk_ReturnsOne()
        {
            int dmg = DamageCalculator.Calculate(0, 5);
            Assert.AreEqual(1, dmg);
        }

        [Test]
        public void Calculate_ZeroDef_ReturnsAtk()
        {
            int dmg = DamageCalculator.Calculate(8, 0);
            Assert.AreEqual(8, dmg);
        }

        [Test]
        public void Calculate_BothZero_ReturnsOne()
        {
            int dmg = DamageCalculator.Calculate(0, 0);
            Assert.AreEqual(1, dmg);
        }

        [Test]
        public void Calculate_LargeValues_ReturnsCorrectDifference()
        {
            int dmg = DamageCalculator.Calculate(100, 30);
            Assert.AreEqual(70, dmg);
        }

        [Test]
        public void Calculate_NegativeAtk_ReturnsOne()
        {
            int dmg = DamageCalculator.Calculate(-5, 3);
            Assert.AreEqual(1, dmg);
        }
    }
}
