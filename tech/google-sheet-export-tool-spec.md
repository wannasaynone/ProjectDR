# Google Sheet Export Tool — ProjectDR 使用規範

> **建立日期**: 2026-04-22（Sprint 7 A6-4）
> **主責**: dev-head
> **狀態**: Active
> **對應 ADR**: ADR-001（資料治理契約）、ADR-002 § [C01] / [C02]
> **對應 Sprint 項目**: Sprint 7 A6（匯出工具串接 + 文件補齊）

---

## Sprint 7 狀態（2026-04-22）

**本 spec 完成度**：工具機制描述 + 操作步驟 ✅；資料對應表（§2）反映 ADR-002 [A] 清單，**與實際 Sheets 21 分頁結構尚未對齊**

**實際發現**：
- Sheets 有 21 分頁（主表 + 子表規範化：例 CharacterIntros + CharacterIntroLines）
- KGC 工具固定 1:1 分頁匯出（純陣列格式）
- 現有 14 個 JSON 為「包裹物件」格式（`{schema_version, note, xxx_array: [...]}`）
- 三層結構不對齊，直接 Convert 會產出與 C# DTO 不匹配的 .txt

**Sprint 8 將處理**：
- 所有 config DTO 改為**純陣列 IGameData 格式**
- 取消 schema_version/note 外包裝（移到 C# 端或 workflow 文件）
- C# DTO 反序列化從 `root.xxx_array` 改為 `root` 即陣列
- Sheets 21 分頁保留，每分頁對應一個 IGameData 實作的純陣列 .txt
- 本 spec § 2 對應表屆時重寫為 21 分頁 × 21 .txt × 21 DTO 的完整 mapping

**目前使用建議**：
- 14 個 `.txt` 保留現有包裹物件格式，可手動編輯使用
- 暫不在 Unity Editor 觸發 Start Convert（會覆蓋現有內容為不匹配的純陣列格式）
- Sprint 8 完成資料模型重構後，本 spec 升 v1.1，此警告段落移除

---

## 0. TL;DR

本工具**不是** ProjectDR 新建，而是 **KahaGameCore（KGC）框架**提供的 Editor tool。ProjectDR 所做的只有「串接 + 設定」：

- Setting 資產位置：`Assets/Game/Resources/Config/_Google Sheet 2 Json Setting.asset`
- Sheets 來源：`1WjvMHTtEZ-JIRE1hmKz0pQMfI78hVI9jXC7ZLKEpLoo`
- 13 個分頁 → 13 個 `.txt` 匯出檔
- 觸發方式：Unity Inspector 手動點 `Start Convert` 按鈕（Unity MCP **無法**自動觸發 Custom Inspector 按鈕）
- 輸出副檔名為 `.txt`（製作人 2026-04-22 拍板；與 Unity TextAsset 慣例一致）

所有遊戲 tabular 數值**必須**走此路徑進入 runtime（違反者違反 ADR-001，IT 階段例外僅限 ADR-002 清單）。

---

## 1. 工具來源

本工具為 KGC 框架的 Editor 資產，**不可**在 ProjectDR 本地修改（若修改需走 KGC 層面 ADR）。

### 相關檔案

| 檔案 | 用途 |
|------|------|
| [`Assets/KahaGameCore/Editor/GoogleSheet2JsonSetting.cs`](../Assets/KahaGameCore/Editor/GoogleSheet2JsonSetting.cs) | ScriptableObject 資料容器，定義 `sheetID` + `sheetNames[]` 兩欄位；`[CreateAssetMenu]` 位於 `Kaha Game Core/Editor/GoogleSheet2Json Setting` |
| [`Assets/KahaGameCore/Editor/GoogleSheet2JsonSettingEditor.cs`](../Assets/KahaGameCore/Editor/GoogleSheet2JsonSettingEditor.cs) | CustomEditor，提供 `Start Convert` 按鈕；呼叫 Google Sheets Public API 讀取每個分頁、反序列化為 row list、推斷欄位型別（int / long / float / string）、再以 JsonFx 序列化為 JSON 陣列，寫入 Setting 資產所在目錄 |

### 工作室級參考文件

- [`doc/tech/development-workflow.md`](../../../doc/tech/development-workflow.md) § 5 — 階段 1：Sheets → JSON 匯出
- [`doc/tech/development-workflow.md`](../../../doc/tech/development-workflow.md) § 6 — `GoogleSheet2JsonSetting` 整體流程

### 工具行為摘要（讀 .cs 後整理）

1. 讀 Setting.asset 的 `sheetID` + `sheetNames[]`
2. 針對每個 sheetName，組出 URL：`https://sheets.googleapis.com/v4/spreadsheets/{sheetID}/values/{sheetName}?key={HARDCODED_API_KEY}`
3. HTTP GET（30 秒 timeout）取得 Sheets raw JSON
4. `Convert(raw)`：第一列為 keys（欄位名），2..N 列為 values
5. 空值 / `NOEX_` 前綴欄位 / 空白 key 自動略過
6. 型別推斷順序：`int` → `long` → `float` → `string`（failover）
7. 輸出檔名：`{sheetName}.txt`，放在 Setting.asset **所在目錄**
8. 輸出格式：JsonFx JSON 陣列（每列一個 Dictionary<string, object>）
9. Console 輸出 `---End---` 表完成；之後 `AssetDatabase.Refresh()`

---

## 2. ProjectDR 實際 Sheets 對應表

Sheets 來源：`1WjvMHTtEZ-JIRE1hmKz0pQMfI78hVI9jXC7ZLKEpLoo`（製作人 2026-04-21 建立 8 分頁，2026-04-22 補齊 13 分頁）。
輸出位置：`Assets/Game/Resources/Config/`

| # | Sheets 分頁名 | 輸出檔名 | 對應 C# DTO | IGameData 狀態 |
|---|--------------|---------|-------------|---------------|
| 1 | `AffinityConfig` | `affinity-config.txt` | `AffinityCharacterConfigData` | ✅ ID = `id` |
| 2 | `CGSceneConfig` | `cg-scene-config.txt` | `CGSceneConfigEntry` | ✅ ID = `id`, Key = `cgSceneId` |
| 3 | `CharacterIntroConfig` | `character-intro-config.txt` | `CharacterIntroData` | ✅ ID = `id`, Key = `intro_id` |
| 4 | `CharacterQuestionsConfig` | `character-questions-config.txt` | `CharacterQuestionEntryData` | ✅ ID = `id`, Key = `question_id` |
| 5 | `CombatConfig` | `combat-config.txt` | `CombatConfigJson` | ✅ ID = 1（singleton） |
| 6 | `CommissionRecipes` | `commission-recipes-config.txt` | `CommissionRecipeEntry` | ✅ ID = `id`, Key = `recipe_id` |
| 7 | `GreetingConfig` | `greeting-config.txt` | `GreetingEntryData` | ✅ ID = `id`, Key = `greeting_id` |
| 8 | `IdleChatConfig` | `idle-chat-config.txt` | `IdleChatTopicData` | ✅ ID = `id`, Key = `topic_id` |
| 9 | `MainQuestConfig` | `main-quest-config.txt` | `MainQuestConfigEntry` | ✅ ID = `id`, Key = `quest_id` |
| 10 | `MonsterConfig` | `monster-config.txt` | `MonsterTypeJson` | ✅ ID = `id`（與 `typeId` 雙欄位） |
| 11 | `NodeDialogueConfig` | `node-dialogue-config.txt` | `NodeDialogueLineData` | ✅ ID = `id`, Key = `line_id` |
| 12 | `StorageExpansionConfig` | `storage-expansion-config.txt` | `StorageExpansionStageData` | ✅ ID = `level` |
| 13 | `InitialResourcesConfig` | `initial-resources-config.txt` | `InitialResourceGrantData` | ✅ ID = `id`, Key = `grant_id` |

### 豁免分頁（不在 13 清單中，但對應檔案仍在 Config/）

| 檔案 | 對應 DTO | 狀態 |
|------|---------|------|
| `guard-first-meet-dialogue-config.txt`（已刪除） | ~~`GuardFirstMeetDialogueConfigData`~~ | 已併入 `node-dialogue-config.txt`（ADR-002 A08） |
| `guard-return-config.txt`（已刪除） | ~~`GuardReturnConfigData`~~ | 已刪除（ADR-002 A09 dead code） |
| `player-questions-config.txt` | `PlayerQuestionsConfigData` | 🟡 豁免（ADR-002 A15；IT 階段 placeholder，VS 重寫時改造） |
| `gift-sword.txt` | `GiftSwordData`（手動 JSON）| 🟡 豁免（ADR-002 A17 相關；守衛贈劍事件 config，VS 階段重新評估） |

說明：豁免檔案目前仍以手動 JSON 維護，不會被 `Start Convert` 覆蓋（因不在 `sheetNames[]`），但進 VS 前需按 ADR-002 評估是否納入 Sheets。

---

## 3. 首次設定步驟（Setup）

本節只在「首次設定」或「Setting.asset 損毀 / 被誤改」時執行。2026-04-22 已由 dev-agent 完成初始設定，日常不需再跑本節。

### 3.1 確認 Setting.asset 存在且位置正確

- 路徑：`Assets/Game/Resources/Config/_Google Sheet 2 Json Setting.asset`
- 若不存在：Unity 選單 → `Assets > Create > Kaha Game Core > Editor > GoogleSheet2Json Setting` 建立，命名為 `_Google Sheet 2 Json Setting`，搬至 `Assets/Game/Resources/Config/`
- **必須**放在 `Config/`，因為工具會把輸出寫入 Setting.asset 所在目錄（見 § 1.8）

### 3.2 確認 sheetID

Inspector 欄位 `Sheet I D`（SerializedProperty 欄位名為 `sheetID`）：

```
1WjvMHTtEZ-JIRE1hmKz0pQMfI78hVI9jXC7ZLKEpLoo
```

**檢查方式**：打開 Google Sheets，網址 `https://docs.google.com/spreadsheets/d/<ID>/edit` 的 `<ID>` 必須與此一致。

### 3.3 確認 sheetNames 陣列

Inspector 欄位 `Sheet Names`（Array）須為以下 **13 筆**（順序不影響功能，但建議與 § 2 表對齊以利 diff 檢視）：

1. `AffinityConfig`
2. `CGSceneConfig`
3. `CharacterIntroConfig`
4. `CharacterQuestionsConfig`
5. `CombatConfig`
6. `CommissionRecipes`
7. `GreetingConfig`
8. `IdleChatConfig`
9. `MainQuestConfig`
10. `MonsterConfig`
11. `NodeDialogueConfig`
12. `StorageExpansionConfig`
13. `InitialResourcesConfig`

### 3.4 確認 Google Sheets 分頁實際存在且共享設定

- 登入 `wannasaynone@gmail.com` 帳號的 Google Sheets，確認 Spreadsheet 所有分頁標題與上述 13 筆完全一致（大小寫敏感）
- Sheet 檔案的共用權限至少為「知道連結的任何人可檢視」（Anyone with the link → Viewer），否則 Public API 會回 401/403

---

## 4. 日常操作步驟（Convert）

製作人或 dev-agent 需要重新匯出 Sheets 資料時，依下列順序操作。單輪匯出通常 < 60 秒。

### 4.1 確認 Google Sheets 為期望版本

- 在 Sheets 上確認所有分頁內容已是想匯入的版本（已存檔、無未完成列）
- 若剛編輯過，等 Google 雲端同步完成（通常 < 5 秒）

### 4.2 Unity Editor 操作

1. 在 Unity Project 視窗導至：`Assets/Game/Resources/Config/`
2. 選中 `_Google Sheet 2 Json Setting.asset`
3. Inspector 視窗應顯示：
   - `Sheet I D` 欄（已填）
   - `Sheet Names` Array（13 筆）
   - `Start Convert` 按鈕
4. 點 **`Start Convert`**
5. Unity Console 應依序顯示：
   - `output path=Assets/Game/Resources/Config`
   - 13 筆 `https://sheets.googleapis.com/...` URL
   - 每筆之後 `Text file is set`
   - 最後 `---End---`
6. Editor 自動觸發 `AssetDatabase.Refresh()`，Project 視窗中 13 個 `.txt` 的 meta 會更新

### 4.3 版控差異檢視

```
git diff Assets/Game/Resources/Config/*.txt
```

預期只看到資料內容差異。若出現 **GUID 或 meta 差異**，表示 `.txt` 被誤刪後重建 → 停止並回覆舊檔（Unity 會重新 assign GUID 會打斷 Prefab 引用，但 .txt 本身通常不被 GUID 引用，風險較低）。

### 4.4 跑反序列化測試

呼叫 dev-agent 跑：

```
Test Runner → EditMode → Run All
```

或最小：`*ConfigDataTests` 全綠即可（各 Config 的 `schema_version` / `IGameData` / `ID != 0` / 欄位對齊斷言皆覆蓋）。

### 4.5 commit

**不要 AI 自行 commit**（工作室規則 Git 操作僅限製作人）。將差異回報給製作人，由製作人決定 commit message 與時機。

---

## 5. 疑難排解

### 5.1 API rate limit（429 / 403 quota exceeded）

- Google Sheets Public API quota：**300 reads / min / project**（本 API key 為 KGC 共用）
- 13 分頁單輪 Convert 遠低於 quota，正常不會觸發
- 若多人同時 Convert 或 CI 自動化，可能超 quota → 人工等待 60 秒再試
- **不**建議切換 API key（KGC 共用 key，改動需走 KGC ADR）

### 5.2 `NOEX_` 欄位用法（設計者備忘）

KGC 工具支援在 Sheets 欄位名前綴加 `NOEX_`，表示該欄位**不匯出**到 JSON：

- 用途：暫時保留實驗欄位、設計備忘欄、中介計算欄
- 做法：將 Sheets 欄位 header 由 `foo` 改為 `NOEX_foo`，Convert 時該欄完全跳過
- 注意：`Contains("NOEX_")` 判斷是 substring，欄位名不可包含這串字作為語意字尾（例：`tag_NOEX_suffix` 也會被略過）

### 5.3 空列處理

- 完全空的列（values count <= 0）會被略過
- 部分空值的列（如某幾欄留白）會略過**那幾欄**但保留該列（其他欄位仍匯出）
- 結論：在 Sheets 插入空行作為區隔是**安全**的，不會產生 `{}` 噪音

### 5.4 欄位不對齊（Sheets vs C# DTO）

- **症狀**：Convert 成功但跑 `*ConfigDataTests` 時 assert failure（欄位缺失 / 型別錯）
- **排查**：
  1. 讀 `Assets/Game/Resources/Config/<檔名>.txt` 第一筆 object，確認欄位名
  2. 對照 DTO 的 `[SerializeField]` / public property
  3. 確認 Sheets 欄位名、JSON key、C# property 三者**完全一致**（camelCase ↔ PascalCase 轉換須集中在反序列化層，見 `.claude/rules/data-files.md`）
- **修法**：改 Sheets 欄位名（設計端）或改 DTO property（程式端）對齊；**禁止**手改 `.txt`（見 § 6 禁忌）

### 5.5 Unity MCP 無法觸發 `Start Convert`

- **現況**：Custom Inspector 的 `GUILayout.Button` 無法被 Unity MCP 遠端點擊
- **對策**：必須在 Unity Editor 有頭模式（headed）下，由製作人或 dev-agent 人工操作 Inspector
- **未來選項**（登記為技術債待評估）：於 KGC Editor 加 `[MenuItem]` 版本 `Start Convert`，讓 MCP 可透過 `execute_menu_item` 觸發。因會改動 KGC 共用框架，需獨立 ADR 評估

### 5.6 401 / 403 Unauthorized

- 可能原因：Google Sheets 分頁的共用權限未設為「知道連結者可檢視」
- 修法：在 Sheets 按「共用」→「一般存取權」→ 改為「知道連結的任何人：檢視者」

### 5.7 輸出檔案不在預期目錄

- `StartConvertTo()` 用 `AssetDatabase.GetAssetPath(target)` 推斷輸出目錄
- 若 Setting.asset 被搬家，輸出目錄會跟著變
- **ProjectDR 規則**：Setting.asset 必須釘在 `Assets/Game/Resources/Config/`，搬移視同違反 ADR-002 C01

---

## 6. 與 ADR-001 的對齊檢查清單

每次 `Start Convert` 完成後，dev-agent / dev-head 驗收時依序檢查：

- [ ] 每筆記錄都有 `id` 欄位（int，非 0）
- [ ] 語意字串主鍵類別額外有對應 `*_id` 欄位（如 `recipe_id` / `quest_id` / `line_id` 等）
- [ ] 反序列化測試 `*ConfigDataTests` 全部通過（含 `IGameData` 實作斷言 + `ID != 0` 斷言，見 ADR-001 § 測試要求）
- [ ] 新加欄位在 Sheets 存在的同時，對應 C# DTO 也有 `[SerializeField]` / public property
- [ ] `.txt` 副檔名保持，**不可**手動改成 `.json`（Unity TextAsset 對 `.txt` 的處理與 `.json` 不同；全專案已統一 `.txt`）
- [ ] 檔案位置保持在 `Assets/Game/Resources/Config/`（Resources.Load 路徑依賴）
- [ ] 無新增繞過 `IGameData` 的 DTO（違反時須加 `// EXEMPT: ADR-002 A-NN` 或走 ADR-001 豁免流程）

---

## 7. 與 ADR-002 的 mapping

ADR-002 退出 Gate 有三個項目直接綁本 spec：

| ADR-002 條目 | 對應本 spec 內容 |
|-------------|----------------|
| **[B]** Sheets 對齊清單 | § 2 ProjectDR 實際 Sheets 對應表（13 分頁 × 檔名 × DTO × IGameData 狀態） |
| **[C01]** 匯出工具就緒 | KGC 工具（§ 1）+ 本 spec 第 3 節首次設定已完成（2026-04-22） |
| **[C02]** 匯出流程文件就緒 | 本 spec 本身 |

完成本 spec 後，ADR-002 § [C02] 可打勾 ✅。

---

## 8. 已知限制（登記為 tech-debt / ADR 候選，非本工具 bug）

以下為本 spec 編寫時確認的限制，已登記或建議登記為技術債，**不**在本 Sprint 內處理：

### 8.1 API Key 硬編於 KGC 共用框架

- **現況**：`GoogleSheet2JsonSettingEditor.cs:12` 將 API key 直接寫在 `GET_DATA_URL` 常數中
- **影響**：
  - Key 無法輪替（若洩漏需改 KGC 原始碼）
  - 無法用環境變數注入（CI / 多工作室環境）
  - 屬跨專案議題（所有使用 KGC 的專案共用同一 key）
- **建議處置**：登記 `tech-debt.md`；若要修，需開 KGC 層面 ADR（不只 ProjectDR）。**不**在 ProjectDR Sprint 7 處理範圍

### 8.2 工具輸出與 GameStaticDataDeserializer 序列化格式一致（目前正常）

- **現況**：工具輸出用 `JsonFx.Json.JsonWriter.Serialize`，ProjectDR runtime `GameStaticDataDeserializer` 也走 JsonFx
- **風險點**：若未來 KGC 升級 JsonFx 版本或換序列化庫（System.Text.Json / Newtonsoft），可能導致欄位大小寫 / 空值 / 陣列格式不一致
- **對策**：反序列化測試全覆蓋，任何不一致會立即被 `*ConfigDataTests` 抓出。若 CI 出現未預期的 failure 且指向序列化差異，先檢查 KGC 版本是否變動

### 8.3 Unity MCP 無法觸發 Custom Inspector 按鈕

- **現況**：見 § 5.5；MCP 只能透過 `execute_menu_item` 觸發 MenuItem，不能點 CustomEditor 的 `GUILayout.Button`
- **影響**：離席模式（`/away`）或 CI 流水線無法自動匯出 Sheets → JSON；製作人必須人工在 Unity Editor 操作
- **未來選項**：於 KGC Editor 加 `[MenuItem("Kaha Game Core/Convert All Sheets")]` 包 wrapper，讓 MCP 可自動觸發；需開 KGC 層面 ADR 評估
- **當前**：接受限制，每次匯出人工觸發

### 8.4 單筆 HTTP 逐一請求（無並行）

- **現況**：`StartConvertTo()` for-loop 內同步呼叫 13 筆 HTTP，每筆獨立建立 `HttpWebRequest`
- **影響**：13 分頁 × ~2-3s / 筆 ≈ 30-40 秒。對當前規模（13 分頁）可接受
- **未來選項**：若分頁數成長至 30+，可考慮並行 / batch API；當前不處理

### 8.5 無 schema_version 驗證

- **現況**：工具直接把 Sheets 當前 row 結構匯出；若 Sheets 欄位變動，JSON 跟著變
- **風險**：設計者改 Sheets 不同步改 DTO → runtime crash；**但 ProjectDR 有測試把關**，CI 跑測試即能抓出
- **建議處置**：工具本身不擴充；靠 `*ConfigDataTests` 與 `.claude/hooks/validate-assets.sh`（ADR-002 C03 已擴充）攔截

---

## 9. 版本更新紀錄

| 版本 | 日期 | 變更摘要 |
|------|------|---------|
| v1.0 | 2026-04-22 | 初版建立（Sprint 7 A6-4；dev-head 主筆） |

---

## 相關連結

- **相關 ADR**：
  - [ADR-001 資料治理契約](../adrs/ADR-001-data-governance-contract.md)
  - [ADR-002 IT 階段例外退出 Gate](../adrs/ADR-002-it-stage-exemption-exit.md)
- **相關規則**：
  - [`.claude/rules/data-files.md`](../../../.claude/rules/data-files.md)
- **相關工作室級文件**：
  - [`doc/tech/development-workflow.md`](../../../doc/tech/development-workflow.md) § 5 / § 6
- **相關 KGC 資產**：
  - [`Assets/KahaGameCore/Editor/GoogleSheet2JsonSetting.cs`](../Assets/KahaGameCore/Editor/GoogleSheet2JsonSetting.cs)
  - [`Assets/KahaGameCore/Editor/GoogleSheet2JsonSettingEditor.cs`](../Assets/KahaGameCore/Editor/GoogleSheet2JsonSettingEditor.cs)
- **相關 tech-debt**：
  - `tech-debt.md` — 本 spec § 8 已知限制登記候選（TD-2026-XXX，待 dev-head 於 D5 統一編號）
