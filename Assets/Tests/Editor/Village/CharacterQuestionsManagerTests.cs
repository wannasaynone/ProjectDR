// CharacterQuestionsManager 單元測試（Sprint 5 B5）。

using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.CharacterQuestions;
using ProjectDR.Village.Affinity;

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
                new AffinityCharacterData[]
                {
                    new AffinityCharacterData { id = 1, character_id = "__default__", thresholds = "5,10,20" }
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
            CharacterQuestionData[] questions = new CharacterQuestionData[]
            {
                new CharacterQuestionData { id=1, question_id="q_vcw_lv1_01", character_id="VillageChiefWife", level=1, prompt="Q1" },
                new CharacterQuestionData { id=2, question_id="q_vcw_lv1_02", character_id="VillageChiefWife", level=1, prompt="Q2" },
                new CharacterQuestionData { id=3, question_id="q_vcw_lv1_03", character_id="VillageChiefWife", level=1, prompt="Q3" },
            };

            CharacterQuestionOptionData[] options = FlatOpts(
                new[] { "q_vcw_lv1_01", "q_vcw_lv1_02", "q_vcw_lv1_03" },
                new[] { "q1", "q2", "q3" });

            CharacterProfileData[] profiles = new CharacterProfileData[]
            {
                new CharacterProfileData { id=1, character_id="VillageChiefWife", preferred_personality_id="personality_gentle" },
                new CharacterProfileData { id=2, character_id="FarmGirl",         preferred_personality_id="personality_lively" },
                new CharacterProfileData { id=3, character_id="Witch",            preferred_personality_id="personality_calm" },
                new CharacterProfileData { id=4, character_id="Guard",            preferred_personality_id="personality_assertive" },
            };

            PersonalityAffinityRuleData[] affinityRules = new PersonalityAffinityRuleData[]
            {
                new PersonalityAffinityRuleData { id=1,  character_id="VillageChiefWife", personality_id="personality_gentle",    affinity_delta=10 },
                new PersonalityAffinityRuleData { id=2,  character_id="VillageChiefWife", personality_id="personality_calm",      affinity_delta=5  },
                new PersonalityAffinityRuleData { id=3,  character_id="VillageChiefWife", personality_id="personality_lively",    affinity_delta=2  },
                new PersonalityAffinityRuleData { id=4,  character_id="VillageChiefWife", personality_id="personality_assertive", affinity_delta=0  },
                new PersonalityAffinityRuleData { id=5,  character_id="FarmGirl",         personality_id="personality_lively",    affinity_delta=10 },
                new PersonalityAffinityRuleData { id=6,  character_id="FarmGirl",         personality_id="personality_assertive", affinity_delta=5  },
                new PersonalityAffinityRuleData { id=7,  character_id="FarmGirl",         personality_id="personality_gentle",    affinity_delta=2  },
                new PersonalityAffinityRuleData { id=8,  character_id="FarmGirl",         personality_id="personality_calm",      affinity_delta=0  },
                new PersonalityAffinityRuleData { id=9,  character_id="Witch",            personality_id="personality_calm",      affinity_delta=10 },
                new PersonalityAffinityRuleData { id=10, character_id="Witch",            personality_id="personality_gentle",    affinity_delta=5  },
                new PersonalityAffinityRuleData { id=11, character_id="Witch",            personality_id="personality_assertive", affinity_delta=2  },
                new PersonalityAffinityRuleData { id=12, character_id="Witch",            personality_id="personality_lively",    affinity_delta=0  },
                new PersonalityAffinityRuleData { id=13, character_id="Guard",            personality_id="personality_assertive", affinity_delta=10 },
                new PersonalityAffinityRuleData { id=14, character_id="Guard",            personality_id="personality_calm",      affinity_delta=5  },
                new PersonalityAffinityRuleData { id=15, character_id="Guard",            personality_id="personality_lively",    affinity_delta=2  },
                new PersonalityAffinityRuleData { id=16, character_id="Guard",            personality_id="personality_gentle",    affinity_delta=0  },
            };

            return new CharacterQuestionsConfig(questions, options, profiles, affinityRules);
        }

        private static CharacterQuestionOptionData[] FlatOpts(string[] questionIds, string[] tags)
        {
            string[] personalities = new[]
            {
                "personality_gentle",
                "personality_lively",
                "personality_calm",
                "personality_assertive",
            };
            string[] suffixes = new[] { "a", "b", "c", "d" };

            List<CharacterQuestionOptionData> result = new List<CharacterQuestionOptionData>();
            int idCounter = 1;
            for (int qi = 0; qi < questionIds.Length; qi++)
            {
                for (int pi = 0; pi < personalities.Length; pi++)
                {
                    result.Add(new CharacterQuestionOptionData
                    {
                        id = idCounter++,
                        question_id = questionIds[qi],
                        personality_id = personalities[pi],
                        text = tags[qi] + suffixes[pi],
                        response = "r",
                    });
                }
            }
            return result.ToArray();
        }
    }
}
