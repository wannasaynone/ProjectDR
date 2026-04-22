// CombatConfigData — 戰鬥系統外部配置的 JSON DTO 與不可變配置物件。
// 配置檔路徑：Assets/Game/Resources/Config/combat-config.json
// Sheets 對應分頁：combat-config
//
// ADR-001 / ADR-002 A05 改造（2026-04-22）：
//   CombatConfigJson 實作 KahaGameCore.GameData.IGameData（singleton 型，ID 固定為 1）。
//   載入路徑改走 GameStaticDataManager.Add<CombatConfigJson>()。
//   原 NOTE 關於「不使用 IGameData」的說明已撤除（豁免僅適用 IT 階段，ADR-002 規定）。

using System;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// JSON DTO for combat configuration. Deserialized from combat-config.json.
    /// 實作 IGameData（singleton 型配置，ID 固定為 1）。
    /// 對應 Sheets 分頁：combat-config / JSON：combat-config.json。
    /// </summary>
    [Serializable]
    public class CombatConfigJson : KahaGameCore.GameData.IGameData
    {
        /// <summary>
        /// IGameData 主鍵（singleton 型配置，固定為 1）。
        /// JSON 欄位 "id"，應在 JSON 中明確設定為 1。
        /// </summary>
        public int id;

        /// <summary>IGameData 契約實作。Singleton 型配置回傳固定值 1。</summary>
        public int ID => id;

        public PlayerStatsJson playerStats;
        public SwordConfigJson sword;
        public float moveSpeedBase;
        public float spdMoveSpeedFactor;
        public float freeMovementBaseSpeed;
        public float spdFreeMovementSpeedFactor;
        public float knockbackDistance;
        public float knockbackDuration;
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
        public float FreeMovementBaseSpeed { get; }
        public float SpdFreeMovementSpeedFactor { get; }
        public float KnockbackDistance { get; }
        public float KnockbackDuration { get; }

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
            FreeMovementBaseSpeed = json.freeMovementBaseSpeed;
            SpdFreeMovementSpeedFactor = json.spdFreeMovementSpeedFactor;
            KnockbackDistance = json.knockbackDistance;
            KnockbackDuration = json.knockbackDuration;
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
