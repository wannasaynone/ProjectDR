// AffinityConfigData — 好感度系統外部配置的 IGameData DTO 與不可變配置物件。
// 對應 Sheets 分頁：Affinity
// 對應 .txt 檔：affinity.txt
//
// Sprint 8 Wave 2.5 重構：
//   - AffinityCharacterConfigData 改名為 AffinityCharacterData，欄位改 snake_case（character_id）
//   - 廢棄包裹類 AffinityConfigData（純陣列格式，JsonFx 直接反序列化 AffinityCharacterData[]）
//   - defaultThresholds 拆為 character_id="__default__" sentinel entry（Q7 拍板）
//   - AffinityConfig 建構子改為接受 AffinityCharacterData[]
// ADR-001 / ADR-002 A01

using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectDR.Village.Affinity
{
    // ===== JSON DTO（供 JsonFx 反序列化純陣列使用） =====

    /// <summary>
    /// 單一角色的好感度門檻配置（JSON DTO）。
    /// 實作 IGameData，int id 為流水號主鍵，character_id 為語意字串外鍵。
    /// 特殊值 character_id="__default__" 為 fallback entry（承接 Q7 拍板）。
    /// 對應 Sheets 分頁 Affinity，.txt 檔 affinity.txt。
    /// </summary>
    [Serializable]
    public class AffinityCharacterData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。回傳 int id 流水號。</summary>
        public int ID => id;

        /// <summary>
        /// 角色 ID（語意字串外鍵）。對應 JSON 欄位 "character_id"。
        /// 特殊值："__default__" 表示 fallback entry（取代舊 defaultThresholds）。
        /// </summary>
        public string character_id;

        /// <summary>語意字串 Key。</summary>
        public string Key => character_id;

        /// <summary>
        /// 好感度升級門檻值（逗號分隔字串，C# 端 split 轉 int[]）。
        /// 例："5,8,12,18,25"。
        /// 對應 JSON 欄位 "thresholds"。
        /// </summary>
        public string thresholds;
    }

    // ===== 不可變配置物件 =====

    /// <summary>
    /// 好感度系統的不可變配置。
    /// 從 AffinityCharacterData[]（純陣列 JSON DTO）建構，提供門檻查詢 API。
    /// character_id="__default__" 的 entry 作為 fallback。
    /// </summary>
    public class AffinityConfig
    {
        private const string DefaultKey = "__default__";

        private readonly Dictionary<string, IReadOnlyList<int>> _characterThresholds;
        private readonly IReadOnlyList<int> _defaultThresholds;

        /// <summary>
        /// 從純陣列 DTO 建構不可變配置。
        /// </summary>
        /// <param name="entries">JsonFx 反序列化後的 AffinityCharacterData 陣列。</param>
        /// <exception cref="ArgumentNullException">entries 為 null 時拋出。</exception>
        public AffinityConfig(AffinityCharacterData[] entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            _characterThresholds = new Dictionary<string, IReadOnlyList<int>>();
            _defaultThresholds = Array.AsReadOnly(Array.Empty<int>());

            foreach (AffinityCharacterData entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.character_id))
                {
                    continue;
                }

                int[] parsed = ParseThresholds(entry.thresholds);

                if (entry.character_id == DefaultKey)
                {
                    _defaultThresholds = Array.AsReadOnly(parsed);
                }
                else
                {
                    _characterThresholds[entry.character_id] = Array.AsReadOnly(parsed);
                }
            }
        }

        /// <summary>
        /// 取得指定角色的好感度門檻清單。
        /// 若角色未明確配置，回傳 __default__ entry 的門檻（Q7 拍板：sentinel fallback）。
        /// </summary>
        /// <param name="characterId">角色 ID。</param>
        /// <returns>門檻值的唯讀清單。</returns>
        public IReadOnlyList<int> GetThresholds(string characterId)
        {
            if (!string.IsNullOrEmpty(characterId) &&
                _characterThresholds.TryGetValue(characterId, out IReadOnlyList<int> thresholds))
            {
                return thresholds;
            }
            return _defaultThresholds;
        }

        /// <summary>解析逗號分隔的門檻字串為 int 陣列。</summary>
        private static int[] ParseThresholds(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return Array.Empty<int>();
            }

            string[] parts = raw.Split(',');
            List<int> result = new List<int>(parts.Length);
            foreach (string part in parts)
            {
                if (int.TryParse(part.Trim(), out int value))
                {
                    result.Add(value);
                }
            }
            return result.ToArray();
        }
    }
}
