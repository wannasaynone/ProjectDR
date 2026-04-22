using System;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Storage;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// QuestManager 的單元測試。
    /// 測試對象：取得進行中任務、接受任務、完成任務、查詢完成狀態、事件發布。
    /// 此測試不依賴 MonoBehaviour 或 Unity 場景，為純邏輯測試。
    /// QuestManager 透過建構函式接收 StorageManager，以便驗證完成條件。
    /// </summary>
    [TestFixture]
    public class QuestManagerTests
    {
        private StorageManager _storageManager;
        private QuestManager _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _storageManager = new StorageManager();
            _sut = new QuestManager(_storageManager);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== GetActiveQuest =====

        [Test]
        public void GetActiveQuest_OnInitialState_ReturnsNull()
        {
            // 初始狀態下沒有進行中任務
            QuestData activeQuest = _sut.GetActiveQuest();

            Assert.IsNull(activeQuest);
        }

        [Test]
        public void GetActiveQuest_AfterAcceptQuest_ReturnsAcceptedQuest()
        {
            // 接受任務後，GetActiveQuest 應回傳該任務
            _sut.AcceptQuest("Quest_GatherWood");

            QuestData activeQuest = _sut.GetActiveQuest();

            Assert.IsNotNull(activeQuest);
            Assert.AreEqual("Quest_GatherWood", activeQuest.QuestId);
        }

        // ===== AcceptQuest =====

        [Test]
        public void AcceptQuest_WhenNoActiveQuest_ReturnsTrue()
        {
            // 沒有進行中任務時接受任務應成功
            bool result = _sut.AcceptQuest("Quest_GatherWood");

            Assert.IsTrue(result);
        }

        [Test]
        public void AcceptQuest_WhenHasActiveQuest_ReturnsFalse()
        {
            // 已有進行中任務時，不可再接新任務
            _sut.AcceptQuest("Quest_GatherWood");

            bool result = _sut.AcceptQuest("Quest_HuntAnimal");

            Assert.IsFalse(result);
        }

        [Test]
        public void AcceptQuest_WhenHasActiveQuest_ActiveQuestRemainsUnchanged()
        {
            // 接受第二個任務失敗後，進行中任務應保持不變
            _sut.AcceptQuest("Quest_GatherWood");
            _sut.AcceptQuest("Quest_HuntAnimal");

            QuestData activeQuest = _sut.GetActiveQuest();

            Assert.AreEqual("Quest_GatherWood", activeQuest.QuestId);
        }

        [Test]
        public void AcceptQuest_NonExistentQuestId_ReturnsFalse()
        {
            // 接受不存在的任務 ID 應回傳 false
            bool result = _sut.AcceptQuest("Quest_ThisDoesNotExist");

            Assert.IsFalse(result);
        }

        // ===== TryCompleteActiveQuest =====

        [Test]
        public void TryCompleteActiveQuest_WhenNoActiveQuest_ReturnsFalse()
        {
            // 沒有進行中任務時，完成應回傳 false
            bool result = _sut.TryCompleteActiveQuest();

            Assert.IsFalse(result);
        }

        [Test]
        public void TryCompleteActiveQuest_WhenConditionsNotMet_ReturnsFalse()
        {
            // 條件未滿足時，不可完成任務
            _sut.AcceptQuest("Quest_GatherWood");

            // 故意不提供任何木材，讓條件不滿足
            bool result = _sut.TryCompleteActiveQuest();

            Assert.IsFalse(result);
        }

        [Test]
        public void TryCompleteActiveQuest_WhenConditionsNotMet_ActiveQuestRemainsActive()
        {
            // 條件未滿足時，進行中任務仍應維持
            _sut.AcceptQuest("Quest_GatherWood");
            _sut.TryCompleteActiveQuest();

            Assert.IsNotNull(_sut.GetActiveQuest());
        }

        [Test]
        public void TryCompleteActiveQuest_WhenConditionsMet_ReturnsTrue()
        {
            // 滿足完成條件時，應回傳 true
            _sut.AcceptQuest("Quest_GatherWood");

            // 準備任務所需資源（依照 Quest 定義，假設需要 10 Wood）
            _storageManager.AddItem("Wood", 10);

            bool result = _sut.TryCompleteActiveQuest();

            Assert.IsTrue(result);
        }

        [Test]
        public void TryCompleteActiveQuest_WhenConditionsMet_ActiveQuestBecomesNull()
        {
            // 完成任務後，進行中任務應清除
            _sut.AcceptQuest("Quest_GatherWood");
            _storageManager.AddItem("Wood", 10);

            _sut.TryCompleteActiveQuest();

            Assert.IsNull(_sut.GetActiveQuest());
        }

        // ===== IsQuestCompleted =====

        [Test]
        public void IsQuestCompleted_NotCompletedQuest_ReturnsFalse()
        {
            // 未完成的任務應回傳 false
            bool result = _sut.IsQuestCompleted("Quest_GatherWood");

            Assert.IsFalse(result);
        }

        [Test]
        public void IsQuestCompleted_AfterCompletion_ReturnsTrue()
        {
            // 完成任務後，IsQuestCompleted 應回傳 true
            _sut.AcceptQuest("Quest_GatherWood");
            _storageManager.AddItem("Wood", 10);
            _sut.TryCompleteActiveQuest();

            bool result = _sut.IsQuestCompleted("Quest_GatherWood");

            Assert.IsTrue(result);
        }

        [Test]
        public void IsQuestCompleted_NonExistentQuestId_ReturnsFalse()
        {
            // 查詢不存在的任務 ID 應回傳 false
            bool result = _sut.IsQuestCompleted("Quest_ThisDoesNotExist");

            Assert.IsFalse(result);
        }

        // ===== QuestAcceptedEvent 事件 =====

        [Test]
        public void AcceptQuest_Success_PublishesQuestAcceptedEvent()
        {
            // 成功接受任務應發布 QuestAcceptedEvent
            QuestAcceptedEvent receivedEvent = null;
            Action<QuestAcceptedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<QuestAcceptedEvent>(handler);

            _sut.AcceptQuest("Quest_GatherWood");

            EventBus.Unsubscribe<QuestAcceptedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
        }

        [Test]
        public void AcceptQuest_Success_EventContainsCorrectQuestId()
        {
            // 發布的事件應包含正確的 questId
            QuestAcceptedEvent receivedEvent = null;
            Action<QuestAcceptedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<QuestAcceptedEvent>(handler);

            _sut.AcceptQuest("Quest_GatherWood");

            EventBus.Unsubscribe<QuestAcceptedEvent>(handler);

            Assert.AreEqual("Quest_GatherWood", receivedEvent.QuestId);
        }

        [Test]
        public void AcceptQuest_Failure_DoesNotPublishQuestAcceptedEvent()
        {
            // 接受失敗時不應發布事件
            _sut.AcceptQuest("Quest_GatherWood");

            bool eventPublished = false;
            Action<QuestAcceptedEvent> handler = (e) => { eventPublished = true; };
            EventBus.Subscribe<QuestAcceptedEvent>(handler);

            _sut.AcceptQuest("Quest_HuntAnimal");

            EventBus.Unsubscribe<QuestAcceptedEvent>(handler);

            Assert.IsFalse(eventPublished);
        }

        // ===== QuestCompletedEvent 事件 =====

        [Test]
        public void TryCompleteActiveQuest_Success_PublishesQuestCompletedEvent()
        {
            // 成功完成任務應發布 QuestCompletedEvent
            _sut.AcceptQuest("Quest_GatherWood");
            _storageManager.AddItem("Wood", 10);

            EventBus.ForceClearAll();

            QuestCompletedEvent receivedEvent = null;
            Action<QuestCompletedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<QuestCompletedEvent>(handler);

            _sut.TryCompleteActiveQuest();

            EventBus.Unsubscribe<QuestCompletedEvent>(handler);

            Assert.IsNotNull(receivedEvent);
        }

        [Test]
        public void TryCompleteActiveQuest_Success_EventContainsCorrectQuestId()
        {
            // 完成事件應包含正確的 questId
            _sut.AcceptQuest("Quest_GatherWood");
            _storageManager.AddItem("Wood", 10);

            EventBus.ForceClearAll();

            QuestCompletedEvent receivedEvent = null;
            Action<QuestCompletedEvent> handler = (e) => { receivedEvent = e; };
            EventBus.Subscribe<QuestCompletedEvent>(handler);

            _sut.TryCompleteActiveQuest();

            EventBus.Unsubscribe<QuestCompletedEvent>(handler);

            Assert.AreEqual("Quest_GatherWood", receivedEvent.QuestId);
        }

        [Test]
        public void TryCompleteActiveQuest_Failure_DoesNotPublishQuestCompletedEvent()
        {
            // 完成失敗時不應發布事件
            _sut.AcceptQuest("Quest_GatherWood");

            bool eventPublished = false;
            Action<QuestCompletedEvent> handler = (e) => { eventPublished = true; };
            EventBus.Subscribe<QuestCompletedEvent>(handler);

            _sut.TryCompleteActiveQuest();

            EventBus.Unsubscribe<QuestCompletedEvent>(handler);

            Assert.IsFalse(eventPublished);
        }
    }
}
