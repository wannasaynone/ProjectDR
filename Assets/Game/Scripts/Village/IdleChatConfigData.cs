// IdleChatConfigData — [閒聊] 問題池配置（Sprint 5 B12）。
// 配置檔路徑：Assets/Game/Resources/Config/idle-chat-config.json
//
// 結構：4 角色 × 20 題，每題 3 個回答。
// 玩家 40 題池耗盡後以此為 fallback：隨機抽一題 + 從該題 3 回答中隨機抽一句。
// 不影響好感度、不累計已看。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village
{
    // ===== JSON DTO =====

    [Serializable]
    public class IdleChatAnswerData
    {
        public string answer_id;
        public string text;
    }

    [Serializable]
    public class IdleChatTopicData
    {
        public string character_id;
        public string topic_id;
        public string prompt;
        public IdleChatAnswerData[] answers;
    }

    [Serializable]
    public class IdleChatConfigData
    {
        public int schema_version;
        public string note;
        public IdleChatTopicData[] topics;
    }

    // ===== 不可變 =====

    public class IdleChatAnswer
    {
        public string AnswerId { get; }
        public string Text { get; }

        public IdleChatAnswer(string answerId, string text)
        {
            AnswerId = answerId;
            Text = text;
        }
    }

    public class IdleChatTopic
    {
        public string CharacterId { get; }
        public string TopicId { get; }
        public string Prompt { get; }
        public IReadOnlyList<IdleChatAnswer> Answers { get; }

        public IdleChatTopic(string characterId, string topicId, string prompt, IReadOnlyList<IdleChatAnswer> answers)
        {
            CharacterId = characterId;
            TopicId = topicId;
            Prompt = prompt;
            Answers = answers;
        }
    }

    public class IdleChatConfig
    {
        private readonly Dictionary<string, List<IdleChatTopic>> _byCharacter;
        private readonly Dictionary<string, IdleChatTopic> _byTopicId;

        public IdleChatConfig(IdleChatConfigData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _byCharacter = new Dictionary<string, List<IdleChatTopic>>();
            _byTopicId = new Dictionary<string, IdleChatTopic>();

            IdleChatTopicData[] topics = data.topics ?? Array.Empty<IdleChatTopicData>();
            foreach (IdleChatTopicData t in topics)
            {
                if (t == null || string.IsNullOrEmpty(t.topic_id) || string.IsNullOrEmpty(t.character_id)) continue;
                List<IdleChatAnswer> answers = new List<IdleChatAnswer>();
                IdleChatAnswerData[] ans = t.answers ?? Array.Empty<IdleChatAnswerData>();
                foreach (IdleChatAnswerData a in ans)
                {
                    if (a == null) continue;
                    answers.Add(new IdleChatAnswer(a.answer_id ?? string.Empty, a.text ?? string.Empty));
                }

                // JSON snake_case → CharacterIds PascalCase
                string canonical = CharacterIdSnakeCaseMapper.ToPascal(t.character_id);
                IdleChatTopic topic = new IdleChatTopic(
                    canonical, t.topic_id, t.prompt ?? string.Empty, answers.AsReadOnly());

                _byTopicId[t.topic_id] = topic;

                AddToCharacter(canonical, topic);
                if (canonical != t.character_id) AddToCharacter(t.character_id, topic);
            }
        }

        private void AddToCharacter(string characterId, IdleChatTopic topic)
        {
            if (!_byCharacter.TryGetValue(characterId, out List<IdleChatTopic> bucket))
            {
                bucket = new List<IdleChatTopic>();
                _byCharacter[characterId] = bucket;
            }
            bucket.Add(topic);
        }

        public IReadOnlyList<IdleChatTopic> GetTopicsForCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return Array.AsReadOnly(Array.Empty<IdleChatTopic>());
            return _byCharacter.TryGetValue(characterId, out List<IdleChatTopic> list)
                ? list.AsReadOnly()
                : Array.AsReadOnly(Array.Empty<IdleChatTopic>());
        }

        public IdleChatTopic GetTopic(string topicId)
        {
            if (string.IsNullOrEmpty(topicId)) return null;
            return _byTopicId.TryGetValue(topicId, out IdleChatTopic t) ? t : null;
        }
    }
}
