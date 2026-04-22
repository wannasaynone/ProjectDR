// IdleChatConfig + IdleChatPresenter 單元測試（Sprint 5 B12）。

using System.IO;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.IdleChat;
using ProjectDR.Village.CharacterUnlock;
using UnityEngine;

namespace ProjectDR.Village.Tests
{
    [TestFixture]
    public class IdleChatConfigTests
    {
        [Test]
        public void Constructor_NullData_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new IdleChatConfig(null));
        }

        [Test]
        public void GetTopicsForCharacter_Empty_ReturnsEmpty()
        {
            IdleChatConfig cfg = new IdleChatConfig(new IdleChatConfigData());
            Assert.AreEqual(0, cfg.GetTopicsForCharacter(CharacterIds.FarmGirl).Count);
        }

        [Test]
        public void GetTopic_NullId_ReturnsNull()
        {
            IdleChatConfig cfg = Build();
            Assert.IsNull(cfg.GetTopic(null));
            Assert.IsNull(cfg.GetTopic("nope"));
        }

        [Test]
        public void GetTopicsForCharacter_ReturnsTwoTopics()
        {
            IdleChatConfig cfg = Build();
            Assert.AreEqual(2, cfg.GetTopicsForCharacter(CharacterIds.VillageChiefWife).Count);
        }

        [Test]
        public void RealJson_4Characters_20TopicsEach()
        {
            UnityEngine.TextAsset asset =
                UnityEngine.Resources.Load<UnityEngine.TextAsset>("Config/idle-chat-config");
            if (asset == null)
            {
                Assert.Ignore("idle-chat-config.json 不在 Resources，跳過真實 JSON 測試。");
                return;
            }
            IdleChatConfigData data = JsonUtility.FromJson<IdleChatConfigData>(asset.text);
            Assert.IsNotNull(data);
            IdleChatConfig cfg = new IdleChatConfig(data);

            foreach (string c in new[] { CharacterIds.VillageChiefWife, CharacterIds.FarmGirl,
                                          CharacterIds.Witch, CharacterIds.Guard })
            {
                var topics = cfg.GetTopicsForCharacter(c);
                Assert.AreEqual(20, topics.Count, $"{c} should have 20 topics");
                foreach (IdleChatTopic t in topics)
                    Assert.AreEqual(3, t.Answers.Count, $"{t.TopicId} should have 3 answers");
            }
        }

        // ===== ADR-001 / ADR-002 A10：IGameData 契約斷言 =====

        [Test]
        public void IdleChatTopicData_ImplementsIGameData()
        {
            IdleChatTopicData entry = new IdleChatTopicData
            {
                id = 1,
                character_id = CharacterIds.VillageChiefWife,
                topic_id = "topic_test",
                prompt = "test prompt",
                answers = new IdleChatAnswerData[0]
            };

            Assert.That(entry, Is.AssignableTo<KahaGameCore.GameData.IGameData>(),
                "IdleChatTopicData 必須實作 IGameData（ADR-001 / ADR-002 A10）");
            Assert.That(entry.ID, Is.Not.Zero,
                "IdleChatTopicData.ID 不得為 0（ADR-002 A10 反序列化要求）");
            Assert.That(entry.Key, Is.EqualTo("topic_test"),
                "IdleChatTopicData.Key 應回傳 topic_id");
        }

        private static IdleChatConfig Build()
        {
            return new IdleChatConfig(new IdleChatConfigData
            {
                topics = new IdleChatTopicData[]
                {
                    new IdleChatTopicData
                    {
                        id = 1, character_id = CharacterIds.VillageChiefWife, topic_id="t1", prompt="p1",
                        answers = new IdleChatAnswerData[]
                        {
                            new IdleChatAnswerData{ answer_id="a1", text="aa" },
                            new IdleChatAnswerData{ answer_id="a2", text="bb" },
                        }
                    },
                    new IdleChatTopicData
                    {
                        id = 2, character_id = CharacterIds.VillageChiefWife, topic_id="t2", prompt="p2",
                        answers = new IdleChatAnswerData[]
                        {
                            new IdleChatAnswerData{ answer_id="a1", text="xx" },
                        }
                    },
                },
            });
        }
    }

    [TestFixture]
    public class IdleChatPresenterTests
    {
        [SetUp] public void SetUp() { EventBus.ForceClearAll(); }
        [TearDown] public void TearDown() { EventBus.ForceClearAll(); }

        [Test]
        public void Trigger_NullCharacter_ReturnsNull()
        {
            IdleChatPresenter p = new IdleChatPresenter(BuildConfig(), seed: 1);
            Assert.IsNull(p.Trigger(null));
        }

        [Test]
        public void Trigger_ReturnsTopicAndAnswer()
        {
            IdleChatPresenter p = new IdleChatPresenter(BuildConfig(), seed: 1);
            IdleChatResult r = p.Trigger(CharacterIds.FarmGirl);
            Assert.IsNotNull(r);
            Assert.AreEqual(CharacterIds.FarmGirl, r.CharacterId);
            Assert.IsFalse(string.IsNullOrEmpty(r.TopicId));
            Assert.IsFalse(string.IsNullOrEmpty(r.Answer));
        }

        [Test]
        public void Trigger_PublishesEvent()
        {
            IdleChatTriggeredEvent rec = null;
            System.Action<IdleChatTriggeredEvent> h = e => rec = e;
            EventBus.Subscribe(h);
            try
            {
                IdleChatPresenter p = new IdleChatPresenter(BuildConfig(), seed: 1);
                p.Trigger(CharacterIds.FarmGirl);
                Assert.IsNotNull(rec);
                Assert.AreEqual(CharacterIds.FarmGirl, rec.CharacterId);
            }
            finally { EventBus.Unsubscribe(h); }
        }

        [Test]
        public void Trigger_ConfigMissingChar_ReturnsNull()
        {
            IdleChatPresenter p = new IdleChatPresenter(BuildConfig(), seed: 1);
            Assert.IsNull(p.Trigger("no_such_character"));
        }

        private static IdleChatConfig BuildConfig()
        {
            return new IdleChatConfig(new IdleChatConfigData
            {
                topics = new IdleChatTopicData[]
                {
                    new IdleChatTopicData
                    {
                        character_id = CharacterIds.FarmGirl, topic_id="ft1", prompt="p1",
                        answers = new IdleChatAnswerData[]
                        {
                            new IdleChatAnswerData{ answer_id="a1", text="Hello!" },
                            new IdleChatAnswerData{ answer_id="a2", text="Hi!" },
                        }
                    },
                },
            });
        }
    }
}
