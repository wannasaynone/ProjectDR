// CharacterIntroConfigData — 角色登場 CG + 短劇情配置的 JSON DTO 與不可變配置物件。
// 配置檔路徑：Assets/Game/Resources/Config/character-intro-config.json
// 資料結構對應 A1 設計師產出：每位角色的登場資料（CG sprite_id + scene_description）
// 與對話行（line_id / intro_id / sequence / speaker / text / line_type）。
//
// B9 OpeningSequenceController 依此讀取村長夫人 intro 資料作為開場；
// B13 登場 CG 播放系統將延伸使用此配置播放任一角色首次進入的登場 CG + 短劇情。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village
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

    // ===== JSON DTO（供 JsonUtility.FromJson 使用） =====

    /// <summary>單一角色的登場 CG + 場景描述資料（JSON DTO）。</summary>
    [Serializable]
    public class CharacterIntroData
    {
        /// <summary>唯一 ID（例：intro_village_chief_wife）。</summary>
        public string intro_id;

        /// <summary>角色 ID（對應 CharacterIds）。</summary>
        public string character_id;

        /// <summary>登場 CG sprite ID（B13 播放時用於載入 sprite）。</summary>
        public string cg_sprite_id;

        /// <summary>場景描述（美術 CG 參考用）。</summary>
        public string scene_description;

        /// <summary>字數目標（G5 規格 500~1500 字）。</summary>
        public int word_count_target;
    }

    /// <summary>
    /// 角色登場劇情的單一對話行（JSON DTO）。
    /// 欄位命名對應 character-intro-config.json。
    /// </summary>
    [Serializable]
    public class CharacterIntroLineData
    {
        /// <summary>對話行唯一識別。</summary>
        public string line_id;

        /// <summary>所屬 intro ID（對應 CharacterIntroData.intro_id）。</summary>
        public string intro_id;

        /// <summary>intro 內播放順序（升序）。</summary>
        public int sequence;

        /// <summary>說話者（角色 ID 或 narrator）。</summary>
        public string speaker;

        /// <summary>對話文字。</summary>
        public string text;

        /// <summary>行類型（CharacterIntroLineTypes）。</summary>
        public string line_type;
    }

    /// <summary>角色登場 CG + 短劇情的完整外部配置（JSON DTO）。</summary>
    [Serializable]
    public class CharacterIntroConfigData
    {
        /// <summary>資料結構版本。</summary>
        public int schema_version;

        /// <summary>配置說明（撰寫者備註）。</summary>
        public string note;

        /// <summary>所有角色的登場資料。</summary>
        public CharacterIntroData[] character_intros;

        /// <summary>所有角色的登場對話行。</summary>
        public CharacterIntroLineData[] character_intro_lines;
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

        /// <summary>登場 CG sprite ID（B13 使用）。</summary>
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
    /// 從 CharacterIntroConfigData（JSON DTO）建構，提供依 intro_id / character_id 查詢 API。
    /// </summary>
    public class CharacterIntroConfig
    {
        private readonly Dictionary<string, CharacterIntroInfo> _introsById;
        private readonly Dictionary<string, CharacterIntroInfo> _introsByCharacter;

        /// <summary>所有 intro 的 ID 清單。</summary>
        public IReadOnlyCollection<string> IntroIds => _introsById.Keys;

        /// <summary>
        /// 從 JSON DTO 建構不可變配置。
        /// </summary>
        /// <param name="data">JSON 反序列化後的 DTO（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">data 為 null 時拋出。</exception>
        public CharacterIntroConfig(CharacterIntroConfigData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _introsById = new Dictionary<string, CharacterIntroInfo>();
            _introsByCharacter = new Dictionary<string, CharacterIntroInfo>();

            CharacterIntroData[] intros = data.character_intros ?? Array.Empty<CharacterIntroData>();
            CharacterIntroLineData[] allLines = data.character_intro_lines ?? Array.Empty<CharacterIntroLineData>();

            // 依 intro_id 分組對話行並排序
            Dictionary<string, List<CharacterIntroLineData>> linesByIntro = new Dictionary<string, List<CharacterIntroLineData>>();
            foreach (CharacterIntroLineData line in allLines)
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
            foreach (CharacterIntroData intro in intros)
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
