# ProjectDR - 文件地圖

## 專案管理

| 檔案 | 用途 |
|------|------|
| `project-status.md` | 專案狀態檔（當前階段、階段焦點、活躍 Sprint、待決事項、已知阻塞） |

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
| `gdd/base-management.md` | 基地經營系統（NPC 導向架構、互動選單結構、設施框架、升級機制、村莊等級、解鎖機制、系統接口） |
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
| `Assets/Game/Scripts/Village/DialogueData.cs` | 對話資料結構（儲存對話行文字陣列） |
| `Assets/Game/Scripts/Village/DialogueManager.cs` | 對話播放狀態管理器（純邏輯，管理對話行推進與事件發布） |
| `Assets/Game/Scripts/Village/CharacterMenuData.cs` | 角色功能選單資料（角色 ID、顯示名稱、對話、功能清單） |
| `Assets/Game/Scripts/Village/VillageEvents.cs` | 村莊系統事件類別定義（AreaUnlockedEvent、DialogueStartedEvent、DialogueCompletedEvent、FarmPlotPlantedEvent、FarmPlotHarvestedEvent、AffinityChangedEvent、AffinityThresholdReachedEvent、CGUnlockedEvent 等 15 個事件） |
| `Assets/Game/Scripts/Village/StorageManager.cs` | 倉庫物品庫存管理器（無容量上限） |
| `Assets/Game/Scripts/Village/BackpackSlot.cs` | 背包格子資料結構（struct） |
| `Assets/Game/Scripts/Village/BackpackSnapshot.cs` | 背包快照（不可變，用於死亡回溯） |
| `Assets/Game/Scripts/Village/BackpackManager.cs` | 格子制背包管理器（容量限制、堆疊邏輯、快照/回溯） |
| `Assets/Game/Scripts/Village/StorageTransferManager.cs` | 背包與倉庫雙向物品轉移管理器 |
| `Assets/Game/Scripts/Village/QuestData.cs` | 任務資料結構 |
| `Assets/Game/Scripts/Village/QuestManager.cs` | 任務管理器 |
| `Assets/Game/Scripts/Village/VillageProgressionManager.cs` | 村莊解鎖進度管理器 |
| `Assets/Game/Scripts/Village/VillageNavigationManager.cs` | 村莊導航管理器 |
| `Assets/Game/Scripts/Village/ExplorationEntryManager.cs` | 探索進入管理器（監聽 ExplorationCompletedEvent 自動結束探索，支援 Dispose） |
| `Assets/Game/Scripts/Village/VillageEntryPoint.cs` | 村莊場景進入點（MonoBehaviour，組裝所有模組） |
| `Assets/Game/Scripts/Village/ItemTypes.cs` | 物品分類常數定義（Seed、Ingredient、Food、Potion、Material、Other） |
| `Assets/Game/Scripts/Village/ItemTypeResolver.cs` | 物品分類解析器（Register/GetItemType/IsType/GetItemsByType） |
| `Assets/Game/Scripts/Village/SeedData.cs` | 種子資料結構（SeedItemId、HarvestItemId、GrowthDurationSeconds） |
| `Assets/Game/Scripts/Village/FarmPlot.cs` | 農田格子 readonly struct（Empty、IsEmpty、IsReadyToHarvest、GetRemainingSeconds） |
| `Assets/Game/Scripts/Village/ITimeProvider.cs` | 時間提供者介面（GetCurrentTimestampUtc），供 FarmManager 取得可替換的時間來源 |
| `Assets/Game/Scripts/Village/SystemTimeProvider.cs` | 系統時間提供者（ITimeProvider 實作，回傳 DateTimeOffset.UtcNow） |
| `Assets/Game/Scripts/Village/FarmManager.cs` | 農田管理器（Plant/Harvest/HarvestAll）與相關 enum/Result 類別（PlantError、PlantResult、HarvestError、HarvestResult、HarvestAllResult） |
| `Assets/Game/Scripts/Village/AffinityManager.cs` | 好感度管理器（GetAffinity/AddAffinity/GetThresholds/GetReachedThresholds，門檻達成事件發布） |
| `Assets/Game/Scripts/Village/AffinityConfigData.cs` | 好感度配置 JSON DTO（AffinityConfigData、AffinityCharacterConfigData）與不可變配置物件（AffinityConfig） |
| `Assets/Game/Scripts/Village/GiftManager.cs` | 送禮業務邏輯管理器（GiveGift：扣物品先背包後倉庫→加好感度），含 GiftResult、GiftError |
| `Assets/Game/Scripts/Village/CGSceneConfigData.cs` | CG 場景配置 JSON DTO（CGSceneConfigEntry、CGSceneConfigData）與不可變配置物件（CGSceneInfo、CGSceneConfig） |
| `Assets/Game/Scripts/Village/CGUnlockManager.cs` | CG 解鎖管理器（監聽 AffinityThresholdReachedEvent、PlayerPrefs 持久化、Dispose 模式） |
| `Assets/Game/Scripts/Village/ResourcesCGProvider.cs` | IT 階段 CG 圖片載入器（ICGProvider 實作，Resources/CG/ 載入或生成 placeholder） |
| `Assets/Game/Scripts/Village/HCGDialogueSetup.cs` | HCG 劇情播放整合層（KGC DialogueManager + GameStaticDataManager，IT 硬編碼 4 角色對話） |

### 村莊 UI（Assets/Game/Scripts/Village/UI）
| 檔案 | 用途 |
|------|------|
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
| `Assets/Game/Prefabs/AreaButton.prefab` | 區域導航按鈕模板（VillageHubView 動態生成用） |
| `Assets/Game/Prefabs/BackpackSlotRow.prefab` | 背包格子行模板（StorageAreaView 背包欄動態生成用） |
| `Assets/Game/Prefabs/WarehouseItemRow.prefab` | 倉庫物品行模板（StorageAreaView 倉庫欄動態生成用） |
| `Assets/Game/Prefabs/FarmPlotUI.prefab` | 農田格子 UI 模板（FarmAreaView 動態生成用） |
| `Assets/Game/Prefabs/SeedItemButton.prefab` | 種子選項按鈕模板（FarmAreaView 種子選擇面板用） |
| `Assets/Game/Prefabs/GiftItemRow.prefab` | 送禮物品行模板（GiftAreaView 物品清單動態生成用） |
| `Assets/Game/Prefabs/ItemRow.prefab` | 物品列模板（舊版，保留備用） |

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

## 測試程式碼（Tests）

| 檔案 | 用途 |
|------|------|
| `Assets/Tests/Editor/Village/Game.Tests.asmdef` | 村莊模組測試 Assembly Definition（引用 Game） |
| `Assets/Tests/Editor/Village/VillageProgressionManagerTests.cs` | VillageProgressionManager 單元測試（11 個） |
| `Assets/Tests/Editor/Village/VillageNavigationManagerTests.cs` | VillageNavigationManager 單元測試（14 個） |
| `Assets/Tests/Editor/Village/StorageManagerTests.cs` | StorageManager 單元測試（19 個） |
| `Assets/Tests/Editor/Village/QuestManagerTests.cs` | QuestManager 單元測試（16 個） |
| `Assets/Tests/Editor/Village/BackpackManagerTests.cs` | BackpackManager 單元測試（格子制新增/移除、容量、堆疊、快照/回溯、事件） |
| `Assets/Tests/Editor/Village/StorageTransferManagerTests.cs` | StorageTransferManager 單元測試（雙向轉移、邊界條件） |
| `Assets/Tests/Editor/Village/ExplorationEntryManagerTests.cs` | ExplorationEntryManager 單元測試（V2：戰利品進背包、出發快照） |
| `Assets/Tests/Editor/Village/DialogueManagerTests.cs` | DialogueManager 單元測試（16 個：初始狀態、開始對話、推進、事件發布、邊界） |
| `Assets/Tests/Editor/Village/ItemTypeResolverTests.cs` | ItemTypeResolver 單元測試（14 個：Register/覆寫/例外、GetItemType/例外、IsType、GetItemsByType） |
| `Assets/Tests/Editor/Village/FarmManagerTests.cs` | FarmManager 單元測試（42 個：建構驗證、格子查詢、種植/收穫成功失敗、時間邊界、批次收穫、事件發布） |
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
| `Assets/Tests/Editor/Village/CGUnlockManagerTests.cs` | CGUnlockManager + CGSceneConfig 單元測試（33 個：解鎖邏輯、PlayerPrefs 持久化、事件發布、JSON 反序列化） |
| `Assets/Tests/Editor/Village/Exploration/Combat/DeathManagerTests.cs` | DeathManager 單元測試（13 個：死亡偵測、背包回溯、事件順序、重複觸發防護、Dispose、Reset） |
