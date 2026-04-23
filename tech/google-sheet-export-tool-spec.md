# Google Sheet Export Tool — ProjectDR 使用規範

> **建立日期**: 2026-04-22（Sprint 7 A6-4）
> **主責**: dev-head
> **狀態**: Active
> **對應 ADR**: ADR-001（資料治理契約）、ADR-002 § [C01] / [C02]
> **對應 Sprint 項目**: Sprint 7 A6（匯出工具串接 + 文件補齊）

---

## 0. TL;DR

本工具**不是** ProjectDR 新建，而是 **KahaGameCore（KGC）框架**提供的 Editor tool。ProjectDR 所做的只有「串接 + 設定」：

- Setting 資產位置：`Assets/Game/Resources/Config/_Google Sheet 2 Json Setting.asset`
- Sheets 來源：`1WjvMHTtEZ-JIRE1hmKz0pQMfI78hVI9jXC7ZLKEpLoo`
- **22 個分頁 → 22 個 `.txt` 匯出檔**（Sprint 8 重構後規模；主表 + 子表 + singleton 三類，PascalCase 命名；詳見 § 2）
- 另有 1 個 ADR-002 A15 豁免檔 `player-questions-config.txt`（舊命名，手動維護，不被 Convert 覆蓋）
- 觸發方式：Unity Inspector 手動點 `Start Convert` 按鈕（Unity MCP **無法**自動觸發 Custom Inspector 按鈕）
- 輸出副檔名為 `.txt`（製作人 2026-04-22 拍板；與 Unity TextAsset 慣例一致）
- 輸出格式：**純陣列 JSON**（Sprint 8 後，ADR-002 Full Exit）— 頂層 `[{...}, {...}, ...]` 每筆為 IGameData entry，**不再**使用 `{schema_version, note, xxx_array}` 包裹物件

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

## 2. ProjectDR 實際 Sheets 對應表（v1.1 Sprint 8 重構後）

Sheets 來源：`1WjvMHTtEZ-JIRE1hmKz0pQMfI78hVI9jXC7ZLKEpLoo`。

- Sprint 7（2026-04-22）：建立 13 個 `*Config` 包裹物件分頁
- Sprint 8（2026-04-22）：ADR-002 Full Exit 重構 —— 舊 13 個 `*Config` 分頁全數刪除，重建為 **22 個 PascalCase 主/子/singleton 分頁**，採純陣列 `IGameData` 格式

輸出位置：`Assets/Game/Resources/Config/`，檔名規則為 **`<SheetName>.txt`** 與分頁名一一對應（PascalCase）。

### 2.1 主表 ↔ 子表對應（一對多規範化映射規則）

Sprint 8 Q3 / Q4 / Q7 拍板採「主表 + 子表」規範化模式，取代原本的管道符字串 / 巢狀物件。映射規則：

- **子表分頁命名**：採 `<主表名><LinesItemsOptionsRequirementsUnlocks>` 後綴（例：`CharacterIntros` 主表 → `CharacterIntroLines` 子表）
- **外鍵欄位命名**：`<父表單數 snake_case>_id`（例：子表 `intro_id` → 主表 `CharacterIntros` 的 `intro_id` 語意鍵，或 `main_quest_id` → `MainQuests.quest_id`）
- **每張子表自帶獨立流水號 `id`**（第一欄）— 子表的 `id` 是子表自己的主鍵，不是繼承自父表
- **每張子表第二欄為 FK 欄**（指向父表的語意鍵；**非**父表的流水號 id）
- **主表與子表分為兩張分頁、各自是純陣列**，KGC 工具固定 1:1 分頁 → `.txt`，不在匯出時 join
- **Runtime 組裝**：由各 Manager（如 `CharacterIntroConfig` / `MainQuestManager` / `StorageExpansionManager`）在 Config 建構期透過 `GameStaticDataManager.GetGameData<子表 DTO>` 全掃 + FK 過濾 / 建 lookup map

本專案有 5 組主/子表關係 + 1 組軟綁定子表（無對應主表）：

| 主表分頁 | 子表分頁 | FK 欄位（子表 → 主表語意鍵） | 拍板依據 |
|---------|---------|---------------------------|---------|
| `CharacterIntros` | `CharacterIntroLines` | `intro_id` → `CharacterIntros.intro_id` | 原 Sheets 既有規範化模式 |
| `CharacterQuestions` | `CharacterQuestionOptions` | `question_id` → `CharacterQuestions.question_id` | Q7 拍板 |
| `IdleChat` | `IdleChatAnswers` | `topic_id` → `IdleChat.topic_id` | Q7 拍板 |
| `MainQuests` | `MainQuestUnlocks` | `main_quest_id` → `MainQuests.quest_id` | Q3 拍板（拆管道符 `unlock_on_complete`） |
| `StorageExpansionStages` | `StorageExpansionRequirements` | `stage_level` → `StorageExpansionStages.level` | Q4 拍板（拆管道符 `required_items`） |
| （無主表 / 軟綁定） | `NodeDialogueLines` | `node_id`（自然語言語意值，非 FK） | Q2 拍板，FK 僅為系統內部分組鍵 |

**跨系統綁定（非 FK）**：如 `MainQuests.completion_condition_value = "node_0_dialogue_complete"` 為自然語言語意值，不是 FK，不強制命名同構。

### 2.2 完整 22 分頁對應表

下表以 `DTO` 模組名、`IGameData` 主鍵設計、主/子/singleton 標記三面向列出全部 22 分頁。DTO 詳細欄位結構見 [`sprint-8-data-model-spec.md`](./sprint-8-data-model-spec.md) § 3 / § 4。

| # | Sheets 分頁名 | 輸出檔名 | 對應 C# DTO | 主/子/singleton | IGameData 主鍵 |
|---|--------------|---------|-------------|---------------|---------------|
| 1 | `Affinity` | `Affinity.txt` | `AffinityCharacterData` | 主 | ID = `id`, Key = `character_id` |
| 2 | `CGScene` | `CGScene.txt` | `CGSceneData` | 主 | ID = `id`, Key = `cg_scene_id` |
| 3 | `CharacterIntros` | `CharacterIntros.txt` | `CharacterIntroData` | 主 | ID = `id`, Key = `intro_id` |
| 4 | `CharacterIntroLines` | `CharacterIntroLines.txt` | `CharacterIntroLineData` | 子（FK `intro_id`） | ID = `id`, Key = `line_id` |
| 5 | `CharacterProfiles` | `CharacterProfiles.txt` | `CharacterProfileData` | 主（Q7 新增） | ID = `id`, Key = `character_id` |
| 6 | `CharacterQuestions` | `CharacterQuestions.txt` | `CharacterQuestionData` | 主 | ID = `id`, Key = `question_id` |
| 7 | `CharacterQuestionOptions` | `CharacterQuestionOptions.txt` | `CharacterQuestionOptionData` | 子（FK `question_id`） | ID = `id` |
| 8 | `Combat` | `Combat.txt` | `CombatConfigData` | singleton | ID = 1 |
| 9 | `CommissionRecipes` | `CommissionRecipes.txt` | `CommissionRecipeData` | 主 | ID = `id`, Key = `recipe_id` |
| 10 | `GiftSwords` | `GiftSwords.txt` | `GiftSwordData` | 主 | ID = `id`, Key = `sword_id` |
| 11 | `Greeting` | `Greeting.txt` | `GreetingData` | 主 | ID = `id`, Key = `greeting_id` |
| 12 | `IdleChat` | `IdleChat.txt` | `IdleChatTopicData` | 主 | ID = `id`, Key = `topic_id` |
| 13 | `IdleChatAnswers` | `IdleChatAnswers.txt` | `IdleChatAnswerData` | 子（FK `topic_id`） | ID = `id`, Key = `answer_id` |
| 14 | `InitialResourceGrants` | `InitialResourceGrants.txt` | `InitialResourceGrantData` | 主 | ID = `id`, Key = `grant_id` |
| 15 | `MainQuests` | `MainQuests.txt` | `MainQuestData` | 主 | ID = `id`, Key = `quest_id` |
| 16 | `MainQuestUnlocks` | `MainQuestUnlocks.txt` | `MainQuestUnlockData` | 子（FK `main_quest_id`） | ID = `id` |
| 17 | `Monster` | `Monster.txt` | `MonsterData` | 主 | ID = `id`, Key = `type_id` |
| 18 | `NodeDialogueLines` | `NodeDialogueLines.txt` | `NodeDialogueLineData` | 子（軟綁定 `node_id`） | ID = `id`, Key = `line_id` |
| 19 | `Personalities` | `Personalities.txt` | `PersonalityData` | 主（Q7 新增） | ID = `id`, Key = `personality_id` |
| 20 | `PersonalityAffinityRules` | `PersonalityAffinityRules.txt` | `PersonalityAffinityRuleData` | 關聯（Q7 新增） | ID = `id` |
| 21 | `StorageExpansionStages` | `StorageExpansionStages.txt` | `StorageExpansionStageData` | 主 | ID = `level` |
| 22 | `StorageExpansionRequirements` | `StorageExpansionRequirements.txt` | `StorageExpansionRequirementData` | 子（FK `stage_level`） | ID = `id` |

**統計**：22 Sheets 分頁 × 22 C# DTO × 22 `.txt` 檔（三層完全對齊）。

### 2.3 豁免 / 已廢棄分頁

| 檔案 | 對應 DTO | 狀態 |
|------|---------|------|
| `player-questions-config.txt`（舊 kebab-case 命名保留） | `PlayerQuestionsConfigData` | 🟡 豁免（ADR-002 A15；IT 階段 placeholder；**不在** `sheetNames[]`，不被 Convert 覆蓋；VS 階段重新評估後改造） |
| ~~`guard-first-meet-dialogue-config.txt`~~ | ~~`GuardFirstMeetDialogueConfigData`~~ | 已併入 `NodeDialogueLines.txt` 的 `node_id=guard_first_meet` 子集（ADR-002 A08） |
| ~~`guard-return-config.txt`~~ | ~~`GuardReturnConfigData`~~ | 已刪除（ADR-002 A09 dead code） |
| ~~舊 13 個 `*-config.txt`（kebab-case）~~ | ~~舊 13 個包裹物件 DTO~~ | 全數刪除（Sprint 8 Wave 2.6，2026-04-22；舊 `schema_version` + `xxx_array` 包裹層已 retired） |

**說明**：豁免檔案仍以手動 JSON 維護、不在 `sheetNames[]`，不被 `Start Convert` 覆蓋；進 VS 前需按 ADR-002 個別條目評估是否納入 Sheets。

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

Inspector 欄位 `Sheet Names`（Array）須為以下 **22 筆 PascalCase 分頁名**（順序不影響功能，建議依字母序填入以利 diff 檢視；與 § 2.2 對應表一致）：

1. `Affinity`
2. `CGScene`
3. `CharacterIntros`
4. `CharacterIntroLines`
5. `CharacterProfiles`
6. `CharacterQuestions`
7. `CharacterQuestionOptions`
8. `Combat`
9. `CommissionRecipes`
10. `GiftSwords`
11. `Greeting`
12. `IdleChat`
13. `IdleChatAnswers`
14. `InitialResourceGrants`
15. `MainQuests`
16. `MainQuestUnlocks`
17. `Monster`
18. `NodeDialogueLines`
19. `Personalities`
20. `PersonalityAffinityRules`
21. `StorageExpansionStages`
22. `StorageExpansionRequirements`

### 3.4 確認 Google Sheets 分頁實際存在且共享設定

- 登入 `wannasaynone@gmail.com` 帳號的 Google Sheets，確認 Spreadsheet 所有分頁標題與上述 22 筆完全一致（大小寫敏感，PascalCase）
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
   - `Sheet Names` Array（22 筆）
   - `Start Convert` 按鈕
4. 點 **`Start Convert`**
5. Unity Console 應依序顯示：
   - `output path=Assets/Game/Resources/Config`
   - 22 筆 `https://sheets.googleapis.com/...` URL
   - 每筆之後 `Text file is set`
   - 最後 `---End---`
6. Editor 自動觸發 `AssetDatabase.Refresh()`，Project 視窗中 22 個 `.txt` 的 meta 會更新

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
- 22 分頁單輪 Convert 遠低於 quota，正常不會觸發
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
| **[B]** Sheets 對齊清單 | § 2 ProjectDR 實際 Sheets 對應表（22 分頁 × 檔名 × DTO × IGameData 主鍵設計；Sprint 8 v1.1 重寫） |
| **[C01]** 匯出工具就緒 | KGC 工具（§ 1）+ 本 spec 第 3 節首次設定已完成（2026-04-22） |
| **[C02]** 匯出流程文件就緒 | 本 spec 本身 |

Sprint 8 完成後，ADR-002 v1.7 Executed（Full Exit），本 spec 與 [`sprint-8-data-model-spec.md`](./sprint-8-data-model-spec.md) 為該 gate 的主要產物。

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

- **現況**：`StartConvertTo()` for-loop 內同步呼叫 22 筆 HTTP，每筆獨立建立 `HttpWebRequest`
- **影響**：22 分頁 × ~2-3s / 筆 ≈ 45-60 秒。對當前規模可接受
- **未來選項**：若分頁數成長至 50+，可考慮並行 / batch API；當前不處理

### 8.5 無 schema_version 驗證

- **現況**：工具直接把 Sheets 當前 row 結構匯出；若 Sheets 欄位變動，JSON 跟著變
- **風險**：設計者改 Sheets 不同步改 DTO → runtime crash；**但 ProjectDR 有測試把關**，CI 跑測試即能抓出
- **建議處置**：工具本身不擴充；靠 `*ConfigDataTests` 與 `.claude/hooks/validate-assets.sh`（ADR-002 C03 已擴充）攔截

---

## 9. 版本更新紀錄

| 版本 | 日期 | 變更摘要 |
|------|------|---------|
| v1.0 | 2026-04-22 | 初版建立（Sprint 7 A6-4；dev-head 主筆） |
| v1.1 | 2026-04-23 | Sprint 8 Full Exit 後更新：移除 Sprint 7 狀態警告段；§ 2 對應表由 13 分頁包裹物件重寫為 22 個 PascalCase 分頁純陣列（主表 15 / 子表 6 / singleton 1），並補 § 2.1 主表↔子表一對多映射規則（子表命名 `<主表名><Lines/Items/...>`、FK 欄位 `<父表單數>_id`、runtime 組裝、跨系統綁定用自然語言語意值而非 FK）；§ 2.3 列 Sprint 8 廢棄清單（舊 13 個 kebab-case `*-config.txt`）；§ 3.3 `sheetNames[]` 更新為 22 筆 PascalCase；§ 4.2 / § 5.1 / § 8.4 數字同步；§ 7 ADR mapping 補 v1.7 Executed 指涉。 |

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
