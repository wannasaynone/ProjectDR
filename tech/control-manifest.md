# Control Manifest — ProjectDR

> **Manifest Version**: 2026-04-22-v2（v1.1）
> **引擎**: Unity 6.0.x（版本釘選檔待補：`projects/ProjectDR/tech/engine-reference/unity/VERSION.md` 尚未建立，請 dev-head 於 Sprint 9 啟動前補建）
> **來源 ADR 數**: 4（ADR-001 / ADR-002 / ADR-003 / ADR-004 全部 Accepted；ADR-002 於 2026-04-22 轉 Accepted (Executed)）
> **產出指令**: `/create-control-manifest`（本次重建為 Sprint 8 Wave 2.6b 手動執行，由 dev-head 依 ADR 全掃產出）
> **重建時機**: ADR 新增、狀態變更（Proposed → Accepted / Accepted → Executed / Superseded）、或手動觸發

---

## 什麼是 Control Manifest

平面化的技術規則清單，供 dev-agent 實作時快速查閱。**不是設計文件**，也**不取代 ADR** —— ADR 解釋「為何」，Manifest 回答「必做什麼 / 禁做什麼 / 量化護欄」。

**閱讀方式**：
- 實作某系統前，先讀「全域通則」與對應 System 區段
- 每條規則後的 `← ADR-NNNN` 是來源，有疑惑時翻該 ADR
- 若遇到 Manifest 未涵蓋的情境，不得自行延伸 —— 回報 dev-head 增訂 ADR 後再重建 Manifest

---

## 全域通則（Global Rules）

**所有系統必須遵守**。

### 必做（Required）

#### 資料治理（ADR-001 + ADR-002 v1.7 + Sprint 8 Wave 2 新增）

- 所有遊戲數值必須外部化（Google Sheets → Convert 匯出 `.txt` → Runtime 讀取），禁止寫死在程式碼中 ← ADR-001
- 進入 Runtime 查詢的 tabular data 必須實作 `KahaGameCore.GameData.IGameData` 介面（`int ID { get; }`）← ADR-001
- **Config `.txt` 必為純陣列 IGameData 格式**（`[{"id":1,...},{"id":2,...}]`），**禁止包裹物件**（`{schema_version, note, xxx_array:[...]}` 已於 Sprint 8 全面廢除）← ADR-001 + ADR-002 v1.7 + Sprint 8 Wave 2 spec
- **Runtime 反序列化統一使用 JsonFx 靜態 API**：`JsonReader.Deserialize<T[]>(json)`（與 KGC 匯出工具 JsonFx 序列化一致）；測試檔也必須使用靜態 API（`new JsonReader()` 模式已廢棄）← ADR-001 + dev-log 2026-04-22-20
- 透過 `GameStaticDataManager.Add<T>(handler)` 統一註冊載入 IGameData 實作 ← ADR-001
- **查詢統一走 `GameStaticDataManager.GetGameData<T>(int id)`** 或透過 `VillageContext.GameDataAccess` delegate（ADR-003 D2.1 VillageContext 注入），**禁止繞過統一入口** ← ADR-001 + ADR-003
- **資料檔副檔名使用 `.txt`**（Unity TextAsset 慣例），不使用 `.json`（製作人 2026-04-22 拍板）← Sprint 8 Wave 2 spec + dev-log 21
- IT 階段暫時繞過 IGameData 契約的 DTO 類別必須在檔頭標註 `// EXEMPT: ADR-002 A-NN`（NN 為 ADR-002 清單編號）；Sprint 8 Wave 2.6b Full Exit 後僅保留 A15 + A17 為原規劃豁免 ← ADR-002 v1.7

#### Installer 與 Composition Root（ADR-003）

- 所有 IVillageInstaller 實作必須是純 POCO（禁止繼承 MonoBehaviour）← ADR-003
- 所有 IVillageInstaller 必須實作 `Install(VillageContext ctx)` 與 `Uninstall()` 兩個方法 ← ADR-003
- Install 訂閱幾次，Uninstall 就要解除幾次（對稱原則）← ADR-003
- Uninstall 在未 Install 時必須安全執行（無例外）← ADR-003

#### Script 組織（ADR-004）

- 每個 namespace 下的類別放置路徑依 ADR-004 規範對應（`ProjectDR.Village.<Module>`）← ADR-004
- 腳本放置路徑必須符合：`Assets/Game/Scripts/Village/<Module>/<Type>/` 格式 ← ADR-004

### 禁做（Forbidden）

- 禁止在程式碼中寫死遊戲數值（無論「暫時性」或「debug」用途）← ADR-001
- 禁止新 ConfigData 類別繞過 IGameData 契約（違反 ADR-001）；若屬 IT 階段必要例外，須有 ADR-002 對應條目 + 檔頭 `// EXEMPT: ADR-002 A-NN` 註解 ← ADR-001 / ADR-002
- 禁止手動編輯匯出的 `.txt`（Sheets 是唯一來源，手動編輯會被下次 Convert 覆蓋）← ADR-001 + `.claude/rules/data-files.md` + Sprint 8 Wave 2 spec
- **禁止 ADR-002 Full Exit 後新增繞過 IGameData 的 DTO**（VS 階段後此行為等同 ADR-001 違反，不再屬於 IT 例外）← ADR-001 + ADR-002 v1.7
- 禁止在 IVillageInstaller 的 Install 完成前訂閱事件、在 Uninstall 完成後殘留訂閱 ← ADR-003
- 禁止 Installer 在建構子做依賴初始化以外的工作（副作用延遲到 Install 時執行）← ADR-003
- 禁止跨 Installer 依賴的填入時機錯亂（後安裝的 Installer 不可假設前一個尚未 Install）← ADR-003
- 禁止違反 ADR-004 的 21 模組 × 5 型別層命名規範 ← ADR-004
- 禁止在 View 層（`View/`）放置業務邏輯；View 只負責顯示與事件派發 ← ADR-004

### 量化護欄（Guardrail）

- **VillageEntryPoint 行數目標 < 600 行**（2026-04-22 TR-arch-007 調整，原 < 300）；目前實際 582 行（partial class 拆為 3 檔：VillageEntryPoint + VillageEntryPointFunctionPrefabs + VillageEntryPointInstallers，合計行數適用護欄）← ADR-003 v1.1
- 單一 Installer 類別行數建議 < 200 行 ← ADR-003
- 每個 Installer 的 Install 方法行數建議 < 80 行 ← ADR-003
- **ADR-002 退出 Gate 已 PASS**（2026-04-22 Sprint 8 Wave 2.6b）：[A][B][C][D] 四區塊全 ✅，VS 階段啟動前置條件達標 ← ADR-002 v1.7
- `validate-file-size.sh` hook 警示閾值：一般警示 > 800 行 / 強警示 > 1200 行；ADR-003 < 600 的限制更嚴 ← TR-arch-007

---

## 分區規則（Per-System）

### 資料治理層（Data Governance）

> 範圍：`Assets/Game/Scripts/Village/<Module>/Data/`、`Assets/Game/Resources/Config/**`（`.txt` 產物）、Google Sheets 22 分頁

#### 必做

- 新 ConfigData 類別必須實作 `IGameData`（`int ID { get; }`）← ADR-001
- 語意字串主鍵類別需同時提供 `public int ID { get; }`（流水號）+ `public string Key { get; }`（語意字串）← ADR-001
- 反序列化測試必須加「實作 IGameData 斷言 + ID 非 0 斷言」← ADR-001
- **Sheets 分頁 1:1 對應 IGameData DTO 與 `.txt` 檔**（命名慣例：Sheets PascalCase 分頁名 = `.txt` 檔名 = DTO 所對應 `<Module>/Data/<Module>Data.cs`）← Sprint 8 Wave 2 spec
- **主表 / 子表 / metadata 分頁命名慣例**：
  - 主表用複數 PascalCase（例：`CharacterIntros`、`MainQuests`、`CommissionRecipes`）
  - 子表用 `<Master>Lines` / `<Master>Options` / `<Master>Answers` / `<Master>Unlocks` / `<Master>Requirements`（例：`CharacterIntroLines`、`MainQuestUnlocks`、`StorageExpansionRequirements`）
  - metadata 用語意描述（例：`Personalities`、`CharacterProfiles`、`PersonalityAffinityRules`）
  - ← Sprint 8 Wave 2 spec Q3/Q4/Q7 拍板
- **FK 命名慣例**：同系統內子表 FK 欄位以 `<父表單數>_id` 命名（例：`CharacterIntroLines.intro_id`）；跨系統綁定用自然語言語意值（例：`MainQuests.completion_condition_value`），不強制命名同構 ← Sprint 8 Wave 2 spec Q2
- **Sheets 每分頁第一欄必須是 `id`**（int 流水號，從 1 連續，分頁獨立編號，不跳號、不重用已刪除 id）← Sprint 8 Wave 2 spec Q5
- IT 階段豁免的每筆 DTO 必須登記在 `adrs/ADR-002-*.md` 清單中；Sprint 8 Wave 2.6b Full Exit 後僅 A15/A17 為原規劃保留項（VS 早期重寫對象）← ADR-002 v1.7

#### 禁做

- **禁止手動編輯 `Assets/Game/Resources/Config/*.txt`** — Sheets 是唯一來源，手動編輯會被下次 Convert 覆蓋 ← ADR-001 + `.claude/rules/data-files.md` + Sprint 8 Wave 2 spec
- 禁止 `.txt` 產物為包裹物件（`{schema_version, xxx_array:[...]}`） —— Sprint 8 Wave 2 全面廢除，必為純陣列 ← ADR-002 v1.7
- 禁止繞過 `GameStaticDataManager` 直接存取資料（每個資料來源只有一個載入點）← ADR-001
- 禁止 IT 階段豁免條目在進 VS 前未清理（ADR-002 v1.7 已 Full Exit，A15/A17 外不得新增新豁免）← ADR-002 v1.7
- 禁止使用 `new JsonReader().Deserialize<T[]>(json)` 模式（已改為靜態 API `JsonReader.Deserialize<T[]>(json)`）← dev-log 2026-04-22-20

#### 量化護欄

- **Sprint 8 Wave 2.6b Full Exit**：[A] 17/17、[B] 4/4、[C] 7/7、[D] 6/6 全 ✅；VS 階段啟動前置條件達標 ← ADR-002 v1.7
- 目前 Sheets 分頁數：22（15 活躍主表 + 7 子表/metadata，對應 22 個 IGameData DTO + 22 個 `.txt`） ← Sprint 8 Wave 2 spec
- **禁止新增分頁 / DTO 繞過此規則**（若需新增，先走 `/architecture-decision` 補 ADR，再建分頁 / DTO）← ADR-001 + ADR-002 v1.7

---

### Village Composition Root（VillageEntryPoint / Installer 層）

> 範圍：`Assets/Game/Scripts/Village/Core/`、`Assets/Game/Scripts/Village/*/Manager/`（Installer 類別）

#### 必做

- VillageEntryPoint 作為 Composition Root，只負責建構 Installer、依序呼叫 Install/Uninstall，以及連接 Tick 驅動 ← ADR-003
- 每個功能域對應一個 IVillageInstaller 實作（目前 6 個：CoreStorageInstaller、ProductionInstaller、CharacterInstaller、CGInstaller、CommissionInstaller、ExplorationInstaller；以及 AffinityInstaller / ProgressionInstaller / DialogueFlowInstaller 等擴充版）← ADR-003 D5
- Installer 安裝順序依賴必須嚴格遵守（ctx 欄位填入的前後依賴）← ADR-003
- 實作新 Installer 前必須確認其依賴的 ctx 欄位已由前一個 Installer 填入 ← ADR-003
- ctx 欄位以 `internal set` 限制，只有 Installer 可以寫入 ← ADR-003
- **VillageContext.GameDataAccess delegate 必須指向真實 GameStaticDataManager**（Sprint 8 Wave 2.6a D4 接線已完成：`(int id) => gameStaticDataManager.GetGameData<IGameData>(id)`）← ADR-003 D2.1 + Sprint 8 Wave 2.6a

#### 禁做

- 禁止 VillageEntryPoint 直接 new Manager（Manager 建構由 Installer 負責）← ADR-003
- 禁止 Installer 在 Install 以外的方法做訂閱 ← ADR-003
- 禁止 Installer 在 Uninstall 以外的方法做解訂 ← ADR-003
- 禁止新增 ctx 公開欄位而不標記 `internal set`（防止 Installer 外部污染）← ADR-003
- **禁止 `ctx.GameDataAccess = null`**（Wave 2.6a 恢復嚴格 null check，Sprint 7 D7 的臨時放寬已移除）← ADR-003 D2.1 + Sprint 8 Wave 2.6a

#### 量化護欄

- VillageContext 欄位數量上限參考：目前 9 欄，擴充前須評估是否需要 sub-context 拆分 ← ADR-003
- Install 調用順序固定為：CoreStorageInstaller(#1) → ProductionInstaller(#2) → CharacterInstaller(#3) → CGInstaller(#4) → CommissionInstaller(#5) → ExplorationInstaller(#6) ← ADR-003

---

### 腳本組織結構（Script Organization）

> 範圍：`Assets/Game/Scripts/Village/` 下所有檔案

#### 必做

- 每個 Village 模組（共 21 個）的檔案依型別放置於對應子目錄：`Config/`、`Data/`、`Event/`、`Manager/`、`View/` ← ADR-004
- namespace 格式固定為 `ProjectDR.Village.<Module>`（Module 為 PascalCase）← ADR-004
- 新增腳本前先確認放置目錄是否符合 21 模組規範（Navigation、Core、Backpack、Storage、Production、Character、Affinity、Commission、Exploration、CG、Guard、MainQuest、RedDot、Time、UI、Node、Quest、Dialogue、Collectible、Gate、Opening）← ADR-004
- Exploration 因子模組複雜，namespace 特許三層 `ProjectDR.Village.Exploration.<SubModule>`（唯一例外）← TR-arch-003

#### 禁做

- 禁止把多個功能域的類別混放在同一目錄 ← ADR-004
- 禁止在 Manager/ 放 View 類別、或在 View/ 放 Manager 類別 ← ADR-004
- 禁止使用 ADR-004 以外的自訂目錄名稱（除非建立新 ADR 更新模組清單）← ADR-004

#### 量化護欄

- 21 個模組（固定清單），新增模組須先更新 ADR-004 ← ADR-004
- 5 個型別層（固定清單）：Config / Data / Event / Manager / View ← ADR-004

---

## 測試規則

### 必做

- 每個 Logic / Manager 類別必附單元測試 ← `.claude/rules/test-standards.md`
- 測試先行（TDD）：先寫測試，再寫實作讓測試通過 ← CLAUDE.md
- 每個 bug 修復必附回歸測試 ← `.claude/rules/test-standards.md`
- ADR-003 要求的 4 個測試案例（T1~T4）適用於每個 IVillageInstaller：Install(null) 拋例外、Install/Uninstall 後 EventBus 訂閱清除、Uninstall 未 Install 時安全執行、重入 Install 不洩漏 ← ADR-003
- 新 IGameData 實作的測試必須含「實作 IGameData 斷言 + ID 非 0 斷言」← ADR-001
- **反序列化測試必用 JsonFx 靜態 API**：`JsonReader.Deserialize<T[]>(json)`，禁用 `new JsonReader()` 建構子模式 ← Sprint 8 Wave 2.5 dev-log 20

### 禁做

- 禁止為了讓測試通過而修改測試（除非測試本身有 bug）← `.claude/rules/test-standards.md`
- 禁止 `Thread.Sleep` / `yield return new WaitForSeconds(n)` 作為等待機制 ← `.claude/rules/test-standards.md`
- 禁止無斷言的測試（只跑不檢查等同無用）← `.claude/rules/test-standards.md`
- 禁止依賴執行順序的測試（每測試自行 setup / teardown）← `.claude/rules/test-standards.md`

### 量化護欄

- **測試基線 1433/1433 PASS**（2026-04-22 Sprint 8 Wave 2.6a 維持，Wave 2.6b 無程式碼變動）← 測試報告
- 所有 IVillageInstaller 實作均需通過 T1~T4 四案例 ← ADR-003
- EditMode 整合測試放置於 `Assets/Tests/Editor/Village/Integration/` ← ADR-003
- 測試目錄須鏡像 runtime 結構（`Assets/Tests/Game/Village/<Module>/<Type>/`；執行延至 VS 階段）← TR-arch-004

---

## 資料與內容

### 必做

- **Google Sheets 是遊戲數值的唯一真相來源**（Sheets ID: `1WjvMHTtEZ-JIRE1hmKz0pQMfI78hVI9jXC7ZLKEpLoo`，22 分頁）← `.claude/rules/data-files.md`
- Sheets 欄位名 = JSON key = C# property name（完全一致，snake_case 貫穿整條鏈）← `.claude/rules/data-files.md` / ADR-001
- 每個 `.txt` 資料檔必須有對應的反序列化測試 ← `.claude/rules/data-files.md`
- 資料流程依序：Sheets 欄位設計 → IGameData 類別定義 → KGC `Start Convert` 匯出 `.txt` → Runtime 讀取 ← ADR-001

### 禁做

- 禁止程式碼中寫死遊戲數值 ← ADR-001 / `.claude/rules/data-files.md`
- **禁止手動編輯匯出的 `.txt`**（Sheets 是唯一來源）← `.claude/rules/data-files.md` + Sprint 8 Wave 2 spec
- 禁止 Sheets 欄位與 C# class 名稱不一致 ← `.claude/rules/data-files.md`
- 禁止跳過 `/development-flow` Phase 1.5 資料源接入驗證 ← `.claude/rules/data-files.md`

---

## UI

### 必做

- UI 層只做顯示（訂閱狀態更新事件）與派發（將輸入轉為 command/event）← `.claude/rules/ui-code.md`
- 使用者可見字串必透過在地化系統（i18n key）← `.claude/rules/ui-code.md`
- Unity UI 使用 UGUI（Canvas + GameObject），不使用 UI Toolkit ← 工作室記憶
- MCP 建立 UGUI 必須逐一設定 RectTransform（anchor / sizeDelta / offset；不可依賴預設 100x100 居中）← 工作室記憶

### 禁做

- 禁止 UI 直接讀寫遊戲核心狀態 ← `.claude/rules/ui-code.md`
- 禁止 UI 呼叫 `PlayerPrefs` 或 `File.Write` ← `.claude/rules/ui-code.md`
- 禁止 UI 層啟動 coroutine / async 做業務邏輯 ← `.claude/rules/ui-code.md`
- 禁止 UI 中 new 業務物件（透過注入或 service locator）← `.claude/rules/ui-code.md`
- 禁止 Inspector 綁定按鈕事件（使用 `Button.onClick.AddListener(...)` 代替）← `.claude/rules/ui-code.md`

---

## 持久化與存檔

### 必做

- 跨 session 的狀態在存檔機制定案前，一律改為 session 內記憶體旗標（`HashSet<string>`、private field），每次進遊戲重置 ← `.claude/rules/gameplay-code.md`

### 禁做

- 禁止使用 `UnityEngine.PlayerPrefs` 儲存任何遊戲狀態 ← `.claude/rules/gameplay-code.md` + TR-save-001
- 已知違規：`CharacterIntroCGPlayer` 使用 PlayerPrefs 作為「只播一次」旗標，待存檔系統設計後補治理 ADR ← TR-save-001 notes

---

## 引擎專屬

### Unity 專屬規則

> 注意：`projects/ProjectDR/tech/engine-reference/unity/VERSION.md` 尚未建立（工作室級 2026-04-22 新增規則）。
> dev-head 於 Sprint 9 啟動前補建此檔後，本節引擎版本相關規則方可完整驗證。

#### 必做

- 寫任何引擎 API 前必須先讀 `projects/ProjectDR/tech/engine-reference/unity/VERSION.md`（目前缺檔，須補建）← CLAUDE.md § dev-agent 開工前置
- Unity UI 使用 TextMeshPro UGUI，不可用舊版 `UnityEngine.UI.Text` ← `.claude/rules/ui-code.md`
- 建立 UGUI 元素時必須設定 RectTransform（anchor / sizeDelta / offset）← 工作室記憶
- MCP 建立 Prefab 後必須刪除場景上的殘留物件 ← 工作室記憶

#### 禁做

- 禁止使用 Godot `ConfigFile` 持久化（本規則順延自 PlayerPrefs 禁令）← `.claude/rules/gameplay-code.md`
- 禁止在 Inspector 綁定按鈕事件 ← `.claude/rules/ui-code.md`

---

## 來源 ADR 索引

| ADR | 標題 | 狀態 | 本 Manifest 涉及段落 |
|-----|------|------|---------------------|
| ADR-001 | Data Governance Contract（IGameData 契約） | Accepted | 全域通則 / 資料治理層 / 資料與內容 / 測試規則 |
| ADR-002 | IT Stage Exemption Exit（IT 階段退出 Gate） | **Accepted (Executed)** | 全域通則 / 資料治理層 / 量化護欄（Full Exit 2026-04-22） |
| ADR-003 | Village Composition Root Contract（IVillageInstaller 契約） | Accepted | 全域通則 / Village Composition Root / 測試規則 / 量化護欄 |
| ADR-004 | Script Organization Structure Contract（腳本組織結構） | Accepted | 全域通則 / 腳本組織結構 |

---

## TR-ID 覆蓋率

| TR-ID | 摘要 | 治理 ADR | 狀態 |
|-------|------|---------|------|
| TR-data-001 | 遊戲數值外部化 | ADR-001 | ✅ |
| TR-data-002 | Runtime 查詢必實作 IGameData | ADR-001 | ✅ |
| TR-data-003 | IT 階段例外退出契約 | ADR-001 + ADR-002 | ✅（Full Exit 2026-04-22） |
| TR-save-001 | 禁止 PlayerPrefs | （空，待後續 ADR 治理） | ⚠️ 已登記無治理 ADR |
| TR-arch-001 | Script 組織模組邊界 | ADR-004 | ✅ |
| TR-arch-002 | 5 層型別資料夾 | ADR-004 | ✅ |
| TR-arch-003 | Namespace 規則 | ADR-004 | ✅ |
| TR-arch-004 | 測試目錄鏡像 | ADR-004 | ✅ |
| TR-arch-005 | IVillageInstaller 契約 | ADR-003 | ✅ |
| TR-arch-006 | VillageContext 依注 | ADR-003 | ✅ |
| TR-arch-007 | VillageEntryPoint 行數護欄 | ADR-003 | ✅ |
| TR-arch-008 | 事件訂閱對稱性 | ADR-003 | ✅ |

**覆蓋率**：11/12 = **91.7%**（TR-save-001 為已知待治理項，存檔系統設計後補 ADR）

---

## 使用此 Manifest 的規則

1. **dev-agent 實作前必讀**（`/development-flow` Phase 1）
2. **每個 Story / Sprint 項目可釘選 `Manifest Version`**，若當前 Manifest Version 較新需先重審
3. **違反條目時必須停下，回報 dev-head**：是該修實作、還是該改 ADR
4. **不得手動編輯此檔**（除了人工附加的備註區段）：任何規則變動都應透過 ADR → 重建 Manifest
5. **ADR 回溯建立時**：先建 ADR，再重建 Manifest；順序不可反

---

## 版本更新紀錄

| 版本 | 日期 | 說明 |
|------|------|------|
| v1.0 | 2026-04-22 | 初版 —— 由 4 條 Accepted ADR（ADR-001～004）抽取；首次建立，共 46 條規則（必做 22 / 禁做 20 / 護欄 4）|
| v1.1 | 2026-04-22 | **Sprint 8 Wave 2.6b 重建**：ADR-002 轉 Accepted (Executed)；新增 7 條 Sprint 8 資料層條目（Config `.txt` 純陣列格式、JsonFx 靜態 API、Sheets 22 分頁命名慣例、FK 命名、手動編輯禁令等）；修訂 VillageEntryPoint 量化護欄從 < 300 調整為 < 600（TR-arch-007 2026-04-22）；TR 覆蓋率段落新增；共計約 50 條規則（必做 25 / 禁做 21 / 護欄 4）|
