// ADR-001 / ADR-002 A13 改造（2026-04-22）：
//   MonsterTypeJson 實作 KahaGameCore.GameData.IGameData，
//   加 int id 欄位（流水號主鍵）+ 保留 typeId（語意字串外鍵）。

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    [Serializable]
    public class MonsterConfigJson
    {
        public MonsterTypeJson[] monsterTypes;
    }

    [Serializable]
    public class MonsterTypeJson : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。回傳 int id 流水號。</summary>
        public int ID => id;

        /// <summary>怪物種類語意字串外鍵（如 "Slime"、"Bat"）。</summary>
        public string typeId;
        public int maxHp;
        public int atk;
        public int def;
        public int spd;
        public float moveCooldownSeconds;
        public int visionRange;
        public int attackRange;
        public float attackAngleDegreesHalf;
        public float attackPrepareSeconds;
        public float attackCooldownSeconds;
        public ColorJson color;
    }

    [Serializable]
    public class ColorJson
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }

    /// <summary>
    /// Immutable data for a single monster type.
    /// </summary>
    public class MonsterTypeData
    {
        public string TypeId { get; }
        public int MaxHp { get; }
        public int Atk { get; }
        public int Def { get; }
        public int Spd { get; }
        public float MoveCooldownSeconds { get; }
        public int VisionRange { get; }
        public int AttackRange { get; }
        public float AttackAngleHalf { get; }
        public float AttackPrepareSeconds { get; }
        public float AttackCooldownSeconds { get; }
        public Color DisplayColor { get; }

        public MonsterTypeData(MonsterTypeJson json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            TypeId = json.typeId;
            MaxHp = json.maxHp;
            Atk = json.atk;
            Def = json.def;
            Spd = json.spd;
            MoveCooldownSeconds = json.moveCooldownSeconds;
            VisionRange = json.visionRange;
            AttackRange = json.attackRange;
            AttackAngleHalf = json.attackAngleDegreesHalf;
            AttackPrepareSeconds = json.attackPrepareSeconds;
            AttackCooldownSeconds = json.attackCooldownSeconds;
            DisplayColor = json.color != null ? json.color.ToColor() : Color.red;
        }
    }

    /// <summary>
    /// Container for all monster type configurations loaded from JSON.
    /// </summary>
    public class MonsterConfig
    {
        private readonly Dictionary<string, MonsterTypeData> _types;

        public IReadOnlyList<MonsterTypeData> AllTypes { get; }

        public MonsterConfig(MonsterConfigJson json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            _types = new Dictionary<string, MonsterTypeData>();
            List<MonsterTypeData> list = new List<MonsterTypeData>();

            if (json.monsterTypes != null)
            {
                for (int i = 0; i < json.monsterTypes.Length; i++)
                {
                    MonsterTypeData data = new MonsterTypeData(json.monsterTypes[i]);
                    _types[data.TypeId] = data;
                    list.Add(data);
                }
            }

            AllTypes = list.AsReadOnly();
        }

        public MonsterTypeData GetType(string typeId)
        {
            if (_types.TryGetValue(typeId, out MonsterTypeData data))
                return data;
            return null;
        }

        /// <summary>
        /// Loads a MonsterConfig from a JSON string.
        /// </summary>
        public static MonsterConfig Load(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("json must not be null or empty.", nameof(json));

            MonsterConfigJson dto = JsonUtility.FromJson<MonsterConfigJson>(json);
            return new MonsterConfig(dto);
        }
    }
}
