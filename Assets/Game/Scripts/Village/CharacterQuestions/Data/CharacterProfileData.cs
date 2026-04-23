// CharacterProfileData — 角色基本側資料主表 IGameData DTO（Sprint 8 Wave 2.5 新建，Q7 拍板）。
// 對應 Sheets 分頁：CharacterProfiles
// 對應 .txt 檔：characterprofiles.txt
// ADR-001 Q7 拍板：原 CharacterQuestionsConfig.character_personality_preference 物件拆為獨立分頁
// ADR-004：放 CharacterQuestions/Data/（資料來源為舊 CharacterQuestions config；未來可搬移）

using System;

namespace ProjectDR.Village.CharacterQuestions
{
    /// <summary>
    /// 角色基本側資料（JSON DTO）。
    /// 承接 Q7 拍板，記錄每個角色偏好的個性類型（preferred_personality_id）。
    /// 未來可擴充其他跨系統角色基本屬性。
    /// </summary>
    [Serializable]
    public class CharacterProfileData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>角色 ID 語意字串。</summary>
        public string character_id;

        /// <summary>語意字串 Key。</summary>
        public string Key => character_id;

        /// <summary>
        /// 該角色偏好的個性類型識別符。
        /// FK → Personalities.personality_id
        /// </summary>
        public string preferred_personality_id;
    }
}
