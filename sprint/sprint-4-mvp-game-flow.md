# Sprint 4 — MVP Game Flow（Dark Room 式按鈕 incremental）

> **目標**：讓製作人能玩到樹林醒來 → 搜索 → 生火 → 蓋小屋 → NPC 來訪 → 對話的完整核心循環，驗證節奏與感覺
> **建立日期**：2026-04-17
> **狀態**：🔄 進行中

---

## 背景

設計流程已走完九輪討論，結論在 `gdd/random-ideas/new-game-flow.md`。主題 X（開局玩法細節）與 V.1/V.2-core/V.4（世界觀基調）已定，其餘（V.2/V.3/V.5/V.6/Y.3/Y.4/Y.5.b~d/Y.6）由工作室填 PLACEHOLDER。

**製作人指示**：先玩到遊戲，設定可以設計師先隨機填寫一版。

本 Sprint 不先寫完整 MVP GDD，改以 `gdd/random-ideas/new-game-flow.md` 第六輪規格總結 + 第九輪 PLACEHOLDER 為實作依據，做最小可玩原型。所有不確定的設計細節在實作過程中若浮現，必須停下等製作人確認（AI 不可自行決定）。

---

## 設計依據

- **主要依據**：`gdd/random-ideas/new-game-flow.md`
  - 第六輪規格總結（X.1~X.10 已確定規格）
  - 第九輪 PLACEHOLDER（V.2/V.3/V.5/V.6/Y.3/Y.4/Y.5.b~d/Y.6）
- **世界觀基調（已定）**：V.1 末日倖存 + 現代、V.2-core 神秘災變（異域物質、動物異變）、V.4 寒冷因災變氣候。參考 The Mist、Annihilation、Stalker、Control
- **未定細節（用 PLACEHOLDER 先行）**：具體時代/地點/NPC 身份背景/UI 視覺語彙等

### X.1~X.10 關鍵規格（實作依據）

| 編號 | 規格 |
|---|---|
| X.1 | 搜索冷卻 1 秒，每次 +1 木材，隨機文字回饋；物品池隨村莊進度可擴充（MVP 僅 1 層） |
| X.2 | 生火解鎖條件：木材 ≥ 5；消耗 1 木材；火堆 60 秒倒數 |
| X.3 | 火堆延長：可手動 +60 秒，消耗 1 木材（X.6 確認） |
| X.4 | 寒冷狀態：火堆歸零觸發，所有行動冷卻 ×2；重新生火立即解除 |
| X.5 | 蓋小屋：首次點生火後解鎖；消 10 木材 + N 秒建造時間；完成後人口上限 +1 |
| X.6 | 延長火堆消耗 1 木材 |
| X.7 | 送禮系統移除，角色互動選單簡化為 [對話][派遣] |
| X.8 | 對話冷卻系統（詳見下方）：時間驅動冷卻，對話驅動好感度 |
| X.9 | HCG 多段解鎖（MVP 範圍：好感度滿紅點亮，不實播 HCG） |
| X.10 | 派遣中仍可對話（不中斷派遣）+ 持續產出資源 |

### X.8 好感度與對話冷卻規格（MVP 必須實作）

- 玩家主動對話：有冷卻 M 秒；派遣時冷卻 ×2（b 配置化）
- 角色主動發話：每角色獨立計時，固定間隔（g 可配置），到時亮紅點
- 對話觸發好感度 +N；取消純時間自動增長（c 確認對話是好感度唯一來源）
- 紅點可忽略玩家冷卻進入（d）
- 多角色可同時亮紅點，獨立計時（e）
- 紅點樣式統一，點進後才判斷普通對話 / HCG（f）
- 內容方向性：角色主動 = 角色提問玩家；玩家主動 = 玩家提問角色（文本用 placeholder）
- 兩種對話好感度增量相同（a）

---

## 既有可重用程式碼（不重做）

| 資產 | 用途 | MVP 使用方式 |
|---|---|---|
| AffinityManager | 好感度數值管理 | 直接重用，對話觸發 +N |
| DialogueManager | 對話播放 | 直接重用於角色互動對話 |
| TypewriterEffect | 文字打字效果 | 對話與回饋訊息 |
| BackpackManager | 資源持有 | 暫可承載木材資源（或由新 ResourceManager 接管，視 B1 決定） |
| ViewBase / ViewController | UI 架構 | 新主畫面 View 沿用 |
| CharacterInteractionView | 角色互動介面 | 需修改：移除送禮/回憶按鈕，保留 [對話][派遣] |

---

## 工作項目

### A. 基礎 UI 框架

- [x] **A1** 按鈕式主畫面 UGUI Prefab（資源顯示列 + 動作按鈕區 + 狀態列 + 角色清單區）— MvpMainView.prefab 完成
- [x] **A2** 主畫面 View 與 ViewController 整合（可與既有探索/村莊系統切換場景）— MvpMain.unity + EntryPoint 連線完成
- [x] **A3** 新場景 `MvpMain.unity` 作為遊戲入口（替換既有 VillageMain，既有場景保留備用）— MvpMain.unity 建立並設為 Build Settings 第一個場景

### B. 核心資源與時間系統

- [x] **B1** ResourceManager（木材資源數值管理 + 變更事件）
- [x] **B2** ActionTimeManager（所有行動的冷卻計時，支援寒冷 ×2 倍率）
- [x] **B3** 配置檔 `mvp-config.json`（搜索冷卻、生火耗材、火堆時長、小屋耗材、蓋屋時間、人口上限等全外部化）

### C. 開局機制（按鈕 incremental）

- [x] **C1** 搜索系統（按鈕、1 秒冷卻、+1 木材、隨機文字回饋）
- [x] **C2** 生火機制（解鎖條件：木材 ≥ 5；消 1 木材；火堆 60 秒倒數；可延長 +60 秒 消 1 木材）
- [x] **C3** 寒冷狀態（火堆歸零時發動，所有冷卻 ×2；重新生火立即解除）
- [x] **C4** 蓋小屋（解鎖條件：首次點生火後；消 10 木材 + N 秒建造時間；完成後人口上限 +1）

### D. NPC 來訪與互動

- [x] **D1** PopulationManager（人口上限變更觸發 NPC 來訪事件）
- [x] **D2** 第一位 NPC 來訪流程（簡單文字通知，不做 CG 登場劇情，用 placeholder 名字）— NpcArrivalManager 邏輯完成
- [x] **D3** 角色清單 UI（主畫面下方列出已加入 NPC，點擊進互動 View）— MvpCharacterListItem.prefab 完成
- [x] **D4** 角色互動 View（立繪 placeholder + 選單 [對話][派遣]，不做送禮/回憶按鈕）— MvpCharacterInteractionView.prefab 完成

### E. 好感度與對話冷卻（X.8 規格）

- [x] **E1** DialogueCooldownManager（玩家主動對話冷卻 M 秒）
- [x] **E2** NPCInitiativeManager（每角色獨立計時，固定間隔主動發話 → 紅點）
- [x] **E3** 紅點 UI（角色清單上顯示紅點，忽略玩家冷卻可進）— RedDotView 已整合至 MvpCharacterListItem.prefab
- [x] **E4** 對話觸發好感度 +N（既有 AffinityManager 重用；無純時間增長）
- [x] **E5** 對話內容方向性（角色主動 = 角色提問玩家 / 玩家主動 = 玩家提問角色，文本用 placeholder）

### G. 整合與測試

- [x] **G1** MVP EntryPoint MonoBehaviour 組裝所有模組
- [ ] **G2** 手動測試腳本（確認完整循環可跑通：醒來 → 搜索 → 生火 → 小屋 → NPC → 對話 → 派遣 → 資源持續產出）— 待 Scene/Prefab 建立後由製作人執行
- [x] **G3** 所有數值最終 config 化驗證（所有可調整數值 100% 外部化至 mvp-config.json；單元測試 MvpConfigJsonLoadTests 斷言 Sprint 4 規格值）

---

## 刻意排除（下一 Sprint 處理）

- HCG 多段解鎖、CG、立繪、背景圖（全用 placeholder）
- 第二階段自由移動探索（既有程式碼保留不動）
- 多位 NPC（MVP 只驗證 1 位，後續複製擴充）
- 登場劇情（用一行文字 placeholder）
- 送禮系統（X.7 已移除）
- 派遣系統（ActionPoint/Dispatch 等，下一 Sprint 處理）
- 農場系統（MVP 不用）
- HCG 劇情觸發（好感度滿紅點先亮但不播 HCG）
- 角色登場劇情的世界觀細節（全部 PLACEHOLDER）

---

## 執行注意事項

1. **MVP 階段所有設計決策若遇到不確定，必須停下等製作人確認**（AI 不可自行決定）
2. 所有遊戲數值禁止寫死，一律外部化至 `mvp-config.json`
3. 既有探索/村莊場景保留備用，MVP 以新 `MvpMain.unity` 為入口
4. 既有 CharacterInteractionView 需修改時，保留原版備份或另開新 View，避免破壞既有探索流程
5. 工作完成後依 CLAUDE.md「任何工作完成後必須更新狀態檔」規則更新 Sprint checkbox、`project-status.md` 與 dev-logs
