// StorageExpansionManager — 倉庫擴建狀態機與流程管理器。
// 依據 GDD `storage-expansion.md`：
// - Idle → InProgress → Completed 狀態機
// - 物資扣除：先背包後倉庫
// - 倒數計時：Tick(deltaTime) 推進
// - 容量生效：倒數完成後呼叫 StorageManager.ExpandCapacity

using ProjectDR.Village.Backpack;
using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Village.Storage
{
    /// <summary>擴建狀態列舉。</summary>
    public enum StorageExpansionState
    {
        /// <summary>無擴建進行中，可開始下一次擴建。</summary>
        Idle,

        /// <summary>擴建倒數中。</summary>
        InProgress,

        /// <summary>倒數已完成，尚未確認領取（容量已生效）。</summary>
        Completed
    }

    /// <summary>StartExpansion 的錯誤類型。</summary>
    public enum StorageExpansionStartError
    {
        /// <summary>無錯誤（成功）。</summary>
        None,

        /// <summary>擴建中，不可重複開始。</summary>
        AlreadyInProgress,

        /// <summary>已達最大擴建等級。</summary>
        MaxLevelReached,

        /// <summary>物資不足。</summary>
        InsufficientResources,

        /// <summary>配置資料遺失（找不到對應等級的階段）。</summary>
        StageNotFound
    }

    /// <summary>StartExpansion 的結果。</summary>
    public class StorageExpansionStartResult
    {
        public bool IsSuccess { get; }
        public StorageExpansionStartError Error { get; }
        public int Level { get; }
        public int NextCapacity { get; }

        private StorageExpansionStartResult(bool success, StorageExpansionStartError error, int level, int nextCapacity)
        {
            IsSuccess = success;
            Error = error;
            Level = level;
            NextCapacity = nextCapacity;
        }

        public static StorageExpansionStartResult Success(int level, int nextCapacity)
        {
            return new StorageExpansionStartResult(true, StorageExpansionStartError.None, level, nextCapacity);
        }

        public static StorageExpansionStartResult Failure(StorageExpansionStartError error)
        {
            return new StorageExpansionStartResult(false, error, 0, 0);
        }
    }

    /// <summary>
    /// 倉庫擴建狀態機。
    /// 純邏輯類別，不依賴 MonoBehaviour。
    /// 外部呼叫 Tick(deltaTime) 推進倒數。
    /// </summary>
    public class StorageExpansionManager
    {
        private readonly StorageManager _storageManager;
        private readonly BackpackManager _backpackManager;
        private readonly StorageExpansionConfig _config;

        private int _currentLevel;
        private StorageExpansionState _state;
        private StorageExpansionStage _activeStage;
        private float _remainingSeconds;

        /// <summary>當前已完成的擴建等級（初始為 0，第 1 次完成後為 1）。</summary>
        public int CurrentLevel => _currentLevel;

        /// <summary>當前擴建狀態。</summary>
        public StorageExpansionState State => _state;

        /// <summary>擴建中的剩餘秒數（State 為 InProgress 時有效）。</summary>
        public float RemainingSeconds => _remainingSeconds;

        /// <summary>當前擴建階段（僅在 InProgress/Completed 狀態下非 null）。</summary>
        public StorageExpansionStage ActiveStage => _activeStage;

        /// <summary>
        /// 建構擴建管理器。
        /// </summary>
        /// <param name="storageManager">倉庫管理器（不可為 null）。</param>
        /// <param name="backpackManager">背包管理器（不可為 null，扣物資用）。</param>
        /// <param name="config">擴建配置（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">任一參數為 null 時拋出。</exception>
        public StorageExpansionManager(
            StorageManager storageManager,
            BackpackManager backpackManager,
            StorageExpansionConfig config)
        {
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _backpackManager = backpackManager ?? throw new ArgumentNullException(nameof(backpackManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _currentLevel = 0;
            _state = StorageExpansionState.Idle;
            _activeStage = null;
            _remainingSeconds = 0f;
        }

        /// <summary>
        /// 取得下一次可執行擴建的階段資料。若已達最大等級則回傳 null。
        /// </summary>
        public StorageExpansionStage GetNextStage()
        {
            int nextLevel = _currentLevel + 1;
            return _config.GetStage(nextLevel);
        }

        /// <summary>
        /// 檢查下一次擴建是否有足夠物資（先背包後倉庫合併計算）。
        /// 若無下一階段（已達上限）回傳 false。
        /// </summary>
        public bool CanStartExpansion()
        {
            if (_state != StorageExpansionState.Idle)
            {
                return false;
            }

            StorageExpansionStage next = GetNextStage();
            if (next == null)
            {
                return false;
            }

            return HasEnoughResources(next);
        }

        /// <summary>
        /// 開始擴建：扣物資、設定倒數、狀態轉為 InProgress、發布 StorageExpansionStartedEvent。
        /// </summary>
        public StorageExpansionStartResult StartExpansion()
        {
            if (_state != StorageExpansionState.Idle)
            {
                return StorageExpansionStartResult.Failure(StorageExpansionStartError.AlreadyInProgress);
            }

            StorageExpansionStage next = GetNextStage();
            if (next == null)
            {
                return StorageExpansionStartResult.Failure(StorageExpansionStartError.MaxLevelReached);
            }

            if (!HasEnoughResources(next))
            {
                return StorageExpansionStartResult.Failure(StorageExpansionStartError.InsufficientResources);
            }

            // 扣物資（先背包後倉庫）
            DeductResources(next);

            _activeStage = next;
            _state = StorageExpansionState.InProgress;
            _remainingSeconds = next.DurationSeconds;

            EventBus.Publish(new StorageExpansionStartedEvent
            {
                Level = next.Level,
                DurationSeconds = next.DurationSeconds,
                CapacityAfter = next.CapacityAfter
            });

            return StorageExpansionStartResult.Success(next.Level, next.CapacityAfter);
        }

        /// <summary>
        /// 推進倒數。若 deltaTime &lt;= 0 則忽略。
        /// 倒數歸零時自動呼叫 CompleteExpansion。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_state != StorageExpansionState.InProgress || deltaTime <= 0f)
            {
                return;
            }

            _remainingSeconds -= deltaTime;
            if (_remainingSeconds <= 0f)
            {
                _remainingSeconds = 0f;
                CompleteExpansion();
            }
        }

        /// <summary>
        /// 完成擴建：套用容量、發布完成事件。
        /// 若非 InProgress 狀態則忽略（避免重複完成）。
        /// </summary>
        public void CompleteExpansion()
        {
            if (_state != StorageExpansionState.InProgress)
            {
                return;
            }

            StorageExpansionStage stage = _activeStage;

            int delta = stage.CapacityDelta;
            if (delta > 0)
            {
                _storageManager.ExpandCapacity(delta);
            }

            _currentLevel = stage.Level;
            _state = StorageExpansionState.Completed;
            _remainingSeconds = 0f;

            EventBus.Publish(new StorageExpansionCompletedEvent
            {
                Level = stage.Level,
                CapacityAfter = stage.CapacityAfter
            });
        }

        /// <summary>
        /// 確認擴建已被玩家領取：狀態從 Completed 回到 Idle，允許下一次擴建。
        /// 若非 Completed 狀態則忽略。
        /// </summary>
        public void AcknowledgeCompletion()
        {
            if (_state != StorageExpansionState.Completed)
            {
                return;
            }

            _state = StorageExpansionState.Idle;
            _activeStage = null;
        }

        // ===== 內部資源邏輯 =====

        private bool HasEnoughResources(StorageExpansionStage stage)
        {
            foreach (KeyValuePair<string, int> requirement in stage.RequiredItems)
            {
                int have = _backpackManager.GetItemCount(requirement.Key)
                         + _storageManager.GetItemCount(requirement.Key);
                if (have < requirement.Value)
                {
                    return false;
                }
            }
            return true;
        }

        private void DeductResources(StorageExpansionStage stage)
        {
            foreach (KeyValuePair<string, int> requirement in stage.RequiredItems)
            {
                int remaining = requirement.Value;

                // 先扣背包
                int removedFromBackpack = _backpackManager.RemoveItem(requirement.Key, remaining);
                remaining -= removedFromBackpack;

                // 剩餘從倉庫扣除
                if (remaining > 0)
                {
                    bool ok = _storageManager.RemoveItem(requirement.Key, remaining);
                    // 依 HasEnoughResources 前置檢查，此處不應失敗
                    if (!ok)
                    {
                        throw new InvalidOperationException(
                            $"擴建資源扣除失敗：{requirement.Key} 尚差 {remaining}（前置檢查與實際狀態不一致）。");
                    }
                }
            }
        }
    }
}
