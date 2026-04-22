namespace ProjectDR.Village.Core
{
    /// <summary>
    /// 選擇性 Tick 介面。需要每幀或每 Update 推進的 Installer 才實作此介面。
    /// VillageEntryPoint.Update() 會對所有實作此介面的 Installer 呼叫 Tick。
    /// </summary>
    public interface IVillageTickable
    {
        /// <summary>
        /// 推進本 Installer 管理的 Tick 類 Manager（例：CommissionManager、CharacterQuestionCountdownManager）。
        /// </summary>
        /// <param name="deltaSeconds">自上一 Update 經過的秒數（來自 Time.unscaledDeltaTime）。</param>
        void Tick(float deltaSeconds);
    }
}
