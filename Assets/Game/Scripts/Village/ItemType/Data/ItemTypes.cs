// ItemTypes — 物品分類常數定義。
// 用於 ItemTypeResolver 的分類字串識別。

namespace ProjectDR.Village.ItemType
{
    /// <summary>
    /// 物品分類常數。
    /// 與 ItemTypeResolver 搭配使用，標記各物品的類別。
    /// </summary>
    public static class ItemTypes
    {
        /// <summary>種子類，可種植於農田。</summary>
        public const string Seed = "種子";

        /// <summary>食材類，可用於烹飪製作。</summary>
        public const string Ingredient = "食材";

        /// <summary>食品類，可直接使用或贈禮。</summary>
        public const string Food = "食品";

        /// <summary>藥劑類，具有特殊效果。</summary>
        public const string Potion = "藥劑";

        /// <summary>素材類，用於製作或建造。</summary>
        public const string Material = "素材";

        /// <summary>其他類，未歸入特定分類的物品。</summary>
        public const string Other = "其他";
    }
}
