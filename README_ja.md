# SkillForUnity - Unity向けModel Context Protocolサーバー

SkillForUnityは、AIアシスタントがUnity Editorとリアルタイムで対話できる包括的なModel Context Protocol (MCP) サーバーです。シーン管理、GameObject操作、コンポーネント編集、アセット操作、UIレイアウト、デザインパターン生成、階層メニュー作成、Prefab、スクリプト管理など、広範なツールを提供します。

## アーキテクチャ

SkillForUnityは**双方向WebSocketブリッジ**アーキテクチャを使用します：

```
AIクライアント (Claude Code/Cursor) <--(MCP)--> Pythonサーバー <--(WebSocket)--> Unity Editorブリッジ
```

### コンポーネント

1. **Unity C#ブリッジ** (`Assets/SkillForUnity/Editor/MCPBridge/`) - Unity Editor内で動作するWebSocketサーバー（Claude SkillのZIPを同梱）
2. **Claude Skill (Python MCPサーバー)** (`.claude/skills/SkillForUnity/src/`) - ブリッジに接続するMCPプロトコル実装

## クイックスタート

### 1. Unityパッケージのインストール

**方法A: Unity Package Manager経由（推奨）**

1. Unity Editorを開く
2. **Window > Package Manager**を開く
3. **+ (プラス)** ボタン → **Add package from git URL...**をクリック
4. 次のURLを入力: `https://github.com/kuroyasouiti/SkillForUnity.git?path=/Assets/SkillForUnity`
5. **Add**をクリック

**方法B: 手動インストール**

1. このリポジトリをダウンロード
2. `Assets/SkillForUnity`をあなたのUnityプロジェクトの`Assets/`フォルダにコピー

### 2. Claude Skillのインストール

Unityパッケージには `Assets/SkillForUnity/SkillForUnity-MCPServer.zip` が同梱されています。

**方法A: 同梱ZIPをClaude Desktopのskillsフォルダへコピー**

```bash
# Claude SkillのZIPをコピー
cp Assets/SkillForUnity/SkillForUnity-MCPServer.zip ~/.claude/skills/

# 展開して ~/SkillForUnity を作成
cd ~/.claude/skills
unzip -o SkillForUnity-MCPServer.zip
```

**方法B: MCPウィンドウから登録**

1. Claude Desktopを開く
2. MCP設定ウィンドウを開く
3. スキル設定で新しいMCPサーバーを追加

**方法C: 手動設定**

Claude Desktopの設定ファイル（`~/.claude/claude_desktop_config.json`）に追加：
```json
{
  "mcpServers": {
    "skill-for-unity": {
      "command": "uv",
      "args": ["run", "--directory", "/path/to/SkillForUnity", "src/main.py"],
      "env": {
        "MCP_SERVER_TRANSPORT": "stdio",
        "MCP_LOG_LEVEL": "info"
      }
    }
  }
}
```

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

SkillForUnityは、MonoBehaviourとScriptableObjectの**スクリプトテンプレート生成**機能を提供します。

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
- **create**: テキストベースアセットの作成（JSON、XML、設定ファイルなど）
- **update**: 既存アセットコンテンツの更新
- **rename**: アセットのリネーム
- **duplicate**: アセットの複製
- **delete**: アセットの削除
- **inspect**: アセットメタデータとコンテンツの検査
- **findMultiple/deleteMultiple/inspectMultiple**: ワイルドカードパターンで複数アセットを操作
- AssetDatabaseの自動更新
> **重要:** C#スクリプト（.csファイル）もサポートしています。新しいMonoBehaviour/ScriptableObjectスクリプトには、`unity_script_template_generate`でテンプレートを生成してから`unity_asset_crud`で編集することをお勧めします。

---
### 高レベルツール（迅速な開発に推奨）

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity.scene.quickSetup` | **即座にシーン設定** | 3D/2D/UI/VR/空のシーンを適切なデフォルトで作成 |
| `unity.gameobject.createFromTemplate` | **GameObjectテンプレート** | テンプレートからプリミティブ、ライト、カメラ、プレイヤー、敵を作成 |
| `unity.ugui.createFromTemplate` | **UI要素テンプレート** | Button、Text、Image、Panel、ScrollView、InputField、Slider、Toggle、Dropdownを作成 |
| `unity.ugui.layoutManage` | **レイアウトコンポーネント管理** | レイアウトグループの追加/更新/削除（Horizontal/Vertical/Grid/ContentSizeFitter等） |
| `unity.hierarchy.builder` | **宣言的階層作成** | 1つのコマンドで複雑なネストされたGameObject構造を構築 |
| `unity.menu.hierarchyCreate` | **階層メニュー作成** | State Patternを使用したネストされたサブメニューシステムを作成 |

**シーンクイックセットアップ (`unity.scene.quickSetup`)** - NEW!
- **3D**: Main CameraとDirectional Lightを作成（既存オブジェクトをチェックして重複を回避）
- **2D**: 正射投影を使用した2D Cameraを作成
- **UI**: CanvasとEventSystemを作成（既存オブジェクトをチェック）
- **VR**: VR Camera設定を作成
- **Empty**: デフォルトオブジェクトなしの空のシーン

**特徴:**
- **重複防止**: 既存のカメラ、ライト、キャンバス、イベントシステムを自動検出
- 1コマンドでシーンの初期化
- 各シーンタイプに適切なデフォルト
- 迅速なプロトタイピングに最適

**例 - UIシーンのセットアップ:**
```json
{
  "tool": "sceneQuickSetup",
  "payload": {
    "setupType": "UI"
  }
}
```

**GameObjectテンプレート (`unity.gameobject.createFromTemplate`)** - NEW!
- **プリミティブ**: Cube、Sphere、Plane、Cylinder、Capsule、Quad（MeshRenderer + Collider付き）
- **ライト**: Directional、Point、Spotライトと適切なデフォルト
- **特殊**: Camera、Empty、Player（CharacterController付き）、Enemy、Particle System、Audio Source

**特徴:**
- 各テンプレート用の事前設定済みコンポーネント
- Transform プロパティ（position、rotation、scale）
- Undoサポート
- 親階層サポート

**例 - プレイヤーの作成:**
```json
{
  "tool": "gameObjectCreateFromTemplate",
  "payload": {
    "template": "Player",
    "position": {"x": 0, "y": 1, "z": 0}
  }
}
```

**UI要素テンプレート (`unity.ugui.createFromTemplate`)** - NEW!
- **要素**: Button、Text、Image、RawImage、Panel、ScrollView、InputField、Slider、Toggle、Dropdown
- 各テンプレートには必要なすべてのコンポーネントが含まれています（Image、Button、Text等）
- カスタマイズ可能なプロパティ（text、fontSize、width、height、interactable、anchorPreset）
- 指定がない場合、自動的にCanvasの親を検索

**特徴:**
- 1コマンドで完全なUI要素
- 各要素タイプに適切なデフォルト
- RectTransformアンカープリセット
- 親階層サポート

**例 - ボタンの作成:**
```json
{
  "tool": "uguiCreateFromTemplate",
  "payload": {
    "template": "Button",
    "text": "ゲーム開始",
    "width": 200,
    "height": 50,
    "anchorPreset": "center"
  }
}
```

**レイアウト管理 (`unity.ugui.layoutManage`)** - NEW!
- **レイアウトグループ**: HorizontalLayoutGroup、VerticalLayoutGroup、GridLayoutGroup
- **フィッター**: ContentSizeFitter、LayoutElement、AspectRatioFitter
- 操作: レイアウトコンポーネントの追加、更新、削除、検査
- スペース、パディング、配置、子コントロールの完全な制御

**例 - 縦型レイアウトの追加:**
```json
{
  "tool": "uguiLayoutManage",
  "payload": {
    "operation": "add",
    "gameObjectPath": "Canvas/Panel",
    "layoutType": "VerticalLayoutGroup",
    "spacing": 10,
    "padding": {"left": 20, "right": 20, "top": 20, "bottom": 20}
  }
}
```

**階層ビルダー (`unity.hierarchy.builder`)** - NEW!
- シンプルなネスト辞書から階層構造を構築
- 空のGameObjectをツリー構造で組織化
- シーン組織化、フォルダ構造、プレースホルダーに最適
- 階層作成後に必要に応じてコンポーネントを追加

**例 - ゲームマネージャー階層の構築:**
```python
unity_hierarchy_builder({
    "hierarchy": {
        "GameManager": {
            "UI": {
                "ScoreText": {},
                "HealthBar": {}
            },
            "Audio": {
                "MusicSource": {},
                "SFXSource": {}
            }
        }
    }
})
# 空のGameObjectを作成後、必要に応じてコンポーネントを追加
```

**シーンインスペクター (`unity_scene_crud` with `operation="inspect"`)** - UPDATED!
- 包括的なシーンの概要を取得（階層、GameObjects、コンポーネント）
- ワイルドカードパターンでフィルタリング（*と?）
- 階層は1層のみ返す（パフォーマンス最適化）
- オブジェクト数を返す（カメラ、ライト、キャンバス）

**例 - シーンの検査:**
```python
unity_scene_crud({
    "operation": "inspect",
    "includeHierarchy": True,
    "includeComponents": True,
    "filter": "Player*"
})
```

**階層メニュー作成 (`unity_menu_hierarchyCreate`)** - NEW!
- ネストされたサブメニューを持つ完全な階層メニューシステムを作成
- State Design Patternを使用したメニューナビゲーション制御
- キーボード、ゲームパッド、または両方の入力サポート
- CanvasGroupを使用した自動的な表示/非表示管理
- VerticalLayoutGroupによる自動的なレイアウト調整
- オプションの「戻る」ボタンによる簡単なナビゲーション

**主な機能:**
1. **宣言的なメニュー定義** - シンプルなJSON構造からメニュー階層全体を作成
2. **State Pattern統合** - メニュー状態を管理するC#スクリプトを自動生成
3. **柔軟な入力処理** - キーボード（矢印キー、Enter、Escape）またはゲームパッド（D-Pad、A、B）
4. **自動レイアウト** - VerticalLayoutGroupとContentSizeFitterによる一貫したレイアウト
5. **ビジュアル階層** - 明確な親子関係を持つ整理されたメニュー構造
6. **カスタマイズ可能** - ボタンサイズ、スペーシング、ナビゲーションモードの設定可能

**例 - シンプルなメインメニュー:**
```python
unity_menu_hierarchyCreate({
    "menuName": "MainMenu",
    "menuStructure": {
        "Start Game": "StartGame",
        "Settings": {
            "Video": {
                "Resolution": "SetResolution",
                "Quality": "SetQuality",
                "Fullscreen": "ToggleFullscreen"
            },
            "Audio": {
                "Master Volume": "SetMasterVolume",
                "Music Volume": "SetMusicVolume",
                "SFX Volume": "SetSFXVolume"
            },
            "Controls": "ConfigureControls"
        },
        "Quit": "QuitGame"
    },
    "generateStateMachine": True,
    "stateMachineScriptPath": "Assets/Scripts/MenuStateMachine.cs",
    "navigationMode": "both",
    "buttonWidth": 200,
    "buttonHeight": 50,
    "spacing": 10,
    "enableBackNavigation": True
})
```

**生成されるUI構造:**
```
Canvas/
└── MainMenu (親パネル - CanvasGroup付き)
    ├── MainMenuPanel (ボタンコンテナ - VerticalLayoutGroup付き)
    │   ├── StartGameButton
    │   ├── SettingsButton
    │   └── QuitButton
    ├── SettingsPanel (サブメニュー - CanvasGroup付き)
    │   ├── VideoButton
    │   ├── AudioButton
    │   ├── ControlsButton
    │   └── BackButton
    ├── VideoPanel (サブメニュー)
    │   ├── ResolutionButton
    │   ├── QualityButton
    │   ├── FullscreenButton
    │   └── BackButton
    └── AudioPanel (サブメニュー)
        ├── MasterVolumeButton
        ├── MusicVolumeButton
        ├── SFXVolumeButton
        └── BackButton
```

**生成されるMenuStateMachineスクリプトの機能:**
- **IMenuState** - Enter()、Update()、Exit()メソッドを持つState Pattern インターフェース
- **MenuState** - 各メニューパネル用の具体的なState実装
- **Input処理** - 設定されたナビゲーションモードに基づく入力ハンドリング
- **State遷移** - メニュー間のスムーズな切り替え
- **CanvasGroup管理** - アクティブなメニューパネルの自動表示/非表示

**使用方法:**
```csharp
// MenuStateMachineコンポーネントを使用
var menuStateMachine = FindFirstObjectByType<MenuStateMachine>();

// メインメニューに切り替え
menuStateMachine.ChangeState("MainMenuPanel");

// サブメニューに切り替え
menuStateMachine.ChangeState("SettingsPanel");
```

**ベストプラクティス:**
1. **Canvas配下に作成** - UIシーンのセットアップ後に使用（`unity_scene_quickSetup` の `setupType: "UI"`）
2. **明確な命名** - メニュー項目には説明的な名前を使用
3. **バックナビゲーションを有効化** - 深い階層には `enableBackNavigation: true` を設定
4. **入力モードを選択** - ターゲットプラットフォームに応じて `navigationMode` を設定
5. **生成スクリプトをカスタマイズ** - 生成されたスクリプトを編集してボタンのコールバックを実装

**一般的な使用例:**
1. **ゲームメインメニュー** - Start Game、Settings、Credits、Quit
2. **設定メニュー** - Video、Audio、Controls、Gameplay のネストされたオプション
3. **一時停止メニュー** - Resume、Options、Return to Main Menu
4. **インベントリシステム** - カテゴリー化されたアイテムと詳細パネル
5. **レベル選択** - ワールドとレベルの階層選択

**戻り値:**
```python
{
    "success": True,
    "menuName": "MainMenu",
    "createdPanels": ["MainMenuPanel", "SettingsPanel", "VideoPanel", "AudioPanel", "ControlsPanel"],
    "scriptPath": "Assets/Scripts/MenuStateMachine.cs",
    "navigationMode": "both"
}
```

**重要な注意事項:**
- UIシーンが既にセットアップされている必要があります（Canvas + EventSystemが存在）
- メニュー構造は3つの形式をサポート：
  - 文字列値 - リーフメニュー項目（`"Start Game": "StartGame"`）
  - 辞書 - サブメニュー（`"Settings": {...}`）
  - 配列 - 同じサブメニューに複数のアクション（`"Options": ["option1", "option2"]`）
- 生成されたスクリプトには、ボタンクリック時に呼び出されるコールバックメソッドのスタブが含まれています
- すべてのパネルにはCanvasGroupコンポーネントが追加され、アクティブでないメニューは自動的に非表示になります
- ボタンにはデフォルトのUIコンポーネント（Image、Text）が含まれ、すぐに使用可能です
- `generateStateMachine: false` を設定すると、スクリプト生成なしでUI階層のみを作成できます

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

### ユーティリティ

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `unity.project.compile` | コンパイル要求 | アセット更新とコンパイル実行・結果待機 |

**Project Compile (`unity.project.compile`)**
- `refreshAssetDatabase` で AssetDatabase.Refresh() を実行してエディタに最新状態を通知
- `requestScriptCompilation` で Unity に C# コンパイルの開始を依頼
- 既定でコンパイル完了を待機（`awaitCompletion` は `true`）し、エラーを即座に把握
- 複数のスクリプト変更は可能な限りまとめてから実行し、再コンパイル回数を最小化
- スクリプトを生成・編集した直後に呼び出して、コンパイルエラーが残っていないか確認するのが推奨

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

### 例1: UIヒエラルキーの作成

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

### 例2: タグとレイヤーの管理

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

### 例3: Prefabワークフロー

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

### 例4: 階層メニューシステムの作成

```python
# ステップ1: UIシーンをセットアップ
unity_scene_quickSetup({"setupType": "UI"})

# ステップ2: 階層メニューを作成
unity_menu_hierarchyCreate({
    "menuName": "GameMenu",
    "menuStructure": {
        "プレイ": "StartGame",
        "設定": {
            "映像": {
                "解像度": "SetResolution",
                "品質": "SetQuality",
                "フルスクリーン": "ToggleFullscreen"
            },
            "音声": {
                "マスター音量": "SetMasterVolume",
                "BGM音量": "SetMusicVolume",
                "効果音音量": "SetSFXVolume"
            },
            "操作設定": "ConfigureControls"
        },
        "クレジット": "ShowCredits",
        "終了": "QuitGame"
    },
    "generateStateMachine": True,
    "stateMachineScriptPath": "Assets/Scripts/UI/GameMenuStateMachine.cs",
    "navigationMode": "both",
    "buttonWidth": 250,
    "buttonHeight": 60,
    "spacing": 15,
    "enableBackNavigation": True
})

# 生成されたメニューシステムは以下を含みます:
# - Canvas/GameMenu に完全なメニュー階層
# - Assets/Scripts/UI/GameMenuStateMachine.cs にState Patternスクリプト
# - キーボードとゲームパッドの両方をサポート
# - すべてのサブメニューに戻るボタン
```

---

## 開発

### ファイル構造

```
SkillForUnity/
├── Assets/
│   └── SkillForUnity/
│       ├── SkillForUnity-MCPServer.zip      # Claude Skill MCPサーバーパッケージ
│       └── Editor/
│           └── MCPBridge/                    # Unity C#ブリッジ
│               ├── McpBridgeService.cs            # WebSocketサーバー
│               ├── McpCommandProcessor.cs         # ツール実行（4700+行）
│               ├── McpContextCollector.cs         # コンテキスト収集
│               ├── McpBridgeWindow.cs             # Unity Editor UI
│               └── McpBridgeSettings.cs           # 設定
│
├── .claude/
│   └── skills/
│       └── SkillForUnity/                    # Claude Skill (Python MCPサーバー)
│           ├── src/                               # サーバー実装
│           │   ├── bridge/                        # Unityブリッジ通信
│           │   ├── tools/                         # ツール定義
│           │   ├── resources/                     # リソース
│           │   └── main.py                        # エントリポイント
│           ├── docs/                              # ドキュメント
│           ├── examples/                          # チュートリアル
│           ├── setup/                             # インストールスクリプト
│           ├── config/                            # 設定テンプレート
│           ├── skill.yml                          # スキル定義
│           └── pyproject.toml                     # Pythonパッケージ構成
```

### 新しいツールの追加

詳細なガイドは以下を参照:
- [CLAUDE.md](CLAUDE.md) - 完全な開発ドキュメント
- [TOOL_SELECTION_GUIDE.md](SkillForUnity/docs/TOOL_SELECTION_GUIDE.md) - バッチ操作やワークフローのまとめ
- `.claude/skills/SkillForUnity/docs/` - API リファレンスと詳細ガイド

---

## 機能

### コア機能
- ✅ WebSocket経由のリアルタイムUnity Editor統合
- ✅ 包括的なシーンとGameObject管理
- ✅ プロパティ更新によるコンポーネント操作
- ✅ アセットの作成と変更
- ✅ コンパイル後の自動再接続

### 高レベルツール
- ✅ **シーンクイックセットアップ** - 即座に3D/2D/UI/VRシーンを初期化
- ✅ **GameObjectテンプレート** - 事前設定されたプリミティブ、ライト、特殊オブジェクト
- ✅ **UI要素テンプレート** - 1コマンドで完全なUIコンポーネント
- ✅ **レイアウト管理** - レイアウトグループとフィッターの簡単な設定
- ✅ **階層ビルダー** - 宣言的なネスト構造作成
- ✅ **階層メニューシステム** - State Patternを使用したネストされたメニューを自動生成
- ✅ **重複防止** - シーン設定時の既存オブジェクトの自動検出

### スクリプト管理
- ✅ **バッチスクリプト管理** - 複数のC#スクリプトを一括作成・更新・削除
- ✅ **自動コンパイル** - 全操作後に単一の統合コンパイル
- ✅ **10-20倍高速** - 個別スクリプト操作と比較

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

### 開発者ツール
- ✅ コンパイル要求と結果通知
- ✅ シーン状態によるコンテキスト対応アシスタンス
- ✅ 構造化されたエラー処理とレポート

---

## ツールリファレンスサマリー

| カテゴリ | ツール数 | 操作 |
|---------|---------|------|
| **コア** | 5ツール | ping, シーン, GameObject, コンポーネント, アセット |
| **高レベル** | 7ツール | シーンクイックセットアップ, GameObjectテンプレート, UIテンプレート, レイアウトマネージャー, 階層ビルダー, 階層メニュー作成, デザインパターン生成 |
| **UI** | 3ツール | UGUI統合 + 専用ツール |
| **システム** | 3ツール | タグ/レイヤー, Prefab, 設定, レンダーパイプライン |
| **スクリプト** | 1ツール | スクリプトテンプレート生成（MonoBehaviour/ScriptableObject） |
| **合計** | **19ツール** | **85+操作** |

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
- **クイックスタート**: [SkillForUnity/QUICKSTART.md](.claude/skills/SkillForUnity/QUICKSTART.md)
- **API リファレンス**: [SkillForUnity/docs/](.claude/skills/SkillForUnity/docs/)
- **バッチ処理例**: [TOOL_SELECTION_GUIDE.md](SkillForUnity/docs/TOOL_SELECTION_GUIDE.md)
- **このファイル**: 完全なツールリファレンスとクイックスタート

---

## ライセンス

MIT License - [MIT License](https://opensource.org/licenses/MIT)

## 貢献


貢献を歓迎します！開発ガイドは[CLAUDE.md](CLAUDE.md)をお読みください。

## サポート

問題、質問、機能リクエストについて:
1. Unity Consoleでエラーメッセージを確認
2. [CLAUDE.md](CLAUDE.md)のドキュメントを確認
3. [TOOL_SELECTION_GUIDE.md](SkillForUnity/docs/TOOL_SELECTION_GUIDE.md)でバッチ操作のワークフローを確認
4. プロジェクトリポジトリにissueを作成

---

**SkillForUnity** - Model Context Protocolによる包括的なUnity Editor自動化
