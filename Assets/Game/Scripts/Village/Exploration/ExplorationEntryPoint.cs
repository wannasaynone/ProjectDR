using System;
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
        private PlayerFreeMovement _playerMovement;

        private Action<PlayerCellChangedEvent> _onPlayerCellChanged;

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

            // SPD-based free movement speed provider
            IMoveSpeedProvider speedProvider = SpdMoveSpeedProvider.FromConfig(combatConfig, _playerStats.Spd);

            // Player free movement
            float cellSize = 1.0f;
            Vector3 mapOrigin = Vector3.zero; // Will be set from map view
            _playerMovement = new PlayerFreeMovement(gridMapFinal, mapData.SpawnPosition, cellSize, mapOrigin, speedProvider);

            // Sword attack
            _swordAttack = SwordAttack.FromConfig(combatConfig, _playerStats.Spd);

            // Evacuation: 6 seconds per GDD rule 21
            float evacuationDuration = 6f;
            _evacuationManager = new EvacuationManager(gridMapFinal, mapData.SpawnPosition, evacuationDuration);

            // Wire cell changed events for evacuation
            _onPlayerCellChanged = (e) =>
            {
                // When leaving a cell, cancel any active evacuation countdown
                if (_evacuationManager.IsEvacuating)
                {
                    _evacuationManager.OnPlayerMoveStarted();
                }

                // When entering a new cell, check if it's an evacuation trigger
                _evacuationManager.OnPlayerArrived(e.NewCell);
            };
            EventBus.Subscribe<PlayerCellChangedEvent>(_onPlayerCellChanged);

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

            // Player view (free movement — syncs transform each frame)
            GameObject playerViewObj = new GameObject("ExplorationFreePlayerView");
            playerViewObj.transform.SetParent(root);
            ExplorationFreePlayerView playerView = playerViewObj.AddComponent<ExplorationFreePlayerView>();
            playerView.Initialize(_playerMovement);

            // Camera follow
            GameObject cameraFollowObj = new GameObject("ExplorationCameraFollow");
            cameraFollowObj.transform.SetParent(root);
            ExplorationCameraFollow cameraFollow = cameraFollowObj.AddComponent<ExplorationCameraFollow>();
            cameraFollow.Initialize(playerViewObj.transform);

            // Player contact detector (collision-based monster contact)
            PlayerContactDetector contactDetector = playerViewObj.AddComponent<PlayerContactDetector>();
            contactDetector.Initialize(_playerMovement);

            // Combat manager (connects player attacks <-> monster damage + contact damage)
            _combatManager = new CombatManager(
                _playerStats, _swordAttack, _monsterManager,
                gridMapFinal, _playerMovement, mapView,
                combatConfig.KnockbackDistance);

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

            // Input handlers (free movement)
            ExplorationFreeInputHandler inputHandler = gameObject.AddComponent<ExplorationFreeInputHandler>();
            inputHandler.Initialize(_playerMovement, _collectionManager);

            CombatInputHandler combatInput = gameObject.AddComponent<CombatInputHandler>();
            combatInput.Initialize(_swordAttack, _playerMovement, _collectionManager, playerViewObj.transform);

            // Aim indicator
            GameObject aimIndicatorObj = new GameObject("AimIndicatorView");
            aimIndicatorObj.transform.SetParent(root);
            AimIndicatorView aimIndicator = aimIndicatorObj.AddComponent<AimIndicatorView>();
            aimIndicator.Initialize(playerViewObj.transform);

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
            playerCombatView.Initialize(_playerStats, _swordAttack, playerViewObj.transform);

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

            // 採集點地圖標記
            GameObject collectIndicatorObj = new GameObject("CollectiblePointIndicatorView");
            collectIndicatorObj.transform.SetParent(root);
            CollectiblePointIndicatorView collectIndicatorView =
                collectIndicatorObj.AddComponent<CollectiblePointIndicatorView>();
            collectIndicatorView.Initialize(gridMapFinal, mapView, mapData);

            // 互動提示
            GameObject interactionHintObj = new GameObject("CollectionInteractionHintView");
            interactionHintObj.transform.SetParent(root);
            CollectionInteractionHintView interactionHintView =
                interactionHintObj.AddComponent<CollectionInteractionHintView>();
            interactionHintView.Initialize(_collectionManager, mapView);

            // 採集進度條
            GameObject gatheringViewObj = new GameObject("CollectionGatheringView");
            gatheringViewObj.transform.SetParent(root);
            CollectionGatheringView gatheringView =
                gatheringViewObj.AddComponent<CollectionGatheringView>();
            gatheringView.Initialize(_collectionManager, mapView);

            // 物品欄 UI
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
                _monsterManager.Update(dt, _playerMovement.CurrentGridCell);
        }

        private void OnDestroy()
        {
            if (_onPlayerCellChanged != null)
                EventBus.Unsubscribe<PlayerCellChangedEvent>(_onPlayerCellChanged);
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
