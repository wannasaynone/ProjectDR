// MainQuestUnlockData — 主線任務解鎖規則子表 IGameData DTO（Sprint 8 Wave 2.5 新建）。
// 對應 Sheets 分頁：MainQuestUnlocks
// 對應 .txt 檔：mainquestunlocks.txt
// ADR-001 Q3 拍板：原 unlock_on_complete 管道符字串拆為獨立子表
// ADR-004：放 MainQuest/Data/

using System;

namespace ProjectDR.Village.MainQuest
{
    /// <summary>
    /// 主線任務解鎖規則（子表 JSON DTO）。
    /// FK：main_quest_id → MainQuests.quest_id
    /// </summary>
    [Serializable]
    public class MainQuestUnlockData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（子表自身流水號）。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>FK 至主表 MainQuests.quest_id。</summary>
        public string main_quest_id;

        /// <summary>解鎖類型（"quest" / "event" / "feature"）。</summary>
        public string unlock_type;

        /// <summary>解鎖值（依 unlock_type 解釋）。</summary>
        public string unlock_value;

        /// <summary>同一 quest 下解鎖觸發順序（可選，0 表示未指定）。</summary>
        public int sort_order;
    }
}
