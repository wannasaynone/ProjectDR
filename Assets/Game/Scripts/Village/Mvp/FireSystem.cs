// FireSystem — 火堆系統。
// 管理火堆點燃、倒數、延長、熄滅。
// 解鎖條件：木材 >= config.FireUnlockWoodThreshold。
// 點燃消耗 config.FireLightCost 木材，初始剩餘 config.FireDurationSeconds 秒。
// 延長每次 +config.FireExtendSeconds 秒，消耗 config.FireExtendCost 木材。
// Tick(delta) 推進剩餘秒數；歸零時發布 IsLit=false 事件。

using System;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Mvp
{
    /// <summary>
    /// 火堆系統：管理生火 / 延長 / 倒數 / 熄滅狀態。
    /// 解鎖條件由外部檢查 IsUnlocked；實際點火呼叫 TryLight。
    /// </summary>
    public class FireSystem
    {
        private readonly ResourceManager _resourceManager;
        private readonly MvpConfig _config;

        private bool _isLit;
        private float _remainingSeconds;

        /// <summary>火堆是否曾經被點燃過（用於解鎖蓋小屋）。</summary>
        public bool HasEverBeenLit { get; private set; }

        /// <summary>當前是否點燃。</summary>
        public bool IsLit => _isLit;

        /// <summary>當前剩餘秒數（熄滅時為 0）。</summary>
        public float RemainingSeconds => _remainingSeconds;

        /// <summary>火堆解鎖條件是否達成（木材 &gt;= 門檻）。</summary>
        public bool IsUnlocked => _resourceManager.GetAmount(MvpResourceIds.Wood) >= _config.FireUnlockWoodThreshold;

        /// <summary>建構火堆系統。</summary>
        public FireSystem(ResourceManager resourceManager, MvpConfig config)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _isLit = false;
            _remainingSeconds = 0f;
        }

        /// <summary>
        /// 嘗試點燃火堆。條件：解鎖（木材 &gt;= 門檻）、尚未點燃、木材足夠支付 lightCost。
        /// 成功時扣木材，發布 MvpFireStateChangedEvent。
        /// </summary>
        public bool TryLight()
        {
            if (_isLit) return false;
            if (!IsUnlocked) return false;
            if (_config.FireLightCost > 0 && !_resourceManager.TrySpend(MvpResourceIds.Wood, _config.FireLightCost))
            {
                return false;
            }

            _isLit = true;
            _remainingSeconds = _config.FireDurationSeconds;
            HasEverBeenLit = true;

            EventBus.Publish(new MvpFireStateChangedEvent
            {
                IsLit = true,
                RemainingSeconds = _remainingSeconds
            });
            return true;
        }

        /// <summary>
        /// 嘗試延長火堆時間。條件：火堆點燃中 + 木材足夠支付 extendCost。
        /// 成功時扣木材、剩餘時間 += extendSeconds，發布 MvpFireExtendedEvent。
        /// </summary>
        public bool TryExtend()
        {
            if (!_isLit) return false;
            if (_config.FireExtendCost > 0 && !_resourceManager.TrySpend(MvpResourceIds.Wood, _config.FireExtendCost))
            {
                return false;
            }

            _remainingSeconds += _config.FireExtendSeconds;
            EventBus.Publish(new MvpFireExtendedEvent { NewRemainingSeconds = _remainingSeconds });
            return true;
        }

        /// <summary>推進火堆倒數。若歸零則熄滅並發布事件。</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f) throw new ArgumentException("deltaSeconds 不可為負。", nameof(deltaSeconds));
            if (!_isLit || deltaSeconds == 0f) return;

            _remainingSeconds -= deltaSeconds;
            if (_remainingSeconds <= 0f)
            {
                _remainingSeconds = 0f;
                _isLit = false;
                EventBus.Publish(new MvpFireStateChangedEvent
                {
                    IsLit = false,
                    RemainingSeconds = 0f
                });
            }
            else
            {
                EventBus.Publish(new MvpFireRemainingChangedEvent { RemainingSeconds = _remainingSeconds });
            }
        }
    }
}
