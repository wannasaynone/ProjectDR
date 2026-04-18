// CommissionInteractionPresenter — B12 工作中狀態驅動器。
// 連接 CommissionManager 事件與 CharacterInteractionView 的委託狀態。
//
// 設計決策：
// - Presenter 持有 CommissionManager（長生命週期）
// - CharacterInteractionView 透過 SetActiveView() 動態更新（每次進入角色時呼叫）
// - 這樣避免持有可能被銷毀的 View 實例
//
// TBD-B12-MultiSlot：多 Slot 顯示策略（placeholder 選擇，待 GDD 確認）
// - 兩個都 InProgress → 顯示剩餘最短者的倒數（最近完成者）
// - 有 Completed + InProgress → 優先顯示「領取」
// - 領取時一次領取所有 Completed slots
//
// 依 character-interaction.md v2.2 § 六

using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// 委託互動表示器（純邏輯，非 MonoBehaviour）。
    /// 由 VillageEntryPoint 建立，整個場景生命週期共用。
    /// </summary>
    public class CommissionInteractionPresenter : IDisposable
    {
        private readonly CommissionManager _commissionManager;

        // 當前活躍的 View（由 SetActiveView 更新）
        private UI.CharacterInteractionView _activeView;
        private bool _isDisposed;

        public CommissionInteractionPresenter(CommissionManager commissionManager)
        {
            _commissionManager = commissionManager ?? throw new ArgumentNullException(nameof(commissionManager));

            EventBus.Subscribe<CommissionTickEvent>(OnCommissionTick);
            EventBus.Subscribe<CommissionCompletedEvent>(OnCommissionCompleted);
            EventBus.Subscribe<CommissionClaimedEvent>(OnCommissionClaimed);
        }

        /// <summary>
        /// 設定當前活躍的 CharacterInteractionView（每次進入角色時呼叫）。
        /// 設定後立即依該角色的 slot 狀態更新 View 顯示。
        /// </summary>
        public void OnEnterCharacter(string characterId, UI.CharacterInteractionView view)
        {
            _activeView = view;

            if (_commissionManager == null || _activeView == null) return;
            if (!_commissionManager.GetManagedCharacterIds().Contains(characterId)) return;

            IReadOnlyList<CommissionSlotInfo> slots = _commissionManager.GetSlots(characterId);
            CommissionDisplayState displayState = ResolveDisplayState(slots);

            switch (displayState)
            {
                case CommissionDisplayState.Normal:
                    // 無委託進行中，不改變 View 狀態（保持 Normal）
                    break;

                case CommissionDisplayState.Ready:
                    _activeView.SetState(UI.CharacterInteractionState.CommissionReady);
                    string readyCharId = characterId;
                    _activeView.SetCommissionClaimCallback(() => ClaimAllCompleted(readyCharId));
                    break;

                case CommissionDisplayState.InProgress:
                    int minRemaining = FindMinRemainingSeconds(slots);
                    _activeView.SetState(UI.CharacterInteractionState.CommissionInProgress);
                    _activeView.SetCommissionRemainingSeconds(minRemaining);
                    break;
            }
        }

        /// <summary>離開角色 View 時呼叫（清除 active view 引用）。</summary>
        public void OnExitCharacter()
        {
            _activeView = null;
        }

        // ===== 私有：事件處理 =====

        private void OnCommissionTick(CommissionTickEvent e)
        {
            if (_activeView == null) return;
            if (_activeView.CurrentCharacterId != e.CharacterId) return;
            if (_activeView.CurrentState != UI.CharacterInteractionState.CommissionInProgress) return;

            IReadOnlyList<CommissionSlotInfo> slots = _commissionManager.GetSlots(e.CharacterId);
            int minRemaining = FindMinRemainingSeconds(slots);
            _activeView.SetCommissionRemainingSeconds(minRemaining);
        }

        private void OnCommissionCompleted(CommissionCompletedEvent e)
        {
            if (_activeView == null) return;
            if (_activeView.CurrentCharacterId != e.CharacterId) return;

            _activeView.SetState(UI.CharacterInteractionState.CommissionReady);
            string charId = e.CharacterId;
            _activeView.SetCommissionClaimCallback(() => ClaimAllCompleted(charId));
        }

        private void OnCommissionClaimed(CommissionClaimedEvent e)
        {
            if (_activeView == null) return;
            if (_activeView.CurrentCharacterId != e.CharacterId) return;

            IReadOnlyList<CommissionSlotInfo> slots = _commissionManager.GetSlots(e.CharacterId);
            CommissionDisplayState displayState = ResolveDisplayState(slots);

            switch (displayState)
            {
                case CommissionDisplayState.Normal:
                    _activeView.SetState(UI.CharacterInteractionState.Normal);
                    _activeView.ShowFunctionMenu();
                    break;
                case CommissionDisplayState.Ready:
                    string charId = e.CharacterId;
                    _activeView.SetCommissionClaimCallback(() => ClaimAllCompleted(charId));
                    break;
                case CommissionDisplayState.InProgress:
                    int minRemaining = FindMinRemainingSeconds(slots);
                    _activeView.SetState(UI.CharacterInteractionState.CommissionInProgress);
                    _activeView.SetCommissionRemainingSeconds(minRemaining);
                    break;
            }
        }

        // ===== 私有：邏輯 =====

        private void ClaimAllCompleted(string characterId)
        {
            if (_commissionManager == null) return;
            int slotCount = _commissionManager.GetSlotCount(characterId);
            for (int i = 0; i < slotCount; i++)
            {
                CommissionSlotInfo info = _commissionManager.GetSlot(characterId, i);
                if (info.State == CommissionSlotState.Completed)
                {
                    ClaimCommissionResult result = _commissionManager.ClaimCommission(characterId, i);
                    if (!result.IsSuccess)
                    {
                        UnityEngine.Debug.LogWarning(string.Format(
                            "[CommissionInteractionPresenter] ClaimCommission 失敗 char={0} slot={1} error={2}",
                            characterId, i, result.Error));
                    }
                }
            }
        }

        private static CommissionDisplayState ResolveDisplayState(IReadOnlyList<CommissionSlotInfo> slots)
        {
            bool anyCompleted = false;
            bool anyInProgress = false;

            foreach (CommissionSlotInfo slot in slots)
            {
                if (slot.State == CommissionSlotState.Completed) anyCompleted = true;
                if (slot.State == CommissionSlotState.InProgress) anyInProgress = true;
            }

            if (anyCompleted) return CommissionDisplayState.Ready;
            if (anyInProgress) return CommissionDisplayState.InProgress;
            return CommissionDisplayState.Normal;
        }

        private static int FindMinRemainingSeconds(IReadOnlyList<CommissionSlotInfo> slots)
        {
            int min = int.MaxValue;
            foreach (CommissionSlotInfo slot in slots)
            {
                if (slot.State == CommissionSlotState.InProgress && slot.RemainingSeconds < min)
                    min = slot.RemainingSeconds;
            }
            return min == int.MaxValue ? 0 : min;
        }

        // ===== IDisposable =====

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            EventBus.Unsubscribe<CommissionTickEvent>(OnCommissionTick);
            EventBus.Unsubscribe<CommissionCompletedEvent>(OnCommissionCompleted);
            EventBus.Unsubscribe<CommissionClaimedEvent>(OnCommissionClaimed);
            _activeView = null;
        }
    }

    /// <summary>多 Slot 顯示策略的展示狀態（CommissionInteractionPresenter 內部用）。</summary>
    internal enum CommissionDisplayState
    {
        Normal,
        InProgress,
        Ready,
    }
}
