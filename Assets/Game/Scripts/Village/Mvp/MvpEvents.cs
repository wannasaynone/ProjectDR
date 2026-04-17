// MvpEvents — MVP 遊戲循環的事件定義。
// 所有 MVP 子系統跨模組通訊皆透過這些事件（KahaGameCore.GameEvent.EventBus）。

using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Mvp
{
    // ----- ResourceManager 相關事件 -----

    /// <summary>資源數值變更時發布。</summary>
    public class MvpResourceChangedEvent : GameEventBase
    {
        /// <summary>資源 ID（例：Wood）。</summary>
        public string ResourceId;

        /// <summary>變更後的最新數量。</summary>
        public int NewAmount;

        /// <summary>此次變更的差值（可正可負）。</summary>
        public int Delta;
    }

    // ----- SearchSystem 相關事件 -----

    /// <summary>搜索成功發布的事件（含隨機文字回饋）。</summary>
    public class MvpSearchCompletedEvent : GameEventBase
    {
        /// <summary>隨機文字回饋。</summary>
        public string FeedbackLine;

        /// <summary>獲得的木材數量。</summary>
        public int WoodGained;
    }

    // ----- FireSystem 相關事件 -----

    /// <summary>火堆狀態變更事件（點燃 / 熄滅）。</summary>
    public class MvpFireStateChangedEvent : GameEventBase
    {
        /// <summary>是否點燃。</summary>
        public bool IsLit;

        /// <summary>剩餘秒數。</summary>
        public float RemainingSeconds;
    }

    /// <summary>火堆剩餘秒數變更事件（Tick 時每次發送）。</summary>
    public class MvpFireRemainingChangedEvent : GameEventBase
    {
        public float RemainingSeconds;
    }

    /// <summary>火堆延長成功事件。</summary>
    public class MvpFireExtendedEvent : GameEventBase
    {
        public float NewRemainingSeconds;
    }

    // ----- ColdStatusSystem 相關事件 -----

    /// <summary>寒冷狀態變更事件。</summary>
    public class MvpColdStateChangedEvent : GameEventBase
    {
        public bool IsCold;
    }

    // ----- HutBuildSystem 相關事件 -----

    /// <summary>小屋建造開始事件。</summary>
    public class MvpHutBuildStartedEvent : GameEventBase
    {
        public float TotalSeconds;
    }

    /// <summary>小屋建造進度事件。</summary>
    public class MvpHutBuildProgressEvent : GameEventBase
    {
        public float ElapsedSeconds;
        public float TotalSeconds;
    }

    /// <summary>小屋建造完成事件。</summary>
    public class MvpHutBuiltEvent : GameEventBase
    {
        public int PopulationCapIncrement;
    }

    // ----- PopulationManager 相關事件 -----

    /// <summary>人口上限增加事件。</summary>
    public class MvpPopulationCapIncreasedEvent : GameEventBase
    {
        public int Increment;
        public int NewCap;
    }

    /// <summary>當前人口變更事件。</summary>
    public class MvpPopulationChangedEvent : GameEventBase
    {
        public int NewCount;
        public int CurrentCap;
    }

    // ----- NpcArrivalManager 相關事件 -----

    /// <summary>NPC 來訪事件。</summary>
    public class MvpNpcArrivedEvent : GameEventBase
    {
        public string CharacterId;
        public string DisplayName;
    }

    // ----- Dialogue Cooldown / Initiative 相關事件 -----

    /// <summary>玩家對話冷卻剩餘變更事件。</summary>
    public class MvpPlayerDialogueCooldownChangedEvent : GameEventBase
    {
        public float RemainingSeconds;
        public float TotalSeconds;
    }

    /// <summary>角色主動發話準備就緒事件（紅點亮起）。</summary>
    public class MvpNpcInitiativeReadyEvent : GameEventBase
    {
        public string CharacterId;
    }

    /// <summary>角色主動發話被消費事件（紅點熄滅）。</summary>
    public class MvpNpcInitiativeConsumedEvent : GameEventBase
    {
        public string CharacterId;
    }

    // ----- MvpDialogueSession 相關事件 -----

    /// <summary>MVP 對話 session 開始事件。</summary>
    public class MvpDialogueSessionStartedEvent : GameEventBase
    {
        public string CharacterId;
        public MvpDialogueDirection Direction;
    }

    /// <summary>MVP 對話 session 結束事件（已觸發好感度 +N）。</summary>
    public class MvpDialogueSessionCompletedEvent : GameEventBase
    {
        public string CharacterId;
        public MvpDialogueDirection Direction;
        public int AffinityGained;
    }

    /// <summary>對話內容方向（表達對話內容方向性，不影響好感度增量）。</summary>
    public enum MvpDialogueDirection
    {
        /// <summary>角色主動 = 角色提問玩家。</summary>
        CharacterInitiative,

        /// <summary>玩家主動 = 玩家提問角色。</summary>
        PlayerInitiative
    }
}
