// ProgressionInstaller — 村莊進度域 Installer（ADR-003 B4b / Sprint 7 E5）。
//
// 負責主線任務、角色解鎖、村莊進度、紅點 4 層等進度相關 Manager 的建構、
// 事件訂閱與服務暴露。
//
// Install 依賴（來自 ctx）：
//   - 無 ctx 欄位依賴（所有 Manager 透過 config 建構子注入）
//   - 建構子注入：MainQuestConfigData、InitialResourcesConfigData、
//                 BackpackManager、StorageManager（供 InitialResourceDispatcher 使用）
//
// 產出到 ctx：
//   - VillageProgressionReadOnly（IVillageProgressionQuery）
//
// 事件訂閱（7 對，對稱）：
//   - 由 CharacterUnlockManager 自管：
//       DialogueChoiceSelectedEvent、NodeDialogueCompletedEvent、
//       MainQuestCompletedEvent、GuardReturnEventCompletedEvent
//   - 由 RedDotManager 自管：
//       CommissionCompletedEvent、CommissionClaimedEvent、
//       MainQuestAvailableEvent、MainQuestStartedEvent、MainQuestCompletedEvent、
//       CharacterUnlockedEvent、CharacterQuestionCountdownReadyEvent
//   - 本 Installer 額外訂閱（用於 VillageProgressionManager.ForceUnlock）：
//       MainQuestCompletedEvent → OnMainQuestCompletedForAreaUnlock
//
// ADR 遵循：ADR-003（IVillageInstaller 契約）、ADR-004（Village/Core/ 路徑）

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.CharacterUnlock;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.Progression;
using ProjectDR.Village.Storage;

namespace ProjectDR.Village.Core
{
    /// <summary>
    /// 村莊進度域 Installer（#2，第二個安裝）。
    /// 純 POCO，禁止繼承 MonoBehaviour。
    /// 管理 MainQuestManager、CharacterUnlockManager、VillageProgressionManager、RedDotManager。
    /// </summary>
    public class ProgressionInstaller : IVillageInstaller
    {
        // ===== 建構子注入（由 VillageEntryPoint 傳入） =====

        private readonly MainQuestData[] _mainQuestEntries;
        private readonly MainQuestUnlockData[] _mainQuestUnlockEntries;
        private readonly InitialResourceGrantData[] _initialResourceGrantEntries;
        private readonly BackpackManager _backpackManager;
        private readonly StorageManager _storageManager;

        // ===== Install 後的 Manager 實例 =====

        private MainQuestConfig _mainQuestConfig;
        private MainQuestManager _mainQuestManager;
        private InitialResourcesConfig _initialResourcesConfig;
        private InitialResourceDispatcher _initialResourceDispatcher;
        private CharacterUnlockManager _characterUnlockManager;
        private VillageProgressionManager _villageProgressionManager;
        private RedDotManager _redDotManager;

        // ===== 事件 Handler 快取（供 Uninstall 對稱解除） =====

        private Action<MainQuestCompletedEvent> _onMainQuestCompletedForAreaUnlock;

        /// <summary>
        /// 建構 ProgressionInstaller，注入所需配置純陣列與依賴。
        /// </summary>
        /// <param name="mainQuestEntries">主線任務主表 DTO 陣列（不可為 null）。</param>
        /// <param name="mainQuestUnlockEntries">主線任務解鎖子表 DTO 陣列（不可為 null）。</param>
        /// <param name="initialResourceGrantEntries">初始資源發放 DTO 陣列（不可為 null）。</param>
        /// <param name="backpackManager">背包管理器（不可為 null；供 InitialResourceDispatcher 發放物品）。</param>
        /// <param name="storageManager">倉庫管理器（不可為 null；供 InitialResourceDispatcher 溢出入庫）。</param>
        public ProgressionInstaller(
            MainQuestData[] mainQuestEntries,
            MainQuestUnlockData[] mainQuestUnlockEntries,
            InitialResourceGrantData[] initialResourceGrantEntries,
            BackpackManager backpackManager,
            StorageManager storageManager)
        {
            _mainQuestEntries = mainQuestEntries
                ?? throw new ArgumentNullException(nameof(mainQuestEntries));
            _mainQuestUnlockEntries = mainQuestUnlockEntries
                ?? throw new ArgumentNullException(nameof(mainQuestUnlockEntries));
            _initialResourceGrantEntries = initialResourceGrantEntries
                ?? throw new ArgumentNullException(nameof(initialResourceGrantEntries));
            _backpackManager = backpackManager
                ?? throw new ArgumentNullException(nameof(backpackManager));
            _storageManager = storageManager
                ?? throw new ArgumentNullException(nameof(storageManager));
        }

        // ===== IVillageInstaller =====

        /// <summary>
        /// 建構進度域各 Manager 並訂閱所需事件。
        /// 建構順序：MainQuestConfig → MainQuestManager → InitialResourcesConfig →
        ///           InitialResourceDispatcher → CharacterUnlockManager →
        ///           VillageProgressionManager → RedDotManager。
        /// 完成後將 IVillageProgressionQuery 注入 ctx.VillageProgressionReadOnly。
        /// </summary>
        /// <param name="ctx">Village 跨 Installer 共用服務容器（不可為 null）。</param>
        /// <exception cref="InvalidOperationException">ctx 為 null 時拋出。</exception>
        public void Install(VillageContext ctx)
        {
            if (ctx == null)
                throw new InvalidOperationException("ProgressionInstaller.Install: ctx 不可為 null");

            // 1. 建構 MainQuestConfig + MainQuestManager
            _mainQuestConfig = new MainQuestConfig(_mainQuestEntries, _mainQuestUnlockEntries);
            _mainQuestManager = new MainQuestManager(_mainQuestConfig);

            // 2. 建構 InitialResourcesConfig + InitialResourceDispatcher
            _initialResourcesConfig = new InitialResourcesConfig(_initialResourceGrantEntries);
            _initialResourceDispatcher = new InitialResourceDispatcher(_backpackManager, _storageManager);

            // 3. 建構 CharacterUnlockManager（自管事件訂閱）
            _characterUnlockManager = new CharacterUnlockManager(_initialResourcesConfig, _initialResourceDispatcher);

            // 4. 建構 VillageProgressionManager（初始僅 Storage 解鎖）
            _villageProgressionManager = new VillageProgressionManager();

            // 5. 建構 RedDotManager（自管事件訂閱）
            _redDotManager = new RedDotManager(_mainQuestConfig, _mainQuestManager);

            // 6. 訂閱 MainQuestCompletedEvent 以推進 VillageProgressionManager 的區域解鎖
            _onMainQuestCompletedForAreaUnlock = OnMainQuestCompletedForAreaUnlock;
            EventBus.Subscribe(_onMainQuestCompletedForAreaUnlock);

            // 7. 填入 ctx.VillageProgressionReadOnly
            ctx.VillageProgressionReadOnly = new VillageProgressionQueryAdapter(
                _characterUnlockManager,
                _villageProgressionManager);
        }

        /// <summary>
        /// 取消事件訂閱，並 Dispose 實作 IDisposable 的 Manager。
        /// </summary>
        public void Uninstall()
        {
            EventBus.Unsubscribe(_onMainQuestCompletedForAreaUnlock);

            _redDotManager?.Dispose();
            _characterUnlockManager?.Dispose();
        }

        // ===== 公開查詢（供 VillageEntryPoint 在 Install 後取得實例）=====

        /// <summary>取得 RedDotManager（Install 後可用）。</summary>
        public RedDotManager GetRedDotManager() => _redDotManager;

        /// <summary>取得 MainQuestManager（Install 後可用）。</summary>
        public MainQuestManager GetMainQuestManager() => _mainQuestManager;

        /// <summary>取得 CharacterUnlockManager（Install 後可用）。</summary>
        public CharacterUnlockManager GetCharacterUnlockManager() => _characterUnlockManager;

        /// <summary>取得 VillageProgressionManager（Install 後可用）。</summary>
        public VillageProgressionManager GetVillageProgressionManager() => _villageProgressionManager;

        // ===== 事件處理 =====

        /// <summary>
        /// 主線任務完成 → 驅動 VillageProgressionManager 的區域解鎖。
        /// T1 完成：unlock_on_complete 包含 "exploration_open"，此事件用於同步 VillageProgressionManager。
        /// 實際的探索功能解鎖（ExplorationFeatureUnlockedEvent）由 CharacterUnlockManager 自行監聽 T1 完成並觸發。
        /// </summary>
        private void OnMainQuestCompletedForAreaUnlock(MainQuestCompletedEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.QuestId)) return;

            MainQuestInfo info = _mainQuestConfig.GetQuest(e.QuestId);
            if (info == null) return;

            foreach (string unlockId in info.UnlockOnComplete)
            {
                // 如果解鎖項目對應已知的 AreaId，驅動 VillageProgressionManager 解鎖
                if (unlockId == AreaIds.Exploration || unlockId == "exploration_open")
                {
                    _villageProgressionManager.ForceUnlock(AreaIds.Exploration);
                }
            }
        }

        // ===== 內部 Adapter =====

        /// <summary>
        /// VillageProgressionQuery 的 Adapter 實作。
        /// 整合 CharacterUnlockManager（角色解鎖狀態）與 VillageProgressionManager（區域解鎖狀態）。
        /// </summary>
        private sealed class VillageProgressionQueryAdapter : IVillageProgressionQuery
        {
            private readonly CharacterUnlockManager _characterUnlockManager;
            private readonly VillageProgressionManager _villageProgressionManager;

            public VillageProgressionQueryAdapter(
                CharacterUnlockManager characterUnlockManager,
                VillageProgressionManager villageProgressionManager)
            {
                _characterUnlockManager = characterUnlockManager;
                _villageProgressionManager = villageProgressionManager;
            }

            /// <inheritdoc />
            public bool IsCharacterUnlocked(string characterId)
            {
                return _characterUnlockManager.IsUnlocked(characterId);
            }

            /// <inheritdoc />
            public bool IsExplorationUnlocked()
            {
                return _characterUnlockManager.IsExplorationFeatureUnlocked
                    || _villageProgressionManager.IsAreaUnlocked(AreaIds.Exploration);
            }
        }
    }
}
