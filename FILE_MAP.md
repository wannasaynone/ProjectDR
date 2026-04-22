# ProjectDR - 文件地圖

## 專案管理

| 檔案 | 用途 |
|------|------|
| `project-status.md` | 專案狀態檔（當前階段、階段焦點、活躍 Sprint、待決事項、已知阻塞） |
| `project-tbd.md` | 專案級 TBD 池（跨 Sprint 累積的製作人待拍板 placeholder 清單，2026-04-20 建立；類別：intro/node/quest/resource/recipe/storage/balance） |
| `tech-debt.md` | 技術債登記（Sprint 7 D5 首次建立；當前 11 條追蹤；含 Sprint 8 候選項：A8 消費點改造、資料層 IGameData 重構、KGC API Key 硬編等） |

## ADR 技術決策記錄

| 檔案 | 用途 |
|------|------|
| `adrs/ADR-001-data-governance-contract.md` | ADR-001：資料治理契約（IGameData 介面 + GameStaticDataManager 註冊；Accepted） |
| `adrs/ADR-002-it-stage-exemption-exit.md` | ADR-002：IT 階段豁免退出 Gate（[A][B][C][D] 四區塊清單，退出條件：全 ✅ + DEV-DATA-INTAKE-REVIEW PASS；2026-04-22 加註 ADR-004 路徑引用） |
| `adrs/ADR-003-village-composition-root-contract.md` | ADR-003：Village Composition Root 契約（IVillageInstaller + VillageContext + VillageEntryPoint 瘦身三元契約；Install 順序鐵律；2026-04-22 Accepted） |
| `adrs/ADR-004-script-organization-structure-contract.md` | ADR-004：Script 組織結構契約（21 模組 × 5 型別層、Namespace 跟資料夾、新檔決策樹、70+ 檔案分配表；2026-04-22 Accepted） |
| `adrs/index.md` | ADR 總表索引（列出所有 ADR 狀態與主題分類；2026-04-22 建立） |
| `adrs/tr-registry.yaml` | 技術需求登記表（TR-ID 與 ADR 綁定；2026-04-22 新增 TR-arch-001~004） |

## 專案技術文件（tech/）

| 檔案 | 用途 |
|------|------|
| `tech/control-manifest.md` | Control Manifest — 從 4 條 Accepted ADR 抽取的平面化規則清單（必做 22 / 禁做 20 / 護欄 4；Manifest Version 2026-04-22；dev-agent 實作前必讀） |
| `tech/data-governance-workflow-patches.md` | 資料治理工作流補丁文件（IGameData 規格補丁記錄） |
| `tech/sprint-7-workflow-retro.md` | Sprint 7 新 /development-flow Phase 2 首次實測 Retro（6 項觀察、6 項流程修補建議清單；2026-04-22 建立）|
| `tech/google-sheet-export-tool-spec.md` | Google Sheet Export Tool 使用規範（KGC 工具串接、13 分頁對應表、首次設定 + 日常操作 + 疑難排解 + 已知限制；Sprint 7 A6-4；對應 ADR-002 [C02]；2026-04-22 建立） |

## GDD 設計文件

| 檔案 | 用途 |
|------|------|
| `gdd/game-concept.md` | 遊戲概念文件（專案最高層級總覽） |
| `gdd/core-definition.md` | 核心定義文件（核心循環、核心樂趣、驗證標準、目標受眾、開發環境） |
| `gdd/world-setting.md` | 世界觀設定文件（世界觀背景、村莊與森林設定、玩家角色；探索系統規則概述，詳細規則見 exploration-system.md） |
| `gdd/exploration-system.md` | 探索系統規格書（完整 55 條規則：地圖、移動、物品箱、背包倉庫、數值、戰鬥、魔物、死亡、撤離） |
| `gdd/characters.md` | 角色設定文件（角色總覽：背景、個性、對應村莊功能） |
| `gdd/character-interaction.md` | 角色互動系統（互動流程、畫面佈局、打字機效果、功能選單、懸浮覆蓋） |
| `gdd/village-economy.md` | 村莊經濟系統（物品分類、農田、通用製作、贈禮效果、角色功能對應、物品流向圖） |
| `gdd/base-management.md` | 基地經營系統（NPC 導向架構統一四項選單、委託循環、擴建循環、紅點四層、系統接口；v2.0 委託制定案重寫） |
| `gdd/commission-system.md` | 委託系統規格書（委託型角色定位、完整流程、格子式工作台、單物品配方、多格子並行、工作中狀態、紅點四層、領取流程、系統接口） |
| `gdd/main-quest-system.md` | 主線任務系統規格書（機制引導定位、事件序列+觸發條件結構、任務按鈕位於所有角色功能選單、承接流程、主線完成≠破關、系統接口；v1.1 新增前期主線任務序列 T0~T4 同時扮演角色解鎖觸發器） |
| `gdd/character-unlock-system.md` | 角色解鎖系統規格書（三節點結構、VN 式選項解鎖、守衛歸來事件身分誤會純劇情演出、解鎖=功能+初始資源綁定、登場 CG + 短劇情規格、節點觸發主線任務完成 QC-D、系統接口） |
| `gdd/storage-expansion.md` | 倉庫擴建機制規格書（格子制+堆疊結構與背包同、100 初始/每次+50、擴建流程、擴建狀態機、擴建物資 TBD、系統接口） |
| `gdd/character-content-template.md` | 單一角色內容量規格模板（好感度階段、對話量、CG/HCG 數量、立繪差分、喜好清單、量產估算） |
| `gdd/dialogue-writing-sop.md` | 對話撰寫 SOP（製作人撰寫單一角色完整對話內容的線性 checklist，按等級 1～7 逐步推進） |

## 敘事文件（narrative）

| 檔案 | 用途 |
|------|------|
| `gdd/narrative/village-chief-wife/character-spec.md` | 村長夫人角色設定 |
| `gdd/narrative/guard/character-spec.md` | 守衛角色設定 |
| `gdd/narrative/witch/character-spec.md` | 魔女（魔藥學家）角色設定 |
| `gdd/narrative/farm-girl/character-spec.md` | 農女角色設定 |

## 正式遊戲程式碼（Assets/Game/Scripts）

### Assembly Definition
| 檔案 | 用途 |
|------|------|
| `Assets/Game/Scripts/Game.asmdef` | 正式遊戲 Runtime Assembly（namespace: ProjectDR） |
| `Assets/Game/Scripts/AssemblyInfo.cs` | 程式集資訊：[assembly: InternalsVisibleTo("Game.Tests")]，允許測試 Assembly 存取 VillageContext 的 internal set 欄位（E4 新增）|

### Village 模組骨架（ADR-004 新建資料夾，E1 批次，2026-04-22）

21 個功能模組資料夾（+ Navigation/Shared/Core 共用資料夾）已建立，各含 5 層子資料夾（Manager/View/Data/Presenter/Interface）。
- **建立期（E1）**：空子層含 `.keep` 佔位檔
- **運維期（dev-log-9，2026-04-22）**：已清除 100 個只含 .keep 的空子層 .keep 檔；剩餘 58 個 .keep 均在有 .cs 的目錄中（保留）
路徑：`Assets/Game/Scripts/Village/<Module>/<Layer>/`

### Village Core 契約層（ADR-003 B2/B3，2026-04-22 新建）
| 檔案 | 用途 |
|------|------|
| `Assets/Game/Scripts/Village/Core/Interface/IVillageInstaller.cs` | Village 場景組裝契約介面（Install(VillageContext) + Uninstall()；ADR-003 D1） |
| `Assets/Game/Scripts/Village/Core/Interface/IVillageTickable.cs` | 選擇性 Tick 介面，需要每 Update 推進的 Installer 實作（ADR-003 D1） |
| `Assets/Game/Scripts/Village/Core/Interface/IAffinityQuery.cs` | 好感度唯讀查詢介面（GetLevel/IsThresholdReached；ADR-003 D6） |
| `Assets/Game/Scripts/Village/Core/Interface/IVillageProgressionQuery.cs` | 村莊進度唯讀查詢介面（IsCharacterUnlocked/IsExplorationUnlocked；ADR-003 D6） |
| `Assets/Game/Scripts/Village/Core/Interface/IStorageQuery.cs` | 倉庫唯讀查詢介面（GetItemCount/HasItem；ADR-003 D6） |
| `Assets/Game/Scripts/Village/Core/Interface/IBackpackQuery.cs` | 背包唯讀查詢介面（GetItemCount/HasItem/IsFull；ADR-003 D6） |
| `Assets/Game/Scripts/Village/Core/Data/GameDataQuery.cs` | IGameData 查詢委派定義（隔離 GameStaticDataManager 靜態依賴；ADR-003 D2.1） |
| `Assets/Game/Scripts/Village/Core/Data/VillageContext.cs` | Village 跨 Installer 共用服務容器（9 欄位，建構器注入；ADR-003 D2，Appendix A） |

### 村莊模組（Assets/Game/Scripts/Village）
| 檔案 | 用途 |
|------|------|
| `Assets/Game/Scripts/Village/Navigation/Data/AreaIds.cs` | 村莊區域 ID 常數定義（Hub、Storage、Exploration、Alchemy、Farm）— ns: ProjectDR.Village.Navigation |
| `Assets/Game/Scripts/Village/Navigation/Data/CharacterIds.cs` | 角色 ID 常數定義（VillageChiefWife、Guard、Witch、FarmGirl）— ns: ProjectDR.Village.Navigation |
| `Assets/Game/Scripts/Village/Dialogue/Data/DialogueData.cs` | 對話資料結構（DialogueData 對話行 + DialogueChoice VN 選項）|
| `Assets/Game/Scripts/Village/Dialogue/Manager/DialogueManager.cs` | 對話播放狀態管理器（純邏輯，管理對話行推進、VN 選項分支、AppendLines 動態延伸）|
| `Assets/Game/Scripts/Village/Dialogue/Data/NodeDialogueConfigData.cs` | 節點劇情對話配置 JSON DTO（NodeDialogueConfigData/NodeDialogueLineData）與不可變配置物件（NodeDialogueConfig/NodeDialogueData/NodeDialogueChoiceOption）+ NodeDialogueLineTypes 常數 |
| `Assets/Game/Scripts/Village/CharacterInteraction/Data/CharacterMenuData.cs` | 角色功能選單資料（角色 ID、顯示名稱、對話、功能清單）|
| `Assets/Game/Scripts/Village/Navigation/Data/VillageEvents.cs` | 村莊系統事件類別定義（AreaUnlockedEvent、DialogueStartedEvent、DialogueCompletedEvent、FarmPlotPlantedEvent、FarmPlotHarvestedEvent、AffinityChangedEvent、AffinityThresholdReachedEvent、CGUnlockedEvent 等 50+ 個事件）— ns: ProjectDR.Village.Navigation |
| `Assets/Game/Scripts/Village/Storage/Manager/StorageManager.cs` | 倉庫物品庫存管理器（格子+堆疊制，初始 100 格，支援 ExpandCapacity；實作 IStorageQuery）— ns: ProjectDR.Village.Storage（E4 搬移）|
| `Assets/Game/Scripts/Village/Backpack/Data/BackpackSlot.cs` | 背包格子資料結構（struct）— ns: ProjectDR.Village.Backpack（E4 搬移）|
| `Assets/Game/Scripts/Village/Backpack/Data/BackpackSnapshot.cs` | 背包快照（不可變，用於死亡回溯）— ns: ProjectDR.Village.Backpack（E4 搬移）|
| `Assets/Game/Scripts/Village/Backpack/Manager/BackpackManager.cs` | 格子制背包管理器（容量限制、堆疊邏輯、快照/回溯；實作 IBackpackQuery）— ns: ProjectDR.Village.Backpack（E4 搬移）|
| `Assets/Game/Scripts/Village/Storage/Manager/StorageTransferManager.cs` | 背包與倉庫雙向物品轉移管理器 — ns: ProjectDR.Village.Storage（E4 搬移）|
| `Assets/Game/Scripts/Village/QuestData.cs` | 任務資料結構 |
| `Assets/Game/Scripts/Village/QuestManager.cs` | 任務管理器 |
| `Assets/Game/Scripts/Village/VillageProgressionManager.cs` | 村莊解鎖進度管理器 |
| `Assets/Game/Scripts/Village/Storage/Data/StorageExpansionConfigData.cs` | 倉庫擴建配置 JSON DTO（StorageExpansionConfigData/StorageExpansionStageData）與不可變配置物件（StorageExpansionConfig/StorageExpansionStage）；A16 IGameData 改造（StorageExpansionStageData：level 作為 ID）— ns: ProjectDR.Village.Storage（E4 搬移）|
| `Assets/Game/Scripts/Village/Storage/Manager/StorageExpansionManager.cs` | 倉庫擴建狀態機與流程管理器（Idle/InProgress/Completed 狀態機、Tick 倒數、物資扣除先背包後倉庫、事件 StorageExpansionStarted/Completed/CapacityChanged）— ns: ProjectDR.Village.Storage（E4 搬移）|
| `Assets/Game/Scripts/Village/Progression/Data/InitialResourcesConfigData.cs` | 初始資源發放配置 JSON DTO（InitialResourcesConfigData/InitialResourceGrantData）與不可變配置物件（InitialResourcesConfig/InitialResourceGrant）+ InitialResourcesTriggerIds 常數；A11 IGameData 改造（InitialResourceGrantData：int id + Key = grant_id）；移除廢棄常數（UnlockFarmGirl/UnlockWitch/GuardReturnEvent）— ns: ProjectDR.Village.Progression（E5 搬移）|
| `Assets/Game/Scripts/Village/Progression/Manager/InitialResourceDispatcher.cs` | 初始資源發放器（IInitialResourceDispatcher 實作，先背包後倉庫邏輯）— ns: ProjectDR.Village.Progression（E5 搬移）|
| `Assets/Game/Scripts/Village/MainQuest/Data/MainQuestConfigData.cs` | 前期主線任務配置 JSON DTO（MainQuestConfigData/MainQuestConfigEntry）與不可變配置物件（MainQuestConfig/MainQuestInfo）+ MainQuestCompletionTypes 常數；A12 IGameData 改造（MainQuestConfigEntry：int id + Key = quest_id）— ns: ProjectDR.Village.MainQuest（E5 搬移）|
| `Assets/Game/Scripts/Village/MainQuest/Manager/MainQuestManager.cs` | 前期主線任務管理器（Locked/Available/InProgress/Completed 狀態機、NotifyCompletionSignal、TryAutoCompleteFirstAutoQuest、事件 MainQuestAvailable/Started/Completed）— ns: ProjectDR.Village.MainQuest（E5 搬移）|
| `Assets/Game/Scripts/Village/CharacterUnlock/Manager/CharacterUnlockManager.cs` | 角色 Hub 按鈕解鎖管理器（VN 選項監聽、守衛事件監聽、探索功能解鎖、grant 派發）+ NodeDialogueBranchIds + IInitialResourceDispatcher 介面 — ns: ProjectDR.Village.CharacterUnlock（E5 搬移）|
| `Assets/Game/Scripts/Village/Navigation/Manager/VillageNavigationManager.cs` | 村莊導航管理器 — ns: ProjectDR.Village.Navigation |
| `Assets/Game/Scripts/Village/Navigation/Manager/ExplorationEntryManager.cs` | 探索進入管理器（監聽 ExplorationCompletedEvent 自動結束探索，支援 Dispose；V6 B10 新增 IExplorationDepartureInterceptor 攔截器介面供守衛歸來事件攔截首次探索）— ns: ProjectDR.Village.Navigation |
| `Assets/Game/Scripts/Village/Core/Manager/VillageEntryPoint.cs` | 村莊場景進入點（MonoBehaviour，partial class 主檔；組裝所有模組，585 行；E7→dev-log-9 瘦身）— ns: ProjectDR.Village.Core |
| `Assets/Game/Scripts/Village/Core/Manager/VillageEntryPointFunctionPrefabs.cs` | VillageEntryPoint partial — Function Prefab 注冊（RegisterFunctionPrefabs / RegisterCraftWorkbenchForFunction / ExplorationDepartureInterceptorAdapter；dev-log-9 新建，147 行）— ns: ProjectDR.Village.Core |
| `Assets/Game/Scripts/Village/Core/Manager/VillageEntryPointInstallers.cs` | VillageEntryPoint partial — RunInstallers() 完整方法（6 個 Installer 序列 + JSON 解析；dev-log-9 新建，187 行）— ns: ProjectDR.Village.Core |
| `Assets/Game/Scripts/Village/ItemType/Data/ItemTypes.cs` | 物品分類常數定義（Seed、Ingredient、Food、Potion、Material、Other）— ns: ProjectDR.Village.ItemType |
| `Assets/Game/Scripts/Village/ItemType/Manager/ItemTypeResolver.cs` | 物品分類解析器（Register/GetItemType/IsType/GetItemsByType）— ns: ProjectDR.Village.ItemType |
| `Assets/Game/Scripts/Village/Farm/Data/SeedData.cs` | 種子資料結構（SeedItemId、HarvestItemId、GrowthDurationSeconds）— ns: ProjectDR.Village.Farm（E4 搬移）|
| `Assets/Game/Scripts/Village/Farm/Data/FarmPlot.cs` | 農田格子 readonly struct（Empty、IsEmpty、IsReadyToHarvest、GetRemainingSeconds）— ns: ProjectDR.Village.Farm（E4 搬移）|
| `Assets/Game/Scripts/Village/TimeProvider/Interface/ITimeProvider.cs` | 時間提供者介面（GetCurrentTimestampUtc），供 FarmManager 取得可替換的時間來源 — ns: ProjectDR.Village.TimeProvider |
| `Assets/Game/Scripts/Village/TimeProvider/Manager/SystemTimeProvider.cs` | 系統時間提供者（ITimeProvider 實作，回傳 DateTimeOffset.UtcNow）— ns: ProjectDR.Village.TimeProvider |
| `Assets/Game/Scripts/Village/Farm/Manager/FarmManager.cs` | 農田管理器（Plant/Harvest/HarvestAll）與相關 enum/Result 類別（PlantError、PlantResult、HarvestError、HarvestResult、HarvestAllResult）— ns: ProjectDR.Village.Farm（E4 搬移）|
| `Assets/Game/Scripts/Village/Commission/Data/CommissionRecipesConfigData.cs` | 委託配方配置 JSON DTO（CommissionRecipesConfigData/CommissionRecipeEntry）與不可變配置物件（CommissionRecipesConfig/CommissionRecipeInfo），提供 GetRecipe/GetRecipesByCharacter/GetRecipeByInputItem/CanCharacterProcessItem/GetWorkbenchSlotCount；A06 IGameData 改造（CommissionRecipeEntry：int id + Key = recipe_id）— ns: ProjectDR.Village.Commission（E5 搬移）|
| `Assets/Game/Scripts/Village/Commission/Manager/CommissionManager.cs` | 委託系統管理器（StartCommission/ClaimCommission/GetSlot/GetSlots/Tick）與相關型別（StartCommissionResult/Error、ClaimCommissionResult/Error、CommissionSlotState、CommissionSlotInfo）；B5 Sprint 4 新增 — ns: ProjectDR.Village.Commission（E5 搬移）|
| `Assets/Game/Scripts/Village/CommissionInteractionPresenter.cs` | 委託互動表示器（純邏輯 IDisposable，連接 CommissionManager 事件與 CharacterInteractionView 委託狀態；B12 Sprint 4） |
| `Assets/Game/Scripts/Village/RedDotManager.cs` | 紅點 4 層管理器（L1 委託完成 / L2 角色發問 / L3 新任務 / L4 主線事件，優先序 L1>L4>L3>L2；訂閱 Commission/Affinity/MainQuest/CharacterUnlock 事件；發布 RedDotUpdatedEvent）+ HubRedDotInfo struct；B7 Sprint 4 |
| `Assets/Game/Scripts/Village/CharacterIntro/Data/CharacterIntroConfigData.cs` | 角色登場 CG + 短劇情配置 JSON DTO（CharacterIntroConfigData/CharacterIntroData/CharacterIntroLineData）與不可變配置物件（CharacterIntroConfig/CharacterIntroInfo）+ CharacterIntroLineTypes 常數；B9 Sprint 4；A03 IGameData 改造（2026-04-22）|
| `Assets/Game/Scripts/Village/CG/Interface/ICGPlayer.cs` | 登場 CG 播放介面（PlayIntroCG，B9 預留，B13 實作真正 CG 播放）|
| `Assets/Game/Scripts/Village/CG/Manager/PlaceholderCGPlayer.cs` | ICGPlayer 的 IT 階段 placeholder 實作（立即完成、發布 CGPlaybackStartedEvent/CompletedEvent 供 UI 預留偵聽）；B9 Sprint 4 |
| `Assets/Game/Scripts/Village/Dialogue/Manager/NodeDialogueController.cs` | 節點 0/1/2 劇情對話播放控制器（協調 DialogueManager + NodeDialogueConfig，播放 intro_lines → present choices → response → 完成；發布 NodeDialogueStarted/Completed）；B9 Sprint 4 |
| `Assets/Game/Scripts/Village/OpeningSequenceController.cs` | 開場劇情演出控制器（協調 ICGPlayer + NodeDialogueController，播放村長夫人登場 CG → 節點 0 對話；發布 OpeningSequenceStarted/Completed）；B9 Sprint 4 |
| `Assets/Game/Scripts/Village/Guard/Manager/GuardReturnEventController.cs` | 守衛歸來事件控制器（一次性觸發，協調 ICGPlayer；發布 GuardReturnEventStarted/Completed；完成後由 CharacterUnlockManager 訂閱處理解鎖+贈劍）；B10 Sprint 4；E6 搬移至 Guard/Manager/，ns: ProjectDR.Village.Guard |
<!-- GuardFirstMeetDialogueConfigData.cs 已刪除（A08 併入 NodeDialogueConfig，2026-04-22）；4 筆對白以 node_id="guard_first_meet" 併入 node-dialogue-config.json（id 32~35） -->
| `Assets/Game/Scripts/Village/Affinity/Manager/AffinityManager.cs` | 好感度管理器（GetAffinity/AddAffinity/GetThresholds/GetReachedThresholds，門檻達成事件發布；實作 IAffinityQuery）— ns: ProjectDR.Village.Affinity（E4 搬移 + IAffinityQuery 介面實作）|
| `Assets/Game/Scripts/Village/Affinity/Data/AffinityConfigData.cs` | 好感度配置 JSON DTO（AffinityConfigData、AffinityCharacterConfigData）與不可變配置物件（AffinityConfig）— ns: ProjectDR.Village.Affinity（E4 搬移）|
| `Assets/Game/Scripts/Village/Gift/Manager/GiftManager.cs` | 送禮業務邏輯管理器（GiveGift：扣物品先背包後倉庫→加好感度），含 GiftResult、GiftError — ns: ProjectDR.Village.Gift（E4 搬移）|
| `Assets/Game/Scripts/Village/CG/Data/CGSceneConfigData.cs` | CG 場景配置 JSON DTO（CGSceneConfigEntry、CGSceneConfigData）與不可變配置物件（CGSceneInfo、CGSceneConfig）；A02 IGameData 改造（2026-04-22）|
| `Assets/Game/Scripts/Village/CG/Manager/CGUnlockManager.cs` | CG 解鎖管理器（監聽 AffinityThresholdReachedEvent、session 記憶體 HashSet、Dispose 模式）|
| `Assets/Game/Scripts/Village/CG/Manager/ResourcesCGProvider.cs` | IT 階段 CG 圖片載入器（ICGProvider 實作，Resources/CG/ 載入或生成 placeholder）|
| `Assets/Game/Scripts/Village/CG/Manager/HCGDialogueSetup.cs` | HCG 劇情播放整合層（KGC DialogueManager + GameStaticDataManager，IT 硬編碼 4 角色對話）|
| `Assets/Game/Scripts/Village/CG/Manager/CharacterIntroCGPlayer.cs` | ICGPlayer 真實實作（B13）：從 CharacterIntroConfig 取 intro → Resources.Load CG Sprite → Instantiate CharacterIntroCGView → 播放完成後銷毀 View + 發布 CompletedEvent；only-once 旗標由 VillageEntryPoint 以 session HashSet 管理 |
| `Assets/Game/Scripts/Village/Core/Manager/CGInstaller.cs` | CG 功能域 Installer（ADR-003 B4d）：建構 CGSceneConfig + CGUnlockManager、訂閱 CGUnlockedEvent；Install/Uninstall 對稱 |
| `Assets/Game/Scripts/Village/Core/Manager/DialogueFlowInstaller.cs` | 對話流功能域 Installer（ADR-003 B4f）：建構 CharacterQuestionsConfig/Manager/CountdownManager、GreetingConfig/Presenter、IdleChatConfig/Presenter、DialogueCooldownManager、CharacterStaminaManager；訂閱 CommissionStartedEvent/CommissionClaimedEvent/CharacterUnlockedEvent；實作 IVillageInstaller + IVillageTickable（E3 建立，E4 修正：移除 AffinityManager 建構子注入，改由 ctx.AffinityReadOnly cast 取得）|
| `Assets/Game/Scripts/Village/Core/Manager/CoreStorageInstaller.cs` | 核心倉庫功能域 Installer（ADR-003 B4a / E4）：建構 BackpackManager + StorageManager + StorageTransferManager，填入 ctx.BackpackReadOnly / ctx.StorageReadOnly；無事件訂閱；安裝順序 #1 |
| `Assets/Game/Scripts/Village/Core/Manager/AffinityInstaller.cs` | 好感度功能域 Installer（ADR-003 B4c / E4）：建構 AffinityManager（實作 IAffinityQuery），填入 ctx.AffinityReadOnly；依賴 ctx.BackpackReadOnly / ctx.StorageReadOnly 就位；無事件訂閱 |
| `Assets/Game/Scripts/Village/Core/Manager/ProgressionInstaller.cs` | 村莊進度域 Installer（ADR-003 B4b / E5）：建構 MainQuestManager + CharacterUnlockManager + VillageProgressionManager + RedDotManager；訂閱 MainQuestCompletedEvent 推進 VillageProgressionManager 區域解鎖；填入 ctx.VillageProgressionReadOnly（VillageProgressionQueryAdapter）；安裝順序 #2 |
| `Assets/Game/Scripts/Village/Core/Manager/CommissionInstaller.cs` | 委託與倉庫擴建域 Installer（ADR-003 B4e / E5）：建構 CommissionManager + StorageExpansionManager；實作 IVillageTickable 驅動兩個 Manager 的 Tick；依賴 ctx.TimeProvider 就位 |
| `Assets/Game/Scripts/Village/CharacterQuestions/Data/PlayerQuestionsConfigData.cs` | 玩家發問配置 JSON DTO（PlayerQuestionsConfigData/PlayerQuestionData）與不可變配置物件（PlayerQuestionsConfig/PlayerQuestionInfo）；API：GetQuestionsForCharacter / GetUnlockedQuestions(charId, stage) / GetQuestion(id)；B14 Sprint 4；EXEMPT: ADR-002 A15；E6 路徑更新，ns: ProjectDR.Village.CharacterQuestions |
| `Assets/Game/Scripts/Village/CharacterIdSnakeCaseMapper.cs` | Sprint 5：JSON snake_case → CharacterIds 常數 (PascalCase) 映射器（內部使用） |
| `Assets/Game/Scripts/Village/CharacterQuestions/Manager/CharacterQuestionCountdownManager.cs` | Sprint 5 B1 → E3 搬移：角色發問倒數管理器（純邏輯，60s 倒數/角色、工作中暫停、紅點上限 1、發布 CharacterQuestionCountdownReadyEvent）— ns: ProjectDR.Village.CharacterQuestions |
| `Assets/Game/Scripts/Village/CharacterQuestions/Data/CharacterQuestionsConfigData.cs` | Sprint 5 B4 → E3 搬移：角色發問 280 題配置 JSON DTO 與不可變 Config（依 character/level 索引、個性 +0/+2/+5/+10 增量對應、snake_case/PascalCase 雙映射）；A04 IGameData 改造（CharacterQuestionEntryData：int id + Key = question_id）— ns: ProjectDR.Village.CharacterQuestions |
| `Assets/Game/Scripts/Village/CharacterQuestions/Manager/CharacterQuestionsManager.cs` | Sprint 5 B5 → E3 搬移：角色發問純邏輯（抽未看過題、標記已看、SubmitAnswer 扣好感度、發布 Asked/Answered 事件）— ns: ProjectDR.Village.CharacterQuestions |
| `Assets/Game/Scripts/Village/Dialogue/Manager/DialogueCooldownManager.cs` | Sprint 5 B10：玩家發問 CD 管理器（純邏輯，60s 基礎、工作中 ×2 規則層倍率、發布 Started/Completed 事件）— ns: ProjectDR.Village.Dialogue |
| `Assets/Game/Scripts/Village/IdleChat/Data/IdleChatConfigData.cs` | Sprint 5 B12 → E3 搬移：閒聊問題池配置（4 角色 × 20 題 × 3 回答）；A10 IGameData 改造（IdleChatTopicData：int id + Key = topic_id）— ns: ProjectDR.Village.IdleChat |
| `Assets/Game/Scripts/Village/IdleChat/Presenter/IdleChatPresenter.cs` | Sprint 5 B12 → E3 搬移：閒聊觸發純邏輯（隨機題 + 隨機回答，不影響好感度/不累計已看、發布 IdleChatTriggeredEvent）— ns: ProjectDR.Village.IdleChat |
| `Assets/Game/Scripts/Village/PlayerQuestionsManager.cs` | Sprint 5 B11：玩家發問純邏輯（剩餘題目規則：≥4 抽 4 / 1~3 顯剩餘 / 0 IdleChatFallback，MarkSeen 標記）— 待 E6 搬移 |
| `Assets/Game/Scripts/Village/CharacterStamina/Manager/CharacterStaminaManager.cs` | Sprint 5 B13 → E3 搬移：角色體力管理器（純邏輯，扣/恢復，placeholder Max=10、每次發問扣 1）— ns: ProjectDR.Village.CharacterStamina |
| `Assets/Game/Scripts/Village/Greeting/Data/GreetingConfigData.cs` | Sprint 5 B15 → E3 搬移：招呼語配置（4 角色 × 7 級 × 10 句 = 280 句）；A07 IGameData 改造（GreetingEntryData：int id + Key = greeting_id）— ns: ProjectDR.Village.Greeting |
| `Assets/Game/Scripts/Village/Greeting/Presenter/GreetingPresenter.cs` | Sprint 5 B16 → E3 搬移：招呼語純邏輯（進入 Normal 狀態時抽句、L1/L4 紅點壓制、L2/L3 仍播，發布 GreetingPlayedEvent）— ns: ProjectDR.Village.Greeting |

### 村莊 UI（各模組 View 層）

注意：`Village/UI/` 頂層目錄已於 E6（2026-04-22）刪除，`Village/Mvp/` 空目錄同步刪除。所有 View 已分散至各模組 View/ 子層。

| 檔案 | 用途 |
|------|------|
| `Assets/Game/Scripts/Village/CG/View/CharacterIntroCGView.cs` | 登場 CG + 短劇情播放 View（全螢幕 overlay，上半 CG / 下半打字機對話框 / 點擊全螢幕推進）；B13 Sprint 4；E6 搬移至 CG/View/ |
| `Assets/Game/Scripts/Village/CharacterInteraction/View/CharacterInteractionView.cs` | 角色互動畫面（立繪區、對話區、功能選單、overlay 容器）；E6 搬移至 CharacterInteraction/View/ |
| `Assets/Game/Scripts/Village/CharacterQuestions/View/PlayerQuestionsView.cs` | 玩家主動發問 overlay View（右側 w=1600 / 題目清單依好感度解鎖 / 點題目→打字機播回答→返回清單）；Sprint 5 |
| `Assets/Game/Scripts/Village/UI/CharacterQuestionsView.cs` | Sprint 5 B6：角色發問 overlay View（打字機 Prompt + 四選項 UI，待 E7 搬移）|
| `Assets/Game/Scripts/Village/Shared/View/ViewBase.cs` | UGUI View 抽象基類（Show/Hide 管理）— ns: ProjectDR.Village.Shared |
| `Assets/Game/Scripts/Village/Shared/View/ViewController.cs` | 管理 View 顯示切換的控制器（排他式，無歷史紀錄）— ns: ProjectDR.Village.Shared |
| `Assets/Game/Scripts/Village/Shared/View/ViewStackController.cs` | 支援 Back 返回與 Prefab Clone 加載的 View 控制器 — ns: ProjectDR.Village.Shared |
| `Assets/Game/Scripts/Village/Navigation/View/VillageHubView.cs` | 村莊主畫面（Hub），顯示角色按鈕（村長夫人、守衛、魔女、農女）— ns: ProjectDR.Village.Navigation |
| `Assets/Game/Scripts/Village/Shared/View/TypewriterEffect.cs` | 打字機效果元件（逐字顯示 TMP_Text，支援跳過）— ns: ProjectDR.Village.Shared |
| `Assets/Game/Scripts/Village/Storage/View/StorageAreaView.cs` | 倉庫畫面，顯示庫存物品清單（支援 overlay 模式）— ns: ProjectDR.Village.Storage（E4 搬移）|
| `Assets/Game/Scripts/Village/Navigation/View/ExplorationAreaView.cs` | 探索入口畫面，提供出發按鈕（出發後由 VillageEntryPoint 處理切換）— ns: ProjectDR.Village.Navigation |
| `Assets/Game/Scripts/Village/UI/AlchemyAreaView.cs` | 煉金工坊畫面（IT 階段 Placeholder，待 E7 搬移）|
| `Assets/Game/Scripts/Village/Farm/View/FarmAreaView.cs` | 農場畫面（農田格子顯示、種植/收穫互動、種子選擇面板）— ns: ProjectDR.Village.Farm（E4 搬移）|
| `Assets/Game/Scripts/Village/Gift/View/GiftAreaView.cs` | 送禮畫面（合併背包+倉庫物品清單、好感度顯示、門檻達成回饋，overlay 模式）— ns: ProjectDR.Village.Gift（E4 搬移）|
| `Assets/Game/Scripts/Village/CG/View/CGGalleryView.cs` | CG 回憶圖鑑 overlay View（已解鎖場景清單、點擊重播 HCG 劇情）|
| `Assets/Game/Scripts/Village/UI/CraftSlotWidget.cs` | 格子式工作台單 Slot Widget（Idle/InProgress/Completed 三種狀態顯示；B11 Sprint 4；待 E7 搬移）|
| `Assets/Game/Scripts/Village/UI/CraftWorkbenchView.cs` | 格子式工作台主 View（動態生成 CraftSlotWidget、訂閱 CommissionTick/Completed/Claimed；B11 Sprint 4；待 E7 搬移）|
| `Assets/Game/Scripts/Village/UI/CraftItemSelectorView.cs` | 物品選擇 Overlay（過濾可用物品、空手委託支援；B11 Sprint 4；待 E7 搬移）|

## 地圖資料（Assets/Game/Resources/Maps）

| 檔案 | 用途 |
|------|------|
| `Assets/Game/Resources/Maps/it-test-map.json` | IT 階段測試地圖（8x8，出發點 (3,7)，撤離點群組 0：(3,0)(4,0)） |

## UI Prefab（Assets/Game/Prefabs）

| 檔案 | 用途 |
|------|------|
| `Assets/Game/Prefabs/VillageHubView.prefab` | 村莊主畫面 UGUI Prefab |
| `Assets/Game/Prefabs/CharacterInteractionView.prefab` | 角色互動畫面 UGUI Prefab（立繪、對話、功能選單、overlay 容器） |
| `Assets/Game/Prefabs/StorageAreaView.prefab` | 倉庫畫面 UGUI Prefab（雙欄佈局：背包+倉庫） |
| `Assets/Game/Prefabs/ExplorationAreaView.prefab` | 探索入口畫面 UGUI Prefab |
| `Assets/Game/Prefabs/AlchemyAreaView.prefab` | 煉金工坊畫面 UGUI Prefab（Placeholder） |
| `Assets/Game/Prefabs/FarmAreaView.prefab` | 農場畫面 UGUI Prefab（Placeholder） |
| `Assets/Game/Prefabs/GiftAreaView.prefab` | 送禮畫面 UGUI Prefab（物品清單、好感度顯示、門檻回饋） |
| `Assets/Game/Prefabs/CGGalleryView.prefab` | CG 回憶圖鑑 UGUI Prefab |
| `Assets/Game/Prefabs/AreaButton.prefab` | 區域導航按鈕模板（CharacterInteractionView 功能選單動態生成用；Sprint 5：新增 RedDot 子物件，預設隱藏，L2/L3 紅點下沉至對話/任務按鈕時 SetActive(true)） |
| `Assets/Game/Prefabs/BackpackSlotRow.prefab` | 背包格子行模板（StorageAreaView 背包欄動態生成用） |
| `Assets/Game/Prefabs/WarehouseItemRow.prefab` | 倉庫物品行模板（StorageAreaView 倉庫欄動態生成用） |
| `Assets/Game/Prefabs/FarmPlotUI.prefab` | 農田格子 UI 模板（FarmAreaView 動態生成用） |
| `Assets/Game/Prefabs/SeedItemButton.prefab` | 種子選項按鈕模板（FarmAreaView 種子選擇面板用） |
| `Assets/Game/Prefabs/GiftItemRow.prefab` | 送禮物品行模板（GiftAreaView 物品清單動態生成用） |
| `Assets/Game/Prefabs/ItemRow.prefab` | 物品列模板（舊版，保留備用） |
| `Assets/Game/Prefabs/ChoiceButton.prefab` | VN 選項按鈕模板（CharacterInteractionView 動態生成選項用，LayoutElement h=80 + Image + Button + TMP_Text） |
| `Assets/Game/Prefabs/Village/UI/CraftSlotWidget.prefab` | 格子式工作台單 Slot UGUI Prefab（w=480 h=680，含背景/TxtItemName/TxtStatus/TxtCountdown/BtnSlotArea/BtnClaim） |
| `Assets/Game/Prefabs/Village/UI/CraftWorkbenchView.prefab` | 格子式工作台主 View UGUI Prefab（w=1280 h=1920，含 SlotsContainer/TxtTitle/BtnReturn） |
| `Assets/Game/Prefabs/Village/UI/CraftItemSelectorView.prefab` | 物品選擇 Overlay UGUI Prefab（w=1280 h=1800，含 ScrollRect/ItemListContainer/TxtTitle/BtnClose） |
| `Assets/Game/Prefabs/Village/UI/ItemRowPrefab.prefab` | 物品行按鈕模板（CraftItemSelectorView 動態生成用，w=1120 h=80） |
| `Assets/Game/Prefabs/Village/UI/CharacterIntroCGView.prefab` | 登場 CG + 短劇情播放全螢幕 overlay Prefab（w=3840 h=2160 / PnlBackground+ImgCG+PnlDialogue+BtnFullScreen）；B13 Sprint 4 |
| `Assets/Game/Prefabs/Village/UI/PlayerQuestionsView.prefab` | 玩家發問 overlay Prefab（右側 w=1600 / TxtTitle+ScrollRect+BtnClose+AnswerPanel）；B14 Sprint 4；Sprint 5 新增 TiredPanel（預設隱藏，體力 0 時顯示「現在好累了」）+TiredLabel |
| `Assets/Game/Prefabs/Village/UI/CharacterQuestionsView.prefab` | 角色發問 overlay Prefab（Sprint 5 B6 新建；w=1600 h=1800 中央 overlay；PnlBackground+TxtDialogue+OptionsContainer+OptionButtonPrefab+BtnClose；所有 SerializeField 已連接） |

### 探索模組（Assets/Game/Scripts/Village/Exploration）
| 檔案 | 用途 |
|------|------|
| `Assets/Game/Scripts/Village/Exploration/CellType.cs` | 格子類型列舉（Explorable、Blocked） |
| `Assets/Game/Scripts/Village/Exploration/MoveDirection.cs` | 移動方向列舉（Up、Down、Left、Right） |
| `Assets/Game/Scripts/Village/Exploration/ExplorationEvents.cs` | 探索系統事件定義（CellRevealedEvent、MonsterCountsChangedEvent、PlayerMoveStartedEvent、PlayerMoveCompletedEvent、PlayerCellChangedEvent、ExplorationMapInitializedEvent、EvacuationStartedEvent、EvacuationCancelledEvent、ExplorationCompletedEvent、CollectionStartedEvent、CollectionCancelledEvent、GatheringCompletedEvent、ItemSlotUnlockedEvent、ItemPickedUpEvent、CollectionPanelClosedEvent） |
| `Assets/Game/Scripts/Village/Exploration/IMonsterPositionProvider.cs` | 魔物位置提供者介面（GridMap 透過此介面取得魔物位置） |
| `Assets/Game/Scripts/Village/Exploration/IMoveSpeedCalculator.cs` | 格子制移動速度計算器介面（回傳 lerp duration，已棄用但保留） |
| `Assets/Game/Scripts/Village/Exploration/FixedMoveSpeedCalculator.cs` | 固定格子制移動速度計算器（已棄用但保留） |
| `Assets/Game/Scripts/Village/Exploration/IMoveSpeedProvider.cs` | 自由移動速度提供者介面（回傳 units/second） |
| `Assets/Game/Scripts/Village/Exploration/FixedMoveSpeedProvider.cs` | 固定自由移動速度提供者（測試用） |
| `Assets/Game/Scripts/Village/Exploration/MapData.cs` | 地圖靜態資料（寬高、格子類型、出發點、撤離點群組，從 JSON 載入） |
| `Assets/Game/Scripts/Village/Exploration/MapDataJson.cs` | JSON DTO（MapDataJson、PositionJson、EvacuationGroupJson）與靜態載入器 MapDataLoader |
| `Assets/Game/Scripts/Village/Exploration/GridMap.cs` | 格子地圖邏輯管理器（探索狀態、魔物數量計算、格子揭開、撤離點管理） |
| `Assets/Game/Scripts/Village/Exploration/PlayerGridMovement.cs` | 玩家格子移動邏輯（已棄用但保留，供舊測試參考） |
| `Assets/Game/Scripts/Village/Exploration/PlayerFreeMovement.cs` | 玩家自由移動邏輯（世界座標追蹤、格子變更偵測、撞牆滑動、擊退） |
| `Assets/Game/Scripts/Village/Exploration/GridCellView.cs` | 單格 View（SpriteRenderer 顏色 + TMP 數字，訂閱 CellRevealedEvent / MonsterCountsChangedEvent） |
| `Assets/Game/Scripts/Village/Exploration/ExplorationMapView.cs` | 地圖 View（動態生成所有可走格子的 GridCellView，提供 GridToWorldPosition） |
| `Assets/Game/Scripts/Village/Exploration/ExplorationPlayerView.cs` | 玩家 token View（已棄用但保留，格子制 Lerp 動畫） |
| `Assets/Game/Scripts/Village/Exploration/ExplorationFreePlayerView.cs` | 玩家 token View（自由移動版，每幀同步 WorldPosition） |
| `Assets/Game/Scripts/Village/Exploration/ExplorationInputHandler.cs` | 鍵盤輸入處理器（已棄用但保留，格子制離散輸入） |
| `Assets/Game/Scripts/Village/Exploration/ExplorationFreeInputHandler.cs` | 鍵盤輸入處理器（自由移動版，WASD 持續輸入） |
| `Assets/Game/Scripts/Village/Exploration/EvacuationManager.cs` | 撤離倒數管理器（純 C# 邏輯，監控玩家位置、管理 6 秒倒數計時） |
| `Assets/Game/Scripts/Village/Exploration/EvacuationView.cs` | 撤離倒數 View（WorldSpace TextMeshPro 顯示倒數文字） |
| `Assets/Game/Scripts/Village/Exploration/CollectiblePointData.cs` | 探索點靜態資料（CollectibleItemEntry + CollectiblePointData，物品清單、採集時間、解鎖時間） |
| `Assets/Game/Scripts/Village/Exploration/CollectiblePointState.cs` | 探索點運行時狀態機（兩層計時：Idle→Gathering→Unlocking，物品欄位解鎖與拾取） |
| `Assets/Game/Scripts/Village/Exploration/CollectionManager.cs` | 採集互動核心邏輯管理器（開始/取消採集、計時推進、物品拾取、移動鎖定） |
| `Assets/Game/Scripts/Village/Exploration/CollectiblePointIndicatorView.cs` | 採集點地圖標記 View（已探索格子有採集點時疊加藍色小方塊，訂閱 CellRevealedEvent） |
| `Assets/Game/Scripts/Village/Exploration/CollectionInteractionHintView.cs` | 採集互動提示 View（站在採集點上顯示「按 E 採集」/「按 E 取消」，WorldSpace TMP） |
| `Assets/Game/Scripts/Village/Exploration/CollectionGatheringView.cs` | 採集第一層計時進度條 View（WorldSpace SpriteRenderer，訂閱 CollectionStartedEvent / CancelledEvent / CompletedEvent） |
| `Assets/Game/Scripts/Village/Exploration/CollectionItemPanelView.cs` | 採集物品欄 UI View（ScreenSpace Overlay UGUI，第二層計時欄位、拾取按鈕、背包狀態、關閉按鈕） |
| `Assets/Game/Scripts/Village/Exploration/ExplorationCameraFollow.cs` | 探索模式攝影機跟隨（LateUpdate 平滑跟隨玩家 token，銷毀時還原位置） |
| `Assets/Game/Scripts/Village/Exploration/ExplorationEntryPoint.cs` | 探索場景進入點 MonoBehaviour（組裝邏輯層與 View 層，序列化 _mapJson TextAsset） |

### 戰鬥模組（Assets/Game/Scripts/Village/Exploration/Combat）
| 檔案 | 用途 |
|------|------|
| `Assets/Game/Scripts/Village/Exploration/Combat/CombatConfigData.cs` | 戰鬥配置 JSON DTO 與不可變 CombatConfig（玩家數值、劍攻擊參數、移動速度、自由移動速度、擊退參數） |
| `Assets/Game/Scripts/Village/Exploration/Combat/MonsterConfigData.cs` | 魔物配置 JSON DTO 與不可變 MonsterConfig / MonsterTypeData（魔物種類定義） |
| `Assets/Game/Scripts/Village/Exploration/Combat/PlayerCombatStats.cs` | 玩家戰鬥數值容器（HP/ATK/DEF/SPD，TakeDamage/Heal，發布 HP 變更事件） |
| `Assets/Game/Scripts/Village/Exploration/Combat/DamageCalculator.cs` | 靜態傷害計算（DMG = ATK - DEF，最小值 1） |
| `Assets/Game/Scripts/Village/Exploration/Combat/SwordAttack.cs` | 劍攻擊邏輯（扇形範圍判定、冷卻、SPD 影響冷卻） |
| `Assets/Game/Scripts/Village/Exploration/Combat/MonsterState.cs` | 單一魔物運行時狀態（位置、HP、AI 狀態機：Idle/Roaming/Chasing/AttackPreparing/AttackCooldown） |
| `Assets/Game/Scripts/Village/Exploration/Combat/MonsterManager.cs` | 魔物管理器（生成、AI 更新、死亡移除），實作 IMonsterPositionProvider |
| `Assets/Game/Scripts/Village/Exploration/Combat/CombatManager.cs` | 戰鬥互動管理器（玩家攻擊判定、魔物攻擊判定、碰觸傷害+擊退） |
| `Assets/Game/Scripts/Village/Exploration/Combat/CombatEvents.cs` | 戰鬥系統事件定義（PlayerHpChanged、PlayerDied、PlayerDeath、DeathRewindCompleted、MonsterDamaged、MonsterDied、PlayerAttack、MonsterAttackPrepare/Execute、PlayerContactDamage、PlayerKnockback、PlayerSteppedOnMonster(legacy)、MonsterMoved、MonsterSpawned） |
| `Assets/Game/Scripts/Village/Exploration/Combat/SpdMoveSpeedCalculator.cs` | SPD 基礎格子制移動速度計算器（已棄用但保留） |
| `Assets/Game/Scripts/Village/Exploration/Combat/SpdMoveSpeedProvider.cs` | SPD 基礎自由移動速度提供者（speed = base + spd * factor） |
| `Assets/Game/Scripts/Village/Exploration/Combat/CombatInputHandler.cs` | 滑鼠輸入處理器（左鍵攻擊，方向跟隨滑鼠，使用 PlayerFreeMovement） |
| `Assets/Game/Scripts/Village/Exploration/Combat/PlayerCombatView.cs` | 玩家戰鬥 View（HP bar、劍揮擊閃現） |
| `Assets/Game/Scripts/Village/Exploration/Combat/PlayerContactDetector.cs` | 玩家碰撞偵測器（Collider2D 偵測魔物碰觸，發布 PlayerContactDamageEvent） |
| `Assets/Game/Scripts/Village/Exploration/Combat/AimIndicatorView.cs` | 瞄準方向指示器（LineRenderer 從玩家到滑鼠方向畫線） |
| `Assets/Game/Scripts/Village/Exploration/Combat/MonsterView.cs` | 魔物 View（彩色方塊 + HP bar + 攻擊準備警告 + 碰撞體，已探索格才可見） |
| `Assets/Game/Scripts/Village/Exploration/Combat/DeathManager.cs` | 死亡管理器（監聽 PlayerDiedEvent，執行背包回溯、發布死亡事件、結束探索） |
| `Assets/Game/Scripts/Village/Exploration/Combat/DeathView.cs` | 死亡視覺回饋 View（紅色閃爍、畫面變暗、「時間回溯...」文字提示） |
| `Assets/Game/Scripts/Village/Exploration/Combat/DamageNumberView.cs` | 傷害數字飄字 View（浮動上升並淡出） |

## 戰鬥配置資料（Assets/Game/Resources/Config）

| 檔案 | 用途 |
|------|------|
| `Assets/Game/Resources/Config/_Google Sheet 2 Json Setting.asset` | KGC GoogleSheet2JsonSetting ScriptableObject（A6-1 搬入 2026-04-22）；sheetID 已填入；sheetNames 含 13 個分頁；Inspector 「Start Convert」按鈕可觸發 Sheets → TXT 匯出 |
| `Assets/Game/Resources/Config/combat-config.txt` | 玩家初始數值（HP/ATK/DEF/SPD）、劍攻擊參數（角度、範圍、冷卻）、移動速度參數（A6-2 由 .json 改名 2026-04-22） |
| `Assets/Game/Resources/Config/monster-config.txt` | 魔物種類定義（Slime、Bat：HP/ATK/DEF/SPD、移動冷卻、視野、攻擊參數、顏色）（A6-2 由 .json 改名 2026-04-22） |
| `Assets/Game/Resources/Config/affinity-config.txt` | 好感度門檻配置（各角色門檻值陣列，IT 階段四角色各 [5]）（A6-2 由 .json 改名 2026-04-22） |
| `Assets/Game/Resources/Config/cg-scene-config.txt` | CG 場景配置（角色對應 CG 場景 ID、門檻值、對話 ID、顯示名稱）（A6-2 由 .json 改名 2026-04-22） |
| `Assets/Game/Resources/Config/commission-recipes-config.txt` | 委託配方表（A7 placeholder：農女 3 + 魔女 3 + 守衛 3 條配方，含輸入/產出/時間/格子上限）（A6-2 由 .json 改名 2026-04-22） |
| `Assets/Game/Resources/Config/storage-expansion-config.txt` | 倉庫擴建階段表（A6 placeholder：5 級擴建 100→350，物資與等待時間遞增曲線）（A6-2 由 .json 改名 2026-04-22） |
| `Assets/Game/Resources/Config/initial-resources-config.txt` | 初始資源配置表（A4 placeholder：節點 0 空背包、農女解鎖/魔女解鎖/守衛歸來事件贈送物）（A6-2 由 .json 改名 2026-04-22） |
| `Assets/Game/Resources/Config/gift-sword-config.txt` | 贈劍屬性表（A4-3 placeholder：木劍 ATK+3，守衛歸來事件贈送）（A6-2 由 .json 改名 2026-04-22） |
| `Assets/Game/Resources/Config/character-intro-config.txt` | 角色登場 CG + 短劇情（A1 placeholder：4 位角色場景描述 + 對話行，village_chief_wife/farm_girl/witch/guard）（A6-2 由 .json 改名 2026-04-22） |
| `Assets/Game/Resources/Config/player-questions-config.txt` | 玩家發問配置（B14 placeholder：28 題，VCW 12/農女 9/魔女 9/守衛 0（F12 決策 6-13 移除 guard_ask_sword），分 stage 0/1/2 三批解鎖，待製作人撰寫正式回答）（A6-2 由 .json 改名 2026-04-22） |
<!-- guard-first-meet-dialogue-config.json 已刪除（A08 併入 NodeDialogueConfig，2026-04-22）；內容移至 node-dialogue-config.json node_id="guard_first_meet" -->
| `Assets/Game/Resources/Config/character-questions-config.txt` | 角色發問配置（Sprint 5 A1~A3 placeholder：4 個性類型定義 personality_gentle/lively/calm/assertive、4 角色個性偏好對應、280 題角色發問 4 角色 × 7 級 × 10 題 × 4 個性選項；所有文字程式化 placeholder 待製作人後續撰寫）（A6-2 由 .json 改名 2026-04-22） |
| `Assets/Game/Resources/Config/greeting-config.txt` | 招呼語配置（Sprint 5 A4 placeholder：280 句 4 角色 × 7 級 × 10 句；進入角色互動畫面自動播放、L1/L4 紅點亮時跳過、L2/L3 仍播放）（A6-2 由 .json 改名 2026-04-22） |
| `Assets/Game/Resources/Config/idle-chat-config.txt` | [閒聊] 問題池配置（Sprint 5 A5 placeholder：4 角色 × 20 題 × 3 回答；玩家 40 題池耗盡後觸發的隨機問答 fallback）（A6-2 由 .json 改名 2026-04-22） |
| `Assets/Game/Resources/CG/` | 登場 CG Sprite 存放目錄（IT 階段空目錄，找不到 Sprite 時顯示深紫色 placeholder 色塊） |
| `Assets/Game/Resources/Config/node-dialogue-config.txt` | 節點 0/1/2 劇情對話（A2 placeholder：村長夫人三節點對話、VN 選項、選擇後回應；choice_branch 標記農女/魔女分支；id 32~35 guard_first_meet 並入）（A6-2 由 .json 改名 2026-04-22） |
| ~~`Assets/Game/Resources/Config/guard-return-config.json`~~ | ~~守衛歸來事件劇情~~ — **已刪除**（E6 2026-04-22，A09 GuardReturnConfigData dead code 整包移除）|
| `Assets/Game/Resources/Config/main-quest-config.txt` | 主線任務序列 T0~T4（A5 placeholder：5 個任務，名稱/描述/完成條件/獎勵 grant_id/解鎖事件）（A6-2 由 .json 改名 2026-04-22） |
<!-- Assets/Resources/Data/ 目錄已刪除（A6-1 2026-04-22，Setting.asset 搬至 Config/，空目錄移除） -->

## 測試程式碼（Tests）

| 檔案 | 用途 |
|------|------|
| `Assets/Tests/Editor/Village/Game.Tests.asmdef` | 村莊模組測試 Assembly Definition（引用 Game） |
| `Assets/Tests/Editor/Village/VillageProgressionManagerTests.cs` | VillageProgressionManager 單元測試（11 個） |
| `Assets/Tests/Editor/Village/VillageNavigationManagerTests.cs` | VillageNavigationManager 單元測試（14 個） |
| `Assets/Tests/Editor/Village/StorageManagerTests.cs` | StorageManager 單元測試（19 個，既有：AddItem/RemoveItem/GetItemCount/HasItem/GetAllItems/事件） |
| `Assets/Tests/Editor/Village/StorageManagerCapacityTests.cs` | StorageManager 容量化新功能測試（25 個：預設容量、自訂建構、TryAddItem 部分加入、超容量拋例外、堆疊跨格、IsFull、RemoveItem 跨格子、ExpandCapacity、GetAllItems 聚合） |
| `Assets/Tests/Editor/Village/StorageExpansionConfigTests.cs` | StorageExpansionConfig 單元測試（6 個：null 檢查、required_items 解析、排序、真實 JSON） |
| `Assets/Tests/Editor/Village/StorageExpansionManagerTests.cs` | StorageExpansionManager 單元測試（21 個：狀態機、物資檢查、扣除先背包後倉庫、Tick 倒數、完成容量套用、AcknowledgeCompletion、多輪擴建、MaxLevelReached） |
| `Assets/Tests/Editor/Village/InitialResourcesConfigTests.cs` | InitialResourcesConfig 單元測試（9 個：依 trigger 分組、null/空 item_id 處理、真實 JSON） |
| `Assets/Tests/Editor/Village/MainQuestConfigTests.cs` | MainQuestConfig 單元測試（8 個：pipe 解析、排序、真實 JSON） |
| `Assets/Tests/Editor/Village/MainQuestManagerTests.cs` | MainQuestManager 單元測試（22 個：狀態機轉換、事件、Auto 自動完成、NotifyCompletionSignal、跨任務流程） |
| `Assets/Tests/Editor/Village/CharacterUnlockManagerTests.cs` | CharacterUnlockManager 單元測試（19 個：初始解鎖、VN 節點 0/1 選擇、守衛事件、探索功能解鎖、grant 派發、Dispose） |
| `Assets/Tests/Editor/Village/QuestManagerTests.cs` | QuestManager 單元測試（16 個） |
| `Assets/Tests/Editor/Village/BackpackManagerTests.cs` | BackpackManager 單元測試（格子制新增/移除、容量、堆疊、快照/回溯、事件） |
| `Assets/Tests/Editor/Village/StorageTransferManagerTests.cs` | StorageTransferManager 單元測試（雙向轉移、邊界條件） |
| `Assets/Tests/Editor/Village/ExplorationEntryManagerTests.cs` | ExplorationEntryManager 單元測試（V2：戰利品進背包、出發快照） |
| `Assets/Tests/Editor/Village/DialogueManagerTests.cs` | DialogueManager 單元測試（16 個：初始狀態、開始對話、推進、事件發布、邊界） |
| `Assets/Tests/Editor/Village/DialogueManagerChoiceTests.cs` | DialogueManager VN 選項分支單元測試（B2 擴展：PresentChoices/SelectChoice/AppendLines、完整 VN 分支流程） |
| `Assets/Tests/Editor/Village/NodeDialogueConfigTests.cs` | NodeDialogueConfig 單元測試（建構驗證、依 node_id/sequence 分組、選項/回應分支、JSON 反序列化） |
| `Assets/Tests/Editor/Village/ItemTypeResolverTests.cs` | ItemTypeResolver 單元測試（14 個：Register/覆寫/例外、GetItemType/例外、IsType、GetItemsByType） |
| `Assets/Tests/Editor/Village/FarmManagerTests.cs` | FarmManager 單元測試（42 個：建構驗證、格子查詢、種植/收穫成功失敗、時間邊界、批次收穫、事件發布） |
| `Assets/Tests/Editor/Village/CommissionRecipesConfigTests.cs` | CommissionRecipesConfig 單元測試（14 個：建構驗證、配方查詢、依角色/輸入物品分組、slot 數聚合、空手委託、無效項目過濾、真實 JSON 反序列化） |
| `Assets/Tests/Editor/Village/CommissionManagerTests.cs` | CommissionManager 單元測試（26 個：建構驗證、slot 查詢、Start 6 個失敗支線、Start 成功/背包優先/空手、Tick 倒數/Completed 去重/多 slot 獨立、Claim 失敗支線、Claim 入背包/溢出倉庫/兩邊都滿、完整流程、主線 commission_count 訊號整合） |
| `Assets/Tests/Editor/Village/RedDotManagerTests.cs` | RedDotManager 單元測試（18 個：建構驗證、4 層觸發、優先序 L1>L4>L3>L2、RedDotUpdatedEvent 去重、GetCharactersWithRedDot、Dispose）；B7 Sprint 4 |
| `Assets/Tests/Editor/Village/CharacterIntroConfigTests.cs` | CharacterIntroConfig 單元測試（5 個：null 檢查、依 intro_id/character_id 分組、依 sequence 排序、GetLineTexts、null 過濾）；B9 Sprint 4 |
| `Assets/Tests/Editor/Village/NodeDialogueControllerTests.cs` | NodeDialogueController 單元測試（12 個：建構驗證、PlayNode 啟動/例外、PresentChoices 立即呈現、完整流程選擇+附加 response+完成事件、Dispose）；B9 Sprint 4 |
| `Assets/Tests/Editor/Village/OpeningSequenceControllerTests.cs` | OpeningSequenceController 單元測試（10 個：建構驗證、StartOpeningSequence 觸發 CG、CG 完成啟動節點、完整流程、重複呼叫忽略、Dispose；使用 FakeCGPlayer）；B9 Sprint 4 |
| `Assets/Tests/Editor/Village/GuardReturnEventControllerTests.cs` | GuardReturnEventController 單元測試（7 個：建構驗證、CanTrigger 初始為 true、TriggerEvent 呼叫 CGPlayer、二次呼叫回 false、HasTriggered、CG 完成後發布 CompletedEvent、null 建構例外）；E6 重建（A09 DTO 刪除後以新介面重建）|
| `Assets/Tests/Editor/Village/CharacterIntroCGPlayerTests.cs` | CharacterIntroCGPlayer 單元測試（4 個：FakeCGPlayer 介面替換、PlaceholderCGPlayer 事件發布、空 characterId）；B13 Sprint 4 |
| `Assets/Tests/Editor/Village/PlayerQuestionsConfigTests.cs` | PlayerQuestionsConfig 單元測試（14 個：null 建構、排序、stage 過濾、GetQuestion、多角色隔離、真實 JSON）；B14 Sprint 4 |
| `Assets/Tests/Editor/Village/CharacterQuestionCountdownManagerTests.cs` | Sprint 5 B1：15 個測試（建構保護、StartCountdown、Tick 邊界、上限 1、ClearReady、工作中暫停恢復、Dispose） |
| `Assets/Tests/Editor/Village/RedDotLayerSubsinkTests.cs` | Sprint 5 B3：5 個測試（IsLayerActive 查詢、L1/L2 共存、L1 清後 L2 保留、null 保護） |
| `Assets/Tests/Editor/Village/CharacterQuestionsConfigTests.cs` | Sprint 5 B4：10 個測試（個性偏好、四檔線性 +0/+2/+5/+10、char×level 查詢、GetQuestion、真實 280 題 JSON） |
| `Assets/Tests/Editor/Village/CharacterQuestionsManagerTests.cs` | Sprint 5 B5：11 個測試（null 建構、抽題不重複、Seen 記憶、SubmitAnswer 加好感度、事件發布） |
| `Assets/Tests/Editor/Village/DialogueCooldownManagerTests.cs` | Sprint 5 B10：12 個測試（建構邊界、Started/Completed 事件、Tick 扣時、工作中 ×2 倍率、運行中切換 Working、Dispose） |
| `Assets/Tests/Editor/Village/IdleChatTests.cs` | Sprint 5 B12：9 個測試（Config null/empty/realJson 20 題×3 回答、Presenter 觸發 + 事件發布） |
| `Assets/Tests/Editor/Village/PlayerQuestionsManagerTests.cs` | Sprint 5 B11：9 個測試（≥4 抽 4 / 3 全顯 / 0 閒聊 fallback、MarkSeen、多角色獨立、GetPresentation 不標記已看） |
| `Assets/Tests/Editor/Village/CharacterStaminaManagerTests.cs` | Sprint 5 B13：9 個測試（預設滿、扣/恢復、上下限、自訂建構、多角色獨立） |
| `Assets/Tests/Editor/Village/GreetingTests.cs` | Sprint 5 B15/B16：11 個測試（Config 建構/真實 JSON 280 句；Presenter L1/L4 壓制、L2 仍播、事件發布、null 保護） |
| `Assets/Tests/Editor/Village/Integration/DialogueFlowIntegrationTest.cs` | Sprint 5 C1~C7：11 個整合測試（紅點分流端到端、玩家發問端到端、閒聊模式、體力歸零、工作中 CD ×2、紅點上限 1、招呼語分流） |
| `Assets/Tests/Editor/Village/Integration/OpeningFlowIntegrationTest.cs` | C1 整合測試 TEST 1：開場流程（10 個：CG→節點 0→VN 選項→解鎖+資源→完成事件）；Sprint 4 C1 |
| `Assets/Tests/Editor/Village/Integration/NodeProgressionIntegrationTest.cs` | C1 整合測試 TEST 2/3：節點 1 流程 + 節點 2 + 探索解鎖（7 個：剩下那位解鎖、T1 完成訊號、T3→探索解鎖、序列驗證）；Sprint 4 C1 |
| `Assets/Tests/Editor/Village/Integration/GuardReturnIntegrationTest.cs` | C1 整合測試 TEST 4：首次探索守衛歸來（7 個：攔截、CG+對話、守衛解鎖+贈劍、一次性觸發）；Sprint 4 C1 |
| `Assets/Tests/Editor/Village/Integration/GuardFirstMeetDialogueIntegrationTest.cs` | 守衛首次取劍對白整合測試（T1~T5b：TryPlayFirstMeetDialogueIfNotTriggered 首次/重複呼叫、NodeDialogueCompletedEvent 發劍+ExplorationGateReopenedEvent、T2 production path、特殊題不在清單中）；A08 併入後重構（2026-04-22），改用 NodeDialogueController 路徑 |
| `Assets/Tests/Editor/Village/Integration/FullLoopIntegrationTest.cs` | C1 整合測試 TEST 5：完整 4 循環並行（15 個：探索/委託/擴建/感情循環 + 紅點 L1/L2/L3 整合 + 多循環並行）；Sprint 4 C1 |
| `Assets/Tests/Editor/Village/Exploration/MapDataTests.cs` | MapData 單元測試（建構驗證、格子查詢、邊界檢查、出發點/撤離點驗證） |
| `Assets/Tests/Editor/Village/Exploration/MapDataLoaderTests.cs` | MapDataLoader 單元測試（8 個：正常反序列化、寬高、出發點、格子類型、撤離點、null/empty/invalid cell 例外） |
| `Assets/Tests/Editor/Village/Exploration/GridMapTests.cs` | GridMap 單元測試（初始化探索、揭開邏輯、魔物數量計算、撤離點、事件發布） |
| `Assets/Tests/Editor/Village/Exploration/EvacuationManagerTests.cs` | EvacuationManager 單元測試（建構驗證、觸發點偵測、倒數啟動/取消/完成、進度計算、狀態鎖定、事件發布） |
| `Assets/Tests/Editor/Village/Exploration/PlayerGridMovementTests.cs` | PlayerGridMovement 單元測試（已棄用但保留，驗證舊格子制移動邏輯） |
| `Assets/Tests/Editor/Village/Exploration/PlayerFreeMovementTests.cs` | PlayerFreeMovement 單元測試（自由移動、格子變更偵測、撞牆、擊退、座標轉換） |
| `Assets/Tests/Editor/Village/Exploration/CollectiblePointDataTests.cs` | CollectiblePointData 單元測試（14 個：建構驗證、防禦性拷貝、邊界條件） |
| `Assets/Tests/Editor/Village/Exploration/CollectiblePointStateTests.cs` | CollectiblePointState 單元測試（44 個：狀態機轉換、兩層計時、物品拾取、取消重置、事件發布） |
| `Assets/Tests/Editor/Village/Exploration/CollectionManagerTests.cs` | CollectionManager 單元測試（45 個：互動可用性、開始/取消採集、移動鎖定、物品拾取、完整流程） |
| `Assets/Tests/Editor/Village/Exploration/Combat/DamageCalculatorTests.cs` | DamageCalculator 單元測試（8 個：ATK>DEF、ATK=DEF、ATK<DEF、零值、負值） |
| `Assets/Tests/Editor/Village/Exploration/Combat/PlayerCombatStatsTests.cs` | PlayerCombatStats 單元測試（14 個：建構、TakeDamage、Heal、事件發布、死亡判定） |
| `Assets/Tests/Editor/Village/Exploration/Combat/SwordAttackTests.cs` | SwordAttack 單元測試（20 個：冷卻計算、攻擊執行、扇形範圍判定、事件發布） |
| `Assets/Tests/Editor/Village/Exploration/Combat/MonsterStateTests.cs` | MonsterState 單元測試（6 個：初始化、唯一 ID、傷害、死亡） |
| `Assets/Tests/Editor/Village/Exploration/Combat/MonsterManagerTests.cs` | MonsterManager 單元測試（17 個：生成、位置查詢、傷害、死亡移除、AI 狀態轉換、事件發布） |
| `Assets/Tests/Editor/Village/Exploration/Combat/SpdMoveSpeedCalculatorTests.cs` | SpdMoveSpeedCalculator 單元測試（已棄用但保留） |
| `Assets/Tests/Editor/Village/Exploration/Combat/SpdMoveSpeedProviderTests.cs` | SpdMoveSpeedProvider 單元測試（6 個：正常計算、下限、零 SPD、無效參數） |
| `Assets/Tests/Editor/Village/Exploration/Combat/CombatManagerTests.cs` | CombatManager 單元測試（6 個：魔物攻擊傷害、碰觸傷害+擊退、死亡魔物無效、事件清理） |
| `Assets/Tests/Editor/Village/AffinityManagerTests.cs` | AffinityManager 單元測試（31 個：建構驗證、好感度增加/累加、門檻觸發/多門檻/跳越門檻、事件發布、JSON 反序列化） |
| `Assets/Tests/Editor/Village/GiftManagerTests.cs` | GiftManager 單元測試（22 個：建構驗證、背包優先扣除、倉庫備援、物品不足、好感度累計、事件發布） |
| `Assets/Tests/Editor/Village/CGUnlockManagerTests.cs` | CGUnlockManager + CGSceneConfig 單元測試（33 個：解鎖邏輯、session 記憶體狀態、事件發布、JSON 反序列化） |
| `Assets/Tests/Editor/Village/CGInstallerTests.cs` | CGInstaller 單元測試（4 個：Install(null) 例外、Uninstall 事件清除、重入安全）；E2 B4d |
| `Assets/Tests/Editor/Village/DialogueFlowInstallerTests.cs` | DialogueFlowInstaller 單元測試（8 個：Install(null) 例外、建構子 null × 3、ctx.AffinityReadOnly 為 null 例外（T8）、Uninstall 事件清除 × 2、未 Install Uninstall 安全、重入安全、CommissionStarted/Claimed 效果）；E3 B4f 建立，E4 更新（移除 affinityManager 建構子參數、補 T8）|
| `Assets/Tests/Editor/Village/CoreStorageInstallerTests.cs` | CoreStorageInstaller 單元測試（12 個：Install(null) 例外、建構子容量 0 × 4、BackpackReadOnly/StorageReadOnly 填入、Accessor 可用、Uninstall 清除、IBackpackQuery × 3、IStorageQuery × 2）；E4 B4a |
| `Assets/Tests/Editor/Village/AffinityInstallerTests.cs` | AffinityInstaller 單元測試（7 個：Install(null) 例外、建構子 null、ctx.BackpackReadOnly 未就位例外、ctx.StorageReadOnly 未就位例外、AffinityReadOnly 填入、型別驗證、Uninstall 清除）；E4 B4c |
| `Assets/Tests/Editor/Village/ProgressionInstallerTests.cs` | ProgressionInstaller 單元測試（11 個：建構子 null × 4、Install(null) 例外、VillageProgressionReadOnly 填入、VCW 預設解鎖、農女初始未解鎖、探索初始未解鎖、GetRedDotManager 非 null、GetMainQuestManager 非 null、Uninstall 不拋例外）；E5 B4b |
| `Assets/Tests/Editor/Village/CommissionInstallerTests.cs` | CommissionInstaller 單元測試（7 個：建構子 null × 4、Install(null) 例外、Install TimeProvider 未就位例外、CommissionManager 非 null、StorageExpansionManager 非 null、Tick 不拋例外、Uninstall 不拋例外）；E5 B4e |
| `Assets/Tests/Editor/Village/Exploration/Combat/DeathManagerTests.cs` | DeathManager 單元測試（13 個：死亡偵測、背包回溯、事件順序、重複觸發防護、Dispose、Reset） |

## 開發日誌（dev-logs）

| 檔案 | 用途 |
|------|------|
| `dev-logs/2026-04-18-1.md` | Sprint 4 B3/B4/B6（CharacterUnlockManager + MainQuestManager + StorageManager 容量化）實作細節 |
| `dev-logs/2026-04-18-2.md` | Sprint 4 B8（VillageHubView 漸進解鎖重構）實作細節 |
| `dev-logs/2026-04-18-3.md` | Sprint 4 B5（CommissionManager 多角色架構）實作細節 |
| `dev-logs/2026-04-18-4.md` | Sprint 4 B11/B12（CraftWorkbenchView/CraftSlotWidget/CraftItemSelectorView Prefab + CommissionInteractionPresenter + WorkingLines 台詞）實作細節 |
| `dev-logs/2026-04-18-5.md` | Sprint 4 B7/B9/B10（RedDotManager + OpeningSequenceController/NodeDialogueController/ICGPlayer/PlaceholderCGPlayer/CharacterIntroConfig + GuardReturnEventController/GuardReturnConfig + ExplorationEntryManager 攔截器整合）實作細節 |
| `dev-logs/2026-04-18-6.md` | Sprint 4 B13/B14（CharacterIntroCGPlayer + CharacterIntroCGView + PlayerQuestionsConfig + PlayerQuestionsView + placeholder JSON + Prefab 建立）實作細節 |
| `dev-logs/2026-04-18-7.md` | Sprint 4 C1/C2 + Sprint 4 收尾（C2 移除強制解鎖、啟動 OpeningSequence、補 6 條事件訂閱、場景 6 個 SerializeField MCP 連接；C1 新增 4 份整合測試共 39 case；Sprint 4 三步收尾完成） |
| `dev-logs/2026-04-18-8.md` | 開場劇情接線修正（CharacterInteractionView 新增外部驅動對話模式、VillageEntryPoint 在開場時 push VCW Forced 模式、OpeningSequenceCompletedEvent 收尾 VCW view） |
| `dev-logs/2026-04-18-9.md` | Sprint 5 B+C（對話功能修正 B1~B21 實作 + C1~C7 整合測試；紅點 L2 倒數改造、角色發問 a 路徑、玩家發問 b 路徑重寫、招呼語系統、工作中 CD ×2、102 個新測試全通過） |
| `dev-logs/2026-04-18-10.md` | 對話按鈕紅點視覺修正（AreaButton RedDot 白→紅、置中→右上角）+ 角色發問選答案後改由主畫面 PlayDialogue 播放 response（CharacterInteractionView 新增 PlayDialogue public API、CharacterQuestionsView responseAction 委外） |
| `dev-logs/2026-04-18-11.md` | 角色 response 打字機 click-to-skip 修正（PrepareDialogueUI 啟用 FullScreenDialogueArea 時強制 SetAsLastSibling；PlayDialogue 改為呼叫 PrepareDialogueUI 統一流程） |
| `dev-logs/2026-04-20-1.md` | Sprint 6 B1/B2/B3（main-quest-config.json 3 任務重構 + initial-resources-config.json 移除農女/魔女 grants + node-dialogue-config.json 確認無需改動；TDD 更新 9 個測試案例）實作細節 |
| `dev-logs/2026-04-20-2.md` | Sprint 6 C1/C2/C4（MainQuestConfigData Node2DialogueComplete 常數 + CharacterUnlockManager T1 探索解鎖 + RedDotManager QuestIdsTriggersNode2="T1"）+ D1/D2/D3 測試更新（9 個測試檔涵蓋新 3-quest 結構）實作細節 |
| `dev-logs/2026-04-20-3.md` | Sprint 6 C5+D3.1+D3.2 補修（dev-head 審核後）：VillageEntryPoint 訊號源修正（node_2 完成送 Node2DialogueComplete R1）、節點 1 CG 後 SetMainQuestEventFlag（R2）、刪除節點 1 殘留死碼（R3）、[Obsolete] attribute（R4）、過時注釋更新（R5）、斷裂測試修復（R6）、R7-1/R7-2 回歸測試新增 |
| `dev-logs/2026-04-20-4.md` | Sprint 6 C6 bugfix（D4 實機測試第一個 bug）：節點 1/2 對話觸發邏輯修正。VillageEntryPoint 新增 _node1TriggerReady/_node2TriggerReady 旗標，與 MainQuest T1/T3 語義解耦；D4-1/D4-2 回歸測試新增 |
| `dev-logs/2026-04-20-5.md` | Sprint 6 C7 bugfix（D4 實機測試第二個 bug）：節點 1 對話結束後「剩下那位」角色 Hub 按鈕未解鎖。CharacterUnlockManager 新增訂閱 NodeDialogueCompletedEvent，node_1 完成時依 _node0ChosenBranch ForceUnlock；8 個測試更新/新增 |
| `dev-logs/2026-04-20-12.md` | Sprint 6 F10 bugfix（D4 bugfix 7）：系統性重構繞道路徑 + Revert F9；分析 callback side effects；抽出 OnCharacterEnteredAndCGDone 共用方法；4 個 F10 回歸測試（GuardInteractViewDialogueRegressionTest 重寫） |
| `dev-logs/2026-04-20-13.md` | Sprint 6 F11 bugfix（D4 bugfix 8）：路徑 B 補 SetState(Normal)；SetState(Normal) 從路徑 A callback 移入 OnCharacterEnteredAndCGDone 共用方法；新增 3 個 F11 回歸測試 |
| `dev-logs/2026-04-20-14.md` | Sprint 6 F12（D4 bugfix 9）：決策 6-13 守衛取劍改為首次進入自動對白觸發；Revert guard_ask_sword；新增 guard-first-meet-dialogue-config.json + GuardFirstMeetDialogueConfigData.cs + CharacterInteractionView.SetFirstMeetOverrideDialogue API + VillageEntryPoint.OnGuardFirstMeetDialogueCompleted；6 個新測試 + regression |
| `dev-logs/2026-04-22-1.md` | Sprint 7 E1（步驟 1/2/5/6）+ C1/C2：21 模組骨架 + Core 契約層（IVillageInstaller/VillageContext/GameDataQuery）+ A01 AffinityCharacterConfigData 改 IGameData + A05 CombatConfigJson 改 IGameData（singleton）+ validate-file-size.sh hook |
| `dev-logs/2026-04-22-2.md` | Sprint 7 E1（步驟 3/4/7）：16 個 C# 檔搬移保留 GUID（Shared/View + Navigation/Data + Navigation/Manager + Navigation/View + TimeProvider + ItemType + Core/Manager）+ Namespace 遷移全面套 ADR-004 規則 + 約 45 個引用檔案補 using + 測試 78/78 通過 |
| `dev-logs/2026-04-22-3.md` | Sprint 7 E2（B4d CGInstaller + A02/A03 IGameData + 搬移 15 個 CG/Dialogue/CharacterIntro/CharacterInteraction 模組檔案 + asmdef 修復 + 消費點 using 更新）；測試 1367/1377 通過（10 pre-existing 失敗） |
| `dev-logs/2026-04-22-4.md` | Sprint 7 E3（A04/A07/A10 IGameData 改造 + 搬移 CharacterQuestions/CharacterStamina/IdleChat/Greeting 四模組 + Namespace 遷移 + B4f DialogueFlowInstaller + 7 個 DialogueFlowInstallerTests）；測試 1367/1377 通過（10 pre-existing 失敗） |
| `dev-logs/2026-04-22-5.md` | Sprint 7 E4（搬移 Backpack/Storage/Farm/Gift/Affinity 五模組 + Namespace 遷移 + A16 StorageExpansionStageData IGameData + BackpackManager/StorageManager 實作 IBackpackQuery/IStorageQuery + AssemblyInfo InternalsVisibleTo + B4a CoreStorageInstaller + B4c AffinityInstaller + E3 TODO 解決（DialogueFlowInstaller 改從 ctx.AffinityReadOnly 取得 AffinityManager）+ 12+7+8 個新測試）；測試 1289/1299 通過（10 pre-existing 失敗）|
| `dev-logs/2026-04-22-6.md` | Sprint 7 E5（A06 CommissionRecipesConfigData IGameData + A11 InitialResourcesConfigData IGameData + A12 MainQuestConfigData IGameData + B4b ProgressionInstaller + B4e CommissionInstaller + 搬移 MainQuest/Commission/CharacterUnlock/Progression 模組 + Namespace 遷移 + 24 個消費者 using 批次補充 + 8 個測試檔批次補 id 欄位 + 18 個新測試）；測試 1367/1377 通過（10 pre-existing 失敗：Exploration 7 + GuardReturn 2 + RedDotManager 1）|
| `dev-logs/2026-04-22-7.md` | Sprint 7 E7（Exploration 子模組重組 + A13 MonsterTypeJson IGameData + B5 VillageEntryPoint Installer 整合 + B6 跨域事件訂閱 Action 快取 + B8 InitializeManagers 消滅 + CGInstaller 更新）；測試 1422/1422 通過（8 pre-existing）|
| `dev-logs/2026-04-22-8.md` | Sprint 7 B7 整合測試確認（7 個整合測試 70 case 全通過，B5/B6/B8 重構無回歸）|
| `dev-logs/2026-04-22-9.md` | Sprint 7 dev-log-9：VillageEntryPoint partial 拆分（859→585 行；抽 VillageEntryPointFunctionPrefabs.cs + VillageEntryPointInstallers.cs）+ 100 個空 .keep 批次刪除 + ADR-004 v1.2 運維細則新增）；測試 1426/1426（1 failed→RedDotManagerTests 測試設計 bug）|
| `dev-logs/2026-04-22-10.md` | Sprint 7 A2：A08 GuardFirstMeetDialogueConfig 併入 NodeDialogueConfig（非豁免路徑，重構併入；id 32~35 加入 node-dialogue-config.json；TryPlayFirstMeetDialogueIfNotTriggered API；GuardFirstMeetDialogueConfigData.cs + JSON + Guard/Data 目錄刪除；ADR-002 A08 ✅ v1.3）；測試 1426/1426 全綠 |
| `dev-logs/2026-04-22-11.md` | Sprint 7 收尾批次：A4 A11 checkbox 補勾 + ExplorationEntryPoint.Start() 拆分（199 行→5 個子方法）+ ADR-002 A17 HCGDialogueSetup IT 例外登記 + TD-2026-003/006 已處置；測試 1426/1426 全綠 |
| `dev-logs/2026-04-22-12.md` | Sprint 7 C03：validate-assets.sh hook 擴充（ADR-002 C03；Config JSON 陣列缺 id 欄位警示）|
| `dev-logs/2026-04-22-13.md` | Sprint 7 A6-1/A6-2：GoogleSheet2JsonSetting 串接（Setting.asset 搬至 Config/、sheetID/sheetNames 13 個分頁填入、15 個 .json → .txt 改名）；ADR-002 C01 ✅ |
| `dev-logs/2026-04-22-14.md` | Sprint 7 A6 降級收尾（純文件修正）：A5/A6 checkbox 降級標記、tool-spec Sprint 7 狀態段落、ADR-002 v1.6 Partial Exit 追加、session-state 更新 |
| `dev-logs/2026-04-22-15.md` | D7 緊急修復 1：VillageContext ctor ArgumentNullException（gameDataAccess null）；null check 放寬 + 回歸測試；1426/1426 全綠 |
| `dev-logs/2026-04-22-16.md` | D7 緊急修復 2：守衛首次進入無取劍對白；StartDialoguePlayback 覆蓋 guard_first_meet 根因修復 + T1b 回歸測試；1427/1427 全綠 |
