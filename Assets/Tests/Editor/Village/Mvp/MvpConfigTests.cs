using System;
using NUnit.Framework;
using ProjectDR.Village.Mvp;

namespace ProjectDR.Tests.Village.Mvp
{
    [TestFixture]
    public class MvpConfigTests
    {
        private MvpConfigData MakeValidData()
        {
            return new MvpConfigData
            {
                search = new MvpSearchConfigData
                {
                    cooldownSeconds = 1f,
                    woodGainPerSearch = 1,
                    feedbackLines = new[] { "a", "b" }
                },
                fire = new MvpFireConfigData
                {
                    unlockWoodThreshold = 5,
                    lightCost = 1,
                    durationSeconds = 60f,
                    extendCost = 1,
                    extendSeconds = 60f
                },
                cold = new MvpColdConfigData { actionCooldownMultiplier = 2f },
                hut = new MvpHutConfigData { woodCost = 10, buildSeconds = 10f, populationCapIncrement = 1 },
                population = new MvpPopulationConfigData { initialCap = 0 },
                dialogue = new MvpDialogueConfigData
                {
                    affinityGainPerDialogue = 3,
                    playerDialogueCooldownSeconds = 30f,
                    dispatchCooldownMultiplier = 2f,
                    npcInitiativeIntervalSeconds = 45f
                },
                placeholderCharacters = new[]
                {
                    new MvpPlaceholderCharacterData { characterId = "A", displayName = "Alice" }
                },
                placeholderDialogue = new MvpPlaceholderDialogueData
                {
                    characterInitiativeLines = new[] { "ci" },
                    playerInitiativeLines = new[] { "pi" }
                }
            };
        }

        [Test]
        public void Ctor_NullData_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MvpConfig(null));
        }

        [Test]
        public void Ctor_Valid_LoadsAllFields()
        {
            MvpConfig cfg = new MvpConfig(MakeValidData());
            Assert.AreEqual(1f, cfg.SearchCooldownSeconds);
            Assert.AreEqual(1, cfg.SearchWoodGainPerSearch);
            Assert.AreEqual(5, cfg.FireUnlockWoodThreshold);
            Assert.AreEqual(1, cfg.FireLightCost);
            Assert.AreEqual(60f, cfg.FireDurationSeconds);
            Assert.AreEqual(60f, cfg.FireExtendSeconds);
            Assert.AreEqual(2f, cfg.ColdActionCooldownMultiplier);
            Assert.AreEqual(10, cfg.HutWoodCost);
            Assert.AreEqual(10f, cfg.HutBuildSeconds);
            Assert.AreEqual(1, cfg.HutPopulationCapIncrement);
            Assert.AreEqual(0, cfg.InitialPopulationCap);
            Assert.AreEqual(3, cfg.DialogueAffinityGain);
            Assert.AreEqual(30f, cfg.PlayerDialogueCooldownSeconds);
            Assert.AreEqual(2f, cfg.DispatchCooldownMultiplier);
            Assert.AreEqual(45f, cfg.NpcInitiativeIntervalSeconds);
            Assert.AreEqual(2, cfg.SearchFeedbackLines.Count);
            Assert.AreEqual(1, cfg.PlaceholderCharacters.Count);
            Assert.AreEqual(1, cfg.CharacterInitiativeLines.Count);
            Assert.AreEqual(1, cfg.PlayerInitiativeLines.Count);
        }

        [Test]
        public void Ctor_InvalidSearchCooldown_Throws()
        {
            MvpConfigData d = MakeValidData();
            d.search.cooldownSeconds = 0f;
            Assert.Throws<ArgumentException>(() => new MvpConfig(d));
        }

        [Test]
        public void Ctor_InvalidColdMultiplier_Throws()
        {
            MvpConfigData d = MakeValidData();
            d.cold.actionCooldownMultiplier = 0.5f;
            Assert.Throws<ArgumentException>(() => new MvpConfig(d));
        }

        [Test]
        public void Ctor_InvalidHutCost_Throws()
        {
            MvpConfigData d = MakeValidData();
            d.hut.woodCost = -1;
            Assert.Throws<ArgumentException>(() => new MvpConfig(d));
        }

        [Test]
        public void Ctor_ZeroHutPopulationIncrement_Throws()
        {
            MvpConfigData d = MakeValidData();
            d.hut.populationCapIncrement = 0;
            Assert.Throws<ArgumentException>(() => new MvpConfig(d));
        }

        [Test]
        public void Ctor_NegativePlayerCooldown_Throws()
        {
            MvpConfigData d = MakeValidData();
            d.dialogue.playerDialogueCooldownSeconds = -1f;
            Assert.Throws<ArgumentException>(() => new MvpConfig(d));
        }

        [Test]
        public void Ctor_DispatchMultiplierBelowOne_Throws()
        {
            MvpConfigData d = MakeValidData();
            d.dialogue.dispatchCooldownMultiplier = 0.5f;
            Assert.Throws<ArgumentException>(() => new MvpConfig(d));
        }

        [Test]
        public void Ctor_ZeroAffinityGain_Throws()
        {
            MvpConfigData d = MakeValidData();
            d.dialogue.affinityGainPerDialogue = 0;
            Assert.Throws<ArgumentException>(() => new MvpConfig(d));
        }
    }
}
