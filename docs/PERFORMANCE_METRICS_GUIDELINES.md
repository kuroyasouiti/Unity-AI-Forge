# パフォーマンスメトリクス実装ガイドライン

SkillForUnityの各操作にパフォーマンスメトリクスを追加するためのガイドラインです。

## 目的

- **パフォーマンスの可視化**: 各操作にかかる時間を計測
- **ボトルネックの特定**: 遅い操作を識別
- **最適化の検証**: 改善効果の測定
- **ユーザー体験の向上**: 処理時間の予測と通知

---

## メトリクスの種類

### 1. 実行時間メトリクス

各操作の実行時間を測定します。

```csharp
// C# 実装例
using System.Diagnostics;

public static object Execute(McpIncomingCommand command)
{
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        object result = command.ToolName switch
        {
            "pingUnityEditor" => HandlePing(),
            "sceneManage" => HandleSceneManage(command.Payload),
            // ... 他の操作
        };
        
        stopwatch.Stop();
        
        // 結果にメトリクスを追加
        if (result is Dictionary<string, object> dict)
        {
            dict["_metrics"] = new Dictionary<string, object>
            {
                ["executionTimeMs"] = stopwatch.ElapsedMilliseconds,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
        
        return result;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        // エラーでもメトリクスを記録
        throw;
    }
}
```

### 2. 操作カウントメトリクス

バッチ操作で処理したアイテム数を記録します。

```csharp
// C# 実装例
private static object HandleBatchOperation(Dictionary<string, object> payload)
{
    int processedCount = 0;
    int successCount = 0;
    int failureCount = 0;
    
    // バッチ処理
    foreach (var item in items)
    {
        processedCount++;
        try
        {
            ProcessItem(item);
            successCount++;
        }
        catch
        {
            failureCount++;
        }
    }
    
    return new Dictionary<string, object>
    {
        ["success"] = true,
        ["_metrics"] = new Dictionary<string, object>
        {
            ["itemsProcessed"] = processedCount,
            ["itemsSucceeded"] = successCount,
            ["itemsFailed"] = failureCount
        }
    };
}
```

### 3. リソース使用量メトリクス

メモリ使用量や Unity Editor の状態を記録します。

```csharp
// C# 実装例
private static Dictionary<string, object> GetResourceMetrics()
{
    return new Dictionary<string, object>
    {
        ["memoryUsedMB"] = (GC.GetTotalMemory(false) / 1024.0 / 1024.0),
        ["editorVersion"] = Application.unityVersion,
        ["isPlaying"] = Application.isPlaying,
        ["frameCount"] = Time.frameCount
    };
}
```

---

## レスポンス構造

### 標準レスポンスフォーマット

```json
{
  "success": true,
  "operation": "create",
  "result": {
    "gameObjectPath": "Player",
    "instanceId": 12345
  },
  "_metrics": {
    "executionTimeMs": 127,
    "timestamp": 1234567890123,
    "category": "gameobject",
    "operation": "create"
  }
}
```

### バッチ操作のレスポンス

```json
{
  "success": true,
  "operation": "addMultiple",
  "results": [...],
  "_metrics": {
    "executionTimeMs": 1523,
    "timestamp": 1234567890123,
    "category": "component",
    "operation": "addMultiple",
    "itemsProcessed": 50,
    "itemsSucceeded": 48,
    "itemsFailed": 2,
    "averageTimePerItemMs": 30.46
  }
}
```

### エラー時のレスポンス

```json
{
  "success": false,
  "error": {
    "code": "GOBJ_NOT_FOUND_001",
    "message": "GameObject not found: 'Player'"
  },
  "_metrics": {
    "executionTimeMs": 5,
    "timestamp": 1234567890123,
    "category": "gameobject",
    "operation": "inspect",
    "failedAt": "resolution"
  }
}
```

---

## 実装パターン

### パターン1: メトリクスヘルパークラス

```csharp
/// <summary>
/// パフォーマンスメトリクスを収集するヘルパークラス
/// </summary>
public class PerformanceMetrics
{
    private readonly Stopwatch _stopwatch;
    private readonly string _category;
    private readonly string _operation;
    private int _itemsProcessed;
    private int _itemsSucceeded;
    private int _itemsFailed;

    public PerformanceMetrics(string category, string operation)
    {
        _category = category;
        _operation = operation;
        _stopwatch = Stopwatch.StartNew();
    }

    public void IncrementProcessed() => _itemsProcessed++;
    public void IncrementSucceeded() => _itemsSucceeded++;
    public void IncrementFailed() => _itemsFailed++;

    public Dictionary<string, object> ToDict()
    {
        _stopwatch.Stop();
        
        var metrics = new Dictionary<string, object>
        {
            ["executionTimeMs"] = _stopwatch.ElapsedMilliseconds,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["category"] = _category,
            ["operation"] = _operation
        };

        if (_itemsProcessed > 0)
        {
            metrics["itemsProcessed"] = _itemsProcessed;
            metrics["itemsSucceeded"] = _itemsSucceeded;
            metrics["itemsFailed"] = _itemsFailed;
            metrics["averageTimePerItemMs"] = 
                (double)_stopwatch.ElapsedMilliseconds / _itemsProcessed;
        }

        return metrics;
    }
}
```

### 使用例

```csharp
public static object HandleGameObjectManage(Dictionary<string, object> payload)
{
    var metrics = new PerformanceMetrics("gameobject", GetString(payload, "operation"));
    
    try
    {
        string operation = GetString(payload, "operation");
        
        var result = operation switch
        {
            "create" => CreateGameObject(payload, metrics),
            "delete" => DeleteGameObject(payload, metrics),
            // ...
        };
        
        if (result is Dictionary<string, object> dict)
        {
            dict["_metrics"] = metrics.ToDict();
        }
        
        return result;
    }
    catch (Exception ex)
    {
        // エラーでもメトリクスを返す
        throw new InvalidOperationException(
            ex.Message + $" | Metrics: {JsonUtility.ToJson(metrics.ToDict())}"
        );
    }
}
```

### パターン2: using ステートメントを使用

```csharp
/// <summary>
/// IDisposable を実装したメトリクス収集クラス
/// </summary>
public class MetricsScope : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly Dictionary<string, object> _result;

    public MetricsScope(Dictionary<string, object> result)
    {
        _result = result;
        _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _result["_metrics"] = new Dictionary<string, object>
        {
            ["executionTimeMs"] = _stopwatch.ElapsedMilliseconds,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }
}
```

### 使用例

```csharp
public static object HandlePing()
{
    var result = new Dictionary<string, object>
    {
        ["editor"] = Application.unityVersion,
        ["project"] = Application.productName,
        ["time"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    };
    
    using (new MetricsScope(result))
    {
        // 処理（メトリクスは自動的に記録される）
    }
    
    return result;
}
```

---

## Python側の実装

### メトリクスの抽出と表示

```python
async def _call_bridge_tool(tool_name: str, args: dict) -> list[types.Content]:
    """ブリッジツールを呼び出し、メトリクスを抽出して表示"""
    
    _ensure_bridge_connected()
    
    start_time = time.time()
    bridge_response = await bridge_manager.send_command(tool_name, args)
    elapsed_ms = (time.time() - start_time) * 1000
    
    # メトリクスを抽出
    metrics = bridge_response.get("_metrics", {})
    
    # メトリクスをログに記録
    if metrics:
        logger.info(
            f"{tool_name} completed in {metrics.get('executionTimeMs', 0)}ms "
            f"(total with network: {elapsed_ms:.0f}ms)"
        )
        
        # バッチ操作の場合、追加情報を表示
        if "itemsProcessed" in metrics:
            logger.info(
                f"Processed {metrics['itemsProcessed']} items: "
                f"{metrics['itemsSucceeded']} succeeded, "
                f"{metrics['itemsFailed']} failed"
            )
    
    # ユーザーへのレスポンス（メトリクスは内部情報なので除外）
    response_copy = dict(bridge_response)
    response_copy.pop("_metrics", None)
    
    return [types.TextContent(type="text", text=as_pretty_json(response_copy))]
```

### メトリクスの集計

```python
class MetricsCollector:
    """メトリクスを収集して統計を取るクラス"""
    
    def __init__(self):
        self._metrics = []
    
    def add(self, tool_name: str, metrics: dict):
        """メトリクスを追加"""
        self._metrics.append({
            "tool": tool_name,
            "timestamp": metrics.get("timestamp"),
            "executionTimeMs": metrics.get("executionTimeMs"),
            "category": metrics.get("category"),
            "operation": metrics.get("operation")
        })
    
    def get_stats(self, tool_name: str = None) -> dict:
        """統計情報を取得"""
        filtered = self._metrics
        if tool_name:
            filtered = [m for m in self._metrics if m["tool"] == tool_name]
        
        if not filtered:
            return {}
        
        times = [m["executionTimeMs"] for m in filtered]
        return {
            "count": len(times),
            "totalMs": sum(times),
            "averageMs": sum(times) / len(times),
            "minMs": min(times),
            "maxMs": max(times)
        }

# グローバルインスタンス
metrics_collector = MetricsCollector()
```

---

## メトリクスの活用

### 1. パフォーマンスダッシュボード

```python
def print_performance_dashboard():
    """パフォーマンスダッシュボードを表示"""
    
    print("=== SkillForUnity Performance Dashboard ===")
    print()
    
    for tool in ["gameobject_crud", "component_crud", "asset_crud"]:
        stats = metrics_collector.get_stats(tool)
        if stats:
            print(f"{tool}:")
            print(f"  Calls: {stats['count']}")
            print(f"  Avg: {stats['averageMs']:.1f}ms")
            print(f"  Min: {stats['minMs']}ms")
            print(f"  Max: {stats['maxMs']}ms")
            print()
```

### 2. 遅い操作の警告

```python
SLOW_OPERATION_THRESHOLD_MS = 1000

def check_slow_operations(metrics: dict, tool_name: str):
    """遅い操作を検出して警告"""
    
    execution_time = metrics.get("executionTimeMs", 0)
    
    if execution_time > SLOW_OPERATION_THRESHOLD_MS:
        logger.warning(
            f"Slow operation detected: {tool_name} took {execution_time}ms. "
            f"Consider using pagination or filtering to improve performance."
        )
```

### 3. 自動最適化の提案

```python
def suggest_optimizations(metrics: dict, args: dict) -> list[str]:
    """メトリクスに基づいて最適化を提案"""
    
    suggestions = []
    
    # バッチ操作で大量のアイテムを処理している場合
    items_processed = metrics.get("itemsProcessed", 0)
    if items_processed > 500:
        suggestions.append(
            f"Processing {items_processed} items may be slow. "
            f"Consider using maxResults parameter to limit batch size."
        )
    
    # includeProperties=true で時間がかかっている場合
    if args.get("includeProperties") and metrics.get("executionTimeMs", 0) > 500:
        suggestions.append(
            "Setting includeProperties=false may improve performance "
            "if you don't need detailed property information."
        )
    
    return suggestions
```

---

## メトリクスのベストプラクティス

### 1. 最小限のオーバーヘッド

```csharp
// 悪い例：毎回メモリを計測（遅い）
var metrics = new Dictionary<string, object>
{
    ["executionTimeMs"] = stopwatch.ElapsedMilliseconds,
    ["memoryUsedMB"] = GC.GetTotalMemory(false) / 1024.0 / 1024.0, // 遅い！
};

// 良い例：必要な情報のみ
var metrics = new Dictionary<string, object>
{
    ["executionTimeMs"] = stopwatch.ElapsedMilliseconds,
    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};
```

### 2. 構造化されたメトリクス

```csharp
// 悪い例：フラットな構造
var metrics = new Dictionary<string, object>
{
    ["time"] = 100,
    ["items"] = 50,
    ["success"] = 48
};

// 良い例：意味のある名前と構造
var metrics = new Dictionary<string, object>
{
    ["executionTimeMs"] = 100,
    ["batch"] = new Dictionary<string, object>
    {
        ["itemsProcessed"] = 50,
        ["itemsSucceeded"] = 48,
        ["itemsFailed"] = 2
    }
};
```

### 3. エラー時のメトリクス

```csharp
public static object Execute(McpIncomingCommand command)
{
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        // 処理
        return result;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        
        // エラー情報にメトリクスを追加
        var errorInfo = new Dictionary<string, object>
        {
            ["success"] = false,
            ["error"] = ex.Message,
            ["_metrics"] = new Dictionary<string, object>
            {
                ["executionTimeMs"] = stopwatch.ElapsedMilliseconds,
                ["failedAt"] = "processing"
            }
        };
        
        // エラーでもメトリクスを返す
        return errorInfo;
    }
}
```

---

## 実装ロードマップ

### Phase 1: 基本的な実行時間メトリクス（即座）

- [ ] `PerformanceMetrics` ヘルパークラスの実装
- [ ] 全ツールに `executionTimeMs` を追加
- [ ] Python側でメトリクスをログに記録

### Phase 2: バッチ操作メトリクス（短期）

- [ ] バッチ操作カウント（processed, succeeded, failed）
- [ ] 平均処理時間の計算
- [ ] メトリクスの集計機能

### Phase 3: 最適化提案（中期）

- [ ] 遅い操作の自動検出
- [ ] パフォーマンス最適化の提案
- [ ] ダッシュボード表示

### Phase 4: 高度なメトリクス（長期）

- [ ] リソース使用量メトリクス
- [ ] 操作履歴の永続化
- [ ] パフォーマンストレンド分析

---

## テスト方法

### 1. ユニットテスト

```csharp
[Test]
public void TestMetricsAreIncluded()
{
    var command = new McpIncomingCommand
    {
        ToolName = "pingUnityEditor",
        Payload = new Dictionary<string, object>()
    };
    
    var result = McpCommandProcessor.Execute(command) as Dictionary<string, object>;
    
    Assert.IsNotNull(result);
    Assert.IsTrue(result.ContainsKey("_metrics"));
    
    var metrics = result["_metrics"] as Dictionary<string, object>;
    Assert.IsNotNull(metrics);
    Assert.IsTrue(metrics.ContainsKey("executionTimeMs"));
    Assert.IsTrue((long)metrics["executionTimeMs"] >= 0);
}
```

### 2. パフォーマンステスト

```csharp
[Test]
public void TestBatchOperationMetrics()
{
    // 100個のGameObjectにコンポーネントを追加
    var result = HandleComponentManage(new Dictionary<string, object>
    {
        ["operation"] = "addMultiple",
        ["pattern"] = "Enemy_*",
        ["componentType"] = "UnityEngine.BoxCollider"
    }) as Dictionary<string, object>;
    
    var metrics = result["_metrics"] as Dictionary<string, object>;
    
    Assert.AreEqual(100, metrics["itemsProcessed"]);
    Assert.IsTrue((double)metrics["averageTimePerItemMs"] < 50); // 50ms以下
}
```

---

## 関連ドキュメント

- [API リファレンス](./API.md) - ツールの詳細仕様
- [ベストプラクティス](./API.md#best-practices) - 推奨される使用方法
- [ツール使用例集](./TOOL_EXAMPLES.md) - 実際の使用例


