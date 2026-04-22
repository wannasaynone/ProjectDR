// PlayerQuestionsManager 單元測試（Sprint 5 B11）。

using System.Collections.Generic;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.CharacterQuestions;

namespace ProjectDR.Village.Tests
{
    [TestFixture]
    public class PlayerQuestionsManagerTests
    {
        private PlayerQuestionsConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = BuildConfigWithN(10);
        }

        [Test]
        public void GetPresentation_4OrMore_Returns4()
        {
            PlayerQuestionsManager m = new PlayerQuestionsManager(_config, seed: 42);
            PlayerQuestionsPresentation p = m.GetPresentation(CharacterIds.VillageChiefWife);
            Assert.IsFalse(p.IsIdleChatFallback);
            Assert.AreEqual(4, p.Questions.Count);
        }

        [Test]
        public void GetPresentation_Exactly4_Returns4()
        {
            PlayerQuestionsConfig c = BuildConfigWithN(4);
            PlayerQuestionsManager m = new PlayerQuestionsManager(c, seed: 42);
            PlayerQuestionsPresentation p = m.GetPresentation(CharacterIds.VillageChiefWife);
            Assert.AreEqual(4, p.Questions.Count);
            Assert.IsFalse(p.IsIdleChatFallback);
        }

        [Test]
        public void GetPresentation_1To3_ReturnsAllRemaining_NotPadded()
        {
            PlayerQuestionsConfig c = BuildConfigWithN(3);
            PlayerQuestionsManager m = new PlayerQuestionsManager(c, seed: 42);
            PlayerQuestionsPresentation p = m.GetPresentation(CharacterIds.VillageChiefWife);
            Assert.AreEqual(3, p.Questions.Count);
            Assert.IsFalse(p.IsIdleChatFallback);
        }

        [Test]
        public void GetPresentation_Zero_ReturnsIdleChatFallback()
        {
            PlayerQuestionsConfig c = BuildConfigWithN(0);
            PlayerQuestionsManager m = new PlayerQuestionsManager(c, seed: 42);
            PlayerQuestionsPresentation p = m.GetPresentation(CharacterIds.VillageChiefWife);
            Assert.IsTrue(p.IsIdleChatFallback);
            Assert.AreEqual(0, p.Questions.Count);
        }

        [Test]
        public void MarkSeen_ReducesUnseenCount()
        {
            PlayerQuestionsManager m = new PlayerQuestionsManager(_config, seed: 42);
            int before = m.GetUnseenCount(CharacterIds.VillageChiefWife);
            m.MarkSeen(CharacterIds.VillageChiefWife, "q_vcw_01");
            Assert.AreEqual(before - 1, m.GetUnseenCount(CharacterIds.VillageChiefWife));
        }

        [Test]
        public void MarkSeen_AllConsumed_IsIdleChatMode()
        {
            PlayerQuestionsManager m = new PlayerQuestionsManager(_config, seed: 42);
            for (int i = 1; i <= 10; i++)
                m.MarkSeen(CharacterIds.VillageChiefWife, $"q_vcw_{i:00}");
            Assert.AreEqual(0, m.GetUnseenCount(CharacterIds.VillageChiefWife));
            Assert.IsTrue(m.IsIdleChatMode(CharacterIds.VillageChiefWife));

            PlayerQuestionsPresentation p = m.GetPresentation(CharacterIds.VillageChiefWife);
            Assert.IsTrue(p.IsIdleChatFallback);
        }

        [Test]
        public void GetPresentation_DoesNotMarkSeen()
        {
            PlayerQuestionsManager m = new PlayerQuestionsManager(_config, seed: 42);
            int before = m.GetUnseenCount(CharacterIds.VillageChiefWife);
            m.GetPresentation(CharacterIds.VillageChiefWife);
            Assert.AreEqual(before, m.GetUnseenCount(CharacterIds.VillageChiefWife));
        }

        [Test]
        public void MultipleCharacters_Independent()
        {
            PlayerQuestionsConfig c = BuildMultiCharConfig();
            PlayerQuestionsManager m = new PlayerQuestionsManager(c, seed: 42);
            m.MarkSeen(CharacterIds.VillageChiefWife, "q_vcw_01");
            Assert.AreEqual(0, m.GetUnseenCount(CharacterIds.VillageChiefWife));
            Assert.AreEqual(2, m.GetUnseenCount(CharacterIds.FarmGirl));
        }

        [Test]
        public void NullCharacter_ReturnsEmpty()
        {
            PlayerQuestionsManager m = new PlayerQuestionsManager(_config, seed: 42);
            PlayerQuestionsPresentation p = m.GetPresentation(null);
            Assert.IsFalse(p.IsIdleChatFallback);
            Assert.AreEqual(0, p.Questions.Count);
        }

        // ===== 助手 =====

        private static PlayerQuestionsConfig BuildConfigWithN(int n)
        {
            List<PlayerQuestionData> qs = new List<PlayerQuestionData>();
            for (int i = 1; i <= n; i++)
            {
                qs.Add(new PlayerQuestionData
                {
                    question_id = $"q_vcw_{i:00}",
                    character_id = CharacterIds.VillageChiefWife,
                    question_text = $"Q{i}?",
                    response_text = $"A{i}",
                    sort_order = i,
                });
            }
            return new PlayerQuestionsConfig(new PlayerQuestionsConfigData { questions = qs.ToArray() });
        }

        private static PlayerQuestionsConfig BuildMultiCharConfig()
        {
            return new PlayerQuestionsConfig(new PlayerQuestionsConfigData
            {
                questions = new PlayerQuestionData[]
                {
                    new PlayerQuestionData{ question_id="q_vcw_01", character_id=CharacterIds.VillageChiefWife, question_text="", response_text="" },
                    new PlayerQuestionData{ question_id="q_fg_01", character_id=CharacterIds.FarmGirl, question_text="", response_text="" },
                    new PlayerQuestionData{ question_id="q_fg_02", character_id=CharacterIds.FarmGirl, question_text="", response_text="" },
                }
            });
        }
    }
}
