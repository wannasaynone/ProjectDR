# MVP UI Prefab 規格文件

> **版本**: v1.0
> **建立日期**: 2026-04-17
> **文件性質**: 技術規格（供 ui-ux-designer 直接執行）

## 文件目的

本文件供後續 `ui-ux-designer` agent 建立 Sprint 4 MVP 的 Unity Scene 與 UGUI Prefab 時直接執行。所有 C# class、SerializeField、事件接線已由 `dev-head` 實作完成（`Assets/Game/Scripts/Village/Mvp/`），本文件只定義 **GameObject 階層、元件掛載、RectTransform 設定與 Prefab 連線**。

## 前置規則（記憶規則提醒）

- **UI 使用 UGUI**（Canvas + GameObject + Image/Button/TMP_Text），**不用 UI Toolkit**
- **MCP 建立 UGUI 時必須逐一設定 RectTransform**（anchor / sizeDelta / offset），預設 100×100 居中方塊必須替換
- **Prefab 建立後必須刪除場景上的殘留物件**（先 Instantiate 到 Scene 調整 → 拖成 Prefab → 刪除 Scene 實例，避免殘留）
- **本次 MVP 是新 `MvpMain.unity` 場景**（村莊同場景規則不適用於此原型）
- 執行佈局前必須依 `ugui-best-practices` skill 與 `ui-layout-validation` skill 檢驗所有元素 **不重疊、不超出螢幕、間距合理**

---

## 1. Scene: `MvpMain.unity`

路徑建議：`Assets/Game/Scenes/MvpMain.unity`

### 根階層

```
MvpMain (Scene)
├── EventSystem                  [預設 Unity EventSystem]
├── MainCamera                   [預設 Camera，Solid Color 黑底，用於背景]
├── Canvas                       [Screen Space - Overlay / Canvas Scaler Reference Resolution: 1920×1080, Match 0.5]
│   ├── MvpMainView              [Prefab 實例化]
│   └── InteractionContainer     [空 GameObject，做為 CharacterInteractionView 的掛載容器]
└── EntryPoint                   [空 GameObject，掛 MvpEntryPoint 元件]
```

### 1.1 EntryPoint GameObject 連線

掛載元件：`ProjectDR.Village.Mvp.MvpEntryPoint`

SerializeField 連線：

| 欄位 | 拖入物件 | 說明 |
|---|---|---|
| `_mvpConfigJson` | `Assets/Game/Resources/Config/mvp-config.json` 的 TextAsset | 必填 |
| `_affinityConfigJson` | `Assets/Game/Resources/Config/affinity-config.json` 的 TextAsset | 必填 |
| `_mainView` | 場景中 MvpMainView Prefab 的實例 | 必填 |
| `_interactionViewPrefab` | `Assets/Game/Prefabs/MvpCharacterInteractionView.prefab` | 必填 |
| `_interactionContainer` | 場景中 `Canvas/InteractionContainer` 的 Transform | 必填 |
| `_randomSeed` | 0（預設，使用系統時間） | 選填 |

---

## 2. Prefab: `MvpMainView.prefab`

路徑建議：`Assets/Game/Prefabs/MvpMainView.prefab`
對應 C# class：`ProjectDR.Village.Mvp.UI.MvpMainView : ViewBase`（掛在根物件上）

### 2.1 GameObject 階層樹

```
MvpMainView (RectTransform, Image 半透明黑底, CanvasGroup, MvpMainView 元件)
├── ResourceBar                  (RectTransform)
│   └── WoodLabel                (RectTransform, TMP_Text)           ← SerializeField: _woodLabel
│
├── StatusBar                    (RectTransform, HorizontalLayoutGroup)
│   ├── FireStatusLabel          (RectTransform, TMP_Text)           ← SerializeField: _fireStatusLabel
│   ├── ColdStatusRoot           (RectTransform, 空容器)             ← SerializeField: _coldStatusRoot
│   │   └── ColdLabel            (RectTransform, TMP_Text 內容「寒冷中（×2）」)
│   └── FeedbackLabel            (RectTransform, TMP_Text)           ← SerializeField: _feedbackLabel
│
├── ActionButtonArea             (RectTransform, VerticalLayoutGroup)
│   ├── SearchButton             (RectTransform, Button, Image, Label 子物件) ← SerializeField: _searchButton
│   ├── LightFireButton          (RectTransform, Button, Image, Label 子物件) ← SerializeField: _lightFireButton
│   ├── ExtendFireButton         (RectTransform, Button, Image, Label 子物件) ← SerializeField: _extendFireButton
│   ├── BuildHutButton           (RectTransform, Button, Image, Label 子物件) ← SerializeField: _buildHutButton
│   └── HutBuildProgress         (RectTransform, Slider)                      ← SerializeField: _hutBuildProgress
│
└── CharacterListArea            (RectTransform)
    └── ScrollView               (RectTransform, ScrollRect)
        └── Viewport
            └── Content          (RectTransform, VerticalLayoutGroup, ContentSizeFitter) ← SerializeField: _characterListContainer
```

SerializeField: `_characterListItemPrefab` → 拖入 `MvpCharacterListItem.prefab`

### 2.2 RectTransform 佈局（1920×1080 基準）

| 物件 | Anchor | Pivot | AnchoredPosition | SizeDelta | 備註 |
|---|---|---|---|---|---|
| MvpMainView（根） | stretch-stretch (0,0)-(1,1) | (0.5,0.5) | (0,0) | (0,0) | 鋪滿 Canvas |
| ResourceBar | top-stretch (0,1)-(1,1) | (0.5,1) | (0,-20) | (-40, 60) | 寬鋪滿，左右各留 20 margin；置頂 |
| WoodLabel | stretch-stretch | (0.5,0.5) | (0,0) | (0,0) | font size 36, center, 顯示「木材：N」 |
| StatusBar | top-stretch (0,1)-(1,1) | (0.5,1) | (0,-100) | (-40, 40) | 置於 ResourceBar 下方 |
| FireStatusLabel | 由 HorizontalLayoutGroup 自動 | - | - | preferredWidth 260 | font size 28 |
| ColdStatusRoot | 由 HorizontalLayoutGroup 自動 | - | - | preferredWidth 260 | 初始 inactive |
| ColdLabel | stretch-stretch | (0.5,0.5) | (0,0) | (0,0) | font size 28, color red |
| FeedbackLabel | 由 HorizontalLayoutGroup 自動 | - | - | flexible 1 | font size 24, color 淡灰 |
| ActionButtonArea | left-stretch (0,0)-(0,1) | (0,0.5) | (40,0) | (360, -200) | 貼左，y 從 top 100 到 bottom 100；寬 360 |
| ActionButton（共用） | 由 VerticalLayoutGroup 自動 | - | - | preferredHeight 80 | 按鈕高 80，間距 16 |
| Button Label（子物件） | stretch-stretch | (0.5,0.5) | (0,0) | (-20,0) | font size 32, center |
| HutBuildProgress | 由 VerticalLayoutGroup 自動 | - | - | preferredHeight 24 | Slider, Fill Color 橙色 |
| CharacterListArea | right-stretch (1,0)-(1,1) | (1,0.5) | (-40, 0) | (420, -200) | 貼右；y 從 top 100 到 bottom 100；寬 420 |
| ScrollView | stretch-stretch | (0.5,0.5) | (0,0) | (0,0) | 鋪滿 parent |
| Viewport | stretch-stretch | (0.5,0.5) | (0,0) | (0,0) | |
| Content | top-stretch (0,1)-(1,1) | (0.5,1) | (0,0) | (0,0) | VerticalLayoutGroup childControlHeight=true, childForceExpandHeight=false, spacing=8, padding=(8,8,8,8); ContentSizeFitter vertical=PreferredSize |

### 2.3 VerticalLayoutGroup / HorizontalLayoutGroup 設定

- **ActionButtonArea VerticalLayoutGroup**: `spacing=16`, `padding=(0,0,0,0)`, `childControlWidth=true`, `childControlHeight=false`, `childForceExpandWidth=true`, `childForceExpandHeight=false`
- **StatusBar HorizontalLayoutGroup**: `spacing=16`, `childControlWidth=false`, `childForceExpandWidth=false`, `childAlignment=MiddleLeft`

### 2.4 字體與顏色

- 全部使用 TextMeshProUGUI（TMP_Text）
- Font Asset 使用專案已有的繁體中文 TMP Font（若無，建立相應 Font Asset）
- 主色：白 `#FFFFFF`（主要文字）、淡灰 `#C0C0C0`（次要回饋）、紅 `#FF4040`（寒冷警告）、橙 `#FF9A3C`（進度條）
- 按鈕底色：`#2D2D2D`（normal）、`#4A4A4A`（highlighted）、`#1A1A1A`（pressed）；按鈕 disabled 色 `#555555` 50% 透明

---

## 3. Prefab: `MvpCharacterListItem.prefab`

路徑建議：`Assets/Game/Prefabs/MvpCharacterListItem.prefab`
對應 C# class：`ProjectDR.Village.Mvp.UI.MvpCharacterListItemView`（掛在根物件上）

### 3.1 GameObject 階層樹

```
MvpCharacterListItem (RectTransform, MvpCharacterListItemView 元件, Button, Image)
├── NameLabel           (RectTransform, TMP_Text)           ← SerializeField: _nameLabel
├── AffinityLabel       (RectTransform, TMP_Text)           ← SerializeField: _affinityLabel
└── RedDot              (RectTransform, RedDotView 元件)    ← SerializeField: _redDot
    └── DotImage        (RectTransform, Image 紅色圓點)     ← RedDotView: _dotImage
```

SerializeField: `_itemButton` → 拖入本根物件上的 `Button` 元件。

### 3.2 RectTransform 佈局

整個項目高 80，寬撐滿父層 Content。

| 物件 | Anchor | Pivot | AnchoredPosition | SizeDelta | 備註 |
|---|---|---|---|---|---|
| Root | stretch-top (0,1)-(1,1)（LayoutGroup 自動控寬） | (0.5,1) | (0,0) | (0,80) | 高 80 |
| NameLabel | left-stretch (0,0)-(0.5,1) | (0,0.5) | (20,0) | (-20, 0) | 左半，font size 28 |
| AffinityLabel | right-stretch (0.5,0)-(1,1) | (1,0.5) | (-60,0) | (-20, 0) | 右半（留 60 給紅點），font size 24 |
| RedDot | right-middle (1,0.5)-(1,0.5) | (1,0.5) | (-16,0) | (24, 24) | 最右側 |
| DotImage | stretch-stretch | (0.5,0.5) | (0,0) | (0,0) | 圓形 Image，color 紅 `#FF3030` |

---

## 4. Prefab: `MvpCharacterInteractionView.prefab`

路徑建議：`Assets/Game/Prefabs/MvpCharacterInteractionView.prefab`
對應 C# class：`ProjectDR.Village.Mvp.UI.MvpCharacterInteractionView : ViewBase`

### 4.1 GameObject 階層樹

```
MvpCharacterInteractionView (RectTransform 鋪滿 Canvas, Image 半透明黑底, CanvasGroup, MvpCharacterInteractionView 元件)
├── HeaderRow                (RectTransform, HorizontalLayoutGroup)
│   ├── CharacterNameLabel   (RectTransform, TMP_Text)    ← SerializeField: _characterNameLabel
│   └── AffinityLabel        (RectTransform, TMP_Text)    ← SerializeField: _affinityLabel
│
├── DialogueArea             (RectTransform, Image 深灰背景)
│   ├── DialogueText         (RectTransform, TMP_Text)    ← SerializeField: _dialogueText
│   └── DialogueClickArea    (RectTransform, Button 透明)  ← SerializeField: _dialogueClickArea
│
├── MenuRow                  (RectTransform, HorizontalLayoutGroup)
│   ├── DialogueButton       (RectTransform, Button)      ← SerializeField: _dialogueButton
│   │   └── Label            (RectTransform, TMP_Text 內容「對話」)
│   └── DispatchButton       (RectTransform, Button)      ← SerializeField: _dispatchButton
│       └── Label            (RectTransform, TMP_Text 內容「派遣（未開放）」) ← SerializeField: _dispatchButtonLabel
│
├── DispatchPlaceholderLabel (RectTransform, TMP_Text)    ← SerializeField: _dispatchPlaceholderLabel
│
└── ReturnButton             (RectTransform, Button)      ← SerializeField: _returnButton
    └── Label                (RectTransform, TMP_Text 內容「返回」)
```

### 4.2 RectTransform 佈局（1920×1080）

| 物件 | Anchor | Pivot | AnchoredPosition | SizeDelta |
|---|---|---|---|---|
| 根 | stretch-stretch | (0.5,0.5) | (0,0) | (0,0) |
| HeaderRow | top-stretch (0,1)-(1,1) | (0.5,1) | (0,-40) | (-80, 60) |
| CharacterNameLabel | 由 HLG 自動 | - | - | flexible 1 | font size 40，左對齊 |
| AffinityLabel | 由 HLG 自動 | - | - | preferredWidth 240 | font size 32，右對齊 |
| DialogueArea | center-middle | (0.5,0.5) | (0,60) | (1280, 300) |
| DialogueText | stretch-stretch | (0.5,0.5) | (0,0) | (-48,-48) | font size 32 |
| DialogueClickArea | stretch-stretch | (0.5,0.5) | (0,0) | (0,0) | 整個 DialogueArea 都可點擊；Button 需設 Color Tint alpha=0 透明 |
| MenuRow | center-bottom | (0.5,0) | (0, 180) | (800, 100) |
| DialogueButton | 由 HLG 自動 | - | - | flexible 1 | 高 80 |
| DispatchButton | 由 HLG 自動 | - | - | flexible 1 | 高 80 |
| DispatchPlaceholderLabel | center-bottom | (0.5,0) | (0, 100) | (800, 40) | font size 22，灰字 |
| ReturnButton | top-right | (1,1) | (-40,-40) | (160, 60) |

---

## 5. EntryPoint 連線總表

`EntryPoint` GameObject 上 `MvpEntryPoint` 元件需連線以下：

| SerializeField | 拖曳物件 |
|---|---|
| `_mvpConfigJson` | `Assets/Game/Resources/Config/mvp-config.json`（拖 TextAsset） |
| `_affinityConfigJson` | `Assets/Game/Resources/Config/affinity-config.json`（拖 TextAsset） |
| `_mainView` | 場景上實例化後的 `MvpMainView` GameObject |
| `_interactionViewPrefab` | `Assets/Game/Prefabs/MvpCharacterInteractionView.prefab` |
| `_interactionContainer` | 場景上 `Canvas/InteractionContainer` 的 Transform |
| `_randomSeed` | 0 |

---

## 6. 佈局驗證檢查清單（必執行）

在建立完所有 Prefab 並放入 Scene 後，依以下清單確認（對應 `ui-layout-validation` skill）：

- [ ] MvpMainView 根層 rect = 1920×1080（鋪滿 Canvas），不溢出
- [ ] 1920×1080 解析度下：ResourceBar 位於螢幕頂端不被裁切（y ∈ [0, 60]）
- [ ] StatusBar 與 ResourceBar 不重疊（y 間距 ≥ 0）
- [ ] ActionButtonArea 的 4 個按鈕高度 80 × 4 + spacing 16 × 3 = 368，不超出其分配區域（高 ≥ 400）
- [ ] CharacterListArea 寬 420，貼右 40 margin，總計 460 像素，未超出 1920 寬度
- [ ] ActionButtonArea（寬 360 + 40 margin = 400）與 CharacterListArea 的左緣（1920 - 460 = 1460）之間留有中央 feedback 空間
- [ ] MvpCharacterInteractionView 的 DialogueArea（1280 寬）置中，左右 margin 各 320
- [ ] MenuRow 的兩個按鈕（DialogueButton / DispatchButton）間距 16 以上
- [ ] 所有按鈕有清楚的 normal / highlighted / pressed 視覺差異
- [ ] 所有 TMP_Text 使用繁體中文字體資產
- [ ] RedDot 在未 Ready 時隱藏（gameObject.SetActive(false)）

---

## 7. Scene 完成後驗收標準

- 播放 Play 進入 MvpMain 場景，console 無 null reference
- 點擊「搜索附近」按鈕 → 木材 +1，回饋文字更新
- 連搜 5 次 → 「生火」按鈕出現（之前不可見），點擊 → 火堆 60 秒倒數開始
- 火堆倒數歸零 → 「寒冷中」狀態顯示，搜索冷卻變 2 秒
- 累積 10 木材（或更多）+ 火堆曾點燃 → 「蓋小屋」按鈕可點擊
- 點擊蓋小屋 → 進度條 10 秒完成 → 右側角色清單多出一位 placeholder NPC
- 點擊 NPC → 進入互動畫面，顯示名字與好感度；點「對話」 → 對話一行，點擊推進後返回選單，好感度 +3
- 推進 45 秒不點對話 → 該 NPC 在角色清單上紅點亮起

---

## 版本更新紀錄

| 版本 | 日期 | 說明 |
|------|------|------|
| v1.0 | 2026-04-17 | 初版，Sprint 4 MVP Prefab 規格完整定義 |
