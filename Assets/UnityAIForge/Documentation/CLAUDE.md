# Unity-AI-Forge CLAUDE.md

AIアシスタントがUnity-AI-ForgeのMCPツールを使ってUnity Editorを操作するためのガイドです。

## プロジェクト概要

Unity-AI-Forgeは、Model Context Protocol (MCP)を通じてAIアシスタントがUnity Editorをリアルタイムで操作するための開発ツールキットです。64のMCPツールを提供し、3層アーキテクチャ（High-Level GameKit / Mid-Level Batch / Low-Level CRUD）で構成されています。

**バージョン:** 2.8.0
**要件:** Unity 2022.3 LTS以降、Python 3.10+、.NET Standard 2.1

### 通信フロー

```
AIクライアント (Claude Code / Cursor) <--MCP--> Python Server <--WebSocket--> Unity Editor Bridge
```

## ツール優先順位

ツールは上位レベルから順に使用してください。上位ツールほど少ないコマンドで多くの処理を実行できます。

```
High-Level GameKit (29ツール)  ← 最優先: ゲームシステム一式を1コマンドで構築
High-Level Analysis  (3ツール)  ← 参照解析・依存関係の可視化
Mid-Level Batch     (18ツール)  ← 複数設定をまとめて適用
Low-Level CRUD       (8ツール)  ← 個別のUnity操作
Utility              (5ツール)  ← 接続確認・コンパイル待機など
Batch Operations     (1ツール)  ← 複数コマンドの順次実行
```

## コード生成アーキテクチャ

GameKitツールはMonoBehaviourを直接追加するのではなく、テンプレートからスタンドアロンのC#スクリプトを生成します。生成されたスクリプトはUnity-AI-Forgeパッケージに依存しないため、パッケージをアンインストールしてもプロジェクトはそのまま動作します。

- **テンプレート:** `Assets/UnityAIForge/Editor/CodeGen/Templates/*.cs.txt`
- **生成先:** ユーザーの`Assets/`フォルダ内
- **エントリポイント:** `CodeGenHelper.GenerateAndAttach()`
- **create操作後:** コンパイル待機が必要（`unity_compilation_await`）

## ツール一覧

### High-Level GameKit（29ツール）

GameKitは3本柱（UI・Logic・Presentation）で構成されています。

**Logicピラー:**

| ツール名 | 用途 |
|----------|------|
| `unity_gamekit_actor` | キャラクター制御（8種の移動プロファイル） |
| `unity_gamekit_manager` | ゲーム管理（スコア、ステート、ゲームオーバー） |
| `unity_gamekit_health` | HP・ダメージシステム |
| `unity_gamekit_combat` | 攻撃・ダメージ計算 |
| `unity_gamekit_spawner` | オブジェクト生成（ウェーブ、ランダム） |
| `unity_gamekit_ai` | AIビヘイビア（パトロール、追跡、攻撃） |
| `unity_gamekit_trigger_zone` | トリガーゾーン（ダメージ、回復、テレポート） |
| `unity_gamekit_timer` | タイマー（カウントダウン、繰り返し） |
| `unity_gamekit_machinations` | 経済システム（リソースプール、フロー） |
| `unity_gamekit_sceneflow` | シーン遷移管理 |
| `unity_gamekit_save` | セーブ/ロードシステム |
| `unity_gamekit_status_effect` | ステータスエフェクト（バフ/デバフ） |
| `unity_gamekit_interaction` | インタラクション（ドア、スイッチ） |
| `unity_gamekit_collectible` | 収集アイテム |
| `unity_gamekit_projectile` | 弾丸・投射物 |
| `unity_gamekit_waypoint` | ウェイポイント移動 |
| `unity_gamekit_inventory` | インベントリシステム |
| `unity_gamekit_dialogue` | ダイアログシステム |
| `unity_gamekit_quest` | クエストシステム |

**UIピラー:**

| ツール名 | 用途 |
|----------|------|
| `unity_gamekit_ui_binding` | UIデータバインディング |
| `unity_gamekit_ui_list` | 動的リスト/グリッド |
| `unity_gamekit_ui_slot` | アイテムスロット（インベントリ/装備） |
| `unity_gamekit_ui_selection` | 選択グループ（ラジオ、トグル、タブ） |
| `unity_gamekit_ui_command` | UIボタンとコマンドのバインディング |

**Presentationピラー:**

| ツール名 | 用途 |
|----------|------|
| `unity_gamekit_effect` | エフェクト管理 |
| `unity_gamekit_animation_sync` | アニメーション同期 |
| `unity_gamekit_vfx` | VFXラッパー（プーリング対応） |
| `unity_gamekit_audio` | オーディオラッパー（フェード対応） |
| `unity_gamekit_feedback` | ゲームフィール（ヒットストップ、画面揺れ） |

### High-Level Analysis（3ツール）

| ツール名 | 用途 |
|----------|------|
| `unity_scene_reference_graph` | シーン内のオブジェクト間参照を解析 |
| `unity_class_dependency_graph` | C#スクリプトのクラス依存関係を解析 |
| `unity_scene_relationship_graph` | シーン全体の関係性を包括的に解析 |

### Mid-Level Batch（18ツール）

| ツール名 | 用途 |
|----------|------|
| `unity_transform_batch` | Transform一括操作 |
| `unity_rectTransform_batch` | RectTransform一括操作 |
| `unity_physics_bundle` | 物理設定（2D/3D） |
| `unity_camera_rig` | カメラプリセット |
| `unity_ui_foundation` | UI要素作成 |
| `unity_ui_hierarchy` | UIメニュー/階層構築 |
| `unity_ui_state` | UI表示状態管理 |
| `unity_ui_navigation` | UIナビゲーション設定 |
| `unity_audio_source_bundle` | オーディオソース設定 |
| `unity_input_profile` | 入力プロファイル |
| `unity_character_controller_bundle` | キャラクターコントローラープリセット |
| `unity_tilemap_bundle` | タイルマップ設定 |
| `unity_sprite2d_bundle` | 2Dスプライト設定 |
| `unity_animation2d_bundle` | 2Dアニメーション設定 |
| `unity_material_bundle` | マテリアル設定 |
| `unity_light_bundle` | ライト設定 |
| `unity_particle_bundle` | パーティクル設定 |
| `unity_animation3d_bundle` | 3Dアニメーション設定 |

### Low-Level CRUD（8ツール）

| ツール名 | 用途 |
|----------|------|
| `unity_scene_crud` | シーン管理（作成、保存、読込、inspect） |
| `unity_gameobject_crud` | GameObject操作（作成、削除、移動、inspect） |
| `unity_component_crud` | コンポーネント操作（追加、更新、削除、inspect） |
| `unity_asset_crud` | アセット管理（作成、更新、削除、inspect） |
| `unity_scriptableObject_crud` | ScriptableObject操作 |
| `unity_prefab_crud` | プレハブ操作 |
| `unity_vector_sprite_convert` | ベクター画像からスプライト生成 |
| `unity_projectSettings_crud` | プロジェクト設定管理 |

### Utility（5ツール）

| ツール名 | 用途 |
|----------|------|
| `unity_ping` | ブリッジ接続確認 |
| `unity_compilation_await` | コンパイル完了待機 |
| `unity_playmode_control` | プレイモード制御 |
| `unity_console_log` | コンソールログ取得 |
| `unity_event_wiring` | UnityEventの接続 |

### Batch Operations（1ツール）

| ツール名 | 用途 |
|----------|------|
| `unity_batch_sequential_execute` | 複数コマンドの順次実行（レジューム対応） |

## 基本ワークフロー

### 1. 操作前にinspectする

変更を加える前に、必ず現在の状態を確認してください。

```python
# シーン全体の階層を確認
unity_scene_crud(operation='inspect', includeHierarchy=True)

# 特定のGameObjectを確認
unity_gameobject_crud(operation='inspect', gameObjectPath='Player')

# コンポーネントの特定プロパティを確認
unity_component_crud(
    operation='inspect',
    gameObjectPath='Player',
    componentType='UnityEngine.Transform',
    propertyFilter=['position', 'rotation']
)
```

### 2. GameKitでゲームシステムを構築する

```python
# プレイヤーキャラクターを作成
unity_gamekit_actor(
    operation='create',
    actorId='player_character',
    moveType='Platformer2D',
    moveSpeed=8.0,
    jumpForce=12.0
)

# コンパイル待機（create操作後は必須）
unity_compilation_await()

# HPシステムを追加
unity_gamekit_health(
    operation='create',
    healthId='player_hp',
    maxHp=100,
    currentHp=100
)
unity_compilation_await()

# 敵スポナーを作成
unity_gamekit_spawner(
    operation='create',
    spawnerId='enemy_spawner',
    spawnMode='Wave',
    waveInterval=5.0,
    enemiesPerWave=3
)
unity_compilation_await()
```

### 3. UIを構築する

UI Toolkitベースの画面構築にはMid-Levelツールを使用します。

```python
# UIドキュメントを作成
unity_ui_foundation(
    operation='create',
    elementType='Document',
    gameObjectPath='HUD'
)

# メニュー階層を構築
unity_ui_hierarchy(
    operation='build',
    rootPath='MainMenu',
    structure={
        'StartButton': {'type': 'Button', 'text': 'Start Game'},
        'OptionsButton': {'type': 'Button', 'text': 'Options'},
        'QuitButton': {'type': 'Button', 'text': 'Quit'}
    }
)
```

### 4. 変更後に参照を検証する

オブジェクトの削除・リネーム・移動後は、壊れた参照がないか確認してください。

```python
# シーン全体の参照とイベントを検証
unity_scene_relationship_graph(
    operation='analyze',
    includeReferences=True,
    includeEvents=True
)

# 削除前に被参照を確認
unity_scene_reference_graph(
    operation='analyze',
    rootPath='TargetObject',
    direction='incoming'
)
```

### 5. 複数コマンドを順次実行する

```python
unity_batch_sequential_execute(
    commands=[
        {'tool': 'unity_gameobject_crud', 'args': {'operation': 'create', 'name': 'Enemy1'}},
        {'tool': 'unity_component_crud', 'args': {
            'operation': 'add',
            'gameObjectPath': 'Enemy1',
            'componentType': 'UnityEngine.Rigidbody'
        }},
        {'tool': 'unity_physics_bundle', 'args': {
            'operation': 'apply',
            'gameObjectPath': 'Enemy1',
            'preset': 'kinematic'
        }}
    ]
)
```

## ベストプラクティス

### 必須ルール

1. **.metaファイルを絶対に編集しない** -- Unityが自動管理するファイルです。手動編集は参照破壊の原因になります。
2. **全てのUnity操作にMCPツールを使う** -- ファイル直接操作は避けてください。
3. **操作前にinspectする** -- 現在の状態を確認してから変更を加えてください。
4. **create操作後はコンパイル待機する** -- GameKitのcreate操作はスクリプト生成を伴うため、`unity_compilation_await`で待機が必要です。

### 設計指針

5. **ツール優先順位を守る** -- High-Level → Mid-Level → Low-Level の順で使用してください。
6. **UI優先設計** -- ゲームロジックの前にUIを構築してください。UIがあることでテスト・デバッグが容易になります。
7. **シーン分離** -- 機能ごとにシーンを分割してください（メインメニュー、ゲームプレイ、設定画面など）。
8. **変更後に検証する** -- 削除・リネーム・参照変更の後は`unity_scene_relationship_graph`で確認してください。

### 型マッピング

コンポーネントプロパティ設定時の型変換ルール:

| Python型 | Unity型 |
|----------|---------|
| `int`, `float`, `bool`, `str` | プリミティブ型 |
| `{"x": 1, "y": 2, "z": 3}` | Vector3 / Vector2 |
| `{"r": 1, "g": 0, "b": 0, "a": 1}` | Color |
| 文字列 | Enum（Enum.Parseで変換） |
| `{"_ref": "asset", "guid": "..."}` | Unity Object参照（GUID） |
| `{"_ref": "asset", "path": "Assets/..."}` | Unity Object参照（パス） |
| `"Assets/Models/Mesh.fbx"` | Unity Object参照（直接パス文字列） |

### GameObjectの参照方法

`gameObjectPath`パラメータでは以下の形式が使用可能です:

- パス: `"Player/Weapon"`
- 名前: `"Enemy"`
- ワイルドカード: `"Enemy*"` （バッチ操作時）
- 型指定: `"type:Enemy"` または `"name:Enemy"`

## トラブルシューティング

### 接続確認

```python
# ブリッジ接続をテスト
unity_ping()
```

レスポンスがない場合:
- Unity Editorが起動しているか確認
- Tools > MCP Assistantウィンドウでブリッジが起動しているか確認
- Python MCPサーバーが稼働しているか確認

### コンパイルエラー

```python
# コンソールログでエラーを確認
unity_console_log(logType='error', count=10)
```

GameKitのcreate操作後にコンパイルエラーが出た場合:
- `unity_compilation_await`でコンパイル完了を待ってから次の操作を行ってください
- 生成されたスクリプトの名前空間衝突がないか確認してください

### プレイモード制御

```python
# プレイモード開始
unity_playmode_control(action='play')

# プレイモード停止
unity_playmode_control(action='stop')

# 一時停止
unity_playmode_control(action='pause')
```

### 壊れた参照の検出

```python
# シーン全体の参照を解析
unity_scene_relationship_graph(
    operation='analyze',
    includeReferences=True,
    includeEvents=True
)
```

## プロジェクト構造

```
Assets/UnityAIForge/
  Editor/
    MCPBridge/          # C# WebSocketブリッジとハンドラー
      Base/             # ブリッジコア（McpBridgeService, McpCommandProcessor）
      Handlers/         # ツールハンドラー（LowLevel, MidLevel, HighLevel, GameKit, Utility）
      Utilities/        # ユーティリティ（GraphAnalysis, HandlerUtilities）
    CodeGen/            # コード生成インフラ
      Templates/        # GameKitテンプレート（*.cs.txt）
  MCPServer/
    src/                # Python MCPサーバー
      tools/            # ツール定義・スキーマ
      bridge/           # WebSocketクライアント
      server/           # MCPサーバー実装
  Tests/
    Editor/             # ユニットテスト
  Documentation/        # ドキュメント
```
