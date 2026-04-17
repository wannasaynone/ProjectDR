using System;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village.Mvp;

namespace ProjectDR.Tests.Village.Mvp
{
    [TestFixture]
    public class ActionTimeManagerTests
    {
        private ColdStatusSystem _cold;
        private ActionTimeManager _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _cold = new ColdStatusSystem();
            _sut = new ActionTimeManager(_cold, 2f);
        }

        [TearDown]
        public void TearDown()
        {
            _cold.Dispose();
            EventBus.ForceClearAll();
        }

        [Test]
        public void Ctor_NullCold_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ActionTimeManager(null, 2f));
        }

        [Test]
        public void Ctor_MultiplierBelowOne_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ActionTimeManager(_cold, 0.5f));
        }

        [Test]
        public void IsOnCooldown_Initial_False()
        {
            Assert.IsFalse(_sut.IsOnCooldown(MvpActionKeys.Search));
        }

        [Test]
        public void TryStartCooldown_FirstCall_Success()
        {
            Assert.IsTrue(_sut.TryStartCooldown(MvpActionKeys.Search, 1f));
            Assert.IsTrue(_sut.IsOnCooldown(MvpActionKeys.Search));
            Assert.AreEqual(1f, _sut.GetRemaining(MvpActionKeys.Search), 0.001f);
        }

        [Test]
        public void TryStartCooldown_WhileOnCooldown_Fails()
        {
            _sut.TryStartCooldown(MvpActionKeys.Search, 1f);
            Assert.IsFalse(_sut.TryStartCooldown(MvpActionKeys.Search, 1f));
        }

        [Test]
        public void TryStartCooldown_InvalidBaseSeconds_Throws()
        {
            Assert.Throws<ArgumentException>(() => _sut.TryStartCooldown(MvpActionKeys.Search, 0f));
            Assert.Throws<ArgumentException>(() => _sut.TryStartCooldown(MvpActionKeys.Search, -1f));
        }

        [Test]
        public void Tick_RemainingDecreasesToZero()
        {
            _sut.TryStartCooldown(MvpActionKeys.Search, 1f);
            _sut.Tick(0.5f);
            Assert.AreEqual(0.5f, _sut.GetRemaining(MvpActionKeys.Search), 0.001f);
            _sut.Tick(1f);
            Assert.AreEqual(0f, _sut.GetRemaining(MvpActionKeys.Search), 0.001f);
            Assert.IsFalse(_sut.IsOnCooldown(MvpActionKeys.Search));
        }

        [Test]
        public void Tick_NegativeDelta_Throws()
        {
            Assert.Throws<ArgumentException>(() => _sut.Tick(-0.1f));
        }

        [Test]
        public void ColdState_DoublesCooldown()
        {
            // 觸發進入寒冷：先 IsLit=true，再 IsLit=false（模擬火堆熄滅）
            EventBus.Publish(new MvpFireStateChangedEvent { IsLit = true, RemainingSeconds = 60f });
            EventBus.Publish(new MvpFireStateChangedEvent { IsLit = false, RemainingSeconds = 0f });
            Assert.IsTrue(_cold.IsCold);

            _sut.TryStartCooldown(MvpActionKeys.Search, 1f);
            // 倍率 2 → 實際 2 秒
            Assert.AreEqual(2f, _sut.GetRemaining(MvpActionKeys.Search), 0.001f);
        }

        [Test]
        public void NonColdState_UsesBaseCooldown()
        {
            // 點燃火堆（IsCold=false）
            EventBus.Publish(new MvpFireStateChangedEvent { IsLit = true, RemainingSeconds = 60f });
            Assert.IsFalse(_cold.IsCold);

            _sut.TryStartCooldown(MvpActionKeys.Search, 1f);
            Assert.AreEqual(1f, _sut.GetRemaining(MvpActionKeys.Search), 0.001f);
        }

        [Test]
        public void IndependentCooldownPerKey()
        {
            _sut.TryStartCooldown(MvpActionKeys.Search, 1f);
            _sut.TryStartCooldown(MvpActionKeys.HutBuild, 10f);
            Assert.IsTrue(_sut.IsOnCooldown(MvpActionKeys.Search));
            Assert.IsTrue(_sut.IsOnCooldown(MvpActionKeys.HutBuild));
            _sut.Tick(1.5f);
            Assert.IsFalse(_sut.IsOnCooldown(MvpActionKeys.Search));
            Assert.IsTrue(_sut.IsOnCooldown(MvpActionKeys.HutBuild));
        }
    }
}
