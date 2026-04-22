// CharacterQuestionsConfig 單元測試（Sprint 5 B4/B9）。
//
// 驗證：
// - 建構 null 保護
// - 個性定義存取、偏好對應、好感度增量對應
// - 依 character/level 索引查詢
// - GetQuestion 查詢單題
// - 真實 JSON 反序列化（Resources/Config/character-questions-config.json）

using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.CharacterQuestions;
using ProjectDR.Village.Navigation;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectDR.Village.Tests
{
    [TestFixture]
    public class CharacterQuestionsConfigTests
    {
        [Test]
        public void Constructor_NullData_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new CharacterQuestionsConfig(null));
        }

        [Test]
        public void Constructor_EmptyData_CreatesValidInstance()
        {
            CharacterQuestionsConfig cfg = new CharacterQuestionsConfig(new CharacterQuestionsConfigData());
            Assert.AreEqual(0, new List<PersonalityType>(cfg.PersonalityTypes).Count);
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
                Assert.IsFalse(string.IsNullOrEmpty(opt.Personality));
        }

        [Test]
        public void GetQuestion_UnknownId_ReturnsNull()
        {
            CharacterQuestionsConfig cfg = BuildMinimalConfig();
            Assert.IsNull(cfg.GetQuestion("nope"));
            Assert.IsNull(cfg.GetQuestion(null));
        }

        [Test]
        public void RealJson_Loads_280Questions()
        {
            UnityEngine.TextAsset asset =
                UnityEngine.Resources.Load<UnityEngine.TextAsset>("Config/character-questions-config");
            if (asset == null)
            {
                Assert.Ignore("character-questions-config.json 不在 Resources，跳過真實 JSON 測試。");
                return;
            }
            CharacterQuestionsConfigData data = JsonUtility.FromJson<CharacterQuestionsConfigData>(asset.text);
            Assert.IsNotNull(data);
            CharacterQuestionsConfig cfg = new CharacterQuestionsConfig(data);

            // 280 題 = 4 角色 × 7 級 × 10 題
            int total = 0;
            foreach (string charId in new[]
            {
                CharacterIds.VillageChiefWife,
                CharacterIds.FarmGirl,
                CharacterIds.Witch,
                CharacterIds.Guard,
            })
            {
                for (int level = 1; level <= 7; level++)
                {
                    total += cfg.GetQuestionsForCharacterLevel(charId, level).Count;
                }
            }
            Assert.AreEqual(280, total);

            // 每角色偏好 = +10
            Assert.AreEqual(10, cfg.GetAffinityDelta(CharacterIds.VillageChiefWife, "personality_gentle"));
            Assert.AreEqual(10, cfg.GetAffinityDelta(CharacterIds.FarmGirl, "personality_lively"));
            Assert.AreEqual(10, cfg.GetAffinityDelta(CharacterIds.Witch, "personality_calm"));
            Assert.AreEqual(10, cfg.GetAffinityDelta(CharacterIds.Guard, "personality_assertive"));
        }

        // ===== ADR-001 / ADR-002 A04：IGameData 契約斷言 =====

        [Test]
        public void CharacterQuestionEntryData_ImplementsIGameData()
        {
            CharacterQuestionEntryData entry = new CharacterQuestionEntryData
            {
                id = 1,
                character_id = CharacterIds.VillageChiefWife,
                level = 1,
                question_id = "q_vcw_lv1_01",
                prompt = "test prompt",
                options = new CharacterQuestionOptionData[0]
            };

            Assert.That(entry, Is.AssignableTo<KahaGameCore.GameData.IGameData>(),
                "CharacterQuestionEntryData 必須實作 IGameData（ADR-001 / ADR-002 A04）");
            Assert.That(entry.ID, Is.Not.Zero,
                "CharacterQuestionEntryData.ID（=id）不得為 0（ADR-002 A04 反序列化要求）");
            Assert.That(entry.Key, Is.EqualTo("q_vcw_lv1_01"),
                "CharacterQuestionEntryData.Key 應回傳 question_id");
        }

        // ===== 助手 =====

        private static CharacterQuestionsConfig BuildMinimalConfig()
        {
            CharacterQuestionsConfigData data = new CharacterQuestionsConfigData
            {
                schema_version = 1,
                note = "test",
                personality_types = new PersonalityTypeData[]
                {
                    new PersonalityTypeData{ id="personality_gentle", name="溫柔型", description="" },
                    new PersonalityTypeData{ id="personality_lively", name="活潑型", description="" },
                    new PersonalityTypeData{ id="personality_calm", name="冷靜型", description="" },
                    new PersonalityTypeData{ id="personality_assertive", name="強勢型", description="" },
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
                        character_id = CharacterIds.VillageChiefWife,
                        level = 1,
                        question_id = "q_vcw_lv1_01",
                        prompt = "Hi?",
                        options = new CharacterQuestionOptionData[]
                        {
                            new CharacterQuestionOptionData{ personality="personality_gentle", text="a", response="r1" },
                            new CharacterQuestionOptionData{ personality="personality_lively", text="b", response="r2" },
                            new CharacterQuestionOptionData{ personality="personality_calm", text="c", response="r3" },
                            new CharacterQuestionOptionData{ personality="personality_assertive", text="d", response="r4" },
                        }
                    }
                },
            };
            return new CharacterQuestionsConfig(data);
        }
    }
}
