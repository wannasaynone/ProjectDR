using KahaGameCore.GameEvent;
using ProjectDR.Village;
using ProjectDR.Village.Exploration.Combat;
using UnityEngine;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// Scene entry point for the exploration mode.
    /// Assembles the logic layer and the view layer in the correct order,
    /// then publishes ExplorationMapInitializedEvent to signal readiness.
    ///
    /// Loads external configs:
    /// - Maps/it-test-map.json: map layout and monster spawn points
    /// - Config/combat-config.json: player stats, sword params, move speed
    /// - Config/monster-config.json: monster type definitions
    /// </summary>
    public class ExplorationEntryPoint : MonoBehaviour
    {
        [SerializeField] private TextAsset _mapJson;
        [SerializeField] private TextAsset _combatConfigJson;
        [SerializeField] private TextAsset _monsterConfigJson;

        private EvacuationManager _evacuationManager;
        private CollectionManager _collectionManager;
        private MonsterManager _monsterManager;
        private CombatManager _combatManager;
        private DeathManager _deathManager;
        private SwordAttack _swordAttack;
        private PlayerCombatStats _playerStats;
        private PlayerGridMovement _playerMovement;

        private System.Action<PlayerMoveCompletedEvent> _onPlayerMoveCompleted;
        private System.Action<PlayerMoveStartedEvent> _onPlayerMoveStarted;

        // Village-level dependencies (injected via Initialize before Start)
        private BackpackManager _villageBackpack;
        private ExplorationEntryManager _explorationEntryManager;

        /// <summary>
        /// Sets the config TextAssets for dynamic creation (when not using SerializeField).
        /// Must be called before Start() runs.
        /// </summary>
        public void SetConfigAssets(TextAsset mapJson, TextAsset combatConfigJson, TextAsset monsterConfigJson)
        {
            _mapJson = mapJson;
            _combatConfigJson = combatConfigJson;
            _monsterConfigJson = monsterConfigJson;
        }

        /// <summary>
        /// Injects village-level dependencies before Start() runs.
        /// If not called, exploration runs in standalone mode without death recovery.
        /// </summary>
        public void Initialize(BackpackManager villageBackpack, ExplorationEntryManager explorationEntryManager)
        {
            _villageBackpack = villageBackpack;
            _explorationEntryManager = explorationEntryManager;
        }

        private void Start()
        {
            // --- Stage 1: Load data ---
            MapData mapData = MapDataLoader.Load(_mapJson.text);

            CombatConfig combatConfig = LoadCombatConfig();
            MonsterConfig monsterConfig = LoadMonsterConfig();

            // --- Stage 2: Build logic layer ---

            // Player combat stats from config
            _playerStats = PlayerCombatStats.FromConfig(combatConfig);

            // SPD-based move speed calculator
            IMoveSpeedCalculator speedCalc = SpdMoveSpeedCalculator.FromConfig(combatConfig, _playerStats.Spd);

            // GridMap (with null monster provider initially — set after MonsterManager is created)
            GridMap gridMapFinal = new GridMap(mapData, null);

            int revealRadius = 3; // GDD rule 3
            int evacGroupIndex = mapData.EvacuationGroups.Count > 0 ? 0 : -1;
            gridMapFinal.InitializeExplored(revealRadius, evacGroupIndex);

            // MonsterManager with gridMapFinal and recalculation callback
            _monsterManager = new MonsterManager(gridMapFinal, () => gridMapFinal.RecalculateAllMonsterCounts());

            // Now wire the MonsterManager as the position provider for monster count queries
            gridMapFinal.SetMonsterPositionProvider(_monsterManager);

            // Spawn monsters from map data
            for (int i = 0; i < mapData.MonsterSpawnPoints.Count; i++)
            {
                MonsterSpawnPoint sp = mapData.MonsterSpawnPoints[i];
                MonsterTypeData typeData = monsterConfig.GetType(sp.TypeId);
                if (typeData != null)
                {
                    _monsterManager.SpawnMonster(typeData, sp.Position);
                }
            }

            // Player movement
            _playerMovement = new PlayerGridMovement(gridMapFinal, mapData.SpawnPosition, speedCalc);

            // Rule 47: visible monsters block player entry
            _playerMovement.SetAdditionalBlockCheck((x, y) =>
            {
                if (!gridMapFinal.IsExplored(x, y)) return false;
                return _monsterManager.GetMonsterAt(x, y) != null;
            });

            // Sword attack
            _swordAttack = SwordAttack.FromConfig(combatConfig, _playerStats.Spd);

            // Evacuation: 6 seconds per GDD rule 21
            float evacuationDuration = 6f;
            _evacuationManager = new EvacuationManager(gridMapFinal, mapData.SpawnPosition, evacuationDuration);

            // Wire events
            _onPlayerMoveCompleted = (e) => _evacuationManager.OnPlayerArrived(e.Position);
            _onPlayerMoveStarted = (e) =>
            {
                _evacuationManager.OnPlayerMoveStarted();
                _combatManager?.OnPlayerMoveStarted(e.From);
            };
            EventBus.Subscribe<PlayerMoveCompletedEvent>(_onPlayerMoveCompleted);
            EventBus.Subscribe<PlayerMoveStartedEvent>(_onPlayerMoveStarted);

            // Collection system — use village backpack if injected, otherwise create standalone
            BackpackManager backpack = _villageBackpack ?? new BackpackManager(10, 99);
            _collectionManager = new CollectionManager(gridMapFinal, _playerMovement, backpack);

            // --- Stage 3: Build view layer ---
            // All dynamically created GameObjects are parented to this transform
            // so that destroying this GameObject cleans up everything.
            Transform root = this.transform;

            GameObject mapViewObj = new GameObject("ExplorationMapView");
            mapViewObj.transform.SetParent(root);
            ExplorationMapView mapView = mapViewObj.AddComponent<ExplorationMapView>();
            mapView.Initialize(gridMapFinal, mapData);

            GameObject playerViewObj = new GameObject("ExplorationPlayerView");
            playerViewObj.transform.SetParent(root);
            ExplorationPlayerView playerView = playerViewObj.AddComponent<ExplorationPlayerView>();
            playerView.Initialize(_playerMovement, mapView, mapData.SpawnPosition);

            // Camera follow — 攝影機跟隨玩家 token
            GameObject cameraFollowObj = new GameObject("ExplorationCameraFollow");
            cameraFollowObj.transform.SetParent(root);
            ExplorationCameraFollow cameraFollow = cameraFollowObj.AddComponent<ExplorationCameraFollow>();
            cameraFollow.Initialize(playerViewObj.transform);

            // Combat manager (connects player attacks <-> monster damage)
            _combatManager = new CombatManager(
                _playerStats, _swordAttack, _monsterManager,
                gridMapFinal, _playerMovement, mapView);

            // Death manager (GDD rules 27-30: HP=0 -> time rewind -> backpack restore -> end exploration)
            if (_explorationEntryManager != null)
            {
                _deathManager = new DeathManager(backpack, _explorationEntryManager);

                // Death view (screen overlay: red flash -> darken -> "時間回溯..." text)
                GameObject deathViewObj = new GameObject("DeathView");
                deathViewObj.transform.SetParent(root);
                DeathView deathView = deathViewObj.AddComponent<DeathView>();
                deathView.Initialize();
            }

            // Input handlers
            ExplorationInputHandler inputHandler = gameObject.AddComponent<ExplorationInputHandler>();
            inputHandler.Initialize(_playerMovement, _collectionManager);

            CombatInputHandler combatInput = gameObject.AddComponent<CombatInputHandler>();
            combatInput.Initialize(_swordAttack, _playerMovement, mapView, _collectionManager, playerViewObj.transform);

            // Evacuation countdown display
            Vector3 evacViewPos = mapView.GridToWorldPosition(mapData.Width / 2, 0)
                + new Vector3(0f, 1.5f, 0f);
            GameObject evacViewObj = new GameObject("EvacuationView");
            evacViewObj.transform.SetParent(root);
            EvacuationView evacView = evacViewObj.AddComponent<EvacuationView>();
            evacView.Initialize(_evacuationManager, evacViewPos);

            // Player combat view (HP bar, sweep indicator)
            GameObject playerCombatViewObj = new GameObject("PlayerCombatView");
            playerCombatViewObj.transform.SetParent(root);
            PlayerCombatView playerCombatView = playerCombatViewObj.AddComponent<PlayerCombatView>();
            playerCombatView.Initialize(_playerStats, _swordAttack, playerView, mapView);

            // Monster views
            for (int i = 0; i < _monsterManager.Monsters.Count; i++)
            {
                MonsterState ms = _monsterManager.Monsters[i];
                GameObject monsterViewObj = new GameObject($"Monster_{ms.TypeData.TypeId}_{ms.Id}");
                monsterViewObj.transform.SetParent(root);
                MonsterView monsterView = monsterViewObj.AddComponent<MonsterView>();
                monsterView.Initialize(ms, gridMapFinal, mapView);
            }

            // Damage number view
            GameObject dmgNumObj = new GameObject("DamageNumberView");
            dmgNumObj.transform.SetParent(root);
            DamageNumberView dmgNumView = dmgNumObj.AddComponent<DamageNumberView>();
            dmgNumView.Initialize(mapView);

            // --- Collection Views ---

            // 採集點地圖標記：已探索且有採集點的格子疊加藍色小方塊
            GameObject collectIndicatorObj = new GameObject("CollectiblePointIndicatorView");
            collectIndicatorObj.transform.SetParent(root);
            CollectiblePointIndicatorView collectIndicatorView =
                collectIndicatorObj.AddComponent<CollectiblePointIndicatorView>();
            collectIndicatorView.Initialize(gridMapFinal, mapView, mapData);

            // 互動提示：站在採集點上顯示「按 E 採集」/「按 E 取消」
            GameObject interactionHintObj = new GameObject("CollectionInteractionHintView");
            interactionHintObj.transform.SetParent(root);
            CollectionInteractionHintView interactionHintView =
                interactionHintObj.AddComponent<CollectionInteractionHintView>();
            interactionHintView.Initialize(_collectionManager, mapView);

            // 採集進度條（第一層計時）：採集開始後顯示進度條
            GameObject gatheringViewObj = new GameObject("CollectionGatheringView");
            gatheringViewObj.transform.SetParent(root);
            CollectionGatheringView gatheringView =
                gatheringViewObj.AddComponent<CollectionGatheringView>();
            gatheringView.Initialize(_collectionManager, mapView);

            // 物品欄 UI（第二層計時）：採集完成後顯示物品欄與背包狀態
            GameObject itemPanelObj = new GameObject("CollectionItemPanelView");
            itemPanelObj.transform.SetParent(root);
            CollectionItemPanelView itemPanelView =
                itemPanelObj.AddComponent<CollectionItemPanelView>();
            itemPanelView.Initialize(_collectionManager, backpack);

            // Recalculate initial monster counts
            gridMapFinal.RecalculateAllMonsterCounts();

            // --- Stage 4: Signal initialization complete ---
            EventBus.Publish<ExplorationMapInitializedEvent>(
                new ExplorationMapInitializedEvent
                {
                    Width = mapData.Width,
                    Height = mapData.Height,
                    SpawnPosition = mapData.SpawnPosition
                });
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_evacuationManager != null)
                _evacuationManager.Update(dt);

            if (_collectionManager != null)
                _collectionManager.Update(dt);

            if (_swordAttack != null)
                _swordAttack.Update(dt);

            if (_monsterManager != null && _playerMovement != null)
                _monsterManager.Update(dt, _playerMovement.CurrentPosition);
        }

        private void OnDestroy()
        {
            if (_onPlayerMoveCompleted != null)
                EventBus.Unsubscribe<PlayerMoveCompletedEvent>(_onPlayerMoveCompleted);
            if (_onPlayerMoveStarted != null)
                EventBus.Unsubscribe<PlayerMoveStartedEvent>(_onPlayerMoveStarted);
            if (_combatManager != null)
                _combatManager.Dispose();
            if (_deathManager != null)
                _deathManager.Dispose();
        }

        private CombatConfig LoadCombatConfig()
        {
            if (_combatConfigJson != null)
                return CombatConfig.Load(_combatConfigJson.text);

            TextAsset asset = Resources.Load<TextAsset>("Config/combat-config");
            return CombatConfig.Load(asset.text);
        }

        private MonsterConfig LoadMonsterConfig()
        {
            if (_monsterConfigJson != null)
                return MonsterConfig.Load(_monsterConfigJson.text);

            TextAsset asset = Resources.Load<TextAsset>("Config/monster-config");
            return MonsterConfig.Load(asset.text);
        }
    }
}
