# マルチシーン設計 ワークフローガイド (Unity-AI-Forge v{VERSION})

大規模ゲームに対応するマルチシーン設計パターン。シーンの分割・加算ロード・遷移管理のベストプラクティスをMCPツール操作例と共に解説します。

---

## パイプライン位置

```
企画 → 設計 → [プロジェクト初期設定 + マルチシーン設計] → プロトタイプ → アルファ → ベータ → リリース
```

本ガイドはプロジェクト初期設定（`game_workflow_guide(phase='project_setup')`）の**補助ガイド**です。複数シーンを使う中〜大規模プロジェクトで、project_setup と並行して参照してください。シンプルなプロジェクトでは project_setup のシーン作成のみで十分です。

**前提**: 企画フェーズでシーン一覧が定義済み（`game_workflow_guide(phase='planning')`）。設計フェーズでシーン間の遷移関係とManagerの配置方針が決定済み（`game_workflow_guide(phase='design')`）。

---

## 概要

小規模ゲームは1シーンで完結しますが、機能が増えると「UIオーバーレイの共有」「オーディオマネージャーの永続化」「レベル単位での独立ロード」が必要になります。Unityのマルチシーン（Additive Loading）とDontDestroyOnLoadを組み合わせることで、メモリ効率と開発効率を両立できます。

**マルチシーン設計の原則:**
- **Boot シーン**: 最初に1回だけロードされ、必要なManagerを初期化してGameシーンへ遷移
- **Manager シーン**: AudioManager・InputManager等の永続オブジェクトを Additive でロード
- **UI シーン**: HUD・メニュー等のオーバーレイを Additive でロード（常時表示）
- **Level シーン**: 各ステージのゲームプレイ内容を Single モードで差し替え
- **シーンは単一責任**: 1シーンに1つの役割を持たせる

---

## ワークフロー概要

```
Boot シーン作成 → Manager シーン作成（Additive常駐）
→ UI オーバーレイシーン作成 → Level シーン群作成
→ Build Settings 登録 → 遷移グラフ検証 → 動作確認
```

---

## 推奨手順

1. **フォルダ構造の整備** - Scenesフォルダをカテゴリ別に整理
2. **シーン一括作成** - 全シーンを作成
3. **Build Settingsへの登録** - 全シーンをBuild Settingsに登録
4. **Boot シーン構築** - アプリ起動時の初期化シーン
5. **Manager シーン構築** - AudioManager・InputManager等の永続シーン
6. **UI シーン構築** - HUD・メニューのオーバーレイシーン
7. **Level シーン群構築** - ステージごとのゲームプレイシーン
8. **シーン遷移グラフで確認** - scene_relationship_graphで設計を検証

---

## 推奨シーン構成

```
Assets/Scenes/
  Boot.unity           # 起動・初期化シーン (Build Index: 0)
  Managers.unity       # 永続Managerシーン (Additive常駐)
  UI/
    MainMenu.unity     # メインメニュー
    GameHUD.unity      # ゲームプレイHUD (Additive常駐)
    PauseMenu.unity    # ポーズメニュー
    GameOver.unity     # ゲームオーバー画面
  Levels/
    Level01.unity      # ステージ1
    Level02.unity      # ステージ2
    Level03.unity      # ステージ3
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: シーン一括作成と Build Settings 登録

```python
# シーンを作成（scene_crud に addToBuildSettings はない）
unity_scene_crud(operation='create', scenePath='Assets/Scenes/Boot.unity')
unity_scene_crud(operation='create', scenePath='Assets/Scenes/Managers.unity')
unity_scene_crud(operation='create', scenePath='Assets/Scenes/UI/MainMenu.unity')
unity_scene_crud(operation='create', scenePath='Assets/Scenes/UI/GameHUD.unity')
unity_scene_crud(operation='create', scenePath='Assets/Scenes/UI/PauseMenu.unity')
unity_scene_crud(operation='create', scenePath='Assets/Scenes/UI/GameOver.unity')
unity_scene_crud(operation='create', scenePath='Assets/Scenes/Levels/Level01.unity')
unity_scene_crud(operation='create', scenePath='Assets/Scenes/Levels/Level02.unity')

# Build Settings に個別登録（projectSettings_crud を使用）
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/Boot.unity')
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/Managers.unity')
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/UI/MainMenu.unity')
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/UI/GameHUD.unity')
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/UI/PauseMenu.unity')
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/UI/GameOver.unity')
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/Levels/Level01.unity')
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/Levels/Level02.unity')

# Boot を index 0 に配置するために並び替え（fromIndex → toIndex で移動）
# addSceneToBuild の登録順で Boot が先頭でない場合に使用
unity_projectSettings_crud(operation='reorderBuildScenes',
    fromIndex=3, toIndex=0)  # Boot のインデックスを 0 に移動
```

### Step 2: Boot シーンの構築

```python
# Boot シーンをロード（additive=False で Single ロード）
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/Boot.unity', additive=False)

# BootManager GameObject を作成
unity_gameobject_crud(operation='create', name='BootManager')

# Boot シーン用スクリプト生成
unity_asset_crud(operation='create',
    assetPath='Assets/Scripts/Boot/BootManager.cs',
    content='''using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootManager : MonoBehaviour
{
    [SerializeField] private string managersScene = "Managers";
    [SerializeField] private string firstScene    = "MainMenu";

    IEnumerator Start()
    {
        // Managers シーンを Additive でロード
        var op = SceneManager.LoadSceneAsync(managersScene, LoadSceneMode.Additive);
        // Additive ロード完了を待つ（完了前に Single ロードすると Managers が破棄される）
        yield return op;

        // メインメニューへ遷移（Single ロードで Boot は破棄される）
        // Managers 内の DontDestroyOnLoad オブジェクトは残る
        SceneManager.LoadScene(firstScene, LoadSceneMode.Single);
    }
}''')

unity_compilation_await(operation='await', timeoutSeconds=30)
unity_component_crud(operation='add',
    gameObjectPath='BootManager', componentType='BootManager')

unity_scene_crud(operation='save', scenePath='Assets/Scenes/Boot.unity')
```

### Step 3: Manager シーンの構築 (DontDestroyOnLoad)

```python
# Manager シーンをロード
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/Managers.unity', additive=False)

# GameManager (DontDestroyOnLoad)
# ※ GameManager の完全な実装（状態管理・スコア・イベント等）は
#   アルファフェーズで作成します: game_workflow_guide(phase='alpha')
#   ここではシーン構造を確認するための空 GameObject のみ配置します。
unity_gameobject_crud(operation='create', name='GameManager')

# AudioManager (DontDestroyOnLoad)
unity_gameobject_crud(operation='create', name='AudioManager')

# InputManager
unity_gameobject_crud(operation='create', name='InputManager')

# オーディオを Managers シーンで管理 (GameKit Audio)
unity_gamekit_audio(operation='create',
    targetPath='AudioManager',
    audioId='bgm_manager',
    audioType='music',
    loop=True)

unity_compilation_await(operation='await', timeoutSeconds=30)
unity_scene_crud(operation='save', scenePath='Assets/Scenes/Managers.unity')
```

### Step 4: GameHUD シーンの構築 (Additive常駐)

```python
# HUD シーンをロード
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/UI/GameHUD.unity', additive=False)

# HUD Canvas
unity_ui_foundation(operation='createCanvas',
    name='HUDCanvas',
    renderMode='ScreenSpaceOverlay',
    sortingOrder=10)

# HUD の内容
unity_ui_hierarchy(operation='create',
    parentPath='HUDCanvas',
    hierarchy={
        'type': 'panel', 'name': 'HUD',
        'children': [
            {'type': 'image', 'name': 'HPBarBG',   'anchors': 'topLeft'},
            {'type': 'image', 'name': 'HPBarFill',  'anchors': 'topLeft'},
            {'type': 'text',  'name': 'ScoreText',  'text': '0',
             'anchors': 'topCenter', 'fontSize': 32},
            {'type': 'text',  'name': 'TimerText',  'text': '00:00',
             'anchors': 'topRight', 'fontSize': 24}
        ]
    })

# HP バインディング
unity_gamekit_ui_binding(operation='create',
    targetPath='HUDCanvas/HUD/HPBarFill',
    bindingId='hud_hp',
    sourceType='health',
    sourceId='player_hp',
    format='ratio')

unity_compilation_await(operation='await', timeoutSeconds=30)
unity_scene_crud(operation='save', scenePath='Assets/Scenes/UI/GameHUD.unity')
```

### Step 5: PauseMenu シーンの構築

```python
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/UI/PauseMenu.unity', additive=False)

unity_ui_foundation(operation='createCanvas',
    name='PauseCanvas',
    renderMode='ScreenSpaceOverlay',
    sortingOrder=20)

unity_ui_hierarchy(operation='create',
    parentPath='PauseCanvas',
    hierarchy={
        'type': 'panel', 'name': 'PausePanel',
        'children': [
            {'type': 'text',   'name': 'Title',       'text': '一時停止', 'fontSize': 48},
            {'type': 'button', 'name': 'ResumeBtn',   'text': '再開'},
            {'type': 'button', 'name': 'SettingsBtn',  'text': '設定'},
            {'type': 'button', 'name': 'QuitBtn',      'text': 'タイトルへ'}
        ],
        'layout': 'Vertical', 'spacing': 20
    })

# キーボード/ゲームパッドナビゲーション
unity_ui_navigation(operation='autoSetup',
    rootPath='PauseCanvas/PausePanel',
    direction='vertical')

unity_scene_crud(operation='save', scenePath='Assets/Scenes/UI/PauseMenu.unity')
```

### Step 6: Level シーンの構築

```python
# Level01 をロード
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/Levels/Level01.unity', additive=False)

# Levelの階層構造 (推奨)
unity_gameobject_crud(operation='create', name='_Environment')
unity_gameobject_crud(operation='create', name='_Characters')
unity_gameobject_crud(operation='create', name='_Enemies')
unity_gameobject_crud(operation='create', name='_Items')
unity_gameobject_crud(operation='create', name='_FX')
unity_gameobject_crud(operation='create', name='_Lighting')

# タイルマップで地形構築
unity_tilemap_bundle(operation='createTilemap',
    tilemapName='GroundTilemap',
    tilesetPath='Assets/Textures/Tileset.png',
    gridParentPath='_Environment')

# ライティング設定
unity_light_bundle(operation='createLightingSetup', setupPreset='daylight')

# LevelManager
unity_gameobject_crud(operation='create', name='LevelManager')
unity_asset_crud(operation='create',
    assetPath='Assets/Scripts/Levels/LevelManager.cs',
    content='''using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private string nextLevel = "Level02";
    [SerializeField] private string hudScene  = "GameHUD";

    void Start()
    {
        // HUD シーンを Additive でロード
        if (!SceneManager.GetSceneByName(hudScene).isLoaded)
            SceneManager.LoadScene(hudScene, LoadSceneMode.Additive);
    }

    public void LoadNextLevel() =>
        SceneManager.LoadScene(nextLevel, LoadSceneMode.Single);

    public void ReturnToMenu() =>
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
}''')

unity_compilation_await(operation='await', timeoutSeconds=30)
unity_component_crud(operation='add',
    gameObjectPath='LevelManager', componentType='LevelManager')
unity_scene_crud(operation='save', scenePath='Assets/Scenes/Levels/Level01.unity')
```

### Step 7: Build Settings の確認と整理

```python
# 現在のBuild Settingsに登録されたシーン一覧を確認
unity_projectSettings_crud(operation='listBuildScenes')

# Boot を index 0 に配置（fromIndex → toIndex で移動）
unity_projectSettings_crud(operation='reorderBuildScenes',
    fromIndex=3, toIndex=0)  # Boot のインデックスを 0 に移動

# Build Settings のバリデーション
unity_scene_relationship_graph(operation='validateBuildSettings')
```

### Step 8: シーン遷移グラフで設計を検証

```python
# 全シーンの遷移関係を解析
unity_scene_relationship_graph(operation='analyzeAll')

# 特定シーンへの遷移元を確認（削除前の安全確認）
unity_scene_relationship_graph(operation='findTransitionsTo',
    sceneName='Level01')

# 特定シーンからの遷移先を確認
unity_scene_relationship_graph(operation='findTransitionsFrom',
    sceneName='Boot')

# シーン内の孤立オブジェクト確認
unity_scene_reference_graph(operation='findOrphans')

# 整合性チェック
unity_validate_integrity(operation='all')
```

---

## チェックリスト

### シーン作成
- [ ] Boot シーン (index 0) を作成した
- [ ] Managers シーン (Additive常駐) を作成した
- [ ] GameHUD シーン (Additive常駐) を作成した
- [ ] MainMenu シーンを作成した
- [ ] Level シーン群を作成した
- [ ] projectSettings_crud(addSceneToBuild) で全シーンを登録した
- [ ] projectSettings_crud(reorderBuildScenes) で順序を設定した

### Boot・Manager
- [ ] BootManager で Additive ロードと初期遷移を実装した
- [ ] GameManager が DontDestroyOnLoad を設定している
- [ ] AudioManager が DontDestroyOnLoad を設定している

### Level シーン
- [ ] Level 階層構造 (_Environment, _Characters 等) を作成した
- [ ] LevelManager が HUD シーンを Additive でロードしている

### 検証
- [ ] scene_relationship_graph(analyzeAll) でシーン遷移を確認した
- [ ] scene_relationship_graph(validateBuildSettings) でビルド設定を確認した
- [ ] validate_integrity(all) で整合性確認した

---

## 注意点・落とし穴

**scene_crud に addToBuildSettings パラメータはない**
シーン作成後、projectSettings_crud(operation='addSceneToBuild') で個別に登録してください。

**scene_crud のロードモードは additive パラメータで指定**
`additive=True` で加算ロード、`additive=False` でSingleロードです。`loadMode` パラメータは存在しません。

**DontDestroyOnLoad の重複に注意**
Singleton パターンで既存インスタンスがある場合は即座に Destroy するロジックが必須です。シーンをリロードするたびに同じManagerが増えます。

**HUD シーンを Additive でロードする場所**
Boot から MainMenu 時点では HUD は不要です。LevelManager のStart() でロードするのが適切です。HUD は Level シーンと一緒にアンロードしないよう注意。

**Single ロードは現在のシーンを全てアンロードする**
Level01 から Level02 へ Single でロードすると、Level01 が消えます。Additive でロードしたシーンも一緒にアンロードされます。Managers シーン内の **全ての永続オブジェクト** に `DontDestroyOnLoad` を設定してください（GameManager, AudioManager, EventSystem 等）。`DontDestroyOnLoad` を設定していないオブジェクトは Single ロード時に破棄されます。

**Build Settings の Index 0 は必ず Boot にする**
ビルドは Index 0 のシーンから起動します。reorderBuildScenes で Boot を先頭に配置してください。

**scene_relationship_graph は Editor 内シーンのみ解析**
実行時の動的ロードはグラフに反映されません。遷移スクリプト内の SceneManager.LoadScene の文字列と実際のシーンパスを一致させてください。

**シーン間でオブジェクト参照を持たない**
シーンをまたぐ GameObject の直接参照（SerializeField でのシーン外参照）は Unity では不可能です。Manager のシングルトンやイベント経由で通信してください。

---

## 各シーン種別の推奨階層

### Boot.unity
```
BootManager
```

### Managers.unity
```
GameManager      (DontDestroyOnLoad)
AudioManager     (DontDestroyOnLoad)
InputManager     (DontDestroyOnLoad)
EventSystem
```

### GameHUD.unity
```
HUDCanvas
  HUD
    HPBarBG / HPBarFill
    ScoreText
    TimerText
    MiniMap
```

### Level01.unity
```
LevelManager
_Environment
  Ground / Tilemap
  Walls
_Characters
  Player
_Enemies
  EnemySpawner
_Items
_FX
_Lighting
  DirectionalLight
  Ambience
```

---

## 関連ツール一覧

| ツール | マルチシーン設計での用途 |
|--------|------------------------|
| `unity_scene_crud` | create / load (additive=True/False) / save |
| `unity_projectSettings_crud` | addSceneToBuild / reorderBuildScenes / listBuildScenes |
| `unity_gameobject_crud` | シーン内オブジェクト作成 |
| `unity_component_crud` | DontDestroyOnLoad 等のコンポーネント追加 |
| `unity_asset_crud` | Manager・LevelManager スクリプト生成 |
| `unity_ui_foundation` | 各シーンのCanvas作成 |
| `unity_ui_hierarchy` | HUD・メニューの宣言的構築 |
| `unity_ui_navigation` | メニューのキーボードナビゲーション |
| `unity_gamekit_ui_binding` | HUDデータバインディング |
| `unity_gamekit_audio` | BGM・SE管理 |
| `unity_light_bundle` | Level シーンのライティング |
| `unity_tilemap_bundle` | Level シーンの地形構築 |
| `unity_scene_relationship_graph` | analyzeAll / validateBuildSettings / findTransitionsTo |
| `unity_scene_reference_graph` | findOrphans / findReferencesTo |
| `unity_validate_integrity` | all で整合性チェック |
| `unity_compilation_await` | スクリプト生成後のコンパイル待機 |
