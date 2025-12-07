# TextMeshPro コンポーネントテスト ドキュメント

## 概要

このドキュメントは、UnityAI-ForgeのMCP componentManageツールを使用したTextMeshProコンポーネントのCRUD（Create, Read, Update, Delete）操作のテストについて説明します。

## 目的

TextMeshProは、Unityで高品質なテキストレンダリングを提供する重要なコンポーネントです。このテストスイートは、AIエージェントがMCPツールを通じてTextMeshProコンポーネントを正しく操作できることを保証します。

## テスト対象コンポーネント

### 1. TextMeshPro 3D (`TMPro.TextMeshPro`)

3D空間でテキストをレンダリングするコンポーネント。

**主な特徴:**
- 3Dシーンで使用
- 標準のTransformコンポーネントを使用
- ワールド座標でのテキスト配置

**使用例:**
- ゲーム内の看板やサイン
- 3D空間でのUIエレメント
- プレイヤーの頭上に表示される名前

### 2. TextMeshProUGUI (`TMPro.TextMeshProUGUI`)

UI Canvas上でテキストをレンダリングするコンポーネント。

**主な特徴:**
- UI Canvasで使用
- RectTransformコンポーネントを必要とする
- スクリーン座標でのテキスト配置

**使用例:**
- ゲームのメニュー
- HUD（ヘッドアップディスプレイ）
- ダイアログやポップアップ

## テスト構成

### テストファイル

- **場所**: `Assets/UnityAIForge/Tests/Editor/TextMeshProComponentTests.cs`
- **テストクラス**: `TextMeshProComponentTests`
- **テストフレームワーク**: NUnit
- **実行モード**: EditMode

### テストカテゴリー

#### 1. 基本的なCRUD操作テスト

##### Create（作成）
- **テスト**: `AddComponent_TextMeshPro_CreatesComponent`
  - TextMeshPro 3Dコンポーネントの追加をテスト
  - コンポーネントが正しく作成されることを検証

- **テスト**: `AddComponent_TextMeshProUGUI_CreatesComponent`
  - TextMeshProUGUIコンポーネントの追加をテスト
  - RectTransformの依存関係を処理

##### Read（読み取り）
- **テスト**: `InspectComponent_TextMeshPro_ReturnsComponentInfo`
  - コンポーネントのプロパティ情報取得をテスト
  - 正しいプロパティ値が返されることを検証

- **テスト**: `InspectComponent_TextMeshProUGUI_ReturnsComponentInfo`
  - UIコンポーネントの情報取得をテスト

##### Update（更新）
- **テスト**: `UpdateComponent_TextMeshPro_UpdatesText`
  - テキストやフォントサイズなどのプロパティ更新をテスト
  - 更新が正しく適用されることを検証

- **テスト**: `UpdateComponent_TextMeshProUGUI_UpdatesText`
  - UIテキストコンポーネントの更新をテスト
  - 色やサイズの変更を検証

##### Delete（削除）
- **テスト**: `RemoveComponent_TextMeshPro_RemovesComponent`
  - コンポーネントの削除をテスト
  - コンポーネントが完全に削除されることを検証

- **テスト**: `RemoveComponent_TextMeshProUGUI_RemovesComponent`
  - UIコンポーネントの削除をテスト

#### 2. 複数コンポーネント操作テスト

##### Batch Add（一括追加）
- **テスト**: `AddMultipleComponents_TextMeshPro_CreatesMultipleComponents`
  - パターンマッチングを使用した複数GameObjectへのコンポーネント追加
  - 初期プロパティの設定
  - 成功/失敗カウントの検証

##### Batch Read（一括読み取り）
- **テスト**: `InspectMultipleComponents_TextMeshPro_ReturnsMultipleComponentInfo`
  - 複数のコンポーネント情報を一度に取得
  - 各コンポーネントのプロパティを個別に検証

##### Batch Update（一括更新）
- **テスト**: `UpdateMultipleComponents_TextMeshPro_UpdatesMultipleComponents`
  - 複数のコンポーネントを一括で更新
  - すべてのコンポーネントが同じ値で更新されることを検証

##### Batch Delete（一括削除）
- **テスト**: `RemoveMultipleComponents_TextMeshPro_RemovesMultipleComponents`
  - パターンに一致するすべてのコンポーネントを削除
  - 削除の完全性を検証

#### 3. 高度なプロパティテスト

##### Advanced Properties（高度なプロパティ）
- **テスト**: `UpdateComponent_TextMeshPro_UpdatesAdvancedProperties`
  - 自動サイズ調整（Auto Sizing）の設定
  - フォントサイズの最小/最大値の設定
  - テキスト配置（Alignment）の設定

##### Property Filtering（プロパティフィルタリング）
- **テスト**: `InspectComponent_TextMeshPro_WithPropertyFilter`
  - 特定のプロパティのみを取得
  - レスポンスサイズの最適化

## MCPツールの使用方法

### componentManage ツール

UnityAI-Forgeの`componentManage`ツールは、MCPプロトコルを通じてコンポーネントのCRUD操作を実行します。

#### 基本構造

```csharp
var command = new McpIncomingCommand 
{ 
    ToolName = "componentManage", 
    Payload = payload 
};
var result = McpCommandProcessor.Execute(command);
```

#### 操作の詳細

##### 1. Add（追加）

**単一コンポーネント:**
```csharp
var payload = new Dictionary<string, object>
{
    ["operation"] = "add",
    ["gameObjectPath"] = "MyGameObject",
    ["componentType"] = "TMPro.TextMeshPro"
};
```

**複数コンポーネント（初期プロパティ付き）:**
```csharp
var payload = new Dictionary<string, object>
{
    ["operation"] = "addMultiple",
    ["pattern"] = "Text*",
    ["useRegex"] = false,
    ["componentType"] = "TMPro.TextMeshPro",
    ["propertyChanges"] = new Dictionary<string, object>
    {
        ["text"] = "Default Text",
        ["fontSize"] = 24.0
    }
};
```

##### 2. Update（更新）

**テキストとフォントサイズの更新:**
```csharp
var payload = new Dictionary<string, object>
{
    ["operation"] = "update",
    ["gameObjectPath"] = "MyGameObject",
    ["componentType"] = "TMPro.TextMeshPro",
    ["propertyChanges"] = new Dictionary<string, object>
    {
        ["text"] = "Updated Text",
        ["fontSize"] = 36.0,
        ["enableAutoSizing"] = true
    }
};
```

**色の更新:**
```csharp
var payload = new Dictionary<string, object>
{
    ["operation"] = "update",
    ["gameObjectPath"] = "MyGameObject",
    ["componentType"] = "TMPro.TextMeshProUGUI",
    ["propertyChanges"] = new Dictionary<string, object>
    {
        ["color"] = new Dictionary<string, object>
        {
            ["r"] = 1.0,
            ["g"] = 0.0,
            ["b"] = 0.0,
            ["a"] = 1.0
        }
    }
};
```

##### 3. Inspect（検査）

**すべてのプロパティを取得:**
```csharp
var payload = new Dictionary<string, object>
{
    ["operation"] = "inspect",
    ["gameObjectPath"] = "MyGameObject",
    ["componentType"] = "TMPro.TextMeshPro"
};
```

**特定のプロパティのみを取得:**
```csharp
var payload = new Dictionary<string, object>
{
    ["operation"] = "inspect",
    ["gameObjectPath"] = "MyGameObject",
    ["componentType"] = "TMPro.TextMeshPro",
    ["propertyFilter"] = new List<object> { "text", "fontSize", "color" }
};
```

##### 4. Remove（削除）

**単一コンポーネント:**
```csharp
var payload = new Dictionary<string, object>
{
    ["operation"] = "remove",
    ["gameObjectPath"] = "MyGameObject",
    ["componentType"] = "TMPro.TextMeshPro"
};
```

**複数コンポーネント:**
```csharp
var payload = new Dictionary<string, object>
{
    ["operation"] = "removeMultiple",
    ["pattern"] = "Text*",
    ["useRegex"] = false,
    ["componentType"] = "TMPro.TextMeshPro"
};
```

## よく使用されるTextMeshProプロパティ

### テキスト関連
- `text` (string): 表示するテキスト内容
- `richText` (bool): リッチテキストタグの有効化

### フォント関連
- `font` (TMP_FontAsset): 使用するフォントアセット
- `fontSize` (float): フォントサイズ
- `fontStyle` (FontStyles): フォントスタイル（Bold, Italic等）

### 自動サイズ調整
- `enableAutoSizing` (bool): 自動サイズ調整の有効化
- `fontSizeMin` (float): 最小フォントサイズ
- `fontSizeMax` (float): 最大フォントサイズ

### 外観
- `color` (Color): テキストの色
- `alpha` (float): 透明度

### レイアウト
- `alignment` (TextAlignmentOptions): テキストの配置
- `enableWordWrapping` (bool): 単語の折り返し
- `overflowMode` (TextOverflowModes): オーバーフローの処理方法

### マージン
- `margin` (Vector4): テキストのマージン

## テストの実行

### Unity Editorから実行

#### 方法1: メニューから実行
1. Unity Editorを開く
2. `Tools > SkillForUnity > Run TextMeshPro Tests` を選択
3. Console ウィンドウでテスト結果を確認

#### 方法2: Test Runner Windowから実行
1. `Tools > SkillForUnity > Open Test Runner Window` を選択
2. `EditMode` タブを選択
3. `TextMeshProComponentTests` を探す
4. 実行したいテストを選択して `Run Selected` をクリック

### コマンドラインから実行

```bash
# すべてのTextMeshProテストを実行
Unity.exe -runTests -testPlatform EditMode -testFilter "TextMeshProComponentTests" -projectPath "<プロジェクトパス>" -batchmode -quit

# 特定のテストのみを実行
Unity.exe -runTests -testPlatform EditMode -testFilter "TextMeshProComponentTests.AddComponent_TextMeshPro_CreatesComponent" -projectPath "<プロジェクトパス>" -batchmode -quit
```

### CI/CDパイプラインでの実行

#### GitHubアクションの例

```yaml
name: Run TextMeshPro Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - uses: game-ci/unity-test-runner@v2
        with:
          testMode: editmode
          testFilter: TextMeshProComponentTests
```

## 依存関係

### 必須パッケージ

1. **TextMeshPro**
   - パッケージ名: `com.unity.textmeshpro`
   - 最小バージョン: 3.0.0
   - インストール方法: Package Manager

2. **Unity Test Framework**
   - パッケージ名: `com.unity.test-framework`
   - 最小バージョン: 1.1.0
   - 自動的にインストールされる

### アセンブリ参照

`UnityAIForge.Tests.Editor.asmdef`は以下のアセンブリを参照する必要があります：

```json
{
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "UnityAIForge.GameKit.Runtime",
        "UnityAIForge.Editor.MCPBridge"
    ]
}
```

## トラブルシューティング

### よくある問題と解決方法

#### 1. TextMeshProが見つからない

**症状:**
```
TypeLoadException: Could not load type 'TMPro.TextMeshPro'
```

**解決方法:**
1. Package Managerを開く（`Window > Package Manager`）
2. "TextMeshPro" を検索
3. "Import" または "Install" をクリック
4. Unity を再起動

#### 2. RectTransformエラー

**症状:**
```
InvalidOperationException: Cannot add UI component to GameObject without RectTransform
```

**解決方法:**
TextMeshProUGUIを追加する前にRectTransformを追加：
```csharp
testGo.AddComponent<RectTransform>();
```

#### 3. MCPCommandProcessorにアクセスできない

**症状:**
```
CS0122: 'McpCommandProcessor' is inaccessible due to its protection level
```

**解決方法:**
`UnityAIForge.Editor.MCPBridge`がテストアセンブリの参照に含まれていることを確認。

#### 4. プロパティ更新が反映されない

**症状:**
テストは成功するが、実際にはプロパティが更新されていない。

**解決方法:**
- `EditorUtility.SetDirty(component)` が呼ばれているか確認
- プロパティ名のスペルが正しいか確認
- プロパティが public で書き込み可能か確認

## ベストプラクティス

### 1. テストの独立性

各テストは独立して実行できるようにする：
```csharp
[SetUp]
public void Setup()
{
    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    testGo = new GameObject("TestObject");
}

[TearDown]
public void Teardown()
{
    if (testGo != null)
    {
        Object.DestroyImmediate(testGo);
    }
}
```

### 2. リソースのクリーンアップ

テストで作成したすべてのGameObjectを確実に削除：
```csharp
try
{
    // テストコード
}
finally
{
    Object.DestroyImmediate(testGo2);
    Object.DestroyImmediate(testGo3);
}
```

### 3. 明確なテスト名

テストメソッド名は、テストの内容が分かるようにする：
- ❌ `Test1()`, `TestUpdate()`
- ✅ `UpdateComponent_TextMeshPro_UpdatesText()`, `AddComponent_TextMeshPro_CreatesComponent()`

### 4. Arrange-Act-Assert パターン

```csharp
[Test]
public void UpdateComponent_TextMeshPro_UpdatesText()
{
    // Arrange（準備）
    var payload = CreatePayload();
    
    // Act（実行）
    var result = McpCommandProcessor.Execute(command);
    
    // Assert（検証）
    Assert.IsNotNull(result);
    Assert.AreEqual(expectedValue, actualValue);
}
```

## パフォーマンス考慮事項

### 1. バッチ操作の使用

複数のコンポーネントを操作する場合は、バッチ操作を使用：
```csharp
// ❌ 非効率
for (int i = 0; i < 100; i++)
{
    var payload = CreateAddPayload($"Object{i}");
    McpCommandProcessor.Execute(new McpIncomingCommand { ... });
}

// ✅ 効率的
var payload = new Dictionary<string, object>
{
    ["operation"] = "addMultiple",
    ["pattern"] = "Object*",
    ["componentType"] = "TMPro.TextMeshPro"
};
McpCommandProcessor.Execute(new McpIncomingCommand { ... });
```

### 2. プロパティフィルタの使用

必要なプロパティのみを取得してレスポンスサイズを削減：
```csharp
var payload = new Dictionary<string, object>
{
    ["operation"] = "inspect",
    ["gameObjectPath"] = "MyObject",
    ["componentType"] = "TMPro.TextMeshPro",
    ["propertyFilter"] = new List<object> { "text", "fontSize" }
};
```

## セキュリティ考慮事項

### 1. 入力検証

MCPツールは入力を検証するため、テストでも検証動作を確認：
```csharp
[Test]
public void AddComponent_WithInvalidType_ThrowsException()
{
    var payload = new Dictionary<string, object>
    {
        ["operation"] = "add",
        ["gameObjectPath"] = "MyObject",
        ["componentType"] = "InvalidType.DoesNotExist"
    };
    
    Assert.Throws<InvalidOperationException>(() => 
        McpCommandProcessor.Execute(new McpIncomingCommand { ... })
    );
}
```

### 2. リソース制限

バッチ操作には最大結果数の制限がある：
```csharp
["maxResults"] = 1000  // デフォルト値
```

## 今後の拡張計画

### 短期（次のリリース）
- [ ] フォントアセットの変更テスト
- [ ] マテリアルプロパティの変更テスト
- [ ] オーバーフローモードの全バリエーションテスト

### 中期（3-6ヶ月）
- [ ] リッチテキストタグのテスト
- [ ] ローカライゼーションのテスト
- [ ] アニメーション効果のテスト

### 長期（6ヶ月以上）
- [ ] パフォーマンステスト
- [ ] ストレステスト（大量のテキストコンポーネント）
- [ ] インテグレーションテスト（他のシステムとの統合）

## 参考リンク

- [UnityAI-Forge ドキュメント](../../README.md)
- [MCPBridge 概要](../GETTING_STARTED.md)
- [テストフレームワーク ガイド](./README.md)
- [TextMeshPro 公式ドキュメント](http://digitalnativestudios.com/textmeshpro/docs/)
- [Unity Test Framework ドキュメント](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)

## 貢献

テストの追加や改善の提案は歓迎します：
1. Issues で新しいテストケースを提案
2. Pull Request でテストコードを提出
3. ドキュメントの改善提案

---

**最終更新日**: 2025-12-06  
**バージョン**: 1.0.0  
**著者**: UnityAI-Forge Team
