// CharacterUnlockManager — 四位村莊角色 Hub 按鈕解鎖狀態管理器。
// 依據 GDD `character-unlock-system.md` v1.2：
// - 村長夫人開局即解鎖
// - 農女/魔女：節點 0 VN 選項選擇觸發 + 節點 1 VN 選項確認剩餘者觸發
// - 守衛：守衛歸來事件完成時觸發（贈劍同步）
// - 節點 2 完成後：探索功能解鎖（發布 ExplorationFeatureUnlockedEvent）
//
// 此管理器同時負責：
// - 監聽 DialogueChoiceSelectedEvent 對應節點 0/1 的選擇
// - 監聽 MainQuestCompletedEvent 對應 T2（節點 1 觸發）、T3（節點 2 + 探索開放）
// - 監聽 GuardReturnEventCompletedEvent 對應守衛解鎖 + 贈劍
// - 觸發初始資源 grant（呼叫 IInitialResourceDispatcher 交由上層實際發放物品）

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    // ===== 節點對話選項分支 ID 常數 =====

    /// <summary>節點對話的分支 ID 常數（對應 node-dialogue-config.json 的 choice_branch 欄位）。</summary>
    public static class NodeDialogueBranchIds
    {
        public const string FarmGirl = "farm_girl";
        public const string Witch = "witch";
    }

    /// <summary>
    /// 初始資源派發器介面。
    /// CharacterUnlockManager 將 grant 委派給上層處理實際的物品發放（先背包後倉庫等業務邏輯）。
    /// </summary>
    public interface IInitialResourceDispatcher
    {
        /// <summary>
        /// 執行一次 grant 的派發。
        /// 若 grant 帶有物品（HasItem = true）則發放到玩家背包/倉庫。
        /// 若 grant 不帶物品（純標記），則可忽略或記錄日誌。
        /// </summary>
        void Dispatch(InitialResourceGrant grant);
    }

    /// <summary>
    /// 四位村莊角色 Hub 按鈕解鎖管理器 + 探索功能解鎖。
    /// 純邏輯，不依賴 MonoBehaviour。
    /// 實作 IDisposable 以取消事件訂閱。
    /// </summary>
    public class CharacterUnlockManager : IDisposable
    {
        private readonly InitialResourcesConfig _resourcesConfig;
        private readonly IInitialResourceDispatcher _dispatcher;
        private readonly HashSet<string> _unlockedCharacters;

        private bool _explorationFeatureUnlocked;
        private bool _disposed;

        private readonly Action<DialogueChoiceSelectedEvent> _onDialogueChoiceSelected;
        private readonly Action<MainQuestCompletedEvent> _onMainQuestCompleted;
        private readonly Action<GuardReturnEventCompletedEvent> _onGuardReturnCompleted;

        // 追蹤節點選擇狀態，以便節點 1 的選項處理「剩下那位」
        private string _node0ChosenBranch;

        /// <summary>探索功能是否已解鎖。</summary>
        public bool IsExplorationFeatureUnlocked => _explorationFeatureUnlocked;

        /// <summary>節點 0 時玩家選擇的分支（NodeDialogueBranchIds.FarmGirl / Witch）。未選擇時為 null。</summary>
        public string Node0ChosenBranch => _node0ChosenBranch;

        /// <summary>
        /// 建構角色解鎖管理器。
        /// 初始狀態：僅村長夫人解鎖（開局即可互動）。
        /// </summary>
        /// <param name="resourcesConfig">初始資源配置（不可為 null）。</param>
        /// <param name="dispatcher">資源派發器（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">任一參數為 null 時拋出。</exception>
        public CharacterUnlockManager(
            InitialResourcesConfig resourcesConfig,
            IInitialResourceDispatcher dispatcher)
        {
            _resourcesConfig = resourcesConfig ?? throw new ArgumentNullException(nameof(resourcesConfig));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            _unlockedCharacters = new HashSet<string>
            {
                CharacterIds.VillageChiefWife
            };

            _onDialogueChoiceSelected = OnDialogueChoiceSelected;
            _onMainQuestCompleted = OnMainQuestCompleted;
            _onGuardReturnCompleted = OnGuardReturnCompleted;

            EventBus.Subscribe(_onDialogueChoiceSelected);
            EventBus.Subscribe(_onMainQuestCompleted);
            EventBus.Subscribe(_onGuardReturnCompleted);

            // 節點 0 開局 grant（可能發放背包物資等，由 dispatcher 決定如何處理）
            DispatchGrantsByTrigger(InitialResourcesTriggerIds.Node0Start);
        }

        /// <summary>查詢指定角色是否已解鎖。</summary>
        public bool IsUnlocked(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return false;
            return _unlockedCharacters.Contains(characterId);
        }

        /// <summary>取得所有已解鎖的角色 ID（唯讀，不保證順序）。</summary>
        public IReadOnlyCollection<string> GetUnlockedCharacters()
        {
            return _unlockedCharacters;
        }

        /// <summary>
        /// 強制解鎖指定角色（測試/偵錯用途 + 守衛歸來事件完成時）。
        /// 已解鎖則忽略。成功解鎖後發布 CharacterUnlockedEvent。
        /// </summary>
        public void ForceUnlock(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            if (_unlockedCharacters.Contains(characterId)) return;

            _unlockedCharacters.Add(characterId);
            EventBus.Publish(new CharacterUnlockedEvent { CharacterId = characterId });
        }

        /// <summary>
        /// 強制解鎖探索功能（測試/偵錯 + 節點 2 完成）。
        /// 已解鎖則忽略。成功解鎖後發布 ExplorationFeatureUnlockedEvent。
        /// </summary>
        public void ForceUnlockExplorationFeature()
        {
            if (_explorationFeatureUnlocked) return;

            _explorationFeatureUnlocked = true;
            EventBus.Publish(new ExplorationFeatureUnlockedEvent());
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            EventBus.Unsubscribe(_onDialogueChoiceSelected);
            EventBus.Unsubscribe(_onMainQuestCompleted);
            EventBus.Unsubscribe(_onGuardReturnCompleted);
        }

        // ===== 事件處理 =====

        /// <summary>
        /// 監聽 VN 對話選項：
        /// - 節點 0：farm_girl / witch 分支 → 解鎖對應角色 + 觸發對應 grant
        /// - 節點 1：剩下那位的確認選項 → 解鎖剩餘者
        /// 判斷方式：依選項分支 ID 與目前已解鎖狀態組合推論。
        /// </summary>
        private void OnDialogueChoiceSelected(DialogueChoiceSelectedEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.ChoiceId))
            {
                return;
            }

            string branch = e.ChoiceId;

            // 節點 0：尚未解鎖任何農女/魔女 → 依 branch 解鎖
            if (_node0ChosenBranch == null
                && (branch == NodeDialogueBranchIds.FarmGirl || branch == NodeDialogueBranchIds.Witch))
            {
                _node0ChosenBranch = branch;
                UnlockByBranch(branch);
                return;
            }

            // 節點 1：剩餘那位。分支 ID 同樣為 farm_girl/witch（對應「剩下那位」的分支）。
            // 若已有 _node0ChosenBranch 且此次 branch 為另一位，則解鎖剩餘者。
            if (_node0ChosenBranch != null
                && (branch == NodeDialogueBranchIds.FarmGirl || branch == NodeDialogueBranchIds.Witch)
                && branch != _node0ChosenBranch)
            {
                UnlockByBranch(branch);
            }
        }

        /// <summary>
        /// 監聽主線任務完成：
        /// - T3 完成（unlock_on_complete 含 "exploration_open" 或 "node_2_complete"）→ 解鎖探索功能
        /// 節點 0 / 1 / 2 的劇情觸發由上層（B9 開場劇情演出系統）透過訂閱 MainQuestCompletedEvent 自行處理；
        /// 本管理器僅負責「解鎖狀態」變更相關的部分（探索入口可見性）。
        /// </summary>
        private void OnMainQuestCompleted(MainQuestCompletedEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.QuestId)) return;

            // T3 完成時解鎖探索功能（GDD v1.2 明示：節點 2 結束後探索入口可見）
            if (e.QuestId == "T3" && !_explorationFeatureUnlocked)
            {
                ForceUnlockExplorationFeature();
            }
        }

        /// <summary>
        /// 監聽守衛歸來事件完成：解鎖守衛 + 觸發贈劍 grant。
        /// </summary>
        private void OnGuardReturnCompleted(GuardReturnEventCompletedEvent e)
        {
            ForceUnlock(CharacterIds.Guard);
            DispatchGrantsByTrigger(InitialResourcesTriggerIds.GuardReturnEvent);
        }

        // ===== 私有工具 =====

        private void UnlockByBranch(string branch)
        {
            if (branch == NodeDialogueBranchIds.FarmGirl)
            {
                ForceUnlock(CharacterIds.FarmGirl);
                DispatchGrantsByTrigger(InitialResourcesTriggerIds.UnlockFarmGirl);
            }
            else if (branch == NodeDialogueBranchIds.Witch)
            {
                ForceUnlock(CharacterIds.Witch);
                DispatchGrantsByTrigger(InitialResourcesTriggerIds.UnlockWitch);
            }
        }

        private void DispatchGrantsByTrigger(string triggerId)
        {
            IReadOnlyList<InitialResourceGrant> grants = _resourcesConfig.GetGrantsByTrigger(triggerId);
            foreach (InitialResourceGrant grant in grants)
            {
                _dispatcher.Dispatch(grant);
            }
        }
    }
}
