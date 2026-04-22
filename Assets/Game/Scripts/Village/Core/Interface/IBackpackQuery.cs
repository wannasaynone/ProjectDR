namespace ProjectDR.Village.Core
{
    /// <summary>
    /// 背包唯讀查詢介面。
    /// 由 CoreStorageInstaller 在 Install 時填入 VillageContext.BackpackReadOnly，
    /// 供其他 Installer 查詢背包狀態，不可修改背包內容。
    /// </summary>
    public interface IBackpackQuery
    {
        /// <summary>取得背包中指定物品的數量（跨所有格子加總）。</summary>
        /// <param name="itemId">物品 ID。</param>
        /// <returns>背包中的總數量（若無則回傳 0）。</returns>
        int GetItemCount(string itemId);

        /// <summary>判斷背包中是否有足夠數量的指定物品。</summary>
        /// <param name="itemId">物品 ID。</param>
        /// <param name="requiredAmount">需要的數量。</param>
        /// <returns>若背包中有足夠物品回傳 true。</returns>
        bool HasItem(string itemId, int requiredAmount = 1);

        /// <summary>判斷背包是否已滿（所有格子都已佔用）。</summary>
        /// <returns>若背包已滿回傳 true。</returns>
        bool IsFull();
    }
}
