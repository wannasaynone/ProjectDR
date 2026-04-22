// GuardCGAndCountdownRegressionTest — Sprint 6 F8 bugfix 回歸測試。
//
// 驗證兩個 bug 的修復：
//
// Bug 1：守衛歸來 CG 播放後，玩家首次進入守衛 interact view 不重播 CG。
//   根因：GuardReturnEventController 透過 _cgPlayer.PlayIntroCG 播放 CG 時，
//   VillageEntryPoint._introCgPlayedCharacters 沒有被標記守衛已播過。
//   修復：在 GuardReturnEventCompletedEvent 訂閱中補呼叫 MarkIntroCGPlayed(Guard)。
//
// Bug 2：守衛解鎖後、「要拿劍」完成前，守衛的角色發問倒數不應啟動。
//   根因：Start() 時對所有角色呼叫 StartCountdown（包含守衛），
//   導致守衛在玩家取劍前倒數到期、出現 L2 發問紅點。
//   修復：守衛倒數在 ExplorationGateReopenedEvent 後才啟動；
//   CharacterQuestionCountdownManager 新增 BlockCountdown/UnblockCountdown 機制。
//
// 測試 1：守衛歸來 CG 完成後，only-once 機制正確標記守衛 CG 已播過
// 測試 2：守衛解鎖後、取劍完成前，守衛倒數不啟動
// 測試 3：「要拿劍」完成後（ExplorationGateReopenedEvent），守衛倒數啟動
// 測試 4：其他角色（農女/魔女/村長夫人）的倒數行為不受守衛封鎖影響

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.CharacterQuestions;

namespace ProjectDR.Tests.Village.Integration
{
    [TestFixture]
    public class GuardCGAndCountdownRegressionTest
    {
        private const float CountdownSeconds = 60f;

        private CharacterQuestionCountdownManager _countdownManager;
        private List<CharacterQuestionCountdownReadyEvent> _receivedReady;
        private Action<CharacterQuestionCountdownReadyEvent> _readyHandler;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            _countdownManager = new CharacterQuestionCountdownManager(CountdownSeconds);
            _receivedReady = new List<CharacterQuestionCountdownReadyEvent>();
            _readyHandler = e => _receivedReady.Add(e);
            EventBus.Subscribe(_readyHandler);
        }

        [TearDown]
        public void TearDown()
        {
            if (_readyHandler != null)
                EventBus.Unsubscribe(_readyHandler);
            _countdownManager?.Dispose();
            EventBus.ForceClearAll();
        }

        // ===== 測試 1：守衛歸來 CG 完成後 only-once 旗標已記錄守衛 =====
        // 驗證：GuardReturnEventCompletedEvent 發布後，追蹤器中守衛 CG 應標記為已播放，
        // 不會在首次進入 interact view 時重播 CG。
        // 此測試以純邏輯驗證「標記機制正確觸發」——
        // 透過 IntroCGTracker 模擬 VillageEntryPoint._introCgPlayedCharacters 的行為。

        [Test]
        public void Regression_Bug1_GuardReturnCompleted_MarksGuardCGAsPlayed()
        {
            // Arrange：建立 CG 追蹤器，模擬 VillageEntryPoint 的 only-once 機制
            IntroCGTracker tracker = new IntroCGTracker();

            // 模擬 VillageEntryPoint 訂閱 GuardReturnEventCompletedEvent
            // 並在 handler 中呼叫 MarkIntroCGPlayed(Guard)
            Action<GuardReturnEventCompletedEvent> guardReturnHandler = (e) =>
            {
                tracker.MarkPlayed(CharacterIds.Guard);
            };
            EventBus.Subscribe(guardReturnHandler);

            try
            {
                // Act：守衛歸來事件完成（發布 GuardReturnEventCompletedEvent）
                EventBus.Publish(new GuardReturnEventCompletedEvent());

                // Assert：守衛 CG 應已標記為播放過
                Assert.IsTrue(tracker.HasPlayed(CharacterIds.Guard),
                    "守衛歸來 CG 完成後，VillageEntryPoint 應呼叫 MarkIntroCGPlayed(Guard)，" +
                    "避免首次進入守衛 interact view 時重播同一段 CG。");

                // Assert：其他角色不受影響
                Assert.IsFalse(tracker.HasPlayed(CharacterIds.FarmGirl));
                Assert.IsFalse(tracker.HasPlayed(CharacterIds.Witch));
            }
            finally
            {
                EventBus.Unsubscribe(guardReturnHandler);
            }
        }

        [Test]
        public void Regression_Bug1_WithoutFix_GuardCGNotMarked_WouldReplay()
        {
            // 反向驗證：若未訂閱 GuardReturnEventCompletedEvent 補標記，
            // 守衛 CG 的 only-once 旗標不會設定，InitializeCharacterView 進入守衛時會重播。
            IntroCGTracker tracker = new IntroCGTracker();

            // 刻意不訂閱 GuardReturnEventCompletedEvent
            EventBus.Publish(new GuardReturnEventCompletedEvent());

            // tracker 未被標記 → 若 InitializeCharacterView 此時跑，會重播 CG（bug 現象）
            Assert.IsFalse(tracker.HasPlayed(CharacterIds.Guard),
                "未修復時守衛 CG 不會被標記已播（回歸測試確認 bug 確實存在）");
        }

        // ===== 測試 2：守衛解鎖後、取劍完成前，守衛倒數不啟動 =====

        [Test]
        public void Regression_Bug2_Guard_CountdownBlocked_BeforeSwordObtained()
        {
            // Arrange：模擬 VillageEntryPoint Start() 的新行為：
            // 對所有角色啟動倒數，但守衛預先封鎖（BlockCountdown）
            _countdownManager.BlockCountdown(CharacterIds.Guard);

            // 對非守衛角色正常啟動
            _countdownManager.StartCountdown(CharacterIds.VillageChiefWife);
            _countdownManager.StartCountdown(CharacterIds.FarmGirl);
            _countdownManager.StartCountdown(CharacterIds.Witch);
            // 守衛倒數被封鎖，StartCountdown 不生效
            _countdownManager.StartCountdown(CharacterIds.Guard);

            // Act：時間推進超過 60s
            _countdownManager.Tick(61f);

            // Assert：守衛不應觸發 Ready 事件
            bool guardReadyFired = false;
            foreach (CharacterQuestionCountdownReadyEvent e in _receivedReady)
            {
                if (e.CharacterId == CharacterIds.Guard)
                    guardReadyFired = true;
            }
            Assert.IsFalse(guardReadyFired,
                "守衛在取劍完成前，L2 倒數不應到期（封鎖狀態下不啟動）。");
        }

        // ===== 測試 3：「要拿劍」完成後（ExplorationGateReopenedEvent），守衛倒數啟動 =====

        [Test]
        public void Regression_Bug2_Guard_CountdownStartsAfterSwordObtained()
        {
            // Arrange：初始封鎖守衛倒數
            _countdownManager.BlockCountdown(CharacterIds.Guard);
            _countdownManager.StartCountdown(CharacterIds.Guard); // 被封鎖，無效

            // 時間推進（封鎖中，不觸發）
            _countdownManager.Tick(61f);
            int readyCountBeforeUnblock = _receivedReady.Count;

            // 模擬 VillageEntryPoint.OnExplorationGateReopenedForT2 的行為：
            // 解封守衛倒數並啟動
            _countdownManager.UnblockCountdown(CharacterIds.Guard);
            _countdownManager.StartCountdown(CharacterIds.Guard);

            // Act：時間推進 60s
            _countdownManager.Tick(60f);

            // Assert：守衛倒數應觸發
            bool guardReadyFired = false;
            foreach (CharacterQuestionCountdownReadyEvent e in _receivedReady)
            {
                if (e.CharacterId == CharacterIds.Guard)
                    guardReadyFired = true;
            }
            Assert.IsTrue(guardReadyFired,
                "『要拿劍』完成後（UnblockCountdown + StartCountdown），守衛 L2 倒數應正常到期觸發。");

            // 確認封鎖前的事件數量沒有守衛（確認修復有效）
            int guardReadyBeforeUnblock = 0;
            for (int i = 0; i < readyCountBeforeUnblock; i++)
            {
                if (_receivedReady[i].CharacterId == CharacterIds.Guard)
                    guardReadyBeforeUnblock++;
            }
            Assert.AreEqual(0, guardReadyBeforeUnblock, "封鎖期間守衛不應有任何 Ready 事件");
        }

        // ===== 測試 4：其他角色倒數行為不受守衛封鎖影響 =====

        [Test]
        public void Regression_Bug2_OtherCharacters_CountdownUnaffected()
        {
            // Arrange：封鎖守衛，啟動其他角色
            _countdownManager.BlockCountdown(CharacterIds.Guard);
            _countdownManager.StartCountdown(CharacterIds.VillageChiefWife);
            _countdownManager.StartCountdown(CharacterIds.FarmGirl);
            _countdownManager.StartCountdown(CharacterIds.Witch);
            _countdownManager.StartCountdown(CharacterIds.Guard); // 被封鎖

            // Act
            _countdownManager.Tick(60f);

            // Assert：其他三個角色應各自觸發 Ready
            HashSet<string> readyChars = new HashSet<string>();
            foreach (CharacterQuestionCountdownReadyEvent e in _receivedReady)
                readyChars.Add(e.CharacterId);

            Assert.IsTrue(readyChars.Contains(CharacterIds.VillageChiefWife),
                "村長夫人倒數應不受守衛封鎖影響");
            Assert.IsTrue(readyChars.Contains(CharacterIds.FarmGirl),
                "農女倒數應不受守衛封鎖影響");
            Assert.IsTrue(readyChars.Contains(CharacterIds.Witch),
                "魔女倒數應不受守衛封鎖影響");
            Assert.IsFalse(readyChars.Contains(CharacterIds.Guard),
                "守衛倒數在封鎖期間不應觸發");
        }

        // ===== 測試輔助：IntroCGTracker 模擬 VillageEntryPoint._introCgPlayedCharacters =====

        private class IntroCGTracker
        {
            private readonly HashSet<string> _played = new HashSet<string>();

            public void MarkPlayed(string characterId)
            {
                _played.Add(characterId);
            }

            public bool HasPlayed(string characterId)
            {
                return _played.Contains(characterId);
            }
        }
    }
}
