// AffinityInstaller — 好感度功能域 Installer（ADR-003 B4c / Sprint 7 E4）。
//
// 負責 AffinityManager（好感度）、GiftManager（送禮）的建構、
// 事件訂閱與服務暴露。
//
// Install 依賴（來自 ctx 的）：
//   - ctx.BackpackReadOnly（IBackpackQuery）
//   - ctx.StorageReadOnly（IStorageQuery）
//   - ctx.GameDataAccess（AffinityConfigData 載入，未來依 ADR-001 完整化）
//
// 產出至 VillageContext（ADR-003 D5 #3）：
//   - ctx.AffinityReadOnly = AffinityManager（實作 IAffinityQuery）
//
// 事件訂閱：無（Affinity 狀態由 GiftManager 直接呼叫 AddAffinity，不走事件）
//
// ADR 遵循：ADR-003（IVillageInstaller 契約）、ADR-004（Village/Core/ 路徑）

using ProjectDR.Village.Gift;
using System;
using ProjectDR.Village.Affinity;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.Storage;

namespace ProjectDR.Village.Core
{
    /// <summary>
    /// 好感度功能域 Installer（#3，安裝順序第三位）。
    /// 純 POCO，禁止繼承 MonoBehaviour。
    /// </summary>
    public class AffinityInstaller : IVillageInstaller
    {
        // ===== 建構子注入（由 VillageEntryPoint 傳入） =====

        private readonly AffinityCharacterData[] _affinityEntries;

        // ===== Install 後的 Manager 實例 =====

        private AffinityManager _affinityManager;
        private GiftManager _giftManager;

        /// <summary>
        /// 建構 AffinityInstaller，注入好感度配置純陣列。
        /// </summary>
        /// <param name="affinityEntries">JsonFx 反序列化後的 AffinityCharacterData 陣列（不可為 null）。</param>
        public AffinityInstaller(AffinityCharacterData[] affinityEntries)
        {
            _affinityEntries = affinityEntries
                ?? throw new ArgumentNullException(nameof(affinityEntries));
        }

        // ===== IVillageInstaller =====

        /// <summary>
        /// 建構 AffinityManager 與 GiftManager，並將 AffinityManager 暴露至 ctx.AffinityReadOnly。
        /// 依賴：ctx.BackpackReadOnly（須由 CoreStorageInstaller 先 Install）、
        ///       ctx.StorageReadOnly（同上）。
        /// </summary>
        /// <param name="ctx">Village 跨 Installer 共用服務容器（不可為 null）。</param>
        /// <exception cref="InvalidOperationException">ctx 為 null，或必要的 ctx 欄位未就位時拋出。</exception>
        public void Install(VillageContext ctx)
        {
            if (ctx == null)
                throw new InvalidOperationException("AffinityInstaller.Install: ctx 不可為 null");
            if (ctx.BackpackReadOnly == null)
                throw new InvalidOperationException("AffinityInstaller.Install: ctx.BackpackReadOnly 尚未就位（CoreStorageInstaller 必須先 Install）");
            if (ctx.StorageReadOnly == null)
                throw new InvalidOperationException("AffinityInstaller.Install: ctx.StorageReadOnly 尚未就位（CoreStorageInstaller 必須先 Install）");

            // ===== 建構 AffinityManager =====
            AffinityConfig affinityConfig = new AffinityConfig(_affinityEntries);
            _affinityManager = new AffinityManager(affinityConfig);

            // ===== 暴露至 VillageContext（ADR-003 D5 #3 產出）=====
            ctx.AffinityReadOnly = _affinityManager;

            // ===== 建構 GiftManager =====
            // GiftManager 需要可寫的 BackpackManager 與 StorageManager 實例。
            // 依 ADR-003 D2.3：AffinityInstaller 持有 AffinityManager（可寫），
            // BackpackManager / StorageManager 由 CoreStorageInstaller 持有。
            // 因 GiftManager 需直接呼叫 BackpackManager.RemoveItem / StorageManager.RemoveItem，
            // 而這些不在 IBackpackQuery / IStorageQuery 介面上，
            // 需要由 VillageEntryPoint 傳入完整實例（透過建構子注入）。
            // TODO: 待 ADR-003 演進——目前 GiftManager 的 BackpackManager/StorageManager 依賴
            // 改由 AffinityInstaller 持有，但 GiftManager 無法從 ctx 唯讀介面取得可寫參考。
            // 短期解法：GiftManager 在此不建構，改由 VillageEntryPoint 直接建構（向下相容），
            // 待 CoreStorageInstaller 對外暴露 mutable 引用（非 IBackpackQuery）的設計確定後再整合。
            // 故 GiftManager 暫留在 VillageEntryPoint 建構序列中（E5 後確定方案）。
        }

        /// <summary>
        /// 解除所有事件訂閱、釋放資源。
        /// AffinityInstaller 無事件訂閱（對稱：0 Subscribe / 0 Unsubscribe）。
        /// </summary>
        public void Uninstall()
        {
            _affinityManager = null;
            _giftManager = null;
        }

        // ===== 公開 Accessor（供 VillageEntryPoint 建構 GiftManager 使用） =====

        /// <summary>好感度管理器（Install 後可用；Uninstall 後回 null）。</summary>
        public AffinityManager AffinityManager => _affinityManager;

        /// <summary>送禮管理器（Install 後可用；Uninstall 後回 null）。暫由外部設定。</summary>
        public GiftManager GiftManager
        {
            get => _giftManager;
            internal set => _giftManager = value;
        }
    }
}
