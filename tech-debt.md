# 技術債登記 —— ProjectDR

> 最後更新：2026-04-22（Sprint 7 D7 緊急修復：新增 TD-2026-011 gameDataAccess Sprint 8 真實實作）
> 總計：11 筆（🔴 高 2 / 🟡 中 3 / 🟢 低 6）；其中 ✅ 已處置 2 筆（TD-2026-003、TD-2026-006）/ 🔄 未處理 9 筆
> 建立者：dev-head（Sprint 7 D5 tech-debt 掃描首次建立）

## 掃描範圍與掃描條件

- **掃描範圍**：`projects/ProjectDR/Assets/Game/Scripts/**/*.cs`（排除 `Assets/Tests/**` 與 `Assets/KahaGameCore/Package/**` 第三方套件）
- **標記掃描**：`// TODO` / `// FIXME` / `// HACK` / `// XXX` / `// TEMP`
- **結構護欄**（源自 Sprint 7 C1 `validate-file-size.sh`）：
  - 檔案 > 800 行 → 警示
  - 檔案 > 1200 行 → 強警示
  - 單方法 > 100 行 → 警示

---

## 🔴 高優先級

### TD-2026-001 CharacterInteractionView.cs 1463 行（強警示 + 架構債）

- **位置**：`Assets/Game/Scripts/Village/CharacterInteraction/View/CharacterInteractionView.cs`
- **標記**：validate-file-size.sh 強警示（> 1200 行）
- **描述**：單一 View class 承載 50+ 個方法，混合五類職責：
  1. 對白播放控制（StartDialoguePlayback / AdvanceDialogue / OnTypewriterLineComplete）
  2. 角色問答系統（StartCharacterQuestionInline / ShowCharacterQuestionChoices / OnCharacterQuestionOptionSelected）
  3. 委託倒數 UI（SetCommissionRemainingSeconds / RefreshCommissionCountdownText / OnCommissionClaimClicked）
  4. 紅點顯示（ApplyButtonRedDots / OnRedDotUpdated）
  5. 功能選單與 Overlay（RefreshMenu / OpenOverlay / CloseOverlay / OpenCraftWorkbench）
- **影響**：
  - 違反單一職責原則（SRP），任何一類功能修改都有高機率波及其他類
  - 修改成本高，測試困難（事件訂閱關係盤根錯節）
  - 是 Sprint 7 架構重構漏網之魚（Sprint 7 B 類重構聚焦在 VillageEntryPoint/Manager 層，未觸及 View 層）
- **發現日**：2026-04-22（Sprint 7 C5 預盤點，D5 正式登記）
- **預估處理時間**：20-30h（拆分為 5 個子 View：DialogueView / QuestionView / CommissionCountdownView / MenuView + 主控 View，需同步重繪 Prefab 與測試）
- **狀態**：🔄 未處理
- **建議處置**：**延後至 VS 階段前專案檢討會議決定**
  - 理由：成本 > 1 day，超出 Sprint 7 剩餘時間預算
  - Sprint 7 的首要目標是 ADR-002 退出 Gate（A/B/C/D 四區塊），本項不阻擋進 VS（只是讓 VS 期間的 View 改動成本偏高）
  - 若 VS 階段確定要大幅度擴充角色互動 UI（例：加戀愛事件選項、加多層選單）則必須先拆；若 VS 不動 View，可延至 DEMO 前
- **關聯 ADR**：無（待建 ADR-005「View 層職責拆分」，若製作人決定處理）

### TD-2026-002 VillageEntryPoint.cs 582 行（警示，未達 Sprint 7 B5 目標）

- **位置**：`Assets/Game/Scripts/Village/Core/Manager/VillageEntryPoint.cs`
- **標記**：validate-file-size.sh 警示（> 800 行為警示；582 未觸發 hook，但明示超出 Sprint 7 B5 設定的「< 300 行」目標）
- **描述**：Sprint 7 B5 目標「瘦身至 < 300 行」實際只達到 582 行。檔案從 1616 行削減至 582 行（削 64%），主要瘦身成果來自 Installer 拆分。剩餘 582 行組成：
  - 欄位宣告 + SerializeField（~60 行）
  - Installer 協調、事件訂閱/解除（~200 行）
  - Intro CG 流程協調、角色 View 初始化、主線 quest 節點路由（~200 行）
  - Navigation/Exploration event handler（~120 行）
- **影響**：
  - 中度影響可讀性，但最長單方法僅 55 行（InitializeCharacterView），未違反 100 行護欄
  - 無 SOLID 級違反；所有職責與 VillageEntryPoint 的「Composition Root + 跨域事件協調」定位一致
  - 若未來再加新域（如戀愛事件、劇情分支）會快速超過 800 行
- **發現日**：2026-04-22（Sprint 7 B5 驗收時即明示「< 300 行目標未達成」，D5 登記為技術債）
- **預估處理時間**：8-12h（抽出 IntroCGCoordinator / CrossDomainEventRouter 兩個 collaborator class）
- **狀態**：🔄 未處理
- **建議處置**：**延後至 VS 階段前評估**
  - 理由：當前未觸發任何護欄、無 SOLID 違規、無單方法超長
  - Sprint 7 B5 已接受「< 300 行未達成」為已知技術債（Sprint 文件第 56 行註記為「已完成」）
  - 進 VS 前若無新域加入，可維持現狀；若 VS 啟動後檔案再次膨脹（例：戀愛事件加入），應優先重構
- **關聯 ADR**：ADR-003（Village Composition Root 契約）

---

## 🟡 中優先級

### TD-2026-003 ExplorationEntryPoint.Start() 199 行（單方法超護欄）

- **位置**：`Assets/Game/Scripts/Village/Exploration/Core/Manager/ExplorationEntryPoint.cs:69-267`
- **標記**：validate-file-size.sh 警示（單方法 > 100 行）
- **描述**：`Start()` 方法 199 行，相當於整個檔案 314 行的 63%。內部混合三階段職責：
  1. Stage 1：讀 config JSON（map / combat / monster）反序列化
  2. Stage 2：建 logic 層（GridMap / MonsterManager / CombatManager / DeathManager / PlayerCombatStats / PlayerFreeMovement / SwordAttack / EvacuationManager / CollectionManager）+ 事件訂閱
  3. Stage 3：建 view 層（ExplorationMapView / ExplorationFreePlayerView / 相機等）
- **影響**：
  - 可讀性中度損失：新 agent 讀 Start() 需一次吸收 3 階段 15+ 物件關係
  - 違反 Sprint 7 B5 後新訂的「無單一方法 > 100 行」工作室規範
  - 屬於 Sprint 7 之前的資產，Sprint 7 C 類未納入（聚焦在 VillageEntryPoint）
- **發現日**：2026-04-22（Sprint 7 C4 體檢時發現，D5 正式登記）
- **預估處理時間**：4-6h（拆為 LoadConfigs() / BuildLogicLayer() / WireEvents() / BuildViewLayer() 四個私有方法；測試：整合測試只需通過，無需新增）
- **狀態**：✅ 已處置（2026-04-22，Sprint 7 收尾批次）
- **處置結果**：Start()（原 199 行）拆為 5 個私有方法：`LoadConfigs()`（11 行）、`BuildLogicLayer()`（59 行）、`WireEvents()`（21 行）、`BuildViewLayer()`（100 行）、`BuildCollectionViews()`（32 行），另加 `BuildCombatViews()`（23 行）。Start() 本身縮至 11 行。總檔案行數 314→380（含新增方法宣告與說明），全部方法均 ≤ 100 行。1426/1426 測試全綠。
- **關聯 ADR**：無（純重構，不涉及契約變更）

### TD-2026-004 CollectionItemPanelView.cs 760 行（接近警示）

- **位置**：`Assets/Game/Scripts/Village/Exploration/Collection/View/CollectionItemPanelView.cs`
- **標記**：validate-file-size.sh 接近警示（距 800 行護欄 40 行）
- **描述**：採集物清單面板 UI，單一 MonoBehaviour 承載採集物顯示 + 資源轉移 UI。是 Sprint 7 前的既有資產。
- **影響**：
  - 當前未觸發護欄，但容易因未來新增採集類型（例：戰鬥戰利品、特殊物品）而突破
  - 影響在未來某次變更中才會浮現，現在無立即阻擋
- **發現日**：2026-04-22
- **預估處理時間**：8-12h（拆分為 ItemListView + TransferPanelView + 主控）
- **狀態**：🔄 未處理
- **建議處置**：**延後，觀察**
  - 新增採集類型前不動；新增前若破 800 行再處理
- **關聯 ADR**：無

### TD-2026-005 CommissionManager.cs 630 行（接近警示）

- **位置**：`Assets/Game/Scripts/Village/Commission/Manager/CommissionManager.cs`
- **標記**：validate-file-size.sh 接近警示
- **描述**：委託系統的核心管理器，630 行主要為委託狀態機 + 委託配方匹配邏輯。
- **影響**：
  - 當前未觸發護欄
  - 是否該拆需評估：委託系統是「完整子域」，630 行集中一處未必不合理；但若後續加多委託併行 / 委託依賴鏈等功能應拆
- **發現日**：2026-04-22
- **預估處理時間**：10-15h（拆為 CommissionStateMachine + CommissionRecipeMatcher + CommissionManager facade）
- **狀態**：🔄 未處理
- **建議處置**：**延後至 VS 前評估**
  - 視 VS 階段是否擴充委託系統決定
- **關聯 ADR**：無

---

## 🟢 低優先級

### TD-2026-006 HCGDialogueSetup.cs 硬編碼 HCG 對白（IT 階段 placeholder）

- **位置**：`Assets/Game/Scripts/Village/CG/Manager/HCGDialogueSetup.cs:101`
- **標記**：`// TODO: 正式版本應從外部資料源（Google Sheets -> JSON）載入`
- **描述**：IT 階段為求快速實裝 HCG，使用 `StringBuilder` 硬編碼 HCG 對白資料並以 `GameStaticDataDeserializer` 反序列化。違反工作室級規則「遊戲數值外部化」，但內容量小（每角色 1 個場景）、且在 ADR-002 IT 階段例外的精神下可接受。
- **影響**：
  - 若 HCG 數量擴張（VS/DEMO 階段會有多場景）會快速變成維護負擔
  - 屬於 ADR-001 豁免精神之外的資產（對白屬敘事資料，理應走 Sheets）
- **發現日**：2026-04-22
- **預估處理時間**：3-5h（建 hcg-dialogue-config.json、CGSceneDialogue ConfigData 實作 IGameData、移除硬編碼）
- **狀態**：✅ 已處置（2026-04-22，Sprint 7 收尾批次）
- **處置結果**：製作人採納 dev-head 建議選 b — 追加 ADR-002 A17 條目登記為 IT 階段例外（豁免至 VS 後 HCG 系統重寫時一併外部化）。`HCGDialogueSetup.cs` 檔頭已加 `// EXEMPT: ADR-002 A17`。ADR-002 Status History 新增 v1.4 條目。
- **關聯 ADR**：ADR-001（資料治理）、ADR-002（IT 階段例外 A17）

### TD-2026-007 AffinityInstaller GiftManager 建構取捨（ADR-003 演進項）

- **位置**：`Assets/Game/Scripts/Village/Core/Manager/AffinityInstaller.cs:83-87`
- **標記**：`// TODO: 待 ADR-003 演進`
- **描述**：按 ADR-003 D2.3，AffinityInstaller 應持有 AffinityManager + GiftManager，但 GiftManager 需要「可寫的」BackpackManager/StorageManager 參考（呼叫 `RemoveItem`），而 ctx 只提供 IBackpackQuery/IStorageQuery 唯讀介面。短期解法：GiftManager 仍由 VillageEntryPoint 直接建構並傳給 AffinityInstaller。
- **影響**：
  - AffinityInstaller 的「自主建構 + Uninstall 自清」契約略有破口（GiftManager 的 lifecycle 仍綁 VillageEntryPoint）
  - 非阻擋性（整合測試通過），但違反 Sprint 7 B 類的 Installer 契約精神
- **發現日**：2026-04-22
- **預估處理時間**：4-6h（方案 A：ctx 追加 mutable 參考接口；方案 B：CoreStorageInstaller 暴露 IBackpackMutable/IStorageMutable；方案 C：引入 IItemRemover 介面僅暴露 RemoveItem）
- **狀態**：🔄 未處理
- **建議處置**：**延後至 VS 前，由 ADR-003 v2 處理**
  - 建議在 VS 前走 `/architecture-decision` 建立 ADR-003 v2 或 ADR-006 明示 mutable manager 跨 Installer 傳遞的規範
- **關聯 ADR**：ADR-003

### TD-2026-009 GoogleSheet2Json API Key 硬編於 KGC 共用框架（跨專案議題）

- **位置**：`Assets/KahaGameCore/Editor/GoogleSheet2JsonSettingEditor.cs:12`（`GET_DATA_URL` 常數）
- **標記**：無（Sprint 7 A6-4 撰寫 `tech/google-sheet-export-tool-spec.md` 時識別）
- **描述**：Google Sheets API Key 以字串常數形式硬編於 KGC Editor 類別，所有使用 KGC 的專案共用同一 key。影響：key 無法輪替（洩漏需改 KGC 原始碼）、無法用環境變數注入（CI / 多工作室環境）、key 進版控歷史難以抹除。
- **影響**：
  - 安全面：key 在公開版控倉庫可見，若 repo 意外公開會暴露
  - 維運面：quota 超用時無法個別專案 throttling（所有專案共用 300 reads/min）
  - 跨專案：屬 KGC 層面議題，不只 ProjectDR
- **發現日**：2026-04-22（Sprint 7 A6-4 撰寫 spec 時審閱 .cs 識別）
- **預估處理時間**：6-10h（KGC 改 Setting 欄位加 `apiKey` + Secure Asset / 環境變數 fallback；各使用 KGC 的專案遷移設定；KGC 層面 ADR 撰寫）
- **狀態**：🔄 未處理
- **建議處置**：**延至 KGC 層面獨立 ADR 評估**
  - 當前 repo 為私有、API key 屬 Public Sheets read-only，洩漏風險可控
  - 改動範圍跨工作室所有專案，不適合在 ProjectDR Sprint 7 決策
  - 登記為待辦，待製作人決定是否開 KGC ADR
- **關聯 ADR**：無（待建 KGC 層面 ADR）
- **關聯 spec**：`tech/google-sheet-export-tool-spec.md` § 8.1

### TD-2026-010 Unity MCP 無法觸發 CustomEditor `Start Convert` 按鈕

- **位置**：`Assets/KahaGameCore/Editor/GoogleSheet2JsonSettingEditor.cs:48`（`GUILayout.Button("Start Convert")`）
- **標記**：無（Sprint 7 A6-4 撰寫 spec 時識別）
- **描述**：Unity MCP 工具集只能透過 `execute_menu_item` 觸發 `[MenuItem]`，無法點擊 CustomEditor 的 `GUILayout.Button`。因此 Sheets → JSON 匯出必須由製作人在 Unity Editor 有頭模式人工點擊 Inspector 按鈕，無法 `/away` 自動化。
- **影響**：
  - 離席模式無法自動跑資料匯出流程
  - A6-3（Start Convert 觸發）在 Sprint 7 一直壓在「需製作人在場」項目
  - 未來 CI pipeline（若導入）無法自動同步 Sheets 版本
- **發現日**：2026-04-22（Sprint 7 A6-3 被阻時識別，A6-4 正式登記）
- **預估處理時間**：2-4h（於 KGC Editor 加 `[MenuItem("Kaha Game Core/Convert All Sheets")]` 包 wrapper，搜尋 Resources 下所有 `GoogleSheet2JsonSetting` 或以 active selection 為目標，呼叫 `StartConvertTo()`）
- **狀態**：🔄 未處理
- **建議處置**：**可於 KGC 層面小改**
  - 改動範圍小，風險低
  - 會顯著改善離席體驗與 CI 能力
  - 但仍屬 KGC 共用框架，建議開 KGC 層面 ADR 或以 KGC 維護者身分直接加
- **關聯 ADR**：無（待建 KGC 層面 ADR）
- **關聯 spec**：`tech/google-sheet-export-tool-spec.md` § 5.5 / § 8.3

### TD-2026-008 VillageHubView 探索鎖定視覺狀態未實作（IT placeholder）

- **位置**：`Assets/Game/Scripts/Village/Navigation/View/VillageHubView.cs:281`
- **標記**：`// TODO(IT-placeholder): 正式版本改為切換 Button.interactable 或替換 sprite`
- **描述**：`SetExplorationButtonLocked(bool)` 方法當前只修改邏輯狀態，未切換視覺提示。玩家在守衛歸來後、拿劍前的狀態下，探索按鈕外觀與正常狀態相同（只在點擊時跳對話引導）。此為 Sprint 6 決策 6-8 明示保留的 IT 階段簡化，由 TBD-ui-NNN 承接。
- **影響**：
  - UX 影響：玩家可能不知道按鈕當前被鎖定
  - 有對話引導 fallback，不會永久卡關
- **發現日**：2026-04-22
- **預估處理時間**：1-2h（加一個 sprite 切換或 alpha 降階；UI 美術決策由製作人拍板）
- **狀態**：🔄 未處理
- **建議處置**：**延後至 VS 前**
  - 此屬 UI/UX 優化，不阻擋 ADR-002 退出
  - 建議與製作人確認 TBD 池是否已有對應條目；若無，登記 TBD-ui-<N>
- **關聯 ADR**：無（UI 決策屬設計範疇）

### TD-2026-011 VillageContext.GameDataAccess 尚未注入真實 delegate（Sprint 7 placeholder）

- **位置**：`Assets/Game/Scripts/Village/Core/Manager/VillageEntryPointInstallers.cs:79`（`gameDataAccess: null`）
- **標記**：`// TODO Sprint 8`（2026-04-22 D7 緊急修復時加入）
- **描述**：`VillageContext.GameDataAccess`（`GameDataQuery<IGameData>` delegate）在 Sprint 7 期間傳入 null，因所有 Installer 在此階段仍透過 constructor 直接注入 ConfigData（ADR-002 IT 例外），尚無任何 Installer 在 `Install(ctx)` 內呼叫 `ctx.GameDataAccess(id)` 做查詢。Sprint 7 D7 實機測試發現 `VillageContext.ctor` 的 null check 未配合此現況放寬，導致 `ArgumentNullException`。緊急修復已放寬 ctor null check（加 Sprint 8 TODO 說明）。Sprint 8 ADR-002 退出後，需補上真實 delegate 注入並恢復 null check。
- **影響**：
  - 目前 `ctx.GameDataAccess` 為 null，若任何 Installer 在 Sprint 7 階段誤呼叫此委派會於 runtime NRE
  - Sprint 8 前的 Installer 若需消費 `IGameData` tabular data，必須走 ADR-002 IT 階段例外（constructor 注入 ConfigData）
- **發現日**：2026-04-22（Sprint 7 D7 實機 playthrough）
- **預估處理時間**：2-4h（建 `GameDataQuery<IGameData>` delegate 指向 `GameStaticDataManager.GetGameData<IGameData>(id)`；確認各 Installer 已移除 IT 例外的 constructor 直注；恢復 VillageContext ctor null check；新增整合測試驗證 delegate 正常回傳資料）
- **狀態**：🔄 未處理（Sprint 8 ADR-002 退出後處理）
- **建議處置**：**Sprint 8 ADR-002 退出批次處理**
  - ADR-002 退出 Gate 時此項必須完成
  - 具體做法：在 VillageEntryPoint 初始化 `_gameStaticDataManager` 後，建立並傳入 delegate；VillageContext ctor 加回 null check；各 Installer 移除 IT placeholder 的 constructor 直注，改走 `ctx.GameDataAccess`
- **關聯 ADR**：ADR-001（資料治理契約）、ADR-002（IT 階段例外退出 Gate）、ADR-003（VillageContext 欄位契約 D2.1）

---

## 🔵 結構性問題（總覽）

上方 🔴🟡🟢 已逐條處置。以下為 validate-file-size.sh 會觸發的檔案盤點快照（2026-04-22）：

| 檔案 | 行數 | 護欄狀態 | 對應條目 |
|------|------|---------|---------|
| CharacterInteractionView.cs | 1463 | 🔴 強警示（> 1200）| TD-2026-001 |
| CollectionItemPanelView.cs | 760 | ⚠ 接近警示 | TD-2026-004 |
| CommissionManager.cs | 630 | ⚠ 接近警示 | TD-2026-005 |
| VillageEvents.cs | 588 | ⚠ 接近警示 | （51 個事件集中檔，結構合理，不登記單獨條目）|
| VillageEntryPoint.cs | 582 | ⚠ 接近警示 | TD-2026-002 |
| CollectiblePointState.cs | 549 | ✓ 通過 | — |
| ExplorationEntryPoint.cs | 314（Start 199 行）| 🟡 單方法超護欄 | TD-2026-003 |

**VillageEvents.cs 不登記為技術債的理由**：588 行為 51 個事件 class 的集中宣告檔，每個 class 小而語意清晰（無行為、純資料）；符合工作室慣例「事件集中檔」，不拆反而方便追蹤。若未來超 800 行再考慮按域拆檔（例：`VillageNavigationEvents.cs` / `VillageMainQuestEvents.cs`）。

---

## 本次掃描變動

### 新增（10 筆）

- TD-2026-001：CharacterInteractionView.cs 1463 行（🔴 高）
- TD-2026-002：VillageEntryPoint.cs 582 行未達 Sprint 7 B5 目標（🔴 高）
- TD-2026-003：ExplorationEntryPoint.Start() 199 行（🟡 中）
- TD-2026-004：CollectionItemPanelView.cs 760 行（🟡 中）
- TD-2026-005：CommissionManager.cs 630 行（🟡 中）
- TD-2026-006：HCGDialogueSetup.cs 硬編碼 HCG 對白（🟢 低）
- TD-2026-007：AffinityInstaller GiftManager 建構取捨（🟢 低）
- TD-2026-008：VillageHubView 探索鎖定視覺狀態未實作（🟢 低）
- TD-2026-009：GoogleSheet2Json API Key 硬編於 KGC 共用框架（🟢 低；KGC 跨專案議題；A6-4 識別）
- TD-2026-010：Unity MCP 無法觸發 CustomEditor `Start Convert` 按鈕（🟢 低；KGC 小改即可；A6-4 識別）

### 已解決（2 筆，2026-04-22）

- **TD-2026-003**：ExplorationEntryPoint.Start() 拆分完成（Sprint 7 收尾批次）
- **TD-2026-006**：HCGDialogueSetup 硬編碼對白登記為 ADR-002 A17 IT 階段例外（Sprint 7 收尾批次）

---

## 待製作人決定事項

Sprint 7 D5 打勾前，以下 🔴 項需製作人拍板處置方向：

1. **TD-2026-001 CharacterInteractionView 拆分**
   - 選項 a：本 Sprint 不處理，延後至 VS 階段前專案檢討
   - 選項 b：本 Sprint 登記後略過，進 VS 後再視擴充需求決定
   - 選項 c：本 Sprint 納入（成本高、需延 Sprint 7 收尾）
   - **dev-head 建議**：選 a 或 b（選 c 會延誤 Sprint 7 主目標 ADR-002 退出 Gate）

2. **TD-2026-002 VillageEntryPoint 582 行**
   - 選項 a：接受現狀為已知技術債，延後至 VS 前
   - 選項 b：本 Sprint 補做到 < 300 行
   - **dev-head 建議**：選 a（當前無護欄違反、無 SOLID 違規）

3. **TD-2026-003 ExplorationEntryPoint.Start() 199 行**
   - 選項 a：納入 Sprint 7 收尾批次（低成本、純重構）
   - 選項 b：延後至 VS 前
   - **dev-head 建議**：選 a（若 Sprint 7 還有 1 天以上）或選 b（若 Sprint 7 已收尾）

4. **TD-2026-006 HCGDialogueSetup 硬編碼**
   - 選項 a：本 Sprint A 類延伸，改走 IGameData（類似 A8 處理模式）
   - 選項 b：追加 ADR-002 條目登記為 IT 階段例外，VS 後處理
   - **dev-head 建議**：選 b（此類 placeholder 屬典型 IT 階段特徵，ADR-002 留存即可）

---

**版本**: v1.0 — 2026-04-22 首次建立（Sprint 7 D5 掃描）
