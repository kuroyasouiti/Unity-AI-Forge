# UI/UX 設計実装ガイド (Unity-AI-Forge {VERSION})

## 概要

優れた UI/UX はゲームの操作性とユーザー体験を大きく左右します。
Unity-AI-Forge の UI Pillar（`unity_gamekit_ui_command`、`unity_gamekit_ui_binding`、
`unity_gamekit_ui_list`、`unity_gamekit_ui_slot`、`unity_gamekit_ui_selection`）を
`unity_ui_foundation`、`unity_ui_hierarchy`、`unity_ui_state`、`unity_ui_navigation` と
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
    renderMode="ScreenSpaceOverlay"
)

# UI 階層の一括構築
unity_ui_hierarchy(
    operation="create",
    parentPath="GameCanvas",
    definition={
        "name": "UIRoot",
        "children": [
            {
                "type": "Panel", "name": "HUD", "active": true,
                "children": [
                    {"type": "Text", "name": "ScoreText", "text": "スコア: 000000"},
                    {"type": "Text", "name": "HPText",    "text": "HP: 100/100"}
                ]
            },
            {
                "type": "Panel", "name": "Screens",
                "children": [
                    {"type": "Panel", "name": "TitlePanel",     "active": true},
                    {"type": "Panel", "name": "PausePanel",     "active": false},
                    {"type": "Panel", "name": "InventoryPanel", "active": false},
                    {"type": "Panel", "name": "SettingsPanel",  "active": false},
                    {"type": "Panel", "name": "ResultPanel",    "active": false}
                ]
            },
            {
                "type": "Panel", "name": "Overlays",
                "children": [
                    {"type": "Panel", "name": "DialogPanel",       "active": false},
                    {"type": "Panel", "name": "ToastNotification", "active": false},
                    {"type": "Panel", "name": "LoadingScreen",     "active": false}
                ]
            }
        ]
    }
)
```

### Step 2: HUD の構築（データバインディング）

```python
# UIBinding で HP データを UI に自動反映
unity_gamekit_ui_binding(
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
unity_gamekit_ui_command(
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

### Step 5: UI 状態管理

```python
# ui_state で画面状態を定義・管理
unity_ui_state(
    operation="defineState",
    targetPath="GameCanvas",
    stateName="Title",
    properties={
        "UIRoot/Screens/TitlePanel": {"active": true},
        "UIRoot/HUD": {"active": false},
        "UIRoot/Screens/PausePanel": {"active": false},
        "UIRoot/Screens/ResultPanel": {"active": false}
    }
)

unity_ui_state(
    operation="defineState",
    targetPath="GameCanvas",
    stateName="InGame",
    properties={
        "UIRoot/Screens/TitlePanel": {"active": false},
        "UIRoot/HUD": {"active": true},
        "UIRoot/Screens/PausePanel": {"active": false}
    }
)

unity_ui_state(
    operation="defineState",
    targetPath="GameCanvas",
    stateName="Paused",
    properties={
        "UIRoot/HUD": {"active": true},
        "UIRoot/Screens/PausePanel": {"active": true}
    }
)

# 状態を適用
unity_ui_state(
    operation="applyState",
    targetPath="GameCanvas",
    stateName="Title"
)
```

### Step 6: キーボード/ゲームパッドナビゲーション

```python
# タイトルメニューのナビゲーション設定
unity_ui_navigation(
    operation="autoSetup",
    targetPath="GameCanvas/UIRoot/Screens/TitlePanel"
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
unity_gamekit_ui_list(
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
unity_gamekit_ui_selection(
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

### パターン 1: フェードイン/アウト遷移

```python
# フェードエフェクト
unity_gamekit_effect(
    operation="create",
    effectId="screen_fade",
    components=[
        {"type": "screenFlash",
         "color": {"r": 0, "g": 0, "b": 0, "a": 1},
         "flashDuration": 0.5, "fadeTime": 0.5}
    ]
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### パターン 2: UI SE

```python
# UI サウンドエフェクト
unity_gamekit_audio(
    operation="create",
    targetPath="GameCanvas",
    audioId="ui_sfx",
    audioType="ui",
    volume=0.5
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### パターン 3: ボタンフィードバック

```python
# ボタン押下時のフィードバック
unity_gamekit_feedback(
    operation="create",
    targetPath="GameCanvas",
    feedbackId="button_feedback",
    components=[
        {"type": "scale", "scaleAmount": {"x": 0.05, "y": 0.05, "z": 0},
         "duration": 0.1},
        {"type": "sound", "soundVolume": 0.3}
    ]
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### パターン 4: 設定画面のタブ切り替え

```python
unity_gamekit_ui_selection(
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

### パターン 5: UI Toolkit との併用

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

### パターン 6: CanvasGroup によるUI制御

```python
# パネルにCanvasGroupを設定して透明度・操作性を制御
unity_ui_foundation(
    operation="configureCanvasGroup",
    gameObjectPath="GameCanvas/UIRoot/Screens/PausePanel",
    alpha=0.8,
    interactable=True,
    blocksRaycasts=True,
    ignoreParentGroups=True  # 親CanvasGroupの影響を無視
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

# show/hide でCanvasGroup経由の表示切替（SetActiveより推奨）
unity_ui_hierarchy(operation="show", targetPath="GameCanvas/UIRoot/Overlays/OverlayPanel")
unity_ui_hierarchy(operation="hide", targetPath="GameCanvas/UIRoot/Overlays/OverlayPanel")

# ui_state で ignoreParentGroups を含む状態定義
unity_ui_state(
    operation="defineState",
    rootPath="GameCanvas",
    stateName="dialog_open",
    elements=[
        {"path": "UIRoot/Overlays/DialogPanel", "active": True, "visible": True,
         "alpha": 1.0, "ignoreParentGroups": True},
        {"path": "UIRoot/Screens", "visible": False, "interactable": False}
    ]
)
```

> **推奨:** UI の表示/非表示には `SetActive(false)` よりも CanvasGroup (`alpha=0, interactable=false, blocksRaycasts=false`) を使用する。
> CanvasGroup はレイアウト再計算を発生させず、フェードアニメーションとの相性も良い。
> `ignoreParentGroups=true` を使えば、親パネルを非表示にしても子要素だけ独立して表示可能。

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
- **HUD (HP, Score, Timer)** → `unity_gamekit_ui_binding` で十分（1対1バインド）
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
| HP/スコア等のシンプルな数値表示 | `unity_gamekit_ui_binding` | 1行で完結、ロジック不要 |
| データの読み書き双方向 | MVP | Slider/Toggle の入力→Model 更新が必要 |
| フィルタ/ソートのあるリスト | MVP + `unity_gamekit_ui_list` | Presenter でデータ変換 |
| 複数画面の連動 | MVP + Event Channel | 画面間のデータ受け渡し |
| プロトタイプ段階 | `unity_gamekit_ui_binding` | 速度優先 |

---

## 注意点・落とし穴

1. **コード生成後のコンパイル待ち**
   `unity_gamekit_ui_binding`、`unity_gamekit_ui_command`、`unity_gamekit_ui_list`、
   `unity_gamekit_ui_slot`、`unity_gamekit_ui_selection` はコードを生成するため、
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
| `unity_ui_foundation` | Canvas・Panel・Button 等の基本 UI 要素作成 |
| `unity_ui_hierarchy` | 宣言的な UI 階層の一括構築 |
| `unity_ui_state` | 画面状態の定義・管理・遷移 |
| `unity_ui_navigation` | キーボード/ゲームパッドナビゲーション |
| `unity_gamekit_ui_command` | ボタンアクション・画面切り替えコマンド |
| `unity_gamekit_ui_binding` | データから UI への自動バインディング |
| `unity_gamekit_ui_list` | 動的リスト・グリッド表示 |
| `unity_gamekit_ui_slot` | スロットバー・装備スロット |
| `unity_gamekit_ui_selection` | 選択状態の管理・タブ切り替え |
| `unity_gamekit_effect` | フェードイン/アウト等の画面効果 |
| `unity_gamekit_feedback` | ボタン押下時のスケールフィードバック |
| `unity_gamekit_audio` | UI SE（ボタン音・決定音・キャンセル音） |
| `unity_event_wiring` | ボタンと処理の接続 |
| `unity_input_profile` | ポーズ・インベントリ等の入力設定 |
| `unity_uitk_document` | UI Toolkit UIDocument の管理 |
| `unity_uitk_asset` | UXML・USS アセットの生成 |
| `unity_compilation_await` | コード生成後のコンパイル完了待ち |
| `unity_validate_integrity` | UI スクリプトの整合性チェック |
