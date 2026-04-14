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
| `gdd/world-setting.md` | 世界觀設定文件（世界觀背景、村莊與森林設定、玩家角色、探索系統規則） |
| `gdd/characters.md` | 角色設定文件（角色總覽：背景、個性、對應村莊功能） |
| `gdd/character-interaction.md` | 角色互動系統（互動流程、畫面佈局、打字機效果、功能選單、懸浮覆蓋） |
| `gdd/village-economy.md` | 村莊經濟系統（物品分類、農田、通用製作、贈禮效果、角色功能對應、物品流向圖） |

## 敘事文件（narrative）

| 檔案 | 用途 |
|------|------|
| `gdd/narrative/village-chief-wife/character-spec.md` | 村長夫人角色設定 |
| `gdd/narrative/hunter/character-spec.md` | 獵人角色設定 |
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
| `Assets/Game/Scripts/Village/CharacterIds.cs` | 角色 ID 常數定義（VillageChiefWife、Hunter、Witch、FarmGirl） |
| `Assets/Game/Scripts/Village/DialogueData.cs` | 對話資料結構（儲存對話行文字陣列） |
| `Assets/Game/Scripts/Village/DialogueManager.cs` | 對話播放狀態管理器（純邏輯，管理對話行推進與事件發布） |
| `Assets/Game/Scripts/Village/CharacterMenuData.cs` | 角色功能選單資料（角色 ID、顯示名稱、對話、功能清單） |
| `Assets/Game/Scripts/Village/VillageEvents.cs` | 村莊系統事件類別定義（AreaUnlockedEvent、DialogueStartedEvent、DialogueCompletedEvent、FarmPlotPlantedEvent、FarmPlotHarvestedEvent 等 12 個事件） |
| `Assets/Game/Scripts/Village/StorageManager.cs` | 倉庫物品庫存管理器（無容量上限） |
| `Assets/Game/Scripts/Village/BackpackSlot.cs` | 背包格子資料結構（struct） |
| `Assets/Game/Scripts/Village/BackpackSnapshot.cs` | 背包快照（不可變，用於死亡回溯） |
| `Assets/Game/Scripts/Village/BackpackManager.cs` | 格子制背包管理器（容量限制、堆疊邏輯、快照/回溯） |
| `Assets/Game/Scripts/Village/StorageTransferManager.cs` | 背包與倉庫雙向物品轉移管理器 |
| `Assets/Game/Scripts/Village/QuestData.cs` | 任務資料結構 |
| `Assets/Game/Scripts/Village/QuestManager.cs` | 任務管理器 |
| `Assets/Game/Scripts/Village/VillageProgressionManager.cs` | 村莊解鎖進度管理器 |
| `Assets/Game/Scripts/Village/VillageNavigationManager.cs` | 村莊導航管理器 |
| `Assets/Game/Scripts/Village/ExplorationEntryManager.cs` | 探索進入管理器（IT 階段版本） |
| `Assets/Game/Scripts/Village/VillageEntryPoint.cs` | 村莊場景進入點（MonoBehaviour，組裝所有模組） |
| `Assets/Game/Scripts/Village/ItemTypes.cs` | 物品分類常數定義（Seed、Ingredient、Food、Potion、Material、Other） |
| `Assets/Game/Scripts/Village/ItemTypeResolver.cs` | 物品分類解析器（Register/GetItemType/IsType/GetItemsByType） |
| `Assets/Game/Scripts/Village/SeedData.cs` | 種子資料結構（SeedItemId、HarvestItemId、GrowthDurationSeconds） |
| `Assets/Game/Scripts/Village/FarmPlot.cs` | 農田格子 readonly struct（Empty、IsEmpty、IsReadyToHarvest、GetRemainingSeconds） |
| `Assets/Game/Scripts/Village/ITimeProvider.cs` | 時間提供者介面（GetCurrentTimestampUtc），供 FarmManager 取得可替換的時間來源 |
| `Assets/Game/Scripts/Village/SystemTimeProvider.cs` | 系統時間提供者（ITimeProvider 實作，回傳 DateTimeOffset.UtcNow） |
| `Assets/Game/Scripts/Village/FarmManager.cs` | 農田管理器（Plant/Harvest/HarvestAll）與相關 enum/Result 類別（PlantError、PlantResult、HarvestError、HarvestResult、HarvestAllResult） |

### 村莊 UI（Assets/Game/Scripts/Village/UI）
| 檔案 | 用途 |
|------|------|
| `Assets/Game/Scripts/Village/UI/ViewBase.cs` | UGUI View 抽象基類（Show/Hide 管理） |
| `Assets/Game/Scripts/Village/UI/ViewController.cs` | 管理 View 顯示切換的控制器（排他式，無歷史紀錄） |
| `Assets/Game/Scripts/Village/UI/ViewStackController.cs` | 支援 Back 返回與 Prefab Clone 加載的 View 控制器 |
| `Assets/Game/Scripts/Village/UI/VillageHubView.cs` | 村莊主畫面（Hub），顯示角色按鈕（村長夫人、獵人、魔女、農女） |
| `Assets/Game/Scripts/Village/UI/CharacterInteractionView.cs` | 角色互動畫面（立繪區、對話區、功能選單、overlay 容器） |
| `Assets/Game/Scripts/Village/UI/TypewriterEffect.cs` | 打字機效果元件（逐字顯示 TMP_Text，支援跳過） |
| `Assets/Game/Scripts/Village/UI/StorageAreaView.cs` | 倉庫畫面，顯示庫存物品清單（支援 overlay 模式） |
| `Assets/Game/Scripts/Village/UI/ExplorationAreaView.cs` | 探索入口畫面，提供出發/返回控制 |
| `Assets/Game/Scripts/Village/UI/AlchemyAreaView.cs` | 煉金工坊畫面（IT 階段 Placeholder） |
| `Assets/Game/Scripts/Village/UI/FarmAreaView.cs` | 農場畫面（農田格子顯示、種植/收穫互動、種子選擇面板） |

## UI Prefab（Assets/Game/Prefabs）

| 檔案 | 用途 |
|------|------|
| `Assets/Game/Prefabs/VillageHubView.prefab` | 村莊主畫面 UGUI Prefab |
| `Assets/Game/Prefabs/CharacterInteractionView.prefab` | 角色互動畫面 UGUI Prefab（立繪、對話、功能選單、overlay 容器） |
| `Assets/Game/Prefabs/StorageAreaView.prefab` | 倉庫畫面 UGUI Prefab（雙欄佈局：背包+倉庫） |
| `Assets/Game/Prefabs/ExplorationAreaView.prefab` | 探索入口畫面 UGUI Prefab |
| `Assets/Game/Prefabs/AlchemyAreaView.prefab` | 煉金工坊畫面 UGUI Prefab（Placeholder） |
| `Assets/Game/Prefabs/FarmAreaView.prefab` | 農場畫面 UGUI Prefab（Placeholder） |
| `Assets/Game/Prefabs/AreaButton.prefab` | 區域導航按鈕模板（VillageHubView 動態生成用） |
| `Assets/Game/Prefabs/BackpackSlotRow.prefab` | 背包格子行模板（StorageAreaView 背包欄動態生成用） |
| `Assets/Game/Prefabs/WarehouseItemRow.prefab` | 倉庫物品行模板（StorageAreaView 倉庫欄動態生成用） |
| `Assets/Game/Prefabs/FarmPlotUI.prefab` | 農田格子 UI 模板（FarmAreaView 動態生成用） |
| `Assets/Game/Prefabs/SeedItemButton.prefab` | 種子選項按鈕模板（FarmAreaView 種子選擇面板用） |
| `Assets/Game/Prefabs/ItemRow.prefab` | 物品列模板（舊版，保留備用） |

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
