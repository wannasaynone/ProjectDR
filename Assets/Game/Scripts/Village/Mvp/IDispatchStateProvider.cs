// IDispatchStateProvider — 提供角色派遣狀態給 DialogueCooldownManager。
// Sprint 4 MVP 不實作派遣系統，使用 NoDispatchProvider 永遠回傳 false。
// Sprint 5 派遣系統實作時，由派遣模組實作此介面。

namespace ProjectDR.Village.Mvp
{
    /// <summary>
    /// 提供「指定角色目前是否派遣中」的查詢介面。
    /// DialogueCooldownManager 根據查詢結果套用派遣冷卻倍率。
    /// </summary>
    public interface IDispatchStateProvider
    {
        /// <summary>指定角色是否派遣中。</summary>
        /// <param name="characterId">角色 ID。</param>
        /// <returns>派遣中回傳 true，否則 false。</returns>
        bool IsDispatched(string characterId);
    }

    /// <summary>
    /// Sprint 4 MVP 版本派遣狀態提供者：永遠回傳 false（無派遣系統）。
    /// </summary>
    public class NoDispatchProvider : IDispatchStateProvider
    {
        public bool IsDispatched(string characterId) => false;
    }
}
