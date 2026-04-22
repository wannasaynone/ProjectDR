// CoreStorageInstaller — 核心倉庫功能域 Installer（ADR-003 B4a / Sprint 7 E4）。
//
// 負責 BackpackManager（背包）、StorageManager（倉庫）、StorageTransferManager（轉移）
// 的建構與服務暴露。
//
// Install 依賴（來自 ctx 的）：無（CoreStorageInstaller 是第一批安裝的 Installer，
//   不依賴其他 Installer 的 ctx 欄位）。
//
// 產出至 VillageContext（ADR-003 D5 #1/#2）：
//   - ctx.BackpackReadOnly = BackpackManager（實作 IBackpackQuery）
//   - ctx.StorageReadOnly  = StorageManager（實作 IStorageQuery）
//
// 事件訂閱：無（BackpackManager / StorageManager 透過 EventBus 自行發布變更事件，
//   CoreStorageInstaller 不額外訂閱）
//
// 安裝順序：#1（最先安裝，其他 Installer 依賴 BackpackReadOnly / StorageReadOnly）
//
// ADR 遵循：ADR-003（IVillageInstaller 契約）、ADR-004（Village/Core/ 路徑）

using System;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.Storage;
using ProjectDR.Village.TimeProvider;

namespace ProjectDR.Village.Core
{
    /// <summary>
    /// 核心倉庫功能域 Installer（#1，最先安裝）。
    /// 純 POCO，禁止繼承 MonoBehaviour。
    /// 建構 BackpackManager、StorageManager、StorageTransferManager，
    /// 並將唯讀查詢介面暴露至 VillageContext。
    /// </summary>
    public class CoreStorageInstaller : IVillageInstaller
    {
        // ===== 建構子注入（由 VillageEntryPoint 傳入） =====

        private readonly int _backpackMaxSlots;
        private readonly int _backpackMaxStack;
        private readonly int _storageInitialCapacity;
        private readonly int _storageMaxStack;

        // ===== Install 後的 Manager 實例 =====

        private BackpackManager _backpackManager;
        private StorageManager _storageManager;
        private StorageTransferManager _storageTransferManager;
        private ITimeProvider _timeProvider;

        /// <summary>
        /// 建構 CoreStorageInstaller，注入倉庫與背包的容量設定。
        /// </summary>
        /// <param name="backpackMaxSlots">背包最大格子數（必須大於 0）。</param>
        /// <param name="backpackMaxStack">背包預設堆疊上限（必須大於 0）。</param>
        /// <param name="storageInitialCapacity">倉庫初始容量格子數（必須大於 0）。</param>
        /// <param name="storageMaxStack">倉庫預設堆疊上限（必須大於 0）。</param>
        /// <exception cref="ArgumentException">任何容量/堆疊數小於等於 0 時拋出。</exception>
        public CoreStorageInstaller(
            int backpackMaxSlots,
            int backpackMaxStack,
            int storageInitialCapacity,
            int storageMaxStack)
        {
            if (backpackMaxSlots <= 0)
                throw new ArgumentException("背包最大格子數必須大於 0。", nameof(backpackMaxSlots));
            if (backpackMaxStack <= 0)
                throw new ArgumentException("背包堆疊上限必須大於 0。", nameof(backpackMaxStack));
            if (storageInitialCapacity <= 0)
                throw new ArgumentException("倉庫初始容量必須大於 0。", nameof(storageInitialCapacity));
            if (storageMaxStack <= 0)
                throw new ArgumentException("倉庫堆疊上限必須大於 0。", nameof(storageMaxStack));

            _backpackMaxSlots = backpackMaxSlots;
            _backpackMaxStack = backpackMaxStack;
            _storageInitialCapacity = storageInitialCapacity;
            _storageMaxStack = storageMaxStack;
        }

        // ===== IVillageInstaller =====

        /// <summary>
        /// 建構 BackpackManager、StorageManager、StorageTransferManager，
        /// 並將唯讀查詢介面填入 VillageContext。
        /// 無前序 Installer 依賴。
        /// </summary>
        /// <param name="ctx">Village 跨 Installer 共用服務容器（不可為 null）。</param>
        /// <exception cref="InvalidOperationException">ctx 為 null 時拋出。</exception>
        public void Install(VillageContext ctx)
        {
            if (ctx == null)
                throw new InvalidOperationException("CoreStorageInstaller.Install: ctx 不可為 null");

            // ===== 建構 BackpackManager =====
            _backpackManager = new BackpackManager(_backpackMaxSlots, _backpackMaxStack);

            // ===== 建構 StorageManager =====
            _storageManager = new StorageManager(_storageInitialCapacity, _storageMaxStack);

            // ===== 建構 StorageTransferManager =====
            _storageTransferManager = new StorageTransferManager(_backpackManager, _storageManager);

            // ===== 建構 ITimeProvider（供 Farm / Commission / Countdown 使用）=====
            _timeProvider = new SystemTimeProvider();

            // ===== 暴露至 VillageContext（ADR-003 D5 #1/#2）=====
            ctx.BackpackReadOnly = _backpackManager;
            ctx.StorageReadOnly = _storageManager;
            ctx.TimeProvider = _timeProvider;
        }

        /// <summary>
        /// 釋放所有 Manager 引用。
        /// CoreStorageInstaller 無事件訂閱（對稱：0 Subscribe / 0 Unsubscribe）。
        /// </summary>
        public void Uninstall()
        {
            _backpackManager = null;
            _storageManager = null;
            _storageTransferManager = null;
            _timeProvider = null;
        }

        // ===== 公開 Accessor（供 VillageEntryPoint 或其他 Installer 取得可寫引用） =====

        /// <summary>背包管理器（Install 後可用；Uninstall 後回 null）。</summary>
        public BackpackManager BackpackManager => _backpackManager;

        /// <summary>倉庫管理器（Install 後可用；Uninstall 後回 null）。</summary>
        public StorageManager StorageManager => _storageManager;

        /// <summary>倉庫轉移管理器（Install 後可用；Uninstall 後回 null）。</summary>
        public StorageTransferManager StorageTransferManager => _storageTransferManager;

        /// <summary>時間提供者（Install 後可用；Uninstall 後回 null）。</summary>
        public ITimeProvider TimeProvider => _timeProvider;
    }
}
