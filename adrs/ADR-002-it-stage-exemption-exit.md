# ADR-002: IT 階段例外退出清單 — 進 VS 前的資料治理清理 Gate

> **狀態**: Accepted
> **提出日期**: 2026-04-21
> **最近更新**: 2026-04-22（退出時點鎖定為候選 C 獨立 Gate Sprint；[A] 區塊 16 個 ConfigData 改造路徑以 ADR-004 新結構為準）
> **提出者**: dev-head（配合 ADR-001 + 製作人 2026-04-21 決策）
> **引擎**: Unity 6.0.x（ProjectDR）
> **取代**: —
> **被取代**: —

---

## Context（脈絡）

### 為何 IT 階段允許例外

ProjectDR 在 Sprint 1~6 期間受三個現實限制，資料治理無法一次到位：

1. **Google Sheets 未建立**：Sprint 1~5 期間 Sheets 尚未登記，2026-04-21 今日才首次建立 8 個分頁
2. **匯出工具鏈未全**：`GoogleSheet2JsonSetting` 匯出管線尚未串接到本專案
3. **速度優先**：IT 階段目標為「最小成本驗證核心玩法是否好玩」，資料層基礎設施工時不值得擋住玩法驗證

因此 `doc/tech/development-workflow.md` §1.5 的「IT 階段例外」條款允許開發暫時繞過 Sheets → IGameData 資料流，直接寫 JSON + 自建 DTO。

### 為何必須清除

進 VS 階段後三件事讓例外**不可持續**：

1. **設計師需要熱改 Sheets**：VS 期間設計師需要調整好感度曲線、煉金配方、CG 門檻等數值，熱改能力是 VS 驗收「外部玩家能否理解且覺得好玩」的必要條件
2. **資料變動量大**：VS 階段要加入真實文本（角色發問 280 題、招呼語 280 句、閒聊 80 主題池、CG 門檻細節等），若仍走手動 JSON 維護會被內容量淹沒
3. **反序列化一致性**：繞過 IGameData 的 DTO 各自重實作查詢結構，VS 階段多系統交互時無法統一偵錯

### 為何需要專屬 ADR（而非僅在 ADR-001 內列清單）

IT 階段例外的「退出」本身是一個獨立工作項目，具備以下特性：

- **可執行檢查清單**：有明確的「完成 / 未完成」二態
- **有 Gate 語意**：未過不可啟動 VS 開發
- **有製作人決策需求**：退出時點、例外範圍需製作人拍板
- **有重建流程**：工作項目分類、工時預估、風險評估

將此清單獨立為 ADR-002 的好處：未來若需要再補類似例外（例：VS 階段某系統性能未達標暫用 mock），可比照 ADR-002 模式建新 ADR，不污染 ADR-001 的通用契約宣告。

---

## Decision（決策）

**IT 階段例外的退出必須通過以下可執行 Gate：所有檢查項 ✅ 才算 IT 階段資料治理清除完成，未過不可啟動 VS 開發。**

### 退出時點定義（2026-04-22 製作人拍板）

**退出時點 = 獨立 Gate Sprint 的完成時點**（候選 C，製作人 2026-04-22 決策）

- 專門開一個 Sprint 清資料層 + 結構債 + ADR/TR 治理基礎設施
- Gate 通過即結束該 Sprint 並進 VS 開發
- **此 Sprint 不混其他功能工作**（避免 IT 例外清理被其他工作項目稀釋）

**此 Sprint 的命名暫稱**：`sprint-gate-vs-readiness`（具體編號 Sprint 7 或其他由製作人於啟動時拍板）

### 撤除的候選（歷史記錄）

2026-04-21 初版曾提供三個候選，2026-04-22 製作人明示選候選 C，其餘撤除：

- ~~候選 A — Sprint 7 啟動時合併資料清理~~：**撤除**（混工作導致清理被稀釋）
- ~~候選 B — Sprint 8 啟動時延後~~：**撤除**（時間不明確）
- ✅ **候選 C — 獨立 Gate Sprint**：採納

### 退出前必做清單（Exit Checklist）

本清單分為四大區塊：[A] ConfigData 類別盤點、[B] Sheets 對齊、[C] 基礎設施、[D] ADR/TR 綁定。

---

## [A] ConfigData 類別盤點（16 個類別）

每個條目註明：檔名、當前狀態、退出動作、預估工時。

| # | 檔名 | 當前狀態 | 退出動作 | 預估工時 |
|---|------|---------|---------|---------|
| A01 | `AffinityConfigData.cs` | 繞過 IGameData | 改為實作 IGameData（數值型資料，主鍵可改 int ID）| 2h |
| A02 | `CGSceneConfigData.cs` | 繞過 IGameData | 改為實作 IGameData + 雙欄位（int ID + string Key）| 3h |
| A03 | `CharacterIntroConfigData.cs` | 繞過 IGameData（string intro_id）| 改為實作 IGameData + 雙欄位 | 3h |
| A04 | `CharacterQuestionsConfigData.cs` | 繞過 IGameData（string question_id）| 改為實作 IGameData + 雙欄位；或 IT 階段後重寫時豁免 | 4h |
| A05 | `CombatConfigData.cs` | 繞過 IGameData | 改為實作 IGameData（數值型資料）| 2h |
| A06 | `CommissionRecipesConfigData.cs` | 繞過 IGameData（string recipe_id）| 改為實作 IGameData + 雙欄位 | 3h |
| A07 | `GreetingConfigData.cs` | 繞過 IGameData | 改為實作 IGameData + 雙欄位 | 3h |
| A08 | ~~`GuardFirstMeetDialogueConfigData.cs`~~ | ✅ **已併入 NodeDialogueConfig**（2026-04-22）| node_id="guard_first_meet" 4 筆 line 加入 node-dialogue-config.json（id 32~35）；NodeDialogueController 擴充 `TryPlayFirstMeetDialogueIfNotTriggered` 首次觸發邏輯；VillageEntryPoint 消費點改走 NodeDialogueController；`NodeDialogueCompletedEvent { NodeId="guard_first_meet" }` 觸發發劍 + `ExplorationGateReopenedEvent`；GuardFirstMeetDialogueConfigData.cs + guard-first-meet-dialogue-config.json + Guard/Data 目錄刪除；整合測試重構不依賴舊 Config |
| A09 | `GuardReturnConfigData.cs` | 繞過 IGameData；決策 6-12 後標為廢棄 | **確認豁免**（決策 6-12 已廢棄）或刪除 | 1h |
| A10 | `IdleChatConfigData.cs` | 繞過 IGameData（string topic_id）| 改為實作 IGameData + 雙欄位 | 3h |
| A11 | `InitialResourcesConfigData.cs` | 繞過 IGameData；決策 6-6 後「完全不發初始物資」| **確認豁免**（部分 Deprecated 條目不需改造）或精簡後改造 | 2h |
| A12 | `MainQuestConfigData.cs` | 繞過 IGameData（string quest_id）| 改為實作 IGameData + 雙欄位 | 3h |
| A13 | `MonsterConfigData.cs` | 繞過 IGameData | 改為實作 IGameData（數值型資料）| 2h |
| A14 | `NodeDialogueConfigData.cs` | 繞過 IGameData（string node_id）| 改為實作 IGameData + 雙欄位 | 4h |
| A15 | `PlayerQuestionsConfigData.cs` | 繞過 IGameData（string question_id） + placeholder 待重寫 | 改為實作 IGameData + 雙欄位；或 IT 階段後重寫時豁免 | 4h |
| A16 | `StorageExpansionConfigData.cs` | 繞過 IGameData | 改為實作 IGameData（數值型資料，level 當 ID）| 2h |
| A17 | `HCGDialogueSetup.cs` | 硬編碼 HCG 對白（違反遊戲數值外部化原則 + 違反 UI 規則「禁止硬編字串」）| 🟡 豁免（IT 階段 placeholder；VS 階段會重寫 HCG 對白系統，連同外部化一併改造）| 1h |

**工時合計**：約 40~45 小時（一個 Sprint 內可處理）

**策略**：A08/A09/A11 由於決策 6-12 / 6-6 已部分廢棄，可與製作人確認後走「豁免 / 刪除」捷徑；其餘 13 個類別走標準改造。

> **2026-04-22 路徑更新**：A01~A16 改造後的檔案路徑以 ADR-004 新結構為準（歸各 `<Module>/Data/` 子資料夾），不再留於 `Village/` 根目錄；詳見 `projects/ProjectDR/tech/control-manifest.md`（Sprint 7 重建後）。Sprint 7 執行時 E 類（結構搬移）與 [A] 區塊（ConfigData 改 IGameData）交叉批次同步進行。

**每個改造項目的 Sub-checklist**：

- [ ] C# class 加 `: IGameData`
- [ ] 加 `public int ID { get; }` 屬性
- [ ] 語意字串主鍵類別加 `public string Key { get; }` 屬性
- [ ] 更新 JSON 結構（增加 `id` 欄位）
- [ ] 更新 Sheets 對應分頁（增加 `id` 欄位）
- [ ] 更新反序列化測試（加「實作 IGameData」斷言）
- [ ] 透過 `GameStaticDataManager.Add<T>(handler)` 載入
- [ ] 查詢點改用 `GetGameData<T>(id)`（既有 `Dictionary<string, T>` 刪除）
- [ ] VillageEntryPoint / 相關 Entry Point 組裝邏輯更新
- [ ] 檔頭移除 `// EXEMPT: ADR-002` 註解（若曾標註）

---

## [B] Google Sheets 對齊清單

2026-04-21 今日已建立 8 個分頁。對齊狀態表：

| # | Sheets 分頁名 | 對應 ConfigData | 對齊狀態 | 退出動作 |
|---|--------------|----------------|---------|---------|
| B01 | （待製作人確認 8 個分頁名）| — | 需逐一對應 | 補欄位 `id` / 對齊 property |
| B02 | — | — | — | — |
| ... | — | — | — | — |

**B 區塊退出條件**：
- [ ] 16 個 ConfigData 的所有欄位在對應 Sheets 分頁中存在（或有對應豁免記錄）
- [ ] Sheets 欄位命名與 C# property 轉換規則統一（走反序列化層，不可各系統各轉）
- [ ] 每個分頁至少有 3 筆資料（最小健康狀態）
- [ ] Sheets → JSON 匯出流程跑過至少一次成功

**此區塊未完成的風險**：VS 設計師無法熱改，退回手動維護 JSON

---

## [C] 基礎設施檢查清單

- [x] **C01**：`GoogleSheet2JsonSetting` 匯出工具就緒（2026-04-22 完成：Setting.asset 搬至 Assets/Game/Resources/Config/；sheetID 已填入；sheetNames 13 個分頁已填入；15 個 .txt 就位；Convert 需製作人點擊 Inspector 按鈕）
- [x] **C02**：匯出流程的文件就緒（2026-04-22 完成：`projects/ProjectDR/tech/google-sheet-export-tool-spec.md` 建立，涵蓋 13 分頁對應表 / 首次設定 / 日常操作 / 疑難排解 / ADR-001/002 對齊 / 已知限制；Sprint 7 A6-4 由 dev-head 撰寫）
- [x] **C03**：`validate-assets.sh` hook 擴充完成（檢查新 config 是否實作 IGameData；見流程修補提案）— 2026-04-22 完成（新增 `check_json_id_field` 函式：Config JSON 陣列物件缺 `id` 欄位時警示；路徑過濾收窄至 `Assets/Game/Resources/Config/`；python 偵測改善：優先 python3/py，驗證可執行性後才使用）
- [ ] **C04**：`/development-flow` Phase 1.5「資料源接入驗證」補完（見流程修補提案）
- [ ] **C05**：`DEV-DATA-INTAKE-REVIEW` gate 入 `director-gates.md`（見流程修補提案）
- [ ] **C06**：PlayerPrefs 使用位點盤點（B13 CharacterIntroCGPlayer 等）— 若仍無存檔系統，至少登記為 TBD，不強制在此 Gate 處理
- [ ] **C07**：`tech-debt.md` 登記所有本 Gate 發現但未處理的技術債

---

## [D] ADR / TR 綁定清單

- [ ] **D01**：`tr-registry.yaml` 所有本批登記的 TR（TR-data-001/002/003、TR-save-001）都有 `governing_adrs` 綁定
- [ ] **D02**：ADR-001 狀態為 Accepted（本 ADR 依賴其豁免條件定義）
- [ ] **D03**：`adrs/index.md` 建立並列出 ADR-001 / ADR-002
- [ ] **D04**：`FILE_MAP.md` 同步本批新建檔案（tr-registry、ADR-001、ADR-002、data-governance-workflow-patches）
- [ ] **D05**：`/create-control-manifest ProjectDR` 執行一次，產出 `projects/ProjectDR/tech/control-manifest.md`
- [ ] **D06**：本 ADR 狀態從 Accepted 轉為「已執行」— 加版本行（例：v1.1 2026-XX-XX Executed）

---

## 退出 Gate 驗證

**Gate 主持人**：dev-head
**Gate 時機**：製作人宣告「準備進 VS 開發」時，由 studio-manager 呼叫 `/gate-check vertical-slice` 前先跑本 Gate
**Gate 結果**：PASS / CONCERNS / FAIL

**PASS 條件**：[A] + [B] + [C] + [D] 四區塊所有項目全部 ✅
**CONCERNS 條件**：[A] ≥ 13/16 完成 + [B] 全部 ✅ + [C01/C04] ✅ + [D01~D04] ✅（其餘可延後但需入 tech-debt）
**FAIL 條件**：上述以外

---

## 若 Gate 未過的後果

- **VS 開發不可啟動**
- `/gate-check vertical-slice` 不通過（STAGE-GATE 會參照本 ADR 作為 Stage 2→3 前置條件）
- 製作人可選擇：(a) 延後 VS 啟動完成清單、(b) 明示豁免某些項目並補 `superseded-by` ADR

---

## Alternatives Considered（考慮過的替代方案）

### 方案 A：採納 — 本 ADR 的獨立 Gate 清單

- 做法：以可執行 checklist 形式定義退出
- 優點：每項可打勾、可追溯、Gate 可重用
- 缺點：清單維護工時（但一次性）
- **採納理由**：ADR-001 已定義契約，需要對應的「退出機制」落實；清單化是最明確做法

### 方案 B：（未採納）— 僅在 ADR-001 內列退出條件

- 做法：把退出清單塞進 ADR-001 的「Exemption criteria」段落
- 優點：一份 ADR 處理完
- 缺點：ADR-001 會膨脹、混淆「通用契約」與「一次性過渡」；未來類似例外無模式可套
- **未採納理由**：違反 ADR 單一職責

### 方案 C：（未採納）— 以 Sprint 文件承擔退出清單

- 做法：不開 ADR，直接在「VS 前置 Sprint」的 sprint-N.md 列清單
- 優點：與 Sprint 流程對齊
- 缺點：Sprint 完成後會被刪除（CLAUDE.md 規則），清單遺失；無法作為未來類似例外的模式參考
- **未採納理由**：違反「ADR 是技術決策 single source of truth」原則

---

## Consequences（後果）

### 正面

- **進 VS 有明確清單可跑**：不會因「忘記清 IT 例外」導致 VS 啟動後資料層仍破碎
- **Gate 結論可重用**：未來若 VS 階段出現類似「暫時性例外」，可比照此 ADR 模式建新 ADR
- **製作人決策點清楚**：退出時點、豁免確認、Gate 結果皆有明確製作人介入點
- **追溯性完整**：每個豁免 DTO 透過 `// EXEMPT: ADR-002 A-NN` 回指此 ADR 條目

### 負面

- **一次性工時 40~45 小時**：需排入某個 Sprint
- **清單維護成本**：若 IT 階段後又新增 ConfigData，需回頭補本清單（但應該透過 ADR-001 +  hook 預防再增）
- **製作人決策頻次增加**：需拍板退出時點 + A08/A09/A11 豁免確認

### 中性 / 待觀察

- **VS 設計師接手後是否能順利熱改**：需 VS Sprint 1 實測驗證 Sheets → JSON 熱改路徑

---

## Engine Compatibility（引擎相容性）

不適用（本 ADR 為退出流程治理，不涉及引擎 API）。

---

## Implementation Guidelines（實作指引）

### 必須做（Required）

- **排定退出 Sprint**：製作人拍板退出時點後，為此清單開專屬 Sprint（建議 Sprint 7）
- **Sprint 內分批執行**：按 [A] [B] [C] [D] 區塊順序執行；[A] 與 [B] 可併行
- **每項 ✅ 前必做 sub-checklist 驗證**：不可「大致完成」就打勾
- **Gate 未過時必須 STOP**：PASS 前不可啟動 VS 開發流程

### 禁止做（Forbidden）

- **禁止在未完成 [A] 的狀態下刪除 `// EXEMPT: ADR-002` 註解**（原因：註解是豁免追溯的錨點，先改造再刪註解）
- **禁止為了過 Gate 而降低 sub-checklist 標準**（原因：Gate 是保護 VS 開發品質的機制）
- **禁止在 VS 開發期間補回繞過 IGameData 的新 DTO**（原因：此行為等同於重演 IT 階段問題；若真的需要新例外，必須走新 ADR 流程）

### Exemption Criteria（豁免條件）

本 ADR 自身無豁免；但清單內的個別項目允許豁免：

- **A 區塊個別項豁免**：需製作人書面確認 + 在本 ADR 相應條目加 `🟡 豁免（理由 / 日期）` 註記
- **B 區塊個別項豁免**：限於「該 ConfigData 已 deprecated，Sheets 分頁不需建立」情境
- **C 區塊個別項豁免**：限於「該基礎設施有替代方案」情境（例：hook 改為人工審查）
- **D 區塊不可豁免**：ADR / TR 綁定是最低可追溯性要求

### 測試要求

本 ADR 不直接對應單元測試，但每個 A 項的改造必須同時更新對應的反序列化測試（見 ADR-001 § 測試要求）。

---

## GDD Requirements Addressed（對應 GDD 需求）

| TR-ID | 需求摘要 | 來源 GDD |
|-------|---------|---------|
| TR-data-003 | IT 階段例外退出機制 | 工作室級規則衍生（project-status.md 2026-04-21） |

---

## Status History（狀態更動紀錄）

| 版本 | 日期 | 狀態 | 變更摘要 |
|------|------|------|---------|
| v1.0 | 2026-04-21 | Accepted | 初次提出並直接 Accepted（retrofit 製作人 2026-04-21 決策） |
| v1.1 | 2026-04-22 | Accepted | 退出時點鎖定為候選 C（獨立 Gate Sprint），撤除候選 A/B；Sprint 命名暫稱 `sprint-gate-vs-readiness`（製作人 2026-04-22 拍板） |
| v1.2 | 2026-04-22 | Accepted | [A] 區塊 16 個 ConfigData 改造路徑以 ADR-004 新結構為準（歸各 `<Module>/Data/`），Sprint 7 執行時與 E 類結構搬移交叉批次同步進行 |
| v1.3 | 2026-04-22 | Accepted | A08 併入 NodeDialogueConfig（非豁免、非刪除、為重構）；原文誤判 dead code，實為 Sprint 6 決策 6-13 活躍功能但應屬節點對話資料擴充；4 筆 line 加入 node-dialogue-config.json；NodeDialogueController 擴充首次觸發 API；GuardFirstMeetDialogueConfigData.cs + JSON + Guard/Data 目錄刪除 |
| v1.4 | 2026-04-22 | Accepted | 新增 A17 HCGDialogueSetup 硬編對白 IT 例外（製作人 2026-04-22 Sprint 7 TD-2026-006 全採納 dev-head 建議）|
| v1.5 | 2026-04-22 | Accepted | [C02] 匯出流程文件就緒（`tech/google-sheet-export-tool-spec.md` 建立，Sprint 7 A6-4 dev-head 撰寫） |
| v1.6 | 2026-04-22 | Partial Exit | Sprint 7 退出執行：[A] 區塊 16 項全處置完成（13 改 IGameData + 1 刪除 + 1 併入 + 1 豁免）；但 [B] Sheets 對齊 + [C] 匯出工具資料對齊發現結構不一致（Sheets 21 分頁規範化 vs 現有 JSON 包裹物件），製作人 2026-04-22 拍板開 Sprint 8 做資料層徹底 IGameData 化。退出 Gate (D1) 本 Sprint 暫為 CONCERNS 狀態，完整 PASS 延至 Sprint 8 結束。|

---

## 相關連結

- **相關 ADR**：ADR-001（資料治理契約）
- **相關規則**：`.claude/rules/data-files.md`、`doc/tech/development-workflow.md` §1.5
- **相關流程草案**：`projects/ProjectDR/tech/data-governance-workflow-patches.md`
- **相關 TBD**：project-tbd.md（`TBD-balance-001/002` 可能在 VS 啟動後重新評估）

---
