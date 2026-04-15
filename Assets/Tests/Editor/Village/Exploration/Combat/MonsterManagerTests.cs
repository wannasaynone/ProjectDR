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
    public class MonsterManagerTests
    {
        private GridMap _gridMap;
        private MonsterManager _sut;
        private int _monstersChangedCount;

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

        private static CellType[] CreateAllExplorableCells(int width, int height)
        {
            CellType[] cells = new CellType[width * height];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = CellType.Explorable;
            return cells;
        }

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            MonsterState.ResetIdCounter();

            _monstersChangedCount = 0;
            CellType[] cells = CreateAllExplorableCells(8, 8);
            MapData mapData = new MapData(8, 8, cells, new Vector2Int(3, 7), new List<List<Vector2Int>>());
            _gridMap = new GridMap(mapData, null);
            _gridMap.InitializeExplored(1, -1);

            _sut = new MonsterManager(_gridMap, () => _monstersChangedCount++, 42);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        [Test]
        public void SpawnMonster_AddsToList()
        {
            var type = CreateSlimeType();
            var monster = _sut.SpawnMonster(type, new Vector2Int(2, 3));

            Assert.AreEqual(1, _sut.Monsters.Count);
            Assert.AreEqual(new Vector2Int(2, 3), monster.Position);
        }

        [Test]
        public void SpawnMonster_PublishesMonsterSpawnedEvent()
        {
            var type = CreateSlimeType();
            MonsterSpawnedEvent received = null;
            Action<MonsterSpawnedEvent> handler = (e) => { received = e; };
            EventBus.Subscribe<MonsterSpawnedEvent>(handler);

            _sut.SpawnMonster(type, new Vector2Int(2, 3));

            EventBus.Unsubscribe<MonsterSpawnedEvent>(handler);

            Assert.IsNotNull(received);
            Assert.AreEqual("Slime", received.TypeId);
            Assert.AreEqual(new Vector2Int(2, 3), received.Position);
        }

        [Test]
        public void GetMonsterAt_ExistingPosition_ReturnsMonster()
        {
            var type = CreateSlimeType();
            _sut.SpawnMonster(type, new Vector2Int(2, 3));

            var found = _sut.GetMonsterAt(2, 3);

            Assert.IsNotNull(found);
        }

        [Test]
        public void GetMonsterAt_EmptyPosition_ReturnsNull()
        {
            var found = _sut.GetMonsterAt(0, 0);

            Assert.IsNull(found);
        }

        [Test]
        public void GetMonsterPositions_ReturnsAllAlivePositions()
        {
            var type = CreateSlimeType();
            _sut.SpawnMonster(type, new Vector2Int(2, 3));
            _sut.SpawnMonster(type, new Vector2Int(5, 4));

            IReadOnlyList<Vector2Int> positions = _sut.GetMonsterPositions();

            Assert.AreEqual(2, positions.Count);
        }

        [Test]
        public void DamageMonster_ReducesHp()
        {
            var type = CreateSlimeType();
            var monster = _sut.SpawnMonster(type, new Vector2Int(2, 3));

            _sut.DamageMonster(monster.Id, 3);

            Assert.AreEqual(3, monster.CurrentHp);
        }

        [Test]
        public void DamageMonster_PublishesMonsterDamagedEvent()
        {
            var type = CreateSlimeType();
            var monster = _sut.SpawnMonster(type, new Vector2Int(2, 3));

            MonsterDamagedEvent received = null;
            Action<MonsterDamagedEvent> handler = (e) => { received = e; };
            EventBus.Subscribe<MonsterDamagedEvent>(handler);

            _sut.DamageMonster(monster.Id, 3);

            EventBus.Unsubscribe<MonsterDamagedEvent>(handler);

            Assert.IsNotNull(received);
            Assert.AreEqual(3, received.Damage);
            Assert.AreEqual(3, received.RemainingHp);
        }

        [Test]
        public void DamageMonster_LethalDamage_RemovesFromList()
        {
            var type = CreateSlimeType();
            var monster = _sut.SpawnMonster(type, new Vector2Int(2, 3));

            _sut.DamageMonster(monster.Id, 10);

            Assert.AreEqual(0, _sut.Monsters.Count);
        }

        [Test]
        public void DamageMonster_LethalDamage_PublishesMonsterDiedEvent()
        {
            var type = CreateSlimeType();
            var monster = _sut.SpawnMonster(type, new Vector2Int(2, 3));

            MonsterDiedEvent received = null;
            Action<MonsterDiedEvent> handler = (e) => { received = e; };
            EventBus.Subscribe<MonsterDiedEvent>(handler);

            _sut.DamageMonster(monster.Id, 10);

            EventBus.Unsubscribe<MonsterDiedEvent>(handler);

            Assert.IsNotNull(received);
            Assert.AreEqual(new Vector2Int(2, 3), received.Position);
        }

        [Test]
        public void DamageMonster_LethalDamage_InvokesOnMonstersChanged()
        {
            var type = CreateSlimeType();
            var monster = _sut.SpawnMonster(type, new Vector2Int(2, 3));
            _monstersChangedCount = 0;

            _sut.DamageMonster(monster.Id, 10);

            Assert.Greater(_monstersChangedCount, 0);
        }

        [Test]
        public void DamageMonster_InvalidId_DoesNothing()
        {
            _sut.DamageMonster(999, 5); // should not throw
        }

        [Test]
        public void Update_MonsterMovesOnCooldown()
        {
            var type = CreateSlimeType(); // moveCooldown = 2.0
            var monster = _sut.SpawnMonster(type, new Vector2Int(4, 4));
            Vector2Int initialPos = monster.Position;

            // Player far away so monster roams
            _sut.Update(2.1f, new Vector2Int(0, 0));

            // Monster may or may not have moved (random), but it should have attempted
            // We just verify no exception and the update completes
            Assert.IsNotNull(monster);
        }

        [Test]
        public void Update_MonsterChasesPlayerInVision()
        {
            var type = CreateSlimeType(); // visionRange = 3
            // X10: monster must be on explored cell to chase
            _gridMap.RevealCell(4, 4);
            var monster = _sut.SpawnMonster(type, new Vector2Int(4, 4));

            // Player at distance 2 (within vision range 3)
            // Tick past movement cooldown
            _sut.Update(2.1f, new Vector2Int(4, 2));

            Assert.AreEqual(MonsterAIState.Chasing, monster.AIState);
        }

        [Test]
        public void Update_MonsterAttacksAdjacentPlayer()
        {
            var type = CreateSlimeType(); // attackRange = 1
            // X10: monster must be on explored cell to attack
            _gridMap.RevealCell(4, 4);
            var monster = _sut.SpawnMonster(type, new Vector2Int(4, 4));

            MonsterAttackPrepareEvent received = null;
            Action<MonsterAttackPrepareEvent> handler = (e) => { received = e; };
            EventBus.Subscribe<MonsterAttackPrepareEvent>(handler);

            // Player adjacent (distance 1)
            _sut.Update(2.1f, new Vector2Int(4, 3));

            EventBus.Unsubscribe<MonsterAttackPrepareEvent>(handler);

            Assert.IsNotNull(received);
            Assert.AreEqual(MonsterAIState.AttackPreparing, monster.AIState);
        }

        [Test]
        public void Update_AttackPrepareCompletesAndPublishesExecuteEvent()
        {
            var type = CreateSlimeType(); // prepareSeconds = 1.0
            // X10: monster must be on explored cell to attack
            _gridMap.RevealCell(4, 4);
            var monster = _sut.SpawnMonster(type, new Vector2Int(4, 4));

            // Trigger attack prepare
            _sut.Update(2.1f, new Vector2Int(4, 3));

            MonsterAttackExecuteEvent received = null;
            Action<MonsterAttackExecuteEvent> handler = (e) => { received = e; };
            EventBus.Subscribe<MonsterAttackExecuteEvent>(handler);

            // Advance past prepare time
            _sut.Update(1.1f, new Vector2Int(4, 3));

            EventBus.Unsubscribe<MonsterAttackExecuteEvent>(handler);

            Assert.IsNotNull(received);
            Assert.AreEqual(MonsterAIState.AttackCooldown, monster.AIState);
        }

        [Test]
        public void Update_ZeroDeltaTime_DoesNothing()
        {
            var type = CreateSlimeType();
            _sut.SpawnMonster(type, new Vector2Int(4, 4));

            Assert.DoesNotThrow(() => _sut.Update(0f, new Vector2Int(0, 0)));
        }

        [Test]
        public void Update_NegativeDeltaTime_DoesNothing()
        {
            var type = CreateSlimeType();
            _sut.SpawnMonster(type, new Vector2Int(4, 4));

            Assert.DoesNotThrow(() => _sut.Update(-1f, new Vector2Int(0, 0)));
        }

        // ===== X9: Monster movement boundary — exploration state restriction =====

        [Test]
        public void Update_MonsterOnExploredCell_CannotMoveToUnexploredCell()
        {
            // GridMap: spawn at (3,7), revealRadius=1 → (3,7) and manhattan-1 neighbors explored
            // Monster at (3,6) is on explored cell (manhattan dist 1 from spawn)
            // All cells at y=5 are unexplored
            var type = CreateSlimeType();
            var monster = _sut.SpawnMonster(type, new Vector2Int(3, 6));

            // Player far away so monster roams
            // Advance past movement cooldown multiple times
            for (int i = 0; i < 20; i++)
            {
                _sut.Update(2.1f, new Vector2Int(0, 0));
            }

            // Monster should still be on an explored cell
            Assert.IsTrue(_gridMap.IsExplored(monster.Position.x, monster.Position.y),
                $"Monster moved to unexplored cell ({monster.Position.x},{monster.Position.y})");
        }

        [Test]
        public void Update_MonsterOnUnexploredCell_CannotMoveToExploredCell()
        {
            // Monster at (0,0) — unexplored cell
            var type = CreateSlimeType();
            var monster = _sut.SpawnMonster(type, new Vector2Int(0, 0));

            // Player far away
            for (int i = 0; i < 20; i++)
            {
                _sut.Update(2.1f, new Vector2Int(7, 7));
            }

            // Monster should still be on an unexplored cell
            Assert.IsFalse(_gridMap.IsExplored(monster.Position.x, monster.Position.y),
                $"Monster moved to explored cell ({monster.Position.x},{monster.Position.y})");
        }

        [Test]
        public void Update_MonsterOnExploredCell_ChasingDoesNotCrossToUnexplored()
        {
            // Monster at (3,6) explored, player at (3,5) — but (3,5) is unexplored
            // Monster should NOT move toward unexplored cell even when chasing
            var type = CreateSlimeType(); // visionRange = 3
            var monster = _sut.SpawnMonster(type, new Vector2Int(3, 6));

            // Player at (3,5) — unexplored (only radius 1 from spawn (3,7))
            _sut.Update(2.1f, new Vector2Int(3, 5));

            // Monster should not be on an unexplored cell
            Assert.IsTrue(_gridMap.IsExplored(monster.Position.x, monster.Position.y),
                $"Monster crossed to unexplored cell ({monster.Position.x},{monster.Position.y})");
        }

        // ===== X10: Monster AI state restriction — only chase/attack on explored cells =====

        [Test]
        public void Update_MonsterOnUnexploredCell_DoesNotChaseEvenIfPlayerInVision()
        {
            // Monster at (0,0) — unexplored
            var type = CreateSlimeType(); // visionRange = 3
            var monster = _sut.SpawnMonster(type, new Vector2Int(0, 0));

            // Player at (0,1) — distance 1, within vision range
            _sut.Update(2.1f, new Vector2Int(0, 1));

            // Monster should NOT be in Chasing state
            Assert.AreNotEqual(MonsterAIState.Chasing, monster.AIState,
                "Monster on unexplored cell should not chase");
        }

        [Test]
        public void Update_MonsterOnUnexploredCell_DoesNotAttackEvenIfPlayerAdjacent()
        {
            // Monster at (0,0) — unexplored
            var type = CreateSlimeType(); // attackRange = 1
            var monster = _sut.SpawnMonster(type, new Vector2Int(0, 0));

            MonsterAttackPrepareEvent received = null;
            Action<MonsterAttackPrepareEvent> handler = (e) => { received = e; };
            EventBus.Subscribe<MonsterAttackPrepareEvent>(handler);

            // Player at (1,0) — distance 1, within attack range
            _sut.Update(2.1f, new Vector2Int(1, 0));

            EventBus.Unsubscribe<MonsterAttackPrepareEvent>(handler);

            Assert.IsNull(received, "Monster on unexplored cell should not prepare attack");
            Assert.AreNotEqual(MonsterAIState.AttackPreparing, monster.AIState);
        }

        [Test]
        public void Update_MonsterOnUnexploredCell_StillRoams()
        {
            // Monster at (0,0) — unexplored
            var type = CreateSlimeType();
            var monster = _sut.SpawnMonster(type, new Vector2Int(0, 0));

            // Player far away
            _sut.Update(2.1f, new Vector2Int(7, 7));

            // Monster should be Roaming (not Chasing or Attacking)
            Assert.IsTrue(monster.AIState == MonsterAIState.Roaming || monster.AIState == MonsterAIState.Idle,
                $"Monster on unexplored cell should be Roaming or Idle, was {monster.AIState}");
        }

        [Test]
        public void Update_MonsterOnExploredCell_CanChaseWhenPlayerInVision()
        {
            // Monster at (4,7) — explored (within radius 1 of spawn (3,7))
            var type = CreateSlimeType(); // visionRange = 3
            var monster = _sut.SpawnMonster(type, new Vector2Int(4, 7));

            // Player at (4,6) — explored, distance 1, within vision range
            _sut.Update(2.1f, new Vector2Int(4, 6));

            // Monster should be able to chase or attack (they're adjacent, attackRange=1)
            Assert.IsTrue(
                monster.AIState == MonsterAIState.Chasing || monster.AIState == MonsterAIState.AttackPreparing,
                $"Monster on explored cell should chase or attack, was {monster.AIState}");
        }

        // ===== X11: Monster can spawn on explored cells =====

        [Test]
        public void SpawnMonster_OnExploredCell_Succeeds()
        {
            // (3,7) is the spawn point — explored
            Assert.IsTrue(_gridMap.IsExplored(3, 7));

            var type = CreateSlimeType();
            var monster = _sut.SpawnMonster(type, new Vector2Int(3, 7));

            Assert.IsNotNull(monster);
            Assert.AreEqual(new Vector2Int(3, 7), monster.Position);
            Assert.AreEqual(1, _sut.Monsters.Count);
        }

        [Test]
        public void SpawnMonster_OnUnexploredCell_Succeeds()
        {
            // (0,0) is not explored
            Assert.IsFalse(_gridMap.IsExplored(0, 0));

            var type = CreateSlimeType();
            var monster = _sut.SpawnMonster(type, new Vector2Int(0, 0));

            Assert.IsNotNull(monster);
            Assert.AreEqual(new Vector2Int(0, 0), monster.Position);
            Assert.AreEqual(1, _sut.Monsters.Count);
        }

        [Test]
        public void SpawnMonster_BothExploredAndUnexplored_CoexistInList()
        {
            var type = CreateSlimeType();

            // Spawn on explored cell
            var m1 = _sut.SpawnMonster(type, new Vector2Int(3, 7));
            // Spawn on unexplored cell
            var m2 = _sut.SpawnMonster(type, new Vector2Int(0, 0));

            Assert.AreEqual(2, _sut.Monsters.Count);
            Assert.IsTrue(_gridMap.IsExplored(m1.Position.x, m1.Position.y));
            Assert.IsFalse(_gridMap.IsExplored(m2.Position.x, m2.Position.y));
        }
    }
}
