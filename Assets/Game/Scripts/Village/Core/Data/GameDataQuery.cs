namespace ProjectDR.Village.Core
{
    /// <summary>
    /// IGameData 查詢委派。VillageContext 透過此委派取得 tabular data，
    /// 不直接依賴 GameStaticDataManager 靜態類別，方便測試替換。
    /// 實際注入值：<c>id => _gameStaticDataManager.GetGameData&lt;T&gt;(id)</c>。
    /// </summary>
    /// <typeparam name="T">實作 IGameData 的資料型別。</typeparam>
    /// <param name="id">資料的 int 主鍵（IGameData.ID）。</param>
    /// <returns>對應 ID 的資料物件；若找不到回傳 null。</returns>
    public delegate T GameDataQuery<T>(int id) where T : class, KahaGameCore.GameData.IGameData;
}
