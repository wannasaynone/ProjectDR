// CharacterQuestionCountdownManager — 角色發問倒數管理器（Sprint 5 B1）。
//
// 職責（依 character-interaction.md v2.3 §5.1、character-content-template.md v1.4 §3.2）：
// - 每角色獨立倒數（60s placeholder，由 VillageEntryPoint 從 config 傳入）
// - 倒數到時發布 CharacterQuestionCountdownReadyEvent，L2 紅點由此觸發
// - 紅點累積上限 1（規則層）：Ready 狀態下再次到期不重複發事件
// - 工作中暫停倒數（SetWorking(charId, true)，由 CommissionManager 狀態變更聯動）
// - ClearReady 由 CharacterQuestionsManager 在玩家觸發角色發問時呼叫
//
// 純邏輯類別，不依賴 MonoBehaviour。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// 角色發問倒數管理器。
    /// 每次 StartCountdown 開啟指定角色的倒數，Tick 推進時間，
    /// 到期時發布 CharacterQuestionCountdownReadyEvent。
    /// </summary>
    public class CharacterQuestionCountdownManager : IDisposable
    {
        private readonly float _durationSeconds;
        private readonly Dictionary<string, CountdownState> _states;
        private bool _disposed;

        /// <summary>
        /// 建構倒數管理器。
        /// </summary>
        /// <param name="durationSeconds">每角色倒數秒數（必須 &gt; 0）。</param>
        /// <exception cref="ArgumentOutOfRangeException">durationSeconds &lt;= 0 時。</exception>
        public CharacterQuestionCountdownManager(float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationSeconds),
                    "durationSeconds must be positive.");
            }
            _durationSeconds = durationSeconds;
            _states = new Dictionary<string, CountdownState>();
        }

        /// <summary>
        /// 開始該角色的倒數。
        /// - 若該角色已處於 Blocked 狀態（封鎖中），此次呼叫被忽略。
        /// - 若該角色已處於 Ready 狀態（紅點亮），此次呼叫被忽略（上限 1）。
        /// - 若該角色目前正在倒數，重設為 0 重新計時。
        /// - 若該角色目前工作中，僅記錄「待恢復後倒數」，實際推進需 SetWorking(false)。
        /// </summary>
        public void StartCountdown(string characterId)
        {
            if (_disposed) return;
            if (string.IsNullOrEmpty(characterId)) return;

            CountdownState state = GetOrCreate(characterId);
            if (state.Blocked)
            {
                // 封鎖中：不啟動倒數（守衛「要拿劍」完成前的保護機制）
                return;
            }
            if (state.Ready)
            {
                // 紅點累積上限 1：已 Ready 則忽略
                return;
            }
            state.Active = true;
            state.Elapsed = 0f;
        }

        /// <summary>推進時間。正值才納入計算；已 Ready 的角色跳過。</summary>
        public void Tick(float deltaSeconds)
        {
            if (_disposed) return;
            if (deltaSeconds <= 0f) return;

            foreach (KeyValuePair<string, CountdownState> kvp in _states)
            {
                CountdownState state = kvp.Value;
                if (!state.Active) continue;
                if (state.Ready) continue;
                if (state.Working) continue;

                state.Elapsed += deltaSeconds;
                if (state.Elapsed >= _durationSeconds)
                {
                    state.Ready = true;
                    state.Active = false;
                    state.Elapsed = 0f;
                    EventBus.Publish(new CharacterQuestionCountdownReadyEvent
                    {
                        CharacterId = kvp.Key,
                    });
                }
            }
        }

        /// <summary>清除 Ready 狀態（玩家觸發角色發問後呼叫）。</summary>
        public void ClearReady(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            if (_states.TryGetValue(characterId, out CountdownState state))
            {
                state.Ready = false;
                state.Elapsed = 0f;
                state.Active = false;
            }
        }

        /// <summary>查詢該角色是否處於 Ready 狀態（紅點亮）。</summary>
        public bool IsReady(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return false;
            return _states.TryGetValue(characterId, out CountdownState state) && state.Ready;
        }

        /// <summary>查詢該角色是否正在倒數中。</summary>
        public bool IsCountingDown(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return false;
            return _states.TryGetValue(characterId, out CountdownState state)
                && state.Active && !state.Ready && !state.Working;
        }

        /// <summary>
        /// 設定該角色工作中狀態。
        /// - true：暫停倒數（Tick 不扣時間）
        /// - false：恢復倒數（若之前正在倒數，繼續從暫停點推進）
        /// </summary>
        public void SetWorking(string characterId, bool working)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            CountdownState state = GetOrCreate(characterId);
            state.Working = working;
        }

        /// <summary>
        /// 封鎖指定角色的倒數啟動。
        /// 封鎖後呼叫 StartCountdown 無效；若該角色正在倒數，立刻停止。
        /// 用途：守衛「要拿劍」完成前，不啟動 L2 角色發問倒數。
        /// </summary>
        public void BlockCountdown(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            CountdownState state = GetOrCreate(characterId);
            state.Blocked = true;
            // 若已在倒數中，立刻停止
            state.Active = false;
            state.Elapsed = 0f;
        }

        /// <summary>
        /// 解除指定角色的倒數封鎖。
        /// 解封後需再次呼叫 StartCountdown 才會開始計時。
        /// </summary>
        public void UnblockCountdown(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            if (!_states.TryGetValue(characterId, out CountdownState state)) return;
            state.Blocked = false;
        }

        /// <summary>查詢該角色是否處於封鎖狀態。</summary>
        public bool IsBlocked(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return false;
            return _states.TryGetValue(characterId, out CountdownState state) && state.Blocked;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _states.Clear();
        }

        private CountdownState GetOrCreate(string characterId)
        {
            if (!_states.TryGetValue(characterId, out CountdownState state))
            {
                state = new CountdownState();
                _states[characterId] = state;
            }
            return state;
        }

        /// <summary>單一角色的倒數狀態（mutable 內部類別）。</summary>
        private class CountdownState
        {
            /// <summary>是否正在倒數（未 Ready 且未工作中才有效）。</summary>
            public bool Active;

            /// <summary>是否已到期（紅點亮，等玩家觸發清除）。</summary>
            public bool Ready;

            /// <summary>工作中（暫停 Tick 扣時）。</summary>
            public bool Working;

            /// <summary>已累積秒數。</summary>
            public float Elapsed;

            /// <summary>
            /// 封鎖中（禁止啟動倒數）。
            /// Sprint 6 F8：守衛「要拿劍」完成前設定為 true，
            /// ExplorationGateReopenedEvent 後設定為 false 再呼叫 StartCountdown。
            /// </summary>
            public bool Blocked;
        }
    }
}
