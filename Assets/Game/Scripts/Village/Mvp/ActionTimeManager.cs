// ActionTimeManager — 行動冷卻計時管理器。
// 以 actionKey (string) 為索引，每個 key 擁有獨立的冷卻計時器。
// 寒冷狀態時所有冷卻套用 config.ColdActionCooldownMultiplier 倍率。
// 推進方式：EntryPoint 每幀呼叫 Tick(deltaSeconds)。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village.Mvp
{
    /// <summary>
    /// 行動冷卻計時管理器。
    /// TryStartCooldown(key, baseSeconds) 啟動一個基於 base 秒數的冷卻，
    /// 寒冷狀態時自動套用倍率，若目前仍在冷卻中則拒絕啟動。
    /// </summary>
    public class ActionTimeManager
    {
        private readonly ColdStatusSystem _coldStatus;
        private readonly float _coldMultiplier;

        private readonly Dictionary<string, float> _remaining = new Dictionary<string, float>();

        /// <summary>
        /// 建構行動冷卻管理器。
        /// </summary>
        /// <param name="coldStatus">寒冷狀態系統（不可為 null）。</param>
        /// <param name="coldMultiplier">寒冷時冷卻倍率（必須 &gt;= 1）。</param>
        /// <exception cref="ArgumentNullException">coldStatus 為 null 時拋出。</exception>
        /// <exception cref="ArgumentException">coldMultiplier &lt; 1 時拋出。</exception>
        public ActionTimeManager(ColdStatusSystem coldStatus, float coldMultiplier)
        {
            _coldStatus = coldStatus ?? throw new ArgumentNullException(nameof(coldStatus));
            if (coldMultiplier < 1f)
            {
                throw new ArgumentException("coldMultiplier 必須 >= 1。", nameof(coldMultiplier));
            }
            _coldMultiplier = coldMultiplier;
        }

        /// <summary>指定 key 是否正在冷卻中。</summary>
        public bool IsOnCooldown(string actionKey)
        {
            ValidateKey(actionKey);
            return _remaining.TryGetValue(actionKey, out float v) && v > 0f;
        }

        /// <summary>取得指定 key 的剩餘冷卻秒數（含寒冷倍率後的實時剩餘）。</summary>
        public float GetRemaining(string actionKey)
        {
            ValidateKey(actionKey);
            if (_remaining.TryGetValue(actionKey, out float v))
            {
                return v > 0f ? v : 0f;
            }
            return 0f;
        }

        /// <summary>
        /// 嘗試啟動一個冷卻：若當前仍在冷卻中回傳 false；否則寫入 (baseSeconds × 寒冷倍率) 剩餘時間並回傳 true。
        /// </summary>
        /// <exception cref="ArgumentNullException">actionKey 為 null 時拋出。</exception>
        /// <exception cref="ArgumentException">actionKey 空字串或 baseSeconds &lt;= 0 時拋出。</exception>
        public bool TryStartCooldown(string actionKey, float baseSeconds)
        {
            ValidateKey(actionKey);
            if (baseSeconds <= 0f)
            {
                throw new ArgumentException("baseSeconds 必須大於 0。", nameof(baseSeconds));
            }

            if (IsOnCooldown(actionKey))
            {
                return false;
            }

            float effective = baseSeconds * (_coldStatus.IsCold ? _coldMultiplier : 1f);
            _remaining[actionKey] = effective;
            return true;
        }

        /// <summary>推進所有冷卻計時，歸零後停留在 0。</summary>
        /// <exception cref="ArgumentException">deltaSeconds &lt; 0 時拋出。</exception>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentException("deltaSeconds 不可為負。", nameof(deltaSeconds));
            }
            if (deltaSeconds == 0f) return;

            // 建立 keys 快照以避免 foreach 時修改
            List<string> keys = new List<string>(_remaining.Keys);
            foreach (string k in keys)
            {
                float v = _remaining[k];
                if (v <= 0f) continue;
                float next = v - deltaSeconds;
                if (next < 0f) next = 0f;
                _remaining[k] = next;
            }
        }

        private static void ValidateKey(string actionKey)
        {
            if (actionKey == null) throw new ArgumentNullException(nameof(actionKey));
            if (actionKey.Length == 0) throw new ArgumentException("actionKey 不可為空字串。", nameof(actionKey));
        }
    }
}
