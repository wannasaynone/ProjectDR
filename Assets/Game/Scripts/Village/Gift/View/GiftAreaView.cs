using ProjectDR.Village.Gift;
using ProjectDR.Village.Affinity;
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
    /// 送禮區域畫面（overlay 模式）。
    /// 顯示背包＋倉庫中所有物品的合併清單，
    /// 玩家選擇物品後透過 GiftManager 執行送禮流程。
    /// 監聽 AffinityChangedEvent 刷新好感度顯示，
    /// 監聽 AffinityThresholdReachedEvent 顯示門檻達成回饋。
    /// </summary>
    public class GiftAreaView : ViewBase
    {
        [Header("物品清單")]
        [SerializeField] private Transform _itemListContainer;
        [SerializeField] private GameObject _itemRowPrefab;

        [Header("好感度顯示")]
        [SerializeField] private TMP_Text _affinityLabel;

        [Header("回饋")]
        [SerializeField] private TMP_Text _feedbackLabel;

        [Header("導航")]
        [SerializeField] private Button _returnButton;

        private GiftManager _giftManager;
        private AffinityManager _affinityManager;
        private BackpackManager _backpackManager;
        private StorageManager _storageManager;
        private string _characterId;

        // Overlay 模式：若有設定 returnAction，return 按鈕觸發此回呼
        private System.Action _returnAction;

        // 回饋文字自動隱藏用計時器
        private float _feedbackTimer;
        private const float FeedbackDisplayDuration = 2f;

        /// <summary>
        /// 由 VillageEntryPoint 注入相依。
        /// </summary>
        public void Initialize(
            GiftManager giftManager,
            AffinityManager affinityManager,
            BackpackManager backpackManager,
            StorageManager storageManager,
            string characterId)
        {
            _giftManager = giftManager;
            _affinityManager = affinityManager;
            _backpackManager = backpackManager;
            _storageManager = storageManager;
            _characterId = characterId;
            _returnAction = null;

            EventBus.Subscribe<AffinityChangedEvent>(OnAffinityChanged);
            EventBus.Subscribe<AffinityThresholdReachedEvent>(OnThresholdReached);
        }

        /// <summary>
        /// 設定 overlay 模式的返回行為。
        /// </summary>
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

            // 隱藏回饋文字
            if (_feedbackLabel != null)
            {
                _feedbackLabel.gameObject.SetActive(false);
            }

            _feedbackTimer = 0f;

            RefreshAffinityDisplay();
            RefreshItemList();
        }

        protected override void OnHide()
        {
            if (_returnButton != null)
            {
                _returnButton.onClick.RemoveListener(OnReturnClicked);
            }

            DestroyItemRows();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<AffinityChangedEvent>(OnAffinityChanged);
            EventBus.Unsubscribe<AffinityThresholdReachedEvent>(OnThresholdReached);
        }

        private void Update()
        {
            // 回饋文字自動隱藏
            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0f && _feedbackLabel != null)
                {
                    _feedbackLabel.gameObject.SetActive(false);
                }
            }
        }

        // ===== 物品清單 =====

        /// <summary>
        /// 重建物品清單：合併背包與倉庫中的所有物品。
        /// </summary>
        private void RefreshItemList()
        {
            DestroyItemRows();

            if (_itemListContainer == null || _itemRowPrefab == null) return;

            // 合併背包與倉庫的物品數量
            Dictionary<string, int> mergedItems = new Dictionary<string, int>();

            // 背包
            IReadOnlyList<BackpackSlot> slots = _backpackManager.GetSlots();
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty) continue;

                string itemId = slots[i].ItemId;
                if (mergedItems.ContainsKey(itemId))
                {
                    mergedItems[itemId] += slots[i].Quantity;
                }
                else
                {
                    mergedItems[itemId] = slots[i].Quantity;
                }
            }

            // 倉庫
            IReadOnlyDictionary<string, int> storageItems = _storageManager.GetAllItems();
            foreach (KeyValuePair<string, int> item in storageItems)
            {
                if (item.Value <= 0) continue;

                if (mergedItems.ContainsKey(item.Key))
                {
                    mergedItems[item.Key] += item.Value;
                }
                else
                {
                    mergedItems[item.Key] = item.Value;
                }
            }

            // 建立 UI 行
            foreach (KeyValuePair<string, int> item in mergedItems)
            {
                if (item.Value <= 0) continue;

                string capturedItemId = item.Key;
                int capturedQuantity = item.Value;

                GameObject row = Instantiate(_itemRowPrefab, _itemListContainer);
                row.SetActive(true);

                // 設定物品名稱與數量
                TMP_Text[] labels = row.GetComponentsInChildren<TMP_Text>();
                if (labels.Length >= 1)
                {
                    labels[0].text = capturedItemId;
                }
                if (labels.Length >= 2)
                {
                    labels[1].text = $"x{capturedQuantity}";
                }

                // 點選即送禮
                Button itemButton = row.GetComponentInChildren<Button>();
                if (itemButton != null)
                {
                    itemButton.onClick.AddListener(() => OnItemClicked(capturedItemId));
                }
            }
        }

        /// <summary>銷毀所有動態建立的物品行。</summary>
        private void DestroyItemRows()
        {
            if (_itemListContainer == null) return;

            for (int i = _itemListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_itemListContainer.GetChild(i).gameObject);
            }
        }

        // ===== 好感度顯示 =====

        private void RefreshAffinityDisplay()
        {
            if (_affinityLabel == null || _affinityManager == null || _characterId == null) return;

            int current = _affinityManager.GetAffinity(_characterId);
            _affinityLabel.text = $"好感度：{current}";
        }

        // ===== 按鈕事件 =====

        private void OnItemClicked(string itemId)
        {
            if (_giftManager == null || _characterId == null) return;

            GiftResult result = _giftManager.GiveGift(_characterId, itemId);

            if (result.IsSuccess)
            {
                // 送禮成功：GiftManager 已發布 GiftSuccessEvent
                // 關閉 overlay 回到角色互動畫面
                if (_returnAction != null)
                {
                    _returnAction.Invoke();
                }
            }
            else
            {
                Debug.LogWarning($"[GiftAreaView] 送禮失敗：{result.Error}（角色 {_characterId}，物品 {itemId}）");
            }
        }

        private void OnReturnClicked()
        {
            if (_returnAction != null)
            {
                _returnAction.Invoke();
                return;
            }
        }

        // ===== 事件回呼 =====

        private void OnAffinityChanged(AffinityChangedEvent e)
        {
            if (e.CharacterId != _characterId) return;
            if (!gameObject.activeInHierarchy) return;

            RefreshAffinityDisplay();
        }

        private void OnThresholdReached(AffinityThresholdReachedEvent e)
        {
            if (e.CharacterId != _characterId) return;
            if (!gameObject.activeInHierarchy) return;

            ShowFeedback("新內容已解鎖！");
        }

        private void ShowFeedback(string message)
        {
            if (_feedbackLabel == null) return;

            _feedbackLabel.text = message;
            _feedbackLabel.gameObject.SetActive(true);
            _feedbackTimer = FeedbackDisplayDuration;
        }
    }
}
