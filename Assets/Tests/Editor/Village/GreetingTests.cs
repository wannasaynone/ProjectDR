// GreetingConfig + GreetingPresenter 單元測試（Sprint 5 B15/B16/B18）。

using System.IO;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using UnityEngine;

namespace ProjectDR.Village.Tests
{
    [TestFixture]
    public class GreetingConfigTests
    {
        [Test]
        public void Constructor_NullData_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new GreetingConfig(null));
        }

        [Test]
        public void Constructor_EmptyData_ReturnsEmpty()
        {
            GreetingConfig cfg = new GreetingConfig(new GreetingConfigData());
            Assert.AreEqual(0, cfg.GetGreetings(CharacterIds.FarmGirl, 1).Count);
        }

        [Test]
        public void GetGreetings_ReturnsCorrectPool()
        {
            GreetingConfig cfg = Build();
            var lv1 = cfg.GetGreetings(CharacterIds.VillageChiefWife, 1);
            Assert.AreEqual(2, lv1.Count);
        }

        [Test]
        public void RealJson_4Chars_7Levels_10EachLevel()
        {
            UnityEngine.TextAsset asset =
                UnityEngine.Resources.Load<UnityEngine.TextAsset>("Config/greeting-config");
            if (asset == null)
            {
                Assert.Ignore("greeting-config.json 不在 Resources，跳過真實 JSON 測試。");
                return;
            }
            GreetingConfigData data = JsonUtility.FromJson<GreetingConfigData>(asset.text);
            GreetingConfig cfg = new GreetingConfig(data);
            int total = 0;
            foreach (string c in new[] { CharacterIds.VillageChiefWife, CharacterIds.FarmGirl,
                                          CharacterIds.Witch, CharacterIds.Guard })
            {
                for (int level = 1; level <= 7; level++)
                {
                    int n = cfg.GetGreetings(c, level).Count;
                    Assert.AreEqual(10, n, $"{c} Lv{level} should have 10 greetings");
                    total += n;
                }
            }
            Assert.AreEqual(280, total);
        }

        private static GreetingConfig Build()
        {
            return new GreetingConfig(new GreetingConfigData
            {
                greetings = new GreetingEntryData[]
                {
                    new GreetingEntryData{ character_id=CharacterIds.VillageChiefWife, level=1, greeting_id="g1", text="a" },
                    new GreetingEntryData{ character_id=CharacterIds.VillageChiefWife, level=1, greeting_id="g2", text="b" },
                    new GreetingEntryData{ character_id=CharacterIds.VillageChiefWife, level=2, greeting_id="g3", text="c" },
                }
            });
        }
    }

    [TestFixture]
    public class GreetingPresenterTests
    {
        private GreetingConfig _config;
        private MainQuestConfig _mqConfig;
        private MainQuestManager _mqManager;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _config = BuildConfig();
            _mqConfig = new MainQuestConfig(new MainQuestConfigData
            {
                main_quests = new MainQuestConfigEntry[0]
            });
            _mqManager = new MainQuestManager(_mqConfig);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        [Test]
        public void TryGreet_NoRedDot_ReturnsGreeting()
        {
            using (RedDotManager rd = new RedDotManager(_mqConfig, _mqManager))
            {
                GreetingPresenter p = new GreetingPresenter(_config, rd, seed: 1);
                GreetingInfo info = p.TryGreet(CharacterIds.VillageChiefWife, 1);
                Assert.IsNotNull(info);
            }
        }

        [Test]
        public void TryGreet_L2RedDot_StillPlays()
        {
            using (RedDotManager rd = new RedDotManager(_mqConfig, _mqManager))
            {
                // L2 紅點亮 → 仍應播招呼（規格：L2/L3 不取代招呼）
                EventBus.Publish(new CharacterQuestionCountdownReadyEvent
                {
                    CharacterId = CharacterIds.VillageChiefWife,
                });
                GreetingPresenter p = new GreetingPresenter(_config, rd, seed: 1);
                GreetingInfo info = p.TryGreet(CharacterIds.VillageChiefWife, 1);
                Assert.IsNotNull(info);
            }
        }

        [Test]
        public void TryGreet_L1RedDot_Suppressed()
        {
            using (RedDotManager rd = new RedDotManager(_mqConfig, _mqManager))
            {
                EventBus.Publish(new CommissionCompletedEvent
                {
                    CharacterId = CharacterIds.VillageChiefWife,
                });
                GreetingPresenter p = new GreetingPresenter(_config, rd, seed: 1);
                GreetingInfo info = p.TryGreet(CharacterIds.VillageChiefWife, 1);
                Assert.IsNull(info);
            }
        }

        [Test]
        public void TryGreet_L4RedDot_Suppressed()
        {
            using (RedDotManager rd = new RedDotManager(_mqConfig, _mqManager))
            {
                rd.SetMainQuestEventFlag(CharacterIds.VillageChiefWife, true);
                GreetingPresenter p = new GreetingPresenter(_config, rd, seed: 1);
                Assert.IsNull(p.TryGreet(CharacterIds.VillageChiefWife, 1));
            }
        }

        [Test]
        public void TryGreet_PublishesEvent()
        {
            using (RedDotManager rd = new RedDotManager(_mqConfig, _mqManager))
            {
                GreetingPlayedEvent received = null;
                System.Action<GreetingPlayedEvent> h = e => received = e;
                EventBus.Subscribe(h);
                try
                {
                    GreetingPresenter p = new GreetingPresenter(_config, rd, seed: 1);
                    p.TryGreet(CharacterIds.VillageChiefWife, 1);
                    Assert.IsNotNull(received);
                    Assert.AreEqual(CharacterIds.VillageChiefWife, received.CharacterId);
                }
                finally { EventBus.Unsubscribe(h); }
            }
        }

        [Test]
        public void TryGreet_EmptyPool_ReturnsNull()
        {
            using (RedDotManager rd = new RedDotManager(_mqConfig, _mqManager))
            {
                GreetingPresenter p = new GreetingPresenter(_config, rd, seed: 1);
                Assert.IsNull(p.TryGreet(CharacterIds.VillageChiefWife, 99));
            }
        }

        [Test]
        public void TryGreet_NullRedDotManager_Works()
        {
            GreetingPresenter p = new GreetingPresenter(_config, null, seed: 1);
            Assert.IsNotNull(p.TryGreet(CharacterIds.VillageChiefWife, 1));
        }

        private static GreetingConfig BuildConfig()
        {
            return new GreetingConfig(new GreetingConfigData
            {
                greetings = new GreetingEntryData[]
                {
                    new GreetingEntryData{ character_id=CharacterIds.VillageChiefWife, level=1, greeting_id="g_vcw_1_1", text="Welcome" },
                    new GreetingEntryData{ character_id=CharacterIds.VillageChiefWife, level=1, greeting_id="g_vcw_1_2", text="Hi" },
                }
            });
        }
    }
}
