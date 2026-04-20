# Session State — Sprint 6 F12 完成（決策 6-13 守衛取劍改為首次進入自動對白觸發）

> **更新時間**: 2026-04-20
> **涉及流程**: /development-flow（F12 — 決策 6-13 守衛取劍新流程實作 + Revert 特殊題路徑）

## 當前任務

F12 完成。守衛取劍流程已改為「首次進入 CharacterInteractionView 自動觸發對白」。A7 GDD 對齊尚未完成（設計部任務），D4 實機測試等 A7 完成後執行。

## 進度清單

### 既有（Sprint 6 原範圍）
- [x] A1~A5 GDD 修改完成
- [x] B1~B3 Config 修改完成
- [x] C1~C4 程式邏輯修改完成
- [x] D1~D3 測試更新完成
- [x] C5~C8 D4 bugfix（節點觸發 / 剩下那位 / 探索按鈕 onClick）
- [x] A6 GDD 對齊（main-quest § 3 + 6 份連帶文件）

### 新增（Sprint 6 擴張，2026-04-20）
- [ ] A7-1 `character-unlock-system.md` v1.4 → v1.5（守衛歸來拆分贈劍）— **待 design-agent 執行**
- [ ] A7-2 `main-quest-system.md` v2.2 → v2.3（新 T2 完成條件重寫）— **待 design-agent 執行**
- [ ] A7-3 `exploration-system.md` 檢視更新（按鈕關閉狀態 + 提示訊息）
- [ ] A7-4 「沒紅點也要主動發問」教學設計原則章節 ← **F12 後可改為：自動對白設計原則**
- [ ] A7-5 `/doc-health --impact` 下游影響追蹤
- [x] B4 `guard-return-config.json` 移除贈劍段落 → **完成（2026-04-20）**
- [x] B5 `player-questions-config.json` 移除守衛「要拿劍」單次題（決策 6-13 revert）→ **完成（2026-04-20）**
- [x] B6 `initial-resources-config.json` trigger 改為 GuardSwordAsked + C# 常數 → **完成（2026-04-20）**
- [x] C9 CharacterUnlockManager 移除贈劍派發 + VillageEvents 4 個新事件 → **完成（2026-04-20）**
- [x] C10 ExplorationEntryManager Locked 狀態 + VillageHubView 訂閱 → **完成（2026-04-20）**
- [x] C11 PlayerQuestionsManager TriggerSingleUseQuestion（泛用化，無守衛特殊分支）→ **完成（2026-04-20）**
- [x] C12 VillageEntryPoint 整合訊號（鎖定/解鎖探索 + T2 完成）→ **完成（2026-04-20）**
- [x] D5 GuardReturnSwordFlowIntegrationTest（8 案例）+ GuardReturnIntegrationTest 更新 → **完成（2026-04-20）**
- [x] F7 bugfix（守衛按鈕未出現）→ **完成（2026-04-20）**
- [x] F8 bugfix（CG 重播 + 倒數干擾）→ **完成（2026-04-20）**
- [x] F9 bugfix（守衛 Hub 按鈕 FirstMeet 紅點永不消失）→ **完成（2026-04-20）**
- [x] F10 bugfix（系統性重構繞道路徑 + Revert F9）→ **完成（2026-04-20）**
- [x] F11 bugfix（路徑 B 補 SetState(Normal)）→ **完成（2026-04-20）**
- [x] F12（決策 6-13）守衛取劍改為首次進入自動對白觸發 → **完成（2026-04-20）**
- [ ] D4 實機測試（重跑完整新流程）— **等 A7 完成後執行**
- [ ] E1 TBD-balance-001/002 保持活躍（TBD-balance-003 已 Deprecated）
- [x] E2 dev-logs 補齊 — **dev-log 2026-04-20-7 ~ 2026-04-20-14 已建立**
- [ ] E3 Sprint 收尾三步（等全部完成）

## 關鍵決策

- **決策 6-7**：守衛歸來事件結尾不贈劍；探索暫時關閉
- **決策 6-8**：鎖定狀態每次點擊都顯示完整 modal
- **決策 6-9 (reverted)**：guard_ask_sword 特殊題已移除（F12 Revert）
- **決策 6-10 (cancelled)**：刻意不設保護機制 → 已不適用（新流程自動對白無需保護）
- **決策 6-11**：納入 Sprint 6，不開新 Sprint
- **決策 6-12**：廢棄 `guard-return-config.json` 對話路徑，守衛歸來劇情由 intro_guard CG 承載
- **決策 6-13**：守衛取劍改為「首次進入 CharacterInteractionView 自動觸發對白」；ExplorationGateReopenedEvent 改由對白完成 callback 發布；`TBD-balance-003` Deprecated；新增 `TBD-content-001`（守衛對白文本待撰寫）

## 已寫入檔案（本輪 F12）

- `Assets/Game/Resources/Config/player-questions-config.json` — 移除 guard_ask_sword
- `Assets/Game/Scripts/Village/PlayerQuestionsManager.cs` — TriggerFlagGrantGuardSword 標 Obsolete + TriggerSingleUseQuestion 泛用化
- `Assets/Game/Resources/Config/guard-first-meet-dialogue-config.json` — 新增（對白外部配置）
- `Assets/Game/Scripts/Village/GuardFirstMeetDialogueConfigData.cs` — 新增（DTO + 不可變配置）
- `Assets/Game/Scripts/Village/UI/CharacterInteractionView.cs` — 新增 SetFirstMeetOverrideDialogue API
- `Assets/Game/Scripts/Village/VillageEntryPoint.cs` — 新增 guard first-meet 配置/旗標/回調
- `Assets/Tests/Editor/Village/Integration/GuardFirstMeetDialogueIntegrationTest.cs` — 新增（T1~T5b）
- `Assets/Tests/Editor/Village/Integration/GuardReturnSwordFlowIntegrationTest.cs` — D/E/F/G/H 測試更新
- `Assets/Tests/Editor/Village/Integration/GuardInteractViewDialogueRegressionTest.cs` — 測試 3 pragma suppress
- `sprint/sprint-6-explore-gate-rework.md` — F12 checkbox 新增並打勾
- `dev-logs/2026-04-20-14.md` — 建立
- `FILE_MAP.md` — 新增 3 個新檔案條目
- `session-state/active.md` — 本次更新

## 下次恢復提示

1. 製作人在 Unity Editor Test Runner 執行 `GuardFirstMeetDialogueIntegrationTest`（T1~T5b）+ `GuardReturnSwordFlowIntegrationTest` 確認全通過
2. A7 GDD 對齊（design-agent 執行 A7-1 ~ A7-5）— 注意 A7-4 「沒紅點發問」已不適用，改為記錄「自動對白設計決策」
3. A7 完成後 design-head 審核（DH-GDD-REVIEW）
4. 製作人執行 D4 實機測試（含 F12 新流程：進入守衛 interact view → 自動對白 → 劍入背包 → 探索開啟）
5. D4 通過後 E 類收尾（Sprint 收尾三步）

<!-- STATUS -->
Epic: Sprint 6 探索開放流程重構 + 守衛歸來流程重構
Feature: F12 完成（決策 6-13 自動對白觸發），等 Unity 測試確認 + A7 GDD 對齊 + D4 實機測試
Task: 請製作人在 Unity 執行 GuardFirstMeetDialogueIntegrationTest + GuardReturnSwordFlowIntegrationTest
<!-- /STATUS -->
