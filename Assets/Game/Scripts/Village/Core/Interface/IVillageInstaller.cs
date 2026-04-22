namespace ProjectDR.Village.Core
{
    /// <summary>
    /// Village 場景組裝契約。每個 Installer 負責單一功能域的
    /// Manager 建構、服務註冊、事件訂閱與 Tick 驅動。
    /// 同一 Installer 的 Install / Uninstall 必須對稱。
    /// </summary>
    public interface IVillageInstaller
    {
        /// <summary>
        /// 建構 Manager、對 VillageContext 註冊本 Installer 對外公開的服務、
        /// 訂閱本 Installer 需要處理的事件。
        /// 禁止在 Install 內直接引用其他 Installer 的實例。
        /// </summary>
        void Install(VillageContext ctx);

        /// <summary>
        /// 解除 Install 期間建立的所有事件訂閱、釋放資源、Dispose 管理器。
        /// Install 訂閱幾次，Uninstall 就要解除幾次（對稱）。
        /// </summary>
        void Uninstall();
    }
}
