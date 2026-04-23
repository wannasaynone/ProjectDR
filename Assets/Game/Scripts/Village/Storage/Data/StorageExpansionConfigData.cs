// StorageExpansionConfigData — 倉庫擴建系統外部配置的 IGameData DTO 與不可變配置物件。
// 對應 Sheets 分頁：StorageExpansionStages（主表）/ StorageExpansionRequirements（子表）
// 對應 .txt 檔：storageexpansionstages.txt / storageexpansionrequirements.txt
//
// Sprint 8 Wave 2.5 重構：
//   - StorageExpansionStageData：ID 改為 id 欄位（非 ID => level），移除 required_items 欄位
//   - 廢棄包裹類 StorageExpansionConfigData（含 initial_capacity/max_expansion_level 等外層欄位）
//   - initial_capacity：由 Sheets 中 level=0 entry 的 capacity_after 值取代（Q7 拍板）
//   - max_expansion_level：移除（runtime 從最大 level 推導）
//   - StorageExpansionConfig 建構子改為接受兩個純陣列 DTO
//   - ParseRequiredItems 改為從 StorageExpansionRequirementData[] 子表取得
// ADR-001 / ADR-002 A16

using System;
using System.Collections.Generic;
using KahaGameCore.GameData;

namespace ProjectDR.Village.Storage
{
    // ===== JSON DTO（供 JsonFx 反序列化純陣列使用） =====

    /// <summary>
    /// 單一擴建階段配置（JSON DTO，主表）。
    /// 實作 IGameData，int id 為流水號主鍵（= level，便於 GetGameData(level) 查詢）。
    /// level=0 entry 代表初始容量（capacity_after = initial_capacity，Q7 拍板）。
    /// 對應 Sheets 分頁 StorageExpansionStages，.txt 檔 storageexpansionstages.txt。
    /// </summary>
    [Serializable]
    public class StorageExpansionStageData : IGameData
    {
        /// <summary>IGameData 主鍵（= level，流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>擴建等級（0 = 初始，1~N = 擴建後）。</summary>
        public int level;

        /// <summary>擴建前容量格數（level=0 時為 0）。</summary>
        public int capacity_before;

        /// <summary>擴建後容量格數（level=0 時為 initial_capacity 值）。</summary>
        public int capacity_after;

        /// <summary>擴建等待時間秒數（level=0 時為 0）。</summary>
        public int duration_seconds;

        /// <summary>設計備忘（可為空）。</summary>
        public string description;
    }

    // ===== 不可變資料物件 =====

    /// <summary>單一擴建階段的不可變資訊。</summary>
    public class StorageExpansionStage
    {
        public int Level { get; }
        public int CapacityBefore { get; }
        public int CapacityAfter { get; }
        public int CapacityDelta => CapacityAfter - CapacityBefore;

        /// <summary>所需物資（唯讀字典，key=itemId，value=quantity）。</summary>
        public IReadOnlyDictionary<string, int> RequiredItems { get; }

        public int DurationSeconds { get; }
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
    /// 從兩個純陣列 DTO（主表 StorageExpansionStageData[] + 子表 StorageExpansionRequirementData[]）建構。
    /// level=0 entry 的 capacity_after 即為初始容量（Q7 拍板取代舊 initial_capacity 欄位）。
    /// </summary>
    public class StorageExpansionConfig
    {
        private readonly Dictionary<int, StorageExpansionStage> _stagesByLevel;
        private readonly List<StorageExpansionStage> _orderedStages;

        /// <summary>最大擴建等級（從 stages 最大 level 推導；Q7 拍板移除 max_expansion_level 欄位）。</summary>
        public int MaxExpansionLevel { get; }

        /// <summary>初始容量格數（= level=0 entry 的 capacity_after；Q7 拍板取代舊 initial_capacity 欄位）。</summary>
        public int InitialCapacity { get; }

        /// <summary>所有擴建階段（依 level 升序）。</summary>
        public IReadOnlyList<StorageExpansionStage> Stages => _orderedStages;

        /// <summary>
        /// 從純陣列 DTO 建構不可變配置。
        /// </summary>
        /// <param name="stageEntries">主表 JsonFx 反序列化後的陣列（不可為 null）。</param>
        /// <param name="requirementEntries">子表 JsonFx 反序列化後的陣列（不可為 null）。</param>
        public StorageExpansionConfig(
            StorageExpansionStageData[] stageEntries,
            StorageExpansionRequirementData[] requirementEntries)
        {
            if (stageEntries == null) throw new ArgumentNullException(nameof(stageEntries));
            if (requirementEntries == null) throw new ArgumentNullException(nameof(requirementEntries));

            _stagesByLevel = new Dictionary<int, StorageExpansionStage>();
            _orderedStages = new List<StorageExpansionStage>();

            // 分組需求子表（依 stage_level）
            Dictionary<int, Dictionary<string, int>> requirementsByLevel =
                new Dictionary<int, Dictionary<string, int>>();
            foreach (StorageExpansionRequirementData req in requirementEntries)
            {
                if (req == null || string.IsNullOrEmpty(req.item_id) || req.quantity <= 0) continue;
                if (!requirementsByLevel.TryGetValue(req.stage_level, out Dictionary<string, int> itemMap))
                {
                    itemMap = new Dictionary<string, int>();
                    requirementsByLevel[req.stage_level] = itemMap;
                }
                itemMap[req.item_id] = req.quantity;
            }

            int maxLevel = 0;
            int initialCapacity = StorageManager.DefaultInitialCapacity;

            foreach (StorageExpansionStageData stage in stageEntries)
            {
                if (stage == null) continue;

                // level=0 entry → 取 capacity_after 作為 InitialCapacity
                if (stage.level == 0)
                {
                    initialCapacity = stage.capacity_after;
                    // level=0 不加入可選擴建清單
                    continue;
                }

                Dictionary<string, int> reqMap = requirementsByLevel.TryGetValue(stage.level, out Dictionary<string, int> found)
                    ? found
                    : new Dictionary<string, int>();

                StorageExpansionStage info = new StorageExpansionStage(
                    stage.level,
                    stage.capacity_before,
                    stage.capacity_after,
                    reqMap,
                    stage.duration_seconds,
                    stage.description ?? string.Empty);

                _stagesByLevel[stage.level] = info;
                _orderedStages.Add(info);

                if (stage.level > maxLevel) maxLevel = stage.level;
            }

            _orderedStages.Sort((a, b) => a.Level.CompareTo(b.Level));
            MaxExpansionLevel = maxLevel;
            InitialCapacity = initialCapacity;
        }

        /// <summary>取得指定等級的擴建階段資料。若不存在則回傳 null。</summary>
        public StorageExpansionStage GetStage(int level)
        {
            _stagesByLevel.TryGetValue(level, out StorageExpansionStage stage);
            return stage;
        }
    }
}
