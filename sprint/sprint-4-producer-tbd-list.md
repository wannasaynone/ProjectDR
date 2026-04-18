# Sprint 4 — 製作人待算清單

> **建立日期**: 2026-04-17
> **狀態**: 🔄 進行中（placeholder 陸續填入後，等製作人決策取代）
> **目的**: 集中列出 Sprint 4 所有需製作人決策的數值與文字內容，避免散落各處遺漏

---

## 使用方式

1. Sprint 4 採取「先自行填寫 placeholder 版讓 LOOP 能跑起來」的策略
2. 本清單由設計部（numerical-designer、design-agent）填入 placeholder 值與 AI 提案
3. 製作人逐項審閱、修改、拍板後，覆蓋對應 JSON config 檔或 GDD 文件
4. 已定案項目在本文件打 ✅，全部完成後本文件封存或轉入 GDD

---

## 資料架構方針（Google Sheet 連動 Future-proof）

### 現況
既有 config 採 JSON 格式（`Assets/Game/Resources/Config/*.json`），主要為 `array of objects` 或 `object with fields` 模式。

### Sprint 4 新增資料檔一覽

| 檔案 | 對應 Google Sheet 分頁 | 結構 | 說明 |
|------|-----------------------|------|------|
| `commission-recipes-config.json` | CommissionRecipes | array of objects | 委託配方表（A7） |
| `storage-expansion-config.json` | StorageExpansion | array of objects | 倉庫擴建階段表（A6） |
| `initial-resources-config.json` | InitialResources | array of objects | 初始資源配置表（A4） |
| `main-quest-config.json` | MainQuests | array of objects | 主線任務序列 T0~T4（A5） |
| `character-intro-config.json` | CharacterIntros | array of objects | 角色登場 CG + 短劇情（A1） |
| `node-dialogue-config.json` | NodeDialogues | array of objects | 節點 0/1/2 劇情文字（A2） |
| `guard-return-config.json` | GuardReturnEvent | object | 守衛歸來事件劇情（A3） |

### 設計原則（所有新資料檔共同遵守）

1. **每行一筆資料**：top-level 為 `array`，每個元素對應 Google Sheet 一 row
2. **主鍵欄位**：每筆必有唯一 `id` 欄位（如 `recipeId`、`questId`）
3. **關聯用 ID 字串**：跨表引用用 ID 字串（如 `characterId = "Witch"`），不用索引
4. **扁平化欄位**：避免深層巢狀，複雜結構改用多張關聯表
5. **陣列用分隔字串**：若欄位含多值（如 prerequisites），用 `|` 分隔以便 CSV 編輯
6. **命名 snake_case**：JSON 欄位名一律 `snake_case`，對應 Sheet 欄位標題
7. **版本欄位**：每個檔案頂層加 `schema_version` 方便未來遷移

### 未來連動 Google Sheet 的重構路徑

- **階段 1（Sprint 4）**：placeholder 直接寫入 JSON 檔，製作人手改 JSON
- **階段 2（Sprint 4 後）**：建立 Google Sheets 作為 source of truth，寫匯出腳本 CSV → JSON
- **階段 3（選擇性）**：Editor Tool 在 Unity 內一鍵同步 Google Sheet 資料
- 所有新資料 DTO 結構對齊欄位命名，重構時僅需改變 「從哪裡讀」而非「資料結構」

---

## A4 — 初始資源配置表

### 決策對象

玩家首次解鎖角色時給予的初始資源，對應「解鎖=功能+初始資源綁定」設計（PB 決策）。

### 已定案（2026-04-17）
- 選農女 → 給種子
- 選魔女 → 給素材
- 首次遇守衛 → 給劍

### 製作人需決策

| 項目 | 需決策內容 | Placeholder 狀態 |
|------|-----------|-----------------|
| A4-1 | 農女解鎖時給予的種子種類與數量（例：番茄種子 × 5） | 📝 placeholder 已填：番茄種子 ×3（`initial-resources-config.json > unlock_farm_girl_seed`） |
| A4-2 | 魔女解鎖時給予的素材種類與數量 | 📝 placeholder 已填：綠藥草 ×3（`initial-resources-config.json > unlock_witch_herb`） |
| A4-3 | 守衛解鎖時贈送的劍屬性（攻擊力、是否有特殊效果） | 📝 placeholder 已填：木劍 ATK+3（`gift-sword-config.json > gift_sword_wooden`，一擊擊殺史萊姆） |
| A4-4 | 村長夫人節點 0 時玩家初始背包內容（可為空或含基本物資） | 📝 placeholder 已填：空背包（`initial-resources-config.json > initial_backpack_node0`） |

---

## A6 — 倉庫擴建物資配置

### 決策對象

倉庫每次擴建 +50 格需要的物資（GDD `storage-expansion.md` 已定 100 初始、+50/次，物資 TBD）。

### 已定案（2026-04-17）
- 初始容量 100
- 每次擴建 +50
- 擴建由村長夫人承接（村長夫人選單 [倉庫] 項）

### 製作人需決策

| 項目 | 需決策內容 | Placeholder 狀態 |
|------|-----------|-----------------|
| A6-1 | 第 1 次擴建（100→150）需要的物資清單與數量 | 📝 placeholder 已填：木材 ×10 + 布料 ×5（`storage-expansion-config.json > level 1`） |
| A6-2 | 第 2 次擴建（150→200）需要的物資清單與數量 | 📝 placeholder 已填：木材 ×20 + 布料 ×10 + 石材 ×5（`storage-expansion-config.json > level 2`） |
| A6-3 | 第 3 次擴建（200→250）的物資遞增曲線 | 📝 placeholder 已填：木 ×30 / 布 ×15 / 石 ×15（3 倍曲線，非指數）（`level 3`） |
| A6-4 | 是否設擴建次數上限（或無限擴建但物資指數成長） | 📝 placeholder 已填：上限 5 次（100→350）（`max_expansion_level: 5`） |
| A6-5 | 擴建等待時間（完成需時多久） | 📝 placeholder 已填：90/120/180/240/300 秒（隨等級遞增，對應短探索→完整探索） |

---

## A7 — 各角色委託配方表

### 決策對象

農女 / 魔女 / 守衛的單物品配方表（GDD `commission-system.md` 已定「單物品配方」結構）。

### 已定案（2026-04-17）
- 單物品配方：放入一格輸入 → 產出一種輸出
- 格子式工作台：可同時開多格並行
- 農女 = 耕種、魔女 = 煉製、守衛 = 探索周圍

### 製作人需決策

#### A7-A 農女（耕種）

| 項目 | 需決策內容 | Placeholder 狀態 |
|------|-----------|-----------------|
| A7-A1 | 作物清單（例：番茄種子→番茄、小麥種子→小麥） | 📝 placeholder 已填：番茄/胡蘿蔔/南瓜 三種作物（`commission-recipes-config.json > farm_*`） |
| A7-A2 | 每種作物的生長時間（短 10 秒 / 長 ≈ 一次探索時間） | 📝 placeholder 已填：番茄 30s / 胡蘿蔔 60s / 南瓜 120s（短/中/長三段階梯） |
| A7-A3 | 農女工作台格子數（初始幾格，是否可擴充） | 📝 placeholder 已填：2 格（`workbench_slot_index_max: 2`）；是否可擴充未決，保留為未來擴展 |

#### A7-B 魔女（煉製）

| 項目 | 需決策內容 | Placeholder 狀態 |
|------|-----------|-----------------|
| A7-B1 | 素材 → 產出清單（藥水/寶石/金工品） | 📝 placeholder 已填：綠藥草→回血藥水、原礦水晶→寶石、鐵礦→金工品（`witch_*`） |
| A7-B2 | 每種配方的煉製時間 | 📝 placeholder 已填：藥水 45s / 寶石 90s / 金工品 120s（稀有度對應時長） |
| A7-B3 | 魔女工作台格子數 | 📝 placeholder 已填：2 格（與農女一致，維持個人接觸感） |

#### A7-C 守衛（探索周圍）

| 項目 | 需決策內容 | Placeholder 狀態 |
|------|-----------|-----------------|
| A7-C1 | 空手委託的基礎物資產出清單 | 📝 placeholder 已填：番茄種子、綠藥草、木材 三種（`guard_patrol_*`）；守衛是擴建物資主力來源 |
| A7-C2 | 若有輸入物品的特殊委託配方 | 📝 placeholder 已填：無（Sprint 4 僅空手委託，工具委託留待後續） |
| A7-C3 | 每種委託的所需時間 | 📝 placeholder 已填：90~120 秒（對應一次短探索，強化「並行探索」節奏） |
| A7-C4 | 守衛工作台格子數 | 📝 placeholder 已填：2 格（與農女/魔女一致） |

---

## A1 — 4 位角色登場 CG 劇情文字

### 決策對象

每位角色首次進入互動畫面時播放的「登場 CG + 短劇情」（500~1500 字，G5 規格）。

### 已定案（2026-04-17）
- 前 3 位（村長夫人/農女/魔女）= 村莊內介紹氛圍
- 守衛 = 森林邊界戰鬥氛圍（身分誤會版本）
- 登場 CG = 1 張 + 短劇情 500~1500 字

### 製作人需決策

| 項目 | 需決策內容 | Placeholder 狀態 |
|------|-----------|-----------------|
| A1-1 | 村長夫人登場 CG 場景設定（室內/庭院/爐邊等） | 📝 placeholder 已填：床邊晨光室內場景，玩家醒來第一面（`character-intro-config.json > intro_village_chief_wife`） |
| A1-2 | 村長夫人短劇情文字（500~1500 字） | 📝 placeholder 已填：24 行對話/旁白，約 850 字；村長夫人溫柔敬語型、世界觀介紹、引路人定位（`character-intro-config.json > intro_vcw_*`） |
| A1-3 | 農女登場 CG 場景設定與短劇情 | 📝 placeholder 已填：田邊午前、農女抬頭場景，約 600 字；爽快常體型（`character-intro-config.json > intro_fg_*`） |
| A1-4 | 魔女登場 CG 場景設定與短劇情 | 📝 placeholder 已填：山邊小屋下午、煉金被打斷，約 550 字；低動能懶洋洋口吻（`character-intro-config.json > intro_w_*`） |
| A1-5 | 守衛登場 CG 場景設定與短劇情（銜接身分誤會解除後） | 📝 placeholder 已填：森林邊界薄霧、誤會→澄清→贈劍，約 700 字；武人端正型（`character-intro-config.json > intro_g_*`）；注意：A1-5 與 A3 劇情有重疊——守衛歸來事件已拆分至 guard-return-config.json，intro_g 為同場景但較精簡版 |

---

## A2 — 節點 0/1/2 劇情文字

### 決策對象

村長夫人三個劇情節點的對話、VN 選項、選擇後回應（GDD `character-unlock-system.md` v1.2）。

### 已定案（2026-04-17）
- 節點 0 = 開場強制流程（登場 CG → 世界觀短劇情 → 對話 → 選項）
- 節點 1 = 選擇 1 後（T1 任務完成觸發）
- 節點 2 = 選擇 2 後（T2 任務完成觸發），推進探索功能開放
- 選擇 1 = 農女 / 魔女
- 選擇 2 = 剩下那位

### 製作人需決策

| 項目 | 需決策內容 | Placeholder 狀態 |
|------|-----------|-----------------|
| A2-1 | 節點 0 世界觀短劇情文字（你是誰、村莊處境） | 📝 placeholder 已填：4 行旁白，霧鎖村/封閉/只有女人/你的出現（`node-dialogue-config.json > node_0 narration_001~004`） |
| A2-2 | 節點 0 村長夫人對話內容（導引到選項） | 📝 placeholder 已填：5 行對話，村莊情況→邀請留下→引見村民（`node-dialogue-config.json > node_0 vcw_001~005`） |
| A2-3 | 節點 0 選項 1 的 VN 選項文字（「我想先認識種菜那位」/「我想先認識煉藥那位」或類似） | 📝 placeholder 已填：「田那邊的女孩。」/「山邊小屋的那位。」（`node-dialogue-config.json > node0_choice_a / node0_choice_b`） |
| A2-4 | 節點 0 選擇後的村長夫人回應（兩案各一） | 📝 placeholder 已填：選農女→提及蘿、選魔女→提及席薇雅（`node-dialogue-config.json > node0_response_*`） |
| A2-5 | 節點 1 村長夫人劇情（對 T1 完成的回應） | 📝 placeholder 已填：讚許主角跟角色相處好、說明引見意義（`node-dialogue-config.json > node_1 vcw_001~005`） |
| A2-6 | 節點 1 VN 選項文字（剩下那位） | 📝 placeholder 已填：「好，帶我去見她。」（`node-dialogue-config.json > node1_choice_meet`） |
| A2-7 | 節點 1 選擇後的村長夫人回應 | 📝 placeholder 已填：農女版/魔女版各一，對應剩下那位角色（`node-dialogue-config.json > node1_response_*`） |
| A2-8 | 節點 2 村長夫人劇情（推進探索開放） | 📝 placeholder 已填：稱讚玩家→守衛鋪墊→探索入口開放（`node-dialogue-config.json > node_2 vcw_001~009`） |

---

## A3 — 守衛歸來事件劇情

### 決策對象

首次探索觸發的守衛歸來事件（身分誤會版本，純劇情演出）。

### 已定案（2026-04-17）
- 觸發時機：探索功能開放後玩家首次進入探索
- 劇情結構：守衛警戒 → 準備拔劍 → 村長夫人澄清 → 守衛收劍 → 登場 CG → 贈劍 → 加入
- 純劇情演出，玩家不參與戰鬥

### 製作人需決策

| 項目 | 需決策內容 | Placeholder 狀態 |
|------|-----------|-----------------|
| A3-1 | 守衛警戒階段對話（誤認為入侵者） | 📝 placeholder 已填：7 行，守衛出現→審查→「你是誰，這裡不是你能進的地方」（`guard-return-config.json > phase=alert`） |
| A3-2 | 村長夫人澄清對話（解釋玩家身分） | 📝 placeholder 已填：9 行，夫人登場→他是村裡的人→能從森林回來（`guard-return-config.json > phase=clarify`） |
| A3-3 | 守衛收劍時的對話（態度轉變） | 📝 placeholder 已填：5 行，職責型認錯，沉著道歉無多餘情緒（`guard-return-config.json > phase=sheathe`） |
| A3-4 | 守衛贈劍時的對話（給予武器的理由） | 📝 placeholder 已填：5 行，不是偏袒是職責、可來找我訓練（`guard-return-config.json > phase=gift_sword`） |
| A3-5 | 事件結束返回村莊 Hub 時的收尾文字 | 📝 placeholder 已填：3 行旁白+夫人一句，返村、守衛按鈕解鎖（`guard-return-config.json > phase=closing`） |

---

## A5 — 前期主線任務 T0~T4

### 決策對象

前期主線任務序列（GDD `main-quest-system.md` v1.1），每個任務對應機制引導定位。

### 已定案（2026-04-17）
- 主線任務 = 機制引導 + 節點 1/2 觸發器
- 任務按鈕在所有角色功能選單
- T0 = 開場（節點 0 內的引導）
- T1 = 完成後觸發節點 1
- T2 = 完成後觸發節點 2，推進探索開放
- T3/T4 = 探索相關引導

### 製作人需決策

| 項目 | 需決策內容 | Placeholder 狀態 |
|------|-----------|-----------------|
| A5-1 | T0 任務：名稱、描述、完成條件、獎勵 | 📝 placeholder 已填：「醒來的地方」，對話完成自動結束，無物資獎勵（`main-quest-config.json > T0`） |
| A5-2 | T1 任務：名稱、描述、完成條件、獎勵（建議與選擇 1 的角色委託相關） | 📝 placeholder 已填：「先去認識她們」，首次角色登場 CG 完成觸發，獎勵=選擇對應的種子/藥草（`main-quest-config.json > T1`） |
| A5-3 | T2 任務：名稱、描述、完成條件、獎勵（建議與選擇 2 的角色委託相關） | 📝 placeholder 已填：「幫她一次」，完成 1 次選擇 1 角色的委託，無物資獎勵（解鎖節點 1）（`main-quest-config.json > T2`） |
| A5-4 | T3 任務：探索引導（建議為首次出發探索） | 📝 placeholder 已填：「再去認識另一個人」，完成 1 次選擇 2 角色的委託，解鎖節點 2 + 探索入口（`main-quest-config.json > T3`） |
| A5-5 | T4 任務：建議為首次擴建倉庫或類似引導 | 📝 placeholder 已填：「出去看看外面」，首次探索+守衛歸來事件完成，獎勵=木劍（`main-quest-config.json > T4`） |

---

## 快速決策模板（製作人版）

填寫完成後貼回本文件對應段落或直接修改 JSON config 檔：

```
A4-1 農女解鎖種子：
□ 採用 placeholder
□ 改為：_______________

A4-2 魔女解鎖素材：
□ 採用 placeholder
□ 改為：_______________

A4-3 守衛贈劍屬性：
□ 採用 placeholder（攻擊力 +?）
□ 改為：_______________

（依此類推）
```

---

## 更新紀錄

| 日期 | 說明 |
|------|------|
| 2026-04-17 | 初版建立，列出 A1~A7 所有 TBD 項目 |
| 2026-04-17 | numerical-designer 完成 A4/A6/A7 placeholder 填寫：產出 `commission-recipes-config.json`（9 配方）/ `storage-expansion-config.json`（5 級擴建）/ `initial-resources-config.json`（4 grants）/ `gift-sword-config.json`（木劍 ATK+3）。所有項目從「⏳ 待提案」轉為「📝 placeholder 已填」，等製作人審閱 |
| 2026-04-17 | design-agent 完成 A1/A2/A3/A5 文字 placeholder 填寫：產出 `character-intro-config.json`（4 角色 / 47 行登場劇情）/ `node-dialogue-config.json`（三節點 / 26 行含 VN 選項雙分支）/ `guard-return-config.json`（守衛歸來純劇情 31 行 / 5 phases）/ `main-quest-config.json`（T0~T4 五任務）。所有 A1/A2/A3/A5 項目從「⏳ 待 design-agent 提案」轉為「📝 placeholder 已填」，等製作人審閱 |
