// CharacterQuestionsConfigData — 角色發問 280 題配置的 IGameData DTO 與不可變配置物件。
// 對應 Sheets 分頁：CharacterQuestions（主表）/ CharacterQuestionOptions（子表）
// 對應 .txt 檔：characterquestions.txt / characterquestionoptions.txt
//
// Sprint 8 Wave 2.5 重構：
//   - CharacterQuestionEntryData 改名為 CharacterQuestionData（去 Entry）
//   - CharacterQuestionOptionData 全面重寫：加 int id + question_id FK + IGameData
//   - 廢棄 CharacterQuestionsConfigData 包裹類及其 personality_types/preference/map metadata 欄位
//   - 廢棄 PersonalityTypeData/CharacterPersonalityPreferenceData/PersonalityAffinityMapData/PersonalityAffinityEntryData
//     （已各自遷至 PersonalityData / CharacterProfileData / PersonalityAffinityRuleData 獨立分頁）
//   - CharacterQuestionsConfig 改為接受兩個純陣列 DTO
//   - personality 偏好 / affinity delta 改由外部傳入（透過 CharacterProfileData[] + PersonalityAffinityRuleData[]）
// ADR-001 / ADR-002 A04

using System;
using System.Collections.Generic;
using KahaGameCore.GameData;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.CharacterUnlock;

namespace ProjectDR.Village.CharacterQuestions
{
    // ===== JSON DTO（供 JsonFx 反序列化純陣列使用） =====

    /// <summary>
    /// 單一角色發問題目（JSON DTO，主表）。
    /// 實作 IGameData，int id 為流水號主鍵，question_id 為語意字串外鍵。
    /// 對應 Sheets 分頁 CharacterQuestions，.txt 檔 characterquestions.txt。
    /// </summary>
    [Serializable]
    public class CharacterQuestionData : IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。回傳 int id 流水號。</summary>
        public int ID => id;

        /// <summary>問題語意識別符（語意字串外鍵）。</summary>
        public string question_id;

        /// <summary>語意字串 Key。</summary>
        public string Key => question_id;

        /// <summary>詢問者角色 ID。</summary>
        public string character_id;

        /// <summary>好感度等級（1~7）。</summary>
        public int level;

        /// <summary>問題文字。</summary>
        public string prompt;
    }

    /// <summary>
    /// 角色發問選項（JSON DTO，子表）。
    /// 實作 IGameData，int id 為子表自身流水號主鍵。
    /// FK：question_id → CharacterQuestions.question_id；personality_id → Personalities.personality_id。
    /// 對應 Sheets 分頁 CharacterQuestionOptions，.txt 檔 characterquestionoptions.txt。
    /// </summary>
    [Serializable]
    public class CharacterQuestionOptionData : IGameData
    {
        /// <summary>IGameData 主鍵（子表自身流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>FK 至主表 CharacterQuestions.question_id。</summary>
        public string question_id;

        /// <summary>FK 至 Personalities.personality_id（四選項對應四個性）。</summary>
        public string personality_id;

        /// <summary>選項文字（UI 顯示）。</summary>
        public string text;

        /// <summary>選擇後角色的回應台詞（打字機播放）。</summary>
        public string response;
    }

    // ===== 不可變配置物件 =====

    /// <summary>角色發問的單一選項（不可變）。</summary>
    public class CharacterQuestionOption
    {
        /// <summary>個性 ID（personality_gentle / lively / calm / assertive 等）。</summary>
        public string PersonalityId { get; }

        /// <summary>選項顯示文字。</summary>
        public string Text { get; }

        /// <summary>選擇後角色的回應台詞。</summary>
        public string Response { get; }

        public CharacterQuestionOption(string personalityId, string text, string response)
        {
            PersonalityId = personalityId;
            Text = text;
            Response = response;
        }

        // 向後相容屬性（舊 Personality 名稱，供既有 CharacterQuestionsManager 使用）
        public string Personality => PersonalityId;
    }

    /// <summary>單一角色發問題目（不可變）。</summary>
    public class CharacterQuestionInfo
    {
        public string QuestionId { get; }
        public string CharacterId { get; }
        /// <summary>好感度等級（1~7）。</summary>
        public int Level { get; }
        public string Prompt { get; }
        /// <summary>4 個選項（依 personality_id 順序）。</summary>
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
    /// 建構子接受：
    ///   - CharacterQuestionData[]（主表，280 題）
    ///   - CharacterQuestionOptionData[]（子表，1120 選項）
    ///   - CharacterProfileData[]（角色偏好個性）
    ///   - PersonalityAffinityRuleData[]（個性 × 角色好感度增量）
    ///
    /// JSON 內 character_id 使用 snake_case（village_chief_wife / farm_girl / witch / guard），
    /// 程式內一律透過 CharacterIdSnakeCaseMapper 映射為 CharacterIds 常數（PascalCase）。
    /// </summary>
    public class CharacterQuestionsConfig
    {
        private readonly Dictionary<string, string> _preferences;                      // charId → preferredPersonalityId
        private readonly Dictionary<string, Dictionary<string, int>> _affinityMap;     // charId → (personalityId → delta)
        private readonly Dictionary<string, Dictionary<int, List<CharacterQuestionInfo>>> _byCharLevel;
        private readonly Dictionary<string, CharacterQuestionInfo> _byId;

        public CharacterQuestionsConfig(
            CharacterQuestionData[] questionEntries,
            CharacterQuestionOptionData[] optionEntries,
            CharacterProfileData[] profileEntries,
            PersonalityAffinityRuleData[] affinityRuleEntries)
        {
            if (questionEntries == null) throw new ArgumentNullException(nameof(questionEntries));
            if (optionEntries == null) throw new ArgumentNullException(nameof(optionEntries));
            if (profileEntries == null) throw new ArgumentNullException(nameof(profileEntries));
            if (affinityRuleEntries == null) throw new ArgumentNullException(nameof(affinityRuleEntries));

            _preferences = new Dictionary<string, string>();
            _affinityMap = new Dictionary<string, Dictionary<string, int>>();
            _byCharLevel = new Dictionary<string, Dictionary<int, List<CharacterQuestionInfo>>>();
            _byId = new Dictionary<string, CharacterQuestionInfo>();

            // 個性偏好（CharacterProfiles）
            foreach (CharacterProfileData profile in profileEntries)
            {
                if (profile == null || string.IsNullOrEmpty(profile.character_id)) continue;
                string canonical = CharacterIdSnakeCaseMapper.ToPascal(profile.character_id);
                if (!string.IsNullOrEmpty(profile.preferred_personality_id))
                {
                    _preferences[canonical] = profile.preferred_personality_id;
                    _preferences[profile.character_id] = profile.preferred_personality_id; // 保留 snake_case key
                }
            }

            // 好感度增量（PersonalityAffinityRules）
            foreach (PersonalityAffinityRuleData rule in affinityRuleEntries)
            {
                if (rule == null || string.IsNullOrEmpty(rule.character_id) || string.IsNullOrEmpty(rule.personality_id)) continue;
                string canonical = CharacterIdSnakeCaseMapper.ToPascal(rule.character_id);
                RegisterAffinityDelta(canonical, rule.personality_id, rule.affinity_delta);
                RegisterAffinityDelta(rule.character_id, rule.personality_id, rule.affinity_delta);
            }

            // 分組選項（question_id → 選項列表）
            Dictionary<string, List<CharacterQuestionOptionData>> optionsByQuestion =
                new Dictionary<string, List<CharacterQuestionOptionData>>();
            foreach (CharacterQuestionOptionData opt in optionEntries)
            {
                if (opt == null || string.IsNullOrEmpty(opt.question_id)) continue;
                if (!optionsByQuestion.TryGetValue(opt.question_id, out List<CharacterQuestionOptionData> bucket))
                {
                    bucket = new List<CharacterQuestionOptionData>();
                    optionsByQuestion[opt.question_id] = bucket;
                }
                bucket.Add(opt);
            }

            // 建立題目
            foreach (CharacterQuestionData q in questionEntries)
            {
                if (q == null || string.IsNullOrEmpty(q.question_id) || string.IsNullOrEmpty(q.character_id)) continue;

                string canonicalCharId = CharacterIdSnakeCaseMapper.ToPascal(q.character_id);

                List<CharacterQuestionOption> options = new List<CharacterQuestionOption>();
                if (optionsByQuestion.TryGetValue(q.question_id, out List<CharacterQuestionOptionData> srcOpts))
                {
                    foreach (CharacterQuestionOptionData opt in srcOpts)
                    {
                        if (opt == null) continue;
                        options.Add(new CharacterQuestionOption(
                            opt.personality_id ?? string.Empty,
                            opt.text ?? string.Empty,
                            opt.response ?? string.Empty));
                    }
                }

                CharacterQuestionInfo info = new CharacterQuestionInfo(
                    q.question_id,
                    canonicalCharId,
                    q.level,
                    q.prompt ?? string.Empty,
                    options.AsReadOnly());
                _byId[q.question_id] = info;

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

        private void RegisterAffinityDelta(string characterId, string personalityId, int delta)
        {
            if (!_affinityMap.TryGetValue(characterId, out Dictionary<string, int> map))
            {
                map = new Dictionary<string, int>();
                _affinityMap[characterId] = map;
            }
            map[personalityId] = delta;
        }

        // ===== 公開 API =====

        /// <summary>該角色的偏好個性 ID（+10 對應）。找不到時回 null。</summary>
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
