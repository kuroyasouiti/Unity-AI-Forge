# UnityMCP - Unity向けModel Context Protocolサーバー

UnityMCPは、AIアシスタントがUnity Editorとリアルタイムで対話できる包括的なModel Context Protocol (MCP) サーバーです。シーン管理、GameObject操作、コンポーネント編集、アセット操作、2D Tilemapデザイン、NavMeshナビゲーション、UIレイアウト、Prefab、入力システムなど、広範なツールを提供します。

## アーキテクチャ

UnityMCPは**双方向WebSocketブリッジ**アーキテクチャを使用します：

```
AIクライアント (Claude Code/Cursor) <--(MCP)--> Pythonサーバー <--(WebSocket)--> Unity Editorブリッジ
```

### コンポーネント

1. **Unity C#ブリッジ** (`Assets/Editor/MCPBridge/`) - Unity Editor内で動作するWebSocketサーバー
2. **Python MCPサーバー** (`Assets/Runtime/MCPServer/`) - ブリッジに接続するMCPプロトコル実装

## クイックスタート

### 1. Unity Editorのセットアップ

1. Unityプロジェクトを開く
2. UnityMCPパッケージをインポート
3. **Tools > MCP Assistant**を開く
4. **Start Bridge**をクリック
5. ブリッジがデフォルトで`ws://localhost:7077/bridge`でリッスンします

### 2. Pythonサーバーのセットアップ

```bash
# MCPサーバーディレクトリに移動
cd Assets/Runtime/MCPServer

# uv で実行（推奨）
uv run main.py

# または Python で直接実行
python main.py --transport stdio
```

### 3. MCPクライアントの設定

MCPクライアント設定に追加（例：Claude Desktop）：

```json
{
  "mcpServers": {
    "unity": {
      "command": "uv",
      "args": ["run", "--directory", "D:/Projects/MCP/Assets/Runtime/MCPServer", "main.py"],
      "env": {
        "MCP_SERVER_TRANSPORT": "stdio",
        "MCP_LOG_LEVEL": "info"
      }
    }
  }
}
```

## 利用可能なツール

### コア操作

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity.ping` | ブリッジ接続の確認 | Unityバージョン、プロジェクト名、タイムスタンプを返す |
| `unity.scene.crud` | シーン管理 | create, load, save, delete, duplicate シーン |
| `unity.gameobject.crud` | GameObjectヒエラルキー管理 | create, delete, move, rename, duplicate, inspect GameObject |
| `unity.component.crud` | コンポーネント操作 | add, remove, update, inspect GameObjectのコンポーネント |
| `unity.asset.crud` | アセットファイル操作 | create, update, rename, duplicate, delete, inspect Assets/ファイル |

**シーン管理 (`unity.scene.crud`)**
- デフォルトGameObjectsを持つ新しいシーンを作成
- シーンを加算的またはシングルモードで読み込み
- アクティブまたは全ての開いているシーンを保存
- シーンの削除と複製
- 完全なAssetDatabase統合

**GameObject管理 (`unity.gameobject.crud`)**
- 親階層を持つGameObjectを作成
- ヒエラルキー内でGameObjectを移動
- 子を含めてリネームと複製
- 全ての添付コンポーネントとプロパティを確認するためのInspect
- Undoサポート付き削除

**コンポーネント管理 (`unity.component.crud`)**
- 完全修飾型名でコンポーネントを追加
- 辞書ベースの変更でコンポーネントプロパティを更新
- Unity Object参照のサポート（メッシュ、マテリアル、スプライト）
- ビルトインリソースの読み込み（`Library/unity default resources::`）
- コンポーネント状態とプロパティの検査

**アセット管理 (`unity.asset.crud`)**
- テキストベースアセットの作成（C#スクリプト、JSON、設定）
- 既存アセットコンテンツの更新
- アセットのリネーム、複製、削除
- アセットメタデータとコンテンツの検査
- AssetDatabaseの自動更新

---

### 2D Tilemapシステム

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity.tilemap.manage` | 2D Tilemap操作 | createTilemap, setTile, getTile, clearTile, fillArea, inspectTilemap, clearAll |

**Tilemap管理 (`unity.tilemap.manage`)**
- **createTilemap**: Gridを親として自動的に新しいTilemapを作成
- **setTile**: 特定のグリッド座標（X, Y, Z）にタイルを配置
- **getTile**: 位置のタイル情報を取得
- **clearTile**: 位置からタイルを削除
- **fillArea**: タイルで矩形エリアを効率的に塗りつぶし
- **inspectTilemap**: 境界、タイル数、統計情報を取得
- **clearAll**: Tilemapから全てのタイルを削除

**特徴:**
- 自動Grid親作成
- 3Dタイル配置のサポート（Z軸）
- Undoサポート付き効率的なエリア塗りつぶし
- Unityの2D Tilemapシステムとの統合
- レベルデザインと手続き型生成に最適

**例 - 2Dレベルの作成:**
```json
{
  "tool": "tilemapManage",
  "payload": {
    "operation": "fillArea",
    "gameObjectPath": "Grid/Level1",
    "tileAssetPath": "Assets/Tiles/Ground.asset",
    "startX": 0,
    "startY": 0,
    "endX": 20,
    "endY": 15
  }
}
```

---

### ナビゲーションシステム (NavMesh)

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity.navmesh.manage` | NavMeshとAIナビゲーション | bakeNavMesh, clearNavMesh, addNavMeshAgent, setDestination, inspectNavMesh, createNavMeshSurface |

**NavMesh管理 (`unity.navmesh.manage`)**
- **bakeNavMesh**: 現在のシーンのナビゲーションメッシュをベイク
- **clearNavMesh**: 全てのベイク済みNavMeshデータをクリア
- **addNavMeshAgent**: 設定付きNavMeshAgentコンポーネントを追加
  - スピード、加速度、停止距離を設定
  - エージェントプロパティ（半径、高さ）を設定
- **setDestination**: NavMeshAgentのターゲット目的地を設定
  - パス状態を返す（hasPath, pathPending）
- **inspectNavMesh**: NavMesh統計情報を取得
  - 三角分割データ（頂点、三角形、エリア）
  - エージェント設定（半径、高さ、傾斜、登攀）
- **updateSettings**: 現在のNavMeshベイク設定を表示（読み取り専用）
- **createNavMeshSurface**: NavMeshSurfaceコンポーネントを追加（NavMesh Componentsパッケージが必要）

**特徴:**
- リアルタイムNavMeshベイク
- 完全なNavMeshAgent設定
- ランタイムパスファインディング制御
- NavMesh統計とデバッグ
- NavMesh Componentsパッケージのサポート

**例 - AIエージェントのセットアップ:**
```json
{
  "operations": [
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "addNavMeshAgent",
        "gameObjectPath": "Enemy",
        "agentSpeed": 3.5,
        "agentStoppingDistance": 1.0
      }
    },
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "setDestination",
        "gameObjectPath": "Enemy",
        "destinationX": 10.0,
        "destinationY": 0.0,
        "destinationZ": 5.0
      }
    }
  ]
}
```

---

### UIシステム (UGUI)

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity.ugui.manage` | **統合UGUI管理** | rectAdjust, setAnchor, setAnchorPreset, convertToAnchored, convertToAbsolute, inspect, updateRect |
| `unity.ugui.rectAdjust` | RectTransformサイズ調整 | レイアウトベースのサイズ調整 |
| `unity.ugui.anchorManage` | RectTransformアンカー管理 | カスタムアンカー、プリセット、位置変換 |

**統合UGUIツール (`unity.ugui.manage`)** - 推奨
- **rectAdjust**: ワールドコーナーに基づいてRectTransformを調整
- **setAnchor**: カスタムアンカー値を設定（min/max X/Y）
- **setAnchorPreset**: 一般的なプリセットを適用（top-left, center, stretchなど）
- **convertToAnchored**: 絶対位置からアンカー位置に変換
- **convertToAbsolute**: アンカー位置から絶対位置に変換
- **inspect**: RectTransform状態を取得
- **updateRect**: RectTransformプロパティを直接更新

**アンカープリセット:**
- 位置: top-left, top-center, top-right, middle-left, center, middle-right, bottom-left, bottom-center, bottom-right
- ストレッチ: stretch-horizontal, stretch-vertical, stretch-all, stretch-top, stretch-middle, stretch-bottom

**特徴:**
- アンカー変更時の視覚的位置の保持
- CanvasScaler参照解像度のサポート
- 直接プロパティ更新（anchoredPosition, sizeDelta, pivot, offsets）
- 完全なRectTransform制御

---

### タグとレイヤー

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity.tagLayer.manage` | タグとレイヤー管理 | GameObject: setTag, getTag, setLayer, getLayer, setLayerRecursive<br>プロジェクト: listTags, addTag, removeTag, listLayers, addLayer, removeLayer |

**タグとレイヤー管理 (`unity.tagLayer.manage`)**
- **GameObject操作:**
  - setTag/getTag: 個別GameObjectのタグ管理
  - setLayer/getLayer: 名前またはインデックスでレイヤーを設定
  - setLayerRecursive: GameObjectと全ての子にレイヤーを設定
- **プロジェクト操作:**
  - listTags/listLayers: 利用可能な全てのタグ/レイヤーを表示
  - addTag/addLayer: プロジェクトに新しいタグ/レイヤーを作成
  - removeTag/removeLayer: プロジェクトからタグ/レイヤーを削除

**特徴:**
- レイヤー名またはインデックスのサポート
- 階層の再帰的レイヤー設定
- 完全なタグ/レイヤープロジェクト管理
- Physicsとレンダリングシステムとの統合

---

### Prefab

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity.prefab.crud` | Prefabワークフロー | create, update, inspect, instantiate, unpack, applyOverrides, revertOverrides |

**Prefab管理 (`unity.prefab.crud`)**
- **create**: シーンGameObjectから新しいPrefabを作成
  - 子を含めるオプション
- **update**: 変更されたインスタンスから既存Prefabを更新
- **inspect**: Prefabアセット情報を取得
- **instantiate**: シーンにPrefabインスタンスを作成
  - Prefab接続を維持
  - オプションの親指定
- **unpack**: Prefabインスタンスを通常のGameObjectsに展開
  - OutermostRootまたはCompletelyモード
- **applyOverrides**: インスタンスの変更をPrefabアセットに適用
- **revertOverrides**: インスタンスをPrefab状態に戻す

**特徴:**
- 完全なPrefabワークフローサポート
- ネストされたPrefabの処理
- オーバーライド管理
- インスタンス追跡

---

### プロジェクト設定

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity.projectSettings.crud` | プロジェクト設定管理 | read, write, list 設定 |
| `unity.renderPipeline.manage` | レンダーパイプライン管理 | inspect, setAsset, getSettings |

**プロジェクト設定 (`unity.projectSettings.crud`)**
- **カテゴリ:**
  - player: PlayerSettings（会社名、製品名、バージョン、画面設定）
  - quality: QualitySettings（品質レベル、影、アンチエイリアシング）
  - time: Time設定（fixedDeltaTime, timeScale）
  - physics: Physics設定（重力、衝突、反復）
  - audio: AudioSettings（DSPバッファ、サンプルレート、ボイス）
  - editor: EditorSettings（シリアライゼーションモード、行末）

**レンダーパイプライン (`unity.renderPipeline.manage`)**
- **inspect**: 現在のパイプラインを確認（Built-in/URP/HDRP/Custom）
- **setAsset**: レンダーパイプラインアセットを変更
- **getSettings**: パイプライン固有の設定を読み取り
- リフレクションによるパイプライン固有プロパティアクセス

---

### 入力システム

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity.inputSystem.manage` | New Input System管理 | listActions, createAsset, addActionMap, addAction, addBinding, inspectAsset, deleteAsset, deleteActionMap, deleteAction, deleteBinding |

**入力システム (`unity.inputSystem.manage`)** - Input Systemパッケージが必要
- **アセット管理:**
  - listActions: 全ての.inputactionsファイルを検索
  - createAsset: 新しいInput Actionアセットを作成
  - inspectAsset: アクションマップとアクションを表示
  - deleteAsset: Input Actionアセットを削除
- **アクションマップ:**
  - addActionMap: アセットにアクションマップを追加
  - deleteActionMap: アクションマップを削除
- **アクション:**
  - addAction: マップにアクションを追加（Button, Value, PassThrough）
  - deleteAction: アクションを削除
- **バインディング:**
  - addBinding: アクションに入力バインディングを追加
  - deleteBinding: 特定または全てのバインディングを削除

**一般的なバインディングパス:**
- キーボード: `<Keyboard>/space`, `<Keyboard>/w`, `<Keyboard>/escape`
- マウス: `<Mouse>/leftButton`, `<Mouse>/position`, `<Mouse>/delta`
- ゲームパッド: `<Gamepad>/buttonSouth`, `<Gamepad>/leftStick`

---

### ユーティリティ

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity.script.outline` | C#スクリプト解析 | オプションのメンバーシグネチャ付きサマリーを生成 |
| `unity.batch.execute` | **バッチ操作** | 1リクエストで複数ツールを実行 |

**スクリプトアウトライン (`unity.script.outline`)**
- C#スクリプトサマリーを生成
- オプションのメンバーシグネチャ含有
- GUIDまたはアセットパス検索のサポート

**バッチ実行 (`unity.batch.execute`)** - 高性能
- 複数の操作を順次実行
- 任意のツールを組み合わせ（tilemap, navmesh, gameObjectなど）
- stopOnErrorフラグによるエラー処理
- 個別操作結果の追跡
- 複雑なシーンセットアップに最適

**バッチ実行機能:**
- **順次処理**: 操作は順番に実行
- **エラー制御**: 最初のエラーで継続または停止
- **結果追跡**: 各操作の成功/失敗を取得
- **ツール混在**: 1つのバッチで任意のUnityツールを組み合わせ
- **性能**: ネットワークオーバーヘッドの削減

**バッチでサポート:**
全てのツールがバッチ操作で使用可能: sceneManage, gameObjectManage, componentManage, assetManage, tilemapManage, navmeshManage, uguiManage, tagLayerManage, prefabManage, projectSettingsManage, renderPipelineManage, inputSystemManage

---

## 利用可能なリソース

| リソース | 説明 |
|---------|------|
| `unity://project/structure` | プロジェクトディレクトリ構造とアセットリスト |
| `unity://editor/log` | Unity Editorログ（最近のエントリ） |
| `unity://scene/active` | アクティブシーンヒエラルキーとGameObject情報 |
| `unity://scene/list` | プロジェクト内の全シーンリスト |
| `unity://asset/{guid}` | GUIDによるアセット詳細 |

---

## 設定

### 環境変数

| 変数 | 説明 | デフォルト |
|-----|------|----------|
| `MCP_SERVER_TRANSPORT` | トランスポートモード: `stdio` または `websocket` | `stdio` |
| `MCP_SERVER_HOST` | WebSocketサーバーホスト（websocketモード） | `127.0.0.1` |
| `MCP_SERVER_PORT` | WebSocketサーバーポート（websocketモード） | `7070` |
| `MCP_BRIDGE_TOKEN` | オプションの認証トークン | - |
| `MCP_LOG_LEVEL` | ログレベル: `trace`, `debug`, `info`, `warn`, `error` | `info` |

### Unity Bridgeの設定

**Tools > MCP Assistant**ウィンドウから設定:

- **Bridge Port**: WebSocketリスナーのポート（デフォルト: 7077）
- **Context Update Interval**: シーン更新のプッシュ頻度（デフォルト: 5秒）
- **Heartbeat Interval**: 接続ヘルスチェック間隔（デフォルト: 10秒）

---

## 使用例

### 例1: ナビゲーション付き完全な2Dレベル構築

```json
{
  "operations": [
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "createTilemap",
        "tilemapName": "Arena"
      }
    },
    {
      "tool": "tilemapManage",
      "payload": {
        "operation": "fillArea",
        "gameObjectPath": "Grid/Arena",
        "tileAssetPath": "Assets/Tiles/Floor.asset",
        "startX": 0,
        "startY": 0,
        "endX": 20,
        "endY": 20
      }
    },
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "bakeNavMesh"
      }
    },
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "Player",
        "name": "Player"
      }
    },
    {
      "tool": "navmeshManage",
      "payload": {
        "operation": "addNavMeshAgent",
        "gameObjectPath": "Player",
        "agentSpeed": 5.0
      }
    }
  ],
  "stopOnError": true
}
```

### 例2: UIヒエラルキーの作成

```json
{
  "operations": [
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "Canvas/Panel",
        "name": "Panel"
      }
    },
    {
      "tool": "componentManage",
      "payload": {
        "operation": "add",
        "gameObjectPath": "Canvas/Panel",
        "componentType": "UnityEngine.UI.Image"
      }
    },
    {
      "tool": "uguiManage",
      "payload": {
        "operation": "setAnchorPreset",
        "gameObjectPath": "Canvas/Panel",
        "preset": "center",
        "preservePosition": true
      }
    },
    {
      "tool": "uguiManage",
      "payload": {
        "operation": "updateRect",
        "gameObjectPath": "Canvas/Panel",
        "sizeDeltaX": 200,
        "sizeDeltaY": 100
      }
    }
  ]
}
```

### 例3: 入力システムのセットアップ

```json
{
  "operations": [
    {
      "tool": "inputSystemManage",
      "payload": {
        "operation": "createAsset",
        "assetPath": "Assets/Input/PlayerControls.inputactions"
      }
    },
    {
      "tool": "inputSystemManage",
      "payload": {
        "operation": "addActionMap",
        "assetPath": "Assets/Input/PlayerControls.inputactions",
        "mapName": "Player"
      }
    },
    {
      "tool": "inputSystemManage",
      "payload": {
        "operation": "addAction",
        "assetPath": "Assets/Input/PlayerControls.inputactions",
        "mapName": "Player",
        "actionName": "Move",
        "actionType": "Value"
      }
    },
    {
      "tool": "inputSystemManage",
      "payload": {
        "operation": "addBinding",
        "assetPath": "Assets/Input/PlayerControls.inputactions",
        "mapName": "Player",
        "actionName": "Move",
        "path": "<Keyboard>/wasd"
      }
    }
  ]
}
```

### 例4: タグとレイヤーの管理

```json
{
  "operations": [
    {
      "tool": "tagLayerManage",
      "payload": {
        "operation": "addTag",
        "tag": "Enemy"
      }
    },
    {
      "tool": "tagLayerManage",
      "payload": {
        "operation": "addLayer",
        "layer": "Characters"
      }
    },
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "EnemyGroup",
        "name": "EnemyGroup"
      }
    },
    {
      "tool": "tagLayerManage",
      "payload": {
        "operation": "setLayerRecursive",
        "gameObjectPath": "EnemyGroup",
        "layer": "Characters"
      }
    },
    {
      "tool": "tagLayerManage",
      "payload": {
        "operation": "setTag",
        "gameObjectPath": "EnemyGroup",
        "tag": "Enemy"
      }
    }
  ]
}
```

### 例5: Prefabワークフロー

```json
{
  "operations": [
    {
      "tool": "gameObjectManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "PlayerCharacter",
        "name": "PlayerCharacter"
      }
    },
    {
      "tool": "componentManage",
      "payload": {
        "operation": "add",
        "gameObjectPath": "PlayerCharacter",
        "componentType": "UnityEngine.CharacterController"
      }
    },
    {
      "tool": "prefabManage",
      "payload": {
        "operation": "create",
        "gameObjectPath": "PlayerCharacter",
        "prefabPath": "Assets/Prefabs/Player.prefab",
        "includeChildren": true
      }
    },
    {
      "tool": "prefabManage",
      "payload": {
        "operation": "instantiate",
        "prefabPath": "Assets/Prefabs/Player.prefab",
        "parentPath": "GameWorld"
      }
    }
  ]
}
```

---

## 開発

### ファイル構造

```
Assets/
├── Editor/
│   └── MCPBridge/                    # Unity C#ブリッジ
│       ├── McpBridgeService.cs            # WebSocketサーバー
│       ├── McpCommandProcessor.cs         # ツール実行（4700+行）
│       ├── McpContextCollector.cs         # コンテキスト収集
│       ├── McpBridgeWindow.cs             # Unity Editor UI
│       └── McpBridgeSettings.cs           # 設定
└── Runtime/
    └── MCPServer/                    # Python MCPサーバー
        ├── main.py                        # サーバーエントリポイント
        ├── bridge/                        # WebSocketクライアント
        │   ├── bridge_manager.py          # 接続管理
        │   └── messages.py                # メッセージプロトコル
        ├── tools/                         # ツール定義
        │   └── register_tools.py          # 全ツールスキーマ（800+行）
        └── resources/                     # リソースプロバイダー
            └── register_resources.py
```

### 新しいツールの追加

詳細なガイドは以下を参照:
- [CLAUDE.md](../CLAUDE.md) - 完全な開発ドキュメント
- [TILEMAP_NAVMESH_TOOLS.md](../TILEMAP_NAVMESH_TOOLS.md) - TileMap & NavMeshツールリファレンス
- [BATCH_PROCESSING_EXAMPLES.md](../BATCH_PROCESSING_EXAMPLES.md) - バッチ操作の例

---

## 機能

### コア機能
- ✅ WebSocket経由のリアルタイムUnity Editor統合
- ✅ 包括的なシーンとGameObject管理
- ✅ プロパティ更新によるコンポーネント操作
- ✅ アセットの作成と変更
- ✅ コンパイル後の自動再接続

### 2Dとナビゲーション
- ✅ **2D Tilemapシステム** - グリッドベースのタイル配置とエリア塗りつぶし
- ✅ **NavMeshシステム** - AIパスファインディングとナビゲーション
- ✅ リアルタイムNavMeshベイクとエージェント制御

### UIとレイアウト
- ✅ UGUIレイアウトと配置ツール
- ✅ RectTransformアンカー管理
- ✅ 位置変換（anchored ↔ absolute）
- ✅ アンカープリセット（stretch, center, corners）

### 高度なシステム
- ✅ タグとレイヤー管理（GameObject & プロジェクト）
- ✅ Prefabワークフロー（作成、更新、インスタンス化、オーバーライド管理）
- ✅ プロジェクト設定構成（6カテゴリ）
- ✅ レンダーパイプライン管理（Built-in/URP/HDRP）
- ✅ Input System統合（New Input System）

### 開発者ツール
- ✅ **バッチ操作実行** - 複数の操作を組み合わせ
- ✅ C#スクリプトアウトラインと解析
- ✅ シーン状態によるコンテキスト対応アシスタンス
- ✅ 構造化されたエラー処理とレポート

---

## ツールリファレンスサマリー

| カテゴリ | ツール数 | 操作 |
|---------|---------|------|
| **コア** | 5ツール | ping, シーン, GameObject, コンポーネント, アセット |
| **2D** | 1ツール | Tilemap（7操作） |
| **ナビゲーション** | 1ツール | NavMesh（7操作） |
| **UI** | 3ツール | UGUI統合 + 専用ツール |
| **システム** | 5ツール | タグ/レイヤー, Prefab, 設定, レンダーパイプライン, 入力 |
| **ユーティリティ** | 2ツール | スクリプトアウトライン, バッチ実行 |
| **合計** | **17ツール** | **100+操作** |

---

## 必要要件

- Unity 2021.3以降（2022.3以降推奨）
- Python 3.10以降
- uv（推奨）またはpip
- オプション: Input Systemパッケージ（`unity.inputSystem.manage`用）
- オプション: NavMesh Componentsパッケージ（`createNavMeshSurface`用）

---

## トラブルシューティング

### ブリッジが接続しない

1. Unity Consoleでエラーを確認
2. **Tools > MCP Assistant**でブリッジが起動しているか確認
3. ファイアウォールがlocalhost:7077をブロックしていないか確認
4. Pythonサーバーログで接続エラーを確認

### ツールが失敗する

1. GameObjectパスが正しいか確認（"Canvas/Panel/Button"のような階層パスを使用）
2. コンポーネント型名が完全修飾されているか確認（例: "UnityEngine.UI.Text"）
3. Tilemap用: GridとTilemapコンポーネントが存在するか確認
4. NavMesh用: エージェント追加前にNavMeshがベイクされているか確認
5. Unity Consoleで詳細なエラーメッセージを確認

### コンパイル後

ブリッジはUnityがスクリプトを再コンパイルした後、自動的に接続状態を保存して再接続します。手動での介入は不要です。

### TileMap問題

- 指定されたパスにタイルアセットが存在するか確認
- GridとTilemapの階層構造を確認
- `inspectTilemap`を使用して状態を確認

### NavMesh問題

- ジオメトリがNavigation Staticとしてマークされているか確認
- エージェントをテストする前にNavMeshをベイク
- Sceneビューでナビゲーション可視化を確認（Window > AI > Navigation）
- `updateSettings`は読み取り専用 - ベイク設定にはUnity Navigationウィンドウを使用

---

## パフォーマンスのヒント

1. **バッチ操作を使用**: 複数の操作を組み合わせて性能向上
2. **コンテキスト更新を制限**: 大きなシーンではコンテキスト更新間隔を増やす
3. **Tilemapバッチ操作**: 複数の`setTile`呼び出しの代わりに`fillArea`を使用
4. **アセット参照をキャッシュ**: アセットを一度読み込み、複数の操作で再利用
5. **エラー時停止**: 依存する操作には`stopOnError: true`を設定

---

## ドキュメント

- **メインドキュメント**: [CLAUDE.md](../CLAUDE.md)
- **TileMap & NavMeshガイド**: [TILEMAP_NAVMESH_TOOLS.md](../TILEMAP_NAVMESH_TOOLS.md)
- **バッチ処理例**: [BATCH_PROCESSING_EXAMPLES.md](../BATCH_PROCESSING_EXAMPLES.md)
- **このファイル**: 完全なツールリファレンスとクイックスタート

---

## ライセンス

[ライセンスをここに追加]

## 貢献


貢献を歓迎します！開発ガイドは[CLAUDE.md](../CLAUDE.md)をお読みください。

## サポート

問題、質問、機能リクエストについて:
1. Unity Consoleでエラーメッセージを確認
2. [CLAUDE.md](../CLAUDE.md)のドキュメントを確認
3. [BATCH_PROCESSING_EXAMPLES.md](../BATCH_PROCESSING_EXAMPLES.md)のバッチ操作例を確認
4. プロジェクトリポジトリにissueを作成

---

**UnityMCP** - Model Context Protocolによる包括的なUnity Editor自動化
