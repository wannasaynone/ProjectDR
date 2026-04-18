// CharacterQuestionsManager 單元測試（Sprint 5 B5）。

using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Village.Tests
{
    [TestFixture]
    public class CharacterQuestionsManagerTests
    {
        private CharacterQuestionsConfig _config;
        private AffinityManager _affinityManager;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _config = BuildConfig();
            _affinityManager = new AffinityManager(new AffinityConfig(
                new AffinityConfigData
                {
                    characters = new AffinityCharacterConfigData[0],
                    defaultThresholds = new int[] { 5, 10, 20 }
                }));
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new CharacterQuestionsManager(null, _affinityManager));
        }

        [Test]
        public void Constructor_NullAffinity_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new CharacterQuestionsManager(_config, null));
        }

        [Test]
        public void PickNextQuestion_EmptyLevel_ReturnsNull()
        {
            CharacterQuestionsManager m = new CharacterQuestionsManager(_config, _affinityManager, seed: 42);
            CharacterQuestionInfo result = m.PickNextQuestion(CharacterIds.FarmGirl, 1);
            Assert.IsNull(result);
        }

        [Test]
        public void PickNextQuestion_ReturnsUnseenQuestion()
        {
            CharacterQuestionsManager m = new CharacterQuestionsManager(_config, _affinityManager, seed: 42);
            CharacterQuestionInfo picked = m.PickNextQuestion(CharacterIds.VillageChiefWife, 1);
            Assert.IsNotNull(picked);
            Assert.AreEqual(CharacterIds.VillageChiefWife, picked.CharacterId);
            Assert.AreEqual(1, picked.Level);
            Assert.IsTrue(m.HasSeen(CharacterIds.VillageChiefWife, 1, picked.QuestionId));
        }

        [Test]
        public void PickNextQuestion_PublishesAskedEvent()
        {
            CharacterQuestionAskedEvent received = null;
            System.Action<CharacterQuestionAskedEvent> handler = e => received = e;
            EventBus.Subscribe(handler);
            try
            {
                CharacterQuestionsManager m = new CharacterQuestionsManager(_config, _affinityManager, seed: 42);
                CharacterQuestionInfo picked = m.PickNextQuestion(CharacterIds.VillageChiefWife, 1);
                Assert.IsNotNull(received);
                Assert.AreEqual(picked.QuestionId, received.QuestionId);
                Assert.AreEqual(1, received.Level);
            }
            finally { EventBus.Unsubscribe(handler); }
        }

        [Test]
        public void PickNextQuestion_DoesNotRepeat()
        {
            CharacterQuestionsManager m = new CharacterQuestionsManager(_config, _affinityManager, seed: 42);
            HashSet<string> picked = new HashSet<string>();
            for (int i = 0; i < 3; i++)
            {
                CharacterQuestionInfo q = m.PickNextQuestion(CharacterIds.VillageChiefWife, 1);
                Assert.IsNotNull(q);
                Assert.IsTrue(picked.Add(q.QuestionId), $"Question {q.QuestionId} repeated!");
            }
            // 3 題後池耗盡
            Assert.IsNull(m.PickNextQuestion(CharacterIds.VillageChiefWife, 1));
        }

        [Test]
        public void SubmitAnswer_AddsAffinity_ByPersonality()
        {
            CharacterQuestionsManager m = new CharacterQuestionsManager(_config, _affinityManager, seed: 42);
            int before = _affinityManager.GetAffinity(CharacterIds.VillageChiefWife);

            int delta = m.SubmitAnswer(CharacterIds.VillageChiefWife, "q_vcw_lv1_01", "personality_gentle");
            Assert.AreEqual(10, delta);
            Assert.AreEqual(before + 10, _affinityManager.GetAffinity(CharacterIds.VillageChiefWife));
        }

        [Test]
        public void SubmitAnswer_ZeroPersonality_NoAffinityAdd()
        {
            CharacterQuestionsManager m = new CharacterQuestionsManager(_config, _affinityManager, seed: 42);
            int before = _affinityManager.GetAffinity(CharacterIds.VillageChiefWife);

            int delta = m.SubmitAnswer(CharacterIds.VillageChiefWife, "q_vcw_lv1_01", "personality_assertive");
            Assert.AreEqual(0, delta);
            Assert.AreEqual(before, _affinityManager.GetAffinity(CharacterIds.VillageChiefWife));
        }

        [Test]
        public void SubmitAnswer_PublishesAnsweredEvent()
        {
            CharacterQuestionAnsweredEvent received = null;
            System.Action<CharacterQuestionAnsweredEvent> handler = e => received = e;
            EventBus.Subscribe(handler);
            try
            {
                CharacterQuestionsManager m = new CharacterQuestionsManager(_config, _affinityManager, seed: 42);
                m.SubmitAnswer(CharacterIds.VillageChiefWife, "q_vcw_lv1_01", "personality_calm");
                Assert.IsNotNull(received);
                Assert.AreEqual("personality_calm", received.SelectedPersonality);
                Assert.AreEqual(5, received.AffinityDelta);
            }
            finally { EventBus.Unsubscribe(handler); }
        }

        [Test]
        public void SubmitAnswer_UnknownCharacter_NoAffinityChange()
        {
            CharacterQuestionsManager m = new CharacterQuestionsManager(_config, _affinityManager, seed: 42);
            int delta = m.SubmitAnswer("unknown", "nope", "personality_gentle");
            Assert.AreEqual(0, delta);
        }

        [Test]
        public void SubmitAnswer_NullPersonality_PublishesEventWithZeroDelta()
        {
            CharacterQuestionAnsweredEvent received = null;
            System.Action<CharacterQuestionAnsweredEvent> handler = e => received = e;
            EventBus.Subscribe(handler);
            try
            {
                CharacterQuestionsManager m = new CharacterQuestionsManager(_config, _affinityManager, seed: 42);
                m.SubmitAnswer(CharacterIds.VillageChiefWife, "q_vcw_lv1_01", null);
                Assert.IsNotNull(received);
                Assert.AreEqual(0, received.AffinityDelta);
            }
            finally { EventBus.Unsubscribe(handler); }
        }

        // ===== 助手 =====

        private static CharacterQuestionsConfig BuildConfig()
        {
            CharacterQuestionsConfigData data = new CharacterQuestionsConfigData
            {
                personality_types = new PersonalityTypeData[]
                {
                    new PersonalityTypeData{ id="personality_gentle" },
                    new PersonalityTypeData{ id="personality_lively" },
                    new PersonalityTypeData{ id="personality_calm" },
                    new PersonalityTypeData{ id="personality_assertive" },
                },
                character_personality_preference = new CharacterPersonalityPreferenceData
                {
                    village_chief_wife = "personality_gentle",
                    farm_girl = "personality_lively",
                    witch = "personality_calm",
                    guard = "personality_assertive",
                },
                personality_affinity_map = new PersonalityAffinityMapData
                {
                    village_chief_wife = new PersonalityAffinityEntryData
                    {
                        personality_gentle = 10, personality_calm = 5, personality_lively = 2, personality_assertive = 0
                    },
                    farm_girl = new PersonalityAffinityEntryData
                    {
                        personality_lively = 10, personality_assertive = 5, personality_gentle = 2, personality_calm = 0
                    },
                    witch = new PersonalityAffinityEntryData
                    {
                        personality_calm = 10, personality_gentle = 5, personality_assertive = 2, personality_lively = 0
                    },
                    guard = new PersonalityAffinityEntryData
                    {
                        personality_assertive = 10, personality_calm = 5, personality_lively = 2, personality_gentle = 0
                    },
                },
                questions = new CharacterQuestionEntryData[]
                {
                    new CharacterQuestionEntryData
                    {
                        character_id = CharacterIds.VillageChiefWife, level = 1,
                        question_id = "q_vcw_lv1_01", prompt = "Q1",
                        options = FourOpts("q1")
                    },
                    new CharacterQuestionEntryData
                    {
                        character_id = CharacterIds.VillageChiefWife, level = 1,
                        question_id = "q_vcw_lv1_02", prompt = "Q2",
                        options = FourOpts("q2")
                    },
                    new CharacterQuestionEntryData
                    {
                        character_id = CharacterIds.VillageChiefWife, level = 1,
                        question_id = "q_vcw_lv1_03", prompt = "Q3",
                        options = FourOpts("q3")
                    },
                },
            };
            return new CharacterQuestionsConfig(data);
        }

        private static CharacterQuestionOptionData[] FourOpts(string tag)
        {
            return new[]
            {
                new CharacterQuestionOptionData{ personality="personality_gentle", text=tag+"a", response="r" },
                new CharacterQuestionOptionData{ personality="personality_lively", text=tag+"b", response="r" },
                new CharacterQuestionOptionData{ personality="personality_calm", text=tag+"c", response="r" },
                new CharacterQuestionOptionData{ personality="personality_assertive", text=tag+"d", response="r" },
            };
        }
    }
}
