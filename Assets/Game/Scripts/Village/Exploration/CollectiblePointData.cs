// CollectiblePointData — 探索點靜態資料。
// 描述單一探索點的採集時間與可獲得物品清單。
// IT 階段例外：探索點資料隨地圖 JSON 載入，不經由 Google Sheets 管理。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// 探索點中單一物品欄位的資料定義。
    /// </summary>
    public class CollectibleItemEntry
    {
        /// <summary>物品 ID。</summary>
        public string ItemId { get; }

        /// <summary>物品數量。</summary>
        public int Quantity { get; }

        /// <summary>解鎖所需時間（秒）。對應 GDD 規則 10, 11（第二層計時）。</summary>
        public float UnlockDurationSeconds { get; }

        public CollectibleItemEntry(string itemId, int quantity, float unlockDurationSeconds)
        {
            if (string.IsNullOrEmpty(itemId))
                throw new ArgumentException("itemId must not be null or empty.", nameof(itemId));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "quantity must be greater than 0.");
            if (unlockDurationSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(unlockDurationSeconds),
                    "unlockDurationSeconds must be >= 0.");

            ItemId = itemId;
            Quantity = quantity;
            UnlockDurationSeconds = unlockDurationSeconds;
        }
    }

    /// <summary>
    /// 探索點靜態資料。包含採集時間（第一層計時）與物品清單（第二層計時）。
    /// 對應 GDD 規則 8-11。
    /// </summary>
    public class CollectiblePointData
    {
        /// <summary>探索點所在位置 X。</summary>
        public int X { get; }

        /// <summary>探索點所在位置 Y。</summary>
        public int Y { get; }

        /// <summary>採集所需時間（秒）。對應 GDD 規則 9（第一層計時）。0 表示不需等待。</summary>
        public float GatherDurationSeconds { get; }

        /// <summary>可獲得的物品清單。</summary>
        public IReadOnlyList<CollectibleItemEntry> Items { get; }

        /// <param name="x">位置 X。</param>
        /// <param name="y">位置 Y。</param>
        /// <param name="gatherDurationSeconds">採集時間（秒），必須 >= 0。</param>
        /// <param name="items">物品清單，不可為 null 或空。</param>
        public CollectiblePointData(int x, int y, float gatherDurationSeconds, IReadOnlyList<CollectibleItemEntry> items)
        {
            if (gatherDurationSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(gatherDurationSeconds),
                    "gatherDurationSeconds must be >= 0.");
            if (items == null || items.Count == 0)
                throw new ArgumentException("items must not be null or empty.", nameof(items));

            X = x;
            Y = y;
            GatherDurationSeconds = gatherDurationSeconds;

            // Defensive copy
            CollectibleItemEntry[] copy = new CollectibleItemEntry[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                copy[i] = items[i] ?? throw new ArgumentException(
                    $"items[{i}] must not be null.", nameof(items));
            }
            Items = Array.AsReadOnly(copy);
        }
    }
}
