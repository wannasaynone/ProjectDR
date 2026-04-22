# 資料治理流程修補提案（Draft）

> **性質**：技術提案 draft，不直接修改 `.claude/` 下的 skill / hook / rule / gate 檔
> **產出日期**：2026-04-21
> **產出者**：dev-head
> **授權狀態**：🔴 待製作人審核 → 授權 studio-manager 寫入
> **對應 ADR**：ADR-001（資料治理契約）、ADR-002（IT 階段例外退出）

---

## 提案背景

ADR-001 定義了 IGameData 契約，ADR-002 定義了 IT 階段的退出 Gate。這兩份 ADR 解決「過去的漏網之魚」與「退出時點」，但**沒有解決「未來如何預防再次繞過」**的問題。

2026-04-21 根因檢討的三層缺口中，第 1 層「`/development-flow` 缺資料源接入 Phase」與第 3 層「基礎設施未就緒」都需要流程層 / hook 層的補強才能真正閉環。

本提案草擬 4 處修補，**不直接動 `.claude/`**，僅提供 draft 內容，由製作人審核後授權 studio-manager 寫入對應檔案。

---

## 提案一：`/development-flow` Phase 1.5「資料源接入驗證」

### 目的

在 dev-agent 進入模組設計前，強制檢查「本次需求是否涉及 tabular data；若是，是否走正式資料流」。避免 dev-agent 預設行為「新增 JSON + 自建 DTO」。

### 插入位置

```
Phase 1.1 需求解析（現有）
    ↓
Phase 1.5 資料源接入驗證（新增）  ← 本提案
    ↓
Phase 2.1 模組設計（現有）
```

### Phase 1.5 內容草案

```markdown
## Phase 1.5 — 資料源接入驗證（新增）

**觸發條件**：Phase 1.1 需求解析產出中，若出現以下 token 之一，必須進入本 Phase：

- 「新 config」「加一份資料」「xxx-config.json」
- 「NPC 資料」「技能資料」「物品資料」「配方」「關卡資料」
- 「Sheets 分頁」「Google Sheets」「表格資料」
- 「常數表」「查詢表」「lookup table」

**若不涉及 tabular data**（例：純邏輯修 bug、UI 佈局調整、動畫補間）→ 跳過本 Phase，直接進 Phase 2。

### Phase 1.5 Checklist

| # | 檢查項 | 通過條件 |
|---|-------|---------|
| 1 | **tr-registry 查詢** | 本需求涉及的技術需求已登記為 TR-ID（例 TR-<system>-NNN）。若未登記 → 呼叫 `/architecture-review` 補登 |
| 2 | **ADR-001 合規檢查** | 新資料 DTO 計畫實作 `IGameData`；若為語意字串主鍵，計畫使用雙欄位（int ID + string Key）|
| 3 | **Sheets 來源確認** | 本資料的 Sheets 分頁已建立，或製作人書面確認「本筆資料不走 Sheets」（豁免需 ADR 背書）|
| 4 | **GameStaticDataManager 接入** | 本資料計畫透過 `GameStaticDataManager.Add<T>(handler)` 載入，而非自建 Dictionary |
| 5 | **反序列化測試規格** | 測試計畫已包含：(a) 欄位對齊、(b) IGameData 實作斷言、(c) ID 非 0 斷言 |
| 6 | **豁免確認**（僅在 IT 階段）| 若走 IT 階段例外，必須在檔頭註明 `// EXEMPT: ADR-002 A-NN` 並在 ADR-002 清單有對應條目 |

### Phase 1.5 產出物

- **資料源接入報告**（簡短、200 字內）：寫入當次 `dev-logs/yyyy-mm-dd-i.md`，包含：
  - 本需求涉及的 tabular data 項目
  - 對應的 TR-ID 清單
  - 計畫的 DTO 實作方式（IGameData ? 雙欄位 ? 豁免 ?）
  - 對應的 Sheets 分頁名與欄位設計（若走 Sheets 路徑）

### Phase 1.5 失敗處置

- 若 Checklist 任何一項未通過 → **不進入 Phase 2**，回退到 Phase 1，由 dev-agent 或 dev-head 補齊
- 若涉及製作人決策（例：豁免確認）→ 停下等待製作人指示，不自行推進
```

### 修改檔位置

`.claude/skills/development-flow/SKILL.md` 的 Phase 1 ~ Phase 2 之間插入新章節。

---

## 提案二：`validate-assets.sh` hook 擴充邏輯

### 目的

在 dev-agent 或製作人寫入新 `Assets/Game/Resources/Config/*.json` 時，自動檢查對應 C# ConfigData 類別是否實作 IGameData，避免繞過規格。

### 觸發條件擴充

現有 hook 已覆蓋 `projects/*/Assets|assets|data/**` 路徑的 JSON 有效性檢查。新增以下檢查：

- **觸發路徑**：`projects/*/Assets/Game/Resources/Config/*.json`
- **觸發時機**：Write / Edit

### 擴充檢查邏輯（bash + grep 實作版）

```bash
# 新增函式：check_config_json_has_igamedata
# 位置：.claude/hooks/validate-assets.sh 內新增

check_config_json_has_igamedata() {
    local json_path="$1"  # 例：projects/ProjectDR/Assets/Game/Resources/Config/affinity-config.json

    # 1. 推算對應的 C# class 檔名
    #    affinity-config.json → AffinityConfigData.cs
    local json_basename=$(basename "$json_path" .json)
    # kebab-case → PascalCase 轉換（簡化版，可用 awk 處理）
    local class_name=$(echo "$json_basename" | awk -F'-' '{for(i=1;i<=NF;i++) printf "%s%s", toupper(substr($i,1,1)), substr($i,2); print ""}')
    local expected_class="${class_name}Data.cs"

    # 2. 搜尋對應 C# 檔
    local cs_files=$(find "$(dirname "$json_path")/../../Scripts" -name "$expected_class" 2>/dev/null)

    if [ -z "$cs_files" ]; then
        echo "⚠️  WARN: 找不到對應的 C# class 檔（預期 $expected_class）"
        echo "   JSON 路徑：$json_path"
        echo "   建議：建立 ConfigData 類別，並實作 IGameData（見 ADR-001）"
        return 0  # 僅警示，不阻擋
    fi

    # 3. 檢查 C# 檔是否實作 IGameData 或有豁免註解
    local has_igamedata=$(grep -l ": IGameData\b\|: KahaGameCore.GameData.IGameData\b\|IGameData,\|IGameData$" "$cs_files" 2>/dev/null)
    local has_exemption=$(grep -l "// EXEMPT: ADR-002" "$cs_files" 2>/dev/null)

    if [ -z "$has_igamedata" ] && [ -z "$has_exemption" ]; then
        echo "⚠️  WARN: $expected_class 未實作 IGameData 介面且無豁免註解"
        echo "   依 ADR-001 要求，此類別應實作 IGameData；若屬 IT 階段例外，需加 // EXEMPT: ADR-002 註解"
        echo "   相關檔：$cs_files"
        return 0  # 僅警示
    fi

    # 4. 檢查反序列化測試是否存在
    local test_file=$(find "$(dirname "$json_path")/../../../../Tests" -name "${class_name}DataTests.cs" 2>/dev/null)
    if [ -z "$test_file" ]; then
        echo "⚠️  WARN: 找不到對應反序列化測試（預期 ${class_name}DataTests.cs）"
        echo "   依 ADR-001 § 測試要求，每個 ConfigData 必須有反序列化測試"
        return 0  # 僅警示
    fi

    return 0
}

# 在 hook 主流程內新增呼叫
if [[ "$FILE_PATH" == *"/Assets/Game/Resources/Config/"*".json" ]]; then
    check_config_json_has_igamedata "$FILE_PATH"
fi
```

### 警示策略

遵循 CLAUDE.md「自動驗證 Hooks」原則：**僅警示，不阻擋**（JSON 無效才阻擋，此 hook 的 JSON 有效性檢查既有）。

### 修改檔位置

`.claude/hooks/validate-assets.sh` 內新增函式 + 主流程呼叫。

### 限制與後續

- 當前實作以 bash + grep 為主，在邊界情境（註解的 `:` 符號、多行宣告）可能誤判 → 後續若誤判率高可改為用 C# 專用工具（roslyn 分析）
- kebab-case ↔ PascalCase 轉換邏輯簡化版，少數例外命名（如 `VS` 全大寫）需人工確認

---

## 提案三：`DEV-DATA-INTAKE-REVIEW` gate 草案

### 目的

為 dev-head 在審查「新資料 / 新 ConfigData 類別」時提供標準化 checklist，集中在 `director-gates.md` 管理。

### Gate ID

`DEV-DATA-INTAKE-REVIEW`

### 觸發時機

- dev-head 審查涉及新 ConfigData 類別或新 `*-config.json` 的 dev-agent 實作產出時
- `/development-flow` Phase 5（驗收）若涉及新 config，必然觸發此 gate

### Gate Checklist

```markdown
## Gate: DEV-DATA-INTAKE-REVIEW

**主持**：dev-head
**時機**：審查涉及新 tabular data 的 dev-agent 實作時

### Checklist

- [ ] **DI-1 合規檢查**：新 ConfigData 類別實作 `IGameData`，或在檔頭有 `// EXEMPT: ADR-002 A-NN` 註解
- [ ] **DI-2 雙欄位檢查**：語意字串主鍵的 DTO 同時有 `public int ID` 與 `public string Key`
- [ ] **DI-3 載入路徑**：透過 `GameStaticDataManager.Add<T>(handler)` 載入，非自建 Dictionary
- [ ] **DI-4 查詢路徑**：查詢點使用 `GetGameData<T>(id)`；若仍保留 string Key 查詢，需經 GameStaticDataManager 或有豁免記錄
- [ ] **DI-5 反序列化測試**：對應 `*DataTests.cs` 存在，斷言包含「IGameData 實作 + ID 非 0」
- [ ] **DI-6 Sheets 對齊**：對應 Sheets 分頁已建立，欄位與 C# property 對齊（或豁免）
- [ ] **DI-7 TR-ID 綁定**：對應技術需求已登記 `tr-registry.yaml`（新需求 append，不 renumber）
- [ ] **DI-8 ADR 合規**：若屬 IT 階段例外，ADR-002 清單有對應條目；否則全項通過 ADR-001

### Verdict 規則

- **PASS**：DI-1 ~ DI-5 全部 ✅（核心規格），DI-6 ~ DI-8 視情境可延後但需入 tech-debt
- **CONCERNS**：DI-1 ~ DI-5 有 1 項為「豁免但豁免記錄不完整」
- **FAIL**：DI-1 ~ DI-5 任何一項實質不合規（無豁免、無註解、無 ADR 背書）

### Review Mode 差異

| Mode | 本 Gate 行為 |
|------|------------|
| `full` | 全部 checklist 逐條確認 |
| `lean` | 關鍵項 DI-1/DI-3/DI-5 必檢，其餘抽檢 |
| `solo` | 略（但 `validate-assets.sh` hook 的警示不跳過）|
```

### 修改檔位置

`.claude/docs/director-gates.md` 的「dev-head 部門」章節新增本 Gate；同時更新 Gate 總覽表（若有）。

---

## 提案四：`.claude/rules/data-files.md` 更新建議

### 目的

在既有 `.claude/rules/data-files.md` 基礎上，明確引用 ADR-001、ADR-002 作為規則來源，讓路徑規則檔與 ADR 形成雙向錨定。

### 建議新增段落

在 `唯一資料源原則` 之後新增：

```markdown
## IGameData 契約（ADR-001）

進入 runtime 查詢的 tabular data 必須實作 `KahaGameCore.GameData.IGameData` 介面（契約為 `int ID { get; }`），並透過 `GameStaticDataManager.Add<T>(handler)` 統一註冊載入。

- 實作細節見 [ADR-001 資料治理契約](../../projects/<專案名>/adrs/ADR-001-data-governance-contract.md)
- 新 ConfigData 類別必須 `: IGameData`
- 語意字串主鍵類別必須同時提供 `int ID`（流水號）+ `string Key`（原語意字串）
- 查詢統一走 `GetGameData<T>(id)`

## IT 階段例外的正式退出（ADR-002）

若某專案在 IT 階段暫時繞過 IGameData 契約，**必須**遵守以下：

- 被豁免的 DTO 類別檔頭註明 `// EXEMPT: ADR-002 A-NN`（NN 為 ADR-002 清單編號）
- 豁免條目必須登記在該專案的 `adrs/ADR-002-*.md` 清單中
- 進 VS 階段前必須通過 ADR-002 的退出 Gate（`DEV-DATA-INTAKE-REVIEW` + 清單全 ✅）
- 通用「IT 階段例外」不是通用出口，每筆豁免需有 ADR 條目對應

詳見 [ADR-002 IT 階段例外退出清單](../../projects/<專案名>/adrs/ADR-002-it-stage-exemption-exit.md)
```

### 建議修訂段落

`禁止事項` 區塊補一條：

```markdown
- 禁止新增 ConfigData 類別時跳過資料填寫設計（Phase 1.5 資料源接入驗證）
  - 跳過的後果：違反 ADR-001；進 VS 時需回補清理；對應 TR-ID 無法追溯
```

### 修改檔位置

`.claude/rules/data-files.md`（既有檔，新增兩段 + 補一條禁止）

---

## 提案執行建議

### 優先順序

1. **提案三（Gate）**：優先度最高，不修改既有流程，僅新增 checklist 供 dev-head 審查時引用
2. **提案四（rule 更新）**：優先度次高，僅新增段落，不改規則既有行為
3. **提案一（Phase 1.5）**：中優先度，改動 skill 流程，需 studio-manager 執行 `/skill-test static development-flow` 驗證
4. **提案二（hook 擴充）**：較低優先度，可作為「已有警示也能運作」的加強層，bash 實作邊界情境需測試

### 建議一次性寫入或分批寫入

- **一次性寫入**（若製作人一次授權）：4 個提案打包為一次工作項，交由 studio-manager 依序執行 + 跑 `/skill-test static`
- **分批寫入**（若製作人要分段審核）：按優先順序 3 → 4 → 1 → 2 分批，每批寫完驗證後再下一批

---

## 對既有流程的向後相容性

| 流程 | 是否仍可用 | 說明 |
|------|----------|------|
| `/development-flow`（不涉及新 config）| 完全相容 | Phase 1.5 有 trigger 條件，不涉及 tabular data 時自動略過 |
| 現有 16 個 ConfigData（繞過 IGameData）| IT 階段相容 | 依 ADR-002 清單在退出時統一清理；hook 會對這些檔案警示，但不阻擋 |
| 既有 `/doc-health`、`/consistency-check` | 完全相容 | 不涉及資料層流程 |
| 既有 `validate-gdd.sh` hook | 完全相容 | 本提案僅擴充 `validate-assets.sh`，不動 GDD hook |

---

## 製作人待決事項

在 studio-manager 執行寫入前，以下四項需要製作人明示授權：

1. **授權 studio-manager 寫入本提案內容到 `.claude/`** — 是否一次性 / 分批？
2. **`.claude/skills/development-flow/SKILL.md` 的 Phase 1.5 插入位置** — 接受本提案的「Phase 1.1 → 1.5 → 2.1」結構，或有其他偏好？
3. **`validate-assets.sh` 的 bash 實作是否夠用** — 或希望後續升級為 C# Roslyn 分析（需額外工具鏈）？
4. **`DEV-DATA-INTAKE-REVIEW` 是否併入既有 `DEV-CODE-REVIEW` gate** — 或維持獨立 Gate ID？

---

## 相關連結

- **ADR-001**：`projects/ProjectDR/adrs/ADR-001-data-governance-contract.md`
- **ADR-002**：`projects/ProjectDR/adrs/ADR-002-it-stage-exemption-exit.md`
- **TR 登記**：`projects/ProjectDR/adrs/tr-registry.yaml`
- **既有 hook**：`.claude/hooks/validate-assets.sh`
- **既有 rule**：`.claude/rules/data-files.md`
- **既有 skill**：`.claude/skills/development-flow/SKILL.md`
- **既有 gates 文件**：`.claude/docs/director-gates.md`
