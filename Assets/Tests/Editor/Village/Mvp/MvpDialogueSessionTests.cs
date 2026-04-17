using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Mvp;

namespace ProjectDR.Tests.Village.Mvp
{
    [TestFixture]
    public class MvpDialogueSessionTests
    {
        private DialogueManager _dialogueManager;
        private AffinityManager _affinity;
        private DialogueCooldownManager _cooldown;
        private NPCInitiativeManager _initiative;
        private MvpConfig _config;
        private MvpDialogueSession _sut;

        private List<MvpDialogueSessionStartedEvent> _startedEvents;
        private List<MvpDialogueSessionCompletedEvent> _completedEvents;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _dialogueManager = new DialogueManager();
            _config = new MvpConfig(MvpTestConfig.MakeDefault());

            AffinityConfigData cfgData = new AffinityConfigData
            {
                characters = new AffinityCharacterConfigData[0],
                defaultThresholds = new[] { 5 }
            };
            _affinity = new AffinityManager(new AffinityConfig(cfgData));

            _cooldown = new DialogueCooldownManager(_config, new NoDispatchProvider());
            _initiative = new NPCInitiativeManager(_config);

            _sut = new MvpDialogueSession(
                _dialogueManager, _affinity,
                _cooldown, _initiative,
                _config, new ZeroRandomSource());

            _startedEvents = new List<MvpDialogueSessionStartedEvent>();
            _completedEvents = new List<MvpDialogueSessionCompletedEvent>();
            EventBus.Subscribe<MvpDialogueSessionStartedEvent>(OnStart);
            EventBus.Subscribe<MvpDialogueSessionCompletedEvent>(OnComplete);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Unsubscribe<MvpDialogueSessionStartedEvent>(OnStart);
            EventBus.Unsubscribe<MvpDialogueSessionCompletedEvent>(OnComplete);
            _sut.Dispose();
            _initiative.Dispose();
            EventBus.ForceClearAll();
        }

        private void OnStart(MvpDialogueSessionStartedEvent e) => _startedEvents.Add(e);
        private void OnComplete(MvpDialogueSessionCompletedEvent e) => _completedEvents.Add(e);

        [Test]
        public void PlayerInitiated_FromIdle_StartsPlayerDirection()
        {
            bool ok = _sut.TryStartPlayerInitiatedDialogue("A");
            Assert.IsTrue(ok);
            Assert.IsTrue(_sut.IsActive);
            Assert.AreEqual(MvpDialogueDirection.PlayerInitiative, _sut.CurrentDirection);
            Assert.IsTrue(_cooldown.IsOnCooldown("A"));
            Assert.AreEqual(1, _startedEvents.Count);
        }

        [Test]
        public void PlayerInitiated_WhileOnCooldownAndNoRedDot_Fails()
        {
            _sut.TryStartPlayerInitiatedDialogue("A");
            // 第一次已推進對話並完成（只有一行）
            bool hasNext = _sut.AdvanceDialogue();
            Assert.IsFalse(hasNext); // 最後一行推進後完成
            Assert.IsFalse(_sut.IsActive);

            // 玩家冷卻中
            Assert.IsTrue(_cooldown.IsOnCooldown("A"));
            bool ok = _sut.TryStartPlayerInitiatedDialogue("A");
            Assert.IsFalse(ok);
        }

        [Test]
        public void PlayerInitiated_WithRedDot_ByPassesCooldownAndUsesCharacterDirection()
        {
            // 先讓玩家冷卻
            _sut.TryStartPlayerInitiatedDialogue("A");
            _sut.AdvanceDialogue(); // 完成對話
            Assert.IsTrue(_cooldown.IsOnCooldown("A"));

            // 讓角色 A 的 initiative Ready
            _initiative.RegisterCharacter("A");
            _initiative.Tick(45f);
            Assert.IsTrue(_initiative.IsReady("A"));

            // 即使冷卻中，點擊仍能進入（走 CharacterInitiative 路徑）
            bool ok = _sut.TryStartPlayerInitiatedDialogue("A");
            Assert.IsTrue(ok);
            Assert.AreEqual(MvpDialogueDirection.CharacterInitiative, _sut.CurrentDirection);
            Assert.IsFalse(_initiative.IsReady("A")); // 已消費
        }

        [Test]
        public void Completed_AddsAffinityAndPublishesEvent()
        {
            _sut.TryStartPlayerInitiatedDialogue("A");
            _sut.AdvanceDialogue();
            Assert.IsFalse(_sut.IsActive);
            Assert.AreEqual(3, _affinity.GetAffinity("A"));
            Assert.AreEqual(1, _completedEvents.Count);
            Assert.AreEqual(3, _completedEvents[0].AffinityGained);
        }

        [Test]
        public void Active_PreventsRestart()
        {
            _sut.TryStartPlayerInitiatedDialogue("A");
            Assert.IsTrue(_sut.IsActive);
            bool ok = _sut.TryStartPlayerInitiatedDialogue("B");
            Assert.IsFalse(ok);
        }

        [Test]
        public void CharacterInitiated_ExplicitPath_UsesCharacterDirection()
        {
            bool ok = _sut.TryStartCharacterInitiatedDialogue("A");
            Assert.IsTrue(ok);
            Assert.AreEqual(MvpDialogueDirection.CharacterInitiative, _sut.CurrentDirection);
            // 不啟動玩家冷卻
            Assert.IsFalse(_cooldown.IsOnCooldown("A"));
        }
    }
}
