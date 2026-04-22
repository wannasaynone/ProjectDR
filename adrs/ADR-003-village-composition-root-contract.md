# ADR-003: Village Composition Root 契約 — IVillageInstaller 介面、VillageContext 共享容器、VillageEntryPoint 瘦身規格

> **狀態**: Accepted
> **提出日期**: 2026-04-22
> **最近更新**: 2026-04-22（DEV-ADR-REVIEW full gate 通過，Proposed → Accepted）
> **提出者**: dev-head（retrofit 既有 VillageEntryPoint 慣例 + Sprint 7 B 類拆分方案）
> **引擎**: Unity 6.0.x（ProjectDR）
> **取代**: —
> **被取代**: —

---

## Context（脈絡）

### 為何需要此決策

ProjectDR 在 Sprint 1~6 期間，村莊場景的組裝邏輯全部集中於單一 `VillageEntryPoint.cs`；至 2026-04-22 體檢時已累積以下結構性病徵：

1. **檔案膨脹至 1590 行**（實測 `wc -l` = 1590）
   - 未做任何合理拆分，全部 manager 的建構、依賴注入、事件訂閱、UI 預製物實例化擠在同一 MonoBehaviour
   - 違反工作室即將在 Sprint 7 C1 建立的「檔案 > 1200 行強警示」量化護欄

2. **單方法 `InitializeManagers()` 270 行**
   - 依依賴順序建構約 30+ 個 manager（StorageManager / BackpackManager / AffinityManager / CGUnlockManager / CommissionManager / NodeDialogueController / OpeningSequenceController / GuardReturnEventController / CharacterQuestionsManager / ... 等）
   - 違反即將建立的「單方法 > 100 行警示」量化護欄
   - 新增任何 manager 的慣性位置為「擠進 InitializeManagers 最末行」，沒有模組邊界來拒絕此慣性

3. **16 對 EventBus Subscribe/Unsubscribe 手動維護於同一檔**（實測 Grep `EventBus.Subscribe` = 16 次）
   - `Start()` / `OnDestroy()` 必須對稱維護 16 對訂閱解除
   - 任一對遺漏即造成記憶體洩漏或跨 scene 重入時的雙訊號
   - 事件類型跨多個功能域（CGUnlocked / CommissionClaimed / CharacterUnlocked / NodeDialogueCompleted / ExplorationDeparted / GuardReturn / MainQuestCompleted / StorageExpansion / OpeningSequence / Navigation），VillageEntryPoint 成為跨模組事件的集散地

4. **驗收流程的結構性缺陷**（2026-04-22 已由新 `/development-flow` Phase 2 治理）
   - 舊驗收流程只查「功能通過 + 測試綠」，不查 SOLID / 重複 / 檔案膨脹 / 單方法行數
   - dev-head 同時擔任實作者與審核者時審核流於自我驗收
   - 結果：2026-04-17 / 04-18 兩日期間 `VillageEntryPoint.cs` 從合理尺寸膨脹至 1590 行而未被任何 gate 攔下

5. **Sprint 7 B 類需要共同契約**
   - Sprint 7 已定案拆分為 6 個 Installer（CoreStorage / Progression / Affinity / CG / Commission / DialogueFlow）
   - 6 個 Installer 必須遵守相同介面、相同初始化規則、相同事件訂閱責任模型，否則拆完仍是 6 個風格不一的小膨脹點
   - ADR-004 Accepted（2026-04-22）已明示 `Core/` 子資料夾為「Village 整體組裝與共用契約（為 ADR-003 Village Composition Root 預留）」，契約此時必須落地

### 當前狀況與問題

- `VillageEntryPoint.cs` 實測 1590 行、`InitializeManagers()` 270 行、`OnDestroy()` ~80 行、`SubscribeToNavigationEvents()` / `SubscribeToExplorationEvents()` 各約 10~40 行
- 約 30+ 個私有 field 儲存 manager 實例（`_storageManager`、`_backpackManager`、`_affinityManager` 等一路到 `_staminaManager`）
- 16 對 `EventBus.Subscribe` / `Unsubscribe` 手動對稱維護
- 場景上只有一個 `VillageEntryPoint` GameObject，沒有中繼層
- 既有單元測試無法獨立驗證「某個子系統的組裝與事件綁定」，因為拆不出

### 相關約束

- **不阻斷 Sprint 7 E 類 ADR-004 搬移**：契約必須能與 ADR-004 21 模組 × 5 型別層結構對齊，即 Installer 必須放 `Village/Core/` 或對應 `<Module>/Core/`
- **不阻斷 Sprint 7 A 類 ConfigData 改 IGameData**：契約的 VillageContext 必須能承載 ADR-001 的 `GameStaticDataManager` 或替代資料存取層
- **不改動 KahaGameCore 框架**：`EventBus`、`GameStaticDataManager` 契約不動
- **scene 仍掛 `VillageEntryPoint` MonoBehaviour**：這是 Unity scene 規範，重構不改此結構
- **先拆後瘦**：不得為追求瘦身而繞過 Installer，導致 VillageContext 膨脹為新「大肥檔」
- **製作人 2026-04-22 拍板**：Sprint 7 範圍 R3 徹底重構、選項 B（Installer 拆分）為架構重構方向

### 相關的 GDD 技術需求（TR-ID）

- `TR-arch-005` Village 組裝必須以 IVillageInstaller 契約分離（新登記）
- `TR-arch-006` Village 跨 Installer 共用服務必須透過 VillageContext 建構器注入（新登記）
- `TR-arch-007` VillageEntryPoint 量化護欄（< 600 行、單方法 < 100 行；2026-04-22 調整，原 < 300 行）（新登記）
- `TR-arch-008` Installer 事件訂閱對稱性：每個 Installer 自管 Subscribe/Unsubscribe（新登記）

---

## Decision（決策）

**一句話**：Village 場景的組裝必須透過 `IVillageInstaller` 介面 + `VillageContext` 共享容器 + 瘦身版 `VillageEntryPoint` 三元契約執行；VillageEntryPoint 僅做「讀 SerializeField → 建 VillageContext → 依固定順序 Install 6 個 Installer → 啟動 Launcher」四件事，每個 Installer 自管自身 manager 的建構與事件訂閱，VillageContext 僅承載跨 Installer 共用的根級服務（非 Service Locator）。

後續展開：

### D1. IVillageInstaller 介面

**位置**：`Village/Core/Interface/IVillageInstaller.cs`
**Namespace**：`ProjectDR.Village.Core`（依 ADR-004 D5）

**最小契約**：

```csharp
namespace ProjectDR.Village.Core
{
    /// <summary>
    /// Village 場景組裝契約。每個 Installer 負責單一功能域的
    /// Manager 建構、服務註冊、事件訂閱與 Tick 驅動。
    /// 同一 Installer 的 Install / Uninstall 必須對稱。
    /// </summary>
    public interface IVillageInstaller
    {
        /// <summary>
        /// 建構 Manager、對 VillageContext 註冊本 Installer 對外公開的服務、
        /// 訂閱本 Installer 需要處理的事件。
        /// 禁止在 Install 內直接引用其他 Installer 的實例。
        /// </summary>
        void Install(VillageContext ctx);

        /// <summary>
        /// 解除 Install 期間建立的所有事件訂閱、釋放資源、Dispose 管理器。
        /// Install 訂閱幾次，Uninstall 就要解除幾次（對稱）。
        /// </summary>
        void Uninstall();
    }
}
```

**設計約束**：

- **Install 唯一參數為 VillageContext**：禁止 Install 收第二個參數（例 `Install(VillageContext, SomeOtherInstaller)`）——跨 Installer 依賴一律透過 VillageContext 暴露
- **Uninstall 不收參數**：Installer 應在 Install 時把需要的釋放句柄存為自身私有 field
- **無 `InstallAsync`**：IT → VS 階段不引入 async 組裝；若未來需要延遲載入，另開 ADR 擴展
- **無 `ITickable` 強制繼承**：Tick 驅動（如 `_commissionManager.Tick`）由需要的 Installer 自行記住 manager 引用，在 `VillageEntryPoint.Update()` 呼叫 Installer 的 `Tick(float dt)` 方法（若該 Installer 有）

**選擇性 Tick 介面**（非強制，需要 Tick 的 Installer 才實作）：

```csharp
namespace ProjectDR.Village.Core
{
    public interface IVillageTickable
    {
        void Tick(float deltaSeconds);
    }
}
```

### D2. VillageContext 共享容器

**位置**：`Village/Core/Data/VillageContext.cs`
**Namespace**：`ProjectDR.Village.Core`

**定位**：**建構器注入（constructor injection）的資料容器**，不是 Service Locator。

**初期欄位清單（保守，僅放確認跨 Installer 共用的根級服務）**：

| # | 欄位 | 型別 | 來源 | 誰會用 |
|---|------|------|------|--------|
| 1 | `Canvas` | `UnityEngine.Canvas` | VillageEntryPoint SerializeField | 需要 spawn UI 的 Installer（CG / DialogueFlow） |
| 2 | `UIContainer` | `UnityEngine.Transform` | VillageEntryPoint SerializeField | 同上 |
| 3 | `ViewStackController` | `ProjectDR.Village.Shared.ViewStackController` | CoreStorageInstaller 建立並註冊 | 所有 Installer 推 / 收 View |
| 4 | `EventBusRef` | `KahaGameCore.GameEvent.EventBus` 包裝或靜態 ref | 靜態全域 | 所有 Installer 訂閱 / 發事件（目前直接用 EventBus.Subscribe，留欄位作為未來切換 DI 的緩衝） |
| 5 | `TimeProvider` | `ProjectDR.Village.TimeProvider.ITimeProvider` | CoreStorageInstaller 建立並註冊 | Farm / Commission / Countdown 等時間相依 Installer |
| 6 | `GameDataAccess` | 委派或介面（見 D2.1） | 靜態參考 `GameStaticDataManager.GetGameData<T>(id)` | 所有消費 IGameData 的 Installer |
| 7 | `VillageProgressionReadOnly` | `IVillageProgressionQuery`（新定義唯讀介面）| ProgressionInstaller 建立並註冊 | 需查角色解鎖的 Installer |
| 8 | `AffinityReadOnly` | `IAffinityQuery`（新定義唯讀介面）| AffinityInstaller 建立並註冊 | 需查好感度的 Installer（CG、DialogueFlow）|

**初期欄位數硬指標：≤ 10**。若實作過程需要第 11 欄，必須在 ADR-003 本檔加註記，並回到製作人評估是否為「真跨 Installer 需求」或可改為 Installer 間事件解耦。

**D2.1 GameDataAccess 的型別**：
為避免 VillageContext 直接耦合到 `GameStaticDataManager` 靜態型別，定義一個最小委派：

```csharp
namespace ProjectDR.Village.Core
{
    /// <summary>
    /// IGameData 查詢委派。VillageContext 透過此委派取得 tabular data，
    /// 不直接依賴 GameStaticDataManager 靜態類別，方便測試替換。
    /// </summary>
    public delegate T GameDataQuery<T>(int id) where T : class, KahaGameCore.GameData.IGameData;
}
```

VillageContext 上放一個 `GameDataQuery<>` 的 field group；實作方式由 dev-agent 在 B3 決定（泛型委派 / 介面 / 泛型方法），只要不讓 Installer 直接 `using static GameStaticDataManager` 即可。

**D2.2 禁用 Service Locator 反模式**：

- **禁止** `VillageContext` 暴露 `object Resolve(Type t)` 或 `T Get<T>()` 型別的泛型查找 API
- **禁止** `VillageContext` 成為 `Dictionary<Type, object>` 的查詢站
- **禁止** 在 Install 內呼叫 `ctx.Resolve<SomeManagerFromOtherInstaller>()`
- **允許**：明示的 readonly 欄位 / property 注入（如 `ctx.ViewStackController`）
- **允許**：唯讀查詢介面的注入（如 `ctx.AffinityReadOnly.GetLevel(characterId)`）

**D2.3 Installer 間不直接引用**：

- **禁止** CGInstaller 在 Install 內直接引用 AffinityInstaller 的 AffinityManager 實例
- **允許** CGInstaller 透過 `ctx.AffinityReadOnly`（由 AffinityInstaller 在 Install 時註冊）間接查詢
- **允許** CGInstaller 透過 EventBus 訂閱 AffinityInstaller 發出的事件
- **允許** 兩個 Installer 都透過 ctx 取得共用服務（ViewStackController、TimeProvider）

### D3. VillageEntryPoint 瘦身規格

**目標行數**：**< 600 行**（硬指標；超過即違反 TR-arch-007）

> **2026-04-22 調整**：原目標 < 300 行在 Sprint 7 B5/B8 實作過程中證實過嚴（585 行已是扣除所有 Installer 可承接邏輯後的 scene 層底線，含 SerializeField 宣告、ViewPrefabRegistry partial 呼叫、跨域事件 handler 等無法再壓縮的 scene 合約）。製作人 2026-04-22 拍板放寬為 < 600 行。原意圖（禁止 InitializeManagers、強制 Installer 分散）不變，僅調整量化指標。

**允許的職責**（只能做這四件事）：

1. **讀 SerializeField**：所有 `TextAsset`、`Prefab`、`Canvas`、`Transform`、float 參數
2. **建 VillageContext**：把 SerializeField 映射為 VillageContext 欄位（或先建某些根服務再組 ctx）
3. **按順序 Install 6 個 Installer**：呼叫 `installer.Install(ctx)`，順序固定（見 D5）
4. **啟動 Launcher**：`TryStartOpeningSequence()` 或等價的遊戲啟動觸發；`Update()` 呼叫需 Tick 的 Installer 的 `Tick(dt)`

**禁止的職責**（任何一項違反即 FAIL）：

- **禁止** `InitializeManagers()` 或等價單方法（實作細節全部移入各 Installer）
- **禁止** 在 VillageEntryPoint 直接 `new XxxManager()`（Manager 建構移入對應 Installer）
- **禁止** 在 VillageEntryPoint 直接 `EventBus.Subscribe<...>`（訂閱移入對應 Installer）
- **禁止** 在 VillageEntryPoint 保留超過 3 個 manager 實例 private field（唯二例外：`_installers` 陣列 + 必要時 `_viewStackController` 作為 Hub view 啟動 shortcut）
- **禁止** 在 VillageEntryPoint 寫任何「事件 handler」（例 `OnCGUnlocked`、`OnCharacterUnlocked*`）—— handler 歸對應 Installer 所有
- **禁止** 為了瘦身而把 InitializeManagers 改名偽裝（例改為 `InitializeLowerLayers`、`SetupSystems`）

**瘦身後的骨架（示意，由 dev-agent 在 B2~B8 實作）**：

```csharp
namespace ProjectDR.Village.Core
{
    public class VillageEntryPoint : MonoBehaviour
    {
        [Header("Canvas / UI")]
        [SerializeField] private Canvas _villageCanvas;
        [SerializeField] private Transform _uiContainer;

        [Header("Configs")]
        [SerializeField] private TextAsset _affinityConfigJson;
        // ... 其他 SerializeField

        private VillageContext _ctx;
        private IVillageInstaller[] _installers;

        private void Start()
        {
            _ctx = BuildContext();
            _installers = BuildInstallers();
            foreach (var inst in _installers) inst.Install(_ctx);
            TryStartOpeningSequence();
        }

        private void OnDestroy()
        {
            if (_installers != null)
                foreach (var inst in _installers) inst.Uninstall();
        }

        private void Update()
        {
            if (_installers == null) return;
            float dt = Time.unscaledDeltaTime;
            foreach (var inst in _installers)
                if (inst is IVillageTickable tickable) tickable.Tick(dt);
        }

        private VillageContext BuildContext() { /* 映射 SerializeField */ }
        private IVillageInstaller[] BuildInstallers() { /* 按 D5 順序建 6 個 */ }
        private void TryStartOpeningSequence() { /* 啟動開場序列 */ }
    }
}
```

### D4. 事件訂閱職責（每 Installer 自管）

**規則**：

1. **每個 Installer 自己的 Install 內 Subscribe**：不論事件來源是本 Installer 發出或其他 Installer 發出，只要是本 Installer 要處理的事件，訂閱寫在本 Installer 的 Install 內
2. **每個 Installer 自己的 Uninstall 內 Unsubscribe**：對稱拆除
3. **事件 handler 方法寫在 Installer 類別內**：例 `OnCGUnlocked` 從 VillageEntryPoint 搬到 CGInstaller
4. **跨 Installer 事件流向透過 EventBus**：不透過 VillageContext 直接引用彼此的 manager
5. **同一事件可被多個 Installer 訂閱**：例 `CharacterUnlockedEvent` 可同時被 ProgressionInstaller（更新進度）與 DialogueFlowInstaller（更新紅點）訂閱，兩個 Installer 各自管自己的 Sub/Unsub

**Sprint 7 B6 對應**：VillageEntryPoint 既有 16 對訂閱完全分散至 6 個 Installer，分配表見「Implementation Guidelines § 事件訂閱分散表」。

### D5. 6 個 Installer 的 Install 順序（固定依賴 graph）

依依賴關係，Install 順序固定為：

| # | Installer | 依賴（來自 ctx 的）| 產出到 ctx 的服務 |
|---|-----------|------------------|------------------|
| 1 | **CoreStorageInstaller** | UIContainer / Canvas | ViewStackController、TimeProvider、StorageManager（read-only query）、BackpackManager（read-only query）、ItemTypeResolver |
| 2 | **ProgressionInstaller** | ViewStackController、GameDataAccess | VillageProgressionReadOnly、CharacterUnlockManager（對外僅透過事件暴露）、NavigationManager（對外僅透過事件暴露）、ExplorationEntryManager（同） |
| 3 | **AffinityInstaller** | GameDataAccess、StorageManager-query、BackpackManager-query | AffinityReadOnly |
| 4 | **CGInstaller** | Canvas、UIContainer、AffinityReadOnly、GameDataAccess | （無新 ctx 欄位；僅訂閱 CGUnlocked、消費 Affinity）|
| 5 | **CommissionInstaller** | TimeProvider、StorageManager-query、BackpackManager-query、GameDataAccess | （無新 ctx 欄位；Tick 類，實作 IVillageTickable）|
| 6 | **DialogueFlowInstaller** | ViewStackController、TimeProvider、AffinityReadOnly、VillageProgressionReadOnly、GameDataAccess | （無新 ctx 欄位；Tick 類）|

**順序鐵律**：

- **每個 Installer 的 Install 只能依賴「編號較小」的 Installer 已註冊到 ctx 的服務**
- **禁止反向依賴**：例 CoreStorageInstaller（#1）不可依賴 Affinity（#3）提供的任何服務
- **禁止橫向依賴**：例 CGInstaller（#4）不可依賴 CommissionInstaller（#5）的 manager 實例（需要時改為事件驅動）
- **若 dev-agent 在 B2~B8 實作時發現反向或橫向依賴**：先回到本 ADR 確認是否應拆重組，若拆重組影響順序表，必須新版本行並通知 dev-head

**初始化阻塞規則**：

- 若 ctx 缺某必要欄位導致後續 Installer 無法 Install，Installer 應 `throw InvalidOperationException` 明示缺什麼，**禁止**靜默降級或用 `new NullObject()` 假裝成功
- 例外：可選的 UI prefab 缺失（例 `_kgcDialogueViewPrefab == null`）可走原有的「本功能失效但其他功能正常」路徑，此情境保留現狀

### D6. VillageContext 的「唯讀查詢介面」設計原則

為避免跨 Installer 誤用「直接改他人 manager 狀態」：

**規則**：

- **只暴露 Query 介面，不暴露 Mutator**
- 例：AffinityReadOnly 只有 `GetLevel(characterId)` / `IsThresholdReached(characterId, level)`，**沒有** `Increase(characterId, amount)`
- 要改 Affinity 狀態的唯一方式：透過 EventBus 發 `AffinityChangeRequestEvent`（或等價）由 AffinityInstaller 處理

**例外：Tick 驅動 Manager**：
CommissionManager / CharacterQuestionCountdownManager 等 Tick 類仍由擁有的 Installer 在 `Tick(dt)` 內直接呼叫 `_mgr.Tick(dt)`，因為 Installer 擁有 manager 引用合法。

### D7. 測試策略

**L1：Installer 單元測試**（每個 Installer 必備）

- 測試 `Install(ctx)` 後：是否對 ctx 註冊預期服務、是否訂閱預期事件
- 測試 `Uninstall()` 後：是否解除所有 Install 時訂閱的事件（Subscribe 次數 == Unsubscribe 次數）
- 測試對稱性：`Install → Uninstall → Install → Uninstall` 可重入不漏
- 測試缺依賴時的明確失敗：給不足 ctx → 預期 `InvalidOperationException`

**L2：Installer 整合測試**（每批 Installer 組合後跑）

- 所有 6 Installer 依序 Install → 每個 ctx 欄位已就位
- 依序 Uninstall → 所有訂閱歸零（可用 EventBus 內部計數或 mock 驗證）
- 發某個事件 → 只有訂閱該事件的 Installer 的 handler 被觸發

**L3：Smoke test**（對應 Sprint 7 E 類 smoke test 清單）

- 完整跑 VillageEntryPoint.Start → 各 Installer Install → OpeningSequence 啟動 → 關鍵玩家動線（Hub → 角色 → 對白 → 探索 → 戰鬥 → 撤離 → 回 Hub）

### D8. 新 Installer 的強制規則

當未來 Sprint 需新增第 7 個 Installer（或為既有 Installer 拆分子 Installer）時：

1. **必須放 `Village/Core/` 或對應模組的 `<Module>/Core/`**（符合 ADR-004 D1）
2. **Namespace 遵循 ADR-004 D5 規則**（`ProjectDR.Village.Core` 或 `ProjectDR.Village.<Module>`）
3. **實作 `IVillageInstaller`**（Install / Uninstall 對稱）
4. **不得跨 Installer 直接引用**（透過 ctx 或事件）
5. **必須加入本 ADR 的「D5 Install 順序表」**，並更新版本行
6. **必須加 L1 單元測試 + 納入 L2 整合測試**
7. **必要時更新 control-manifest.md**（dev-head 在 ADR 變更後觸發）

### D9. 禁止事項清單

- **禁止** `VillageEntryPoint.cs` > 300 行（量化護欄，由 `validate-file-size.sh` hook 警示）
- **禁止** `VillageEntryPoint` 任一方法 > 100 行（量化護欄，由 hook 警示）
- **禁止** 重新出現 `InitializeManagers` 或等價的「上帝方法」
- **禁止** VillageContext 暴露 `object Resolve(Type t)` 或 `T Get<T>()`（Service Locator 反模式）
- **禁止** Installer 在 Install 內引用其他 Installer 的實例
- **禁止** 把事件 handler 寫在 VillageEntryPoint
- **禁止** Install 順序與 D5 表不一致
- **禁止** Installer 的 Install 與 Uninstall 不對稱（Subscribe 次數 ≠ Unsubscribe 次數）
- **禁止** 為繞過 Installer 限制而把 manager 設為 public static
- **禁止** VillageContext 欄位數超過 10（硬指標，超過必回改 ADR）

---

## Alternatives Considered（考慮過的替代方案）

### 方案 A：採納 — IVillageInstaller + VillageContext（建構器注入容器）三元契約

- 做法：定義 `IVillageInstaller` 介面（`Install(ctx) / Uninstall()`），`VillageContext` 作為明示欄位容器，VillageEntryPoint 瘦身至 600 行以下（2026-04-22 調整）只做「讀 SerializeField / 建 ctx / 序列 Install / 啟動 Launcher」
- 優點：
  - Installer 邊界明確，單一職責
  - VillageContext 欄位為明示 readonly，編譯期可知跨 Installer 共用了什麼
  - Install 順序固定於 D5 表，依賴關係一目了然
  - 每個 Installer 可獨立測試
  - 與 ADR-004 的 Core/ 資料夾自然對齊
  - 16 對事件訂閱分散後，每個 Installer 平均約 2~4 對，人類可審
- 缺點：
  - 需新增 2 個型別檔（IVillageInstaller + VillageContext）
  - VillageContext 欄位設計需要前期思考（初期欄位清單要保守）
  - 跨 Installer 事件訂閱順序仍需謹慎（詳見 Consequences）
- **採納理由**：製作人 2026-04-22 拍板選項 B，本方案直接對應；方案同時解決「檔案膨脹 + 單方法膨脹 + 事件訂閱分散責任」三個病徵；VillageContext 作為資料容器而非 Service Locator，避開 anti-pattern 坑

### 方案 B：（未採納）— 全 Service Locator：`VillageContext.Resolve<T>()`

- 做法：VillageContext 改為型別字典，任何 Installer 可 `ctx.Resolve<SomeManager>()`
- 優點：
  - 極簡，新增 Installer 不需改 ctx 欄位
  - 新增共用服務只要 `ctx.Register<T>(instance)`
- 缺點：
  - 經典 Service Locator 反模式，跨 Installer 耦合隱藏
  - 編譯期無法知道誰依賴誰
  - 測試時必須 mock 全字典
  - Install 順序錯誤只在 runtime 拋錯，不像方案 A 在編譯期就有「欄位尚未賦值」的訊號
- **未採納理由**：Service Locator 在多 Installer 場景下會讓依賴 graph 徹底隱形，反而無法發現循環依賴；VillageEntryPoint 變瘦但把病灶轉移到「ctx 越長越大」

### 方案 C：（未採納）— DI 框架（Zenject / VContainer）

- 做法：引入 Zenject 或 VContainer，Installer 採用框架規範，依賴自動解析
- 優點：
  - 業界成熟方案，constructor injection 標準化
  - Binding 聲明式
  - 測試時可直接替換 binding
- 缺點：
  - 引入新 library 依賴（Zenject ~1MB、VContainer ~500KB）
  - 學習曲線：所有開發者需學框架規則
  - Unity 6 相容性需另外驗證（LLM 知識截止後版本）
  - Binding 錯誤在 runtime 才爆，調試複雜
  - 與既有 KahaGameCore 框架（EventBus / GameStaticDataManager 靜態）協同需額外包裝
- **未採納理由**：IT → VS 階段不引入新 library 依賴；本方案的複雜度與 ProjectDR 團隊規模（目前單人 + AI agent）不匹配；方案 A 的手工契約已足夠解決當前病徵

### 方案 D：（未採納）— 僅抽出 `InitializeManagers` 為多個 `InitializeXxx` 方法（不改類別結構）

- 做法：把 `InitializeManagers()` 270 行拆成 `InitializeStorage()`、`InitializeAffinity()` 等 6 個方法，但仍在 VillageEntryPoint 內
- 優點：
  - 改動最小，不引入新型別
  - 單方法行數達標
- 缺點：
  - 檔案仍 1590+ 行（方法多反而更長）
  - 事件訂閱仍集中在 VillageEntryPoint
  - 單元測試仍無法獨立測「某子系統組裝」
  - 只治單方法症狀，不治檔案膨脹根因
  - 新 manager 仍會「擠進最新的 Initialize 方法」
- **未採納理由**：不解決結構問題，只改方法切片；ADR-004 已建立 Core/ 位置期待真正的 Installer 契約，方案 D 不使用該位置

### 方案 E：（未採納）— 每個 Installer 自己是 MonoBehaviour 掛場景

- 做法：6 個 Installer 各自是 MonoBehaviour，掛在場景上 6 個 GameObject，透過 `Start()` 自行初始化
- 優點：
  - Unity 慣性友好
  - SerializeField 可分散到各 Installer
- 缺點：
  - Install 順序依賴 Unity 內部 Start 呼叫順序（不可靠）
  - VillageEntryPoint 失去統籌能力
  - ctx 建構需靜態或 singleton，破壞 constructor injection 精神
  - 測試時需 Unity runtime，純 NUnit 測不了
- **未採納理由**：Install 順序是本 ADR 的核心保證，Unity Start 順序不可控；方案破壞可測試性

---

## Consequences（後果）

### 正面

- **VillageEntryPoint 從 1590 行降至 < 600 行**：檔案回歸 Composition Root 本份（2026-04-22 護欄調整）
- **InitializeManagers 270 行單方法消滅**：分散至 6 個 Installer，每 Installer 的 Install 方法預計 < 80 行
- **事件訂閱責任分散**：16 對訂閱分散至各 Installer，每 Installer 平均 2~4 對，對稱性人類可審
- **單元可測試性**：每個 Installer 可以 mock ctx 獨立測試 Install / Uninstall 行為
- **與 ADR-004 一致**：Installer 放 `Village/Core/` 與 ADR-004 D1 Core/ 預留對齊
- **新 Installer 有明確套路**：D8 規範了擴展規則，下次需要新 Installer 時不會再退回「擠 VillageEntryPoint」
- **量化護欄有自動檢測**：Sprint 7 C1 建立的 `validate-file-size.sh` hook 可自動警示未來回退
- **C3 `/create-control-manifest` 觸發條件達成**：ADR-001 / 002 / 003 全為 Accepted 後，Sprint 7 C3 可執行

### 負面

- **新增 2 個型別檔**（IVillageInstaller + VillageContext）與 6 個 Installer 類別檔，總檔數 +8
- **跨 Installer 事件訂閱順序需謹慎設計**：若 Installer A 發出事件而 Installer B 尚未 Install，B 會錯過該事件；對策是 D5 順序表嚴格鎖死
- **VillageContext 擴張風險**：若欄位無節制增加，可能變相成為 Service Locator；對策是硬指標 ≤ 10、加欄位需改 ADR
- **既有 17 對事件訂閱需重新分散**：Sprint 7 B6 工作項目的實作成本；對策是 Implementation Guidelines 提供分配表
- **VillageContext 的唯讀查詢介面需先定義**（IAffinityQuery / IVillageProgressionQuery）：dev-agent 在 B2~B4 需先抽出這些介面，增加 ~4 小時工作量
- **測試要求新增**：每個 Installer 需 L1 單元測試，Sprint 7 B7 整合測試需新增 5 檔

### 中性 / 待觀察

- **Tick 介面 IVillageTickable 是選擇性**：若未來有 Installer 需要 Tick，自行實作該介面；若多 Installer 都需要，可評估是否升為強制
- **EventBus 仍為靜態全域**：本 ADR 不重構 EventBus；未來若 EventBus 切換為實例化並注入 ctx，僅需改 ctx.EventBusRef 欄位，Installer 程式碼動少量
- **GameDataAccess 的實作形式**：D2.1 留了彈性（委派 / 介面 / 泛型方法），dev-agent 在 B3 實作時由當時情境決定，事後 retro
- **IT 階段 DTO 仍部分用 string 主鍵**：ADR-002 豁免條目仍存，本 ADR 不改動 ADR-002 的退出時點；Sprint 7 A 類完成後 ADR-002 退出，此時 VillageContext 的 GameDataAccess 真正發揮作用
- **與 ExplorationEntryPoint 的關係**：ExplorationEntryPoint 未在本 ADR 範圍內；Sprint 7 C4 體檢後決定是否比照本契約重構（若超 800 行）

---

## Engine Compatibility（引擎相容性）

| 項目 | 內容 |
|------|------|
| 涉及引擎 | Unity 6.0.x |
| 涉及 API / 模組 | MonoBehaviour lifecycle（Start / OnDestroy / Update）、SerializeField、Unity Inspector 序列化 |
| LLM 知識截止後的風險 | LOW（MonoBehaviour 生命週期與 SerializeField 為 Unity 長期穩定 API；本 ADR 未用任何 Unity 6 新 API）|
| 需驗證的 API 行為 | Installer 為普通 POCO 類（非 MonoBehaviour），生命週期由 VillageEntryPoint 手動驅動，無 Unity 相依 |
| 已讀過的版本遷移文件 | `projects/ProjectDR/tech/engine-reference/unity/VERSION.md`（若尚未建立，Sprint 7 執行前補建；本 ADR 不新增任何 Unity 專屬 API 使用）|

**實作注意**：

- Installer 為純 POCO，**禁止**繼承 MonoBehaviour（否則需掛 GameObject 才能實例化，破壞 D5 順序保證）
- VillageContext 為普通 class，**禁止**加 `[Serializable]`（不透過 Inspector 序列化）
- 若 Installer 需要 Coroutine，透過 ctx 傳入 MonoBehaviour runner（VillageEntryPoint 可暴露 `StartCoroutine` helper），**禁止**Installer 自建 MonoBehaviour

---

## Implementation Guidelines（實作指引）

### 必須做（Required）

**契約層（Sprint 7 B2）**：

- 建立 `Village/Core/Interface/IVillageInstaller.cs`（`Install(VillageContext) / Uninstall()`）
- 建立 `Village/Core/Interface/IVillageTickable.cs`（`Tick(float deltaSeconds)`）
- 兩檔 namespace 皆為 `ProjectDR.Village.Core`

**容器層（Sprint 7 B3）**：

- 建立 `Village/Core/Data/VillageContext.cs`，欄位遵循 D2 初期清單（≤ 10 欄）
- 建立 `Village/Core/Interface/IAffinityQuery.cs`、`IVillageProgressionQuery.cs` 等唯讀查詢介面（依實際需要）
- 建立 `Village/Core/Data/GameDataQuery.cs`（委派定義）

**Installer 層（Sprint 7 B4a ~ B4f）**：

- 6 個 Installer 各放對應位置：
  - `CoreStorageInstaller` → `Village/Core/`（跨模組根級）或 `Village/Storage/Core/`（若決議模組級）——**dev-head 在 B3 完成後回顧，保守決議 `Village/Core/`**（6 個全部放這裡，與 VillageEntryPoint 同目錄，邊界最清晰）
  - `ProgressionInstaller` → `Village/Core/`
  - `AffinityInstaller` → `Village/Core/`
  - `CGInstaller` → `Village/Core/`
  - `CommissionInstaller` → `Village/Core/`
  - `DialogueFlowInstaller` → `Village/Core/`
- 6 個 Installer namespace 皆為 `ProjectDR.Village.Core`
- 每個 Installer 實作 IVillageInstaller；Tick 類另實作 IVillageTickable

**瘦身層（Sprint 7 B5 + B8）**：

- VillageEntryPoint.cs 瘦身至 < 600 行（2026-04-22 調整）
- 刪除 `InitializeManagers()` 方法
- 所有 manager private field 移至對應 Installer
- 所有事件 handler 方法移至對應 Installer

**事件訂閱分散表（Sprint 7 B6）**：

| 事件 | 原訂閱位置（VillageEntryPoint 行號）| 新歸宿 Installer |
|------|-----------------------------------|-----------------|
| `CGUnlockedEvent` | L189 | CGInstaller |
| `CommissionClaimedEvent → OnCommissionClaimedForMainQuest` | L441 | ProgressionInstaller（主線消費）|
| `CharacterUnlockedEvent → OnCharacterUnlockedForProgression` | L445 | ProgressionInstaller |
| `CharacterUnlockedEvent → OnCharacterUnlockedForDialogueRedDot` | L449 | DialogueFlowInstaller |
| `NodeDialogueCompletedEvent → OnNodeDialogueCompletedForMainQuest` | L453 | ProgressionInstaller |
| `ExplorationDepartedEvent → OnExplorationDepartedForMainQuest` | L458 | ProgressionInstaller |
| `GuardReturnEventCompletedEvent → OnGuardReturnLockExploration` | L461 | ProgressionInstaller（Guard 廢棄前暫留；Sprint 7 E6 後審視）|
| `ExplorationGateReopenedEvent → OnExplorationGateReopenedForT2` | L463 | ProgressionInstaller |
| `MainQuestCompletedEvent → OnMainQuestCompletedForNodeDialogue` | L466 | ProgressionInstaller |
| `StorageExpansionCompletedEvent → OnStorageExpansionCompletedForMainQuest` | L469 | ProgressionInstaller |
| `CommissionStartedEvent → OnCommissionStartedForDialogue` | L576 | DialogueFlowInstaller |
| `CommissionClaimedEvent → OnCommissionClaimedForDialogue` | L577 | DialogueFlowInstaller |
| `OpeningSequenceCompletedEvent → OnOpeningSequenceCompletedMarkPlayed` | L840 | ProgressionInstaller |
| `NavigatedToAreaEvent` | L1402 | CoreStorageInstaller（view stack 相關）|
| `ReturnedToHubEvent` | L1403 | CoreStorageInstaller |
| `ExplorationDepartedEvent → OnExplorationDeparted` | L1414 | CoreStorageInstaller（view stack 相關；與 L458 是不同 handler）|
| `ExplorationCompletedEvent` | L1415 | CoreStorageInstaller |

實測 16 對。dev-agent 實作時以此表為準，偏離需回改本 ADR。

**測試層（Sprint 7 B7）**：

- 5 個 Installer 整合測試檔命名：`VillageInstaller<Domain>Tests.cs`（放 `Assets/Tests/Game/Village/Core/`）
- 每個 Installer 單元測試檔：`<InstallerName>Tests.cs`（同目錄）
- 測試命名：`test_install_registers_expected_services`、`test_uninstall_releases_all_subscriptions`、`test_reentry_no_leak`
- 負面測試：`test_install_throws_when_missing_dependency`（缺 ctx 欄位時拋 `InvalidOperationException`）

**Unity 操作**：

- Installer 建立後透過 Unity Editor Move 操作搬位置（保留 meta GUID，若有 meta）
- VillageEntryPoint 瘦身前先跑既有測試確保全綠基線
- 瘦身後跑 smoke test（Title → Hub → 各 Area → 探索 → 回 Hub），無 console error

### 禁止做（Forbidden）

- **禁止** 在 VillageEntryPoint 保留 `InitializeManagers` 或改名的等價方法（D9）
- **禁止** Installer 繼承 MonoBehaviour（Engine Compatibility 段）
- **禁止** VillageContext 加 `Resolve<T>()` / `Get<T>()` 型別查找 API（D2.2）
- **禁止** Installer 間直接引用（D2.3）
- **禁止** 事件 handler 寫在 VillageEntryPoint（D4）
- **禁止** 改動 D5 Install 順序表而未更新本 ADR 版本行（D5 順序鐵律）
- **禁止** VillageContext 欄位超過 10（D2 硬指標）
- **禁止** 為 pass hook warning 而把檔案拆為 `VillageEntryPoint.Part1.cs / Part2.cs` 等 partial class（partial 合計行數仍算）

### 護欄（Guardrail，量化）

- **VillageEntryPoint.cs 行數**：< 600 行（硬指標；2026-04-22 調整，原 < 300；`validate-file-size.sh` 警示閾值 800 / 強警示 1200；本 ADR 要求的 600 仍低於 hook 警示）
- **VillageEntryPoint 單方法行數**：< 100 行（與 hook 警示閾值一致）
- **VillageContext 欄位數**：≤ 10（初期）
- **Installer 類別行數**：建議 < 500 行；若某 Installer 需 > 500 行，應評估是否拆子 Installer
- **Installer.Install 方法行數**：建議 < 100 行
- **每個 Installer 事件訂閱數**：建議 ≤ 6 對（目前 16 對分散至 6 Installer，平均 2~3 對）

### 測試要求

- **L1 Installer 單元測試**（必要，每 Installer 都有）：
  - `test_install_registers_<service>_into_context`
  - `test_uninstall_clears_all_subscriptions`
  - `test_install_uninstall_reentry_is_idempotent_free_of_leak`
  - `test_install_throws_<InvalidOperationException>_when_missing_<dep>`
- **L2 整合測試**（必要）：
  - `test_all_6_installers_install_in_order_without_throw`
  - `test_all_6_installers_uninstall_reverses_all_subscriptions`
  - `test_event_<eventName>_triggers_only_<expected_handlers>`（抽 3~5 個關鍵事件驗證）
- **L3 Smoke test**（必要，手動）：
  - 完整玩家動線：Title → Hub → 任一 Area → 退回 → 進探索 → 戰鬥 → 撤離 → 回 Hub，無 NRE / console error
  - VillageEntryPoint.Start / OnDestroy 各跑一次無 error

---

## GDD Requirements Addressed（對應 GDD 需求）

| TR-ID | 需求摘要 | 來源 GDD |
|-------|---------|---------|
| TR-arch-005 | Village 場景組裝必須透過 IVillageInstaller 契約（Install / Uninstall 對稱，單一參數為 VillageContext），禁止在 VillageEntryPoint 直接 new manager 或直接 Subscribe EventBus | 工作室級規則衍生：CLAUDE.md § 分工鐵則 + ADR-004 Core/ 預留 + 製作人 2026-04-22 Sprint 7 選項 B 拍板 |
| TR-arch-006 | Village 跨 Installer 共用服務必須透過 VillageContext 建構器注入（明示 readonly 欄位），禁止 Service Locator 形式的 Resolve<T>()；VillageContext 欄位數初期 ≤ 10 | 同上 |
| TR-arch-007 | VillageEntryPoint.cs 量化護欄：檔案 < 600 行（2026-04-22 調整，原 < 300）、單方法 < 100 行；禁止 InitializeManagers 等價上帝方法；禁止 partial class 拆分繞過檢查 | 同上 + Sprint 7 C1 validate-file-size.sh hook 配套 |
| TR-arch-008 | 每個 Installer 的 Install 內訂閱事件、Uninstall 內解除訂閱（Subscribe 次數 == Unsubscribe 次數）；事件 handler 方法歸訂閱的 Installer 所有，不得寫在 VillageEntryPoint | 同上 |

---

## Status History（狀態更動紀錄）

| 版本 | 日期 | 狀態 | 變更摘要 |
|------|------|------|---------|
| v1.0 | 2026-04-22 | Proposed | 初次提出（retrofit 自 VillageEntryPoint 1590 行膨脹病徵 + Sprint 7 B 類 6 Installer 拆分方案）|
| v1.1 | 2026-04-22 | Accepted | 經 dev-head 走 DEV-ADR-REVIEW gate（full 模式）通過；製作人離席模式授權連續執行，若 gate PASS 則直接 Accepted |
| v1.2 | 2026-04-22 | Amended | D3 護欄調整：VillageEntryPoint 行數上限由 < 300 改為 < 600（製作人 2026-04-22 Sprint 7 D3 選 b 拍板）。原 300 目標在 B5/B8 實作過程證實過嚴；585 行已是扣除 6 Installer 可承接邏輯後的 scene 層底線（SerializeField 宣告 + ViewPrefabRegistry partial + 跨域事件 handler 無法再壓縮）。TR-arch-007 同步更新 |

---

## 相關連結

- **相關 ADR**：
  - **ADR-004**（Script 組織結構契約）：互為雙胞胎。ADR-004 的 `Core/` 資料夾正是為本 ADR 預留；6 個 Installer 的位置（`Village/Core/`）由 ADR-004 D1 認可；namespace（`ProjectDR.Village.Core`）依 ADR-004 D5
  - **ADR-001**（資料治理契約）：VillageContext 的 `GameDataQuery<T>` 委派指向 `GameStaticDataManager.GetGameData<T>(id)`，是 ADR-001 在組裝層的 consumer；Installer 消費 tabular data 必須走 ctx.GameDataAccess 而非直接 `new FromJson<ConfigData>()`
  - **ADR-002**（IT 階段例外退出清單）：Sprint 7 A 類完成後 ADR-002 退出，屆時 VillageContext 的 GameDataAccess 真正成為唯一資料入口；本 ADR 在 Sprint 7 期間與 ADR-002 並行存活
- **相關規則**：
  - `.claude/rules/gameplay-code.md`（作用路徑涵蓋 `projects/*/Assets/Game/Scripts/**`；Installer 實作需遵守）
  - `.claude/agents/dev-head.md § 分工鐵則`（本 ADR 由 dev-head 撰寫，但 Installer 實作由 dev-agent 執行）
- **相關 Sprint 項目**：Sprint 7 B2（IVillageInstaller）、B3（VillageContext）、B4a~B4f（6 Installer 實作）、B5（VillageEntryPoint 瘦身）、B6（事件訂閱分散）、B7（整合測試）、B8（InitializeManagers 消滅）、C3（create-control-manifest 觸發）
- **相關文件**：
  - `projects/ProjectDR/project-status.md`（2026-04-22 條目：VillageEntryPoint 結構災難、Sprint 7 選項 B 拍板）
  - `projects/ProjectDR/sprint/sprint-7-it-to-vs-restructure.md`（§ B. 架構層重構 = 本 ADR 落地執行）
  - `projects/ProjectDR/FILE_MAP.md`（本 ADR Accepted 後新增 Installer 檔時同步更新）
  - `projects/ProjectDR/adrs/ADR-004-script-organization-structure-contract.md`（Core/ 預留段）

---

## Appendix A：VillageContext 初期欄位清單（完整版）

實作時以此表為 source-of-truth；偏離需回改本 ADR。

```csharp
namespace ProjectDR.Village.Core
{
    public class VillageContext
    {
        // 1. UI 根節點
        public Canvas Canvas { get; }
        public Transform UIContainer { get; }

        // 2. View Stack（由 CoreStorageInstaller 建立後填入）
        public ProjectDR.Village.Shared.ViewStackController ViewStackController { get; internal set; }

        // 3. 時間提供者（由 CoreStorageInstaller 建立後填入）
        public ProjectDR.Village.TimeProvider.ITimeProvider TimeProvider { get; internal set; }

        // 4. Game Data 查詢委派（由 VillageEntryPoint 建 ctx 時就位）
        public GameDataQuery<KahaGameCore.GameData.IGameData> GameDataAccess { get; }

        // 5. 唯讀查詢介面（由對應 Installer Install 後填入）
        public IVillageProgressionQuery VillageProgressionReadOnly { get; internal set; }
        public IAffinityQuery AffinityReadOnly { get; internal set; }

        // 6. 倉庫 / 背包唯讀查詢（由 CoreStorageInstaller Install 後填入）
        public IStorageQuery StorageReadOnly { get; internal set; }
        public IBackpackQuery BackpackReadOnly { get; internal set; }

        // 目前 9 欄（含 2 UI + 2 Core 服務 + 1 委派 + 4 唯讀查詢）<= 10
        // 若需第 10 欄，仍在硬指標內；超過 10 欄必須回改本 ADR
    }
}
```

**欄位新增規則**：

- 欄位新增需滿足：「確實跨 ≥ 2 個 Installer 使用」+「用 EventBus 事件解耦不可行」
- 若僅 2 個 Installer 共用，優先考慮抽第三個中介 Installer 代管
- 加欄位前必須回本 ADR 新版本行

---

## Appendix B：VillageEntryPoint 瘦身前後對照

### 前（2026-04-22 體檢）

- 檔案行數：**1590**
- `InitializeManagers()` 方法行數：**270**
- `OnDestroy()` 方法行數：~80
- private manager fields：**30+**
- `EventBus.Subscribe` 呼叫：**16** 次（對應 Unsubscribe 亦 16 次）
- 單一類別承擔的責任：讀取 config / 建構所有 manager / 訂閱所有事件 / 處理所有事件 handler / UI 預製物實例化 / 村莊 Hub 啟動 / 探索切換 / OpeningSequence 觸發 / 主線自動完成

### 後（Sprint 7 B5 + B8 完成後期望）

- 檔案行數：**< 300**（預期 ~180~250）
- 單方法行數：**< 100**（預期 Start / OnDestroy / Update / BuildContext / BuildInstallers / TryStartOpeningSequence 各約 10~50 行）
- private manager fields：**0~2**（最多：`_ctx` + `_installers[]`）
- `EventBus.Subscribe` 呼叫：**0**（全分散至 Installer）
- 單一類別承擔的責任：讀取 SerializeField / 建 ctx / 序列呼叫 Installer.Install / 啟動 OpeningSequence / Update 轉發 Tick

---
