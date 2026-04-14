using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village.Exploration;

namespace ProjectDR.Tests.Village.Exploration
{
    /// <summary>
    /// EvacuationManager unit tests.
    /// Covers: constructor validation, trigger detection, evacuation start/cancel,
    /// countdown update, completion state locking, progress calculation, and event publishing.
    /// Pure logic tests with no MonoBehaviour or Unity scene dependency.
    /// </summary>
    [TestFixture]
    public class EvacuationManagerTests
    {
        // ===== Mock: IMonsterPositionProvider =====

        private class MockMonsterPositionProvider : IMonsterPositionProvider
        {
            public IReadOnlyList<Vector2Int> GetMonsterPositions()
            {
                return new List<Vector2Int>().AsReadOnly();
            }
        }

        // ===== Helper methods =====

        private static CellType[] CreateAllExplorableCells(int width, int height)
        {
            CellType[] cells = new CellType[width * height];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = CellType.Explorable;
            }
            return cells;
        }

        private GridMap _gridMap;
        private MapData _mapData;
        private EvacuationManager _sut;

        private const float DefaultDuration = 6f;

        // Default map: 5x5, spawn at (2,4), evacuation points at (0,0) and (4,0).
        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            CellType[] cells = CreateAllExplorableCells(5, 5);
            List<List<Vector2Int>> evacuationGroups = new List<List<Vector2Int>>
            {
                new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(4, 0) }
            };
            _mapData = new MapData(5, 5, cells, new Vector2Int(2, 4), evacuationGroups);
            _gridMap = new GridMap(_mapData, new MockMonsterPositionProvider());
            _gridMap.InitializeExplored(1, 0);

            _sut = new EvacuationManager(_gridMap, _mapData.SpawnPosition, DefaultDuration);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== Constructor =====

        [Test]
        public void Constructor_ValidParameters_CreatesManager()
        {
            Assert.IsFalse(_sut.IsEvacuating);
            Assert.IsFalse(_sut.IsCompleted);
            Assert.AreEqual(0f, _sut.RemainingTime);
        }

        [Test]
        public void Constructor_ZeroDuration_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EvacuationManager(_gridMap, _mapData.SpawnPosition, 0f));
        }

        [Test]
        public void Constructor_NegativeDuration_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EvacuationManager(_gridMap, _mapData.SpawnPosition, -1f));
        }

        // ===== IsEvacuationTrigger =====

        [Test]
        public void IsEvacuationTrigger_SpawnPosition_ReturnsTrue()
        {
            Assert.IsTrue(_sut.IsEvacuationTrigger(2, 4));
        }

        [Test]
        public void IsEvacuationTrigger_EvacuationPoint_ReturnsTrue()
        {
            Assert.IsTrue(_sut.IsEvacuationTrigger(0, 0));
            Assert.IsTrue(_sut.IsEvacuationTrigger(4, 0));
        }

        [Test]
        public void IsEvacuationTrigger_NormalCell_ReturnsFalse()
        {
            Assert.IsFalse(_sut.IsEvacuationTrigger(1, 1));
            Assert.IsFalse(_sut.IsEvacuationTrigger(3, 3));
        }

        // ===== OnPlayerArrived + evacuation start =====

        [Test]
        public void OnPlayerArrived_AtSpawnPoint_StartsEvacuation()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));

            Assert.IsTrue(_sut.IsEvacuating);
        }

        [Test]
        public void OnPlayerArrived_AtEvacuationPoint_StartsEvacuation()
        {
            _sut.OnPlayerArrived(new Vector2Int(0, 0));

            Assert.IsTrue(_sut.IsEvacuating);
        }

        [Test]
        public void OnPlayerArrived_AtNormalCell_DoesNotStartEvacuation()
        {
            _sut.OnPlayerArrived(new Vector2Int(1, 1));

            Assert.IsFalse(_sut.IsEvacuating);
        }

        [Test]
        public void OnPlayerArrived_AtTrigger_PublishesEvacuationStartedEvent()
        {
            EvacuationStartedEvent receivedEvent = null;
            Action<EvacuationStartedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<EvacuationStartedEvent>(handler);

            _sut.OnPlayerArrived(new Vector2Int(2, 4));

            EventBus.Unsubscribe<EvacuationStartedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(DefaultDuration, receivedEvent.Duration, 0.001f);
        }

        [Test]
        public void OnPlayerArrived_AtTrigger_SetsIsEvacuatingTrue()
        {
            _sut.OnPlayerArrived(new Vector2Int(0, 0));

            Assert.IsTrue(_sut.IsEvacuating);
            Assert.AreEqual(DefaultDuration, _sut.RemainingTime, 0.001f);
        }

        [Test]
        public void OnPlayerArrived_AlreadyEvacuating_DoesNotRestartTimer()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));
            _sut.Update(2f); // 4 seconds remaining

            int eventCount = 0;
            Action<EvacuationStartedEvent> handler = (e) => { eventCount++; };
            EventBus.Subscribe<EvacuationStartedEvent>(handler);

            _sut.OnPlayerArrived(new Vector2Int(2, 4));

            EventBus.Unsubscribe<EvacuationStartedEvent>(handler);

            Assert.AreEqual(0, eventCount);
            Assert.AreEqual(4f, _sut.RemainingTime, 0.001f);
        }

        [Test]
        public void OnPlayerArrived_AtNormalCell_DoesNotPublishEvent()
        {
            int eventCount = 0;
            Action<EvacuationStartedEvent> handler = (e) => { eventCount++; };
            EventBus.Subscribe<EvacuationStartedEvent>(handler);

            _sut.OnPlayerArrived(new Vector2Int(1, 1));

            EventBus.Unsubscribe<EvacuationStartedEvent>(handler);

            Assert.AreEqual(0, eventCount);
        }

        // ===== OnPlayerMoveStarted + evacuation cancel =====

        [Test]
        public void OnPlayerMoveStarted_WhileEvacuating_CancelsEvacuation()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));

            _sut.OnPlayerMoveStarted();

            Assert.IsFalse(_sut.IsEvacuating);
        }

        [Test]
        public void OnPlayerMoveStarted_WhileEvacuating_PublishesEvacuationCancelledEvent()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));

            EvacuationCancelledEvent receivedEvent = null;
            Action<EvacuationCancelledEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<EvacuationCancelledEvent>(handler);

            _sut.OnPlayerMoveStarted();

            EventBus.Unsubscribe<EvacuationCancelledEvent>(handler);

            Assert.IsNotNull(receivedEvent);
        }

        [Test]
        public void OnPlayerMoveStarted_WhileEvacuating_SetsIsEvacuatingFalse()
        {
            _sut.OnPlayerArrived(new Vector2Int(0, 0));

            _sut.OnPlayerMoveStarted();

            Assert.IsFalse(_sut.IsEvacuating);
            Assert.AreEqual(0f, _sut.RemainingTime);
        }

        [Test]
        public void OnPlayerMoveStarted_WhenNotEvacuating_DoesNothing()
        {
            _sut.OnPlayerMoveStarted();

            Assert.IsFalse(_sut.IsEvacuating);
        }

        [Test]
        public void OnPlayerMoveStarted_WhenNotEvacuating_DoesNotPublishEvent()
        {
            int eventCount = 0;
            Action<EvacuationCancelledEvent> handler = (e) => { eventCount++; };
            EventBus.Subscribe<EvacuationCancelledEvent>(handler);

            _sut.OnPlayerMoveStarted();

            EventBus.Unsubscribe<EvacuationCancelledEvent>(handler);

            Assert.AreEqual(0, eventCount);
        }

        // ===== Update + countdown completion =====

        [Test]
        public void Update_WhileEvacuating_DecreasesRemainingTime()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));

            _sut.Update(1f);

            Assert.AreEqual(5f, _sut.RemainingTime, 0.001f);
        }

        [Test]
        public void Update_CompletesCountdown_SetsIsCompletedTrue()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));

            _sut.Update(DefaultDuration);

            Assert.IsTrue(_sut.IsCompleted);
        }

        [Test]
        public void Update_CompletesCountdown_PublishesExplorationCompletedEvent()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));

            ExplorationCompletedEvent receivedEvent = null;
            Action<ExplorationCompletedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<ExplorationCompletedEvent>(handler);

            _sut.Update(DefaultDuration);

            EventBus.Unsubscribe<ExplorationCompletedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
        }

        [Test]
        public void Update_CompletesCountdown_SetsIsEvacuatingFalse()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));

            _sut.Update(DefaultDuration);

            Assert.IsFalse(_sut.IsEvacuating);
        }

        [Test]
        public void Update_WhenNotEvacuating_DoesNothing()
        {
            _sut.Update(10f);

            Assert.IsFalse(_sut.IsEvacuating);
            Assert.IsFalse(_sut.IsCompleted);
            Assert.AreEqual(0f, _sut.RemainingTime);
        }

        [Test]
        public void Update_WhenCompleted_DoesNothing()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));
            _sut.Update(DefaultDuration);

            int eventCount = 0;
            Action<ExplorationCompletedEvent> handler = (e) => { eventCount++; };
            EventBus.Subscribe<ExplorationCompletedEvent>(handler);

            _sut.Update(10f);

            EventBus.Unsubscribe<ExplorationCompletedEvent>(handler);

            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void Update_NegativeDeltaTime_DoesNotAdvanceTimer()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));

            _sut.Update(-1f);

            Assert.AreEqual(DefaultDuration, _sut.RemainingTime, 0.001f);
        }

        [Test]
        public void Update_ExactlyCompletesCountdown_Completes()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));

            _sut.Update(3f);
            _sut.Update(3f);

            Assert.IsTrue(_sut.IsCompleted);
        }

        [Test]
        public void Update_OvershootsCountdown_Completes()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));

            _sut.Update(100f);

            Assert.IsTrue(_sut.IsCompleted);
            Assert.AreEqual(0f, _sut.RemainingTime);
        }

        // ===== Completion state locking =====

        [Test]
        public void OnPlayerArrived_WhenCompleted_DoesNothing()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));
            _sut.Update(DefaultDuration);

            int eventCount = 0;
            Action<EvacuationStartedEvent> handler = (e) => { eventCount++; };
            EventBus.Subscribe<EvacuationStartedEvent>(handler);

            _sut.OnPlayerArrived(new Vector2Int(0, 0));

            EventBus.Unsubscribe<EvacuationStartedEvent>(handler);

            Assert.AreEqual(0, eventCount);
            Assert.IsFalse(_sut.IsEvacuating);
        }

        [Test]
        public void OnPlayerMoveStarted_WhenCompleted_DoesNothing()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));
            _sut.Update(DefaultDuration);

            int eventCount = 0;
            Action<EvacuationCancelledEvent> handler = (e) => { eventCount++; };
            EventBus.Subscribe<EvacuationCancelledEvent>(handler);

            _sut.OnPlayerMoveStarted();

            EventBus.Unsubscribe<EvacuationCancelledEvent>(handler);

            Assert.AreEqual(0, eventCount);
        }

        // ===== Progress =====

        [Test]
        public void Progress_NotEvacuating_ReturnsZero()
        {
            Assert.AreEqual(0f, _sut.Progress, 0.001f);
        }

        [Test]
        public void Progress_HalfwayThrough_ReturnsCorrectValue()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));

            _sut.Update(3f); // Half of 6 seconds

            Assert.AreEqual(0.5f, _sut.Progress, 0.001f);
        }

        [Test]
        public void Progress_Completed_ReturnsOne()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));
            _sut.Update(DefaultDuration);

            Assert.AreEqual(1f, _sut.Progress, 0.001f);
        }

        // ===== Cancel then restart =====

        [Test]
        public void OnPlayerArrived_AfterCancel_RestartsEvacuation()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));
            _sut.Update(2f);
            _sut.OnPlayerMoveStarted(); // cancel

            _sut.OnPlayerArrived(new Vector2Int(0, 0));

            Assert.IsTrue(_sut.IsEvacuating);
            Assert.AreEqual(DefaultDuration, _sut.RemainingTime, 0.001f);
        }

        [Test]
        public void Update_ZeroDeltaTime_DoesNotAdvanceTimer()
        {
            _sut.OnPlayerArrived(new Vector2Int(2, 4));

            _sut.Update(0f);

            Assert.AreEqual(DefaultDuration, _sut.RemainingTime, 0.001f);
        }
    }
}
