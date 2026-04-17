// NpcArrivalManager — NPC 來訪管理器。
// 訂閱 MvpPopulationCapIncreasedEvent：每次人口上限增加，從 placeholder 角色池中抽一個尚未到訪的角色加入。
// 加入時呼叫 PopulationManager.TryIncrementCount，並發布 MvpNpcArrivedEvent。
// 純邏輯（非 MonoBehaviour），UI 層訂閱事件顯示。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Mvp
{
    /// <summary>
    /// NPC 來訪管理器。人口上限增加時觸發來訪流程，從 placeholder 池中抽出新 NPC。
    /// </summary>
    public class NpcArrivalManager : IDisposable
    {
        private readonly PopulationManager _populationManager;
        private readonly MvpConfig _config;
        private readonly IRandomSource _random;

        private readonly HashSet<string> _arrivedIds = new HashSet<string>();
        private readonly List<MvpPlaceholderCharacterData> _arrivedOrder = new List<MvpPlaceholderCharacterData>();

        private bool _subscribed;

        /// <summary>已到訪角色的唯讀清單（加入順序）。</summary>
        public IReadOnlyList<MvpPlaceholderCharacterData> ArrivedCharacters => _arrivedOrder.AsReadOnly();

        public NpcArrivalManager(
            PopulationManager populationManager,
            MvpConfig config,
            IRandomSource random)
        {
            _populationManager = populationManager ?? throw new ArgumentNullException(nameof(populationManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = random ?? throw new ArgumentNullException(nameof(random));

            EventBus.Subscribe<MvpPopulationCapIncreasedEvent>(OnCapIncreased);
            _subscribed = true;
        }

        /// <summary>
        /// 當人口上限增加時：每上升 1 容量嘗試抽一位新 NPC 加入（直到 Count 達 Cap 或池清空）。
        /// </summary>
        private void OnCapIncreased(MvpPopulationCapIncreasedEvent e)
        {
            for (int i = 0; i < e.Increment; i++)
            {
                if (!TryInviteOne())
                {
                    break;
                }
            }
        }

        private bool TryInviteOne()
        {
            // 收集尚未到訪的 placeholder
            IReadOnlyList<MvpPlaceholderCharacterData> pool = _config.PlaceholderCharacters;
            List<MvpPlaceholderCharacterData> candidates = new List<MvpPlaceholderCharacterData>();
            for (int i = 0; i < pool.Count; i++)
            {
                MvpPlaceholderCharacterData c = pool[i];
                if (c == null || string.IsNullOrEmpty(c.characterId)) continue;
                if (_arrivedIds.Contains(c.characterId)) continue;
                candidates.Add(c);
            }

            if (candidates.Count == 0) return false;

            int idx = _random.Range(0, candidates.Count);
            if (idx < 0) idx = 0;
            if (idx >= candidates.Count) idx = candidates.Count - 1;

            MvpPlaceholderCharacterData picked = candidates[idx];

            if (!_populationManager.TryIncrementCount())
            {
                return false;
            }

            _arrivedIds.Add(picked.characterId);
            _arrivedOrder.Add(picked);

            EventBus.Publish(new MvpNpcArrivedEvent
            {
                CharacterId = picked.characterId,
                DisplayName = picked.displayName
            });
            return true;
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                EventBus.Unsubscribe<MvpPopulationCapIncreasedEvent>(OnCapIncreased);
                _subscribed = false;
            }
        }
    }
}
