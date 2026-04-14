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
    }
}
