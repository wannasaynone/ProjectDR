using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    // ----- VillageProgressionManager 相關事件 -----

    /// <summary>區域解鎖時發布的事件。</summary>
    public class AreaUnlockedEvent : GameEventBase
    {
        /// <summary>被解鎖的區域 ID。</summary>
        public string AreaId;
    }

    // ----- VillageNavigationManager 相關事件 -----

    /// <summary>成功導航至某區域時發布的事件。</summary>
    public class NavigatedToAreaEvent : GameEventBase
    {
        /// <summary>導航目標區域 ID。</summary>
        public string AreaId;
    }

    /// <summary>返回主畫面 Hub 時發布的事件。</summary>
    public class ReturnedToHubEvent : GameEventBase { }

    // ----- StorageManager 相關事件 -----

    /// <summary>庫存發生變化（新增或移除物品）時發布的事件。</summary>
    public class StorageChangedEvent : GameEventBase
    {
        /// <summary>發生變化的物品 ID。</summary>
        public string ItemId;

        /// <summary>變化後的最新數量。</summary>
        public int NewQuantity;
    }

    // ----- QuestManager 相關事件 -----

    /// <summary>成功接受任務時發布的事件。</summary>
    public class QuestAcceptedEvent : GameEventBase
    {
        /// <summary>被接受的任務 ID。</summary>
        public string QuestId;
    }

    /// <summary>成功完成任務時發布的事件。</summary>
    public class QuestCompletedEvent : GameEventBase
    {
        /// <summary>完成的任務 ID。</summary>
        public string QuestId;
    }

    // ----- ExplorationEntryManager 相關事件 -----

    /// <summary>玩家出發探索時發布的事件。</summary>
    public class ExplorationDepartedEvent : GameEventBase { }

    /// <summary>探索返回時發布的事件。</summary>
    public class ExplorationReturnedEvent : GameEventBase
    {
        /// <summary>此次探索獲得的戰利品，key 為 itemId，value 為數量。</summary>
        public IReadOnlyDictionary<string, int> Loot;
    }
}
