using NUnit.Framework;
using KahaGameCore.GameEvent;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    [TestFixture]
    public class DialogueManagerTests
    {
        private DialogueManager _manager;

        [SetUp]
        public void SetUp()
        {
            // 清理靜態 EventBus 以避免殘留 handler 干擾測試
            EventBus.ForceClearAll();
            _manager = new DialogueManager();
        }

        // === 初始狀態 ===

        [Test]
        public void IsActive_InitialState_ReturnsFalse()
        {
            Assert.IsFalse(_manager.IsActive);
        }

        [Test]
        public void IsComplete_InitialState_ReturnsFalse()
        {
            Assert.IsFalse(_manager.IsComplete);
        }

        [Test]
        public void GetCurrentLine_InitialState_ReturnsNull()
        {
            Assert.IsNull(_manager.GetCurrentLine());
        }

        // === StartDialogue ===

        [Test]
        public void StartDialogue_WithValidData_SetsIsActiveTrue()
        {
            DialogueData data = new DialogueData(new string[] { "Line1" });
            _manager.StartDialogue(data);

            Assert.IsTrue(_manager.IsActive);
        }

        [Test]
        public void StartDialogue_WithValidData_SetsCurrentLineToFirstLine()
        {
            DialogueData data = new DialogueData(new string[] { "Line1", "Line2" });
            _manager.StartDialogue(data);

            Assert.AreEqual("Line1", _manager.GetCurrentLine());
        }

        [Test]
        public void StartDialogue_WithNullData_DoesNotActivate()
        {
            _manager.StartDialogue(null);

            Assert.IsFalse(_manager.IsActive);
        }

        [Test]
        public void StartDialogue_WithEmptyLines_DoesNotActivate()
        {
            DialogueData data = new DialogueData(new string[] { });
            _manager.StartDialogue(data);

            Assert.IsFalse(_manager.IsActive);
        }

        [Test]
        public void StartDialogue_PublishesDialogueStartedEvent()
        {
            bool eventReceived = false;
            string receivedFirstLine = null;

            System.Action<DialogueStartedEvent> handler = (DialogueStartedEvent e) =>
            {
                eventReceived = true;
                receivedFirstLine = e.FirstLine;
            };

            EventBus.Subscribe<DialogueStartedEvent>(handler);

            DialogueData data = new DialogueData(new string[] { "Hello" });
            _manager.StartDialogue(data);

            Assert.IsTrue(eventReceived);
            Assert.AreEqual("Hello", receivedFirstLine);

            EventBus.Unsubscribe<DialogueStartedEvent>(handler);
        }

        // === Advance ===

        [Test]
        public void Advance_WithMultipleLines_MovesToNextLine()
        {
            DialogueData data = new DialogueData(new string[] { "Line1", "Line2", "Line3" });
            _manager.StartDialogue(data);

            bool result = _manager.Advance();

            Assert.IsTrue(result);
            Assert.AreEqual("Line2", _manager.GetCurrentLine());
        }

        [Test]
        public void Advance_AtLastLine_ReturnsFalseAndCompletesDialogue()
        {
            DialogueData data = new DialogueData(new string[] { "OnlyLine" });
            _manager.StartDialogue(data);

            bool result = _manager.Advance();

            Assert.IsFalse(result);
            Assert.IsTrue(_manager.IsComplete);
        }

        [Test]
        public void Advance_AtLastLine_PublishesDialogueCompletedEvent()
        {
            bool eventReceived = false;

            System.Action<DialogueCompletedEvent> handler = (DialogueCompletedEvent e) =>
            {
                eventReceived = true;
            };

            EventBus.Subscribe<DialogueCompletedEvent>(handler);

            DialogueData data = new DialogueData(new string[] { "OnlyLine" });
            _manager.StartDialogue(data);
            _manager.Advance();

            Assert.IsTrue(eventReceived);

            EventBus.Unsubscribe<DialogueCompletedEvent>(handler);
        }

        [Test]
        public void Advance_WhenNoActiveDialogue_ReturnsFalse()
        {
            bool result = _manager.Advance();

            Assert.IsFalse(result);
        }

        [Test]
        public void Advance_AfterDialogueComplete_ReturnsFalse()
        {
            DialogueData data = new DialogueData(new string[] { "OnlyLine" });
            _manager.StartDialogue(data);
            _manager.Advance(); // 完成

            bool result = _manager.Advance();

            Assert.IsFalse(result);
        }

        // === IsActive / IsComplete 狀態轉換 ===

        [Test]
        public void IsActive_DuringDialogue_ReturnsTrue()
        {
            DialogueData data = new DialogueData(new string[] { "Line1", "Line2" });
            _manager.StartDialogue(data);

            Assert.IsTrue(_manager.IsActive);
            Assert.IsFalse(_manager.IsComplete);
        }

        [Test]
        public void IsActive_AfterDialogueComplete_ReturnsFalse()
        {
            DialogueData data = new DialogueData(new string[] { "OnlyLine" });
            _manager.StartDialogue(data);
            _manager.Advance();

            Assert.IsFalse(_manager.IsActive);
            Assert.IsTrue(_manager.IsComplete);
        }

        // === 多行完整走完 ===

        [Test]
        public void Advance_WalkThroughAllLines_CompletesCorrectly()
        {
            DialogueData data = new DialogueData(new string[] { "A", "B", "C" });
            _manager.StartDialogue(data);

            Assert.AreEqual("A", _manager.GetCurrentLine());

            Assert.IsTrue(_manager.Advance());
            Assert.AreEqual("B", _manager.GetCurrentLine());

            Assert.IsTrue(_manager.Advance());
            Assert.AreEqual("C", _manager.GetCurrentLine());

            Assert.IsFalse(_manager.Advance());
            Assert.IsTrue(_manager.IsComplete);
            Assert.IsNull(_manager.GetCurrentLine());
        }

        // === 重新開始對話 ===

        [Test]
        public void StartDialogue_AfterComplete_ResetsToNewDialogue()
        {
            DialogueData data1 = new DialogueData(new string[] { "Old" });
            _manager.StartDialogue(data1);
            _manager.Advance(); // 完成

            DialogueData data2 = new DialogueData(new string[] { "New1", "New2" });
            _manager.StartDialogue(data2);

            Assert.IsTrue(_manager.IsActive);
            Assert.IsFalse(_manager.IsComplete);
            Assert.AreEqual("New1", _manager.GetCurrentLine());
        }

        // === GetCurrentLine 邊界 ===

        [Test]
        public void GetCurrentLine_AfterComplete_ReturnsNull()
        {
            DialogueData data = new DialogueData(new string[] { "Done" });
            _manager.StartDialogue(data);
            _manager.Advance();

            Assert.IsNull(_manager.GetCurrentLine());
        }
    }
}
