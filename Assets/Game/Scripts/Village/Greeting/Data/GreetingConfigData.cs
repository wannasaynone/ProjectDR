// GreetingConfigData — 招呼語配置（Sprint 5 B15）。
// 配置檔路徑：Assets/Game/Resources/Config/greeting-config.json
//
// 結構：4 角色 × 7 級 × 10 句 = 280 句
// 進入角色互動畫面 Normal 狀態時自動播放一句（隨機從該等級池選取）。
// 不消耗體力、不影響好感度、[L1/L4 紅點亮時跳過]、[L2/L3 紅點亮時仍播放]。
// ADR-002 A07：GreetingEntryData 實作 IGameData；int id 為流水號主鍵，greeting_id 為語意字串外鍵。

using System;
using System.Collections.Generic;
using KahaGameCore.GameData;
using ProjectDR.Village.CharacterUnlock;

namespace ProjectDR.Village.Greeting
{
    // ===== JSON DTO =====

    [Serializable]
    public class GreetingEntryData : IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;
        public string character_id;
        public int level;
        public string greeting_id;
        public string text;

        /// <summary>IGameData 契約實作。回傳 int id 流水號。</summary>
        public int ID => id;
        /// <summary>語意字串主鍵（唯一識別此招呼語）。</summary>
        public string Key => greeting_id;
    }

    [Serializable]
    public class GreetingConfigData
    {
        public int schema_version;
        public string note;
        public GreetingEntryData[] greetings;
    }

    // ===== 不可變 =====

    public class GreetingInfo
    {
        public string CharacterId { get; }
        public int Level { get; }
        public string GreetingId { get; }
        public string Text { get; }

        public GreetingInfo(string characterId, int level, string greetingId, string text)
        {
            CharacterId = characterId;
            Level = level;
            GreetingId = greetingId;
            Text = text;
        }
    }

    public class GreetingConfig
    {
        private readonly Dictionary<string, Dictionary<int, List<GreetingInfo>>> _byCharLevel;
        private readonly Dictionary<string, GreetingInfo> _byId;

        public GreetingConfig(GreetingConfigData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _byCharLevel = new Dictionary<string, Dictionary<int, List<GreetingInfo>>>();
            _byId = new Dictionary<string, GreetingInfo>();

            GreetingEntryData[] entries = data.greetings ?? Array.Empty<GreetingEntryData>();
            foreach (GreetingEntryData e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.greeting_id) || string.IsNullOrEmpty(e.character_id)) continue;

                // JSON 內 snake_case → CharacterIds 常數 (PascalCase)
                string canonical = CharacterIdSnakeCaseMapper.ToPascal(e.character_id);
                GreetingInfo info = new GreetingInfo(canonical, e.level, e.greeting_id, e.text ?? string.Empty);
                _byId[e.greeting_id] = info;

                AddToCharLevel(canonical, e.level, info);
                if (canonical != e.character_id) AddToCharLevel(e.character_id, e.level, info);
            }
        }

        private void AddToCharLevel(string charId, int level, GreetingInfo info)
        {
            if (!_byCharLevel.TryGetValue(charId, out Dictionary<int, List<GreetingInfo>> byLevel))
            {
                byLevel = new Dictionary<int, List<GreetingInfo>>();
                _byCharLevel[charId] = byLevel;
            }
            if (!byLevel.TryGetValue(level, out List<GreetingInfo> bucket))
            {
                bucket = new List<GreetingInfo>();
                byLevel[level] = bucket;
            }
            bucket.Add(info);
        }

        /// <summary>取得指定角色 × 等級的招呼語池。</summary>
        public IReadOnlyList<GreetingInfo> GetGreetings(string characterId, int level)
        {
            if (string.IsNullOrEmpty(characterId)) return Array.AsReadOnly(Array.Empty<GreetingInfo>());
            if (!_byCharLevel.TryGetValue(characterId, out Dictionary<int, List<GreetingInfo>> byLevel))
                return Array.AsReadOnly(Array.Empty<GreetingInfo>());
            if (!byLevel.TryGetValue(level, out List<GreetingInfo> list))
                return Array.AsReadOnly(Array.Empty<GreetingInfo>());
            return list.AsReadOnly();
        }

        public GreetingInfo GetGreeting(string greetingId)
        {
            if (string.IsNullOrEmpty(greetingId)) return null;
            return _byId.TryGetValue(greetingId, out GreetingInfo info) ? info : null;
        }
    }
}
