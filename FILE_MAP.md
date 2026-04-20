# ProjectDR - 文件地圖

## 專案管理

| 檔案 | 用途 |
|------|------|
| `project-status.md` | 專案狀態檔（當前階段、階段焦點、活躍 Sprint、待決事項、已知阻塞） |
| `project-tbd.md` | 專案級 TBD 池（跨 Sprint 累積的製作人待拍板 placeholder 清單，2026-04-20 建立；類別：intro/node/quest/resource/recipe/storage/balance） |
| `sprint/sprint-6-explore-gate-rework.md` | Sprint 6 — 探索開放流程重構（移除委託強制教學；2026-04-20 建立；承接 6 條設計決策） |
| `session-state/active.md` | 當前 session 狀態（Sprint 6 進度追蹤） |

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

### 村莊模組（Assets/Game/Scripts/Village）
| 檔案 | 用途 |
|------|------|
| `Assets/Game/Scripts/Village/AreaIds.cs` | 村莊區域 ID 常數定義（Hub、Storage、Exploration、Alchemy、Farm） |
| `Assets/Game/Scripts/Village/CharacterIds.cs` | 角色 ID 常數定義（VillageChiefWife、Guard、Witch、FarmGirl） |
| `Assets/Game/Scripts/Village/DialogueData.cs` | 對話資料結構（DialogueData 對話行 + DialogueChoice VN 選項） |
| `Assets/Game/Scripts/Village/DialogueManager.cs` | 對話播放狀態管理器（純邏輯，管理對話行推進、VN 選項分支、AppendLines 動態延伸） |
| `Assets/Game/Scripts/Village/NodeDialogueConfigData.cs` | 節點劇情對話配置 JSON DTO（NodeDialogueConfigData/NodeDialogueLineData）與不可變配置物件（NodeDialogueConfig/NodeDialogueData/NodeDialogueChoiceOption）+ NodeDialogueLineTypes 常數 |
| `Assets/Game/Scripts/Village/CharacterMenuData.cs` | 角色功能選單資料（角色 ID、顯示名稱、對話、功能清單） |
| `Assets/Game/Scripts/Village/VillageEvents.cs` | 村莊系統事件類別定義（AreaUnlockedEvent、DialogueStartedEvent、DialogueCompletedEvent、FarmPlotPlantedEvent、FarmPlotHarvestedEvent、AffinityChangedEvent、AffinityThresholdReachedEvent、CGUnlockedEvent 等 15 個事件） |
| `Assets/Game/Scripts/Village/StorageManager.cs` | 倉庫物品庫存管理器（格子+堆疊制，初始 100 格，支援 ExpandCapacity；保留舊字串 API 相容 QuestManager/GiftManager/FarmManager） |
| `Assets/Game/Scripts/Village/BackpackSlot.cs` | 背包格子資料結構（struct） |
| `Assets/Game/Scripts/Village/BackpackSnapshot.cs` | 背包快照（不可變，用於死亡回溯） |
| `Assets/Game/Scripts/Village/BackpackManager.cs` | 格子制背包管理器（容量限制、堆疊邏輯、快照/回溯） |
| `Assets/Game/Scripts/Village/StorageTransferManager.cs` | 背包與倉庫雙向物品轉移管理器 |
| `Assets/Game/Scripts/Village/QuestData.cs` | 任務資料結構 |
| `Assets/Game/Scripts/Village/QuestManager.cs` | 任務管理器 |
| `Assets/Game/Scripts/Village/VillageProgressionManager.cs` | 村莊解鎖進度管理器 |
| `Assets/Game/Scripts/Village/StorageExpansionConfigData.cs` | 倉庫擴建配置 JSON DTO（StorageExpansionConfigData/StorageExpansionStageData）與不可變配置物件（StorageExpansionConfig/StorageExpansionStage） |
| `Assets/Game/Scripts/Village/StorageExpansionManager.cs` | 倉庫擴建狀態機與流程管理器（Idle/InProgress/Completed 狀態機、Tick 倒數、物資扣除先背包後倉庫、事件 StorageExpansionStarted/Completed/CapacityChanged） |
| `Assets/Game/Scripts/Village/InitialResourcesConfigData.cs` | 初始資源發放配置 JSON DTO（InitialResourcesConfigData/InitialResourceGrantData）與不可變配置物件（InitialResourcesConfig/InitialResourceGrant）+ InitialResourcesTriggerIds 常數 |
| `Assets/Game/Scripts/Village/InitialResourceDispatcher.cs` | 初始資源發放器（IInitialResourceDispatcher 實作，先背包後倉庫邏輯） |
| `Assets/Game/Scripts/Village/MainQuestConfigData.cs` | 前期主線任務配置 JSON DTO（MainQuestConfigData/MainQuestConfigEntry）與不可變配置物件（MainQuestConfig/MainQuestInfo）+ MainQuestCompletionTypes 常數 |
| `Assets/Game/Scripts/Village/MainQuestManager.cs` | 前期主線任務管理器（Locked/Available/InProgress/Completed 狀態機、NotifyCompletionSignal、TryAutoCompleteFirstAutoQuest、事件 MainQuestAvailable/Started/Completed） |
| `Assets/Game/Scripts/Village/CharacterUnlockManager.cs` | 角色 Hub 按鈕解鎖管理器（VN 選項監聽、守衛事件監聽、探索功能解鎖、grant 派發）+ NodeDialogueBranchIds + IInitialResourceDispatcher 介面 |
| `Assets/Game/Scripts/Village/VillageNavigationManager.cs` | 村莊導航管理器 |
| `Assets/Game/Scripts/Village/ExplorationEntryManager.cs` | 探索進入管理器（監聽 ExplorationCompletedEvent 自動結束探索，支援 Dispose；V6 B10 新增 IExplorationDepartureInterceptor 攔截器介面供守衛歸來事件攔截首次探索） |
| `Assets/Game/Scripts/Village/VillageEntryPoint.cs` | 村莊場景進入點（MonoBehaviour，組裝所有模組） |
| `Assets/Game/Scripts/Village/ItemTypes.cs` | 物品分類常數定義（Seed、Ingredient、Food、Potion、Material、Other） |
| `Assets/Game/Scripts/Village/ItemTypeResolver.cs` | 物品分類解析器（Register/GetItemType/IsType/GetItemsByType） |
| `Assets/Game/Scripts/Village/SeedData.cs` | 種子資料結構（SeedItemId、HarvestItemId、GrowthDurationSeconds） |
| `Assets/Game/Scripts/Village/FarmPlot.cs` | 農田格子 readonly struct（Empty、IsEmpty、IsReadyToHarvest、GetRemainingSeconds） |
| `Assets/Game/Scripts/Village/ITimeProvider.cs` | 時間提供者介面（GetCurrentTimestampUtc），供 FarmManager 取得可替換的時間來源 |
| `Assets/Game/Scripts/Village/SystemTimeProvider.cs` | 系統時間提供者（ITimeProvider 實作，回傳 DateTimeOffset.UtcNow） |
| `Assets/Game/Scripts/Village/FarmManager.cs` | 農田管理器（Plant/Harvest/HarvestAll）與相關 enum/Result 類別（PlantError、PlantResult、HarvestError、HarvestResult、HarvestAllResult）— B5 階段與 CommissionManager 共存，B11 UI 統合後移除 |
| `Assets/Game/Scripts/Village/CommissionRecipesConfigData.cs` | 委託配方配置 JSON DTO（CommissionRecipesConfigData/CommissionRecipeEntry）與不可變配置物件（CommissionRecipesConfig/CommissionRecipeInfo），提供 GetRecipe/GetRecipesByCharacter/GetRecipeByInputItem/CanCharacterProcessItem/GetWorkbenchSlotCount |
| `Assets/Game/Scripts/Village/CommissionManager.cs` | 委託系統管理器（StartCommission/ClaimCommission/GetSlot/GetSlots/Tick）與相關型別（StartCommissionResult/Error、ClaimCommissionResult/Error、CommissionSlotState、CommissionSlotInfo）；B5 Sprint 4 新增 |
| `Assets/Game/Scripts/Village/CommissionInteractionPresenter.cs` | 委託互動表示器（純邏輯 IDisposable，連接 CommissionManager 事件與 CharacterInteractionView 委託狀態；B12 Sprint 4） |
| `Assets/Game/Scripts/Village/RedDotManager.cs` | 紅點 4 層管理器（L1 委託完成 / L2 角色發問 / L3 新任務 / L4 主線事件，優先序 L1>L4>L3>L2；訂閱 Commission/Affinity/MainQuest/CharacterUnlock 事件；發布 RedDotUpdatedEvent）+ HubRedDotInfo struct；B7 Sprint 4 |
| `Assets/Game/Scripts/Village/CharacterIntroConfigData.cs` | 角色登場 CG + 短劇情配置 JSON DTO（CharacterIntroConfigData/CharacterIntroData/CharacterIntroLineData）與不可變配置物件（CharacterIntroConfig/CharacterIntroInfo）+ CharacterIntroLineTypes 常數；B9 Sprint 4 |
| `Assets/Game/Scripts/Village/ICGPlayer.cs` | 登場 CG 播放介面（PlayIntroCG，B9 預留，B13 實作真正 CG 播放） |
| `Assets/Game/Scripts/Village/PlaceholderCGPlayer.cs` | ICGPlayer 的 IT 階段 placeholder 實作（立即完成、發布 CGPlaybackStartedEvent/CompletedEvent 供 UI 預留偵聽）；B9 Sprint 4 |
| `Assets/Game/Scripts/Village/NodeDialogueController.cs` | 節點 0/1/2 劇情對話播放控制器（協調 DialogueManager + NodeDialogueConfig，播放 intro_lines → present choices → response → 完成；發布 NodeDialogueStarted/Completed）；B9 Sprint 4 |
| `Assets/Game/Scripts/Village/OpeningSequenceController.cs` | 開場劇情演出控制器（協調 ICGPlayer + NodeDialogueController，播放村長夫人登場 CG → 節點 0 對話；發布 OpeningSequenceStarted/Completed）；B9 Sprint 4 |
| `Assets/Game/Scripts/Village/GuardReturnConfigData.cs` | 守衛歸來事件劇情配置 JSON DTO（GuardReturnConfigData/GuardReturnLineData）與不可變配置物件（GuardReturnConfig）+ GuardReturnPhaseIds 常數（alert/clarify/sheathe/gift_sword/closing）；B10 Sprint 4 |
| `Assets/Game/Scripts/Village/GuardReturnEventController.cs` | 守衛歸來事件控制器（一次性觸發，協調 ICGPlayer + DialogueManager + 31 行劇情；發布 GuardReturnEventStarted/Completed；完成後由 CharacterUnlockManager 訂閱處理解鎖+贈劍）；B10 Sprint 4 |
| `Assets/Game/Scripts/Village/AffinityManager.cs` | 好感度管理器（GetAffinity/AddAffinity/GetThresholds/GetReachedThresholds，門檻達成事件發布） |
| `Assets/Game/Scripts/Village/AffinityConfigData.cs` | 好感度配置 JSON DTO（AffinityConfigData、AffinityCharacterConfigData）與不可變配置物件（AffinityConfig） |
| `Assets/Game/Scripts/Village/GiftManager.cs` | 送禮業務邏輯管理器（GiveGift：扣物品先背包後倉庫→加好感度），含 GiftResult、GiftError |
| `Assets/Game/Scripts/Village/CGSceneConfigData.cs` | CG 場景配置 JSON DTO（CGSceneConfigEntry、CGSceneConfigData）與不可變配置物件（CGSceneInfo、CGSceneConfig） |
| `Assets/Game/Scripts/Village/CGUnlockManager.cs` | CG 解鎖管理器（監聽 AffinityThresholdReachedEvent、session 記憶體 HashSet、Dispose 模式） |
| `Assets/Game/Scripts/Village/ResourcesCGProvider.cs` | IT 階段 CG 圖片載入器（ICGProvider 實作，Resources/CG/ 載入或生成 placeholder） |
| `Assets/Game/Scripts/Village/HCGDialogueSetup.cs` | HCG 劇情播放整合層（KGC DialogueManager + GameStaticDataManager，IT 硬編碼 4 角色對話） |
| `Assets/Game/Scripts/Village/CharacterIntroCGPlayer.cs` | ICGPlayer 真實實作（B13）：從 CharacterIntroConfig 取 intro → Resources.Load CG Sprite → Instantiate CharacterIntroCGView → 播放完成後銷毀 View + 發布 CompletedEvent；only-once 旗標由 VillageEntryPoint 以 session HashSet 管理 |
| `Assets/Game/Scripts/Village/PlayerQuestionsConfigData.cs` | 玩家發問配置 JSON DTO（PlayerQuestionsConfigData/PlayerQuestionData）與不可變配置物件（PlayerQuestionsConfig/PlayerQuestionInfo）；API：GetQuestionsForCharacter / GetUnlockedQuestions(charId, stage) / GetQuestion(id)；B14 Sprint 4 |
| `Assets/Game/Scripts/Village/GuardFirstMeetDialogueConfigData.cs` | 守衛首次進入取劍對白配置 JSON DTO（GuardFirstMeetDialogueConfigData）與不可變配置物件（GuardFirstMeetDialogueConfig），DialogueLines IReadOnlyList<string>，含 fallback；F12 Sprint 6 決策 6-13 |
| `Assets/Game/Scripts/Village/CharacterIdSnakeCaseMapper.cs` | Sprint 5：JSON snake_case → CharacterIds 常數 (PascalCase) 映射器（內部使用） |
| `Assets/Game/Scripts/Village/CharacterQuestionCountdownManager.cs` | Sprint 5 B1：角色發問倒數管理器（純邏輯，60s 倒數/角色、工作中暫停、紅點上限 1、發布 CharacterQuestionCountdownReadyEvent） |
| `Assets/Game/Scripts/Village/CharacterQuestionsConfigData.cs` | Sprint 5 B4：角色發問 280 題配置 JSON DTO 與不可變 Config（依 character/level 索引、個性 +0/+2/+5/+10 增量對應、snake_case/PascalCase 雙映射） |
| `Assets/Game/Scripts/Village/CharacterQuestionsManager.cs` | Sprint 5 B5：角色發問純邏輯（抽未看過題、標記已看、SubmitAnswer 扣好感度、發布 Asked/Answered 事件） |
| `Assets/Game/Scripts/Village/DialogueCooldownManager.cs` | Sprint 5 B10：玩家發問 CD 管理器（純邏輯，60s 基礎、工作中 ×2 規則層倍率、發布 Started/Completed 事件） |
| `Assets/Game/Scripts/Village/IdleChatConfigData.cs` | Sprint 5 B12：閒聊問題池配置（4 角色 × 20 題 × 3 回答） |
| `Assets/Game/Scripts/Village/IdleChatPresenter.cs` | Sprint 5 B12：閒聊觸發純邏輯（隨機題 + 隨機回答，不影響好感度/不累計已看、發布 IdleChatTriggeredEvent） |
| `Assets/Game/Scripts/Village/PlayerQuestionsManager.cs` | Sprint 5 B11：玩家發問純邏輯（剩餘題目規則：≥4 抽 4 / 1~3 顯剩餘 / 0 IdleChatFallback，MarkSeen 標記） |
| `Assets/Game/Scripts/Village/CharacterStaminaManager.cs` | Sprint 5 B13：角色體力管理器（純邏輯，扣/恢復，placeholder Max=10、每次發問扣 1） |
| `Assets/Game/Scripts/Village/GreetingConfigData.cs` | Sprint 5 B15：招呼語配置（4 角色 × 7 級 × 10 句 = 280 句） |
| `Assets/Game/Scripts/Village/GreetingPresenter.cs` | Sprint 5 B16：招呼語純邏輯（進入 Normal 狀態時抽句、L1/L4 紅點壓制、L2/L3 仍播，發布 GreetingPlayedEvent） |

### 村莊 UI（Assets/Game/Scripts/Village/UI）
| 檔案 | 用途 |
|------|------|
| `Assets/Game/Scripts/Village/UI/CharacterIntroCGView.cs` | 登場 CG + 短劇情播放 View（全螢幕 overlay，上半 CG / 下半打字機對話框 / 點擊全螢幕推進）；B13 Sprint 4 |
| `Assets/Game/Scripts/Village/UI/PlayerQuestionsView.cs` | 玩家主動發問 overlay View（右側 w=1600 / 題目清單依好感度解鎖 / 點題目→打字機播回答→返回清單）；Sprint 5 重寫：剩餘題目規則+[閒聊] fallback+體力接線+CD 接線+「現在好累了」 tired panel |
| `Assets/Game/Scripts/Village/UI/CharacterQuestionsView.cs` | Sprint 5 B6：角色發問 overlay View（打字機 Prompt + 四選項 UI 只顯示文字不顯示 +N 數值 + 選後 response 打字機 + 清 L2 紅點與 ClearReady） |
| `Assets/Game/Scripts/Village/UI/ViewBase.cs` | UGUI View 抽象基類（Show/Hide 管理） |
| `Assets/Game/Scripts/Village/UI/ViewController.cs` | 管理 View 顯示切換的控制器（排他式，無歷史紀錄） |
| `Assets/Game/Scripts/Village/UI/ViewStackController.cs` | 支援 Back 返回與 Prefab Clone 加載的 View 控制器 |
| `Assets/Game/Scripts/Village/UI/VillageHubView.cs` | 村莊主畫面（Hub），顯示角色按鈕（村長夫人、守衛、魔女、農女） |
| `Assets/Game/Scripts/Village/UI/CharacterInteractionView.cs` | 角色互動畫面（立繪區、對話區、功能選單、overlay 容器） |
| `Assets/Game/Scripts/Village/UI/TypewriterEffect.cs` | 打字機效果元件（逐字顯示 TMP_Text，支援跳過） |
| `Assets/Game/Scripts/Village/UI/StorageAreaView.cs` | 倉庫畫面，顯示庫存物品清單（支援 overlay 模式） |
| `Assets/Game/Scripts/Village/UI/ExplorationAreaView.cs` | 探索入口畫面，提供出發按鈕（出發後由 VillageEntryPoint 處理切換） |
| `Assets/Game/Scripts/Village/UI/AlchemyAreaView.cs` | 煉金工坊畫面（IT 階段 Placeholder） |
| `Assets/Game/Scripts/Village/UI/FarmAreaView.cs` | 農場畫面（農田格子顯示、種植/收穫互動、種子選擇面板） |
| `Assets/Game/Scripts/Village/UI/GiftAreaView.cs` | 送禮畫面（合併背包+倉庫物品清單、好感度顯示、門檻達成回饋，overlay 模式） |
| `Assets/Game/Scripts/Village/UI/CGGalleryView.cs` | CG 回憶圖鑑 overlay View（已解鎖場景清單、點擊重播 HCG 劇情） |
| `Assets/Game/Scripts/Village/UI/CraftSlotWidget.cs` | 格子式工作台單 Slot Widget（Idle/InProgress/Completed 三種狀態顯示；B11 Sprint 4） |
| `Assets/Game/Scripts/Village/UI/CraftWorkbenchView.cs` | 格子式工作台主 View（動態生成 CraftSlotWidget、訂閱 CommissionTick/Completed/Claimed；B11 Sprint 4） |
| `Assets/Game/Scripts/Village/UI/CraftItemSelectorView.cs` | 物品選擇 Overlay（過濾可用物品、空手委託支援；B11 Sprint 4） |

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
| `Assets/Game/Resources/Config/combat-config.json` | 玩家初始數值（HP/ATK/DEF/SPD）、劍攻擊參數（角度、範圍、冷卻）、移動速度參數 |
| `Assets/Game/Resources/Config/monster-config.json` | 魔物種類定義（Slime、Bat：HP/ATK/DEF/SPD、移動冷卻、視野、攻擊參數、顏色） |
| `Assets/Game/Resources/Config/affinity-config.json` | 好感度門檻配置（各角色門檻值陣列，IT 階段四角色各 [5]） |
| `Assets/Game/Resources/Config/cg-scene-config.json` | CG 場景配置（角色對應 CG 場景 ID、門檻值、對話 ID、顯示名稱） |
| `Assets/Game/Resources/Config/commission-recipes-config.json` | 委託配方表（A7 placeholder：農女 3 + 魔女 3 + 守衛 3 條配方，含輸入/產出/時間/格子上限） |
| `Assets/Game/Resources/Config/storage-expansion-config.json` | 倉庫擴建階段表（A6 placeholder：5 級擴建 100→350，物資與等待時間遞增曲線） |
| `Assets/Game/Resources/Config/initial-resources-config.json` | 初始資源配置表（A4 placeholder：節點 0 空背包、農女解鎖/魔女解鎖/守衛歸來事件贈送物） |
| `Assets/Game/Resources/Config/gift-sword-config.json` | 贈劍屬性表（A4-3 placeholder：木劍 ATK+3，守衛歸來事件贈送） |
| `Assets/Game/Resources/Config/character-intro-config.json` | 角色登場 CG + 短劇情（A1 placeholder：4 位角色場景描述 + 對話行，village_chief_wife/farm_girl/witch/guard） |
| `Assets/Game/Resources/Config/player-questions-config.json` | 玩家發問配置（B14 placeholder：28 題，VCW 12/農女 9/魔女 9/守衛 0（F12 決策 6-13 移除 guard_ask_sword），分 stage 0/1/2 三批解鎖，待製作人撰寫正式回答） |
| `Assets/Game/Resources/Config/guard-first-meet-dialogue-config.json` | 守衛首次進入取劍對白配置（F12 Sprint 6 決策 6-13，placeholder 2 行，待製作人撰寫正式台詞） |
| `Assets/Game/Resources/Config/character-questions-config.json` | 角色發問配置（Sprint 5 A1~A3 placeholder：4 個性類型定義 personality_gentle/lively/calm/assertive、4 角色個性偏好對應、280 題角色發問 4 角色 × 7 級 × 10 題 × 4 個性選項；所有文字程式化 placeholder 待製作人後續撰寫） |
| `Assets/Game/Resources/Config/greeting-config.json` | 招呼語配置（Sprint 5 A4 placeholder：280 句 4 角色 × 7 級 × 10 句；進入角色互動畫面自動播放、L1/L4 紅點亮時跳過、L2/L3 仍播放） |
| `Assets/Game/Resources/Config/idle-chat-config.json` | [閒聊] 問題池配置（Sprint 5 A5 placeholder：4 角色 × 20 題 × 3 回答；玩家 40 題池耗盡後觸發的隨機問答 fallback） |
| `Assets/Game/Resources/CG/` | 登場 CG Sprite 存放目錄（IT 階段空目錄，找不到 Sprite 時顯示深紫色 placeholder 色塊） |
| `Assets/Game/Resources/Config/node-dialogue-config.json` | 節點 0/1/2 劇情對話（A2 placeholder：村長夫人三節點對話、VN 選項、選擇後回應；choice_branch 標記農女/魔女分支） |
| `Assets/Game/Resources/Config/guard-return-config.json` | 守衛歸來事件劇情（A3 placeholder：純劇情演出 31 行，五 phase：alert/clarify/sheathe/gift_sword/closing） |
| `Assets/Game/Resources/Config/main-quest-config.json` | 主線任務序列 T0~T4（A5 placeholder：5 個任務，名稱/描述/完成條件/獎勵 grant_id/解鎖事件） |

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
| `Assets/Tests/Editor/Village/GuardReturnConfigTests.cs` | GuardReturnConfig 單元測試（6 個：null 檢查、排序、GetAllLineTexts、GetLinesByPhase）；B10 Sprint 4 |
| `Assets/Tests/Editor/Village/GuardReturnEventControllerTests.cs` | GuardReturnEventController 單元測試（12 個：建構驗證、一次性觸發、CG→對話→完成事件流程、空 config 立即完成、Dispose；使用 FakeCGPlayer）；B10 Sprint 4 |
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
| `Assets/Tests/Editor/Village/Integration/GuardFirstMeetDialogueIntegrationTest.cs` | F12 Sprint 6 決策 6-13 整合測試（T1~T5b：首次進入觸發邏輯、callback 發劍+ExplorationGateReopenedEvent、重複保護、T2 production path、特殊題不在清單中） |
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
