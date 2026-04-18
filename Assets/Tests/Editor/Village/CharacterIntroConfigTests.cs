using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// CharacterIntroConfig 的單元測試（B9）。
    /// 驗證 JSON DTO 反序列化、依 intro_id / character_id 分組、對話行排序。
    /// </summary>
    [TestFixture]
    public class CharacterIntroConfigTests
    {
        [Test]
        public void Constructor_NullData_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CharacterIntroConfig(null));
        }

        [Test]
        public void Constructor_EmptyData_ProducesNoIntros()
        {
            CharacterIntroConfigData data = new CharacterIntroConfigData
            {
                character_intros = new CharacterIntroData[0],
                character_intro_lines = new CharacterIntroLineData[0],
            };
            CharacterIntroConfig sut = new CharacterIntroConfig(data);
            Assert.AreEqual(0, sut.IntroIds.Count);
            Assert.IsNull(sut.GetIntro("unknown"));
            Assert.IsNull(sut.GetIntroByCharacter("unknown"));
        }

        [Test]
        public void Constructor_GroupsLinesByIntroIdAndSortsBySequence()
        {
            CharacterIntroConfigData data = new CharacterIntroConfigData
            {
                character_intros = new CharacterIntroData[]
                {
                    new CharacterIntroData
                    {
                        intro_id = "intro_a",
                        character_id = CharacterIds.FarmGirl,
                        cg_sprite_id = "cg_a",
                        scene_description = "scene a",
                        word_count_target = 500,
                    },
                    new CharacterIntroData
                    {
                        intro_id = "intro_b",
                        character_id = CharacterIds.Witch,
                        cg_sprite_id = "cg_b",
                        scene_description = "scene b",
                        word_count_target = 800,
                    },
                },
                character_intro_lines = new CharacterIntroLineData[]
                {
                    new CharacterIntroLineData { line_id = "l1", intro_id = "intro_a", sequence = 2, text = "a2", speaker = "narrator", line_type = "narration" },
                    new CharacterIntroLineData { line_id = "l2", intro_id = "intro_a", sequence = 1, text = "a1", speaker = "narrator", line_type = "narration" },
                    new CharacterIntroLineData { line_id = "l3", intro_id = "intro_b", sequence = 1, text = "b1", speaker = "Witch", line_type = "dialogue" },
                },
            };
            CharacterIntroConfig sut = new CharacterIntroConfig(data);

            CharacterIntroInfo a = sut.GetIntro("intro_a");
            Assert.IsNotNull(a);
            Assert.AreEqual(CharacterIds.FarmGirl, a.CharacterId);
            Assert.AreEqual("cg_a", a.CgSpriteId);
            Assert.AreEqual(2, a.Lines.Count);
            Assert.AreEqual("a1", a.Lines[0].text); // sorted by sequence
            Assert.AreEqual("a2", a.Lines[1].text);

            CharacterIntroInfo b = sut.GetIntroByCharacter(CharacterIds.Witch);
            Assert.IsNotNull(b);
            Assert.AreEqual("intro_b", b.IntroId);
            Assert.AreEqual(1, b.Lines.Count);
        }

        [Test]
        public void GetLineTexts_ReturnsAllLinesInOrder()
        {
            CharacterIntroConfigData data = new CharacterIntroConfigData
            {
                character_intros = new CharacterIntroData[]
                {
                    new CharacterIntroData { intro_id = "i1", character_id = CharacterIds.Guard },
                },
                character_intro_lines = new CharacterIntroLineData[]
                {
                    new CharacterIntroLineData { intro_id = "i1", sequence = 1, text = "one" },
                    new CharacterIntroLineData { intro_id = "i1", sequence = 2, text = "two" },
                    new CharacterIntroLineData { intro_id = "i1", sequence = 3, text = "three" },
                },
            };
            CharacterIntroConfig sut = new CharacterIntroConfig(data);
            string[] texts = sut.GetIntro("i1").GetLineTexts();
            Assert.AreEqual(new[] { "one", "two", "three" }, texts);
        }

        [Test]
        public void Constructor_NullIntroId_Ignored()
        {
            CharacterIntroConfigData data = new CharacterIntroConfigData
            {
                character_intros = new CharacterIntroData[]
                {
                    new CharacterIntroData { intro_id = null, character_id = CharacterIds.Guard },
                    new CharacterIntroData { intro_id = "", character_id = CharacterIds.Guard },
                    new CharacterIntroData { intro_id = "valid", character_id = CharacterIds.Guard },
                },
                character_intro_lines = new CharacterIntroLineData[0],
            };
            CharacterIntroConfig sut = new CharacterIntroConfig(data);
            Assert.AreEqual(1, sut.IntroIds.Count);
            Assert.IsNotNull(sut.GetIntro("valid"));
        }
    }
}
