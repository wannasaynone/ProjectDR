using UnityEngine;
using ProjectDR.Village.Shared;
using ProjectDR.Village.TimeProvider;

namespace ProjectDR.Village.Core
{
    /// <summary>
    /// Village 場景跨 Installer 共用服務容器（建構器注入資料容器）。
    /// 定位：明示欄位的建構器注入，不是 Service Locator。
    ///
    /// 使用規則：
    /// - Canvas / UIContainer / GameDataAccess 由 VillageEntryPoint 在建構時透過 constructor 參數傳入。
    /// - 其他欄位由對應 Installer 在 Install() 內透過 internal set 填入。
    /// - 禁止暴露 Resolve&lt;T&gt;() / Get&lt;T&gt;() 泛型查找 API（Service Locator 反模式）。
    /// - 欄位數硬指標 ≤ 10；超過必須回改 ADR-003。
    /// </summary>
    public class VillageContext
    {
        // ===== 1. UI 根節點（由 VillageEntryPoint constructor 傳入） =====

        /// <summary>Village 場景的 UGUI Canvas 根節點。供需要 spawn UI 的 Installer 使用（CG / DialogueFlow）。</summary>
        public Canvas Canvas { get; }

        /// <summary>UI 物件容器，供動態 Instantiate UI Prefab 時指定父節點。</summary>
        public Transform UIContainer { get; }

        // ===== 2. View Stack（由 CoreStorageInstaller Install 後填入） =====

        /// <summary>
        /// View 堆疊控制器（由 CoreStorageInstaller 建立後填入）。
        /// 所有 Installer 透過此控制器 Push / Pop View。
        /// </summary>
        public ViewStackController ViewStackController { get; internal set; }

        // ===== 3. 時間提供者（由 CoreStorageInstaller Install 後填入） =====

        /// <summary>
        /// 時間提供者（由 CoreStorageInstaller 建立後填入）。
        /// Farm / Commission / Countdown 等時間相依 Installer 使用。
        /// </summary>
        public ITimeProvider TimeProvider { get; internal set; }

        // ===== 4. Game Data 查詢委派（由 VillageEntryPoint 建 ctx 時就位） =====

        /// <summary>
        /// IGameData 查詢委派（由 VillageEntryPoint 在建構 ctx 時注入）。
        /// 指向 GameStaticDataManager.GetGameData&lt;T&gt;(id)，
        /// 但透過委派隔離靜態依賴，方便測試替換。
        /// </summary>
        public GameDataQuery<KahaGameCore.GameData.IGameData> GameDataAccess { get; }

        // ===== 5. 唯讀查詢介面（由對應 Installer Install 後填入） =====

        /// <summary>
        /// 村莊進度唯讀查詢（由 ProgressionInstaller Install 後填入）。
        /// 供需要查詢角色解鎖狀態的 Installer 使用。
        /// </summary>
        public IVillageProgressionQuery VillageProgressionReadOnly { get; internal set; }

        /// <summary>
        /// 好感度唯讀查詢（由 AffinityInstaller Install 後填入）。
        /// 供 CG、DialogueFlow 等 Installer 查詢好感度狀態。
        /// </summary>
        public IAffinityQuery AffinityReadOnly { get; internal set; }

        // ===== 6. 倉庫 / 背包唯讀查詢（由 CoreStorageInstaller Install 後填入） =====

        /// <summary>倉庫唯讀查詢（由 CoreStorageInstaller Install 後填入）。</summary>
        public IStorageQuery StorageReadOnly { get; internal set; }

        /// <summary>背包唯讀查詢（由 CoreStorageInstaller Install 後填入）。</summary>
        public IBackpackQuery BackpackReadOnly { get; internal set; }

        // 目前 9 欄（含 2 UI + 2 Core 服務 + 1 委派 + 4 唯讀查詢）<= 10
        // 若需第 10 欄，仍在硬指標內；超過 10 欄必須回改 ADR-003

        /// <summary>
        /// 建構 VillageContext，注入根級服務。
        /// Canvas、UIContainer 為必要欄位，由 VillageEntryPoint 傳入。
        /// GameDataAccess 在 Sprint 7 期間允許為 null（所有 Installer 仍透過 constructor 直接注入 ConfigData，尚未使用此委派）。
        /// Sprint 8 ADR-002 退出後，此欄位將成為唯一資料入口，屆時再加回 null check。
        /// 其他欄位由各 Installer 在 Install() 內填入（internal set）。
        /// </summary>
        /// <param name="canvas">UGUI Canvas 根節點（不可為 null）。</param>
        /// <param name="uiContainer">UI 物件容器 Transform（不可為 null）。</param>
        /// <param name="gameDataAccess">IGameData 查詢委派（Sprint 7 期間允許為 null；Sprint 8 完整實作後將轉為必要）。</param>
        /// <exception cref="System.ArgumentNullException">canvas 或 uiContainer 為 null 時拋出。</exception>
        public VillageContext(
            Canvas canvas,
            Transform uiContainer,
            GameDataQuery<KahaGameCore.GameData.IGameData> gameDataAccess)
        {
            Canvas = canvas ?? throw new System.ArgumentNullException(nameof(canvas));
            UIContainer = uiContainer ?? throw new System.ArgumentNullException(nameof(uiContainer));
            // TODO Sprint 8：ADR-002 退出後，此處恢復 null check：
            // GameDataAccess = gameDataAccess ?? throw new System.ArgumentNullException(nameof(gameDataAccess));
            GameDataAccess = gameDataAccess;
        }
    }
}
