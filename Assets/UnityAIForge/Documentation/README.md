# Unity-AI-Forge - AI駆動型Unity開発ツールキット

**AI連携でUnityゲームを創造。Model Context ProtocolとGameKitフレームワークの統合。**

[![Python](https://img.shields.io/badge/Python-3.10%2B-blue)](https://www.python.org/)
[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black)](https://unity.com/)
[![MCP](https://img.shields.io/badge/MCP-0.9.0%2B-green)](https://modelcontextprotocol.io/)
[![Version](https://img.shields.io/badge/Version-2.9.0-brightgreen)](https://github.com/kuroyasouiti/Unity-AI-Forge/releases)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## v2.9.0 のハイライト

- **コード生成アーキテクチャへの完全移行**
  - GameKitハンドラーがスタンドアロンC#スクリプトをテンプレートから生成
  - ランタイムライブラリ（`UnityAIForge.GameKit.Runtime`）を削除し、ゼロ依存を実現
  - ユーザープロジェクトはUnity-AI-Forgeパッケージをアンインストールしても動作可能

- **GameKit Logic PillarにAnalysisツールを統合**
  - クラス依存関係・シーン参照・シーン関係性グラフツールをLogic Pillarに移動
  - 3本柱アーキテクチャの一貫性を向上

- **ツール構成の最適化（48ツール）**
  - GameKit 14 (UI 5 + Presentation 5 + Logic 4)
  - Mid-Level 20 (バッチ操作 + UI Toolkit)
  - Low-Level 8 / Utility 5 / Batch 1
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
│       │   ├── MCPBridge/                      # Unity C# WebSocketブリッジ
│       │   │   └── Handlers/                   # 48ハンドラー（6カテゴリ）
│       │   ├── CodeGen/                        # コード生成インフラ
│       │   │   └── Templates/                  # 11テンプレート（*.cs.txt）
│       │   └── MCPServerManager/               # サーバー管理UI
│       ├── MCPServer/                          # ⭐ MCPサーバー（Python）
│       │   ├── src/                            # Python MCPサーバー
│       │   ├── setup/                          # インストールスクリプト
│       │   └── config/                         # 設定テンプレート
│       ├── Tests/                              # テストスイート
│       ├── Documentation/                      # ドキュメント
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

- **[Getting Started](../GETTING_STARTED.md)** - はじめてのセットアップガイド
- **[Quick Start](../Installation/QUICKSTART.md)** - 5分で始める
- **[Installation Guide](../Installation/INSTALL_GUIDE.md)** - 詳細なインストール手順
- **[MCP Server README](../MCPServer/README.md)** - 完全なMCPサーバードキュメント
- **[Examples](../Examples/README.md)** - 実践的なチュートリアルとウォークスルー

### 開発者向け

- **[CLAUDE.md](../CLAUDE.md)** - Claude Code統合の手順
- **[テストスイート](../Testing/README.md)** - すべてのツールの包括的なテストスイート
- **[ドキュメントインデックス](../INDEX.md)** - 全ドキュメント索引

## 🏗️ アーキテクチャ

```
AIクライアント (Claude/Cursor) <--(MCP)--> Python MCPサーバー <--(WebSocket)--> Unity C# Bridge
                                      (MCPServer/src/)         (Editor/MCPBridge/)
```

### コンポーネント

| コンポーネント | 場所 | 説明 |
|-----------|----------|-------------|
| **Unity C# Bridge** | `Editor/MCPBridge/` | Unity Editor内で実行されるWebSocketサーバー |
| **Python MCPサーバー** | `MCPServer/src/` | MCPプロトコル実装 |
| **GameKitコード生成** | `Editor/CodeGen/` | テンプレートベースのコード生成インフラ |
| **MCP Server Manager** | `Editor/MCPServerManager/` | サーバーライフサイクル管理UI |
| **セットアップスクリプト** | `MCPServer/setup/` | インストールと設定ヘルパー |
| **Tests** | `Tests/Editor/` | 包括的なテストスイート |

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

詳細は[テストスイートドキュメント](../Testing/README.md)を参照してください。

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

### 高レベルGameKitツール（15ツール）

GameKitはコード生成方式でスタンドアロンC#スクリプトを生成します。3本柱アーキテクチャ:

- **UIピラー** (5) - UICommand, UIBinding, UIList, UISlot, UISelection
- **Presentationピラー** (5) - AnimationSync, Effect, Feedback, VFX, Audio
- **Logicピラー** (5) - Integrity検証, ClassCatalog, ClassDependencyGraph, SceneReferenceGraph, SceneRelationshipGraph

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
│       │   ├── MCPBridge/           # Unity C# Bridge
│       │   │   ├── Base/            # ハンドラー基盤クラス
│       │   │   ├── Handlers/        # 48ハンドラー（6カテゴリ）
│       │   │   │   ├── LowLevel/    # 基本CRUD操作（7）
│       │   │   │   ├── MidLevel/    # バッチ・プリセット（20）
│       │   │   │   ├── HighLevel/   # 分析・整合性（5）
│       │   │   │   ├── GameKit/     # UI・Presentation（10）
│       │   │   │   ├── Utility/     # ユーティリティ（5）
│       │   │   │   └── Settings/    # プロジェクト設定（1）
│       │   │   └── Utilities/       # 共通ユーティリティ
│       │   ├── CodeGen/             # コード生成インフラ
│       │   │   └── Templates/       # 11テンプレート（*.cs.txt）
│       │   └── MCPServerManager/    # サーバー管理UI
│       ├── MCPServer/               # MCPサーバー（Python）
│       │   ├── src/                 # サーバーソース
│       │   │   ├── bridge/          # Unity Bridge通信
│       │   │   ├── tools/           # MCPツール定義・スキーマ
│       │   │   ├── resources/       # MCPリソース
│       │   │   └── main.py          # エントリーポイント
│       │   ├── setup/               # インストールスクリプト
│       │   ├── config/              # 設定テンプレート
│       │   └── pyproject.toml       # Pythonパッケージ設定
│       ├── Tests/
│       │   └── Editor/              # Unity Test Frameworkテスト
│       ├── Documentation/           # ドキュメント
│       └── package.json             # Unity Package定義
│
├── ProjectSettings/                 # Unityプロジェクト設定
├── Packages/                        # Unityパッケージ
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

- **クイックスタート**: [Installation/QUICKSTART.md](../Installation/QUICKSTART.md)
- **インストールガイド**: [Installation/INSTALL_GUIDE.md](../Installation/INSTALL_GUIDE.md)
- **Examples**: [Examples/README.md](../Examples/README.md)
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
