// PlayerQuestionsManager — 玩家發問純邏輯（Sprint 5 B11）。
//
// 職責（依 character-content-template.md v1.4 §3.3、character-interaction.md v2.3 §5.2）：
// - 每次開啟發問：依剩餘題目規則決定顯示清單
//   - ≥4 題未看 → 隨機抽 4 題
//   - 1~3 題 → 只顯示該 1~3 題（不補滿）
//   - 0 題 → 只顯示 [閒聊] 單一虛擬項
// - 玩家選題後 → 標記為已看（不影響好感度、純情報收集）
// - seen 狀態為 session 內記憶體（不持久化，未來改存檔再整合）

using System;
using System.Collections.Generic;

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

        private readonly PlayerQuestionsConfig _config;
        private readonly Random _random;

        // 每角色已看過題目 session 記憶體集合。Key = characterId。
        private readonly Dictionary<string, HashSet<string>> _seenByCharacter;

        public PlayerQuestionsManager(PlayerQuestionsConfig config) : this(config, null) { }

        public PlayerQuestionsManager(PlayerQuestionsConfig config, int? seed)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            _seenByCharacter = new Dictionary<string, HashSet<string>>();
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

        /// <summary>該角色未看過的題目數。</summary>
        public int GetUnseenCount(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return 0;
            IReadOnlyList<PlayerQuestionInfo> all = _config.GetQuestionsForCharacter(characterId);
            HashSet<string> seen = GetOrCreateSeen(characterId);
            int n = 0;
            foreach (PlayerQuestionInfo q in all)
                if (!seen.Contains(q.QuestionId)) n++;
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
