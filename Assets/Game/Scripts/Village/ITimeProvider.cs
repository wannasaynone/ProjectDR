// ITimeProvider — 時間提供者介面。
// 抽象化當前時間取得，便於測試中替換為假實作。

namespace ProjectDR.Village
{
    /// <summary>
    /// 時間提供者介面。
    /// 提供 UTC 時間戳記（秒），使 FarmManager 可在測試中控制時間。
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>取得當前 UTC 時間戳記（秒）。</summary>
        long GetCurrentTimestampUtc();
    }
}
