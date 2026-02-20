# Unity-AI-Forge - AI駆動型Unity開発ツールキット

[![Python](https://img.shields.io/badge/Python-3.10%2B-blue)](https://www.python.org/)
[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black)](https://unity.com/)
[![MCP](https://img.shields.io/badge/MCP-0.9.0%2B-green)](https://modelcontextprotocol.io/)
[![Version](https://img.shields.io/badge/Version-2.9.0-brightgreen)](https://github.com/kuroyasouiti/Unity-AI-Forge/releases)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Unity-AI-Forgeは、AIとの協働でUnityゲームを鍛造する開発ツールキットです。Model Context Protocol統合とGameKitフレームワークにより、AIアシスタントがUnity Editorとリアルタイムで対話。**49ツール**を備え、Low-Level CRUD操作、Mid-Levelバッチツール、High-Level GameKit（3本柱アーキテクチャ）の3層構造で、シンプルなアセット操作から複雑なゲームシステム構築まで対応します。

## v2.9.0のハイライト

- **コード生成アーキテクチャへの完全移行**
  - GameKitハンドラーがスタンドアロンC#スクリプトをテンプレートから生成
  - ランタイムライブラリ（`UnityAIForge.GameKit.Runtime`）を削除し、ゼロ依存を実現
  - ユーザープロジェクトはUnity-AI-Forgeパッケージをアンインストールしても動作可能

- **GameKit 3本柱アーキテクチャ（15ツール）**
  - **UIピラー** (5): UICommand, UIBinding, UIList, UISlot, UISelection（UI Toolkit ベース）
  - **Presentationピラー** (5): AnimationSync, Effect, Feedback, VFX, Audio
  - **Logicピラー** (5): Integrity検証, ClassCatalog, ClassDependencyGraph, SceneReferenceGraph, SceneRelationshipGraph

- **ツール構成の最適化（49ツール）**
  - GameKit 15 (UI 5 + Presentation 5 + Logic 5)
  - Mid-Level 20 (バッチ操作 + ビジュアル制御 + UI Toolkit)
  - Low-Level 8 / Utility 5 / Settings 1 / Batch 1

## 📦 パッケージ構造

```
Unity-AI-Forge/
├── Assets/
│   └── UnityAIForge/                           # Unity Package
│       ├── Editor/
│       │   ├── MCPBridge/                      # Unity C# WebSocketブリッジ
│       │   │   └── Handlers/                   # 48ハンドラー（6カテゴリ）
│       │   ├── CodeGen/                        # コード生成インフラ
│       │   │   └── Templates/                  # 11テンプレート（*.cs.txt）
│       │   └── MCPServerManager/               # サーバー管理UI
│       ├── MCPServer/                          # MCPサーバー（Python）
│       │   ├── src/                            # Python MCPサーバー
│       │   ├── setup/                          # インストールスクリプト
│       │   └── config/                         # 設定テンプレート
│       ├── Tests/                              # テストスイート
│       ├── Documentation/                      # ドキュメント
│       └── package.json                        # Unity Package定義
```

## クイックスタート

### 1. Unityパッケージのインストール

**方法A: Unity Package Manager経由（推奨）**

1. Unity Editorを開く
2. **Window > Package Manager**を開く
3. **+ (プラス)** ボタン → **Add package from git URL...**をクリック
4. 次のURLを入力: `https://github.com/kuroyasouiti/Unity-AI-Forge.git?path=/Assets/UnityAIForge`
5. **Add**をクリック

**方法B: 手動インストール**

1. このリポジトリをダウンロード
2. `Assets/UnityAIForge`をあなたのUnityプロジェクトの`Assets/`フォルダにコピー

### 2. MCPサーバーのインストール

MCPサーバーは `Assets/UnityAIForge/MCPServer/` にあります。

**方法A: Unity経由の自動インストール（推奨）**

1. パッケージをインストールしたUnity Editorを開く
2. **Tools > Unity-AI-Forge > MCP Server Manager**へ
3. **Install Server**をクリック（`~/Unity-AI-Forge`にインストール）
4. 使用するAIツール（Cursor、Claude Desktopなど）の**Register**をクリック
5. AIツールを再起動

**方法B: 手動セットアップ**

```bash
# Windows (PowerShell)
xcopy /E /I /Y "Assets\UnityAIForge\MCPServer" "%USERPROFILE%\Unity-AI-Forge"
cd %USERPROFILE%\Unity-AI-Forge
uv sync

# macOS/Linux
cp -r Assets/UnityAIForge/MCPServer ~/Unity-AI-Forge
cd ~/Unity-AI-Forge
uv sync
```

**方法C: 手動設定**

Claude Desktopの設定ファイル（`~/.claude/claude_desktop_config.json`）に追加：
```json
{
  "mcpServers": {
    "unity-ai-forge": {
      "command": "uv",
      "args": ["--directory", "/path/to/Unity-AI-Forge", "run", "unity-ai-forge"],
      "env": {
        "MCP_SERVER_TRANSPORT": "stdio",
        "MCP_LOG_LEVEL": "info"
      }
    }
  }
}
```

`/path/to/Unity-AI-Forge` を実際のパスに置き換えてください：
- Windows: `C:\Users\YOUR_USERNAME\Unity-AI-Forge`
- macOS: `/Users/YOUR_USERNAME/Unity-AI-Forge`
- Linux: `/home/YOUR_USERNAME/Unity-AI-Forge`

### 3. Unity Bridgeの起動

1. Unityプロジェクトを開く
2. **Tools > MCP Assistant**を開く
3. **Start Bridge**をクリック
4. ステータスが「Connected」になるまで待つ

### 4. 接続テスト

Claude Desktopで以下のように尋ねてください：
```
Unity MCP接続をテストしてください
```

AIが`unity_ping()`を呼び出し、Unityバージョン情報を表示するはずです。

## アーキテクチャ

```
AIクライアント (Claude Code/Cursor) <--(MCP)--> Pythonサーバー <--(WebSocket)--> Unity Editorブリッジ
                                                (MCPServer/src/)      (Editor/MCPBridge/)
```

### コンポーネント

| コンポーネント | 場所 | 説明 |
|-----------|----------|-------------|
| **Unity C#ブリッジ** | `Editor/MCPBridge/` | Unity Editor内で動作するWebSocketサーバー |
| **Python MCPサーバー** | `MCPServer/src/` | MCPプロトコル実装 |
| **GameKitコード生成** | `Editor/CodeGen/` | テンプレートベースのコード生成インフラ |
| **MCP Server Manager** | `Editor/MCPServerManager/` | サーバーライフサイクル管理UI |
| **セットアップスクリプト** | `MCPServer/setup/` | インストールと設定ヘルパー |
| **テスト** | `Tests/Editor/` | 包括的テストスイート |

## ✨ 機能

### Low-Levelツール（8ツール - 基本CRUD操作）

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity_scene_crud` | シーン管理 | create, load, save, delete, duplicate, inspect |
| `unity_gameobject_crud` | GameObjectヒエラルキー管理 | create, delete, move, rename, update, duplicate, inspect, batch |
| `unity_component_crud` | コンポーネント操作 | add, remove, update, inspect, batch |
| `unity_asset_crud` | アセットファイル操作 | create, update, rename, duplicate, delete, inspect |
| `unity_scriptableObject_crud` | ScriptableObject管理 | create, inspect, update, delete, duplicate, list, findByType |
| `unity_prefab_crud` | Prefab管理 | create, update, inspect, instantiate, unpack, applyOverrides, revertOverrides |
| `unity_vector_sprite_convert` | Vector/Sprite変換 | primitiveToSprite, svgToSprite, textureToSprite, createColorSprite |
| `unity_projectSettings_crud` | プロジェクト設定管理 | read, write, list（Player, Quality, Physics, Audio, Editor, ビルド設定, タグ/レイヤー） |

### Mid-Levelツール（20ツール - バッチ操作・プリセット・ビジュアル制御）

| ツール | 説明 |
|------|------|
| `unity_transform_batch` | Transformバッチ操作（配置、リネーム） |
| `unity_rectTransform_batch` | RectTransformバッチ（アンカー、整列、分配） |
| `unity_physics_bundle` | 物理バンドル（2D/3Dプリセット） |
| `unity_camera_rig` | カメラリグ（follow, orbit, splitScreen, fixed, dolly） |
| `unity_ui_foundation` | UI基礎（Canvas, Button, Text, Image, etc.） |
| `unity_ui_hierarchy` | 宣言的UIヒエラルキー |
| `unity_ui_state` | UI状態管理 |
| `unity_ui_navigation` | キーボード/ゲームパッドナビゲーション |
| `unity_audio_source_bundle` | オーディオソースバンドル（music, sfx, ambient, voice, ui） |
| `unity_input_profile` | 入力プロファイル（New Input System） |
| `unity_character_controller_bundle` | CharacterControllerバンドル |
| `unity_tilemap_bundle` | Tilemap作成・管理 |
| `unity_sprite2d_bundle` | 2Dスプライト管理 |
| `unity_animation2d_bundle` | 2Dアニメーションセットアップ |
| `unity_animation3d_bundle` | 3Dアニメーション（BlendTree, AvatarMask） |
| `unity_material_bundle` | マテリアル作成・プロパティ管理 |
| `unity_light_bundle` | ライトセットアップ（directional, point, spot, area） |
| `unity_particle_bundle` | パーティクルシステム作成・設定 |
| `unity_uitk_document` | UI Toolkit UIDocument管理 |
| `unity_uitk_asset` | UI Toolkitアセット作成（UXML, USS） |

### Utilityツール（5ツール）

| ツール | 説明 |
|------|------|
| `unity_ping` | ブリッジ接続確認 |
| `unity_compilation_await` | コンパイル完了待機 |
| `unity_playmode_control` | プレイモード制御（play/pause/stop/step） |
| `unity_console_log` | コンソールログ取得・フィルタリング |
| `unity_event_wiring` | UnityEventワイヤリング（Button.onClick等） |

### Settings（1ツール）

| ツール | 説明 |
|------|------|
| `unity_projectSettings_crud` | プロジェクト設定管理 |

### High-Level GameKitツール（15ツール - 3本柱アーキテクチャ）

GameKitはコード生成方式でスタンドアロンC#スクリプトを生成。ユーザープロジェクトにゼロ依存。

**UIピラー (5ツール)** - UI ToolkitベースのUI連携:

| ツール | 説明 |
|------|------|
| `unity_gamekit_ui_command` | UIコマンドパネル（ボタン→アクション） |
| `unity_gamekit_ui_binding` | UIデータバインディング |
| `unity_gamekit_ui_list` | UIリスト・ScrollView管理 |
| `unity_gamekit_ui_slot` | UIスロットシステム |
| `unity_gamekit_ui_selection` | UI選択管理 |

**Presentationピラー (5ツール)** - 視覚・聴覚フィードバック:

| ツール | 説明 |
|------|------|
| `unity_gamekit_animation_sync` | アニメーション同期 |
| `unity_gamekit_effect` | エフェクト管理 |
| `unity_gamekit_feedback` | フィードバックシステム |
| `unity_gamekit_vfx` | VFX管理 |
| `unity_gamekit_audio` | オーディオ管理 |

**Logicピラー (5ツール)** - 分析・整合性検証:

| ツール | 説明 |
|------|------|
| `unity_validate_integrity` | シーン整合性検証（欠落スクリプト、null参照、壊れたイベント/Prefab） |
| `unity_class_catalog` | 型列挙・検査（クラス、MonoBehaviour、enum） |
| `unity_class_dependency_graph` | クラス依存関係分析 |
| `unity_scene_reference_graph` | シーン内参照分析 |
| `unity_scene_relationship_graph` | シーン遷移・関係性分析 |

### バッチ実行ツール (1ツール)

| ツール | 説明 |
|------|------|
| `unity_batch_sequential_execute` | 逐次実行（レジューム対応） |

## ツールリファレンスサマリー

| カテゴリ | ツール数 | 主なツール |
|---------|---------|----------|
| **Low-Level CRUD** | 8 | Scene, GameObject, Component, Asset, ScriptableObject, Prefab, VectorSprite, ProjectSettings |
| **Mid-Level Batch** | 20 | Transform, RectTransform, Physics, Camera, UI, Audio, Input, Material, Light, Particle, UIToolkit, etc. |
| **Utility** | 5 | Ping, CompilationAwait, PlayModeControl, ConsoleLog, EventWiring |
| **GameKit** | 15 | UI Pillar (5) + Presentation Pillar (5) + Logic Pillar (5) |
| **Batch** | 1 | BatchSequentialExecute |
| **合計** | **49ツール** | |

## 使用例

### 例1: UIコマンドの作成（GameKit UIピラー）

```python
# UIコマンドパネルをコード生成で作成
unity_gamekit_ui_command({
    "operation": "create",
    "gameObjectPath": "Canvas/ActionPanel",
    "commandId": "player_actions",
    "className": "PlayerActionCommands"
})

# コンパイル完了を待機
unity_compilation_await({})
```

### 例2: Mid-Levelツール - カメラリグとUI

```python
# フォローカメラリグを作成
unity_camera_rig({
    "operation": "createRig",
    "rigType": "follow",
    "cameraName": "MainCamera",
    "targetPath": "Player",
    "offset": {"x": 0, "y": 5, "z": -10},
    "smoothSpeed": 5.0
})

# UIキャンバスとボタンを作成
unity_ui_foundation({
    "operation": "createCanvas",
    "canvasName": "GameUI",
    "renderMode": "screenSpaceOverlay"
})

unity_ui_foundation({
    "operation": "createButton",
    "buttonName": "StartButton",
    "parentPath": "GameUI",
    "text": "ゲーム開始",
    "anchorPreset": "center",
    "width": 200,
    "height": 60
})
```

### 例3: シーン整合性検証（GameKit Logicピラー）

```python
# シーンの整合性をチェック
unity_validate_integrity({
    "operation": "validate"
})

# クラス依存関係を分析
unity_class_dependency_graph({
    "operation": "analyze",
    "scriptPath": "Assets/Scripts/PlayerController.cs"
})

# シーン内のオブジェクト参照関係を分析
unity_scene_relationship_graph({
    "operation": "analyze",
    "includeReferences": true,
    "includeEvents": true
})
```

### 例4: ScriptableObjectとPrefab管理

```python
# ScriptableObjectアセットを作成
unity_scriptableobject_manage({
    "operation": "create",
    "typeName": "MyGame.Data.GameConfig",
    "assetPath": "Assets/Data/DefaultConfig.asset",
    "properties": {
        "gameName": "Adventure Quest",
        "maxPlayers": 8
    }
})

# Prefabを作成・インスタンス化
unity_prefab_crud({
    "operation": "create",
    "gameObjectPath": "Enemy",
    "prefabPath": "Assets/Prefabs/Enemy.prefab"
})

unity_prefab_crud({
    "operation": "instantiate",
    "prefabPath": "Assets/Prefabs/Enemy.prefab",
    "targetPath": "Enemies/Enemy_001",
    "position": {"x": 5, "y": 0, "z": 0}
})
```

## 利用可能なリソース

| リソース | 説明 |
|---------|------|
| `unity://project/structure` | プロジェクトディレクトリ構造とアセットリスト |
| `unity://editor/log` | Unity Editorログ（最近のエントリ） |
| `unity://scene/active` | アクティブシーンヒエラルキーとGameObject情報 |
| `unity://scene/list` | プロジェクト内の全シーンリスト |
| `unity://asset/{guid}` | GUIDによるアセット詳細 |

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

- **Bridge Port**: WebSocketリスナーのポート（デフォルト: 7070、自動ポート調整対応）
- **Context Update Interval**: シーン更新のプッシュ頻度（デフォルト: 5秒）
- **Heartbeat Interval**: 接続ヘルスチェック間隔（デフォルト: 10秒）

## 🧪 テスト

Unity Test Frameworkによる包括的なテストスイート：

- **100+ユニットテスト** - 全ツールカテゴリをカバー
- **自動CI/CD** - GitHub Actions統合
- **エディタメニュー統合** - クイックテスト実行
- **コマンドラインテストランナー** - バッチテスト対応

テスト実行方法：
- Unity Editor: `Tools > Unity-AI-Forge > Run All Tests`
- PowerShell: `.\run-tests.ps1`
- Bash: `./run-tests.sh`

詳細は[テストスイートドキュメント](Testing/README.md)を参照してください。

## 必要要件

- Unity 2022.3 LTS以降
- Python 3.10以降
- uv（推奨）またはpip
- .NET Standard 2.1

## トラブルシューティング

### ブリッジが接続しない

1. Unity Consoleでエラーを確認
2. **Tools > MCP Assistant**でブリッジが起動しているか確認
3. ファイアウォールがlocalhostポートをブロックしていないか確認
4. Pythonサーバーログで接続エラーを確認
5. 複数プロジェクトの場合、自動ポート調整が正常に動作しているか確認

### ツールが失敗する

1. GameObjectパスが正しいか確認（"Canvas/Panel/Button"のような階層パスを使用）
2. コンポーネント型名が完全修飾されているか確認（例: "UnityEngine.UI.Text"）
3. Unity Consoleで詳細なエラーメッセージを確認

### コンパイル後

ブリッジはUnityがスクリプトを再コンパイルした後、自動的に接続状態を保存して再接続します。手動での介入は不要です。

### スクリプト作成のベストプラクティス

- `unity_asset_crud`の`create`操作でC#スクリプトを作成
- GameKitツールはコード生成方式でスタンドアロンスクリプトを自動生成
- コード生成後は`unity_compilation_await`でコンパイル完了を待機
- コンパイルエラーはUnity Consoleで確認

## ドキュメント

- **メインドキュメント**: [CLAUDE.md](CLAUDE.md)
- **Getting Started**: [GETTING_STARTED.md](GETTING_STARTED.md)
- **クイックスタート**: [Installation/QUICKSTART.md](Installation/QUICKSTART.md)
- **インストールガイド**: [Installation/INSTALL_GUIDE.md](Installation/INSTALL_GUIDE.md)
- **テストスイート**: [Testing/README.md](Testing/README.md)
- **ドキュメントインデックス**: [INDEX.md](INDEX.md)

## ライセンス

MIT License - [MIT License](https://opensource.org/licenses/MIT)

## 貢献

貢献を歓迎します！開発ガイドは[CLAUDE.md](CLAUDE.md)をお読みください。

## サポート

問題、質問、機能リクエストについて:
1. Unity Consoleでエラーメッセージを確認
2. [CLAUDE.md](CLAUDE.md)のドキュメントを確認
3. [ドキュメントインデックス](INDEX.md)を参照
4. [GitHub Issues](https://github.com/kuroyasouiti/Unity-AI-Forge/issues)にissueを作成

---

**Unity-AI-Forge** - Model Context Protocolによる包括的なUnity Editor自動化
