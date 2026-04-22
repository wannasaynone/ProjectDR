// CraftItemSelectorView — 物品選擇 Overlay。
// 顯示玩家背包+倉庫中可由指定角色處理的物品。
// 玩家選擇物品後回呼給 CraftWorkbenchView 觸發 StartCommission。
//
// UX 法則依據：
// - 菲茨定律：物品行高 80px，易於點擊
// - 席克定律：僅顯示可處理物品（過濾不相關選項，降低認知負擔）
// - 鄰近性：物品名稱與數量在同一行緊密排列
//
// 依 GDD commission-system.md v1.1 § 3.4（物品層級判斷）

using ProjectDR.Village.Commission;
using ProjectDR.Village.Storage;
using ProjectDR.Village.Backpack;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectDR.Village.Shared;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Village.Alchemy
{
    /// <summary>
    /// 物品選擇 Overlay：顯示可放入指定 slot 的物品清單。
    /// 由 CraftWorkbenchView 負責建立與銷毀。
    /// </summary>
    public class CraftItemSelectorView : ViewBase
    {
        [Header("UI 元素")]
        [SerializeField] private TMP_Text _txtTitle;
        [SerializeField] private Transform _itemListContainer;
        [SerializeField] private Button _btnClose;

        [Header("物品行 Prefab（動態生成）")]
        [SerializeField] private Button _itemRowPrefab;

        // 注入相依
        private CommissionRecipesConfig _recipesConfig;
        private BackpackManager _backpackManager;
        private StorageManager _storageManager;

        private string _characterId;
        private int _targetSlotIndex;
        private Action<string, int> _onItemSelected;  // (recipeId, slotIndex)
        private Action _onClose;

        // Design Token 顏色
        private static readonly Color ColorBgPanel    = new Color(0.10f, 0.10f, 0.16f, 0.96f);
        private static readonly Color ColorItemNormal = new Color(0.18f, 0.18f, 0.26f, 1.00f);
        private static readonly Color ColorTextPrimary = new Color(0.94f, 0.94f, 0.94f, 1.00f);
        private static readonly Color ColorTextSecond  = new Color(0.70f, 0.70f, 0.75f, 1.00f);

        /// <summary>初始化物品選擇器。</summary>
        public void Initialize(
            CommissionRecipesConfig recipesConfig,
            BackpackManager backpackManager,
            StorageManager storageManager)
        {
            _recipesConfig = recipesConfig;
            _backpackManager = backpackManager;
            _storageManager = storageManager;
        }

        /// <summary>設定要選擇物品的角色與 slot，並提供選擇後的回呼。</summary>
        public void Open(
            string characterId,
            int slotIndex,
            Action<string, int> onItemSelected,
            Action onClose)
        {
            _characterId = characterId;
            _targetSlotIndex = slotIndex;
            _onItemSelected = onItemSelected;
            _onClose = onClose;

            string charDisplayName = GetCharacterDisplayName(characterId);
            if (_txtTitle != null)
                _txtTitle.text = string.Format("選擇物品 — {0}", charDisplayName);

            BuildItemList();
        }

        protected override void OnShow()
        {
            if (_btnClose != null)
                _btnClose.onClick.AddListener(HandleClose);
        }

        protected override void OnHide()
        {
            if (_btnClose != null)
                _btnClose.onClick.RemoveListener(HandleClose);

            ClearItemList();
        }

        // ===== 私有：建立物品清單 =====

        private void BuildItemList()
        {
            ClearItemList();
            if (_itemListContainer == null || _itemRowPrefab == null) return;
            if (_recipesConfig == null || _backpackManager == null || _storageManager == null) return;

            IReadOnlyList<CommissionRecipeInfo> recipes = _recipesConfig.GetRecipesByCharacter(_characterId);
            bool anyItem = false;

            foreach (CommissionRecipeInfo recipe in recipes)
            {
                if (recipe.IsEmptyHanded) continue;

                string itemId = recipe.InputItemId;
                int backpackCount = _backpackManager.GetItemCount(itemId);
                int storageCount = _storageManager.GetItemCount(itemId);
                int total = backpackCount + storageCount;

                if (total < recipe.InputQuantity) continue;  // 數量不足，不顯示

                anyItem = true;
                string capturedRecipeId = recipe.RecipeId;
                int capturedSlot = _targetSlotIndex;

                Button row = Instantiate(_itemRowPrefab, _itemListContainer);
                row.gameObject.SetActive(true);

                // 設定按鈕背景色
                Image rowBg = row.GetComponent<Image>();
                if (rowBg != null) rowBg.color = ColorItemNormal;

                // 設定文字：「物品ID  x數量  →  產出ID」
                TMP_Text label = row.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = string.Format("{0}  x{1}  →  {2}", itemId, total, recipe.OutputItemId);
                    label.color = ColorTextPrimary;
                    label.raycastTarget = false;  // UGUI P2：子 Text 關閉 Raycast
                }

                row.onClick.AddListener(() => HandleItemSelected(capturedRecipeId, capturedSlot));
            }

            // 空手委託角色（守衛）：顯示空手選項
            foreach (CommissionRecipeInfo recipe in recipes)
            {
                if (!recipe.IsEmptyHanded) continue;

                anyItem = true;
                string capturedRecipeId = recipe.RecipeId;
                int capturedSlot = _targetSlotIndex;

                Button row = Instantiate(_itemRowPrefab, _itemListContainer);
                row.gameObject.SetActive(true);

                Image rowBg = row.GetComponent<Image>();
                if (rowBg != null) rowBg.color = ColorItemNormal;

                TMP_Text label = row.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = string.Format("（空手出發）→  {0}", recipe.OutputItemId);
                    label.color = ColorTextPrimary;
                    label.raycastTarget = false;
                }

                row.onClick.AddListener(() => HandleItemSelected(capturedRecipeId, capturedSlot));
            }

            // 若無任何可用物品，顯示提示
            if (!anyItem)
            {
                Button row = Instantiate(_itemRowPrefab, _itemListContainer);
                row.gameObject.SetActive(true);
                row.interactable = false;

                TMP_Text label = row.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = "（沒有可用的物品）";
                    label.color = ColorTextSecond;
                    label.raycastTarget = false;
                }
            }
        }

        private void ClearItemList()
        {
            if (_itemListContainer == null) return;
            for (int i = _itemListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_itemListContainer.GetChild(i).gameObject);
            }
        }

        private void HandleItemSelected(string recipeId, int slotIndex)
        {
            _onItemSelected?.Invoke(recipeId, slotIndex);
        }

        private void HandleClose()
        {
            _onClose?.Invoke();
        }

        private static string GetCharacterDisplayName(string characterId)
        {
            if (characterId == CharacterIds.FarmGirl) return "農女";
            if (characterId == CharacterIds.Witch) return "魔女";
            if (characterId == CharacterIds.Guard) return "守衛";
            return characterId;
        }
    }
}
