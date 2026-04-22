// QuestManager — 任務管理器。
// IT 階段例外：任務資料庫以硬編碼方式定義（不使用 Google Sheets / GameStaticDataManager），
// 原因：IT 階段僅需驗證核心玩法流程，不需要完整資料管線。
// 正式版本應從 GameStaticDataManager.GetAllGameData<QuestData>() 取得。

using ProjectDR.Village.Storage;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Village.MainQuest
{
    /// <summary>
    /// 任務管理器。
    /// 管理任務的接受、完成與狀態查詢。
    /// 依賴 StorageManager 驗證完成條件並扣除所需物品。
    /// </summary>
    public class QuestManager
    {
        private readonly StorageManager _storageManager;

        // IT 階段靜態任務資料庫（例外允許：IT 階段不接入 Google Sheets 資料管線）
        private readonly Dictionary<string, QuestData> _questDatabase;

        private QuestData _activeQuest = null;
        private readonly HashSet<string> _completedQuestIds = new HashSet<string>();

        public QuestManager(StorageManager storageManager)
        {
            _storageManager = storageManager;

            // 建立 IT 階段任務資料庫
            // 正式版本應從 GameStaticDataManager.GetAllGameData<QuestData>() 取得
            _questDatabase = new Dictionary<string, QuestData>
            {
                {
                    "Quest_GatherWood",
                    new QuestData("Quest_GatherWood", new Dictionary<string, int> { { "Wood", 10 } })
                },
                {
                    "Quest_HuntAnimal",
                    new QuestData("Quest_HuntAnimal", new Dictionary<string, int> { { "Meat", 5 } })
                }
            };
        }

        /// <summary>取得目前進行中的任務。若無，回傳 null。</summary>
        public QuestData GetActiveQuest()
        {
            return _activeQuest;
        }

        /// <summary>
        /// 接受指定任務。
        /// 若已有進行中任務，或任務 ID 不存在，回傳 false。
        /// 成功接受後發布 QuestAcceptedEvent。
        /// </summary>
        public bool AcceptQuest(string questId)
        {
            if (_activeQuest != null)
            {
                return false;
            }

            if (!_questDatabase.ContainsKey(questId))
            {
                return false;
            }

            _activeQuest = _questDatabase[questId];
            EventBus.Publish(new QuestAcceptedEvent { QuestId = questId });
            return true;
        }

        /// <summary>
        /// 嘗試完成進行中任務。
        /// 若無進行中任務，或完成條件未滿足，回傳 false。
        /// 成功完成時，扣除所需物品、清除進行中任務，並發布 QuestCompletedEvent。
        /// </summary>
        public bool TryCompleteActiveQuest()
        {
            if (_activeQuest == null)
            {
                return false;
            }

            // 驗證所有物品需求
            foreach (KeyValuePair<string, int> requirement in _activeQuest.RequiredItems)
            {
                if (!_storageManager.HasItem(requirement.Key, requirement.Value))
                {
                    return false;
                }
            }

            // 扣除物品
            foreach (KeyValuePair<string, int> requirement in _activeQuest.RequiredItems)
            {
                _storageManager.RemoveItem(requirement.Key, requirement.Value);
            }

            string completedQuestId = _activeQuest.QuestId;
            _completedQuestIds.Add(completedQuestId);
            _activeQuest = null;

            EventBus.Publish(new QuestCompletedEvent { QuestId = completedQuestId });
            return true;
        }

        /// <summary>查詢指定任務是否已完成。</summary>
        public bool IsQuestCompleted(string questId)
        {
            return _completedQuestIds.Contains(questId);
        }
    }
}
