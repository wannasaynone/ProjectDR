// CombatConfigDataTests — 戰鬥配置資料 IGameData 契約驗證測試。
// ADR-001 / ADR-002 A05 改造後（Sprint 8 Wave 2.5）：
//   - CombatConfigJson → CombatConfigData（欄位 snake_case 扁平化）
//   - CombatConfig.Load() → new CombatConfig(CombatConfigData)
//   - MonsterTypeJson → MonsterData（欄位 snake_case 扁平化）
//   - MonsterConfig.Load() → new MonsterConfig(MonsterData[])
//   - monsterConfig.GetType() → monsterConfig.GetMonsterType()
// ADR-001 / ADR-002 A13 MonsterData IGameData 契約驗證（2026-04-22）。

using NUnit.Framework;
using JsonFx.Json;
using ProjectDR.Village.Exploration.Combat;

namespace ProjectDR.Tests.Village.Exploration.Combat
{
    /// <summary>
    /// CombatConfigData / CombatConfig 的單元測試。
    /// 重點：ADR-001 IGameData 契約驗證 + 反序列化正確性。
    /// </summary>
    [TestFixture]
    public class CombatConfigDataTests
    {
        // ===== ADR-001 IGameData 契約驗證 =====

        [Test]
        public void CombatConfigData_ImplementsIGameData()
        {
            var dto = new CombatConfigData { id = 1 };
            Assert.IsInstanceOf<KahaGameCore.GameData.IGameData>(dto,
                "CombatConfigData 必須實作 IGameData（ADR-001）");
        }

        [Test]
        public void CombatConfigData_ID_IsNonZero_WhenSetToOne()
        {
            const int SINGLETON_ID = 1;
            var dto = new CombatConfigData { id = SINGLETON_ID };

            Assert.AreNotEqual(0, dto.ID,
                "CombatConfigData.ID 不可為 0（ADR-001 IGameData 契約，singleton 型 ID=1）");
            Assert.AreEqual(SINGLETON_ID, dto.ID);
        }

        [Test]
        public void CombatConfigData_ID_MapsToIdField()
        {
            var dto = new CombatConfigData { id = 1 };
            Assert.AreEqual(1, dto.ID, "ID property 應對應 int id 欄位");
        }

        // ===== 反序列化正確性 =====

        [Test]
        public void CombatConfigData_Deserialization_WithId_ParsesCorrectly()
        {
            // 扁平化 JSON 格式（Sprint 8 Wave 2.5 後）
            string json = @"[{
                ""id"": 1,
                ""player_max_hp"": 20,
                ""player_atk"": 5,
                ""player_def"": 2,
                ""player_spd"": 10,
                ""sword_angle_degrees_half"": 45.0,
                ""sword_range"": 1.5,
                ""sword_base_cooldown_seconds"": 0.8,
                ""sword_spd_cooldown_factor"": 0.02,
                ""move_speed_base"": 0.2,
                ""spd_move_speed_factor"": 0.005,
                ""free_movement_base_speed"": 3.0,
                ""spd_free_movement_speed_factor"": 0.1,
                ""knockback_distance"": 1.5,
                ""knockback_duration"": 0.2
            }]";

            CombatConfigData[] arr = JsonReader.Deserialize<CombatConfigData[]>(json);

            Assert.IsNotNull(arr);
            Assert.AreEqual(1, arr.Length);
            CombatConfigData dto = arr[0];
            Assert.AreEqual(1, dto.id);
            Assert.AreEqual(1, dto.ID, "反序列化後 ID 應為 1（singleton 型）");
            Assert.AreNotEqual(0, dto.ID, "反序列化後 ID 不可為 0（ADR-001）");
            Assert.AreEqual(20, dto.player_max_hp);
            Assert.AreEqual(5, dto.player_atk);
        }

        [Test]
        public void CombatConfig_Constructor_WithValidData_BuildsCorrectConfig()
        {
            // new CombatConfig(CombatConfigData) 取代舊 CombatConfig.Load(json)
            var dto = new CombatConfigData
            {
                id = 1,
                player_max_hp = 20,
                player_atk = 5,
                player_def = 2,
                player_spd = 10,
                sword_angle_degrees_half = 45f,
                sword_range = 1.5f,
                sword_base_cooldown_seconds = 0.8f,
                sword_spd_cooldown_factor = 0.02f,
                move_speed_base = 0.2f,
                spd_move_speed_factor = 0.005f,
                free_movement_base_speed = 3.0f,
                spd_free_movement_speed_factor = 0.1f,
                knockback_distance = 1.5f,
                knockback_duration = 0.2f
            };

            CombatConfig config = new CombatConfig(dto);

            Assert.IsNotNull(config);
            Assert.AreEqual(20, config.PlayerMaxHp);
            Assert.AreEqual(5, config.PlayerAtk);
            Assert.AreEqual(45f, config.SwordAngleHalf, 0.001f);
        }

        [Test]
        public void CombatConfig_Constructor_NullData_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new CombatConfig(null));
        }

        // ===== A13: MonsterData IGameData 契約驗證 =====

        [Test]
        public void MonsterData_ImplementsIGameData()
        {
            var dto = new MonsterData { id = 1, type_id = "Slime" };
            Assert.IsInstanceOf<KahaGameCore.GameData.IGameData>(dto,
                "MonsterData 必須實作 IGameData（ADR-001 A13）");
        }

        [Test]
        public void MonsterData_ID_IsNonZero_WhenIdSet()
        {
            const int SAMPLE_ID = 1;
            var dto = new MonsterData { id = SAMPLE_ID };
            Assert.AreNotEqual(0, dto.ID,
                "MonsterData.ID 不可為 0（ADR-001）");
            Assert.AreEqual(SAMPLE_ID, dto.ID);
        }

        [Test]
        public void MonsterConfig_Constructor_WithIdField_ParsesAllEntries()
        {
            // new MonsterConfig(MonsterData[]) 取代舊 MonsterConfig.Load(json)
            var entries = new MonsterData[]
            {
                new MonsterData
                {
                    id = 1, type_id = "Slime", max_hp = 6, atk = 3, def = 1, spd = 4,
                    move_cooldown_seconds = 2.0f, vision_range = 3, attack_range = 1,
                    attack_angle_degrees_half = 45f, attack_prepare_seconds = 1.0f,
                    attack_cooldown_seconds = 1.5f,
                    color_r = 0.2f, color_g = 0.8f, color_b = 0.2f, color_a = 1.0f
                },
                new MonsterData
                {
                    id = 2, type_id = "Bat", max_hp = 4, atk = 4, def = 0, spd = 8,
                    move_cooldown_seconds = 1.2f, vision_range = 4, attack_range = 1,
                    attack_angle_degrees_half = 30f, attack_prepare_seconds = 0.6f,
                    attack_cooldown_seconds = 1.0f,
                    color_r = 0.5f, color_g = 0.1f, color_b = 0.6f, color_a = 1.0f
                }
            };

            MonsterConfig config = new MonsterConfig(entries);

            Assert.IsNotNull(config);
            Assert.AreEqual(2, config.AllTypes.Count);

            // GetMonsterType 取代舊 GetType（避免與 System.Object.GetType 衝突）
            MonsterTypeData slime = config.GetMonsterType("Slime");
            Assert.IsNotNull(slime);
            Assert.AreEqual(6, slime.MaxHp);
            Assert.AreEqual("Slime", slime.TypeId);
        }
    }
}
