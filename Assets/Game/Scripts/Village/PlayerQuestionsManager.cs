// PlayerQuestionsManager — 玩家發問純邏輯（Sprint 5 B11）。
//
// 職責（依 character-content-template.md v1.4 §3.3、character-interaction.md v2.3 §5.2）：
// - 每次開啟發問：依剩餘題目規則決定顯示清單
//   - ≥4 題未看 → 隨機抽 4 題
//   - 1~3 題 → 只顯示該 1~3 題（不補滿）
//   - 0 題 → 只顯示 [閒聊] 單一虛擬項
// - 玩家選題後 → 標記為已看（不影響好感度、純情報收集）
// - seen 狀態為 session 內記憶體（不持久化，未來改存檔再整合）
//
// Sprint 6 擴張（B5/C11）：
// - 支援 is_single_use 特殊題（問後從清單永久消失）
// - TriggerSingleUseQuestion：觸發特殊題 flag handler，
//   發布 PlayerSpecialQuestionTriggeredEvent，並將該特殊題加入 _consumedSingleUseIds 永久排除
//
// Sprint 6 決策 6-13（Revert 部分）：
// - guard_ask_sword 特殊題已從 player-questions-config.json 移除
// - 守衛取劍改為「首次進入自動對白觸發」（由 VillageEntryPoint 處理）
// - TriggerFlagGrantGuardSword 常數標記 [Obsolete]，未來可完全移除
// - TriggerSingleUseQuestion 中的 grant_guard_sword 分支已移除，改為泛型 dispatcher 調用

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// 依剩餘題目規則回傳「本次對話要呈現的題目候選清單」。
    /// IsIdleChatFallback = true 表示應呼叫 IdleChatPresenter.Trigger。
    /// </summary>
    public class PlayerQuestionsPresentation
    {
        public bool IsIdleChatFallback { get; }
        public IReadOnlyList<PlayerQuestionInfo> Questions { get; }

        public PlayerQuestionsPresentation(bool isIdleChat, IReadOnlyList<PlayerQuestionInfo> questions)
        {
            IsIdleChatFallback = isIdleChat;
            Questions = questions ?? Array.AsReadOnly(Array.Empty<PlayerQuestionInfo>());
        }
    }

    public class PlayerQuestionsManager
    {
        private const int DisplayLimit = 4;

        /// <summary>
        /// grant_guard_sword trigger flag 常數。
        /// Sprint 6 決策 6-13：守衛取劍改為「首次進入自動對白觸發」，此常數已無使用點。
        /// 保留供歷史引用，未來可完全移除。
        /// </summary>
        [System.Obsolete("Sprint 6 決策 6-13：守衛取劍改為首次進入自動對白觸發，此常數已無使用點。")]
        public const string TriggerFlagGrantGuardSword = "grant_guard_sword";

        private readonly PlayerQuestionsConfig _config;
        private readonly Random _random;

        // 每角色已看過題目 session 記憶體集合。Key = characterId。
        private readonly Dictionary<string, HashSet<string>> _seenByCharacter;

        // 已觸發的單次特殊題 ID 集合（session 內永久排除）。
        private readonly HashSet<string> _consumedSingleUseIds;

        public PlayerQuestionsManager(PlayerQuestionsConfig config) : this(config, null) { }

        public PlayerQuestionsManager(PlayerQuestionsConfig config, int? seed)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            _seenByCharacter = new Dictionary<string, HashSet<string>>();
            _consumedSingleUseIds = new HashSet<string>();
        }

        /// <summary>
        /// 取得本次對話應呈現的題目清單。
        /// - 剩餘 ≥ 4 題 → 隨機 4 題
        /// - 1~3 題 → 全部顯示
        /// - 0 題 → IsIdleChatFallback = true
        /// </summary>
        public PlayerQuestionsPresentation GetPresentation(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return new PlayerQuestionsPresentation(false, Array.AsReadOnly(Array.Empty<PlayerQuestionInfo>()));
            }

            IReadOnlyList<PlayerQuestionInfo> all = _config.GetQuestionsForCharacter(characterId);
            HashSet<string> seen = GetOrCreateSeen(characterId);

            List<PlayerQuestionInfo> unseen = new List<PlayerQuestionInfo>();
            foreach (PlayerQuestionInfo q in all)
            {
                // 已消耗的單次特殊題永久從清單排除
                if (_consumedSingleUseIds.Contains(q.QuestionId)) continue;
                if (!seen.Contains(q.QuestionId)) unseen.Add(q);
            }

            if (unseen.Count == 0)
            {
                return new PlayerQuestionsPresentation(true, Array.AsReadOnly(Array.Empty<PlayerQuestionInfo>()));
            }

            if (unseen.Count <= 3)
            {
                return new PlayerQuestionsPresentation(false, unseen.AsReadOnly());
            }

            // ≥ 4 題 → 洗牌取前 4
            Shuffle(unseen);
            List<PlayerQuestionInfo> top4 = unseen.GetRange(0, DisplayLimit);
            return new PlayerQuestionsPresentation(false, top4.AsReadOnly());
        }

        /// <summary>玩家選擇指定題目後呼叫，標記為已看。</summary>
        public void MarkSeen(string characterId, string questionId)
        {
            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(questionId)) return;
            GetOrCreateSeen(characterId).Add(questionId);
        }

        /// <summary>
        /// 觸發指定角色的單次特殊題（依 triggerFlag 查找 grant 並派發）。
        /// 若找不到對應特殊題或已被消耗，則無效。
        ///
        /// Sprint 6 決策 6-13（重構）：
        /// 移除 grant_guard_sword 特殊分支，改為泛用 table-lookup（透過 triggerFlag 對應 trigger_id 查 grant）。
        /// 發布 PlayerSpecialQuestionTriggeredEvent，不再發布 ExplorationGateReopenedEvent
        /// （守衛取劍的探索重開改由 VillageEntryPoint 的首次進入對白完成後發布）。
        /// </summary>
        /// <param name="characterId">發問角色的 ID。</param>
        /// <param name="triggerFlag">特殊題的觸發旗標（對應 trigger_id 查找 grant）。</param>
        /// <param name="dispatcher">用於派發 grant 的資源派發器。</param>
        /// <param name="resourcesConfig">初始資源配置，用於查找對應的 grant。</param>
        public void TriggerSingleUseQuestion(
            string characterId,
            string triggerFlag,
            IInitialResourceDispatcher dispatcher,
            InitialResourcesConfig resourcesConfig)
        {
            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(triggerFlag)) return;
            if (dispatcher == null || resourcesConfig == null) return;

            // 查找符合的特殊題
            string consumedQuestionId = null;
            IReadOnlyList<PlayerQuestionInfo> all = _config.GetQuestionsForCharacter(characterId);
            foreach (PlayerQuestionInfo q in all)
            {
                if (q.IsSingleUse && q.TriggerFlag == triggerFlag
                    && !_consumedSingleUseIds.Contains(q.QuestionId))
                {
                    consumedQuestionId = q.QuestionId;
                    break;
                }
            }

            if (consumedQuestionId == null) return;

            // 永久消耗此特殊題（從清單移除）
            _consumedSingleUseIds.Add(consumedQuestionId);

            // 泛用 grant 派發：以 triggerFlag 作為 trigger_id 查 grant 並派發
            System.Collections.Generic.IReadOnlyList<InitialResourceGrant> grants =
                resourcesConfig.GetGrantsByTrigger(triggerFlag);
            foreach (InitialResourceGrant grant in grants)
            {
                dispatcher.Dispatch(grant);
            }

            // 發布特殊題觸發事件（通知上層）
            EventBus.Publish(new PlayerSpecialQuestionTriggeredEvent
            {
                CharacterId = characterId,
                QuestionId = consumedQuestionId,
                TriggerFlag = triggerFlag
            });
        }

        /// <summary>該角色未看過的題目數（已消耗的單次特殊題不計入）。</summary>
        public int GetUnseenCount(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return 0;
            IReadOnlyList<PlayerQuestionInfo> all = _config.GetQuestionsForCharacter(characterId);
            HashSet<string> seen = GetOrCreateSeen(characterId);
            int n = 0;
            foreach (PlayerQuestionInfo q in all)
            {
                if (_consumedSingleUseIds.Contains(q.QuestionId)) continue;
                if (!seen.Contains(q.QuestionId)) n++;
            }
            return n;
        }

        /// <summary>是否已到閒聊階段（未看題目 = 0）。</summary>
        public bool IsIdleChatMode(string characterId) => GetUnseenCount(characterId) == 0;

        private HashSet<string> GetOrCreateSeen(string characterId)
        {
            if (!_seenByCharacter.TryGetValue(characterId, out HashSet<string> seen))
            {
                seen = new HashSet<string>();
                _seenByCharacter[characterId] = seen;
            }
            return seen;
        }

        private void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
