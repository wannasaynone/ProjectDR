// PersonalityData — 個性類型主表 IGameData DTO（Sprint 8 Wave 2.5 新建，Q7 拍板）。
// 對應 Sheets 分頁：Personalities
// 對應 .txt 檔：personalities.txt
// ADR-001 Q7 拍板：原 CharacterQuestionsConfig.personality_types 陣列拆為獨立分頁
// ADR-004：放 CharacterQuestions/Data/

using System;

namespace ProjectDR.Village.CharacterQuestions
{
    /// <summary>
    /// 個性類型資料（JSON DTO）。
    /// 4 筆：gentle / calm / lively / assertive。
    /// </summary>
    [Serializable]
    public class PersonalityData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>個性類型識別符語意字串。</summary>
        public string personality_id;

        /// <summary>語意字串 Key。</summary>
        public string Key => personality_id;

        /// <summary>顯示名稱（繁中）。</summary>
        public string display_name;

        /// <summary>設計說明。</summary>
        public string description;
    }
}
