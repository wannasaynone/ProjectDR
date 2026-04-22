// GreetingPresenter — 招呼語純邏輯（Sprint 5 B16）。
//
// 依 character-interaction.md v2.3 §4.2、§5、character-content-template.md v1.4 §3.1：
// - 進入角色 Normal 狀態時呼叫 TryGreet(charId, level)
// - 若 L1/L4 紅點亮 → 不播招呼語（紅點對話取代）
// - 若 L2/L3 紅點亮 → 仍播招呼語（L2 紅點是按鈕紅點，不取代招呼）
// - 無紅點 → 播招呼語
// - 從該角色該等級招呼語池隨機抽一句
// - 成功播放時發布 GreetingPlayedEvent
//
// 實作重點：
// - 不直接操作 DialogueManager，而是回傳 GreetingInfo 由 View 層決定如何顯示（打字機）
// - RedDotManager 判斷依 L1/L4 啟用與否

using System;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;
using ProjectDR.Village;
using ProjectDR.Village.Progression; // RedDotManager (暫留 ProjectDR.Village，待 E5 搬至 Progression)

namespace ProjectDR.Village.Greeting
{
    public class GreetingPresenter
    {
        private readonly GreetingConfig _config;
        private readonly RedDotManager _redDotManager;
        private readonly System.Random _random;

        public GreetingPresenter(GreetingConfig config, RedDotManager redDotManager)
            : this(config, redDotManager, seed: null) { }

        public GreetingPresenter(GreetingConfig config, RedDotManager redDotManager, int? seed)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _redDotManager = redDotManager; // 可 null → 視為沒有紅點，一律播招呼
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        /// <summary>
        /// 嘗試播放招呼語。
        /// 若 L1/L4 紅點亮或招呼語池為空 → 回 null（不播）。
        /// 否則回 GreetingInfo 並發布 GreetingPlayedEvent。
        /// </summary>
        public GreetingInfo TryGreet(string characterId, int level)
        {
            if (string.IsNullOrEmpty(characterId)) return null;

            // L1/L4 紅點亮 → 由紅點對話取代招呼
            if (ShouldBeSuppressedByRedDot(characterId))
            {
                return null;
            }

            System.Collections.Generic.IReadOnlyList<GreetingInfo> pool =
                _config.GetGreetings(characterId, level);
            if (pool == null || pool.Count == 0) return null;

            GreetingInfo picked = pool[_random.Next(pool.Count)];
            EventBus.Publish(new GreetingPlayedEvent
            {
                CharacterId = characterId,
                Level = level,
                GreetingId = picked.GreetingId,
            });
            return picked;
        }

        /// <summary>檢查該角色是否因 L1/L4 紅點亮而應跳過招呼語。</summary>
        public bool ShouldBeSuppressedByRedDot(string characterId)
        {
            if (_redDotManager == null) return false;
            if (string.IsNullOrEmpty(characterId)) return false;
            bool l1 = _redDotManager.IsLayerActive(characterId, RedDotLayer.CommissionCompleted);
            bool l4 = _redDotManager.IsLayerActive(characterId, RedDotLayer.MainQuestEvent);
            return l1 || l4;
        }
    }
}
