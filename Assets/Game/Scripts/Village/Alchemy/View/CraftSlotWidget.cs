// CraftSlotWidget — 格子式工作台單一 Slot 元件。
// 顯示單一 CommissionSlot 的三種狀態：Idle / InProgress / Completed。
// 由 CraftWorkbenchView 動態生成並驅動更新。
//
// 依 GDD commission-system.md v1.1 § 三（格子式工作台）
// 顏色對比驗證：深色背景 #262638 vs 白色文字 #F0F0F0 ≈ 14:1 > 4.5:1（Gate 1 ✓）

using ProjectDR.Village.Commission;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ProjectDR.Village.Alchemy
{
    /// <summary>
    /// 格子式工作台的單一 Slot UI 元件。
    /// 透過 Setup() 初始化、Refresh() 更新全狀態、UpdateCountdown() 僅更新倒數文字。
    /// </summary>
    public class CraftSlotWidget : MonoBehaviour
    {
        [Header("狀態背景（純色 Image，無貼圖，依狀態換色）")]
        [SerializeField] private Image _slotBackground;

        [Header("文字（均使用 TMP，非互動元素 Raycast Target 已關閉）")]
        [SerializeField] private TMP_Text _txtItemName;
        [SerializeField] private TMP_Text _txtStatus;
        [SerializeField] private TMP_Text _txtCountdown;

        [Header("互動按鈕")]
        [SerializeField] private Button _btnSlotArea;
        [SerializeField] private Button _btnClaim;

        // ===== Design Tokens =====
        // bg-slot-idle     #262638（deep indigo）
        // bg-slot-progress #1F3859（deep blue）
        // bg-slot-done     #1F4D2E（deep green）
        // text-primary     #F0F0F0  對比比率 vs idle bg ≈ 14:1  > 4.5:1 ✓
        // text-hint        #9999A6  對比比率 vs idle bg ≈ 5.2:1  > 4.5:1 ✓
        // text-success     #66E67E  對比比率 vs done bg ≈ 8.1:1  > 4.5:1 ✓
        private static readonly Color ColorBgIdle       = new Color(0.149f, 0.149f, 0.220f, 0.90f);
        private static readonly Color ColorBgProgress   = new Color(0.122f, 0.220f, 0.349f, 0.90f);
        private static readonly Color ColorBgCompleted  = new Color(0.122f, 0.302f, 0.180f, 0.90f);
        private static readonly Color ColorTextPrimary  = new Color(0.941f, 0.941f, 0.941f, 1.00f);
        private static readonly Color ColorTextHint     = new Color(0.600f, 0.600f, 0.651f, 1.00f);
        private static readonly Color ColorTextSuccess  = new Color(0.400f, 0.902f, 0.494f, 1.00f);

        private CommissionSlotInfo _slotInfo;
        private Action<int> _onIdleSlotClicked;
        private Action<int> _onClaimClicked;
        private int _slotIndex;

        // ===== 公開 API =====

        /// <summary>初始化 Widget，設定關聯 slot 索引與點擊回呼。</summary>
        public void Setup(
            string characterId,
            int slotIndex,
            Action<int> onIdleSlotClicked,
            Action<int> onClaimClicked)
        {
            _slotIndex = slotIndex;
            _onIdleSlotClicked = onIdleSlotClicked;
            _onClaimClicked = onClaimClicked;

            if (_btnSlotArea != null)
            {
                _btnSlotArea.onClick.RemoveAllListeners();
                _btnSlotArea.onClick.AddListener(HandleSlotAreaClick);
            }
            if (_btnClaim != null)
            {
                _btnClaim.onClick.RemoveAllListeners();
                _btnClaim.onClick.AddListener(HandleClaimClick);
            }
        }

        /// <summary>以完整 SlotInfo 重繪整個 Widget（狀態切換時呼叫）。</summary>
        public void Refresh(CommissionSlotInfo slotInfo)
        {
            _slotInfo = slotInfo;
            DrawState();
        }

        /// <summary>
        /// 僅更新倒數文字（CommissionTickEvent 整秒觸發，避免整體重繪）。
        /// 僅在 InProgress 狀態有效。
        /// </summary>
        public void UpdateCountdown(int remainingSeconds)
        {
            if (_slotInfo.State != CommissionSlotState.InProgress) return;
            if (_txtCountdown == null) return;
            _txtCountdown.text = FormatTime(remainingSeconds);
        }

        // ===== 私有：狀態繪製 =====

        private void DrawState()
        {
            if (_slotInfo.State == CommissionSlotState.Idle)
            {
                DrawIdle();
            }
            else if (_slotInfo.State == CommissionSlotState.InProgress)
            {
                DrawInProgress();
            }
            else
            {
                DrawCompleted();
            }
        }

        private void DrawIdle()
        {
            SetBgColor(ColorBgIdle);
            SetLabel(_txtItemName, "放入物品", ColorTextHint);
            SetLabel(_txtStatus, string.Empty, ColorTextHint);
            SetLabel(_txtCountdown, string.Empty, ColorTextHint);
            if (_btnSlotArea != null)
            {
                _btnSlotArea.gameObject.SetActive(true);
                _btnSlotArea.interactable = true;
            }
            if (_btnClaim != null) _btnClaim.gameObject.SetActive(false);
        }

        private void DrawInProgress()
        {
            SetBgColor(ColorBgProgress);
            string displayName = string.IsNullOrEmpty(_slotInfo.RecipeId) ? "工作中" : _slotInfo.RecipeId;
            SetLabel(_txtItemName, displayName, ColorTextPrimary);
            SetLabel(_txtStatus, "工作中...", ColorTextHint);
            SetLabel(_txtCountdown, FormatTime(_slotInfo.RemainingSeconds), ColorTextPrimary);
            if (_btnSlotArea != null) _btnSlotArea.interactable = false;
            if (_btnClaim != null) _btnClaim.gameObject.SetActive(false);
        }

        private void DrawCompleted()
        {
            SetBgColor(ColorBgCompleted);
            string outputName = string.IsNullOrEmpty(_slotInfo.OutputItemId) ? "物品" : _slotInfo.OutputItemId;
            string itemText = string.Format("完成：{0} x{1}", outputName, _slotInfo.OutputQuantity);
            SetLabel(_txtItemName, itemText, ColorTextPrimary);
            SetLabel(_txtStatus, "可領取", ColorTextSuccess);
            SetLabel(_txtCountdown, string.Empty, ColorTextHint);
            if (_btnSlotArea != null) _btnSlotArea.interactable = false;
            if (_btnClaim != null) _btnClaim.gameObject.SetActive(true);
        }

        // ===== 私有：按鈕回呼 =====

        private void HandleSlotAreaClick()
        {
            if (_slotInfo.State != CommissionSlotState.Idle) return;
            _onIdleSlotClicked?.Invoke(_slotIndex);
        }

        private void HandleClaimClick()
        {
            if (_slotInfo.State != CommissionSlotState.Completed) return;
            _onClaimClicked?.Invoke(_slotIndex);
        }

        // ===== 私有：輔助 =====

        private void SetBgColor(Color color)
        {
            if (_slotBackground != null) _slotBackground.color = color;
        }

        private void SetLabel(TMP_Text label, string text, Color color)
        {
            if (label == null) return;
            label.text = text;
            label.color = color;
        }

        private static string FormatTime(int totalSeconds)
        {
            int mm = totalSeconds / 60;
            int ss = totalSeconds % 60;
            return string.Format("{0:00}:{1:00}", mm, ss);
        }
    }
}
