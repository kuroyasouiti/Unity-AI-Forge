# シーン構築 ワークフローガイド (Unity-AI-Forge v{VERSION})

プロジェクト初期設定で作成した空シーンに、階層構造・Manager・カメラ・ライティング・UI Canvas を配置し、プロトタイプフェーズで即座にゲームプレイを構築できる状態にするためのガイドです。

---

## パイプライン位置

```
企画 → 設計 → プロジェクト初期設定 → [シーン構築] → プロトタイプ → アルファ → ベータ → リリース
```

**前提**: プロジェクト初期設定でタグ・レイヤー・フォルダ構造・空シーン・Build Settings 登録が完了済み（`game_workflow_guide(phase='project_setup')`）。設計フェーズで決定したシーン構成・クラス設計に基づいてシーンを構築します。

---

## 概要

シーン構築フェーズの目的は「プロトタイプを始められるシーン基盤を用意する」ことです。プロジェクト初期設定で作成した空シーンに、Boot ロジック・Manager 階層・カメラ・ライティング・UI Canvas の骨格を配置します。ゲームプレイのコンテンツ（プレイヤー・敵・アイテム等）はプロトタイプフェーズで追加するため、このフェーズではシーンのインフラストラクチャに集中します。

**シーン構築の原則:**
- 空シーンに「構造」を入れる。ゲームプレイコンテンツはまだ入れない
- Boot → Manager → UI → Level の順に構築する
- 各シーンの階層構造（空の親 GameObject）を先に作る
- カメラ・ライティングは最低限のデフォルトを配置する
- UI Canvas は構造だけ作り、中身はプロトタイプで追加する
- シーン遷移スクリプトは最小限（Boot → MainMenu → Level の流れ）
- 単一シーンプロジェクトの場合は Level シーンの階層構造のみで十分

---

## ワークフロー概要

```
Boot シーン構築 → Manager シーン構築 → UI シーン構築
→ Level シーン階層構築 → カメラ・ライティング配置
→ シーン遷移スクリプト作成 → Build Settings 検証
```

---

## 推奨手順

1. **Boot シーンの構築** - BootManager を配置し、起動フローを定義
2. **Manager シーンの構築** - 永続 Manager オブジェクトを配置
3. **UI シーンの構築** - メインメニュー・HUD の Canvas 骨格を作成
4. **Level シーンの階層構築** - カテゴリ別の親 GameObject を配置
5. **カメラ・ライティングの配置** - デフォルトのカメラとライトを設定
6. **シーン遷移スクリプトの作成** - 最小限の遷移ロジックを実装
7. **Build Settings の検証** - シーン遷移グラフで設計を確認
8. **整合性チェック** - validate_integrity で確認

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: Boot シーンの構築

Boot シーンはアプリ起動時に最初にロードされ、Manager シーンの Additive ロードと最初の画面への遷移を担当します。

```python
# Boot シーンをロード
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/Boot.unity', additive=False)

# BootManager GameObject を作成
unity_gameobject_crud(operation='create', name='BootManager')

# BootManager スクリプト生成
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
        yield return op;

        // 最初の画面へ遷移（Single ロードで Boot は破棄される）
        SceneManager.LoadScene(firstScene, LoadSceneMode.Single);
    }
}''')

unity_compilation_await(operation='await', timeoutSeconds=30)
unity_component_crud(operation='add',
    gameObjectPath='BootManager', componentType='BootManager')

# シーンを保存
unity_scene_crud(operation='save', scenePath='Assets/Scenes/Boot.unity')
```

**単一シーンプロジェクトの場合**: Boot シーンは不要です。Step 4 の Level シーン階層構築から始めてください。

### Step 2: Manager シーンの構築

Manager シーンには DontDestroyOnLoad で永続化する Manager オブジェクトを配置します。

```python
# Manager シーンをロード
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/Managers.unity', additive=False)

# Manager の階層構造を作成
unity_gameobject_crud(operation='create', name='GameManager')
unity_gameobject_crud(operation='create', name='AudioManager')
unity_gameobject_crud(operation='create', name='InputManager')

# AudioManager に BGM 管理を設定
unity_gamekit_audio(operation='create',
    targetPath='AudioManager',
    audioId='bgm_manager',
    audioType='music',
    loop=True)

unity_compilation_await(operation='await', timeoutSeconds=30)

# シーンを保存
unity_scene_crud(operation='save', scenePath='Assets/Scenes/Managers.unity')
```

**注意**: GameManager・AudioManager 等の完全な実装（Singleton、DontDestroyOnLoad、状態管理）はアルファフェーズ（`game_workflow_guide(phase='alpha')`）で行います。ここではシーンの構造として GameObject を配置するのみです。

### Step 3: UI シーンの構築

UI シーンには Canvas の骨格を配置します。ボタンやテキスト等の具体的な UI 要素はプロトタイプフェーズで追加します。

#### MainMenu シーン

```python
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/UI/MainMenu.unity', additive=False)

# メインメニュー Canvas
unity_ui_foundation(operation='createCanvas',
    name='MenuCanvas',
    renderMode='ScreenSpaceOverlay',
    sortingOrder=0)

# メニュー構造の骨格（中身はプロトタイプで追加）
unity_ui_hierarchy(operation='create',
    parentPath='MenuCanvas',
    hierarchy={
        'type': 'panel', 'name': 'MainMenuPanel',
        'children': [
            {'type': 'text',   'name': 'TitleText',   'text': 'Game Title', 'fontSize': 48},
            {'type': 'button', 'name': 'StartBtn',    'text': 'Start'},
            {'type': 'button', 'name': 'SettingsBtn', 'text': 'Settings'},
            {'type': 'button', 'name': 'QuitBtn',     'text': 'Quit'}
        ],
        'layout': 'Vertical', 'spacing': 24
    })

unity_scene_crud(operation='save', scenePath='Assets/Scenes/UI/MainMenu.unity')
```

#### GameHUD シーン

```python
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/UI/GameHUD.unity', additive=False)

# HUD Canvas（Additive ロードで Level シーンに重ねる）
unity_ui_foundation(operation='createCanvas',
    name='HUDCanvas',
    renderMode='ScreenSpaceOverlay',
    sortingOrder=10)

# HUD の骨格（バインディングはアルファで追加）
unity_ui_hierarchy(operation='create',
    parentPath='HUDCanvas',
    hierarchy={
        'type': 'panel', 'name': 'HUD',
        'children': [
            {'type': 'text', 'name': 'ScoreText', 'text': 'Score: 0', 'fontSize': 24},
            {'type': 'text', 'name': 'HPText',    'text': 'HP: 100',  'fontSize': 24},
            {'type': 'text', 'name': 'TimerText', 'text': 'Time: 0',  'fontSize': 20}
        ],
        'layout': 'Vertical', 'spacing': 8
    })

unity_scene_crud(operation='save', scenePath='Assets/Scenes/UI/GameHUD.unity')
```

#### GameOver シーン

```python
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/UI/GameOver.unity', additive=False)

unity_ui_foundation(operation='createCanvas',
    name='GameOverCanvas',
    renderMode='ScreenSpaceOverlay',
    sortingOrder=20)

unity_ui_hierarchy(operation='create',
    parentPath='GameOverCanvas',
    hierarchy={
        'type': 'panel', 'name': 'GameOverPanel',
        'children': [
            {'type': 'text',   'name': 'GameOverText', 'text': 'Game Over', 'fontSize': 48},
            {'type': 'text',   'name': 'FinalScore',   'text': 'Score: 0',  'fontSize': 24},
            {'type': 'button', 'name': 'RetryBtn',     'text': 'Retry'},
            {'type': 'button', 'name': 'TitleBtn',     'text': 'Title'}
        ],
        'layout': 'Vertical', 'spacing': 16
    })

unity_scene_crud(operation='save', scenePath='Assets/Scenes/UI/GameOver.unity')
```

### Step 4: Level シーンの階層構築

Level シーンにカテゴリ別の親 GameObject を配置します。実際のゲームオブジェクト（プレイヤー・敵等）はプロトタイプフェーズで追加します。

```python
# Level01 をロード
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/Levels/Level01.unity', additive=False)

# カテゴリ別の親 GameObject を作成（推奨階層）
unity_gameobject_crud(operation='create', name='_Environment')
unity_gameobject_crud(operation='create', name='_Characters')
unity_gameobject_crud(operation='create', name='_Enemies')
unity_gameobject_crud(operation='create', name='_Items')
unity_gameobject_crud(operation='create', name='_FX')
unity_gameobject_crud(operation='create', name='_Lighting')
unity_gameobject_crud(operation='create', name='LevelManager')

# シーンを保存
unity_scene_crud(operation='save', scenePath='Assets/Scenes/Levels/Level01.unity')
```

**命名規則**: カテゴリ親には `_` プレフィックスを付けることで、Hierarchy ビューで上部にソートされ、見つけやすくなります。

**単一シーンプロジェクトの場合**: この Step のみで十分です。Manager オブジェクトも同じシーン内に配置してください。

### Step 5: カメラ・ライティングの配置

各 Level シーンにデフォルトのカメラとライティングを配置します。

```python
# Level01 をロード（前の Step から続けている場合は不要）
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/Levels/Level01.unity', additive=False)

# デフォルトライティングセットアップ
unity_light_bundle(operation='createLightingSetup', setupPreset='daylight')

# カメラリグ（プロトタイプで target を設定）
# ※ targetPath は Player 配置後にプロトタイプで変更する
unity_camera_rig(operation='createRig', rigType='follow',
    rigName='MainCam',
    targetPath='',
    offset={'x': 0, 'y': 5, 'z': -10},
    smoothTime=0.3)

# シーンを保存
unity_scene_crud(operation='save', scenePath='Assets/Scenes/Levels/Level01.unity')
```

### Step 6: シーン遷移スクリプトの作成

最小限のシーン遷移ロジックを作成します。

```python
# LevelManager スクリプト（シーン遷移の基本）
unity_asset_crud(operation='create',
    assetPath='Assets/Scripts/Managers/LevelManager.cs',
    content='''using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private string nextLevel = "";
    [SerializeField] private string hudScene  = "GameHUD";

    void Start()
    {
        // HUD シーンを Additive でロード
        if (!string.IsNullOrEmpty(hudScene)
            && !SceneManager.GetSceneByName(hudScene).isLoaded)
            SceneManager.LoadScene(hudScene, LoadSceneMode.Additive);
    }

    public void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(nextLevel))
            SceneManager.LoadScene(nextLevel, LoadSceneMode.Single);
    }

    public void ReturnToMenu() =>
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
}''')

unity_compilation_await(operation='await', timeoutSeconds=30)

# LevelManager をシーンに配置
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/Levels/Level01.unity', additive=False)
unity_component_crud(operation='add',
    gameObjectPath='LevelManager', componentType='LevelManager')

unity_scene_crud(operation='save', scenePath='Assets/Scenes/Levels/Level01.unity')
```

### Step 7: Build Settings の検証

```python
# Build Settings に登録されたシーン一覧を確認
unity_projectSettings_crud(operation='listBuildScenes')

# Boot が index 0 であることを確認
# 必要に応じて並び替え
unity_projectSettings_crud(operation='reorderBuildScenes',
    fromIndex=3, toIndex=0)

# シーン遷移グラフで設計を検証
unity_scene_relationship_graph(operation='analyzeAll')

# Build Settings のバリデーション
unity_scene_relationship_graph(operation='validateBuildSettings')
```

### Step 8: 整合性チェック

```python
# 全シーンの整合性を確認
unity_validate_integrity(operation='all')

# Missing Script がないか確認
unity_validate_integrity(operation='removeMissingScripts')

# 孤立オブジェクトの確認
unity_scene_reference_graph(operation='findOrphans')

# コンパイルエラー確認
unity_console_log(operation='getCompilationErrors')
```

---

## 単一シーンプロジェクトの場合

小規模・シンプルなプロジェクトでは、Boot・Manager・UI を分けず 1 シーンで完結させることも可能です。

```python
# メインシーンをロード
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/Main.unity', additive=False)

# 階層構造を作成
unity_gameobject_crud(operation='create', name='_Managers')
unity_gameobject_crud(operation='create', name='_Environment')
unity_gameobject_crud(operation='create', name='_Characters')
unity_gameobject_crud(operation='create', name='_Enemies')
unity_gameobject_crud(operation='create', name='_Items')
unity_gameobject_crud(operation='create', name='_FX')
unity_gameobject_crud(operation='create', name='_UI')

# Canvas
unity_ui_foundation(operation='createCanvas',
    name='GameCanvas',
    renderMode='ScreenSpaceOverlay')

# ライティング
unity_light_bundle(operation='createLightingSetup', setupPreset='daylight')

unity_scene_crud(operation='save', scenePath='Assets/Scenes/Main.unity')
```

---

## チェックリスト

### Boot シーン
- [ ] BootManager GameObject を配置した
- [ ] BootManager スクリプトで Managers の Additive ロードと初期遷移を実装した
- [ ] compilation_await でコンパイル完了を確認した
- [ ] シーンを保存した

### Manager シーン
- [ ] GameManager, AudioManager, InputManager を配置した
- [ ] GameKit Audio で BGM 管理の骨格を作成した
- [ ] シーンを保存した

### UI シーン
- [ ] MainMenu に Canvas と基本ボタン構造を作成した
- [ ] GameHUD に Canvas と表示テキスト構造を作成した
- [ ] GameOver に Canvas とリザルト構造を作成した
- [ ] 各シーンを保存した

### Level シーン
- [ ] カテゴリ別の親 GameObject を配置した（_Environment, _Characters, _Enemies, _Items, _FX, _Lighting）
- [ ] LevelManager を配置した
- [ ] デフォルトのライティングを設定した
- [ ] カメラリグを配置した
- [ ] シーンを保存した

### シーン遷移
- [ ] BootManager で Managers のロードと初期遷移を実装した
- [ ] LevelManager で HUD のロードとシーン遷移を実装した
- [ ] compilation_await でコンパイル完了を確認した

### 検証
- [ ] scene_relationship_graph(analyzeAll) でシーン遷移を確認した
- [ ] scene_relationship_graph(validateBuildSettings) で Build Settings を確認した
- [ ] validate_integrity(all) で整合性確認した
- [ ] console_log(getCompilationErrors) でエラーなし確認した

---

## 注意点・落とし穴

**Manager の実装はアルファフェーズで行う**
このフェーズでは GameManager 等の GameObject を配置するだけです。Singleton パターン・DontDestroyOnLoad・状態管理の実装はアルファフェーズ（`game_workflow_guide(phase='alpha')`）で行います。

**scene_crud に addToBuildSettings パラメータはない**
シーンは project_setup で既に Build Settings に登録済みです。新しいシーンを追加した場合は projectSettings_crud(operation='addSceneToBuild') で個別に登録してください。

**scene_crud のロードモードは additive パラメータで指定**
`additive=True` で加算ロード、`additive=False`（または省略）で Single ロードです。`loadMode` パラメータは存在しません。

**スクリプト生成後は必ず compilation_await を実行する**
コンパイル前に component_crud(add) しようとするとエラーになります。

**シーン保存を忘れない**
各シーンの構築後に `scene_crud(operation='save')` を実行してください。保存前にシーンを切り替えると変更が失われます。

**UI の命名を一貫させる**
ここで付けた名前（ScoreText, HPText, TitleText 等）はプロトタイプ以降で参照されます。`game_workflow_guide(phase='project_setup')` の命名規則に従ってください。

**マルチシーン設計の詳細は scene_structure ガイドを参照**
複数シーンの Additive ロード・DontDestroyOnLoad・シーン間通信の設計パターンは `game_workflow_guide(phase='scene_structure')` で詳しく解説しています。

---

## 次のフェーズへ

シーンの基盤構築が完了したら、次はプロトタイプフェーズです:

1. **プロトタイプ** (`game_workflow_guide(phase='prototype')`) - プリミティブ配置・物理プリセット・最小スクリプトでコアループを検証
   - Level シーンの _Characters 以下にプレイヤー (Capsule) を配置
   - _Environment 以下に地面・壁 (Cube) を配置
   - 物理プリセットでキャラクター動作を有効化
   - カメラリグの target をプレイヤーに接続

シーン構築で作った階層構造・Canvas 骨格・シーン遷移をそのままプロトタイプで活用します。

---

## 関連ツール一覧

| ツール | シーン構築での用途 |
|--------|------------------|
| `unity_scene_crud` | load / save でシーンの読み書き |
| `unity_gameobject_crud` | 階層構造の親 GameObject 作成 |
| `unity_component_crud` | スクリプトのアタッチ |
| `unity_asset_crud` | Boot/LevelManager スクリプト生成 |
| `unity_ui_foundation` | Canvas 作成 |
| `unity_ui_hierarchy` | 宣言的 UI の骨格構築 |
| `unity_gamekit_audio` | AudioManager の BGM 管理設定 |
| `unity_light_bundle` | createLightingSetup でデフォルトライティング |
| `unity_camera_rig` | follow カメラリグの配置 |
| `unity_projectSettings_crud` | listBuildScenes / reorderBuildScenes |
| `unity_scene_relationship_graph` | analyzeAll / validateBuildSettings |
| `unity_scene_reference_graph` | findOrphans で孤立確認 |
| `unity_validate_integrity` | all で整合性チェック |
| `unity_console_log` | getCompilationErrors でエラー確認 |
| `unity_compilation_await` | スクリプト生成後のコンパイル待機 |
