// CharacterQuestionsManager — 角色發問純邏輯管理器（Sprint 5 B5）。
//
// 職責（依 character-content-template.md v1.4 §3.2、character-interaction.md v2.3 §5.1）：
// - 從當前角色等級未看過的題目中隨機抽一題（發布 CharacterQuestionAskedEvent）
// - 玩家選擇後：依個性對應扣好感度（由 AffinityManager）並發布 CharacterQuestionAnsweredEvent
// - 維護每角色每等級「已看過題目」記憶（session 內，不持久化；未來改存檔時由 EventSubscriber 同步）
// - 題目池耗盡時 PickNextQuestion 回 null
//
// 依 Sprint 5 placeholder 階段要求：
// - 當前角色等級以固定值 1 開始（等級切換由 AffinityManager 後續擴充驅動）
// - 本 Manager 不直接讀取等級：由呼叫端（CharacterInteractionView / 上層）傳入

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.Affinity;

namespace ProjectDR.Village.CharacterQuestions
{
    /// <summary>
    /// 從當前等級抽題、記錄已看、扣好感度、發布事件。
    /// 純邏輯類別（不依賴 MonoBehaviour）。
    /// </summary>
    public class CharacterQuestionsManager
    {
        private readonly CharacterQuestionsConfig _config;
        private readonly AffinityManager _affinityManager;
        private readonly Random _random;

        // 每角色每等級已看過的 question_id 集合
        // Key = $"{characterId}|{level}"
        private readonly Dictionary<string, HashSet<string>> _seenByCharLevel;

        public CharacterQuestionsManager(
            CharacterQuestionsConfig config,
            AffinityManager affinityManager)
            : this(config, affinityManager, seed: null)
        {
        }

        /// <summary>
        /// 測試友善建構子：可注入隨機種子以讓抽題結果可預測。
        /// </summary>
        public CharacterQuestionsManager(
            CharacterQuestionsConfig config,
            AffinityManager affinityManager,
            int? seed)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _affinityManager = affinityManager ?? throw new ArgumentNullException(nameof(affinityManager));
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            _seenByCharLevel = new Dictionary<string, HashSet<string>>();
        }

        /// <summary>
        /// 從指定角色指定等級中抽一題未看過的題目。
        /// 已全部看過時回 null。
        /// 成功時自動發布 CharacterQuestionAskedEvent。
        /// </summary>
        public CharacterQuestionInfo PickNextQuestion(string characterId, int level)
        {
            if (string.IsNullOrEmpty(characterId)) return null;

            IReadOnlyList<CharacterQuestionInfo> all = _config.GetQuestionsForCharacterLevel(characterId, level);
            if (all == null || all.Count == 0) return null;

            HashSet<string> seen = GetOrCreateSeen(characterId, level);

            List<CharacterQuestionInfo> unseen = new List<CharacterQuestionInfo>();
            foreach (CharacterQuestionInfo info in all)
            {
                if (!seen.Contains(info.QuestionId)) unseen.Add(info);
            }

            if (unseen.Count == 0) return null;

            int index = _random.Next(unseen.Count);
            CharacterQuestionInfo picked = unseen[index];
            seen.Add(picked.QuestionId);

            EventBus.Publish(new CharacterQuestionAskedEvent
            {
                CharacterId = characterId,
                Level = level,
                QuestionId = picked.QuestionId,
            });

            return picked;
        }

        /// <summary>
        /// 玩家選擇選項後提交結果。
        /// 依 personality 查詢好感度增量 → 呼叫 AffinityManager.AddAffinity →
        /// 發布 CharacterQuestionAnsweredEvent。
        /// 回傳本次增量（已加入好感度），找不到配對時回 0。
        /// </summary>
        public int SubmitAnswer(string characterId, string questionId, string selectedPersonality)
        {
            if (string.IsNullOrEmpty(characterId)) return 0;
            if (string.IsNullOrEmpty(questionId)) return 0;

            int delta = _config.GetAffinityDelta(characterId, selectedPersonality);
            if (delta > 0)
            {
                _affinityManager.AddAffinity(characterId, delta);
            }

            EventBus.Publish(new CharacterQuestionAnsweredEvent
            {
                CharacterId = characterId,
                QuestionId = questionId,
                SelectedPersonality = selectedPersonality ?? string.Empty,
                AffinityDelta = delta,
            });

            return delta;
        }

        /// <summary>該角色在指定等級已看過的題目 ID 集合（唯讀快照）。</summary>
        public IReadOnlyCollection<string> GetSeenQuestionIds(string characterId, int level)
        {
            HashSet<string> seen = GetOrCreateSeen(characterId, level);
            return seen;
        }

        /// <summary>是否已看過指定題目。</summary>
        public bool HasSeen(string characterId, int level, string questionId)
        {
            if (string.IsNullOrEmpty(questionId)) return false;
            return GetOrCreateSeen(characterId, level).Contains(questionId);
        }

        private HashSet<string> GetOrCreateSeen(string characterId, int level)
        {
            string key = $"{characterId}|{level}";
            if (!_seenByCharLevel.TryGetValue(key, out HashSet<string> seen))
            {
                seen = new HashSet<string>();
                _seenByCharLevel[key] = seen;
            }
            return seen;
        }
    }
}
