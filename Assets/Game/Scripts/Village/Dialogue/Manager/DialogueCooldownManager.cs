// DialogueCooldownManager — 玩家主動發問冷卻管理器（Sprint 5 B10）。
//
// 職責（依 character-content-template.md v1.4 §3.3）：
// - 每角色獨立 CD
// - 基礎冷卻 60s（placeholder，數值設計師可調）
// - 工作中 ×2 倍率（規則層，不可由數值調整）
// - Tick 推進時間，CD 完成時發布 DialogueCooldownCompletedEvent
// - StartCooldown(charId) 啟動 CD，發布 DialogueCooldownStartedEvent
// - SetWorking(charId, true/false) 切換倍率（true = ×2，false = ×1）
//
// 設計決策：
// - 以「剩餘秒數」追蹤而非「總共累積秒數」，因 Working 狀態下倍率影響扣減速度
// - 工作中會讓 Tick(dt) 等效扣 dt/2 的剩餘秒數（即實際 CD 耗時 ×2）
// - IsOnCooldown(charId) 用於 UI 判斷「對話」按鈕是否可點

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Village.Dialogue
{
    public class DialogueCooldownManager : IDisposable
    {
        private readonly float _baseDurationSeconds;
        private readonly Dictionary<string, CooldownState> _states;
        private bool _disposed;

        public const float WorkingMultiplier = 2f;

        public DialogueCooldownManager(float baseDurationSeconds)
        {
            if (baseDurationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(baseDurationSeconds));
            _baseDurationSeconds = baseDurationSeconds;
            _states = new Dictionary<string, CooldownState>();
        }

        /// <summary>基礎 CD 秒數（60s placeholder）。</summary>
        public float BaseDurationSeconds => _baseDurationSeconds;

        /// <summary>
        /// 啟動該角色的 CD。
        /// 若正在 CD 中，覆寫為新的完整 CD。
        /// 事件中的 DurationSeconds 已套用當前倍率（工作中 = ×2）。
        /// </summary>
        public void StartCooldown(string characterId)
        {
            if (_disposed || string.IsNullOrEmpty(characterId)) return;
            CooldownState state = GetOrCreate(characterId);
            state.Active = true;
            state.Remaining = _baseDurationSeconds;

            EventBus.Publish(new DialogueCooldownStartedEvent
            {
                CharacterId = characterId,
                DurationSeconds = state.Working ? _baseDurationSeconds * WorkingMultiplier : _baseDurationSeconds,
            });
        }

        /// <summary>
        /// 推進時間。
        /// - 工作中（Working = true）：扣 deltaSeconds / WorkingMultiplier 等效讓總 CD 時間 ×2
        /// - 非工作中：扣 deltaSeconds（×1）
        /// 扣至 0 以下 → 發布 Completed 事件並關閉 CD。
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (_disposed || deltaSeconds <= 0f) return;

            // 避免列舉時修改字典：先取 key 清單
            List<string> keys = new List<string>(_states.Keys);
            foreach (string charId in keys)
            {
                CooldownState state = _states[charId];
                if (!state.Active) continue;

                float effectiveDelta = state.Working ? deltaSeconds / WorkingMultiplier : deltaSeconds;
                state.Remaining -= effectiveDelta;
                if (state.Remaining <= 0f)
                {
                    state.Remaining = 0f;
                    state.Active = false;
                    EventBus.Publish(new DialogueCooldownCompletedEvent
                    {
                        CharacterId = charId,
                    });
                }
            }
        }

        /// <summary>是否處於冷卻中（未到期）。</summary>
        public bool IsOnCooldown(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return false;
            return _states.TryGetValue(characterId, out CooldownState state)
                && state.Active && state.Remaining > 0f;
        }

        /// <summary>剩餘秒數（不在 CD 時回 0）。</summary>
        public float GetRemainingSeconds(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return 0f;
            if (!_states.TryGetValue(characterId, out CooldownState state)) return 0f;
            return state.Active ? state.Remaining : 0f;
        }

        /// <summary>
        /// 切換工作中倍率。
        /// - true：Tick 每秒扣 0.5 秒剩餘（CD 總時長 ×2）
        /// - false：Tick 每秒扣 1 秒剩餘（×1）
        /// </summary>
        public void SetWorking(string characterId, bool working)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            CooldownState state = GetOrCreate(characterId);
            state.Working = working;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _states.Clear();
        }

        private CooldownState GetOrCreate(string characterId)
        {
            if (!_states.TryGetValue(characterId, out CooldownState state))
            {
                state = new CooldownState();
                _states[characterId] = state;
            }
            return state;
        }

        private class CooldownState
        {
            public bool Active;
            public float Remaining;
            public bool Working;
        }
    }
}
