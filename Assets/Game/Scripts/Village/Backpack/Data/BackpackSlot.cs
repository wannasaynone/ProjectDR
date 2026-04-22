// BackpackSlot — 背包格子資料結構。
// 表示背包中單一格子的狀態，包含物品 ID 與數量。

namespace ProjectDR.Village.Backpack
{
    /// <summary>
    /// 背包格子資料結構（值型別）。
    /// 每個格子存放一種物品，記錄物品 ID 與當前數量。
    /// </summary>
    public struct BackpackSlot
    {
        /// <summary>格子中物品的 ID。空格子為 null。</summary>
        public string ItemId { get; }

        /// <summary>格子中物品的數量。空格子為 0。</summary>
        public int Quantity { get; }

        /// <summary>此格子是否為空。</summary>
        public bool IsEmpty => ItemId == null;

        public BackpackSlot(string itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }

        /// <summary>建立空格子。</summary>
        public static BackpackSlot Empty => new BackpackSlot(null, 0);
    }
}
