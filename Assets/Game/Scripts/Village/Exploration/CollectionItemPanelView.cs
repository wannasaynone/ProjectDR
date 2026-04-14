// CollectionItemPanelView — 採集物品欄 UI（第二層計時）。
// 採集完成後（進入 Unlocking 階段）顯示物品欄面板。
// 使用 ScreenSpace Overlay UGUI 動態建立，不需要 Prefab。
//
// 面板結構：
//   - 標題「採集完成！」
//   - 每個物品欄位：物品名稱 / 數量 / 解鎖進度條 / 狀態
//   - 背包狀態（已用/總格子）
//   - 關閉按鈕

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// 採集物品欄 UI View（ScreenSpace Overlay UGUI）。
    /// 在 Unlocking 階段顯示，玩家可點擊 Unlocked 的欄位拾取物品。
    /// </summary>
    public class CollectionItemPanelView : MonoBehaviour
    {
        private CollectionManager _collectionManager;
        private BackpackManager _backpack;

        // UGUI 根 Canvas
        private Canvas _canvas;
        private GameObject _panelRoot;

        // 物品欄位列表（對應 CollectiblePointState 的每個 slot）
        private readonly List<SlotRowUI> _slotRows = new List<SlotRowUI>();

        // 背包狀態文字
        private TextMeshProUGUI _backpackStatusText;

        private bool _isVisible;

        private Action<GatheringCompletedEvent> _onGatheringCompleted;
        private Action<ItemSlotUnlockedEvent> _onSlotUnlocked;
        private Action<ItemPickedUpEvent> _onItemPickedUp;
        private Action<CollectionPanelClosedEvent> _onPanelClosed;

        // ----------------------------------------------------------------
        // 內部類別：單一物品欄位 UI 行
        // ----------------------------------------------------------------

        private class SlotRowUI
        {
            public GameObject Root;
            public TextMeshProUGUI ItemLabel;
            public TextMeshProUGUI StatusLabel;
            public Image FillBar;
            public Button PickButton;
            public int SlotIndex;
        }

        // ----------------------------------------------------------------
        // 顏色常數
        // ----------------------------------------------------------------

        private static readonly Color ColorLocked = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color ColorUnlocking = new Color(0.8f, 0.7f, 0.2f);
        private static readonly Color ColorUnlocked = new Color(0.3f, 0.9f, 0.3f);
        private static readonly Color ColorTaken = new Color(0.35f, 0.35f, 0.35f, 0.6f);

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
            _onSlotUnlocked = (e) => RefreshSlots();
            _onItemPickedUp = (e) => RefreshSlots();
            _onPanelClosed = (e) => HidePanel();

            EventBus.Subscribe<GatheringCompletedEvent>(_onGatheringCompleted);
            EventBus.Subscribe<ItemSlotUnlockedEvent>(_onSlotUnlocked);
            EventBus.Subscribe<ItemPickedUpEvent>(_onItemPickedUp);
            EventBus.Subscribe<CollectionPanelClosedEvent>(_onPanelClosed);
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

            for (int i = 0; i < _slotRows.Count; i++)
            {
                SlotRowUI row = _slotRows[i];
                if (row.SlotIndex >= state.SlotCount) continue;

                CollectibleSlotState slotState = state.GetSlotState(row.SlotIndex);
                if (slotState == CollectibleSlotState.Unlocking)
                {
                    float progress = state.GetSlotUnlockProgress(row.SlotIndex);
                    row.FillBar.fillAmount = progress;
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

            // 重新建立欄位 UI（根據當前採集點的物品數量）
            RebuildSlotRows(state);
            RefreshSlots();
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
        // 欄位刷新
        // ----------------------------------------------------------------

        private void RefreshSlots()
        {
            CollectiblePointState state = _collectionManager?.ActivePointState;
            if (state == null) return;

            for (int i = 0; i < _slotRows.Count; i++)
            {
                SlotRowUI row = _slotRows[i];
                if (row.SlotIndex >= state.SlotCount) continue;

                CollectibleSlotState slotState = state.GetSlotState(row.SlotIndex);
                float progress = state.GetSlotUnlockProgress(row.SlotIndex);

                ApplySlotVisual(row, slotState, progress);
            }

            RefreshBackpackStatus();
        }

        private void ApplySlotVisual(SlotRowUI row, CollectibleSlotState slotState, float progress)
        {
            switch (slotState)
            {
                case CollectibleSlotState.Locked:
                    row.Root.GetComponent<Image>().color = ColorLocked;
                    row.ItemLabel.text = "???";
                    row.StatusLabel.text = "鎖定";
                    row.FillBar.fillAmount = 0f;
                    row.FillBar.color = new Color(0.5f, 0.5f, 0.5f);
                    row.PickButton.interactable = false;
                    SetButtonLabel(row.PickButton, "等待");
                    break;

                case CollectibleSlotState.Unlocking:
                    row.Root.GetComponent<Image>().color = ColorUnlocking;
                    row.ItemLabel.text = "???";
                    row.StatusLabel.text = "解鎖中...";
                    row.FillBar.fillAmount = progress;
                    row.FillBar.color = new Color(1f, 0.85f, 0.2f);
                    row.PickButton.interactable = false;
                    SetButtonLabel(row.PickButton, "等待");
                    break;

                case CollectibleSlotState.Unlocked:
                    row.Root.GetComponent<Image>().color = ColorUnlocked;
                    row.ItemLabel.text = GetItemDisplayText(row.SlotIndex);
                    row.StatusLabel.text = "可拾取";
                    row.FillBar.fillAmount = 1f;
                    row.FillBar.color = new Color(0.3f, 0.9f, 0.3f);
                    row.PickButton.interactable = true;
                    SetButtonLabel(row.PickButton, "拾取");
                    break;

                case CollectibleSlotState.Taken:
                    row.Root.GetComponent<Image>().color = ColorTaken;
                    row.ItemLabel.text = GetItemDisplayText(row.SlotIndex);
                    row.StatusLabel.text = "已拾取";
                    row.FillBar.fillAmount = 1f;
                    row.FillBar.color = new Color(0.4f, 0.4f, 0.4f);
                    row.PickButton.interactable = false;
                    SetButtonLabel(row.PickButton, "完成");
                    break;
            }
        }

        private string GetItemDisplayText(int slotIndex)
        {
            CollectiblePointState state = _collectionManager?.ActivePointState;
            if (state == null || slotIndex >= state.Data.Items.Count) return "???";
            CollectibleItemEntry entry = state.Data.Items[slotIndex];
            return string.Format("{0}  x{1}", entry.ItemId, entry.Quantity);
        }

        private void RefreshBackpackStatus()
        {
            if (_backpack == null || _backpackStatusText == null) return;

            System.Collections.Generic.IReadOnlyList<BackpackSlot> slots = _backpack.GetSlots();
            int used = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty) used++;
            }
            _backpackStatusText.text = string.Format("背包：{0} / {1}", used, _backpack.MaxSlots);
        }

        // ----------------------------------------------------------------
        // 欄位列表重建（依採集點物品數量）
        // ----------------------------------------------------------------

        private void RebuildSlotRows(CollectiblePointState state)
        {
            // 清除舊欄位
            for (int i = 0; i < _slotRows.Count; i++)
            {
                if (_slotRows[i].Root != null)
                    Destroy(_slotRows[i].Root);
            }
            _slotRows.Clear();

            // 重新定位 slot 容器（考慮欄位數量，居中對齊）
            RectTransform slotContainer = GetOrCreateSlotContainer();

            float rowHeight = 60f;
            float containerHeight = rowHeight * state.SlotCount + 10f;
            slotContainer.sizeDelta = new Vector2(360f, containerHeight);

            for (int i = 0; i < state.SlotCount; i++)
            {
                SlotRowUI row = CreateSlotRow(slotContainer, i, state, rowHeight);
                _slotRows.Add(row);
            }

            // 重新計算面板高度
            float panelHeight = 60f + containerHeight + 50f + 60f; // 標題 + slots + 背包狀態 + 關閉按鈕
            RectTransform panelRect = _panelRoot.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(400f, Mathf.Min(panelHeight, 520f));
        }

        private RectTransform GetOrCreateSlotContainer()
        {
            Transform existing = _panelRoot.transform.Find("SlotContainer");
            if (existing != null)
                return existing.GetComponent<RectTransform>();

            GameObject container = new GameObject("SlotContainer");
            container.transform.SetParent(_panelRoot.transform, false);
            RectTransform rt = container.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -55f);
            return rt;
        }

        private SlotRowUI CreateSlotRow(RectTransform parent, int slotIndex,
            CollectiblePointState state, float rowHeight)
        {
            CollectiblePointData data = state.Data;
            CollectibleItemEntry entry = data.Items[slotIndex];

            GameObject rowObj = new GameObject($"SlotRow_{slotIndex}");
            rowObj.transform.SetParent(parent, false);

            RectTransform rowRect = rowObj.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -slotIndex * (rowHeight + 5f));
            rowRect.sizeDelta = new Vector2(0f, rowHeight);

            Image rowBg = rowObj.AddComponent<Image>();
            rowBg.color = ColorLocked;

            // 物品名稱 + 數量標籤
            TextMeshProUGUI itemLabel = CreateLabel(rowObj.transform,
                string.Format("{0}  x{1}", entry.ItemId, entry.Quantity),
                new Vector2(-50f, 10f), 14f, TextAlignmentOptions.Left);
            itemLabel.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
            itemLabel.GetComponent<RectTransform>().anchorMax = new Vector2(0.6f, 1f);
            itemLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(8f, 0f);
            itemLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(-16f, 0f);

            // 狀態文字
            TextMeshProUGUI statusLabel = CreateLabel(rowObj.transform,
                "鎖定", new Vector2(20f, 10f), 12f, TextAlignmentOptions.Center);
            RectTransform statusRect = statusLabel.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(0.6f, 0.5f);
            statusRect.anchoredPosition = new Vector2(8f, 0f);
            statusRect.sizeDelta = new Vector2(-16f, 0f);

            // 解鎖進度條（背景）
            GameObject barBgObj = new GameObject("BarBg");
            barBgObj.transform.SetParent(rowObj.transform, false);
            RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.6f, 0.2f);
            barBgRect.anchorMax = new Vector2(0.75f, 0.8f);
            barBgRect.offsetMin = Vector2.zero;
            barBgRect.offsetMax = Vector2.zero;
            Image barBgImage = barBgObj.AddComponent<Image>();
            barBgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            // 解鎖進度條（填充）
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

            // 拾取按鈕
            Button pickButton = CreatePickButton(rowObj.transform, slotIndex);

            SlotRowUI row = new SlotRowUI
            {
                Root = rowObj,
                ItemLabel = itemLabel,
                StatusLabel = statusLabel,
                FillBar = fillImage,
                PickButton = pickButton,
                SlotIndex = slotIndex
            };
            return row;
        }

        private Button CreatePickButton(Transform parent, int slotIndex)
        {
            GameObject btnObj = new GameObject("PickButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.77f, 0.1f);
            btnRect.anchorMax = new Vector2(1f, 0.9f);
            btnRect.offsetMin = new Vector2(2f, 0f);
            btnRect.offsetMax = new Vector2(-4f, 0f);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.5f, 0.2f);

            Button btn = btnObj.AddComponent<Button>();

            // 按鈕文字
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI btnText = labelObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "拾取";
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontSize = 13f;
            btnText.color = Color.white;

            int capturedIndex = slotIndex;
            btn.onClick.AddListener(() => OnPickButtonClicked(capturedIndex));
            btn.interactable = false;

            return btn;
        }

        private void OnPickButtonClicked(int slotIndex)
        {
            if (_collectionManager == null) return;
            _collectionManager.TryPickItem(slotIndex);
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
            panelRect.anchoredPosition = new Vector2(280f, 0f);
            panelRect.sizeDelta = new Vector2(400f, 400f);

            Image panelBg = _panelRoot.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.12f, 0.15f, 0.92f);

            // 標題
            TextMeshProUGUI title = CreateLabel(_panelRoot.transform,
                "採集完成！", Vector2.zero, 18f, TextAlignmentOptions.Center);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -8f);
            titleRect.sizeDelta = new Vector2(0f, 40f);
            title.color = new Color(0.9f, 0.8f, 0.3f);

            // 背包狀態
            _backpackStatusText = CreateLabel(_panelRoot.transform,
                "背包：0 / 10", Vector2.zero, 12f, TextAlignmentOptions.Right);
            RectTransform backpackRect = _backpackStatusText.GetComponent<RectTransform>();
            backpackRect.anchorMin = new Vector2(0f, 0f);
            backpackRect.anchorMax = new Vector2(1f, 0f);
            backpackRect.pivot = new Vector2(0.5f, 0f);
            backpackRect.anchoredPosition = new Vector2(0f, 58f);
            backpackRect.sizeDelta = new Vector2(-16f, 24f);
            _backpackStatusText.color = new Color(0.7f, 0.7f, 0.7f);

            // 關閉按鈕
            CreateCloseButton(_panelRoot.transform);
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.1f, 0f);
            btnRect.anchorMax = new Vector2(0.9f, 0f);
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
        }
    }
}
