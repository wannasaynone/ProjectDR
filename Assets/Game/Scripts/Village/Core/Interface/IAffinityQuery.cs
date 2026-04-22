namespace ProjectDR.Village.Core
{
    /// <summary>
    /// 好感度唯讀查詢介面。
    /// 由 AffinityInstaller 在 Install 時填入 VillageContext.AffinityReadOnly，
    /// 供其他 Installer（CG、DialogueFlow）查詢好感度狀態，不可修改。
    /// 若要修改好感度，透過 EventBus 發送請求事件由 AffinityInstaller 處理。
    /// </summary>
    public interface IAffinityQuery
    {
        /// <summary>取得指定角色的當前好感度值。</summary>
        /// <param name="characterId">角色 ID（不可為 null 或空字串）。</param>
        /// <returns>當前好感度整數值。</returns>
        int GetLevel(string characterId);

        /// <summary>
        /// 判斷指定角色是否已達到指定好感度門檻。
        /// </summary>
        /// <param name="characterId">角色 ID。</param>
        /// <param name="thresholdValue">要確認的門檻值。</param>
        /// <returns>若已達到該門檻回傳 true。</returns>
        bool IsThresholdReached(string characterId, int thresholdValue);
    }
}
