// StorageExpansionConfigData — 倉庫擴建系統外部配置的 JSON DTO 與不可變配置物件。
// 配置檔路徑：Assets/Game/Resources/Config/storage-expansion-config.json
// 此配置不經由 Google Sheets 管理，因為 IT 階段擴建階段表為簡易固定值，
// 正式版本再視需求決定是否遷移至 Google Sheets。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village
{
    // ===== JSON DTO（供 JsonUtility.FromJson 使用） =====

    /// <summary>單一擴建階段的配置項（JSON DTO）。</summary>
    [Serializable]
    public class StorageExpansionStageData
    {
        /// <summary>擴建等級（從 1 起算）。</summary>
        public int level;

        /// <summary>擴建前的容量格數。</summary>
        public int capacity_before;

        /// <summary>擴建後的容量格數。</summary>
        public int capacity_after;

        /// <summary>
        /// 所需物資（格式：itemId:quantity，多筆以 | 分隔）。
        /// 範例："material_wood:10|material_cloth:5"。
        /// </summary>
        public string required_items;

        /// <summary>擴建等待時間（秒）。</summary>
        public int duration_seconds;

        /// <summary>階段描述（撰寫者備註）。</summary>
        public string description;
    }

    /// <summary>倉庫擴建配置的完整外部資料（JSON DTO）。</summary>
    [Serializable]
    public class StorageExpansionConfigData
    {
        /// <summary>資料結構版本。</summary>
        public int schema_version;

        /// <summary>最大擴建等級。</summary>
        public int max_expansion_level;

        /// <summary>初始容量格數。</summary>
        public int initial_capacity;

        /// <summary>所有擴建階段。</summary>
        public StorageExpansionStageData[] stages;
    }

    // ===== 不可變資料物件 =====

    /// <summary>單一擴建階段的不可變資訊。</summary>
    public class StorageExpansionStage
    {
        /// <summary>擴建等級。</summary>
        public int Level { get; }

        /// <summary>擴建前容量格數。</summary>
        public int CapacityBefore { get; }

        /// <summary>擴建後容量格數。</summary>
        public int CapacityAfter { get; }

        /// <summary>本次擴建增加的格數（= CapacityAfter - CapacityBefore）。</summary>
        public int CapacityDelta => CapacityAfter - CapacityBefore;

        /// <summary>所需物資（唯讀字典，key=itemId，value=quantity）。</summary>
        public IReadOnlyDictionary<string, int> RequiredItems { get; }

        /// <summary>擴建等待時間（秒）。</summary>
        public int DurationSeconds { get; }

        /// <summary>階段描述。</summary>
        public string Description { get; }

        public StorageExpansionStage(
            int level,
            int capacityBefore,
            int capacityAfter,
            IReadOnlyDictionary<string, int> requiredItems,
            int durationSeconds,
            string description)
        {
            Level = level;
            CapacityBefore = capacityBefore;
            CapacityAfter = capacityAfter;
            RequiredItems = requiredItems;
            DurationSeconds = durationSeconds;
            Description = description;
        }
    }

    // ===== 不可變配置物件 =====

    /// <summary>
    /// 倉庫擴建系統的不可變配置。
    /// 從 StorageExpansionConfigData（JSON DTO）建構，提供階段查詢 API。
    /// </summary>
    public class StorageExpansionConfig
    {
        private readonly Dictionary<int, StorageExpansionStage> _stagesByLevel;
        private readonly List<StorageExpansionStage> _orderedStages;

        /// <summary>最大擴建等級。</summary>
        public int MaxExpansionLevel { get; }

        /// <summary>初始容量格數。</summary>
        public int InitialCapacity { get; }

        /// <summary>所有擴建階段（依 level 升序）。</summary>
        public IReadOnlyList<StorageExpansionStage> Stages => _orderedStages;

        /// <summary>
        /// 從 JSON DTO 建構不可變配置。
        /// </summary>
        /// <param name="data">JSON 反序列化後的 DTO。</param>
        /// <exception cref="ArgumentNullException">data 為 null 時拋出。</exception>
        public StorageExpansionConfig(StorageExpansionConfigData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            MaxExpansionLevel = data.max_expansion_level;
            InitialCapacity = data.initial_capacity;

            _stagesByLevel = new Dictionary<int, StorageExpansionStage>();
            _orderedStages = new List<StorageExpansionStage>();

            StorageExpansionStageData[] stages = data.stages ?? Array.Empty<StorageExpansionStageData>();
            foreach (StorageExpansionStageData stage in stages)
            {
                if (stage == null) continue;

                Dictionary<string, int> requiredItems = ParseRequiredItems(stage.required_items);

                StorageExpansionStage info = new StorageExpansionStage(
                    stage.level,
                    stage.capacity_before,
                    stage.capacity_after,
                    requiredItems,
                    stage.duration_seconds,
                    stage.description ?? string.Empty);

                _stagesByLevel[stage.level] = info;
                _orderedStages.Add(info);
            }

            _orderedStages.Sort((a, b) => a.Level.CompareTo(b.Level));
        }

        /// <summary>
        /// 取得指定等級的擴建階段資料。若不存在則回傳 null。
        /// </summary>
        public StorageExpansionStage GetStage(int level)
        {
            _stagesByLevel.TryGetValue(level, out StorageExpansionStage stage);
            return stage;
        }

        /// <summary>
        /// 解析所需物資字串為字典。
        /// 格式：itemId:quantity，多筆以 | 分隔。
        /// 空字串回傳空字典。
        /// </summary>
        private static Dictionary<string, int> ParseRequiredItems(string raw)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            if (string.IsNullOrEmpty(raw))
            {
                return result;
            }

            string[] pairs = raw.Split('|');
            foreach (string pair in pairs)
            {
                if (string.IsNullOrEmpty(pair)) continue;

                int colonIndex = pair.IndexOf(':');
                if (colonIndex <= 0 || colonIndex == pair.Length - 1)
                {
                    continue;
                }

                string itemId = pair.Substring(0, colonIndex);
                string quantityText = pair.Substring(colonIndex + 1);
                if (!int.TryParse(quantityText, out int quantity) || quantity <= 0)
                {
                    continue;
                }

                result[itemId] = quantity;
            }

            return result;
        }
    }
}
