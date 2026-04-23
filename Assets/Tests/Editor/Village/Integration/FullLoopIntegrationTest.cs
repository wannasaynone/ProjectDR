// FullLoopIntegrationTest — Sprint 4 C1 端到端 Loop 整合測試。
// 驗證 TEST 5（完整 4 循環）：
//   A. 探索循環：出發 → 採集 → 撤離 → 回到 Hub（簡化模擬）
//   B. 委託循環：CommissionManager.StartCommission → Tick → Complete → Claim
//   C. 擴建循環：StorageExpansionManager 擴建（StartExpansion → Tick → Complete → 容量增加）
//   D. 感情循環：送禮、好感度、紅點 L2 清除
//
// 使用真實 Manager（不 mock），所有循環在單一 fixture 中完整執行，
// 並驗證 MainQuest/RedDot 同步。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Gift;
using ProjectDR.Village.Affinity;
using ProjectDR.Village.Storage;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.Commission;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Progression;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.TimeProvider;

namespace ProjectDR.Tests.Village.Integration
{
    [TestFixture]
    public class FullLoopIntegrationTest
    {
        private BackpackManager _backpack;
        private StorageManager _storage;
        private AffinityManager _affinityManager;
        private GiftManager _giftManager;
        private CommissionManager _commissionManager;
        private CommissionRecipesConfig _commissionConfig;
        private StorageExpansionManager _expansionManager;
        private StorageExpansionConfig _expansionConfig;
        private MainQuestConfig _mainQuestConfig;
        private MainQuestManager _mainQuestManager;
        private RedDotManager _redDotManager;
        private ExplorationEntryManager _explorationManager;
        private FakeTimeProvider _timeProvider;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _backpack = new BackpackManager(20, 99);
            _storage = new StorageManager(100, 99);

            _affinityManager = new AffinityManager(BuildAffinityConfig());
            _giftManager = new GiftManager(_affinityManager, _backpack, _storage);

            _timeProvider = new FakeTimeProvider { CurrentTimestamp = 1000L };
            _commissionConfig = BuildCommissionConfig();
            _commissionManager = new CommissionManager(
                _commissionConfig, _backpack, _storage, _timeProvider,
                new[] { CharacterIds.Witch, CharacterIds.Guard, CharacterIds.FarmGirl });

            _expansionConfig = BuildExpansionConfig();
            _expansionManager = new StorageExpansionManager(_storage, _backpack, _expansionConfig);

            _mainQuestConfig = BuildMainQuestConfig();
            _mainQuestManager = new MainQuestManager(_mainQuestConfig);

            _redDotManager = new RedDotManager(_mainQuestConfig, _mainQuestManager);

            _explorationManager = new ExplorationEntryManager(_backpack);
        }

        [TearDown]
        public void TearDown()
        {
            _redDotManager?.Dispose();
            _explorationManager?.Dispose();
            EventBus.ForceClearAll();
        }

        // ===== A. 探索循環 =====

        [Test]
        public void ExplorationLoop_DepartAndReturn_BackpackContainsLoot()
        {
            bool departed = _explorationManager.Depart();
            Assert.IsTrue(departed);

            Dictionary<string, int> loot = new Dictionary<string, int> { { "herb_green", 2 } };
            _explorationManager.SimulateReturn(loot);

            Assert.AreEqual(2, _backpack.GetItemCount("herb_green"));
            Assert.IsTrue(_explorationManager.CanDepart(), "返回後應可再次出發");
        }

        [Test]
        public void ExplorationLoop_CannotDepartTwiceWithoutReturn()
        {
            _explorationManager.Depart();
            Assert.IsFalse(_explorationManager.Depart(), "未返回時不可重複出發");
        }

        // ===== B. 委託循環 =====

        [Test]
        public void CommissionLoop_StartTickCompleteClaim_WitchHealPotion()
        {
            _backpack.AddItem("herb_green", 1);
            StartCommissionResult start = _commissionManager.StartCommission(
                CharacterIds.Witch, "witch_heal_potion", 0);
            Assert.IsTrue(start.IsSuccess, "StartCommission 應成功: " + start.Error);
            Assert.AreEqual(0, _backpack.GetItemCount("herb_green"), "輸入物品應被扣除");

            // 推進時間至完成
            _timeProvider.CurrentTimestamp += 60L;
            _commissionManager.Tick(60f);

            CommissionSlotInfo info = _commissionManager.GetSlot(CharacterIds.Witch, 0);
            Assert.AreEqual(CommissionSlotState.Completed, info.State);

            // 領取
            ClaimCommissionResult claim = _commissionManager.ClaimCommission(CharacterIds.Witch, 0);
            Assert.IsTrue(claim.IsSuccess);
            Assert.AreEqual(1, _backpack.GetItemCount("potion_heal"));

            // slot 回 Idle
            Assert.AreEqual(CommissionSlotState.Idle,
                _commissionManager.GetSlot(CharacterIds.Witch, 0).State);
        }

        [Test]
        public void CommissionLoop_PublishesClaimedEvent_ForMainQuestSignal()
        {
            _backpack.AddItem("herb_green", 1);
            _commissionManager.StartCommission(CharacterIds.Witch, "witch_heal_potion", 0);
            _timeProvider.CurrentTimestamp += 60L;
            _commissionManager.Tick(60f);

            CommissionClaimedEvent received = null;
            Action<CommissionClaimedEvent> handler = (e) => received = e;
            EventBus.Subscribe(handler);
            try
            {
                _commissionManager.ClaimCommission(CharacterIds.Witch, 0);
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            Assert.IsNotNull(received);
            Assert.AreEqual(CharacterIds.Witch, received.CharacterId);
        }

        [Test]
        public void CommissionLoop_EmptyHandedGuardPatrol_ProducesOutput()
        {
            StartCommissionResult start = _commissionManager.StartCommission(
                CharacterIds.Guard, "guard_patrol_basic", 0);
            Assert.IsTrue(start.IsSuccess);

            _timeProvider.CurrentTimestamp += 100L;
            _commissionManager.Tick(100f);

            ClaimCommissionResult claim = _commissionManager.ClaimCommission(CharacterIds.Guard, 0);
            Assert.IsTrue(claim.IsSuccess);
            Assert.AreEqual(1, _backpack.GetItemCount("seed_tomato"));
        }

        // ===== C. 擴建循環 =====

        [Test]
        public void ExpansionLoop_StartTickComplete_CapacityIncreased()
        {
            _backpack.AddItem("material_wood", 10);
            _backpack.AddItem("material_cloth", 5);

            int capacityBefore = _storage.Capacity;

            StorageExpansionStartResult start = _expansionManager.StartExpansion();
            Assert.IsTrue(start.IsSuccess, "擴建啟動失敗：" + start.Error);
            Assert.AreEqual(0, _backpack.GetItemCount("material_wood"), "物資已扣除");
            Assert.AreEqual(0, _backpack.GetItemCount("material_cloth"));

            // 推進擴建倒數
            _expansionManager.Tick(90f);
            Assert.AreEqual(StorageExpansionState.Completed, _expansionManager.State);
            Assert.AreEqual(capacityBefore + 50, _storage.Capacity);

            _expansionManager.AcknowledgeCompletion();
            Assert.AreEqual(StorageExpansionState.Idle, _expansionManager.State);
            Assert.AreEqual(1, _expansionManager.CurrentLevel);
        }

        [Test]
        public void ExpansionLoop_InsufficientResources_Fails()
        {
            // 沒有任何物資
            StorageExpansionStartResult start = _expansionManager.StartExpansion();
            Assert.IsFalse(start.IsSuccess);
            Assert.AreEqual(StorageExpansionStartError.InsufficientResources, start.Error);
        }

        // ===== D. 感情循環（送禮） =====

        [Test]
        public void AffinityLoop_GiftSuccessfully_RaisesAffinity()
        {
            _backpack.AddItem("gift_gem", 1);
            GiftResult result = _giftManager.GiveGift(CharacterIds.FarmGirl, "gift_gem");
            Assert.IsTrue(result.IsSuccess);

            int affinity = _affinityManager.GetAffinity(CharacterIds.FarmGirl);
            Assert.Greater(affinity, 0, "送禮應增加好感度");
            Assert.AreEqual(0, _backpack.GetItemCount("gift_gem"), "禮物應被消耗");
        }

        [Test]
        public void AffinityLoop_ThresholdReached_PublishesEvent()
        {
            AffinityThresholdReachedEvent received = null;
            Action<AffinityThresholdReachedEvent> handler = (e) =>
            {
                if (e.CharacterId == CharacterIds.FarmGirl) received = e;
            };
            EventBus.Subscribe(handler);
            try
            {
                // 直接加足夠好感度觸發門檻
                _affinityManager.AddAffinity(CharacterIds.FarmGirl, 5);
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            Assert.IsNotNull(received);
            Assert.AreEqual(5, received.ThresholdValue);
        }

        // ===== 紅點 L1 + L3 整合 =====

        [Test]
        public void RedDot_L1_CommissionCompleted_ShowsRedDotOnCharacter()
        {
            _backpack.AddItem("herb_green", 1);
            _commissionManager.StartCommission(CharacterIds.Witch, "witch_heal_potion", 0);
            _timeProvider.CurrentTimestamp += 60L;
            _commissionManager.Tick(60f);

            HubRedDotInfo info = _redDotManager.GetHubRedDot(CharacterIds.Witch);
            Assert.AreEqual(RedDotLayer.CommissionCompleted, info.HighestLayer);
            Assert.IsTrue(info.ShouldShow);
        }

        [Test]
        public void RedDot_L1_Cleared_AfterClaim()
        {
            _backpack.AddItem("herb_green", 1);
            _commissionManager.StartCommission(CharacterIds.Witch, "witch_heal_potion", 0);
            _timeProvider.CurrentTimestamp += 60L;
            _commissionManager.Tick(60f);
            _commissionManager.ClaimCommission(CharacterIds.Witch, 0);

            HubRedDotInfo info = _redDotManager.GetHubRedDot(CharacterIds.Witch);
            Assert.AreEqual(RedDotLayer.None, info.HighestLayer);
        }

        [Test]
        public void RedDot_L3_NewQuest_AvailableShowsOnOwner()
        {
            // T0 為 Available（MainQuestManager 建構時會自動 Publish）
            HubRedDotInfo info = _redDotManager.GetHubRedDot(CharacterIds.VillageChiefWife);
            Assert.IsTrue(info.HighestLayer == RedDotLayer.NewQuest || info.HighestLayer == RedDotLayer.MainQuestEvent);
        }

        [Test]
        public void RedDot_L2_CharacterQuestion_ManualSet()
        {
            _redDotManager.SetCharacterQuestionFlag(CharacterIds.FarmGirl, true);
            HubRedDotInfo info = _redDotManager.GetHubRedDot(CharacterIds.FarmGirl);
            Assert.AreEqual(RedDotLayer.CharacterQuestion, info.HighestLayer);

            _redDotManager.SetCharacterQuestionFlag(CharacterIds.FarmGirl, false);
            info = _redDotManager.GetHubRedDot(CharacterIds.FarmGirl);
            Assert.AreEqual(RedDotLayer.None, info.HighestLayer);
        }

        // ===== 綜合整合：多循環並行 =====

        [Test]
        public void MultiLoop_CommissionWhileExpansion_BothProgressIndependently()
        {
            _backpack.AddItem("herb_green", 1);
            _backpack.AddItem("material_wood", 10);
            _backpack.AddItem("material_cloth", 5);

            _commissionManager.StartCommission(CharacterIds.Witch, "witch_heal_potion", 0);
            _expansionManager.StartExpansion();

            // 推進委託（45 秒）— 擴建還沒完成（需 90 秒）
            _timeProvider.CurrentTimestamp += 45L;
            _commissionManager.Tick(45f);
            _expansionManager.Tick(45f);

            Assert.AreEqual(CommissionSlotState.Completed,
                _commissionManager.GetSlot(CharacterIds.Witch, 0).State);
            Assert.AreEqual(StorageExpansionState.InProgress, _expansionManager.State);

            // 再 45 秒讓擴建完成
            _timeProvider.CurrentTimestamp += 45L;
            _expansionManager.Tick(45f);
            Assert.AreEqual(StorageExpansionState.Completed, _expansionManager.State);
        }

        // ===== Helpers =====

        private static AffinityConfig BuildAffinityConfig()
        {
            return new AffinityConfig(new AffinityCharacterData[]
            {
                new AffinityCharacterData { id = 1, character_id = "__default__", thresholds = "5" },
            });
        }

        private static CommissionRecipesConfig BuildCommissionConfig()
        {
            return new CommissionRecipesConfig(new CommissionRecipeData[]
            {
                new CommissionRecipeData
                {
                    id = 1, recipe_id = "witch_heal_potion", character_id = CharacterIds.Witch,
                    input_item_id = "herb_green", input_quantity = 1,
                    output_item_id = "potion_heal", output_quantity = 1,
                    duration_seconds = 45, workbench_slot_index_max = 2,
                },
                new CommissionRecipeData
                {
                    id = 2, recipe_id = "guard_patrol_basic", character_id = CharacterIds.Guard,
                    input_item_id = "", input_quantity = 0,
                    output_item_id = "seed_tomato", output_quantity = 1,
                    duration_seconds = 90, workbench_slot_index_max = 2,
                },
                new CommissionRecipeData
                {
                    id = 3, recipe_id = "farm_tomato", character_id = CharacterIds.FarmGirl,
                    input_item_id = "seed_tomato", input_quantity = 1,
                    output_item_id = "crop_tomato", output_quantity = 1,
                    duration_seconds = 30, workbench_slot_index_max = 2,
                },
            });
        }

        private static StorageExpansionConfig BuildExpansionConfig()
        {
            StorageExpansionStageData[] stages = new StorageExpansionStageData[]
            {
                new StorageExpansionStageData { id = 0, level = 0, capacity_before = 0, capacity_after = 100, duration_seconds = 0 },
                new StorageExpansionStageData { id = 1, level = 1, capacity_before = 100, capacity_after = 150, duration_seconds = 90, description = "stage 1" },
                new StorageExpansionStageData { id = 2, level = 2, capacity_before = 150, capacity_after = 200, duration_seconds = 120, description = "stage 2" },
            };
            StorageExpansionRequirementData[] requirements = new StorageExpansionRequirementData[]
            {
                new StorageExpansionRequirementData { id = 1, stage_level = 1, item_id = "material_wood",  quantity = 10 },
                new StorageExpansionRequirementData { id = 2, stage_level = 1, item_id = "material_cloth", quantity = 5  },
                new StorageExpansionRequirementData { id = 3, stage_level = 2, item_id = "material_wood",  quantity = 20 },
                new StorageExpansionRequirementData { id = 4, stage_level = 2, item_id = "material_cloth", quantity = 10 },
                new StorageExpansionRequirementData { id = 5, stage_level = 2, item_id = "material_stone", quantity = 5  },
            };
            return new StorageExpansionConfig(stages, requirements);
        }

        private static MainQuestConfig BuildMainQuestConfig()
        {
            // Sprint 6 B1：新 T0/T1/T2 三條結構（純陣列格式）
            return new MainQuestConfig(
                new MainQuestData[]
                {
                    new MainQuestData { id = 1, quest_id = "T0", display_name = "醒來的地方",   owner_character_id = CharacterIds.VillageChiefWife, completion_condition_type = MainQuestCompletionTypes.Auto,         completion_condition_value = MainQuestSignalValues.Node0DialogueComplete,  sort_order = 0 },
                    new MainQuestData { id = 2, quest_id = "T1", display_name = "認識所有人",   owner_character_id = CharacterIds.VillageChiefWife, completion_condition_type = MainQuestCompletionTypes.DialogueEnd,   completion_condition_value = MainQuestSignalValues.Node2DialogueComplete,  sort_order = 1 },
                    new MainQuestData { id = 3, quest_id = "T2", display_name = "出去看看外面", owner_character_id = CharacterIds.VillageChiefWife, completion_condition_type = MainQuestCompletionTypes.FirstExplore,  completion_condition_value = MainQuestSignalValues.GuardReturnEventComplete, sort_order = 2 },
                },
                new MainQuestUnlockData[]
                {
                    new MainQuestUnlockData { id = 1, main_quest_id = "T0", unlock_type = "quest",   unlock_value = "T1" },
                    new MainQuestUnlockData { id = 2, main_quest_id = "T0", unlock_type = "event",   unlock_value = "node_0_complete" },
                    new MainQuestUnlockData { id = 3, main_quest_id = "T1", unlock_type = "quest",   unlock_value = "T2" },
                    new MainQuestUnlockData { id = 4, main_quest_id = "T1", unlock_type = "event",   unlock_value = "node_2_complete" },
                    new MainQuestUnlockData { id = 5, main_quest_id = "T1", unlock_type = "feature", unlock_value = "exploration_open" },
                    new MainQuestUnlockData { id = 6, main_quest_id = "T2", unlock_type = "event",   unlock_value = "guard_unlock" },
                    new MainQuestUnlockData { id = 7, main_quest_id = "T2", unlock_type = "feature", unlock_value = "exploration_full_open" },
                });
        }

        private class FakeTimeProvider : ITimeProvider
        {
            public long CurrentTimestamp { get; set; }
            public long GetCurrentTimestampUtc() => CurrentTimestamp;
        }
    }
}
