// PersonalityAffinityRuleData — 個性×角色好感度規則關聯表 IGameData DTO（Sprint 8 Wave 2.5 新建，Q7 拍板）。
// 對應 Sheets 分頁：PersonalityAffinityRules
// 對應 .txt 檔：personalityaffinityrules.txt
// ADR-001 Q7 拍板：原 CharacterQuestionsConfig.personality_affinity_map 巢狀物件拆為獨立分頁
// ADR-004：放 CharacterQuestions/Data/

using System;

namespace ProjectDR.Village.CharacterQuestions
{
    /// <summary>
    /// 個性 × 角色好感度增減規則（關聯表 JSON DTO）。
    /// 記錄「某角色對某個性的好感度增量」。
    /// 約 4 角色 × 4 個性 = 16 列。
    /// </summary>
    [Serializable]
    public class PersonalityAffinityRuleData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>
        /// FK → CharacterProfiles.character_id（目標角色）。
        /// </summary>
        public string character_id;

        /// <summary>
        /// FK → Personalities.personality_id（選項個性）。
        /// </summary>
        public string personality_id;

        /// <summary>對該角色的好感度增減值。</summary>
        public int affinity_delta;
    }
}
