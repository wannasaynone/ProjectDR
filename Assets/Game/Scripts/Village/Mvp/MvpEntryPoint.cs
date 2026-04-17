// MvpEntryPoint — MVP 場景（MvpMain.unity）的 MonoBehaviour 進入點。
// Awake/Start 流程：
//   1. 載入 mvp-config.json → MvpConfig
//   2. 建立所有 pure logic 系統並注入相依
//   3. 取得 MvpMainView（由 Scene 上的 Prefab 掛）並 Initialize
//   4. 註冊 MvpCharacterInteractionView Prefab 給 ViewStackController（或本地切換）
// Update：依序 Tick(deltaTime) 所有計時類系統。

using System;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Mvp.UI;
using ProjectDR.Village.UI;
using UnityEngine;

namespace ProjectDR.Village.Mvp
{
    /// <summary>
    /// MVP 場景進入點。
    /// </summary>
    public class MvpEntryPoint : MonoBehaviour
    {
        [Header("Config")]
        [Tooltip("MVP 配置 JSON（拖曳 Resources/Config/mvp-config.json 的 TextAsset）。")]
        [SerializeField] private TextAsset _mvpConfigJson;

        [Tooltip("好感度配置 JSON（拖曳 Resources/Config/affinity-config.json）。")]
        [SerializeField] private TextAsset _affinityConfigJson;

        [Header("主畫面")]
        [Tooltip("場景中已掛在 Canvas 底下的 MvpMainView 實例（Prefab 拖入 Scene 後）。")]
        [SerializeField] private MvpMainView _mainView;

        [Header("角色互動畫面")]
        [Tooltip("MvpCharacterInteractionView Prefab（會被 Instantiate 到 _interactionContainer 下）。")]
        [SerializeField] private MvpCharacterInteractionView _interactionViewPrefab;

        [Tooltip("角色互動畫面的掛載容器（通常為 Canvas 下的 GameObject）。")]
        [SerializeField] private Transform _interactionContainer;

        [Header("隨機來源")]
        [Tooltip("固定隨機種子（0 表示使用系統時間）。")]
        [SerializeField] private int _randomSeed = 0;

        // === 邏輯層系統 ===
        private MvpConfig _config;
        private ResourceManager _resourceManager;
        private ColdStatusSystem _coldStatus;
        private ActionTimeManager _actionTime;
        private FireSystem _fireSystem;
        private PopulationManager _populationManager;
        private HutBuildSystem _hutBuildSystem;
        private SearchSystem _searchSystem;
        private NpcArrivalManager _npcArrivalManager;
        private NPCInitiativeManager _initiativeManager;
        private DialogueCooldownManager _cooldownManager;
        private MvpDialogueSession _dialogueSession;

        // 重用既有系統
        private DialogueManager _dialogueManager;
        private AffinityManager _affinityManager;

        // UI
        private MvpCharacterInteractionView _currentInteractionInstance;

        private bool _initialized;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;

            // 1) 載入 Config
            MvpConfigData configData = null;
            if (_mvpConfigJson != null)
            {
                configData = JsonUtility.FromJson<MvpConfigData>(_mvpConfigJson.text);
            }
            if (configData == null)
            {
                Debug.LogError("[MvpEntryPoint] mvp-config.json 缺失或反序列化失敗。");
                return;
            }
            _config = new MvpConfig(configData);

            // 2) 既有系統
            _dialogueManager = new DialogueManager();

            AffinityConfigData affinityData = _affinityConfigJson != null
                ? JsonUtility.FromJson<AffinityConfigData>(_affinityConfigJson.text)
                : new AffinityConfigData
                {
                    characters = Array.Empty<AffinityCharacterConfigData>(),
                    defaultThresholds = new[] { 5 }
                };
            AffinityConfig affinityConfig = new AffinityConfig(affinityData);
            _affinityManager = new AffinityManager(affinityConfig);

            // 3) MVP 邏輯層系統（建立順序反映相依）
            IRandomSource random = _randomSeed != 0
                ? new SystemRandomSource(_randomSeed)
                : new SystemRandomSource();

            _resourceManager = new ResourceManager();
            _coldStatus = new ColdStatusSystem();
            _actionTime = new ActionTimeManager(_coldStatus, _config.ColdActionCooldownMultiplier);
            _fireSystem = new FireSystem(_resourceManager, _config);
            _populationManager = new PopulationManager(_config.InitialPopulationCap);
            _hutBuildSystem = new HutBuildSystem(_resourceManager, _fireSystem, _populationManager, _config);
            _searchSystem = new SearchSystem(_resourceManager, _actionTime, _config, random);
            _npcArrivalManager = new NpcArrivalManager(_populationManager, _config, random);
            _initiativeManager = new NPCInitiativeManager(_config);
            _cooldownManager = new DialogueCooldownManager(_config, new NoDispatchProvider());
            _dialogueSession = new MvpDialogueSession(
                _dialogueManager, _affinityManager,
                _cooldownManager, _initiativeManager,
                _config, random);

            // 4) UI 初始化
            if (_mainView != null)
            {
                _mainView.Initialize(
                    _resourceManager,
                    _fireSystem,
                    _hutBuildSystem,
                    _searchSystem,
                    _coldStatus,
                    _npcArrivalManager,
                    _initiativeManager,
                    _affinityManager,
                    _actionTime,
                    OnCharacterClickedFromMain);
                _mainView.Show();
            }
            else
            {
                Debug.LogWarning("[MvpEntryPoint] _mainView 未指定，UI 未初始化。");
            }

            EventBus.Subscribe<MvpNpcArrivedEvent>(OnNpcArrivedAutoOpenInteraction);

            _initialized = true;
        }

        private void OnNpcArrivedAutoOpenInteraction(MvpNpcArrivedEvent e)
        {
            // NPC 到訪時自動開啟互動 View（Dark Room 風的登場事件）。
            OnCharacterClickedFromMain(e.CharacterId);
        }

        private void Update()
        {
            if (!_initialized) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            _actionTime?.Tick(dt);
            _fireSystem?.Tick(dt);
            _hutBuildSystem?.Tick(dt);
            _searchSystem?.Tick(dt);
            _cooldownManager?.Tick(dt);
            _initiativeManager?.Tick(dt);
        }

        private void OnCharacterClickedFromMain(string characterId)
        {
            if (_interactionViewPrefab == null || _interactionContainer == null)
            {
                Debug.LogWarning("[MvpEntryPoint] interactionViewPrefab / container 未設定。");
                return;
            }

            // 關閉主畫面，開啟互動畫面
            if (_mainView != null) _mainView.Hide();

            if (_currentInteractionInstance != null)
            {
                Destroy(_currentInteractionInstance.gameObject);
                _currentInteractionInstance = null;
            }

            MvpCharacterInteractionView instance = Instantiate(_interactionViewPrefab, _interactionContainer);
            _currentInteractionInstance = instance;

            instance.Initialize(
                _dialogueSession,
                _dialogueManager,
                _affinityManager,
                _initiativeManager,
                _cooldownManager,
                OnInteractionReturn);

            // 從已到訪 NPC 清單找顯示名
            string displayName = characterId;
            if (_npcArrivalManager != null)
            {
                foreach (MvpPlaceholderCharacterData c in _npcArrivalManager.ArrivedCharacters)
                {
                    if (c.characterId == characterId)
                    {
                        displayName = c.displayName;
                        break;
                    }
                }
            }
            instance.SetCharacter(characterId, displayName);
            instance.Show();
        }

        private void OnInteractionReturn()
        {
            if (_currentInteractionInstance != null)
            {
                Destroy(_currentInteractionInstance.gameObject);
                _currentInteractionInstance = null;
            }
            if (_mainView != null) _mainView.Show();
        }

        private void OnDestroy()
        {
            _coldStatus?.Dispose();
            _npcArrivalManager?.Dispose();
            _initiativeManager?.Dispose();
            _dialogueSession?.Dispose();

            EventBus.ForceClearAll();
        }
    }
}
