# ADR-004: Script 組織結構契約 — 模組邊界、5 層資料夾與 Namespace 規則

> **狀態**: Accepted
> **提出日期**: 2026-04-22
> **最近更新**: 2026-04-22（DEV-ADR-REVIEW full gate 通過，Proposed → Accepted）
> **提出者**: dev-head（製作人 2026-04-22 六題拍板後歸納）
> **引擎**: Unity 6.0.x（ProjectDR）
> **取代**: —
> **被取代**: —

---

## Context（脈絡）

### 為何需要此決策

ProjectDR 在 Sprint 1~6 期間累積了約 **130+ 個 `.cs` 檔案**，其中 `Assets/Game/Scripts/Village/` 根目錄下就有 **70+ 個扁平並列的 `.cs` 檔案**（未計 meta），外加 `Village/Exploration/`（約 32 檔）、`Village/Exploration/Combat/`（約 14 檔）、`Village/UI/`（約 21 檔）、`Village/Mvp/UI/`（空）等子目錄。2026-04-22 製作人檢討定位以下結構性問題：

1. **扁平目錄導致新檔歸宿不明**：dev-agent 接到新功能時，預設行為是「丟到 `Village/` 根目錄」，因為沒有規則說該進哪個子資料夾。結果 Village/ 根目錄成為所有非 UI、非 Exploration 類別的大雜燴。
2. **模組邊界隱形**：從檔名可以**猜**出哪些是同一功能（如 `AffinityConfigData` + `AffinityManager`、`GiftManager` + `GiftAreaView`），但沒有 enforce 機制；跨模組的耦合（如 `CharacterUnlockManager` 寫死引用 `NodeDialogueController`、`GuardReturnEventController` 等多條依賴）無法透過目錄結構發現。
3. **結構災難前例**：2026-04-17 / 04-18 期間 `VillageEntryPoint.cs` 膨脹至 **1590 行、單方法 `InitializeManagers()` 270 行**，根因之一即是「所有 manager 的組裝邏輯都擠在同一 entry point」，沒有模組邊界來拒絕「新東西就塞進 VillageEntryPoint」的慣性。
4. **UI 類別混雜**：`Village/UI/` 目錄下同時放「與模組強綁定的 AreaView」（如 `AlchemyAreaView`、`FarmAreaView`、`GiftAreaView`）+「框架類 View 基礎設施」（`ViewBase`、`ViewController`、`ViewStackController`）+「Hub 層級的 VillageHubView」，職責層級不同卻並列，違反 `.claude/rules/ui-code.md` 的「職責分離」原則。
5. **Namespace 不一致**：目前部分類別用 `ProjectDR` 頂層 namespace、部分無 namespace、部分沿用 `KahaGameCore`；命名空間與目錄結構不對齊，import 語句缺少「看 namespace 就知道屬於哪個模組」的訊號。
6. **為 Sprint 7 VS 前 gate 提供結構前置**：ADR-002 已將 Sprint 7 定為獨立 Gate Sprint（候選 C），清資料層 + ADR/TR 治理。但資料層清理過程會觸碰大量 ConfigData 類別，**若無模組邊界，資料層改造會再次把檔案擠進錯誤目錄，清理完畢結構仍破碎**。因此結構契約必須與資料治理同步。

### 當前狀況與問題

- **Village/ 根目錄**：約 70 個 `.cs` 檔扁平並列，估算分屬 21 個功能模組
- **Exploration/ 目錄**：約 32 個 `.cs` 檔扁平並列（僅一個 `Combat/` 子資料夾）
- **Combat/ 目錄**：約 14 個 `.cs` 檔扁平並列（屬 Exploration 內部子模組）
- **UI/ 目錄**：約 21 個 `.cs` 檔扁平並列，包含 AreaView、Widget、框架類三種不同職責
- **Mvp/UI/**：空目錄（遺留結構）
- **沒有 namespace 規範文件**：每個新檔各自決定是否加 namespace、加哪個 namespace

### 相關約束

- **製作人 2026-04-22 六題拍板結果**（詳見 project-status.md 2026-04-22 條目）：
  - **Q1 = a**：21 個模組細分
  - **Q2 = a**：5 層型別資料夾（Manager / View / Data / Presenter / Interface）
  - **Q3 = a**：一律強制分層（即使模組只有 1-2 檔）
  - **Q4 = a**：UI 全拆散（AreaView → 各模組 View/；框架類 → Shared/View/；VillageHubView → Navigation/View/）
  - **Q5 = c**：交叉批次（7 批次 E+B 類整合）
  - **Q6 = a**：Namespace 完全跟資料夾走（子層不納入 namespace 避免過深）
- **規則一致性優先於檔案密度**：製作人明示「寧可資料夾空也要規則一致」，不得為小模組開例外
- **不阻斷 Sprint 7 資料層清理**：結構契約必須能與 ADR-002 [A] 區塊 16 個 ConfigData 改造交叉批次執行
- **測試目錄搬移不納入 Sprint 7**：ADR 規定「測試鏡像 runtime 結構」，但實際搬移登記為 tech-debt

### 相關的 GDD 技術需求（TR-ID）

- `TR-arch-001` Script 組織 — 模組化目錄結構（新登記）
- `TR-arch-002` Script 組織 — 5 層型別子資料夾（新登記）
- `TR-arch-003` Script 組織 — Namespace 完全跟資料夾走（新登記）
- `TR-arch-004` Script 組織 — 測試鏡像 runtime 結構（新登記）

---

## Decision（決策）

**一句話**：`projects/ProjectDR/Assets/Game/Scripts/Village/` 下的所有遊戲程式碼必須依「模組 / 型別層」兩級目錄結構組織，模組邊界為 21 個固定清單，型別層為固定 5 層（Manager / View / Data / Presenter / Interface），namespace 規則為 `ProjectDR.Village.<Module>`（不含型別子層）；新檔必須走決策樹找到唯一歸宿，規則一致性優先於檔案密度。

後續展開：

### D1. 模組邊界（21 個模組，固定清單）

Village 根目錄下只能存在以下 21 個模組資料夾（+ `Shared/`、`Navigation/` 共用資料夾，共 23 個一級子資料夾）；**禁止**在 Village 根目錄放任何 `.cs` 檔。

| # | 模組資料夾 | 模組描述 | 代表檔案（改制前位置，皆位於 Village/） |
|---|-----------|---------|---------------------------------------|
| M01 | `Affinity/` | 好感度 | AffinityConfigData, AffinityManager |
| M02 | `Alchemy/` | 煉金 / 通用製作工作台（UI 層級） | AlchemyAreaView, CraftWorkbenchView, CraftItemSelectorView, CraftSlotWidget, CommissionInteractionPresenter |
| M03 | `Backpack/` | 背包 | BackpackManager, BackpackSlot, BackpackSnapshot |
| M04 | `CG/` | CG 畫廊與 CG 解鎖 | CGGalleryView, CGSceneConfigData, CGUnlockManager, ICGPlayer, PlaceholderCGPlayer, ResourcesCGProvider |
| M05 | `CharacterIntro/` | 角色登場 / 介紹 CG | CharacterIntroCGPlayer, CharacterIntroConfigData, CharacterIntroCGView |
| M06 | `CharacterInteraction/` | 角色互動（對話開啟、選單派發） | CharacterInteractionView, CharacterMenuData, HCGDialogueSetup |
| M07 | `CharacterQuestions/` | 角色發問 / 玩家發問 | CharacterQuestionCountdownManager, CharacterQuestionsConfigData, CharacterQuestionsManager, CharacterQuestionsView, PlayerQuestionsConfigData, PlayerQuestionsManager, PlayerQuestionsView |
| M08 | `CharacterStamina/` | 角色體力 | CharacterStaminaManager |
| M09 | `CharacterUnlock/` | 角色解鎖 | CharacterIdSnakeCaseMapper, CharacterUnlockManager |
| M10 | `Commission/` | 委託系統 | CommissionManager, CommissionRecipesConfigData |
| M11 | `Dialogue/` | 基礎對話播放（狀態機、冷卻） | DialogueCooldownManager, DialogueData, DialogueManager |
| M12 | `Farm/` | 農田 | FarmManager, FarmPlot, SeedData, FarmAreaView |
| M13 | `Gift/` | 贈禮 | GiftManager, GiftAreaView |
| M14 | `Greeting/` | 招呼語 | GreetingConfigData, GreetingPresenter |
| M15 | `GuardReturn/` | 守衛歸來事件（已於決策 6-12 部分廢棄，但目前檔案仍存） | GuardFirstMeetDialogueConfigData, GuardReturnConfigData, GuardReturnEventController |
| M16 | `IdleChat/` | 閒聊 | IdleChatConfigData, IdleChatPresenter |
| M17 | `ItemType/` | 物品分類系統 | ItemTypes, ItemTypeResolver |
| M18 | `MainQuest/` | 前期主線任務 | MainQuestConfigData, MainQuestManager |
| M19 | `NodeDialogue/` | 節點劇情（VN 選項、分支） | NodeDialogueConfigData, NodeDialogueController |
| M20 | `OpeningSequence/` | 開場序列 | OpeningSequenceController |
| M21 | `Progression/` | 村莊進度 / 解鎖進度 / 初始資源派發 / RedDot | VillageProgressionManager, InitialResourceDispatcher, InitialResourcesConfigData, RedDotManager |
| M22 | `Storage/` | 倉庫 / 擴建 / 背包倉庫互轉 | StorageManager, StorageTransferManager, StorageExpansionConfigData, StorageExpansionManager, StorageAreaView |
| M23 | `TimeProvider/` | 時間提供者 | ITimeProvider, SystemTimeProvider |

**共用資料夾**（不屬於 21 模組清單，但位於 Village/ 一級目錄）：

| 資料夾 | 用途 | 代表檔案 |
|-------|------|---------|
| `Navigation/` | 村莊區域導航與 Hub 層級 | AreaIds, VillageNavigationManager, VillageHubView, ExplorationEntryManager, VillageEvents |
| `Shared/View/` | 框架類 View 基礎設施（非模組特定） | ViewBase, ViewController, ViewStackController, TypewriterEffect |
| `Core/` | Village 整體組裝與共用契約（為 ADR-003 Village Composition Root 預留） | VillageEntryPoint（暫留，ADR-003 建立後拆為 VillageContext + IVillableInstaller） |
| `CharacterIds.cs`（例外）| Village 層級角色 ID 常數 | 放 `Navigation/` 或 `Shared/` 由 ADR-004 實作時拍板，先暫擱 `Navigation/` |

**Exploration 內部結構**（相同規則套用）：

`Exploration/` 視為獨立大模組，內部再分模組；Sprint 7 對齊 ADR-004 規則：

| 子模組 | 描述 | 代表檔案 |
|-------|------|---------|
| `Exploration/Core/` | 探索 Entry Point 與事件 | ExplorationEntryPoint, ExplorationEvents |
| `Exploration/Map/` | 地圖資料與格子 | MapData, MapDataJson, GridMap, GridCellView, CellType |
| `Exploration/Movement/` | 移動輸入與玩家視圖 | ExplorationInputHandler, ExplorationFreeInputHandler, ExplorationFreePlayerView, ExplorationPlayerView, PlayerFreeMovement, PlayerGridMovement, MoveDirection |
| `Exploration/MoveSpeed/` | 移動速度計算 | IMoveSpeedCalculator, IMoveSpeedProvider, FixedMoveSpeedCalculator, FixedMoveSpeedProvider, SpdMoveSpeedCalculator, SpdMoveSpeedProvider |
| `Exploration/Camera/` | 攝影機 | ExplorationCameraFollow, ExplorationMapView |
| `Exploration/Collection/` | 採集點與互動 | CollectionManager, CollectiblePointData, CollectiblePointState, CollectionGatheringView, CollectionItemPanelView, CollectionInteractionHintView, CollectiblePointIndicatorView |
| `Exploration/Evacuation/` | 撤離 | EvacuationManager, EvacuationView |
| `Exploration/Combat/` | 戰鬥（已為獨立子資料夾，繼續保留，但內部套 5 層規則） | CombatManager, CombatConfigData, CombatInputHandler, DamageCalculator, DamageNumberView, DeathManager, DeathView, MonsterManager, MonsterConfigData, MonsterState, MonsterView, PlayerCombatStats, PlayerCombatView, PlayerContactDetector, SwordAttack, AimIndicatorView, IMonsterPositionProvider |

### D2. 型別層（5 層固定子資料夾）

每個模組資料夾內部強制分為以下 5 層子資料夾（**一律分層，不看檔案數**）：

| 子層 | 職責判準 | 典型檔案後綴 / 特徵 |
|------|---------|------------------|
| `Manager/` | 狀態管理器、邏輯控制器、流程機 | `*Manager.cs`、`*Controller.cs`、`*Dispatcher.cs`、`*Calculator.cs`、`*Resolver.cs`、`*Handler.cs`（非輸入處理類可視情況歸於 Manager） |
| `View/` | MonoBehaviour 的視覺呈現層（只做顯示 + 輸入派發，不擁有遊戲狀態） | `*View.cs`、`*Widget.cs`、`*AreaView.cs`、`*Effect.cs`（純視覺特效） |
| `Data/` | 純資料結構、DTO、Config、常數 | `*ConfigData.cs`、`*Data.cs`、`*Snapshot.cs`、`*Ids.cs`（模組級 ID 常數）、`*Types.cs`、`*State.cs`（純資料狀態） |
| `Presenter/` | 「Manager 狀態 ↔ View 顯示」的中介層；不擁有狀態、不做 I/O | `*Presenter.cs` |
| `Interface/` | 該模組對外公開的介面、抽象契約 | `I*.cs`（例如 `ICGPlayer`、`ITimeProvider`、`IMoveSpeedProvider`）、`*Interceptor.cs`（行為攔截契約）|

**分層判準詳解**：

- **Manager vs Presenter**：Manager 擁有狀態、可被單元測試（無 Unity 依賴）；Presenter 僅在 Manager 狀態變動時更新 View 顯示、在 View 輸入時呼叫 Manager 方法，不自擁狀態
- **View vs Presenter**：View 是 MonoBehaviour，綁定 GameObject；Presenter 是 POCO，可有可無 MonoBehaviour 外殼。UI 類別若兼具顯示 + 中介邏輯，應拆為 View + Presenter 兩檔
- **Data vs Interface**：Data 是具體結構（class / struct / readonly struct）；Interface 是契約（interface 或 abstract class）
- **Input Handler 歸屬**：遊戲輸入相關類別（`CombatInputHandler`、`ExplorationInputHandler`）歸於對應模組的 `Manager/` 子層，不另立 `Input/` 層（理由：5 層固定，不增）
- **Effect / Camera 類**：視覺效果類（`TypewriterEffect`）、攝影機類（`ExplorationCameraFollow`）視為 View 子類，歸於 `View/`

**5 層之外禁止新增**：即使模組複雜，不得自建 `Utils/`、`Helpers/`、`Factory/` 等第 6 層。若確實需要共用工具，應評估是否應歸於 `Shared/` 或提升為獨立模組。

**空子層的處理**：即使某模組某層沒有任何檔案（例：`Greeting/Interface/` 可能為空），**仍須建立空資料夾 + `.keep` 檔保留結構**，以維持「掃目錄即可看出模組規模」的可預測性。

**運維細則（2026-04-22 追加，製作人拍板）**：

上述規則（空子層保留 `.keep`）適用於**建立期（初始骨架完整化、大規模搬檔過程中）**。大規模搬檔完成後進入**運維期**，可執行以下清理：

- **清理時機**：大規模搬檔（如 Sprint 7 E 類）完成、確認編譯 0 error 且測試不退化後，方可一次性清除空子層
- **清理對象**：只有 `.keep` 檔、無任何 `.cs` 的子資料夾（連同 `.keep` 與對應 `.meta` 一起刪除）
- **清理方式**：透過 Unity Editor MCP 刪除，保留 Unity meta 系統一致性；禁止直接在 OS 刪除（會遺留孤兒 .meta）
- **清理後的新增規則**：未來新 `.cs` 歸屬到已被清除的子層時，**先重建資料夾（含 .keep）再放入檔案**，不可因「之前被清了」而改變歸宿
- **TR-arch-002 仍有效**：「強制 5 層」規則仍是建立期的黃金規則；運維清理是事後整理，不代表可以在建立新模組時省略 5 層骨架
- **文件更新**：清理後需同步更新 `FILE_MAP.md` 與 `control-manifest.md`（移除空子層條目）

### D3. 強制分層規則（一律分，不看檔案數）

**所有 21 模組 + Exploration 8 子模組 + 共用 `Navigation/` + `Shared/`（若放程式碼）一律強制走 5 層子資料夾**，即使某模組只有 1~2 個檔案。

範例：

- `Greeting/` 僅 2 檔（GreetingConfigData、GreetingPresenter）→ 分為 `Greeting/Data/GreetingConfigData.cs` + `Greeting/Presenter/GreetingPresenter.cs` + 空 `Greeting/Manager/`、`Greeting/View/`、`Greeting/Interface/`（各含 `.keep`）
- `TimeProvider/` 僅 2 檔（ITimeProvider、SystemTimeProvider）→ 分為 `TimeProvider/Interface/ITimeProvider.cs` + `TimeProvider/Manager/SystemTimeProvider.cs` + 空 `TimeProvider/View/`、`TimeProvider/Data/`、`TimeProvider/Presenter/`

**設計理由（製作人 2026-04-22 明示）**：
- 規則一致可預測，dev-agent 新增檔案時不需判斷「這模組夠大嗎」，只需走決策樹
- 模組成長時不需回頭重構結構
- IDE 檔案樹展開時，每個模組外觀一致

**已知代價**（在 Consequences 章節誠實列出）：資料夾膨脹、部分空子層無內容。

### D4. UI 拆散規則

2026-04-22 Q4=a 拍板：現有 `Village/UI/` 目錄下 21 檔必須全拆到對應歸宿，拆完後**禁止**保留 `Village/UI/` 頂層目錄。拆解表：

| 現有檔案 | 新歸宿 | 理由 |
|---------|-------|------|
| `AlchemyAreaView.cs` | `Alchemy/View/` | 與 Alchemy 模組強綁定 |
| `CraftWorkbenchView.cs` | `Alchemy/View/` | 煉金工作台 UI，屬 Alchemy |
| `CraftItemSelectorView.cs` | `Alchemy/View/` | 同上 |
| `CraftSlotWidget.cs` | `Alchemy/View/` | Alchemy 工作台內的格子 Widget |
| `FarmAreaView.cs` | `Farm/View/` | 與 Farm 模組強綁定 |
| `GiftAreaView.cs` | `Gift/View/` | 與 Gift 模組強綁定 |
| `StorageAreaView.cs` | `Storage/View/` | 與 Storage 模組強綁定 |
| `ExplorationAreaView.cs` | `Navigation/View/` | Hub 側的「進探索」按鈕面板，不進 Exploration 模組（Exploration 是獨立 runtime） |
| `CGGalleryView.cs` | `CG/View/` | 與 CG 模組強綁定 |
| `CharacterInteractionView.cs` | `CharacterInteraction/View/` | 同模組 |
| `CharacterIntroCGView.cs` | `CharacterIntro/View/` | 同模組 |
| `CharacterQuestionsView.cs` | `CharacterQuestions/View/` | 同模組 |
| `PlayerQuestionsView.cs` | `CharacterQuestions/View/` | 玩家發問屬 CharacterQuestions 模組（同一系統兩視角） |
| `VillageHubView.cs` | `Navigation/View/` | Village Hub 選單，屬導航層 |
| `ViewBase.cs` | `Shared/View/` | UI 框架基底類，非模組特定 |
| `ViewController.cs` | `Shared/View/` | UI 框架控制器，非模組特定 |
| `ViewStackController.cs` | `Shared/View/` | UI 框架堆疊控制器，非模組特定 |
| `TypewriterEffect.cs` | `Shared/View/` | 通用打字機效果 UI，可被多模組使用 |
| `CommissionInteractionPresenter.cs` | `Alchemy/Presenter/`（若實際綁 Alchemy 工作台）或 `Commission/Presenter/`（若綁 Commission）| 依實際引用判定，**預設歸 Alchemy**（現有 Commission 是透過 Alchemy 工作台派發）|

**UI 框架三類歸屬規則**：

1. **AreaView 類（模組強綁定的 UI）**：歸各模組的 `View/`（例：`Alchemy/View/AlchemyAreaView.cs`）
2. **框架類（跨模組可重用的 View 基礎設施）**：歸 `Shared/View/`
3. **Hub / 導航類 UI**：歸 `Navigation/View/`

**`Mvp/UI/` 處理**：現有空目錄（Mvp 已完成 View stack 重構遺留），ADR-004 實作時刪除。

### D5. Namespace 規則

**完全跟資料夾走到「模組層」為止，型別子層不納入 namespace**（製作人 2026-04-22 Q6=a）。

格式：

```
ProjectDR.Village.<ModuleName>
```

| 類別位置 | Namespace |
|---------|----------|
| `Village/Affinity/Manager/AffinityManager.cs` | `ProjectDR.Village.Affinity` |
| `Village/Affinity/Data/AffinityConfigData.cs` | `ProjectDR.Village.Affinity` |
| `Village/Greeting/Presenter/GreetingPresenter.cs` | `ProjectDR.Village.Greeting` |
| `Village/Navigation/View/VillageHubView.cs` | `ProjectDR.Village.Navigation` |
| `Village/Shared/View/ViewBase.cs` | `ProjectDR.Village.Shared` |
| `Village/Core/VillageEntryPoint.cs` | `ProjectDR.Village.Core` |
| `Village/Exploration/Core/ExplorationEntryPoint.cs` | `ProjectDR.Village.Exploration.Core`（**例外：Exploration 作為大模組，內部子模組納入 namespace**）|
| `Village/Exploration/Combat/Manager/CombatManager.cs` | `ProjectDR.Village.Exploration.Combat` |
| `Village/Exploration/Movement/View/PlayerFreeMovement.cs` | `ProjectDR.Village.Exploration.Movement` |

**Exploration 例外說明**：Exploration 因為內部複雜度高（8 子模組、46 檔），視為「子 Village」處理，namespace 多一層 `Exploration.<SubModule>`。這是唯一允許 namespace 三層的情境，**不得**再往下延伸到 `Exploration.Combat.Manager`（子模組內仍遵循「型別子層不納入 namespace」原則）。

**禁止事項**：
- 禁止不加 namespace 的 `.cs`（頂層散落會污染全域型別表）
- 禁止 namespace 與目錄脫鉤（例：檔案在 `Farm/` 但 namespace 宣告為 `ProjectDR.Village.Alchemy`）
- 禁止把型別子層（Manager/View/Data/Presenter/Interface）寫進 namespace（例：`ProjectDR.Village.Farm.Manager` ❌）

### D6. 測試目錄鏡像規則（tech-debt 登記）

ADR 規定：測試目錄必須鏡像 runtime 結構。

```
projects/ProjectDR/Assets/Tests/Game/Village/<Module>/<Type>/
```

例：`AffinityManager.cs` 的測試位於 `Assets/Tests/Game/Village/Affinity/Manager/AffinityManagerTests.cs`。

**Sprint 7 範圍處理**：實際搬移測試**不納入** Sprint 7（避免 Sprint 7 範圍無限膨脹）。登記為 tech-debt：
- `tech-debt.md` 新增條目：`[結構] 測試目錄鏡像 runtime 結構（ADR-004）`
- 預估工時：8~12h（搬移 + 更新 Test Runner assembly reference）
- 觸發時機：VS 階段啟動後任一 Sprint 的閒置時段，或下次有大規模測試新增時一併整理

### D7. 新檔決策樹（加入強制規則區塊）

dev-agent / dev-head 新增任何 `.cs` 檔時，**必須**依下列決策樹決定歸宿，不可憑感覺：

```
新 .cs 檔
  │
  ├─ Step 1: 屬於 Village 還是其他範圍？
  │    ├─ Village → 進入 Step 2
  │    ├─ Exploration 運行時（非 Village Hub）→ 進入 Exploration 決策樹（內部規則同）
  │    └─ 非 Village 也非 Exploration → 超出 ADR-004 範圍，另案處理
  │
  ├─ Step 2: 屬於哪個模組？
  │    ├─ 能唯一對應 21 模組之一 → 歸該模組
  │    ├─ 屬於跨模組的 UI 框架基礎設施 → 歸 Shared/View/
  │    ├─ 屬於 Hub / 導航層 → 歸 Navigation/
  │    ├─ 屬於 Village 整體組裝 / Composition Root → 歸 Core/
  │    └─ 無法唯一歸屬 → STOP，呼叫 dev-head 評估是否需新增模組（更新本 ADR 的 21 模組清單 + 加版本行）
  │
  ├─ Step 3: 屬於 5 層的哪一層？
  │    ├─ 狀態管理 / 邏輯流程 → Manager/
  │    ├─ MonoBehaviour 視覺呈現 → View/
  │    ├─ 純資料 / DTO / Config / 常數 → Data/
  │    ├─ Manager ↔ View 中介 → Presenter/
  │    ├─ 介面 / 抽象契約 → Interface/
  │    └─ 無法唯一歸屬 → STOP，可能職責混淆，應拆檔
  │
  ├─ Step 4: 設定 namespace
  │    └─ 一律 `ProjectDR.Village.<ModuleName>`（Exploration 例外見 D5）
  │
  └─ Step 5: 若為新模組，同步更新
       ├─ 本 ADR Decision § D1 的 21 模組清單（加版本行）
       ├─ tr-registry.yaml（若涉及新技術需求）
       ├─ FILE_MAP.md
       └─ 重建 control-manifest（若 Accepted ADR 條件觸發）
```

### D8. 禁止事項清單

- **禁止在 Village 根目錄放任何 `.cs` 檔**（違反 D1）
- **禁止在模組資料夾根部放 `.cs` 檔**（必須在 5 層子資料夾之一內；例：`Farm/FarmManager.cs` ❌，`Farm/Manager/FarmManager.cs` ✅）
- **禁止新增 5 層之外的子資料夾**（例：`Alchemy/Utils/` ❌）
- **禁止因「檔案少」而不分層**（違反 D3 Q3=a）
- **禁止 namespace 與目錄脫鉤**（違反 D5）
- **禁止在 namespace 中加入型別子層**（違反 D5）
- **禁止跨模組循環引用**（例：Farm 引用 Gift 的同時 Gift 又引用 Farm）—— 若確實需要，應抽共用契約至 Shared/ 或提升為獨立模組
- **禁止在 `Village/UI/` 或 `Village/Mvp/UI/` 新增檔案**（這兩個目錄將在 Sprint 7 E 類拆空後刪除）
- **禁止把「框架類 View」放進模組 View/**（例：ViewBase 不得放 Alchemy/View/）
- **禁止 Exploration 內部跨子模組放檔**（例：Combat 類別放 Exploration/Collection/）

---

## Alternatives Considered（考慮過的替代方案）

### 方案 A：採納 — 模組 × 型別兩級強制，規則一致性優先

- 做法：21 固定模組 × 5 固定型別層，一律強制分，空子層留 .keep
- 優點：
  - 新檔歸宿唯一確定，dev-agent 不需判斷
  - 模組成長時不需回頭重構
  - IDE 掃目錄即可看出模組規模
- 缺點：
  - 部分模組空子層多（Greeting、TimeProvider 等）
  - 資料夾層級 +1，滾動距離變長
- **採納理由**：製作人 2026-04-22 明示「規則一致性優先於檔案密度」，本方案直接對應 Q1~Q6 六題拍板

### 方案 B：（未採納）— 模組分，型別層依檔案數彈性

- 做法：模組 ≥ 4 檔才強制分 5 層，少於 4 檔扁平放
- 優點：空資料夾少、目錄精簡
- 缺點：
  - 模組成長到第 4 檔時需回頭重構（違反「結構穩定」原則）
  - dev-agent 需判斷「這模組夠大嗎」，決策樹複雜化
  - 不同模組外觀不一致，難預測
- **未採納理由**：製作人 Q3=a 明示選「一律強制分」

### 方案 C：（未採納）— 僅分型別層，不分模組

- 做法：Village/Manager/、Village/View/、Village/Data/ 三層，內部不分模組
- 優點：極簡
- 缺點：
  - 每個型別層內部仍是大雜燴（Village/Manager/ 會有 20+ manager 扁平放）
  - 跨模組耦合無法透過目錄發現
  - 實際上是現況問題（UI/ 目錄）的重演
- **未採納理由**：不解決模組邊界問題，只搬了位置

### 方案 D：（未採納）— 僅分模組，不分型別層

- 做法：Village/Affinity/、Village/Farm/ 等 21 模組，模組內部扁平
- 優點：結構簡單
- 缺點：
  - 模組內部混雜 Manager / View / Data / Config 等不同職責
  - 無法看出哪些檔案是介面、哪些是資料 DTO
  - 違反製作人 Q2=a 的 5 層型別細分
- **未採納理由**：製作人 Q2=a 明示選 5 層

---

## Consequences（後果）

### 正面

- **規則一致可預測**：dev-agent 新增檔案時決策樹明確，不需判斷模組大小
- **模組邊界清晰**：跨模組依賴必須透過明顯的 `using ProjectDR.Village.X` 出現，code review 時可即時發現
- **新檔有明確歸宿**：杜絕「丟到 Village/ 根目錄」的慣性，預防 VillageEntryPoint 爆炸再現
- **UI 職責分離落地**：AreaView 與模組同處，框架類獨立於 Shared/，與 `.claude/rules/ui-code.md` 的「職責分離」原則對齊
- **Namespace 可反推位置**：看到 `ProjectDR.Village.Farm` 立即知道檔案在 `Assets/Game/Scripts/Village/Farm/`
- **支援 ADR-003 落地**：Core/ 子資料夾為後續 VillageContext + IVillableInstaller 預留歸宿
- **為 Sprint 7 資料層清理降風險**：[A] 16 個 ConfigData 改造在新結構下各歸對應 `<Module>/Data/`，不會再次散落

### 負面

- **資料夾膨脹**：21 模組 × 5 層 = 105 個型別子資料夾（+ Exploration 內部 8 子模組 × 5 層 = 40 個），其中預估 30~50 個為空（只含 .keep）
- **小模組開發摩擦**：Greeting（2 檔）、TimeProvider（2 檔）等需建 5 個資料夾只填 2 個，IDE 檔案樹展開較繁瑣
- **Namespace 改動面大**：~130 個 `.cs` 檔全面改 namespace，Rider / VS 批次工具可處理但需人工檢查不小心改到 meta 檔的 guid 或 Unity 引用
- **Sprint 7 E 類工時增加**：原 Sprint 7 僅有 A/B/C/D 四區塊（~45h），加入 E 類後估 ~170~210h 總工時（製作人已接受）
- **測試目錄搬移延後**：ADR 規定但 Sprint 7 不做，形成已知 tech-debt，需追蹤至 VS 階段啟動前處理

### 中性 / 待觀察

- **空資料夾 UX**：git 不追蹤空目錄，需以 `.keep` 檔佔位；部分 IDE（VS）對空資料夾顯示不一致
- **命名空間深度**：`ProjectDR.Village.Exploration.Movement` 雖三層但 Exploration 是例外；若未來其他模組也複雜化想比照例外，應走 ADR 修訂而非自行擴展
- **模組邊界演化**：21 模組清單可能隨新需求變動；演化機制為「新增模組必更新 ADR + 加版本行」，保留歷史
- **與既有 Assembly Definition 關係**：目前 `Game.asmdef` 為單一 assembly；若未來需拆 assembly（例：Village / Exploration 各自 asmdef），ADR-004 結構可作基礎，但本 ADR 不預先決策

---

## Engine Compatibility（引擎相容性）

| 項目 | 內容 |
|------|------|
| 涉及引擎 | Unity 6.0.x |
| 涉及 API / 模組 | Assembly Definition（`Game.asmdef`）、Unity meta 檔 GUID 機制 |
| LLM 知識截止後的風險 | LOW（目錄與 namespace 為 C# 標準，非 Unity 專屬 API） |
| 需驗證的 API 行為 | Unity meta 檔在資料夾搬移時的 GUID 保留行為（Unity 6 與 Unity 2022 一致）|
| 已讀過的版本遷移文件 | projects/ProjectDR/tech/engine-reference/unity/VERSION.md（若未建立，Sprint 7 執行前補建）|

**實作注意**：使用 Unity Editor 移動檔案（右鍵 Move）而非檔案總管直接拖曳，以確保 meta 檔的 GUID 與資源引用不遺失。

---

## Implementation Guidelines（實作指引）

### 必須做（Required）

- **嚴格遵守 D1 的 21 模組清單 + 4 個共用資料夾**：新增模組必走 ADR 修訂
- **嚴格遵守 D2 的 5 層子資料夾**：新增第 6 層必走 ADR 修訂
- **空子層必建 `.keep` 檔**：維持結構可預測性
- **namespace 與目錄對齊**：依 D5 格式，Exploration 例外見 D5
- **新檔必走 D7 決策樹**：不可憑感覺決定歸宿
- **Unity Editor 移動檔案**：使用 Project 視窗右鍵 Move（保留 meta 檔 GUID）
- **搬移完成後跑編譯**：確保無破引用，Unity Console 無 error

### 禁止做（Forbidden）

- **禁止在 Village 根目錄放 `.cs`**（D8）
- **禁止在模組資料夾根部放 `.cs`**（D8）
- **禁止新增 5 層之外的子資料夾**（D8）
- **禁止 namespace 與目錄脫鉤**（D8）
- **禁止因「暫時性」繞過決策樹**（D7）
- **禁止跨模組循環引用**（D8）

### 護欄（Guardrail）

- **單一模組檔案數上限**：若某模組單層（例：`Farm/Manager/`）檔案數超過 10，應評估是否該拆子模組；超過 15 為強制拆分警戒線
- **Namespace 深度上限**：一般模組 3 層（`ProjectDR.Village.<Module>`），Exploration 例外 4 層（`ProjectDR.Village.Exploration.<SubModule>`），不得更深
- **模組清單變動頻率**：建議每季不超過 2 次新增模組；頻繁新增代表初始邊界劃分有問題，應回頭檢討

### 測試要求

- **ADR 本身無單元測試**（結構性決策）
- **搬移完成後必跑**：
  - Unity Play Mode Test 全綠
  - Edit Mode Test 全綠
  - 遊戲 smoke test：從 Title → Village Hub → 進任一 Area → 退回 → 進 Exploration → 戰鬥 → 撤離 → 回 Hub，無 NRE / 無 console error
- **每個 Sprint 7 E 類批次完成後跑 smoke test**：確保批次間不累積破引用

---

## GDD Requirements Addressed（對應 GDD 需求）

| TR-ID | 需求摘要 | 來源 GDD |
|-------|---------|---------|
| TR-arch-001 | Script 組織必須依 21 固定模組清單 + 4 個共用資料夾結構 | 工作室級規則衍生（CLAUDE.md § Conventions § Source code + 製作人 2026-04-22 決策） |
| TR-arch-002 | 每個模組內部強制分為 5 層型別子資料夾（Manager / View / Data / Presenter / Interface），即使檔案數少於 5 仍強制分 | 同上 |
| TR-arch-003 | Namespace 完全跟資料夾走到模組層為止，型別子層不納入 namespace，格式 `ProjectDR.Village.<Module>`（Exploration 例外 `ProjectDR.Village.Exploration.<SubModule>`） | 同上 |
| TR-arch-004 | 測試目錄必須鏡像 runtime 結構（`Assets/Tests/Game/Village/<Module>/<Type>/`），實際搬移登記為 tech-debt | 同上 |

---

## Status History（狀態更動紀錄）

| 版本 | 日期 | 狀態 | 變更摘要 |
|------|------|------|---------|
| v1.0 | 2026-04-22 | Proposed | 初次提出（依製作人六題 Q1=a/Q2=a/Q3=a/Q4=a/Q5=c/Q6=a 拍板） |
| v1.1 | 2026-04-22 | Accepted | 經 dev-head 走 DEV-ADR-REVIEW gate（full 模式）通過 |
| v1.2 | 2026-04-22 | Accepted | 追加「運維細則」：大規模搬檔完成後可清除空子層（含 .keep）；建立期 5 層骨架規則不變；製作人 2026-04-22 拍板（清空資料夾指令） |

---

## 相關連結

- **相關 ADR**：
  - ADR-001（資料治理契約）：16 個 ConfigData 改造在新結構下歸 `<Module>/Data/`；ADR-001 的契約（IGameData）仍為唯一資料治理契約，ADR-004 僅規範「檔案放哪」不改變「檔案該實作什麼」
  - ADR-002（IT 階段例外退出清單）：[A] 區塊 16 個 ConfigData 改造的路徑以 ADR-004 新結構為準；ADR-002 需加一行註記「路徑以 ADR-004 新結構為準，見 control-manifest」
  - ADR-003（Village Composition Root 契約，Sprint 7 B1 將建立）：需引用 ADR-004 的 `Core/` 位置放 VillageContext + IVillableInstaller；ADR-003 實際撰寫時對應引用本 ADR
- **相關規則**：
  - `.claude/rules/gameplay-code.md`（作用路徑涵蓋 `projects/*/Assets/Game/Scripts/**`）
  - `.claude/rules/ui-code.md`（作用路徑涵蓋 `projects/*/Assets/Game/Scripts/UI/**`，ADR-004 後改為「各模組 View/」）
  - `.claude/rules/test-standards.md`（作用路徑涵蓋 `projects/*/Assets/Tests/**`，ADR-004 後要求鏡像 runtime 結構）
  - `CLAUDE.md § Conventions § Source code`（工作室級慣例，Unity 專案路徑為 `projects/<專案名>/Assets/Game/Scripts/`）
- **相關 Sprint 項目**：Sprint 7（IT→VS 重構 Gate Sprint）E 類 7 批次工作項目（本 ADR Phase 8 產出建議清單）
- **相關文件**：
  - `projects/ProjectDR/project-status.md`（2026-04-22 條目：六題拍板記錄、結構災難背景）
  - `projects/ProjectDR/FILE_MAP.md`（Sprint 7 結構搬移後須大規模更新）

---

## Appendix A：21 模組 + Exploration 內部分配完整草案表

**本附表為 Sprint 7 E 類搬移的 source-of-truth**。每個現有檔案對應新路徑；executes 時以此表為準，偏離需回頭改 ADR。

### Village 根目錄檔案 → 新路徑（共約 70 檔）

| 現有路徑 | 新路徑 |
|---------|-------|
| `Village/AffinityConfigData.cs` | `Village/Affinity/Data/AffinityConfigData.cs` |
| `Village/AffinityManager.cs` | `Village/Affinity/Manager/AffinityManager.cs` |
| `Village/AreaIds.cs` | `Village/Navigation/Data/AreaIds.cs` |
| `Village/BackpackManager.cs` | `Village/Backpack/Manager/BackpackManager.cs` |
| `Village/BackpackSlot.cs` | `Village/Backpack/Data/BackpackSlot.cs` |
| `Village/BackpackSnapshot.cs` | `Village/Backpack/Data/BackpackSnapshot.cs` |
| `Village/CGSceneConfigData.cs` | `Village/CG/Data/CGSceneConfigData.cs` |
| `Village/CGUnlockManager.cs` | `Village/CG/Manager/CGUnlockManager.cs` |
| `Village/CharacterIdSnakeCaseMapper.cs` | `Village/CharacterUnlock/Manager/CharacterIdSnakeCaseMapper.cs` |
| `Village/CharacterIds.cs` | `Village/Navigation/Data/CharacterIds.cs` |
| `Village/CharacterIntroCGPlayer.cs` | `Village/CharacterIntro/Manager/CharacterIntroCGPlayer.cs` |
| `Village/CharacterIntroConfigData.cs` | `Village/CharacterIntro/Data/CharacterIntroConfigData.cs` |
| `Village/CharacterMenuData.cs` | `Village/CharacterInteraction/Data/CharacterMenuData.cs` |
| `Village/CharacterQuestionCountdownManager.cs` | `Village/CharacterQuestions/Manager/CharacterQuestionCountdownManager.cs` |
| `Village/CharacterQuestionsConfigData.cs` | `Village/CharacterQuestions/Data/CharacterQuestionsConfigData.cs` |
| `Village/CharacterQuestionsManager.cs` | `Village/CharacterQuestions/Manager/CharacterQuestionsManager.cs` |
| `Village/CharacterStaminaManager.cs` | `Village/CharacterStamina/Manager/CharacterStaminaManager.cs` |
| `Village/CharacterUnlockManager.cs` | `Village/CharacterUnlock/Manager/CharacterUnlockManager.cs` |
| `Village/CommissionInteractionPresenter.cs` | `Village/Alchemy/Presenter/CommissionInteractionPresenter.cs`（預設；若實引用顯示屬 Commission，改歸 `Village/Commission/Presenter/`） |
| `Village/CommissionManager.cs` | `Village/Commission/Manager/CommissionManager.cs` |
| `Village/CommissionRecipesConfigData.cs` | `Village/Commission/Data/CommissionRecipesConfigData.cs` |
| `Village/DialogueCooldownManager.cs` | `Village/Dialogue/Manager/DialogueCooldownManager.cs` |
| `Village/DialogueData.cs` | `Village/Dialogue/Data/DialogueData.cs` |
| `Village/DialogueManager.cs` | `Village/Dialogue/Manager/DialogueManager.cs` |
| `Village/ExplorationEntryManager.cs` | `Village/Navigation/Manager/ExplorationEntryManager.cs`（理由：從 Village 端發動探索的進入閘道）|
| `Village/FarmManager.cs` | `Village/Farm/Manager/FarmManager.cs` |
| `Village/FarmPlot.cs` | `Village/Farm/Data/FarmPlot.cs` |
| `Village/GiftManager.cs` | `Village/Gift/Manager/GiftManager.cs` |
| `Village/GreetingConfigData.cs` | `Village/Greeting/Data/GreetingConfigData.cs` |
| `Village/GreetingPresenter.cs` | `Village/Greeting/Presenter/GreetingPresenter.cs` |
| `Village/GuardFirstMeetDialogueConfigData.cs` | `Village/GuardReturn/Data/GuardFirstMeetDialogueConfigData.cs` |
| `Village/GuardReturnConfigData.cs` | `Village/GuardReturn/Data/GuardReturnConfigData.cs` |
| `Village/GuardReturnEventController.cs` | `Village/GuardReturn/Manager/GuardReturnEventController.cs` |
| `Village/HCGDialogueSetup.cs` | `Village/CharacterInteraction/Manager/HCGDialogueSetup.cs` |
| `Village/ICGPlayer.cs` | `Village/CG/Interface/ICGPlayer.cs` |
| `Village/ITimeProvider.cs` | `Village/TimeProvider/Interface/ITimeProvider.cs` |
| `Village/IdleChatConfigData.cs` | `Village/IdleChat/Data/IdleChatConfigData.cs` |
| `Village/IdleChatPresenter.cs` | `Village/IdleChat/Presenter/IdleChatPresenter.cs` |
| `Village/InitialResourceDispatcher.cs` | `Village/Progression/Manager/InitialResourceDispatcher.cs` |
| `Village/InitialResourcesConfigData.cs` | `Village/Progression/Data/InitialResourcesConfigData.cs` |
| `Village/ItemTypeResolver.cs` | `Village/ItemType/Manager/ItemTypeResolver.cs` |
| `Village/ItemTypes.cs` | `Village/ItemType/Data/ItemTypes.cs` |
| `Village/MainQuestConfigData.cs` | `Village/MainQuest/Data/MainQuestConfigData.cs` |
| `Village/MainQuestManager.cs` | `Village/MainQuest/Manager/MainQuestManager.cs` |
| `Village/NodeDialogueConfigData.cs` | `Village/NodeDialogue/Data/NodeDialogueConfigData.cs` |
| `Village/NodeDialogueController.cs` | `Village/NodeDialogue/Manager/NodeDialogueController.cs` |
| `Village/OpeningSequenceController.cs` | `Village/OpeningSequence/Manager/OpeningSequenceController.cs` |
| `Village/PlaceholderCGPlayer.cs` | `Village/CG/Manager/PlaceholderCGPlayer.cs` |
| `Village/PlayerQuestionsConfigData.cs` | `Village/CharacterQuestions/Data/PlayerQuestionsConfigData.cs` |
| `Village/PlayerQuestionsManager.cs` | `Village/CharacterQuestions/Manager/PlayerQuestionsManager.cs` |
| `Village/QuestData.cs` | `Village/MainQuest/Data/QuestData.cs`（或建 `Quest/`；預設併入 MainQuest；若後續另有 SideQuest 需求再拆）|
| `Village/QuestManager.cs` | `Village/MainQuest/Manager/QuestManager.cs`（同上）|
| `Village/RedDotManager.cs` | `Village/Progression/Manager/RedDotManager.cs` |
| `Village/ResourcesCGProvider.cs` | `Village/CG/Manager/ResourcesCGProvider.cs` |
| `Village/SeedData.cs` | `Village/Farm/Data/SeedData.cs` |
| `Village/StorageExpansionConfigData.cs` | `Village/Storage/Data/StorageExpansionConfigData.cs` |
| `Village/StorageExpansionManager.cs` | `Village/Storage/Manager/StorageExpansionManager.cs` |
| `Village/StorageManager.cs` | `Village/Storage/Manager/StorageManager.cs` |
| `Village/StorageTransferManager.cs` | `Village/Storage/Manager/StorageTransferManager.cs` |
| `Village/SystemTimeProvider.cs` | `Village/TimeProvider/Manager/SystemTimeProvider.cs` |
| `Village/VillageEntryPoint.cs` | `Village/Core/VillageEntryPoint.cs`（暫留原名；ADR-003 實施時拆為 VillageContext + IVillableInstaller）|
| `Village/VillageEvents.cs` | `Village/Navigation/Data/VillageEvents.cs`（理由：跨模組事件匯總，屬共用層；若後續事件拆分到各模組，可分散）|
| `Village/VillageNavigationManager.cs` | `Village/Navigation/Manager/VillageNavigationManager.cs` |
| `Village/VillageProgressionManager.cs` | `Village/Progression/Manager/VillageProgressionManager.cs` |

### Village/UI/ → 新路徑（共 21 檔）

已於 D4 章節列出，此處不重複。

### Village/Exploration/ → 新路徑（共約 32 檔 + Combat 14 檔）

| 現有路徑 | 新路徑 |
|---------|-------|
| `Exploration/ExplorationEntryPoint.cs` | `Exploration/Core/ExplorationEntryPoint.cs` |
| `Exploration/ExplorationEvents.cs` | `Exploration/Core/ExplorationEvents.cs` |
| `Exploration/MapData.cs` | `Exploration/Map/Data/MapData.cs` |
| `Exploration/MapDataJson.cs` | `Exploration/Map/Data/MapDataJson.cs` |
| `Exploration/GridMap.cs` | `Exploration/Map/Manager/GridMap.cs` |
| `Exploration/GridCellView.cs` | `Exploration/Map/View/GridCellView.cs` |
| `Exploration/CellType.cs` | `Exploration/Map/Data/CellType.cs` |
| `Exploration/ExplorationInputHandler.cs` | `Exploration/Movement/Manager/ExplorationInputHandler.cs` |
| `Exploration/ExplorationFreeInputHandler.cs` | `Exploration/Movement/Manager/ExplorationFreeInputHandler.cs` |
| `Exploration/ExplorationPlayerView.cs` | `Exploration/Movement/View/ExplorationPlayerView.cs` |
| `Exploration/ExplorationFreePlayerView.cs` | `Exploration/Movement/View/ExplorationFreePlayerView.cs` |
| `Exploration/PlayerFreeMovement.cs` | `Exploration/Movement/Manager/PlayerFreeMovement.cs` |
| `Exploration/PlayerGridMovement.cs` | `Exploration/Movement/Manager/PlayerGridMovement.cs` |
| `Exploration/MoveDirection.cs` | `Exploration/Movement/Data/MoveDirection.cs` |
| `Exploration/IMoveSpeedCalculator.cs` | `Exploration/MoveSpeed/Interface/IMoveSpeedCalculator.cs` |
| `Exploration/IMoveSpeedProvider.cs` | `Exploration/MoveSpeed/Interface/IMoveSpeedProvider.cs` |
| `Exploration/FixedMoveSpeedCalculator.cs` | `Exploration/MoveSpeed/Manager/FixedMoveSpeedCalculator.cs` |
| `Exploration/FixedMoveSpeedProvider.cs` | `Exploration/MoveSpeed/Manager/FixedMoveSpeedProvider.cs` |
| `Exploration/SpdMoveSpeedCalculator.cs` | `Exploration/MoveSpeed/Manager/SpdMoveSpeedCalculator.cs` |
| `Exploration/SpdMoveSpeedProvider.cs` | `Exploration/MoveSpeed/Manager/SpdMoveSpeedProvider.cs` |
| `Exploration/ExplorationCameraFollow.cs` | `Exploration/Camera/View/ExplorationCameraFollow.cs` |
| `Exploration/ExplorationMapView.cs` | `Exploration/Camera/View/ExplorationMapView.cs` |
| `Exploration/CollectionManager.cs` | `Exploration/Collection/Manager/CollectionManager.cs` |
| `Exploration/CollectiblePointData.cs` | `Exploration/Collection/Data/CollectiblePointData.cs` |
| `Exploration/CollectiblePointState.cs` | `Exploration/Collection/Data/CollectiblePointState.cs` |
| `Exploration/CollectionGatheringView.cs` | `Exploration/Collection/View/CollectionGatheringView.cs` |
| `Exploration/CollectionItemPanelView.cs` | `Exploration/Collection/View/CollectionItemPanelView.cs` |
| `Exploration/CollectionInteractionHintView.cs` | `Exploration/Collection/View/CollectionInteractionHintView.cs` |
| `Exploration/CollectiblePointIndicatorView.cs` | `Exploration/Collection/View/CollectiblePointIndicatorView.cs` |
| `Exploration/EvacuationManager.cs` | `Exploration/Evacuation/Manager/EvacuationManager.cs` |
| `Exploration/EvacuationView.cs` | `Exploration/Evacuation/View/EvacuationView.cs` |
| `Exploration/IMonsterPositionProvider.cs` | `Exploration/Combat/Interface/IMonsterPositionProvider.cs` |
| `Exploration/Combat/CombatConfigData.cs` | `Exploration/Combat/Data/CombatConfigData.cs` |
| `Exploration/Combat/CombatEvents.cs` | `Exploration/Combat/Data/CombatEvents.cs` |
| `Exploration/Combat/CombatInputHandler.cs` | `Exploration/Combat/Manager/CombatInputHandler.cs` |
| `Exploration/Combat/CombatManager.cs` | `Exploration/Combat/Manager/CombatManager.cs` |
| `Exploration/Combat/DamageCalculator.cs` | `Exploration/Combat/Manager/DamageCalculator.cs` |
| `Exploration/Combat/DamageNumberView.cs` | `Exploration/Combat/View/DamageNumberView.cs` |
| `Exploration/Combat/DeathManager.cs` | `Exploration/Combat/Manager/DeathManager.cs` |
| `Exploration/Combat/DeathView.cs` | `Exploration/Combat/View/DeathView.cs` |
| `Exploration/Combat/MonsterConfigData.cs` | `Exploration/Combat/Data/MonsterConfigData.cs` |
| `Exploration/Combat/MonsterManager.cs` | `Exploration/Combat/Manager/MonsterManager.cs` |
| `Exploration/Combat/MonsterState.cs` | `Exploration/Combat/Data/MonsterState.cs` |
| `Exploration/Combat/MonsterView.cs` | `Exploration/Combat/View/MonsterView.cs` |
| `Exploration/Combat/PlayerCombatStats.cs` | `Exploration/Combat/Data/PlayerCombatStats.cs` |
| `Exploration/Combat/PlayerCombatView.cs` | `Exploration/Combat/View/PlayerCombatView.cs` |
| `Exploration/Combat/PlayerContactDetector.cs` | `Exploration/Combat/Manager/PlayerContactDetector.cs` |
| `Exploration/Combat/SpdMoveSpeedCalculator.cs`（若存在於此處為誤置）| 已在 MoveSpeed；勿重複 |
| `Exploration/Combat/SwordAttack.cs` | `Exploration/Combat/Manager/SwordAttack.cs` |
| `Exploration/Combat/AimIndicatorView.cs` | `Exploration/Combat/View/AimIndicatorView.cs` |

### 需刪除的遺留目錄

- `Village/UI/`（拆空後刪除）
- `Village/Mvp/`（含 `Mvp/UI/`，整棵刪除）

---

## Appendix B：Sprint 7 E 類工作項目建議清單（7 批次）

本清單依 Q5=c 交叉批次原則產出；每批次同步處理「E 類結構搬移」+「B 類 ConfigData 改 IGameData（ADR-002 [A] 區塊對應條目）」。工時為粗估，執行時可細調。

詳見 Phase 8 § Sprint 7 E 類工作項目清單（下方「回報要求」章節）。

---
