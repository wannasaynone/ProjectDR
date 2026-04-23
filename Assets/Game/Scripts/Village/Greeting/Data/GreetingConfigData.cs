// GreetingConfigData — 招呼語配置的 IGameData DTO 與不可變配置物件。
// 對應 Sheets 分頁：Greeting
// 對應 .txt 檔：greeting.txt
//
// Sprint 8 Wave 2.5 重構：
//   - GreetingEntryData 改名為 GreetingData（去 Entry）
//   - 廢棄包裹類 GreetingConfigData（純陣列格式）
//   - GreetingConfig 建構子改為接受 GreetingData[]
// ADR-001 / ADR-002 A07

using System;
using System.Collections.Generic;
using KahaGameCore.GameData;
using ProjectDR.Village.CharacterUnlock;

namespace ProjectDR.Village.Greeting
{
    // ===== JSON DTO（供 JsonFx 反序列化純陣列使用） =====

    /// <summary>
    /// 單一招呼語（JSON DTO）。
    /// 實作 IGameData，int id 為流水號主鍵，greeting_id 為語意字串外鍵。
    /// 對應 Sheets 分頁 Greeting，.txt 檔 greeting.txt。
    /// </summary>
    [Serializable]
    public class GreetingData : IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>招呼語識別符語意字串。</summary>
        public string greeting_id;

        /// <summary>語意字串 Key。</summary>
        public string Key => greeting_id;

        /// <summary>角色 ID。</summary>
        public string character_id;

        /// <summary>好感度等級（1~7）。</summary>
        public int level;

        /// <summary>招呼語文字。</summary>
        public string text;
    }

    // ===== 不可變資料物件 =====

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

    // ===== 不可變配置物件 =====

    public class GreetingConfig
    {
        private readonly Dictionary<string, Dictionary<int, List<GreetingInfo>>> _byCharLevel;
        private readonly Dictionary<string, GreetingInfo> _byId;

        /// <summary>
        /// 從純陣列 DTO 建構不可變配置。
        /// </summary>
        /// <param name="entries">JsonFx 反序列化後的 GreetingData 陣列（不可為 null）。</param>
        public GreetingConfig(GreetingData[] entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            _byCharLevel = new Dictionary<string, Dictionary<int, List<GreetingInfo>>>();
            _byId = new Dictionary<string, GreetingInfo>();

            foreach (GreetingData e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.greeting_id) || string.IsNullOrEmpty(e.character_id)) continue;

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
