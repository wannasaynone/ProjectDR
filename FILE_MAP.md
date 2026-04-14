# ProjectDR - 文件地圖

## GDD 設計文件

| 檔案 | 用途 |
|------|------|
| `gdd/game-concept.md` | 遊戲概念文件（專案最高層級總覽） |
| `gdd/core-definition.md` | 核心定義文件（核心循環、核心樂趣、驗證標準、目標受眾、開發環境） |
| `gdd/world-setting.md` | 世界觀設定文件（世界觀背景、村莊與森林設定、玩家角色、探索系統規則） |
| `gdd/characters.md` | 角色設定文件（角色總覽：背景、個性、對應村莊功能） |

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
| `Assets/Game/Scripts/Village/VillageEvents.cs` | 村莊系統事件類別定義（AreaUnlockedEvent 等 8 個事件） |
| `Assets/Game/Scripts/Village/StorageManager.cs` | 物品庫存管理器 |
| `Assets/Game/Scripts/Village/QuestData.cs` | 任務資料結構 |
| `Assets/Game/Scripts/Village/QuestManager.cs` | 任務管理器 |
| `Assets/Game/Scripts/Village/VillageProgressionManager.cs` | 村莊解鎖進度管理器 |
| `Assets/Game/Scripts/Village/VillageNavigationManager.cs` | 村莊導航管理器 |
| `Assets/Game/Scripts/Village/ExplorationEntryManager.cs` | 探索進入管理器（IT 階段版本） |
| `Assets/Game/Scripts/Village/VillageEntryPoint.cs` | 村莊場景進入點（MonoBehaviour，組裝所有模組） |

### 村莊 UI（Assets/Game/Scripts/Village/UI）
| 檔案 | 用途 |
|------|------|
| `Assets/Game/Scripts/Village/UI/ViewBase.cs` | UGUI View 抽象基類（Show/Hide 管理） |
| `Assets/Game/Scripts/Village/UI/ViewController.cs` | 管理 View 顯示切換的控制器（排他式，無歷史紀錄） |
| `Assets/Game/Scripts/Village/UI/ViewStackController.cs` | 支援 Back 返回與 Prefab Clone 加載的 View 控制器 |
| `Assets/Game/Scripts/Village/UI/VillageHubView.cs` | 村莊主畫面（Hub），顯示可導航區域按鈕 |
| `Assets/Game/Scripts/Village/UI/StorageAreaView.cs` | 倉庫畫面，顯示庫存物品清單 |
| `Assets/Game/Scripts/Village/UI/ExplorationAreaView.cs` | 探索入口畫面，提供出發/返回控制 |
| `Assets/Game/Scripts/Village/UI/AlchemyAreaView.cs` | 煉金工坊畫面（IT 階段 Placeholder） |
| `Assets/Game/Scripts/Village/UI/FarmAreaView.cs` | 農場畫面（IT 階段 Placeholder） |

## UI Prefab（Assets/Game/Prefabs）

| 檔案 | 用途 |
|------|------|
| `Assets/Game/Prefabs/VillageHubView.prefab` | 村莊主畫面 UGUI Prefab |
| `Assets/Game/Prefabs/StorageAreaView.prefab` | 倉庫畫面 UGUI Prefab |
| `Assets/Game/Prefabs/ExplorationAreaView.prefab` | 探索入口畫面 UGUI Prefab |
| `Assets/Game/Prefabs/AlchemyAreaView.prefab` | 煉金工坊畫面 UGUI Prefab（Placeholder） |
| `Assets/Game/Prefabs/FarmAreaView.prefab` | 農場畫面 UGUI Prefab（Placeholder） |
| `Assets/Game/Prefabs/AreaButton.prefab` | 區域導航按鈕模板（VillageHubView 動態生成用） |
| `Assets/Game/Prefabs/ItemRow.prefab` | 物品列模板（StorageAreaView 動態生成用） |

## 測試程式碼（Tests）

| 檔案 | 用途 |
|------|------|
| `Assets/Tests/Editor/Village/Game.Tests.asmdef` | 村莊模組測試 Assembly Definition（引用 Game） |
| `Assets/Tests/Editor/Village/VillageProgressionManagerTests.cs` | VillageProgressionManager 單元測試（11 個） |
| `Assets/Tests/Editor/Village/VillageNavigationManagerTests.cs` | VillageNavigationManager 單元測試（14 個） |
| `Assets/Tests/Editor/Village/StorageManagerTests.cs` | StorageManager 單元測試（19 個） |
| `Assets/Tests/Editor/Village/QuestManagerTests.cs` | QuestManager 單元測試（16 個） |
| `Assets/Tests/Editor/Village/ExplorationEntryManagerTests.cs` | ExplorationEntryManager 單元測試（15 個） |
