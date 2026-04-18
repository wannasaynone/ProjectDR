// CraftWorkbenchView — 格子式工作台主 View。
// 顯示指定角色的所有工作台 slots，管理 CraftSlotWidget 生命週期。
//
// 職責：
// 1. 初始化時從 CommissionManager.GetSlots(characterId) 取得當前 slot 狀態
// 2. 監聽 CommissionTickEvent / CommissionCompletedEvent 更新對應 CraftSlotWidget
// 3. Idle Slot 點擊 → 開啟 CraftItemSelectorView overlay
// 4. 選擇物品後呼叫 CommissionManager.StartCommission
// 5. Completed Slot 領取 → 呼叫 CommissionManager.ClaimCommission
//
// B12 整合點：
// - 開啟時若角色有 InProgress/Completed slot → 外部（CharacterInteractionPresenter）
//   已透過 CharacterInteractionView.SetState(CommissionInProgress/CommissionReady) 處理
// - 本 View 僅負責工作台介面，不控制 CharacterInteractionView 狀態
//
// 依 GDD commission-system.md v1.1 § 三、character-interaction.md v2.2 § 六

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 格子式工作台 View。
    /// 由 CharacterInteractionView 的 overlay container 中動態 Instantiate。
    /// </summary>
    public class CraftWorkbenchView : ViewBase
    {
        // ===== 佈局數值驗算（ui-layout-validation）=====
        // Canvas: 3840x2160, matchWidthOrHeight=0.5
        // 安全區域 X: ±1700, Y: ±950
        //
        // PnlWorkbench  cx=640  cy=0   w=1280  h=1920  L=0    R=1280  T=960   B=-960
        // TxtTitle      cx=640  cy=840 w=960   h=64    L=160  R=1120  T=872   B=808  (在PnlWorkbench內)
        // PnlSlots      cx=640  cy=200 w=1120  h=800   L=80   R=1200  T=600   B=-200 (在PnlWorkbench內)
        // BtnReturn     cx=640  cy=-840 w=280  h=72    L=500  R=780   T=-804  B=-876 (在PnlWorkbench內)
        //
        // Slot間距驗算：2格並排，每格w=520，間距=(1120-2*520)/2=40px（外側各20px留白）
        // 相鄰元素邊界不重疊: L_slot1=0, R_slot1=520, L_slot2=600, R_slot2=1120 ✓

        [Header("根面板")]
        [SerializeField] private Image _panelBackground;

        [Header("標題")]
        [SerializeField] private TMP_Text _txtTitle;

        [Header("Slot 容器（水平排列）")]
        [SerializeField] private Transform _slotsContainer;

        [Header("Slot Widget Prefab（動態生成）")]
        [SerializeField] private CraftSlotWidget _slotWidgetPrefab;

        [Header("物品選擇 Overlay Prefab")]
        [SerializeField] private CraftItemSelectorView _itemSelectorPrefab;

        [Header("返回按鈕")]
        [SerializeField] private Button _btnReturn;

        // 注入相依
        private CommissionManager _commissionManager;
        private CommissionRecipesConfig _recipesConfig;
        private BackpackManager _backpackManager;
        private StorageManager _storageManager;

        private string _characterId;
        private Action _onReturnCallback;

        // 動態生成的 slot widgets
        private readonly List<CraftSlotWidget> _slotWidgets = new List<CraftSlotWidget>();

        // 當前開啟的物品選擇器
        private CraftItemSelectorView _currentSelector;

        // Design Tokens
        private static readonly Color ColorBgPanel = new Color(0.08f, 0.08f, 0.13f, 0.97f);

        // ===== 公開 API =====

        /// <summary>注入相依（由 VillageEntryPoint 呼叫）。</summary>
        public void Initialize(
            CommissionManager commissionManager,
            CommissionRecipesConfig recipesConfig,
            BackpackManager backpackManager,
            StorageManager storageManager)
        {
            _commissionManager = commissionManager;
            _recipesConfig = recipesConfig;
            _backpackManager = backpackManager;
            _storageManager = storageManager;
        }

        /// <summary>設定目標角色（顯示前呼叫）。</summary>
        public void SetCharacter(string characterId)
        {
            _characterId = characterId;
        }

        /// <summary>設定返回按鈕回呼（由 CharacterInteractionView 提供）。</summary>
        public void SetReturnAction(Action onReturn)
        {
            _onReturnCallback = onReturn;
        }

        // ===== ViewBase 生命週期 =====

        protected override void OnShow()
        {
            if (_panelBackground != null)
                _panelBackground.color = ColorBgPanel;

            // 設定標題
            if (_txtTitle != null)
                _txtTitle.text = GetWorkbenchTitle(_characterId);

            // 返回按鈕
            if (_btnReturn != null)
                _btnReturn.onClick.AddListener(HandleReturn);

            // 建立 Slot Widgets
            BuildSlotWidgets();

            // 訂閱事件
            EventBus.Subscribe<CommissionTickEvent>(OnCommissionTick);
            EventBus.Subscribe<CommissionCompletedEvent>(OnCommissionCompleted);
            EventBus.Subscribe<CommissionClaimedEvent>(OnCommissionClaimed);
        }

        protected override void OnHide()
        {
            if (_btnReturn != null)
                _btnReturn.onClick.RemoveListener(HandleReturn);

            // 關閉物品選擇器
            CloseItemSelector();

            // 清除 Slot Widgets
            ClearSlotWidgets();

            // 取消訂閱
            EventBus.Unsubscribe<CommissionTickEvent>(OnCommissionTick);
            EventBus.Unsubscribe<CommissionCompletedEvent>(OnCommissionCompleted);
            EventBus.Unsubscribe<CommissionClaimedEvent>(OnCommissionClaimed);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CommissionTickEvent>(OnCommissionTick);
            EventBus.Unsubscribe<CommissionCompletedEvent>(OnCommissionCompleted);
            EventBus.Unsubscribe<CommissionClaimedEvent>(OnCommissionClaimed);
        }

        // ===== 私有：Slot Widget 管理 =====

        private void BuildSlotWidgets()
        {
            ClearSlotWidgets();
            if (_slotsContainer == null || _slotWidgetPrefab == null) return;
            if (_commissionManager == null || string.IsNullOrEmpty(_characterId)) return;

            IReadOnlyList<CommissionSlotInfo> slots = _commissionManager.GetSlots(_characterId);
            for (int i = 0; i < slots.Count; i++)
            {
                int capturedIndex = i;
                CraftSlotWidget widget = Instantiate(_slotWidgetPrefab, _slotsContainer);
                widget.gameObject.SetActive(true);
                widget.Setup(_characterId, i, OnIdleSlotClicked, OnClaimClicked);
                widget.Refresh(slots[i]);
                _slotWidgets.Add(widget);
            }
        }

        private void ClearSlotWidgets()
        {
            foreach (CraftSlotWidget widget in _slotWidgets)
            {
                if (widget != null) Destroy(widget.gameObject);
            }
            _slotWidgets.Clear();
        }

        // ===== 私有：事件處理 =====

        private void OnCommissionTick(CommissionTickEvent e)
        {
            if (e.CharacterId != _characterId) return;
            if (e.SlotIndex < 0 || e.SlotIndex >= _slotWidgets.Count) return;
            _slotWidgets[e.SlotIndex].UpdateCountdown(e.RemainingSeconds);
        }

        private void OnCommissionCompleted(CommissionCompletedEvent e)
        {
            if (e.CharacterId != _characterId) return;
            if (e.SlotIndex < 0 || e.SlotIndex >= _slotWidgets.Count) return;
            CommissionSlotInfo info = _commissionManager.GetSlot(_characterId, e.SlotIndex);
            _slotWidgets[e.SlotIndex].Refresh(info);
        }

        private void OnCommissionClaimed(CommissionClaimedEvent e)
        {
            if (e.CharacterId != _characterId) return;
            if (e.SlotIndex < 0 || e.SlotIndex >= _slotWidgets.Count) return;
            CommissionSlotInfo info = _commissionManager.GetSlot(_characterId, e.SlotIndex);
            _slotWidgets[e.SlotIndex].Refresh(info);
        }

        // ===== 私有：Slot 互動 =====

        private void OnIdleSlotClicked(int slotIndex)
        {
            OpenItemSelector(slotIndex);
        }

        private void OnClaimClicked(int slotIndex)
        {
            if (_commissionManager == null) return;
            ClaimCommissionResult result = _commissionManager.ClaimCommission(_characterId, slotIndex);
            if (!result.IsSuccess)
            {
                Debug.LogWarning(string.Format(
                    "[CraftWorkbenchView] ClaimCommission 失敗 slot={0} error={1}",
                    slotIndex, result.Error));
            }
        }

        // ===== 私有：物品選擇器 =====

        private void OpenItemSelector(int slotIndex)
        {
            CloseItemSelector();
            if (_itemSelectorPrefab == null || _slotsContainer == null) return;

            // 在本 View 的父容器中建立 selector（同層 overlay）
            Transform parent = transform;
            _currentSelector = Instantiate(_itemSelectorPrefab, parent);

            _currentSelector.Initialize(_recipesConfig, _backpackManager, _storageManager);
            _currentSelector.Open(
                _characterId,
                slotIndex,
                OnItemSelected,
                CloseItemSelector);
            _currentSelector.Show();
        }

        private void CloseItemSelector()
        {
            if (_currentSelector != null)
            {
                _currentSelector.Hide();
                Destroy(_currentSelector.gameObject);
                _currentSelector = null;
            }
        }

        private void OnItemSelected(string recipeId, int slotIndex)
        {
            CloseItemSelector();
            if (_commissionManager == null) return;

            StartCommissionResult result = _commissionManager.StartCommission(_characterId, recipeId, slotIndex);
            if (result.IsSuccess)
            {
                // 刷新對應 Widget
                if (slotIndex >= 0 && slotIndex < _slotWidgets.Count)
                {
                    CommissionSlotInfo info = _commissionManager.GetSlot(_characterId, slotIndex);
                    _slotWidgets[slotIndex].Refresh(info);
                }
            }
            else
            {
                Debug.LogWarning(string.Format(
                    "[CraftWorkbenchView] StartCommission 失敗 recipe={0} slot={1} error={2}",
                    recipeId, slotIndex, result.Error));
            }
        }

        // ===== 私有：返回 =====

        private void HandleReturn()
        {
            _onReturnCallback?.Invoke();
        }

        // ===== 私有：輔助 =====

        private static string GetWorkbenchTitle(string characterId)
        {
            if (characterId == CharacterIds.FarmGirl) return "耕種";
            if (characterId == CharacterIds.Witch) return "煉製";
            if (characterId == CharacterIds.Guard) return "探索周圍";
            return "工作台";
        }
    }
}
