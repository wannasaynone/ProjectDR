// GiftSwordData — 劍禮物外部配置的 IGameData DTO（Sprint 8 Wave 2.5 新建）。
// 對應 Sheets 分頁：GiftSwords
// 對應 .txt 檔：giftswords.txt
// Namespace：ProjectDR.Village.Gift
// ADR-001：implements IGameData（id 流水號主鍵 + sword_id 語意字串）
// ADR-004：放 Gift/Data/

using System;

namespace ProjectDR.Village.Gift
{
    /// <summary>
    /// 劍禮物設定資料（JSON DTO）。
    /// 欄位命名對應 GiftSwords 分頁 header。
    /// </summary>
    [Serializable]
    public class GiftSwordData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>劍識別符語意字串。</summary>
        public string sword_id;

        /// <summary>語意字串 Key（與 Key 屬性同值）。</summary>
        public string Key => sword_id;

        /// <summary>顯示名稱。</summary>
        public string display_name;

        /// <summary>ATK 加成。</summary>
        public int atk_bonus;

        /// <summary>冷卻修正（秒）。</summary>
        public float cooldown_modifier_seconds;

        /// <summary>攻擊範圍修正。</summary>
        public float range_modifier;

        /// <summary>攻擊角度修正（度）。</summary>
        public float angle_modifier_degrees;

        /// <summary>特殊效果識別符（可為空）。</summary>
        public string special_effect;

        /// <summary>設計備忘（可為空）。</summary>
        public string description;
    }
}
