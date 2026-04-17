using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village.Mvp;

namespace ProjectDR.Tests.Village.Mvp
{
    [TestFixture]
    public class HutBuildSystemTests
    {
        private ResourceManager _resource;
        private FireSystem _fire;
        private PopulationManager _population;
        private MvpConfig _config;
        private HutBuildSystem _sut;

        private List<MvpHutBuildStartedEvent> _startedEvents;
        private List<MvpHutBuildProgressEvent> _progressEvents;
        private List<MvpHutBuiltEvent> _builtEvents;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _resource = new ResourceManager();
            _config = new MvpConfig(MvpTestConfig.MakeDefault());
            _fire = new FireSystem(_resource, _config);
            _population = new PopulationManager(0);
            _sut = new HutBuildSystem(_resource, _fire, _population, _config);

            _startedEvents = new List<MvpHutBuildStartedEvent>();
            _progressEvents = new List<MvpHutBuildProgressEvent>();
            _builtEvents = new List<MvpHutBuiltEvent>();
            EventBus.Subscribe<MvpHutBuildStartedEvent>(OnStarted);
            EventBus.Subscribe<MvpHutBuildProgressEvent>(OnProgress);
            EventBus.Subscribe<MvpHutBuiltEvent>(OnBuilt);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Unsubscribe<MvpHutBuildStartedEvent>(OnStarted);
            EventBus.Unsubscribe<MvpHutBuildProgressEvent>(OnProgress);
            EventBus.Unsubscribe<MvpHutBuiltEvent>(OnBuilt);
            EventBus.ForceClearAll();
        }

        private void OnStarted(MvpHutBuildStartedEvent e) => _startedEvents.Add(e);
        private void OnProgress(MvpHutBuildProgressEvent e) => _progressEvents.Add(e);
        private void OnBuilt(MvpHutBuiltEvent e) => _builtEvents.Add(e);

        private void LightFireForTest()
        {
            _resource.Add(MvpResourceIds.Wood, 5);
            _fire.TryLight();
        }

        [Test]
        public void IsUnlocked_BeforeFire_False()
        {
            Assert.IsFalse(_sut.IsUnlocked);
        }

        [Test]
        public void IsUnlocked_AfterFire_True()
        {
            LightFireForTest();
            Assert.IsTrue(_sut.IsUnlocked);
        }

        [Test]
        public void TryStartBuild_NotUnlocked_Fails()
        {
            _resource.Add(MvpResourceIds.Wood, 100);
            Assert.IsFalse(_sut.TryStartBuild());
        }

        [Test]
        public void TryStartBuild_InsufficientWood_Fails()
        {
            LightFireForTest(); // 剩 4 木材
            // 尚未湊到 10
            Assert.IsFalse(_sut.TryStartBuild());
        }

        [Test]
        public void TryStartBuild_Success_ConsumesWood()
        {
            LightFireForTest();
            _resource.Add(MvpResourceIds.Wood, 10); // 4 + 10 = 14
            bool ok = _sut.TryStartBuild();
            Assert.IsTrue(ok);
            Assert.IsTrue(_sut.IsBuilding);
            Assert.AreEqual(4, _resource.GetAmount(MvpResourceIds.Wood)); // 扣 10
            Assert.AreEqual(1, _startedEvents.Count);
            Assert.AreEqual(10f, _startedEvents[0].TotalSeconds, 0.001f);
        }

        [Test]
        public void TryStartBuild_AlreadyBuilding_Fails()
        {
            LightFireForTest();
            _resource.Add(MvpResourceIds.Wood, 30);
            _sut.TryStartBuild();
            Assert.IsFalse(_sut.TryStartBuild());
        }

        [Test]
        public void Tick_ProgressesAndCompletes()
        {
            LightFireForTest();
            _resource.Add(MvpResourceIds.Wood, 10);
            _sut.TryStartBuild();

            _sut.Tick(5f);
            Assert.IsTrue(_sut.IsBuilding);
            Assert.AreEqual(5f, _sut.ElapsedSeconds, 0.001f);
            Assert.AreEqual(1, _progressEvents.Count);

            _sut.Tick(5f);
            Assert.IsFalse(_sut.IsBuilding);
            Assert.AreEqual(1, _builtEvents.Count);
            Assert.AreEqual(1, _builtEvents[0].PopulationCapIncrement);
            Assert.AreEqual(1, _population.Cap); // 人口上限 +1
        }

        [Test]
        public void Tick_Exceeds_ClampsToTotal()
        {
            LightFireForTest();
            _resource.Add(MvpResourceIds.Wood, 10);
            _sut.TryStartBuild();
            _sut.Tick(20f);
            Assert.IsFalse(_sut.IsBuilding);
            Assert.AreEqual(10f, _sut.ElapsedSeconds, 0.001f);
            Assert.AreEqual(1, _builtEvents.Count);
        }
    }
}
