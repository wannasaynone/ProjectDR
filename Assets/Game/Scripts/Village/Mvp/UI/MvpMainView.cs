// MvpMainView — MVP 主畫面 UGUI View（骨架）。
// 僅定義 C# class 結構與 SerializeField 對應，實際 Prefab 由 ui-ux-designer 建構。
// 結構：資源顯示列 + 狀態列（火堆/寒冷）+ 動作按鈕區（搜索/生火/延長/蓋屋）+ 角色清單。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDR.Village.Mvp.UI
{
    /// <summary>
    /// MVP 主畫面 View。
    /// 由 MvpEntryPoint 透過 Initialize 注入所有系統，訂閱事件刷新 UI。
    /// </summary>
    public class MvpMainView : ViewBase
    {
        [Header("資源顯示列")]
        [Tooltip("顯示木材數量（格式：『木材：N』）。")]
        [SerializeField] private TMP_Text _woodLabel;

        [Header("狀態列")]
        [Tooltip("顯示火堆剩餘秒數（格式：『火堆：NN 秒』或『火堆：熄滅』）。")]
        [SerializeField] private TMP_Text _fireStatusLabel;

        [Tooltip("寒冷狀態提示（寒冷時顯示『寒冷中（行動冷卻 ×2）』，非寒冷時 inactive）。")]
        [SerializeField] private GameObject _coldStatusRoot;

        [Tooltip("搜索隨機文字回饋（最近一次搜索的文字）。")]
        [SerializeField] private TMP_Text _feedbackLabel;

        [Header("動作按鈕區")]
        [SerializeField] private Button _searchButton;
        [SerializeField] private Button _lightFireButton;
        [SerializeField] private Button _extendFireButton;
        [SerializeField] private Button _buildHutButton;

        [Tooltip("蓋屋進度條（Slider，value 0~1）。")]
        [SerializeField] private Slider _hutBuildProgress;

        [Header("角色清單區")]
        [Tooltip("角色清單的 Layout 容器（VerticalLayoutGroup）。")]
        [SerializeField] private Transform _characterListContainer;

        [Tooltip("角色清單單項 Prefab（含 MvpCharacterListItemView 元件）。")]
        [SerializeField] private MvpCharacterListItemView _characterListItemPrefab;

        // 注入的系統
        private ResourceManager _resourceManager;
        private FireSystem _fireSystem;
        private HutBuildSystem _hutBuildSystem;
        private SearchSystem _searchSystem;
        private ColdStatusSystem _coldStatus;
        private NpcArrivalManager _npcArrivalManager;
        private NPCInitiativeManager _initiativeManager;
        private AffinityManager _affinityManager;
        private ActionTimeManager _actionTime;
        private Action<string> _onCharacterClicked;

        private TMP_Text _searchButtonLabel;
        private string _searchButtonBaseText;

        private readonly Dictionary<string, MvpCharacterListItemView> _itemViews
            = new Dictionary<string, MvpCharacterListItemView>();

        private bool _eventsSubscribed;

        /// <summary>
        /// 由 MvpEntryPoint 注入所有相依。
        /// </summary>
        public void Initialize(
            ResourceManager resourceManager,
            FireSystem fireSystem,
            HutBuildSystem hutBuildSystem,
            SearchSystem searchSystem,
            ColdStatusSystem coldStatus,
            NpcArrivalManager npcArrivalManager,
            NPCInitiativeManager initiativeManager,
            AffinityManager affinityManager,
            ActionTimeManager actionTime,
            Action<string> onCharacterClicked)
        {
            _resourceManager = resourceManager;
            _fireSystem = fireSystem;
            _hutBuildSystem = hutBuildSystem;
            _searchSystem = searchSystem;
            _coldStatus = coldStatus;
            _npcArrivalManager = npcArrivalManager;
            _initiativeManager = initiativeManager;
            _affinityManager = affinityManager;
            _actionTime = actionTime;
            _onCharacterClicked = onCharacterClicked;

            if (_searchButton != null)
            {
                _searchButtonLabel = _searchButton.GetComponentInChildren<TMP_Text>(true);
                if (_searchButtonLabel != null)
                {
                    _searchButtonBaseText = _searchButtonLabel.text;
                }
            }

            HookButtons();
            RefreshAll();
        }

        protected override void OnShow()
        {
            if (!_eventsSubscribed)
            {
                EventBus.Subscribe<MvpResourceChangedEvent>(OnResourceChanged);
                EventBus.Subscribe<MvpFireStateChangedEvent>(OnFireStateChanged);
                EventBus.Subscribe<MvpFireRemainingChangedEvent>(OnFireRemainingChanged);
                EventBus.Subscribe<MvpFireExtendedEvent>(OnFireExtended);
                EventBus.Subscribe<MvpColdStateChangedEvent>(OnColdStateChanged);
                EventBus.Subscribe<MvpHutBuildStartedEvent>(OnHutBuildStarted);
                EventBus.Subscribe<MvpHutBuildProgressEvent>(OnHutBuildProgress);
                EventBus.Subscribe<MvpHutBuiltEvent>(OnHutBuilt);
                EventBus.Subscribe<MvpNpcArrivedEvent>(OnNpcArrived);
                EventBus.Subscribe<MvpSearchCompletedEvent>(OnSearchCompleted);
                EventBus.Subscribe<MvpNpcInitiativeReadyEvent>(OnNpcInitiativeReady);
                EventBus.Subscribe<MvpNpcInitiativeConsumedEvent>(OnNpcInitiativeConsumed);
                EventBus.Subscribe<AffinityChangedEvent>(OnAffinityChanged);
                _eventsSubscribed = true;
            }
            RefreshAll();
        }

        protected override void OnHide()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            if (_searchSystem != null || _fireSystem != null || _hutBuildSystem != null)
            {
                RefreshButtonAvailability();
                RefreshActionCooldownLabel();
                RefreshHutProgressLabel();
            }
        }

        private void RefreshActionCooldownLabel()
        {
            if (_actionTime == null || _searchButtonLabel == null || _searchButtonBaseText == null) return;
            float remain = _actionTime.GetRemaining(MvpActionKeys.Search);
            if (remain > 0f)
            {
                _searchButtonLabel.text = $"{_searchButtonBaseText} ({remain:F1}s)";
            }
            else
            {
                _searchButtonLabel.text = _searchButtonBaseText;
            }
        }

        private void RefreshHutProgressLabel()
        {
            if (_feedbackLabel == null || _hutBuildSystem == null) return;
            if (!_hutBuildSystem.IsBuilding) return;
            float remaining = _hutBuildSystem.TotalSeconds - _hutBuildSystem.ElapsedSeconds;
            if (remaining < 0f) remaining = 0f;
            _feedbackLabel.text = $"蓋小屋中...（剩 {remaining:F1}s）";
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void UnsubscribeEvents()
        {
            if (!_eventsSubscribed) return;
            EventBus.Unsubscribe<MvpResourceChangedEvent>(OnResourceChanged);
            EventBus.Unsubscribe<MvpFireStateChangedEvent>(OnFireStateChanged);
            EventBus.Unsubscribe<MvpFireRemainingChangedEvent>(OnFireRemainingChanged);
            EventBus.Unsubscribe<MvpFireExtendedEvent>(OnFireExtended);
            EventBus.Unsubscribe<MvpColdStateChangedEvent>(OnColdStateChanged);
            EventBus.Unsubscribe<MvpHutBuildStartedEvent>(OnHutBuildStarted);
            EventBus.Unsubscribe<MvpHutBuildProgressEvent>(OnHutBuildProgress);
            EventBus.Unsubscribe<MvpHutBuiltEvent>(OnHutBuilt);
            EventBus.Unsubscribe<MvpNpcArrivedEvent>(OnNpcArrived);
            EventBus.Unsubscribe<MvpSearchCompletedEvent>(OnSearchCompleted);
            EventBus.Unsubscribe<MvpNpcInitiativeReadyEvent>(OnNpcInitiativeReady);
            EventBus.Unsubscribe<MvpNpcInitiativeConsumedEvent>(OnNpcInitiativeConsumed);
            EventBus.Unsubscribe<AffinityChangedEvent>(OnAffinityChanged);
            _eventsSubscribed = false;
        }

        private void HookButtons()
        {
            if (_searchButton != null)
            {
                _searchButton.onClick.RemoveAllListeners();
                _searchButton.onClick.AddListener(OnSearchClicked);
            }
            if (_lightFireButton != null)
            {
                _lightFireButton.onClick.RemoveAllListeners();
                _lightFireButton.onClick.AddListener(OnLightFireClicked);
            }
            if (_extendFireButton != null)
            {
                _extendFireButton.onClick.RemoveAllListeners();
                _extendFireButton.onClick.AddListener(OnExtendFireClicked);
            }
            if (_buildHutButton != null)
            {
                _buildHutButton.onClick.RemoveAllListeners();
                _buildHutButton.onClick.AddListener(OnBuildHutClicked);
            }
        }

        private void OnSearchClicked()
        {
            _searchSystem?.TrySearch();
        }

        private void OnLightFireClicked()
        {
            _fireSystem?.TryLight();
        }

        private void OnExtendFireClicked()
        {
            _fireSystem?.TryExtend();
        }

        private void OnBuildHutClicked()
        {
            _hutBuildSystem?.TryStartBuild();
        }

        private void RefreshAll()
        {
            RefreshWoodLabel();
            RefreshFireStatus();
            RefreshColdStatus();
            RefreshButtonAvailability();
            RefreshHutProgress();
            RefreshCharacterList();
        }

        private void RefreshWoodLabel()
        {
            if (_woodLabel == null || _resourceManager == null) return;
            int wood = _resourceManager.GetAmount(MvpResourceIds.Wood);
            _woodLabel.text = $"木材：{wood}";
        }

        private void RefreshFireStatus()
        {
            if (_fireStatusLabel == null || _fireSystem == null) return;
            if (_fireSystem.IsLit)
            {
                _fireStatusLabel.text = $"火堆：{_fireSystem.RemainingSeconds:F0} 秒";
            }
            else
            {
                _fireStatusLabel.text = "火堆：熄滅";
            }
        }

        private void RefreshColdStatus()
        {
            if (_coldStatusRoot == null || _coldStatus == null) return;
            _coldStatusRoot.SetActive(_coldStatus.IsCold);
        }

        private void RefreshHutProgress()
        {
            if (_hutBuildProgress == null || _hutBuildSystem == null) return;
            _hutBuildProgress.gameObject.SetActive(_hutBuildSystem.IsBuilding);
            if (_hutBuildSystem.IsBuilding && _hutBuildSystem.TotalSeconds > 0f)
            {
                _hutBuildProgress.value = Mathf.Clamp01(_hutBuildSystem.ElapsedSeconds / _hutBuildSystem.TotalSeconds);
            }
        }

        private void RefreshButtonAvailability()
        {
            if (_searchButton != null && _searchSystem != null)
            {
                _searchButton.interactable = _searchSystem.CanSearch;
            }
            if (_lightFireButton != null && _fireSystem != null)
            {
                _lightFireButton.gameObject.SetActive(_fireSystem.IsUnlocked && !_fireSystem.IsLit);
            }
            if (_extendFireButton != null && _fireSystem != null)
            {
                _extendFireButton.gameObject.SetActive(_fireSystem.IsLit);
            }
            if (_buildHutButton != null && _hutBuildSystem != null)
            {
                _buildHutButton.gameObject.SetActive(_hutBuildSystem.IsUnlocked && !_hutBuildSystem.IsBuilding);
            }
        }

        private void RefreshCharacterList()
        {
            // MVP：角色清單只在 NpcArrived 時新增，這裡做初始檢查（場景切換返回時）。
            if (_npcArrivalManager == null) return;
            IReadOnlyList<MvpPlaceholderCharacterData> arrived = _npcArrivalManager.ArrivedCharacters;
            foreach (MvpPlaceholderCharacterData data in arrived)
            {
                if (!_itemViews.ContainsKey(data.characterId))
                {
                    SpawnCharacterItem(data);
                }
            }
        }

        private void SpawnCharacterItem(MvpPlaceholderCharacterData data)
        {
            if (_characterListContainer == null || _characterListItemPrefab == null) return;
            if (_itemViews.ContainsKey(data.characterId)) return;

            MvpCharacterListItemView item = Instantiate(_characterListItemPrefab, _characterListContainer);
            item.gameObject.SetActive(true);
            int aff = _affinityManager != null ? _affinityManager.GetAffinity(data.characterId) : 0;
            item.Bind(data.characterId, data.displayName, aff, HandleCharacterClicked);
            item.SetRedDotVisible(_initiativeManager != null && _initiativeManager.IsReady(data.characterId));

            _itemViews[data.characterId] = item;
        }

        private void HandleCharacterClicked(string characterId)
        {
            _onCharacterClicked?.Invoke(characterId);
        }

        // ===== 事件處理 =====

        private void OnResourceChanged(MvpResourceChangedEvent e)
        {
            RefreshWoodLabel();
            RefreshButtonAvailability();
        }

        private void OnFireStateChanged(MvpFireStateChangedEvent e)
        {
            RefreshFireStatus();
            RefreshButtonAvailability();
        }

        private void OnFireRemainingChanged(MvpFireRemainingChangedEvent e)
        {
            RefreshFireStatus();
        }

        private void OnFireExtended(MvpFireExtendedEvent e)
        {
            RefreshFireStatus();
        }

        private void OnColdStateChanged(MvpColdStateChangedEvent e)
        {
            RefreshColdStatus();
        }

        private void OnHutBuildStarted(MvpHutBuildStartedEvent e)
        {
            RefreshButtonAvailability();
            RefreshHutProgress();
        }

        private void OnHutBuildProgress(MvpHutBuildProgressEvent e)
        {
            RefreshHutProgress();
        }

        private void OnHutBuilt(MvpHutBuiltEvent e)
        {
            RefreshButtonAvailability();
            RefreshHutProgress();
            if (_feedbackLabel != null)
            {
                _feedbackLabel.text = "小屋蓋好了，人口上限提升。";
            }
        }

        private void OnNpcArrived(MvpNpcArrivedEvent e)
        {
            MvpPlaceholderCharacterData data = new MvpPlaceholderCharacterData
            {
                characterId = e.CharacterId,
                displayName = e.DisplayName
            };
            SpawnCharacterItem(data);
        }

        private void OnSearchCompleted(MvpSearchCompletedEvent e)
        {
            if (_feedbackLabel != null)
            {
                _feedbackLabel.text = e.FeedbackLine;
            }
        }

        private void OnNpcInitiativeReady(MvpNpcInitiativeReadyEvent e)
        {
            if (_itemViews.TryGetValue(e.CharacterId, out MvpCharacterListItemView item))
            {
                item.SetRedDotVisible(true);
            }
        }

        private void OnNpcInitiativeConsumed(MvpNpcInitiativeConsumedEvent e)
        {
            if (_itemViews.TryGetValue(e.CharacterId, out MvpCharacterListItemView item))
            {
                item.SetRedDotVisible(false);
            }
        }

        private void OnAffinityChanged(AffinityChangedEvent e)
        {
            if (_itemViews.TryGetValue(e.CharacterId, out MvpCharacterListItemView item))
            {
                item.SetAffinity(e.NewValue);
            }
        }
    }
}
