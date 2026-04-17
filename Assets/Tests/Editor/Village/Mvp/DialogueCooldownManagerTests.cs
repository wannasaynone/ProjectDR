using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village.Mvp;

namespace ProjectDR.Tests.Village.Mvp
{
    internal class StubDispatchProvider : IDispatchStateProvider
    {
        public HashSet<string> Dispatched { get; } = new HashSet<string>();
        public bool IsDispatched(string characterId) => Dispatched.Contains(characterId);
    }

    [TestFixture]
    public class DialogueCooldownManagerTests
    {
        private MvpConfig _config;
        private StubDispatchProvider _dispatch;
        private DialogueCooldownManager _sut;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _config = new MvpConfig(MvpTestConfig.MakeDefault());
            _dispatch = new StubDispatchProvider();
            _sut = new DialogueCooldownManager(_config, _dispatch);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        [Test]
        public void Initial_NotOnCooldown()
        {
            Assert.IsFalse(_sut.IsOnCooldown("A"));
        }

        [Test]
        public void TryStart_FirstCall_Succeeds()
        {
            Assert.IsTrue(_sut.TryStartPlayerDialogueCooldown("A"));
            Assert.IsTrue(_sut.IsOnCooldown("A"));
            Assert.AreEqual(30f, _sut.GetRemaining("A"), 0.001f);
        }

        [Test]
        public void TryStart_WhileOnCooldown_Fails()
        {
            _sut.TryStartPlayerDialogueCooldown("A");
            Assert.IsFalse(_sut.TryStartPlayerDialogueCooldown("A"));
        }

        [Test]
        public void Tick_DecreasesRemaining()
        {
            _sut.TryStartPlayerDialogueCooldown("A");
            _sut.Tick(10f);
            Assert.AreEqual(20f, _sut.GetRemaining("A"), 0.001f);
            _sut.Tick(30f);
            Assert.AreEqual(0f, _sut.GetRemaining("A"), 0.001f);
            Assert.IsFalse(_sut.IsOnCooldown("A"));
        }

        [Test]
        public void DispatchedCharacter_UsesMultiplier()
        {
            _dispatch.Dispatched.Add("A");
            _sut.TryStartPlayerDialogueCooldown("A");
            // 30 × 2 = 60
            Assert.AreEqual(60f, _sut.GetRemaining("A"), 0.001f);
        }

        [Test]
        public void IndependentCooldownPerCharacter()
        {
            _sut.TryStartPlayerDialogueCooldown("A");
            _sut.TryStartPlayerDialogueCooldown("B");
            Assert.IsTrue(_sut.IsOnCooldown("A"));
            Assert.IsTrue(_sut.IsOnCooldown("B"));
            _sut.Tick(30f);
            Assert.IsFalse(_sut.IsOnCooldown("A"));
            Assert.IsFalse(_sut.IsOnCooldown("B"));
        }
    }
}
