# Sprint 8 資料模型 Spec — 純陣列 IGameData 化 × Sheets 分頁對齊

> **建立日期**：2026-04-22
> **主責**：dev-head
> **狀態**：Active（Sprint 8 Wave 2 產出）
> **對應 ADR**：ADR-001、ADR-002、ADR-004
> **對應 Sprint**：Sprint 8（A1 盤點 + B1 盤點後的設計定案）
> **授權依據**：製作人 2026-04-22 授權工作室「自主重構資料結構」，無需逐題回問
> **前置 dev-log**：`projects/ProjectDR/dev-logs/2026-04-22-17.md`（Wave 1 盤點報告）

---

## 0. TL;DR

- **6 技術決策全部由 dev-head 自主拍板**（製作人 2026-04-22 書面授權），核心方向為「規範化拆子表 + Sheets 所有分頁強制流水號 id + C# 端不自動補 id」。
- **最終資料規模**：15 個 Sheets 主表/子表新分頁 + 3 個規範化子表 + 1 個 singleton 系統 config 分頁 = **19 個分頁**；對應 **19 個 IGameData DTO**（含 3 個規範化子表 DTO + 1 個 singleton DTO）。
- **13 個舊 `*Config` 空分頁全部從 Sheets 刪除**（Q6 拍板：刪），對應 C# 端的包裹類一併移除。
- **舊 .txt 遷移由 dev-agent 代寫 Sheets**（機械性搬運），製作人僅需最後點 Convert。
- **Wave 2 dev-agent 派工建議**：切 5 批次，每批次由一個 dev-agent 負責，每批次內含「DTO 改寫 + 測試更新 + Sheets 補 header/資料」，最後一批收斂 Installer / 反序列化層。

---

## 1. 脈絡與範圍

### 1.1 本 Spec 處理的問題

Sprint 7 已完成 ADR-002 [A] 16 個 ConfigData 的 IGameData 改造，但 [B] Sheets 對齊與 [C] 匯出工具資料對齊發現結構不一致：

1. **Sheets 規範化**：主表 + 子表規範化（例 `CharacterIntros` + `CharacterIntroLines`），無包裹層
2. **KGC 工具**：固定 1:1 分頁 → `.txt` 純陣列格式匯出
3. **現有 JSON**：包裹物件格式（`{schema_version, note, xxx_array: [...]}`）

Sprint 8 的資料層目標：讓這三層對齊。本 spec 負責定案**哪些 DTO / 分頁 / 欄位要長什麼樣**，Wave 3+ 由 dev-agent 依此實作。

### 1.2 與 ADR 的關聯

| ADR | 本 spec 的關聯 |
|-----|---------------|
| **ADR-001** | 所有新 DTO 必須 `implements IGameData`；語意字串主鍵採雙欄位（`int ID` + `string Key`）；本 spec 的 19 個 DTO 全部遵循 |
| **ADR-002** | 本 spec 是 [B] + [C] 完整 PASS 的設計依據；Sprint 8 完成後 ADR-002 升 v1.7 Full Exit |
| **ADR-004** | 所有 DTO 歸各 `<Module>/Data/` 子資料夾；新 DTO 不得放 Village 根目錄；namespace 為 `ProjectDR.Village.<Module>` |

### 1.3 製作人授權原則（2026-04-22）

> 「自主重構資料結構，目的是後續可以在 Sheets 填資料，不需就每個技術細節回問。」

本 spec 的 6 技術決策（Q2~Q7）全部基於下列三項綜合考量：

1. **ADR-001 合規**：主鍵以 int ID 為準、跨查詢走 `GameStaticDataManager`
2. **最小結構變動**：不過度拆分、保留既有語意、避免產生過多空子層
3. **製作人填表友善**：Sheets header 命名直觀、欄位數量控制、子表 FK 清晰

---

## 2. 6 技術決策拍板（Q2~Q7）

### Q2：`NodeDialogueLines.node_id` 與 `MainQuests.quest_id` 命名對齊

#### 問題陳述

- `NodeDialogueLines` 的 `node_id` 取值為 `node_0` / `node_1` / `node_2` / `guard_first_meet`（劇情節點標示）
- `MainQuests` 的 `quest_id` 取值為 `T0` / `T1` / `T2`（主線任務 ID）
- 兩者語意**不完全對應**：`T0` 的完成條件是 `node0_dialogue_complete`（透過 `completion_condition_value` 欄位綁定），但 `T1` 的條件是 `node_2_dialogue_complete`（跳過 node_1）、`T2` 是 `guard_return_event_complete`（不是節點）。

#### 考慮過的選項

- **A**：`MainQuests` 新增 `node_id` 欄位（強制每個 quest 指一個 node）— 缺點：T2 不走 node，會產生 null / 空值欄位
- **B**：`NodeDialogueLines` 改用 `quest_id` 作外鍵 — 缺點：node 與 quest 非一對一（node_1 無對應 quest），會造成 null FK
- **C**：維持現況（命名不對齊，語意不同），C# 端透過 `completion_condition_value` 解析 — 現況已有實作
- **D**：**拆開兩種概念 + 明示命名約定**：`node_id` 是「劇情節點識別符」屬 NodeDialogue 系統內部鍵；`quest_id` 是「主線任務識別符」屬 MainQuest 系統內部鍵；兩者之間的綁定透過 `MainQuests.completion_condition_value` 以自然語言值表達（例：`node2_dialogue_complete`）而非 FK

#### 拍板結果：**選 C + 文件化（=D 的文件補強版）**

**技術理由**：

1. **語意本質不同**：節點是「劇情結構分段」，任務是「玩家目標項目」，強行共用 ID 會混淆兩種概念
2. **現況已穩定**：Sprint 6 已定案「T0→node_0 / T1→node_2 / T2→guard_return_event」的對應；硬改會拖動大量測試
3. **ADR-001 不強制欄位命名同構**：IGameData 只要求 `int ID`，各分頁的語意鍵名稱可獨立
4. **FK 已有既定用途**：`NodeDialogueLines.node_id` 是 NodeDialogue 系統內部 FK（指向一組對話），不是跨系統 FK；它跟 `MainQuests.quest_id` 不存在 FK 關係

**補強動作**（Sprint 8 C 區塊）：

- 在 `.claude/rules/data-files.md` 補「FK 命名慣例」條文：
  > 同系統內子表 FK 欄位以 `<父表單數>_id` 命名（例：`CharacterIntroLines.intro_id`）；跨系統綁定請使用 `completion_condition_value` / `trigger_id` 等自然語言語意值，不強制命名同構。

#### 對後續實作的影響

- **MainQuests**：欄位不變（保留 `quest_id` + `completion_condition_value`）；新增 `int id` 流水號 + 拆 `unlock_on_complete`（見 Q3）
- **NodeDialogueLines**：欄位不變（保留 `node_id` + `line_id`）；新增 `int id` 流水號
- **DTO 命名**：`MainQuestData`（IGameData）+ `NodeDialogueLineData`（IGameData）；兩者不共享父類

---

### Q3：`MainQuests.unlock_on_complete` 管道符混用多種 ID 類型

#### 問題陳述

現況：`unlock_on_complete` = `T1|node_0_complete|exploration_open`

同一欄位混三種語意類型：
- `T1` = quest_id（要解鎖的下一個任務）
- `node_0_complete` = event flag（標記某節點完成的事件旗標）
- `exploration_open` = feature unlock（解鎖某功能）

#### 考慮過的選項

- **A**：維持現況，C# 端用前綴分類解析（`T*` → quest；`*_complete` → event；其他 → feature）— 缺點：解析邏輯隱性、易出錯、Sheets 欄位混雜
- **B**：拆成三欄位 `unlock_quests` / `unlock_events` / `unlock_features`（各自管道符分隔）— 優點：語意清晰、製作人填表時分類明確
- **C**：拆為子表 `MainQuestUnlocks`（規範化 1:N）欄位：`id`、`main_quest_id`（FK）、`unlock_type`（quest/event/feature）、`unlock_value` — 優點：徹底規範化、查詢最靈活、支援未來擴充

#### 拍板結果：**選 C（拆子表 `MainQuestUnlocks`）**

**技術理由**：

1. **規範化與 ADR-001 精神一致**：IGameData 鼓勵每個 entity 有獨立 id，子表是最乾淨的規範化形式
2. **與 `CharacterIntroLines` 模式一致**：本專案已有主表/子表規範化範例（CharacterIntros ↔ CharacterIntroLines），沿用同模式降低心智負擔
3. **解析邏輯明示化**：`unlock_type` 欄位是 enum 值（`quest` / `event` / `feature`），程式端用 switch 處理，測試覆蓋明確
4. **製作人填表友善**：每條解鎖獨立一列，新增 / 修改 / 刪除單獨處理；不需記憶管道符順序與前綴規則
5. **既有資料量小**：當前僅 3 個主任務、合計 5~6 筆解鎖條目，規範化後也只增 6 列，成本極低

**對比選項 B 的劣勢**：

- B 雖然比 A 清晰，但仍保留「管道符字串」這個半結構化反模式，仍需 C# 端 split
- C 的 `unlock_value` 是 string 自由格式，足以涵蓋 B 三欄位的所有用途

#### 對後續實作的影響

- **MainQuests** 分頁移除 `unlock_on_complete` 欄位
- 新增 **`MainQuestUnlocks`** 子表分頁（流水號 id + main_quest_id FK + unlock_type + unlock_value）
- 新增 `MainQuestUnlockData` IGameData DTO
- `MainQuestManager` 查詢改為：透過 `GameStaticDataManager.GetGameData<MainQuestUnlockData>(int id)` 全掃，以 `main_quest_id` 過濾（或在 Config 建構期建 lookup map）
- 既有測試需新增 `MainQuestUnlockDataTests`

---

### Q4：`StorageExpansionStages.required_items` 複合字串

#### 問題陳述

現況：`required_items` = `material_wood:10|material_cloth:5`（管道符分隔 item:qty 對）

#### 考慮過的選項

- **A**：維持現況（複合字串），C# 端 split 後轉 `Dictionary<string, int>` — 缺點：無 FK 驗證、製作人打字錯不會即時發現
- **B**：拆為子表 `StorageExpansionRequirements`（規範化，欄位：`id`、`stage_level`（FK）、`item_id`、`quantity`）— 優點：欄位型別化、Sheets 填錯即時看出、C# 端只需單層 loop

#### 拍板結果：**選 B（拆子表 `StorageExpansionRequirements`）**

**技術理由**：

1. **與 Q3 同邏輯**：規範化路徑一致，全專案採單一模式（主表 + 子表）
2. **型別化**：`quantity` 為 int，Sheets 欄位型別推斷即可驗證；複合字串需手動 parse int 易出錯
3. **未來擴充容易**：若未來要加「替代品」「條件」等欄位，複合字串無法承載，子表可直接 append 欄位
4. **既有資料量極小**：5 階擴建 × 平均 3 項物資 ≈ 15 列，規範化成本可忽略

**保留設計**：`capacity_before` / `capacity_after` / `duration_seconds` / `description` 仍留在主表（每階段唯一），不拆子表。

#### 對後續實作的影響

- **StorageExpansionStages** 分頁移除 `required_items` 欄位
- 新增 **`StorageExpansionRequirements`** 子表分頁
- 新增 `StorageExpansionRequirementData` IGameData DTO
- `StorageExpansionManager.GetStageRequirements(int level)` 內部改從子表組合成 `Dictionary<string, int>` 回傳

---

### Q5：所有新分頁補 `id`（int 流水號）策略

#### 問題陳述

ADR-001 要求每個 IGameData 有 `int ID` 非 0 主鍵，但 Sheets 8 個新分頁目前**沒有** `id` 欄位（只有語意字串主鍵）。

#### 考慮過的選項

- **A**：在 Sheets 每分頁新增 `id` 欄位（製作人 / dev-agent 手動填流水號） — 優點：資料層真相來源完整、不需程式輔助；缺點：手動填 id 容易跳號、重複、不重用
- **B**：C# 端反序列化時自動生成序號（用陣列索引 + 1） — 優點：不需人工填 id；缺點：違反「Sheets 是單一真相來源」原則、重啟 / 重新匯入 / 刪除中間一列會使 id 不穩定
- **C**：語意字串主鍵轉哈希值作 ID（決定性 `string.GetHashCode()` 或固定算法） — 優點：不需人工填、穩定；缺點：哈希可能衝突、哈希值非連續、debug 時難辨識

#### 拍板結果：**選 A（Sheets 填 id 流水號，C# 不自動補）**

**技術理由**：

1. **ADR-001 § `.claude/rules/data-files.md` 明示**：「Sheets 是遊戲數值的唯一真相來源」。B 方案違反此原則
2. **穩定性**：id 穩定不隨列順序變動是 IGameData 契約的隱含要求；索引法在刪除中間列時會 id shift，造成測試和存檔對不上
3. **可預測性**：A 方案下，測試寫 `Assert.Equal(3, data[2].ID)` 是穩定的；B / C 會隨資料變動失效
4. **填寫成本可控**：Sheets 填 id 只是「複製上一行 + 1」，且 Sprint 8 一次性補完（dev-agent 代寫 Sheets）後新資料進來時單筆補即可
5. **哈希法（C）的衝突成本遠大於收益**：雖決定性，但 runtime 先 compute 再查 Dictionary<int, T> 無法預測查哪個 id，失去 `GetGameData<T>(id)` 的直觀性

**配套規則**（寫入 § 8 未來維護守則）：

- id 從 1 開始連續編號，不跳號、不重複、不重用已刪除的 id
- 刪除一筆：對應 id 直接不再使用（留空），新增改用下一個最大 id + 1
- 每個分頁獨立 id 空間（分頁 A 的 id=1 與分頁 B 的 id=1 不衝突，因為查詢時帶型別 `GetGameData<T>(1)`）

#### 對後續實作的影響

- 所有 19 個新分頁第一欄必須是 `id`（int），非 0
- 子表分頁第一欄仍是 `id`（子表自己的流水號），**不是**繼承自父表的 id；FK 另立欄位
- Sprint 8 B 區塊：dev-agent 代寫 Sheets 時，為所有新分頁填入 id 欄位
- C# 反序列化測試斷言：`data.Length > 0 && data.All(x => x.ID > 0)`

---

### Q6：13 個空舊 `*Config` 分頁處置

#### 問題陳述

13 個舊分頁（`AffinityConfig` ~ `StorageExpansionConfig`）在 Sprint 7 前建立但 Sprint 8 後被 8 個新分頁取代，目前全部空（無 header / 無資料）。

#### 考慮過的選項

- **A**：保留（作為歷史備份） — 缺點：`sheetNames[]` 可能誤填、製作人看 Sheets 時多 13 個無用分頁、未來新同事困惑
- **B**：刪除（從 Sheets 移除） — 優點：Sheets 乾淨、與 C# / 匯出工具完全對齊；缺點：若後悔無法直接復原（需從 Sheets 版本歷史還原）

#### 拍板結果：**選 B（全部刪除）**

**技術理由**：

1. **空分頁無備份價值**：這些分頁從未填入資料；「備份」意義只存在於「有資料被替換」的情境，此處不成立
2. **Sheets 有版本歷史**：Google Sheets 內建 30 天版本歷史，若真有誤刪可還原；不需靠保留空分頁做「備份」
3. **避免匯出工具誤觸**：Sprint 8 B3 要更新 `sheetNames[]`；若舊分頁還在，未來改 sheetNames 時容易誤把 13 個舊分頁名加回去
4. **認知負擔**：Sheets 一眼看到「空分頁 vs 有資料分頁」的分類一致性，新人上手時減少誤會
5. **ADR-002 Full Exit 乾淨**：[B] 區塊驗收時 Sheets 分頁數 = C# DTO 數 = `.txt` 數，三者一致

**執行動作**（Sprint 8 B3 前）：

- dev-agent（有 `mcp__google-sheets__update_cells` 權限）**無法刪除分頁**（MCP 工具限制）
- **製作人手動在 Google Sheets 刪除**這 13 個空分頁（逐一右鍵 → 刪除工作表）
- 刪除後 dev-agent 再更新 `sheetNames[]` 為新 19 分頁

#### 對後續實作的影響

- Sprint 8 新增一個工作項目 **B0**（執行於 B3 前）：「製作人於 Google Sheets 手動刪除 13 個舊空分頁」
- `google-sheet-export-tool-spec.md` § 2 對應表的 13 個舊分頁條目一併移除

---

### Q7：外層非 entry 欄位歸宿（3 個 DTO 受影響）

#### 問題陳述

以下「屬於外層包裹層但不是 entry」的欄位需找新家：

| 來源 DTO | 欄位 | 型別 | 用途 |
|---------|------|------|------|
| `AffinityConfig` | `defaultThresholds` | `int[]` | 未明確配置角色的預設好感度門檻 |
| `StorageExpansionConfig` | `initial_capacity` | `int` | 初始倉庫容量（level=0 時） |
| `StorageExpansionConfig` | `max_expansion_level` | `int` | 最大可擴建等級 |
| `CharacterQuestionsConfig` | `personality_types` | 物件陣列（4 筆：id/name/description） | 個性類型定義 |
| `CharacterQuestionsConfig` | `character_personality_preference` | 物件（`<char_id>: <personality_id>`） | 角色的個性偏好 |
| `CharacterQuestionsConfig` | `personality_affinity_map` | 物件（`<char_id>.<personality_id>: <score>`） | 個性 × 角色 → 好感度增量 |

#### 考慮過的選項

- **A**：移入 C# 常數 — 違反 TR-data-001「數值外部化」，否決
- **B**：每個外層欄位另開 singleton config DTO（`AffinitySystemConfigData` 等，ID=1） — 缺點：分頁數量膨脹（每種 metadata 一個 singleton 分頁）
- **C**：將 metadata 拆入相關 entry（每個 entry 帶 default 值） — 缺點：預設值重複 N 次、不符合正規化
- **D**：**聚合一個系統級 singleton 分頁**：`VillageSystemConfig`，存放所有跨 entry 的 system-level 配置（每欄位為獨立欄）

#### 拍板結果：**混合方案**（依欄位性質分處）

| 來源欄位 | 拍板歸宿 | 理由 |
|---------|---------|------|
| `AffinityConfig.defaultThresholds` | `Affinity` 主表新增一筆「預設 entry」：`id=0` 不可用（ADR-001 禁用），改為在 `Affinity` 主表加一筆 `character_id="__default__"` 的 entry 作為 fallback | 最小結構變動：不新建分頁，單筆 entry 即可；`__default__` 是 sentinel 值，程式端明示處理 |
| `StorageExpansionStages.initial_capacity` | 轉為主表新增一筆 **`level=0` 的 entry**：`capacity_before=0`, `capacity_after=100`（= 原 initial_capacity）, `duration_seconds=0`, 無 `StorageExpansionRequirements` 對應子表 | 初始容量本質就是「等級 0 的 capacity」，併入主表語意最自然 |
| `StorageExpansionStages.max_expansion_level` | **移除**（改由 runtime `int.Max(stages.Select(s => s.level))` 推導） | 此欄位是「stages 最大 level」的快取；Sheets 若維護此欄位反而違反單一真相來源（資料冗餘易不一致） |
| `CharacterQuestionsConfig.personality_types` | 新增獨立分頁 **`Personalities`**（IGameData，4 筆） | 個性類型是跨系統概念（CharacterQuestions / 未來可能的對話分支），值得獨立管理 |
| `CharacterQuestionsConfig.character_personality_preference` | 併入 **`CharacterProfiles`** 分頁（本 spec 新增，作為跨系統角色側資料容器，後續可擴充） | 這是「某角色的個性傾向」，屬角色基本檔案的一部分 |
| `CharacterQuestionsConfig.personality_affinity_map` | 新增獨立分頁 **`PersonalityAffinityRules`**（IGameData：id / character_id / personality_id / affinity_delta） | 這是「個性 × 角色」的交叉規則，明確為多對多關聯，值得獨立規範化 |

**綜合影響**：新增 3 個系統級分頁（`Personalities`、`CharacterProfiles`、`PersonalityAffinityRules`）。加上 Q3/Q4 的兩個子表（`MainQuestUnlocks`、`StorageExpansionRequirements`），規模已從「15 個新 DTO」擴大到 **19 個 DTO + 19 個 Sheets 分頁**。

**為何不走 D（聚合 singleton）？**

- D 方案會產生一個 Sheets 欄位爆多的分頁（17 欄以上），違反 Sheets「一列一筆 entity」的天然格式
- singleton 的 IGameData 也彆扭（`ID=1` 的固定值沒有查詢意義）
- 拆分後的 4 個分頁（`Personalities` / `CharacterProfiles` / `PersonalityAffinityRules` / 主表擴充 entry）語意各自獨立，Sheets 易維護

#### 對後續實作的影響

- **Affinity**：不新增 singleton 分頁；`AffinityConfig.GetThresholds(string characterId)` 改為查詢 `__default__` entry 作為 fallback
- **StorageExpansion**：主表補 level=0 entry；移除 `initial_capacity` / `max_expansion_level` 獨立欄位
- **CharacterQuestions**：外層 3 個 metadata 欄位拆到 3 個新分頁
- **DTO 新增**：`PersonalityData`、`CharacterProfileData`、`PersonalityAffinityRuleData`

---

### 拍板結論一覽

| 題號 | 一句話結論 |
|------|----------|
| **Q2** | 維持 `node_id` / `quest_id` 各自獨立語意，不強制對齊；透過 `completion_condition_value` 軟綁定 |
| **Q3** | `unlock_on_complete` 拆子表 `MainQuestUnlocks`（id / main_quest_id / unlock_type / unlock_value） |
| **Q4** | `required_items` 拆子表 `StorageExpansionRequirements`（id / stage_level / item_id / quantity） |
| **Q5** | Sheets 每分頁第一欄填 `id`（int 流水號，從 1 連續）；C# 端不自動補；製作人透過 dev-agent 代寫 |
| **Q6** | 13 個舊空分頁全部刪除（製作人於 Sheets 手動刪，dev-agent 無權限） |
| **Q7** | 混合：metadata 欄位依語意歸入「主表 sentinel entry」/「主表新增 level=0 entry」/「獨立新分頁」/「刪除冗餘」四種處置 |

---

## 3. 19 個 IGameData DTO 結構定案

本章節按分頁名字母序列出。每 DTO 提供以下欄位：

- DTO 類別名、檔案路徑、對應 Sheets 分頁、對應 `.txt` 檔、主鍵來源、欄位清單、IGameData 實作方式

**命名原則**（ADR-001 + ADR-004 + 本 spec）：

1. 去包裹類、避免 `Config` 後綴（除非是 singleton 的系統級設定）
2. 主表 DTO 命名 `<Master>Data`（例：`MainQuestData`）
3. 子表 DTO 命名 `<Master>LineData` / `<Master>EntryData` / 語意描述（例：`CharacterIntroLineData`、`MainQuestUnlockData`）
4. 系統級 singleton DTO 以 `Config` 後綴明示（例：`CombatConfigData`）
5. 所有 DTO 放 `<Module>/Data/` 子資料夾（ADR-004 D2），namespace 為 `ProjectDR.Village.<Module>`

---

### 3.1 `AffinityCharacterData`（主表）

| 屬性 | 內容 |
|------|------|
| 類別名 | `AffinityCharacterData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/Affinity/Data/AffinityCharacterData.cs` |
| 對應 Sheets 分頁 | `Affinity` |
| 對應 `.txt` 檔 | `Affinity.txt` |
| Namespace | `ProjectDR.Village.Affinity` |
| 主鍵來源 | `int id`（流水號） + `string character_id`（語意外鍵） |
| FK | 無 |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵，流水號 |
| `character_id` | string | Y | 角色識別符；特殊值 `__default__` 表示 fallback entry（Q7 拍板） |
| `thresholds` | `int[]` | Y | 好感度升級門檻升序陣列 |

**備註**：舊 `AffinityConfig.defaultThresholds` 併入一筆 `character_id="__default__"` 的 entry（Q7）。

---

### 3.2 `CGSceneData`（主表）

| 屬性 | 內容 |
|------|------|
| 類別名 | `CGSceneData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/CG/Data/CGSceneData.cs` |
| 對應 Sheets 分頁 | `CGScene` |
| 對應 `.txt` 檔 | `CGScene.txt` |
| Namespace | `ProjectDR.Village.CG` |
| 主鍵來源 | `int id` + `string cg_scene_id` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `cg_scene_id` | string | Y | CG 場景語意識別符（外鍵用） |
| `character_id` | string | Y | 主角角色 ID |
| `required_threshold` | int | Y | 觸發所需的好感度 |
| `dialogue_id` | int | Y | 對應對話識別符（暫用 int，後續可改為外鍵至 NodeDialogue 或另一系統） |
| `display_name` | string | Y | 顯示名稱（繁中）|

---

### 3.3 `CharacterIntroData`（主表）

| 屬性 | 內容 |
|------|------|
| 類別名 | `CharacterIntroData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/CharacterIntro/Data/CharacterIntroData.cs` |
| 對應 Sheets 分頁 | `CharacterIntros` |
| 對應 `.txt` 檔 | `CharacterIntros.txt` |
| Namespace | `ProjectDR.Village.CharacterIntro` |
| 主鍵 | `int id` + `string intro_id` |
| 子表關聯 | `CharacterIntroLineData.intro_id` FK → 本表 `intro_id` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `intro_id` | string | Y | 登場 CG 識別符 |
| `character_id` | string | Y | 角色 ID |
| `cg_sprite_id` | string | Y | 對應立繪資源 ID |
| `scene_description` | string | Y | 場景描述（供美術 CG 參考） |
| `word_count_target` | int | Y | 撰寫字數目標 |

---

### 3.4 `CharacterIntroLineData`（子表，FK `intro_id` → CharacterIntros）

| 屬性 | 內容 |
|------|------|
| 類別名 | `CharacterIntroLineData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/CharacterIntro/Data/CharacterIntroLineData.cs` |
| 對應 Sheets 分頁 | `CharacterIntroLines` |
| 對應 `.txt` 檔 | `CharacterIntroLines.txt` |
| Namespace | `ProjectDR.Village.CharacterIntro` |
| 主鍵 | `int id` + `string line_id` |
| FK | `intro_id` → `CharacterIntros.intro_id` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵（子表自身流水號） |
| `line_id` | string | Y | 對白識別符 |
| `intro_id` | string | Y | FK 至主表 |
| `sequence` | int | Y | 在主表內的播放順序 |
| `speaker` | string | Y | 說話者（`narrator` / `player` / `<角色名>`） |
| `text` | string | Y | 對白文字 |
| `line_type` | string | Y | 對白類型（`narration` / `dialogue` 等） |

---

### 3.5 `CharacterProfileData`（新增主表，Q7 拍板）

| 屬性 | 內容 |
|------|------|
| 類別名 | `CharacterProfileData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/CharacterQuestions/Data/CharacterProfileData.cs` |
| 對應 Sheets 分頁 | `CharacterProfiles` |
| 對應 `.txt` 檔 | `CharacterProfiles.txt` |
| Namespace | `ProjectDR.Village.CharacterQuestions` |
| 主鍵 | `int id` + `string character_id` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `character_id` | string | Y | 角色 ID |
| `preferred_personality_id` | string | Y | 該角色偏好的個性類型（FK → Personalities.personality_id） |

**備註**：承接 Q7 拍板，原 `CharacterQuestionsConfig.character_personality_preference` 拆入本分頁。未來可擴充（例 `display_name`、`default_mood` 等跨系統角色基本資料）。**若未來角色基本資料走獨立模組**，此 DTO 可搬移至 `Village/CharacterUnlock/Data/` 或類似位置；本 Sprint 先歸 `CharacterQuestions`（因為資料來源為舊 CharacterQuestions config）。

---

### 3.6 `CharacterQuestionData`（主表）

| 屬性 | 內容 |
|------|------|
| 類別名 | `CharacterQuestionData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/CharacterQuestions/Data/CharacterQuestionData.cs` |
| 對應 Sheets 分頁 | `CharacterQuestions` |
| 對應 `.txt` 檔 | `CharacterQuestions.txt` |
| Namespace | `ProjectDR.Village.CharacterQuestions` |
| 主鍵 | `int id` + `string question_id` |
| 子表關聯 | `CharacterQuestionOptionData.question_id` FK |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `question_id` | string | Y | 問題語意識別符 |
| `character_id` | string | Y | 詢問者角色 ID |
| `level` | int | Y | 好感度等級（1~7） |
| `prompt` | string | Y | 問題文字 |

**備註**：4 選項拆到子表 `CharacterQuestionOptionData`（見 3.7）。

---

### 3.7 `CharacterQuestionOptionData`（子表，FK `question_id`）

| 屬性 | 內容 |
|------|------|
| 類別名 | `CharacterQuestionOptionData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/CharacterQuestions/Data/CharacterQuestionOptionData.cs` |
| 對應 Sheets 分頁 | `CharacterQuestionOptions` |
| 對應 `.txt` 檔 | `CharacterQuestionOptions.txt` |
| Namespace | `ProjectDR.Village.CharacterQuestions` |
| 主鍵 | `int id`（子表自身） |
| FK | `question_id` → `CharacterQuestions.question_id`；`personality_id` → `Personalities.personality_id` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `question_id` | string | Y | FK 至主表 |
| `personality_id` | string | Y | FK 至 `Personalities`（四選項對應四個性） |
| `text` | string | Y | 選項文字 |
| `response` | string | Y | 角色選擇後的回應台詞 |

**備註**：本 DTO 為 **Sprint 8 拍板新增的 3 個子表之一**（補償 Q7 拍板下原 options 巢狀陣列需展開）。現況 JSON 每題有 4 options 陣列，280 題共 1120 筆選項資料，規範化後為 1120 列。

---

### 3.8 `CombatConfigData`（singleton 系統 config）

| 屬性 | 內容 |
|------|------|
| 類別名 | `CombatConfigData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/Exploration/Combat/Data/CombatConfigData.cs`（已存在，本 Sprint 調整結構但路徑不變） |
| 對應 Sheets 分頁 | `Combat` |
| 對應 `.txt` 檔 | `Combat.txt` |
| Namespace | `ProjectDR.Village.Exploration.Combat` |
| 主鍵 | `int id`（固定為 1，只有一筆資料） |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**（Sheets 全部扁平化，原 playerStats / sword 巢狀物件展開為欄位前綴）：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | 固定為 1 |
| `player_max_hp` | int | Y | 玩家最大 HP |
| `player_atk` | int | Y | 玩家 ATK |
| `player_def` | int | Y | 玩家 DEF |
| `player_spd` | int | Y | 玩家 SPD |
| `sword_angle_degrees_half` | float | Y | 劍攻擊半角度 |
| `sword_range` | float | Y | 劍攻擊範圍 |
| `sword_base_cooldown_seconds` | float | Y | 劍攻擊基礎冷卻 |
| `sword_spd_cooldown_factor` | float | Y | SPD 對冷卻的影響係數 |
| `move_speed_base` | float | Y | 移動速度基礎值 |
| `spd_move_speed_factor` | float | Y | SPD 對移動速度的影響係數 |
| `free_movement_base_speed` | float | Y | 自由移動基礎速度 |
| `spd_free_movement_speed_factor` | float | Y | SPD 對自由移動的影響係數 |
| `knockback_distance` | float | Y | 擊退距離 |
| `knockback_duration` | float | Y | 擊退持續時間 |

**備註**：原 JSON 為 singleton object，純陣列化後為「單元素陣列」`[{id:1, ...}]`。C# 反序列化層載入後取 `array[0]` 即可；`CombatConfig`（不可變物件）建構邏輯不變。

---

### 3.9 `CommissionRecipeData`（主表）

| 屬性 | 內容 |
|------|------|
| 類別名 | `CommissionRecipeData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/Commission/Data/CommissionRecipeData.cs` |
| 對應 Sheets 分頁 | `CommissionRecipes` |
| 對應 `.txt` 檔 | `CommissionRecipes.txt` |
| Namespace | `ProjectDR.Village.Commission` |
| 主鍵 | `int id` + `string recipe_id` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**（對齊現有 Sheets 的 9 欄）：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `recipe_id` | string | Y | 配方語意識別符 |
| `character_id` | string | Y | 委託執行者 |
| `input_item_id` | string | N | 輸入物品（守衛巡邏類可為空） |
| `input_quantity` | int | Y | 輸入數量（為 0 表示空手委託） |
| `output_item_id` | string | Y | 產出物品 |
| `output_quantity` | int | Y | 產出數量 |
| `duration_seconds` | int | Y | 完成秒數 |
| `workbench_slot_index_max` | int | Y | 工作台可用格子上限 |
| `description` | string | N | 設計備忘 |

---

### 3.10 `GiftSwordData`（主表，保留）

| 屬性 | 內容 |
|------|------|
| 類別名 | `GiftSwordData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/GuardReturn/Data/GiftSwordData.cs`（或 `Village/Gift/Data/` 由 dev-head 於實作時依引用路徑判定；預設 `GuardReturn`）|
| 對應 Sheets 分頁 | `GiftSwords` |
| 對應 `.txt` 檔 | `GiftSwords.txt` |
| Namespace | `ProjectDR.Village.GuardReturn`（或 `Village.Gift`，依歸宿） |
| 主鍵 | `int id` + `string sword_id` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**（對齊現有 Sheets 的 8 欄）：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `sword_id` | string | Y | 劍識別符 |
| `display_name` | string | Y | 顯示名稱 |
| `atk_bonus` | int | Y | ATK 加成 |
| `cooldown_modifier_seconds` | float | Y | 冷卻修正 |
| `range_modifier` | float | Y | 攻擊範圍修正 |
| `angle_modifier_degrees` | float | Y | 攻擊角度修正 |
| `special_effect` | string | N | 特殊效果識別符 |
| `description` | string | N | 設計備忘 |

---

### 3.11 `GreetingData`（主表）

| 屬性 | 內容 |
|------|------|
| 類別名 | `GreetingData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/Greeting/Data/GreetingData.cs` |
| 對應 Sheets 分頁 | `Greeting` |
| 對應 `.txt` 檔 | `Greeting.txt` |
| Namespace | `ProjectDR.Village.Greeting` |
| 主鍵 | `int id` + `string greeting_id` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `greeting_id` | string | Y | 招呼語識別符 |
| `character_id` | string | Y | 角色 ID |
| `level` | int | Y | 好感度等級（1~7） |
| `text` | string | Y | 招呼語文字 |

**備註**：現況約 280 筆（4 角色 × 7 級 × 10 句），規範化後直接 280 列。

---

### 3.12 `IdleChatTopicData`（主表）

| 屬性 | 內容 |
|------|------|
| 類別名 | `IdleChatTopicData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/IdleChat/Data/IdleChatTopicData.cs` |
| 對應 Sheets 分頁 | `IdleChat` |
| 對應 `.txt` 檔 | `IdleChat.txt` |
| Namespace | `ProjectDR.Village.IdleChat` |
| 主鍵 | `int id` + `string topic_id` |
| 子表關聯 | `IdleChatAnswerData.topic_id` FK |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `topic_id` | string | Y | 閒聊主題識別符 |
| `character_id` | string | Y | 角色 ID |
| `prompt` | string | Y | 角色發問文字 |

**備註**：現有 JSON 的 `answers` 3 元素陣列拆到子表（見 3.13）；原因同 3.7（巢狀陣列規範化）。

---

### 3.13 `IdleChatAnswerData`（子表，FK `topic_id`）

| 屬性 | 內容 |
|------|------|
| 類別名 | `IdleChatAnswerData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/IdleChat/Data/IdleChatAnswerData.cs` |
| 對應 Sheets 分頁 | `IdleChatAnswers` |
| 對應 `.txt` 檔 | `IdleChatAnswers.txt` |
| Namespace | `ProjectDR.Village.IdleChat` |
| 主鍵 | `int id`（子表自身） |
| FK | `topic_id` → `IdleChat.topic_id` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `answer_id` | string | Y | 回答識別符 |
| `topic_id` | string | Y | FK |
| `text` | string | Y | 回答文字 |

**備註**：4 角色 × 20 題 × 3 回答 = 240 列。**本 DTO 為 Sprint 8 拍板新增的 3 個子表之一**。

---

### 3.14 `InitialResourceGrantData`（主表）

| 屬性 | 內容 |
|------|------|
| 類別名 | `InitialResourceGrantData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/Progression/Data/InitialResourceGrantData.cs` |
| 對應 Sheets 分頁 | `InitialResourceGrants` |
| 對應 `.txt` 檔 | `InitialResourceGrants.txt` |
| Namespace | `ProjectDR.Village.Progression` |
| 主鍵 | `int id` + `string grant_id` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**（對齊現有 Sheets 的 5 欄）：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `grant_id` | string | Y | 發放規則識別符 |
| `trigger_id` | string | Y | 觸發條件識別符 |
| `item_id` | string | N | 發放物品 ID（空 = 不發物） |
| `quantity` | int | Y | 發放數量（0 = 不發） |
| `description` | string | N | 設計備忘 |

---

### 3.15 `MainQuestData`（主表）

| 屬性 | 內容 |
|------|------|
| 類別名 | `MainQuestData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/MainQuest/Data/MainQuestData.cs` |
| 對應 Sheets 分頁 | `MainQuests` |
| 對應 `.txt` 檔 | `MainQuests.txt` |
| Namespace | `ProjectDR.Village.MainQuest` |
| 主鍵 | `int id` + `string quest_id` |
| 子表關聯 | `MainQuestUnlockData.main_quest_id` FK |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**（承 Q3 拍板移除 `unlock_on_complete`）：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `quest_id` | string | Y | 任務識別符（T0 / T1 / T2）|
| `display_name` | string | Y | 顯示名稱 |
| `description` | string | Y | 任務描述 |
| `owner_character_id` | string | N | 任務持有角色（空 = 非角色派發） |
| `completion_condition_type` | string | Y | 完成條件類型（`auto` / `dialogue_end` / `first_explore`） |
| `completion_condition_value` | string | Y | 完成條件值（自然語言，非 FK） |
| `reward_grant_ids` | string | N | 完成獎勵 grant_id（多個以 `\|` 分隔；未來可考慮再拆子表） |
| `sort_order` | int | Y | 排序 |

**備註**：`reward_grant_ids` 仍保留管道符字串（當前僅 3 個任務，複雜度低，暫不規範化）；若未來 reward 結構變複雜可再拆子表。

---

### 3.16 `MainQuestUnlockData`（子表，FK `main_quest_id` → MainQuests）

| 屬性 | 內容 |
|------|------|
| 類別名 | `MainQuestUnlockData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/MainQuest/Data/MainQuestUnlockData.cs` |
| 對應 Sheets 分頁 | `MainQuestUnlocks` |
| 對應 `.txt` 檔 | `MainQuestUnlocks.txt` |
| Namespace | `ProjectDR.Village.MainQuest` |
| 主鍵 | `int id`（子表自身） |
| FK | `main_quest_id` → `MainQuests.quest_id` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**（Q3 拍板新增）：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `main_quest_id` | string | Y | FK 至主表 quest_id |
| `unlock_type` | string | Y | 解鎖類型 enum（`quest` / `event` / `feature`） |
| `unlock_value` | string | Y | 解鎖值（依 type 解釋） |
| `sort_order` | int | N | 同一 quest 下解鎖觸發順序（可選） |

**備註**：**本 DTO 為 Sprint 8 拍板新增**（承 Q3）。

---

### 3.17 `MonsterData`（主表）

| 屬性 | 內容 |
|------|------|
| 類別名 | `MonsterData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/Exploration/Combat/Data/MonsterData.cs`（取代現有 `MonsterConfigData`） |
| 對應 Sheets 分頁 | `Monster` |
| 對應 `.txt` 檔 | `Monster.txt` |
| Namespace | `ProjectDR.Village.Exploration.Combat` |
| 主鍵 | `int id` + `string type_id` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**（`color` 巢狀物件扁平化為 r/g/b/a 四欄）：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `type_id` | string | Y | 魔物類型識別符（例：`Slime`） |
| `max_hp` | int | Y | 最大 HP |
| `atk` | int | Y | 攻擊力 |
| `def` | int | Y | 防禦力 |
| `spd` | int | Y | 速度 |
| `move_cooldown_seconds` | float | Y | 移動冷卻 |
| `vision_range` | int | Y | 視野範圍（格） |
| `attack_range` | int | Y | 攻擊範圍（格） |
| `attack_angle_degrees_half` | float | Y | 攻擊半角度 |
| `attack_prepare_seconds` | float | Y | 攻擊預備秒數 |
| `attack_cooldown_seconds` | float | Y | 攻擊冷卻 |
| `color_r` | float | Y | 顏色 R（0~1） |
| `color_g` | float | Y | 顏色 G |
| `color_b` | float | Y | 顏色 B |
| `color_a` | float | Y | 顏色 Alpha |

**備註**：巢狀 `color` 物件扁平化，因 Sheets 為 2D 表格，無法表達巢狀；C# 端反序列化後 `MonsterConfig`（不可變物件）建構期組合成 `Color` struct。

---

### 3.18 `NodeDialogueLineData`（子表，FK `node_id`，無對應主表 DTO）

| 屬性 | 內容 |
|------|------|
| 類別名 | `NodeDialogueLineData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/Dialogue/Data/NodeDialogueLineData.cs`（已存在，本 Sprint 繼續使用但調整欄位對齊） |
| 對應 Sheets 分頁 | `NodeDialogueLines` |
| 對應 `.txt` 檔 | `NodeDialogueLines.txt` |
| Namespace | `ProjectDR.Village.Dialogue` |
| 主鍵 | `int id` + `string line_id` |
| FK | `node_id` → （無專屬主表，是 MainQuest 系統的 `completion_condition_value` 軟綁定，Q2 拍板） |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**（對齊現有 7 欄）：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `line_id` | string | Y | 對白識別符 |
| `node_id` | string | Y | 節點識別符（非 FK，屬 NodeDialogue 系統內部鍵） |
| `sequence` | int | Y | 節點內播放順序 |
| `speaker` | string | Y | 說話者 |
| `text` | string | Y | 對白文字 |
| `line_type` | string | Y | 對白類型（`narration` / `dialogue` / `choice_prompt` / `choice_option` / `choice_response`） |
| `choice_branch` | string | N | 選擇分支標記（`farm_girl` / `witch` / 空） |

**備註**：**本 DTO 無專屬主表分頁**（NodeDialogue 沒有像 CharacterIntros 那樣的主表 metadata）。若未來需要儲存「每個 node 的起始條件、結束事件」等 metadata，可另建 `NodeDialogueMeta` 分頁；本 Sprint 不建。

---

### 3.19 `PersonalityData`（新增主表，Q7 拍板）

| 屬性 | 內容 |
|------|------|
| 類別名 | `PersonalityData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/CharacterQuestions/Data/PersonalityData.cs` |
| 對應 Sheets 分頁 | `Personalities` |
| 對應 `.txt` 檔 | `Personalities.txt` |
| Namespace | `ProjectDR.Village.CharacterQuestions` |
| 主鍵 | `int id` + `string personality_id` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `personality_id` | string | Y | 個性類型識別符（`personality_gentle` / `personality_lively` 等）|
| `display_name` | string | Y | 顯示名稱（繁中） |
| `description` | string | Y | 設計說明 |

---

### 3.20 `PersonalityAffinityRuleData`（新增，Q7 拍板）

| 屬性 | 內容 |
|------|------|
| 類別名 | `PersonalityAffinityRuleData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/CharacterQuestions/Data/PersonalityAffinityRuleData.cs` |
| 對應 Sheets 分頁 | `PersonalityAffinityRules` |
| 對應 `.txt` 檔 | `PersonalityAffinityRules.txt` |
| Namespace | `ProjectDR.Village.CharacterQuestions` |
| 主鍵 | `int id` |
| FK | `character_id` → `CharacterProfiles.character_id`；`personality_id` → `Personalities.personality_id` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**（承 Q7）：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `character_id` | string | Y | FK（目標角色）|
| `personality_id` | string | Y | FK（選項個性）|
| `affinity_delta` | int | Y | 對該角色的好感度增減值 |

**備註**：承 Q7 拍板，原 `personality_affinity_map` 的 `<char>.<personality>: <delta>` 巢狀物件拆為行資料。約 4 角色 × 4 個性 = 16 列。

---

### 3.21 `StorageExpansionStageData`（主表）

| 屬性 | 內容 |
|------|------|
| 類別名 | `StorageExpansionStageData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/Storage/Data/StorageExpansionStageData.cs`（已存在，本 Sprint 繼續使用） |
| 對應 Sheets 分頁 | `StorageExpansionStages` |
| 對應 `.txt` 檔 | `StorageExpansionStages.txt` |
| Namespace | `ProjectDR.Village.Storage` |
| 主鍵 | `int id`（可等於 level，方便查詢） |
| 子表關聯 | `StorageExpansionRequirementData.stage_level` FK |
| IGameData 實作 | `public int ID => id;`（id = level，由 Sheets 手填為 level 同值） |

**欄位清單**（承 Q4/Q7 拍板移除 `required_items`、併入 level=0 初始 entry、移除 max_expansion_level）：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵（= level，便於 `GetGameData<>(level)`）|
| `level` | int | Y | 擴建等級（0 = 初始，1~N = 擴建後）|
| `capacity_before` | int | Y | 擴建前容量（level=0 時為 0）|
| `capacity_after` | int | Y | 擴建後容量（level=0 時為 initial_capacity）|
| `duration_seconds` | int | Y | 擴建所需秒數（level=0 時為 0）|
| `description` | string | N | 設計備忘 |

**備註**：`id` 與 `level` 同值（資料冗餘，為查詢友善）；6 列（level 0~5）。

---

### 3.22 `StorageExpansionRequirementData`（子表，FK `stage_level`）

| 屬性 | 內容 |
|------|------|
| 類別名 | `StorageExpansionRequirementData` |
| 檔案路徑 | `projects/ProjectDR/Assets/Game/Scripts/Village/Storage/Data/StorageExpansionRequirementData.cs` |
| 對應 Sheets 分頁 | `StorageExpansionRequirements` |
| 對應 `.txt` 檔 | `StorageExpansionRequirements.txt` |
| Namespace | `ProjectDR.Village.Storage` |
| 主鍵 | `int id`（子表自身） |
| FK | `stage_level` → `StorageExpansionStages.level` |
| IGameData 實作 | `public int ID => id;` |

**欄位清單**（Q4 拍板新增）：

| 欄位 | 型別 | required | 說明 |
|------|------|----------|------|
| `id` | int | Y | IGameData 主鍵 |
| `stage_level` | int | Y | FK 至主表 level |
| `item_id` | string | Y | 所需物品 ID |
| `quantity` | int | Y | 所需數量 |

**備註**：**本 DTO 為 Sprint 8 拍板新增**（承 Q4）。level=0 entry 無子表對應列（初始容量不需物資）。實際資料約 5 級 × 平均 3 物資 = 15 列。

---

### 3.23 彙整表

22 個 DTO 彙整（依 module 分組）：

> **命名慣例**：`.txt` 檔名為 PascalCase（與 Sheets 分頁名完全一致），例 `Affinity.txt`、`CharacterIntros.txt`。KGC 匯出工具以分頁名 1:1 輸出，不做 case 轉換。

| # | DTO | Sheets 分頁 | .txt | Module | 主/子/singleton |
|---|-----|------------|------|--------|---------------|
| 1 | AffinityCharacterData | Affinity | Affinity.txt | Affinity | 主 |
| 2 | CGSceneData | CGScene | CGScene.txt | CG | 主 |
| 3 | CharacterIntroData | CharacterIntros | CharacterIntros.txt | CharacterIntro | 主 |
| 4 | CharacterIntroLineData | CharacterIntroLines | CharacterIntroLines.txt | CharacterIntro | 子（FK intro_id） |
| 5 | CharacterProfileData | CharacterProfiles | CharacterProfiles.txt | CharacterQuestions | 主 |
| 6 | CharacterQuestionData | CharacterQuestions | CharacterQuestions.txt | CharacterQuestions | 主 |
| 7 | CharacterQuestionOptionData | CharacterQuestionOptions | CharacterQuestionOptions.txt | CharacterQuestions | 子（FK question_id） |
| 8 | CombatConfigData | Combat | Combat.txt | Exploration.Combat | singleton |
| 9 | CommissionRecipeData | CommissionRecipes | CommissionRecipes.txt | Commission | 主 |
| 10 | GiftSwordData | GiftSwords | GiftSwords.txt | GuardReturn（或 Gift） | 主 |
| 11 | GreetingData | Greeting | Greeting.txt | Greeting | 主 |
| 12 | IdleChatTopicData | IdleChat | IdleChat.txt | IdleChat | 主 |
| 13 | IdleChatAnswerData | IdleChatAnswers | IdleChatAnswers.txt | IdleChat | 子（FK topic_id） |
| 14 | InitialResourceGrantData | InitialResourceGrants | InitialResourceGrants.txt | Progression | 主 |
| 15 | MainQuestData | MainQuests | MainQuests.txt | MainQuest | 主 |
| 16 | MainQuestUnlockData | MainQuestUnlocks | MainQuestUnlocks.txt | MainQuest | 子（FK main_quest_id） |
| 17 | MonsterData | Monster | Monster.txt | Exploration.Combat | 主 |
| 18 | NodeDialogueLineData | NodeDialogueLines | NodeDialogueLines.txt | Dialogue | 子（FK node_id 軟綁定） |
| 19 | PersonalityData | Personalities | Personalities.txt | CharacterQuestions | 主 |
| 20 | PersonalityAffinityRuleData | PersonalityAffinityRules | PersonalityAffinityRules.txt | CharacterQuestions | 關聯表 |
| 21 | StorageExpansionStageData | StorageExpansionStages | StorageExpansionStages.txt | Storage | 主 |
| 22 | StorageExpansionRequirementData | StorageExpansionRequirements | StorageExpansionRequirements.txt | Storage | 子（FK stage_level） |

**22 個 DTO × 22 個 Sheets 分頁 × 22 個 .txt 檔**（超出原題目 15 個，因 Q3/Q4/Q7 拍板拆子表共增 7 個）。

**Sheets 分頁實際增減**：
- 舊 8 個新分頁 + Sprint 8 新增 14 個 = 共 22 個分頁（與 DTO 數一致）
- 舊 13 個空 `*Config` 分頁刪除（Q6）

---

## 4. Sheets 新分頁 Header 設計

本章節列出 22 個分頁的 header 欄位順序與型別規則。**第一欄一律為 `id`（int）**，子表第二欄為 FK 欄。

> **KGC 工具型別推斷**：`int` → `long` → `float` → `string` failover。`id` 欄必須全為數字，工具會自動推斷為 int。

---

### 4.1 Affinity（主）

- **Header 欄位順序**：`id`, `character_id`, `thresholds`
- **型別**：`id=int`, `character_id=string`, `thresholds=string`（陣列存為 `5,8,12,18,25` 逗號分隔；C# 端 split 轉 int[]）
- **說明**：第一欄 id 流水號；含 1 筆 `character_id="__default__"` 作 fallback；其餘為各角色配置
- **筆數預估**：5（4 角色 + 1 default）

**特別註**：Sheets 無法直接存陣列，`thresholds` 以逗號分隔字串儲存。若未來門檻複雜度上升，可拆子表 `AffinityThresholds`。

---

### 4.2 CGScene（主）

- **Header 欄位順序**：`id`, `cg_scene_id`, `character_id`, `required_threshold`, `dialogue_id`, `display_name`
- **型別**：`id=int`, `cg_scene_id=string`, `character_id=string`, `required_threshold=int`, `dialogue_id=int`, `display_name=string`
- **筆數預估**：4（四角色 × 1 場景；後續擴充）

---

### 4.3 CharacterIntros（主）

- **Header 欄位順序**：`id`, `intro_id`, `character_id`, `cg_sprite_id`, `scene_description`, `word_count_target`
- **筆數預估**：4

---

### 4.4 CharacterIntroLines（子，FK `intro_id` → CharacterIntros.intro_id）

- **Header 欄位順序**：`id`, `line_id`, `intro_id`, `sequence`, `speaker`, `text`, `line_type`
- **筆數預估**：41（現況）

---

### 4.5 CharacterProfiles（主，Q7 新增）

- **Header 欄位順序**：`id`, `character_id`, `preferred_personality_id`
- **筆數預估**：4

---

### 4.6 CharacterQuestions（主）

- **Header 欄位順序**：`id`, `question_id`, `character_id`, `level`, `prompt`
- **筆數預估**：280（4 角色 × 7 級 × 10 題）

---

### 4.7 CharacterQuestionOptions（子，FK `question_id`）

- **Header 欄位順序**：`id`, `question_id`, `personality_id`, `text`, `response`
- **筆數預估**：1120（280 題 × 4 選項）

---

### 4.8 Combat（singleton）

- **Header 欄位順序**：`id`, `player_max_hp`, `player_atk`, `player_def`, `player_spd`, `sword_angle_degrees_half`, `sword_range`, `sword_base_cooldown_seconds`, `sword_spd_cooldown_factor`, `move_speed_base`, `spd_move_speed_factor`, `free_movement_base_speed`, `spd_free_movement_speed_factor`, `knockback_distance`, `knockback_duration`
- **型別**：`id=int`, 整數類為 int, 浮點類為 float
- **筆數**：1（固定）

---

### 4.9 CommissionRecipes（主）

- **Header 欄位順序**：`id`, `recipe_id`, `character_id`, `input_item_id`, `input_quantity`, `output_item_id`, `output_quantity`, `duration_seconds`, `workbench_slot_index_max`, `description`
- **筆數預估**：9（現況）

---

### 4.10 GiftSwords（主）

- **Header 欄位順序**：`id`, `sword_id`, `display_name`, `atk_bonus`, `cooldown_modifier_seconds`, `range_modifier`, `angle_modifier_degrees`, `special_effect`, `description`
- **筆數預估**：1（目前僅木劍）

---

### 4.11 Greeting（主）

- **Header 欄位順序**：`id`, `greeting_id`, `character_id`, `level`, `text`
- **筆數預估**：280（4 角色 × 7 級 × 10 句）

---

### 4.12 IdleChat（主）

- **Header 欄位順序**：`id`, `topic_id`, `character_id`, `prompt`
- **筆數預估**：80（4 角色 × 20 題）

---

### 4.13 IdleChatAnswers（子，FK `topic_id`）

- **Header 欄位順序**：`id`, `answer_id`, `topic_id`, `text`
- **筆數預估**：240（80 題 × 3 回答）

---

### 4.14 InitialResourceGrants（主）

- **Header 欄位順序**：`id`, `grant_id`, `trigger_id`, `item_id`, `quantity`, `description`
- **筆數預估**：2（現況）

---

### 4.15 MainQuests（主）

- **Header 欄位順序**：`id`, `quest_id`, `display_name`, `description`, `owner_character_id`, `completion_condition_type`, `completion_condition_value`, `reward_grant_ids`, `sort_order`
- **筆數預估**：3

---

### 4.16 MainQuestUnlocks（子，FK `main_quest_id`，Q3 新增）

- **Header 欄位順序**：`id`, `main_quest_id`, `unlock_type`, `unlock_value`, `sort_order`
- **型別**：`unlock_type` 為 enum string（`quest` / `event` / `feature`）
- **筆數預估**：6~8（現況 T0/T1/T2 合計 6 個解鎖規則）

---

### 4.17 Monster（主）

- **Header 欄位順序**：`id`, `type_id`, `max_hp`, `atk`, `def`, `spd`, `move_cooldown_seconds`, `vision_range`, `attack_range`, `attack_angle_degrees_half`, `attack_prepare_seconds`, `attack_cooldown_seconds`, `color_r`, `color_g`, `color_b`, `color_a`
- **筆數預估**：2（Slime + Bat）

---

### 4.18 NodeDialogueLines（子，FK `node_id` 軟綁定）

- **Header 欄位順序**：`id`, `line_id`, `node_id`, `sequence`, `speaker`, `text`, `line_type`, `choice_branch`
- **筆數預估**：35（現況含 guard_first_meet 的 4 筆 id 32~35）

---

### 4.19 Personalities（主，Q7 新增）

- **Header 欄位順序**：`id`, `personality_id`, `display_name`, `description`
- **筆數預估**：4

---

### 4.20 PersonalityAffinityRules（Q7 新增）

- **Header 欄位順序**：`id`, `character_id`, `personality_id`, `affinity_delta`
- **筆數預估**：16（4 角色 × 4 個性）

---

### 4.21 StorageExpansionStages（主）

- **Header 欄位順序**：`id`, `level`, `capacity_before`, `capacity_after`, `duration_seconds`, `description`
- **筆數預估**：6（level 0~5）

---

### 4.22 StorageExpansionRequirements（子，FK `stage_level`，Q4 新增）

- **Header 欄位順序**：`id`, `stage_level`, `item_id`, `quantity`
- **筆數預估**：15（平均 5 級 × 3 物資）

---

## 5. 舊資料遷移計畫

本章節描述舊 .txt 資料 → 新 Sheets 分頁的遷移對應，供 dev-agent 執行機械性搬運。

### 5.1 遷移原則

1. **機械性搬運**：絕不在此步驟修改資料內容或語意；placeholder 文字保留，日後再由製作人補正式內容
2. **id 從 1 開始連續編號**：由 dev-agent 代寫時自動遞增
3. **子表 id 獨立計數**：主表 id 與子表 id 不共享序列
4. **FK 命名一致性**：遷移後的 FK 欄位值必須精確對應主表原始語意鍵
5. **有 FK 依賴的子表後遷移**：主表先、子表後，避免 runtime 測試抓不到父表

### 5.2 遷移順序

> **工作室代寫 Sheets 的建議**：由 dev-agent 透過 `mcp__google-sheets__update_cells` 代寫（本 spec § 7 明示 ADR-001 `.claude/rules/data-files.md` 允許的途徑）。每批寫入前 dev-agent 先 list 將寫入的列範圍讓 dev-head 確認。

**Phase 1：singleton / 獨立主表**（無 FK 依賴，可並行）

1. `affinity.txt` → `Affinity`
2. `combat.txt` → `Combat`
3. `monster.txt` → `Monster`
4. `gift-sword-config.txt` → `GiftSwords`（已有資料，僅需 id 欄位補齊）
5. `initial-resources-config.txt` → `InitialResourceGrants`（已部分有資料）
6. `main-quest-config.txt` → `MainQuests`（但 `unlock_on_complete` 欄位移除）
7. `cg-scene-config.txt` → `CGScene`

**Phase 2：CharacterQuestions 系列重構**（有依賴：`Personalities` → `CharacterProfiles` → `CharacterQuestions` → `CharacterQuestionOptions` → `PersonalityAffinityRules`）

8. `character-questions-config.txt § personality_types` → `Personalities`（4 筆）
9. `character-questions-config.txt § character_personality_preference` → `CharacterProfiles`（4 筆）
10. `character-questions-config.txt § character_questions` → `CharacterQuestions`（280 筆）
11. `character-questions-config.txt § character_questions[].options` → `CharacterQuestionOptions`（1120 筆）
12. `character-questions-config.txt § personality_affinity_map` → `PersonalityAffinityRules`（16 筆）

**Phase 3：Greeting / IdleChat**

13. `greeting-config.txt` → `Greeting`（280 筆）
14. `idle-chat-config.txt § topics` → `IdleChat`（80 筆）
15. `idle-chat-config.txt § topics[].answers` → `IdleChatAnswers`（240 筆）

**Phase 4：CharacterIntros 已在 Sheets，僅補 id**

16. `CharacterIntros` + `CharacterIntroLines`（現已在 Sheets，僅補 `id` 欄位）

**Phase 5：NodeDialogue / MainQuestUnlocks / Storage**

17. `node-dialogue-config.txt § node_dialogue_lines` → `NodeDialogueLines`（現已有 id 1~35，核對後直接搬）
18. `main-quest-config.txt § unlock_on_complete` 拆解 → `MainQuestUnlocks`（6~8 筆）
19. `storage-expansion-config.txt § stages` → `StorageExpansionStages`（6 筆含新增 level=0 entry）
20. `storage-expansion-config.txt § stages[].required_items` → `StorageExpansionRequirements`（15 筆）

**Phase 6：CommissionRecipes 已在 Sheets，僅補 id 與欄位對齊**

21. `CommissionRecipes`（現已在 Sheets，核對欄位與 id）

### 5.3 欄位結構轉換要點

| 舊結構 | 新結構 | 轉換規則 |
|--------|--------|---------|
| `AffinityConfig.defaultThresholds: [5]` | `Affinity` 新增 `character_id="__default__"` entry | 建 1 列，thresholds 填 `5` |
| `StorageExpansionConfig.initial_capacity / max_expansion_level` | 併入主表 level=0 entry / 移除 max | initial_capacity 成為 level=0 entry 的 capacity_after；max 從資料導出不再儲存 |
| `CharacterQuestionsConfig.personality_types[] 陣列` | 新分頁 Personalities | 4 筆，`id/personality_id/display_name/description` |
| `CharacterQuestionsConfig.character_personality_preference.<char>: <personality>` 物件 | 新分頁 CharacterProfiles | `<char, personality>` → 一列 |
| `CharacterQuestionsConfig.personality_affinity_map.<char>.<personality>: <delta>` | 新分頁 PersonalityAffinityRules | 每個 `char × personality` 組合為一列 |
| `CharacterQuestionsConfig.character_questions[].options[]` 巢狀陣列 | 子表 CharacterQuestionOptions | 每個選項獨立列 |
| `IdleChatConfig.topics[].answers[]` 巢狀陣列 | 子表 IdleChatAnswers | 每個 answer 獨立列 |
| `StorageExpansionConfig.stages[].required_items` 管道符字串 | 子表 StorageExpansionRequirements | split `\|` 後 split `:` 拆 item_id:quantity 為列 |
| `MainQuest.unlock_on_complete` 管道符字串 | 子表 MainQuestUnlocks | 依前綴分類（`T*` → quest; `*_complete` → event; 其他 → feature） |
| `Monster.color: {r,g,b,a}` 巢狀物件 | 扁平為 color_r/color_g/color_b/color_a | 直接拆 4 欄 |

### 5.4 舊 `*Config` 包裹層欄位處理

所有舊 JSON 的以下外層欄位**遷移時一律丟棄**（語意已透過 ADR / Sprint 記錄）：

- `schema_version`：schema 版本改由 C# DTO 的 namespace + ADR 綁定追溯
- `note`：撰寫背景移至 dev-log（Sprint 8 相關 dev-log 已包含）
- 包裹陣列欄位名（`affinity_characters`、`main_quests`、`grants` 等）：純陣列後無外層，包裹名消失

### 5.5 預估遷移資料規模

| 分組 | 預估列數 |
|------|---------|
| Affinity | 5 |
| CGScene | 4 |
| CharacterIntros | 4 |
| CharacterIntroLines | 41 |
| CharacterProfiles | 4 |
| CharacterQuestions | 280 |
| CharacterQuestionOptions | 1120 |
| Combat | 1 |
| CommissionRecipes | 9 |
| GiftSwords | 1 |
| Greeting | 280 |
| IdleChat | 80 |
| IdleChatAnswers | 240 |
| InitialResourceGrants | 2 |
| MainQuests | 3 |
| MainQuestUnlocks | 6~8 |
| Monster | 2 |
| NodeDialogueLines | 35 |
| Personalities | 4 |
| PersonalityAffinityRules | 16 |
| StorageExpansionStages | 6 |
| StorageExpansionRequirements | 15 |
| **合計** | **約 2158 列** |

---

## 6. C# 重構影響清單

### 6.1 廢棄的包裹類（13 個）

以下包裹類（外層 `*ConfigData` with 陣列欄位）將被拆解為「只保留 entry DTO」或刪除：

- `AffinityConfigData`（包裹層刪除；`AffinityCharacterConfigData` 改名為 `AffinityCharacterData`）
- `CGSceneConfigData`（包裹層刪除；`CGSceneConfigEntry` 改名為 `CGSceneData`）
- `CharacterIntroConfigData`（包裹層刪除；`CharacterIntroData` 保留；`CharacterIntroLineData` 主鍵調整）
- `CharacterQuestionsConfigData`（包裹層刪除；`CharacterQuestionEntryData` 改名為 `CharacterQuestionData`；options 拆子表 DTO；personality_types / preference / map 各拆為三新 DTO）
- `CombatConfigJson`（singleton，改為 CombatConfigData 並繼承 IGameData，ID=1）
- `CommissionRecipesConfigData`（包裹層刪除；`CommissionRecipeEntry` 改名為 `CommissionRecipeData`）
- `GreetingConfigData`（包裹層刪除；`GreetingEntryData` 改名為 `GreetingData`）
- `IdleChatConfigData`（包裹層刪除；`IdleChatTopicData` 保留；answers 拆子表 DTO）
- `MainQuestConfigData`（包裹層刪除；`MainQuestConfigEntry` 改名為 `MainQuestData`；unlock_on_complete 拆子表 DTO）
- `MonsterConfigData`（包裹層刪除；`MonsterTypeJson` 改名為 `MonsterData`）
- `NodeDialogueConfigData`（包裹層刪除；`NodeDialogueLineData` 保留但欄位調整）
- `StorageExpansionConfigData`（包裹層刪除；`StorageExpansionStageData` 保留；required_items 拆子表 DTO）
- `InitialResourcesConfigData`（包裹層刪除；`InitialResourceGrantData` 改名保留）

**GiftSwordData** 為新增（原為手動 JSON 無 IGameData）。

### 6.2 新建 / 重寫的 DTO（22 個 IGameData DTO）

見 § 3.23 彙整表。其中 **7 個新 DTO 為 Sprint 8 拍板新增**：

1. `CharacterQuestionOptionData`（Q7 巢狀展開）
2. `IdleChatAnswerData`（Q7 巢狀展開）
3. `MainQuestUnlockData`（Q3 規範化）
4. `StorageExpansionRequirementData`（Q4 規範化）
5. `PersonalityData`（Q7 拆出）
6. `CharacterProfileData`（Q7 拆出）
7. `PersonalityAffinityRuleData`（Q7 拆出）

### 6.3 反序列化路徑變更

**現況**（GameStaticDataDeserializer 或等價類別）：

```csharp
var wrapped = JsonUtility.FromJson<AffinityConfigData>(json);
foreach (var entry in wrapped.characters) { ... }
```

**重構後**（純陣列）：

```csharp
// JsonUtility 不支援根陣列，改走 JsonFx 或手動包裝：
var wrappedJson = "{\"Items\":" + json + "}";  // Unity JsonUtility workaround
var wrap = JsonUtility.FromJson<ArrayWrap<AffinityCharacterData>>(wrappedJson);
foreach (var entry in wrap.Items) { ... }
```

**或採用 JsonFx**（KGC 工具匯出使用 JsonFx，runtime 反序列化統一改用 JsonFx 可避免格式不一致）：

```csharp
var items = JsonReader.Deserialize<AffinityCharacterData[]>(json);
```

**推薦**：全面改走 JsonFx（與 KGC 工具一致），由 `GameStaticDataDeserializer` 內部統一處理。**本決策已在 Sprint 7 A 區塊部分落地**，Sprint 8 全面收斂。

### 6.4 受影響的 Installer / EntryPoint

- **VillageEntryPoint.cs / VillageEntryPointFunctionPrefabs.cs / VillageEntryPointInstallers.cs**：所有 `Resources.Load<TextAsset>("Config/xxx")` + 反序列化呼叫點，需依新 DTO 調整
- **各 Installer**（ADR-003 D5 的 6 個 Installer）：Install 時接收 `VillageContext.gameDataAccess` delegate，查詢路徑從 `dict[string]` 改為 `GameStaticDataManager.GetGameData<T>(int id)`
- **TD-2026-011**：`VillageContext.gameDataAccess` delegate 實際接線（本 Sprint D4 工作項）

### 6.5 預估受影響的測試檔案

依 Wave 1 A1 盤點：**約 20 個測試檔案、60~100 個 test 受影響**。分布：

| 類別 | 檔案估算 | 測試估算 |
|------|---------|---------|
| 反序列化測試（`*ConfigDataTests`） | 13~15 | 30~50 |
| Installer / EntryPoint 整合測試 | 3~5 | 15~25 |
| 模組級 manager 測試（querying 邏輯） | 3~5 | 15~25 |
| **合計** | **19~25** | **60~100** |

**新增測試**（Sprint 8 範圍）：

- 7 個新 DTO 的反序列化測試（每個含 `IGameData` 實作斷言 + `ID != 0` 斷言 + 欄位對齊 + FK 有效性）
- `MainQuestUnlockData` 的 FK 查詢測試（例：`GetUnlocksForQuest("T0")` 回傳正確條目）
- `StorageExpansionRequirementData` 的 FK 查詢測試
- `CharacterQuestionOptionData` 的 FK 查詢測試
- `PersonalityAffinityRuleData` 的雙 FK 查詢測試

**測試基線目標**：≥1427（Sprint 7 基線）+ 新增測試後 ≥1460

---

## 7. 與 ADR-001 / ADR-002 / ADR-004 的合規檢查

### 7.1 ADR-001 § 契約

| 條目 | 本 Spec 合規狀態 |
|------|----------------|
| 所有 DTO `implements IGameData` | ✅ 22 個 DTO 全部實作 |
| `public int ID` 非 0 | ✅ Q5 拍板 Sheets 填 id 流水號從 1 開始；反序列化測試斷言 `ID > 0` |
| 語意字串主鍵雙欄位（int ID + string Key） | ✅ 主表 + 子表均遵循（例：MainQuestData.id + quest_id） |
| runtime 載入走 GameStaticDataManager | ✅ 所有 22 個 DTO 透過 `GameStaticDataManager.Add<T>` 註冊（Sprint 8 D4 接線） |
| 查詢走 `GetGameData<T>(int id)` | ✅ Installer 查詢路徑變更已列入 § 6.4 |
| Sheets 欄位名 ↔ C# property 一致 | ✅ 本 spec 所有欄位採 snake_case；C# 反序列化層（JsonFx）自動處理 snake_case ↔ C# 命名 |
| ConfigData XML 註解標註來源 | ✅ 規格內每 DTO 記錄「對應 Sheets 分頁」與「對應 .txt 檔」；實作時補入 XML 註解 |

**無不合規項**。

### 7.2 ADR-002 退出 Gate

| 區塊 | 條目 | Sprint 8 完成後狀態 |
|------|------|-------------------|
| [A] | A01 ~ A17（16 個 ConfigData 改造） | ✅ Sprint 7 已完成，Sprint 8 依本 spec 進一步將包裹層刪除 |
| [B] | B01 ~ B?? Sheets 對齊 | ✅ Sprint 8 B 區塊依本 spec 執行；22 個分頁 header 齊全、資料對齊 |
| [C] | C01 匯出工具就緒 | ✅ Sprint 7 完成 |
| [C] | C02 匯出流程文件就緒 | ✅ Sprint 8 C 區塊更新 google-sheet-export-tool-spec.md v1.1（依本 spec） |
| [C] | C03 validate-assets.sh hook | ✅ Sprint 7 完成 |
| [C] | C04 `/development-flow` Phase 1.5 資料源接入驗證 | ⏳ Sprint 8 不處理（沿用 Sprint 7 進度） |
| [C] | C05 DEV-DATA-INTAKE-REVIEW gate | ⏳ 同上 |
| [C] | C06 PlayerPrefs 使用位點盤點 | ⏳ 同上 |
| [C] | C07 tech-debt.md 登記 | ⏳ 同上 |
| [D] | D01 tr-registry 綁定 | ✅ Sprint 8 D2 執行 |
| [D] | D02 ADR-001 Accepted | ✅ 已 Accepted |
| [D] | D03 adrs/index.md 列出 | ✅ 已列 |
| [D] | D04 FILE_MAP 同步 | ✅ Sprint 8 C3 執行 |
| [D] | D05 `/create-control-manifest` | ✅ Sprint 8 D3 執行 |
| [D] | D06 本 ADR 狀態 Full Exit | ✅ Sprint 8 D1 升 v1.7 |

**評估**：依 Sprint 8 工作項目全部完成，ADR-002 退出 Gate PASS 條件滿足（[A]+[B]+[C01/C02/C03/C04*]+[D] 全 ✅；C04~C07 為延後項，屬 CONCERNS 而非 FAIL）。

*若要達完整 PASS，C04~C07 需於 Sprint 8 之後另排 Sprint 補齊。本 spec 範圍不涵蓋這些項目。

### 7.3 ADR-004 五層結構

| 模組 | 本 spec 新增 DTO 所在 `<Module>/Data/` | 合規 |
|------|----------------------------------|------|
| Affinity | AffinityCharacterData | ✅ |
| CG | CGSceneData | ✅ |
| CharacterIntro | CharacterIntroData, CharacterIntroLineData | ✅ |
| CharacterQuestions | CharacterQuestionData, CharacterQuestionOptionData, CharacterProfileData, PersonalityData, PersonalityAffinityRuleData | ✅（5 個 DTO 同歸 CharacterQuestions/Data/） |
| Commission | CommissionRecipeData | ✅ |
| Dialogue | NodeDialogueLineData | ✅ |
| Exploration.Combat | CombatConfigData, MonsterData | ✅ |
| Greeting | GreetingData | ✅ |
| GuardReturn（或 Gift） | GiftSwordData | ✅（實作時依引用判定最終模組） |
| IdleChat | IdleChatTopicData, IdleChatAnswerData | ✅ |
| MainQuest | MainQuestData, MainQuestUnlockData | ✅ |
| Progression | InitialResourceGrantData | ✅ |
| Storage | StorageExpansionStageData, StorageExpansionRequirementData | ✅ |

Namespace 規則：所有 DTO 使用 `ProjectDR.Village.<Module>`（Exploration 例外 `ProjectDR.Village.Exploration.<SubModule>`）。合規。

---

## 8. 未來維護守則（為製作人後續填表而寫）

本章節為 Sprint 8 完成後的 VS / DEMO 階段日常維運指南。

### 8.1 新增一筆資料（最常見情境）

**情境**：要在 Greeting 分頁新增一句「村長夫人 Lv2 的第 11 句招呼語」。

1. 打開 Google Sheets → 切到 `Greeting` 分頁
2. 在最後一列後插入新列
3. 填入：
   - `id`：上一列的 id + 1（例如舊最後 id=280，新填 281）
   - `greeting_id`：遵循既有命名 `g_vcw_lv2_11`
   - `character_id`：`village_chief_wife`
   - `level`：`2`
   - `text`：新撰寫的招呼語
4. 存檔（Google Sheets 自動）
5. Unity Editor 中打開 `Assets/Game/Resources/Config/_Google Sheet 2 Json Setting.asset` → 點 `Start Convert`
6. 跑 `*ConfigDataTests`（反序列化測試全綠）
7. 交由製作人 commit

**注意事項**：

- **id 不能跳號、不能重複**：從最大 id + 1 續編；若刪除中間列，留該 id 號空著不重用
- **id 不能複用已刪除的 id**：歷史追溯重要
- **保持 header 欄位順序不變**：若要新增欄位請走 8.2 流程

### 8.2 新增一個欄位

**情境**：要在 `CommissionRecipes` 加入 `difficulty` 欄位（int，1~5）。

1. 在 Google Sheets `CommissionRecipes` 分頁最右側新增一欄，header 命名 `difficulty`
2. 為現有每列填值（使用安全預設 `1`）
3. 改對應 C# DTO：`CommissionRecipeData.cs` 加欄位 `public int difficulty;`
4. 加反序列化測試：至少一筆 JSON 測試覆蓋 `difficulty != 0`
5. 點 `Start Convert` → 跑測試 → 回報製作人

**注意事項**：

- **改欄位名前先查 XML 註解 + 反序列化測試**：命名必須與 Sheets header 完全一致
- **若欄位是 enum string**：在 DTO XML 註解列出所有允許值，Sheets 欄位欄位說明也補註釋（NOEX_ 註解）

### 8.3 新增一個分頁

**情境**：要建立新系統 `Achievement`（成就系統）。

1. 走 `/architecture-decision` skill，由 dev-head 建新 ADR 評估是否應獨立分頁
2. ADR Accepted 後，建 Sheets 新分頁 `Achievement`，header 遵循本 spec 命名原則：
   - 第一欄 `id`（int 流水號）
   - 第二欄 `<system>_id`（語意字串主鍵，例：`achievement_id`）
   - 其餘欄位依需求
3. 建 C# DTO：`Village/Achievement/Data/AchievementData.cs`，`implements IGameData`
4. 更新 `_Google Sheet 2 Json Setting.asset` 的 `sheetNames[]` 加入 `Achievement`
5. 點 `Start Convert` → 驗 `.txt` 輸出正確
6. 加反序列化測試
7. 更新 `FILE_MAP.md`、`google-sheet-export-tool-spec.md § 2` 對應表
8. 若 Accepted ADR ≥ 3 條，執行 `/create-control-manifest` 重建

### 8.4 刪除一筆資料

1. Sheets 中刪除該列
2. 對應 id 不重用（歷史追溯保留）
3. 若被刪的 entry 有子表 FK 指向它，先刪子表對應列（否則 runtime 讀取會有 orphan）

### 8.5 刪除一個欄位

1. 先評估是否 C# 程式碼有用到 → 移除所有引用點
2. Sheets 中刪除欄位
3. DTO 刪除欄位
4. 反序列化測試同步更新
5. Convert → 跑測試

### 8.6 資料驗證紅線

製作人填表時若遇下列情形，**立即停止並回報工作室**，由 dev-head 評估：

- **id 欄出現非數字值**：KGC 工具型別推斷會 fallback 為 string，造成整欄型別退化
- **id 跳號或重複**：違反 IGameData 契約穩定性前提
- **子表 FK 指向主表不存在的 id**：runtime orphan data
- **Sheets header 改名但 C# DTO 未同步**：Convert 後反序列化測試會紅
- **新增分頁但未更新 sheetNames[]**：Convert 會忽略該分頁

### 8.7 Sheets 欄位的 `NOEX_` 前綴用法

若製作人要在 Sheets 加暫時性實驗欄位（不想匯出到 .txt），在 header 前加 `NOEX_`：

- 範例：header 為 `NOEX_wip_balance_note`，該欄會被 KGC 工具完全忽略
- 適用：設計備忘、計算中介欄、未定案實驗欄位
- 不適用：長期註解欄（這類應放 `description` 欄位即可）

詳見 `projects/ProjectDR/tech/google-sheet-export-tool-spec.md § 5.2`。

---

## 9. 相關連結

- **Sprint 文件**：[`projects/ProjectDR/sprint/sprint-8-data-igamedata-sheets-alignment.md`](../sprint/sprint-8-data-igamedata-sheets-alignment.md)
- **ADR-001**：[`projects/ProjectDR/adrs/ADR-001-data-governance-contract.md`](../adrs/ADR-001-data-governance-contract.md)
- **ADR-002**：[`projects/ProjectDR/adrs/ADR-002-it-stage-exemption-exit.md`](../adrs/ADR-002-it-stage-exemption-exit.md)
- **ADR-004**：[`projects/ProjectDR/adrs/ADR-004-script-organization-structure-contract.md`](../adrs/ADR-004-script-organization-structure-contract.md)
- **匯出工具 spec**：[`projects/ProjectDR/tech/google-sheet-export-tool-spec.md`](./google-sheet-export-tool-spec.md)
- **Wave 1 盤點報告**：[`projects/ProjectDR/dev-logs/2026-04-22-17.md`](../dev-logs/2026-04-22-17.md)
- **資料檔規則**：[`.claude/rules/data-files.md`](../../../.claude/rules/data-files.md)
- **TR Registry**：[`projects/ProjectDR/adrs/tr-registry.yaml`](../adrs/tr-registry.yaml)
- **Control Manifest**：[`projects/ProjectDR/tech/control-manifest.md`](./control-manifest.md)

---

## 10. 版本更新紀錄

| 版本 | 日期 | 變更摘要 |
|------|------|---------|
| v1.0 | 2026-04-22 | 初版建立（Sprint 8 Wave 2；dev-head 主筆；製作人授權自主拍板 Q2~Q7） |

---
