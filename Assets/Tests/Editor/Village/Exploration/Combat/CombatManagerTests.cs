using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village.Exploration;
using ProjectDR.Village.Exploration.Combat;

namespace ProjectDR.Tests.Village.Exploration.Combat
{
    [TestFixture]
    public class CombatManagerTests
    {
        private GridMap _gridMap;
        private MonsterManager _monsterManager;
        private PlayerGridMovement _playerMovement;
        private PlayerCombatStats _playerStats;
        private SwordAttack _swordAttack;
        private CombatManager _sut;

        private static CellType[] CreateAllExplorableCells(int w, int h)
        {
            CellType[] cells = new CellType[w * h];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = CellType.Explorable;
            return cells;
        }

        private MonsterTypeData CreateSlimeType()
        {
            var json = new MonsterTypeJson
            {
                typeId = "Slime",
                maxHp = 6,
                atk = 3,
                def = 1,
                spd = 4,
                moveCooldownSeconds = 2.0f,
                visionRange = 3,
                attackRange = 1,
                attackAngleDegreesHalf = 45f,
                attackPrepareSeconds = 1.0f,
                attackCooldownSeconds = 1.5f,
                color = new ColorJson { r = 0.2f, g = 0.8f, b = 0.2f, a = 1f }
            };
            return new MonsterTypeData(json);
        }

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            MonsterState.ResetIdCounter();

            CellType[] cells = CreateAllExplorableCells(8, 8);
            MapData mapData = new MapData(8, 8, cells, new Vector2Int(3, 7), new List<List<Vector2Int>>());

            // First pass: create GridMap with null monster provider (no monsters yet)
            _gridMap = new GridMap(mapData, null);
            _gridMap.InitializeExplored(3, -1);

            // Create monster manager with the gridMap
            _monsterManager = new MonsterManager(_gridMap, () => _gridMap.RecalculateAllMonsterCounts(), 42);

            var speedCalc = new FixedMoveSpeedCalculator(0.5f);
            _playerMovement = new PlayerGridMovement(_gridMap, new Vector2Int(3, 7), speedCalc);

            _playerStats = new PlayerCombatStats(20, 5, 2, 10);
            _swordAttack = new SwordAttack(45f, 1.5f, 0.8f, 0.02f, 10);

            _sut = new CombatManager(_playerStats, _swordAttack, _monsterManager, _gridMap, _playerMovement, null);
        }

        [TearDown]
        public void TearDown()
        {
            _sut?.Dispose();
            EventBus.ForceClearAll();
        }

        [Test]
        public void IsBlockedByVisibleMonster_ExploredCellWithMonster_ReturnsTrue()
        {
            _monsterManager.SpawnMonster(CreateSlimeType(), new Vector2Int(3, 6));

            bool blocked = _sut.IsBlockedByVisibleMonster(3, 6);

            Assert.IsTrue(blocked);
        }

        [Test]
        public void IsBlockedByVisibleMonster_ExploredCellWithoutMonster_ReturnsFalse()
        {
            bool blocked = _sut.IsBlockedByVisibleMonster(3, 6);

            Assert.IsFalse(blocked);
        }

        [Test]
        public void IsBlockedByVisibleMonster_UnexploredCellWithMonster_ReturnsFalse()
        {
            // Cell (0,0) is unexplored (outside reveal radius 3 from spawn (3,7))
            _monsterManager.SpawnMonster(CreateSlimeType(), new Vector2Int(0, 0));

            bool blocked = _sut.IsBlockedByVisibleMonster(0, 0);

            Assert.IsFalse(blocked);
        }

        [Test]
        public void MonsterAttackExecute_PlayerInFacingDirection_TakeDamage()
        {
            // Place monster at (3,6), facing down (0,1), player at (3,7)
            var type = CreateSlimeType(); // atk=3
            var monster = _monsterManager.SpawnMonster(type, new Vector2Int(3, 6));
            monster.FacingDirection = new Vector2Int(0, 1); // facing toward player

            int hpBefore = _playerStats.CurrentHp;

            // Simulate monster attack execute event
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
            // Monster at (3,6), facing left (-1,0), player at (3,7) - not in attack direction
            var type = CreateSlimeType();
            var monster = _monsterManager.SpawnMonster(type, new Vector2Int(3, 6));
            monster.FacingDirection = new Vector2Int(-1, 0);

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
        public void PlayerMoveCompleted_StepOnMonster_TakesDamageAndPublishesEvent()
        {
            // Place a hidden monster at (3,6)
            _monsterManager.SpawnMonster(CreateSlimeType(), new Vector2Int(3, 6));

            PlayerSteppedOnMonsterEvent received = null;
            Action<PlayerSteppedOnMonsterEvent> handler = (e) => { received = e; };
            EventBus.Subscribe<PlayerSteppedOnMonsterEvent>(handler);

            // Record previous position
            _sut.OnPlayerMoveStarted(new Vector2Int(3, 7));

            // Simulate player arriving at monster cell
            EventBus.Publish(new PlayerMoveCompletedEvent { Position = new Vector2Int(3, 6) });

            EventBus.Unsubscribe<PlayerSteppedOnMonsterEvent>(handler);

            Assert.IsNotNull(received);
            Assert.AreEqual(new Vector2Int(3, 6), received.MonsterPosition);
            Assert.AreEqual(new Vector2Int(3, 7), received.ReturnPosition);
            Assert.Greater(received.DamageDealt, 0);
        }

        [Test]
        public void PlayerMoveCompleted_NoMonster_NoDamage()
        {
            int hpBefore = _playerStats.CurrentHp;

            _sut.OnPlayerMoveStarted(new Vector2Int(3, 7));
            EventBus.Publish(new PlayerMoveCompletedEvent { Position = new Vector2Int(3, 6) });

            Assert.AreEqual(hpBefore, _playerStats.CurrentHp);
        }

        [Test]
        public void Dispose_UnsubscribesAllEvents()
        {
            _sut.Dispose();

            // After dispose, events should not cause damage
            int hpBefore = _playerStats.CurrentHp;

            _monsterManager.SpawnMonster(CreateSlimeType(), new Vector2Int(3, 6));
            EventBus.Publish(new PlayerMoveCompletedEvent { Position = new Vector2Int(3, 6) });

            Assert.AreEqual(hpBefore, _playerStats.CurrentHp);

            _sut = null; // prevent double dispose in TearDown
        }
    }
}
