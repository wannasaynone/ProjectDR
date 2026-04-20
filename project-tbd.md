# ProjectDR — TBD 池（製作人待拍板清單）

> **建立日期**：2026-04-20
> **用途**：跨 Sprint 累積的製作人待拍板 placeholder 清單，與單一 Sprint 解耦
> **寫入時機**：Sprint 收尾時搬移本 Sprint 產生的 placeholder，或 Sprint 執行中臨時發現即時登記
> **讀取時機**：`/next` 進度報告主動列出待消化清單（只列標題），製作人挑選後展開細節
> **ID 規則**：`TBD-<類別>-<3 位數序號>`；ID 一經配發永久保留不重用；拍板後改狀態為 ✅，不刪除條目

---

## 類別速覽

| 類別 | 用途 | 待拍板 | 已拍板 | Deprecated | 合計 |
|------|------|-------|-------|-----------|------|
| `intro` | 角色登場 CG + 短劇情（A1） | 5 | 0 | 0 | 5 |
| `node` | 節點 0/1/2 劇情文字（A2） | 8 | 0 | 0 | 8 |
| `quest` | 守衛歸來事件（A3）與前期主線 T0~T4（A5） | 3 | 0 | 7 | 10 |
| `resource` | 初始資源配置（A4） | 2 | 0 | 2 | 4 |
| `recipe` | 委託配方表（A7） | 10 | 0 | 0 | 10 |
| `storage` | 倉庫擴建物資（A6） | 5 | 0 | 0 | 5 |
| `balance` | 平衡驗證類（需實機測試後決定） | 2 | 0 | 1 | 3 |
| `content` | 角色對白文本（需撰寫/拍板） | 1 | 0 | 0 | 1 |
| **合計** | | **36** | **0** | **10** | **46** |

> **2026-04-20 更新**：Sprint 6 設計轉向（移除委託強制教學）後，`TBD-quest-012/013` 與 `TBD-resource-001/002` 改為 Deprecated；新增 `balance` 類兩條（`TBD-balance-001/002`）。
> **2026-04-20 再更新**：Sprint 6 範圍擴張（守衛歸來流程重構）後，新增 `balance` 類第三條（`TBD-balance-003` 守衛發問教學關卡卡關風險驗證）。
> **2026-04-20 三更新**：決策 6-12（F7 bug 修復方案 A）後，`TBD-quest-001~005` 改為 Deprecated（`guard-return-config.json` 路徑廢棄，劇情由 `intro_guard` CG 承載）；`TBD-intro-005` 字數規格上調，涵蓋完整守衛歸來全流程。`quest` 類 Deprecated 2 → 7，待拍板 8 → 3；合計待拍板 41 → 36，Deprecated 4 → 9。
> **2026-04-20 四更新**：決策 6-13（守衛取劍改為首次進入自動對白觸發）後，`TBD-balance-003` 改為 Deprecated（無卡關風險，驗證 TBD 失效）；新增 `content` 類第一條（`TBD-content-001` 守衛首次進入取劍對白文本）。`balance` 類 待拍板 3 → 2、Deprecated 0 → 1；新增 `content` 類（待拍板 1）；合計待拍板維持 36、Deprecated 9 → 10，合計 45 → 46。

---

## intro — 角色登場 CG + 短劇情

每位角色首次進入互動畫面時播放的「登場 CG + 短劇情」（500~1500 字，G5 規格）。

### TBD-intro-001 — 村長夫人登場 CG 場景設定

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A1-1
- **Placeholder 內容**：床邊晨光室內場景，玩家醒來第一面
- **影響檔**：`Assets/Game/Resources/Config/character-intro-config.json > intro_village_chief_wife`
- **建立日**：2026-04-17

### TBD-intro-002 — 村長夫人短劇情文字（500~1500 字）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A1-2
- **Placeholder 內容**：24 行對話/旁白，約 850 字；村長夫人溫柔敬語型、世界觀介紹、引路人定位
- **影響檔**：`Assets/Game/Resources/Config/character-intro-config.json > intro_vcw_*`
- **建立日**：2026-04-17

### TBD-intro-003 — 農女登場 CG 場景設定與短劇情

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A1-3
- **Placeholder 內容**：田邊午前、農女抬頭場景，約 600 字；爽快常體型
- **影響檔**：`Assets/Game/Resources/Config/character-intro-config.json > intro_fg_*`
- **建立日**：2026-04-17

### TBD-intro-004 — 魔女登場 CG 場景設定與短劇情

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A1-4
- **Placeholder 內容**：山邊小屋下午、煉金被打斷，約 550 字；低動能懶洋洋口吻
- **影響檔**：`Assets/Game/Resources/Config/character-intro-config.json > intro_w_*`
- **建立日**：2026-04-17

### TBD-intro-005 — 守衛登場 CG 場景設定與短劇情（銜接身分誤會解除後）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A1-5
- **Placeholder 內容**：森林邊界薄霧、誤會→澄清→贈劍，約 700 字；武人端正型
- **影響檔**：`Assets/Game/Resources/Config/character-intro-config.json > intro_guard`（17 條 CG 對話）
- **備註**：A1-5 與 A3 劇情有重疊——守衛歸來事件已拆分至 `guard-return-config.json`，`intro_g` 為同場景但較精簡版
- **備註（2026-04-20）**：決策 6-12 後，`guard-return-config.json` 路徑廢棄（`TBD-quest-001~005` 全部 Deprecated），`intro_guard` 要承載**全部**守衛歸來劇情。字數應從原「約 700 字」增加至涵蓋完整守衛歸來全流程（身分誤會 + 澄清 + 收劍 + 贈劍動機 + 收尾），預估字數翻倍以上（對應原 17 條 CG 對話的擴寫空間）。
- **建立日**：2026-04-17

---

## node — 節點 0/1/2 劇情文字

村長夫人三個劇情節點的對話、VN 選項、選擇後回應（GDD `character-unlock-system.md` v1.2）。

### TBD-node-001 — 節點 0 世界觀短劇情文字（你是誰、村莊處境）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A2-1
- **Placeholder 內容**：4 行旁白，霧鎖村/封閉/只有女人/你的出現
- **影響檔**：`Assets/Game/Resources/Config/node-dialogue-config.json > node_0 narration_001~004`
- **建立日**：2026-04-17

### TBD-node-002 — 節點 0 村長夫人對話內容（導引到選項）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A2-2
- **Placeholder 內容**：5 行對話，村莊情況→邀請留下→引見村民
- **影響檔**：`Assets/Game/Resources/Config/node-dialogue-config.json > node_0 vcw_001~005`
- **建立日**：2026-04-17

### TBD-node-003 — 節點 0 選項 1 的 VN 選項文字

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A2-3
- **Placeholder 內容**：「田那邊的女孩。」/「山邊小屋的那位。」
- **影響檔**：`Assets/Game/Resources/Config/node-dialogue-config.json > node0_choice_a / node0_choice_b`
- **建立日**：2026-04-17

### TBD-node-004 — 節點 0 選擇後的村長夫人回應（兩案各一）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A2-4
- **Placeholder 內容**：選農女→提及蘿、選魔女→提及席薇雅
- **影響檔**：`Assets/Game/Resources/Config/node-dialogue-config.json > node0_response_*`
- **建立日**：2026-04-17

### TBD-node-005 — 節點 1 村長夫人劇情（對 T1 完成的回應）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A2-5
- **Placeholder 內容**：讚許主角跟角色相處好、說明引見意義
- **影響檔**：`Assets/Game/Resources/Config/node-dialogue-config.json > node_1 vcw_001~005`
- **建立日**：2026-04-17
- **備註（2026-04-20）**：Sprint 6 決策 5 後，節點 1 觸發條件改為「選擇 1 角色登場 CG 完成」（已不依賴 T2 委託完成）。劇情文字仍待撰寫，此條目存續。

### TBD-node-006 — 節點 1 VN 選項文字（剩下那位）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A2-6
- **Placeholder 內容**：「好，帶我去見她。」
- **影響檔**：`Assets/Game/Resources/Config/node-dialogue-config.json > node1_choice_meet`
- **建立日**：2026-04-17

### TBD-node-007 — 節點 1 選擇後的村長夫人回應

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A2-7
- **Placeholder 內容**：農女版/魔女版各一，對應剩下那位角色
- **影響檔**：`Assets/Game/Resources/Config/node-dialogue-config.json > node1_response_*`
- **建立日**：2026-04-17

### TBD-node-008 — 節點 2 村長夫人劇情（推進探索開放）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A2-8
- **Placeholder 內容**：稱讚玩家→守衛鋪墊→探索入口開放
- **影響檔**：`Assets/Game/Resources/Config/node-dialogue-config.json > node_2 vcw_001~009`
- **建立日**：2026-04-17
- **備註（2026-04-20）**：Sprint 6 決策 3/4，節點 2 觸發條件改為「魔女對話結束瞬間 L4 紅點亮」（已不依賴 T3 委託完成）。劇情方向保持「稱讚→守衛鋪墊→探索開放」，文字仍待撰寫，此條目存續。

---

## quest — 守衛歸來事件（A3）與前期主線 T0~T4（A5）

兩個類別共用 `quest`，以序號區分：`001~009` 為守衛歸來事件（A3），`010~` 為前期主線（A5）。

### TBD-quest-001 — 守衛警戒階段對話（誤認為入侵者）

- **狀態**：🔴 Deprecated（2026-04-20）
- **來源**：Sprint 4 / A3-1
- **Placeholder 內容**：7 行，守衛出現→審查→「你是誰，這裡不是你能進的地方」
- **影響檔**：`Assets/Game/Resources/Config/guard-return-config.json > phase=alert`
- **建立日**：2026-04-17
- **Deprecate 原因**：2026-04-20 決策 6-12：`guard-return-config.json` 路徑廢棄，實際劇情由 `intro_guard` CG 對話（`TBD-intro-005`）承載。此條 TBD 不再需要製作人撰寫。

### TBD-quest-002 — 村長夫人澄清對話（解釋玩家身分）

- **狀態**：🔴 Deprecated（2026-04-20）
- **來源**：Sprint 4 / A3-2
- **Placeholder 內容**：9 行，夫人登場→他是村裡的人→能從森林回來
- **影響檔**：`Assets/Game/Resources/Config/guard-return-config.json > phase=clarify`
- **建立日**：2026-04-17
- **Deprecate 原因**：2026-04-20 決策 6-12：`guard-return-config.json` 路徑廢棄，實際劇情由 `intro_guard` CG 對話（`TBD-intro-005`）承載。此條 TBD 不再需要製作人撰寫。

### TBD-quest-003 — 守衛收劍時的對話（態度轉變）

- **狀態**：🔴 Deprecated（2026-04-20）
- **來源**：Sprint 4 / A3-3
- **Placeholder 內容**：5 行，職責型認錯，沉著道歉無多餘情緒
- **影響檔**：`Assets/Game/Resources/Config/guard-return-config.json > phase=sheathe`
- **建立日**：2026-04-17
- **Deprecate 原因**：2026-04-20 決策 6-12：`guard-return-config.json` 路徑廢棄，實際劇情由 `intro_guard` CG 對話（`TBD-intro-005`）承載。此條 TBD 不再需要製作人撰寫。

### TBD-quest-004 — 守衛贈劍時的對話（給予武器的理由）

- **狀態**：🔴 Deprecated（2026-04-20）
- **來源**：Sprint 4 / A3-4
- **Placeholder 內容**：5 行，不是偏袒是職責、可來找我訓練
- **影響檔**：`Assets/Game/Resources/Config/guard-return-config.json > phase=gift_sword`
- **建立日**：2026-04-17
- **Deprecate 原因**：2026-04-20 決策 6-12：`guard-return-config.json` 路徑廢棄，實際劇情由 `intro_guard` CG 對話（`TBD-intro-005`）承載。此條 TBD 不再需要製作人撰寫。

### TBD-quest-005 — 事件結束返回村莊 Hub 時的收尾文字

- **狀態**：🔴 Deprecated（2026-04-20）
- **來源**：Sprint 4 / A3-5
- **Placeholder 內容**：3 行旁白+夫人一句，返村、守衛按鈕解鎖
- **影響檔**：`Assets/Game/Resources/Config/guard-return-config.json > phase=closing`
- **建立日**：2026-04-17
- **Deprecate 原因**：2026-04-20 決策 6-12：`guard-return-config.json` 路徑廢棄，實際劇情由 `intro_guard` CG 對話（`TBD-intro-005`）承載。此條 TBD 不再需要製作人撰寫。

### TBD-quest-010 — T0 任務：名稱、描述、完成條件、獎勵

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A5-1
- **Placeholder 內容**：「醒來的地方」，對話完成自動結束，無物資獎勵
- **影響檔**：`Assets/Game/Resources/Config/main-quest-config.json > T0`
- **建立日**：2026-04-17

### TBD-quest-011 — T1 任務：名稱、描述、完成條件、獎勵（建議與選擇 1 的角色委託相關）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A5-2
- **Placeholder 內容**：「先去認識她們」，首次角色登場 CG 完成觸發，獎勵=選擇對應的種子/藥草
- **影響檔**：`Assets/Game/Resources/Config/main-quest-config.json > T1`
- **建立日**：2026-04-17
- **備註（2026-04-20）**：Sprint 6 決策 5 將原 T1「先去認識她們」重寫為合併後的 T1「認識所有人」（原 T2、T3 合併入此條），獎勵設計不變為「解鎖對應角色 Hub 按鈕」，但 Sprint 6 決策 6 已移除初始種子/藥草發放。條目存續、內容於 Sprint 6 / A2 改寫。

### TBD-quest-012 — T2 任務：名稱、描述、完成條件、獎勵（建議與選擇 2 的角色委託相關）

- **狀態**：🔴 Deprecated（2026-04-20）
- **來源**：Sprint 4 / A5-3
- **Placeholder 內容**：「幫她一次」，完成 1 次選擇 1 角色的委託，無物資獎勵（解鎖節點 1）
- **影響檔**：`Assets/Game/Resources/Config/main-quest-config.json > T2`
- **建立日**：2026-04-17
- **Deprecate 原因**：Sprint 6 決策 5，原 T2「幫她一次」因「移除委託強制教學」決策作廢，合併入新 T1「認識所有人」。本條目存續但不再需要拍板，相關 config 項由 Sprint 6 / B1 刪除。

### TBD-quest-013 — T3 任務：探索引導（建議為首次出發探索）

- **狀態**：🔴 Deprecated（2026-04-20）
- **來源**：Sprint 4 / A5-4
- **Placeholder 內容**：「再去認識另一個人」，完成 1 次選擇 2 角色的委託，解鎖節點 2 + 探索入口
- **影響檔**：`Assets/Game/Resources/Config/main-quest-config.json > T3`
- **建立日**：2026-04-17
- **Deprecate 原因**：Sprint 6 決策 5，原 T3「再去認識另一個人」因「移除委託強制教學」決策作廢，合併入新 T1「認識所有人」。節點 2 觸發條件改為「魔女對話結束瞬間」（決策 4），不再需要「完成委託」前置。相關 config 項由 Sprint 6 / B1 刪除。

### TBD-quest-014 — T4 任務：建議為首次擴建倉庫或類似引導

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A5-5
- **Placeholder 內容**：「出去看看外面」，首次探索+守衛歸來事件完成，獎勵=木劍
- **影響檔**：`Assets/Game/Resources/Config/main-quest-config.json > T4`
- **建立日**：2026-04-17
- **備註（2026-04-20）**：Sprint 6 決策 5，此任務內容保留但編號重編為新 T2（原 T2/T3 合併入新 T1，原 T4 上移）。條目 ID 保持 `TBD-quest-014` 不動（CLAUDE.md：ID 永久不變、只能改狀態；編號變動屬實作層）。相關 config 由 Sprint 6 / B1 重編；獎勵「木劍」仍為守衛贈劍，保留不受影響。

---

## resource — 初始資源配置

玩家首次解鎖角色時給予的初始資源，對應「解鎖=功能+初始資源綁定」設計（PB 決策）。

### TBD-resource-001 — 農女解鎖時給予的種子種類與數量

- **狀態**：🔴 Deprecated（2026-04-20）
- **來源**：Sprint 4 / A4-1
- **Placeholder 內容**：番茄種子 ×3
- **影響檔**：`Assets/Game/Resources/Config/initial-resources-config.json > unlock_farm_girl_seed`
- **建立日**：2026-04-17
- **Deprecate 原因**：Sprint 6 決策 6「完全不發初始物資」，移除農女解鎖時的種子發放。物資來源完全依賴探索。相關 config 項（`unlock_farm_girl_seed`）由 Sprint 6 / B2 刪除。

### TBD-resource-002 — 魔女解鎖時給予的素材種類與數量

- **狀態**：🔴 Deprecated（2026-04-20）
- **來源**：Sprint 4 / A4-2
- **Placeholder 內容**：綠藥草 ×3
- **影響檔**：`Assets/Game/Resources/Config/initial-resources-config.json > unlock_witch_herb`
- **建立日**：2026-04-17
- **Deprecate 原因**：Sprint 6 決策 6「完全不發初始物資」，移除魔女解鎖時的藥草發放。物資來源完全依賴探索。相關 config 項（`unlock_witch_herb`）由 Sprint 6 / B2 刪除。

### TBD-resource-003 — 守衛解鎖時贈送的劍屬性（攻擊力、是否有特殊效果）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A4-3
- **Placeholder 內容**：木劍 ATK+3，一擊擊殺史萊姆
- **影響檔**：`Assets/Game/Resources/Config/gift-sword-config.json > gift_sword_wooden`
- **建立日**：2026-04-17

### TBD-resource-004 — 村長夫人節點 0 時玩家初始背包內容（可為空或含基本物資）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A4-4
- **Placeholder 內容**：空背包
- **影響檔**：`Assets/Game/Resources/Config/initial-resources-config.json > initial_backpack_node0`
- **建立日**：2026-04-17

---

## recipe — 委託配方表

農女 / 魔女 / 守衛的單物品配方表（GDD `commission-system.md` 已定「單物品配方」結構）。

### TBD-recipe-001 — 農女作物清單（例：番茄種子→番茄、小麥種子→小麥）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A7-A1
- **Placeholder 內容**：番茄/胡蘿蔔/南瓜 三種作物
- **影響檔**：`Assets/Game/Resources/Config/commission-recipes-config.json > farm_*`
- **建立日**：2026-04-17

### TBD-recipe-002 — 農女每種作物的生長時間（短 10 秒 / 長 ≈ 一次探索時間）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A7-A2
- **Placeholder 內容**：番茄 30s / 胡蘿蔔 60s / 南瓜 120s（短/中/長三段階梯）
- **影響檔**：`Assets/Game/Resources/Config/commission-recipes-config.json > farm_*`
- **建立日**：2026-04-17

### TBD-recipe-003 — 農女工作台格子數（初始幾格，是否可擴充）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A7-A3
- **Placeholder 內容**：2 格（`workbench_slot_index_max: 2`）；是否可擴充未決，保留為未來擴展
- **影響檔**：`Assets/Game/Resources/Config/commission-recipes-config.json > farm_*`
- **建立日**：2026-04-17

### TBD-recipe-004 — 魔女素材 → 產出清單（藥水/寶石/金工品）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A7-B1
- **Placeholder 內容**：綠藥草→回血藥水、原礦水晶→寶石、鐵礦→金工品
- **影響檔**：`Assets/Game/Resources/Config/commission-recipes-config.json > witch_*`
- **建立日**：2026-04-17

### TBD-recipe-005 — 魔女每種配方的煉製時間

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A7-B2
- **Placeholder 內容**：藥水 45s / 寶石 90s / 金工品 120s（稀有度對應時長）
- **影響檔**：`Assets/Game/Resources/Config/commission-recipes-config.json > witch_*`
- **建立日**：2026-04-17

### TBD-recipe-006 — 魔女工作台格子數

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A7-B3
- **Placeholder 內容**：2 格（與農女一致，維持個人接觸感）
- **影響檔**：`Assets/Game/Resources/Config/commission-recipes-config.json > witch_*`
- **建立日**：2026-04-17

### TBD-recipe-007 — 守衛空手委託的基礎物資產出清單

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A7-C1
- **Placeholder 內容**：番茄種子、綠藥草、木材 三種；守衛是擴建物資主力來源
- **影響檔**：`Assets/Game/Resources/Config/commission-recipes-config.json > guard_patrol_*`
- **建立日**：2026-04-17

### TBD-recipe-008 — 守衛若有輸入物品的特殊委託配方

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A7-C2
- **Placeholder 內容**：無（Sprint 4 僅空手委託，工具委託留待後續）
- **影響檔**：`Assets/Game/Resources/Config/commission-recipes-config.json > guard_patrol_*`
- **建立日**：2026-04-17

### TBD-recipe-009 — 守衛每種委託的所需時間

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A7-C3
- **Placeholder 內容**：90~120 秒（對應一次短探索，強化「並行探索」節奏）
- **影響檔**：`Assets/Game/Resources/Config/commission-recipes-config.json > guard_patrol_*`
- **建立日**：2026-04-17

### TBD-recipe-010 — 守衛工作台格子數

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A7-C4
- **Placeholder 內容**：2 格（與農女/魔女一致）
- **影響檔**：`Assets/Game/Resources/Config/commission-recipes-config.json > guard_patrol_*`
- **建立日**：2026-04-17

---

## storage — 倉庫擴建物資

倉庫每次擴建 +50 格需要的物資（GDD `storage-expansion.md` 已定 100 初始、+50/次，物資 TBD）。

### TBD-storage-001 — 第 1 次擴建（100→150）需要的物資清單與數量

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A6-1
- **Placeholder 內容**：木材 ×10 + 布料 ×5
- **影響檔**：`Assets/Game/Resources/Config/storage-expansion-config.json > level 1`
- **建立日**：2026-04-17

### TBD-storage-002 — 第 2 次擴建（150→200）需要的物資清單與數量

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A6-2
- **Placeholder 內容**：木材 ×20 + 布料 ×10 + 石材 ×5
- **影響檔**：`Assets/Game/Resources/Config/storage-expansion-config.json > level 2`
- **建立日**：2026-04-17

### TBD-storage-003 — 第 3 次擴建（200→250）的物資遞增曲線

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A6-3
- **Placeholder 內容**：木 ×30 / 布 ×15 / 石 ×15（3 倍曲線，非指數）
- **影響檔**：`Assets/Game/Resources/Config/storage-expansion-config.json > level 3`
- **建立日**：2026-04-17

### TBD-storage-004 — 是否設擴建次數上限（或無限擴建但物資指數成長）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A6-4
- **Placeholder 內容**：上限 5 次（100→350）（`max_expansion_level: 5`）
- **影響檔**：`Assets/Game/Resources/Config/storage-expansion-config.json`
- **建立日**：2026-04-17

### TBD-storage-005 — 擴建等待時間（完成需時多久）

- **狀態**：🟠 待拍板
- **來源**：Sprint 4 / A6-5
- **Placeholder 內容**：90/120/180/240/300 秒（隨等級遞增，對應短探索→完整探索）
- **影響檔**：`Assets/Game/Resources/Config/storage-expansion-config.json`
- **建立日**：2026-04-17

---

## balance — 平衡驗證類（需實機測試後決定）

Sprint 6「移除委託強制教學 + 探索開放前流程」後引入的平衡風險驗證項。這類 TBD 不是單純的文字/配方拍板，而是需要實機測試產生數據後才能判斷如何調整。

### TBD-balance-001 — 好感度節奏調整（物資稀缺後送禮變少）

- **狀態**：🟠 待拍板
- **來源**：2026-04-20 設計討論（Sprint 6 決策 6 後延伸）
- **Placeholder 內容**：
  - Sprint 6 決策 6 後物資僅來自探索 → 送禮變稀缺 → 好感度累積可能變慢
  - 是否要調高對話（角色發問 / 玩家發問）給的好感度權重？還是維持現狀靠玩家自主管理？
  - 需實機測試一輪（約達到某角色好感度 L3）後評估
- **影響檔**：`Assets/Game/Resources/Config/affinity-config.json`、`character-questions-config.json` 好感度增量設計（目前 +0/+2/+5/+10）
- **建立日**：2026-04-20

### TBD-balance-002 — 探索物資產出覆蓋下游需求驗證

- **狀態**：🟠 待拍板
- **來源**：2026-04-20 設計討論（Sprint 6 決策 6 後延伸）
- **Placeholder 內容**：
  - 物資只靠探索 → 必須支撐四個下游：委託輸入物資、送禮、倉庫擴建物資、未來可能的生存消耗
  - 現有探索產出（`CollectiblePointData` 配置、`it-test-map.json`）是否足以穩定供應？
  - 倉庫擴建物資（木材 / 布料 / 石材）是否都能從探索取得？（若不能取得則擴建直接卡死）
  - 需實機測試一輪探索可取得的物資量 vs 下游需求
- **影響檔**：`gdd/exploration-system.md`、`commission-recipes-config.json`、`storage-expansion-config.json`、探索地圖 JSON（`it-test-map.json`）
- **建立日**：2026-04-20

### TBD-balance-003 — 守衛發問教學關卡卡關風險驗證

- **狀態**：🔴 Deprecated（2026-04-20）
- **來源**：2026-04-20 Sprint 6 擴張設計討論（決策 6-7 ~ 6-10 後延伸）
- **Placeholder 內容**：
  - 新流程中「拿劍」= 玩家必須主動發問「要拿劍」才取得，無紅點引導、無時限提醒、無守衛被動對話提示
  - 守衛歸來事件結束後：探索按鈕關閉（點擊顯示提示「要去找守衛對話拿劍...」）、守衛 Hub 按鈕解鎖
  - 玩家必須自己連結「守衛 → [對話] → 玩家發問清單 → 選『要拿劍』」三步
  - 部分玩家可能永遠不主動發問 → 永久卡關（無法進入探索）
  - 需實機測試驗證：新玩家在「沒紅點 + 探索關閉 + 提示訊息」下能否找到對話發問路徑
  - 若卡關比率過高，需補保護機制（延遲紅點 / 守衛被動提示 / 其他）
- **影響檔**：`guard-return-config.json`、`player-questions-config.json`（「要拿劍」題）、`character-unlock-system.md`、`main-quest-system.md`、玩家卡關率需實機數據
- **建立日**：2026-04-20
- **Deprecate 原因**：2026-04-20 決策 6-13：守衛取劍改為「首次進入 interact view 自動觸發對白」，無卡關風險，此驗證 TBD 失效。

---

---

## content — 角色對白文本（需撰寫/拍板）

守衛或其他角色的特殊對白文本，需製作人撰寫或拍板後交由 dev-agent 實作。

### TBD-content-001 — 守衛首次進入取劍對白文本

- **狀態**：🟠 待拍板
- **來源**：2026-04-20 決策 6-13（守衛取劍改為首次進入自動對白觸發）
- **Placeholder 內容**：
  - 守衛主動開口，贈劍敘事，約 5~10 行
  - 符合「武人端正型、職責先於一切」個性（見 `gdd/narrative/guard/character-spec.md`）
  - 敘事方向：守衛說明贈劍理由（職責/保護村莊/信任玩家的根據）+ 交劍動作描述 + 叮囑台詞
  - 對白結束後系統自動執行：劍入背包 + 探索入口重開
- **影響檔**：實作方式待 dev-agent 確認（新增 config JSON 或程式碼 placeholder）；撰寫規格見 `gdd/dialogue-writing-sop.md` 守衛首次進入取劍對白段落
- **建立日**：2026-04-20

---

## 更新紀錄

| 日期 | 說明 |
|------|------|
| 2026-04-20 | 初版建立；從 `sprint/sprint-4-producer-tbd-list.md` 搬移 42 條 TBD（intro 5 / node 8 / quest 10 / resource 4 / recipe 10 / storage 5） |
| 2026-04-20 | Sprint 6 設計轉向（移除委託強制教學 + 探索開放前流程）— 新增 `balance` 類 2 條（`TBD-balance-001/002`）；`TBD-quest-012/013` 改為 🔴 Deprecated（T2/T3 合併入新 T1）；`TBD-resource-001/002` 改為 🔴 Deprecated（移除初始物資）；`TBD-quest-011/014` 與 `TBD-node-005/008` 加備註說明觸發條件變更 |
| 2026-04-20 | Sprint 6 範圍擴張（守衛歸來流程重構）— 新增 `balance` 類第 3 條（`TBD-balance-003` 守衛發問教學關卡卡關風險驗證）；類別速覽合計 44 → 45，待拍板 40 → 41 |
| 2026-04-20 | 決策 6-12（F7 bug 方案 A）— `TBD-quest-001~005` 改為 🔴 Deprecated（`guard-return-config.json` 31 條對話路徑廢棄，劇情改由 `intro_guard` CG 承載）；`TBD-intro-005` 字數規格上調涵蓋完整守衛歸來全流程；類別速覽 `quest` Deprecated 2 → 7、待拍板 8 → 3；合計待拍板 41 → 36、Deprecated 4 → 9 |
| 2026-04-20 | 決策 6-13（守衛取劍改為首次進入自動對白觸發）— `TBD-balance-003` 改為 🔴 Deprecated（新流程無卡關風險）；新增 `content` 類 `TBD-content-001`（守衛首次進入取劍對白文本，待拍板）；類別速覽 `balance` 待拍板 3 → 2、Deprecated 0 → 1；新增 `content` 類（合計 1）；總 Deprecated 9 → 10，合計 45 → 46 |
