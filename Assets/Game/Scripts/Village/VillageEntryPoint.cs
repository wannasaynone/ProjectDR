using System.Collections;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Exploration;
using ProjectDR.Village.UI;
using UnityEngine;

namespace ProjectDR.Village
{
    /// <summary>
    /// 村莊場景的進入點（MonoBehaviour）。
    /// 負責建立所有村莊模組、注入相依關係、初始化 UI 並監聽導航事件。
    /// 掛載在場景的 VillageEntryPoint GameObject 上。
    /// </summary>
    public class VillageEntryPoint : MonoBehaviour
    {
        [Header("View Prefabs")]
        [SerializeField] private VillageHubView _hubViewPrefab;
        [SerializeField] private CharacterInteractionView _characterInteractionViewPrefab;
        [SerializeField] private StorageAreaView _storageViewPrefab;
        [SerializeField] private ExplorationAreaView _explorationViewPrefab;
        [SerializeField] private AlchemyAreaView _alchemyViewPrefab;
        [SerializeField] private FarmAreaView _farmViewPrefab;

        [Header("Village Canvas")]
        [SerializeField] private Canvas _villageCanvas;

        [Header("Exploration Config")]
        [SerializeField] private TextAsset _mapJson;
        [SerializeField] private TextAsset _combatConfigJson;
        [SerializeField] private TextAsset _monsterConfigJson;

        [Header("UI Container")]
        [SerializeField] private Transform _uiContainer;

        [Header("打字機設定")]
        [Tooltip("打字機每秒顯示的字元數")]
        [SerializeField] private float _typewriterCharsPerSecond = 20f;

        private StorageManager _storageManager;
        private BackpackManager _backpackManager;
        private StorageTransferManager _transferManager;
        private VillageProgressionManager _progressionManager;
        private VillageNavigationManager _navigationManager;
        private ExplorationEntryManager _explorationManager;
        private QuestManager _questManager;
        private DialogueManager _dialogueManager;

        // V3 農田系統
        private ItemTypeResolver _itemTypeResolver;
        private FarmManager _farmManager;
        private ITimeProvider _timeProvider;

        private ViewStackController _stackController;
        private readonly HashSet<string> _initializedViews = new HashSet<string>();

        // 探索切換用
        private GameObject _explorationRoot;

        // 角色資料（IT 階段在此定義 placeholder）
        private List<CharacterMenuData> _characters;

        private void Start()
        {
            InitializeManagers();
            InitializeCharacterData();
            InitializeUI();
            SubscribeToNavigationEvents();
            SubscribeToExplorationEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromNavigationEvents();
            UnsubscribeFromExplorationEvents();

            if (_explorationManager != null)
            {
                _explorationManager.Dispose();
            }
        }

        private void InitializeManagers()
        {
            // 依照相依順序建立模組（被依賴的先建立）
            _storageManager = new StorageManager();

            // IT 階段暫用硬編碼數值，正式版本應從外部資料源載入
            // TODO: 將 maxSlots 與 defaultMaxStack 移至外部配置
            int backpackMaxSlots = 20;
            int backpackDefaultMaxStack = 99;
            _backpackManager = new BackpackManager(backpackMaxSlots, backpackDefaultMaxStack);
            _transferManager = new StorageTransferManager(_backpackManager, _storageManager);

            _progressionManager = new VillageProgressionManager();
            _navigationManager = new VillageNavigationManager(_progressionManager);
            _explorationManager = new ExplorationEntryManager(_backpackManager);
            _questManager = new QuestManager(_storageManager);
            _dialogueManager = new DialogueManager();

            // V3 農田系統
            _timeProvider = new SystemTimeProvider();
            _itemTypeResolver = new ItemTypeResolver();

            // IT 階段：手動註冊物品分類
            // TODO: 正式版本應從外部資料表載入
            _itemTypeResolver.Register("seed_wheat", ItemTypes.Seed);
            _itemTypeResolver.Register("seed_carrot", ItemTypes.Seed);
            _itemTypeResolver.Register("seed_herb", ItemTypes.Seed);
            _itemTypeResolver.Register("wheat", ItemTypes.Ingredient);
            _itemTypeResolver.Register("carrot", ItemTypes.Ingredient);
            _itemTypeResolver.Register("herb", ItemTypes.Ingredient);

            var seedDataMap = new Dictionary<string, SeedData>
            {
                { "seed_wheat", new SeedData("seed_wheat", "wheat", 300f) },    // 5 分鐘
                { "seed_carrot", new SeedData("seed_carrot", "carrot", 600f) }, // 10 分鐘
                { "seed_herb", new SeedData("seed_herb", "herb", 180f) }       // 3 分鐘
            };

            int farmPlotCount = 3;
            _farmManager = new FarmManager(
                farmPlotCount, seedDataMap, _itemTypeResolver,
                _storageManager, _timeProvider);

            // IT 測試用：預設種子入庫，方便測試農田種植功能
            // TODO: 正式版本刪除此段
            _storageManager.AddItem("seed_wheat", 5);
            _storageManager.AddItem("seed_carrot", 3);
            _storageManager.AddItem("seed_herb", 2);

            // IT 階段：強制解鎖所有角色 ID 以便導航
            _progressionManager.ForceUnlock(CharacterIds.VillageChiefWife);
            _progressionManager.ForceUnlock(CharacterIds.Guard);
            _progressionManager.ForceUnlock(CharacterIds.Witch);
            _progressionManager.ForceUnlock(CharacterIds.FarmGirl);
        }

        /// <summary>
        /// 初始化 IT 階段的角色資料（placeholder 對話與功能選單）。
        /// 正式版本應從外部資料源載入。
        /// </summary>
        private void InitializeCharacterData()
        {
            _characters = new List<CharacterMenuData>
            {
                new CharacterMenuData(
                    CharacterIds.VillageChiefWife,
                    "村長夫人",
                    new DialogueData(new string[]
                    {
                        "歡迎回來，今天辛苦了。",
                        "倉庫裡的物資我已經整理好了，需要什麼儘管拿。"
                    }),
                    new string[] { AreaIds.Storage, FunctionIds.Dialogue }
                ),
                new CharacterMenuData(
                    CharacterIds.Guard,
                    "守衛",
                    new DialogueData(new string[]
                    {
                        "又要出門嗎？小心點。",
                        "森林裡最近不太安寧。"
                    }),
                    new string[] { AreaIds.Exploration, FunctionIds.Dialogue }
                ),
                new CharacterMenuData(
                    CharacterIds.Witch,
                    "魔女",
                    new DialogueData(new string[]
                    {
                        "嗯...你來了啊。",
                        "需要藥水的話，自己看著辦吧。"
                    }),
                    new string[] { AreaIds.Alchemy, FunctionIds.Dialogue }
                ),
                new CharacterMenuData(
                    CharacterIds.FarmGirl,
                    "農女",
                    new DialogueData(new string[]
                    {
                        "啊！你來得正好！",
                        "今天的作物長得可好了！"
                    }),
                    new string[] { AreaIds.Farm, FunctionIds.Dialogue }
                )
            };
        }

        private void InitializeUI()
        {
            _stackController = new ViewStackController(_uiContainer);

            // 註冊 View Prefab（延遲 Instantiate）
            _stackController.RegisterPrefab(AreaIds.Hub, _hubViewPrefab);

            // 所有角色共用同一個 CharacterInteractionView Prefab，
            // 但以不同角色 ID 註冊，讓每個角色有獨立的 View 實例
            foreach (CharacterMenuData character in _characters)
            {
                _stackController.RegisterPrefab(character.CharacterId, _characterInteractionViewPrefab);
            }

            // 先建立 Hub 實例並注入相依，再顯示
            InitializeViewDependencies();
            _stackController.SetRoot(AreaIds.Hub);
        }

        private void InitializeViewDependencies()
        {
            // 取得已建立的 Hub 實例並注入相依
            VillageHubView hubView = _stackController.GetOrCreateInstance(AreaIds.Hub) as VillageHubView;
            if (hubView != null)
            {
                hubView.Initialize(_navigationManager, _characters.AsReadOnly());
            }
        }

        private void InitializeCharacterView(string characterId)
        {
            if (_initializedViews.Contains(characterId)) return;

            ViewBase view = _stackController.GetOrCreateInstance(characterId);
            if (view == null) return;

            _initializedViews.Add(characterId);

            CharacterInteractionView interactionView = view as CharacterInteractionView;
            if (interactionView == null) return;

            interactionView.Initialize(_dialogueManager, _navigationManager, _typewriterCharsPerSecond);

            // 註冊功能 View Prefab 與初始化回呼
            RegisterFunctionPrefabs(interactionView);

            // 找到對應的角色資料並設定
            CharacterMenuData characterData = FindCharacterData(characterId);
            if (characterData != null)
            {
                interactionView.SetCharacter(characterData);
            }
        }

        /// <summary>
        /// 為 CharacterInteractionView 註冊所有功能 View 的 Prefab 與初始化回呼。
        /// </summary>
        private void RegisterFunctionPrefabs(CharacterInteractionView interactionView)
        {
            // 倉庫
            if (_storageViewPrefab != null)
            {
                interactionView.RegisterFunctionPrefab(
                    AreaIds.Storage,
                    _storageViewPrefab,
                    (ViewBase view) =>
                    {
                        StorageAreaView storageView = view as StorageAreaView;
                        if (storageView != null)
                        {
                            storageView.Initialize(
                                _storageManager, _backpackManager,
                                _transferManager, _navigationManager);
                            storageView.SetReturnAction(() => interactionView.CloseOverlay());
                        }
                    }
                );
            }

            // 探索
            if (_explorationViewPrefab != null)
            {
                interactionView.RegisterFunctionPrefab(
                    AreaIds.Exploration,
                    _explorationViewPrefab,
                    (ViewBase view) =>
                    {
                        ExplorationAreaView explorationView = view as ExplorationAreaView;
                        if (explorationView != null)
                        {
                            explorationView.Initialize(_explorationManager, _navigationManager);
                            explorationView.SetReturnAction(() => interactionView.CloseOverlay());
                        }
                    }
                );
            }

            // 煉金（Placeholder）
            if (_alchemyViewPrefab != null)
            {
                interactionView.RegisterFunctionPrefab(
                    AreaIds.Alchemy,
                    _alchemyViewPrefab,
                    (ViewBase view) =>
                    {
                        AlchemyAreaView alchemyView = view as AlchemyAreaView;
                        if (alchemyView != null)
                        {
                            alchemyView.Initialize(_navigationManager);
                            alchemyView.SetReturnAction(() => interactionView.CloseOverlay());
                        }
                    }
                );
            }

            // 農場（V3 完整實作）
            if (_farmViewPrefab != null)
            {
                interactionView.RegisterFunctionPrefab(
                    AreaIds.Farm,
                    _farmViewPrefab,
                    (ViewBase view) =>
                    {
                        FarmAreaView farmView = view as FarmAreaView;
                        if (farmView != null)
                        {
                            farmView.Initialize(
                                _farmManager, _storageManager,
                                _itemTypeResolver, _navigationManager);
                            farmView.SetReturnAction(() => interactionView.CloseOverlay());
                        }
                    }
                );
            }
        }

        private CharacterMenuData FindCharacterData(string characterId)
        {
            foreach (CharacterMenuData data in _characters)
            {
                if (data.CharacterId == characterId)
                {
                    return data;
                }
            }
            return null;
        }

        private void SubscribeToNavigationEvents()
        {
            EventBus.Subscribe<NavigatedToAreaEvent>(OnNavigatedToArea);
            EventBus.Subscribe<ReturnedToHubEvent>(OnReturnedToHub);
        }

        private void UnsubscribeFromNavigationEvents()
        {
            EventBus.Unsubscribe<NavigatedToAreaEvent>(OnNavigatedToArea);
            EventBus.Unsubscribe<ReturnedToHubEvent>(OnReturnedToHub);
        }

        private void SubscribeToExplorationEvents()
        {
            EventBus.Subscribe<ExplorationDepartedEvent>(OnExplorationDeparted);
            EventBus.Subscribe<ExplorationCompletedEvent>(OnExplorationCompleted);
        }

        private void UnsubscribeFromExplorationEvents()
        {
            EventBus.Unsubscribe<ExplorationDepartedEvent>(OnExplorationDeparted);
            EventBus.Unsubscribe<ExplorationCompletedEvent>(OnExplorationCompleted);
        }

        private void OnNavigatedToArea(NavigatedToAreaEvent e)
        {
            // 判斷是角色 ID 還是舊的區域 ID
            if (IsCharacterId(e.AreaId))
            {
                InitializeCharacterView(e.AreaId);
                _stackController.PushView(e.AreaId);
            }
        }

        private void OnReturnedToHub(ReturnedToHubEvent e)
        {
            // 清除角色 View 的初始化標記，讓下次進入時重新設定角色資料
            foreach (CharacterMenuData character in _characters)
            {
                _initializedViews.Remove(character.CharacterId);
            }

            _stackController.SetRoot(AreaIds.Hub);
        }

        private bool IsCharacterId(string id)
        {
            return id == CharacterIds.VillageChiefWife
                || id == CharacterIds.Guard
                || id == CharacterIds.Witch
                || id == CharacterIds.FarmGirl;
        }

        // ===== 探索切換 =====

        private void OnExplorationDeparted(ExplorationDepartedEvent e)
        {
            // 隱藏村莊 Canvas
            if (_villageCanvas != null)
            {
                _villageCanvas.gameObject.SetActive(false);
            }

            // 動態建立探索根物件與 ExplorationEntryPoint
            _explorationRoot = new GameObject("ExplorationRoot");
            ExplorationEntryPoint explorationEntry = _explorationRoot.AddComponent<ExplorationEntryPoint>();

            // 注入配置 TextAsset（透過反射設定 SerializeField，因為無法直接存取 private field）
            // 改用 Initialize 注入村莊依賴，TextAsset 透過公開方法設定
            explorationEntry.SetConfigAssets(_mapJson, _combatConfigJson, _monsterConfigJson);
            explorationEntry.Initialize(_backpackManager, _explorationManager);
        }

        private void OnExplorationCompleted(ExplorationCompletedEvent e)
        {
            // 延遲一小段時間讓死亡動畫播放完畢
            float returnDelay = 1.5f;
            StartCoroutine(ReturnToVillageAfterDelay(returnDelay));
        }

        private IEnumerator ReturnToVillageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // 銷毀探索根物件（連同所有子物件一起清理）
            if (_explorationRoot != null)
            {
                Destroy(_explorationRoot);
                _explorationRoot = null;
            }

            // 顯示村莊 Canvas
            if (_villageCanvas != null)
            {
                _villageCanvas.gameObject.SetActive(true);
            }

            // 回到 Hub
            _navigationManager.ReturnToHub();
        }
    }
}
