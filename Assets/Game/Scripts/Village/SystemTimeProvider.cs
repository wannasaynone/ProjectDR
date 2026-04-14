// SystemTimeProvider — 系統時間提供者。
// 以 DateTimeOffset.UtcNow 實作 ITimeProvider，回傳真實 UTC 秒數時間戳記。

using System;

namespace ProjectDR.Village
{
    /// <summary>
    /// 系統時間提供者。
    /// 使用 DateTimeOffset.UtcNow 取得當前 UTC 時間，
    /// 支援離線計時（與 Unity 的 Time.time 無關）。
    /// </summary>
    public class SystemTimeProvider : ITimeProvider
    {
        /// <summary>取得當前 UTC 時間戳記（秒）。</summary>
        public long GetCurrentTimestampUtc()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
