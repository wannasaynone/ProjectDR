// HutBuildSystem — 小屋建造系統。
// 解鎖條件：火堆曾被點燃過（FireSystem.HasEverBeenLit）。
// 開工條件：未建造中 + 木材足夠支付 config.HutWoodCost。
// 開工時扣木材、設為建造中、發布 MvpHutBuildStartedEvent。
// Tick 推進進度，完成時 PopulationManager.IncreaseCap + 發布 MvpHutBuiltEvent。

using System;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Mvp
{
    /// <summary>
    /// 小屋建造系統：啟動時扣木材、開始倒數，完成時增加人口上限。
    /// </summary>
    public class HutBuildSystem
    {
        private readonly ResourceManager _resourceManager;
        private readonly FireSystem _fireSystem;
        private readonly PopulationManager _populationManager;
        private readonly MvpConfig _config;

        private bool _isBuilding;
        private float _elapsedSeconds;

        /// <summary>當前是否建造中。</summary>
        public bool IsBuilding => _isBuilding;

        /// <summary>當前建造累計秒數。</summary>
        public float ElapsedSeconds => _elapsedSeconds;

        /// <summary>建造總秒數（config）。</summary>
        public float TotalSeconds => _config.HutBuildSeconds;

        /// <summary>蓋小屋是否解鎖（需火堆曾被點燃）。</summary>
        public bool IsUnlocked => _fireSystem.HasEverBeenLit;

        public HutBuildSystem(
            ResourceManager resourceManager,
            FireSystem fireSystem,
            PopulationManager populationManager,
            MvpConfig config)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _fireSystem = fireSystem ?? throw new ArgumentNullException(nameof(fireSystem));
            _populationManager = populationManager ?? throw new ArgumentNullException(nameof(populationManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 嘗試開始建造小屋。條件：未建造中 + 解鎖 + 木材足夠。
        /// 成功時扣木材、設為建造中、發布 MvpHutBuildStartedEvent。
        /// </summary>
        public bool TryStartBuild()
        {
            if (_isBuilding) return false;
            if (!IsUnlocked) return false;
            if (_config.HutWoodCost > 0 && !_resourceManager.TrySpend(MvpResourceIds.Wood, _config.HutWoodCost))
            {
                return false;
            }

            _isBuilding = true;
            _elapsedSeconds = 0f;
            EventBus.Publish(new MvpHutBuildStartedEvent { TotalSeconds = _config.HutBuildSeconds });
            return true;
        }

        /// <summary>推進建造進度；完成時觸發 PopulationManager.IncreaseCap 與 MvpHutBuiltEvent。</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f) throw new ArgumentException("deltaSeconds 不可為負。", nameof(deltaSeconds));
            if (!_isBuilding || deltaSeconds == 0f) return;

            _elapsedSeconds += deltaSeconds;

            if (_elapsedSeconds >= _config.HutBuildSeconds)
            {
                _elapsedSeconds = _config.HutBuildSeconds;
                _isBuilding = false;

                _populationManager.IncreaseCap(_config.HutPopulationCapIncrement);

                EventBus.Publish(new MvpHutBuiltEvent
                {
                    PopulationCapIncrement = _config.HutPopulationCapIncrement
                });
            }
            else
            {
                EventBus.Publish(new MvpHutBuildProgressEvent
                {
                    ElapsedSeconds = _elapsedSeconds,
                    TotalSeconds = _config.HutBuildSeconds
                });
            }
        }
    }
}
