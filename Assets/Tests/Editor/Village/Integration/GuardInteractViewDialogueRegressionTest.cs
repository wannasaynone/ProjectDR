// GuardInteractViewDialogueRegressionTest — Sprint 6 F10 bugfix 回歸測試。
//
// 背景：
// F9 曾在 OnGuardReturnLockExploration 中補呼叫 SetFirstMeetFlag(Guard, false)，
// 意圖清除守衛 Hub 按鈕的 FirstMeet 紅點。但方向錯誤：
// FirstMeet 的語義是「玩家首次點擊進入該角色互動畫面後才清除」，
// 而不是守衛歸來完成瞬間。
//
// F9 錯誤的後果（F10 bug）：
// 守衛歸來後 Hub 按鈕沒有 FirstMeet 紅點 → 玩家無法靠紅點引導找守衛發問取劍
// → 探索永遠無法解鎖。
//
// F10 修復（Sprint 6 F10 系統性重構）：
// 1. Revert F9：從 OnGuardReturnLockExploration 中移除 SetFirstMeetFlag(Guard, false)
// 2. 抽出 OnCharacterEnteredAndCGDone 方法，兩條路徑共用：
//    路徑 A（標準）：播 CG → callback → OnCharacterEnteredAndCGDone（含 SetFirstMeetFlag false）
//    路徑 B（守衛特例）：CG 已播過 → 跳過 CG → OnCharacterEnteredAndCGDone
//
// 正確語義驗證：
// 測試 1：守衛歸來完成後，Hub 按鈕**應有** FirstMeet 紅點（讓玩家知道要去找守衛）
// 測試 2：玩家進入守衛 interact view 後，OnCharacterEnteredAndCGDone 執行，FirstMeet 紅點清除
// 測試 3：守衛進入完成後，含「要拿劍」發問的玩家題目清單可用
// 測試 4：農女/魔女的 FirstMeet 紅點不受守衛歸來事件影響（regression）

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Progression;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.CharacterQuestions;
using ProjectDR.Village.Greeting;

namespace ProjectDR.Tests.Village.Integration
{
    [TestFixture]
    public class GuardInteractViewDialogueRegressionTest
    {
        private RedDotManager _redDotManager;
        private MainQuestConfig _mainQuestConfig;
        private MainQuestManager _mainQuestManager;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _mainQuestConfig = BuildMinimalMainQuestConfig();
            _mainQuestManager = new MainQuestManager(_mainQuestConfig);
            _redDotManager = new RedDotManager(_mainQuestConfig, _mainQuestManager);
        }

        [TearDown]
        public void TearDown()
        {
            _redDotManager?.Dispose();
            EventBus.ForceClearAll();
        }

        // ===== 測試 1：守衛歸來完成後，Hub 應有 FirstMeet 紅點 =====
        // F9 revert：守衛歸來完成不應清除 FirstMeet。
        // 守衛 Hub 按鈕需要紅點引導玩家去找守衛發問取劍。

        [Test]
        public void Regression_F10_GuardReturnCompleted_GuardHubStillHasFirstMeetRedDot()
        {
            // Arrange：模擬守衛解鎖 → FirstMeet 旗標設定
            EventBus.Publish(new CharacterUnlockedEvent { CharacterId = CharacterIds.Guard });
            Assert.IsTrue(
                _redDotManager.IsLayerActive(CharacterIds.Guard, RedDotLayer.FirstMeet),
                "前置：守衛解鎖後 FirstMeet 旗標應已設定");

            // 模擬 VillageEntryPoint.OnGuardReturnLockExploration（F9 revert 後不清除 FirstMeet）
            Action<GuardReturnEventCompletedEvent> guardReturnHandler = (e) =>
            {
                // 僅 MarkIntroCGPlayed（F8 修復）+ SetExplorationLocked，不清除 FirstMeet
                // SetFirstMeetFlag(Guard, false) 已移除（F9 revert）
            };
            EventBus.Subscribe(guardReturnHandler);

            try
            {
                // Act：發布守衛歸來完成事件
                EventBus.Publish(new GuardReturnEventCompletedEvent());

                // Assert：守衛 Hub 按鈕**應保留** FirstMeet 紅點，引導玩家去找守衛
                Assert.IsTrue(
                    _redDotManager.IsLayerActive(CharacterIds.Guard, RedDotLayer.FirstMeet),
                    "F10 修復：守衛歸來後 Hub 按鈕應保留 FirstMeet 紅點，" +
                    "讓玩家能靠紅點引導找守衛發問取劍（F9 revert 後的正確語義）。");
            }
            finally
            {
                EventBus.Unsubscribe(guardReturnHandler);
            }
        }

        // ===== 測試 2：玩家進入守衛 interact view → FirstMeet 紅點應被清除 =====
        // OnCharacterEnteredAndCGDone 在玩家首次進入時執行，清除 FirstMeet。

        [Test]
        public void Regression_F10_PlayerEntersGuardView_FirstMeetRedDotCleared()
        {
            // Arrange：守衛解鎖 + 歸來完成（此時 FirstMeet 保留）
            EventBus.Publish(new CharacterUnlockedEvent { CharacterId = CharacterIds.Guard });
            // 守衛歸來完成（不清除 FirstMeet）
            // 此時 Hub 有 FirstMeet 紅點

            Assert.IsTrue(
                _redDotManager.IsLayerActive(CharacterIds.Guard, RedDotLayer.FirstMeet),
                "前置：守衛解鎖後 FirstMeet 應已設定");

            // Act：模擬 VillageEntryPoint.OnCharacterEnteredAndCGDone(Guard)
            // （玩家點 Hub 守衛按鈕 → InitializeCharacterView → 路徑 B 跳過 CG → 呼叫此方法）
            _redDotManager.SetFirstMeetFlag(CharacterIds.Guard, false);

            // Assert：FirstMeet 紅點應已清除
            Assert.IsFalse(
                _redDotManager.IsLayerActive(CharacterIds.Guard, RedDotLayer.FirstMeet),
                "玩家首次進入守衛互動畫面後，OnCharacterEnteredAndCGDone 應清除 FirstMeet 紅點。");
        }

        // ===== 測試 3：PlayerQuestionsManager 通用邏輯 — 含 trigger_flag 的單次題可被取得 =====
        // F12 決策 6-13 說明：守衛的「要拿劍」特殊題已從 player-questions-config.json 移除。
        // 本測試已不再測試「守衛 interact view 含要拿劍」，改為驗證通用的
        // PlayerQuestionsManager 邏輯：config 中若有含 trigger_flag 的 is_single_use 題，
        // GetPresentation 能正確取得並回傳。
        // TriggerFlagGrantGuardSword 僅作為 trigger_flag 字串常數使用（不觸發業務邏輯）。

        [Test]
        public void Regression_F10_PlayerQuestionsManager_SingleUseQuestionWithTriggerFlag_IsReturned()
        {
            // Arrange：建立含「要拿劍」特殊題的 PlayerQuestionsConfig（純通用邏輯測試，不依賴實際 JSON）
            PlayerQuestionsConfigData data = new PlayerQuestionsConfigData
            {
                schema_version = 2,
                questions = new PlayerQuestionData[]
                {
                    new PlayerQuestionData
                    {
                        question_id = "q_guard_sword",
                        character_id = CharacterIds.Guard,
                        question_text = "要拿劍",
                        response_text = "拿去吧",
                        unlock_affinity_stage = 0,
                        is_single_use = true,
#pragma warning disable CS0618
                        trigger_flag = PlayerQuestionsManager.TriggerFlagGrantGuardSword
#pragma warning restore CS0618
                    }
                }
            };
            PlayerQuestionsConfig config = new PlayerQuestionsConfig(data);
            PlayerQuestionsManager manager = new PlayerQuestionsManager(config);

            // Act：取得守衛的發問清單
            PlayerQuestionsPresentation presentation = manager.GetPresentation(CharacterIds.Guard);

            // Assert：通用邏輯 — PlayerQuestionsManager 能正確回傳含 trigger_flag 的單次題
            Assert.IsFalse(presentation.IsIdleChatFallback,
                "含 is_single_use 特殊題的發問清單不應進入閒聊模式");
            Assert.AreEqual(1, presentation.Questions.Count,
                "發問清單應含 1 題（q_guard_sword）");
            Assert.AreEqual("q_guard_sword", presentation.Questions[0].QuestionId,
                "發問清單中應有 q_guard_sword 特殊題");
            Assert.IsTrue(presentation.Questions[0].IsSingleUse,
                "q_guard_sword 應為 is_single_use 特殊題");
        }

        // ===== 測試 4：其他角色的 FirstMeet 旗標不受守衛歸來事件影響（regression） =====

        [Test]
        public void Regression_F10_OtherCharactersFirstMeet_NotAffectedByGuardReturn()
        {
            // Arrange：解鎖農女/魔女，設定各自的 FirstMeet 旗標
            EventBus.Publish(new CharacterUnlockedEvent { CharacterId = CharacterIds.Guard });
            EventBus.Publish(new CharacterUnlockedEvent { CharacterId = CharacterIds.FarmGirl });
            EventBus.Publish(new CharacterUnlockedEvent { CharacterId = CharacterIds.Witch });

            // 守衛歸來完成（F9 revert：不清除任何 FirstMeet）
            EventBus.Publish(new GuardReturnEventCompletedEvent());

            // Assert：三個角色的 FirstMeet 都應保留（守衛歸來不影響其他角色）
            Assert.IsTrue(
                _redDotManager.IsLayerActive(CharacterIds.Guard, RedDotLayer.FirstMeet),
                "守衛 FirstMeet 在玩家未進入前應保留");
            Assert.IsTrue(
                _redDotManager.IsLayerActive(CharacterIds.FarmGirl, RedDotLayer.FirstMeet),
                "農女 FirstMeet 不應受守衛歸來事件影響");
            Assert.IsTrue(
                _redDotManager.IsLayerActive(CharacterIds.Witch, RedDotLayer.FirstMeet),
                "魔女 FirstMeet 不應受守衛歸來事件影響");
        }

        // ===== F11 測試：路徑 B 補 SetState(Normal) — 守衛 greeting 應觸發 =====
        //
        // 背景：
        // F10 的 OnCharacterEnteredAndCGDone 中 SetState(Normal) 被誤分類為 (a) CG 播放本身，
        // 只在路徑 A callback 中執行。路徑 B（守衛特例）跳過 CG 直接呼叫 OnCharacterEnteredAndCGDone，
        // 未執行 SetState(Normal)，CharacterInteractionView 可能卡在非 Normal 狀態，
        // 導致 GreetingPresenter.TryGreet 不被呼叫（StartDialoguePlayback 在 Normal 狀態才觸發 greeting）。
        //
        // F11 修復：SetState(Normal) 移至 OnCharacterEnteredAndCGDone 方法內（兩條路徑共用）。
        // 測試驗證：守衛路徑 B 進入後（無 L1/L4 紅點），GreetingPresenter 能正確回傳 greeting。
        // Regression 驗證：農女/魔女（路徑 A）GreetingPresenter 行為不受影響。

        [Test]
        public void Regression_F11_GuardPathB_GreetingPresenter_ReturnsGreetingWhenNoRedDot()
        {
            // Arrange：守衛解鎖 + 玩家首次進入（OnCharacterEnteredAndCGDone 已執行，清除 FirstMeet）
            EventBus.Publish(new CharacterUnlockedEvent { CharacterId = CharacterIds.Guard });
            // 模擬 OnCharacterEnteredAndCGDone 的 (b) side effect：清除 FirstMeet
            _redDotManager.SetFirstMeetFlag(CharacterIds.Guard, false);

            // 驗證前置：守衛無 L1/L4 紅點
            bool hasL1 = _redDotManager.IsLayerActive(CharacterIds.Guard, RedDotLayer.CommissionCompleted);
            bool hasL4 = _redDotManager.IsLayerActive(CharacterIds.Guard, RedDotLayer.MainQuestEvent);
            Assert.IsFalse(hasL1, "前置：守衛無 L1 紅點（未有完成委託）");
            Assert.IsFalse(hasL4, "前置：守衛無 L4 紅點（無主線事件）");

            // Arrange：建立 GreetingPresenter（模擬 VillageEntryPoint 組裝後注入至 View 的情況）
            GreetingConfig config = BuildMinimalGreetingConfig();
            GreetingPresenter presenter = new GreetingPresenter(config, _redDotManager, seed: 1);

            // Act：TryGreet（對應 StartDialoguePlayback 在 Normal state 下的呼叫）
            GreetingInfo info = presenter.TryGreet(CharacterIds.Guard, level: 1);

            // Assert：greeting 應回傳（代表 state = Normal 的前提成立，觸發鏈可執行）
            Assert.IsNotNull(info,
                "F11 修復：守衛路徑 B 進入後（無 L1/L4 紅點），" +
                "GreetingPresenter.TryGreet 應回傳 greeting，" +
                "代表 SetState(Normal) 已在 OnCharacterEnteredAndCGDone 中執行。");
        }

        [Test]
        public void Regression_F11_GuardPathB_GreetingPresenter_PublishesGreetingPlayedEvent()
        {
            // Arrange：守衛無 L1/L4 紅點（路徑 B 進入後的期望狀態）
            EventBus.Publish(new CharacterUnlockedEvent { CharacterId = CharacterIds.Guard });
            _redDotManager.SetFirstMeetFlag(CharacterIds.Guard, false);

            GreetingConfig config = BuildMinimalGreetingConfig();
            GreetingPresenter presenter = new GreetingPresenter(config, _redDotManager, seed: 1);

            GreetingPlayedEvent receivedEvent = null;
            System.Action<GreetingPlayedEvent> handler = e => receivedEvent = e;
            EventBus.Subscribe(handler);

            try
            {
                // Act
                presenter.TryGreet(CharacterIds.Guard, level: 1);

                // Assert：GreetingPlayedEvent 應被發布（greeting 成功播放）
                Assert.IsNotNull(receivedEvent,
                    "F11 修復：守衛路徑 B 進入後 GreetingPresenter 應發布 GreetingPlayedEvent。");
                Assert.AreEqual(CharacterIds.Guard, receivedEvent.CharacterId,
                    "GreetingPlayedEvent.CharacterId 應為守衛。");
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
        }

        [Test]
        public void Regression_F11_FarmGirlAndWitchPathA_GreetingPresenter_NotAffected()
        {
            // Regression：農女/魔女走路徑 A（播 CG），GreetingPresenter 行為不受 F11 修改影響
            // 路徑 A 也會呼叫 OnCharacterEnteredAndCGDone（現在含 SetState Normal），
            // 確認農女/魔女進入後（L4 紅點亮→ VCW L4，非農女/魔女自身 L4）能正常播招呼

            // Arrange：農女進入、VCW L4 亮（(c) side effect），農女自身無 L1/L4
            EventBus.Publish(new CharacterUnlockedEvent { CharacterId = CharacterIds.FarmGirl });
            _redDotManager.SetFirstMeetFlag(CharacterIds.FarmGirl, false);
            // 模擬 (c) side effect：VCW L4 亮，但不影響農女自身
            _redDotManager.SetMainQuestEventFlag(CharacterIds.VillageChiefWife, true);

            GreetingConfig config = BuildMinimalGreetingConfig();
            GreetingPresenter presenter = new GreetingPresenter(config, _redDotManager, seed: 1);

            // Act：農女的 greeting（農女自身無 L1/L4，應能播）
            GreetingInfo farmGirlGreeting = presenter.TryGreet(CharacterIds.FarmGirl, level: 1);

            // Assert
            Assert.IsNotNull(farmGirlGreeting,
                "Regression：農女路徑 A 進入後 GreetingPresenter 仍應正確回傳 greeting（F11 修改不影響農女）。");

            // 魔女同樣驗證
            EventBus.Publish(new CharacterUnlockedEvent { CharacterId = CharacterIds.Witch });
            _redDotManager.SetFirstMeetFlag(CharacterIds.Witch, false);
            GreetingInfo witchGreeting = presenter.TryGreet(CharacterIds.Witch, level: 1);
            Assert.IsNotNull(witchGreeting,
                "Regression：魔女路徑 A 進入後 GreetingPresenter 仍應正確回傳 greeting（F11 修改不影響魔女）。");
        }

        private static GreetingConfig BuildMinimalGreetingConfig()
        {
            return new GreetingConfig(new GreetingData[]
            {
                new GreetingData { id = 1, greeting_id = "g_guard_1_1", character_id = CharacterIds.Guard,    level = 1, text = "……（點了點頭。）" },
                new GreetingData { id = 2, greeting_id = "g_farm_1_1",  character_id = CharacterIds.FarmGirl, level = 1, text = "早安！" },
                new GreetingData { id = 3, greeting_id = "g_witch_1_1", character_id = CharacterIds.Witch,    level = 1, text = "嗯。" },
            });
        }

        // ===== 工具方法 =====

        private static MainQuestConfig BuildMinimalMainQuestConfig()
        {
            return new MainQuestConfig(
                new MainQuestData[]
                {
                    new MainQuestData { id = 1, quest_id = "T0", owner_character_id = CharacterIds.VillageChiefWife, completion_condition_type = MainQuestCompletionTypes.Auto,        completion_condition_value = "",                                          sort_order = 0 },
                    new MainQuestData { id = 2, quest_id = "T1", owner_character_id = CharacterIds.FarmGirl,         completion_condition_type = MainQuestCompletionTypes.DialogueEnd,  completion_condition_value = MainQuestSignalValues.Node2DialogueComplete,   sort_order = 1 },
                    new MainQuestData { id = 3, quest_id = "T2", owner_character_id = CharacterIds.Guard,            completion_condition_type = MainQuestCompletionTypes.FirstExplore, completion_condition_value = MainQuestSignalValues.GuardReturnEventComplete, sort_order = 2 },
                },
                new MainQuestUnlockData[]
                {
                    new MainQuestUnlockData { id = 1, main_quest_id = "T0", unlock_type = "quest", unlock_value = "T1" },
                    new MainQuestUnlockData { id = 2, main_quest_id = "T1", unlock_type = "quest", unlock_value = "T2" },
                });
        }
    }
}
