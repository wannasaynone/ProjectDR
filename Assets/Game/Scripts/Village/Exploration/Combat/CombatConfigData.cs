using System;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    // NOTE: CombatConfigData and MonsterConfigData are intentionally NOT using Google Sheets / IGameData.
    // These are design-tuning JSON configs that ship as TextAssets in Resources/Config/.
    // They are not tabular data requiring the GoogleSheet2JsonSetting pipeline.
    // Exception recorded here per development-workflow.md S1.5.

    /// <summary>
    /// JSON DTO for combat configuration. Deserialized from combat-config.json.
    /// </summary>
    [Serializable]
    public class CombatConfigJson
    {
        public PlayerStatsJson playerStats;
        public SwordConfigJson sword;
        public float moveSpeedBase;
        public float spdMoveSpeedFactor;
    }

    [Serializable]
    public class PlayerStatsJson
    {
        public int maxHp;
        public int atk;
        public int def;
        public int spd;
    }

    [Serializable]
    public class SwordConfigJson
    {
        public float angleDegreesHalf;
        public float range;
        public float baseCooldownSeconds;
        public float spdCooldownFactor;
    }

    /// <summary>
    /// Immutable combat configuration loaded from JSON.
    /// </summary>
    public class CombatConfig
    {
        public int PlayerMaxHp { get; }
        public int PlayerAtk { get; }
        public int PlayerDef { get; }
        public int PlayerSpd { get; }

        public float SwordAngleHalf { get; }
        public float SwordRange { get; }
        public float SwordBaseCooldown { get; }
        public float SwordSpdCooldownFactor { get; }

        public float MoveSpeedBase { get; }
        public float SpdMoveSpeedFactor { get; }

        public CombatConfig(CombatConfigJson json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            if (json.playerStats == null) throw new ArgumentException("playerStats is null.", nameof(json));
            if (json.sword == null) throw new ArgumentException("sword is null.", nameof(json));

            PlayerMaxHp = json.playerStats.maxHp;
            PlayerAtk = json.playerStats.atk;
            PlayerDef = json.playerStats.def;
            PlayerSpd = json.playerStats.spd;

            SwordAngleHalf = json.sword.angleDegreesHalf;
            SwordRange = json.sword.range;
            SwordBaseCooldown = json.sword.baseCooldownSeconds;
            SwordSpdCooldownFactor = json.sword.spdCooldownFactor;

            MoveSpeedBase = json.moveSpeedBase;
            SpdMoveSpeedFactor = json.spdMoveSpeedFactor;
        }

        /// <summary>
        /// Loads a CombatConfig from a JSON string.
        /// </summary>
        public static CombatConfig Load(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("json must not be null or empty.", nameof(json));

            CombatConfigJson dto = JsonUtility.FromJson<CombatConfigJson>(json);
            return new CombatConfig(dto);
        }
    }
}
