using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectDR.Village.UI
{
    // ══════════════════════════════════════════════════════════════
    // B8 VillageHubView 漸進解鎖重構（2026-04-18）
    //
    // 設計決策：
    //   保留動態生成角色按鈕方式（_areaButtonPrefab），但改為只生成
    //   已解鎖的角色按鈕。探索入口改為靜態 SerializeField 按鈕，
    //   預設 Inactive，解鎖後才 SetActive(true)。
    //
    // 顯隱規則（GDD character-unlock-system.md v1.2）：
    //   - 角色按鈕：只顯示已解鎖角色（未解鎖完全不生成，不留空位）
    //   - 探索入口：預設隱藏，ExplorationFeatureUnlockedEvent 後顯示
    //
    // 事件訂閱：
    //   CharacterUnlockedEvent    → RefreshCharacterButtons（重新生成）
    //   ExplorationFeatureUnlockedEvent → SetExplorationButtonVisible(true)
    //
    // UGUI 規範（ugui-best-practices.md）：
    //   - 非互動元素關閉 Raycast Target
    //   - 禁用 SetActive(false) 而非 Color.alpha=0（符合 P9）
    //   - 初始狀態由程式碼控制（S7）
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 村莊主畫面（Hub）。
    /// 顯示已解鎖的角色按鈕清單，以及探索入口（節點 2 完成後才顯示）。
    /// 監聽 CharacterUnlockedEvent 與 ExplorationFeatureUnlockedEvent
    /// 以動態更新顯示狀態。
    /// </summary>
    public class VillageHubView : ViewBase
    {
        [Header("角色按鈕區")]
        [SerializeField] private Transform _areaButtonContainer;
        [SerializeField] private Button _areaButtonPrefab;

        [Header("探索入口（預設 Inactive，解鎖後才顯示）")]
        [SerializeField] private Button _explorationButton;

        private VillageNavigationManager _navigationManager;
        private IReadOnlyList<CharacterMenuData> _characters;
        private CharacterUnlockManager _characterUnlockManager;
        private RedDotManager _redDotManager;

        // 事件委派快取（用於 Unsubscribe）
        private readonly Action<CharacterUnlockedEvent> _onCharacterUnlocked;
        private readonly Action<ExplorationFeatureUnlockedEvent> _onExplorationFeatureUnlocked;
        private readonly Action<RedDotUpdatedEvent> _onRedDotUpdated;

        // 記錄每個角色按鈕所對應的紅點 Image（由程式動態建立），以便即時更新顯示
        private readonly Dictionary<string, GameObject> _redDotIndicators = new Dictionary<string, GameObject>();

        public VillageHubView()
        {
            _onCharacterUnlocked = OnCharacterUnlocked;
            _onExplorationFeatureUnlocked = OnExplorationFeatureUnlocked;
            _onRedDotUpdated = OnRedDotUpdated;
        }

        /// <summary>
        /// 注入相依。
        /// </summary>
        /// <param name="navigationManager">導航管理器。</param>
        /// <param name="characters">角色資料清單（含所有角色，由本 View 自行篩選已解鎖者）。</param>
        /// <param name="characterUnlockManager">角色解鎖狀態查詢。</param>
        /// <param name="redDotManager">紅點管理器（可為 null，此時不顯示紅點）。</param>
        public void Initialize(
            VillageNavigationManager navigationManager,
            IReadOnlyList<CharacterMenuData> characters,
            CharacterUnlockManager characterUnlockManager,
            RedDotManager redDotManager = null)
        {
            _navigationManager = navigationManager;
            _characters = characters;
            _characterUnlockManager = characterUnlockManager;
            _redDotManager = redDotManager;

            // 訂閱解鎖事件
            EventBus.Subscribe(_onCharacterUnlocked);
            EventBus.Subscribe(_onExplorationFeatureUnlocked);
            EventBus.Subscribe(_onRedDotUpdated);

            // 確保探索按鈕初始狀態由程式碼控制（不依賴 Editor 殘留狀態）
            // 已解鎖狀態在 OnShow 時套用
            SetExplorationButtonVisible(false);
        }

        protected override void OnShow()
        {
            // 每次顯示時查詢當前解鎖狀態，確保從存檔進入時正確
            RefreshCharacterButtons();
            ApplyExplorationButtonState();
        }

        protected override void OnHide()
        {
            // 無需額外清理
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe(_onCharacterUnlocked);
            EventBus.Unsubscribe(_onExplorationFeatureUnlocked);
            EventBus.Unsubscribe(_onRedDotUpdated);
        }

        // ===== 公開方法（供外部呼叫或測試）=====

        /// <summary>
        /// 設定探索入口按鈕的顯示狀態。
        /// 使用 SetActive 而非 Color.alpha=0，確保不留空 layout 位置（符合 UGUI P9）。
        /// </summary>
        /// <param name="visible">是否顯示。</param>
        public void SetExplorationButtonVisible(bool visible)
        {
            if (_explorationButton != null)
            {
                _explorationButton.gameObject.SetActive(visible);
            }
        }

        // ===== 私有實作 =====

        /// <summary>
        /// 根據 CharacterUnlockManager 的當前狀態，重新生成角色按鈕。
        /// 只生成已解鎖的角色，未解鎖者完全不出現（GDD 漸進揭露哲學）。
        /// </summary>
        private void RefreshCharacterButtons()
        {
            if (_areaButtonContainer == null) return;

            // 清除現有按鈕與紅點快取
            for (int i = _areaButtonContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_areaButtonContainer.GetChild(i).gameObject);
            }
            _redDotIndicators.Clear();

            if (_characters == null) return;

            foreach (CharacterMenuData character in _characters)
            {
                // 只顯示已解鎖的角色（未解鎖者完全隱藏，不是灰色）
                bool isUnlocked = _characterUnlockManager != null
                    && _characterUnlockManager.IsUnlocked(character.CharacterId);

                if (!isUnlocked) continue;

                string capturedCharacterId = character.CharacterId;
                Button button = Instantiate(_areaButtonPrefab, _areaButtonContainer);
                button.gameObject.SetActive(true);

                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = character.DisplayName;
                }

                button.onClick.AddListener(() => _navigationManager.NavigateTo(capturedCharacterId));

                // 紅點指示器（動態建立，不依賴 Prefab 修改）
                GameObject redDot = CreateRedDotIndicator(button.transform);
                _redDotIndicators[capturedCharacterId] = redDot;
                ApplyRedDotVisibility(capturedCharacterId);
            }
        }

        /// <summary>
        /// 建立紅點指示器（12px 圓形紅色 Image），置於按鈕右上角。
        /// 預設 Inactive，由 ApplyRedDotVisibility 依 RedDotManager 狀態決定顯隱。
        /// </summary>
        private GameObject CreateRedDotIndicator(Transform parent)
        {
            var go = new GameObject("RedDot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(48f, 48f); // 3840×2160 reference res，48px 夠小
            rt.anchoredPosition = new Vector2(-16f, -16f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.9f, 0.2f, 0.2f, 1f);
            img.raycastTarget = false;
            // 沒有圓形 sprite，用 UI 內建方形 sprite（視覺上是小紅方塊，IT 階段可接受）
            img.sprite = null;

            go.SetActive(false);
            return go;
        }

        private void ApplyRedDotVisibility(string characterId)
        {
            if (!_redDotIndicators.TryGetValue(characterId, out GameObject dot)) return;
            bool show = _redDotManager != null && _redDotManager.HasAnyRedDot(characterId);
            dot.SetActive(show);
        }

        /// <summary>
        /// OnShow 時套用探索按鈕的當前解鎖狀態（確保從存檔進入時正確）。
        /// </summary>
        private void ApplyExplorationButtonState()
        {
            if (_characterUnlockManager == null) return;
            SetExplorationButtonVisible(_characterUnlockManager.IsExplorationFeatureUnlocked);
        }

        // ===== 事件處理 =====

        private void OnCharacterUnlocked(CharacterUnlockedEvent e)
        {
            // 有角色解鎖時重新整理按鈕清單
            RefreshCharacterButtons();
        }

        private void OnExplorationFeatureUnlocked(ExplorationFeatureUnlockedEvent e)
        {
            SetExplorationButtonVisible(true);
        }

        private void OnRedDotUpdated(RedDotUpdatedEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.CharacterId)) return;
            ApplyRedDotVisibility(e.CharacterId);
        }
    }
}
