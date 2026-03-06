# UI/UX 設計実装ガイド (Unity-AI-Forge {VERSION})

## 概要

優れた UI/UX はゲームの操作性とユーザー体験を大きく左右します。
Unity-AI-Forge の UI Pillar（`unity_gamekit_ui(widgetType='command')`、`unity_gamekit_ui(widgetType='binding')`、
`unity_gamekit_ui(widgetType='list')`、`unity_gamekit_ui(widgetType='slot')`、`unity_gamekit_ui(widgetType='selection')`）を
`unity_ui_foundation`、`unity_ui_state`、`unity_ui_navigation` と
組み合わせることで、コントローラー対応・レスポンシブなゲーム UI を素早く構築できます。

---

## 設計パターン

### UI Pillar の 5 つの役割

```
UICommand    -- 「何かをする」命令型 UI
             例: ボタン押下でアクション実行

UIBinding    -- 「データを表示する」バインディング型 UI
             例: HP バー <- Player.currentHp の自動反映

UIList       -- 「一覧を表示する」リスト型 UI
             例: インベントリ、スキル一覧、ランキング

UISlot       -- 「アイテムを収める」スロット型 UI
             例: 装備スロット、クイックスロット

UISelection  -- 「選択状態を管理する」セレクション型 UI
             例: メニュー選択、タブ切り替え、ハイライト管理
```

### 画面フロー設計

```
[タイトル画面]
  +-- スタート -> [ゲームプレイ画面]
  +-- 設定   -> [設定画面]
  +-- 終了   -> アプリ終了

[ゲームプレイ画面]
  +-- HUD（HP・スコア・時間）常時表示
  +-- ポーズ入力 -> [ポーズ画面]
  +-- ゲームクリア/オーバー -> [リザルト画面]

[ポーズ画面]
  +-- 再開  -> [ゲームプレイ画面]
  +-- 設定  -> [設定画面]
  +-- タイトルへ -> [タイトル画面]
```

### Canvas 構成（推奨階層）

```
Canvas (Screen Space - Overlay, Sort Order: 0)
  +-- HUD                      # ゲームプレイ中常時表示
  |    +-- HPBar               # プレイヤー HP バー
  |    +-- ScoreDisplay        # スコア
  |    +-- TimerDisplay        # タイマー
  +-- Screens                  # 画面パネル群
  |    +-- TitlePanel          # タイトル
  |    +-- PausePanel          # ポーズ
  |    +-- InventoryPanel      # インベントリ
  |    +-- SettingsPanel       # 設定
  |    +-- ResultPanel         # リザルト
  +-- Overlays                 # 最前面に常駐
       +-- DialogPanel         # 確認ダイアログ
       +-- ToastNotification   # トースト通知
       +-- LoadingScreen       # ローディング
```

### レスポンシブレイアウト設計

```
Anchors の使い方:
  - 四隅固定     -> min=(0,0), max=(1,1)  全画面パネル
  - 上部固定     -> min=(0,1), max=(1,1)  上部 HUD バー
  - 中央固定     -> min=(0.5,0.5), max=(0.5,0.5)  ダイアログ
  - 左上固定     -> min=(0,1), max=(0,1)  ミニマップ、コイン表示
```

---

## 推奨フォルダ構造

```
Assets/
  Scripts/
    UI/
      Screens/
        TitleScreen.cs          # タイトル画面ロジック
        HUDScreen.cs            # HUD 管理
        PauseScreen.cs          # ポーズ画面ロジック
        ResultScreen.cs         # リザルト画面ロジック
        SettingsScreen.cs       # 設定画面ロジック
  Prefabs/
    UI/
      ToastNotification.prefab
      DialogBox.prefab
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: Canvas と基本構造の構築

```python
# メイン Canvas の作成
unity_ui_foundation(
    operation="createCanvas",
    name="GameCanvas",
    renderMode="screenSpaceOverlay"
)

# UI 階層の構築（ui_foundation で個別に作成）
unity_ui_foundation(operation="createPanel", name="UIRoot", parentPath="GameCanvas")

# HUD パネル
unity_ui_foundation(operation="createPanel", name="HUD", parentPath="GameCanvas/UIRoot")
unity_ui_foundation(operation="createText", name="ScoreText", parentPath="GameCanvas/UIRoot/HUD",
    text="スコア: 000000")
unity_ui_foundation(operation="createText", name="HPText", parentPath="GameCanvas/UIRoot/HUD",
    text="HP: 100/100")

# Screens パネル（各画面）
unity_ui_foundation(operation="createPanel", name="Screens", parentPath="GameCanvas/UIRoot")
unity_ui_foundation(operation="createPanel", name="TitlePanel", parentPath="GameCanvas/UIRoot/Screens")
unity_ui_foundation(operation="createPanel", name="PausePanel", parentPath="GameCanvas/UIRoot/Screens")
unity_ui_foundation(operation="createPanel", name="InventoryPanel", parentPath="GameCanvas/UIRoot/Screens")
unity_ui_foundation(operation="createPanel", name="SettingsPanel", parentPath="GameCanvas/UIRoot/Screens")
unity_ui_foundation(operation="createPanel", name="ResultPanel", parentPath="GameCanvas/UIRoot/Screens")

# Overlays パネル
unity_ui_foundation(operation="createPanel", name="Overlays", parentPath="GameCanvas/UIRoot")
unity_ui_foundation(operation="createPanel", name="DialogPanel", parentPath="GameCanvas/UIRoot/Overlays")
unity_ui_foundation(operation="createPanel", name="ToastNotification", parentPath="GameCanvas/UIRoot/Overlays")
unity_ui_foundation(operation="createPanel", name="LoadingScreen", parentPath="GameCanvas/UIRoot/Overlays")

# 初期非表示パネルを隠す
unity_ui_foundation(operation="hide", targetPath="GameCanvas/UIRoot/Screens/PausePanel")
unity_ui_foundation(operation="hide", targetPath="GameCanvas/UIRoot/Screens/InventoryPanel")
unity_ui_foundation(operation="hide", targetPath="GameCanvas/UIRoot/Screens/SettingsPanel")
unity_ui_foundation(operation="hide", targetPath="GameCanvas/UIRoot/Screens/ResultPanel")
unity_ui_foundation(operation="hide", targetPath="GameCanvas/UIRoot/Overlays/DialogPanel")
unity_ui_foundation(operation="hide", targetPath="GameCanvas/UIRoot/Overlays/ToastNotification")
unity_ui_foundation(operation="hide", targetPath="GameCanvas/UIRoot/Overlays/LoadingScreen")
```

### Step 2: HUD の構築（データバインディング）

```python
# UIBinding で HP データを UI に自動反映
unity_gamekit_ui(widgetType='binding',
    operation="create",
    targetPath="GameCanvas/UIRoot/HUD",
    bindingId="hud_hp_binding",
    elementName="hp-bar",
    sourceType="health",
    sourceId="player_health",
    format="ratio",
    formatString="HP: {0}/{1}",
    smoothTransition=true,
    smoothSpeed=5.0
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 3: タイトルメニューのボタン配置

```python
# タイトル画面にボタンを配置
unity_ui_foundation(
    operation="createButton",
    name="BtnStart",
    parentPath="GameCanvas/UIRoot/Screens/TitlePanel",
    text="ゲームスタート"
)

unity_ui_foundation(
    operation="createButton",
    name="BtnSettings",
    parentPath="GameCanvas/UIRoot/Screens/TitlePanel",
    text="設定"
)

unity_ui_foundation(
    operation="createButton",
    name="BtnQuit",
    parentPath="GameCanvas/UIRoot/Screens/TitlePanel",
    text="終了"
)

# レイアウトグループで整列
unity_ui_foundation(
    operation="addLayoutGroup",
    targetPath="GameCanvas/UIRoot/Screens/TitlePanel",
    layoutType="vertical",
    spacing=16
)
```

### Step 4: 画面遷移の UICommand 設定

```python
# 画面遷移コマンドをまとめて定義
unity_gamekit_ui(widgetType='command',
    operation="createCommandPanel",
    panelId="screen_navigator",
    parentPath="GameCanvas",
    commands=[
        {"name": "StartGame",   "label": "ゲーム開始", "commandType": "custom"},
        {"name": "ShowPause",   "label": "ポーズ",     "commandType": "custom"},
        {"name": "HidePause",   "label": "再開",       "commandType": "custom"},
        {"name": "ShowInven",   "label": "インベントリ", "commandType": "custom"},
        {"name": "HideInven",   "label": "閉じる",     "commandType": "custom"},
        {"name": "GoToTitle",   "label": "タイトルへ", "commandType": "custom"},
        {"name": "ShowResult",  "label": "リザルト",   "commandType": "custom"}
    ]
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 5: UI 状態管理（ゲーム操作/メニュー切替の標準手法）

`unity_ui_state` はゲームプレイ中の操作画面とメニュー画面の切り替えを管理する標準ツールです。
各画面モード（タイトル、ゲームプレイ、ポーズ、インベントリ等）を状態として定義し、
`applyState` で瞬時に切り替えます。`createStateGroup` で排他制御も可能です。

```python
# ui_state で画面モードを定義
unity_ui_state(
    operation="defineState",
    rootPath="GameCanvas",
    stateName="Title",
    elements=[
        {"path": "UIRoot/Screens/TitlePanel", "visible": True},
        {"path": "UIRoot/HUD", "visible": False},
        {"path": "UIRoot/Screens/PausePanel", "visible": False},
        {"path": "UIRoot/Screens/ResultPanel", "visible": False}
    ]
)

unity_ui_state(
    operation="defineState",
    rootPath="GameCanvas",
    stateName="Gameplay",
    elements=[
        {"path": "UIRoot/Screens/TitlePanel", "visible": False},
        {"path": "UIRoot/HUD", "visible": True},
        {"path": "UIRoot/Screens/PausePanel", "visible": False}
    ]
)

unity_ui_state(
    operation="defineState",
    rootPath="GameCanvas",
    stateName="Paused",
    elements=[
        {"path": "UIRoot/HUD", "visible": True, "interactable": False},
        {"path": "UIRoot/Screens/PausePanel", "visible": True, "interactable": True}
    ]
)

unity_ui_state(
    operation="defineState",
    rootPath="GameCanvas",
    stateName="Inventory",
    elements=[
        {"path": "UIRoot/HUD", "visible": True, "interactable": False},
        {"path": "UIRoot/Screens/InventoryPanel", "visible": True, "interactable": True}
    ]
)

# 排他グループ: 同時に1つの画面モードのみアクティブ
unity_ui_state(
    operation="createStateGroup",
    rootPath="GameCanvas",
    groupName="screen_mode",
    states=["Title", "Gameplay", "Paused", "Inventory", "Settings", "Result"],
    defaultState="Title"
)

# 状態を適用（ゲーム開始時）
unity_ui_state(
    operation="applyState",
    rootPath="GameCanvas",
    stateName="Title"
)

# ポーズボタン押下時 → Paused に切替
unity_ui_state(
    operation="applyState",
    rootPath="GameCanvas",
    stateName="Paused"
)

# 再開時 → Gameplay に戻す
unity_ui_state(
    operation="applyState",
    rootPath="GameCanvas",
    stateName="Gameplay"
)
```

### Step 6: キーボード/ゲームパッドナビゲーション

```python
# タイトルメニューのナビゲーション設定
unity_ui_navigation(
    operation="autoSetup",
    rootPath="GameCanvas/UIRoot/Screens/TitlePanel"
)

# 初期選択を設定
unity_ui_navigation(
    operation="setFirstSelected",
    targetPath="GameCanvas/UIRoot/Screens/TitlePanel",
    firstSelected="BtnStart"
)
```

### Step 7: イベント接続

```python
# タイトルボタンのイベント接続
unity_event_wiring(
    operation="wire",
    source={"gameObject": "GameCanvas/UIRoot/Screens/TitlePanel/BtnStart",
            "component": "Button", "event": "onClick"},
    target={"gameObject": "GameCanvas", "method": "StartGame"}
)

unity_event_wiring(
    operation="wire",
    source={"gameObject": "GameCanvas/UIRoot/Screens/TitlePanel/BtnSettings",
            "component": "Button", "event": "onClick"},
    target={"gameObject": "GameCanvas", "method": "ShowSettings"}
)

# ポーズ・インベントリ入力のバインド
unity_input_profile(
    operation="createInputActions",
    targetPath="GameCanvas",
    actionMapName="UI",
    actions=[
        {"name": "Pause",     "type": "Button", "binding": "<Keyboard>/escape"},
        {"name": "Inventory", "type": "Button", "binding": "<Keyboard>/i"}
    ]
)
```

### Step 8: スキル/アビリティ選択 UI

```python
# スキル一覧を UIList で実装
unity_gamekit_ui(widgetType='list',
    operation="create",
    parentPath="GameCanvas/UIRoot/Screens",
    listId="skill_list",
    name="SkillPanel",
    layout="grid",
    columns=4,
    selectable=true,
    multiSelect=false
)

# UISelection で選択状態の管理
unity_gamekit_ui(widgetType='selection',
    operation="create",
    parentPath="GameCanvas/UIRoot/Screens/SkillPanel",
    selectionId="skill_selection",
    name="SkillSelection",
    selectionType="radio",
    layout="grid"
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

---

## よくあるパターン

### パターン 1: 設定画面のタブ切り替え

```python
unity_gamekit_ui(widgetType='selection',
    operation="create",
    parentPath="GameCanvas/UIRoot/Screens/SettingsPanel",
    selectionId="settings_tabs",
    name="SettingsTabs",
    selectionType="tab",
    layout="horizontal",
    items=[
        {"id": "tab_graphics", "label": "グラフィック",
         "associatedPanelPath": "GameCanvas/UIRoot/Screens/SettingsPanel/GraphicsPanel"},
        {"id": "tab_audio",    "label": "サウンド",
         "associatedPanelPath": "GameCanvas/UIRoot/Screens/SettingsPanel/AudioPanel"},
        {"id": "tab_controls", "label": "操作設定",
         "associatedPanelPath": "GameCanvas/UIRoot/Screens/SettingsPanel/ControlsPanel"}
    ]
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### パターン 2: UI Toolkit との併用

```python
# UI Toolkit アセットの作成
unity_uitk_asset(
    operation="createUXML",
    assetPath="Assets/UI/UXML/MainMenu.uxml"
)

unity_uitk_asset(
    operation="createUSS",
    assetPath="Assets/UI/USS/MainMenu.uss"
)

# UIDocument をシーンに配置
unity_uitk_document(
    operation="create",
    targetPath="MainMenuUI",
    uxmlPath="Assets/UI/UXML/MainMenu.uxml",
    ussPath="Assets/UI/USS/MainMenu.uss"
)
```

### パターン 3: CanvasGroup によるUI制御

```python
# パネルにCanvasGroupを設定して透明度・操作性を制御
unity_component_crud(
    operation="update",
    gameObjectPath="GameCanvas/UIRoot/Screens/PausePanel",
    componentType="CanvasGroup",
    propertyChanges={"alpha": 0.8, "interactable": True, "blocksRaycasts": True, "ignoreParentGroups": True}
)

# パネル作成時にCanvasGroupを同時追加
unity_ui_foundation(
    operation="createPanel",
    name="OverlayPanel",
    parentPath="GameCanvas/UIRoot/Overlays",
    addCanvasGroup=True,
    alpha=0.0,        # 初期状態は透明
    interactable=False
)

# show/hide でCanvasGroup経由の表示切替
unity_ui_foundation(operation="show", targetPath="GameCanvas/UIRoot/Overlays/OverlayPanel")
unity_ui_foundation(operation="hide", targetPath="GameCanvas/UIRoot/Overlays/OverlayPanel")

# ui_state で ignoreParentGroups を含む状態定義
unity_ui_state(
    operation="defineState",
    rootPath="GameCanvas",
    stateName="dialog_open",
    elements=[
        {"path": "UIRoot/Overlays/DialogPanel", "visible": True,
         "alpha": 1.0, "ignoreParentGroups": True},
        {"path": "UIRoot/Screens", "visible": False}
    ]
)
```

> **方針:** UI の表示/非表示は常に CanvasGroup (`alpha=0, interactable=false, blocksRaycasts=false`) で制御する。`SetActive` は使用しない。
> CanvasGroup はレイアウト再計算を発生させず、フェードアニメーションとの相性も良い。
> `ignoreParentGroups=true` を使えば、親パネルを非表示にしても子要素だけ独立して表示可能。

---

## ゲームフェーズ ↔ UI状態の連動

ゲームのフェーズ（ターン制バトルのフェーズ、メニュー画面の状態等）に応じて
UI パネルの表示/非表示を切り替えるパターン。**フェーズ変更時に UpdatePhaseUI() を呼び、
CanvasGroup で各パネルの表示を制御する。**

### パターン: フェーズ連動パネル制御

```python
# 各パネルに CanvasGroup を追加し、初期状態は非表示
# GameObjectは常にアクティブ、CanvasGroup で表示制御
unity_ui_foundation(operation="hide", targets=[
    "BattleCanvas/StatusPanel",
    "BattleCanvas/CommandMenu",
    "BattleCanvas/DamagePredict",
    "BattleCanvas/EnemyPhaseOverlay",
    "BattleCanvas/ResultPanel"
])
```

C# スクリプト側の実装パターン:

```csharp
// パネル参照は CanvasGroup 型で持つ（GameObject ではない）
[Header("Panels")]
public CanvasGroup statusPanel;
public CanvasGroup commandMenu;
public CanvasGroup resultPanel;

void UpdatePhaseUI()
{
    // 全パネルを一旦非表示
    SetPanel(commandMenu, false);
    SetPanel(resultPanel, false);

    // フェーズに応じて必要なパネルのみ表示
    switch (currentPhase)
    {
        case Phase.PlayerIdle:
            SetPanel(statusPanel, false);
            break;
        case Phase.PlayerAction:
            SetPanel(statusPanel, true);
            SetPanel(commandMenu, true);
            break;
        case Phase.Result:
            SetPanel(resultPanel, true);
            break;
    }
}

void SetPanel(CanvasGroup panel, bool active)
{
    if (panel == null) return;
    panel.alpha = active ? 1f : 0f;
    panel.interactable = active;
    panel.blocksRaycasts = active;
}
```

### パターン: フェーズ連動ボタン制御

特定フェーズでのみ操作可能なボタンは `Button.interactable` で制御する。
CanvasGroup の interactable とは別に、個別ボタン単位の制御が必要。

```csharp
public Button endTurnButton;

void UpdatePhaseUI()
{
    // ターン終了ボタンは PlayerIdle のみ操作可能
    if (endTurnButton != null)
        endTurnButton.interactable = (currentPhase == Phase.PlayerIdle);
}
```

> **注意:** CanvasGroup.interactable はパネル全体の操作可否を制御する。
> 個別ボタンの有効/無効は `Button.interactable` で制御すること。
> 両方 false の場合はどちらか一方を true にしてもボタンは押せない。

### パターン: 複数兄弟要素の配置

同一親に複数の UI 要素を配置する場合は **必ず LayoutGroup を追加する**。
LayoutGroup なしでは全要素が同位置に重なる。

```python
# 悪い例: LayoutGroup なしで兄弟要素が重なる
unity_ui_foundation(operation='createButton', name='Btn1', parentPath='Canvas/Panel', text='A')
unity_ui_foundation(operation='createButton', name='Btn2', parentPath='Canvas/Panel', text='B')
# → Btn1 と Btn2 が同じ位置に重なる

# 良い例: 先に LayoutGroup を追加
unity_ui_foundation(operation='addLayoutGroup', targetPath='Canvas/Panel',
    layoutType='Vertical', spacing=10)
unity_ui_foundation(operation='createButton', name='Btn1', parentPath='Canvas/Panel', text='A')
unity_ui_foundation(operation='createButton', name='Btn2', parentPath='Canvas/Panel', text='B')
```

---

## UI アーキテクチャパターン

### MVP (Model-View-Presenter)

ゲーム UI に最も適した分離パターン。Presenter がデータとUIの仲介役を担い、
View はデータを直接参照しません。Unity 公式の QuizU サンプルでも採用されています。

```
Model (ScriptableObject / C# class)
  ├── データ保持（HP, スコア, 設定値）
  └── データ変更イベント発火
        ↓ event通知
Presenter (MonoBehaviour)
  ├── Model と View の橋渡し
  ├── Model のイベントを購読
  └── View の更新メソッドを呼び出し
        ↓ メソッド呼び出し
View (MonoBehaviour on UI)
  ├── UI 要素への参照（Text, Image, Slider）
  ├── 表示更新のみ（ロジックなし）
  └── ユーザー入力を Presenter に通知
```

**使い分け:**
- **HUD (HP, Score, Timer)** → `unity_gamekit_ui(widgetType='binding')` で十分（1対1バインド）
- **インベントリ画面** → MVP が有効（複数要素の連動、フィルタ、ソート等）
- **設定画面** → MVP が有効（設定値の読み書き、適用/リセットのロジック）

### MVP 実装例

```python
# Model: ゲーム設定データ
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/UI/Settings/SettingsModel.cs",
    content="""using System;
using UnityEngine;

[CreateAssetMenu(menuName = \"Game/SettingsModel\")]
public class SettingsModel : ScriptableObject
{
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public bool fullscreen = true;
    public int qualityLevel = 2;

    public event Action OnSettingsChanged;

    public void Apply()
    {
        AudioListener.volume = masterVolume;
        Screen.fullScreen = fullscreen;
        QualitySettings.SetQualityLevel(qualityLevel);
        OnSettingsChanged?.Invoke();
    }
}"""
)

# View: UI 要素の参照と表示更新のみ
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/UI/Settings/SettingsView.cs",
    content="""using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsView : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    public Slider MasterVolumeSlider => masterVolumeSlider;
    public Slider BgmVolumeSlider => bgmVolumeSlider;
    public Slider SfxVolumeSlider => sfxVolumeSlider;
    public Toggle FullscreenToggle => fullscreenToggle;
    public TMP_Dropdown QualityDropdown => qualityDropdown;

    public void UpdateView(float master, float bgm, float sfx, bool fs, int quality)
    {
        masterVolumeSlider.SetValueWithoutNotify(master);
        bgmVolumeSlider.SetValueWithoutNotify(bgm);
        sfxVolumeSlider.SetValueWithoutNotify(sfx);
        fullscreenToggle.SetIsOnWithoutNotify(fs);
        qualityDropdown.SetValueWithoutNotify(quality);
    }
}"""
)

# Presenter: Model と View の仲介
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/UI/Settings/SettingsPresenter.cs",
    content="""using UnityEngine;

public class SettingsPresenter : MonoBehaviour
{
    [SerializeField] private SettingsModel model;
    [SerializeField] private SettingsView view;

    void Start()
    {
        // View → Model の入力接続
        view.MasterVolumeSlider.onValueChanged.AddListener(v => { model.masterVolume = v; model.Apply(); });
        view.BgmVolumeSlider.onValueChanged.AddListener(v => { model.bgmVolume = v; model.Apply(); });
        view.SfxVolumeSlider.onValueChanged.AddListener(v => { model.sfxVolume = v; model.Apply(); });
        view.FullscreenToggle.onValueChanged.AddListener(v => { model.fullscreen = v; model.Apply(); });
        view.QualityDropdown.onValueChanged.AddListener(v => { model.qualityLevel = v; model.Apply(); });

        // 初期表示
        view.UpdateView(model.masterVolume, model.bgmVolume, model.sfxVolume,
                        model.fullscreen, model.qualityLevel);
    }
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)
```

### GameKit UIBinding との使い分け

| 場面 | 推奨 | 理由 |
|------|------|------|
| HP/スコア等のシンプルな数値表示 | `unity_gamekit_ui(widgetType='binding')` | 1行で完結、ロジック不要 |
| データの読み書き双方向 | MVP | Slider/Toggle の入力→Model 更新が必要 |
| フィルタ/ソートのあるリスト | MVP + `unity_gamekit_ui(widgetType='list')` | Presenter でデータ変換 |
| 複数画面の連動 | MVP + Event Channel | 画面間のデータ受け渡し |
| プロトタイプ段階 | `unity_gamekit_ui(widgetType='binding')` | 速度優先 |

---

## 注意点・落とし穴

1. **コード生成後のコンパイル待ち**
   `unity_gamekit_ui(widgetType='binding')`、`unity_gamekit_ui(widgetType='command')`、`unity_gamekit_ui(widgetType='list')`、
   `unity_gamekit_ui(widgetType='slot')`、`unity_gamekit_ui(widgetType='selection')` はコードを生成するため、
   実行後は必ず `unity_compilation_await` を呼ぶこと。

2. **EventSystem の重複**
   Canvas を複数作成すると EventSystem が重複することがある。
   シーン内に EventSystem は 1 つだけ存在するよう確認すること。

3. **Z オーダーと Sort Order**
   パネルの表示優先度は Hierarchy の順序で決まる（下が手前）。
   複数 Canvas がある場合は Sort Order で明示的に管理すること。

4. **Scale with Screen Size の設定**
   Reference Resolution は制作解像度（1920x1080 など）に合わせ、
   Match Width or Height は 0.5 程度（縦横折衷）に設定する。

5. **ゲームパッドの初期フォーカス**
   ゲームパッドで操作する場合、画面表示時に必ず最初の選択要素に
   `unity_ui_navigation` の `setFirstSelected` でフォーカスを当てること。

6. **UI アニメーションと Time.timeScale**
   ポーズ中のアニメーションには `Time.unscaledDeltaTime` を使用すること。
   `timeScale = 0` にすると `Time.deltaTime` が 0 になりアニメーションが止まる。

---

## 関連ツール一覧

| ツール名 | 用途 |
|---|---|
| `unity_ui_foundation` | Canvas・Panel・Button 等の基本 UI 要素作成・show/hide/toggle |
| `unity_ui_state` | **ゲーム操作/メニュー切替の標準ツール**。画面モード（gameplay/paused/inventory等）の定義・排他切替 |
| `unity_ui_navigation` | キーボード/ゲームパッドナビゲーション |
| `unity_gamekit_ui(widgetType='command')` | ボタンアクション・画面切り替えコマンド |
| `unity_gamekit_ui(widgetType='binding')` | データから UI への自動バインディング |
| `unity_gamekit_ui(widgetType='list')` | 動的リスト・グリッド表示 |
| `unity_gamekit_ui(widgetType='slot')` | スロットバー・装備スロット |
| `unity_gamekit_ui(widgetType='selection')` | 選択状態の管理・タブ切り替え |
| `unity_event_wiring` | ボタンと処理の接続 |
| `unity_input_profile` | ポーズ・インベントリ等の入力設定 |
| `unity_uitk_document` | UI Toolkit UIDocument の管理 |
| `unity_uitk_asset` | UXML・USS アセットの生成 |
| `unity_compilation_await` | コード生成後のコンパイル完了待ち |
| `unity_validate_integrity` | UI スクリプトの整合性チェック |
