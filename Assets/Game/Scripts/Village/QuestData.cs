// QuestData — 任務資料結構。
// IT 階段例外：任務資料以硬編碼方式存於 QuestManager，
// 正式版本應從 GameStaticDataManager 取得並實作 IGameData。

using System.Collections.Generic;

namespace ProjectDR.Village
{
    /// <summary>
    /// 任務資料。包含任務識別與完成條件。
    /// </summary>
    public class QuestData
    {
        /// <summary>任務唯一識別碼。</summary>
        public string QuestId { get; private set; }

        /// <summary>完成此任務所需的物品需求，key 為 itemId，value 為數量。</summary>
        public IReadOnlyDictionary<string, int> RequiredItems { get; private set; }

        public QuestData(string questId, Dictionary<string, int> requiredItems)
        {
            QuestId = questId;
            RequiredItems = requiredItems;
        }
    }
}
