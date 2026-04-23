// IdleChatConfig + IdleChatPresenter 單元測試（Sprint 5 B12）。
// Sprint 8 Wave 2.5：配合純陣列 DTO 重構（廢棄 IdleChatConfigData 包裹類，改兩個獨立陣列）。

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
        public void Constructor_NullTopicEntries_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new IdleChatConfig(null, new IdleChatAnswerData[0]));
        }

        [Test]
        public void Constructor_NullAnswerEntries_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new IdleChatConfig(new IdleChatTopicData[0], null));
        }

        [Test]
        public void GetTopicsForCharacter_Empty_ReturnsEmpty()
        {
            IdleChatConfig cfg = new IdleChatConfig(new IdleChatTopicData[0], new IdleChatAnswerData[0]);
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

        // ===== ADR-001 / ADR-002 A10：IGameData 契約斷言 =====

        [Test]
        public void IdleChatTopicData_ImplementsIGameData()
        {
            IdleChatTopicData entry = new IdleChatTopicData
            {
                id = 1,
                character_id = "village_chief_wife",
                topic_id = "topic_test",
                prompt = "test prompt"
            };

            Assert.That(entry, Is.AssignableTo<KahaGameCore.GameData.IGameData>(),
                "IdleChatTopicData 必須實作 IGameData（ADR-001 / ADR-002 A10）");
            Assert.That(entry.ID, Is.Not.Zero,
                "IdleChatTopicData.ID 不得為 0（ADR-002 A10 反序列化要求）");
            Assert.That(entry.Key, Is.EqualTo("topic_test"),
                "IdleChatTopicData.Key 應回傳 topic_id");
        }

        [Test]
        public void IdleChatAnswerData_ImplementsIGameData()
        {
            IdleChatAnswerData entry = new IdleChatAnswerData
            {
                id = 1,
                answer_id = "a_001",
                topic_id = "topic_test",
                text = "test answer"
            };

            Assert.That(entry, Is.AssignableTo<KahaGameCore.GameData.IGameData>(),
                "IdleChatAnswerData 必須實作 IGameData（ADR-001）");
            Assert.That(entry.ID, Is.Not.Zero,
                "IdleChatAnswerData.ID 不得為 0");
            Assert.That(entry.Key, Is.EqualTo("a_001"),
                "IdleChatAnswerData.Key 應回傳 answer_id");
        }

        private static IdleChatConfig Build()
        {
            IdleChatTopicData[] topics = new IdleChatTopicData[]
            {
                new IdleChatTopicData { id = 1, character_id = "village_chief_wife", topic_id = "t1", prompt = "p1" },
                new IdleChatTopicData { id = 2, character_id = "village_chief_wife", topic_id = "t2", prompt = "p2" },
            };

            IdleChatAnswerData[] answers = new IdleChatAnswerData[]
            {
                new IdleChatAnswerData { id = 1, topic_id = "t1", answer_id = "a1", text = "aa" },
                new IdleChatAnswerData { id = 2, topic_id = "t1", answer_id = "a2", text = "bb" },
                new IdleChatAnswerData { id = 3, topic_id = "t2", answer_id = "a3", text = "xx" },
            };

            return new IdleChatConfig(topics, answers);
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
            IdleChatTopicData[] topics = new IdleChatTopicData[]
            {
                new IdleChatTopicData { id = 1, character_id = "farm_girl", topic_id = "ft1", prompt = "p1" },
            };

            IdleChatAnswerData[] answers = new IdleChatAnswerData[]
            {
                new IdleChatAnswerData { id = 1, topic_id = "ft1", answer_id = "a1", text = "Hello!" },
                new IdleChatAnswerData { id = 2, topic_id = "ft1", answer_id = "a2", text = "Hi!" },
            };

            return new IdleChatConfig(topics, answers);
        }
    }
}
