// CGInstaller — CG 功能域 Installer（ADR-003 B4d）。
// 負責 CG 場景解鎖系統的 Manager 建構、事件訂閱與 Uninstall 清理。
//
// Install 依賴：Canvas（可選，供未來 CGGalleryView 使用）、GameDataAccess（CGSceneConfig 載入）
// 事件訂閱（1 對）：CGUnlockedEvent → OnCGUnlocked
// 暴露至 VillageContext：無（CG 域為純事件消費，不暴露新 ctx 欄位；ADR-003 D5 #4）
//
// ADR 遵循：ADR-003（IVillageInstaller 契約）、ADR-004（Village/Core/ 路徑）

using ProjectDR.Village.CG;
using ProjectDR.Village.Navigation;
using System;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Core
{
    /// <summary>
    /// CG 功能域 Installer。
    /// 建構 CGSceneConfig 與 CGUnlockManager，訂閱 AffinityThresholdReachedEvent
    /// 的下游 CGUnlockedEvent，以更新 Gallery 顯示（未來由 CGGalleryView 消費）。
    ///
    /// 此 Installer 為純 POCO（禁止繼承 MonoBehaviour）。
    /// </summary>
    public class CGInstaller : IVillageInstaller
    {
        // ===== 建構子注入（由 VillageEntryPoint 傳入） =====

        private readonly CGSceneData[] _cgSceneEntries;

        // ===== Install 後的 Manager 實例 =====

        private CGSceneConfig _cgSceneConfig;
        private CGUnlockManager _cgUnlockManager;
        private Action<CGUnlockedEvent> _onCGUnlocked;

        /// <summary>
        /// 建構 CGInstaller，注入 CG 場景配置純陣列。
        /// </summary>
        /// <param name="cgSceneEntries">JsonFx 反序列化後的 CGSceneData 陣列（不可為 null）。</param>
        public CGInstaller(CGSceneData[] cgSceneEntries)
        {
            _cgSceneEntries = cgSceneEntries
                ?? throw new ArgumentNullException(nameof(cgSceneEntries));
        }

        // ===== IVillageInstaller =====

        /// <summary>
        /// 建構 CG 域 Manager 並訂閱所需事件。
        /// </summary>
        /// <param name="ctx">Village 跨 Installer 共用服務容器（不可為 null）。</param>
        /// <exception cref="InvalidOperationException">ctx 為 null 時拋出。</exception>
        public void Install(VillageContext ctx)
        {
            if (ctx == null) throw new InvalidOperationException("CGInstaller.Install: ctx 不可為 null");

            // 建構 CGSceneConfig（由 VillageEntryPoint 注入純陣列 DTO）
            _cgSceneConfig = new CGSceneConfig(_cgSceneEntries);
            Village.CG.CGSceneConfig sceneConfig = _cgSceneConfig;

            // 建構 CGUnlockManager（建構內部已訂閱 AffinityThresholdReachedEvent）
            _cgUnlockManager = new CGUnlockManager(sceneConfig);

            // 訂閱 CGUnlockedEvent（ADR-003 事件分散表 L507）
            _onCGUnlocked = OnCGUnlocked;
            EventBus.Subscribe(_onCGUnlocked);
        }

        /// <summary>
        /// 解除所有事件訂閱、Dispose Manager。
        /// Install 訂閱幾次，Uninstall 就要解除幾次（對稱）。
        /// </summary>
        public void Uninstall()
        {
            if (_onCGUnlocked != null)
            {
                EventBus.Unsubscribe(_onCGUnlocked);
                _onCGUnlocked = null;
            }

            if (_cgUnlockManager != null)
            {
                _cgUnlockManager.Dispose();
                _cgUnlockManager = null;
            }

            _cgSceneConfig = null;
        }

        // ===== 公開查詢（供 VillageEntryPoint 取得 CG 實例） =====

        /// <summary>CG 場景配置（Install 後可用；Uninstall 後回 null）。</summary>
        public CGSceneConfig CgSceneConfig => _cgSceneConfig;

        /// <summary>CG 解鎖管理器（Install 後可用；Uninstall 後回 null）。</summary>
        public CGUnlockManager CgUnlockManager => _cgUnlockManager;

        // ===== Event Handlers =====

        private void OnCGUnlocked(CGUnlockedEvent e)
        {
            // 目前階段（IT）：CGUnlockedEvent 已由 CGUnlockManager 發布，
            // 此 handler 預留給未來 CGGalleryView 的「新解鎖 CG 紅點」邏輯使用。
            // VS 階段接入 CGGalleryView 後，在此呼叫 Gallery 更新。
        }
    }
}
