// MainQuestConfigData — 主線任務配置的 IGameData DTO 與不可變配置物件。
// 對應 Sheets 分頁：MainQuests（主表）/ MainQuestUnlocks（子表）
// 對應 .txt 檔：mainquests.txt / mainquestunlocks.txt
//
// Sprint 8 Wave 2.5 重構：
//   - MainQuestConfigEntry 改名為 MainQuestData（去 ConfigEntry）
//   - 移除 unlock_on_complete 欄位（Q3 拍板：拆子表 MainQuestUnlockData）
//   - 廢棄包裹類 MainQuestConfigData（純陣列格式）
//   - MainQuestConfig 建構子改為接受 MainQuestData[] + MainQuestUnlockData[]
//   - MainQuestInfo.UnlockOnComplete 改為 IReadOnlyList<MainQuestUnlockData>（型別化）
// ADR-001 / ADR-002 A12

using System;
using System.Collections.Generic;

namespace ProjectDR.Village.MainQuest
{
    // ===== 完成條件類型常數 =====

    /// <summary>主線任務完成條件類型常數（對應 JSON completion_condition_type 欄位）。</summary>
    public static class MainQuestCompletionTypes
    {
        /// <summary>自動完成（開局瞬間 / 前置系統觸發完成）。</summary>
        public const string Auto = "auto";

        /// <summary>特定對話序列結束。</summary>
        public const string DialogueEnd = "dialogue_end";

        /// <summary>累積指定角色的委託完成次數。</summary>
        public const string CommissionCount = "commission_count";

        /// <summary>首次探索相關完成。</summary>
        public const string FirstExplore = "first_explore";

        /// <summary>首次倉庫擴建完成。</summary>
        public const string FirstStorageExpand = "first_storage_expand";

        /// <summary>首次角色引導流程完成（角色解鎖登場 CG 全播完）。</summary>
        [System.Obsolete("Sprint 6 後已廢棄，改用 MainQuestCompletionTypes.DialogueEnd + Node2DialogueComplete。此常數無任何引用，可安全刪除。")]
        public const string FirstCharIntroComplete = "first_char_intro_complete";
    }

    // ===== 完成條件附加值常數 =====

    /// <summary>
    /// 主線任務 completion_condition_value 常用字串常數集。
    /// 對應 MainQuests 分頁各任務的 completion_condition_value 欄位。
    /// </summary>
    public static class MainQuestSignalValues
    {
        /// <summary>（舊 T1 完成條件，Sprint 6 後已廢棄）首次角色 intro 完成。</summary>
        [System.Obsolete("Sprint 6 後已廢棄，改用 Node2DialogueComplete。")]
        public const string FirstCharIntroComplete = "first_char_intro_complete";

        /// <summary>T0 完成條件：節點 0 對話完成。</summary>
        public const string Node0DialogueComplete = "node0_dialogue_complete";

        /// <summary>新 T1 完成條件（Sprint 6）：節點 2 對話完成。</summary>
        public const string Node2DialogueComplete = "node_2_dialogue_complete";

        /// <summary>新 T2 完成條件（Sprint 6，舊 T4）：守衛歸來事件完成。</summary>
        public const string GuardReturnEventComplete = "guard_return_event_complete";
    }

    // ===== JSON DTO（供 JsonFx 反序列化純陣列使用） =====

    /// <summary>
    /// 單一主線任務（JSON DTO，主表）。
    /// 實作 IGameData，int id 為流水號主鍵，quest_id 為語意字串外鍵。
    /// 對應 Sheets 分頁 MainQuests，.txt 檔 mainquests.txt。
    /// 注意：unlock_on_complete 欄位已移除（Q3 拍板拆子表 MainQuestUnlockData）。
    /// </summary>
    [Serializable]
    public class MainQuestData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>任務語意識別符（T0 / T1 / T2）。</summary>
        public string quest_id;

        /// <summary>語意字串 Key。</summary>
        public string Key => quest_id;

        /// <summary>顯示名稱。</summary>
        public string display_name;

        /// <summary>任務描述。</summary>
        public string description;

        /// <summary>任務持有角色 ID（空 = 非角色派發）。</summary>
        public string owner_character_id;

        /// <summary>完成條件類型（見 MainQuestCompletionTypes）。</summary>
        public string completion_condition_type;

        /// <summary>完成條件附加值（自然語言，非 FK）。</summary>
        public string completion_condition_value;

        /// <summary>
        /// 完成獎勵 grant_id 清單（多個以 | 分隔；未來可考慮拆子表）。
        /// </summary>
        public string reward_grant_ids;

        /// <summary>排序（升序）。</summary>
        public int sort_order;
    }

    // ===== 不可變資料物件 =====

    /// <summary>單一主線任務的不可變資訊。</summary>
    public class MainQuestInfo
    {
        /// <summary>IGameData 流水號主鍵。</summary>
        public int ID { get; }

        /// <summary>語意字串外鍵（= quest_id）。</summary>
        public string Key { get; }

        /// <summary>任務 ID（同 Key，保留向後相容）。</summary>
        public string QuestId => Key;

        public string DisplayName { get; }
        public string Description { get; }
        public string OwnerCharacterId { get; }
        public string CompletionConditionType { get; }
        public string CompletionConditionValue { get; }

        /// <summary>完成獎勵 grant_id 清單（已解析）。</summary>
        public IReadOnlyList<string> RewardGrantIds { get; }

        /// <summary>完成後解鎖條目（Q3 拍板：型別化子表，取代舊 unlock_on_complete 字串）。</summary>
        public IReadOnlyList<MainQuestUnlockData> UnlockEntries { get; }

        public int SortOrder { get; }

        public MainQuestInfo(
            int id,
            string questId,
            string displayName,
            string description,
            string ownerCharacterId,
            string completionConditionType,
            string completionConditionValue,
            IReadOnlyList<string> rewardGrantIds,
            IReadOnlyList<MainQuestUnlockData> unlockEntries,
            int sortOrder)
        {
            ID = id;
            Key = questId;
            DisplayName = displayName;
            Description = description;
            OwnerCharacterId = ownerCharacterId;
            CompletionConditionType = completionConditionType;
            CompletionConditionValue = completionConditionValue;
            RewardGrantIds = rewardGrantIds;
            UnlockEntries = unlockEntries;
            SortOrder = sortOrder;
        }

        // 向後相容：舊 UnlockOnComplete 字串列表（由 UnlockEntries 轉換）
        public IReadOnlyList<string> UnlockOnComplete
        {
            get
            {
                List<string> values = new List<string>();
                foreach (MainQuestUnlockData entry in UnlockEntries)
                {
                    if (!string.IsNullOrEmpty(entry.unlock_value))
                        values.Add(entry.unlock_value);
                }
                return values.AsReadOnly();
            }
        }
    }

    // ===== 不可變配置物件 =====

    /// <summary>
    /// 主線任務配置（不可變）。
    /// 從兩個純陣列 DTO（主表 MainQuestData[] + 子表 MainQuestUnlockData[]）建構。
    /// </summary>
    public class MainQuestConfig
    {
        private readonly Dictionary<string, MainQuestInfo> _questsById;
        private readonly List<MainQuestInfo> _questsBySortOrder;

        /// <summary>依 sort_order 升序排列的任務清單。</summary>
        public IReadOnlyList<MainQuestInfo> OrderedQuests => _questsBySortOrder;

        /// <summary>
        /// 從純陣列 DTO 建構不可變配置。
        /// </summary>
        /// <param name="questEntries">主表 JsonFx 反序列化後的陣列（不可為 null）。</param>
        /// <param name="unlockEntries">子表 JsonFx 反序列化後的陣列（不可為 null）。</param>
        public MainQuestConfig(MainQuestData[] questEntries, MainQuestUnlockData[] unlockEntries)
        {
            if (questEntries == null) throw new ArgumentNullException(nameof(questEntries));
            if (unlockEntries == null) throw new ArgumentNullException(nameof(unlockEntries));

            _questsById = new Dictionary<string, MainQuestInfo>();
            _questsBySortOrder = new List<MainQuestInfo>();

            // 分組解鎖條目
            Dictionary<string, List<MainQuestUnlockData>> unlocksByQuestId =
                new Dictionary<string, List<MainQuestUnlockData>>();
            foreach (MainQuestUnlockData unlock in unlockEntries)
            {
                if (unlock == null || string.IsNullOrEmpty(unlock.main_quest_id)) continue;
                if (!unlocksByQuestId.TryGetValue(unlock.main_quest_id, out List<MainQuestUnlockData> bucket))
                {
                    bucket = new List<MainQuestUnlockData>();
                    unlocksByQuestId[unlock.main_quest_id] = bucket;
                }
                bucket.Add(unlock);
            }

            foreach (MainQuestData entry in questEntries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.quest_id)) continue;

                IReadOnlyList<string> rewardGrantIds = SplitPipeList(entry.reward_grant_ids);

                IReadOnlyList<MainQuestUnlockData> unlockList;
                if (unlocksByQuestId.TryGetValue(entry.quest_id, out List<MainQuestUnlockData> ul))
                {
                    ul.Sort((a, b) => a.sort_order.CompareTo(b.sort_order));
                    unlockList = ul.AsReadOnly();
                }
                else
                {
                    unlockList = Array.AsReadOnly(Array.Empty<MainQuestUnlockData>());
                }

                MainQuestInfo info = new MainQuestInfo(
                    entry.id,
                    entry.quest_id,
                    entry.display_name ?? string.Empty,
                    entry.description ?? string.Empty,
                    entry.owner_character_id ?? string.Empty,
                    entry.completion_condition_type ?? string.Empty,
                    entry.completion_condition_value ?? string.Empty,
                    rewardGrantIds,
                    unlockList,
                    entry.sort_order);

                _questsById[entry.quest_id] = info;
                _questsBySortOrder.Add(info);
            }

            _questsBySortOrder.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
        }

        /// <summary>依 quest_id 取得任務資訊。找不到時回傳 null。</summary>
        public MainQuestInfo GetQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            _questsById.TryGetValue(questId, out MainQuestInfo info);
            return info;
        }

        private static IReadOnlyList<string> SplitPipeList(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return Array.AsReadOnly(Array.Empty<string>());
            }

            string[] parts = raw.Split('|');
            List<string> cleaned = new List<string>(parts.Length);
            foreach (string part in parts)
            {
                if (!string.IsNullOrEmpty(part))
                {
                    cleaned.Add(part);
                }
            }
            return cleaned.AsReadOnly();
        }
    }
}
