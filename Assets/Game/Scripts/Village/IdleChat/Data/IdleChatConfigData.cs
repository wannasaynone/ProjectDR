// IdleChatConfigData — 閒聊問題池配置的 IGameData DTO 與不可變配置物件。
// 對應 Sheets 分頁：IdleChat（主表）/ IdleChatAnswers（子表）
// 對應 .txt 檔：idlechat.txt / idlechatanswers.txt
//
// Sprint 8 Wave 2.5 重構：
//   - IdleChatAnswerData 全面重寫：加 int id + topic_id FK + IGameData
//   - IdleChatTopicData 保留（已有 IGameData），移除舊 answers[] 嵌套欄位
//   - 廢棄包裹類 IdleChatConfigData（純陣列格式）
//   - IdleChatConfig 建構子改為接受兩個獨立陣列
// ADR-001 / ADR-002 A10

using System;
using System.Collections.Generic;
using KahaGameCore.GameData;
using ProjectDR.Village.CharacterUnlock;

namespace ProjectDR.Village.IdleChat
{
    // ===== JSON DTO（供 JsonFx 反序列化純陣列使用） =====

    /// <summary>
    /// 閒聊主題（JSON DTO，主表）。
    /// 實作 IGameData，int id 為流水號主鍵，topic_id 為語意字串外鍵。
    /// 對應 Sheets 分頁 IdleChat，.txt 檔 idlechat.txt。
    /// </summary>
    [Serializable]
    public class IdleChatTopicData : IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>閒聊主題識別符語意字串。</summary>
        public string topic_id;

        /// <summary>語意字串 Key。</summary>
        public string Key => topic_id;

        /// <summary>角色 ID。</summary>
        public string character_id;

        /// <summary>角色發問文字。</summary>
        public string prompt;
    }

    /// <summary>
    /// 閒聊回答（JSON DTO，子表）。
    /// 實作 IGameData，int id 為子表自身流水號主鍵。
    /// FK：topic_id → IdleChat.topic_id。
    /// 對應 Sheets 分頁 IdleChatAnswers，.txt 檔 idlechatanswers.txt。
    /// </summary>
    [Serializable]
    public class IdleChatAnswerData : IGameData
    {
        /// <summary>IGameData 主鍵（子表自身流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>回答識別符語意字串。</summary>
        public string answer_id;

        /// <summary>語意字串 Key。</summary>
        public string Key => answer_id;

        /// <summary>FK 至主表 IdleChat.topic_id。</summary>
        public string topic_id;

        /// <summary>回答文字。</summary>
        public string text;
    }

    // ===== 不可變資料物件 =====

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

    // ===== 不可變配置物件 =====

    public class IdleChatConfig
    {
        private readonly Dictionary<string, List<IdleChatTopic>> _byCharacter;
        private readonly Dictionary<string, IdleChatTopic> _byTopicId;

        /// <summary>
        /// 從純陣列 DTO 建構（主表 + 子表）。
        /// </summary>
        /// <param name="topicEntries">主表 JsonFx 反序列化後的陣列（不可為 null）。</param>
        /// <param name="answerEntries">子表 JsonFx 反序列化後的陣列（不可為 null）。</param>
        public IdleChatConfig(IdleChatTopicData[] topicEntries, IdleChatAnswerData[] answerEntries)
        {
            if (topicEntries == null) throw new ArgumentNullException(nameof(topicEntries));
            if (answerEntries == null) throw new ArgumentNullException(nameof(answerEntries));

            _byCharacter = new Dictionary<string, List<IdleChatTopic>>();
            _byTopicId = new Dictionary<string, IdleChatTopic>();

            // 依 topic_id 分組回答
            Dictionary<string, List<IdleChatAnswerData>> answersByTopic =
                new Dictionary<string, List<IdleChatAnswerData>>();
            foreach (IdleChatAnswerData ans in answerEntries)
            {
                if (ans == null || string.IsNullOrEmpty(ans.topic_id)) continue;
                if (!answersByTopic.TryGetValue(ans.topic_id, out List<IdleChatAnswerData> bucket))
                {
                    bucket = new List<IdleChatAnswerData>();
                    answersByTopic[ans.topic_id] = bucket;
                }
                bucket.Add(ans);
            }

            // 建立 topic 不可變物件
            foreach (IdleChatTopicData t in topicEntries)
            {
                if (t == null || string.IsNullOrEmpty(t.topic_id) || string.IsNullOrEmpty(t.character_id)) continue;

                List<IdleChatAnswer> answers = new List<IdleChatAnswer>();
                if (answersByTopic.TryGetValue(t.topic_id, out List<IdleChatAnswerData> ansList))
                {
                    foreach (IdleChatAnswerData a in ansList)
                    {
                        if (a == null) continue;
                        answers.Add(new IdleChatAnswer(a.answer_id ?? string.Empty, a.text ?? string.Empty));
                    }
                }

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
