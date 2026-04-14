using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// ExplorationEntryManager 的單元測試。
    /// 測試對象：出發可行性檢查、出發、模擬返回、背包快照、事件發布。
    /// V2：戰利品進入背包而非直接進倉庫；出發時自動拍攝背包快照。
    /// 此測試不依賴 MonoBehaviour 或 Unity 場景，為純邏輯測試。
    /// </summary>
    [TestFixture]
    public class ExplorationEntryManagerTests
    {
        private const int TestMaxSlots = 10;
        private const int TestMaxStack = 99;

        private BackpackManager _backpackManager;
        private ExplorationEntryManager _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _backpackManager = new BackpackManager(TestMaxSlots, TestMaxStack);
            _sut = new ExplorationEntryManager(_backpackManager);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== CanDepart =====

        [Test]
        public void CanDepart_OnInitialState_ReturnsTrue()
        {
            // 初始狀態下（未在探索中），應可出發
            bool result = _sut.CanDepart();

            Assert.IsTrue(result);
        }

        [Test]
        public void CanDepart_WhenAlreadyExploring_ReturnsFalse()
        {
            // 已在探索中時，不可再次出發
            _sut.Depart();

            bool result = _sut.CanDepart();

            Assert.IsFalse(result);
        }

        [Test]
        public void CanDepart_AfterReturn_ReturnsTrue()
        {
            // 返回後，應可再次出發
            _sut.Depart();
            _sut.SimulateReturn(new Dictionary<string, int>());

            bool result = _sut.CanDepart();

            Assert.IsTrue(result);
        }

        // ===== Depart =====

        [Test]
        public void Depart_WhenCanDepart_ReturnsTrue()
        {
            // 可出發時，Depart 應回傳 true
            bool result = _sut.Depart();

            Assert.IsTrue(result);
        }

        [Test]
        public void Depart_WhenAlreadyExploring_ReturnsFalse()
        {
            // 已在探索中時，重複出發應回傳 false
            _sut.Depart();

            bool result = _sut.Depart();

            Assert.IsFalse(result);
        }

        [Test]
        public void Depart_WhenAlreadyExploring_DoesNotChangeState()
        {
            // 重複出發失敗後，狀態不應改變（仍處於探索中）
            _sut.Depart();
            _sut.Depart();

            // 狀態仍為探索中，CanDepart 應為 false
            Assert.IsFalse(_sut.CanDepart());
        }

        // ===== SimulateReturn =====

        [Test]
        public void SimulateReturn_WithLoot_AddLootToBackpack()
        {
            // 模擬帶著戰利品返回，應將物品新增至背包
            _sut.Depart();

            Dictionary<string, int> loot = new Dictionary<string, int>
            {
                { "Wood", 5 },
                { "Stone", 3 }
            };

            _sut.SimulateReturn(loot);

            Assert.AreEqual(5, _backpackManager.GetItemCount("Wood"));
            Assert.AreEqual(3, _backpackManager.GetItemCount("Stone"));
        }

        [Test]
        public void SimulateReturn_WithEmptyLoot_DoesNotAddItems()
        {
            // 空戰利品返回，背包不應有任何物品
            _sut.Depart();
            _sut.SimulateReturn(new Dictionary<string, int>());

            Assert.IsTrue(_backpackManager.IsEmpty);
        }

        [Test]
        public void SimulateReturn_WhenNotExploring_DoesNotAddItems()
        {
            // 未在探索中呼叫 SimulateReturn，不應新增物品
            Dictionary<string, int> loot = new Dictionary<string, int>
            {
                { "Wood", 5 }
            };

            _sut.SimulateReturn(loot);

            Assert.AreEqual(0, _backpackManager.GetItemCount("Wood"));
        }

        [Test]
        public void SimulateReturn_AfterReturn_StateIsNotExploring()
        {
            // 返回後，應恢復為可出發狀態
            _sut.Depart();
            _sut.SimulateReturn(new Dictionary<string, int>());

            Assert.IsTrue(_sut.CanDepart());
        }

        // ===== 背包快照 =====

        [Test]
        public void Depart_TakesBackpackSnapshot()
        {
            // 出發時應拍攝背包快照
            _backpackManager.AddItem("Wood", 3);
            _sut.Depart();

            BackpackSnapshot snapshot = _sut.GetDepartureSnapshot();

            Assert.IsNotNull(snapshot);
            IReadOnlyList<BackpackSlot> slots = snapshot.GetSlots();
            Assert.AreEqual("Wood", slots[0].ItemId);
            Assert.AreEqual(3, slots[0].Quantity);
        }

        [Test]
        public void GetDepartureSnapshot_BeforeDepart_ReturnsNull()
        {
            // 尚未出發時，快照應為 null
            BackpackSnapshot snapshot = _sut.GetDepartureSnapshot();

            Assert.IsNull(snapshot);
        }

        [Test]
        public void DepartureSnapshot_IsIndependentOfLaterChanges()
        {
            // 出發後修改背包，快照不應受影響
            _backpackManager.AddItem("Wood", 3);
            _sut.Depart();

            // 探索中獲得新物品（透過直接操作背包模擬）
            _backpackManager.AddItem("Stone", 2);

            BackpackSnapshot snapshot = _sut.GetDepartureSnapshot();
            IReadOnlyList<BackpackSlot> slots = snapshot.GetSlots();
            Assert.AreEqual(3, slots[0].Quantity); // Wood 仍是 3
            Assert.IsTrue(slots[1].IsEmpty); // 快照中沒有 Stone
        }

        [Test]
        public void DeathRecovery_RestoreSnapshot_RestoresBackpackToPreDepartState()
        {
            // 模擬死亡回溯：出發前背包有 Wood x3，探索中獲得 Stone x2，死亡後回溯
            _backpackManager.AddItem("Wood", 3);
            _sut.Depart();

            // 模擬探索中獲得物品
            Dictionary<string, int> loot = new Dictionary<string, int> { { "Stone", 2 } };
            _sut.SimulateReturn(loot);

            // 死亡回溯
            BackpackSnapshot snapshot = _sut.GetDepartureSnapshot();
            _backpackManager.RestoreSnapshot(snapshot);

            Assert.AreEqual(3, _backpackManager.GetItemCount("Wood"));
            Assert.AreEqual(0, _backpackManager.GetItemCount("Stone"));
        }

        // ===== ExplorationDepartedEvent 事件 =====

        [Test]
        public void Depart_Success_PublishesExplorationDepartedEvent()
        {
            // 成功出發應發布 ExplorationDepartedEvent
            bool eventPublished = false;
            Action<ExplorationDepartedEvent> handler = (e) => { eventPublished = true; };
            EventBus.Subscribe<ExplorationDepartedEvent>(handler);

            _sut.Depart();

            EventBus.Unsubscribe<ExplorationDepartedEvent>(handler);

            Assert.IsTrue(eventPublished);
        }

        [Test]
        public void Depart_Failure_DoesNotPublishExplorationDepartedEvent()
        {
            // 出發失敗時不應發布事件
            _sut.Depart();

            bool eventPublished = false;
            Action<ExplorationDepartedEvent> handler = (e) => { eventPublished = true; };
            EventBus.Subscribe<ExplorationDepartedEvent>(handler);

            _sut.Depart();

            EventBus.Unsubscribe<ExplorationDepartedEvent>(handler);

            Assert.IsFalse(eventPublished);
        }

        // ===== ExplorationReturnedEvent 事件 =====

        [Test]
        public void SimulateReturn_WhenExploring_PublishesExplorationReturnedEvent()
        {
            // 從探索中返回應發布 ExplorationReturnedEvent
            _sut.Depart();

            EventBus.ForceClearAll();

            bool eventPublished = false;
            Action<ExplorationReturnedEvent> handler = (e) => { eventPublished = true; };
            EventBus.Subscribe<ExplorationReturnedEvent>(handler);

            _sut.SimulateReturn(new Dictionary<string, int>());

            EventBus.Unsubscribe<ExplorationReturnedEvent>(handler);

            Assert.IsTrue(eventPublished);
        }

        [Test]
        public void SimulateReturn_WhenExploring_EventContainsLoot()
        {
            // 返回事件應包含戰利品資料
            _sut.Depart();

            EventBus.ForceClearAll();

            Dictionary<string, int> loot = new Dictionary<string, int>
            {
                { "Wood", 5 }
            };

            ExplorationReturnedEvent receivedEvent = null;
            Action<ExplorationReturnedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<ExplorationReturnedEvent>(handler);

            _sut.SimulateReturn(loot);

            EventBus.Unsubscribe<ExplorationReturnedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
            Assert.IsNotNull(receivedEvent.Loot);
            Assert.AreEqual(5, receivedEvent.Loot["Wood"]);
        }

        [Test]
        public void SimulateReturn_WhenNotExploring_DoesNotPublishExplorationReturnedEvent()
        {
            // 未在探索中呼叫 SimulateReturn，不應發布事件
            bool eventPublished = false;
            Action<ExplorationReturnedEvent> handler = (e) => { eventPublished = true; };
            EventBus.Subscribe<ExplorationReturnedEvent>(handler);

            _sut.SimulateReturn(new Dictionary<string, int>());

            EventBus.Unsubscribe<ExplorationReturnedEvent>(handler);

            Assert.IsFalse(eventPublished);
        }
    }
}
