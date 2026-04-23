// StorageExpansionRequirementData — 倉庫擴建需求子表 IGameData DTO（Sprint 8 Wave 2.5 新建）。
// 對應 Sheets 分頁：StorageExpansionRequirements
// 對應 .txt 檔：storageexpansionrequirements.txt
// ADR-001 Q4 拍板：原 required_items 管道符字串拆為獨立子表
// ADR-004：放 Storage/Data/

using System;

namespace ProjectDR.Village.Storage
{
    /// <summary>
    /// 倉庫擴建所需物品（子表 JSON DTO）。
    /// FK：stage_level → StorageExpansionStages.level
    /// </summary>
    [Serializable]
    public class StorageExpansionRequirementData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（子表自身流水號）。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>FK 至主表 StorageExpansionStages.level。</summary>
        public int stage_level;

        /// <summary>所需物品 ID。</summary>
        public string item_id;

        /// <summary>所需數量。</summary>
        public int quantity;
    }
}
