// MVP 整合流測試：完整跑通開局循環（搜索 → 生火 → 蓋屋 → NPC 來訪 → 對話）。
// 驗證多系統事件時序正確，無循環相依與多次事件重複觸發。

using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Mvp;

namespace ProjectDR.Tests.Village.Mvp
{
    [TestFixture]
    public class MvpIntegrationFlowTests
    {
        private MvpConfig _config;
        private ResourceManager _resource;
        private ColdStatusSystem _cold;
        private ActionTimeManager _actionTime;
        private FireSystem _fire;
        private PopulationManager _population;
        private HutBuildSystem _hut;
        private SearchSystem _search;
        private NpcArrivalManager _arrival;
        private NPCInitiativeManager _initiative;
        private DialogueCooldownManager _cooldown;
        private DialogueManager _dialogueManager;
        private AffinityManager _affinity;
        private MvpDialogueSession _session;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            _config = new MvpConfig(MvpTestConfig.MakeDefault());
            _resource = new ResourceManager();
            _cold = new ColdStatusSystem();
            _actionTime = new ActionTimeManager(_cold, _config.ColdActionCooldownMultiplier);
            _fire = new FireSystem(_resource, _config);
            _population = new PopulationManager(_config.InitialPopulationCap);
            _hut = new HutBuildSystem(_resource, _fire, _population, _config);
            _search = new SearchSystem(_resource, _actionTime, _config, new ZeroRandomSource());
            _arrival = new NpcArrivalManager(_population, _config, new ZeroRandomSource());
            _initiative = new NPCInitiativeManager(_config);
            _cooldown = new DialogueCooldownManager(_config, new NoDispatchProvider());
            _dialogueManager = new DialogueManager();
            _affinity = new AffinityManager(new AffinityConfig(new AffinityConfigData
            {
                characters = new AffinityCharacterConfigData[0],
                defaultThresholds = new[] { 5 }
            }));
            _session = new MvpDialogueSession(
                _dialogueManager, _affinity,
                _cooldown, _initiative,
                _config, new ZeroRandomSource());
        }

        [TearDown]
        public void TearDown()
        {
            _session?.Dispose();
            _initiative?.Dispose();
            _arrival?.Dispose();
            _cold?.Dispose();
            EventBus.ForceClearAll();
        }

        private void AdvanceTime(float totalSeconds, float step = 0.1f)
        {
            float elapsed = 0f;
            while (elapsed < totalSeconds)
            {
                float dt = System.Math.Min(step, totalSeconds - elapsed);
                _actionTime.Tick(dt);
                _fire.Tick(dt);
                _hut.Tick(dt);
                _search.Tick(dt);
                _initiative.Tick(dt);
                _cooldown.Tick(dt);
                elapsed += dt;
            }
        }

        [Test]
        public void FullFlow_Search_Fire_Hut_NpcArrive_Dialogue()
        {
            List<MvpNpcArrivedEvent> arrivedEvents = new List<MvpNpcArrivedEvent>();
            EventBus.Subscribe<MvpNpcArrivedEvent>(e => arrivedEvents.Add(e));

            // Step 1：搜索 5 次累積 5 木材
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(_search.TrySearch(), $"第 {i + 1} 次搜索應成功");
                AdvanceTime(1.01f);
            }
            Assert.AreEqual(5, _resource.GetAmount(MvpResourceIds.Wood));
            Assert.IsTrue(_fire.IsUnlocked);

            // Step 2：生火
            Assert.IsTrue(_fire.TryLight());
            Assert.IsTrue(_fire.IsLit);
            Assert.AreEqual(4, _resource.GetAmount(MvpResourceIds.Wood)); // 扣 1
            Assert.IsFalse(_cold.IsCold);

            // Step 3：搜索補木材到可蓋屋（現有 4，需 10，至少補 6）
            // 再搜索 6 次
            for (int i = 0; i < 6; i++)
            {
                Assert.IsTrue(_search.TrySearch());
                AdvanceTime(1.01f);
            }
            Assert.GreaterOrEqual(_resource.GetAmount(MvpResourceIds.Wood), 10);

            // Step 4：開工蓋屋
            Assert.IsTrue(_hut.TryStartBuild());
            Assert.IsTrue(_hut.IsBuilding);

            // Step 5：推進 10 秒蓋屋完成
            AdvanceTime(10.01f);
            Assert.IsFalse(_hut.IsBuilding);
            Assert.AreEqual(1, _population.Cap);
            Assert.AreEqual(1, _population.Count);
            Assert.AreEqual(1, arrivedEvents.Count, "蓋屋完成後應觸發 1 位 NPC 來訪");

            string npcId = arrivedEvents[0].CharacterId;

            // Step 6：對話
            Assert.IsTrue(_session.TryStartPlayerInitiatedDialogue(npcId));
            _session.AdvanceDialogue();
            Assert.IsFalse(_session.IsActive);
            Assert.AreEqual(3, _affinity.GetAffinity(npcId));
        }

        [Test]
        public void ColdStatus_DoublesActionCooldown_AndClearsOnReLight()
        {
            // 先拿到 5 木材點燃火堆
            _resource.Add(MvpResourceIds.Wood, 6);
            _fire.TryLight();
            Assert.IsTrue(_fire.IsLit);

            // 推進 60 秒讓火堆熄滅
            AdvanceTime(61f);
            Assert.IsFalse(_fire.IsLit);
            Assert.IsTrue(_cold.IsCold);

            // 搜索冷卻現在是 2 秒
            Assert.IsTrue(_search.TrySearch());
            Assert.AreEqual(2f, _actionTime.GetRemaining(MvpActionKeys.Search), 0.01f);

            // 重新生火 → 寒冷立即解除
            Assert.GreaterOrEqual(_resource.GetAmount(MvpResourceIds.Wood), 5);
            _fire.TryLight();
            Assert.IsFalse(_cold.IsCold);
        }

        [Test]
        public void NpcInitiative_RedDotFlow()
        {
            // 直接觸發 NPC arrived
            _population.IncreaseCap(1);
            Assert.AreEqual(1, _arrival.ArrivedCharacters.Count);
            string npcId = _arrival.ArrivedCharacters[0].characterId;

            // 推進 45 秒讓紅點亮
            AdvanceTime(45f);
            Assert.IsTrue(_initiative.IsReady(npcId));

            // 點對話（紅點路徑，方向為 CharacterInitiative）
            Assert.IsTrue(_session.TryStartPlayerInitiatedDialogue(npcId));
            Assert.AreEqual(MvpDialogueDirection.CharacterInitiative, _session.CurrentDirection);
            Assert.IsFalse(_initiative.IsReady(npcId));

            // 結束對話
            _session.AdvanceDialogue();
            Assert.AreEqual(3, _affinity.GetAffinity(npcId));
        }
    }
}
