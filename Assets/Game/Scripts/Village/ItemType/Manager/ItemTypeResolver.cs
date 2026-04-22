// ItemTypeResolver — 物品分類解析器。
// 管理 itemId 與分類字串的對應關係，供 FarmManager 等模組查詢物品類型。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village.ItemType
{
    /// <summary>
    /// 物品分類解析器。
    /// 維護 itemId 到分類字串的對應，支援註冊、查詢與過濾功能。
    /// </summary>
    public class ItemTypeResolver
    {
        private readonly Dictionary<string, string> _itemTypeMap = new Dictionary<string, string>();

        /// <summary>
        /// 註冊物品分類。
        /// 若 itemId 已存在，覆寫原有分類。
        /// </summary>
        /// <param name="itemId">物品 ID，不可為 null 或空字串。</param>
        /// <param name="type">分類字串，不可為 null 或空字串。</param>
        /// <exception cref="ArgumentException">itemId 或 type 為 null/empty 時拋出。</exception>
        public void Register(string itemId, string type)
        {
            if (string.IsNullOrEmpty(itemId))
                throw new ArgumentException("itemId 不可為 null 或空字串。", nameof(itemId));
            if (string.IsNullOrEmpty(type))
                throw new ArgumentException("type 不可為 null 或空字串。", nameof(type));

            _itemTypeMap[itemId] = type;
        }

        /// <summary>
        /// 查詢物品分類。
        /// </summary>
        /// <param name="itemId">物品 ID，不可為 null 或空字串。</param>
        /// <returns>分類字串；若未註冊回傳 null。</returns>
        /// <exception cref="ArgumentException">itemId 為 null/empty 時拋出。</exception>
        public string GetItemType(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                throw new ArgumentException("itemId 不可為 null 或空字串。", nameof(itemId));

            return _itemTypeMap.TryGetValue(itemId, out string type) ? type : null;
        }

        /// <summary>
        /// 判斷物品是否屬於指定分類。
        /// 未註冊的物品回傳 false。
        /// </summary>
        /// <param name="itemId">物品 ID。</param>
        /// <param name="expectedType">期望的分類字串。</param>
        /// <returns>是否符合指定分類。</returns>
        public bool IsType(string itemId, string expectedType)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;

            return _itemTypeMap.TryGetValue(itemId, out string type) && type == expectedType;
        }

        /// <summary>
        /// 取得屬於指定分類的所有物品 ID。
        /// 無結果時回傳空集合。
        /// </summary>
        /// <param name="type">要查詢的分類字串。</param>
        /// <returns>符合分類的物品 ID 集合（唯讀）。</returns>
        public IReadOnlyList<string> GetItemsByType(string type)
        {
            var result = new List<string>();
            foreach (var kvp in _itemTypeMap)
            {
                if (kvp.Value == type)
                    result.Add(kvp.Key);
            }
            return result;
        }
    }
}
