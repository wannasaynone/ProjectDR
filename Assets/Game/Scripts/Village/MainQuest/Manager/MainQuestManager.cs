// MainQuestManager — 前期主線任務 T0~T4 的狀態管理器。
// 依據 GDD `main-quest-system.md` v1.1：
// - 機制引導定位（告訴玩家可以做什麼、下一步在哪）
// - 任務狀態機：Locked → Available → InProgress → Completed
// - 完成條件類型見 MainQuestCompletionTypes
// - 事件發布：MainQuestAvailableEvent / MainQuestStartedEvent / MainQuestCompletedEvent
//
// 注意：完成條件的觸發由外部系統呼叫 NotifyCompletionSignal 推進。
// 本管理器不主動偵測完成（解耦化），由各系統在對應時刻通知。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Village.MainQuest
{
    /// <summary>主線任務狀態。</summary>
    public enum MainQuestState
    {
        /// <summary>尚未解鎖（前置任務未完成）。</summary>
        Locked,

        /// <summary>可承接（已解鎖但尚未進行）。</summary>
        Available,

        /// <summary>進行中（已接受、尚未完成）。</summary>
        InProgress,

        /// <summary>已完成。</summary>
        Completed
    }

    /// <summary>
    /// 前期主線任務管理器。
    /// 管理 T0~T4 的狀態機、完成觸發、解鎖後續任務、獎勵 grant_id 派發給上層處理。
    /// 純邏輯，不依賴 MonoBehaviour。
    /// </summary>
    public class MainQuestManager
    {
        private readonly MainQuestConfig _config;
        private readonly Dictionary<string, MainQuestState> _states;

        /// <summary>
        /// 建構主線任務管理器。
        /// 所有任務初始狀態為 Locked，僅 sort_order 最小者自動轉為 Available。
        /// </summary>
        /// <param name="config">主線任務配置（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">config 為 null 時拋出。</exception>
        public MainQuestManager(MainQuestConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _states = new Dictionary<string, MainQuestState>();

            foreach (MainQuestInfo quest in _config.OrderedQuests)
            {
                _states[quest.QuestId] = MainQuestState.Locked;
            }

            // 第一個任務（sort_order 最小）自動成為可承接
            if (_config.OrderedQuests.Count > 0)
            {
                MainQuestInfo firstQuest = _config.OrderedQuests[0];
                _states[firstQuest.QuestId] = MainQuestState.Available;
                EventBus.Publish(new MainQuestAvailableEvent { QuestId = firstQuest.QuestId });
            }
        }

        /// <summary>取得指定任務的狀態。未知任務回傳 Locked。</summary>
        public MainQuestState GetState(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return MainQuestState.Locked;
            return _states.TryGetValue(questId, out MainQuestState state) ? state : MainQuestState.Locked;
        }

        /// <summary>查詢指定任務是否已完成。</summary>
        public bool IsQuestCompleted(string questId)
        {
            return GetState(questId) == MainQuestState.Completed;
        }

        /// <summary>取得目前進行中的任務 ID 清單（狀態為 InProgress 的任務）。</summary>
        public IReadOnlyList<string> GetActiveQuests()
        {
            return GetQuestsInState(MainQuestState.InProgress);
        }

        /// <summary>取得目前唯一進行中的任務。若無則回傳 null。僅回傳第一個（前期為線性序列，通常唯一）。</summary>
        public MainQuestInfo GetActiveQuest()
        {
            foreach (MainQuestInfo quest in _config.OrderedQuests)
            {
                if (_states[quest.QuestId] == MainQuestState.InProgress)
                {
                    return quest;
                }
            }
            return null;
        }

        /// <summary>取得指定狀態的所有任務 ID 清單（依 sort_order 排序）。</summary>
        public IReadOnlyList<string> GetQuestsInState(MainQuestState state)
        {
            List<string> result = new List<string>();
            foreach (MainQuestInfo quest in _config.OrderedQuests)
            {
                if (_states[quest.QuestId] == state)
                {
                    result.Add(quest.QuestId);
                }
            }
            return result.AsReadOnly();
        }

        /// <summary>
        /// 嘗試開始（承接）指定任務。
        /// 僅當狀態為 Available 時可承接；成功時發布 MainQuestStartedEvent 並回傳 true。
        /// </summary>
        public bool StartQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return false;
            if (!_states.TryGetValue(questId, out MainQuestState state)) return false;
            if (state != MainQuestState.Available) return false;

            _states[questId] = MainQuestState.InProgress;
            EventBus.Publish(new MainQuestStartedEvent { QuestId = questId });
            return true;
        }

        /// <summary>
        /// 嘗試完成指定任務。
        /// 僅當狀態為 InProgress 時可完成。
        /// 成功時發布 MainQuestCompletedEvent 並解鎖 unlock_on_complete 中的後續任務（若屬任務 ID）。
        /// </summary>
        public bool CompleteQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return false;
            if (!_states.TryGetValue(questId, out MainQuestState state)) return false;
            if (state != MainQuestState.InProgress) return false;

            _states[questId] = MainQuestState.Completed;

            // 發布完成事件
            EventBus.Publish(new MainQuestCompletedEvent { QuestId = questId });

            // 解鎖 unlock_on_complete 中的任務
            MainQuestInfo info = _config.GetQuest(questId);
            if (info != null)
            {
                foreach (string unlockId in info.UnlockOnComplete)
                {
                    if (_states.TryGetValue(unlockId, out MainQuestState targetState)
                        && targetState == MainQuestState.Locked)
                    {
                        _states[unlockId] = MainQuestState.Available;
                        EventBus.Publish(new MainQuestAvailableEvent { QuestId = unlockId });
                    }
                    // unlockId 也可能是非任務 ID（例如節點 ID / 系統 ID），由上層訂閱 MainQuestCompletedEvent 自行判讀。
                }
            }

            return true;
        }

        /// <summary>
        /// 自動承接並完成當前最早 Available 的 auto 類型任務。
        /// 用於 T0（開局自動完成）等特殊類型。
        /// 若找不到對應任務或類型不符，回傳 false。
        /// </summary>
        public bool TryAutoCompleteFirstAutoQuest()
        {
            foreach (MainQuestInfo quest in _config.OrderedQuests)
            {
                if (_states[quest.QuestId] != MainQuestState.Available) continue;
                if (quest.CompletionConditionType != MainQuestCompletionTypes.Auto) continue;

                StartQuest(quest.QuestId);
                CompleteQuest(quest.QuestId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 通知完成訊號：由外部系統在對應事件發生時呼叫，
        /// 讓管理器檢查是否有 InProgress 或 Available 狀態的任務可因此完成。
        ///
        /// 規則：
        /// - 比對任務的 completion_condition_type 與 signalType
        /// - 若 signalValue 非 null/空，額外要求 completion_condition_value 匹配（字串相等）
        /// - 若任務當前為 Available，則自動先 StartQuest 再 CompleteQuest
        /// - 回傳因此訊號被完成的任務 ID 清單
        /// </summary>
        public IReadOnlyList<string> NotifyCompletionSignal(string signalType, string signalValue)
        {
            List<string> completed = new List<string>();
            if (string.IsNullOrEmpty(signalType)) return completed.AsReadOnly();

            foreach (MainQuestInfo quest in _config.OrderedQuests)
            {
                MainQuestState state = _states[quest.QuestId];
                if (state != MainQuestState.InProgress && state != MainQuestState.Available)
                {
                    continue;
                }

                if (quest.CompletionConditionType != signalType)
                {
                    continue;
                }

                // 若 signalValue 非空，要求完全相等
                if (!string.IsNullOrEmpty(signalValue)
                    && !string.IsNullOrEmpty(quest.CompletionConditionValue)
                    && quest.CompletionConditionValue != signalValue)
                {
                    continue;
                }

                if (state == MainQuestState.Available)
                {
                    StartQuest(quest.QuestId);
                }
                if (CompleteQuest(quest.QuestId))
                {
                    completed.Add(quest.QuestId);
                }
            }

            return completed.AsReadOnly();
        }
    }
}
