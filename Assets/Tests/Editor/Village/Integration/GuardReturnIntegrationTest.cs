// GuardReturnIntegrationTest — Sprint 4 C1 端到端 Loop 整合測試。
// 驗證 TEST 4（首次探索守衛歸來）：
//   玩家嘗試 ExplorationEntryManager.Depart()
//   → interceptor 攔截（條件：探索功能已解鎖 + 守衛未解鎖 + 事件未觸發）
//   → GuardReturnEventController.TriggerEvent() 啟動
//   → CG 播放（FakeCGPlayer）→ DialogueManager 播放 31 行
//   → DialogueCompleted → GuardReturnEventCompletedEvent
//   → CharacterUnlockManager 解鎖守衛 + 發放木劍
//   → 再次 Depart() 不再觸發
//
// 使用真實 Manager（不 mock），透過 FakeCGPlayer 讓 CG 立即完成。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village.Integration
{
    [TestFixture]
    public class GuardReturnIntegrationTest
    {
        private BackpackManager _backpack;
        private StorageManager _storage;
        private DialogueManager _dialogueManager;
        private InitialResourcesConfig _resourcesConfig;
        private InitialResourceDispatcher _dispatcher;
        private CharacterUnlockManager _unlockManager;
        private GuardReturnConfig _guardConfig;
        private GuardReturnEventController _guardController;
        private ExplorationEntryManager _explorationManager;
        private FakeCGPlayer _cgPlayer;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _backpack = new BackpackManager(20, 99);
            _storage = new StorageManager(100, 99);
            _dialogueManager = new DialogueManager();

            _resourcesConfig = BuildInitialResourcesConfig();
            _dispatcher = new InitialResourceDispatcher(_backpack, _storage);
            _unlockManager = new CharacterUnlockManager(_resourcesConfig, _dispatcher);

            _cgPlayer = new FakeCGPlayer { AutoComplete = true };
            _guardConfig = BuildGuardReturnConfig();
            _guardController = new GuardReturnEventController(_cgPlayer, _dialogueManager, _guardConfig);

            _explorationManager = new ExplorationEntryManager(_backpack);
            // 模擬「探索功能已解鎖」— CharacterUnlockManager 的 IsExplorationFeatureUnlocked 只是內部狀態，
            // 我們透過 ForceUnlockExplorationFeature 觸發
            _unlockManager.ForceUnlockExplorationFeature();

            // 注入攔截器
            _explorationManager.SetDepartureInterceptor(new Interceptor(_guardController, _unlockManager));
        }

        [TearDown]
        public void TearDown()
        {
            _guardController?.Dispose();
            _unlockManager?.Dispose();
            _explorationManager?.Dispose();
            EventBus.ForceClearAll();
        }

        // ===== TEST 4-A：首次出發被攔截 =====

        [Test]
        public void FirstDepart_WhileExplorationUnlockedAndGuardLocked_Intercepted()
        {
            // CG 保持手動完成，讓流程停在 CG 播放中以確認攔截成功
            _cgPlayer.AutoComplete = false;
            bool departed = _explorationManager.Depart();
            Assert.IsFalse(departed, "首次探索應被攔截器攔截，不應實際出發");
            Assert.IsTrue(_guardController.IsRunning);
            Assert.IsTrue(_guardController.HasTriggered);
        }

        [Test]
        public void FirstDepart_PublishesGuardReturnStartedEvent()
        {
            bool received = false;
            Action<GuardReturnEventStartedEvent> handler = (e) => received = true;
            EventBus.Subscribe(handler);
            try
            {
                _cgPlayer.AutoComplete = false;
                _explorationManager.Depart();
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
            Assert.IsTrue(received);
        }

        // ===== TEST 4-B：CG 完成 → 對話播放 =====

        [Test]
        public void AfterCGCompletion_DialogueBegins()
        {
            _cgPlayer.AutoComplete = true; // CG 完成後自動啟動對話
            _explorationManager.Depart();

            Assert.IsTrue(_dialogueManager.IsActive, "CG 完成後應啟動對話");
        }

        [Test]
        public void DialoguePhaseComplete_PublishesGuardReturnCompletedEvent()
        {
            bool completed = false;
            Action<GuardReturnEventCompletedEvent> handler = (e) => completed = true;
            EventBus.Subscribe(handler);
            try
            {
                _cgPlayer.AutoComplete = true;
                _explorationManager.Depart();
                // 推進對話至結束
                while (_dialogueManager.IsActive && _dialogueManager.Advance()) { }
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
            Assert.IsTrue(completed);
        }

        // ===== TEST 4-C：守衛解鎖 + 贈劍 =====

        [Test]
        public void EventComplete_UnlocksGuardAndDispatchesSword()
        {
            _cgPlayer.AutoComplete = true;
            _explorationManager.Depart();
            while (_dialogueManager.IsActive && _dialogueManager.Advance()) { }

            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.Guard));
            Assert.AreEqual(1, _backpack.GetItemCount("gift_sword_wooden"));
        }

        // ===== TEST 4-D：一次性觸發 =====

        [Test]
        public void SecondDepart_NotIntercepted_AfterEventComplete()
        {
            _cgPlayer.AutoComplete = true;
            _explorationManager.Depart();
            while (_dialogueManager.IsActive && _dialogueManager.Advance()) { }

            // 事件已完成 + 守衛已解鎖 → 再次 Depart 應真正出發
            bool departed = _explorationManager.Depart();
            Assert.IsTrue(departed, "事件完成後再次出發應成功（非攔截）");
        }

        [Test]
        public void GuardControllerCanTrigger_BlockedAfterFirstTrigger()
        {
            _cgPlayer.AutoComplete = true;
            _explorationManager.Depart();
            Assert.IsTrue(_guardController.HasTriggered);

            // 再次呼叫應直接回傳 false
            Assert.IsFalse(_guardController.TriggerEvent());
        }

        // ===== Interceptor 實作（與 VillageEntryPoint 中的 Adapter 一致行為） =====

        private class Interceptor : IExplorationDepartureInterceptor
        {
            private readonly GuardReturnEventController _guardController;
            private readonly CharacterUnlockManager _unlockManager;

            public Interceptor(GuardReturnEventController guardController, CharacterUnlockManager unlockManager)
            {
                _guardController = guardController;
                _unlockManager = unlockManager;
            }

            public bool TryIntercept()
            {
                if (_guardController.HasTriggered) return false;
                if (_unlockManager.IsUnlocked(CharacterIds.Guard)) return false;
                return _guardController.TriggerEvent();
            }
        }

        // ===== Helpers =====

        private static InitialResourcesConfig BuildInitialResourcesConfig()
        {
            return new InitialResourcesConfig(new InitialResourcesConfigData
            {
                schema_version = 1,
                grants = new InitialResourceGrantData[]
                {
                    new InitialResourceGrantData { grant_id = "initial_backpack_node0", trigger_id = InitialResourcesTriggerIds.Node0Start, item_id = "", quantity = 0 },
                    new InitialResourceGrantData { grant_id = "unlock_guard_sword", trigger_id = InitialResourcesTriggerIds.GuardReturnEvent, item_id = "gift_sword_wooden", quantity = 1 },
                },
            });
        }

        private static GuardReturnConfig BuildGuardReturnConfig()
        {
            return new GuardReturnConfig(new GuardReturnConfigData
            {
                schema_version = 1,
                guard_return_lines = new GuardReturnLineData[]
                {
                    new GuardReturnLineData { line_id = "g1", sequence = 1, speaker = "Guard", text = "站住！", line_type = "dialogue", phase_id = "alert" },
                    new GuardReturnLineData { line_id = "g2", sequence = 2, speaker = "VillageChiefWife", text = "不是陌生人", line_type = "dialogue", phase_id = "clarify" },
                    new GuardReturnLineData { line_id = "g3", sequence = 3, speaker = "Guard", text = "...（收劍）", line_type = "narration", phase_id = "sheathe" },
                    new GuardReturnLineData { line_id = "g4", sequence = 4, speaker = "Guard", text = "這把劍給你", line_type = "dialogue", phase_id = "gift_sword" },
                    new GuardReturnLineData { line_id = "g5", sequence = 5, speaker = "Guard", text = "走吧", line_type = "dialogue", phase_id = "closing" },
                },
            });
        }

        private class FakeCGPlayer : ICGPlayer
        {
            public bool AutoComplete { get; set; } = true;
            public string LastCharacterId { get; private set; }
            private Action _storedComplete;

            public void PlayIntroCG(string characterId, Action onComplete)
            {
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
