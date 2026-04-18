using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// GuardReturnEventController 單元測試（B10 Sprint 4）。
    /// 驗證：CG 播放 → 對話 → 完成事件流程、一次性觸發、攔截器介面整合。
    /// </summary>
    [TestFixture]
    public class GuardReturnEventControllerTests
    {
        private DialogueManager _dialogueManager;
        private GuardReturnConfig _config;
        private FakeCGPlayer _cgPlayer;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _dialogueManager = new DialogueManager();
            _config = BuildConfig();
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
                new GuardReturnEventController(null, _dialogueManager, _config));
        }

        [Test]
        public void Constructor_NullDialogueManager_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GuardReturnEventController(_cgPlayer, null, _config));
        }

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GuardReturnEventController(_cgPlayer, _dialogueManager, null));
        }

        [Test]
        public void Constructor_InitialState_NotTriggeredAndNotRunning()
        {
            using (GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer, _dialogueManager, _config))
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
            using (GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer, _dialogueManager, _config))
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
            using (GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer, _dialogueManager, _config))
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
        public void TriggerEvent_CGComplete_StartsDialogue()
        {
            using (GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer, _dialogueManager, _config))
            {
                _cgPlayer.AutoComplete = true;
                sut.TriggerEvent();
                Assert.IsTrue(_dialogueManager.IsActive);
            }
        }

        [Test]
        public void FullFlow_CGAndDialogueComplete_PublishesCompletedEvent()
        {
            using (GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer, _dialogueManager, _config))
            {
                _cgPlayer.AutoComplete = true;

                bool completedReceived = false;
                Action<GuardReturnEventCompletedEvent> handler = (e) => { completedReceived = true; };
                EventBus.Subscribe(handler);

                try
                {
                    sut.TriggerEvent();
                    while (_dialogueManager.IsActive && _dialogueManager.Advance()) { }
                }
                finally
                {
                    EventBus.Unsubscribe(handler);
                }

                Assert.IsTrue(completedReceived);
                Assert.IsFalse(sut.IsRunning);
                Assert.IsTrue(sut.HasTriggered);
            }
        }

        [Test]
        public void TriggerEvent_AfterTriggered_ReturnsFalse()
        {
            using (GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer, _dialogueManager, _config))
            {
                _cgPlayer.AutoComplete = true;
                sut.TriggerEvent();
                while (_dialogueManager.IsActive && _dialogueManager.Advance()) { }

                // 第二次觸發應該失敗
                Assert.IsFalse(sut.CanTriggerGuardReturn());
                Assert.IsFalse(sut.TriggerEvent());
            }
        }

        [Test]
        public void TriggerEvent_WhileRunning_ReturnsFalse()
        {
            using (GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer, _dialogueManager, _config))
            {
                _cgPlayer.AutoComplete = false; // 卡在 CG 階段
                sut.TriggerEvent();
                Assert.IsFalse(sut.CanTriggerGuardReturn());
                Assert.IsFalse(sut.TriggerEvent());
            }
        }

        [Test]
        public void TriggerEvent_EmptyConfig_CompletesImmediately()
        {
            GuardReturnConfig emptyConfig = new GuardReturnConfig(new GuardReturnConfigData
            {
                guard_return_lines = new GuardReturnLineData[0],
            });
            using (GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer, _dialogueManager, emptyConfig))
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
                Assert.IsTrue(completedReceived);
                Assert.IsFalse(sut.IsRunning);
            }
        }

        // ===== Dispose =====

        [Test]
        public void Dispose_DoesNotThrow()
        {
            GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer, _dialogueManager, _config);
            Assert.DoesNotThrow(() => sut.Dispose());
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            GuardReturnEventController sut = new GuardReturnEventController(_cgPlayer, _dialogueManager, _config);
            sut.Dispose();
            Assert.DoesNotThrow(() => sut.Dispose());
        }

        // ===== Helper =====

        private static GuardReturnConfig BuildConfig()
        {
            return new GuardReturnConfig(new GuardReturnConfigData
            {
                schema_version = 1,
                guard_return_lines = new GuardReturnLineData[]
                {
                    new GuardReturnLineData { line_id = "l1", sequence = 1, speaker = "narrator", text = "intro", line_type = "narration", phase_id = GuardReturnPhaseIds.Alert },
                    new GuardReturnLineData { line_id = "l2", sequence = 2, speaker = "Guard", text = "stop", line_type = "dialogue", phase_id = GuardReturnPhaseIds.Alert },
                    new GuardReturnLineData { line_id = "l3", sequence = 3, speaker = "VillageChiefWife", text = "friend", line_type = "dialogue", phase_id = GuardReturnPhaseIds.Clarify },
                },
            });
        }

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
