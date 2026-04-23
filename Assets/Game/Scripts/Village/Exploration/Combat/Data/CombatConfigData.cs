// CombatConfigData — 戰鬥系統外部配置的 IGameData DTO 與不可變配置物件。
// 對應 Sheets 分頁：Combat
// 對應 .txt 檔：combat.txt
//
// Sprint 8 Wave 2.5 重構：
//   - CombatConfigJson 改名為 CombatConfigData，欄位扁平化（原 playerStats/sword 巢狀物件展開）
//   - 廢棄 PlayerStatsJson / SwordConfigJson 子物件（純陣列 singleton 格式；1 筆資料）
//   - CombatConfig 建構子改為接受新 CombatConfigData
//   - singleton 格式：JsonFx 反序列化為 CombatConfigData[]，runtime 取 array[0]
// ADR-001 / ADR-002 A05

using System;

namespace ProjectDR.Village.Exploration.Combat
{
    // ===== JSON DTO（供 JsonFx 反序列化純陣列使用） =====

    /// <summary>
    /// 戰鬥系統配置（JSON DTO，singleton）。
    /// 實作 IGameData，id 固定為 1（只有一筆資料）。
    /// 欄位扁平化：原 playerStats/sword 巢狀物件展開為欄位前綴形式。
    /// 對應 Sheets 分頁 Combat，.txt 檔 combat.txt。
    /// </summary>
    [Serializable]
    public class CombatConfigData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（singleton，固定為 1）。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        // --- 玩家基本屬性 ---
        public int player_max_hp;
        public int player_atk;
        public int player_def;
        public int player_spd;

        // --- 劍攻擊屬性 ---
        public float sword_angle_degrees_half;
        public float sword_range;
        public float sword_base_cooldown_seconds;
        public float sword_spd_cooldown_factor;

        // --- 移動 ---
        public float move_speed_base;
        public float spd_move_speed_factor;
        public float free_movement_base_speed;
        public float spd_free_movement_speed_factor;

        // --- 擊退 ---
        public float knockback_distance;
        public float knockback_duration;
    }

    // ===== 不可變配置物件 =====

    /// <summary>
    /// 戰鬥系統的不可變配置。
    /// 從 CombatConfigData（扁平化 JSON DTO）建構。
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

        /// <summary>
        /// 從新 CombatConfigData（扁平化 DTO）建構。
        /// </summary>
        public CombatConfig(CombatConfigData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            PlayerMaxHp = data.player_max_hp;
            PlayerAtk = data.player_atk;
            PlayerDef = data.player_def;
            PlayerSpd = data.player_spd;

            SwordAngleHalf = data.sword_angle_degrees_half;
            SwordRange = data.sword_range;
            SwordBaseCooldown = data.sword_base_cooldown_seconds;
            SwordSpdCooldownFactor = data.sword_spd_cooldown_factor;

            MoveSpeedBase = data.move_speed_base;
            SpdMoveSpeedFactor = data.spd_move_speed_factor;
            FreeMovementBaseSpeed = data.free_movement_base_speed;
            SpdFreeMovementSpeedFactor = data.spd_free_movement_speed_factor;
            KnockbackDistance = data.knockback_distance;
            KnockbackDuration = data.knockback_duration;
        }
    }
}
