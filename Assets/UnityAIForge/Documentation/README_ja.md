# Unity-AI-Forge - AI駆動型Unity開発ツールキット

[![Python](https://img.shields.io/badge/Python-3.10%2B-blue)](https://www.python.org/)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black)](https://unity.com/)
[![MCP](https://img.shields.io/badge/MCP-0.9.0%2B-green)](https://modelcontextprotocol.io/)
[![Version](https://img.shields.io/badge/Version-2.3.0-brightgreen)](https://github.com/kuroyasouiti/Unity-AI-Forge/releases)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Unity-AI-Forgeは、AIとの協働でUnityゲームを鍛造する開発ツールキットです。Model Context Protocol統合とGameKitフレームワークにより、AIアシスタントがUnity Editorとリアルタイムで対話。Low-Level CRUD操作、Mid-Levelバッチツール、High-Level GameKitフレームワークの3層構造で、シンプルなアセット操作から複雑なゲームシステム構築まで対応します。

## 🆕 v2.3.0の新機能

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

- **💾 状態保存システム**: 完全なセーブ/ロード機能
  - JSON形式でリソース状態をエクスポート/インポート
  - ファイルまたはPlayerPrefsに保存
  - 自動タイムスタンプとメタデータ追跡
  - クラウドセーブ対応のシリアライズ可能な状態
  - Manager便利メソッドで簡単統合

- **🎮 GameKit UICommand拡張**: Manager制御対応
  - **新ターゲットタイプ**: Actor または Manager
  - **11コマンドタイプ**: Move/Jump/Action/Look/Custom + AddResource/SetResource/ConsumeResource/ChangeState/NextTurn/TriggerScene
  - UIボタンからゲーム経済、状態、ターンを直接制御可能
  - ストラテジーゲーム、ショップUI、リソース管理に最適

- **📊 GameKit Machinations強化**: ダイアグラム実行機能
  - ProcessDiagramFlows() - 自動リソースフローの実行
  - CheckDiagramTriggers() - 閾値イベントの監視
  - ExecuteConverter() - 特定のリソース変換実行
  - SetFlowEnabled() - 実行時の動的フロー制御
  - フローとトリガーの自動実行モード

- **🎯 ResourceManager簡略化**: コア機能に集中
  - 純粋なリソースストレージとイベント管理
  - 複雑なロジックは外部コントローラーまたはMachinationsへ
  - パフォーマンス向上（デフォルトでUpdate()オーバーヘッドなし）
  - より明確な責任分離

- **📚 包括的なドキュメント**: GameKit完全ガイド追加
  - SKILL_GAMEKIT.md - 完全なGameKitガイド
  - 3つの完全なゲーム例（RPG、タワーディフェンス、ターン制ストラテジー）
  - 全24ツールの詳細スキーマ説明
  - ベストプラクティスとトラブルシューティング

## v2.0.0の主要機能

- **🎯 ハブベースアーキテクチャ**: GameKitコンポーネント全体を賢いハブとして再設計
  - モジュラー、拡張可能、宣言的な設計パターンを全体に適用

- **🎮 GameKit Actor**: 強化されたコントローラー→ビヘイビアハブ
  - **8つの移動モード**: 2D（Linear, Physics, TileGrid）、3D（CharacterController, Physics, NavMesh）、GraphNode、SplineMovement
  - **4つの制御モード**: DirectController（Input System）、AIAutonomous、UICommand、ScriptTriggerOnly
  - すべての入力用UnityEvents（Move, Jump, Action, Look）

- **⚙️ GameKit Managerハブ**: モード別コンポーネントの動的追加
  - **TurnBased** → GameKitTurnManager（フェーズ、ターンカウンター、イベント）
  - **ResourcePool** → GameKitResourceManager（Machinationsフレームワーク着想のフロー、コンバーター、トリガー）
  - **EventHub** → GameKitEventManager（グローバルイベントシステム）
  - **StateManager** → GameKitStateManager（状態スタック、履歴）
  - **Realtime** → GameKitRealtimeManager（タイムスケール、一時停止、タイマー）

- **🎭 GameKit Interactionハブ**: マルチトリガー宣言的システム
  - 従来型: Collision, Trigger, Input, Proximity, Raycast
  - 専門型: **TilemapCell**、**GraphNode**、**SplineProgress**
  - アクション: TriggerActorAction、UpdateManagerResource、TriggerSceneFlow、テレポート
  - 条件: ActorId、ManagerResource、カスタム条件
  - クールダウン、リピート、UnityEvents

- **🎬 GameKit SceneFlow**: シーン中心トランジションシステム
  - 同じトリガー → シーンごとに異なる遷移先
  - 共有シーンの統合（個別グループ不要）
  - シーンごとのトランジション定義

- **📱 GameKit UI Command**: 構造化コマンドハブ
  - 型安全なコマンドシステム（Move, Jump, Action, Look, Custom）
  - パラメータ付きボタン登録
  - GameKitActorへの直接統合

- **🛤️ Spline Movement**: 2.5Dスプライン移動
  - Catmull-Rom補間
  - 閉ループ、横方向オフセット、自動回転
  - 手動/自動速度制御

- **Mid-Levelツール**: バッチ操作とプリセット
  - Transform/RectTransformバッチ操作（配置、整列、分配）
  - 物理バンドル（2D/3D プリセット: dynamic, kinematic, character, platformer, vehicle）
  - CharacterControllerバンドル（fps, tps, platformer, child, large, narrowプリセット）
  - カメラリグ（follow, orbit, split-screen, fixed, dolly）
  - UI基礎（Canvas, Panel, Button, Text, Image, InputField）
  - オーディオソースバンドル（music, sfx, ambient, voice, ui プリセット）
  - 入力プロファイル（New Input System統合）

- **コンパイル待機機能**: 自動コンパイル処理
  - 操作実行後、コンパイルが開始された場合に自動待機
  - ブリッジ再接続検出により早期解除
  - 透明な待機情報をレスポンスに含める

- **包括的テストスイート**: 100+ユニットテスト
  - Unity Test Framework統合
  - 全ツールカテゴリで97.7%の成功率
  - GitHub Actionsによる自動CI/CD
  - エディタメニュー統合（`Tools > Unity-AI-Forge > Run All Tests`）

- **ドキュメント**: 完全刷新
  - テストスイートドキュメントと結果
  - ツーリングロードマップ（日本語）
  - コンパイル待機機能ガイド
  - レガシークリーンアップサマリー
  - [完全なリリースノート](docs/Release_Notes_v1.8.0.md)
  - [変更履歴](CHANGELOG.md)

## 📦 パッケージ構造

Unity-AI-ForgeはMCPサーバーを統合したUnityパッケージです！

```
Unity-AI-Forge/
├── Assets/
│   └── UnityAIForge/                           # Unityパッケージ
│       ├── Editor/
│       │   └── MCPBridge/                      # Unity C# WebSocketブリッジ
│       ├── GameKit/                            # GameKitフレームワーク
│       ├── MCPServer/                          # ⭐ MCPサーバー（Python、ドキュメント、ツール）
│       │   ├── src/                            # Python MCPサーバー
│       │   ├── setup/                          # インストールスクリプト
│       │   ├── examples/                       # 実践的チュートリアル
│       │   ├── config/                         # 設定テンプレート
│       │   └── docs/                           # 追加ドキュメント
│       ├── Tests/                              # テストスイート
│       └── package.json                        # Unityパッケージ定義
```

## アーキテクチャ

Unity-AI-Forgeは**双方向WebSocketブリッジ**アーキテクチャを使用します：

```
AIクライアント (Claude Code/Cursor) <--(MCP)--> Pythonサーバー <--(WebSocket)--> Unity Editorブリッジ
                                                (MCPServer/src/)      (Editor/MCPBridge/)
```

### コンポーネント

| コンポーネント | 場所 | 説明 |
|-----------|----------|-------------|
| **Unity C#ブリッジ** | `Assets/UnityAIForge/Editor/MCPBridge/` | Unity Editor内で動作するWebSocketサーバー |
| **Python MCPサーバー** | `Assets/UnityAIForge/MCPServer/src/` | MCPプロトコル実装 |
| **GameKitフレームワーク** | `Assets/UnityAIForge/GameKit/Runtime/` | ハイレベルゲーム開発コンポーネント |
| **セットアップスクリプト** | `Assets/UnityAIForge/MCPServer/setup/` | インストールと設定ヘルパー |
| **サンプル** | `Assets/UnityAIForge/MCPServer/examples/` | 実践的チュートリアル |
| **テスト** | `Assets/UnityAIForge/Tests/Editor/` | 包括的テストスイート |

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

## 📝 スクリプトテンプレート生成

Unity-AI-Forgeは、MonoBehaviourとScriptableObjectの**スクリプトテンプレート生成**機能を提供します。

### 主な機能

- **MonoBehaviourテンプレート** - 標準ライフサイクルメソッド（Awake、Start、Update、OnDestroy）を含む
- **ScriptableObjectテンプレート** - CreateAssetMenu属性付きのデータコンテナクラス
- **名前空間サポート** - オプションでC#名前空間を追加
- **迅速な開発** - 適切な構造でスクリプトを素早く作成

### 例: MonoBehaviourスクリプトの生成

```python
unity_script_template_generate({
    "templateType": "MonoBehaviour",
    "className": "PlayerController",
    "scriptPath": "Assets/Scripts/PlayerController.cs",
    "namespace": "MyGame.Player"
})
```

### 例: ScriptableObjectスクリプトの生成

```python
unity_script_template_generate({
    "templateType": "ScriptableObject",
    "className": "GameConfig",
    "scriptPath": "Assets/ScriptableObjects/GameConfig.cs"
})
```

テンプレート生成後、`unity_asset_crud`の`update`操作でスクリプト内容を変更できます。

```python
unity_asset_crud({
    "operation": "update",
    "assetPath": "Assets/Scripts/PlayerController.cs",
    "content": "using UnityEngine;\n\nnamespace MyGame.Player\n{\n    public class PlayerController : MonoBehaviour\n    {\n        public float speed = 5f;\n        \n        void Update()\n        {\n            // 移動コード\n        }\n    }\n}"
})
```

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

詳細は[テストスイートドキュメント](Assets/Unity-AI-Forge/Tests/Editor/README.md)を参照してください。

## ✨ 機能

### Low-Levelツール（CRUD操作）

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity_ping` | ブリッジ接続の確認 | Unityバージョン、プロジェクト名、タイムスタンプを返す |
| `unity_scene_crud` | シーン管理 | create, load, save, delete, duplicate, inspect シーン、ビルド設定管理 |
| `unity_gameobject_crud` | GameObjectヒエラルキー管理 | create, delete, move, rename, duplicate, inspect GameObject、バッチ操作 |
| `unity_component_crud` | コンポーネント操作 | add, remove, update, inspect GameObjectのコンポーネント、バッチ操作 |
| `unity_asset_crud` | アセットファイル操作 | create, update, rename, duplicate, delete, inspect Assets/ファイル、インポーター設定 |
| `unity_scriptableObject_crud` | ScriptableObject管理 | create, inspect, update, delete, duplicate, list, findByType ScriptableObject |
| `unity_prefab_crud` | Prefab管理 | create, update, inspect, instantiate, unpack, applyOverrides, revertOverrides |
| `unity_vector_sprite_convert` | Vector/Sprite変換 | primitiveToSprite, svgToSprite, textureToSprite, createColorSprite |
| `unity_projectSettings_crud` | プロジェクト設定管理 | read, write, list 設定（Player, Quality, Time, Physics, Audio, Editor）|

### Mid-Levelツール（バッチ操作とプリセット）

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity_transform_batch` | Transformバッチ操作 | arrangeCircle, arrangeLine, renameSequential, renameFromList, createMenuList |
| `unity_rectTransform_batch` | RectTransformバッチ操作 | setAnchors, setPivot, setSizeDelta, alignToParent, distribute, matchSize |
| `unity_physics_bundle` | 物理バンドル | applyPreset2D/3D, updateRigidbody, updateCollider, inspect |
| `unity_camera_rig` | カメラリグ | createRig, updateRig, inspect（follow, orbit, splitScreen, fixed, dolly） |
| `unity_ui_foundation` | UI基礎 | createCanvas, createPanel, createButton, createText, createImage, createInputField |
| `unity_audio_source_bundle` | オーディオソースバンドル | createAudioSource, updateAudioSource, inspect（music, sfx, ambient, voice, ui） |
| `unity_input_profile` | 入力プロファイル | createPlayerInput, createInputActions, inspect（New Input System） |

**Transform Batch (`unity_transform_batch`)**
- 円形・直線配置でオブジェクトを整列
- 連番リネーム、リストベースリネーム
- メニュー階層の自動生成

**RectTransform Batch (`unity_rectTransform_batch`)**
- アンカー、ピボット、サイズ、位置の一括設定
- 親への整列プリセット
- 水平/垂直分配
- サイズマッチング

**Physics Bundle (`unity_physics_bundle`)**
- 2D/3D Rigidbody + Colliderプリセット適用
- プリセット: dynamic, kinematic, static, character, platformer, topDown, vehicle, projectile
- 個別物理プロパティの更新

**Camera Rig (`unity_camera_rig`)**
- follow, orbit, split-screen, fixed, dolly カメラリグ作成
- ターゲット追跡とスムーズ移動
- ビューポート設定

**UI Foundation (`unity_ui_foundation`)**
- Canvas, Panel, Button, Text, Image, InputField作成
- アンカープリセット
- TextMeshPro対応
- 自動レイアウト

**Audio Source Bundle (`unity_audio_source_bundle`)**
- music, sfx, ambient, voice, ui プリセット
- 2D/3D空間オーディオ設定
- ミキサーグループ統合

**Input Profile (`unity_input_profile`)**
- New Input System統合
- アクションマップ設定
- 通知動作: sendMessages, broadcastMessages, invokeUnityEvents, invokeCSharpEvents

### High-Level GameKitツール（ゲームシステム構築）

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity_gamekit_actor` | ゲームアクター | create, update, inspect, delete（コントローラー→ビヘイビアハブ、UnityEvents） |
| `unity_gamekit_manager` | ゲームマネージャー | create, update, inspect, delete（ターン制御、リソース管理、Machinations） |
| `unity_gamekit_interaction` | インタラクション | create, update, inspect, delete（トリガー、アクション、条件） |
| `unity_gamekit_ui_command` | UIコマンド | createCommandPanel, addCommand, inspect, delete |
| `unity_gamekit_sceneflow` | シーンフロー | create, update, inspect, delete, transition（シーン遷移、共有グループ） |

**GameKit Actor (`unity_gamekit_actor`)**
- コントローラー→ビヘイビア間のハブとして機能
- UnityEventsによる入力中継: OnMoveInput, OnJumpInput, OnActionInput, OnLookInput
- 振る舞いプロファイル: 
  - 2D: リニア、物理、タイルグリッド（`TileGridMovement`コンポーネント自動追加）
  - 3D: キャラクターコントローラー、物理、NavMesh
- 制御モード: 直接コントローラー、AI、UIコマンド、スクリプトトリガーのみ

**GameKit Manager (`unity_gamekit_manager`)**
- マネージャータイプ: ターン制、リアルタイム、リソースプール、イベントハブ、ステートマネージャー
- ターンフェーズ管理とサイクル
- リソースプール（追加、消費、可用性チェック）
- Machinationsフレームワーク対応（ノード、接続、デザインパターン）
- 永続化（DontDestroyOnLoad）

**GameKit Interaction (`unity_gamekit_interaction`)**
- トリガータイプ: collision, trigger, raycast, proximity, input
- アクション: Prefabスポーン、オブジェクト破壊、サウンド再生、メッセージ送信、シーン変更
- 条件: tag, layer, distance, custom
- 自動コライダー/Rigidbody設定

**GameKit UI Command (`unity_gamekit_ui_command`)**
- コマンドパネル作成（horizontal, vertical, gridレイアウト）
- ボタン生成（ラベルとアイコン）
- アクターへのコマンド送信
- 自動Canvas/Panel設定

**GameKit SceneFlow (`unity_gamekit_sceneflow`)**
- シーンステートマシンと遷移
- 加算シーンロード対応
- 永続マネージャーシーン（アンロードされない）
- 共有シーングループ（UI、Audioなど）
- シーン固有のオプトイン/オプトアウト
- シーン間参照解決（GUID/AddressableKey + ランタイムサービス）

---

## 🧪 テスト

Unity Test Frameworkによる包括的なテストスイート：

- **43テスト** - Low/Mid/High-Level全カテゴリ
- **97.7%成功率** - 高品質保証
- **エディタ統合** - `Tools > Unity-AI-Forge`メニュー
- **CI/CD対応** - GitHub Actions自動実行

テスト実行：
```bash
# Unity Editor内
Tools > Unity-AI-Forge > Run All Tests

# コマンドライン
.\run-tests.ps1              # Windows
./run-tests.sh               # macOS/Linux
```

詳細: [テストスイートREADME](Assets/Unity-AI-Forge/Tests/Editor/README.md)

---

## 利用可能なツール

### Low-Levelツール（基本CRUD操作）

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity_ping` | ブリッジ接続の確認 | Unityバージョン、プロジェクト名、タイムスタンプを返す |
| `unity_scene_crud` | シーン管理 | create, load, save, delete, duplicate, inspect シーン、ビルド設定 |
| `unity_gameobject_crud` | GameObjectヒエラルキー管理 | create, delete, move, rename, duplicate, inspect GameObject、バッチ |
| `unity_component_crud` | コンポーネント操作 | add, remove, update, inspect GameObjectのコンポーネント、バッチ |
| `unity_asset_crud` | アセットファイル操作 | create, update, rename, duplicate, delete, inspect Assets/ファイル |
| `unity_scriptableObject_crud` | ScriptableObject管理 | create, inspect, update, delete, duplicate, list, findByType |
| `unity_prefab_crud` | Prefab管理 | create, update, inspect, instantiate, unpack, applyOverrides, revertOverrides |
| `unity_vector_sprite_convert` | Vector/Sprite変換 | primitiveToSprite, svgToSprite, textureToSprite, createColorSprite |
| `unity_projectSettings_crud` | プロジェクト設定 | read, write, list（Player, Quality, Time, Physics, Audio, Editor）|

**Prefab Management (`unity_prefab_crud`)**
- GameObjectからPrefab作成
- Prefabの更新と検査
- シーンへのインスタンス化
- アンパック（完全/最外層）
- オーバーライドの適用/復元

**Vector Sprite Conversion (`unity_vector_sprite_convert`)**
- プリミティブ形状スプライト生成（square, circle, triangle, polygon）
- SVGインポート
- テクスチャからスプライト変換
- 単色スプライト作成

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

### 例1: ゲームアクターの作成（GameKit）

```python
# プレイヤーアクターを作成（コントローラー→ビヘイビアハブ）
unity_gamekit_actor({
    "operation": "create",
    "actorId": "player_001",
    "behaviorProfile": "2dPhysics",
    "controlMode": "directController",
    "spritePath": "Assets/Sprites/Player.png"
})
# → OnMoveInput, OnJumpInput, OnActionInput, OnLookInputイベントが自動設定される
# → コントローラースクリプトはSendMoveInput()等でアクターに入力を送る
# → ビヘイビアスクリプトはOnMoveInput.AddListener()で入力を受け取る

# 敵アクターを作成（AIコントロール）
unity_gamekit_actor({
    "operation": "create",
    "actorId": "enemy_001",
    "behaviorProfile": "2dPhysics",
    "controlMode": "aiAutonomous"
})
```

### 例2: ターン制ゲームマネージャー（GameKit）

```python
# ターン制マネージャーを作成
unity_gamekit_manager({
    "operation": "create",
    "managerId": "turn_manager",
    "managerType": "turnBased",
    "turnPhases": ["PlayerTurn", "EnemyTurn", "EndTurn"],
    "resourcePool": {
        "actionPoints": 3,
        "mana": 100
    },
    "dontDestroyOnLoad": True
})

# リソースを消費
unity_gamekit_manager({
    "operation": "update",
    "managerId": "turn_manager",
    "consumeResource": {
        "resourceName": "actionPoints",
        "amount": 1
    }
})

# 次のフェーズへ
unity_gamekit_manager({
    "operation": "update",
    "managerId": "turn_manager",
    "advancePhase": True
})
```

### 例3: シーンフロー管理（GameKit）

```python
# シーンフローを作成
unity_gamekit_sceneflow({
    "operation": "create",
    "flowId": "main_flow",
    "scenes": [
        {
            "name": "Title",
            "path": "Assets/Scenes/Title.unity",
            "loadMode": "single"
        },
        {
            "name": "Level1",
            "path": "Assets/Scenes/Level1.unity",
            "loadMode": "additive",
            "sharedGroups": ["UI", "Audio"]
        },
        {
            "name": "Level2",
            "path": "Assets/Scenes/Level2.unity",
            "loadMode": "additive",
            "sharedGroups": ["UI", "Audio"]
        }
    ],
    "sharedSceneGroups": {
        "UI": ["Assets/Scenes/GameUI.unity"],
        "Audio": ["Assets/Scenes/AudioManager.unity"]
    },
    "transitions": [
        {"from": "Title", "to": "Level1", "trigger": "StartGame"},
        {"from": "Level1", "to": "Level2", "trigger": "NextLevel"},
        {"from": "Level2", "to": "Title", "trigger": "ReturnToTitle"}
    ]
})

# シーン遷移をトリガー
unity_gamekit_sceneflow({
    "operation": "transition",
    "flowId": "main_flow",
    "trigger": "StartGame"
})
```

### 例4: Mid-Levelツール - カメラリグとUI

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

# 物理プリセットを適用
unity_physics_bundle({
    "operation": "applyPreset2D",
    "gameObjectPath": "Player",
    "preset": "platformer"
})
```

### 例5: Vector Sprite生成とPrefab作成

```python
# プリミティブスプライトを生成
unity_vector_sprite_convert({
    "operation": "primitiveToSprite",
    "primitiveType": "circle",
    "width": 256,
    "height": 256,
    "color": {"r": 1.0, "g": 0.0, "b": 0.0, "a": 1.0},
    "outputPath": "Assets/Sprites/RedCircle.png"
})

# GameObjectを作成してスプライトを適用
unity_gameobject_crud({
    "operation": "create",
    "gameObjectPath": "Enemy",
    "components": ["SpriteRenderer"]
})

unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Enemy",
    "componentType": "UnityEngine.SpriteRenderer",
    "properties": {
        "sprite": "Assets/Sprites/RedCircle.png"
    }
})

# Prefabを作成
unity_prefab_crud({
    "operation": "create",
    "gameObjectPath": "Enemy",
    "prefabPath": "Assets/Prefabs/Enemy.prefab",
    "includeChildren": True
})

# Prefabをインスタンス化
unity_prefab_crud({
    "operation": "instantiate",
    "prefabPath": "Assets/Prefabs/Enemy.prefab",
    "targetPath": "Enemies/Enemy_001",
    "position": {"x": 5, "y": 0, "z": 0}
})
```

---

## 開発

### ファイル構造

```
Unity-AI-Forge/
├── Assets/
│   └── UnityAIForge/                          # Unityパッケージ
│       ├── Editor/
│       │   └── MCPBridge/                     # Unity C#ブリッジ
│       │       ├── McpBridgeService.cs            # WebSocketサーバー
│       │       ├── McpCommandProcessor.cs         # ツール実行
│       │       ├── McpContextCollector.cs         # コンテキスト収集
│       │       ├── McpBridgeWindow.cs             # Unity Editor UI
│       │       └── Handlers/                      # ツール実装
│       ├── GameKit/
│       │   └── Runtime/                       # GameKitフレームワーク
│       │       ├── Actor/
│       │       ├── Manager/
│       │       ├── Interaction/
│       │       └── SceneFlow/
│       ├── MCPServer/                         # MCPサーバー（Python）
│       │   ├── src/                               # サーバー実装
│       │   │   ├── bridge/                        # Unityブリッジ通信
│       │   │   ├── tools/                         # ツール定義
│       │   │   ├── resources/                     # リソース
│       │   │   └── main.py                        # エントリポイント
│       │   ├── setup/                             # インストールスクリプト
│       │   ├── examples/                          # チュートリアル
│       │   ├── config/                            # 設定テンプレート
│       │   ├── skill.yml                          # MCPサーバーマニフェスト
│       │   └── pyproject.toml                     # Pythonパッケージ構成
│       ├── Tests/
│       │   └── Editor/                        # Unity Test Frameworkテスト
│       └── package.json                       # Unityパッケージ定義
│
├── ProjectSettings/                           # Unityプロジェクト設定
├── Packages/                                  # Unityパッケージ
├── docs/                                      # プロジェクトドキュメント
└── README.md                                  # このファイル
```

### 新しいツールの追加

詳細なガイドは以下を参照:
- [CLAUDE.md](CLAUDE.md) - 完全な開発ドキュメント
- [Assets/UnityAIForge/MCPServer/](Assets/UnityAIForge/MCPServer/) - MCPサーバーソースコード
- [docs/](docs/) - プロジェクトドキュメント

---

## 機能ハイライト

### 3層アーキテクチャ
- ✅ **Low-Level CRUD** - Scene, GameObject, Component, Asset, ScriptableObject, Prefab, Sprite, Settings
- ✅ **Mid-Level Batch** - Transform, RectTransform, Physics, Camera, UI, Audio, Input
- ✅ **High-Level GameKit** - Actor, Manager, Interaction, UICommand, SceneFlow

### コア機能
- ✅ WebSocket経由のリアルタイムUnity Editor統合
- ✅ 21ツール、100+操作
- ✅ 自動コンパイル待機（操作後待機 + ブリッジ再接続検出）
- ✅ 包括的テストスイート（100+テスト、97.7%成功率）
- ✅ CI/CD統合（GitHub Actions）

### GameKitフレームワーク
- ✅ **Actor System** - コントローラー→ビヘイビア間のハブ、UnityEventsによる入力中継
- ✅ **Manager System** - ターン制御、リソース管理、Machinations対応
- ✅ **Interaction System** - トリガー、アクション、条件の宣言的定義
- ✅ **UI Command System** - UIボタンからアクターへのコマンド送信
- ✅ **SceneFlow System** - シーン遷移ステートマシン、共有シーングループ

### Mid-Levelツール
- ✅ **バッチ操作** - 円形/直線配置、連番リネーム、メニュー自動生成
- ✅ **物理プリセット** - 8種類の2D/3Dプリセット（character, platformer, vehicle等）
- ✅ **カメラリグ** - 5種類のカメラ設定（follow, orbit, split-screen等）
- ✅ **UI基礎** - Canvas、ボタン、テキストなどの自動生成
- ✅ **オーディオプリセット** - music, sfx, ambient, voice, ui
- ✅ **入力システム** - New Input System統合

### 開発者ツール
- ✅ Prefab完全管理（作成、更新、インスタンス化、オーバーライド）
- ✅ Vector/Spriteユーティリティ（プリミティブ、SVG、テクスチャ変換）
- ✅ コンパイル待機システム（操作後自動待機、ブリッジ再接続検出）
- ✅ 包括的テストスイート（Unity Test Framework、エディタメニュー統合）
- ✅ 構造化されたエラー処理とレポート

---

## ツールリファレンスサマリー

| カテゴリ | ツール数 | 主なツール |
|---------|---------|----------|
| **Low-Level CRUD** | 8ツール | Scene, GameObject, Component, Asset, ScriptableObject, Prefab, VectorSprite, ProjectSettings |
| **Mid-Level Batch** | 7ツール | Transform, RectTransform, Physics, Camera, UI, Audio, Input |
| **High-Level GameKit** | 5ツール | Actor, Manager, Interaction, UICommand, SceneFlow |
| **Utility** | 1ツール | Ping |
| **合計** | **21ツール** | **100+操作** |

---

## 必要要件

- Unity 2021.3以降（2022.3以降推奨）
- Python 3.10以降
- uv（推奨）またはpip

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
3. Unity Consoleで詳細なエラーメッセージを確認

### コンパイル後

ブリッジはUnityがスクリプトを再コンパイルした後、自動的に接続状態を保存して再接続します。手動での介入は不要です。

### スクリプト作成のベストプラクティス

- 新しいスクリプトには`unity_script_template_generate()`でテンプレートを生成
- 生成後は`unity_asset_crud`で内容を変更
- コンパイルエラーはUnity Consoleで確認

---

## パフォーマンスのヒント

1. **バッチ操作を使用**: 複数の操作を組み合わせて性能向上
2. **コンテキスト更新を制限**: 大きなシーンではコンテキスト更新間隔を増やす
3. **スクリプトテンプレート**: 新規スクリプトには`unity_script_template_generate`でテンプレートを生成
4. **アセット参照をキャッシュ**: アセットを一度読み込み、複数の操作で再利用
5. **エラー時停止**: 依存する操作には`stopOnError: true`を設定

---

## ドキュメント

- **メインドキュメント**: [CLAUDE.md](CLAUDE.md)
- **クイックスタート**: [Assets/UnityAIForge/MCPServer/QUICKSTART.md](Assets/UnityAIForge/MCPServer/QUICKSTART.md)
- **インストールガイド**: [Assets/UnityAIForge/MCPServer/INSTALL_GUIDE.md](Assets/UnityAIForge/MCPServer/INSTALL_GUIDE.md)
- **サンプル**: [Assets/UnityAIForge/MCPServer/examples/](Assets/UnityAIForge/MCPServer/examples/)
- **テストスイート**: [Assets/UnityAIForge/Tests/Editor/README.md](Assets/UnityAIForge/Tests/Editor/README.md)
- **プロジェクトドキュメント**: [docs/](docs/)

---

## ライセンス

MIT License - [MIT License](https://opensource.org/licenses/MIT)

## 貢献


貢献を歓迎します！開発ガイドは[CLAUDE.md](CLAUDE.md)をお読みください。

## サポート

問題、質問、機能リクエストについて:
1. Unity Consoleでエラーメッセージを確認
2. [CLAUDE.md](CLAUDE.md)のドキュメントを確認
3. [Assets/UnityAIForge/MCPServer/examples/](Assets/UnityAIForge/MCPServer/examples/)のサンプルを確認
4. [GitHub Issues](https://github.com/kuroyasouiti/Unity-AI-Forge/issues)にissueを作成

---

**Unity-AI-Forge** - Model Context Protocolによる包括的なUnity Editor自動化
