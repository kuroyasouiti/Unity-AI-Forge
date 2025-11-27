# McpCommandProcessor インターフェース抽出レポート

このドキュメントでは、McpCommandProcessorから抽出された共通パターンとインターフェース設計を定義します。

## 目的

- コードの構造を明確化
- 責任分離の原則に従った設計
- 将来的な拡張性の向上
- テスタビリティの改善

---

## 📊 現在の構造分析

### Partial Class構成

McpCommandProcessorは以下の12個のpartial classファイルに分割されています：

| ファイル | 行数 | 責務 | 主要メソッド |
|---------|------|------|-------------|
| `McpCommandProcessor.cs` | 138 | メインディスパッチャー | `Execute`, `HandlePing` |
| `Helpers.cs` | 1,293 | ヘルパーメソッド | `GetString`, `ResolveType`, `SerializeValue` |
| `Scene.cs` | 502 | シーン管理 | `HandleSceneManage`, `CreateScene`, `LoadScene` |
| `GameObject.cs` | 430 | GameObject管理 | `HandleGameObjectManage`, `CreateGameObject` |
| `Component.cs` | 634 | コンポーネント管理 | `HandleComponentManage`, `AddComponent` |
| `Asset.cs` | 465 | アセット管理 | `HandleAssetManage`, `CreateTextAsset` |
| `ScriptableObject.cs` | 496 | ScriptableObject管理 | `HandleScriptableObjectManage` |
| `UI.cs` | 2,081 | UI管理 | `HandleUguiManage`, `HandleUguiRectAdjust` |
| `Prefab.cs` | - | Prefab管理 | `HandlePrefabManage`, `CreatePrefab` |
| `Settings.cs` | 1,679 | プロジェクト設定 | `HandleProjectSettingsManage` |
| `Utilities.cs` | 241 | ユーティリティ | `HandleContextInspect` |
| `Template.cs` | - | テンプレート生成 | `HandleDesignPatternGenerate` |

**合計**: 約8,000行のコード

---

## 🔍 抽出された共通パターン

### パターン1: コマンドハンドラー

全てのカテゴリは同じディスパッチパターンに従います：

```csharp
private static object Handle{Category}Manage(Dictionary<string, object> payload)
{
    // 1. 操作の取得とバリデーション
    var operation = GetString(payload, "operation");
    if (string.IsNullOrEmpty(operation))
    {
        throw new InvalidOperationException("operation is required");
    }

    // 2. コンパイル待機（必要な場合）
    Dictionary<string, object> compilationWaitInfo = null;
    if (/* 書き込み操作の場合 */)
    {
        compilationWaitInfo = EnsureNoCompilationInProgress("categoryName", maxWaitSeconds: 30f);
    }

    // 3. 操作のディスパッチ
    object result = operation switch
    {
        "create" => CreateOperation(payload),
        "delete" => DeleteOperation(payload),
        "update" => UpdateOperation(payload),
        "inspect" => InspectOperation(payload),
        // ...
        _ => throw new InvalidOperationException($"Unknown operation: {operation}"),
    };

    // 4. コンパイル待機情報の追加
    if (compilationWaitInfo != null && result is Dictionary<string, object> resultDict)
    {
        resultDict["compilationWait"] = compilationWaitInfo;
    }

    return result;
}
```

### パターン2: CRUD操作

ほとんどのカテゴリは以下のCRUD操作をサポートします：

| 操作 | メソッド命名 | 戻り値 |
|------|-------------|--------|
| Create | `Create{Entity}` | `{ success: true, path: "...", ... }` |
| Read/Inspect | `Inspect{Entity}` | `{ success: true, data: {...} }` |
| Update | `Update{Entity}` | `{ success: true, updated: [...] }` |
| Delete | `Delete{Entity}` | `{ success: true, message: "..." }` |
| Duplicate | `Duplicate{Entity}` | `{ success: true, newPath: "..." }` |
| List | `List{Entities}` | `{ success: true, items: [...] }` |

### パターン3: バッチ操作

バッチ操作は一貫した命名とシグネチャに従います：

```csharp
private static object {Operation}Multiple{Entities}(Dictionary<string, object> payload)
{
    var pattern = GetString(payload, "pattern");
    var useRegex = GetBool(payload, "useRegex");
    var maxResults = GetInt(payload, "maxResults", 1000);
    
    // パターンマッチング
    var matchedItems = FindItemsByPattern(pattern, useRegex, maxResults);
    
    // 各アイテムに操作を適用
    var results = new List<object>();
    foreach (var item in matchedItems)
    {
        try
        {
            var itemResult = PerformOperation(item);
            results.Add(itemResult);
        }
        catch (Exception ex)
        {
            if (stopOnError) throw;
            results.Add(new { success = false, error = ex.Message });
        }
    }
    
    return new Dictionary<string, object>
    {
        ["success"] = true,
        ["results"] = results
    };
}
```

### パターン4: ヘルパーメソッド

共通のヘルパーメソッドは一貫したシグネチャを持ちます：

```csharp
// ペイロード取得
private static string GetString(Dictionary<string, object> payload, string key)
private static string GetString(Dictionary<string, object> payload, string key, string defaultValue)
private static bool GetBool(Dictionary<string, object> payload, string key)
private static int GetInt(Dictionary<string, object> payload, string key, int defaultValue)
private static float GetFloat(Dictionary<string, object> payload, string key, float defaultValue)
private static List<object> GetList(Dictionary<string, object> payload, string key)

// リソース解決
private static Type ResolveType(string typeName)
private static GameObject ResolveGameObject(string path)
private static string ResolveAssetPath(string path)

// バリデーション
private static string EnsureValue(string value, string paramName)
private static void ValidateAssetPath(string path)
private static void EnsureDirectoryExists(string filePath)
```

---

## 🎯 提案されるインターフェース設計

### 1. ICommandHandler インターフェース

```csharp
/// <summary>
/// コマンドハンドラーの基本インターフェース
/// </summary>
public interface ICommandHandler
{
    /// <summary>
    /// コマンドを実行します
    /// </summary>
    /// <param name="payload">コマンドパラメータ</param>
    /// <returns>実行結果</returns>
    object Execute(Dictionary<string, object> payload);
    
    /// <summary>
    /// ハンドラーが対応している操作のリスト
    /// </summary>
    IEnumerable<string> SupportedOperations { get; }
    
    /// <summary>
    /// ハンドラーのカテゴリ名
    /// </summary>
    string Category { get; }
}
```

### 2. IOperationHandler インターフェース

```csharp
/// <summary>
/// 個別の操作ハンドラー
/// </summary>
public interface IOperationHandler
{
    /// <summary>
    /// 操作名
    /// </summary>
    string OperationName { get; }
    
    /// <summary>
    /// 操作を実行
    /// </summary>
    object Execute(Dictionary<string, object> payload);
    
    /// <summary>
    /// 読み取り専用操作かどうか
    /// </summary>
    bool IsReadOnly { get; }
    
    /// <summary>
    /// 必須パラメータのリスト
    /// </summary>
    IEnumerable<string> RequiredParameters { get; }
}
```

### 3. IPayloadValidator インターフェース

```csharp
/// <summary>
/// ペイロードバリデーター
/// </summary>
public interface IPayloadValidator
{
    /// <summary>
    /// ペイロードをバリデート
    /// </summary>
    /// <param name="payload">バリデート対象のペイロード</param>
    /// <param name="operation">操作名</param>
    /// <returns>バリデーション結果</returns>
    ValidationResult Validate(Dictionary<string, object> payload, string operation);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }
    public Dictionary<string, object> NormalizedPayload { get; set; }
}
```

### 4. IResourceResolver インターフェース

```csharp
/// <summary>
/// リソース解決インターフェース
/// </summary>
public interface IResourceResolver<T>
{
    /// <summary>
    /// リソースを解決
    /// </summary>
    /// <param name="identifier">識別子（パス、GUID、名前など）</param>
    /// <returns>解決されたリソース</returns>
    T Resolve(string identifier);
    
    /// <summary>
    /// リソースが存在するかチェック
    /// </summary>
    bool Exists(string identifier);
}
```

---

## 🏗️ 提案される実装構造

### オプション1: ファクトリーパターン

```csharp
public class CommandHandlerFactory
{
    private static readonly Dictionary<string, ICommandHandler> _handlers = new()
    {
        ["sceneManage"] = new SceneCommandHandler(),
        ["gameObjectManage"] = new GameObjectCommandHandler(),
        ["componentManage"] = new ComponentCommandHandler(),
        // ...
    };
    
    public static ICommandHandler GetHandler(string toolName)
    {
        if (!_handlers.ContainsKey(toolName))
        {
            throw new InvalidOperationException($"Unknown tool: {toolName}");
        }
        return _handlers[toolName];
    }
}
```

### オプション2: ストラテジーパターン

```csharp
public abstract class BaseCommandHandler : ICommandHandler
{
    protected abstract string CategoryName { get; }
    
    public object Execute(Dictionary<string, object> payload)
    {
        // 共通の前処理
        var operation = ValidateAndGetOperation(payload);
        var compilationWaitInfo = EnsureNoCompilationIfNeeded(operation);
        
        // 個別の処理
        var result = ExecuteOperation(operation, payload);
        
        // 共通の後処理
        AddCompilationWaitInfo(result, compilationWaitInfo);
        
        return result;
    }
    
    protected abstract object ExecuteOperation(string operation, Dictionary<string, object> payload);
}

public class SceneCommandHandler : BaseCommandHandler
{
    protected override string CategoryName => "scene";
    
    protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
    {
        return operation switch
        {
            "create" => CreateScene(payload),
            "load" => LoadScene(payload),
            // ...
        };
    }
}
```

### オプション3: コマンドパターン

```csharp
public interface ICommand
{
    object Execute();
}

public class CreateGameObjectCommand : ICommand
{
    private readonly Dictionary<string, object> _payload;
    
    public CreateGameObjectCommand(Dictionary<string, object> payload)
    {
        _payload = payload;
    }
    
    public object Execute()
    {
        // GameObject作成ロジック
    }
}

public class CommandInvoker
{
    public object Invoke(ICommand command)
    {
        return command.Execute();
    }
}
```

---

## 📈 リファクタリング計画

### Phase 1: インターフェース定義（1週間）

- [ ] 基本インターフェースの定義
- [ ] 既存コードへの適用計画の策定
- [ ] ユニットテストの作成

### Phase 2: ベースクラスの実装（2週間）

- [ ] `BaseCommandHandler` クラスの実装
- [ ] 共通ロジックの抽出
- [ ] ヘルパーメソッドのリファクタリング

### Phase 3: 個別ハンドラーの移行（4週間）

- [ ] SceneCommandHandler の実装
- [ ] GameObjectCommandHandler の実装
- [ ] ComponentCommandHandler の実装
- [ ] AssetCommandHandler の実装
- [ ] その他のハンドラーの実装

### Phase 4: ファクトリーとディスパッチャーの実装（1週間）

- [ ] CommandHandlerFactory の実装
- [ ] メインディスパッチャーのリファクタリング
- [ ] 統合テストの実行

### Phase 5: ドキュメントとクリーンアップ（1週間）

- [ ] APIドキュメントの更新
- [ ] コードレビュー
- [ ] パフォーマンステスト

**合計所要時間**: 約9週間

---

## 🎯 期待される効果

### 1. コードの可読性向上

**現在**:
```csharp
// 9,000行以上の単一のpartial class
internal static partial class McpCommandProcessor
{
    // 複雑な switch文とif/else
}
```

**リファクタリング後**:
```csharp
// 明確な責任分離
public class SceneCommandHandler : BaseCommandHandler
{
    // シーン管理のみに集中
}

public class GameObjectCommandHandler : BaseCommandHandler
{
    // GameObject管理のみに集中
}
```

### 2. テスタビリティの向上

```csharp
// ユニットテスト例
[Test]
public void TestSceneCreation()
{
    var handler = new SceneCommandHandler();
    var payload = new Dictionary<string, object>
    {
        ["operation"] = "create",
        ["scenePath"] = "Assets/Test.unity"
    };
    
    var result = handler.Execute(payload);
    
    Assert.IsTrue((bool)result["success"]);
}
```

### 3. 拡張性の向上

```csharp
// 新しいハンドラーの追加が容易
public class AnimationCommandHandler : BaseCommandHandler
{
    protected override string CategoryName => "animation";
    
    protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
    {
        // アニメーション操作の実装
    }
}

// ファクトリーに登録するだけ
CommandHandlerFactory.Register("animationManage", new AnimationCommandHandler());
```

### 4. パフォーマンスの最適化

- ハンドラーの遅延初期化
- キャッシュ戦略の適用
- 並列処理の実装

---

## 🚧 リファクタリングのリスクと対策

### リスク1: 既存機能の破壊

**対策**:
- 段階的なリファクタリング
- 包括的なテストスイートの作成
- 既存のAPIを維持したままの内部リファクタリング

### リスク2: パフォーマンスの低下

**対策**:
- パフォーマンステストの実施
- プロファイリングによる最適化
- 必要に応じてインライン化

### リスク3: チーム学習コスト

**対策**:
- 詳細なドキュメント作成
- コードレビューセッション
- ペアプログラミング

---

## 📊 メトリクス

### 現在のコード品質指標

| メトリクス | 値 | 評価 |
|-----------|-----|------|
| 循環的複雑度 | 高 | ⚠️ 要改善 |
| 結合度 | 高 | ⚠️ 要改善 |
| 凝集度 | 中 | 🔸 改善可能 |
| テストカバレッジ | 低 (< 20%) | ❌ 改善必須 |
| コード重複 | 中 | 🔸 改善可能 |

### 目標指標

| メトリクス | 目標値 | 優先度 |
|-----------|--------|--------|
| 循環的複雑度 | 低-中 | 高 |
| 結合度 | 低 | 高 |
| 凝集度 | 高 | 高 |
| テストカバレッジ | > 70% | 高 |
| コード重複 | 低 | 中 |

---

## 🔗 関連ドキュメント

- [REFACTORING_PLAN.md](./REFACTORING_PLAN.md) - 以前のリファクタリング計画
- [TOOLS_REVIEW.md](./TOOLS_REVIEW.md) - ツールレビュー
- [ERROR_CODE_GUIDELINES.md](./ERROR_CODE_GUIDELINES.md) - エラーコード標準
- [PERFORMANCE_METRICS_GUIDELINES.md](./PERFORMANCE_METRICS_GUIDELINES.md) - パフォーマンスメトリクス

---

## 📝 結論

McpCommandProcessorのコードベースは、明確な共通パターンに従っています。これらのパターンをインターフェースとして抽出することで、以下の改善が期待できます：

1. **保守性の向上**: 明確な責任分離
2. **テスタビリティの向上**: 独立したユニットテスト
3. **拡張性の向上**: 新機能の追加が容易
4. **可読性の向上**: 意図が明確なコード

ただし、リファクタリングは段階的に行い、既存機能を壊さないよう慎重に進める必要があります。

---

**次のステップ**: このドキュメントをチームでレビューし、リファクタリング計画について合意を得る。


