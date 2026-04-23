using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village.Exploration;
using ProjectDR.Village.Exploration.Movement;
using ProjectDR.Village.Exploration.MoveSpeed;
using ProjectDR.Village.Exploration.Map;
using ProjectDR.Village.Exploration.Combat;

namespace ProjectDR.Tests.Village.Exploration.Combat
{
    [TestFixture]
    public class CombatManagerTests
    {
        private class MockMoveSpeedProvider : IMoveSpeedProvider
        {
            public float GetMoveSpeed() => 5.0f;
        }

        private GridMap _gridMap;
        private MonsterManager _monsterManager;
        private PlayerFreeMovement _playerMovement;
        private PlayerCombatStats _playerStats;
        private SwordAttack _swordAttack;
        private CombatManager _sut;

        private const float CellSize = 1.0f;
        private static readonly Vector3 MapOrigin = Vector3.zero;
        private const float KnockbackDistance = 1.5f;

        private static CellType[] CreateAllExplorableCells(int w, int h)
        {
            CellType[] cells = new CellType[w * h];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = CellType.Explorable;
            return cells;
        }

        private MonsterTypeData CreateSlimeType()
        {
            var data = new MonsterData
            {
                id = 1,
                type_id = "Slime",
                max_hp = 6,
                atk = 3,
                def = 1,
                spd = 4,
                move_cooldown_seconds = 2.0f,
                vision_range = 3,
                attack_range = 1,
                attack_angle_degrees_half = 45f,
                attack_prepare_seconds = 1.0f,
                attack_cooldown_seconds = 1.5f,
                color_r = 0.2f,
                color_g = 0.8f,
                color_b = 0.2f,
                color_a = 1f
            };
            return new MonsterTypeData(data);
        }

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            MonsterState.ResetIdCounter();

            CellType[] cells = CreateAllExplorableCells(8, 8);
            MapData mapData = new MapData(8, 8, cells, new Vector2Int(3, 7), new List<List<Vector2Int>>());

            _gridMap = new GridMap(mapData, null);
            _gridMap.InitializeExplored(3, -1);

            _monsterManager = new MonsterManager(_gridMap, () => _gridMap.RecalculateAllMonsterCounts(), 42);

            var speedProvider = new MockMoveSpeedProvider();
            _playerMovement = new PlayerFreeMovement(_gridMap, new Vector2Int(3, 7), CellSize, MapOrigin, speedProvider);

            _playerStats = new PlayerCombatStats(20, 5, 2, 10);
            _swordAttack = new SwordAttack(45f, 1.5f, 0.8f, 0.02f, 10);

            _sut = new CombatManager(_playerStats, _swordAttack, _monsterManager, _gridMap, _playerMovement, null, KnockbackDistance);
        }

        [TearDown]
        public void TearDown()
        {
            _sut?.Dispose();
            EventBus.ForceClearAll();
        }

        [Test]
        public void MonsterAttackExecute_PlayerInFacingDirection_TakeDamage()
        {
            // Place monster at (3,6), facing down (0,1), player at (3,7)
            var type = CreateSlimeType(); // atk=3
            var monster = _monsterManager.SpawnMonster(type, new Vector2Int(3, 6));
            monster.FacingDirection = new Vector2Int(0, 1);

            int hpBefore = _playerStats.CurrentHp;

            EventBus.Publish(new MonsterAttackExecuteEvent
            {
                MonsterId = monster.Id,
                Position = monster.Position,
                FacingDirection = monster.FacingDirection
            });

            // DMG = monsterAtk(3) - playerDef(2) = max(1, 1) = 1
            Assert.AreEqual(hpBefore - 1, _playerStats.CurrentHp);
        }

        [Test]
        public void MonsterAttackExecute_PlayerNotInFacingDirection_NoDamage()
        {
            var type = CreateSlimeType();
            var monster = _monsterManager.SpawnMonster(type, new Vector2Int(3, 6));
            monster.FacingDirection = new Vector2Int(-1, 0); // facing left, not toward player

            int hpBefore = _playerStats.CurrentHp;

            EventBus.Publish(new MonsterAttackExecuteEvent
            {
                MonsterId = monster.Id,
                Position = monster.Position,
                FacingDirection = monster.FacingDirection
            });

            Assert.AreEqual(hpBefore, _playerStats.CurrentHp);
        }

        [Test]
        public void ContactDamage_TakesDamageAndPublishesKnockback()
        {
            var type = CreateSlimeType(); // atk=3
            var monster = _monsterManager.SpawnMonster(type, new Vector2Int(3, 6));

            int hpBefore = _playerStats.CurrentHp;

            PlayerKnockbackEvent knockbackReceived = null;
            Action<PlayerKnockbackEvent> handler = (e) => { knockbackReceived = e; };
            EventBus.Subscribe<PlayerKnockbackEvent>(handler);

            EventBus.Publish(new PlayerContactDamageEvent
            {
                MonsterId = monster.Id,
                ContactPosition = new Vector2(3f, -6f),
                KnockbackDirection = Vector2.down,
                DamageDealt = 0 // Actual damage is calculated by CombatManager
            });

            EventBus.Unsubscribe<PlayerKnockbackEvent>(handler);

            // Should take damage: DMG = 3 - 2 = 1
            Assert.AreEqual(hpBefore - 1, _playerStats.CurrentHp);

            // Should publish knockback
            Assert.IsNotNull(knockbackReceived);
            Assert.AreEqual(KnockbackDistance, knockbackReceived.Distance, 0.001f);
        }

        [Test]
        public void ContactDamage_DeadMonster_NoDamage()
        {
            var type = CreateSlimeType();
            var monster = _monsterManager.SpawnMonster(type, new Vector2Int(3, 6));

            // Kill the monster
            _monsterManager.DamageMonster(monster.Id, 100);
            Assert.IsTrue(monster.IsDead);

            int hpBefore = _playerStats.CurrentHp;

            EventBus.Publish(new PlayerContactDamageEvent
            {
                MonsterId = monster.Id,
                ContactPosition = new Vector2(3f, -6f),
                KnockbackDirection = Vector2.down,
                DamageDealt = 0
            });

            Assert.AreEqual(hpBefore, _playerStats.CurrentHp);
        }

        [Test]
        public void ContactDamage_ZeroKnockbackDirection_NoCrash()
        {
            var type = CreateSlimeType();
            var monster = _monsterManager.SpawnMonster(type, new Vector2Int(3, 6));

            // Zero direction should not crash, just apply damage without knockback
            Assert.DoesNotThrow(() =>
            {
                EventBus.Publish(new PlayerContactDamageEvent
                {
                    MonsterId = monster.Id,
                    ContactPosition = new Vector2(3f, -6f),
                    KnockbackDirection = Vector2.zero,
                    DamageDealt = 0
                });
            });
        }

        [Test]
        public void Dispose_UnsubscribesAllEvents()
        {
            _sut.Dispose();

            int hpBefore = _playerStats.CurrentHp;

            var type = CreateSlimeType();
            _monsterManager.SpawnMonster(type, new Vector2Int(3, 6));

            // Contact damage should not be processed after dispose
            EventBus.Publish(new PlayerContactDamageEvent
            {
                MonsterId = 1,
                ContactPosition = Vector2.zero,
                KnockbackDirection = Vector2.down,
                DamageDealt = 0
            });

            Assert.AreEqual(hpBefore, _playerStats.CurrentHp);

            _sut = null;
        }
    }
}
