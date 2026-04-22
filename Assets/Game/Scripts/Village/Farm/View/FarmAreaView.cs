using ProjectDR.Village.Farm;
using ProjectDR.Village.Storage;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectDR.Village.Shared;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.ItemType;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// 農田區域畫面。
    /// 顯示所有農田格子的狀態（空閒/生長中/可收穫），
    /// 提供種植、收穫、全部收穫與返回 Hub 的功能。
    /// </summary>
    public class FarmAreaView : ViewBase
    {
        [Header("農田格子")]
        [SerializeField] private Transform _plotContainer;
        [SerializeField] private GameObject _plotUIPrefab;  // 動態生成每個格子 UI

        [Header("種子選擇")]
        [SerializeField] private GameObject _seedSelectionPanel;
        [SerializeField] private Transform _seedListContainer;
        [SerializeField] private GameObject _seedItemPrefab;  // 種子選項按鈕
        [SerializeField] private Button _cancelSeedSelectionButton;

        [Header("操作")]
        [SerializeField] private Button _harvestAllButton;
        [SerializeField] private Button _returnButton;

        private FarmManager _farmManager;
        private StorageManager _storageManager;
        private ItemTypeResolver _itemTypeResolver;
        private VillageNavigationManager _navigationManager;

        // Overlay 模式：若有設定 returnAction，return 按鈕觸發此回呼而非 ReturnToHub
        private System.Action _returnAction;

        // 動態建立的格子 UI 清單（按索引對應農田格子）
        private readonly List<GameObject> _plotUIObjects = new List<GameObject>();

        // 目前等待種植的格子索引（-1 表示未選擇）
        private int _pendingPlotIndex = -1;

        /// <summary>
        /// 由 VillageEntryPoint 注入相依。
        /// </summary>
        public void Initialize(
            FarmManager farmManager,
            StorageManager storageManager,
            ItemTypeResolver itemTypeResolver,
            VillageNavigationManager navigationManager)
        {
            _farmManager = farmManager;
            _storageManager = storageManager;
            _itemTypeResolver = itemTypeResolver;
            _navigationManager = navigationManager;
            _returnAction = null;

            EventBus.Subscribe<FarmPlotPlantedEvent>(OnFarmPlotChanged);
            EventBus.Subscribe<FarmPlotHarvestedEvent>(OnFarmPlotChanged);
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

            if (_harvestAllButton != null)
            {
                _harvestAllButton.onClick.AddListener(OnHarvestAllClicked);
            }

            if (_cancelSeedSelectionButton != null)
            {
                _cancelSeedSelectionButton.onClick.AddListener(OnCancelSeedSelection);
            }

            // 預設隱藏種子選擇面板
            if (_seedSelectionPanel != null)
            {
                _seedSelectionPanel.SetActive(false);
            }

            _pendingPlotIndex = -1;

            BuildPlotUIs();
            RefreshAllPlots();
        }

        protected override void OnHide()
        {
            if (_returnButton != null)
            {
                _returnButton.onClick.RemoveListener(OnReturnClicked);
            }

            if (_harvestAllButton != null)
            {
                _harvestAllButton.onClick.RemoveListener(OnHarvestAllClicked);
            }

            if (_cancelSeedSelectionButton != null)
            {
                _cancelSeedSelectionButton.onClick.RemoveListener(OnCancelSeedSelection);
            }

            // 清理動態建立的格子 UI
            DestroyPlotUIs();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<FarmPlotPlantedEvent>(OnFarmPlotChanged);
            EventBus.Unsubscribe<FarmPlotHarvestedEvent>(OnFarmPlotChanged);
        }

        /// <summary>
        /// Update：每幀更新生長中格子的倒計時顯示。
        /// </summary>
        private void Update()
        {
            if (_farmManager == null) return;

            IReadOnlyList<FarmPlot> plots = _farmManager.GetAllPlots();
            long currentTime = GetCurrentTimestamp();

            for (int i = 0; i < _plotUIObjects.Count && i < plots.Count; i++)
            {
                FarmPlot plot = plots[i];
                if (plot.IsEmpty || plot.IsReadyToHarvest(currentTime)) continue;

                // 僅更新倒計時文字，不重建整個格子 UI
                TMP_Text statusText = GetPlotStatusText(i);
                if (statusText != null)
                {
                    float remaining = plot.GetRemainingSeconds(currentTime);
                    statusText.text = FormatRemainingTime(remaining);
                }
            }
        }

        // ===== 農田格子 UI 建立與刷新 =====

        /// <summary>依照 FarmManager 的格子數量，動態建立格子 UI 物件。</summary>
        private void BuildPlotUIs()
        {
            DestroyPlotUIs();

            if (_plotContainer == null || _plotUIPrefab == null || _farmManager == null) return;

            for (int i = 0; i < _farmManager.PlotCount; i++)
            {
                int capturedIndex = i;
                GameObject plotUI = Instantiate(_plotUIPrefab, _plotContainer);
                plotUI.SetActive(true);
                _plotUIObjects.Add(plotUI);

                // 綁定操作按鈕（種植或收穫）
                Button actionButton = plotUI.GetComponentInChildren<Button>();
                if (actionButton != null)
                {
                    actionButton.onClick.AddListener(() => OnPlotActionClicked(capturedIndex));
                }
            }
        }

        /// <summary>銷毀所有動態建立的格子 UI 物件。</summary>
        private void DestroyPlotUIs()
        {
            for (int i = 0; i < _plotUIObjects.Count; i++)
            {
                if (_plotUIObjects[i] != null)
                {
                    Destroy(_plotUIObjects[i]);
                }
            }
            _plotUIObjects.Clear();
        }

        /// <summary>刷新所有農田格子的 UI 顯示。</summary>
        private void RefreshAllPlots()
        {
            if (_farmManager == null) return;

            IReadOnlyList<FarmPlot> plots = _farmManager.GetAllPlots();
            long currentTime = GetCurrentTimestamp();

            for (int i = 0; i < _plotUIObjects.Count && i < plots.Count; i++)
            {
                RefreshPlotUI(i, plots[i], currentTime);
            }
        }

        /// <summary>刷新單一格子的 UI 顯示。</summary>
        private void RefreshPlotUI(int plotIndex, FarmPlot plot, long currentTime)
        {
            if (plotIndex < 0 || plotIndex >= _plotUIObjects.Count) return;

            GameObject plotUI = _plotUIObjects[plotIndex];
            if (plotUI == null) return;

            TMP_Text statusText = GetPlotStatusText(plotIndex);
            Button actionButton = plotUI.GetComponentInChildren<Button>();
            TMP_Text buttonLabel = GetPlotButtonLabel(plotIndex);

            if (plot.IsEmpty)
            {
                // 空閒狀態：顯示「空閒」，按鈕顯示「種植」
                if (statusText != null) statusText.text = "空閒";
                if (buttonLabel != null) buttonLabel.text = "種植";
                if (actionButton != null) actionButton.interactable = true;
            }
            else if (plot.IsReadyToHarvest(currentTime))
            {
                // 可收穫狀態：顯示作物 ID，按鈕顯示「收穫」
                if (statusText != null) statusText.text = $"可收穫：{plot.HarvestItemId}";
                if (buttonLabel != null) buttonLabel.text = "收穫";
                if (actionButton != null) actionButton.interactable = true;
            }
            else
            {
                // 生長中狀態：顯示倒計時，按鈕禁用
                float remaining = plot.GetRemainingSeconds(currentTime);
                if (statusText != null) statusText.text = FormatRemainingTime(remaining);
                if (buttonLabel != null) buttonLabel.text = "生長中";
                if (actionButton != null) actionButton.interactable = false;
            }
        }

        // ===== 種子選擇面板 =====

        /// <summary>開啟種子選擇面板，列出倉庫中所有種子。</summary>
        private void OpenSeedSelectionPanel(int plotIndex)
        {
            _pendingPlotIndex = plotIndex;

            if (_seedSelectionPanel == null) return;

            // 清除舊的種子清單
            if (_seedListContainer != null)
            {
                for (int i = _seedListContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(_seedListContainer.GetChild(i).gameObject);
                }
            }

            // 列出倉庫中屬於「種子」類型且有庫存的物品
            IReadOnlyDictionary<string, int> allItems = _storageManager.GetAllItems();
            foreach (KeyValuePair<string, int> item in allItems)
            {
                string itemId = item.Key;
                int quantity = item.Value;

                if (!_itemTypeResolver.IsType(itemId, ItemTypes.Seed)) continue;
                if (quantity <= 0) continue;

                if (_seedListContainer != null && _seedItemPrefab != null)
                {
                    string capturedItemId = itemId;
                    int capturedQuantity = quantity;

                    GameObject seedEntry = Instantiate(_seedItemPrefab, _seedListContainer);
                    seedEntry.SetActive(true);

                    // 設定種子名稱與數量顯示
                    TMP_Text[] labels = seedEntry.GetComponentsInChildren<TMP_Text>();
                    if (labels.Length >= 1)
                    {
                        labels[0].text = capturedItemId;
                    }
                    if (labels.Length >= 2)
                    {
                        labels[1].text = $"x{capturedQuantity}";
                    }

                    // 點選即種植
                    Button seedButton = seedEntry.GetComponentInChildren<Button>();
                    if (seedButton != null)
                    {
                        seedButton.onClick.AddListener(() => OnSeedSelected(capturedItemId));
                    }
                }
            }

            _seedSelectionPanel.SetActive(true);
        }

        /// <summary>關閉種子選擇面板並重設待種植格子索引。</summary>
        private void CloseSeedSelectionPanel()
        {
            _pendingPlotIndex = -1;

            if (_seedSelectionPanel != null)
            {
                _seedSelectionPanel.SetActive(false);
            }
        }

        // ===== 按鈕事件處理 =====

        /// <summary>格子操作按鈕點擊：根據格子狀態決定種植或收穫。</summary>
        private void OnPlotActionClicked(int plotIndex)
        {
            if (_farmManager == null) return;

            FarmPlot plot = _farmManager.GetPlot(plotIndex);
            long currentTime = GetCurrentTimestamp();

            if (plot.IsEmpty)
            {
                // 空閒格子 → 開啟種子選擇面板
                OpenSeedSelectionPanel(plotIndex);
            }
            else if (plot.IsReadyToHarvest(currentTime))
            {
                // 可收穫格子 → 直接收穫
                _farmManager.Harvest(plotIndex);
            }
        }

        /// <summary>玩家選擇種子後執行種植。</summary>
        private void OnSeedSelected(string seedItemId)
        {
            if (_farmManager == null || _pendingPlotIndex < 0) return;

            PlantResult result = _farmManager.Plant(_pendingPlotIndex, seedItemId);

            if (result.IsSuccess)
            {
                CloseSeedSelectionPanel();
                // 種植成功後由事件驅動刷新（FarmPlotPlantedEvent）
            }
            else
            {
                // IT 階段：種植失敗僅 Log 錯誤，不顯示 UI 錯誤訊息
                Debug.LogWarning($"[FarmAreaView] 種植失敗：{result.Error}（格子 {_pendingPlotIndex}，種子 {seedItemId}）");
            }
        }

        /// <summary>取消種子選擇。</summary>
        private void OnCancelSeedSelection()
        {
            CloseSeedSelectionPanel();
        }

        /// <summary>全部收穫按鈕點擊。</summary>
        private void OnHarvestAllClicked()
        {
            if (_farmManager == null) return;

            HarvestAllResult result = _farmManager.HarvestAll();

            if (result.HarvestedCount == 0)
            {
                // IT 階段：無可收穫格子時不顯示 UI 訊息，僅 Log
                Debug.Log("[FarmAreaView] 全部收穫：目前沒有可收穫的格子。");
            }
        }

        /// <summary>返回按鈕點擊。</summary>
        private void OnReturnClicked()
        {
            if (_returnAction != null)
            {
                _returnAction.Invoke();
                return;
            }

            _navigationManager.ReturnToHub();
        }

        // ===== 事件回呼 =====

        private void OnFarmPlotChanged(FarmPlotPlantedEvent e)
        {
            if (gameObject.activeInHierarchy)
            {
                RefreshAllPlots();
            }
        }

        private void OnFarmPlotChanged(FarmPlotHarvestedEvent e)
        {
            if (gameObject.activeInHierarchy)
            {
                RefreshAllPlots();
            }
        }

        // ===== 工具方法 =====

        /// <summary>取得指定格子的狀態文字元件。</summary>
        private TMP_Text GetPlotStatusText(int plotIndex)
        {
            if (plotIndex < 0 || plotIndex >= _plotUIObjects.Count) return null;

            GameObject plotUI = _plotUIObjects[plotIndex];
            if (plotUI == null) return null;

            TMP_Text[] texts = plotUI.GetComponentsInChildren<TMP_Text>();
            // texts[0] = 狀態文字
            return texts.Length >= 1 ? texts[0] : null;
        }

        /// <summary>取得指定格子的操作按鈕標籤文字元件。</summary>
        private TMP_Text GetPlotButtonLabel(int plotIndex)
        {
            if (plotIndex < 0 || plotIndex >= _plotUIObjects.Count) return null;

            GameObject plotUI = _plotUIObjects[plotIndex];
            if (plotUI == null) return null;

            TMP_Text[] texts = plotUI.GetComponentsInChildren<TMP_Text>();
            // texts[1] = 按鈕文字
            return texts.Length >= 2 ? texts[1] : null;
        }

        /// <summary>取得當前 UTC 時間戳記（秒），使用 SystemTimeProvider 支援離線計時。</summary>
        private static long GetCurrentTimestamp()
        {
            return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// 將剩餘秒數格式化為可讀字串。
        /// 小於 1 小時顯示 MM:SS，1 小時以上顯示 HH:MM:SS。
        /// </summary>
        private static string FormatRemainingTime(float remainingSeconds)
        {
            int total = Mathf.CeilToInt(remainingSeconds);
            int hours = total / 3600;
            int minutes = (total % 3600) / 60;
            int seconds = total % 60;

            if (hours > 0)
            {
                return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            }
            else
            {
                return $"{minutes:D2}:{seconds:D2}";
            }
        }
    }
}
