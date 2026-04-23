using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.CharacterIntro;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// CharacterIntroConfig 的單元測試（B9）。
    /// Sprint 8 Wave 2.5：配合純陣列 DTO 重構（廢棄包裹類 CharacterIntroConfigData）。
    /// 驗證 JSON DTO 反序列化、依 intro_id / character_id 分組、對話行排序。
    /// </summary>
    [TestFixture]
    public class CharacterIntroConfigTests
    {
        [Test]
        public void Constructor_NullIntroEntries_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CharacterIntroConfig(null, new CharacterIntroLineData[0]));
        }

        [Test]
        public void Constructor_NullLineEntries_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CharacterIntroConfig(new CharacterIntroData[0], null));
        }

        [Test]
        public void Constructor_EmptyData_ProducesNoIntros()
        {
            CharacterIntroConfig sut = new CharacterIntroConfig(
                new CharacterIntroData[0],
                new CharacterIntroLineData[0]);
            Assert.AreEqual(0, sut.IntroIds.Count);
            Assert.IsNull(sut.GetIntro("unknown"));
            Assert.IsNull(sut.GetIntroByCharacter("unknown"));
        }

        [Test]
        public void Constructor_GroupsLinesByIntroIdAndSortsBySequence()
        {
            CharacterIntroData[] intros = new CharacterIntroData[]
            {
                new CharacterIntroData
                {
                    id = 1,
                    intro_id = "intro_a",
                    character_id = CharacterIds.FarmGirl,
                    cg_sprite_id = "cg_a",
                    scene_description = "scene a",
                    word_count_target = 500,
                },
                new CharacterIntroData
                {
                    id = 2,
                    intro_id = "intro_b",
                    character_id = CharacterIds.Witch,
                    cg_sprite_id = "cg_b",
                    scene_description = "scene b",
                    word_count_target = 800,
                },
            };

            CharacterIntroLineData[] lines = new CharacterIntroLineData[]
            {
                new CharacterIntroLineData { line_id = "l1", intro_id = "intro_a", sequence = 2, text = "a2", speaker = "narrator", line_type = "narration" },
                new CharacterIntroLineData { line_id = "l2", intro_id = "intro_a", sequence = 1, text = "a1", speaker = "narrator", line_type = "narration" },
                new CharacterIntroLineData { line_id = "l3", intro_id = "intro_b", sequence = 1, text = "b1", speaker = "Witch", line_type = "dialogue" },
            };

            CharacterIntroConfig sut = new CharacterIntroConfig(intros, lines);

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
            CharacterIntroData[] intros = new CharacterIntroData[]
            {
                new CharacterIntroData { id = 1, intro_id = "i1", character_id = CharacterIds.Guard },
            };

            CharacterIntroLineData[] lines = new CharacterIntroLineData[]
            {
                new CharacterIntroLineData { intro_id = "i1", sequence = 1, text = "one" },
                new CharacterIntroLineData { intro_id = "i1", sequence = 2, text = "two" },
                new CharacterIntroLineData { intro_id = "i1", sequence = 3, text = "three" },
            };

            CharacterIntroConfig sut = new CharacterIntroConfig(intros, lines);
            string[] texts = sut.GetIntro("i1").GetLineTexts();
            Assert.AreEqual(new[] { "one", "two", "three" }, texts);
        }

        [Test]
        public void Constructor_NullIntroId_Ignored()
        {
            CharacterIntroData[] intros = new CharacterIntroData[]
            {
                new CharacterIntroData { id = 0, intro_id = null, character_id = CharacterIds.Guard },
                new CharacterIntroData { id = 0, intro_id = "", character_id = CharacterIds.Guard },
                new CharacterIntroData { id = 3, intro_id = "valid", character_id = CharacterIds.Guard },
            };

            CharacterIntroConfig sut = new CharacterIntroConfig(intros, new CharacterIntroLineData[0]);
            Assert.AreEqual(1, sut.IntroIds.Count);
            Assert.IsNotNull(sut.GetIntro("valid"));
        }

        // ===== ADR-001 / ADR-002 A03：IGameData 契約斷言 =====

        [Test]
        public void CharacterIntroData_ImplementsIGameData()
        {
            CharacterIntroData entry = new CharacterIntroData
            {
                id = 5,
                intro_id = "intro_vcw",
                character_id = CharacterIds.VillageChiefWife,
                cg_sprite_id = "cg_vcw",
                scene_description = "test scene",
                word_count_target = 500,
            };

            // IGameData 介面斷言
            Assert.IsInstanceOf<KahaGameCore.GameData.IGameData>(entry,
                "CharacterIntroData 必須實作 IGameData（ADR-001 / ADR-002 A03）");
            // ID 非 0
            Assert.AreNotEqual(0, entry.ID,
                "CharacterIntroData.ID 不得為 0（ADR-002 A03 反序列化要求）");
            // Key 與 intro_id 一致
            Assert.AreEqual(entry.intro_id, entry.Key,
                "CharacterIntroData.Key 應回傳與 intro_id 相同的語意字串");
        }

        [Test]
        public void CharacterIntroLineData_ImplementsIGameData()
        {
            CharacterIntroLineData entry = new CharacterIntroLineData
            {
                id = 1,
                line_id = "l_001",
                intro_id = "intro_vcw",
                sequence = 1,
                speaker = "narrator",
                text = "test",
                line_type = "narration"
            };

            Assert.IsInstanceOf<KahaGameCore.GameData.IGameData>(entry,
                "CharacterIntroLineData 必須實作 IGameData（ADR-001）");
            Assert.AreNotEqual(0, entry.ID,
                "CharacterIntroLineData.ID 不得為 0");
            Assert.AreEqual("l_001", entry.Key,
                "CharacterIntroLineData.Key 應回傳 line_id");
        }
    }
}
