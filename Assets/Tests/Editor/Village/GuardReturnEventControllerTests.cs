// GuardReturnEventControllerTests — GuardReturnEventController 單元測試。
// 驗證守衛歸來事件控制器的一次性觸發邏輯。

using NUnit.Framework;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Guard;
using ProjectDR.Village.CG;
using ProjectDR.Village.Navigation;
using System;

namespace ProjectDR.Tests.Village
{
    [TestFixture]
    public class GuardReturnEventControllerTests
    {
        private const string CHAR_GUARD = CharacterIds.Guard;

        // ===== Arrange Helpers =====

        private class FakeCGPlayer : ICGPlayer
        {
            public bool PlayIntroCGCalled { get; private set; }
            public string LastCharacterId { get; private set; }
            private Action _lastCallback;

            public void PlayIntroCG(string characterId, Action onComplete)
            {
                PlayIntroCGCalled = true;
                LastCharacterId = characterId;
                _lastCallback = onComplete;
            }

            public void SimulateComplete() => _lastCallback?.Invoke();
        }

        // ===== Tests =====

        [Test]
        public void test_can_trigger_guard_return_initially_true()
        {
            // Arrange
            var fake = new FakeCGPlayer();
            var ctrl = new GuardReturnEventController(fake);

            // Act & Assert
            Assert.That(ctrl.CanTriggerGuardReturn(), Is.True);
        }

        [Test]
        public void test_trigger_event_calls_cg_player()
        {
            // Arrange
            var fake = new FakeCGPlayer();
            var ctrl = new GuardReturnEventController(fake);

            // Act
            bool result = ctrl.TriggerEvent();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(fake.PlayIntroCGCalled, Is.True);
            Assert.That(fake.LastCharacterId, Is.EqualTo(CHAR_GUARD));
        }

        [Test]
        public void test_trigger_event_second_time_returns_false()
        {
            // Arrange
            var fake = new FakeCGPlayer();
            var ctrl = new GuardReturnEventController(fake);

            // Act
            ctrl.TriggerEvent();
            bool secondResult = ctrl.TriggerEvent();

            // Assert
            Assert.That(secondResult, Is.False);
        }

        [Test]
        public void test_has_triggered_true_after_trigger()
        {
            // Arrange
            var fake = new FakeCGPlayer();
            var ctrl = new GuardReturnEventController(fake);

            // Act
            ctrl.TriggerEvent();

            // Assert
            Assert.That(ctrl.HasTriggered, Is.True);
        }

        [Test]
        public void test_can_trigger_false_after_triggered()
        {
            // Arrange
            var fake = new FakeCGPlayer();
            var ctrl = new GuardReturnEventController(fake);
            ctrl.TriggerEvent();

            // Assert
            Assert.That(ctrl.CanTriggerGuardReturn(), Is.False);
        }

        [Test]
        public void test_cg_complete_publishes_completed_event()
        {
            // Arrange
            var fake = new FakeCGPlayer();
            var ctrl = new GuardReturnEventController(fake);
            bool receivedCompleted = false;
            EventBus.Subscribe<GuardReturnEventCompletedEvent>(_ => receivedCompleted = true);

            // Act
            ctrl.TriggerEvent();
            fake.SimulateComplete();

            // Cleanup
            EventBus.Unsubscribe<GuardReturnEventCompletedEvent>(_ => receivedCompleted = true);

            // Assert
            Assert.That(receivedCompleted, Is.True);
        }

        [Test]
        public void test_constructor_null_throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GuardReturnEventController(null));
        }
    }
}
