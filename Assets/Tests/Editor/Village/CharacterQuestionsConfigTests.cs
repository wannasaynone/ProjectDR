// CharacterQuestionsConfig 單元測試（Sprint 5 B4/B9）。
//
// Sprint 8 Wave 2.5：配合純陣列 DTO 重構
//   - 廢棄 CharacterQuestionsConfigData 包裹類
//   - personality_types / preference / affinity map 拆為三個獨立分頁（PersonalityData / CharacterProfileData / PersonalityAffinityRuleData）
//   - CharacterQuestionsConfig 建構子改為接受 4 個純陣列

using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.CharacterQuestions;
using ProjectDR.Village.Navigation;
using System.Collections.Generic;

namespace ProjectDR.Village.Tests
{
    [TestFixture]
    public class CharacterQuestionsConfigTests
    {
        [Test]
        public void Constructor_NullQuestionEntries_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new CharacterQuestionsConfig(
                    null,
                    new CharacterQuestionOptionData[0],
                    new CharacterProfileData[0],
                    new PersonalityAffinityRuleData[0]));
        }

        [Test]
        public void Constructor_NullOptionEntries_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new CharacterQuestionsConfig(
                    new CharacterQuestionData[0],
                    null,
                    new CharacterProfileData[0],
                    new PersonalityAffinityRuleData[0]));
        }

        [Test]
        public void Constructor_NullProfileEntries_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new CharacterQuestionsConfig(
                    new CharacterQuestionData[0],
                    new CharacterQuestionOptionData[0],
                    null,
                    new PersonalityAffinityRuleData[0]));
        }

        [Test]
        public void Constructor_NullAffinityRuleEntries_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new CharacterQuestionsConfig(
                    new CharacterQuestionData[0],
                    new CharacterQuestionOptionData[0],
                    new CharacterProfileData[0],
                    null));
        }

        [Test]
        public void Constructor_EmptyData_CreatesValidInstance()
        {
            CharacterQuestionsConfig cfg = new CharacterQuestionsConfig(
                new CharacterQuestionData[0],
                new CharacterQuestionOptionData[0],
                new CharacterProfileData[0],
                new PersonalityAffinityRuleData[0]);
            Assert.IsNull(cfg.GetPersonalityPreference(CharacterIds.VillageChiefWife));
            Assert.AreEqual(0, cfg.GetAffinityDelta(CharacterIds.VillageChiefWife, "personality_gentle"));
        }

        [Test]
        public void PersonalityPreference_MatchesConfig()
        {
            CharacterQuestionsConfig cfg = BuildMinimalConfig();
            Assert.AreEqual("personality_gentle", cfg.GetPersonalityPreference(CharacterIds.VillageChiefWife));
            Assert.AreEqual("personality_lively", cfg.GetPersonalityPreference(CharacterIds.FarmGirl));
            Assert.AreEqual("personality_calm", cfg.GetPersonalityPreference(CharacterIds.Witch));
            Assert.AreEqual("personality_assertive", cfg.GetPersonalityPreference(CharacterIds.Guard));
        }

        [Test]
        public void AffinityDelta_FourTiersLinearMap()
        {
            CharacterQuestionsConfig cfg = BuildMinimalConfig();
            Assert.AreEqual(10, cfg.GetAffinityDelta(CharacterIds.VillageChiefWife, "personality_gentle"));
            Assert.AreEqual(5, cfg.GetAffinityDelta(CharacterIds.VillageChiefWife, "personality_calm"));
            Assert.AreEqual(2, cfg.GetAffinityDelta(CharacterIds.VillageChiefWife, "personality_lively"));
            Assert.AreEqual(0, cfg.GetAffinityDelta(CharacterIds.VillageChiefWife, "personality_assertive"));
        }

        [Test]
        public void AffinityDelta_UnknownCharacter_ReturnsZero()
        {
            CharacterQuestionsConfig cfg = BuildMinimalConfig();
            Assert.AreEqual(0, cfg.GetAffinityDelta("unknown", "personality_gentle"));
            Assert.AreEqual(0, cfg.GetAffinityDelta(CharacterIds.VillageChiefWife, "unknown"));
        }

        [Test]
        public void GetQuestionsForCharacterLevel_ReturnsCorrectSet()
        {
            CharacterQuestionsConfig cfg = BuildMinimalConfig();
            IReadOnlyList<CharacterQuestionInfo> lv1 = cfg.GetQuestionsForCharacterLevel(CharacterIds.VillageChiefWife, 1);
            Assert.AreEqual(1, lv1.Count);
            Assert.AreEqual("q_vcw_lv1_01", lv1[0].QuestionId);
        }

        [Test]
        public void GetQuestionsForCharacterLevel_UnknownCharacter_ReturnsEmpty()
        {
            CharacterQuestionsConfig cfg = BuildMinimalConfig();
            Assert.AreEqual(0, cfg.GetQuestionsForCharacterLevel("nope", 1).Count);
            Assert.AreEqual(0, cfg.GetQuestionsForCharacterLevel(CharacterIds.VillageChiefWife, 99).Count);
        }

        [Test]
        public void GetQuestion_ReturnsCorrectInfo()
        {
            CharacterQuestionsConfig cfg = BuildMinimalConfig();
            CharacterQuestionInfo info = cfg.GetQuestion("q_vcw_lv1_01");
            Assert.IsNotNull(info);
            Assert.AreEqual(CharacterIds.VillageChiefWife, info.CharacterId);
            Assert.AreEqual(1, info.Level);
            Assert.AreEqual(4, info.Options.Count);
            // 選項都有 personality
            foreach (CharacterQuestionOption opt in info.Options)
                Assert.IsFalse(string.IsNullOrEmpty(opt.PersonalityId));
        }

        [Test]
        public void GetQuestion_UnknownId_ReturnsNull()
        {
            CharacterQuestionsConfig cfg = BuildMinimalConfig();
            Assert.IsNull(cfg.GetQuestion("nope"));
            Assert.IsNull(cfg.GetQuestion(null));
        }

        // ===== ADR-001 / ADR-002 A04：IGameData 契約斷言 =====

        [Test]
        public void CharacterQuestionData_ImplementsIGameData()
        {
            CharacterQuestionData entry = new CharacterQuestionData
            {
                id = 1,
                character_id = "village_chief_wife",
                level = 1,
                question_id = "q_vcw_lv1_01",
                prompt = "test prompt"
            };

            Assert.That(entry, Is.AssignableTo<KahaGameCore.GameData.IGameData>(),
                "CharacterQuestionData 必須實作 IGameData（ADR-001 / ADR-002 A04）");
            Assert.That(entry.ID, Is.Not.Zero,
                "CharacterQuestionData.ID 不得為 0（ADR-002 A04 反序列化要求）");
            Assert.That(entry.Key, Is.EqualTo("q_vcw_lv1_01"),
                "CharacterQuestionData.Key 應回傳 question_id");
        }

        [Test]
        public void CharacterQuestionOptionData_ImplementsIGameData()
        {
            CharacterQuestionOptionData entry = new CharacterQuestionOptionData
            {
                id = 1,
                question_id = "q_vcw_lv1_01",
                personality_id = "personality_gentle",
                text = "選項文字",
                response = "回應台詞"
            };

            Assert.That(entry, Is.AssignableTo<KahaGameCore.GameData.IGameData>(),
                "CharacterQuestionOptionData 必須實作 IGameData（ADR-001）");
            Assert.That(entry.ID, Is.Not.Zero,
                "CharacterQuestionOptionData.ID 不得為 0");
        }

        // ===== 助手 =====

        private static CharacterQuestionsConfig BuildMinimalConfig()
        {
            CharacterQuestionData[] questions = new CharacterQuestionData[]
            {
                new CharacterQuestionData
                {
                    id = 1,
                    character_id = "village_chief_wife",
                    level = 1,
                    question_id = "q_vcw_lv1_01",
                    prompt = "Hi?"
                }
            };

            CharacterQuestionOptionData[] options = new CharacterQuestionOptionData[]
            {
                new CharacterQuestionOptionData { id = 1, question_id = "q_vcw_lv1_01", personality_id = "personality_gentle",    text = "a", response = "r1" },
                new CharacterQuestionOptionData { id = 2, question_id = "q_vcw_lv1_01", personality_id = "personality_lively",    text = "b", response = "r2" },
                new CharacterQuestionOptionData { id = 3, question_id = "q_vcw_lv1_01", personality_id = "personality_calm",      text = "c", response = "r3" },
                new CharacterQuestionOptionData { id = 4, question_id = "q_vcw_lv1_01", personality_id = "personality_assertive", text = "d", response = "r4" },
            };

            // 偏好個性（使用 snake_case character_id 對應 JSON 格式）
            CharacterProfileData[] profiles = new CharacterProfileData[]
            {
                new CharacterProfileData { id = 1, character_id = "village_chief_wife", preferred_personality_id = "personality_gentle"    },
                new CharacterProfileData { id = 2, character_id = "farm_girl",          preferred_personality_id = "personality_lively"    },
                new CharacterProfileData { id = 3, character_id = "witch",              preferred_personality_id = "personality_calm"      },
                new CharacterProfileData { id = 4, character_id = "guard",              preferred_personality_id = "personality_assertive" },
            };

            // 好感度增量規則（VCW × 4 個性）
            PersonalityAffinityRuleData[] rules = new PersonalityAffinityRuleData[]
            {
                new PersonalityAffinityRuleData { id = 1,  character_id = "village_chief_wife", personality_id = "personality_gentle",    affinity_delta = 10 },
                new PersonalityAffinityRuleData { id = 2,  character_id = "village_chief_wife", personality_id = "personality_calm",      affinity_delta = 5  },
                new PersonalityAffinityRuleData { id = 3,  character_id = "village_chief_wife", personality_id = "personality_lively",    affinity_delta = 2  },
                new PersonalityAffinityRuleData { id = 4,  character_id = "village_chief_wife", personality_id = "personality_assertive", affinity_delta = 0  },
                new PersonalityAffinityRuleData { id = 5,  character_id = "farm_girl",          personality_id = "personality_lively",    affinity_delta = 10 },
                new PersonalityAffinityRuleData { id = 6,  character_id = "farm_girl",          personality_id = "personality_assertive", affinity_delta = 5  },
                new PersonalityAffinityRuleData { id = 7,  character_id = "farm_girl",          personality_id = "personality_gentle",    affinity_delta = 2  },
                new PersonalityAffinityRuleData { id = 8,  character_id = "farm_girl",          personality_id = "personality_calm",      affinity_delta = 0  },
                new PersonalityAffinityRuleData { id = 9,  character_id = "witch",              personality_id = "personality_calm",      affinity_delta = 10 },
                new PersonalityAffinityRuleData { id = 10, character_id = "witch",              personality_id = "personality_gentle",    affinity_delta = 5  },
                new PersonalityAffinityRuleData { id = 11, character_id = "witch",              personality_id = "personality_assertive", affinity_delta = 2  },
                new PersonalityAffinityRuleData { id = 12, character_id = "witch",              personality_id = "personality_lively",    affinity_delta = 0  },
                new PersonalityAffinityRuleData { id = 13, character_id = "guard",              personality_id = "personality_assertive", affinity_delta = 10 },
                new PersonalityAffinityRuleData { id = 14, character_id = "guard",              personality_id = "personality_calm",      affinity_delta = 5  },
                new PersonalityAffinityRuleData { id = 15, character_id = "guard",              personality_id = "personality_lively",    affinity_delta = 2  },
                new PersonalityAffinityRuleData { id = 16, character_id = "guard",              personality_id = "personality_gentle",    affinity_delta = 0  },
            };

            return new CharacterQuestionsConfig(questions, options, profiles, rules);
        }
    }
}
