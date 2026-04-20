// MainQuestConfigData — 前期主線任務序列配置的 JSON DTO 與不可變配置物件。
// 配置檔路徑：Assets/Game/Resources/Config/main-quest-config.json
// 此配置不經由 Google Sheets 管理，因為前期主線任務序列為固定劇情骨架，
// 正式版本再視需求決定是否遷移至 Google Sheets。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village
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

    // ===== 完成條件附加值常數（由 VillageEntryPoint 等上層傳給 NotifyCompletionSignal） =====

    /// <summary>
    /// 主線任務 completion_condition_value 常用字串常數集。
    /// 對應 main-quest-config.json 中各任務的 completion_condition_value 欄位。
    /// </summary>
    public static class MainQuestSignalValues
    {
        /// <summary>（舊 T1 完成條件，Sprint 6 後已廢棄）首次角色 intro 完成（= 節點 1 結束）。保留供相容性。</summary>
        [System.Obsolete("Sprint 6 後已廢棄，改用 Node2DialogueComplete。VillageEntryPoint 不再送此訊號，無任何引用，可安全刪除。")]
        public const string FirstCharIntroComplete = "first_char_intro_complete";

        /// <summary>T0 完成條件：節點 0 對話完成。</summary>
        public const string Node0DialogueComplete = "node0_dialogue_complete";

        /// <summary>新 T1 完成條件（Sprint 6）：節點 2 對話完成（= 魔女登場 CG + 對話結束）。</summary>
        public const string Node2DialogueComplete = "node_2_dialogue_complete";

        /// <summary>新 T2 完成條件（Sprint 6，舊 T4）：守衛歸來事件完成。</summary>
        public const string GuardReturnEventComplete = "guard_return_event_complete";
    }

    // ===== JSON DTO（供 JsonUtility.FromJson 使用） =====

    /// <summary>單一主線任務的配置項（JSON DTO）。</summary>
    [Serializable]
    public class MainQuestConfigEntry
    {
        /// <summary>任務 ID（例：T0、T1）。</summary>
        public string quest_id;

        /// <summary>任務顯示名稱。</summary>
        public string display_name;

        /// <summary>任務描述。</summary>
        public string description;

        /// <summary>任務擁有者角色 ID。</summary>
        public string owner_character_id;

        /// <summary>完成條件類型（見 MainQuestCompletionTypes）。</summary>
        public string completion_condition_type;

        /// <summary>完成條件附加值（依 type 不同格式不同）。</summary>
        public string completion_condition_value;

        /// <summary>
        /// 獎勵的 grant_id 清單（對應 initial-resources-config.json 的 grant_id）。
        /// 多值以 | 分隔，空字串表示無獎勵。
        /// </summary>
        public string reward_grant_ids;

        /// <summary>
        /// 任務完成時解鎖的項目 ID 清單（任務 ID、節點 ID、系統 ID）。
        /// 多值以 | 分隔，空字串表示無後續解鎖。
        /// </summary>
        public string unlock_on_complete;

        /// <summary>排序（升序，用於承接順序）。</summary>
        public int sort_order;
    }

    /// <summary>主線任務配置的完整外部資料（JSON DTO）。</summary>
    [Serializable]
    public class MainQuestConfigData
    {
        /// <summary>資料結構版本。</summary>
        public int schema_version;

        /// <summary>配置說明（撰寫者備註）。</summary>
        public string note;

        /// <summary>所有主線任務配置。</summary>
        public MainQuestConfigEntry[] main_quests;
    }

    // ===== 不可變資料物件 =====

    /// <summary>單一主線任務的不可變資訊。</summary>
    public class MainQuestInfo
    {
        /// <summary>任務 ID。</summary>
        public string QuestId { get; }

        /// <summary>顯示名稱。</summary>
        public string DisplayName { get; }

        /// <summary>描述。</summary>
        public string Description { get; }

        /// <summary>擁有者角色 ID。</summary>
        public string OwnerCharacterId { get; }

        /// <summary>完成條件類型。</summary>
        public string CompletionConditionType { get; }

        /// <summary>完成條件附加值。</summary>
        public string CompletionConditionValue { get; }

        /// <summary>獎勵 grant_id 清單（已解析）。</summary>
        public IReadOnlyList<string> RewardGrantIds { get; }

        /// <summary>完成後解鎖項目清單（已解析）。</summary>
        public IReadOnlyList<string> UnlockOnComplete { get; }

        /// <summary>排序。</summary>
        public int SortOrder { get; }

        public MainQuestInfo(
            string questId,
            string displayName,
            string description,
            string ownerCharacterId,
            string completionConditionType,
            string completionConditionValue,
            IReadOnlyList<string> rewardGrantIds,
            IReadOnlyList<string> unlockOnComplete,
            int sortOrder)
        {
            QuestId = questId;
            DisplayName = displayName;
            Description = description;
            OwnerCharacterId = ownerCharacterId;
            CompletionConditionType = completionConditionType;
            CompletionConditionValue = completionConditionValue;
            RewardGrantIds = rewardGrantIds;
            UnlockOnComplete = unlockOnComplete;
            SortOrder = sortOrder;
        }
    }

    // ===== 不可變配置物件 =====

    /// <summary>
    /// 主線任務配置（不可變）。
    /// 從 MainQuestConfigData（JSON DTO）建構，提供依 quest_id 查詢與依 sort_order 迭代 API。
    /// </summary>
    public class MainQuestConfig
    {
        private readonly Dictionary<string, MainQuestInfo> _questsById;
        private readonly List<MainQuestInfo> _questsBySortOrder;

        /// <summary>依 sort_order 升序排列的任務清單。</summary>
        public IReadOnlyList<MainQuestInfo> OrderedQuests => _questsBySortOrder;

        /// <summary>
        /// 從 JSON DTO 建構不可變配置。
        /// </summary>
        /// <param name="data">JSON 反序列化後的 DTO。</param>
        /// <exception cref="ArgumentNullException">data 為 null 時拋出。</exception>
        public MainQuestConfig(MainQuestConfigData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _questsById = new Dictionary<string, MainQuestInfo>();
            _questsBySortOrder = new List<MainQuestInfo>();

            MainQuestConfigEntry[] quests = data.main_quests ?? Array.Empty<MainQuestConfigEntry>();
            foreach (MainQuestConfigEntry entry in quests)
            {
                if (entry == null || string.IsNullOrEmpty(entry.quest_id))
                {
                    continue;
                }

                IReadOnlyList<string> rewardGrantIds = SplitPipeList(entry.reward_grant_ids);
                IReadOnlyList<string> unlockOnComplete = SplitPipeList(entry.unlock_on_complete);

                MainQuestInfo info = new MainQuestInfo(
                    entry.quest_id,
                    entry.display_name ?? string.Empty,
                    entry.description ?? string.Empty,
                    entry.owner_character_id ?? string.Empty,
                    entry.completion_condition_type ?? string.Empty,
                    entry.completion_condition_value ?? string.Empty,
                    rewardGrantIds,
                    unlockOnComplete,
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

        /// <summary>
        /// 將以 | 分隔的字串切割為唯讀清單。空字串回傳空清單。
        /// </summary>
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
