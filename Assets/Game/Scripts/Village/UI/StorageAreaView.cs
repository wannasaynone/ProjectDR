using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 倉庫區域畫面。
    /// 顯示所有儲存物品的清單與數量，並提供返回 Hub 的按鈕。
    /// </summary>
    public class StorageAreaView : ViewBase
    {
        [SerializeField] private Transform _itemListContainer;
        [SerializeField] private GameObject _itemRowPrefab;
        [SerializeField] private TMP_Text _emptyLabel;
        [SerializeField] private Button _returnButton;

        private StorageManager _storageManager;
        private VillageNavigationManager _navigationManager;

        public void Initialize(StorageManager storageManager, VillageNavigationManager navigationManager)
        {
            _storageManager = storageManager;
            _navigationManager = navigationManager;
            EventBus.Subscribe<StorageChangedEvent>(OnStorageChanged);
        }

        protected override void OnShow()
        {
            if (_returnButton != null)
            {
                _returnButton.onClick.AddListener(OnReturnClicked);
            }

            RefreshItemList();
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
        }

        private void OnStorageChanged(StorageChangedEvent e)
        {
            if (gameObject.activeInHierarchy)
            {
                RefreshItemList();
            }
        }

        private void RefreshItemList()
        {
            if (_itemListContainer == null) return;

            // 清除現有項目
            for (int i = _itemListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_itemListContainer.GetChild(i).gameObject);
            }

            IReadOnlyDictionary<string, int> allItems = _storageManager.GetAllItems();

            if (allItems.Count == 0)
            {
                if (_emptyLabel != null) _emptyLabel.gameObject.SetActive(true);
                return;
            }

            if (_emptyLabel != null) _emptyLabel.gameObject.SetActive(false);

            foreach (KeyValuePair<string, int> item in allItems)
            {
                GameObject row = Instantiate(_itemRowPrefab, _itemListContainer);
                row.SetActive(true);

                TMP_Text[] labels = row.GetComponentsInChildren<TMP_Text>();
                if (labels.Length >= 2)
                {
                    labels[0].text = item.Key;
                    labels[1].text = item.Value.ToString();
                }
            }
        }

        private void OnReturnClicked()
        {
            _navigationManager.ReturnToHub();
        }
    }
}
