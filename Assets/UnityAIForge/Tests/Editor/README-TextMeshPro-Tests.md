# TextMeshPro Component Tests

このドキュメントは、UnityAI-ForgeのコンポーネントツールでTextMeshProのCRUD操作をテストする方法を説明します。

## 概要

`TextMeshProComponentTests.cs`は、MCPのcomponentManageツールを使用してTextMeshProコンポーネント（3DとUI）のCRUD操作をテストするテストクラスです。

## テスト対象

### TextMeshPro 3D (`TMPro.TextMeshPro`)
- 3Dシーンでのテキストレンダリング
- 標準のTransformコンポーネントを使用

### TextMeshProUGUI (`TMPro.TextMeshProUGUI`)
- UI Canvasでのテキストレンダリング
- RectTransformコンポーネントを必要とする

## テストケース

### 基本的なCRUD操作

#### Create (作成)
- `AddComponent_TextMeshPro_CreatesComponent`: TextMeshPro 3Dコンポーネントの追加
- `AddComponent_TextMeshProUGUI_CreatesComponent`: TextMeshProUGUIコンポーネントの追加

#### Read (読み取り)
- `InspectComponent_TextMeshPro_ReturnsComponentInfo`: TextMeshPro 3Dの情報取得
- `InspectComponent_TextMeshProUGUI_ReturnsComponentInfo`: TextMeshProUGUIの情報取得
- `InspectComponent_TextMeshPro_WithPropertyFilter`: プロパティフィルタを使用した情報取得

#### Update (更新)
- `UpdateComponent_TextMeshPro_UpdatesText`: TextMeshPro 3Dのプロパティ更新
- `UpdateComponent_TextMeshProUGUI_UpdatesText`: TextMeshProUGUIのプロパティ更新
- `UpdateComponent_TextMeshPro_UpdatesAdvancedProperties`: 高度なプロパティの更新

#### Delete (削除)
- `RemoveComponent_TextMeshPro_RemovesComponent`: TextMeshPro 3Dコンポーネントの削除
- `RemoveComponent_TextMeshProUGUI_RemovesComponent`: TextMeshProUGUIコンポーネントの削除

### 複数コンポーネント操作

#### Batch Create (一括作成)
- `AddMultipleComponents_TextMeshPro_CreatesMultipleComponents`: 複数のGameObjectにTextMeshProを一括追加

#### Batch Read (一括読み取り)
- `InspectMultipleComponents_TextMeshPro_ReturnsMultipleComponentInfo`: 複数のTextMeshProコンポーネントの情報を一括取得

#### Batch Update (一括更新)
- `UpdateMultipleComponents_TextMeshPro_UpdatesMultipleComponents`: 複数のTextMeshProコンポーネントを一括更新

#### Batch Delete (一括削除)
- `RemoveMultipleComponents_TextMeshPro_RemovesMultipleComponents`: 複数のTextMeshProコンポーネントを一括削除

## テストの実行方法

### Unity Editorから実行

1. **Test Runnerウィンドウを開く**:
   - `Tools > SkillForUnity > Open Test Runner Window`
   - または `Window > General > Test Runner`

2. **TextMeshProテストを実行**:
   - EditModeタブを選択
   - `TextMeshProComponentTests`を探して実行

### コマンドラインから実行

```bash
# すべてのEditModeテストを実行
Unity.exe -runTests -testPlatform EditMode -projectPath <プロジェクトパス>

# TextMeshProテストのみを実行
Unity.exe -runTests -testPlatform EditMode -testFilter "TextMeshProComponentTests" -projectPath <プロジェクトパス>
```

## テスト対象のMCP操作

### componentManage ツール

UnityAI-Forgeの`componentManage`ツールは以下の操作をサポートしています：

#### 単一コンポーネント操作

1. **add**: コンポーネントの追加
   ```json
   {
     "operation": "add",
     "gameObjectPath": "TestObject",
     "componentType": "TMPro.TextMeshPro"
   }
   ```

2. **update**: コンポーネントのプロパティ更新
   ```json
   {
     "operation": "update",
     "gameObjectPath": "TestObject",
     "componentType": "TMPro.TextMeshPro",
     "propertyChanges": {
       "text": "Hello World",
       "fontSize": 36.0
     }
   }
   ```

3. **inspect**: コンポーネントの情報取得
   ```json
   {
     "operation": "inspect",
     "gameObjectPath": "TestObject",
     "componentType": "TMPro.TextMeshPro"
   }
   ```

4. **remove**: コンポーネントの削除
   ```json
   {
     "operation": "remove",
     "gameObjectPath": "TestObject",
     "componentType": "TMPro.TextMeshPro"
   }
   ```

#### 複数コンポーネント操作

1. **addMultiple**: 複数のGameObjectにコンポーネントを一括追加
   ```json
   {
     "operation": "addMultiple",
     "pattern": "TextObject*",
     "useRegex": false,
     "componentType": "TMPro.TextMeshPro",
     "propertyChanges": {
       "text": "Default Text"
     }
   }
   ```

2. **updateMultiple**: 複数のコンポーネントを一括更新
   ```json
   {
     "operation": "updateMultiple",
     "pattern": "TextObject*",
     "useRegex": false,
     "componentType": "TMPro.TextMeshPro",
     "propertyChanges": {
       "fontSize": 24.0
     }
   }
   ```

3. **inspectMultiple**: 複数のコンポーネントの情報を一括取得
   ```json
   {
     "operation": "inspectMultiple",
     "pattern": "TextObject*",
     "useRegex": false,
     "componentType": "TMPro.TextMeshPro"
   }
   ```

4. **removeMultiple**: 複数のコンポーネントを一括削除
   ```json
   {
     "operation": "removeMultiple",
     "pattern": "TextObject*",
     "useRegex": false,
     "componentType": "TMPro.TextMeshPro"
   }
   ```

## TextMeshProの主要プロパティ

### テストでよく使用されるプロパティ

- `text` (string): 表示するテキスト
- `fontSize` (float): フォントサイズ
- `color` (Color): テキストの色
- `enableAutoSizing` (bool): 自動サイズ調整の有効化
- `fontSizeMin` (float): 最小フォントサイズ
- `fontSizeMax` (float): 最大フォントサイズ
- `alignment` (TextAlignmentOptions): テキストの配置

## 依存関係

### 必須パッケージ

- TextMeshPro (com.unity.textmeshpro)
- Unity Test Framework (com.unity.test-framework)

### アセンブリ参照

- `UnityAIForge.Editor.MCPBridge`: MCPコマンドプロセッサ
- `MCP.Editor`: MCPエディター機能
- `MCP.Editor.Base`: MCP基本クラス

## トラブルシューティング

### TextMeshProが見つからない

TextMeshProパッケージがインストールされていない場合：
1. `Window > Package Manager`を開く
2. `TextMeshPro`を検索してインストール
3. Unityを再起動

### RectTransformエラー

TextMeshProUGUIを使用する際は、事前にRectTransformコンポーネントが必要です：
```csharp
testGo.AddComponent<RectTransform>();
```

### コンパイルエラー

MCPBridgeアセンブリが参照されていない場合：
1. `UnityAIForge.Tests.Editor.asmdef`を確認
2. `UnityAIForge.Editor.MCPBridge`が参照に含まれているか確認

## テストカバレッジ

- ✅ TextMeshPro 3Dの基本CRUD操作
- ✅ TextMeshProUGUIの基本CRUD操作
- ✅ 複数コンポーネントの一括操作
- ✅ プロパティフィルタを使用した情報取得
- ✅ 高度なプロパティの更新

## 今後の拡張

- [ ] フォントアセットの動的変更テスト
- [ ] マテリアルプロパティの変更テスト
- [ ] リッチテキストタグのテスト
- [ ] オーバーフローモードのテスト
- [ ] ローカライゼーションのテスト

## 関連ドキュメント

- [MCPBridge概要](../../Documentation/README.md)
- [テストガイド](../../Documentation/Testing/README.md)
- [TextMeshPro公式ドキュメント](http://digitalnativestudios.com/textmeshpro/docs/)
