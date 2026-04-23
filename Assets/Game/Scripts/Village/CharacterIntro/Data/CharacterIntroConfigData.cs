// CharacterIntroConfigData — 角色登場 CG + 短劇情配置的 IGameData DTO 與不可變配置物件。
// 對應 Sheets 分頁：CharacterIntros（主表）/ CharacterIntroLines（子表）
// 對應 .txt 檔：characterintros.txt / characterintrolines.txt
//
// Sprint 8 Wave 2.5 重構：
//   - 廢棄包裹類 CharacterIntroConfigData（純陣列格式，JsonFx 各分頁獨立反序列化）
//   - CharacterIntroData 保留（已有 IGameData + int id + intro_id）
//   - CharacterIntroLineData 加 int id + IGameData 實作（子表自身流水號）
//   - CharacterIntroConfig 建構子改為接受兩個獨立陣列
// ADR-001 / ADR-002 A03

using System;
using System.Collections.Generic;

namespace ProjectDR.Village.CharacterIntro
{
    // ===== 對話行類型常數 =====

    /// <summary>角色登場對話行的類型常數（對應 JSON line_type 欄位）。</summary>
    public static class CharacterIntroLineTypes
    {
        /// <summary>旁白（narrator speaker）。</summary>
        public const string Narration = "narration";

        /// <summary>角色台詞（一般對話行）。</summary>
        public const string Dialogue = "dialogue";
    }

    // ===== JSON DTO（供 JsonFx 反序列化純陣列使用） =====

    /// <summary>
    /// 單一角色的登場 CG + 場景描述資料（JSON DTO，主表）。
    /// 實作 IGameData，int id 為流水號主鍵，intro_id 為語意字串外鍵。
    /// 對應 Sheets 分頁 CharacterIntros，.txt 檔 characterintros.txt。
    /// </summary>
    [Serializable]
    public class CharacterIntroData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。回傳 int id 流水號。</summary>
        public int ID => id;

        /// <summary>登場識別符（語意字串外鍵）。</summary>
        public string intro_id;

        /// <summary>語意字串 Key。</summary>
        public string Key => intro_id;

        /// <summary>角色 ID（對應 CharacterIds）。</summary>
        public string character_id;

        /// <summary>登場 CG sprite ID（播放時用於載入 sprite）。</summary>
        public string cg_sprite_id;

        /// <summary>場景描述（美術 CG 參考用）。</summary>
        public string scene_description;

        /// <summary>字數目標。</summary>
        public int word_count_target;
    }

    /// <summary>
    /// 角色登場劇情的單一對話行（JSON DTO，子表）。
    /// 實作 IGameData，int id 為子表自身流水號主鍵。
    /// FK：intro_id → CharacterIntros.intro_id。
    /// 對應 Sheets 分頁 CharacterIntroLines，.txt 檔 characterintrolines.txt。
    /// </summary>
    [Serializable]
    public class CharacterIntroLineData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（子表自身流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>對話行識別符語意字串。</summary>
        public string line_id;

        /// <summary>語意字串 Key。</summary>
        public string Key => line_id;

        /// <summary>FK 至主表 CharacterIntros.intro_id。</summary>
        public string intro_id;

        /// <summary>intro 內播放順序（升序）。</summary>
        public int sequence;

        /// <summary>說話者（角色 ID 或 narrator / player）。</summary>
        public string speaker;

        /// <summary>對話文字。</summary>
        public string text;

        /// <summary>行類型（CharacterIntroLineTypes）。</summary>
        public string line_type;
    }

    // ===== 不可變配置物件 =====

    /// <summary>
    /// 單一角色登場劇情資料（不可變）。
    /// 包含 CG sprite ID、場景描述、依 sequence 排序的對話行。
    /// </summary>
    public class CharacterIntroInfo
    {
        /// <summary>介紹 ID。</summary>
        public string IntroId { get; }

        /// <summary>角色 ID。</summary>
        public string CharacterId { get; }

        /// <summary>登場 CG sprite ID。</summary>
        public string CgSpriteId { get; }

        /// <summary>場景描述。</summary>
        public string SceneDescription { get; }

        /// <summary>依 sequence 排序的對話行。</summary>
        public IReadOnlyList<CharacterIntroLineData> Lines { get; }

        public CharacterIntroInfo(
            string introId,
            string characterId,
            string cgSpriteId,
            string sceneDescription,
            IReadOnlyList<CharacterIntroLineData> lines)
        {
            IntroId = introId;
            CharacterId = characterId;
            CgSpriteId = cgSpriteId;
            SceneDescription = sceneDescription;
            Lines = lines;
        }

        /// <summary>取得所有對話行的純文字陣列（順序與 Lines 相同）。供 DialogueData 使用。</summary>
        public string[] GetLineTexts()
        {
            if (Lines == null || Lines.Count == 0)
            {
                return Array.Empty<string>();
            }
            string[] texts = new string[Lines.Count];
            for (int i = 0; i < Lines.Count; i++)
            {
                texts[i] = Lines[i]?.text ?? string.Empty;
            }
            return texts;
        }
    }

    /// <summary>
    /// 角色登場 CG + 短劇情的不可變配置。
    /// 從兩個純陣列 DTO（主表 CharacterIntroData[] + 子表 CharacterIntroLineData[]）建構。
    /// </summary>
    public class CharacterIntroConfig
    {
        private readonly Dictionary<string, CharacterIntroInfo> _introsById;
        private readonly Dictionary<string, CharacterIntroInfo> _introsByCharacter;

        /// <summary>所有 intro 的 ID 清單。</summary>
        public IReadOnlyCollection<string> IntroIds => _introsById.Keys;

        /// <summary>
        /// 從純陣列 DTO 建構不可變配置。
        /// </summary>
        /// <param name="introEntries">主表 JsonFx 反序列化後的陣列（不可為 null）。</param>
        /// <param name="lineEntries">子表 JsonFx 反序列化後的陣列（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">任一陣列為 null 時拋出。</exception>
        public CharacterIntroConfig(CharacterIntroData[] introEntries, CharacterIntroLineData[] lineEntries)
        {
            if (introEntries == null)
            {
                throw new ArgumentNullException(nameof(introEntries));
            }
            if (lineEntries == null)
            {
                throw new ArgumentNullException(nameof(lineEntries));
            }

            _introsById = new Dictionary<string, CharacterIntroInfo>();
            _introsByCharacter = new Dictionary<string, CharacterIntroInfo>();

            // 依 intro_id 分組對話行並排序
            Dictionary<string, List<CharacterIntroLineData>> linesByIntro = new Dictionary<string, List<CharacterIntroLineData>>();
            foreach (CharacterIntroLineData line in lineEntries)
            {
                if (line == null || string.IsNullOrEmpty(line.intro_id)) continue;
                if (!linesByIntro.TryGetValue(line.intro_id, out List<CharacterIntroLineData> bucket))
                {
                    bucket = new List<CharacterIntroLineData>();
                    linesByIntro[line.intro_id] = bucket;
                }
                bucket.Add(line);
            }

            foreach (KeyValuePair<string, List<CharacterIntroLineData>> kvp in linesByIntro)
            {
                kvp.Value.Sort((CharacterIntroLineData a, CharacterIntroLineData b)
                    => a.sequence.CompareTo(b.sequence));
            }

            // 建立每位角色的 intro info
            foreach (CharacterIntroData intro in introEntries)
            {
                if (intro == null || string.IsNullOrEmpty(intro.intro_id)) continue;

                IReadOnlyList<CharacterIntroLineData> linesReadonly;
                if (linesByIntro.TryGetValue(intro.intro_id, out List<CharacterIntroLineData> foundLines))
                {
                    linesReadonly = foundLines.AsReadOnly();
                }
                else
                {
                    linesReadonly = Array.AsReadOnly(Array.Empty<CharacterIntroLineData>());
                }

                CharacterIntroInfo info = new CharacterIntroInfo(
                    intro.intro_id,
                    intro.character_id ?? string.Empty,
                    intro.cg_sprite_id ?? string.Empty,
                    intro.scene_description ?? string.Empty,
                    linesReadonly);

                _introsById[intro.intro_id] = info;

                if (!string.IsNullOrEmpty(intro.character_id))
                {
                    _introsByCharacter[intro.character_id] = info;
                }
            }
        }

        /// <summary>依 intro_id 取得資訊。找不到時回傳 null。</summary>
        public CharacterIntroInfo GetIntro(string introId)
        {
            if (string.IsNullOrEmpty(introId)) return null;
            _introsById.TryGetValue(introId, out CharacterIntroInfo info);
            return info;
        }

        /// <summary>依 character_id 取得資訊。找不到時回傳 null。</summary>
        public CharacterIntroInfo GetIntroByCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            _introsByCharacter.TryGetValue(characterId, out CharacterIntroInfo info);
            return info;
        }
    }
}
