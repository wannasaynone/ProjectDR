// CombatConfigDataTests — 戰鬥配置資料 IGameData 契約驗證測試。
// ADR-001 / ADR-002 A05 改造後新增（2026-04-22）。
// ADR-001 / ADR-002 A13 MonsterTypeJson IGameData 契約驗證（2026-04-22）。
// 驗證：IGameData 實作、ID 非零、反序列化正確性。

using NUnit.Framework;
using ProjectDR.Village.Exploration.Combat;
using UnityEngine;

namespace ProjectDR.Tests.Village.Exploration.Combat
{
    /// <summary>
    /// CombatConfigJson / CombatConfig 的單元測試。
    /// 重點：ADR-001 IGameData 契約驗證 + 反序列化正確性。
    /// </summary>
    [TestFixture]
    public class CombatConfigDataTests
    {
        // ===== ADR-001 IGameData 契約驗證 =====

        [Test]
        public void CombatConfigJson_ImplementsIGameData()
        {
            // CombatConfigJson 必須實作 IGameData（ADR-001）
            var dto = new CombatConfigJson
            {
                id = 1,
                playerStats = new PlayerStatsJson { maxHp = 20, atk = 5, def = 2, spd = 10 },
                sword = new SwordConfigJson { angleDegreesHalf = 45f, range = 1.5f, baseCooldownSeconds = 0.8f, spdCooldownFactor = 0.02f }
            };

            Assert.IsInstanceOf<KahaGameCore.GameData.IGameData>(dto,
                "CombatConfigJson 必須實作 IGameData（ADR-001）");
        }

        [Test]
        public void CombatConfigJson_ID_IsNonZero_WhenSetToOne()
        {
            // Singleton 型配置：ID 固定設為 1（ADR-001 ID 非 0 斷言）
            const int SINGLETON_ID = 1;
            var dto = new CombatConfigJson { id = SINGLETON_ID };

            Assert.AreNotEqual(0, dto.ID,
                "CombatConfigJson.ID 不可為 0（ADR-001 IGameData 契約，singleton 型 ID=1）");
            Assert.AreEqual(SINGLETON_ID, dto.ID);
        }

        [Test]
        public void CombatConfigJson_ID_MapsToIdField()
        {
            // 確認 IGameData.ID property 對應 int id 欄位
            var dto = new CombatConfigJson { id = 1 };

            Assert.AreEqual(1, dto.ID, "ID property 應對應 int id 欄位");
        }

        // ===== 反序列化正確性 =====

        [Test]
        public void CombatConfigJson_Deserialization_WithId_ParsesCorrectly()
        {
            // 包含 id 欄位的 JSON 應能正確反序列化（ADR-001 改造後 JSON 格式）
            string json = @"{
                ""id"": 1,
                ""playerStats"": { ""maxHp"": 20, ""atk"": 5, ""def"": 2, ""spd"": 10 },
                ""sword"": { ""angleDegreesHalf"": 45.0, ""range"": 1.5, ""baseCooldownSeconds"": 0.8, ""spdCooldownFactor"": 0.02 },
                ""moveSpeedBase"": 0.2,
                ""spdMoveSpeedFactor"": 0.005,
                ""freeMovementBaseSpeed"": 3.0,
                ""spdFreeMovementSpeedFactor"": 0.1,
                ""knockbackDistance"": 1.5,
                ""knockbackDuration"": 0.2
            }";

            CombatConfigJson dto = JsonUtility.FromJson<CombatConfigJson>(json);

            Assert.IsNotNull(dto);
            Assert.AreEqual(1, dto.id);
            Assert.AreEqual(1, dto.ID, "反序列化後 ID 應為 1（singleton 型）");
            Assert.AreNotEqual(0, dto.ID, "反序列化後 ID 不可為 0（ADR-001）");
            Assert.IsNotNull(dto.playerStats);
            Assert.AreEqual(20, dto.playerStats.maxHp);
            Assert.AreEqual(5, dto.playerStats.atk);
        }

        [Test]
        public void CombatConfig_Load_WithIdField_BuildsCorrectConfig()
        {
            // CombatConfig.Load 能從包含 id 欄位的 JSON 正確建構不可變配置
            string json = @"{
                ""id"": 1,
                ""playerStats"": { ""maxHp"": 20, ""atk"": 5, ""def"": 2, ""spd"": 10 },
                ""sword"": { ""angleDegreesHalf"": 45.0, ""range"": 1.5, ""baseCooldownSeconds"": 0.8, ""spdCooldownFactor"": 0.02 },
                ""moveSpeedBase"": 0.2,
                ""spdMoveSpeedFactor"": 0.005,
                ""freeMovementBaseSpeed"": 3.0,
                ""spdFreeMovementSpeedFactor"": 0.1,
                ""knockbackDistance"": 1.5,
                ""knockbackDuration"": 0.2
            }";

            CombatConfig config = CombatConfig.Load(json);

            Assert.IsNotNull(config);
            Assert.AreEqual(20, config.PlayerMaxHp);
            Assert.AreEqual(5, config.PlayerAtk);
            Assert.AreEqual(45f, config.SwordAngleHalf, 0.001f);
        }

        [Test]
        public void CombatConfig_Load_NullOrEmpty_ThrowsArgumentException()
        {
            Assert.Throws<System.ArgumentException>(() => CombatConfig.Load(null));
            Assert.Throws<System.ArgumentException>(() => CombatConfig.Load(string.Empty));
        }

        // ===== A13: MonsterTypeJson IGameData 契約驗證 =====

        [Test]
        public void MonsterTypeJson_ImplementsIGameData()
        {
            // MonsterTypeJson 必須實作 IGameData（ADR-001 A13）
            var dto = new MonsterTypeJson { id = 1, typeId = "Slime" };
            Assert.IsInstanceOf<KahaGameCore.GameData.IGameData>(dto,
                "MonsterTypeJson 必須實作 IGameData（ADR-001 A13）");
        }

        [Test]
        public void MonsterTypeJson_ID_IsNonZero_WhenIdSet()
        {
            const int SAMPLE_ID = 1;
            var dto = new MonsterTypeJson { id = SAMPLE_ID };
            Assert.AreNotEqual(0, dto.ID,
                "MonsterTypeJson.ID 不可為 0（ADR-001）");
            Assert.AreEqual(SAMPLE_ID, dto.ID);
        }

        [Test]
        public void MonsterConfig_Load_WithIdField_ParsesAllEntries()
        {
            // MonsterConfig.Load 能從包含 id 欄位的 JSON 正確建構（A13 JSON 格式）
            string json = @"{
                ""monsterTypes"": [
                    { ""id"": 1, ""typeId"": ""Slime"", ""maxHp"": 6, ""atk"": 3, ""def"": 1, ""spd"": 4,
                      ""moveCooldownSeconds"": 2.0, ""visionRange"": 3, ""attackRange"": 1,
                      ""attackAngleDegreesHalf"": 45, ""attackPrepareSeconds"": 1.0, ""attackCooldownSeconds"": 1.5,
                      ""color"": { ""r"": 0.2, ""g"": 0.8, ""b"": 0.2, ""a"": 1.0 } },
                    { ""id"": 2, ""typeId"": ""Bat"", ""maxHp"": 4, ""atk"": 4, ""def"": 0, ""spd"": 8,
                      ""moveCooldownSeconds"": 1.2, ""visionRange"": 4, ""attackRange"": 1,
                      ""attackAngleDegreesHalf"": 30, ""attackPrepareSeconds"": 0.6, ""attackCooldownSeconds"": 1.0,
                      ""color"": { ""r"": 0.5, ""g"": 0.1, ""b"": 0.6, ""a"": 1.0 } }
                ]
            }";

            MonsterConfig config = MonsterConfig.Load(json);

            Assert.IsNotNull(config);
            Assert.AreEqual(2, config.AllTypes.Count);
            MonsterTypeData slime = config.GetType("Slime");
            Assert.IsNotNull(slime);
            Assert.AreEqual(6, slime.MaxHp);
            Assert.AreEqual("Slime", slime.TypeId);
        }
    }
}
