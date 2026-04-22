using ProjectDR.Village.Storage;
using ProjectDR.Village.Backpack;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectDR.Village.Shared;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 倉庫區域畫面（雙欄佈局）。
    /// 左側顯示背包格子清單，右側顯示倉庫物品清單，
    /// 提供雙向轉移按鈕與返回 Hub 的按鈕。
    /// </summary>
    public class StorageAreaView : ViewBase
    {
        [Header("背包區")]
        [SerializeField] private Transform _backpackContainer;
        [SerializeField] private GameObject _backpackSlotRowPrefab;
        [SerializeField] private TMP_Text _backpackTitleLabel;
        [SerializeField] private TMP_Text _backpackEmptyLabel;

        [Header("倉庫區")]
        [SerializeField] private Transform _warehouseContainer;
        [SerializeField] private GameObject _warehouseItemRowPrefab;
        [SerializeField] private TMP_Text _warehouseEmptyLabel;

        [Header("導航")]
        [SerializeField] private Button _returnButton;

        private StorageManager _storageManager;
        private BackpackManager _backpackManager;
        private StorageTransferManager _transferManager;
        private VillageNavigationManager _navigationManager;

        // Overlay 模式：若有設定 returnAction，return 按鈕觸發此回呼而非 ReturnToHub
        private System.Action _returnAction;

        /// <summary>
        /// 由 VillageEntryPoint 注入相依。
        /// 簽名與既有呼叫端一致，無需修改 VillageEntryPoint。
        /// </summary>
        public void Initialize(
            StorageManager storageManager,
            BackpackManager backpackManager,
            StorageTransferManager transferManager,
            VillageNavigationManager navigationManager)
        {
            _storageManager = storageManager;
            _backpackManager = backpackManager;
            _transferManager = transferManager;
            _navigationManager = navigationManager;
            _returnAction = null;
            EventBus.Subscribe<StorageChangedEvent>(OnStorageChanged);
            EventBus.Subscribe<BackpackChangedEvent>(OnBackpackChanged);
        }

        /// <summary>
        /// 設定 overlay 模式的返回行為。
        /// 設定後，return 按鈕會觸發此回呼而非 ReturnToHub()。
        /// </summary>
        /// <param name="returnAction">返回時要執行的回呼。</param>
        public void SetReturnAction(System.Action returnAction)
        {
            _returnAction = returnAction;
        }

        protected override void OnShow()
        {
            if (_returnButton != null)
            {
                _returnButton.onClick.AddListener(OnReturnClicked);
            }

            RefreshAll();
        }

        protected override void OnHide()
        {
            if (_returnButton != null)
            {
                _returnButton.onClick.RemoveListener(OnReturnClicked);
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<StorageChangedEvent>(OnStorageChanged);
            EventBus.Unsubscribe<BackpackChangedEvent>(OnBackpackChanged);
        }

        private void OnStorageChanged(StorageChangedEvent e)
        {
            if (gameObject.activeInHierarchy)
            {
                RefreshAll();
            }
        }

        private void OnBackpackChanged(BackpackChangedEvent e)
        {
            if (gameObject.activeInHierarchy)
            {
                RefreshAll();
            }
        }

        private void RefreshAll()
        {
            RefreshBackpackPanel();
            RefreshWarehousePanel();
            UpdateBackpackTitle();
        }

        private void RefreshBackpackPanel()
        {
            if (_backpackContainer == null) return;

            // 清除現有項目
            for (int i = _backpackContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_backpackContainer.GetChild(i).gameObject);
            }

            IReadOnlyList<BackpackSlot> slots = _backpackManager.GetSlots();
            bool hasItems = false;

            for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
            {
                BackpackSlot slot = slots[slotIndex];
                if (slot.IsEmpty) continue;

                hasItems = true;
                GameObject row = Instantiate(_backpackSlotRowPrefab, _backpackContainer);
                row.SetActive(true);

                TMP_Text[] labels = row.GetComponentsInChildren<TMP_Text>();
                // labels[0] = 物品名稱, labels[1] = 數量, labels[2] = 按鈕文字（跳過）
                if (labels.Length >= 2)
                {
                    labels[0].text = slot.ItemId;
                    labels[1].text = $"{slot.Quantity}/{_backpackManager.DefaultMaxStack}";
                }

                // 整格轉移至倉庫
                int capturedSlotIndex = slotIndex;
                int capturedQuantity = slot.Quantity;
                Button transferButton = row.GetComponentInChildren<Button>();
                if (transferButton != null)
                {
                    transferButton.onClick.AddListener(() =>
                    {
                        _transferManager.TransferToWarehouse(capturedSlotIndex, capturedQuantity);
                    });
                }
            }

            if (_backpackEmptyLabel != null)
            {
                _backpackEmptyLabel.gameObject.SetActive(!hasItems);
            }
        }

        private void RefreshWarehousePanel()
        {
            if (_warehouseContainer == null) return;

            // 清除現有項目
            for (int i = _warehouseContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_warehouseContainer.GetChild(i).gameObject);
            }

            IReadOnlyDictionary<string, int> allItems = _storageManager.GetAllItems();
            bool hasItems = allItems.Count > 0;

            foreach (KeyValuePair<string, int> item in allItems)
            {
                GameObject row = Instantiate(_warehouseItemRowPrefab, _warehouseContainer);
                row.SetActive(true);

                TMP_Text[] labels = row.GetComponentsInChildren<TMP_Text>();
                // labels[0] = 物品名稱, labels[1] = 數量, labels[2] = 按鈕文字（跳過）
                if (labels.Length >= 2)
                {
                    labels[0].text = item.Key;
                    labels[1].text = item.Value.ToString();
                }

                // 轉移一堆疊量至背包
                string capturedItemId = item.Key;
                int maxStack = _backpackManager.DefaultMaxStack;
                Button transferButton = row.GetComponentInChildren<Button>();
                if (transferButton != null)
                {
                    transferButton.onClick.AddListener(() =>
                    {
                        _transferManager.TransferToBackpack(capturedItemId, maxStack);
                    });
                }
            }

            if (_warehouseEmptyLabel != null)
            {
                _warehouseEmptyLabel.gameObject.SetActive(!hasItems);
            }
        }

        private void UpdateBackpackTitle()
        {
            if (_backpackTitleLabel == null) return;

            IReadOnlyList<BackpackSlot> slots = _backpackManager.GetSlots();
            int usedSlots = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    usedSlots++;
                }
            }

            _backpackTitleLabel.text = $"背包 ({usedSlots}/{_backpackManager.MaxSlots})";
        }

        private void OnReturnClicked()
        {
            if (_returnAction != null)
            {
                _returnAction.Invoke();
                return;
            }

            _navigationManager.ReturnToHub();
        }
    }
}
