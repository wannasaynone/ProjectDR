using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using ProjectDR.Village;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.Storage;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// StorageExpansionManager 單元測試。
    /// 驗證：狀態機轉換、物資檢查與扣除、倒數推進、事件發布、邊界條件。
    /// </summary>
    [TestFixture]
    public class StorageExpansionManagerTests
    {
        private StorageManager _storage;
        private BackpackManager _backpack;
        private StorageExpansionConfig _config;

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
            _storage = new StorageManager(10, 5);
            _backpack = new BackpackManager(20, 99);
            _config = BuildTwoStageConfig();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        // ===== Helpers =====

        private static StorageExpansionConfig BuildTwoStageConfig()
        {
            StorageExpansionConfigData data = new StorageExpansionConfigData
            {
                schema_version = 1,
                max_expansion_level = 2,
                initial_capacity = 10,
                stages = new StorageExpansionStageData[]
                {
                    new StorageExpansionStageData
                    {
                        level = 1,
                        capacity_before = 10,
                        capacity_after = 15,
                        required_items = "wood:3|cloth:2",
                        duration_seconds = 10,
                        description = "stage 1"
                    },
                    new StorageExpansionStageData
                    {
                        level = 2,
                        capacity_before = 15,
                        capacity_after = 20,
                        required_items = "wood:5",
                        duration_seconds = 20,
                        description = "stage 2"
                    }
                }
            };
            return new StorageExpansionConfig(data);
        }

        private StorageExpansionManager CreateSut()
        {
            return new StorageExpansionManager(_storage, _backpack, _config);
        }

        // ===== 建構驗證 =====

        [Test]
        public void Constructor_NullStorage_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StorageExpansionManager(null, _backpack, _config));
        }

        [Test]
        public void Constructor_NullBackpack_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StorageExpansionManager(_storage, null, _config));
        }

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StorageExpansionManager(_storage, _backpack, null));
        }

        [Test]
        public void Constructor_InitialState_Idle()
        {
            StorageExpansionManager sut = CreateSut();
            Assert.AreEqual(StorageExpansionState.Idle, sut.State);
            Assert.AreEqual(0, sut.CurrentLevel);
        }

        [Test]
        public void GetNextStage_InitialState_ReturnsLevel1Stage()
        {
            StorageExpansionManager sut = CreateSut();
            StorageExpansionStage next = sut.GetNextStage();
            Assert.IsNotNull(next);
            Assert.AreEqual(1, next.Level);
        }

        // ===== CanStartExpansion =====

        [Test]
        public void CanStartExpansion_NoResources_ReturnsFalse()
        {
            StorageExpansionManager sut = CreateSut();
            Assert.IsFalse(sut.CanStartExpansion());
        }

        [Test]
        public void CanStartExpansion_EnoughFromBackpack_ReturnsTrue()
        {
            _backpack.AddItem("wood", 3);
            _backpack.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();
            Assert.IsTrue(sut.CanStartExpansion());
        }

        [Test]
        public void CanStartExpansion_EnoughFromStorage_ReturnsTrue()
        {
            _storage.AddItem("wood", 3);
            _storage.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();
            Assert.IsTrue(sut.CanStartExpansion());
        }

        [Test]
        public void CanStartExpansion_SplitBetweenBackpackAndStorage_ReturnsTrue()
        {
            _backpack.AddItem("wood", 2);
            _storage.AddItem("wood", 1);
            _storage.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();
            Assert.IsTrue(sut.CanStartExpansion());
        }

        // ===== StartExpansion =====

        [Test]
        public void StartExpansion_EnoughResources_Success()
        {
            _backpack.AddItem("wood", 3);
            _backpack.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();

            StorageExpansionStartResult result = sut.StartExpansion();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, result.Level);
            Assert.AreEqual(15, result.NextCapacity);
            Assert.AreEqual(StorageExpansionState.InProgress, sut.State);
        }

        [Test]
        public void StartExpansion_DeductsResourcesFromBackpackFirst()
        {
            _backpack.AddItem("wood", 3);
            _backpack.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();

            sut.StartExpansion();

            Assert.AreEqual(0, _backpack.GetItemCount("wood"));
            Assert.AreEqual(0, _backpack.GetItemCount("cloth"));
        }

        [Test]
        public void StartExpansion_BackpackInsufficient_DeductsRestFromStorage()
        {
            _backpack.AddItem("wood", 1);
            _storage.AddItem("wood", 2);
            _storage.AddItem("cloth", 2);

            StorageExpansionManager sut = CreateSut();
            sut.StartExpansion();

            Assert.AreEqual(0, _backpack.GetItemCount("wood"));
            Assert.AreEqual(0, _storage.GetItemCount("wood"));
            Assert.AreEqual(0, _storage.GetItemCount("cloth"));
        }

        [Test]
        public void StartExpansion_Insufficient_ReturnsFailure()
        {
            StorageExpansionManager sut = CreateSut();
            StorageExpansionStartResult result = sut.StartExpansion();
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(StorageExpansionStartError.InsufficientResources, result.Error);
        }

        [Test]
        public void StartExpansion_AlreadyInProgress_ReturnsFailure()
        {
            _backpack.AddItem("wood", 10);
            _backpack.AddItem("cloth", 5);
            StorageExpansionManager sut = CreateSut();
            sut.StartExpansion();

            StorageExpansionStartResult second = sut.StartExpansion();
            Assert.IsFalse(second.IsSuccess);
            Assert.AreEqual(StorageExpansionStartError.AlreadyInProgress, second.Error);
        }

        [Test]
        public void StartExpansion_PublishesStartedEvent()
        {
            _backpack.AddItem("wood", 3);
            _backpack.AddItem("cloth", 2);

            StorageExpansionStartedEvent received = null;
            Action<StorageExpansionStartedEvent> handler = (e) => { received = e; };
            EventBus.Subscribe(handler);
            try
            {
                StorageExpansionManager sut = CreateSut();
                sut.StartExpansion();
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            Assert.IsNotNull(received);
            Assert.AreEqual(1, received.Level);
            Assert.AreEqual(15, received.CapacityAfter);
            Assert.AreEqual(10f, received.DurationSeconds);
        }

        // ===== Tick / CompleteExpansion =====

        [Test]
        public void Tick_PartialTime_DecrementsRemaining()
        {
            _backpack.AddItem("wood", 3);
            _backpack.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();
            sut.StartExpansion();

            sut.Tick(3f);

            Assert.AreEqual(StorageExpansionState.InProgress, sut.State);
            Assert.AreEqual(7f, sut.RemainingSeconds, 0.001f);
        }

        [Test]
        public void Tick_FullDuration_Completes()
        {
            _backpack.AddItem("wood", 3);
            _backpack.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();
            sut.StartExpansion();

            sut.Tick(10f);

            Assert.AreEqual(StorageExpansionState.Completed, sut.State);
            Assert.AreEqual(1, sut.CurrentLevel);
        }

        [Test]
        public void Tick_OverDuration_CompletesAtZero()
        {
            _backpack.AddItem("wood", 3);
            _backpack.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();
            sut.StartExpansion();

            sut.Tick(25f);

            Assert.AreEqual(StorageExpansionState.Completed, sut.State);
            Assert.AreEqual(0f, sut.RemainingSeconds);
        }

        [Test]
        public void CompleteExpansion_AppliesCapacityToStorage()
        {
            _backpack.AddItem("wood", 3);
            _backpack.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();
            sut.StartExpansion();
            int before = _storage.Capacity;

            sut.CompleteExpansion();

            Assert.AreEqual(before + 5, _storage.Capacity);
        }

        [Test]
        public void CompleteExpansion_PublishesCompletedEvent()
        {
            _backpack.AddItem("wood", 3);
            _backpack.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();
            sut.StartExpansion();

            StorageExpansionCompletedEvent received = null;
            Action<StorageExpansionCompletedEvent> handler = (e) => { received = e; };
            EventBus.Subscribe(handler);
            try
            {
                sut.CompleteExpansion();
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }

            Assert.IsNotNull(received);
            Assert.AreEqual(1, received.Level);
        }

        [Test]
        public void CompleteExpansion_Twice_SecondIgnored()
        {
            _backpack.AddItem("wood", 3);
            _backpack.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();
            sut.StartExpansion();
            sut.CompleteExpansion();
            int capacityAfterFirst = _storage.Capacity;
            sut.CompleteExpansion();
            Assert.AreEqual(capacityAfterFirst, _storage.Capacity);
        }

        // ===== AcknowledgeCompletion + 第二輪擴建 =====

        [Test]
        public void AcknowledgeCompletion_ResetsStateToIdle()
        {
            _backpack.AddItem("wood", 3);
            _backpack.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();
            sut.StartExpansion();
            sut.CompleteExpansion();
            sut.AcknowledgeCompletion();
            Assert.AreEqual(StorageExpansionState.Idle, sut.State);
        }

        [Test]
        public void SecondExpansion_AfterAcknowledge_UsesLevel2Stage()
        {
            _backpack.AddItem("wood", 8); // 3 for level1 + 5 for level2
            _backpack.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();

            sut.StartExpansion();
            sut.CompleteExpansion();
            sut.AcknowledgeCompletion();

            StorageExpansionStartResult second = sut.StartExpansion();
            Assert.IsTrue(second.IsSuccess);
            Assert.AreEqual(2, second.Level);
            Assert.AreEqual(20, second.NextCapacity);
        }

        [Test]
        public void StartExpansion_WhenMaxLevelReached_ReturnsMaxLevelReached()
        {
            // 完成兩輪後沒有第 3 階段
            _backpack.AddItem("wood", 8);
            _backpack.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();

            sut.StartExpansion();
            sut.CompleteExpansion();
            sut.AcknowledgeCompletion();

            sut.StartExpansion();
            sut.CompleteExpansion();
            sut.AcknowledgeCompletion();

            StorageExpansionStartResult third = sut.StartExpansion();
            Assert.IsFalse(third.IsSuccess);
            Assert.AreEqual(StorageExpansionStartError.MaxLevelReached, third.Error);
        }

        // ===== Tick 在非 InProgress 狀態下無作用 =====

        [Test]
        public void Tick_IdleState_NoOp()
        {
            StorageExpansionManager sut = CreateSut();
            sut.Tick(100f);
            Assert.AreEqual(StorageExpansionState.Idle, sut.State);
        }

        [Test]
        public void Tick_ZeroDeltaTime_NoChange()
        {
            _backpack.AddItem("wood", 3);
            _backpack.AddItem("cloth", 2);
            StorageExpansionManager sut = CreateSut();
            sut.StartExpansion();
            float before = sut.RemainingSeconds;
            sut.Tick(0f);
            Assert.AreEqual(before, sut.RemainingSeconds);
        }
    }
}
