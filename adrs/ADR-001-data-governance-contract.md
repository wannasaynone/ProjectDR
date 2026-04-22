# ADR-001: 資料治理契約 — IGameData 作為遊戲資料單一契約

> **狀態**: Accepted
> **提出日期**: 2026-04-21
> **最近更新**: 2026-04-22（豁免條款收窄為僅 IT 階段時效）
> **提出者**: dev-head（retrofit 既有共用框架 + 製作人 2026-04-21 決策）
> **引擎**: Unity 6.0.x（ProjectDR）；契約來自 KahaGameCore（跨專案共用框架）
> **取代**: —
> **被取代**: —

---

## Context（脈絡）

### 為何需要此決策

ProjectDR 在 Sprint 1~6 期間累積 16 個 config DTO（`*ConfigData.cs`）+ 34 個 Resources/Config JSON 檔，**無一實作 `IGameData` 契約**、**無一透過 `GameStaticDataManager` 註冊**。每份 DTO 自建 `Dictionary<string, T>` 查詢結構、自定反序列化流程，等同在 KahaGameCore 既有的資料治理框架旁另建平行系統。

2026-04-21 根因檢討定位三個結構性漏洞：

1. **流程缺口**：`/development-flow` Phase 1（需求解析）→ Phase 2（模組設計）之間沒有「資料源接入驗證」節點。dev-agent 接到新 config 需求時，預設行為是「在 Config/ 目錄加一份 JSON + 寫一個 DTO class」，不會主動查「這份資料該不該走 IGameData / GameStaticDataManager」。
2. **條款被濫用**：`doc/tech/development-workflow.md` §1.5 的「IT 階段例外」條款原意是「Sheets 尚未建立、工具鏈未全的臨時過渡」，實際變成「IT 可以繞過規格」的通用出口，沒有退出時點、沒有檢查清單、沒有 Gate。
3. **基礎設施未就緒**：Sprint 1~6 期間 Google Sheets 尚未登記（2026-04-21 今日才建立 8 個分頁），`GoogleSheet2JsonSetting` 匯出工具未串接，實務上也無法走正式資料流。

### 當前狀況與問題

- **16 個 ConfigData 類別全未實作 IGameData**：均以 `string` 主鍵（如 `intro_id`、`recipe_id`、`quest_id`）建立 `Dictionary` 查詢，繞過 `GameStaticDataManager.GetGameData<T>(int id)`
- **反序列化測試不驗介面實作**：現有測試只驗「欄位與 JSON 對齊」，不驗「是否實作 IGameData」
- **Sheets 欄位名 ↔ C# property 一致性無自動檢查**：目前 Sheets 剛建，尚未匯出過一次 JSON
- **設計師無法熱改數值**：進 VS 後需要設計師透過 Sheets 調整數值（好感度曲線、煉金配方、CG 門檻等），現有繞過 IGameData 的 DTO 無對應 Sheets 來源

### 相關約束

- **KahaGameCore 是跨專案共用框架**：改 IGameData 契約會影響其他專案，製作人 2026-04-21 已拍板「不動」
- **IT 驗收時程壓力**：現有 16 個 DTO 的 runtime 行為正確、測試通過，大重構有 regression 風險
- **進 VS 前必須就緒**：VS 階段設計師需要 Sheets 熱改能力，正式資料流必須在 VS 啟動前建立

### 相關的 GDD 技術需求（TR-ID）

- `TR-data-001` 遊戲數值外部化（工作室級規則衍生）
- `TR-data-002` IGameData 契約（int ID 主鍵）
- `TR-data-003` IT 階段例外退出機制（由 ADR-002 執行驗證）

---

## Decision（決策）

**所有進入 runtime 查詢的遊戲資料（tabular data）必須實作 `KahaGameCore.GameData.IGameData` 介面，契約為 `int ID { get; }` 作為主鍵；runtime 載入必須透過 `GameStaticDataManager.Add<T>(handler)` 統一註冊；查詢必須透過 `GameStaticDataManager.GetGameData<T>(int id)`。**

### 具體選擇

1. **保持 IGameData 契約現狀**：`int ID { get; }`，不泛型化、不改 KGC 共用框架
2. **新資料 DTO 強制跟隨規格**：進入 runtime 的 tabular data 必須 `implements IGameData`
3. **語意字串主鍵的處理**：當來源需求使用語意字串（如 `intro_vcw_001`、`recipe_flower_potion`）作為人類可讀鍵時，DTO 必須同時包含：
   - `int ID { get; }`（流水號，作為 IGameData 契約主鍵）
   - `string Key { get; }`（原語意字串，作為外鍵 / 查表用途）
4. **載入路徑統一**：所有 runtime 載入走 `GameStaticDataManager.Add<T>(handler)`，禁止在 MonoBehaviour / 系統類別中自建 `Dictionary<string, T>` 平行查詢
5. **豁免由 ADR-002 執行驗證**：IT 階段既有 16 個繞過 IGameData 的 DTO，退出驗證由 ADR-002 處理

### 範圍

- **直接影響**：`projects/ProjectDR/Assets/Game/Scripts/Village/*ConfigData.cs`（16 個類別）+ 所有未來新增的 `*ConfigData.cs`
- **間接影響**：`Assets/Game/Resources/Config/*.json`（34 個 JSON，主鍵欄位需對齊）
- **影響 agent**：dev-agent（實作時）、dev-head（審核時）、design-agent（寫 GDD 技術需求時）、ui-ux-designer（若需要 view-time 資料）
- **不影響**：純展示資料（Sprite、AudioClip、Prefab 引用）由 Unity 既有資產管線處理；一次性狀態（session 旗標、PlayerPrefs 暫存）不屬於 tabular data

---

## Alternatives Considered（考慮過的替代方案）

### 方案 A：輕修（採納）— 維持 IGameData 契約 + 補流程 + 補 hook

- 做法：保持 `int ID` 契約，新 config 強制跟隨；補 `/development-flow` Phase 1.5「資料源接入驗證」；補 `validate-assets.sh` 檢查；既有 16 個 DTO 透過 ADR-002 排退出
- 優點：
  - 不動 KahaGameCore 共用框架，零 regression 風險
  - 新程式碼從明日起符合規格，舊程式碼有明確退出時點
  - 流程 + hook 兩道防線，預防再次漏接
- 缺點：
  - 語意字串主鍵需雙欄位（`int ID` + `string Key`），DTO 稍微變重
  - 既有 16 個 DTO 仍需分批改寫，VS 前還要一次清理
- **採納理由**：成本最低、風險最小、改動面最明確；符合製作人 2026-04-21「不大重構」決策

### 方案 B：中改（未採納）— 泛型化 IGameData<TKey>

- 做法：將 IGameData 改為 `IGameData<TKey>`，`TKey` 可為 `int` 或 `string`；GameStaticDataManager 同步泛型化
- 優點：
  - 語意字串主鍵可直接當 `TKey = string`，DTO 更簡潔
  - 支援未來更多主鍵型態（Guid、複合鍵）
- 缺點：
  - 動 KahaGameCore 共用框架，影響其他專案
  - Dictionary 跨 TKey 儲存需重構（目前 `Dictionary<Type, IGameData[]>` 無法直接擴展到泛型 TKey）
  - 製作人明示不大重構
- **未採納理由**：違反製作人決策；成本效益不划算（現有 DTO 改雙欄位比全框架泛型化便宜一個數量級）

### 方案 C：大重構（未採納）— 改寫資料層為 ScriptableObject + Addressables

- 做法：徹底拋棄 JSON + IGameData 路徑，改用 ScriptableObject 資料資產 + Addressables 載入
- 優點：
  - Unity 原生對齊（Inspector 直接編輯、Ref 自動處理、熱更友善）
  - 測試更直觀（ScriptableObject 可 mock）
- 缺點：
  - 與 Sheets 導出流程不相容（Sheets → JSON 是目前決策）
  - 需重寫所有 DTO + 載入邏輯，規模 ~3 週
  - 對 KGC 其他專案造成壓力（如果 KGC 保留 IGameData，本專案自建一條平行路徑）
- **未採納理由**：違反「Sheets 是遊戲數值單一真相來源」原則（`.claude/rules/data-files.md`）；工時不合理

---

## Consequences（後果）

### 正面

- **資料流收斂到單一契約**：Sheets → IGameData(int ID) → JSON → GameStaticDataManager → runtime 四段清晰
- **反序列化測試有單一斷言錨點**：每個 DTO 加「實作 IGameData」斷言即可（見實作指引 § 測試要求）
- **設計師熱改路徑明確**：VS 階段設計師改 Sheets → 匯出 JSON → runtime 生效，無需動程式碼
- **繞過行為有 hook 擋關**：`validate-assets.sh` 擴充後，新 config 漏接 IGameData 會被警示
- **回退 ADR 成為工作室規則的反饋閉環**：工作室級規則（TR-data-001/002）透過此 ADR 在本專案落地

### 負面

- **語意字串主鍵的 DTO 變重**：需雙欄位（`int ID` + `string Key`），開發時多一道手續
- **既有 16 個 DTO 仍需清理**：VS 啟動前必須排清理工時（具體工作項目由 ADR-002 盤點）
- **測試模板需擴充**：現有反序列化測試必須加「實作 IGameData」斷言模板
- **KGC 框架保留舊包袱**：若未來真需要泛型主鍵（`Guid`、複合鍵），仍需回頭解（但超出當前視野）

### 中性 / 待觀察

- **GoogleSheet2JsonSetting 匯出工具的串接進度**：若工具未就緒，VS 啟動前的既有 DTO 清理仍走手動 JSON 維護
- **豁免條款是否會被再次濫用**：若未來又出現「IT 階段例外」類條款，需同樣補退出 Gate，不可通用出口

---

## Engine Compatibility（引擎相容性）

| 項目 | 內容 |
|------|------|
| 涉及引擎 | Unity 6.0.x |
| 涉及 API / 模組 | `KahaGameCore.GameData.IGameData`、`KahaGameCore.GameData.Implemented.GameStaticDataManager`、`Resources.Load`（目前既有路徑） |
| LLM 知識截止後的風險 | LOW（IGameData 為 KGC 自定介面，非引擎 API；`Resources.Load` 為 Unity 穩定 API） |
| 需驗證的 API 行為 | `GameStaticDataManager.Add<T>(IGameStaticDataHandler)` 的 async 版本在多 handler 並行載入時的順序保證（進 VS 時若 handler 數量增加需重檢） |
| 已讀過的版本遷移文件 | `projects/ProjectDR/tech/engine-reference/unity/VERSION.md` |

---

## Implementation Guidelines（實作指引）

### 必須做（Required）

- **新 config DTO 必須 `implements IGameData`**：`class XxxConfigData : IGameData`，暴露 `public int ID { get; }` 屬性
- **語意字串主鍵雙欄位**：若來源資料使用語意字串（例：`intro_vcw_001`、`recipe_flower_potion`），DTO 必須：
  - `public int ID { get; }` — 流水號，作為 IGameData 契約主鍵
  - `public string Key { get; }` — 原語意字串，保留作為外鍵 / 查表用途
- **runtime 載入走 GameStaticDataManager**：新資料透過 `GameStaticDataManager.Add<T>(handler)` 註冊；查詢透過 `GetGameData<T>(int id)`
- **反序列化測試必須驗兩件事**：
  1. 欄位名與 JSON / Sheets 欄位對齊（既有）
  2. 類別實作 `IGameData`，`ID` 屬性非 0（新增）
- **Sheets 欄位名 ↔ C# property 一致**：命名統一透過反序列化層轉換（camelCase ↔ PascalCase），不可每系統各轉一套
- **ConfigData 類別 XML 註解標註來源**：類別註解需註明對應的 Sheets 分頁名 + JSON 檔名，方便追溯

### 禁止做（Forbidden）

- **禁止新 config DTO 自創 string 主鍵且不實作 IGameData**（原因：繞過 GameStaticDataManager 統一載入，導致載入時序、依賴、測試骨架都得另寫）
- **禁止在系統類別內自建 `Dictionary<string, T>` 平行查詢結構**（原因：重複實作 GameStaticDataManager 職責，未來 Sheets 熱改無法生效）
- **禁止手動編輯 `Assets/Game/Resources/Config/*.json`**（原因：JSON 是 Sheets 匯出產物，手改會被下次匯出覆蓋，且無 diff 稽核；見 `.claude/rules/data-files.md`）
- **禁止「臨時用 string 主鍵、之後再改」的口頭承諾**（原因：此 ADR 存在前已有 16 個「臨時」DTO 入庫；正式機制請透過豁免條款）

### 護欄（Guardrail）

- **單 JSON 大小 < 500KB**（超過時應拆分或改走 Addressables；目前 34 個 JSON 最大 ~100KB，留 5x 空間）
- **反序列化時間 < 50ms / JSON**（GameStaticDataManager.AddAsync 的 per-handler 預算，VS 階段若超過需補 profile）
- **每個 ConfigData 類別對應的測試覆蓋率 100%**（所有公開欄位被至少一筆 JSON 覆蓋到非預設值）

### Exemption Criteria（豁免條件，2026-04-22 收窄）

本 ADR 的豁免**僅適用於 IT 階段時效**，進 VS 前必須清除所有豁免。具體退出清單 + Gate 由 ADR-002 定義並執行。

**唯一豁免條件（2026-04-22 製作人決策）**：

1. **IT 階段內有效**：退出時點由 ADR-002 定義，進 VS 前必須清除。進 VS 開發後所有豁免失效，不可保留。

**已撤除的豁免選項**（2026-04-22 製作人拍板）：

- ~~「一次性資料暫免（非查詢型）」~~：**已撤除**。無論資料是否為查詢型，皆須實作 IGameData；若該資料 IT 後會重寫/廢棄，退出時由 ADR-002 清單處理（豁免或刪除），不能以「反正要重寫」為藉口保留繞過結構
- ~~「檔頭標註作為豁免憑證」~~：**已撤除作為獨立條款**。檔頭 `// EXEMPT: ADR-002 A-NN` 仍是追溯錨點，但**不構成豁免的理由**，僅是 IT 階段期間（條件 1 已滿足時）的標示義務

**不構成豁免的理由**：

- 「平台能力限制」：Unity 環境支援 C# 介面，不存在「無法實作 IGameData」的情境
- 「這是臨時的」/「IT 之後會重寫」：正式機制請透過 ADR-002 退出流程，不以「臨時」作藉口
- 「性能考量」：若有性能疑慮須開獨立 ADR 背書，不在本條款自動豁免
- 「dev-agent 不熟悉契約」：違反分工鐵則與契約認知缺口，不構成例外

### 測試要求

- **類型**：單元測試（反序列化正確性）
- **命名規範**：`[ConfigDataName]Tests.cs`，例 `AffinityConfigDataTests.cs`
- **必要覆蓋**：
  - 每個公開欄位至少一筆 JSON 資料覆蓋到非預設值
  - 測試類別實作 `IGameData` 且 `ID` 非 0
  - 測試重複 ID（若發生）是否能被框架偵測（或明確允許）
  - 測試反序列化失敗時的行為（空 JSON、缺欄位、型別錯誤）
- **測試放置**：`projects/ProjectDR/Assets/Tests/Village/Data/*`（既有測試目錄）

---

## GDD Requirements Addressed（對應 GDD 需求）

| TR-ID | 需求摘要 | 來源 GDD |
|-------|---------|---------|
| TR-data-001 | 所有遊戲數值必須外部化 | 工作室級規則（CLAUDE.md） |
| TR-data-002 | 所有 runtime 查詢資料必須實作 IGameData | KahaGameCore 框架契約 |

（TR-data-003 由 ADR-002 主要治理，本 ADR 為次要關聯 ADR）

---

## Status History（狀態更動紀錄）

| 版本 | 日期 | 狀態 | 變更摘要 |
|------|------|------|---------|
| v1.0 | 2026-04-21 | Accepted | 初次提出並直接 Accepted（retrofit 既有 KahaGameCore 框架決策 + 製作人 2026-04-21 拍板） |
| v1.1 | 2026-04-22 | Accepted | 豁免條款收窄：僅保留「IT 階段時效」一項；移除「一次性資料暫免」與「檔頭標註作為獨立豁免」；加入「不構成豁免的理由」清單（製作人 2026-04-22 決策）|

**Retrofit 說明**：本 ADR 為「既有技術慣例形式化」— IGameData 契約已存在於 KahaGameCore 多年，製作人 2026-04-21 明示「不大重構」確認此契約保留；本 ADR 將此慣例 + 製作人決策一併記錄為 Accepted，跳過 Proposed 階段。

---

## 相關連結

- **相關 ADR**：ADR-002（IT 階段例外退出清單）
- **相關程式碼**：
  - `projects/ProjectDR/Assets/KahaGameCore/InGame/GameData/IGameData.cs`
  - `projects/ProjectDR/Assets/KahaGameCore/InGame/GameData/Implemented/GameStaticDataManager.cs`
  - `projects/ProjectDR/Assets/Game/Scripts/Village/*ConfigData.cs`（16 個違反案例）
- **相關 dev-log**：2026-04-21 根因檢討（詳見 project-status.md 同日決策條目）
- **相關規則**：`.claude/rules/data-files.md`、CLAUDE.md「遊戲數值外部化」
- **相關流程草案**：`projects/ProjectDR/tech/data-governance-workflow-patches.md`

---
