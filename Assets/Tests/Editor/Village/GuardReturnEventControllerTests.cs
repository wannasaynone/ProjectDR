using System;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// GuardReturnEventController 單元測試（B10 Sprint 4，F7 bugfix 更新）。
    /// 驗證：CG 播放 → 直接完成事件流程、一次性觸發。
    ///
    /// F7 bugfix：移除 DialogueManager 依賴，CG 完成後直接發布 GuardReturnEventCompletedEvent。
    /// guard_return_lines 台詞已整合於 character-intro-config.json 的 intro_guard lines，
    /// 由 CharacterIntroCGView 在 CG 期間展示。
    /// </summary>
    [TestFixture]
    public class GuardReturnEventControllerTests
    {
        private FakeCGPlayer _cgPlayer;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _cgPlayer = new FakeCGPlayer();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== 建構 =====

        [Test]
        public void Constructor_NullCGPlayer_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GuardReturnEventController(null));
        }

        [Test]
        public void Constructor_InitialState_NotTriggeredAndNotRunning()
        {
            using (GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer))
            {
                Assert.IsFalse(sut.IsRunning);
                Assert.IsFalse(sut.HasTriggered);
                Assert.IsTrue(sut.CanTriggerGuardReturn());
            }
        }

        // ===== TriggerEvent =====

        [Test]
        public void TriggerEvent_StartsCGPlayback()
        {
            using (GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer))
            {
                _cgPlayer.AutoComplete = false;
                bool result = sut.TriggerEvent();
                Assert.IsTrue(result);
                Assert.IsTrue(sut.IsRunning);
                Assert.IsTrue(sut.HasTriggered);
                Assert.AreEqual(CharacterIds.Guard, _cgPlayer.LastCharacterId);
            }
        }

        [Test]
        public void TriggerEvent_PublishesStartedEvent()
        {
            using (GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer))
            {
                _cgPlayer.AutoComplete = false;
                bool received = false;
                Action<GuardReturnEventStartedEvent> handler = (e) => { received = true; };
                EventBus.Subscribe(handler);
                try
                {
                    sut.TriggerEvent();
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }
                Assert.IsTrue(received);
            }
        }

        [Test]
        public void TriggerEvent_CGComplete_PublishesCompletedEvent()
        {
            // F7 bugfix：CG 完成後直接發布 GuardReturnEventCompletedEvent，不依賴 DialogueManager。
            using (GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer))
            {
                _cgPlayer.AutoComplete = true;

                bool completedReceived = false;
                Action<GuardReturnEventCompletedEvent> handler = (e) => { completedReceived = true; };
                EventBus.Subscribe(handler);
                try
                {
                    sut.TriggerEvent();
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }

                Assert.IsTrue(completedReceived, "CG 完成後應立即發布 GuardReturnEventCompletedEvent");
                Assert.IsFalse(sut.IsRunning, "事件完成後 IsRunning 應為 false");
                Assert.IsTrue(sut.HasTriggered, "事件完成後 HasTriggered 應為 true");
            }
        }

        [Test]
        public void TriggerEvent_AfterTriggered_ReturnsFalse()
        {
            using (GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer))
            {
                _cgPlayer.AutoComplete = true;
                sut.TriggerEvent();
                // CG 完成後事件已結束

                // 第二次觸發應該失敗
                Assert.IsFalse(sut.CanTriggerGuardReturn());
                Assert.IsFalse(sut.TriggerEvent());
            }
        }

        [Test]
        public void TriggerEvent_WhileRunning_ReturnsFalse()
        {
            using (GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer))
            {
                _cgPlayer.AutoComplete = false; // 卡在 CG 階段，事件執行中
                sut.TriggerEvent();
                Assert.IsTrue(sut.IsRunning, "CG 未完成時 IsRunning 應為 true");
                Assert.IsFalse(sut.CanTriggerGuardReturn());
                Assert.IsFalse(sut.TriggerEvent());
            }
        }

        // ===== Dispose =====

        [Test]
        public void Dispose_DoesNotThrow()
        {
            GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer);
            Assert.DoesNotThrow(() => sut.Dispose());
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer);
            sut.Dispose();
            Assert.DoesNotThrow(() => sut.Dispose());
        }

        [Test]
        public void Dispose_WhileCGRunning_CompletedEventNotPublished()
        {
            // CG 執行中 Dispose 後，CG callback 被忽略（_disposed = true）
            GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer);
            _cgPlayer.AutoComplete = false; // CG 卡住

            bool completedReceived = false;
            Action<GuardReturnEventCompletedEvent> handler = (e) => { completedReceived = true; };
            EventBus.Subscribe(handler);
            try
            {
                sut.TriggerEvent();
                sut.Dispose();
                // 現在手動觸發 CG 完成
                _cgPlayer.InvokeStoredComplete();
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            Assert.IsFalse(completedReceived, "Dispose 後 CG callback 應被忽略，不發布完成事件");
        }

        // ===== Helper =====

        private class FakeCGPlayer : ICGPlayer
        {
            public bool AutoComplete { get; set; } = true;
            public int CallCount { get; private set; }
            public string LastCharacterId { get; private set; }
            private Action _storedComplete;

            public void PlayIntroCG(string characterId, Action onComplete)
            {
                CallCount++;
                LastCharacterId = characterId;
                if (AutoComplete)
                {
                    onComplete?.Invoke();
                }
                else
                {
                    _storedComplete = onComplete;
                }
            }

            public void InvokeStoredComplete()
            {
                _storedComplete?.Invoke();
                _storedComplete = null;
            }
        }
    }
}
