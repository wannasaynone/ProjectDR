// GuardReturnIntegrationTest — Sprint 4 C1 端到端 Loop 整合測試。
// 驗證 TEST 4（首次探索守衛歸來）：
//   玩家嘗試 ExplorationEntryManager.Depart()
//   → interceptor 攔截（條件：探索功能已解鎖 + 守衛未解鎖 + 事件未觸發）
//   → GuardReturnEventController.TriggerEvent() 啟動
//   → CG 播放（FakeCGPlayer，台詞已整合於 intro_lines）
//   → CG 完成後直接發布 GuardReturnEventCompletedEvent（F7 bugfix：不再依賴 DialogueManager）
//   → CharacterUnlockManager 解鎖守衛
//   → 再次 Depart() 不再觸發
//
// Sprint 6 擴張：守衛歸來事件不再贈劍。劍由玩家主動發問「要拿劍」特殊題觸發（C11）。
// F7 bugfix：GuardReturnEventController 不再呼叫 DialogueManager.StartDialogue()。
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
        private InitialResourcesConfig _resourcesConfig;
        private InitialResourceDispatcher _dispatcher;
        private CharacterUnlockManager _unlockManager;
        private GuardReturnEventController _guardController;
        private ExplorationEntryManager _explorationManager;
        private FakeCGPlayer _cgPlayer;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _backpack = new BackpackManager(20, 99);
            _storage = new StorageManager(100, 99);

            _resourcesConfig = BuildInitialResourcesConfig();
            _dispatcher = new InitialResourceDispatcher(_backpack, _storage);
            _unlockManager = new CharacterUnlockManager(_resourcesConfig, _dispatcher);

            _cgPlayer = new FakeCGPlayer { AutoComplete = true };
            _guardController = new GuardReturnEventController(_cgPlayer);

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

        // ===== TEST 4-B：CG 完成 → 直接發布 GuardReturnEventCompletedEvent =====
        // F7 bugfix：不再透過 DialogueManager 播放 guard_return_lines，CG 結束後直接完成。

        [Test]
        public void AfterCGCompletion_PublishesGuardReturnCompletedEvent()
        {
            bool completed = false;
            Action<GuardReturnEventCompletedEvent> handler = (e) => completed = true;
            EventBus.Subscribe(handler);
            try
            {
                _cgPlayer.AutoComplete = true;
                _explorationManager.Depart();
                // CG 完成後同步發布 GuardReturnEventCompletedEvent，不需推進對話
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
            Assert.IsTrue(completed, "CG 完成後應立即發布 GuardReturnEventCompletedEvent");
        }

        // ===== TEST 4-C：守衛解鎖（Sprint 6 擴張：不再贈劍，劍改由玩家主動發問取得）=====

        [Test]
        public void EventComplete_UnlocksGuard_SwordNotGrantedYet()
        {
            _cgPlayer.AutoComplete = true;
            _explorationManager.Depart();
            // F7 bugfix：CG 完成後同步完成事件，不需推進對話

            Assert.IsTrue(_unlockManager.IsUnlocked(CharacterIds.Guard));
            // Sprint 6 擴張：守衛歸來完成不再直接贈劍；劍由玩家主動發問「要拿劍」觸發。
            Assert.AreEqual(0, _backpack.GetItemCount("gift_sword_wooden"),
                "守衛歸來事件完成時不應直接贈劍（劍由玩家主動發問取得）");
        }

        // ===== TEST 4-D：一次性觸發 =====

        [Test]
        public void SecondDepart_NotIntercepted_AfterEventComplete()
        {
            _cgPlayer.AutoComplete = true;
            _explorationManager.Depart();
            // F7 bugfix：CG 完成後同步完成事件，守衛已解鎖

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
                    // Sprint 6 擴張：trigger 改為 GuardSwordAsked（玩家主動發問觸發），不再於守衛歸來時派發
                    new InitialResourceGrantData { grant_id = "unlock_guard_sword", trigger_id = InitialResourcesTriggerIds.GuardSwordAsked, item_id = "gift_sword_wooden", quantity = 1 },
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
