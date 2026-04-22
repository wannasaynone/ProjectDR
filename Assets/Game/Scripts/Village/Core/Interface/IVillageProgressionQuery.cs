namespace ProjectDR.Village.Core
{
    /// <summary>
    /// 村莊進度唯讀查詢介面。
    /// 由 ProgressionInstaller 在 Install 時填入 VillageContext.VillageProgressionReadOnly，
    /// 供其他 Installer 查詢角色解鎖狀態、任務進度，不可修改。
    /// </summary>
    public interface IVillageProgressionQuery
    {
        /// <summary>判斷指定角色是否已解鎖（可進入互動）。</summary>
        /// <param name="characterId">角色 ID。</param>
        /// <returns>若角色已解鎖回傳 true。</returns>
        bool IsCharacterUnlocked(string characterId);

        /// <summary>判斷探索功能是否已解鎖。</summary>
        /// <returns>若探索已解鎖回傳 true。</returns>
        bool IsExplorationUnlocked();
    }
}
