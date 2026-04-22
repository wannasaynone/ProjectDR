# Control Manifest — ProjectDR

> **Manifest Version**: 2026-04-22
> **引擎**: Unity（版本待補 — `projects/ProjectDR/tech/engine-reference/unity/VERSION.md` 尚未建立，請 dev-head 補建）
> **來源 ADR 數**: 4（由 `projects/ProjectDR/adrs/` 的 Accepted ADR 抽取）
> **產出指令**: `/create-control-manifest`
> **重建時機**: ADR 新增、狀態變更（Proposed → Accepted / Accepted → Superseded）、或手動觸發

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

- 所有遊戲數值必須外部化（Google Sheets → 匯出 JSON → Runtime 讀取），禁止寫死在程式碼中 ← ADR-001
- 進入 Runtime 查詢的 tabular data 必須實作 `KahaGameCore.GameData.IGameData` 介面（`int ID { get; }`）← ADR-001
- 透過 `GameStaticDataManager.Add<T>(handler)` 統一註冊載入 IGameData 實作 ← ADR-001
- 查詢統一走 `GetGameData<T>(int id)`，禁止繞過統一入口 ← ADR-001
- IT 階段暫時繞過 IGameData 契約的 DTO 類別必須在檔頭標註 `// EXEMPT: ADR-002 A-NN`（NN 為 ADR-002 清單編號）← ADR-002
- 所有 IVillageInstaller 實作必須是純 POCO（禁止繼承 MonoBehaviour）← ADR-003
- 所有 IVillageInstaller 必須實作 `Install(VillageContext ctx)` 與 `Uninstall()` 兩個方法 ← ADR-003
- Install 訂閱幾次，Uninstall 就要解除幾次（對稱原則）← ADR-003
- Uninstall 在未 Install 時必須安全執行（無例外）← ADR-003
- 每個 namespace 下的類別放置路徑依 ADR-004 規範對應（`ProjectDR.Village.<Module>`）← ADR-004
- 腳本放置路徑必須符合：`Assets/Game/Scripts/Village/<Module>/<Type>/` 格式 ← ADR-004

### 禁做（Forbidden）

- 禁止在程式碼中寫死遊戲數值（無論「暫時性」或「debug」用途）← ADR-001
- 禁止新 ConfigData 類別繞過 IGameData 契約（違反 ADR-001）；若屬 IT 階段必要例外，須有 ADR-002 對應條目 + 檔頭 `// EXEMPT: ADR-002 A-NN` 註解 ← ADR-001 / ADR-002
- 禁止手動編輯匯出的 JSON（JSON 是產物，不是源頭）← ADR-001
- 禁止在 IVillageInstaller 的 Install 完成前訂閱事件、在 Uninstall 完成後殘留訂閱 ← ADR-003
- 禁止 Installer 在建構子做依賴初始化以外的工作（副作用延遲到 Install 時執行）← ADR-003
- 禁止跨 Installer 依賴的填入時機錯亂（後安裝的 Installer 不可假設前一個尚未 Install）← ADR-003
- 禁止違反 ADR-004 的 21 模組 × 5 型別層命名規範 ← ADR-004
- 禁止在 View 層（`View/`）放置業務邏輯；View 只負責顯示與事件派發 ← ADR-004

### 量化護欄（Guardrail）

- VillageEntryPoint 行數目標 < 300 行（目前實際 859 行，屬已知技術債，進 VS 前須清理）← ADR-003
- 單一 Installer 類別行數建議 < 200 行 ← ADR-003
- 每個 Installer 的 Install 方法行數建議 < 80 行 ← ADR-003
- VS 前需完成 ADR-002 退出 Gate：[A][B][C][D] 四區塊所有項目全 ✅ ← ADR-002

---

## 分區規則（Per-System）

### 資料治理層（Data Governance）

> 範圍：`Assets/Game/Scripts/Village/*/Config/`、`Assets/Game/Scripts/Village/*/Data/`、`Assets/Resources/Data/**`

#### 必做

- 新 ConfigData 類別必須實作 `IGameData`（`int ID { get; }`）← ADR-001
- 語意字串主鍵類別需同時提供 `public int ID { get; }`（流水號）+ `public string Key { get; }`（語意字串）← ADR-001
- 反序列化測試必須加「實作 IGameData 斷言 + ID 非 0 斷言」← ADR-001
- IT 階段豁免的每筆 DTO 必須登記在 `adrs/ADR-002-*.md` 清單中，並於 VS 前完成轉換 ← ADR-002

#### 禁做

- 禁止 IT 階段豁免條目在進 VS 前未清理（每筆豁免需有 ADR-002 條目對應）← ADR-002
- 禁止繞過 `GameStaticDataManager` 直接存取資料（每個資料來源只有一個載入點）← ADR-001

#### 量化護欄

- VS 前 ADR-002 豁免清單（A 區）所有條目必須標記 ✅ ← ADR-002
- VS 前 ADR-002 B、C、D 區所有條目必須標記 ✅ ← ADR-002

---

### Village Composition Root（VillageEntryPoint / Installer 層）

> 範圍：`Assets/Game/Scripts/Village/Core/`、`Assets/Game/Scripts/Village/*/Manager/`（Installer 類別）

#### 必做

- VillageEntryPoint 作為 Composition Root，只負責建構 Installer、依序呼叫 Install/Uninstall，以及連接 Tick 驅動 ← ADR-003
- 每個功能域對應一個 IVillageInstaller 實作（目前 6 個：CoreStorageInstaller、ProductionInstaller、CharacterInstaller、CGInstaller、CommissionInstaller、ExplorationInstaller）← ADR-003
- Installer 安裝順序依賴必須嚴格遵守（ctx 欄位填入的前後依賴）← ADR-003
- 實作新 Installer 前必須確認其依賴的 ctx 欄位已由前一個 Installer 填入 ← ADR-003
- ctx 欄位以 `internal set` 限制，只有 Installer 可以寫入 ← ADR-003

#### 禁做

- 禁止 VillageEntryPoint 直接 new Manager（Manager 建構由 Installer 負責）← ADR-003
- 禁止 Installer 在 Install 以外的方法做訂閱 ← ADR-003
- 禁止 Installer 在 Uninstall 以外的方法做解訂 ← ADR-003
- 禁止新增 ctx 公開欄位而不標記 `internal set`（防止 Installer 外部污染）← ADR-003

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

### 禁做

- 禁止為了讓測試通過而修改測試（除非測試本身有 bug）← `.claude/rules/test-standards.md`
- 禁止 `Thread.Sleep` / `yield return new WaitForSeconds(n)` 作為等待機制 ← `.claude/rules/test-standards.md`
- 禁止無斷言的測試（只跑不檢查等同無用）← `.claude/rules/test-standards.md`
- 禁止依賴執行順序的測試（每測試自行 setup / teardown）← `.claude/rules/test-standards.md`

### 量化護欄

- 所有 IVillageInstaller 實作均需通過 T1~T4 四案例 ← ADR-003
- EditMode 整合測試放置於 `Assets/Tests/Editor/Village/Integration/` ← ADR-003

---

## 資料與內容

### 必做

- Google Sheets 是遊戲數值的唯一真相來源 ← `.claude/rules/data-files.md`
- Sheets 欄位名 = JSON key = C# property name（完全一致）← `.claude/rules/data-files.md` / ADR-001
- 每個 JSON 資料檔必須有對應的反序列化測試 ← `.claude/rules/data-files.md`
- 資料流程依序：Sheets 欄位設計 → IGameData 類別定義 → Sheets 匯出 JSON → Runtime 讀取 ← ADR-001

### 禁做

- 禁止程式碼中寫死遊戲數值 ← ADR-001 / `.claude/rules/data-files.md`
- 禁止手動編輯匯出的 JSON ← `.claude/rules/data-files.md`
- 禁止 Sheets 欄位與 C# class 名稱不一致 ← `.claude/rules/data-files.md`
- 禁止跳過 `/development-flow` Phase 1.5 資料源接入驗證 ← `.claude/rules/data-files.md`

---

## UI

### 必做

- UI 層只做顯示（訂閱狀態更新事件）與派發（將輸入轉為 command/event）← `.claude/rules/ui-code.md`
- 使用者可見字串必透過在地化系統（i18n key）← `.claude/rules/ui-code.md`
- Unity UI 使用 UGUI（Canvas + GameObject），不使用 UI Toolkit ← 工作室記憶

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

- 禁止使用 `UnityEngine.PlayerPrefs` 儲存任何遊戲狀態 ← `.claude/rules/gameplay-code.md`

---

## 引擎專屬

### Unity 專屬規則

> 注意：`projects/ProjectDR/tech/engine-reference/unity/VERSION.md` 尚未建立。
> dev-head 必須補建此檔後，本節引擎版本相關規則方可完整驗證。

#### 必做

- 寫任何引擎 API 前必須先讀 `projects/ProjectDR/tech/engine-reference/unity/VERSION.md`（目前缺檔，須補建）← CLAUDE.md § dev-agent 開工前置
- Unity UI 使用 TextMeshPro UGUI，不可用舊版 `UnityEngine.UI.Text` ← `.claude/rules/ui-code.md`
- 建立 UGUI 元素時必須設定 RectTransform（anchor / sizeDelta / offset）← 工作室記憶

#### 禁做

- 禁止使用 Godot `ConfigFile` 持久化（本規則順延自 PlayerPrefs 禁令）← `.claude/rules/gameplay-code.md`
- 禁止在 Inspector 綁定按鈕事件 ← `.claude/rules/ui-code.md`

---

## 來源 ADR 索引

| ADR | 標題 | 狀態 | 本 Manifest 涉及段落 |
|-----|------|------|---------------------|
| ADR-001 | Data Governance Contract（IGameData 契約） | Accepted | 全域通則 / 資料治理層 / 資料與內容 / 測試規則 |
| ADR-002 | IT Stage Exemption Exit（IT 階段退出 Gate） | Accepted | 全域通則 / 資料治理層 / 量化護欄 |
| ADR-003 | Village Composition Root Contract（IVillageInstaller 契約） | Accepted | 全域通則 / Village Composition Root / 測試規則 / 量化護欄 |
| ADR-004 | Script Organization Structure Contract（腳本組織結構） | Accepted | 全域通則 / 腳本組織結構 |

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
