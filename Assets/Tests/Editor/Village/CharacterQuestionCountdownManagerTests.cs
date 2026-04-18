// CharacterQuestionCountdownManager 單元測試（Sprint 5 B1）。
//
// 驗證：
// - 建構驗證
// - 每角色獨立倒數（呼叫 StartCountdown(charId) 開始）
// - Tick 時間推進、倒數完成時發布 CharacterQuestionCountdownReadyEvent
// - 紅點累積上限 1（Ready 後再次到期不重複發事件，直到 ClearReady 呼叫）
// - 工作中暫停倒數（SetWorking(charId, true)）
// - Dispose 後不再發事件

using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Village.Tests
{
    [TestFixture]
    public class CharacterQuestionCountdownManagerTests
    {
        private const float CountdownSeconds = 60f;
        private const string VCW = "village_chief_wife";
        private const string FARM = "farm_girl";

        private List<CharacterQuestionCountdownReadyEvent> _receivedReady;
        private System.Action<CharacterQuestionCountdownReadyEvent> _handler;

        [SetUp]
        public void SetUp()
        {
            _receivedReady = new List<CharacterQuestionCountdownReadyEvent>();
            _handler = e => _receivedReady.Add(e);
            EventBus.Subscribe(_handler);
        }

        [TearDown]
        public void TearDown()
        {
            if (_handler != null)
                EventBus.Unsubscribe(_handler);
        }

        [Test]
        public void Constructor_NegativeDuration_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new CharacterQuestionCountdownManager(-1f));
        }

        [Test]
        public void Constructor_ZeroDuration_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new CharacterQuestionCountdownManager(0f));
        }

        [Test]
        public void StartCountdown_PublishesReady_WhenTicksPastDuration()
        {
            CharacterQuestionCountdownManager m = new CharacterQuestionCountdownManager(CountdownSeconds);
            m.StartCountdown(VCW);

            m.Tick(30f);
            Assert.AreEqual(0, _receivedReady.Count);

            m.Tick(30f);
            Assert.AreEqual(1, _receivedReady.Count);
            Assert.AreEqual(VCW, _receivedReady[0].CharacterId);
        }

        [Test]
        public void Tick_SlightOver_PublishesReady()
        {
            CharacterQuestionCountdownManager m = new CharacterQuestionCountdownManager(CountdownSeconds);
            m.StartCountdown(VCW);

            m.Tick(61f);
            Assert.AreEqual(1, _receivedReady.Count);
        }

        [Test]
        public void Tick_UnderDuration_NoEvent()
        {
            CharacterQuestionCountdownManager m = new CharacterQuestionCountdownManager(CountdownSeconds);
            m.StartCountdown(VCW);

            m.Tick(59.9f);
            Assert.AreEqual(0, _receivedReady.Count);
        }

        [Test]
        public void MultipleCharacters_IndependentCountdown()
        {
            CharacterQuestionCountdownManager m = new CharacterQuestionCountdownManager(CountdownSeconds);
            m.StartCountdown(VCW);
            m.StartCountdown(FARM);

            m.Tick(60f);

            Assert.AreEqual(2, _receivedReady.Count);
            // 兩個角色都應發事件（順序不保證）
            HashSet<string> chars = new HashSet<string>();
            foreach (CharacterQuestionCountdownReadyEvent e in _receivedReady)
                chars.Add(e.CharacterId);
            Assert.IsTrue(chars.Contains(VCW));
            Assert.IsTrue(chars.Contains(FARM));
        }

        [Test]
        public void RedDotCap_NoDuplicateEventAfterReady()
        {
            CharacterQuestionCountdownManager m = new CharacterQuestionCountdownManager(CountdownSeconds);
            m.StartCountdown(VCW);

            m.Tick(60f);
            Assert.AreEqual(1, _receivedReady.Count);

            // 已 Ready 但尚未清除 → 再 Tick 60 秒不應再次發事件
            m.Tick(60f);
            Assert.AreEqual(1, _receivedReady.Count);

            // 再次 StartCountdown 在 Ready 狀態下應該被忽略
            m.StartCountdown(VCW);
            m.Tick(60f);
            Assert.AreEqual(1, _receivedReady.Count);
        }

        [Test]
        public void ClearReady_AllowsNextCountdown()
        {
            CharacterQuestionCountdownManager m = new CharacterQuestionCountdownManager(CountdownSeconds);
            m.StartCountdown(VCW);
            m.Tick(60f);
            Assert.AreEqual(1, _receivedReady.Count);

            // 清除 Ready 後重新啟動倒數
            m.ClearReady(VCW);
            m.StartCountdown(VCW);
            m.Tick(60f);
            Assert.AreEqual(2, _receivedReady.Count);
        }

        [Test]
        public void IsReady_ReturnsCorrectState()
        {
            CharacterQuestionCountdownManager m = new CharacterQuestionCountdownManager(CountdownSeconds);
            m.StartCountdown(VCW);
            Assert.IsFalse(m.IsReady(VCW));

            m.Tick(60f);
            Assert.IsTrue(m.IsReady(VCW));

            m.ClearReady(VCW);
            Assert.IsFalse(m.IsReady(VCW));
        }

        [Test]
        public void SetWorking_PausesTick()
        {
            CharacterQuestionCountdownManager m = new CharacterQuestionCountdownManager(CountdownSeconds);
            m.StartCountdown(VCW);

            m.Tick(30f);
            m.SetWorking(VCW, true);
            m.Tick(60f); // 工作中 → 不扣時間
            Assert.AreEqual(0, _receivedReady.Count);

            m.SetWorking(VCW, false);
            m.Tick(30f); // 恢復 → 剛好完成
            Assert.AreEqual(1, _receivedReady.Count);
        }

        [Test]
        public void SetWorking_ResumeMidCountdown_ContinuesFromPausePoint()
        {
            CharacterQuestionCountdownManager m = new CharacterQuestionCountdownManager(CountdownSeconds);
            m.StartCountdown(VCW);

            m.Tick(20f);  // 已過 20s
            m.SetWorking(VCW, true);
            m.Tick(1000f); // 暫停期間的 tick 不計
            m.SetWorking(VCW, false);
            m.Tick(40f); // 恢復後再過 40s = 累計 60s
            Assert.AreEqual(1, _receivedReady.Count);
        }

        [Test]
        public void SetWorking_UnknownCharacter_NoException()
        {
            CharacterQuestionCountdownManager m = new CharacterQuestionCountdownManager(CountdownSeconds);
            Assert.DoesNotThrow(() => m.SetWorking("not_started_char", true));
        }

        [Test]
        public void StartCountdown_EmptyCharacterId_Ignored()
        {
            CharacterQuestionCountdownManager m = new CharacterQuestionCountdownManager(CountdownSeconds);
            Assert.DoesNotThrow(() => m.StartCountdown(null));
            Assert.DoesNotThrow(() => m.StartCountdown(string.Empty));

            m.Tick(60f);
            Assert.AreEqual(0, _receivedReady.Count);
        }

        [Test]
        public void Tick_NegativeDelta_Ignored()
        {
            CharacterQuestionCountdownManager m = new CharacterQuestionCountdownManager(CountdownSeconds);
            m.StartCountdown(VCW);

            m.Tick(60f);
            int before = _receivedReady.Count;
            m.ClearReady(VCW);
            m.StartCountdown(VCW);
            m.Tick(-10f);
            m.Tick(60f);
            Assert.AreEqual(before + 1, _receivedReady.Count);
        }

        [Test]
        public void Dispose_StopsEventPublication()
        {
            CharacterQuestionCountdownManager m = new CharacterQuestionCountdownManager(CountdownSeconds);
            m.StartCountdown(VCW);
            m.Dispose();

            Assert.DoesNotThrow(() => m.Tick(60f));
            Assert.AreEqual(0, _receivedReady.Count);
        }
    }
}
