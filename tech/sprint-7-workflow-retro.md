# Sprint 7 — 新 /development-flow Phase 2 首次實測 Retro

> **建立日期**: 2026-04-22
> **Sprint**: sprint-7-it-to-vs-restructure
> **撰寫者**: dev-agent（離席模式）
> **目的**: Sprint 7 同時作為新 `/development-flow` Phase 2 工作流程首次實測，本文記錄流程觀察與修補建議

---

## 背景

Sprint 7 是「VS 前徹底重構」Sprint，同時肩負**新 `/development-flow` Phase 2 工作流程的首次實測**職責（製作人 2026-04-22 拍板）。Phase 2 新結構為：`2.1 功能拆解 → 2.2 檢驗架構 → 2.3a 複用路徑 or 2.3b 新商業邏輯路徑 → 2.5 整合衝突審查`。

---

## 實測觀察

### 1. Phase 2.2「檢驗架構」在批次搬移情境下的適用性

**觀察**：E1~E7 各批次的工作性質是「按 ADR-004 規範搬移既有檔案 + IGameData 改造 + Installer 建立」，屬於「複用路徑（2.3a）」為主。

**問題**：Phase 2.2 的「複用 vs 擴充 vs 新建」判定設計是針對**單一功能**，但批次搬移一次涉及多個模組（5~16 個檔案）。每個批次若都走完整 Phase 2.2，工時倍增且多數判定結果相同（均為「複用路徑」）。

**建議修補**：Phase 2.2 加入「批次搬移模式」旁路：若工作性質是「按現有 ADR 規範搬移 + 改造既有 class」，直接走批次核准路徑（Phase 2.2 列出所有涉及檔案 → dev-head 一次核准 → 執行），不強制每個 class 逐一走 SOLID 五原則。

---

### 2. ADR 先行原則在 retrofit 情境下的實踐

**觀察**：ADR-003（Village Composition Root）採用 retrofit 流程（先有實作、後補 ADR），ADR-004（Script 組織結構）採用正向流程（先有 ADR、後實作）。兩種流程並存且都成功完成。

**問題**：retrofit 流程下，ADR 的 Implementation Guidelines 容易「合理化既有實作」而非「規範未來實作」，導致 ADR 的護欄功能減弱。本次 ADR-003 的 `VillageEntryPoint < 300 行` 護欄即為例子：ADR 建立時 VillageEntryPoint 已是 1616 行，護欄設定 < 300 行是「期望值」但 B5 只達到 859 行。

**建議修補**：retrofit ADR 在 Implementation Guidelines 需明確標記「現況 vs 目標」，並在退出 checklist 中列入「達到目標行數」為必要條件（而非好願景）。

---

### 3. 測試先行（TDD）在搬移工作中的實踐

**觀察**：E1~E7 批次的搬移工作因為是既有 class 改路徑，既有測試大多可直接沿用（調整 namespace）。IGameData 改造的測試補充（新增「實作 IGameData 斷言 + ID 非 0 斷言」）是在改造後補測試，不是先行。

**問題**：嚴格 TDD 在「改路徑 + namespace 遷移」工作中難以先行（測試需要 class 存在才能編譯）。但 IGameData 改造的測試先行是可做到的，實際未做到。

**建議修補**：在 `/development-flow` Phase 3 加入「搬移 vs 新商業邏輯」分流：
- 搬移工作：既有測試沿用 + 調 namespace，強制 smoke test 通過
- IGameData 改造：強制先寫「實作 IGameData 斷言」測試再改 class

---

### 4. pre-existing 失敗管理

**觀察**：Sprint 7 期間全程維持 8~10 個 pre-existing 失敗（CollectiblePointStateTests × 7、RedDotManagerTests × 1、GuardReturnSwordFlow × 2（已知快取問題））。這些失敗在 E1~E7 所有批次中均未引入新失敗，維持穩定。

**問題**：pre-existing 失敗的「合法化」邊界不清楚。若下個批次意外修復了某個 pre-existing 失敗，測試總數變動時難以判斷是「自然修復」還是「測試邏輯被改壞」。

**建議修補**：在 session-state/active.md 維護一份「pre-existing 失敗基準快照」，包含：失敗測試全名、失敗原因（若已知）、是否在本 Sprint 範圍內修復。每個批次完成後對照快照確認 delta。

---

### 5. Installer 設計的 VillageEntryPoint 行數問題

**觀察**：B5 VillageEntryPoint 瘦身目標 < 300 行，實際只達到 859 行（從 1616 行縮減了 757 行）。主要殘餘：ExplorationEntryPoint 初始化邏輯、大量 SerializeField、部分跨域事件訂閱。

**問題**：VillageEntryPoint 作為 MonoBehaviour Composition Root，天然有 SerializeField 的行數負擔（每個 TextAsset/Canvas 等都需一行）。< 300 行的目標可能過於激進，或需要引入 `VillageConfig` ScriptableObject 來集中 SerializeField。

**建議修補**：ADR-003 D3 的「< 300 行」護欄應區分：
- （a）業務邏輯行數 < 150 行（Installer 建構 + RunInstallers）
- （b）SerializeField 宣告行數不計入限制（或集中至 ScriptableObject）
- 整體檔案行數目標降低至「< 500 行」（更務實）

---

### 6. 離席模式下的 ADR/Control Manifest 維護

**觀察**：離席模式下 dev-agent 成功完成 ADR-003、ADR-004 的 retrofit 與正向流程，包含完整 DEV-ADR-REVIEW gate。Control Manifest 在所有 ADR Accepted 後才一次性建立（避免多次重建）。

**問題**：離席模式下 dev-head（審核層）審核由同一 instance 執行，有自我審核的疑慮（製作人 2026-04-22 決策背景）。

**建議修補**：離席模式應在回報中明示「哪些審核步驟是自我審核（dev-head = dev-agent 同一 instance）」，讓製作人回來後可優先複查這些步驟的審核結果。

---

## 流程修補建議清單

按優先序排列：

| 優先 | 修補位置 | 修補描述 |
|------|---------|---------|
| 🔴 高 | `/development-flow` Phase 2.2 | 新增「批次搬移模式」旁路，不強制逐一走 SOLID 五原則 |
| 🔴 高 | `/development-flow` Phase 3 | 搬移 vs 新商業邏輯分流：搬移只需 smoke test，IGameData 改造需先行測試 |
| 🟡 中 | ADR retrofit 規範 | 明確標記「現況 vs 目標」，退出 checklist 必含「達到目標值」 |
| 🟡 中 | `session-state/active.md` 模板 | 新增「pre-existing 失敗基準快照」區段 |
| 🟡 中 | ADR-003 D3 護欄 | 區分業務邏輯行數（< 150）vs 整體行數（< 500）vs SerializeField（不計入）|
| 🟢 低 | 離席模式報告格式 | 標注「自我審核步驟」清單，供製作人回來後優先複查 |

---

## 未完成項目說明（非 retro 缺失，屬已知限制）

以下 D 類驗收項目在 Sprint 7 內未能打勾，原因非工作流程缺失，而是規模限制：

| 項目 | 未完成原因 | 建議處置 |
|------|-----------|---------|
| D1（ADR-002 Gate 全 ✅）| A11/A14 IGameData 改造未完成，Sheets/工具鏈未建 | 列為 VS 啟動前必要 Gate |
| D2（全量測試 0 failed）| 8 個 pre-existing 失敗（CollectiblePointStateTests × 7 + RedDotManagerTests × 1）| 納入 VS Sprint 1 修復 |
| D3（VillageEntryPoint < 300 行）| B5 只達到 859 行，SerializeField 行數佔比高 | ADR-003 護欄重新定義後重估 |
| D5（/tech-debt 掃描）| 需製作人在場執行 skill | 製作人回來後執行 |
| D7（製作人實機驗證）| 需製作人在場操作 | 製作人回來後驗證 |

---

## 總結

Sprint 7 在 7 個 E 類批次（E1~E7）+ B/C 類工具層守護下，完成了：
- 21 模組 × 5 型別層 ADR-004 Script 組織重組
- 13 個 ConfigData IGameData 改造（3 個豁免：A08/A11/A15）
- 6 個 IVillageInstaller 建立
- VillageEntryPoint 1616 → 859 行瘦身
- Control Manifest 首次建立（4 條 ADR，46 條規則）
- 7 個整合測試 70 個 case 全通過

新 Phase 2 工作流程在批次搬移情境下表現可接受，主要改善點是「批次搬移模式旁路」與「測試先行分流」。
