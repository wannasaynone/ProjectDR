// CharacterStaminaManager 單元測試（Sprint 5 B13）。

using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Village.Tests
{
    [TestFixture]
    public class CharacterStaminaManagerTests
    {
        [Test]
        public void DefaultConstructor_UsesDefaults()
        {
            CharacterStaminaManager m = new CharacterStaminaManager();
            Assert.AreEqual(10, m.MaxStamina);
            Assert.AreEqual(1, m.ConsumePerDialogue);
        }

        [Test]
        public void NewCharacter_HasFullStamina()
        {
            CharacterStaminaManager m = new CharacterStaminaManager();
            Assert.AreEqual(10, m.GetStamina(CharacterIds.FarmGirl));
            Assert.IsTrue(m.HasEnoughForDialogue(CharacterIds.FarmGirl));
        }

        [Test]
        public void TryConsume_ReducesStamina()
        {
            CharacterStaminaManager m = new CharacterStaminaManager();
            Assert.IsTrue(m.TryConsumeForDialogue(CharacterIds.FarmGirl));
            Assert.AreEqual(9, m.GetStamina(CharacterIds.FarmGirl));
        }

        [Test]
        public void TryConsume_WhenZero_ReturnsFalse()
        {
            CharacterStaminaManager m = new CharacterStaminaManager();
            m.SetStamina(CharacterIds.FarmGirl, 0);
            Assert.IsFalse(m.TryConsumeForDialogue(CharacterIds.FarmGirl));
            Assert.AreEqual(0, m.GetStamina(CharacterIds.FarmGirl));
        }

        [Test]
        public void HasEnoughForDialogue_ZeroStamina_False()
        {
            CharacterStaminaManager m = new CharacterStaminaManager();
            m.SetStamina(CharacterIds.FarmGirl, 0);
            Assert.IsFalse(m.HasEnoughForDialogue(CharacterIds.FarmGirl));
        }

        [Test]
        public void Restore_IncreasesStamina_CapsAtMax()
        {
            CharacterStaminaManager m = new CharacterStaminaManager();
            m.SetStamina(CharacterIds.FarmGirl, 3);
            m.Restore(CharacterIds.FarmGirl, 5);
            Assert.AreEqual(8, m.GetStamina(CharacterIds.FarmGirl));

            m.Restore(CharacterIds.FarmGirl, 99);
            Assert.AreEqual(10, m.GetStamina(CharacterIds.FarmGirl));
        }

        [Test]
        public void MultipleCharacters_Independent()
        {
            CharacterStaminaManager m = new CharacterStaminaManager();
            m.TryConsumeForDialogue(CharacterIds.FarmGirl);
            Assert.AreEqual(9, m.GetStamina(CharacterIds.FarmGirl));
            Assert.AreEqual(10, m.GetStamina(CharacterIds.Witch));
        }

        [Test]
        public void CustomConstructor_AppliesValues()
        {
            CharacterStaminaManager m = new CharacterStaminaManager(5, 2);
            Assert.AreEqual(5, m.MaxStamina);
            Assert.AreEqual(2, m.ConsumePerDialogue);

            Assert.IsTrue(m.TryConsumeForDialogue(CharacterIds.FarmGirl));
            Assert.AreEqual(3, m.GetStamina(CharacterIds.FarmGirl));
        }

        [Test]
        public void NullCharacter_Ignored()
        {
            CharacterStaminaManager m = new CharacterStaminaManager();
            Assert.IsFalse(m.TryConsumeForDialogue(null));
            Assert.AreEqual(0, m.GetStamina(null));
        }
    }
}
