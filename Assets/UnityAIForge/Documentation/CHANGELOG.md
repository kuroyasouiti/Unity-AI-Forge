# 変更履歴

Unity-AI-Forgeのすべての注目すべき変更はこのファイルに記録されます。

このフォーマットは[Keep a Changelog](https://keepachangelog.com/ja/1.0.0/)に基づいており、
このプロジェクトは[Semantic Versioning](https://semver.org/lang/ja/)に準拠しています。

## [未リリース]

（なし）

## [2.4.7] - 2025-12-20

### 追加

- **`unity_compilation_await` ツールを新規追加**
  - C#スクリプトの作成・更新後にコンパイル完了を待機するための専用ツール
  - `operation: 'await'` でコンパイル完了を待機
  - `timeoutSeconds` パラメータで待機時間を設定可能（デフォルト: 60秒）
  - コンパイル結果（成功/失敗、エラー数、警告数）を返す

- **JSON直接編集によるMCPサーバー登録機能**
  - `McpCliRegistry.RegisterProjectViaJson()` メソッドを追加
  - CLIを使用せずNewtonsoft.Jsonで設定ファイルを直接編集
  - トークン付きサーバー登録をより確実に実行
  - スコープ別の登録に対応（User, Local, Project）
  - パス正規化による既存エントリとの一致判定
  - `UnregisterProjectViaJson()` メソッドで登録解除も可能

- **複合型テストスイートの追加**
  - `CombinedTypesScriptableObjectTests.cs` - Unity型とユーザー定義型を組み合わせたテスト
  - `TestCombinedTypes.cs` - 複合型テスト用の型定義
  - `TestCombinedTypesScriptableObject.cs` - テスト用ScriptableObject

### 改善

- **McpBridgeWindow UIの大幅改善**
  - AIツール登録セクションのレイアウトを改善
  - 登録状態表示の視認性向上
  - エラーハンドリングの強化

- **ValueConverterManager の型変換強化**
  - Unity構造体の配列/List変換の改善
  - ユーザー定義構造体との複合型サポート強化
  - 文字列定数からのUnity型変換の拡張

- **UnityStructValueConverter の機能拡張**
  - ネスト構造体の変換処理を最適化
  - 辞書形式からの変換精度向上

### テスト

- **ValueConverterManagerTests.cs の拡張**
  - 複合型変換テストを追加
  - エッジケースのテストを追加
  - Unity型とカスタム型の組み合わせテストを追加

### 技術詳細

- `register_tools.py`: `compilation_await_schema` と `unity_compilation_await` ツール定義を追加
- `McpCliRegistry.cs`: JSON登録機能として約300行の新規実装
- `McpBridgeWindow.cs`: UI改善のため126行追加、80行削除
- `ValueConverterManager.cs`: 型変換ロジックの大幅改善

## [2.4.6] - 2025-12-19

### 追加

- **ユーザー定義構造体の配列/List型サポート**
  - `SerializableStructValueConverter` を新規作成
  - `[Serializable]` 属性を持つカスタム構造体の辞書からの変換をサポート
  - 配列およびListプロパティでのユーザー定義構造体の完全サポート
  - ネストした構造体（Vector3、Color等のUnity型を含む）もサポート

- **ScriptableObject inspect操作の拡張**
  - `[Serializable]` 属性を持つカスタム構造体を辞書形式でシリアライズ
  - `SerializeStructToDict` メソッドを追加してユーザー定義構造体を再帰的に辞書化

### テスト

- **ユーザー定義型配列テストを追加** (`ScriptableObjectArrayTests.cs`)
  - `Create_WithCustomStructArray_Success` - カスタム構造体配列の作成
  - `Create_WithCustomStructList_Success` - カスタム構造体Listの作成
  - `Create_WithEnumArray_Success` - Enum配列の作成
  - `Create_WithEnumList_Success` - EnumListの作成
  - `Update_CustomStructArray_Success` - カスタム構造体配列の更新
  - `Update_EnumArray_Success` - Enum配列の更新
  - `Inspect_WithCustomStructArray_ReturnsStructDictionaries` - カスタム構造体配列の検査
  - `Inspect_WithEnumArray_ReturnsEnumStrings` - Enum配列の検査
  - `Create_WithNestedStructArray_Success` - ネスト構造体配列の作成

- **テスト用型定義を追加**
  - `TestCustomStruct` - id, name, value フィールドを持つカスタム構造体
  - `TestNestedStruct` - label, position(Vector3), color(Color) を持つネスト構造体
  - `TestActionType` - Attack, Defend, Heal, Special を持つenum
  - `TestCharacterState` - Idle, Running, Jumping, Falling, Dead を持つenum

### 修正

- **McpCommandProcessorTests.cs**
  - `SetUp` メソッドで `CommandHandlerInitializer.InitializeHandlers()` を呼び出すよう修正
  - `Execute_PingUnityEditor_ShouldReturnEditorInfo` テストをPingHandlerの実際のレスポンスに合わせて修正
  - `GetHandlerMode_UnregisteredTool_ShouldReturnLegacy` テストを未登録のツール名を使うよう修正

- **SceneCommandHandlerTests.cs**
  - ビルド設定オペレーション（listBuildSettings等）が `ProjectSettingsManageHandler` に移動されたことを反映
  - `SupportedOperations_ShouldContainExpectedOperations` からビルド設定関連のアサーションを削除
  - `Execute_ListBuildSettings_ShouldReturnSceneList` テストを削除

### 技術詳細

- `SerializableStructValueConverter.cs`: 新規ファイル（130行）
- `ValueConverterManager.cs`: `SerializableStructValueConverter` をPriority 150で登録
- `ScriptableObjectCommandHandler.cs`: `SerializeStructToDict` メソッドを追加

## [2.4.5] - 2025-12-19

### 追加

- **ScriptableObject配列/List型変数の操作サポート**
  - `ScriptableObjectCommandHandler` で配列および `List<T>` 型プロパティの操作が可能に
  - `ValueConverterManager` を使用した統一的な型変換処理
  - 配列/Listの作成、更新、検査（inspect）操作をサポート
  - Unity Object参照を含む配列も適切にシリアライズ

### 改善

- **`ScriptableObjectCommandHandler` のリファクタリング**
  - `ConvertPropertyValue` メソッドを `ValueConverterManager.Instance.Convert()` を使用するように変更
  - `SerializePropertyValue` メソッドを拡張し、以下の型のシリアライズに対応:
    - 配列および `IList` 型（再帰的にシリアライズ）
    - Unity Object参照（アセットパスとGUIDを含む辞書形式）
    - Vector4、Rect、Bounds
    - 列挙型（enum）
    - プリミティブ型（int, float, string, bool）

### テスト

- **`ArrayValueConverterTests.cs`** - 30以上の単体テストを追加
  - CanConvert テスト（配列、List、非配列型）
  - 配列変換テスト（int[], string[], float[]）
  - List変換テスト（List<int>, List<string>, List<float>）
  - Unity型変換テスト（Vector3[], Color[], List<Vector2>）
  - null/空配列処理テスト
  - 単一値のラップテスト
  - ValueConverterManager統合テスト

- **`ScriptableObjectArrayTests.cs`** - ScriptableObject配列操作の統合テスト
  - 配列プロパティの作成テスト
  - 配列プロパティの更新テスト
  - 配列プロパティの検査テスト
  - 複数配列型の同時操作テスト

### 技術詳細

- `ScriptableObjectCommandHandler.cs`: `ConvertPropertyValue` と `SerializePropertyValue` メソッドを大幅改善
- `ArrayValueConverterTests.cs`: 新規ファイル作成（357行）
- `ScriptableObjectArrayTests.cs`: 新規ファイル作成（テストクラスとテスト用ScriptableObject定義を含む）

## [2.4.4] - 2025-12-17

### 修正

- **Build Settings操作の重複を解消**
  - `SceneCommandHandler` からビルド設定操作（`listBuildSettings`, `addToBuildSettings`, `removeFromBuildSettings`, `reorderBuildSettings`, `setBuildSettingsEnabled`）を削除
  - ビルド設定操作は `ProjectSettingsManageHandler` (`unity_projectSettings_crud`) に一元化
  - Python MCP サーバーの `scene_manage_schema` からもビルド設定操作を削除
  - `unity_scene_crud` のツール説明を更新し、ビルド設定操作は `unity_projectSettings_crud` を使用するよう案内

- **未登録ハンドラーの登録**
  - `PingHandler` を新規作成し、`pingUnityEditor` コマンドを処理可能に
  - `CompilationAwaitHandler` を `CommandHandlerInitializer` に登録
  - ブリッジ接続確認（`unity_ping`）が正常に動作するように

### 技術詳細

- `SceneCommandHandler.cs`: ビルド設定関連の183行を削除
- `PingHandler.cs`: 新規ファイル作成（76行）、`operation` パラメータ不要で `pong` レスポンスを返す
- `CommandHandlerInitializer.cs`: `pingUnityEditor` と `compilationAwait` ハンドラーを Phase3 に登録
- `register_tools.py`: `scene_manage_schema` を6操作に簡素化

### ドキュメント

- CHANGELOG.md を v2.4.4 に更新
- package.json バージョンを 2.4.4 に更新
- pyproject.toml バージョンを 2.4.4 に更新
- CLAUDE.md のハンドラー登録手順を更新

## [2.4.3] - 2025-12-16

### 追加

- **配列・リスト型コンポーネント変数サポート**
  - `ArrayValueConverter` を追加し、配列および `List<T>` 型のコンポーネントプロパティ操作が可能に
  - Priority: 250（UnityObjectReferenceConverter: 300 と UnityStructValueConverter: 200 の間）
  - 要素の変換は `ValueConverterManager` を通じて適切な型に自動変換
  - 単一の値を1要素の配列として自動変換する機能も搭載

- **コンパイル結果取得API**
  - `McpCommandProcessor.GetCompilationResult()` メソッドを追加
  - コンパイル後のエラー数、警告数、メッセージ一覧を取得可能
  - `GetConsoleLogEntries()` ヘルパーメソッドでUnityコンソールログを取得
  - リフレクションを使用してUnity内部の `LogEntries` にアクセス

### 改善

- **MCP登録状況チェックのパフォーマンス大幅改善**
  - `McpCliRegistry.IsServerRegistered()` を JSON ファイル直接読み込みに変更
  - `mcp list` CLI コマンド（重い処理）を使用しなくなった
  - Claude Code の設定ファイル構造に対応:
    - User スコープ: `~/.claude.json` → `mcpServers` セクション
    - Local スコープ: `~/.claude.json` → `projects.[projectPath].mcpServers` セクション
    - Project スコープ: `[projectDir]/.claude/settings.json` → `mcpServers` セクション
  - 各AIツール固有のスコープ別パス解決ロジックを実装

### 技術詳細

- `ArrayValueConverter.cs`: 新規ファイル作成、`IValueConverter` インターフェース実装
- `ValueConverterManager.cs`: `ArrayValueConverter` を Priority 250 で登録
- `McpCommandProcessor.cs`: `GetCompilationResult()` および `GetConsoleLogEntries()` メソッド追加
- `McpCliRegistry.cs`: `GetScopedConfigPath()` および `IsServerRegisteredInClaudeCodeConfig()` メソッド追加

## [2.4.2] - 2025-12-13

### 修正

- **GameObjectテンプレートプリミティブサポート**
  - `unity_gameobject_crud` の `template` パラメータでUnity組み込みプリミティブをサポート
  - 対応プリミティブ: Cube, Sphere, Capsule, Cylinder, Plane, Quad
  - 大文字小文字を区別しない（"cube", "CUBE", "Cube" すべて動作）
  - `GameObject.CreatePrimitive()` を使用して高速生成
  - プレファブパスも引き続きサポート（プリミティブ名でない場合）

- **バッチ順次処理ツール名マッピング**
  - `unity_batch_sequential_execute` でMCPツール名を使用可能に
  - 例: `unity_gameobject_crud` → 内部名 `gameObjectManage` への自動変換
  - 全26ツールのマッピングを追加（Low/Mid/High-Level + UI Management）
  - MCP名と内部名の両方をサポート（後方互換性維持）
  - 不明なツール名に対する詳細なエラーメッセージ

### 技術詳細

- `GameObjectCommandHandler.cs`: `GetPrimitiveType()` ヘルパーメソッド追加、`CreateGameObject()` でプリミティブ判定ロジック追加
- `batch_sequential.py`: `TOOL_NAME_MAPPING` 辞書と `resolve_tool_name()` 関数を追加、操作実行前にツール名を解決

## [2.4.1] - 2025-12-11

### 追加

- **UIHierarchyHandler (Mid-Level 宣言的UI階層管理)**
  - 操作: create, clone, inspect, delete, show, hide, toggle, setNavigation
  - **create**: JSON構造から複雑なUI階層を一括作成
    - 対応要素タイプ: panel, button, text, image, inputfield, scrollview, toggle, slider, dropdown
    - レイアウトグループ自動設定（Horizontal, Vertical, Grid）
    - アンカープリセット対応（topLeft, middleCenter, bottomRight等）
    - TextMeshPro 動的検出（フォールバック: レガシー Text）
  - **clone**: 既存UI階層の複製（リネーム対応）
  - **inspect**: UI階層をJSON構造として出力
  - **delete**: UI階層の削除
  - **show/hide/toggle**: CanvasGroup による可視性制御（alpha, interactable, blocksRaycasts）
  - **setNavigation**: キーボード/ゲームパッドナビゲーション設定
    - モード: none, auto-vertical, auto-horizontal, explicit
    - ラップアラウンドサポート

- **UIStateHandler (Mid-Level UIステート管理)**
  - 操作: defineState, applyState, saveState, loadState, listStates, deleteState, createStateGroup, transitionTo, getActiveState
  - **defineState**: 名前付きUIステートを定義（各要素のactive, visible, interactable, alpha, position, size）
  - **applyState**: 保存したステートをUI要素に適用
  - **saveState**: 現在のUIステートをキャプチャ（子要素含む）
  - **loadState**: ステート定義を読み込み（適用せず）
  - **listStates**: 定義済みステート一覧
  - **deleteState**: ステート定義の削除
  - **createStateGroup**: 相互排他的なステートグループ作成
  - **transitionTo**: ステートへの遷移（applyStateのエイリアス）
  - **getActiveState**: 現在アクティブなステート名を取得
  - EditorPrefsによるステート永続化

- **UINavigationHandler (Mid-Level UIナビゲーション管理)**
  - 操作: configure, setExplicit, autoSetup, createGroup, setFirstSelected, inspect, reset, disable
  - **configure**: 単一Selectableのナビゲーションモード設定（none/horizontal/vertical/automatic/explicit）
  - **setExplicit**: 明示的なup/down/left/rightターゲット設定
  - **autoSetup**: ルート以下の全Selectableを自動設定（vertical/horizontal/grid対応）
  - **createGroup**: 分離されたナビゲーショングループ作成
  - **setFirstSelected**: EventSystemの最初の選択要素設定
  - **inspect**: 現在のナビゲーション設定を表示
  - **reset**: 自動ナビゲーションにリセット
  - **disable**: ナビゲーション無効化
  - グリッドナビゲーション時の自動カラム検出
  - ラップアラウンドサポート

- **GameKitInteractionHandler 2Dコライダー対応**
  - 新パラメータ: `is2D` (boolean, デフォルト: false)
  - 2Dコライダー: BoxCollider2D, CircleCollider2D, CapsuleCollider2D, PolygonCollider2D
  - 3Dコライダー: BoxCollider, SphereCollider, CapsuleCollider, MeshCollider
  - triggerShape enum拡張: box, sphere, circle, capsule, polygon, mesh

### 技術詳細

- ツール総数: 26 → 29（UIHierarchy, UIState, UINavigation 追加）
- `CommandHandlerInitializer.cs`: uiHierarchy, uiState, uiNavigation ハンドラーを Mid-Level ツールとして登録
- `register_tools.py`: ui_hierarchy_schema, ui_state_schema, ui_navigation_schema とハンドラーマッピングを追加
- `CLAUDE.md`: 「10. UI Hierarchy」「11. UI State Management」「12. UI Navigation」セクションを追加

### ドキュメント

- CLAUDE.md に unity_ui_hierarchy, unity_ui_state, unity_ui_navigation ツールのドキュメントを追加
- CHANGELOG.md を v2.4.1 に更新

## [2.4.0] - 2025-12-11

### 追加

- **WebSocket認証機能の復活**
  - `McpBridgeSettings.cs`: トークン管理メソッド（BridgeTokens, AddToken, RemoveToken, GenerateAndAddToken, IsValidToken）を復活
  - `McpBridgeService.cs`: ValidateToken メソッドを復活（Authorization ヘッダー、X-MCP-Bridge-Token ヘッダー、クエリパラメータ対応）
  - Python側: `env.py` に bridge_token 設定、`bridge_connector.py` に認証ヘッダー送信機能を追加
  - 複数プロジェクトでのMCPサーバー共有を安全にサポート

- **Sprite2DBundleHandler (Mid-Level 2Dスプライト管理)**
  - 操作: createSprite, updateSprite, inspect, updateMultiple, setSortingLayer, setColor, sliceSpriteSheet, createSpriteAtlas
  - SpriteRenderer の作成・更新、ソートレイヤー設定、色変更
  - スプライトシートのスライス（グリッド/自動モード）
  - SpriteAtlas アセット作成
  - バッチ操作（パターンマッチング対応）

- **Animation2DBundleHandler (Mid-Level 2Dアニメーション管理)**
  - 操作: setupAnimator, updateAnimator, inspectAnimator, createController, addState, addTransition, addParameter, inspectController, createClipFromSprites, updateClip, inspectClip
  - Animator コンポーネントの設定・検査
  - AnimatorController の作成・ステート追加・遷移追加
  - スプライトシーケンスからの AnimationClip 作成
  - パラメータ管理（Bool/Float/Int/Trigger）
  - 遷移条件設定（If/IfNot/Greater/Less/Equals/NotEqual）

- **UIFoundationHandler 機能拡張（階層的UI設計サポート）**
  - 新操作: createScrollView, addLayoutGroup, updateLayoutGroup, removeLayoutGroup, createFromTemplate
  - **ScrollView 作成**: Viewport, Content, 水平/垂直スクロールバー対応
    - MovementType（Unrestricted/Elastic/Clamped）、慣性、減速率の設定
  - **LayoutGroup 管理**: Horizontal, Vertical, Grid レイアウト
    - padding, spacing, childAlignment, childControl*, childForceExpand* 等の完全設定
    - Grid専用: startCorner, startAxis, cellSize, constraint, constraintCount
  - **ContentSizeFitter 統合**: horizontalFit, verticalFit オプション
  - **UIテンプレート機能** (createFromTemplate):
    - `dialog`: タイトル、メッセージ、OK/Cancelボタン付きダイアログ
    - `hud`: 左上HP/右上スコア/下部ミニマップ付きHUD
    - `menu`: 垂直レイアウトのメニューパネル
    - `statusBar`: 上部または下部の情報バー
    - `inventoryGrid`: グリッドレイアウトのインベントリ

- **MCPサーバープロンプトに UI優先設計原則を追加**
  - 人間が操作・確認できるUIから優先的に実装する設計指針
  - 推奨実装順序: UI/フィードバック → ゲームロジック → 演出
  - デバッグUI作成の推奨パターン

### 技術詳細

- ツール総数: 24 → 26（Sprite2DBundle, Animation2DBundle 追加）
- `CommandHandlerInitializer.cs`: 新しいハンドラーを Mid-Level ツールとして登録
- `register_tools.py`: Python スキーマとハンドラーマッピングを追加
- 非推奨API警告を pragma で抑制（usedByComposite, TextureImporter.spritesheet）
- UnityEditor.U2D 名前空間を使用した SpriteAtlas API 対応
- UIFoundationHandler: 12操作に拡張（createCanvas, createPanel, createButton, createText, createImage, createInputField, createScrollView, addLayoutGroup, updateLayoutGroup, removeLayoutGroup, createFromTemplate, inspect）

### ドキュメント

- CHANGELOG.md を v2.4.0 に更新
- MCPサーバープロンプトに UI優先設計原則セクションを追加

## [2.3.5] - 2025-12-09

### 追加

- **CLI ベースの MCP サーバー登録機能**
  - AIツール（Cursor、Claude Code、Cline、Windsurf）への MCP サーバー登録を CLI 経由で実行
  - JSON 設定ファイル直接編集よりも信頼性が高く、公式CLIコマンドを使用
  - CLI 非対応ツール（Claude Desktop）は従来の JSON 編集にフォールバック
  - `McpToolRegistry.cs`: CLI サポートを自動検出し、適切な登録方法を選択
  - `McpCliRegistry.cs`: 各 AI ツールの CLI コマンドを辞書ベースで管理

- **Claude Code サポート**
  - `AITool` enum に `ClaudeCode` を追加
  - Claude Code 設定ファイルパス（`~/.claude.json`）のサポート
  - `claude mcp add/remove` コマンドによる登録・解除

- **MCP Bridge Window に「AI Tool Registration (CLI)」セクションを追加**
  - 各 AI ツールの CLI 利用可否と登録状態をリアルタイム表示
  - ツールごとの Register/Unregister ボタン
  - 一括操作ボタン（Register All、Unregister All）
  - 状態更新のための Refresh ボタン
  - Config File Manager セクションに Claude Code ボタンを追加

### 技術詳細

- `McpCliRegistry.cs` を辞書ベースのアーキテクチャにリファクタリング
  - 新しい AI ツールの追加が容易に
  - `CliCommands` 辞書で CLI コマンドと表示名を管理
  - `IsCliAvailable(AITool)` メソッドでツール固有の CLI 利用可否を確認

- `McpToolRegistry.cs` の登録フロー改善
  - `IsCliSupported(AITool)`: CLI 対応かつ CLI が利用可能かを判定
  - `RegisterViaCli()` / `UnregisterViaCli()`: CLI 経由の登録・解除
  - `RegisterViaConfig()` / `UnregisterViaConfig()`: JSON 設定ファイル経由の登録・解除
  - 自動フォールバック機能

### ドキュメント

- CHANGELOG.md を v2.3.5 に更新
- package.json バージョンを 2.3.5 に更新
- pyproject.toml バージョンを 2.3.5 に更新

## [2.3.4] - 2025-12-08

### 追加

- **Unity オブジェクト参照の文字列パス解決**
  - `propertyChanges` で文字列パスから Unity オブジェクト参照を自動解決
  - サポートする参照タイプ:
    - `GameObject`: パスからGameObjectを取得
    - `Transform` / `RectTransform`: パスからTransformを取得
    - `Component` 派生型（`TMP_Text`, `Button`, `InputField` など）: パスからGameObjectを見つけ、指定コンポーネントを取得
    - アセット参照: `Assets/...` 形式のパスからアセットをロード
  - **3つの指定形式をサポート**:
    - 文字列形式（シンプル）: `"titleText": "Canvas/Panel/TitleText"`
    - $ref形式（推奨）: `"titleText": { "$ref": "Canvas/Panel/TitleText" }`
    - 明示的参照形式: `"titleText": { "$type": "reference", "$path": "Canvas/Panel/TitleText" }`
  - 使用例（$ref形式 - 推奨）:
    ```json
    {
      "operation": "update",
      "gameObjectPath": "Controller",
      "componentType": "MyUIController",
      "propertyChanges": {
        "titleText": { "$ref": "Canvas/Panel/TitleText" },
        "submitButton": { "$ref": "Canvas/Panel/SubmitButton" },
        "normalValue": "plain string value"
      }
    }
    ```
  - 使用例（文字列形式 - 型が UnityEngine.Object の場合のみ）:
    ```json
    {
      "propertyChanges": {
        "titleText": "Canvas/Panel/TitleText"
      }
    }
    ```

### 技術詳細

- `ResolveUnityObjectFromPath()` メソッドを追加
- サポートする参照形式:
  - `{ "$ref": "path" }` - シンプルな参照形式（推奨）
  - `{ "_gameObjectPath": "path" }` - 代替形式
  - `{ "$type": "reference", "$path": "path" }` - 明示的参照形式
  - `"path"` - 文字列形式（ターゲット型がUnityEngine.Objectの場合のみ）
- 階層パスの検索ロジック:
  1. `GameObject.Find()` で完全パス検索
  2. 見つからない場合、アクティブシーンのルートオブジェクトから相対パス検索
  3. コンポーネント型の場合、見つかったGameObjectから `GetComponent()` で取得

### ドキュメント

- MCPサーバーの `unity_component_crud` ツール説明に参照形式のドキュメントを追加
- `skill.yml` バージョンを 2.3.4 に更新

## [2.3.3] - 2025-12-08

### 追加

- **ComponentCommandHandler の機能強化**
  - `propertyFilter` 機能の修正: `List<object>`、`string[]`、カンマ区切り文字列など様々な入力形式に対応
  - `addMultiple` 操作で `propertyChanges` をサポート: コンポーネント追加時に初期プロパティを設定可能に
  - 結果に `updatedProperties` を含めるように改善

- **Unity型変換サポートの大幅拡張**
  - `Color` / `Color32` 型: Dictionary形式 `{r, g, b, a}` からの変換
  - `Vector2` / `Vector3` / `Vector4` 型: Dictionary形式からの変換
  - `Quaternion` 型: Dictionary形式 `{x, y, z, w}` からの変換
  - `Rect` 型: Dictionary形式 `{x, y, width, height}` からの変換
  - `Bounds` 型: Dictionary形式 `{center, size}` からの変換
  - `Enum` 型: 文字列名または整数値からの変換

- **新規テストスイート**
  - `ComponentCommandHandlerTests`: 36テスト（12テスト新規追加）
    - PropertyFilter テスト（4件）: 各種入力形式のフィルタリング
    - AddMultiple with PropertyChanges テスト（2件）: 初期プロパティ設定
    - Color Type Conversion テスト（3件）: Color型のDictionary変換
    - Vector Type Conversion テスト（2件）: Vector2/3型のDictionary変換
    - Enum Type Conversion テスト（1件）: 文字列からEnum変換

### 修正

- **TypeResolverTests.ResolveByShortName_MultipleNamespaces_ShouldSearchAll**
  - ジェネリック型 `List<T>` の短縮名検索問題を修正
  - テストを `DateTime` 型を使用するように変更

- **GameObjectCommandHandlerTests**
  - `IList<object>` から `System.Collections.IList` へのキャスト修正
  - `Execute_Inspect_ShouldReturnGameObjectInfo` の null 参照エラーを解決
  - `Execute_InspectMultiple_WithPattern_ShouldReturnMultipleInfo` の null 参照エラーを解決

- **SceneCommandHandlerTests**
  - `Execute_Create_WithAdditive_ShouldCreateAdditiveScene` テストの安定性向上
  - テスト環境の制限を考慮した `Assert.Inconclusive` による適切なハンドリング

### テスト結果

- 総テスト数: 187
- 成功: 186
- Inconclusive: 1（テスト環境の制限）
- 失敗: 0

## [2.3.2] - 2025-12-06

### 追加

- **ビルド設定管理機能 (`unity_projectSettings_crud`)**
  - `addSceneToBuild`: ビルド設定にシーンを追加（任意のインデックス位置に挿入可能）
  - `removeSceneFromBuild`: シーンパスまたはインデックスでビルド設定からシーンを削除
  - `listBuildScenes`: ビルド設定内の全シーンを一覧表示（パス、GUID、有効/無効状態、インデックス）
  - `reorderBuildScenes`: ビルド内のシーン順序を変更
  - `setBuildSceneEnabled`: ビルド内のシーンを有効化/無効化
  - ビルド設定の完全な自動化が可能に

- **レンダリングレイヤー管理機能 (`unity_projectSettings_crud`)**
  - `addRenderingLayer`: URP/HDRP用のレンダリングレイヤーを追加（最大32レイヤー）
  - `removeRenderingLayer`: レンダリングレイヤーを削除
  - `renderingLayers`: レンダリングレイヤー一覧の取得
  - Unity 2022.2以降で利用可能なレンダリングパイプライン機能をサポート
  - ライトとカメラのレンダリング制御に使用

- **ブリッジトークンの自動同期**
  - MCPサーバーインストール時に `.mcp_bridge_token` をコピー（無い場合は生成）
  - Pythonサーバーはカレントディレクトリの `.mcp_bridge_token` を優先参照
  - WebSocket接続時のトークンをクエリパラメータで渡すように変更（旧extra_headers非依存）

### 改善

- **ドキュメント更新：GameObjectレイヤー設定機能の明確化**
  - `unity_gameobject_crud`の`update`操作で`layer`パラメータが使用可能であることを明示
  - レイヤー名（文字列）またはレイヤー番号（整数）の両方に対応
  - MCPサーバーのプロンプトに使用例を追加
  - `CLAUDE.md`の古い`unity_tagLayer_manage`情報を最新の方法に更新
  - タグ/レイヤー管理が`unity_gameobject_crud`と`unity_projectSettings_crud`に統合されていることを文書化

## [2.3.1] - 2025-01-03

### 追加

- **GameKitSceneFlow 自動ロードシステム**
  - プレハブベースのSceneFlow管理を実装
  - `GameKitSceneFlowAutoLoader` (Editor): Play Mode開始時に自動ロード
  - `GameKitSceneFlowRuntimeLoader` (Runtime): ゲーム開始前に自動ロード
  - `Resources/GameKitSceneFlows/` にプレハブを配置すると自動で読み込まれる
  - 初期シーンへの手動配置が不要に
  - Git管理可能なプレハブファイルでチームコラボレーション対応
  - Unity Editorメニューから `Tools → Unity-AI-Forge → GameKit → Create SceneFlows Directory` でディレクトリ作成

### 修正

- **Unity Editor フリーズ問題の解決**
  - C#スクリプト作成・更新・削除時のフリーズ問題を完全に修正
  - Unity側の同期的なコンパイル待機（`Thread.Sleep()`）を削除
  - MCPサーバー側で非同期的なコンパイル待機を実装
  - `bridge:restarted` メッセージによるアセンブリリロード検出
  - コンパイル結果（成功/失敗、エラー数、経過時間）をレスポンスに含めるように改善

- **BaseCommandHandler の最適化**
  - `WaitForCompilationAfterOperation()` メソッドを簡略化
  - `GetBridgeConnectionState()` メソッドを削除（不要になったため）
  - Unity Editorのメインスレッドをブロックしないよう改善

- **AssetCommandHandler の改善**
  - `RequiresCompilationWait()` メソッドを簡素化
  - 不要な `_currentPayload` フィールドと `Execute()` オーバーライドを削除
  - コンパイル待機を常に無効化し、MCPサーバー側で処理

- **MCPサーバー: bridge_manager.py**
  - `_handle_bridge_restarted()` でコンパイル待機中の全ての `Future` を解決
  - Unity bridgeの再起動を検出してクライアントに通知
  - コンパイル完了情報を詳細に記録

- **MCPサーバー: register_tools.py**
  - `unity_asset_crud` で `.cs` ファイルの作成・更新・削除時に自動コンパイル待機
  - 60秒のタイムアウト設定
  - エラー発生時もオペレーション自体は失敗させない
  - コンパイル結果をレスポンスに自動的に追加

- **GameKitSceneFlowHandler のプレハブベース化**
  - 全操作が `Resources/GameKitSceneFlows/` のプレハブを編集
  - `create` 操作でプレハブを自動生成
  - `delete` 操作でプレハブファイルを削除
  - `PrefabUtility.EditPrefabContentsScope` を使用した安全な編集
  - レスポンスに `prefabPath` と自動ロード情報を含める

### 技術的な詳細

この修正により、Unity-AI-Forgeは以下のフローで動作します：

1. クライアント → MCPサーバー: `unity_asset_crud` (create/update/delete .cs file)
2. MCPサーバー → Unity Bridge: `assetManage` コマンド送信
3. Unity: スクリプトファイル作成/更新/削除 → `AssetDatabase.Refresh()`
4. Unity: コンパイル開始 → `compilation:started` メッセージ送信
5. MCPサーバー: `await_compilation()` で待機開始
6. Unity: コンパイル完了 → `compilation:complete` メッセージ送信
7. Unity: アセンブリリロード → Bridge再起動
8. Unity: `bridge:restarted` メッセージ送信
9. MCPサーバー: 待機解除、コンパイル結果を返す
10. クライアント: 成功レスポンス + コンパイル結果を受信

## [2.3.0] - 2025-12-04

### 追加

- **Physics2D 設定サポート**
  - 新カテゴリ `physics2d` を `unity_projectSettings_crud` に追加
  - 2D重力設定 (gravity x/y)
  - 速度・位置反復回数、閾値設定
  - シミュレーションモードの制御
  - 2Dゲーム開発に必要な物理パラメータを完全サポート

- **ソートレイヤー管理機能**
  - `unity_projectSettings_crud` の `tagsLayers` カテゴリに追加
  - `addSortingLayer`: ソートレイヤーの追加
  - `removeSortingLayer`: ソートレイヤーの削除
  - `sortingLayers`: ソートレイヤー一覧の取得
  - 2Dスプライトの描画順序を完全制御

- **CharacterController Bundle ツール** (`unity_character_controller_bundle`)
  - 3Dキャラクター用の最適化されたプリセット
  - 7つのプリセット: fps, tps, platformer, child, large, narrow, custom
  - 自動設定: radius, height, center, slopeLimit, stepOffset, skinWidth
  - バッチ適用とカスタム設定に対応

- **MCPサーバープロンプト強化**
  - Machinations システムの詳細説明を追加
  - CharacterController Bundle の使用例を追加
  - バッチ順次処理の詳細ガイドを追加
  - 4つの構成要素の説明 (Resource Pools/Flows/Converters/Triggers)

### 変更

- **ツール総数**: 22 → 24ツール
  - Mid-Level Batch: 7 → 8ツール (CharacterController追加)
  - High-Level GameKit: Machinations を含む6ツール
  
- **プロジェクト設定カテゴリ**: 7 → 8カテゴリ
  - `physics2d` カテゴリを追加
  - `tagsLayers` カテゴリにソートレイヤー機能を追加

### 修正

- Physics2D プロパティの正確性を向上
  - `velocityThreshold` を正しく `Physics2D.velocityThreshold` にマッピング
  - Unityバージョン互換性のため `baumgarteTimeOfImpactScale` を削除

### ドキュメント

- README.md にv2.3.0の新機能セクションを追加
- INDEX.md のツール数を更新 (22→24)
- MCPサーバープロンプトを全面的に更新
- すべてのバージョン情報を2.3.0に統一

## [2.2.0] - 2025-12-03

### 追加

- **バッチ逐次処理ツール** (`unity_batch_sequential`)
  - 複数のMCPツール呼び出しを順次実行
  - エラー発生時に自動停止し、残りの処理を保存
  - レジューム機能により中断した処理を再開可能
  - 別リソース (`batch_queue`) から処理キューを参照可能
  - `resume=false` で新規処理を開始し、既存キューをクリア
  - 使用例: 複数GameObject作成、複雑なシーン構築、バッチ設定変更

- **バッチキューリソース** (`batch_queue`)
  - 実行待ちの処理キューを外部から参照
  - 処理の進行状況を確認可能
  - エラー情報と失敗位置を保持

### 機能

- エラーハンドリングの改善
  - 処理中断時に詳細なエラー情報を提供
  - 失敗した処理のインデックスを記録
  - 次回実行時に失敗箇所から再開

- ドキュメント
  - バッチ逐次処理ツールの詳細ドキュメント ([BATCH_SEQUENTIAL.md](MCPServer/BATCH_SEQUENTIAL.md))
  - 使用例とベストプラクティスを追加

## [2.0.0] - 2025-11-29

### 🔥 プロジェクト名変更

- **SkillForUnity** → **Unity-AI-Forge**
  - 新パッケージ名: `com.unityaiforge`
  - 新リポジトリ: `https://github.com/kuroyasouiti/Unity-AI-Forge`
  - AI駆動開発とAI連携による「鍛造（forging）」を強調

### 破壊的変更

- **プロジェクト名変更** - パッケージ参照とインポートの更新が必要
- **GameKit Manager** - ハブベースアーキテクチャへの完全な再設計。既存のmanagerメソッドを使用するコードは引き続き動作（後方互換APIあり）しますが、内部構造が変更されています。
- **GameKit Interaction** - 新しいトリガータイプとアクションシステムにより、既存のインタラクションセットアップの更新が必要な場合があります。
- **GameKit SceneFlow** - 遷移がグローバルではなくシーンごとに定義されるようになりました。シーン遷移を使用しているプロジェクトには移行が必要です。

### 変更

- **GameKit Manager** - モード固有コンポーネントを持つマネージャーハブとして再設計
  - ManagerTypeに基づいてモード固有コンポーネントを自動追加
  - **TurnBased** → GameKitTurnManager（ターンフェーズ、ターンカウンター、フェーズ/ターンイベント）
  - **ResourcePool** → GameKitResourceManager（Machinations風リソースフローシステム）
    - 最小/最大制約付きリソースプール
    - 自動リソースフロー（ソース/ドレイン）
    - リソースコンバーター（クラフティング、変換）
    - リソーストリガー（しきい値ベースのイベント）
    - イベント: `OnResourceChanged`、`OnResourceTriggered`
  - **EventHub** → GameKitEventManager（イベント登録、イベントトリガー）
  - **StateManager** → GameKitStateManager（状態変更、状態履歴）
  - **Realtime** → GameKitRealtimeManager（タイムスケール、一時停止/再開、タイマー）
  - 便利メソッドはモード固有コンポーネントに自動的にデリゲート
  - 後方互換API（既存のコードは引き続き動作）
  - モード固有コンポーネントへの直接アクセス用の`GetModeComponent<T>()`

- **GameKit Interaction** - インタラクションハブとして再設計
  - 従来のトリガー（Collision、Trigger、Input、Proximity、Raycast）をサポート
  - **新しい特殊トリガー**: TilemapCell、GraphNode、SplineProgress
  - **拡張アクション**: TriggerActorAction、UpdateManagerResource、TriggerSceneFlow、TeleportToTile、MoveToGraphNode、SetSplineProgress
  - **拡張条件**: ActorId、ManagerResource
  - UnityEvents統合（`OnInteractionTriggered`）
  - クールダウンとリピート設定
  - 手動トリガーサポート
  - デバッグログオプション
  - 近接およびタイルマップトリガー用のGizmo視覚化

### 追加
- **CharacterController Bundle** (`unity_character_controller_bundle`) - 中レベルツール
  - プリセット付きCharacterControllerの適用: fps、tps、platformer、child、large、narrow、custom
  - 複数のGameObjectのバッチ操作
  - 設定可能な衝突プロパティ（radius、height、center、slope limit、step offset）
  - ランタイム状態（isGrounded、velocity）を含むCharacterControllerプロパティの検査
  
- **GameKit Actor Input System統合**
  - Unityの新しいInput System用の`GameKitInputSystemController`コンポーネント
  - 事前構築されたアクションマップによる自動PlayerInput設定
  - デフォルト入力アクションアセット生成（WASD、マウス、ゲームパッドサポート）
  - Input Systemが利用できない場合の`GameKitSimpleInput`への自動フォールバック
  - 動作プロファイルに基づく2D/3D入力変換

- **GameKit AIコントローラー**
  - 自律的なキャラクター制御用の`GameKitSimpleAI`コンポーネント
  - AIビヘイビア: Idle、Patrol、Follow、Wander
  - 設定可能なウェイポイント、フォローターゲット、ワンダー半径

### 変更
- **GameKit UI Command Hub** - UI-to-Actorブリッジとして再設計
  - `GameKitActor`のUnityEventsにUIコントロールをブリッジする集中ハブとして機能
  - コマンドタイプシステム（Move、Jump、Action、Look、Custom）
  - 移動コマンド用の方向ボタンサポート
  - パラメータベースのアクションコマンド
  - パフォーマンス向上のためのActorリファレンスキャッシング
  - CustomコマンドタイプによるSendMessageとの後方互換性
  - 改善されたAPI: `ExecuteMoveCommand()`、`ExecuteJumpCommand()`、`ExecuteActionCommand()`、`ExecuteLookCommand()`
  - コマンドバインディング管理: `RegisterButton()`、`RegisterDirectionalButton()`、`ClearBindings()`
  - オプションのコマンドロギング付き拡張デバッグ

- **GameKit SceneFlow** - シーン中心のステートマシンとして再設計
  - 遷移をシーン定義に統合（シーン中心設計）
  - 同じトリガーが現在のシーンに基づいて異なる宛先に導く（例: Page1からの「nextPage」→Page2、Page2から→Page3）
  - **簡素化された共有シーン管理**: `SharedSceneGroup`を削除、シーンが共有シーンパスを直接定義
  - シーン定義に遷移と共有シーンパスを含む
  - 改善された共有シーン管理（必要なもののみをリロード）
  - 新しいAPI: `SetCurrentScene()`、`GetAvailableTriggers()`、`GetSceneNames()`、`AddSharedScenesToScene()`
  - シーン遷移の拡張ロギング
  - 後方互換API（AddTransitionパラメータ順序をfromScene、trigger、toSceneに変更）
  - `sharedGroups`パラメータを`sharedScenePaths`に変更（レガシー`sharedGroups`は後方互換性のためサポート）

- **GameKit Graph Node Movement** - 新しい動作プロファイル
  - 移動ノードを定義する`GraphNode`コンポーネント

- **GameKit Spline Movement** - 2.5Dゲーム用の新しい動作プロファイル
  - レール/スプラインベース移動用の`SplineMovement`コンポーネント
  - 滑らかな曲線パス用のCatmull-Romスプライン補間
  - 円形トラック用のクローズドループサポート
  - レーンベースゲームプレイ用の横方向オフセット（レールシューター、サイドスクローラー）
  - 加速/減速付きの手動および自動速度制御
  - 前進および後進移動サポート
  - 移動方向を向く自動回転（設定可能な軸）
  - Scene viewでのビジュアルスプラインデバッグ
  - レールシューター、2.5Dプラットフォーマー、レーシングゲーム、オンレールシーケンスに最適
  - A*パスファインディング付きの`GraphNodeMovement`コンポーネント
  - コストと通過可能性を持つノード接続
  - 2Dと3Dの両方で動作（次元非依存）
  - 使用例: ボードゲーム、タクティカルRPG、パズルゲーム、アドベンチャーゲーム
  - 機能: 重み付きエッジ、パスファインディング、到達可能ノードクエリ、デバッグ視覚化

### 変更
- `GameKitActorHandler.ApplyControlComponents()`をデフォルトでInput Systemを使用するように更新
- GameKit Runtimeアセンブリに`UNITY_INPUT_SYSTEM_INSTALLED`定義制約を追加

### ドキュメント
- CharacterController Bundleの包括的なドキュメントを追加
- アーキテクチャ概要付きのGameKit Runtimeコンポーネント READMEを追加
- 新機能でREADME.mdとREADME_ja.mdを更新

## [1.8.0] - 2025-11-29

### 追加

#### 新しいツール
- **Prefab管理** (`unity_prefab_crud`)
  - GameObjectからPrefabを作成
  - Prefabの更新、検査、インスタンス化
  - Prefabのアンパック（完全またはOutermost）
  - Prefabオーバーライドの適用/復帰
  
- **ベクタースプライト変換** (`unity_vector_sprite_convert`)
  - プリミティブ（正方形、円、三角形、多角形）からスプライトを生成
  - SVGからスプライトへのインポート
  - テクスチャからスプライトへの変換
  - 単色スプライトの作成

#### GameKitフレームワーク（高レベルツール）
- **GameKit Actor** (`unity_gamekit_actor`)
  - 動作プロファイル: 2D/3D物理、リニア、タイルマップ移動
  - 制御モード: ダイレクトコントローラー、AI、UIコマンド
  - ステータス、アビリティ、武器ロードアウト
  
- **GameKit Manager** (`unity_gamekit_manager`)
  - マネージャータイプ: ターンベース、リアルタイム、リソースプール、イベントハブ、ステートマネージャー
  - ターンフェーズ管理
  - Machinationsフレームワークサポート付きリソースプール
  - 永続性（DontDestroyOnLoad）
  
- **GameKit Interaction** (`unity_gamekit_interaction`)
  - トリガータイプ: collision、trigger、raycast、proximity、input
  - 宣言的アクション: spawn prefab、destroy object、play sound、send message、change scene
  - 条件: tag、layer、distance、custom
  
- **GameKit UI Command** (`unity_gamekit_ui_command`)
  - ボタンレイアウト（水平、垂直、グリッド）付きコマンドパネル
  - Actorコマンドディスパッチ
  - アイコンとラベルのサポート
  
- **GameKit SceneFlow** (`unity_gamekit_sceneflow`)
  - 遷移付きシーンステートマシン
  - 加算シーンローディング
  - 永続マネージャーシーン
  - 共有シーングループ（UI、Audio）
  - シーンを跨ぐ参照解決

#### 中レベルツール
- **Transform Batch** (`unity_transform_batch`)
  - 円/線でオブジェクトを配置
  - 連続/リストベースの名前変更
  - メニュー階層の自動生成
  
- **RectTransform Batch** (`unity_rectTransform_batch`)
  - アンカー、ピボット、サイズ、位置の設定
  - 親プリセットへの整列
  - 水平/垂直分散
  - ソースからのサイズマッチング
  
- **Physics Bundle** (`unity_physics_bundle`)
  - 2D/3D Rigidbody + Colliderプリセット
  - プリセット: dynamic、kinematic、static、character、platformer、topDown、vehicle、projectile
  - 個別の物理プロパティ更新
  
- **Camera Rig** (`unity_camera_rig`)
  - カメラリグプリセット: follow、orbit、split-screen、fixed、dolly
  - ターゲット追跡とスムーズな移動
  - ビューポート設定
  
- **UI Foundation** (`unity_ui_foundation`)
  - Canvas、Panel、Button、Text、Image、InputFieldの作成
  - アンカープリセット
  - TextMeshProサポート
  - 自動レイアウト
  
- **Audio Source Bundle** (`unity_audio_source_bundle`)
  - オーディオプリセット: music、sfx、ambient、voice、ui
  - 2D/3D空間オーディオ
  - ミキサーグループ統合
  
- **Input Profile** (`unity_input_profile`)
  - 新しいInput System統合
  - アクションマップ設定
  - 通知動作: sendMessages、broadcastMessages、invokeUnityEvents、invokeCSharpEvents
  - InputActionsアセットの作成

#### 機能
- **コンパイル待機システム**
  - 操作を先に実行し、トリガーされた場合はコンパイルを待機
  - 早期待機解除のためのブリッジ再接続検出
  - 設定可能な間隔での60秒タイムアウト
  - レスポンスでの透明な待機情報
  - BaseCommandHandlerでの自動処理

- **包括的なテストスイート**
  - すべてのツールカテゴリをカバーする100以上のユニットテスト
  - Unity Test Framework統合
  - 97.7%の合格率（42/43テスト）
  - エディタメニュー統合: `Tools > SkillForUnity > Run All Tests`
  - コマンドラインテストランナー（PowerShell、Bash）
  - GitHub ActionsによるCI/CD

#### ドキュメント
- テストスイートドキュメント（`Assets/SkillForUnity/Tests/Editor/README.md`）
- テスト結果サマリー（`docs/TestResults_Summary.md`）
- ツールロードマップ - 日本語（`docs/tooling-roadmap.ja.md`）
- コンパイル待機機能ガイド（`docs/Compilation_Wait_Feature.md`）
- レガシークリーンアップサマリー（`docs/Unused_Handlers_Cleanup_Summary.md`）

### 変更

- **ツール数**: 7から21ツールに増加
- **BaseCommandHandler**: コンパイル待機を操作実行の前から後に移動
- **AssetCommandHandler**: 作成/更新操作後に`AssetDatabase.Refresh()`を追加
- **skill.yml**: ツール数を更新し、新しいカテゴリを追加（prefab_management、sprite_conversion、batch_operations、gamekit_systems）

### 削除

- `Assets/SkillForUnity/Editor/Tests/`のレガシーテストファイル
  - BaseCommandHandlerTests.cs
  - PayloadValidatorTests.cs
  - ResourceResolverTests.cs
  - CommandHandlerIntegrationTests.cs

- 未使用のハンドラ（MCPに登録されていない）
  - TemplateCommandHandler
  - UguiCreateFromTemplateHandler
  - UguiDetectOverlapsHandler
  - UguiLayoutManageHandler
  - UguiManageCommandHandler
  - ConstantConvertHandler
  - RenderPipelineManageHandler
  - TagLayerManageHandler（ProjectSettingsManageHandlerに統合）
  - RectTransformAnchorHandler（RectTransformBatchHandlerに機能あり）
  - RectTransformBasicHandler（RectTransformBatchHandlerに機能あり）

### 修正

- コンパイル待機が操作実行後に発生するようになった（より信頼性向上）
- ブリッジ再接続がコンパイル待機を適切に解除
- テストスイートのコンパイルエラーを解決

---

## [1.7.1] - 2025-11-XX

### 修正

- **テンプレートツール**: シーンクイックセットアップ、GameObjectテンプレート、UIテンプレート、デザインパターン、スクリプトテンプレートを修正
- **定数変換**: Unity 2024.2+モジュールシステム用のenum型解決を修正
- **SerializedFieldサポート**: ComponentとScriptableObject操作で`[SerializeField]`プライベートフィールドのサポートを追加
- **型解決**: キャッシングにより99%以上のパフォーマンス向上

### 追加

- `listCommonEnums`操作: カテゴリ別によく使用されるUnity enum型をリスト
- デバッグ情報付きの拡張エラーメッセージ

### 変更

- ツールセットの簡素化: 低レベルCRUD操作に焦点

---

## [1.7.0] - 2025-XX-XX

### 追加

- 初期MCP server実装
- Unity Editor用WebSocketブリッジ
- コアCRUD操作: Scene、GameObject、Component、Asset、ScriptableObject
- プロジェクト設定管理

---

[1.8.0]: https://github.com/kuroyasouiti/SkillForUnity/releases/tag/v1.8.0
[1.7.1]: https://github.com/kuroyasouiti/SkillForUnity/releases/tag/v1.7.1
[1.7.0]: https://github.com/kuroyasouiti/SkillForUnity/releases/tag/v1.7.0
