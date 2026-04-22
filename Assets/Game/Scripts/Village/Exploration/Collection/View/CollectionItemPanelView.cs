// CollectionItemPanelView — 物品箱 UI（雙欄佈局）。
// 採集完成後（進入 Unlocking 階段）顯示物品箱面板。
// 使用 ScreenSpace Overlay UGUI 動態建立，不需要 Prefab。
//
// 面板結構：
//   - 左側：物品箱 6 格（2x3 grid）
//     - 未解鎖的顯示計時
//     - 已解鎖的顯示物品+拾取按鈕
//     - 空格顯示為可存放
//     - 玩家存放的物品顯示+取回按鈕
//   - 右側：背包內容
//     - 每個物品顯示名稱+數量+存放按鈕
//   - 底部：關閉按鈕

using ProjectDR.Village.Exploration.Core;
using ProjectDR.Village.Backpack;
using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectDR.Village.Exploration.Collection
{
    /// <summary>
    /// 物品箱 UI View（ScreenSpace Overlay UGUI）。
    /// 在 Unlocking 階段顯示，支援雙向操作。
    /// </summary>
    public class CollectionItemPanelView : MonoBehaviour
    {
        private CollectionManager _collectionManager;
        private BackpackManager _backpack;

        // UGUI 根 Canvas
        private Canvas _canvas;
        private GameObject _panelRoot;

        // 左側物品箱欄位列表（固定 6 格）
        private readonly List<BoxSlotUI> _boxSlots = new List<BoxSlotUI>();

        // 右側背包欄位列表
        private readonly List<BackpackSlotUI> _backpackSlots = new List<BackpackSlotUI>();

        // 背包狀態文字
        private TextMeshProUGUI _backpackStatusText;

        // 右側背包容器（用於重建背包列表）
        private RectTransform _backpackContainer;

        private bool _isVisible;

        private Action<GatheringCompletedEvent> _onGatheringCompleted;
        private Action<ItemSlotUnlockedEvent> _onSlotUnlocked;
        private Action<ItemPickedUpEvent> _onItemPickedUp;
        private Action<CollectionPanelClosedEvent> _onPanelClosed;
        private Action<ItemStoredInBoxEvent> _onItemStored;
        private Action<ItemRemovedFromBoxEvent> _onItemRemoved;

        // ----------------------------------------------------------------
        // 內部類別：物品箱格子 UI
        // ----------------------------------------------------------------

        private class BoxSlotUI
        {
            public GameObject Root;
            public TextMeshProUGUI ItemLabel;
            public TextMeshProUGUI StatusLabel;
            public Image FillBar;
            public Button ActionButton;
            public int SlotIndex;
        }

        // ----------------------------------------------------------------
        // 內部類別：背包格子 UI
        // ----------------------------------------------------------------

        private class BackpackSlotUI
        {
            public GameObject Root;
            public TextMeshProUGUI ItemLabel;
            public Button StoreButton;
            public int BackpackSlotIndex;
        }

        // ----------------------------------------------------------------
        // 顏色常數
        // ----------------------------------------------------------------

        private static readonly Color ColorLocked = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color ColorUnlocking = new Color(0.8f, 0.7f, 0.2f);
        private static readonly Color ColorUnlocked = new Color(0.3f, 0.9f, 0.3f);
        private static readonly Color ColorTaken = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        private static readonly Color ColorEmpty = new Color(0.25f, 0.25f, 0.3f, 0.7f);
        private static readonly Color ColorPlayerStored = new Color(0.3f, 0.5f, 0.9f);
        private static readonly Color ColorPanelBg = new Color(0.1f, 0.12f, 0.15f, 0.92f);

        // ----------------------------------------------------------------
        // 佈局常數
        // ----------------------------------------------------------------

        private const float PanelWidth = 800f;
        private const float PanelHeight = 480f;
        private const float BoxGridColumns = 2f;
        private const float BoxGridRows = 3f;
        private const float BoxSlotWidth = 170f;
        private const float BoxSlotHeight = 70f;
        private const float BoxSlotSpacing = 6f;
        private const float BackpackRowHeight = 44f;
        private const float BackpackRowSpacing = 4f;

        /// <summary>
        /// 初始化物品欄 UI，建立 Canvas 與面板結構，訂閱事件。
        /// </summary>
        public void Initialize(CollectionManager collectionManager, BackpackManager backpack)
        {
            _collectionManager = collectionManager;
            _backpack = backpack;

            BuildCanvas();
            BuildPanel();
            HidePanel();

            _onGatheringCompleted = (e) => ShowPanel();
            _onSlotUnlocked = (e) => RefreshAll();
            _onItemPickedUp = (e) => RefreshAll();
            _onPanelClosed = (e) => HidePanel();
            _onItemStored = (e) => RefreshAll();
            _onItemRemoved = (e) => RefreshAll();

            EventBus.Subscribe<GatheringCompletedEvent>(_onGatheringCompleted);
            EventBus.Subscribe<ItemSlotUnlockedEvent>(_onSlotUnlocked);
            EventBus.Subscribe<ItemPickedUpEvent>(_onItemPickedUp);
            EventBus.Subscribe<CollectionPanelClosedEvent>(_onPanelClosed);
            EventBus.Subscribe<ItemStoredInBoxEvent>(_onItemStored);
            EventBus.Subscribe<ItemRemovedFromBoxEvent>(_onItemRemoved);
        }

        // ----------------------------------------------------------------
        // Update：每幀刷新解鎖進度條（Unlocking 欄位）
        // ----------------------------------------------------------------

        private void Update()
        {
            if (!_isVisible) return;
            if (_collectionManager == null) return;

            CollectiblePointState state = _collectionManager.ActivePointState;
            if (state == null || state.Phase != GatheringPhase.Unlocking) return;

            for (int i = 0; i < _boxSlots.Count; i++)
            {
                BoxSlotUI slot = _boxSlots[i];
                if (slot.SlotIndex >= state.SlotCount) continue;

                CollectibleSlotState slotState = state.GetSlotState(slot.SlotIndex);
                if (slotState == CollectibleSlotState.Unlocking)
                {
                    float progress = state.GetSlotUnlockProgress(slot.SlotIndex);
                    slot.FillBar.fillAmount = progress;
                }
            }
        }

        // ----------------------------------------------------------------
        // 顯示 / 隱藏
        // ----------------------------------------------------------------

        private void ShowPanel()
        {
            CollectiblePointState state = _collectionManager?.ActivePointState;
            if (state == null) return;

            RebuildBoxSlots(state);
            RebuildBackpackSlots();
            RefreshAll();
            RefreshBackpackStatus();

            _panelRoot.SetActive(true);
            _isVisible = true;
        }

        private void HidePanel()
        {
            _isVisible = false;
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        // ----------------------------------------------------------------
        // 全面刷新
        // ----------------------------------------------------------------

        private void RefreshAll()
        {
            RefreshBoxSlots();
            RebuildBackpackSlots();
            RefreshBackpackStatus();
        }

        private void RefreshBoxSlots()
        {
            CollectiblePointState state = _collectionManager?.ActivePointState;
            if (state == null) return;

            BoxSlotInfo[] allSlots = state.GetAllSlots();

            for (int i = 0; i < _boxSlots.Count; i++)
            {
                BoxSlotUI slotUI = _boxSlots[i];
                if (slotUI.SlotIndex >= allSlots.Length) continue;

                BoxSlotInfo info = allSlots[slotUI.SlotIndex];
                CollectibleSlotState slotState = state.GetSlotState(slotUI.SlotIndex);
                float progress = state.GetSlotUnlockProgress(slotUI.SlotIndex);

                ApplyBoxSlotVisual(slotUI, slotState, info, progress);
            }
        }

        private void ApplyBoxSlotVisual(BoxSlotUI slotUI, CollectibleSlotState slotState,
            BoxSlotInfo info, float progress)
        {
            Image bgImage = slotUI.Root.GetComponent<Image>();

            switch (slotState)
            {
                case CollectibleSlotState.Locked:
                    bgImage.color = ColorLocked;
                    slotUI.ItemLabel.text = "???";
                    slotUI.StatusLabel.text = "鎖定";
                    slotUI.FillBar.fillAmount = 0f;
                    slotUI.FillBar.color = new Color(0.5f, 0.5f, 0.5f);
                    slotUI.ActionButton.interactable = false;
                    SetButtonLabel(slotUI.ActionButton, "等待");
                    break;

                case CollectibleSlotState.Unlocking:
                    bgImage.color = ColorUnlocking;
                    slotUI.ItemLabel.text = "???";
                    slotUI.StatusLabel.text = "解鎖中...";
                    slotUI.FillBar.fillAmount = progress;
                    slotUI.FillBar.color = new Color(1f, 0.85f, 0.2f);
                    slotUI.ActionButton.interactable = false;
                    SetButtonLabel(slotUI.ActionButton, "等待");
                    break;

                case CollectibleSlotState.Unlocked:
                    bgImage.color = ColorUnlocked;
                    slotUI.ItemLabel.text = string.Format("{0}  x{1}", info.ItemId, info.Quantity);
                    slotUI.StatusLabel.text = "可拾取";
                    slotUI.FillBar.fillAmount = 1f;
                    slotUI.FillBar.color = new Color(0.3f, 0.9f, 0.3f);
                    slotUI.ActionButton.interactable = true;
                    SetButtonLabel(slotUI.ActionButton, "拾取");
                    break;

                case CollectibleSlotState.Taken:
                    bgImage.color = ColorTaken;
                    slotUI.ItemLabel.text = string.Format("{0}  x{1}", info.ItemId, info.Quantity);
                    slotUI.StatusLabel.text = "已拾取";
                    slotUI.FillBar.fillAmount = 1f;
                    slotUI.FillBar.color = new Color(0.4f, 0.4f, 0.4f);
                    slotUI.ActionButton.interactable = false;
                    SetButtonLabel(slotUI.ActionButton, "完成");
                    break;

                case CollectibleSlotState.Empty:
                    bgImage.color = ColorEmpty;
                    slotUI.ItemLabel.text = "空格";
                    slotUI.StatusLabel.text = "可存放";
                    slotUI.FillBar.fillAmount = 0f;
                    slotUI.FillBar.color = new Color(0.3f, 0.3f, 0.3f);
                    slotUI.ActionButton.interactable = false;
                    SetButtonLabel(slotUI.ActionButton, "-");
                    break;

                case CollectibleSlotState.PlayerStored:
                    bgImage.color = ColorPlayerStored;
                    slotUI.ItemLabel.text = string.Format("{0}  x{1}", info.ItemId, info.Quantity);
                    slotUI.StatusLabel.text = "已存放";
                    slotUI.FillBar.fillAmount = 1f;
                    slotUI.FillBar.color = new Color(0.3f, 0.5f, 0.9f);
                    slotUI.ActionButton.interactable = true;
                    SetButtonLabel(slotUI.ActionButton, "取回");
                    break;
            }
        }

        private void RefreshBackpackStatus()
        {
            if (_backpack == null || _backpackStatusText == null) return;

            IReadOnlyList<BackpackSlot> slots = _backpack.GetSlots();
            int used = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty) used++;
            }
            _backpackStatusText.text = string.Format("背包：{0} / {1}", used, _backpack.MaxSlots);
        }

        // ----------------------------------------------------------------
        // 左側：物品箱 6 格重建（2x3 grid）
        // ----------------------------------------------------------------

        private void RebuildBoxSlots(CollectiblePointState state)
        {
            // 清除舊欄位
            for (int i = 0; i < _boxSlots.Count; i++)
            {
                if (_boxSlots[i].Root != null)
                    Destroy(_boxSlots[i].Root);
            }
            _boxSlots.Clear();

            // Container 的寬高由 anchor + GetOrCreateContainer 的 sizeDelta 決定，不可覆寫
            RectTransform boxContainer = GetOrCreateContainer("BoxContainer", true);

            for (int i = 0; i < CollectiblePointState.MaxSlots; i++)
            {
                int col = i % (int)BoxGridColumns;
                int row = i / (int)BoxGridColumns;

                BoxSlotUI slotUI = CreateBoxSlot(boxContainer, i, col, row);
                _boxSlots.Add(slotUI);
            }
        }

        private BoxSlotUI CreateBoxSlot(RectTransform parent, int slotIndex, int col, int row)
        {
            GameObject slotObj = new GameObject($"BoxSlot_{slotIndex}");
            slotObj.transform.SetParent(parent, false);

            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(0f, 1f);
            slotRect.pivot = new Vector2(0f, 1f);
            float x = col * (BoxSlotWidth + BoxSlotSpacing);
            float y = -(row * (BoxSlotHeight + BoxSlotSpacing));
            slotRect.anchoredPosition = new Vector2(x, y);
            slotRect.sizeDelta = new Vector2(BoxSlotWidth, BoxSlotHeight);

            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = ColorLocked;

            // 物品名稱
            TextMeshProUGUI itemLabel = CreateLabel(slotObj.transform,
                "???", Vector2.zero, 12f, TextAlignmentOptions.Left);
            RectTransform itemRect = itemLabel.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(0.6f, 1f);
            itemRect.anchoredPosition = new Vector2(6f, 0f);
            itemRect.sizeDelta = new Vector2(-12f, 0f);

            // 狀態文字
            TextMeshProUGUI statusLabel = CreateLabel(slotObj.transform,
                "鎖定", Vector2.zero, 10f, TextAlignmentOptions.Left);
            RectTransform statusRect = statusLabel.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(0.6f, 0.5f);
            statusRect.anchoredPosition = new Vector2(6f, 0f);
            statusRect.sizeDelta = new Vector2(-12f, 0f);

            // 進度條背景
            GameObject barBgObj = new GameObject("BarBg");
            barBgObj.transform.SetParent(slotObj.transform, false);
            RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.6f, 0.15f);
            barBgRect.anchorMax = new Vector2(0.75f, 0.85f);
            barBgRect.offsetMin = Vector2.zero;
            barBgRect.offsetMax = Vector2.zero;
            Image barBgImage = barBgObj.AddComponent<Image>();
            barBgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            // 進度條填充
            GameObject barFillObj = new GameObject("BarFill");
            barFillObj.transform.SetParent(barBgObj.transform, false);
            RectTransform barFillRect = barFillObj.AddComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;
            Image fillImage = barFillObj.AddComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;
            fillImage.color = new Color(0.5f, 0.5f, 0.5f);

            // 操作按鈕
            Button actionButton = CreateActionButton(slotObj.transform, slotIndex);

            return new BoxSlotUI
            {
                Root = slotObj,
                ItemLabel = itemLabel,
                StatusLabel = statusLabel,
                FillBar = fillImage,
                ActionButton = actionButton,
                SlotIndex = slotIndex
            };
        }

        private Button CreateActionButton(Transform parent, int slotIndex)
        {
            GameObject btnObj = new GameObject("ActionButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.77f, 0.1f);
            btnRect.anchorMax = new Vector2(1f, 0.9f);
            btnRect.offsetMin = new Vector2(2f, 0f);
            btnRect.offsetMax = new Vector2(-4f, 0f);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.5f, 0.2f);

            Button btn = btnObj.AddComponent<Button>();

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI btnText = labelObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "等待";
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontSize = 11f;
            btnText.color = Color.white;

            int capturedIndex = slotIndex;
            btn.onClick.AddListener(() => OnBoxSlotActionClicked(capturedIndex));
            btn.interactable = false;

            return btn;
        }

        private void OnBoxSlotActionClicked(int slotIndex)
        {
            if (_collectionManager == null) return;
            // TryPickItem handles both Unlocked (map item) and PlayerStored (stored item)
            _collectionManager.TryPickItem(slotIndex);
        }

        // ----------------------------------------------------------------
        // 右側：背包內容列表重建
        // ----------------------------------------------------------------

        private void RebuildBackpackSlots()
        {
            // 清除舊欄位
            for (int i = 0; i < _backpackSlots.Count; i++)
            {
                if (_backpackSlots[i].Root != null)
                    Destroy(_backpackSlots[i].Root);
            }
            _backpackSlots.Clear();

            if (_backpack == null || _backpackContainer == null) return;

            IReadOnlyList<BackpackSlot> slots = _backpack.GetSlots();
            int visibleCount = 0;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty) continue;

                BackpackSlotUI slotUI = CreateBackpackRow(_backpackContainer, i, slots[i], visibleCount);
                _backpackSlots.Add(slotUI);
                visibleCount++;
            }

            // 調整容器高度
            float containerHeight = visibleCount * (BackpackRowHeight + BackpackRowSpacing);
            _backpackContainer.sizeDelta = new Vector2(_backpackContainer.sizeDelta.x, containerHeight);
        }

        private BackpackSlotUI CreateBackpackRow(RectTransform parent, int backpackIndex,
            BackpackSlot slot, int visualIndex)
        {
            GameObject rowObj = new GameObject($"BackpackRow_{backpackIndex}");
            rowObj.transform.SetParent(parent, false);

            RectTransform rowRect = rowObj.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -(visualIndex * (BackpackRowHeight + BackpackRowSpacing)));
            rowRect.sizeDelta = new Vector2(0f, BackpackRowHeight);

            Image rowBg = rowObj.AddComponent<Image>();
            rowBg.color = new Color(0.2f, 0.22f, 0.25f, 0.8f);

            // 物品名稱+數量
            TextMeshProUGUI itemLabel = CreateLabel(rowObj.transform,
                string.Format("{0}  x{1}", slot.ItemId, slot.Quantity),
                Vector2.zero, 12f, TextAlignmentOptions.Left);
            RectTransform itemRect = itemLabel.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0f);
            itemRect.anchorMax = new Vector2(0.65f, 1f);
            itemRect.anchoredPosition = new Vector2(8f, 0f);
            itemRect.sizeDelta = new Vector2(-16f, 0f);

            // 存放按鈕
            GameObject btnObj = new GameObject("StoreButton");
            btnObj.transform.SetParent(rowObj.transform, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.67f, 0.1f);
            btnRect.anchorMax = new Vector2(1f, 0.9f);
            btnRect.offsetMin = new Vector2(2f, 0f);
            btnRect.offsetMax = new Vector2(-4f, 0f);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.3f, 0.6f);

            Button storeBtn = btnObj.AddComponent<Button>();

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI btnText = labelObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "存放";
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontSize = 11f;
            btnText.color = Color.white;

            string capturedItemId = slot.ItemId;
            int capturedQuantity = slot.Quantity;
            storeBtn.onClick.AddListener(() => OnStoreButtonClicked(capturedItemId, capturedQuantity));

            // 若物品箱沒有空格，停用按鈕
            CollectiblePointState state = _collectionManager?.ActivePointState;
            storeBtn.interactable = state != null && state.FindFirstEmptySlot() >= 0;

            return new BackpackSlotUI
            {
                Root = rowObj,
                ItemLabel = itemLabel,
                StoreButton = storeBtn,
                BackpackSlotIndex = backpackIndex
            };
        }

        private void OnStoreButtonClicked(string itemId, int quantity)
        {
            if (_collectionManager == null) return;
            _collectionManager.TransferToBox(itemId, quantity);
        }

        // ----------------------------------------------------------------
        // Canvas 與面板建立
        // ----------------------------------------------------------------

        private void BuildCanvas()
        {
            GameObject canvasObj = new GameObject("CollectionItemPanelCanvas");
            canvasObj.transform.SetParent(transform);

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Ensure EventSystem exists for UGUI button interaction
            if (EventSystem.current == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
        }

        private void BuildPanel()
        {
            _panelRoot = new GameObject("CollectionItemPanel");
            _panelRoot.transform.SetParent(_canvas.transform, false);

            RectTransform panelRect = _panelRoot.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            Image panelBg = _panelRoot.AddComponent<Image>();
            panelBg.color = ColorPanelBg;

            // 左側標題「物品箱」
            TextMeshProUGUI boxTitle = CreateLabel(_panelRoot.transform,
                "物品箱", Vector2.zero, 16f, TextAlignmentOptions.Center);
            RectTransform boxTitleRect = boxTitle.GetComponent<RectTransform>();
            boxTitleRect.anchorMin = new Vector2(0f, 1f);
            boxTitleRect.anchorMax = new Vector2(0.5f, 1f);
            boxTitleRect.pivot = new Vector2(0.5f, 1f);
            boxTitleRect.anchoredPosition = new Vector2(0f, -8f);
            boxTitleRect.sizeDelta = new Vector2(0f, 36f);
            boxTitle.color = new Color(0.9f, 0.8f, 0.3f);

            // 右側標題「背包」
            TextMeshProUGUI bpTitle = CreateLabel(_panelRoot.transform,
                "背包", Vector2.zero, 16f, TextAlignmentOptions.Center);
            RectTransform bpTitleRect = bpTitle.GetComponent<RectTransform>();
            bpTitleRect.anchorMin = new Vector2(0.5f, 1f);
            bpTitleRect.anchorMax = new Vector2(1f, 1f);
            bpTitleRect.pivot = new Vector2(0.5f, 1f);
            bpTitleRect.anchoredPosition = new Vector2(0f, -8f);
            bpTitleRect.sizeDelta = new Vector2(0f, 36f);
            bpTitle.color = new Color(0.7f, 0.85f, 1f);

            // 左側物品箱容器（已在 RebuildBoxSlots 裡懶建立）
            // 右側背包容器
            _backpackContainer = GetOrCreateContainer("BackpackContainer", false);

            // 背包狀態文字
            _backpackStatusText = CreateLabel(_panelRoot.transform,
                "背包：0 / 10", Vector2.zero, 11f, TextAlignmentOptions.Right);
            RectTransform backpackStatusRect = _backpackStatusText.GetComponent<RectTransform>();
            backpackStatusRect.anchorMin = new Vector2(0.5f, 0f);
            backpackStatusRect.anchorMax = new Vector2(1f, 0f);
            backpackStatusRect.pivot = new Vector2(1f, 0f);
            backpackStatusRect.anchoredPosition = new Vector2(-8f, 56f);
            backpackStatusRect.sizeDelta = new Vector2(-16f, 24f);
            _backpackStatusText.color = new Color(0.7f, 0.7f, 0.7f);

            // 關閉按鈕
            CreateCloseButton(_panelRoot.transform);
        }

        private RectTransform GetOrCreateContainer(string name, bool isLeft)
        {
            Transform existing = _panelRoot.transform.Find(name);
            if (existing != null)
                return existing.GetComponent<RectTransform>();

            GameObject container = new GameObject(name);
            container.transform.SetParent(_panelRoot.transform, false);
            RectTransform rt = container.AddComponent<RectTransform>();

            if (isLeft)
            {
                // 左半部分，物品箱
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -48f);
                rt.sizeDelta = new Vector2(-20f, 320f);
            }
            else
            {
                // 右半部分，背包
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -48f);
                rt.sizeDelta = new Vector2(-20f, 320f);
            }
            return rt;
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.3f, 0f);
            btnRect.anchorMax = new Vector2(0.7f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0f, 8f);
            btnRect.sizeDelta = new Vector2(0f, 44f);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.6f, 0.2f, 0.2f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(OnCloseButtonClicked);

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI btnText = labelObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "關閉 [Esc]";
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontSize = 14f;
            btnText.color = Color.white;
        }

        private void OnCloseButtonClicked()
        {
            _collectionManager?.CloseItemPanel();
        }

        // ----------------------------------------------------------------
        // 工具方法
        // ----------------------------------------------------------------

        private static TextMeshProUGUI CreateLabel(Transform parent, string text,
            Vector2 anchoredPos, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject("Label");
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        private static void SetButtonLabel(Button button, string text)
        {
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = text;
        }

        private void OnDestroy()
        {
            if (_onGatheringCompleted != null)
                EventBus.Unsubscribe<GatheringCompletedEvent>(_onGatheringCompleted);
            if (_onSlotUnlocked != null)
                EventBus.Unsubscribe<ItemSlotUnlockedEvent>(_onSlotUnlocked);
            if (_onItemPickedUp != null)
                EventBus.Unsubscribe<ItemPickedUpEvent>(_onItemPickedUp);
            if (_onPanelClosed != null)
                EventBus.Unsubscribe<CollectionPanelClosedEvent>(_onPanelClosed);
            if (_onItemStored != null)
                EventBus.Unsubscribe<ItemStoredInBoxEvent>(_onItemStored);
            if (_onItemRemoved != null)
                EventBus.Unsubscribe<ItemRemovedFromBoxEvent>(_onItemRemoved);
        }
    }
}
