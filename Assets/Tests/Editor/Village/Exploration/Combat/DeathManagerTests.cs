using System;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.Exploration;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.Exploration.Combat;
using ProjectDR.Village.Exploration.Core;

namespace ProjectDR.Tests.Village.Exploration.Combat
{
    /// <summary>
    /// DeathManager 單元測試。
    /// 測試對象：死亡偵測、背包回溯、事件發布、重複觸發防護、Dispose 清理。
    /// </summary>
    [TestFixture]
    public class DeathManagerTests
    {
        private const int TestMaxSlots = 10;
        private const int TestMaxStack = 99;

        private BackpackManager _backpackManager;
        private ExplorationEntryManager _explorationEntryManager;
        private DeathManager _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _backpackManager = new BackpackManager(TestMaxSlots, TestMaxStack);
            _explorationEntryManager = new ExplorationEntryManager(_backpackManager);
            _sut = new DeathManager(_backpackManager, _explorationEntryManager);
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
            EventBus.ForceClearAll();
        }

        // ===== 初始狀態 =====

        [Test]
        public void InitialState_IsAlive()
        {
            Assert.IsFalse(_sut.IsDead);
        }

        // ===== 死亡觸發 =====

        [Test]
        public void OnPlayerDied_SetsIsDeadTrue()
        {
            EventBus.Publish(new PlayerDiedEvent());

            Assert.IsTrue(_sut.IsDead);
        }

        [Test]
        public void OnPlayerDied_PublishesPlayerDeathEvent()
        {
            bool eventReceived = false;
            Action<PlayerDeathEvent> handler = (e) => { eventReceived = true; };
            EventBus.Subscribe<PlayerDeathEvent>(handler);

            EventBus.Publish(new PlayerDiedEvent());

            EventBus.Unsubscribe<PlayerDeathEvent>(handler);
            Assert.IsTrue(eventReceived);
        }

        [Test]
        public void OnPlayerDied_RestoresBackpackToSnapshot()
        {
            // 出發前背包有 Wood x3
            _backpackManager.AddItem("Wood", 3);
            _explorationEntryManager.Depart();

            // 探索中拾取 Stone x2
            _backpackManager.AddItem("Stone", 2);

            // 死亡觸發
            EventBus.Publish(new PlayerDiedEvent());

            // 背包應回溯至出發前狀態
            Assert.AreEqual(3, _backpackManager.GetItemCount("Wood"));
            Assert.AreEqual(0, _backpackManager.GetItemCount("Stone"));
        }

        [Test]
        public void OnPlayerDied_WhenNoSnapshot_DoesNotThrow()
        {
            // 沒有出發過（無快照），死亡不應拋出例外
            Assert.DoesNotThrow(() => EventBus.Publish(new PlayerDiedEvent()));
            Assert.IsTrue(_sut.IsDead);
        }

        [Test]
        public void OnPlayerDied_PublishesExplorationCompletedEvent()
        {
            bool eventReceived = false;
            Action<ExplorationCompletedEvent> handler = (e) => { eventReceived = true; };
            EventBus.Subscribe<ExplorationCompletedEvent>(handler);

            EventBus.Publish(new PlayerDiedEvent());

            EventBus.Unsubscribe<ExplorationCompletedEvent>(handler);
            Assert.IsTrue(eventReceived);
        }

        // ===== 重複觸發防護 =====

        [Test]
        public void OnPlayerDied_SecondTime_DoesNotPublishAgain()
        {
            EventBus.Publish(new PlayerDiedEvent());

            int deathEventCount = 0;
            Action<PlayerDeathEvent> handler = (e) => { deathEventCount++; };
            EventBus.Subscribe<PlayerDeathEvent>(handler);

            // 第二次死亡事件
            EventBus.Publish(new PlayerDiedEvent());

            EventBus.Unsubscribe<PlayerDeathEvent>(handler);
            Assert.AreEqual(0, deathEventCount);
        }

        [Test]
        public void OnPlayerDied_SecondTime_DoesNotRestoreBackpackAgain()
        {
            _backpackManager.AddItem("Wood", 3);
            _explorationEntryManager.Depart();
            _backpackManager.AddItem("Stone", 2);

            // 第一次死亡 — 回溯
            EventBus.Publish(new PlayerDiedEvent());
            Assert.AreEqual(0, _backpackManager.GetItemCount("Stone"));

            // 手動改背包
            _backpackManager.AddItem("Iron", 1);

            // 第二次死亡 — 不應再回溯
            EventBus.Publish(new PlayerDiedEvent());
            Assert.AreEqual(1, _backpackManager.GetItemCount("Iron"));
        }

        // ===== 事件順序 =====

        [Test]
        public void OnPlayerDied_BackpackRestoredBeforeDeathEventPublished()
        {
            // 背包回溯應在 PlayerDeathEvent 發布前完成
            _backpackManager.AddItem("Wood", 3);
            _explorationEntryManager.Depart();
            _backpackManager.AddItem("Stone", 2);

            int woodCountAtDeathEvent = -1;
            int stoneCountAtDeathEvent = -1;
            Action<PlayerDeathEvent> handler = (e) =>
            {
                woodCountAtDeathEvent = _backpackManager.GetItemCount("Wood");
                stoneCountAtDeathEvent = _backpackManager.GetItemCount("Stone");
            };
            EventBus.Subscribe<PlayerDeathEvent>(handler);

            EventBus.Publish(new PlayerDiedEvent());

            EventBus.Unsubscribe<PlayerDeathEvent>(handler);
            Assert.AreEqual(3, woodCountAtDeathEvent);
            Assert.AreEqual(0, stoneCountAtDeathEvent);
        }

        [Test]
        public void OnPlayerDied_DeathEventPublishedBeforeExplorationCompleted()
        {
            // PlayerDeathEvent 應在 ExplorationCompletedEvent 之前發布
            int order = 0;
            int deathOrder = -1;
            int explorationOrder = -1;

            Action<PlayerDeathEvent> deathHandler = (e) => { deathOrder = order++; };
            Action<ExplorationCompletedEvent> explorationHandler = (e) => { explorationOrder = order++; };

            EventBus.Subscribe<PlayerDeathEvent>(deathHandler);
            EventBus.Subscribe<ExplorationCompletedEvent>(explorationHandler);

            EventBus.Publish(new PlayerDiedEvent());

            EventBus.Unsubscribe<PlayerDeathEvent>(deathHandler);
            EventBus.Unsubscribe<ExplorationCompletedEvent>(explorationHandler);

            Assert.Greater(explorationOrder, deathOrder);
        }

        // ===== Dispose =====

        [Test]
        public void Dispose_StopsListeningToPlayerDiedEvent()
        {
            _sut.Dispose();

            // Dispose 後死亡事件不應觸發 DeathManager
            EventBus.Publish(new PlayerDiedEvent());
            Assert.IsFalse(_sut.IsDead);
        }

        // ===== Reset =====

        [Test]
        public void Reset_ClearsDeadState()
        {
            EventBus.Publish(new PlayerDiedEvent());
            Assert.IsTrue(_sut.IsDead);

            _sut.Reset();

            Assert.IsFalse(_sut.IsDead);
        }

        [Test]
        public void Reset_AllowsDeathToTriggerAgain()
        {
            EventBus.Publish(new PlayerDiedEvent());
            _sut.Reset();

            bool eventReceived = false;
            Action<PlayerDeathEvent> handler = (e) => { eventReceived = true; };
            EventBus.Subscribe<PlayerDeathEvent>(handler);

            EventBus.Publish(new PlayerDiedEvent());

            EventBus.Unsubscribe<PlayerDeathEvent>(handler);
            Assert.IsTrue(eventReceived);
            Assert.IsTrue(_sut.IsDead);
        }
    }
}
