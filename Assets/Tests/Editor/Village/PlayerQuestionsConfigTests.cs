// PlayerQuestionsConfigTests — PlayerQuestionsConfig 單元測試（B14）。

using System.Collections.Generic;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.CharacterQuestions;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Village.Tests
{
    [TestFixture]
    public class PlayerQuestionsConfigTests
    {
        // ── 建構驗證 ────────────────────────────────────────────────────

        [Test]
        public void Constructor_NullData_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new PlayerQuestionsConfig(null));
        }

        [Test]
        public void Constructor_EmptyQuestions_NoException()
        {
            var data = new PlayerQuestionsConfigData { questions = new PlayerQuestionData[0] };
            Assert.DoesNotThrow(() => new PlayerQuestionsConfig(data));
        }

        // ── GetQuestionsForCharacter ─────────────────────────────────────

        [Test]
        public void GetQuestionsForCharacter_ReturnsQuestionsInSortOrder()
        {
            var data = BuildConfigData(new[]
            {
                BuildQuestion("q1", "C1", 0, "B 問題", sort: 2),
                BuildQuestion("q2", "C1", 0, "A 問題", sort: 1),
            });
            var config = new PlayerQuestionsConfig(data);

            IReadOnlyList<PlayerQuestionInfo> result = config.GetQuestionsForCharacter("C1");

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("q2", result[0].QuestionId);   // sort=1 在前
            Assert.AreEqual("q1", result[1].QuestionId);
        }

        [Test]
        public void GetQuestionsForCharacter_UnknownCharacter_ReturnsEmpty()
        {
            var data = BuildConfigData(new PlayerQuestionData[0]);
            var config = new PlayerQuestionsConfig(data);

            IReadOnlyList<PlayerQuestionInfo> result = config.GetQuestionsForCharacter("NoSuch");
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetQuestionsForCharacter_NullId_ReturnsEmpty()
        {
            var data = BuildConfigData(new PlayerQuestionData[0]);
            var config = new PlayerQuestionsConfig(data);

            IReadOnlyList<PlayerQuestionInfo> result = config.GetQuestionsForCharacter(null);
            Assert.AreEqual(0, result.Count);
        }

        // ── GetUnlockedQuestions ─────────────────────────────────────────

        [Test]
        public void GetUnlockedQuestions_Stage0_ReturnsOnlyStage0Questions()
        {
            var data = BuildConfigData(new[]
            {
                BuildQuestion("q1", "C1", 0, "Q stage 0", sort: 1),
                BuildQuestion("q2", "C1", 1, "Q stage 1", sort: 2),
                BuildQuestion("q3", "C1", 2, "Q stage 2", sort: 3),
            });
            var config = new PlayerQuestionsConfig(data);

            IReadOnlyList<PlayerQuestionInfo> result = config.GetUnlockedQuestions("C1", 0);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("q1", result[0].QuestionId);
        }

        [Test]
        public void GetUnlockedQuestions_Stage1_ReturnsStage0And1Questions()
        {
            var data = BuildConfigData(new[]
            {
                BuildQuestion("q1", "C1", 0, "Q stage 0", sort: 1),
                BuildQuestion("q2", "C1", 1, "Q stage 1", sort: 2),
                BuildQuestion("q3", "C1", 2, "Q stage 2", sort: 3),
            });
            var config = new PlayerQuestionsConfig(data);

            IReadOnlyList<PlayerQuestionInfo> result = config.GetUnlockedQuestions("C1", 1);
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void GetUnlockedQuestions_HighStage_ReturnsAll()
        {
            var data = BuildConfigData(new[]
            {
                BuildQuestion("q1", "C1", 0, "Q0", sort: 1),
                BuildQuestion("q2", "C1", 1, "Q1", sort: 2),
                BuildQuestion("q3", "C1", 2, "Q2", sort: 3),
            });
            var config = new PlayerQuestionsConfig(data);

            IReadOnlyList<PlayerQuestionInfo> result = config.GetUnlockedQuestions("C1", 99);
            Assert.AreEqual(3, result.Count);
        }

        // ── GetQuestion ──────────────────────────────────────────────────

        [Test]
        public void GetQuestion_ExistingId_ReturnsInfo()
        {
            var data = BuildConfigData(new[]
            {
                BuildQuestion("q1", "C1", 0, "問題文字", sort: 1),
            });
            var config = new PlayerQuestionsConfig(data);

            PlayerQuestionInfo info = config.GetQuestion("q1");
            Assert.IsNotNull(info);
            Assert.AreEqual("問題文字", info.QuestionText);
        }

        [Test]
        public void GetQuestion_UnknownId_ReturnsNull()
        {
            var data = BuildConfigData(new PlayerQuestionData[0]);
            var config = new PlayerQuestionsConfig(data);
            Assert.IsNull(config.GetQuestion("no_such"));
        }

        [Test]
        public void GetQuestion_NullId_ReturnsNull()
        {
            var data = BuildConfigData(new PlayerQuestionData[0]);
            var config = new PlayerQuestionsConfig(data);
            Assert.IsNull(config.GetQuestion(null));
        }

        // ── 多角色隔離 ───────────────────────────────────────────────────

        [Test]
        public void Questions_DifferentCharacters_AreIsolated()
        {
            var data = BuildConfigData(new[]
            {
                BuildQuestion("q1", "C1", 0, "C1 問題", sort: 1),
                BuildQuestion("q2", "C2", 0, "C2 問題", sort: 1),
            });
            var config = new PlayerQuestionsConfig(data);

            Assert.AreEqual(1, config.GetQuestionsForCharacter("C1").Count);
            Assert.AreEqual(1, config.GetQuestionsForCharacter("C2").Count);
        }

        // ── 真實 JSON ────────────────────────────────────────────────────

        [Test]
        public void RealJson_Deserializes_WithExpectedQuestions()
        {
            UnityEngine.TextAsset asset =
                UnityEngine.Resources.Load<UnityEngine.TextAsset>("Config/player-questions-config");
            if (asset == null)
            {
                Assert.Ignore("player-questions-config.json 不在 Resources，跳過真實 JSON 測試。");
                return;
            }

            var data = UnityEngine.JsonUtility.FromJson<PlayerQuestionsConfigData>(asset.text);
            var config = new PlayerQuestionsConfig(data);

            IReadOnlyList<PlayerQuestionInfo> vcwQuestions =
                config.GetQuestionsForCharacter(CharacterIds.VillageChiefWife);
            Assert.Greater(vcwQuestions.Count, 0, "村長夫人應有問題");
        }

        // ── 工具方法 ─────────────────────────────────────────────────────

        private static PlayerQuestionsConfigData BuildConfigData(PlayerQuestionData[] questions)
        {
            return new PlayerQuestionsConfigData
            {
                schema_version = 1,
                questions = questions,
            };
        }

        private static PlayerQuestionData BuildQuestion(
            string id, string charId, int stage, string text, int sort)
        {
            return new PlayerQuestionData
            {
                question_id = id,
                character_id = charId,
                unlock_affinity_stage = stage,
                question_text = text,
                response_text = "回答 " + text,
                sort_order = sort,
            };
        }
    }
}
