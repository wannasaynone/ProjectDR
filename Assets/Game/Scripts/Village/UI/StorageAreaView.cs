using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 倉庫區域畫面。
    /// 顯示所有儲存物品的清單與數量，並提供返回 Hub 的按鈕。
    /// </summary>
    public class StorageAreaView : UIToolkitViewBase
    {
        private StorageManager _storageManager;
        private VillageNavigationManager _navigationManager;
        private VisualElement _itemListContainer;
        private Button _returnButton;

        public void Initialize(StorageManager storageManager, VillageNavigationManager navigationManager)
        {
            _storageManager = storageManager;
            _navigationManager = navigationManager;
            EventBus.Subscribe<StorageChangedEvent>(OnStorageChanged);
        }

        protected override void OnShow()
        {
            _itemListContainer = Root.Q<VisualElement>("item-list-container");
            _returnButton = Root.Q<Button>("return-button");

            if (_returnButton != null)
            {
                _returnButton.clicked += OnReturnClicked;
            }

            RefreshItemList();
        }

        protected override void OnHide()
        {
            if (_returnButton != null)
            {
                _returnButton.clicked -= OnReturnClicked;
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

            _itemListContainer.Clear();

            IReadOnlyDictionary<string, int> allItems = _storageManager.GetAllItems();

            if (allItems.Count == 0)
            {
                Label emptyLabel = new Label("倉庫是空的");
                emptyLabel.AddToClassList("empty-label");
                _itemListContainer.Add(emptyLabel);
                return;
            }

            foreach (KeyValuePair<string, int> item in allItems)
            {
                VisualElement row = new VisualElement();
                row.AddToClassList("item-row");

                Label nameLabel = new Label(item.Key);
                nameLabel.AddToClassList("item-name");

                Label countLabel = new Label(item.Value.ToString());
                countLabel.AddToClassList("item-count");

                row.Add(nameLabel);
                row.Add(countLabel);
                _itemListContainer.Add(row);
            }
        }

        private void OnReturnClicked()
        {
            _navigationManager.ReturnToHub();
        }
    }
}
