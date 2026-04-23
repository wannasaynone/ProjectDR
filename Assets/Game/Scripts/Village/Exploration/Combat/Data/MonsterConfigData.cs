// MonsterConfigData — 怪物類型外部配置的 IGameData DTO 與不可變配置物件。
// 對應 Sheets 分頁：Monsters
// 對應 .txt 檔：monsters.txt
//
// Sprint 8 Wave 2.5 重構：
//   - MonsterTypeJson 改名為 MonsterData（去 Json 後綴）
//   - 欄位 camelCase → snake_case（typeId→type_id, maxHp→max_hp 等）
//   - color 巢狀物件（ColorJson）展開為扁平欄位（color_r/color_g/color_b/color_a）
//   - 廢棄包裹類 MonsterConfigJson（純陣列格式）
//   - 廢棄 ColorJson（color 欄位扁平化後不再需要）
//   - MonsterConfig 建構子改為接受新 MonsterData[]
//   - MonsterTypeData 建構子改為接受新 MonsterData
// ADR-001 / ADR-002 A13

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    // ===== JSON DTO（供 JsonFx 反序列化純陣列使用） =====

    /// <summary>
    /// 單一怪物類型（JSON DTO）。
    /// 實作 IGameData，int id 為流水號主鍵，type_id 為語意字串外鍵。
    /// 欄位扁平化：原 color 巢狀物件展開為 color_r/color_g/color_b/color_a。
    /// 對應 Sheets 分頁 Monsters，.txt 檔 monsters.txt。
    /// </summary>
    [Serializable]
    public class MonsterData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>怪物種類語意識別符（如 "Slime"、"Bat"）。</summary>
        public string type_id;

        /// <summary>語意字串 Key。</summary>
        public string Key => type_id;

        // --- 基本屬性 ---
        public int max_hp;
        public int atk;
        public int def;
        public int spd;

        // --- 移動 ---
        public float move_cooldown_seconds;

        // --- 感知 ---
        public int vision_range;

        // --- 攻擊 ---
        public int attack_range;
        public float attack_angle_degrees_half;
        public float attack_prepare_seconds;
        public float attack_cooldown_seconds;

        // --- 顯示顏色（扁平化，取代巢狀 ColorJson） ---
        public float color_r;
        public float color_g;
        public float color_b;
        public float color_a;
    }

    // ===== 不可變資料物件 =====

    /// <summary>
    /// 單一怪物類型的不可變資料。
    /// 從 MonsterData（扁平化 JSON DTO）建構。
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

        /// <summary>
        /// 從新 MonsterData（扁平化 DTO）建構。
        /// </summary>
        public MonsterTypeData(MonsterData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            TypeId = data.type_id;
            MaxHp = data.max_hp;
            Atk = data.atk;
            Def = data.def;
            Spd = data.spd;
            MoveCooldownSeconds = data.move_cooldown_seconds;
            VisionRange = data.vision_range;
            AttackRange = data.attack_range;
            AttackAngleHalf = data.attack_angle_degrees_half;
            AttackPrepareSeconds = data.attack_prepare_seconds;
            AttackCooldownSeconds = data.attack_cooldown_seconds;
            DisplayColor = new Color(data.color_r, data.color_g, data.color_b, data.color_a);
        }
    }

    // ===== 不可變配置物件 =====

    /// <summary>
    /// 怪物類型配置（不可變）。
    /// 從純陣列 DTO（MonsterData[]）建構。
    /// </summary>
    public class MonsterConfig
    {
        private readonly Dictionary<string, MonsterTypeData> _types;

        /// <summary>所有怪物類型（依輸入順序）。</summary>
        public IReadOnlyList<MonsterTypeData> AllTypes { get; }

        /// <summary>
        /// 從純陣列 DTO 建構不可變配置。
        /// </summary>
        /// <param name="entries">JsonFx 反序列化後的 MonsterData 陣列（不可為 null）。</param>
        public MonsterConfig(MonsterData[] entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            _types = new Dictionary<string, MonsterTypeData>();
            List<MonsterTypeData> list = new List<MonsterTypeData>();

            foreach (MonsterData entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.type_id)) continue;

                MonsterTypeData data = new MonsterTypeData(entry);
                _types[data.TypeId] = data;
                list.Add(data);
            }

            AllTypes = list.AsReadOnly();
        }

        /// <summary>依 type_id 取得怪物類型資料。找不到時回傳 null。</summary>
        public MonsterTypeData GetMonsterType(string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return null;
            _types.TryGetValue(typeId, out MonsterTypeData data);
            return data;
        }
    }
}
