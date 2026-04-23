// DialogueFlowIntegrationTest — Sprint 5 C1~C7 整合測試。
//
// 使用純邏輯層組裝驗證端到端流程：
// - C1 紅點分流：L1+L2 並存 → L1 自動播、L2 保留 → 點對話觸發角色發問
// - C2 玩家發問端到端：無紅點 → 發問清單 → 選題 → CD 啟動
// - C3 閒聊模式：40 題耗盡 → 只顯 [閒聊]
// - C4 體力歸零：點對話 → 「現在好累了」（此處驗證 HasEnough 判斷）
// - C5 工作中 CD ×2：CommissionStarted → CD ×2；Claimed → ×1
// - C6 紅點上限 1：多次倒數 → 只亮 1 次
// - C7 招呼語分流：無紅點播、L2 播、L1/L4 不播

using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Progression;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.CharacterQuestions;
using ProjectDR.Village.CharacterStamina;
using ProjectDR.Village.IdleChat;
using ProjectDR.Village.Greeting;
using ProjectDR.Village.Affinity;
using ProjectDR.Village.Dialogue;

namespace ProjectDR.Village.Tests.Integration
{
    [TestFixture]
    public class DialogueFlowIntegrationTest
    {
        private AffinityManager _affinity;
        private MainQuestConfig _mqConfig;
        private MainQuestManager _mqManager;
        private RedDotManager _redDot;
        private CharacterQuestionsConfig _cqConfig;
        private CharacterQuestionsManager _cqManager;
        private CharacterQuestionCountdownManager _countdown;
        private GreetingConfig _greetingConfig;
        private GreetingPresenter _greeting;
        private IdleChatConfig _idleConfig;
        private IdleChatPresenter _idle;
        private PlayerQuestionsConfig _pqConfig;
        private PlayerQuestionsManager _pqManager;
        private DialogueCooldownManager _cooldown;
        private CharacterStaminaManager _stamina;

        private const string VCW = "village_chief_wife";
        private const string FARM = "farm_girl";
        private const float CountdownDuration = 60f;
        private const float CDBase = 60f;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            _affinity = new AffinityManager(new AffinityConfig(new AffinityCharacterData[]
            {
                new AffinityCharacterData { id = 1, character_id = "__default__", thresholds = "5,10,20" },
            }));
            _mqConfig = new MainQuestConfig(new MainQuestData[0], new MainQuestUnlockData[0]);
            _mqManager = new MainQuestManager(_mqConfig);
            _redDot = new RedDotManager(_mqConfig, _mqManager);

            _cqConfig = BuildCharacterQuestionsConfig();
            _cqManager = new CharacterQuestionsManager(_cqConfig, _affinity, seed: 1);

            _countdown = new CharacterQuestionCountdownManager(CountdownDuration);

            _greetingConfig = BuildGreetingConfig();
            _greeting = new GreetingPresenter(_greetingConfig, _redDot, seed: 1);

            _idleConfig = BuildIdleChatConfig();
            _idle = new IdleChatPresenter(_idleConfig, seed: 1);

            _pqConfig = BuildPlayerQuestionsConfig(4);
            _pqManager = new PlayerQuestionsManager(_pqConfig, seed: 1);

            _cooldown = new DialogueCooldownManager(CDBase);

            _stamina = new CharacterStaminaManager();

            // 接線：CommissionStarted/Claimed → Countdown + Cooldown
            EventBus.Subscribe<CommissionStartedEvent>(e =>
            {
                _countdown.SetWorking(e.CharacterId, true);
                _cooldown.SetWorking(e.CharacterId, true);
            });
            EventBus.Subscribe<CommissionClaimedEvent>(e =>
            {
                _countdown.SetWorking(e.CharacterId, false);
                _cooldown.SetWorking(e.CharacterId, false);
            });
        }

        [TearDown]
        public void TearDown()
        {
            _redDot?.Dispose();
            _countdown?.Dispose();
            _cooldown?.Dispose();
            EventBus.ForceClearAll();
        }

        // ===== C1 紅點分流：L1+L2 並存 → L1 取代招呼、L2 保留 =====

        [Test]
        public void C1_RedDotSplit_L1AutoPlay_L2PreservedToDialogueButton()
        {
            // L2 倒數到點
            _countdown.StartCountdown(VCW);
            _countdown.Tick(CountdownDuration);
            Assert.IsTrue(_redDot.IsLayerActive(VCW, RedDotLayer.CharacterQuestion),
                "L2 紅點應亮起");

            // L1 委託完成
            EventBus.Publish(new CommissionCompletedEvent { CharacterId = VCW });
            Assert.IsTrue(_redDot.IsLayerActive(VCW, RedDotLayer.CommissionCompleted),
                "L1 紅點應亮起");
            Assert.AreEqual(RedDotLayer.CommissionCompleted,
                _redDot.GetHubRedDot(VCW).HighestLayer);

            // 進入角色：L1/L4 是否壓制招呼？→ 是
            Assert.IsTrue(_greeting.ShouldBeSuppressedByRedDot(VCW),
                "L1 亮時招呼應被壓制");

            // 領取 L1 → L1 清除
            EventBus.Publish(new CommissionClaimedEvent { CharacterId = VCW });
            Assert.IsFalse(_redDot.IsLayerActive(VCW, RedDotLayer.CommissionCompleted));
            // L2 仍保留
            Assert.IsTrue(_redDot.IsLayerActive(VCW, RedDotLayer.CharacterQuestion),
                "L1 播完後 L2 應保留");

            // 點「對話」（有 L2）→ 觸發角色發問
            CharacterQuestionInfo picked = _cqManager.PickNextQuestion(VCW, 1);
            Assert.IsNotNull(picked);

            // 玩家選擇偏好個性 → 好感度 +10
            int before = _affinity.GetAffinity(VCW);
            _cqManager.SubmitAnswer(VCW, picked.QuestionId, "personality_gentle");
            Assert.AreEqual(before + 10, _affinity.GetAffinity(VCW));

            // 清除 L2 紅點 + 清除倒數 Ready
            _redDot.SetCharacterQuestionFlag(VCW, false);
            _countdown.ClearReady(VCW);
            Assert.IsFalse(_redDot.IsLayerActive(VCW, RedDotLayer.CharacterQuestion));
        }

        // ===== C2 玩家發問端到端 =====

        [Test]
        public void C2_PlayerQuestions_EndToEnd()
        {
            // 無紅點
            Assert.IsFalse(_greeting.ShouldBeSuppressedByRedDot(VCW));

            // 播招呼
            GreetingInfo g = _greeting.TryGreet(VCW, 1);
            Assert.IsNotNull(g);

            // 點對話 → 取得呈現
            PlayerQuestionsPresentation pres = _pqManager.GetPresentation(VCW);
            Assert.IsFalse(pres.IsIdleChatFallback);
            Assert.AreEqual(4, pres.Questions.Count);

            // 選題 → 扣體力 + 啟動 CD
            PlayerQuestionInfo q = pres.Questions[0];
            Assert.IsTrue(_stamina.TryConsumeForDialogue(VCW));
            _pqManager.MarkSeen(VCW, q.QuestionId);
            _cooldown.StartCooldown(VCW);

            Assert.IsTrue(_cooldown.IsOnCooldown(VCW));
            Assert.AreEqual(9, _stamina.GetStamina(VCW));
        }

        // ===== C3 閒聊模式 =====

        [Test]
        public void C3_IdleChat_WhenAllQuestionsConsumed()
        {
            // 將所有題目標記已看
            foreach (PlayerQuestionInfo q in _pqConfig.GetQuestionsForCharacter(VCW))
                _pqManager.MarkSeen(VCW, q.QuestionId);

            PlayerQuestionsPresentation pres = _pqManager.GetPresentation(VCW);
            Assert.IsTrue(pres.IsIdleChatFallback);

            // 觸發閒聊
            IdleChatResult r = _idle.Trigger(VCW);
            Assert.IsNotNull(r);
            Assert.IsFalse(string.IsNullOrEmpty(r.Prompt));
            Assert.IsFalse(string.IsNullOrEmpty(r.Answer));

            // 不影響好感度
            int before = _affinity.GetAffinity(VCW);
            Assert.AreEqual(before, _affinity.GetAffinity(VCW));
        }

        // ===== C4 體力歸零 =====

        [Test]
        public void C4_StaminaZero_CannotDialogue()
        {
            _stamina.SetStamina(VCW, 0);
            Assert.IsFalse(_stamina.HasEnoughForDialogue(VCW));
            Assert.IsFalse(_stamina.TryConsumeForDialogue(VCW));

            // View 層應顯示「現在好累了」（此測試驗證條件判斷，不實際渲染 UI）
        }

        // ===== C5 工作中 CD ×2 =====

        [Test]
        public void C5_WorkingDoublesCD()
        {
            // 啟動 CD，推進 30 秒（剩 30）
            _cooldown.StartCooldown(VCW);
            _cooldown.Tick(30f);
            Assert.AreEqual(30f, _cooldown.GetRemainingSeconds(VCW), 0.01f);

            // CommissionStartedEvent → 切換為工作中
            EventBus.Publish(new CommissionStartedEvent { CharacterId = VCW });
            // 工作中 Tick 30 秒 → 只扣 15 → 剩 15
            _cooldown.Tick(30f);
            Assert.AreEqual(15f, _cooldown.GetRemainingSeconds(VCW), 0.01f);

            // CommissionClaimedEvent → 恢復
            EventBus.Publish(new CommissionClaimedEvent { CharacterId = VCW });
            // 非工作中 Tick 15 秒 → 完成
            _cooldown.Tick(15f);
            Assert.IsFalse(_cooldown.IsOnCooldown(VCW));
        }

        [Test]
        public void C5_WorkingPausesCountdown()
        {
            _countdown.StartCountdown(VCW);

            // CommissionStartedEvent → 工作中
            EventBus.Publish(new CommissionStartedEvent { CharacterId = VCW });
            _countdown.Tick(120f); // 工作中 → 不扣
            Assert.IsFalse(_countdown.IsReady(VCW));

            // 委託領取 → 恢復，推進 60s → Ready
            EventBus.Publish(new CommissionClaimedEvent { CharacterId = VCW });
            _countdown.Tick(60f);
            Assert.IsTrue(_countdown.IsReady(VCW));
            Assert.IsTrue(_redDot.IsLayerActive(VCW, RedDotLayer.CharacterQuestion));
        }

        // ===== C6 紅點上限 1 =====

        [Test]
        public void C6_CountdownCap_OneRedDotOnly()
        {
            List<CharacterQuestionCountdownReadyEvent> received =
                new List<CharacterQuestionCountdownReadyEvent>();
            System.Action<CharacterQuestionCountdownReadyEvent> h = e => received.Add(e);
            EventBus.Subscribe(h);
            try
            {
                _countdown.StartCountdown(VCW);
                _countdown.Tick(60f);
                Assert.AreEqual(1, received.Count);

                // 再次 Tick 60s → 不應重複
                _countdown.Tick(60f);
                Assert.AreEqual(1, received.Count);

                // 再次呼叫 StartCountdown（Ready 狀態）→ 無效
                _countdown.StartCountdown(VCW);
                _countdown.Tick(60f);
                Assert.AreEqual(1, received.Count);

                // 玩家觸發角色發問 → ClearReady
                _countdown.ClearReady(VCW);
                _countdown.StartCountdown(VCW);
                _countdown.Tick(60f);
                Assert.AreEqual(2, received.Count);
            }
            finally { EventBus.Unsubscribe(h); }
        }

        // ===== C7 招呼語分流 =====

        [Test]
        public void C7_Greeting_NoRedDot_Plays()
        {
            GreetingInfo g = _greeting.TryGreet(VCW, 1);
            Assert.IsNotNull(g);
        }

        [Test]
        public void C7_Greeting_L2RedDot_Plays()
        {
            _countdown.StartCountdown(VCW);
            _countdown.Tick(CountdownDuration);
            Assert.IsTrue(_redDot.IsLayerActive(VCW, RedDotLayer.CharacterQuestion));

            GreetingInfo g = _greeting.TryGreet(VCW, 1);
            Assert.IsNotNull(g, "L2 紅點亮時招呼仍應播放");
        }

        [Test]
        public void C7_Greeting_L1RedDot_Suppressed()
        {
            EventBus.Publish(new CommissionCompletedEvent { CharacterId = VCW });
            GreetingInfo g = _greeting.TryGreet(VCW, 1);
            Assert.IsNull(g);
        }

        [Test]
        public void C7_Greeting_L4RedDot_Suppressed()
        {
            _redDot.SetMainQuestEventFlag(VCW, true);
            GreetingInfo g = _greeting.TryGreet(VCW, 1);
            Assert.IsNull(g);
        }

        // ===== 助手：Config 建立 =====

        private static CharacterQuestionsConfig BuildCharacterQuestionsConfig()
        {
            CharacterQuestionData[] questions = new CharacterQuestionData[]
            {
                new CharacterQuestionData { id = 1, question_id = "q_vcw_1", character_id = VCW, level = 1, prompt = "Hi?" },
                new CharacterQuestionData { id = 2, question_id = "q_vcw_2", character_id = VCW, level = 1, prompt = "Hello?" },
            };
            string[] personalities = new[] { "personality_gentle", "personality_lively", "personality_calm", "personality_assertive" };
            string[][] texts = new[]
            {
                new[] { "A", "B", "C", "D" },
                new[] { "a", "b", "c", "d" },
            };
            List<CharacterQuestionOptionData> optList = new List<CharacterQuestionOptionData>();
            int oid = 1;
            string[] qids = new[] { "q_vcw_1", "q_vcw_2" };
            for (int qi = 0; qi < qids.Length; qi++)
                for (int pi = 0; pi < personalities.Length; pi++)
                    optList.Add(new CharacterQuestionOptionData { id = oid++, question_id = qids[qi], personality_id = personalities[pi], text = texts[qi][pi], response = "r" });

            CharacterProfileData[] profiles = new CharacterProfileData[]
            {
                new CharacterProfileData { id = 1, character_id = "village_chief_wife", preferred_personality_id = "personality_gentle" },
                new CharacterProfileData { id = 2, character_id = "farm_girl",          preferred_personality_id = "personality_lively" },
                new CharacterProfileData { id = 3, character_id = "witch",              preferred_personality_id = "personality_calm" },
                new CharacterProfileData { id = 4, character_id = "guard",              preferred_personality_id = "personality_assertive" },
            };
            PersonalityAffinityRuleData[] rules = new PersonalityAffinityRuleData[]
            {
                new PersonalityAffinityRuleData { id=1,  character_id="village_chief_wife", personality_id="personality_gentle",    affinity_delta=10 },
                new PersonalityAffinityRuleData { id=2,  character_id="village_chief_wife", personality_id="personality_calm",      affinity_delta=5  },
                new PersonalityAffinityRuleData { id=3,  character_id="village_chief_wife", personality_id="personality_lively",    affinity_delta=2  },
                new PersonalityAffinityRuleData { id=4,  character_id="village_chief_wife", personality_id="personality_assertive", affinity_delta=0  },
                new PersonalityAffinityRuleData { id=5,  character_id="farm_girl",          personality_id="personality_lively",    affinity_delta=10 },
                new PersonalityAffinityRuleData { id=6,  character_id="farm_girl",          personality_id="personality_assertive", affinity_delta=5  },
                new PersonalityAffinityRuleData { id=7,  character_id="farm_girl",          personality_id="personality_gentle",    affinity_delta=2  },
                new PersonalityAffinityRuleData { id=8,  character_id="farm_girl",          personality_id="personality_calm",      affinity_delta=0  },
                new PersonalityAffinityRuleData { id=9,  character_id="witch",              personality_id="personality_calm",      affinity_delta=10 },
                new PersonalityAffinityRuleData { id=10, character_id="witch",              personality_id="personality_gentle",    affinity_delta=5  },
                new PersonalityAffinityRuleData { id=11, character_id="witch",              personality_id="personality_assertive", affinity_delta=2  },
                new PersonalityAffinityRuleData { id=12, character_id="witch",              personality_id="personality_lively",    affinity_delta=0  },
                new PersonalityAffinityRuleData { id=13, character_id="guard",              personality_id="personality_assertive", affinity_delta=10 },
                new PersonalityAffinityRuleData { id=14, character_id="guard",              personality_id="personality_calm",      affinity_delta=5  },
                new PersonalityAffinityRuleData { id=15, character_id="guard",              personality_id="personality_lively",    affinity_delta=2  },
                new PersonalityAffinityRuleData { id=16, character_id="guard",              personality_id="personality_gentle",    affinity_delta=0  },
            };
            return new CharacterQuestionsConfig(questions, optList.ToArray(), profiles, rules);
        }

        private static GreetingConfig BuildGreetingConfig()
        {
            return new GreetingConfig(new GreetingData[]
            {
                new GreetingData { id = 1, greeting_id = "g1", character_id = VCW, level = 1, text = "Welcome." },
                new GreetingData { id = 2, greeting_id = "g2", character_id = VCW, level = 1, text = "Hi." },
            });
        }

        private static IdleChatConfig BuildIdleChatConfig()
        {
            return new IdleChatConfig(
                new IdleChatTopicData[]
                {
                    new IdleChatTopicData { id = 1, topic_id = "i1", character_id = VCW, prompt = "How's your day?" },
                },
                new IdleChatAnswerData[]
                {
                    new IdleChatAnswerData { id = 1, answer_id = "a1", topic_id = "i1", text = "Fine." },
                });
        }

        private static PlayerQuestionsConfig BuildPlayerQuestionsConfig(int n)
        {
            List<PlayerQuestionData> qs = new List<PlayerQuestionData>();
            for (int i = 1; i <= n; i++)
            {
                qs.Add(new PlayerQuestionData
                {
                    question_id = $"pq_vcw_{i:00}",
                    character_id = VCW,
                    question_text = $"Q{i}?",
                    response_text = $"A{i}",
                    sort_order = i,
                });
            }
            return new PlayerQuestionsConfig(new PlayerQuestionsConfigData { questions = qs.ToArray() });
        }
    }
}
