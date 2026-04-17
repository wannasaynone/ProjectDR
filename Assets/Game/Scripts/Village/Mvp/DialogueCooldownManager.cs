// DialogueCooldownManager — 玩家主動對話冷卻管理器。
// 玩家對某角色發起對話時啟動冷卻 M 秒；派遣中則 × dispatchMultiplier。
// 每角色獨立冷卻計時。Tick 推進所有冷卻。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Mvp
{
    /// <summary>
    /// 玩家主動對話冷卻管理器。
    /// 每角色獨立冷卻計時，派遣中套用倍率。
    /// </summary>
    public class DialogueCooldownManager
    {
        private readonly MvpConfig _config;
        private readonly IDispatchStateProvider _dispatchProvider;

        private readonly Dictionary<string, float> _remaining = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _total = new Dictionary<string, float>();

        public DialogueCooldownManager(MvpConfig config, IDispatchStateProvider dispatchProvider)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _dispatchProvider = dispatchProvider ?? throw new ArgumentNullException(nameof(dispatchProvider));
        }

        /// <summary>指定角色是否仍在冷卻中。</summary>
        public bool IsOnCooldown(string characterId)
        {
            ValidateId(characterId);
            return _remaining.TryGetValue(characterId, out float v) && v > 0f;
        }

        /// <summary>取得剩餘秒數（非冷卻中回傳 0）。</summary>
        public float GetRemaining(string characterId)
        {
            ValidateId(characterId);
            if (_remaining.TryGetValue(characterId, out float v) && v > 0f) return v;
            return 0f;
        }

        /// <summary>取得最近一次啟動時的總秒數（用於 UI 進度條）。</summary>
        public float GetTotal(string characterId)
        {
            ValidateId(characterId);
            if (_total.TryGetValue(characterId, out float v)) return v;
            return 0f;
        }

        /// <summary>
        /// 玩家嘗試對某角色啟動對話冷卻。
        /// 若仍在冷卻中回傳 false；否則根據派遣狀態套用倍率並回傳 true。
        /// </summary>
        public bool TryStartPlayerDialogueCooldown(string characterId)
        {
            ValidateId(characterId);
            if (IsOnCooldown(characterId)) return false;

            float baseSec = _config.PlayerDialogueCooldownSeconds;
            float effective = baseSec * (_dispatchProvider.IsDispatched(characterId)
                ? _config.DispatchCooldownMultiplier
                : 1f);

            _remaining[characterId] = effective;
            _total[characterId] = effective;

            EventBus.Publish(new MvpPlayerDialogueCooldownChangedEvent
            {
                RemainingSeconds = effective,
                TotalSeconds = effective
            });
            return true;
        }

        /// <summary>推進所有冷卻計時。</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f) throw new ArgumentException("deltaSeconds 不可為負。", nameof(deltaSeconds));
            if (deltaSeconds == 0f) return;

            List<string> keys = new List<string>(_remaining.Keys);
            foreach (string k in keys)
            {
                float v = _remaining[k];
                if (v <= 0f) continue;
                float next = v - deltaSeconds;
                if (next < 0f) next = 0f;
                _remaining[k] = next;

                EventBus.Publish(new MvpPlayerDialogueCooldownChangedEvent
                {
                    RemainingSeconds = next,
                    TotalSeconds = _total.TryGetValue(k, out float t) ? t : 0f
                });
            }
        }

        private static void ValidateId(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (id.Length == 0) throw new ArgumentException("characterId 不可為空字串。", nameof(id));
        }
    }
}
