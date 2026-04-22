// RedDotManager 紅點下沉查詢測試（Sprint 5 B3）。
//
// 驗證：
// - IsLayerActive(characterId, layer) 可獨立查詢個別層啟用狀態
// - 即使 HighestLayer 被 L1/L4 覆蓋，L2/L3 的 IsLayerActive 仍回 true
// - L1/L4 播完後 L2/L3 保留（清 L1 後 IsLayerActive(L2) 仍為 true）

using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Progression;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Tests.Village
{
    [TestFixture]
    public class RedDotLayerSubsinkTests
    {
        private MainQuestConfig _config;
        private MainQuestManager _questManager;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _config = new MainQuestConfig(new MainQuestConfigData
            {
                main_quests = new MainQuestConfigEntry[]
                {
                    new MainQuestConfigEntry
                    {
                        id = 1,
                        quest_id = "T0",
                        display_name = "T0",
                        completion_condition_type = "auto",
                        owner_character_id = CharacterIds.VillageChiefWife,
                        sort_order = 0,
                    },
                }
            });
            _questManager = new MainQuestManager(_config);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        [Test]
        public void IsLayerActive_NoFlag_ReturnsFalse()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                Assert.IsFalse(sut.IsLayerActive(CharacterIds.FarmGirl, RedDotLayer.CharacterQuestion));
                Assert.IsFalse(sut.IsLayerActive(CharacterIds.FarmGirl, RedDotLayer.NewQuest));
            }
        }

        [Test]
        public void IsLayerActive_L2Only_ReturnsTrueForL2()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                EventBus.Publish(new CharacterQuestionCountdownReadyEvent
                {
                    CharacterId = CharacterIds.FarmGirl,
                });
                Assert.IsTrue(sut.IsLayerActive(CharacterIds.FarmGirl, RedDotLayer.CharacterQuestion));
                Assert.IsFalse(sut.IsLayerActive(CharacterIds.FarmGirl, RedDotLayer.NewQuest));
            }
        }

        [Test]
        public void IsLayerActive_L1AndL2Coexist_BothReturnTrue()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                // 先啟用 L2
                EventBus.Publish(new CharacterQuestionCountdownReadyEvent
                {
                    CharacterId = CharacterIds.FarmGirl,
                });
                // 再啟用 L1
                EventBus.Publish(new CommissionCompletedEvent
                {
                    CharacterId = CharacterIds.FarmGirl,
                });

                // HighestLayer 為 L1
                Assert.AreEqual(RedDotLayer.CommissionCompleted,
                    sut.GetHubRedDot(CharacterIds.FarmGirl).HighestLayer);
                // L2 依然啟用
                Assert.IsTrue(sut.IsLayerActive(CharacterIds.FarmGirl, RedDotLayer.CharacterQuestion));
                Assert.IsTrue(sut.IsLayerActive(CharacterIds.FarmGirl, RedDotLayer.CommissionCompleted));
            }
        }

        [Test]
        public void IsLayerActive_L1Cleared_L2Preserved()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                EventBus.Publish(new CharacterQuestionCountdownReadyEvent
                {
                    CharacterId = CharacterIds.FarmGirl,
                });
                EventBus.Publish(new CommissionCompletedEvent
                {
                    CharacterId = CharacterIds.FarmGirl,
                });
                // 委託領取 → L1 清除
                EventBus.Publish(new CommissionClaimedEvent
                {
                    CharacterId = CharacterIds.FarmGirl,
                });

                Assert.IsFalse(sut.IsLayerActive(CharacterIds.FarmGirl, RedDotLayer.CommissionCompleted));
                Assert.IsTrue(sut.IsLayerActive(CharacterIds.FarmGirl, RedDotLayer.CharacterQuestion));
                Assert.AreEqual(RedDotLayer.CharacterQuestion,
                    sut.GetHubRedDot(CharacterIds.FarmGirl).HighestLayer);
            }
        }

        [Test]
        public void IsLayerActive_NullCharacterId_ReturnsFalse()
        {
            using (RedDotManager sut = new RedDotManager(_config, _questManager))
            {
                Assert.IsFalse(sut.IsLayerActive(null, RedDotLayer.CharacterQuestion));
                Assert.IsFalse(sut.IsLayerActive(string.Empty, RedDotLayer.NewQuest));
            }
        }
    }
}
