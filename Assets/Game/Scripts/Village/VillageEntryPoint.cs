using KahaGameCore.GameEvent;
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
        [SerializeField] private StorageAreaView _storageViewPrefab;
        [SerializeField] private ExplorationAreaView _explorationViewPrefab;
        [SerializeField] private AlchemyAreaView _alchemyViewPrefab;
        [SerializeField] private FarmAreaView _farmViewPrefab;

        [Header("UI Container")]
        [SerializeField] private Transform _uiContainer;

        private StorageManager _storageManager;
        private VillageProgressionManager _progressionManager;
        private VillageNavigationManager _navigationManager;
        private ExplorationEntryManager _explorationManager;
        private QuestManager _questManager;

        private ViewStackController _stackController;
        private readonly System.Collections.Generic.HashSet<string> _initializedViews
            = new System.Collections.Generic.HashSet<string>();

        private void Start()
        {
            InitializeManagers();
            InitializeUI();
            SubscribeToNavigationEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromNavigationEvents();
        }

        private void InitializeManagers()
        {
            // 依照相依順序建立模組（被依賴的先建立）
            _storageManager = new StorageManager();
            _progressionManager = new VillageProgressionManager();
            _navigationManager = new VillageNavigationManager(_progressionManager);
            _explorationManager = new ExplorationEntryManager(_storageManager);
            _questManager = new QuestManager(_storageManager);

            // IT 階段：強制解鎖所有區域以便測試完整流程
            // 正式版本應移除，改為 A Dark Room 式漸進解鎖
            _progressionManager.ForceUnlock(AreaIds.Exploration);
            _progressionManager.ForceUnlock(AreaIds.Alchemy);
            _progressionManager.ForceUnlock(AreaIds.Farm);
        }

        private void InitializeUI()
        {
            _stackController = new ViewStackController(_uiContainer);

            // 註冊 View Prefab（延遲 Instantiate）
            _stackController.RegisterPrefab(AreaIds.Hub, _hubViewPrefab);
            _stackController.RegisterPrefab(AreaIds.Storage, _storageViewPrefab);
            _stackController.RegisterPrefab(AreaIds.Exploration, _explorationViewPrefab);
            _stackController.RegisterPrefab(AreaIds.Alchemy, _alchemyViewPrefab);
            _stackController.RegisterPrefab(AreaIds.Farm, _farmViewPrefab);

            // 先建立 Hub 實例並注入相依，再顯示
            InitializeViewDependencies();
            _stackController.SetRoot(AreaIds.Hub);
        }

        private void InitializeViewDependencies()
        {
            // 取得已建立的 Hub 實例並注入相依
            VillageHubView hubView = _stackController.GetOrCreateInstance(AreaIds.Hub) as VillageHubView;
            if (hubView != null) hubView.Initialize(_navigationManager);
        }

        private void InitializeAreaView(string areaId)
        {
            if (_initializedViews.Contains(areaId)) return;

            ViewBase view = _stackController.GetOrCreateInstance(areaId);
            if (view == null) return;

            _initializedViews.Add(areaId);

            switch (areaId)
            {
                case AreaIds.Storage:
                {
                    StorageAreaView storageView = view as StorageAreaView;
                    if (storageView != null) storageView.Initialize(_storageManager, _navigationManager);
                    break;
                }
                case AreaIds.Exploration:
                {
                    ExplorationAreaView explorationView = view as ExplorationAreaView;
                    if (explorationView != null) explorationView.Initialize(_explorationManager, _navigationManager);
                    break;
                }
                case AreaIds.Alchemy:
                {
                    AlchemyAreaView alchemyView = view as AlchemyAreaView;
                    if (alchemyView != null) alchemyView.Initialize(_navigationManager);
                    break;
                }
                case AreaIds.Farm:
                {
                    FarmAreaView farmView = view as FarmAreaView;
                    if (farmView != null) farmView.Initialize(_navigationManager);
                    break;
                }
            }
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

        private void OnNavigatedToArea(NavigatedToAreaEvent e)
        {
            InitializeAreaView(e.AreaId);
            _stackController.PushView(e.AreaId);
        }

        private void OnReturnedToHub(ReturnedToHubEvent e)
        {
            _stackController.SetRoot(AreaIds.Hub);
        }
    }
}
