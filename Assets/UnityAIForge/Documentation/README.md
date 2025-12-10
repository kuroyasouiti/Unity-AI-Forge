# Unity-AI-Forge - AI駆動型Unity開発ツールキット

**AI連携でUnityゲームを創造。Model Context ProtocolとGameKitフレームワークの統合。**

[![Python](https://img.shields.io/badge/Python-3.10%2B-blue)](https://www.python.org/)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black)](https://unity.com/)
[![MCP](https://img.shields.io/badge/MCP-0.9.0%2B-green)](https://modelcontextprotocol.io/)
[![Version](https://img.shields.io/badge/Version-2.3.5-brightgreen)](https://github.com/kuroyasouiti/Unity-AI-Forge/releases)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 🆕 v2.3.5の新機能

- **🖥️ CLI ベースの MCP サーバー登録機能**
  - AIツール（Cursor、Claude Code、Cline、Windsurf）への MCP サーバー登録を CLI 経由で実行
  - JSON 設定ファイル直接編集よりも信頼性が高く、公式CLIコマンドを使用
  - CLI 非対応ツール（Claude Desktop）は従来の JSON 編集にフォールバック
  - Claude Code サポート追加（`claude mcp add/remove` コマンド対応）

- **🛠️ MCP Bridge Window 機能強化**
  - 「AI Tool Registration (CLI)」セクションを新規追加
  - 各 AI ツールの CLI 利用可否と登録状態をリアルタイム表示
  - Register All/Unregister All による一括操作

### v2.3.4の修正と追加

- **🔗 Unity オブジェクト参照の文字列パス解決**
  - `propertyChanges` で文字列パスから Unity オブジェクト参照を自動解決
  - `TMP_Text`, `Button`, `InputField` などのコンポーネント参照をパスで指定可能に
  - 例: `"titleText": "Canvas/Panel/TitleText"` → 自動的にTMP_Text参照に変換

### v2.3.3の修正と追加

- **🎨 ComponentCommandHandler の機能強化**
  - **propertyFilter 修正**: `inspect` 操作で指定したプロパティのみを取得可能に
  - **addMultiple 初期プロパティ**: コンポーネント追加時に `propertyChanges` で初期値を設定可能
  - **Unity型変換の大幅拡張**: Color, Vector2/3/4, Quaternion, Rect, Bounds, Enum 型のDictionary形式からの自動変換

### v2.3.2の修正と追加

- **🎬 GameKitSceneFlow 自動ロードシステム**
  - **プレハブベース管理**: `Resources/GameKitSceneFlows/` にプレハブを配置
  - **自動ロード**: Editor (Play Mode) と Runtime (ビルド) で自動読み込み
  - **初期シーン不要**: どのシーンからでも使用可能
  - **Git管理可能**: プレハブファイルでチーム協業をサポート
  - **DontDestroyOnLoad**: 自動的に永続化
  - Unity Editorメニュー: `Tools → Unity-AI-Forge → GameKit → Create SceneFlows Directory`

- **🐛 Unity Editorフリーズ問題の完全解決**
  - C#スクリプト作成・更新・削除時のフリーズを修正
  - Unity側の同期待機を削除し、MCPサーバー側で非同期処理を実装
  - コンパイル結果（成功/失敗、エラー数、経過時間）をレスポンスに含めるように改善
  - Unity Editorのメインスレッドをブロックしない最適化

### トピックアップデート

- **🔐 ブリッジトークン自動同期**: MCPサーバーインストール時に `.mcp_bridge_token` をコピー/生成。Pythonサーバーはインストール先のトークンを自動参照し、WebSocketはクエリパラメータで認証する互換仕様に。
- **🎛 ビルド設定管理**: `unity_projectSettings_crud` でシーンの追加/削除/並び替え/有効化をサポート。
- **🖌 レンダリングレイヤー管理**: URP/HDRPのレンダリングレイヤー追加/削除に対応。

### 前回のリリース（v2.3.0）のハイライト

- **⚙️ Physics2D 完全サポート**: 2D物理設定の読み書き
  - 2D重力の設定 (gravity x/y)
  - 速度・位置反復回数、閾値の調整
  - シミュレーションモードの制御
  - 2Dゲームに必要な全物理パラメータに対応

- **🎨 ソートレイヤー管理**: 2Dスプライトの描画順序を完全制御
  - ソートレイヤーの追加/削除
  - レイヤー一覧の取得
  - Unity Editorの手動設定が不要に

- **🏃 CharacterController Bundle**: 3Dキャラクター設定を簡単に
  - 7つの最適化プリセット (fps/tps/platformer/child/large/narrow/custom)
  - 自動的に適切なカプセルサイズと物理パラメータを設定
  - バッチ適用で複数キャラクターを一括設定

- **📋 Batch Sequential Execute**: 複雑な多段階処理を確実に
  - 順次実行でエラー発生時に自動停止
  - レジューム機能で中断した処理を再開
  - 進捗状態の保存と確認が可能

- **📖 GameKit Machinations詳細ドキュメント**: 経済システム設計ガイド
  - Resource Pools/Flows/Converters/Triggersの4要素を詳しく解説
  - 実践的な使用例とユースケースを追加
  - RPG、リソース管理、カードゲームでの活用方法

**ツール総数: 22 → 24ツール** (Mid-Level Batchに2ツール追加)

### 前回のリリース（v2.1.0）のハイライト

- **💾 ステート永続化システム**: 完全なsave/load機能
  - リソース状態をJSONでエクスポート/インポート
  - ファイルまたはPlayerPrefsへの保存
  - タイムスタンプとメタデータの自動追跡
  - シリアライズ可能な状態でクラウドセーブ対応
  - 簡単な統合のためのManager便利メソッド

- **🎮 GameKit UICommand拡張**: Manager制御のサポート
  - **新しいターゲットタイプ**: ActorまたはManager
  - **11種類のコマンド**: Move/Jump/Action/Look/Custom + AddResource/SetResource/ConsumeResource/ChangeState/NextTurn/TriggerScene
  - UIボタンでゲーム経済、状態、ターンを制御可能
  - ストラテジーゲーム、ショップUI、リソース管理に最適

- **📊 GameKit Machinations強化**: ダイアグラム実行
  - ProcessDiagramFlows() - 自動リソースフローの実行
  - CheckDiagramTriggers() - しきい値イベントの監視
  - ExecuteConverter() - 特定のリソース変換の実行
  - SetFlowEnabled() - ランタイムでの動的フロー制御
  - フローとトリガーの自動実行モード

- **🎯 ResourceManager簡素化**: コア機能に集中
  - 純粋なリソースストレージとイベント管理
  - 複雑なロジックは外部コントローラーまたはMachinationsに移動
  - パフォーマンスの向上（デフォルトでUpdate()オーバーヘッドなし）
  - 関心事のより明確な分離

### 前回のリリース（v2.0.0）のハイライト

- **🎯 ハブベースアーキテクチャ**: すべてのGameKitコンポーネントをインテリジェントハブとして再設計
- **🎮 GameKit Actor**: 8つの動作プロファイル、4つの制御モード、UnityEvents
- **⚙️ GameKit Manager Hub**: 動的なモード固有コンポーネント（TurnBased、ResourcePool、EventHub、StateManager、Realtime）
- **🎭 GameKit Interaction Hub**: マルチトリガー宣言型システム、特殊トリガー搭載
- **🎬 GameKit SceneFlow**: シーン中心の遷移システム、加算読み込み対応
- **🛤️ Spline Movement**: 2.5Dスプライン移動、Catmull-Rom補間対応

- **🛤️ Spline Movement**: 2.5Dスプライン移動
  - Catmull-Rom補間
  - クローズドループ、横方向オフセット、自動回転
  - 手動/自動速度制御

- **中レベルツール**: バッチ操作とプリセット
  - Transform/RectTransformバッチ操作（配置、整列、分散）
  - 物理バンドル（2D/3Dプリセット: dynamic、kinematic、character、platformer、vehicle）
  - CharacterControllerバンドル（fps、tps、platformer、child、large、narrowプリセット）
  - カメラリグ（follow、orbit、split-screen、fixed、dolly）
  - UI基盤（Canvas、Panel、Button、Text、Image、InputField）
  - オーディオソースバンドル（music、sfx、ambient、voice、uiプリセット）
  - 入力プロファイル（新Input System統合）

- **コンパイル待機機能**: 自動コンパイル処理
  - 操作を実行してから、トリガーされた場合はコンパイルを待機
  - 早期待機解除のためのブリッジ再接続検出
  - レスポンスでの透明な待機情報

- **包括的なテストスイート**: 100以上のユニットテスト
  - Unity Test Framework統合
  - 全ツールカテゴリで97.7%の合格率
  - GitHub ActionsによるCI/CD
  - エディタメニュー統合（`Tools > Unity-AI-Forge > Run All Tests`）

- **ドキュメント**: 完全な見直し
  - テストスイートのドキュメントと結果
  - ツールロードマップ（日本語）
  - コンパイル待機機能ガイド
  - レガシークリーンアップサマリー
  - [完全なリリースノート](docs/Release_Notes_v1.8.0.md)
  - [変更履歴](CHANGELOG.md)

## 📦 パッケージ構造

Unity-AI-ForgeはMCPサーバーを統合したUnityパッケージです！

```
Unity-AI-Forge/
├── Assets/
│   └── UnityAIForge/                           # Unity Package
│       ├── Editor/
│       │   └── MCPBridge/                      # Unity C# WebSocketブリッジ
│       ├── GameKit/                            # GameKitフレームワーク ランタイム
│       ├── MCPServer/                          # ⭐ MCPサーバー（Python、docs、tools）
│       │   ├── src/                            # Python MCPサーバー
│       │   ├── setup/                          # インストールスクリプト
│       │   ├── examples/                       # 実践的なチュートリアル
│       │   ├── config/                         # 設定テンプレート
│       │   └── docs/                           # 追加ドキュメント
│       ├── Tests/                              # テストスイート
│       └── package.json                        # Unity Package定義
```

## 🚀 クイックスタート

### 1. Unityパッケージのインストール

**オプションA: Unity Package Manager経由（推奨）**

1. Unity Editorを開く
2. **Window > Package Manager**を開く
3. **+（プラス）**ボタンをクリック → **Add package from git URL...**
4. 入力: `https://github.com/kuroyasouiti/Unity-AI-Forge.git?path=/Assets/UnityAIForge`
5. **Add**をクリック

**オプションB: 手動インストール**

1. このリポジトリをダウンロード
2. `Assets/UnityAIForge`をUnityプロジェクトの`Assets/`フォルダにコピー

### 2. MCPサーバーのインストール

MCPサーバーは`Assets/UnityAIForge/MCPServer/`にあります。

**オプションA: Unity経由の自動インストール（推奨）**

1. パッケージがインストールされたUnity Editorを開く
2. **Tools > Unity-AI-Forge > MCP Server Manager**に移動
3. **Install Server**をクリック（`~/Unity-AI-Forge`にインストール）
4. AIツール（Cursor、Claude Desktopなど）用に**Register**をクリック
5. AIツールを再起動

**オプションB: 手動セットアップ**

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

**オプションC: 手動設定**

Claude Desktopの設定（`~/.claude/claude_desktop_config.json`）に追加:
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

`/path/to/Unity-AI-Forge`を以下に置き換え:
- Windows: `C:\Users\YOUR_USERNAME\Unity-AI-Forge`
- macOS/Linux: `/Users/YOUR_USERNAME/Unity-AI-Forge` または `/home/YOUR_USERNAME/Unity-AI-Forge`

### 3. Unity Bridgeの起動

1. プロジェクトでUnity Editorを開く
2. **Tools > MCP Assistant**に移動
3. **Start Bridge**をクリック
4. "Connected"ステータスを待つ

### 4. 接続テスト

Claude Desktopで以下を試してください:
```
Unity MCP接続をテストしてもらえますか？
```

AIは`unity_ping()`を呼び出し、Unityのバージョン情報を表示します。

## 📚 ドキュメント

### ユーザー向け

- **[MCP Server QUICKSTART](Assets/UnityAIForge/MCPServer/QUICKSTART.md)** - 5分で始める
- **[MCP Server README](Assets/UnityAIForge/MCPServer/README.md)** - 完全なMCPサーバードキュメント
- **[インストールガイド](Assets/UnityAIForge/MCPServer/INSTALL_GUIDE.md)** - 詳細なインストール手順
- **[Examples](Assets/UnityAIForge/MCPServer/examples/)** - 実践的なチュートリアルとウォークスルー

### 開発者向け

- **[CLAUDE.md](CLAUDE.md)** - Claude Code統合の手順
- **[テストスイート](Assets/UnityAIForge/Tests/Editor/README.md)** - すべてのツールの包括的なテストスイート
- **[ドキュメントインデックス](docs/)** - 追加のガイドとリリースノート

## 🏗️ アーキテクチャ

```
AIクライアント (Claude/Cursor) <--(MCP)--> Python MCPサーバー <--(WebSocket)--> Unity C# Bridge
                                      (MCPServer/src/)         (Editor/MCPBridge/)
```

### コンポーネント

| コンポーネント | 場所 | 説明 |
|-----------|----------|-------------|
| **Unity C# Bridge** | `Assets/UnityAIForge/Editor/MCPBridge/` | Unity Editor内で実行されるWebSocketサーバー |
| **Python MCPサーバー** | `Assets/UnityAIForge/MCPServer/src/` | MCPプロトコル実装 |
| **GameKitフレームワーク** | `Assets/UnityAIForge/GameKit/Runtime/` | 高レベルゲーム開発コンポーネント |
| **セットアップスクリプト** | `Assets/UnityAIForge/MCPServer/setup/` | インストールと設定ヘルパー |
| **Examples** | `Assets/UnityAIForge/MCPServer/examples/` | 実践的なチュートリアルとガイド |
| **Tests** | `Assets/UnityAIForge/Tests/Editor/` | 包括的なテストスイート |

## 🧪 テスト

Unity Test Frameworkによる包括的なテストスイート:

- **100以上のユニットテスト** すべてのツールカテゴリをカバー
- **自動化されたCI/CD** GitHub Actions使用
- **エディタメニュー統合** 素早いテスト実行のため
- **コマンドラインテストランナー** バッチテスト用

テストの実行方法:
- Unity Editor: `Tools > Unity-AI-Forge > Run All Tests`
- PowerShell: `.\run-tests.ps1`
- Bash: `./run-tests.sh`

詳細は[テストスイートドキュメント](Assets/Unity-AI-Forge/Tests/Editor/README.md)を参照してください。

## ✨ 機能

### コアツール

- **シーン管理** - シーンの作成、読み込み、保存、削除、検査
- **GameObject CRUD** - バッチ操作による完全な階層操作
- **Component CRUD** - バッチサポート付きのコンポーネントの追加、更新、削除
- **アセット操作** - 名前変更、複製、削除、検査、インポーター設定の更新
- **ScriptableObject管理** - ScriptableObjectアセットの作成、検査、更新、削除、複製、検索
- **Prefab管理** (`unity_prefab_crud`) - GameObjectからのPrefab作成、更新、検査、シーンへのインスタンス化、アンパック、オーバーライドの適用/復帰
- **ベクタースプライト変換** (`unity_vector_sprite_convert`) - プリミティブ（正方形、円、三角形、多角形）からのスプライト生成、SVGインポート、テクスチャ変換、単色スプライト作成
- **プロジェクト設定** - プレイヤー、品質、時間、物理、オーディオ、エディタ設定の構成
- **タグとレイヤー** - プロジェクト設定ツールからタグとレイヤーの追加または削除

### 中レベルバッチツール

- **Transform Batch** (`unity_transform_batch`) - 円/線でのオブジェクト配置、連続/リストベースの名前変更、メニュー階層の自動生成
- **RectTransform Batch** (`unity_rectTransform_batch`) - アンカー/ピボット/サイズ/位置の設定、親プリセットへの整列、水平/垂直分散、ソースからのサイズマッチング
- **Physics Bundle** (`unity_physics_bundle`) - 2D/3D Rigidbody + Colliderプリセット（dynamic、kinematic、static、character、platformer、topDown、vehicle、projectile）の適用、個別の物理プロパティ更新、物理コンポーネントの検査
- **Camera Rig** (`unity_camera_rig`) - ターゲット追跡、スムーズな移動、ビューポート設定を備えたカメラリグ（follow、orbit、split-screen、fixed、dolly）の作成
- **UI Foundation** (`unity_ui_foundation`) - アンカープリセット、TextMeshProサポート、自動レイアウトを備えたUI要素（Canvas、Panel、Button、Text、Image、InputField）の作成
- **Audio Source Bundle** (`unity_audio_source_bundle`) - プリセット（music、sfx、ambient、voice、ui）、2D/3D空間オーディオ、ミキサーグループ統合によるAudioSourceの作成と設定
- **Input Profile** (`unity_input_profile`) - 新Input SystemでのPlayerInput作成、アクションマップの設定、通知動作の設定、InputActionsアセットの作成

### 高レベルGameKitツール

- **GameKit Actor** (`unity_gamekit_actor`) - UnityEvents（OnMoveInput、OnJumpInput、OnActionInput、OnLookInput）を介して入力を中継するコントローラー-動作ハブとしてのゲームアクターを作成
- **GameKit Manager** (`unity_gamekit_manager`) - 永続性、ターンフェーズ、リソース管理を備えたゲームマネージャー（ターンベース、リアルタイム、リソースプール、イベントハブ、ステートマネージャー）の作成
- **GameKit Interaction** (`unity_gamekit_interaction`) - 宣言的アクション（spawn、destroy、sound、message、scene change）と条件を備えたインタラクショントリガー（collision、raycast、proximity、input）の作成
- **GameKit UI Command** (`unity_gamekit_ui_command`) - UIコマンド制御モードのアクターにコマンドを送信するボタン付きコマンドパネルの作成、水平/垂直/グリッドレイアウトのサポート
- **GameKit SceneFlow** (`unity_gamekit_sceneflow`) - ステートマシン、加算読み込み、永続マネージャーシーン、共有シーングループ（UI/Audio）、トリガーベース遷移によるシーン遷移の管理

## 📦 ScriptableObject管理の例

```python
# ScriptableObjectアセットを作成
unity_scriptableobject_manage({
    "operation": "create",
    "typeName": "MyGame.Data.GameConfig",
    "assetPath": "Assets/Data/DefaultConfig.asset",
    "properties": {
        "gameName": "Adventure Quest",
        "maxPlayers": 8,
        "gameSpeed": 1.5,
        "enableDebugMode": True
    }
})

# プロパティを検査
config_info = unity_scriptableobject_manage({
    "operation": "inspect",
    "assetPath": "Assets/Data/DefaultConfig.asset",
    "includeProperties": True
})

# 選択した値を更新
unity_scriptableobject_manage({
    "operation": "update",
    "assetPath": "Assets/Data/DefaultConfig.asset",
    "properties": {
        "maxPlayers": 16,
        "gameSpeed": 2.0
    }
})

# 実験用に複製
unity_scriptableobject_manage({
    "operation": "duplicate",
    "sourceAssetPath": "Assets/Data/DefaultConfig.asset",
    "destinationAssetPath": "Assets/Data/HighSpeedConfig.asset"
})

# フォルダ内のすべての設定をリスト
all_configs = unity_scriptableobject_manage({
    "operation": "findByType",
    "typeName": "MyGame.Data.GameConfig",
    "searchPath": "Assets/Data",
    "includeProperties": False
})
```

## 🛠️ 開発

### プロジェクト構造

```
Unity-AI-Forge/
├── Assets/
│   └── UnityAIForge/                # Unity Package
│       ├── Editor/
│       │   └── MCPBridge/           # Unity C# Bridge
│       │       ├── McpBridgeService.cs
│       │       ├── McpCommandProcessor.cs
│       │       ├── McpContextCollector.cs
│       │       └── Handlers/        # ツール実装
│       ├── GameKit/
│       │   └── Runtime/             # GameKitフレームワーク
│       │       ├── Actor/
│       │       ├── Manager/
│       │       ├── Interaction/
│       │       └── SceneFlow/
│       ├── MCPServer/               # MCPサーバー（Python）
│       │   ├── src/                 # サーバーソース
│       │   │   ├── bridge/          # Unity Bridge通信
│       │   │   ├── tools/           # MCPツール定義
│       │   │   ├── resources/       # MCPリソース
│       │   │   └── main.py          # エントリーポイント
│       │   ├── setup/               # インストールスクリプト
│       │   ├── examples/            # チュートリアル
│       │   ├── config/              # 設定テンプレート
│       │   ├── skill.yml            # MCPサーバーマニフェスト
│       │   └── pyproject.toml       # Pythonパッケージ設定
│       ├── Tests/
│       │   └── Editor/              # Unity Test Frameworkテスト
│       └── package.json             # Unity Package定義
│
├── ProjectSettings/                 # Unityプロジェクト設定
├── Packages/                        # Unityパッケージ
├── docs/                            # プロジェクトドキュメント
└── README.md                        # このファイル
```

### 開発依存関係のインストール

```bash
cd Unity-AI-Forge
uv sync --dev
```

### テストの実行

```bash
cd Unity-AI-Forge
pytest
```

### コードのフォーマット

```bash
cd Unity-AI-Forge
black src/
ruff check src/
```

## 🤝 コントリビューション

コントリビューションを歓迎します！以下の手順をお願いします：

1. リポジトリをフォーク
2. フィーチャーブランチを作成
3. 変更を加える
4. テストとドキュメントを追加
5. プルリクエストを送信

開発ガイドラインについては[CLAUDE.md](CLAUDE.md)を参照してください。

## 📄 ライセンス

MITライセンス - 詳細は[MITライセンス](https://opensource.org/licenses/MIT)を参照してください。

## 🙏 謝辞

- **Model Context Protocol** by Anthropic
- **Unity Technologies** 素晴らしいゲームエンジンに感謝
- すべてのコントリビューターとコミュニティメンバー

## 🆘 サポート

- **クイックスタート**: [Assets/UnityAIForge/MCPServer/QUICKSTART.md](Assets/UnityAIForge/MCPServer/QUICKSTART.md)
- **Examples**: [Assets/UnityAIForge/MCPServer/examples/](Assets/UnityAIForge/MCPServer/examples/)
- **インストールガイド**: [Assets/UnityAIForge/MCPServer/INSTALL_GUIDE.md](Assets/UnityAIForge/MCPServer/INSTALL_GUIDE.md)
- **Issues**: [GitHub Issues](https://github.com/kuroyasouiti/Unity-AI-Forge/issues)

## 🔄 旧構造からの移行

旧構造を使用していた場合:

1. **Unity側**: 正しいパスでUnity Package Manager経由でインストール:
   ```
   https://github.com/kuroyasouiti/Unity-AI-Forge.git?path=/Assets/UnityAIForge
   ```
2. **MCPサーバー側**: UnityのMCP Server Managerを使用:
   - **Tools > Unity-AI-Forge > MCP Server Manager**に移動
   - **Install Server**をクリックしてファイルを`~/.claude/skills/Unity-AI-Forge`にコピー
   - AIツール用に**Register**をクリック
3. 必要に応じて古いインストールファイルを削除

---

**UnityとAIコミュニティのために ❤️ を込めて作られました**

**今すぐAI支援でUnityプロジェクトの構築を始めましょう！** 🚀
