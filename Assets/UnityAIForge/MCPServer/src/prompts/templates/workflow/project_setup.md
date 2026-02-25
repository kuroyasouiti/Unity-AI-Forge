# プロジェクト初期設定 ワークフローガイド (Unity-AI-Forge v{VERSION})

新規Unityプロジェクトを正しく初期設定するためのワークフローガイドです。タグ・レイヤー設定からフォルダ構造、入力設定、初期シーン作成まで、プロジェクト開始時に行うべき作業を網羅します。

---

## パイプライン位置

```
企画 → 設計 → [プロジェクト初期設定] → シーン構築 → プロトタイプ → アルファ → ベータ → リリース
```

**前提**: 設計フェーズでデザインパターン・クラス構造・UML図が決定済み（`game_workflow_guide(phase='design')`）。設計で定義したタグ・レイヤー・シーン構成をもとにプロジェクトの骨格を構築します。

---

## 概要

プロジェクトの初期設定は後から変更が難しい項目を含みます。タグ・レイヤーの設計ミスは後続の物理処理や描画に影響し、フォルダ構造の乱れはチーム開発・大規模化時に問題を起こします。最初に正しく設定することで、以降の開発をスムーズに進められます。

**初期設定の原則:**
- タグ・レイヤーを先に設計してから実装を始める
- フォルダ構造はプロジェクト初日に決める
- 入力システム（New Input System）は早期に設定する
- Build Settings に全シーンを登録する習慣をつける
- 命名規則を統一する（PascalCase for scripts, kebab-case for assets）

---

## ワークフロー概要

```
プロジェクト設定確認 → タグ・レイヤー設計 → フォルダ構造作成
→ 入力設定 → 初期シーン群作成 → Build Settings 登録
→ 命名規則・品質設定 → validate_integrity で確認
```

---

## 推奨フォルダ構造

```
Assets/
  Audio/
    BGM/
    SFX/
    Ambient/
  Animations/
    Characters/
    UI/
  Materials/
    Characters/
    Environment/
    UI/
  Prefabs/
    Characters/
    Enemies/
    Items/
    UI/
    FX/
  Scenes/
    Levels/
    UI/
    Shared/
  Scripts/
    Boot/
    Characters/
    Enemies/
    Managers/
    UI/
    Utilities/
  Sprites/
    Characters/
    Environment/
    UI/
  Textures/
  UI/
    UXML/
    USS/
  Data/
    ScriptableObjects/
```

---

## 推奨手順

1. **Project Settings の確認** - Unity バージョン・会社名・製品名
2. **タグの追加** - ゲーム固有のタグを登録
3. **レイヤーの設計** - 物理・描画用レイヤーを定義
4. **フォルダ構造の作成** - 上記の推奨構造を作成
5. **入力プロファイルの設定** - New Input System のアクションマップ
6. **品質設定** - Quality Settings, Physics Settings
7. **初期シーン群の作成** - Boot/MainMenu/GameHUD 等
8. **Build Settings への登録** - 全シーンを正しい順序で登録
9. **Data フォルダの準備** - SO 型定義はアルファフェーズで実施
10. **整合性確認** - validate_integrity + class_catalog で設定を確認

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: Project Settings の確認・更新

```python
# 現在の設定を確認
unity_projectSettings_crud(operation='read', category='player')

# 会社名・製品名・バージョンを設定
unity_projectSettings_crud(operation='write', category='player',
    property='companyName', value='MyStudio')

unity_projectSettings_crud(operation='write', category='player',
    property='productName', value='MyGame')

unity_projectSettings_crud(operation='write', category='player',
    property='bundleVersion', value='0.1.0')

# ターゲット解像度・フレームレートを設定
unity_projectSettings_crud(operation='write', category='quality',
    property='vSyncCount', value=1)

unity_projectSettings_crud(operation='write', category='time',
    property='fixedDeltaTime', value=0.02)  # 50fps物理
```

### Step 2: タグの設計と登録

```python
# ゲーム固有タグを追加
unity_projectSettings_crud(operation='write', category='tagsLayers',
    property='addTag', value='Player')

unity_projectSettings_crud(operation='write', category='tagsLayers',
    property='addTag', value='Enemy')

unity_projectSettings_crud(operation='write', category='tagsLayers',
    property='addTag', value='Item')

unity_projectSettings_crud(operation='write', category='tagsLayers',
    property='addTag', value='Projectile')

unity_projectSettings_crud(operation='write', category='tagsLayers',
    property='addTag', value='Ground')

unity_projectSettings_crud(operation='write', category='tagsLayers',
    property='addTag', value='Trigger')

unity_projectSettings_crud(operation='write', category='tagsLayers',
    property='addTag', value='Collectible')

# タグ登録を確認
unity_projectSettings_crud(operation='read', category='tagsLayers')
```

### Step 3: レイヤーの設計と登録

```python
# 推奨レイヤー設計 (User Layer 6 以降)
# Layer 6: Player
# Layer 7: Enemy
# Layer 8: Projectile
# Layer 9: Ground
# Layer 10: Item
# Layer 11: Trigger (非物理のトリガー)
# Layer 12: UI3D (ワールドスペースUI)

unity_projectSettings_crud(operation='write', category='tagsLayers',
    property='addLayer', value={'index': 6, 'name': 'Player'})

unity_projectSettings_crud(operation='write', category='tagsLayers',
    property='addLayer', value={'index': 7, 'name': 'Enemy'})

unity_projectSettings_crud(operation='write', category='tagsLayers',
    property='addLayer', value={'index': 8, 'name': 'Projectile'})

unity_projectSettings_crud(operation='write', category='tagsLayers',
    property='addLayer', value={'index': 9, 'name': 'Ground'})

unity_projectSettings_crud(operation='write', category='tagsLayers',
    property='addLayer', value={'index': 10, 'name': 'Item'})

unity_projectSettings_crud(operation='write', category='tagsLayers',
    property='addLayer', value={'index': 11, 'name': 'Trigger'})

unity_projectSettings_crud(operation='write', category='tagsLayers',
    property='addLayer', value={'index': 12, 'name': 'UI3D'})
```

### Step 4: 物理レイヤーマトリクスの設定

```python
# Player と Enemy の衝突を有効
unity_projectSettings_crud(operation='write', category='physics2d',
    property='layerCollisionMatrix',
    value={
        'layers': ['Player', 'Enemy', 'Projectile', 'Ground', 'Item', 'Trigger'],
        'collisions': {
            'Player':     ['Enemy', 'Ground', 'Item', 'Trigger'],
            'Enemy':      ['Player', 'Ground', 'Projectile'],
            'Projectile': ['Enemy', 'Ground'],
            'Ground':     ['Player', 'Enemy', 'Projectile', 'Item'],
            'Item':       ['Player', 'Ground'],
            'Trigger':    ['Player']
        }
    })
```

### Step 5: フォルダ構造の作成

```python
# フォルダ用ダミーファイルでフォルダを作成
# (Unity は空フォルダを保持しないため .gitkeep を使用)
folder_paths = [
    'Assets/Audio/BGM',
    'Assets/Audio/SFX',
    'Assets/Audio/Ambient',
    'Assets/Animations/Characters',
    'Assets/Animations/UI',
    'Assets/Materials/Characters',
    'Assets/Materials/Environment',
    'Assets/Materials/UI',
    'Assets/Prefabs/Characters',
    'Assets/Prefabs/Enemies',
    'Assets/Prefabs/Items',
    'Assets/Prefabs/UI',
    'Assets/Prefabs/FX',
    'Assets/Scripts/Boot',
    'Assets/Scripts/Characters',
    'Assets/Scripts/Enemies',
    'Assets/Scripts/Managers',
    'Assets/Scripts/UI',
    'Assets/Scripts/Utilities',
    'Assets/Sprites/Characters',
    'Assets/Sprites/Environment',
    'Assets/Sprites/UI',
    'Assets/Data/ScriptableObjects',
    'Assets/UI/UXML',
    'Assets/UI/USS'
]

# 各フォルダに .gitkeep を配置
for path in folder_paths:
    unity_asset_crud(operation='create',
        assetPath=f'{path}/.gitkeep',
        content='')
```

### Step 6: 入力プロファイルの設定

```python
# New Input System の InputActions アセットを作成
unity_input_profile(operation='createInputActions',
    inputActionsAssetPath='Assets/Settings/GameInputActions.inputactions')

# ※アクションマップの詳細設定（Move, Jump, Attack, Pause等）は
#   Unity Editor の InputActions インスペクタで行ってください。
```

### Step 7: 命名規則ドキュメントとユーティリティ

```python
# 共通ユーティリティクラス（定数管理）
unity_asset_crud(operation='create',
    assetPath='Assets/Scripts/Utilities/GameConstants.cs',
    content='''/// <summary>
/// ゲーム全体で使用する定数を定義します。
/// タグ名・レイヤー名・シーン名はここで一元管理してください。
/// </summary>
public static class GameConstants
{
    // Tags
    public const string TAG_PLAYER      = "Player";
    public const string TAG_ENEMY       = "Enemy";
    public const string TAG_ITEM        = "Item";
    public const string TAG_PROJECTILE  = "Projectile";
    public const string TAG_GROUND      = "Ground";
    public const string TAG_COLLECTIBLE = "Collectible";

    // Layers
    public const int LAYER_PLAYER      = 6;
    public const int LAYER_ENEMY       = 7;
    public const int LAYER_PROJECTILE  = 8;
    public const int LAYER_GROUND      = 9;
    public const int LAYER_ITEM        = 10;

    // Scenes
    public const string SCENE_BOOT      = "Boot";
    public const string SCENE_MANAGERS  = "Managers";
    public const string SCENE_MAIN_MENU = "MainMenu";
    public const string SCENE_GAME_HUD  = "GameHUD";
    public const string SCENE_LEVEL_01  = "Level01";

    // Audio
    public const string BGM_MAIN   = "bgm_main";
    public const string SFX_JUMP   = "sfx_jump";
    public const string SFX_ATTACK = "sfx_attack";
    public const string SFX_HIT    = "sfx_hit";
    public const string SFX_COIN   = "sfx_coin";
}''')

unity_compilation_await(operation='await', timeoutSeconds=30)
```

### Step 8: 初期シーン群の作成と Build Settings 登録

```python
# シーン作成
unity_scene_crud(operation='create', scenePath='Assets/Scenes/Boot.unity')
unity_scene_crud(operation='create', scenePath='Assets/Scenes/UI/MainMenu.unity')
unity_scene_crud(operation='create', scenePath='Assets/Scenes/UI/GameHUD.unity')
unity_scene_crud(operation='create', scenePath='Assets/Scenes/Levels/Level01.unity')

# Build Settings に登録（projectSettings_crud で個別登録）
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/Boot.unity')
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/UI/MainMenu.unity')
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/UI/GameHUD.unity')
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/Levels/Level01.unity')

# Boot を index 0 に配置（fromIndex → toIndex で移動）
# addSceneToBuild の登録順で Boot が先頭でない場合に使用
unity_projectSettings_crud(operation='reorderBuildScenes',
    fromIndex=3, toIndex=0)  # Boot のインデックスを 0 に移動

# Build Settings のバリデーション
unity_scene_relationship_graph(operation='validateBuildSettings')
```

### Step 9: Data フォルダの準備

ScriptableObject によるゲーム設定（GameConfig, EnemyData 等）の型定義とインスタンス作成は、アルファフェーズ（`game_workflow_guide(phase='alpha')`）で行います。ここでは Data フォルダが作成済みであることを確認します。

```python
# Data/ScriptableObjects フォルダが Step 5 で作成済みであることを確認
# アルファフェーズで GameConfig, EnemyData 等の SO を作成します
```

### Step 10: 最終確認

```python
# クラスカタログで全型を確認
unity_class_catalog(operation='listTypes',
    typeKind='MonoBehaviour',
    searchPath='Assets/Scripts')

# 整合性チェック
unity_validate_integrity(operation='all')

# コンパイルエラー確認
unity_console_log(operation='getCompilationErrors')

# シーン遷移グラフで確認
unity_scene_relationship_graph(operation='analyzeAll')
unity_scene_relationship_graph(operation='validateBuildSettings')
```

---

## チェックリスト

### Project Settings
- [ ] 会社名・製品名・バージョンを設定した
- [ ] Fixed Delta Time を設定した (0.02 = 50fps)

### タグ・レイヤー
- [ ] Player, Enemy, Item, Projectile, Ground タグを追加した
- [ ] Player(6), Enemy(7), Projectile(8), Ground(9), Item(10) レイヤーを追加した
- [ ] 物理レイヤーマトリクスを設定した

### フォルダ構造
- [ ] Audio/BGM, Audio/SFX を作成した
- [ ] Scripts/{Boot,Characters,Enemies,Managers,UI,Utilities} を作成した
- [ ] Prefabs/{Characters,Enemies,Items,UI,FX} を作成した
- [ ] Data/ScriptableObjects を作成した

### 入力設定
- [ ] input_profile(createInputActions) で InputActions アセットを作成した
- [ ] Unity Editor の InputActions インスペクタで Move, Jump, Attack, Pause を設定した
- [ ] ゲームパッドとキーボード両方のバインディングを設定した

### 定数・設定
- [ ] GameConstants.cs でタグ・レイヤー・シーン名を定数化した
- [ ] Data/ScriptableObjects フォルダが存在する（SO作成はアルファで実施）

### シーン
- [ ] Boot, MainMenu, GameHUD, Level01 シーンを作成した
- [ ] projectSettings_crud(addSceneToBuild) で全シーンを登録した
- [ ] projectSettings_crud(reorderBuildScenes) で正しい順序に設定した

### 確認
- [ ] validate_integrity(all) で整合性確認した
- [ ] scene_relationship_graph(validateBuildSettings) で確認した
- [ ] console_log(getCompilationErrors) でエラーなし確認した
- [ ] class_catalog(listTypes) で生成スクリプトを確認した

---

## 命名規則

| 対象 | 規則 | 例 |
|------|------|----|
| C# スクリプト | PascalCase | `PlayerController.cs` |
| ScriptableObject 型 | PascalCase + "Data"/"Config" | `GameConfig.cs`, `EnemyData.cs` |
| Prefab | PascalCase | `Player.prefab`, `Enemy_Slime.prefab` |
| Scene | PascalCase | `Level01.unity`, `MainMenu.unity` |
| Material | PascalCase + "Mat" | `PlayerMat.mat` |
| Sprite/Texture | snake_case | `player_idle_0.png` |
| AudioClip | snake_case | `sfx_jump.wav`, `bgm_main.mp3` |
| Tag | PascalCase | `Player`, `Enemy` |
| Layer | PascalCase | `Player`, `Ground` |
| GameObjects | PascalCase | `Player`, `EnemySpawner` |
| Private Fields (C#) | _camelCase | `_health`, `_moveSpeed` |
| Public Fields (C#) | camelCase | `moveSpeed`, `jumpForce` |

---

## 注意点・落とし穴

**scene_crud に addToBuildSettings パラメータはない**
シーン作成後に projectSettings_crud(operation='addSceneToBuild') を別途実行してください。

**input_profile に create 操作はない**
createInputActions（アクションアセット作成）または createPlayerInput（PlayerInput コンポーネント追加）を使用してください。

**タグとレイヤーは後から変更が難しい**
スクリプト内でハードコードされているとリファクタリングコストが高くなります。必ず GameConstants.cs で定数化してください。

**フォルダ作成は Unity API 経由で**
OS から直接作成した場合、.meta ファイルが生成されずバージョン管理の問題が生じることがあります。asset_crud で作成すると.metaが自動生成されます。

**New Input System のアクションマップはシーンをまたいで共有**
InputActions アセットは Assets/Scripts/ に配置し、複数シーンから参照できる状態にしてください。

**Build Settings の順序は変更に弱い**
SceneManager.LoadScene(int buildIndex) は使わず、必ずシーン名 (string) でロードし、GameConstants.cs で名前を管理してください。

**Physics2D と Physics3D は独立したレイヤーマトリクス**
2D プロジェクトと 3D プロジェクトでは別々にレイヤーマトリクスを設定する必要があります。

---

## 次のフェーズへ

プロジェクト初期設定が完了したら、次はシーン構築フェーズです:

1. **シーン構築** (`game_workflow_guide(phase='scene_building')`) - 空シーンに階層構造・Manager・Canvas 骨格を配置
   - Boot シーンに BootManager を配置
   - Manager シーンに永続 Manager オブジェクトを配置
   - UI シーンに Canvas 骨格を作成
   - Level シーンにカテゴリ別の親 GameObject を配置

マルチシーン設計の詳細は `game_workflow_guide(phase='scene_structure')` を参照してください。

---

## 関連ツール一覧

| ツール | プロジェクト初期設定での用途 |
|--------|---------------------------|
| `unity_projectSettings_crud` | read/write でタグ・レイヤー・品質設定、addSceneToBuild/reorderBuildScenes |
| `unity_asset_crud` | create でフォルダ作成・スクリプト生成 |
| `unity_scriptableObject_crud` | create で GameConfig アセット作成 |
| `unity_input_profile` | createInputActions / createPlayerInput |
| `unity_scene_crud` | create で初期シーン作成 |
| `unity_scene_relationship_graph` | analyzeAll / validateBuildSettings |
| `unity_class_catalog` | listTypes / inspectType で生成スクリプト確認 |
| `unity_validate_integrity` | all で初期設定の整合性確認 |
| `unity_console_log` | getCompilationErrors でコンパイルエラー確認 |
| `unity_compilation_await` | await でスクリプト生成後のコンパイル待機 |
