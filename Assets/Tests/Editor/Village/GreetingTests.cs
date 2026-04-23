// GreetingConfig + GreetingPresenter 單元測試（Sprint 5 B15/B16/B18）。
// Sprint 8 Wave 2.5：配合純陣列 DTO 重構（GreetingData[]，廢棄 GreetingConfigData 包裹類）。

using System.IO;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Progression;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.Greeting;
using ProjectDR.Village.CharacterUnlock;
using UnityEngine;

namespace ProjectDR.Village.Tests
{
    [TestFixture]
    public class GreetingConfigTests
    {
        [Test]
        public void Constructor_NullEntries_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new GreetingConfig(null));
        }

        [Test]
        public void Constructor_EmptyEntries_ReturnsEmpty()
        {
            GreetingConfig cfg = new GreetingConfig(new GreetingData[0]);
            Assert.AreEqual(0, cfg.GetGreetings(CharacterIds.FarmGirl, 1).Count);
        }

        [Test]
        public void GetGreetings_ReturnsCorrectPool()
        {
            GreetingConfig cfg = Build();
            var lv1 = cfg.GetGreetings(CharacterIds.VillageChiefWife, 1);
            Assert.AreEqual(2, lv1.Count);
        }

        // ===== ADR-001 / ADR-002 A07：IGameData 契約斷言 =====

        [Test]
        public void GreetingData_ImplementsIGameData()
        {
            GreetingData entry = new GreetingData
            {
                id = 1,
                character_id = "village_chief_wife",
                level = 1,
                greeting_id = "g_test",
                text = "test"
            };

            Assert.That(entry, Is.AssignableTo<KahaGameCore.GameData.IGameData>(),
                "GreetingData 必須實作 IGameData（ADR-001 / ADR-002 A07）");
            Assert.That(entry.ID, Is.Not.Zero,
                "GreetingData.ID 不得為 0（ADR-002 A07 反序列化要求）");
            Assert.That(entry.Key, Is.EqualTo("g_test"),
                "GreetingData.Key 應回傳 greeting_id");
        }

        private static GreetingConfig Build()
        {
            return new GreetingConfig(new GreetingData[]
            {
                new GreetingData{ id=1, character_id="village_chief_wife", level=1, greeting_id="g1", text="a" },
                new GreetingData{ id=2, character_id="village_chief_wife", level=1, greeting_id="g2", text="b" },
                new GreetingData{ id=3, character_id="village_chief_wife", level=2, greeting_id="g3", text="c" },
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
            // Sprint 8 Wave 2.5：使用純陣列建構子
            _mqConfig = new MainQuestConfig(new MainQuestData[0], new MainQuestUnlockData[0]);
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
            return new GreetingConfig(new GreetingData[]
            {
                new GreetingData{ id=1, character_id="village_chief_wife", level=1, greeting_id="g_vcw_1_1", text="Welcome" },
                new GreetingData{ id=2, character_id="village_chief_wife", level=1, greeting_id="g_vcw_1_2", text="Hi" },
            });
        }
    }
}
