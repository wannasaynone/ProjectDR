// NPCInitiativeManager — 角色主動發話管理器。
// 每個註冊的角色擁有獨立倒數計時器，時間到 → IsReady=true → 發布 MvpNpcInitiativeReadyEvent。
// 對話發生時呼叫 ConsumeInitiative(charId) 重設計時。
// 訂閱 MvpNpcArrivedEvent 自動註冊新加入 NPC。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Mvp
{
    /// <summary>
    /// 角色主動發話管理器。
    /// 每角色獨立計時，到時 → 發布 Ready 事件（紅點亮起）。
    /// </summary>
    public class NPCInitiativeManager : IDisposable
    {
        private readonly MvpConfig _config;

        // characterId -> (remainingSeconds, isReady)
        private readonly Dictionary<string, float> _remaining = new Dictionary<string, float>();
        private readonly Dictionary<string, bool> _ready = new Dictionary<string, bool>();

        private bool _subscribed;

        public NPCInitiativeManager(MvpConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            EventBus.Subscribe<MvpNpcArrivedEvent>(OnNpcArrived);
            _subscribed = true;
        }

        /// <summary>註冊角色（開始計時主動發話）。</summary>
        public void RegisterCharacter(string characterId)
        {
            ValidateId(characterId);
            if (!_remaining.ContainsKey(characterId))
            {
                _remaining[characterId] = _config.NpcInitiativeIntervalSeconds;
                _ready[characterId] = false;
            }
        }

        /// <summary>已註冊的角色清單。</summary>
        public IEnumerable<string> RegisteredCharacterIds => _remaining.Keys;

        /// <summary>指定角色是否紅點亮起（主動發話準備就緒）。</summary>
        public bool IsReady(string characterId)
        {
            ValidateId(characterId);
            return _ready.TryGetValue(characterId, out bool v) && v;
        }

        /// <summary>
        /// 消費指定角色的主動發話（對話發生時呼叫），重置計時。
        /// </summary>
        public void ConsumeInitiative(string characterId)
        {
            ValidateId(characterId);
            if (!_remaining.ContainsKey(characterId)) return;

            bool wasReady = _ready.TryGetValue(characterId, out bool v) && v;
            _ready[characterId] = false;
            _remaining[characterId] = _config.NpcInitiativeIntervalSeconds;

            if (wasReady)
            {
                EventBus.Publish(new MvpNpcInitiativeConsumedEvent { CharacterId = characterId });
            }
        }

        /// <summary>推進所有計時；歸零時切換至 Ready 並發布事件。</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f) throw new ArgumentException("deltaSeconds 不可為負。", nameof(deltaSeconds));
            if (deltaSeconds == 0f) return;

            List<string> keys = new List<string>(_remaining.Keys);
            foreach (string k in keys)
            {
                bool isReady = _ready.TryGetValue(k, out bool rd) && rd;
                if (isReady) continue; // 已 Ready 不再倒數，等待 Consume

                float v = _remaining[k];
                float next = v - deltaSeconds;
                if (next <= 0f)
                {
                    next = 0f;
                    _ready[k] = true;
                    EventBus.Publish(new MvpNpcInitiativeReadyEvent { CharacterId = k });
                }
                _remaining[k] = next;
            }
        }

        private void OnNpcArrived(MvpNpcArrivedEvent e)
        {
            RegisterCharacter(e.CharacterId);
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                EventBus.Unsubscribe<MvpNpcArrivedEvent>(OnNpcArrived);
                _subscribed = false;
            }
        }

        private static void ValidateId(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (id.Length == 0) throw new ArgumentException("characterId 不可為空字串。", nameof(id));
        }
    }
}
