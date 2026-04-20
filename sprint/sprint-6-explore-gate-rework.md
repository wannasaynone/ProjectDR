# Sprint 6 — 探索開放流程重構（移除委託強制教學）+ 守衛歸來流程重構

> **目標**：依 2026-04-20 設計討論的 6 個決策，重構「四連擊登場 → 探索開放」流程，徹底移除委託強制教學；探索成為玩家唯一的物資來源。**2026-04-20 範圍擴張，納入守衛歸來流程重構**（決策 6-7 ~ 6-11）：守衛歸來事件不直接贈劍、探索暫時關閉、玩家主動發問「要拿劍」才取得劍並重開探索，強化「沒紅點也要主動發問」的教學定位。
> **建立日期**：2026-04-20
> **擴張日期**：2026-04-20（併入守衛歸來流程重構，不開新 Sprint）
> **狀態**：🔄 進行中
> **涉及階段**：GDD 完善

---

## 背景

2026-04-20 `/design-flow` 設計討論後，製作人拍板 6 個決策（見 `project-status.md` 製作人決策記錄 2026-04-20），將委託系統從「強制主線教學」轉為「自主發現」，並重組前期主線序列。原 T2「幫她一次」、T3「再去認識另一個人」合併為新 T1「認識所有人」，原 T4「出去看看外面」重編為新 T2。節點 2 觸發條件改為「魔女對話結束瞬間」。物資來源完全依賴探索（移除農女/魔女解鎖時的種子/藥草發放）。

此 Sprint 為 Phase D5 增訂的最新修訂，影響文件範圍：`character-unlock-system.md` / `main-quest-system.md` / `commission-system.md` / `core-definition.md`，並連帶 3 個 config JSON 與 4 個 Manager 程式檔案。

---

## 工作項目

> 格式慣例：項目以 `[分類字母][流水號]` 編號，方便在 dev-log 與進度報告中引用。
> 每項完成後將 `[ ]` 改為 `[x]`。
> A 類 = 設計部先行（`/design-flow` Phase 4 派 design-agent）；B/C/D 類 = 實作（`/development-flow` 派 dev-agent）；E 類 = 收尾。

### A. GDD 修改（設計部先行）

- [x] **A1** 更新 `gdd/character-unlock-system.md`：階段 2/4 循環累積條件移除 T2/T3；節點 2 觸發條件改為「魔女對話結束瞬間」；T 編號全文校正（T1/T2 新定義）；版本號遞增與版本更新紀錄 → **v1.2 → v1.3 完成（2026-04-20）**
  - [x] **A1-F1** DH 審核後校正：§1.2 階段 6 殘留舊 T3 編號改為新 T1 → **v1.3 → v1.4 完成（2026-04-20）**
- [x] **A2** 更新 `gdd/main-quest-system.md`：§1.4 前期主線序列重寫（T0→T1「認識所有人」合併→T2「出去看看外面」）；§1.1 任務範式表更新；版本號遞增 → **v1.1 → v2.0 完成（2026-04-20）**
  - [x] **A2-F2F3** DH 審核後校正：刪除 §1.1 舊 T 編號範例表；§2.2/§8.2 殘留舊 T 引用改為抽象表述 → **v2.0 → v2.1 完成（2026-04-20）**
- [x] **A3** 更新 `gdd/commission-system.md`：新增「自主發現定位」章節，明示非強制教學、無紅點強推、設計哲學為「沉浸式自主發現」；版本號遞增 → **v1.2 → v1.3 完成（2026-04-20）**
- [x] **A4** 審視 `gdd/core-definition.md` §1「四條次循環」的進入時機描述，確認與決策 3（節點 2 直推探索）一致；必要時更新版本 → **無需修改（已確認）**
- [x] **A5** 執行 `/doc-health projectdr --impact <修改的 GDD 路徑>` 追蹤其他 GDD 下游影響（含 base-management / village-economy / characters 等）→ **影響報告完成（2026-04-20）**

#### A7 範圍擴張（2026-04-20，守衛歸來流程重構）

- [ ] **A7-1** `gdd/character-unlock-system.md` v1.4 → v1.5：守衛解鎖段落拆分「歸來事件 = 解鎖 Hub 按鈕 + 探索暫時關閉」與「玩家發問『要拿劍』= 取得劍 + 探索重新開啟」兩步；版本號遞增與版本更新紀錄
- [ ] **A7-2** `gdd/main-quest-system.md` v2.2 → v2.3：新 T2「出去看看外面」完成條件重寫為「玩家發問『要拿劍』成功」（原「守衛歸來事件完成」拆分）；版本號遞增
- [ ] **A7-3** `gdd/exploration-system.md` 檢視並更新（若適用）：新增「探索按鈕關閉狀態」描述、點擊提示訊息「要去找守衛對話拿劍...」、探索再開條件（玩家發問「要拿劍」成功）
- [ ] **A7-4** GDD 新增「沒紅點也要主動發問」教學設計原則章節（可寫入 `character-unlock-system.md` 或獨立為設計哲學附錄），明示此關卡刻意不設保護機制，並連結 `TBD-balance-003` 實機驗證項
- [ ] **A7-5** 執行 `/doc-health projectdr --impact <A7 修改的 GDD 路徑>` 追蹤下游影響（含 characters / base-management / commission-system / narrative/guard 等）

### B. Config / 資料修改（實作）

- [x] **B1** `main-quest-config.json`：刪除原 T2/T3 條目、將原 T4 重編為新 T2、更新各任務的 `unlock_on_complete`、`completion_condition_type/value` 引用；T1 完成條件改為 `dialogue_end` / `node_2_dialogue_complete`；守衛歸來為 T2 完成條件 → **完成（2026-04-20）**
- [x] **B2** `initial-resources-config.json`：移除 `unlock_farm_girl_seed`、`unlock_witch_herb` 兩個 grant；保留 `unlock_guard_sword`（守衛歸來事件贈劍）；保留 `initial_backpack_node0`（節點 0 空背包） → **完成（2026-04-20）**
- [x] **B3** `node-dialogue-config.json`：確認 config 僅存對話內容（無 trigger_condition 欄位），節點 2 觸發邏輯由程式層決定（C1/C4 範疇）；無舊 T3/T4 殘留 → **無需改 config（2026-04-20）**

#### B 範圍擴張（2026-04-20，守衛歸來流程重構）

- [x] **B4** `guard-return-config.json` 移除贈劍段落（phase=gift_sword 保留劇情文字用於玩家發問「要拿劍」時的守衛回應，但事件內不觸發 grant）；事件結尾改為發布「探索關閉 + 守衛 Hub 解鎖」兩個狀態旗標 → **完成（2026-04-20）**
- [x] **B5** `player-questions-config.json` 新增守衛「要拿劍」單次特殊題（schema_version 2，`is_single_use: true` / `trigger_flag: "grant_guard_sword"` / `affinity_gain: 0`）；`PlayerQuestionsConfigData` C# DTO 新增對應欄位 → **完成（2026-04-20）**
- [x] **B6** `gift-sword-config.json` 更新 description 說明觸發時機改變；`initial-resources-config.json` 的 `unlock_guard_sword` grant 的 `trigger_id` 從 `guard_return_event` 改為 `guard_sword_asked`；`InitialResourcesTriggerIds` 新增 `GuardSwordAsked` 常數、舊 `GuardReturnEvent` 標記 `[Obsolete]` → **完成（2026-04-20）**

### C. 程式邏輯修改（實作）

- [x] **C1** `MainQuestManager` / `VillageEntryPoint`：移除 T2/T3 狀態處理與相關 `NotifyCompletionSignal` 訊號 hook；`MainQuestSignalValues` 新增 `Node2DialogueComplete` 常數 → **完成（2026-04-20）**
- [x] **C2** `CharacterUnlockManager`：移除農女/魔女解鎖時的 grant 派發（`UnlockByBranch` 中的 `DispatchGrantsByTrigger` 呼叫移除）；`OnMainQuestCompleted` 的探索解鎖觸發從 T3 改為 T1 → **完成（2026-04-20）**
- [x] **C3** `InitialResourceDispatcher`：確認為完全泛型 table-lookup，B2 已刪除 config entries，無需修改程式碼 → **無需改動（2026-04-20）**
- [x] **C4** `RedDotManager`：`QuestIdsTriggersNode2` 從 "T3" 改為 "T1"；`QuestIdsTriggersNode1` 標記 `[Obsolete]`；`OnMainQuestCompleted` 移除 Node1 條件分支（改由外部 `SetMainQuestEventFlag` 觸發）→ **完成（2026-04-20）**

#### C 範圍擴張（2026-04-20，守衛歸來流程重構）

- [x] **C9** `CharacterUnlockManager`：`OnGuardReturnCompleted` 移除 `DispatchGrantsByTrigger("guard_return_event")`，僅保留 `ForceUnlock(Guard)`；`VillageEvents.cs` 新增 `ExplorationGateLockedEvent`、`ExplorationGateReopenedEvent`、`PlayerSpecialQuestionTriggeredEvent`、`ExplorationGateLockedClickedEvent` → **完成（2026-04-20）**
- [x] **C10** `ExplorationEntryManager` 新增 `_isLocked` 狀態 + `SetExplorationLocked` + `IsExplorationLocked`；Locked 狀態下 `Depart()` 發布 `ExplorationGateLockedClickedEvent` 回傳 false；`VillageHubView` 訂閱三個新事件（GateLocked / GateReopened / GateLockedClicked），每次點擊 locked 按鈕均顯示完整 modal → **完成（2026-04-20）**
- [x] **C11** `PlayerQuestionsManager` 新增 `TriggerFlagGrantGuardSword` 常數 + `TriggerSingleUseQuestion` 方法（派發 grant + 發布 `ExplorationGateReopenedEvent` + `PlayerSpecialQuestionTriggeredEvent` + 永久消耗特殊題）；`PlayerQuestionsView` 新增 `SetSingleUseQuestionDependencies` + `OnQuestionSelected` 特殊題分支 → **完成（2026-04-20）**
- [x] **C12** `VillageEntryPoint` 整合：訂閱 `GuardReturnEventCompletedEvent` → `SetExplorationLocked(true)` + `Publish(ExplorationGateLockedEvent)`；訂閱 `ExplorationGateReopenedEvent` → `SetExplorationLocked(false)` + `NotifyCompletionSignal(T2)`；`SetSingleUseQuestionDependencies` 注入到 `PlayerQuestionsView` 初始化 → **完成（2026-04-20）**

### D. 測試

- [x] **D1** 更新 `MainQuestManagerTests`：`BuildFiveQuestConfig` 改為 `BuildThreeQuestConfig`（T0 Auto + T1 DialogueEnd/node_2 + T2 FirstExplore）；新增 `FullFlow_NewStructure_T0ToT2` 端到端測試；`Constructor_InitialState_ThreeQuests` 驗證三任務初始狀態 → **完成（2026-04-20）**
- [x] **D2** 更新 `OpeningFlowIntegrationTest` / `NodeProgressionIntegrationTest`：`BuildInitialResourcesConfig` 移除農女/魔女 grants；選擇農女/魔女不再派發物資（期望 dispatch 次數 = 0）；T1/T3 相關節點名稱對應 Sprint 6 新架構 → **完成（2026-04-20）**
- [x] **D3** 更新 `CharacterUnlockManagerTests` + `FullLoopIntegrationTest`：T3 → T1 探索解鎖測試；移除農女/魔女 grant dispatch 斷言；`RedDotManagerTests.BuildConfig` T1 completion_condition_value 更新為 `node_2_dialogue_complete` → **完成（2026-04-20）**
- [x] **C5（補修 R1+R2+R3）** `VillageEntryPoint`：修正新 T1 訊號源（移除舊 `FirstCharIntroComplete` 送訊邏輯，改在 node_2 完成時送 `Node2DialogueComplete`）；節點 1 CG 完成後補呼叫 `SetMainQuestEventFlag(VCW, true)` 點亮 L4 紅點（R2）；刪除節點 1 殘留解鎖邏輯與死碼 `DispatchInitialResourceGrants` 方法（R3）→ **完成（2026-04-20）**
- [x] **D3.1（補修 R4+R6）** `MainQuestConfigData`：L32/L44 兩個 `FirstCharIntroComplete` 常數加 `[System.Obsolete]` attribute（R4）；`InitialResourcesConfigTests.GetGrant_ExistingGrantId_ReturnsEntry` 測試改為測試 `unlock_guard_sword`（R6）→ **完成（2026-04-20）**
- [x] **D3.2（補修 R5+R7）** 更新 `VillageEntryPoint` 過時註解（R5）；`NodeProgressionIntegrationTest` 新增 `Regression_R7_1` 與 `Regression_R7_2` 兩個回歸測試，防止舊訊號誤觸發與新 production path 斷裂（R7）→ **完成（2026-04-20）**
- [x] **C6（D4 bugfix）** `VillageEntryPoint`：修正節點 1/2 對話觸發邏輯（Sprint 6 D4 實機測試發現 bug）。根因：`GetPendingMainQuestNodeId` 以 `IsQuestCompleted("T1")` 判斷節點 1，但 Sprint 6 後 T1 語義改為「認識所有人」（需節點 2 播完才完成），導致節點 1 永遠無法觸發；節點 2 以 `IsQuestCompleted("T3")` 判斷，T3 已刪除故永遠 false。修復：新增 `_node1TriggerReady`（由 CG 播完設定）與 `_node2TriggerReady`（由 T1 完成設定）旗標，與 MainQuest 語義解耦；`OnMainQuestCompletedForNodeDialogue` 補實現 T1 完成後設定 `_node2TriggerReady + L4 紅點`；`NodeProgressionIntegrationTest` 新增 D4-1/D4-2 回歸測試 → **完成（2026-04-20）**
- [x] **C7（D4 bugfix 2）** `CharacterUnlockManager`：修正節點 1 對話結束後「剩下那位」角色 Hub 按鈕未解鎖 bug。根因：C5 R3 刪除 `ForceUnlock` 路徑後，依賴 `OnDialogueChoiceSelected` 解鎖；但真實 `node-dialogue-config.json` 節點 1 選項 `choice_branch = ""`（空字串），永遠不觸發解鎖。修復：`CharacterUnlockManager` 新增訂閱 `NodeDialogueCompletedEvent`，node_1 完成時依 `_node0ChosenBranch` 推算剩下那位並 `ForceUnlock`；更新相關測試（`CharacterUnlockManagerTests`、`NodeProgressionIntegrationTest`）同步新路徑 → **完成（2026-04-20）**
- [x] **C8（D4 bugfix 3）** `VillageHubView` / `VillageEntryPoint`：修正探索按鈕點擊無反應 bug。根因：`VillageHubView` 的 `_explorationButton` 在 B8（2026-04-18）建立時只有 `SetActive` 顯隱邏輯，從未在 `OnShow()` 中綁定 `onClick` handler，導致按鈕可見但點擊完全無效。修復：`VillageHubView.Initialize()` 新增 `ExplorationEntryManager` 參數、`OnShow()` 綁定 `onClick → Depart()`、`OnHide()` 移除；`VillageEntryPoint.InitializeViewDependencies()` 傳入 `_explorationManager`；`NodeProgressionIntegrationTest` 新增 C8-1/C8-2 回歸測試 → **完成（2026-04-20）**
- [ ] **D4** 實機測試（製作人或 dev-head 執行）：走一次新流程，確認無 regression、物資來源僅限探索、委託系統仍可運作但無強制引導 — **2026-04-20 中止：Sprint 6 擴張納入守衛歸來流程重構，D4 待 A7/B4~B6/C9~C12/D5 全部完成後重跑完整流程**

#### D 範圍擴張（2026-04-20，守衛歸來流程重構）

- [x] **D5** 整合測試 `GuardReturnSwordFlowIntegrationTest.cs`（8 個測試案例）：A.守衛歸來後 ExplorationGateLockedEvent 發布 / B.鎖定後 Depart 回傳 false / C.守衛 Hub 已解鎖 / D.發問清單含要拿劍特殊題 / E.觸發後劍入背包 / F.觸發後 ExplorationGateReopenedEvent 發布 / G.特殊題永久消失 / H.T2 完成條件觸發；既有 GuardReturnIntegrationTest 更新（EventComplete_UnlocksGuard_SwordNotGrantedYet 斷言劍=0）→ **完成（2026-04-20）**
- [x] **F1-F4 補修（DEV-CODE-REVIEW 退回 2026-04-20）** 移除 `OnGuardReturnForMainQuest` 雙訊號源（F1）；新增負面測試 `GuardReturnComplete_DoesNotCompleteT2_UntilPlayerAsks`（F2）；Test H 改為 production path（F3）；新增連續點擊測試 `LockedButton_ClickedThreeTimes_PublishesThreeEvents`（F4）；清除 Obsolete 常數引用（F5）；`SetExplorationButtonLocked` 加 TODO 標記（F6）→ **完成（2026-04-20）**
- [x] **F7（D4 bugfix 4）守衛按鈕未出現**：根因：`GuardReturnEventController.OnCGComplete()` 呼叫 `DialogueManager.StartDialogue(guard_return_lines)` 後無任何 View 呼叫 `Advance()`，對話永遠不完成，`GuardReturnEventCompletedEvent` 不發布，守衛不被解鎖。修復：`GuardReturnEventController` 移除 `DialogueManager` 依賴，CG 完成後直接呼叫 `CompleteEvent()`（台詞已整合於 `character-intro-config.json` intro_lines 在 CG 期間展示）；更新 `GuardReturnEventControllerTests` / `GuardReturnIntegrationTest` / `GuardReturnSwordFlowIntegrationTest`；更新過時測試 `GuardReturnEventCompleted_DispatchesSwordGrant` → `DoesNotDispatchSwordGrant`；新增回歸測試 `GuardReturnCGComplete_WithoutManualAdvance_PublishesCharacterUnlockedEvent` → **完成（2026-04-20）**
- [x] **F8（D4 bugfix 5）CG 重播 + 角色發問倒數干擾**：根因 1（CG 重播）：`GuardReturnEventController` 透過 `_cgPlayer.PlayIntroCG` 播放守衛 CG，但 `VillageEntryPoint._introCgPlayedCharacters` 沒有標記守衛已播過，導致首次進入守衛互動畫面再播一次。根因 2（倒數提前）：`Start()` 對所有角色呼叫 `StartCountdown`，守衛在「要拿劍」完成前即啟動 60s L2 倒數。修復 1：`OnGuardReturnLockExploration` 補呼叫 `MarkIntroCGPlayed(Guard)`。修復 2：`CharacterQuestionCountdownManager` 新增 `BlockCountdown`/`UnblockCountdown`/`IsBlocked`；`Start()` 呼叫 `BlockCountdown(Guard)` 封鎖；`OnExplorationGateReopenedForT2` 解封並啟動守衛倒數。新增回歸測試 `GuardCGAndCountdownRegressionTest`（4 個測試）+ `CharacterQuestionCountdownManagerTests` 新增 6 個 Block 相關測試 → **完成（2026-04-20）**
- [x] **F9（D4 bugfix 6）守衛 Hub 按鈕 FirstMeet 紅點永不消失**：根因：F8 修復在 `OnGuardReturnLockExploration` 中補標記 `MarkIntroCGPlayed(Guard)`，但略過了 `InitializeCharacterView` CG callback 的另一個副作用 `SetFirstMeetFlag(Guard, false)`，導致守衛歸來 CG 播完後，Hub 按鈕的 FirstMeet 紅點永遠不消失（玩家只能靠紅點引導時會看到一直亮著的紅點）。修復：`OnGuardReturnLockExploration` 補呼叫 `_redDotManager.SetFirstMeetFlag(Guard, false)`。新增回歸測試 `GuardInteractViewDialogueRegressionTest`（4 個測試）→ **完成（2026-04-20）**
- [x] **A6（D4 衍生）** `main-quest-system.md` § 3 全面改寫對齊 Sprint 6 實作（v2.1 → v2.2）：§ 3.1 四角色選單改為 [主功能][送禮][CG 圖鑑][對話]；§ 3.2/3.3 改寫為 T0~T2 自動觸發模式；連帶更新 `base-management.md` / `characters.md` / `character-unlock-system.md` / `commission-system.md` / `storage-expansion.md` / `narrative/village-chief-wife/character-spec.md` 中的功能選單交叉引用 → **完成（2026-04-20）**
- [x] **F10（D4 bugfix 7）系統性重構繞道路徑 + Revert F9**：根因分析：F7/F8/F9/F10 是同一陷阱（守衛歸來繞過 InitializeCharacterView 標準路徑）的連續發現。F9 方向錯誤（在守衛歸來瞬間清除 FirstMeet，導致 Hub 無紅點引導玩家）。修復：Revert F9 的 `SetFirstMeetFlag(Guard, false)` 從 `OnGuardReturnLockExploration` 移除；抽出 `OnCharacterEnteredAndCGDone` 共用方法，兩條路徑（路徑 A：標準 CG 路徑 / 路徑 B：守衛特例跳過 CG）皆呼叫此方法執行 (b)/(c) 類 side effects；FirstMeet 清除改在玩家首次點擊進入角色時執行；新增 4 個 F10 回歸測試 → **完成（2026-04-20）**
- [x] **F11（D4 bugfix 8）路徑 B 補 SetState(Normal)**：根因：F10 盤點 side effects 時，`SetState(Normal)` 被誤分類為 (a) CG 播放本身，只放在路徑 A callback 中執行。路徑 B（守衛特例）直接呼叫 `OnCharacterEnteredAndCGDone` 時未執行 `SetState(Normal)`，CharacterInteractionView 可能卡在非 Normal 狀態，GreetingPresenter 的 greeting 觸發鏈無法執行（StartDialoguePlayback 只在 Normal state 時呼叫 TryGreet）。修復：`SetState(Normal)` 從路徑 A callback 移入 `OnCharacterEnteredAndCGDone` 共用方法（F11 bugfix 正確分類：顯示模式切換屬共用流程，非 CG 播放本身）；新增 3 個 F11 回歸測試 → **完成（2026-04-20）**
- [x] **F12（D4 bugfix 9）守衛取劍改為首次進入自動對白觸發（決策 6-13）**：製作人 2026-04-20 拍板。Revert：`player-questions-config.json` 移除 `guard_ask_sword` 特殊題；`TriggerFlagGrantGuardSword` 常數標記 `[Obsolete]`；`TriggerSingleUseQuestion` 移除 `grant_guard_sword` 特殊分支改為純泛用 table-lookup。新增：`guard-first-meet-dialogue-config.json`（對白外部化）；`GuardFirstMeetDialogueConfigData.cs`（DTO + 不可變配置）；`CharacterInteractionView.SetFirstMeetOverrideDialogue()` API（timing 解耦）；`VillageEntryPoint.OnGuardFirstMeetDialogueCompleted()`（發劍 + 發布 ExplorationGateReopenedEvent）；`_guardFirstMeetDialogueCompleted` session 記憶體旗標；整合測試 `GuardFirstMeetDialogueIntegrationTest.cs`（T1~T5）；更新 `GuardReturnSwordFlowIntegrationTest`（regression：F、H 改為 assert false）；更新 `GuardInteractViewDialogueRegressionTest`（測試 3 加 pragma warning suppress）→ **完成（2026-04-20）**

### E. 收尾

- [ ] **E1** `TBD-balance-001`、`TBD-balance-002`、`TBD-balance-003` 保持活躍，實機測試後依數據回填拍板（好感度節奏、探索產出覆蓋下游需求、守衛發問教學關卡卡關率）— E 類收尾等擴張範圍全部完成後再執行
- [ ] **E2** 建立 `dev-logs/<日期>-<序>.md` 記錄本 Sprint 實作細節（B/C/D 類完成時）
- [ ] **E3** Sprint 收尾三步（見下方「Sprint 結束動作」）

---

## 驗收標準

- [ ] 所有工作項目 checkbox 已打勾
- [ ] 對應 dev-logs 已補齊（B/C/D 類每次實作都有 dev-log）
- [ ] `project-status.md` 已更新至反映本 Sprint 結果
- [ ] 實機測試：開局 → 節點 0 → 選擇 1 → 節點 1 → 選擇 2 → 魔女對話結束 → 節點 2 L4 自動亮 → 節點 2 → 探索入口開啟；過程中無強制要求完成任何委託
- [ ] 物資檢查：開局到首次探索結束前，玩家無任何主動物資（除守衛贈劍需在 T2 完成時）
- [ ] 既有自動化測試全通過（排除與本 Sprint 無關的 CollectiblePointStateTests 既有失敗）

---

## 已識別的風險

| 風險 | 可能影響項目 | 緩解策略 |
|------|------------|---------|
| 物資僅靠探索可能供應不足，導致下游（委託/送禮/擴建）全部卡住 | B2 / D4 | 登記 `TBD-balance-002`，實機測試後評估是否調整探索產出 |
| 好感度累積變慢（送禮變稀缺） | D4 | 登記 `TBD-balance-001`，實機測試後評估是否調整對話好感度權重 |
| 原 T2/T3 訊號源移除，可能遺漏關聯程式碼 | C1 / C2 / C4 | 全文搜尋 `T2` / `T3` / `completion_condition_value` 相關常數確保清理乾淨 |
| 節點 2 L4 改為「魔女對話結束」觸發，但玩家可能先對話其他角色 | C1 / D2 | 僅訂閱「選擇 2 角色」的對話結束事件（依節點 1 VN 選項結果動態綁定） |
| **（2026-04-20 擴張）** 新流程「沒紅點也要主動發問」教學關卡可能讓部分玩家永久卡關 | A7 / C9~C12 / D5 | 登記 `TBD-balance-003`，實機測試後評估是否補保護機制（延遲紅點 / 守衛被動提示 / 其他） |
| **（2026-04-20 擴張）** 守衛歸來事件贈劍邏輯從 grant 派發改為玩家發問觸發，可能遺漏相關訊號接線 | C9 / C11 / C12 | 全文搜尋 `unlock_guard_sword` / `GuardReturnEventCompleted` / `DispatchGrantsByTrigger("guard_return")` 等關鍵字確保清理乾淨 |

---

## 製作人決策記錄（Sprint 中）

[Sprint 執行期間製作人做的新決策記在這。結束時同步至 `project-status.md`。]

- [ ] yyyy-mm-dd [決策內容]

---

## Sprint 結束動作（強制）

依 CLAUDE.md「Sprint 生命週期」步驟 3，所有項目完成後**必須在同一次回報中**：

1. [ ] 刪除本 Sprint 檔案（不保留歷史紀錄）
2. [ ] 將 `project-status.md` 的「活躍 Sprint」欄位更新為「無」或下一個 Sprint
3. [ ] 在 `project-status.md` 的「製作人決策記錄」加入「Sprint 6 完成」條目
4. [ ] 本 Sprint 執行中若產生新的 placeholder / 待拍板項目，登記至 `project-tbd.md`（目前已預先登記 `TBD-balance-001/002/003`）
