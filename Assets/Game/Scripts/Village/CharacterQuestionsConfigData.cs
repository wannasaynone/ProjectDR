// CharacterQuestionsConfigData — 角色發問 280 題配置的 JSON DTO 與不可變配置物件（Sprint 5 B4）。
// 配置檔路徑：Assets/Game/Resources/Config/character-questions-config.json
//
// 涵蓋內容：
// - 4 種個性類型定義（personality_gentle/lively/calm/assertive）
// - 4 角色個性偏好對應（character_personality_preference）
// - 4 角色 × 4 個性 → 好感度增量對應（personality_affinity_map）
// - 280 題角色發問（4 角色 × 7 級 × 10 題，每題 4 個性選項 + 每選項回應台詞）
//
// API：
// - GetPersonalityDefinitions / GetPersonalityPreference(charId)
// - GetAffinityDelta(charId, personalityId)
// - GetQuestionsForCharacterLevel(charId, level) → IReadOnlyList<CharacterQuestionInfo>
// - GetQuestion(questionId) → 單一題目
//
// ⚠️ 本配置中的所有題目均為 placeholder，待製作人後續透過 dialogue-writing-sop 流程撰寫正式內容。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village
{
    // ===== JSON DTO =====

    [Serializable]
    public class PersonalityTypeData
    {
        public string id;
        public string name;
        public string description;
    }

    [Serializable]
    public class CharacterPersonalityPreferenceData
    {
        public string village_chief_wife;
        public string farm_girl;
        public string witch;
        public string guard;
    }

    [Serializable]
    public class PersonalityAffinityEntryData
    {
        public int personality_gentle;
        public int personality_lively;
        public int personality_calm;
        public int personality_assertive;
    }

    [Serializable]
    public class PersonalityAffinityMapData
    {
        public PersonalityAffinityEntryData village_chief_wife;
        public PersonalityAffinityEntryData farm_girl;
        public PersonalityAffinityEntryData witch;
        public PersonalityAffinityEntryData guard;
    }

    [Serializable]
    public class CharacterQuestionOptionData
    {
        public string personality;
        public string text;
        public string response;
    }

    [Serializable]
    public class CharacterQuestionEntryData
    {
        public string character_id;
        public int level;
        public string question_id;
        public string prompt;
        public CharacterQuestionOptionData[] options;
    }

    [Serializable]
    public class CharacterQuestionsConfigData
    {
        public int schema_version;
        public string note;
        public PersonalityTypeData[] personality_types;
        public CharacterPersonalityPreferenceData character_personality_preference;
        public PersonalityAffinityMapData personality_affinity_map;
        public CharacterQuestionEntryData[] questions;
    }

    // ===== 不可變配置物件 =====

    /// <summary>個性類型定義（不可變）。</summary>
    public class PersonalityType
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }

        public PersonalityType(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }

    /// <summary>角色發問的單一選項（不可變）。</summary>
    public class CharacterQuestionOption
    {
        /// <summary>個性 ID（personality_gentle/lively/calm/assertive）。</summary>
        public string Personality { get; }
        /// <summary>選項顯示文字（UI 只顯示此欄位，不顯示好感度數值）。</summary>
        public string Text { get; }
        /// <summary>選擇後角色的回應台詞（打字機播放）。</summary>
        public string Response { get; }

        public CharacterQuestionOption(string personality, string text, string response)
        {
            Personality = personality;
            Text = text;
            Response = response;
        }
    }

    /// <summary>單一角色發問題目（不可變）。</summary>
    public class CharacterQuestionInfo
    {
        public string QuestionId { get; }
        public string CharacterId { get; }
        /// <summary>好感度等級（1~7）。</summary>
        public int Level { get; }
        public string Prompt { get; }
        /// <summary>4 個選項，一律按 personality_gentle/lively/calm/assertive 順序存放。</summary>
        public IReadOnlyList<CharacterQuestionOption> Options { get; }

        public CharacterQuestionInfo(
            string questionId,
            string characterId,
            int level,
            string prompt,
            IReadOnlyList<CharacterQuestionOption> options)
        {
            QuestionId = questionId;
            CharacterId = characterId;
            Level = level;
            Prompt = prompt;
            Options = options;
        }
    }

    /// <summary>
    /// 角色發問配置（不可變）。
    ///
    /// JSON 內 character_id 使用 snake_case（village_chief_wife / farm_girl / witch / guard），
    /// 但程式內一律使用 CharacterIds 常數（PascalCase）。本 Config 在建構時會透過
    /// CharacterIdSnakeCaseMapper 進行 snake_case → PascalCase 的映射。
    /// </summary>
    public class CharacterQuestionsConfig
    {
        private readonly Dictionary<string, PersonalityType> _personalityTypes;
        private readonly Dictionary<string, string> _preferences;                      // charId → personalityId（+10 對應）
        private readonly Dictionary<string, Dictionary<string, int>> _affinityMap;     // charId → (personalityId → delta)
        private readonly Dictionary<string, Dictionary<int, List<CharacterQuestionInfo>>> _byCharLevel;
        private readonly Dictionary<string, CharacterQuestionInfo> _byId;

        public CharacterQuestionsConfig(CharacterQuestionsConfigData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _personalityTypes = new Dictionary<string, PersonalityType>();
            _preferences = new Dictionary<string, string>();
            _affinityMap = new Dictionary<string, Dictionary<string, int>>();
            _byCharLevel = new Dictionary<string, Dictionary<int, List<CharacterQuestionInfo>>>();
            _byId = new Dictionary<string, CharacterQuestionInfo>();

            // 個性類型定義
            PersonalityTypeData[] types = data.personality_types ?? Array.Empty<PersonalityTypeData>();
            foreach (PersonalityTypeData t in types)
            {
                if (t == null || string.IsNullOrEmpty(t.id)) continue;
                _personalityTypes[t.id] = new PersonalityType(t.id, t.name ?? string.Empty, t.description ?? string.Empty);
            }

            // 角色偏好（+10 對應）
            CharacterPersonalityPreferenceData pref = data.character_personality_preference;
            if (pref != null)
            {
                if (!string.IsNullOrEmpty(pref.village_chief_wife))
                    _preferences[CharacterIds.VillageChiefWife] = pref.village_chief_wife;
                if (!string.IsNullOrEmpty(pref.farm_girl))
                    _preferences[CharacterIds.FarmGirl] = pref.farm_girl;
                if (!string.IsNullOrEmpty(pref.witch))
                    _preferences[CharacterIds.Witch] = pref.witch;
                if (!string.IsNullOrEmpty(pref.guard))
                    _preferences[CharacterIds.Guard] = pref.guard;
            }
            // 同時保留 snake_case key（方便測試直接用 snake_case 查詢）
            if (pref != null)
            {
                if (!string.IsNullOrEmpty(pref.village_chief_wife))
                    _preferences["village_chief_wife"] = pref.village_chief_wife;
                if (!string.IsNullOrEmpty(pref.farm_girl))
                    _preferences["farm_girl"] = pref.farm_girl;
                if (!string.IsNullOrEmpty(pref.witch))
                    _preferences["witch"] = pref.witch;
                if (!string.IsNullOrEmpty(pref.guard))
                    _preferences["guard"] = pref.guard;
            }

            // 好感度增量對應表
            PersonalityAffinityMapData affinity = data.personality_affinity_map;
            if (affinity != null)
            {
                // 同時註冊 PascalCase（CharacterIds）與 snake_case（JSON 原 key）
                RegisterAffinity(CharacterIds.VillageChiefWife, affinity.village_chief_wife);
                RegisterAffinity(CharacterIds.FarmGirl, affinity.farm_girl);
                RegisterAffinity(CharacterIds.Witch, affinity.witch);
                RegisterAffinity(CharacterIds.Guard, affinity.guard);
                RegisterAffinity("village_chief_wife", affinity.village_chief_wife);
                RegisterAffinity("farm_girl", affinity.farm_girl);
                RegisterAffinity("witch", affinity.witch);
                RegisterAffinity("guard", affinity.guard);
            }

            // 題目
            CharacterQuestionEntryData[] qs = data.questions ?? Array.Empty<CharacterQuestionEntryData>();
            foreach (CharacterQuestionEntryData q in qs)
            {
                if (q == null || string.IsNullOrEmpty(q.question_id) || string.IsNullOrEmpty(q.character_id)) continue;

                // 將 JSON 內的 snake_case character_id 映射為 CharacterIds 常數 (PascalCase)
                string canonicalCharId = CharacterIdSnakeCaseMapper.ToPascal(q.character_id);

                List<CharacterQuestionOption> options = new List<CharacterQuestionOption>();
                CharacterQuestionOptionData[] srcOpts = q.options ?? Array.Empty<CharacterQuestionOptionData>();
                foreach (CharacterQuestionOptionData opt in srcOpts)
                {
                    if (opt == null) continue;
                    options.Add(new CharacterQuestionOption(
                        opt.personality ?? string.Empty,
                        opt.text ?? string.Empty,
                        opt.response ?? string.Empty));
                }

                CharacterQuestionInfo info = new CharacterQuestionInfo(
                    q.question_id,
                    canonicalCharId,
                    q.level,
                    q.prompt ?? string.Empty,
                    options.AsReadOnly());
                _byId[q.question_id] = info;

                // 註冊兩個 key（PascalCase 與原 snake_case），使測試可用任一種查詢
                AddToCharLevel(canonicalCharId, q.level, info);
                if (canonicalCharId != q.character_id)
                {
                    AddToCharLevel(q.character_id, q.level, info);
                }
            }
        }

        private void AddToCharLevel(string charId, int level, CharacterQuestionInfo info)
        {
            if (!_byCharLevel.TryGetValue(charId, out Dictionary<int, List<CharacterQuestionInfo>> byLevel))
            {
                byLevel = new Dictionary<int, List<CharacterQuestionInfo>>();
                _byCharLevel[charId] = byLevel;
            }
            if (!byLevel.TryGetValue(level, out List<CharacterQuestionInfo> bucket))
            {
                bucket = new List<CharacterQuestionInfo>();
                byLevel[level] = bucket;
            }
            bucket.Add(info);
        }

        private void RegisterAffinity(string characterId, PersonalityAffinityEntryData entry)
        {
            if (entry == null) return;
            Dictionary<string, int> map = new Dictionary<string, int>
            {
                { "personality_gentle", entry.personality_gentle },
                { "personality_lively", entry.personality_lively },
                { "personality_calm", entry.personality_calm },
                { "personality_assertive", entry.personality_assertive },
            };
            _affinityMap[characterId] = map;
        }

        // ===== 公開 API =====

        /// <summary>所有個性類型定義。</summary>
        public IReadOnlyCollection<PersonalityType> PersonalityTypes => _personalityTypes.Values;

        /// <summary>該角色的 +10 偏好個性 ID。找不到時回 null。</summary>
        public string GetPersonalityPreference(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            return _preferences.TryGetValue(characterId, out string v) ? v : null;
        }

        /// <summary>
        /// 該角色選擇指定個性選項時應累加的好感度。
        /// 找不到配對時回傳 0。
        /// </summary>
        public int GetAffinityDelta(string characterId, string personalityId)
        {
            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(personalityId)) return 0;
            if (!_affinityMap.TryGetValue(characterId, out Dictionary<string, int> map)) return 0;
            return map.TryGetValue(personalityId, out int v) ? v : 0;
        }

        /// <summary>取得指定角色 × 等級的所有題目。找不到時回空列表。</summary>
        public IReadOnlyList<CharacterQuestionInfo> GetQuestionsForCharacterLevel(string characterId, int level)
        {
            if (string.IsNullOrEmpty(characterId)) return Array.AsReadOnly(Array.Empty<CharacterQuestionInfo>());
            if (!_byCharLevel.TryGetValue(characterId, out Dictionary<int, List<CharacterQuestionInfo>> byLevel))
                return Array.AsReadOnly(Array.Empty<CharacterQuestionInfo>());
            if (!byLevel.TryGetValue(level, out List<CharacterQuestionInfo> list))
                return Array.AsReadOnly(Array.Empty<CharacterQuestionInfo>());
            return list.AsReadOnly();
        }

        /// <summary>依 question_id 取得題目。找不到時回 null。</summary>
        public CharacterQuestionInfo GetQuestion(string questionId)
        {
            if (string.IsNullOrEmpty(questionId)) return null;
            return _byId.TryGetValue(questionId, out CharacterQuestionInfo info) ? info : null;
        }
    }
}
