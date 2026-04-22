// IdleChatPresenter — [閒聊] 模式純邏輯（Sprint 5 B12）。
//
// 依 character-content-template.md v1.4 §3.3 [閒聊] 模式：
// - 玩家 40 題池耗盡後觸發
// - 從該角色的閒聊主題池隨機抽 1 + 該題 3 回答中隨機抽 1
// - 不影響好感度、不累計已看
// - 發布 IdleChatTriggeredEvent

using System;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Village.IdleChat
{
    /// <summary>[閒聊] 模式單次觸發結果。</summary>
    public class IdleChatResult
    {
        public string CharacterId { get; }
        public string TopicId { get; }
        public string Prompt { get; }
        public string AnswerId { get; }
        public string Answer { get; }

        public IdleChatResult(string characterId, string topicId, string prompt, string answerId, string answer)
        {
            CharacterId = characterId;
            TopicId = topicId;
            Prompt = prompt;
            AnswerId = answerId;
            Answer = answer;
        }
    }

    public class IdleChatPresenter
    {
        private readonly IdleChatConfig _config;
        private readonly Random _random;

        public IdleChatPresenter(IdleChatConfig config) : this(config, seed: null) { }

        public IdleChatPresenter(IdleChatConfig config, int? seed)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// 為指定角色觸發一次閒聊：隨機題 + 隨機回答。
        /// 找不到題目時回 null。
        /// 成功時發布 IdleChatTriggeredEvent。
        /// </summary>
        public IdleChatResult Trigger(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;

            System.Collections.Generic.IReadOnlyList<IdleChatTopic> topics
                = _config.GetTopicsForCharacter(characterId);
            if (topics == null || topics.Count == 0) return null;

            IdleChatTopic topic = topics[_random.Next(topics.Count)];
            if (topic.Answers == null || topic.Answers.Count == 0)
            {
                // 無回答 → 仍回傳空回答結果
                IdleChatResult emptyResult = new IdleChatResult(
                    characterId, topic.TopicId, topic.Prompt, string.Empty, string.Empty);
                EventBus.Publish(new IdleChatTriggeredEvent
                {
                    CharacterId = characterId,
                    TopicId = topic.TopicId,
                    AnswerId = string.Empty,
                });
                return emptyResult;
            }
            IdleChatAnswer answer = topic.Answers[_random.Next(topic.Answers.Count)];

            EventBus.Publish(new IdleChatTriggeredEvent
            {
                CharacterId = characterId,
                TopicId = topic.TopicId,
                AnswerId = answer.AnswerId,
            });

            return new IdleChatResult(characterId, topic.TopicId, topic.Prompt, answer.AnswerId, answer.Text);
        }
    }
}
