// CommissionInstaller — 委託與倉庫擴建域 Installer（ADR-003 B4e / Sprint 7 E5）。
//
// 負責委託管理（CommissionManager）與倉庫擴建（StorageExpansionManager）的建構、
// 事件訂閱與 Tick 驅動。
//
// Install 依賴（來自 ctx）：
//   - ctx.TimeProvider（由 CoreStorageInstaller 填入）
//
// 建構子注入：
//   - CommissionRecipesConfigData（委託配方 JSON DTO）
//   - StorageExpansionConfigData（倉庫擴建 JSON DTO）
//   - BackpackManager（供 CommissionManager / StorageExpansionManager 使用）
//   - StorageManager（供 CommissionManager / StorageExpansionManager 使用）
//
// 產出到 ctx：無新 ctx 欄位（ADR-003 D5 #5）。
//
// 事件訂閱：CommissionManager 自管（CommissionStartedEvent / CompletedEvent / ClaimedEvent）
// Tick 驅動：IVillageTickable.Tick() → CommissionManager.Tick() + StorageExpansionManager.Tick()
//
// ADR 遵循：ADR-003（IVillageInstaller 契約）、ADR-004（Village/Core/ 路徑）

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.Commission;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.Storage;

namespace ProjectDR.Village.Core
{
    /// <summary>
    /// 委託與倉庫擴建域 Installer（#5，第五個安裝）。
    /// 純 POCO，禁止繼承 MonoBehaviour。
    /// 實作 IVillageTickable 以驅動 CommissionManager 和 StorageExpansionManager 的時間推進。
    /// </summary>
    public class CommissionInstaller : IVillageInstaller, IVillageTickable
    {
        // ===== 建構子注入（由 VillageEntryPoint 傳入） =====

        private readonly CommissionRecipeData[] _commissionEntries;
        private readonly StorageExpansionStageData[] _storageExpansionStageEntries;
        private readonly StorageExpansionRequirementData[] _storageExpansionRequirementEntries;
        private readonly BackpackManager _backpackManager;
        private readonly StorageManager _storageManager;
        private readonly IReadOnlyList<string> _allowedCommissionCharacterIds;

        // ===== Install 後的 Manager 實例 =====

        private CommissionManager _commissionManager;
        private StorageExpansionManager _storageExpansionManager;

        /// <summary>
        /// 建構 CommissionInstaller，注入所需配置純陣列與依賴。
        /// </summary>
        /// <param name="commissionEntries">委託配方 DTO 陣列（不可為 null）。</param>
        /// <param name="storageExpansionStageEntries">倉庫擴建階段主表 DTO 陣列（不可為 null）。</param>
        /// <param name="storageExpansionRequirementEntries">倉庫擴建需求子表 DTO 陣列（不可為 null）。</param>
        /// <param name="backpackManager">背包管理器（不可為 null）。</param>
        /// <param name="storageManager">倉庫管理器（不可為 null）。</param>
        public CommissionInstaller(
            CommissionRecipeData[] commissionEntries,
            StorageExpansionStageData[] storageExpansionStageEntries,
            StorageExpansionRequirementData[] storageExpansionRequirementEntries,
            BackpackManager backpackManager,
            StorageManager storageManager,
            IReadOnlyList<string> allowedCommissionCharacterIds = null)
        {
            _commissionEntries = commissionEntries
                ?? throw new ArgumentNullException(nameof(commissionEntries));
            _storageExpansionStageEntries = storageExpansionStageEntries
                ?? throw new ArgumentNullException(nameof(storageExpansionStageEntries));
            _storageExpansionRequirementEntries = storageExpansionRequirementEntries
                ?? throw new ArgumentNullException(nameof(storageExpansionRequirementEntries));
            _backpackManager = backpackManager
                ?? throw new ArgumentNullException(nameof(backpackManager));
            _storageManager = storageManager
                ?? throw new ArgumentNullException(nameof(storageManager));
            _allowedCommissionCharacterIds = allowedCommissionCharacterIds; // null = 全部啟用
        }

        // ===== IVillageInstaller =====

        /// <summary>
        /// 建構委託與倉庫擴建 Manager 並完成依賴注入。
        /// 依賴 ctx.TimeProvider（由 CoreStorageInstaller 在 #1 Install 時填入）。
        /// </summary>
        /// <param name="ctx">Village 跨 Installer 共用服務容器（不可為 null）。</param>
        /// <exception cref="InvalidOperationException">ctx 為 null 或 ctx.TimeProvider 未就位時拋出。</exception>
        public void Install(VillageContext ctx)
        {
            if (ctx == null)
                throw new InvalidOperationException("CommissionInstaller.Install: ctx 不可為 null");

            if (ctx.TimeProvider == null)
                throw new InvalidOperationException(
                    "CommissionInstaller.Install: ctx.TimeProvider 未就位，請確認 CoreStorageInstaller 已在 CommissionInstaller 之前 Install。");

            // 1. 建構 CommissionManager（自管事件訂閱）
            CommissionRecipesConfig recipesConfig = new CommissionRecipesConfig(_commissionEntries);
            _commissionManager = new CommissionManager(
                recipesConfig,
                _backpackManager,
                _storageManager,
                ctx.TimeProvider,
                _allowedCommissionCharacterIds);

            // 2. 建構 StorageExpansionManager
            StorageExpansionConfig expansionConfig = new StorageExpansionConfig(
                _storageExpansionStageEntries, _storageExpansionRequirementEntries);
            _storageExpansionManager = new StorageExpansionManager(
                _storageManager,
                _backpackManager,
                expansionConfig);
        }

        /// <summary>
        /// 取消事件訂閱，並清理 Manager 持有的資源。
        /// CommissionManager 不實作 IDisposable，但會在 Uninstall 時自動解除事件訂閱。
        /// </summary>
        public void Uninstall()
        {
            // CommissionManager 不實作 IDisposable，不需要顯式清理。
        }

        // ===== IVillageTickable =====

        /// <summary>
        /// 每幀推進 CommissionManager（委託計時）與 StorageExpansionManager（擴建計時）。
        /// </summary>
        /// <param name="deltaTime">距上一幀的秒數。</param>
        public void Tick(float deltaTime)
        {
            _commissionManager?.Tick(deltaTime);
            _storageExpansionManager?.Tick(deltaTime);
        }

        // ===== 公開查詢（供 VillageEntryPoint 在 Install 後取得實例）=====

        /// <summary>取得 CommissionManager（Install 後可用）。</summary>
        public CommissionManager GetCommissionManager() => _commissionManager;

        /// <summary>取得 StorageExpansionManager（Install 後可用）。</summary>
        public StorageExpansionManager GetStorageExpansionManager() => _storageExpansionManager;
    }
}
