using System;
using System.Collections.Generic;
using NUnit.Framework;
using KahaGameCore.GameEvent;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// DialogueManager 的 VN 式選項分支測試（B2）。
    /// 涵蓋 PresentChoices / SelectChoice / AppendLines 與既有 Advance 的互動。
    /// </summary>
    [TestFixture]
    public class DialogueManagerChoiceTests
    {
        private DialogueManager _manager;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _manager = new DialogueManager();
        }

        private static DialogueData MakeDialogue(params string[] lines)
        {
            return new DialogueData(lines);
        }

        private static IReadOnlyList<DialogueChoice> MakeChoices(params (string id, string text)[] entries)
        {
            List<DialogueChoice> list = new List<DialogueChoice>();
            foreach ((string id, string text) e in entries)
            {
                list.Add(new DialogueChoice(e.id, e.text));
            }
            return list;
        }

        // ===== 初始狀態 =====

        [Test]
        public void IsWaitingForChoice_InitialState_ReturnsFalse()
        {
            Assert.IsFalse(_manager.IsWaitingForChoice);
        }

        [Test]
        public void CurrentChoices_InitialState_ReturnsNull()
        {
            Assert.IsNull(_manager.CurrentChoices);
        }

        [Test]
        public void LastSelectedChoiceId_InitialState_ReturnsNull()
        {
            Assert.IsNull(_manager.LastSelectedChoiceId);
        }

        // ===== PresentChoices =====

        [Test]
        public void PresentChoices_WithoutActiveDialogue_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                _manager.PresentChoices(MakeChoices(("a", "A")));
            });
        }

        [Test]
        public void PresentChoices_WithNull_Throws()
        {
            _manager.StartDialogue(MakeDialogue("line"));
            Assert.Throws<ArgumentNullException>(() =>
            {
                _manager.PresentChoices(null);
            });
        }

        [Test]
        public void PresentChoices_WithEmpty_Throws()
        {
            _manager.StartDialogue(MakeDialogue("line"));
            Assert.Throws<ArgumentException>(() =>
            {
                _manager.PresentChoices(new List<DialogueChoice>());
            });
        }

        [Test]
        public void PresentChoices_WhenActive_SetsWaitingAndPublishesEvent()
        {
            bool eventReceived = false;
            IReadOnlyList<DialogueChoice> receivedChoices = null;
            Action<DialogueChoicePresentedEvent> handler = (DialogueChoicePresentedEvent e) =>
            {
                eventReceived = true;
                receivedChoices = e.Choices;
            };
            EventBus.Subscribe(handler);

            _manager.StartDialogue(MakeDialogue("prompt"));
            IReadOnlyList<DialogueChoice> choices = MakeChoices(
                ("farm_girl", "田那邊的女孩"),
                ("witch", "山邊小屋的那位"));
            _manager.PresentChoices(choices);

            Assert.IsTrue(eventReceived);
            Assert.AreSame(choices, receivedChoices);
            Assert.IsTrue(_manager.IsWaitingForChoice);
            Assert.AreSame(choices, _manager.CurrentChoices);

            EventBus.Unsubscribe(handler);
        }

        [Test]
        public void PresentChoices_WhileAlreadyWaiting_Throws()
        {
            _manager.StartDialogue(MakeDialogue("prompt"));
            _manager.PresentChoices(MakeChoices(("a", "A")));

            Assert.Throws<InvalidOperationException>(() =>
            {
                _manager.PresentChoices(MakeChoices(("b", "B")));
            });
        }

        // ===== Advance 在選項呈現中 =====

        [Test]
        public void Advance_WhileWaitingForChoice_ReturnsFalseAndDoesNotAdvance()
        {
            _manager.StartDialogue(MakeDialogue("prompt", "after"));
            _manager.PresentChoices(MakeChoices(("a", "A")));

            bool result = _manager.Advance();

            Assert.IsFalse(result);
            Assert.AreEqual("prompt", _manager.GetCurrentLine());
            Assert.IsTrue(_manager.IsWaitingForChoice);
        }

        // ===== SelectChoice =====

        [Test]
        public void SelectChoice_WithoutPendingChoices_Throws()
        {
            _manager.StartDialogue(MakeDialogue("line"));

            Assert.Throws<InvalidOperationException>(() =>
            {
                _manager.SelectChoice("a");
            });
        }

        [Test]
        public void SelectChoice_WithInvalidId_Throws()
        {
            _manager.StartDialogue(MakeDialogue("prompt"));
            _manager.PresentChoices(MakeChoices(("a", "A"), ("b", "B")));

            Assert.Throws<ArgumentException>(() =>
            {
                _manager.SelectChoice("nonexistent");
            });
            // 選項未清除
            Assert.IsTrue(_manager.IsWaitingForChoice);
        }

        [Test]
        public void SelectChoice_WithValidId_ClearsChoicesAndPublishesEvent()
        {
            bool eventReceived = false;
            string receivedId = null;
            Action<DialogueChoiceSelectedEvent> handler = (DialogueChoiceSelectedEvent e) =>
            {
                eventReceived = true;
                receivedId = e.ChoiceId;
            };
            EventBus.Subscribe(handler);

            _manager.StartDialogue(MakeDialogue("prompt"));
            _manager.PresentChoices(MakeChoices(("farm_girl", "A"), ("witch", "B")));
            _manager.SelectChoice("witch");

            Assert.IsTrue(eventReceived);
            Assert.AreEqual("witch", receivedId);
            Assert.IsFalse(_manager.IsWaitingForChoice);
            Assert.IsNull(_manager.CurrentChoices);
            Assert.AreEqual("witch", _manager.LastSelectedChoiceId);

            EventBus.Unsubscribe(handler);
        }

        // ===== AppendLines =====

        [Test]
        public void AppendLines_WithoutActiveDialogue_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                _manager.AppendLines(new List<string> { "x" });
            });
        }

        [Test]
        public void AppendLines_WithNull_Throws()
        {
            _manager.StartDialogue(MakeDialogue("line"));
            Assert.Throws<ArgumentNullException>(() =>
            {
                _manager.AppendLines(null);
            });
        }

        [Test]
        public void AppendLines_WithEmpty_NoOp()
        {
            _manager.StartDialogue(MakeDialogue("A"));
            _manager.AppendLines(new List<string>());

            Assert.AreEqual("A", _manager.GetCurrentLine());
        }

        [Test]
        public void AppendLines_ExtendsCurrentDialogue_ContinuesToNextLine()
        {
            _manager.StartDialogue(MakeDialogue("A"));
            Assert.AreEqual("A", _manager.GetCurrentLine());

            _manager.AppendLines(new List<string> { "B", "C" });

            bool advanceResult = _manager.Advance();
            Assert.IsTrue(advanceResult);
            Assert.AreEqual("B", _manager.GetCurrentLine());

            advanceResult = _manager.Advance();
            Assert.IsTrue(advanceResult);
            Assert.AreEqual("C", _manager.GetCurrentLine());

            Assert.IsFalse(_manager.Advance());
            Assert.IsTrue(_manager.IsComplete);
        }

        [Test]
        public void AppendLines_AfterComplete_ReopensAndAdvances()
        {
            _manager.StartDialogue(MakeDialogue("A"));
            _manager.Advance(); // complete
            Assert.IsTrue(_manager.IsComplete);

            _manager.AppendLines(new List<string> { "B" });

            Assert.IsFalse(_manager.IsComplete);
            Assert.AreEqual("B", _manager.GetCurrentLine());

            Assert.IsFalse(_manager.Advance());
            Assert.IsTrue(_manager.IsComplete);
        }

        // ===== 完整 VN 選項分支流程 =====

        [Test]
        public void FullVnBranchFlow_ChoosingBranch_PlaysBranchSpecificLines()
        {
            // intro → prompt → choice → branch response → complete
            _manager.StartDialogue(MakeDialogue("intro", "prompt"));

            // 走到 prompt
            Assert.AreEqual("intro", _manager.GetCurrentLine());
            Assert.IsTrue(_manager.Advance());
            Assert.AreEqual("prompt", _manager.GetCurrentLine());

            // 呈現選項
            _manager.PresentChoices(MakeChoices(
                ("farm_girl", "田那邊的女孩"),
                ("witch", "山邊小屋的那位")));
            Assert.IsTrue(_manager.IsWaitingForChoice);

            // 選擇 witch
            _manager.SelectChoice("witch");

            // 呼叫端依 LastSelectedChoiceId 附加分支回應
            Assert.AreEqual("witch", _manager.LastSelectedChoiceId);
            _manager.AppendLines(new List<string> { "席薇雅。她多半在小屋裡做研究。" });

            // 繼續推進
            Assert.IsTrue(_manager.Advance());
            Assert.AreEqual("席薇雅。她多半在小屋裡做研究。", _manager.GetCurrentLine());

            // 完成
            bool completed = false;
            Action<DialogueCompletedEvent> handler = (DialogueCompletedEvent e) => { completed = true; };
            EventBus.Subscribe(handler);

            Assert.IsFalse(_manager.Advance());
            Assert.IsTrue(completed);
            Assert.IsTrue(_manager.IsComplete);

            EventBus.Unsubscribe(handler);
        }

        // ===== StartDialogue 重置 =====

        [Test]
        public void StartDialogue_ResetsPendingChoicesAndLastChoice()
        {
            _manager.StartDialogue(MakeDialogue("p"));
            _manager.PresentChoices(MakeChoices(("a", "A")));
            _manager.SelectChoice("a");
            Assert.AreEqual("a", _manager.LastSelectedChoiceId);

            _manager.StartDialogue(MakeDialogue("new"));

            Assert.IsFalse(_manager.IsWaitingForChoice);
            Assert.IsNull(_manager.LastSelectedChoiceId);
            Assert.AreEqual("new", _manager.GetCurrentLine());
        }
    }
}
