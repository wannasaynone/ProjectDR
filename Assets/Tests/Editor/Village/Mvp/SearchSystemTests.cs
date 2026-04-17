using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village.Mvp;

namespace ProjectDR.Tests.Village.Mvp
{
    [TestFixture]
    public class SearchSystemTests
    {
        private ResourceManager _resource;
        private ColdStatusSystem _cold;
        private ActionTimeManager _actionTime;
        private MvpConfig _config;
        private SearchSystem _sut;
        private List<MvpSearchCompletedEvent> _events;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _resource = new ResourceManager();
            _cold = new ColdStatusSystem();
            _config = new MvpConfig(MvpTestConfig.MakeDefault());
            _actionTime = new ActionTimeManager(_cold, _config.ColdActionCooldownMultiplier);
            _sut = new SearchSystem(_resource, _actionTime, _config, new ZeroRandomSource());
            _events = new List<MvpSearchCompletedEvent>();
            EventBus.Subscribe<MvpSearchCompletedEvent>(OnEvt);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Unsubscribe<MvpSearchCompletedEvent>(OnEvt);
            _cold.Dispose();
            EventBus.ForceClearAll();
        }

        private void OnEvt(MvpSearchCompletedEvent e) => _events.Add(e);

        [Test]
        public void CanSearch_Initial_True()
        {
            Assert.IsTrue(_sut.CanSearch);
        }

        [Test]
        public void TrySearch_PendingUntilCooldownEnds()
        {
            bool ok = _sut.TrySearch();
            Assert.IsTrue(ok);
            Assert.IsTrue(_sut.IsPending);
            // 冷卻中還未結算，木材不變
            Assert.AreEqual(0, _resource.GetAmount(MvpResourceIds.Wood));
            Assert.AreEqual(0, _events.Count);
        }

        [Test]
        public void Tick_AfterCooldown_AddsWoodAndPublishesEvent()
        {
            _sut.TrySearch();
            _actionTime.Tick(1f);
            _sut.Tick(1f);
            Assert.AreEqual(1, _resource.GetAmount(MvpResourceIds.Wood));
            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual("撿到木頭。", _events[0].FeedbackLine); // ZeroRandom 取索引 0
            Assert.AreEqual(1, _events[0].WoodGained);
            Assert.IsFalse(_sut.IsPending);
        }

        [Test]
        public void Tick_BeforeCooldownEnds_DoesNotResolve()
        {
            _sut.TrySearch();
            _actionTime.Tick(0.5f);
            _sut.Tick(0.5f);
            Assert.IsTrue(_sut.IsPending);
            Assert.AreEqual(0, _resource.GetAmount(MvpResourceIds.Wood));
            Assert.AreEqual(0, _events.Count);
        }

        [Test]
        public void TrySearch_WhilePending_Fails()
        {
            _sut.TrySearch();
            bool ok2 = _sut.TrySearch();
            Assert.IsFalse(ok2);
            Assert.AreEqual(0, _resource.GetAmount(MvpResourceIds.Wood));
        }

        [Test]
        public void TrySearch_AfterResolve_Succeeds()
        {
            _sut.TrySearch();
            _actionTime.Tick(1f);
            _sut.Tick(1f);
            bool ok = _sut.TrySearch();
            Assert.IsTrue(ok);
            Assert.IsTrue(_sut.IsPending);
            _actionTime.Tick(1f);
            _sut.Tick(1f);
            Assert.AreEqual(2, _resource.GetAmount(MvpResourceIds.Wood));
        }

        [Test]
        public void TrySearch_WhileCold_UsesDoubledCooldown()
        {
            // 進入寒冷
            EventBus.Publish(new MvpFireStateChangedEvent { IsLit = true });
            EventBus.Publish(new MvpFireStateChangedEvent { IsLit = false });
            Assert.IsTrue(_cold.IsCold);

            _sut.TrySearch();
            // 冷卻實際為 2 秒（1 × 2）
            Assert.AreEqual(2f, _actionTime.GetRemaining(MvpActionKeys.Search), 0.001f);
            _actionTime.Tick(1.5f);
            _sut.Tick(1.5f);
            Assert.IsFalse(_sut.CanSearch); // 尚未結束
            Assert.IsTrue(_sut.IsPending);
            _actionTime.Tick(1f);
            _sut.Tick(1f);
            Assert.IsTrue(_sut.CanSearch);
            Assert.AreEqual(1, _resource.GetAmount(MvpResourceIds.Wood));
        }
    }
}
