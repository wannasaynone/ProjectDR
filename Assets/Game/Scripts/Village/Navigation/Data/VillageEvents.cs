using ProjectDR.Village.Dialogue;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Navigation
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

    /// <summary>倉庫容量變動時發布的事件（擴建成功完成時）。</summary>
    public class StorageCapacityChangedEvent : GameEventBase
    {
        /// <summary>擴建前容量。</summary>
        public int PreviousCapacity;

        /// <summary>擴建後容量。</summary>
        public int NewCapacity;
    }

    /// <summary>倉庫擴建流程開始時發布的事件。</summary>
    public class StorageExpansionStartedEvent : GameEventBase
    {
        /// <summary>擴建等級（1 起算）。</summary>
        public int Level;

        /// <summary>預計完成的總秒數。</summary>
        public float DurationSeconds;

        /// <summary>擴建後的容量格數。</summary>
        public int CapacityAfter;
    }

    /// <summary>倉庫擴建流程完成時發布的事件（倒數結束、容量生效前）。</summary>
    public class StorageExpansionCompletedEvent : GameEventBase
    {
        /// <summary>擴建等級（1 起算）。</summary>
        public int Level;

        /// <summary>擴建後的容量格數。</summary>
        public int CapacityAfter;
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

    // ----- BackpackManager 相關事件 -----

    /// <summary>背包內容發生變化時發布的事件。</summary>
    public class BackpackChangedEvent : GameEventBase
    {
        /// <summary>發生變化的物品 ID。回溯快照時為 null。</summary>
        public string ItemId;

        /// <summary>該物品在背包中的最新總數量。回溯快照時為 0。</summary>
        public int TotalQuantity;
    }

    // ----- DialogueManager 相關事件 -----

    /// <summary>對話開始時發布的事件。</summary>
    public class DialogueStartedEvent : GameEventBase
    {
        /// <summary>對話的第一行文字。</summary>
        public string FirstLine;
    }

    /// <summary>對話全部結束時發布的事件。</summary>
    public class DialogueCompletedEvent : GameEventBase { }

    /// <summary>
    /// VN 式選項呈現時發布的事件。
    /// UI 層接收後顯示選項按鈕供玩家點擊。
    /// </summary>
    public class DialogueChoicePresentedEvent : GameEventBase
    {
        /// <summary>可選擇的選項清單。</summary>
        public System.Collections.Generic.IReadOnlyList<DialogueChoice> Choices;
    }

    /// <summary>
    /// 玩家選擇了選項時發布的事件。
    /// 由 DialogueManager 於收到選擇後自動發布。
    /// </summary>
    public class DialogueChoiceSelectedEvent : GameEventBase
    {
        /// <summary>被選擇的選項識別。</summary>
        public string ChoiceId;
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

    // ----- FarmManager 相關事件 -----

    /// <summary>農田格子種植成功時發布的事件。</summary>
    public class FarmPlotPlantedEvent : GameEventBase
    {
        /// <summary>種植的格子索引。</summary>
        public int PlotIndex;

        /// <summary>種植的種子 ID。</summary>
        public string SeedItemId;

        /// <summary>預計收穫的 UTC 時間戳記（秒）。</summary>
        public long ExpectedHarvestTimestampUtc;
    }

    /// <summary>農田格子收穫成功時發布的事件。</summary>
    public class FarmPlotHarvestedEvent : GameEventBase
    {
        /// <summary>收穫的格子索引。</summary>
        public int PlotIndex;

        /// <summary>收穫的作物 ID。</summary>
        public string HarvestedItemId;

        /// <summary>收穫的數量。</summary>
        public int Quantity;
    }

    // ----- CommissionManager 相關事件 (Sprint 4 B5) -----

    /// <summary>委託開始（玩家交付物品、倒數開始）時發布的事件。</summary>
    public class CommissionStartedEvent : GameEventBase
    {
        /// <summary>執行委託的角色 ID。</summary>
        public string CharacterId;

        /// <summary>該角色工作台中的 slot 索引（0-based）。</summary>
        public int SlotIndex;

        /// <summary>啟動的配方 ID。</summary>
        public string RecipeId;

        /// <summary>預計完成的 UTC 時間戳記（秒）。</summary>
        public long ExpectedCompletionTimestampUtc;
    }

    /// <summary>
    /// 委託倒數完成（產出已可領取）時發布的事件。
    /// 每個 slot 從「工作中」跨越到「可領取」邊界時發布一次，不重複發布。
    /// 紅點系統 L1 委託完成層應監聽此事件。
    /// </summary>
    public class CommissionCompletedEvent : GameEventBase
    {
        /// <summary>執行委託的角色 ID。</summary>
        public string CharacterId;

        /// <summary>該角色工作台中的 slot 索引。</summary>
        public int SlotIndex;

        /// <summary>完成的配方 ID。</summary>
        public string RecipeId;

        /// <summary>產出的物品 ID。</summary>
        public string OutputItemId;

        /// <summary>產出的物品數量。</summary>
        public int OutputQuantity;
    }

    /// <summary>
    /// 委託產出被領取（物品已進入背包/倉庫、slot 重新變空）時發布的事件。
    /// 主線任務 commission_count 完成訊號應由此事件觸發。
    /// </summary>
    public class CommissionClaimedEvent : GameEventBase
    {
        /// <summary>執行委託的角色 ID。</summary>
        public string CharacterId;

        /// <summary>該角色工作台中的 slot 索引。</summary>
        public int SlotIndex;

        /// <summary>完成的配方 ID。</summary>
        public string RecipeId;

        /// <summary>實際進入的物品 ID。</summary>
        public string OutputItemId;

        /// <summary>實際入庫的數量（背包+倉庫合計）。</summary>
        public int OutputQuantity;
    }

    /// <summary>
    /// 委託倒數 Tick 事件。
    /// 僅在剩餘秒數的整秒值發生變化（或從 InProgress 跨到完成）時發布，
    /// 避免每幀發布造成事件風暴。供 UI 倒數文字更新使用。
    /// </summary>
    public class CommissionTickEvent : GameEventBase
    {
        /// <summary>執行委託的角色 ID。</summary>
        public string CharacterId;

        /// <summary>該角色工作台中的 slot 索引。</summary>
        public int SlotIndex;

        /// <summary>剩餘的秒數（向上取整至整秒）；0 代表已完成。</summary>
        public int RemainingSeconds;
    }

    // ----- AffinityManager 相關事件 -----

    /// <summary>好感度數值變更時發布的事件。</summary>
    public class AffinityChangedEvent : GameEventBase
    {
        /// <summary>角色 ID。</summary>
        public string CharacterId;

        /// <summary>變更後的好感度數值。</summary>
        public int NewValue;

        /// <summary>此次變更的增加量。</summary>
        public int Amount;
    }

    /// <summary>好感度達到門檻值時發布的事件。UI 層監聯此事件以處理解鎖表現。</summary>
    public class AffinityThresholdReachedEvent : GameEventBase
    {
        /// <summary>角色 ID。</summary>
        public string CharacterId;

        /// <summary>達到的門檻值。</summary>
        public int ThresholdValue;
    }

    // ----- GiftManager 相關事件 -----

    /// <summary>送禮成功時發布的事件。</summary>
    public class GiftSuccessEvent : GameEventBase
    {
        /// <summary>送禮目標角色 ID。</summary>
        public string CharacterId;

        /// <summary>送出的物品 ID。</summary>
        public string ItemId;
    }

    // ----- CGUnlockManager 相關事件 -----

    /// <summary>CG 場景解鎖時發布的事件。</summary>
    public class CGUnlockedEvent : GameEventBase
    {
        /// <summary>解鎖的 CG 場景 ID。</summary>
        public string CgSceneId;

        /// <summary>所屬角色 ID。</summary>
        public string CharacterId;
    }

    // ----- CharacterUnlockManager 相關事件 -----

    /// <summary>角色 Hub 按鈕解鎖（變為可見）時發布的事件。</summary>
    public class CharacterUnlockedEvent : GameEventBase
    {
        /// <summary>被解鎖的角色 ID（CharacterIds 常數）。</summary>
        public string CharacterId;
    }

    /// <summary>探索功能解鎖（Hub 探索入口可見）時發布的事件。</summary>
    public class ExplorationFeatureUnlockedEvent : GameEventBase { }

    // ----- MainQuestManager 相關事件 -----

    /// <summary>主線任務切換為 Available（可承接）時發布的事件。</summary>
    public class MainQuestAvailableEvent : GameEventBase
    {
        /// <summary>可承接的任務 ID。</summary>
        public string QuestId;
    }

    /// <summary>主線任務開始（進入 InProgress）時發布的事件。</summary>
    public class MainQuestStartedEvent : GameEventBase
    {
        /// <summary>開始的任務 ID。</summary>
        public string QuestId;
    }

    /// <summary>主線任務完成時發布的事件。</summary>
    public class MainQuestCompletedEvent : GameEventBase
    {
        /// <summary>完成的任務 ID。</summary>
        public string QuestId;
    }

    // ----- 守衛歸來事件相關（B10 + Sprint 6 擴張） -----

    /// <summary>
    /// 守衛歸來事件開始播放時發布的事件。
    /// 由 GuardReturnEventController 在攔截首次探索後、開始播放對話前發布。
    /// 訂閱者可用於暫停其他輸入、顯示全螢幕劇情遮罩等。
    /// </summary>
    public class GuardReturnEventStartedEvent : GameEventBase { }

    /// <summary>
    /// 守衛歸來事件完成時發布的事件。
    /// Sprint 6 擴張：事件完成後不再直接贈劍；
    /// 贈劍改由玩家主動向守衛發問「要拿劍」特殊題時觸發。
    /// </summary>
    public class GuardReturnEventCompletedEvent : GameEventBase { }

    /// <summary>
    /// 探索入口進入「鎖定」狀態（守衛歸來事件完成後）時發布的事件（Sprint 6 擴張）。
    /// VillageHubView 訂閱此事件後切換探索按鈕為「可見但鎖定」狀態。
    /// </summary>
    public class ExplorationGateLockedEvent : GameEventBase { }

    /// <summary>
    /// 探索入口從「鎖定」狀態重新開啟（玩家發問「要拿劍」成功後）時發布的事件（Sprint 6 擴張）。
    /// VillageHubView 訂閱此事件後恢復探索按鈕為正常可互動狀態。
    /// </summary>
    public class ExplorationGateReopenedEvent : GameEventBase { }

    /// <summary>
    /// 玩家發問單次特殊題觸發完成時發布的事件（Sprint 6 擴張）。
    /// PlayerQuestionsManager 觸發 trigger_flag 對應的效果後發布。
    /// </summary>
    public class PlayerSpecialQuestionTriggeredEvent : GameEventBase
    {
        /// <summary>角色 ID。</summary>
        public string CharacterId;

        /// <summary>題目 ID。</summary>
        public string QuestionId;

        /// <summary>觸發旗標 ID（對應 trigger_flag 欄位）。</summary>
        public string TriggerFlag;
    }

    /// <summary>
    /// 探索入口鎖定狀態下玩家點擊探索按鈕時發布的事件（Sprint 6 擴張，C3 決策）。
    /// VillageHubView 訂閱此事件後每次點擊都顯示完整提示 modal。
    /// </summary>
    public class ExplorationGateLockedClickedEvent : GameEventBase { }

    // ----- 紅點系統相關（B7） -----

    /// <summary>
    /// 紅點的層級（依優先序由高至低：L1 &gt; L4 &gt; FirstMeet &gt; L3 &gt; L2，GDD commission-system.md § 4.3）。
    /// 數值定義為顯示用排序依據；GetHighest 以自訂優先序判斷。
    /// </summary>
    public enum RedDotLayer
    {
        /// <summary>無紅點。</summary>
        None = 0,

        /// <summary>L2 — 角色發問層（好感度門檻/時間差/隨機節奏；優先序最低）。</summary>
        CharacterQuestion = 2,

        /// <summary>L3 — 新任務層（該角色擁有新的可承接主線任務）。</summary>
        NewQuest = 3,

        /// <summary>L4 — 主線事件層（節點劇情待推進，優先序次高）。</summary>
        MainQuestEvent = 4,

        /// <summary>L1 — 委託完成層（優先序最高）。</summary>
        CommissionCompleted = 1,

        /// <summary>角色首次登場（剛解鎖、尚未進入過其互動畫面播放登場 CG）。介於 L4 與 L3 之間。</summary>
        FirstMeet = 5,
    }

    /// <summary>
    /// 紅點狀態變化時發布的事件。
    /// 當某角色的任一紅點層級觸發或解除時，RedDotManager 發布此事件。
    /// UI 層（VillageHubView）訂閱此事件更新角色按鈕上的紅點顯示。
    /// </summary>
    public class RedDotUpdatedEvent : GameEventBase
    {
        /// <summary>受影響的角色 ID。</summary>
        public string CharacterId;

        /// <summary>該角色當前最高優先序的紅點層級（None 表示無紅點）。</summary>
        public RedDotLayer HighestLayer;
    }

    // ----- 開場劇情演出系統相關（B9） -----

    /// <summary>
    /// 開場劇情演出序列開始時發布的事件。
    /// 由 OpeningSequenceController 在呼叫 StartOpeningSequence 後、進入第一階段前發布。
    /// </summary>
    public class OpeningSequenceStartedEvent : GameEventBase { }

    /// <summary>
    /// 開場劇情演出序列完成時發布的事件。
    /// 完成時玩家已取得 Hub 主動權（返回按鈕開啟）、角色按鈕已解鎖、初始資源已發放。
    /// </summary>
    public class OpeningSequenceCompletedEvent : GameEventBase { }

    /// <summary>
    /// 節點劇情對話開始播放時發布的事件（B9 NodeDialogueController）。
    /// 適用於節點 0/1/2。UI 層可用於切換至強制模式 interact view。
    /// </summary>
    public class NodeDialogueStartedEvent : GameEventBase
    {
        /// <summary>節點 ID（node_0 / node_1 / node_2）。</summary>
        public string NodeId;
    }

    /// <summary>
    /// 節點劇情對話完成時發布的事件（B9 NodeDialogueController）。
    /// 完成時分支選擇已透過 DialogueChoiceSelectedEvent 推進至 CharacterUnlockManager。
    /// </summary>
    public class NodeDialogueCompletedEvent : GameEventBase
    {
        /// <summary>完成的節點 ID。</summary>
        public string NodeId;

        /// <summary>玩家選擇的分支 ID（無選項時為 null 或空）。</summary>
        public string SelectedBranchId;
    }

    // ----- CG 播放系統相關（B9 / B13） -----

    /// <summary>
    /// CG 播放開始時發布的事件（ICGPlayer 實作發布）。
    /// B9 的 PlaceholderCGPlayer 亦會發布此事件以供 UI 預留處理。
    /// </summary>
    public class CGPlaybackStartedEvent : GameEventBase
    {
        /// <summary>播放的介紹 ID（對應 character-intro-config.json 的 intro_id）。</summary>
        public string IntroId;

        /// <summary>所屬角色 ID。</summary>
        public string CharacterId;
    }

    /// <summary>CG 播放完成時發布的事件（ICGPlayer 實作發布）。</summary>
    public class CGPlaybackCompletedEvent : GameEventBase
    {
        /// <summary>已播放完成的介紹 ID。</summary>
        public string IntroId;

        /// <summary>所屬角色 ID。</summary>
        public string CharacterId;
    }

    // ----- Sprint 5 對話功能修正相關事件 -----

    /// <summary>
    /// 角色發問倒數完成事件（Sprint 5 B1/B19）。
    /// CharacterQuestionCountdownManager 倒數至 0 時發布一次，L2 紅點由此觸發。
    /// 由於紅點累積上限 = 1，紅點亮後再觸發的 Ready 不會再次累積。
    /// </summary>
    public class CharacterQuestionCountdownReadyEvent : GameEventBase
    {
        /// <summary>倒數完成的角色 ID。</summary>
        public string CharacterId;
    }

    /// <summary>
    /// 角色發問：題目呈現時發布的事件（Sprint 5 B5/B19）。
    /// </summary>
    public class CharacterQuestionAskedEvent : GameEventBase
    {
        /// <summary>角色 ID。</summary>
        public string CharacterId;

        /// <summary>該角色當前的好感度等級（1~7）。</summary>
        public int Level;

        /// <summary>題目 ID。</summary>
        public string QuestionId;
    }

    /// <summary>
    /// 角色發問：玩家選擇選項後發布的事件（Sprint 5 B5/B19）。
    /// </summary>
    public class CharacterQuestionAnsweredEvent : GameEventBase
    {
        /// <summary>角色 ID。</summary>
        public string CharacterId;

        /// <summary>題目 ID。</summary>
        public string QuestionId;

        /// <summary>選到的個性 ID（personality_gentle/lively/calm/assertive）。</summary>
        public string SelectedPersonality;

        /// <summary>該角色對此個性的好感度增量（+0/+2/+5/+10 placeholder）。</summary>
        public int AffinityDelta;
    }

    /// <summary>
    /// 玩家發問冷卻開始事件（Sprint 5 B10/B19）。
    /// </summary>
    public class DialogueCooldownStartedEvent : GameEventBase
    {
        /// <summary>進入 CD 的角色 ID。</summary>
        public string CharacterId;

        /// <summary>設定的冷卻秒數（已套用 ×2 倍率者為 120s）。</summary>
        public float DurationSeconds;
    }

    /// <summary>
    /// 玩家發問冷卻完成事件（Sprint 5 B10/B19）。
    /// </summary>
    public class DialogueCooldownCompletedEvent : GameEventBase
    {
        /// <summary>完成 CD 的角色 ID。</summary>
        public string CharacterId;
    }

    /// <summary>
    /// 招呼語播放事件（Sprint 5 B16/B19）。
    /// GreetingPresenter 進入角色 Normal 狀態時發布。
    /// </summary>
    public class GreetingPlayedEvent : GameEventBase
    {
        /// <summary>角色 ID。</summary>
        public string CharacterId;

        /// <summary>好感度等級。</summary>
        public int Level;

        /// <summary>招呼語 ID。</summary>
        public string GreetingId;
    }

    /// <summary>
    /// [閒聊] 模式觸發事件（Sprint 5 B12/B19）。
    /// 玩家發問 40 題池耗盡後，點擊 [閒聊] 時觸發。
    /// </summary>
    public class IdleChatTriggeredEvent : GameEventBase
    {
        /// <summary>角色 ID。</summary>
        public string CharacterId;

        /// <summary>抽到的問題 ID。</summary>
        public string TopicId;

        /// <summary>抽到的回答 ID。</summary>
        public string AnswerId;
    }
}
