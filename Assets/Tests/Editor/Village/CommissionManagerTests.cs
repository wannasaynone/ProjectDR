using System.Collections.Generic;
using System.Linq;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Storage;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.Commission;
using ProjectDR.Village.MainQuest;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.TimeProvider;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// CommissionManager 單元測試。
    /// 覆蓋：建構、slot 管理、StartCommission 成敗支線、ClaimCommission 成敗支線、
    /// Tick 倒數行為、事件發布、空手委託、allowedCharacterIds 過濾、背包/倉庫入庫策略。
    /// </summary>
    [TestFixture]
    public class CommissionManagerTests
    {
        // ===== Fake time provider =====

        private class FakeTimeProvider : ITimeProvider
        {
            public long CurrentTimestamp { get; set; }
            public long GetCurrentTimestampUtc() => CurrentTimestamp;
        }

        // ===== 常數 =====

        private const string WitchId = "Witch";
        private const string GuardId = "Guard";
        private const string FarmGirlId = "FarmGirl";

        private const string HerbId = "herb_green";
        private const string PotionId = "potion_heal";
        private const string OreId = "ore_iron";
        private const string GoldcraftId = "gift_goldcraft";

        private const string WitchHealRecipe = "witch_heal";
        private const string WitchGoldcraftRecipe = "witch_goldcraft";
        private const string GuardPatrolRecipe = "guard_patrol";
        private const string FarmTomatoRecipe = "farm_tomato";

        // ===== 共用設定 =====

        private FakeTimeProvider _time;
        private BackpackManager _backpack;
        private StorageManager _storage;
        private CommissionRecipesConfig _config;

        // 事件收集器
        private List<CommissionStartedEvent> _startedEvents;
        private List<CommissionCompletedEvent> _completedEvents;
        private List<CommissionClaimedEvent> _claimedEvents;
        private List<CommissionTickEvent> _tickEvents;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();

            _time = new FakeTimeProvider { CurrentTimestamp = 1000L };
            _backpack = new BackpackManager(20, 99);
            _storage = new StorageManager(100, 99);
            _config = BuildConfig();

            _startedEvents = new List<CommissionStartedEvent>();
            _completedEvents = new List<CommissionCompletedEvent>();
            _claimedEvents = new List<CommissionClaimedEvent>();
            _tickEvents = new List<CommissionTickEvent>();

            EventBus.Subscribe<CommissionStartedEvent>(e => _startedEvents.Add(e));
            EventBus.Subscribe<CommissionCompletedEvent>(e => _completedEvents.Add(e));
            EventBus.Subscribe<CommissionClaimedEvent>(e => _claimedEvents.Add(e));
            EventBus.Subscribe<CommissionTickEvent>(e => _tickEvents.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== 建構 =====

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new CommissionManager(null, _backpack, _storage, _time));
        }

        [Test]
        public void Constructor_NullBackpack_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new CommissionManager(_config, null, _storage, _time));
        }

        [Test]
        public void Constructor_NullStorage_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new CommissionManager(_config, _backpack, null, _time));
        }

        [Test]
        public void Constructor_NullTime_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new CommissionManager(_config, _backpack, _storage, null));
        }

        [Test]
        public void Constructor_NoFilter_ManagesAllConfiguredCharacters()
        {
            CommissionManager sut = new CommissionManager(_config, _backpack, _storage, _time);
            IReadOnlyCollection<string> managed = sut.GetManagedCharacterIds();
            Assert.IsTrue(managed.Contains(WitchId));
            Assert.IsTrue(managed.Contains(GuardId));
            Assert.IsTrue(managed.Contains(FarmGirlId));
        }

        [Test]
        public void Constructor_WithFilter_OnlyManagesAllowedCharacters()
        {
            CommissionManager sut = new CommissionManager(
                _config, _backpack, _storage, _time,
                new[] { WitchId, GuardId });

            Assert.AreEqual(2, sut.GetManagedCharacterIds().Count);
            Assert.AreEqual(2, sut.GetSlotCount(WitchId));
            Assert.AreEqual(2, sut.GetSlotCount(GuardId));
            Assert.AreEqual(0, sut.GetSlotCount(FarmGirlId));  // 被過濾掉
        }

        // ===== Slot 查詢 =====

        [Test]
        public void GetSlot_Idle_ReturnsIdleInfo()
        {
            CommissionManager sut = BuildManager();
            CommissionSlotInfo info = sut.GetSlot(WitchId, 0);
            Assert.AreEqual(CommissionSlotState.Idle, info.State);
            Assert.AreEqual(WitchId, info.CharacterId);
            Assert.IsNull(info.RecipeId);
        }

        [Test]
        public void GetSlots_Witch_ReturnsCorrectCount()
        {
            CommissionManager sut = BuildManager();
            IReadOnlyList<CommissionSlotInfo> slots = sut.GetSlots(WitchId);
            Assert.AreEqual(2, slots.Count);
            Assert.AreEqual(CommissionSlotState.Idle, slots[0].State);
            Assert.AreEqual(CommissionSlotState.Idle, slots[1].State);
        }

        [Test]
        public void GetSlotCount_Unknown_ReturnsZero()
        {
            CommissionManager sut = BuildManager();
            Assert.AreEqual(0, sut.GetSlotCount("Nobody"));
            Assert.AreEqual(0, sut.GetSlotCount(""));
            Assert.AreEqual(0, sut.GetSlotCount(null));
        }

        // ===== StartCommission 失敗支線 =====

        [Test]
        public void Start_UnknownCharacter_Fails()
        {
            CommissionManager sut = BuildManager();
            StartCommissionResult r = sut.StartCommission("Nobody", WitchHealRecipe, 0);
            Assert.IsFalse(r.IsSuccess);
            Assert.AreEqual(StartCommissionError.UnknownCharacter, r.Error);
            Assert.AreEqual(0, _startedEvents.Count);
        }

        [Test]
        public void Start_InvalidSlotIndex_Fails()
        {
            CommissionManager sut = BuildManager();
            StartCommissionResult r1 = sut.StartCommission(WitchId, WitchHealRecipe, -1);
            StartCommissionResult r2 = sut.StartCommission(WitchId, WitchHealRecipe, 99);
            Assert.AreEqual(StartCommissionError.InvalidSlotIndex, r1.Error);
            Assert.AreEqual(StartCommissionError.InvalidSlotIndex, r2.Error);
        }

        [Test]
        public void Start_UnknownRecipe_Fails()
        {
            CommissionManager sut = BuildManager();
            StartCommissionResult r = sut.StartCommission(WitchId, "bogus_recipe", 0);
            Assert.AreEqual(StartCommissionError.UnknownRecipe, r.Error);
        }

        [Test]
        public void Start_RecipeCharacterMismatch_Fails()
        {
            // witch_heal 屬於 Witch，傳給 Guard 應失敗
            CommissionManager sut = BuildManager();
            StartCommissionResult r = sut.StartCommission(GuardId, WitchHealRecipe, 0);
            Assert.AreEqual(StartCommissionError.RecipeCharacterMismatch, r.Error);
        }

        [Test]
        public void Start_InsufficientInput_NoBackpackNoStorage_Fails()
        {
            CommissionManager sut = BuildManager();
            // 背包和倉庫都沒有 herb_green
            StartCommissionResult r = sut.StartCommission(WitchId, WitchHealRecipe, 0);
            Assert.AreEqual(StartCommissionError.InsufficientInput, r.Error);
            Assert.AreEqual(0, _startedEvents.Count);
        }

        [Test]
        public void Start_SlotNotIdle_Fails()
        {
            CommissionManager sut = BuildManager();
            _backpack.AddItem(HerbId, 2);
            Assert.IsTrue(sut.StartCommission(WitchId, WitchHealRecipe, 0).IsSuccess);

            StartCommissionResult r = sut.StartCommission(WitchId, WitchHealRecipe, 0);
            Assert.AreEqual(StartCommissionError.SlotNotIdle, r.Error);
        }

        // ===== StartCommission 成功 =====

        [Test]
        public void Start_BackpackOnly_DeductsFromBackpack()
        {
            CommissionManager sut = BuildManager();
            _backpack.AddItem(HerbId, 5);

            StartCommissionResult r = sut.StartCommission(WitchId, WitchHealRecipe, 0);

            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(4, _backpack.GetItemCount(HerbId));
            Assert.AreEqual(0, _storage.GetItemCount(HerbId));
            Assert.AreEqual(1, _startedEvents.Count);
            Assert.AreEqual(WitchId, _startedEvents[0].CharacterId);
            Assert.AreEqual(0, _startedEvents[0].SlotIndex);
            Assert.AreEqual(WitchHealRecipe, _startedEvents[0].RecipeId);
        }

        [Test]
        public void Start_SplitsBetweenBackpackAndStorage_PrefersBackpack()
        {
            // 配方需要 3 個 ore_iron；背包 1 個，倉庫 5 個
            CommissionManager sut = BuildManager();
            _backpack.AddItem(OreId, 1);
            _storage.AddItem(OreId, 5);

            StartCommissionResult r = sut.StartCommission(WitchId, WitchGoldcraftRecipe, 0);

            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(0, _backpack.GetItemCount(OreId), "背包應優先扣光");
            Assert.AreEqual(3, _storage.GetItemCount(OreId), "倉庫補扣 2 個");
        }

        [Test]
        public void Start_EmptyHanded_NoInputDeduction()
        {
            CommissionManager sut = BuildManager();
            // 守衛空手委託
            StartCommissionResult r = sut.StartCommission(GuardId, GuardPatrolRecipe, 0);

            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(1, _startedEvents.Count);
        }

        [Test]
        public void Start_SetsSlotStateInProgress()
        {
            CommissionManager sut = BuildManager();
            _backpack.AddItem(HerbId, 1);
            sut.StartCommission(WitchId, WitchHealRecipe, 0);

            CommissionSlotInfo slot = sut.GetSlot(WitchId, 0);
            Assert.AreEqual(CommissionSlotState.InProgress, slot.State);
            Assert.AreEqual(WitchHealRecipe, slot.RecipeId);
            Assert.AreEqual(PotionId, slot.OutputItemId);
        }

        // ===== Tick / Completed =====

        [Test]
        public void Tick_BeforeCompletion_NoCompletedEvent()
        {
            CommissionManager sut = BuildManager();
            _backpack.AddItem(HerbId, 1);
            sut.StartCommission(WitchId, WitchHealRecipe, 0);

            // witch_heal 45 秒，推進 10 秒
            _time.CurrentTimestamp += 10;
            sut.Tick(10f);

            Assert.AreEqual(0, _completedEvents.Count);
            CommissionSlotInfo slot = sut.GetSlot(WitchId, 0);
            Assert.AreEqual(CommissionSlotState.InProgress, slot.State);
            Assert.AreEqual(35, slot.RemainingSeconds);
        }

        [Test]
        public void Tick_AtCompletion_PublishesCompletedEventOnce()
        {
            CommissionManager sut = BuildManager();
            _backpack.AddItem(HerbId, 1);
            sut.StartCommission(WitchId, WitchHealRecipe, 0);

            // 推進到剛好完成
            _time.CurrentTimestamp += 45;
            sut.Tick(45f);

            Assert.AreEqual(1, _completedEvents.Count);
            Assert.AreEqual(WitchId, _completedEvents[0].CharacterId);
            Assert.AreEqual(PotionId, _completedEvents[0].OutputItemId);
            Assert.AreEqual(1, _completedEvents[0].OutputQuantity);

            // 再 Tick 一次不應重複發布
            sut.Tick(1f);
            Assert.AreEqual(1, _completedEvents.Count);
        }

        [Test]
        public void Tick_PublishesTickEventPerSecondChange()
        {
            CommissionManager sut = BuildManager();
            _backpack.AddItem(HerbId, 1);
            sut.StartCommission(WitchId, WitchHealRecipe, 0);

            int initialTickCount = _tickEvents.Count;

            // 推進 2 秒
            _time.CurrentTimestamp += 1;
            sut.Tick(1f);
            _time.CurrentTimestamp += 1;
            sut.Tick(1f);

            // 至少新增 2 次 Tick 事件（剩餘從 45 變 44 變 43）
            Assert.GreaterOrEqual(_tickEvents.Count - initialTickCount, 2);
        }

        [Test]
        public void Tick_MultipleSlots_EachTracksIndependently()
        {
            CommissionManager sut = BuildManager();
            _backpack.AddItem(HerbId, 2);

            // Slot 0：45 秒委託
            sut.StartCommission(WitchId, WitchHealRecipe, 0);
            // Slot 1：同樣 45 秒委託，但晚 10 秒開始
            _time.CurrentTimestamp += 10;
            sut.StartCommission(WitchId, WitchHealRecipe, 1);

            // 再推進 35 秒，Slot 0 正好完成（35+10=45），Slot 1 還剩 10 秒
            _time.CurrentTimestamp += 35;
            sut.Tick(35f);

            Assert.AreEqual(CommissionSlotState.Completed, sut.GetSlot(WitchId, 0).State);
            Assert.AreEqual(CommissionSlotState.InProgress, sut.GetSlot(WitchId, 1).State);
            Assert.AreEqual(1, _completedEvents.Count);
            Assert.AreEqual(0, _completedEvents[0].SlotIndex);
        }

        // ===== Claim =====

        [Test]
        public void Claim_Idle_Fails()
        {
            CommissionManager sut = BuildManager();
            ClaimCommissionResult r = sut.ClaimCommission(WitchId, 0);
            Assert.AreEqual(ClaimCommissionError.SlotEmpty, r.Error);
        }

        [Test]
        public void Claim_InProgressNotReady_Fails()
        {
            CommissionManager sut = BuildManager();
            _backpack.AddItem(HerbId, 1);
            sut.StartCommission(WitchId, WitchHealRecipe, 0);
            // 只過 10 秒
            _time.CurrentTimestamp += 10;

            ClaimCommissionResult r = sut.ClaimCommission(WitchId, 0);
            Assert.AreEqual(ClaimCommissionError.NotReady, r.Error);
        }

        [Test]
        public void Claim_Completed_IntoBackpack_Succeeds()
        {
            CommissionManager sut = BuildManager();
            _backpack.AddItem(HerbId, 1);
            sut.StartCommission(WitchId, WitchHealRecipe, 0);
            _time.CurrentTimestamp += 45;
            sut.Tick(45f);

            ClaimCommissionResult r = sut.ClaimCommission(WitchId, 0);

            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(PotionId, r.ClaimedItemId);
            Assert.AreEqual(1, r.ClaimedQuantity);
            Assert.AreEqual(1, _backpack.GetItemCount(PotionId));
            Assert.AreEqual(0, _storage.GetItemCount(PotionId));
            Assert.AreEqual(CommissionSlotState.Idle, sut.GetSlot(WitchId, 0).State);
            Assert.AreEqual(1, _claimedEvents.Count);
            Assert.AreEqual(WitchId, _claimedEvents[0].CharacterId);
            Assert.AreEqual(PotionId, _claimedEvents[0].OutputItemId);
        }

        [Test]
        public void Claim_WithoutExplicitTick_TransitionsAndClaims()
        {
            // 驗證 Claim 即使沒先 Tick，只要時間已到也能完成
            CommissionManager sut = BuildManager();
            _backpack.AddItem(HerbId, 1);
            sut.StartCommission(WitchId, WitchHealRecipe, 0);
            _time.CurrentTimestamp += 45;
            // 不呼叫 Tick

            ClaimCommissionResult r = sut.ClaimCommission(WitchId, 0);

            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(1, _completedEvents.Count, "Claim 過程應補發 Completed 事件");
            Assert.AreEqual(1, _claimedEvents.Count);
        }

        [Test]
        public void Claim_BackpackFull_OverflowsToStorage()
        {
            CommissionManager sut = BuildManager();
            // 背包填滿：20 格 × 99 堆疊 = 1980；用 potion 填滿讓 potion_heal 無法再加入
            // 更簡單：直接把所有格子都用不同物品塞滿
            int backpackSlots = 20;
            int stack = 99;
            for (int i = 0; i < backpackSlots; i++)
            {
                _backpack.AddItem("filler_" + i, stack);
            }
            // 確認背包已滿（對 potion_heal 無空間）
            Assert.AreEqual(0, _backpack.AddItem(PotionId, 1), "背包應完全滿");
            // 移回一個 backpack 以便後續驗證
            _backpack.RemoveFromSlot(0, stack);
            // 再填滿
            _backpack.AddItem("filler_0", stack);
            Assert.AreEqual(0, _backpack.AddItem(PotionId, 1));

            _storage.AddItem(HerbId, 1);
            sut.StartCommission(WitchId, WitchHealRecipe, 0);
            _time.CurrentTimestamp += 45;
            sut.Tick(45f);

            ClaimCommissionResult r = sut.ClaimCommission(WitchId, 0);

            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(0, _backpack.GetItemCount(PotionId));
            Assert.AreEqual(1, _storage.GetItemCount(PotionId), "應溢出到倉庫");
        }

        [Test]
        public void Claim_BothFull_Fails_KeepsSlotCompleted()
        {
            // 背包+倉庫都滿，Claim 應回傳 NoSpaceForOutput 且 slot 保留 Completed 狀態
            CommissionManager sut = BuildManager();

            // 填滿背包
            for (int i = 0; i < 20; i++) _backpack.AddItem("bp_fill_" + i, 99);
            // 填滿倉庫
            for (int i = 0; i < 100; i++) _storage.AddItem("st_fill_" + i, 99);

            // 放入素材（先放入倉庫）再扣除 → 扣除 1 個就空了一格；為了讓兩個都滿，我們採取另一條路：
            // 用倉庫裡某個 filler 的一個空位讓 herb 加進去，再扣除，讓扣除後再度全滿。
            // 更簡單的策略：直接把 slot 手動塞入 Completed 並檢查 Claim 失敗。
            // 這裡改以：存 herb 1 個於倉庫（接受容量被動擴充一格），開始委託時扣掉，最後再 fill 兩邊。

            // 重設策略：另起新的 storage / backpack 以測試兩者都滿情境
            StorageManager storage2 = new StorageManager(2, 99);
            BackpackManager backpack2 = new BackpackManager(2, 99);
            // 在空空的 storage2 放 1 個 herb
            storage2.AddItem(HerbId, 1);

            CommissionManager sut2 = new CommissionManager(_config, backpack2, storage2, _time);
            sut2.StartCommission(WitchId, WitchHealRecipe, 0);
            // 開始後 storage2 herb 被扣光，storage2 全空。現在把 backpack2 塞滿 + storage2 塞滿
            backpack2.AddItem("bp_fillA", 99);
            backpack2.AddItem("bp_fillB", 99);
            storage2.AddItem("st_fillA", 99);
            storage2.AddItem("st_fillB", 99);

            _time.CurrentTimestamp += 45;
            sut2.Tick(45f);

            ClaimCommissionResult r = sut2.ClaimCommission(WitchId, 0);

            Assert.IsFalse(r.IsSuccess);
            Assert.AreEqual(ClaimCommissionError.NoSpaceForOutput, r.Error);
            // slot 狀態應維持 Completed，允許玩家清出空間後再 Claim
            Assert.AreEqual(CommissionSlotState.Completed, sut2.GetSlot(WitchId, 0).State);
            // 背包/倉庫應未新增 potion
            Assert.AreEqual(0, backpack2.GetItemCount(PotionId));
            Assert.AreEqual(0, storage2.GetItemCount(PotionId));
        }

        [Test]
        public void Claim_UnknownCharacter_Fails()
        {
            CommissionManager sut = BuildManager();
            ClaimCommissionResult r = sut.ClaimCommission("Nobody", 0);
            Assert.AreEqual(ClaimCommissionError.UnknownCharacter, r.Error);
        }

        [Test]
        public void Claim_InvalidSlotIndex_Fails()
        {
            CommissionManager sut = BuildManager();
            ClaimCommissionResult r = sut.ClaimCommission(WitchId, 99);
            Assert.AreEqual(ClaimCommissionError.InvalidSlotIndex, r.Error);
        }

        // ===== 完整流程 =====

        [Test]
        public void FullFlow_Start_Tick_Complete_Claim()
        {
            CommissionManager sut = BuildManager();
            _backpack.AddItem(HerbId, 1);

            // 1) Start
            Assert.IsTrue(sut.StartCommission(WitchId, WitchHealRecipe, 0).IsSuccess);
            Assert.AreEqual(1, _startedEvents.Count);

            // 2) Tick 直到完成
            _time.CurrentTimestamp += 45;
            sut.Tick(45f);
            Assert.AreEqual(1, _completedEvents.Count);

            // 3) Claim
            Assert.IsTrue(sut.ClaimCommission(WitchId, 0).IsSuccess);
            Assert.AreEqual(1, _claimedEvents.Count);

            // 最終狀態
            Assert.AreEqual(CommissionSlotState.Idle, sut.GetSlot(WitchId, 0).State);
            Assert.AreEqual(1, _backpack.GetItemCount(PotionId));
        }

        [Test]
        public void ClaimedEvent_TriggersMainQuestCommissionCountSignal()
        {
            // 驗證 CommissionClaimedEvent 發布後，外部 subscriber 可呼叫
            // MainQuestManager.NotifyCompletionSignal 以觸發 commission_count 任務完成。
            // 此測試模擬 VillageEntryPoint.OnCommissionClaimedForMainQuest 的行為。

            // 設定一個 commission_count 類型的任務（signalValue=WitchId）
            MainQuestConfig questConfig = new MainQuestConfig(new MainQuestConfigData
            {
                main_quests = new[]
                {
                    new MainQuestConfigEntry
                    {
                        id = 1,
                        quest_id = "TC",
                        completion_condition_type = MainQuestCompletionTypes.CommissionCount,
                        completion_condition_value = WitchId,
                        sort_order = 0,
                    }
                }
            });
            MainQuestManager mqm = new MainQuestManager(questConfig);
            mqm.StartQuest("TC");
            Assert.AreEqual(MainQuestState.InProgress, mqm.GetState("TC"));

            // 模擬訂閱：CommissionClaimedEvent → NotifyCompletionSignal
            System.Action<CommissionClaimedEvent> handler = ev =>
                mqm.NotifyCompletionSignal(MainQuestCompletionTypes.CommissionCount, ev.CharacterId);
            EventBus.Subscribe(handler);

            // 執行委託全流程
            CommissionManager sut = BuildManager();
            _backpack.AddItem(HerbId, 1);
            sut.StartCommission(WitchId, WitchHealRecipe, 0);
            _time.CurrentTimestamp += 45;
            sut.Tick(45f);
            sut.ClaimCommission(WitchId, 0);

            Assert.IsTrue(mqm.IsQuestCompleted("TC"));
            EventBus.Unsubscribe(handler);
        }

        [Test]
        public void FullFlow_GuardEmptyHanded_ProducesBasicResource()
        {
            CommissionManager sut = BuildManager();

            Assert.IsTrue(sut.StartCommission(GuardId, GuardPatrolRecipe, 0).IsSuccess);
            _time.CurrentTimestamp += 90;
            sut.Tick(90f);
            Assert.IsTrue(sut.ClaimCommission(GuardId, 0).IsSuccess);

            // guard_patrol 配方產出 seed_tomato × 1
            Assert.AreEqual(1, _backpack.GetItemCount("seed_tomato"));
        }

        // ===== 輔助 =====

        private CommissionManager BuildManager()
        {
            return new CommissionManager(_config, _backpack, _storage, _time);
        }

        private static CommissionRecipesConfig BuildConfig()
        {
            CommissionRecipesConfigData data = new CommissionRecipesConfigData
            {
                recipes = new[]
                {
                    new CommissionRecipeEntry
                    {
                        recipe_id = WitchHealRecipe,
                        character_id = WitchId,
                        input_item_id = HerbId,
                        input_quantity = 1,
                        output_item_id = PotionId,
                        output_quantity = 1,
                        duration_seconds = 45f,
                        workbench_slot_index_max = 2,
                    },
                    new CommissionRecipeEntry
                    {
                        recipe_id = WitchGoldcraftRecipe,
                        character_id = WitchId,
                        input_item_id = OreId,
                        input_quantity = 3,
                        output_item_id = GoldcraftId,
                        output_quantity = 1,
                        duration_seconds = 120f,
                        workbench_slot_index_max = 2,
                    },
                    new CommissionRecipeEntry
                    {
                        recipe_id = GuardPatrolRecipe,
                        character_id = GuardId,
                        input_item_id = "",
                        input_quantity = 0,
                        output_item_id = "seed_tomato",
                        output_quantity = 1,
                        duration_seconds = 90f,
                        workbench_slot_index_max = 2,
                    },
                    new CommissionRecipeEntry
                    {
                        recipe_id = FarmTomatoRecipe,
                        character_id = FarmGirlId,
                        input_item_id = "seed_tomato",
                        input_quantity = 1,
                        output_item_id = "crop_tomato",
                        output_quantity = 1,
                        duration_seconds = 30f,
                        workbench_slot_index_max = 2,
                    },
                }
            };
            return new CommissionRecipesConfig(data);
        }
    }
}
