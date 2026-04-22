namespace ProjectDR.Village.Core
{
    /// <summary>
    /// 倉庫唯讀查詢介面。
    /// 由 CoreStorageInstaller 在 Install 時填入 VillageContext.StorageReadOnly，
    /// 供其他 Installer 查詢倉庫庫存，不可修改庫存（修改透過 StorageManager 直接調用，
    /// 或在同一 Installer 內持有 StorageManager 引用）。
    /// </summary>
    public interface IStorageQuery
    {
        /// <summary>取得倉庫中指定物品的數量。</summary>
        /// <param name="itemId">物品 ID。</param>
        /// <returns>庫存數量（若無則回傳 0）。</returns>
        int GetItemCount(string itemId);

        /// <summary>判斷倉庫中是否有足夠數量的指定物品。</summary>
        /// <param name="itemId">物品 ID。</param>
        /// <param name="requiredAmount">需要的數量。</param>
        /// <returns>若庫存足夠回傳 true。</returns>
        bool HasItem(string itemId, int requiredAmount = 1);
    }
}
