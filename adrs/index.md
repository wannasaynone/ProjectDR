# ProjectDR — ADR 總表

本頁列出 ProjectDR 所有 ADR（Architecture Decision Record）。新增 ADR 時必同步更新本表。

## 目錄

| ADR | 狀態 | 標題 | 提出日期 | 最近更新 | 主題 |
|-----|------|------|---------|---------|------|
| [ADR-001](ADR-001-data-governance-contract.md) | Accepted | 資料治理契約 — IGameData 作為遊戲資料單一契約 | 2026-04-21 | 2026-04-22 | 資料 |
| [ADR-002](ADR-002-it-stage-exemption-exit.md) | Accepted | IT 階段例外退出清單 — 進 VS 前的資料治理清理 Gate | 2026-04-21 | 2026-04-22 | 資料 / Gate |
| [ADR-003](ADR-003-village-composition-root-contract.md) | Accepted | Village Composition Root 契約 — IVillageInstaller + VillageContext + VillageEntryPoint 瘦身 | 2026-04-22 | 2026-04-22 | 組裝 |
| [ADR-004](ADR-004-script-organization-structure-contract.md) | Accepted | Script 組織結構契約 — 模組邊界、5 層資料夾與 Namespace 規則 | 2026-04-22 | 2026-04-22 | 結構 |

## 依主題分類

### 資料治理

- **ADR-001**：IGameData 契約（所有 tabular data 的契約基線）
- **ADR-002**：IT 階段例外的退出清單（ADR-001 豁免的一次性 Gate）

### 結構 / 組裝

- **ADR-003**：Village Composition Root 契約（IVillageInstaller 介面 + VillageContext 共享容器 + VillageEntryPoint < 300 行瘦身）
- **ADR-004**：Script 組織結構契約（21 模組 × 5 型別層、Namespace 規則、新檔決策樹）

## Related TR-IDs

| ADR | 治理的 TR-ID |
|-----|-------------|
| ADR-001 | TR-data-001, TR-data-002 |
| ADR-002 | TR-data-003 |
| ADR-003 | TR-arch-005, TR-arch-006, TR-arch-007, TR-arch-008 |
| ADR-004 | TR-arch-001, TR-arch-002, TR-arch-003, TR-arch-004 |

## 狀態變更紀錄

| 日期 | 事件 |
|------|------|
| 2026-04-21 | ADR-001 / ADR-002 Accepted（retrofit 2026-04-21 製作人決策） |
| 2026-04-22 | ADR-001 / ADR-002 v1.1 更新（豁免條款收窄 + 退出時點鎖定為候選 C） |
| 2026-04-22 | ADR-004 建立（Proposed → Accepted，同日完成 DEV-ADR-REVIEW full gate） |
| 2026-04-22 | ADR-003 建立（Proposed → Accepted，Sprint 7 B1 retrofit；同日完成 DEV-ADR-REVIEW full gate） |

## 維護規則

- 本表由 dev-head 維護；新增 ADR 時必同步更新
- 狀態變更（Proposed → Accepted / Superseded）需在狀態變更紀錄加一行
- 若 ADR 累積 ≥ 3 條 Accepted，觸發 `/create-control-manifest` 條件；ADR 狀態變更後重建 Control Manifest
