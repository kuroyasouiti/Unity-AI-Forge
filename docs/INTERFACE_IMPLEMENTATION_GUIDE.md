# インターフェース実装ガイド

このドキュメントでは、抽出されたインターフェースを使用して新しいコマンドハンドラーを実装する方法を説明します。

## 📚 目次

1. [概要](#概要)
2. [基本的な実装手順](#基本的な実装手順)
3. [サンプル実装](#サンプル実装)
4. [ベストプラクティス](#ベストプラクティス)
5. [テスト](#テスト)
6. [トラブルシューティング](#トラブルシューティング)

---

## 概要

SkillForUnityでは、以下のインターフェースとクラスを提供しています：

### インターフェース

| インターフェース | 用途 | ファイル |
|----------------|------|---------|
| `ICommandHandler` | コマンドハンドラーの基本インターフェース | `Interfaces/ICommandHandler.cs` |
| `IOperationHandler` | 個別の操作ハンドラー | `Interfaces/IOperationHandler.cs` |
| `IPayloadValidator` | ペイロードバリデーター | `Interfaces/IPayloadValidator.cs` |
| `IResourceResolver<T>` | リソース解決 | `Interfaces/IResourceResolver.cs` |
| `IGameObjectResolver` | GameObject専用の解決 | `Interfaces/IResourceResolver.cs` |
| `IAssetResolver` | Asset専用の解決 | `Interfaces/IResourceResolver.cs` |
| `ITypeResolver` | Type専用の解決 | `Interfaces/IResourceResolver.cs` |

### 基底クラス

| クラス | 用途 | ファイル |
|-------|------|---------|
| `BaseCommandHandler` | コマンドハンドラーの基底クラス | `Base/BaseCommandHandler.cs` |
| `CommandHandlerFactory` | ファクトリークラス | `Base/CommandHandlerFactory.cs` |

---

## 基本的な実装手順

### ステップ1: 基底クラスを継承

```csharp
using System.Collections.Generic;
using MCP.Editor.Base;

namespace MCP.Editor.Handlers
{
    public class MyCommandHandler : BaseCommandHandler
    {
        // 実装
    }
}
```

### ステップ2: 必須プロパティを実装

```csharp
public override string Category => "myCategory";

public override IEnumerable<string> SupportedOperations => new[]
{
    "create",
    "delete",
    "update",
    "inspect"
};
```

### ステップ3: 操作実行メソッドを実装

```csharp
protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
{
    return operation switch
    {
        "create" => CreateOperation(payload),
        "delete" => DeleteOperation(payload),
        "update" => UpdateOperation(payload),
        "inspect" => InspectOperation(payload),
        _ => throw new InvalidOperationException($"Unknown operation: {operation}")
    };
}
```

### ステップ4: 個別の操作メソッドを実装

```csharp
private object CreateOperation(Dictionary<string, object> payload)
{
    // 1. パラメータの取得
    var name = GetString(payload, "name");
    
    // 2. バリデーション
    if (string.IsNullOrEmpty(name))
    {
        throw new InvalidOperationException("name is required");
    }
    
    // 3. 操作の実行
    // ... あなたのロジック
    
    // 4. 結果の返却
    return CreateSuccessResponse(
        ("name", name),
        ("message", "Created successfully")
    );
}
```

### ステップ5: ファクトリーに登録

```csharp
// エディター初期化時またはアプリケーション起動時
CommandHandlerFactory.Register("myCommandManage", new MyCommandHandler());
```

---

## サンプル実装

### 例1: シンプルなCRUDハンドラー

```csharp
using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// プレイヤーデータ管理ハンドラー
    /// </summary>
    public class PlayerDataCommandHandler : BaseCommandHandler
    {
        public override string Category => "playerData";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "create",
            "delete",
            "update",
            "inspect",
            "list"
        };
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreatePlayerData(payload),
                "delete" => DeletePlayerData(payload),
                "update" => UpdatePlayerData(payload),
                "inspect" => InspectPlayerData(payload),
                "list" => ListPlayerData(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }
        
        private object CreatePlayerData(Dictionary<string, object> payload)
        {
            var playerName = GetString(payload, "playerName");
            var level = GetInt(payload, "level", 1);
            
            if (string.IsNullOrEmpty(playerName))
            {
                throw new InvalidOperationException("playerName is required");
            }
            
            // プレイヤーデータを作成
            var playerId = Guid.NewGuid().ToString();
            
            Debug.Log($"Creating player data: {playerName} (Level {level})");
            
            return CreateSuccessResponse(
                ("playerId", playerId),
                ("playerName", playerName),
                ("level", level),
                ("message", $"Player '{playerName}' created successfully")
            );
        }
        
        private object DeletePlayerData(Dictionary<string, object> payload)
        {
            var playerId = GetString(payload, "playerId");
            
            if (string.IsNullOrEmpty(playerId))
            {
                throw new InvalidOperationException("playerId is required");
            }
            
            Debug.Log($"Deleting player data: {playerId}");
            
            return CreateSuccessResponse(
                ("playerId", playerId),
                ("message", "Player deleted successfully")
            );
        }
        
        private object UpdatePlayerData(Dictionary<string, object> payload)
        {
            var playerId = GetString(payload, "playerId");
            var level = GetInt(payload, "level", -1);
            
            if (string.IsNullOrEmpty(playerId))
            {
                throw new InvalidOperationException("playerId is required");
            }
            
            Debug.Log($"Updating player data: {playerId}");
            
            var updated = new List<string>();
            if (level >= 0)
            {
                updated.Add("level");
            }
            
            return CreateSuccessResponse(
                ("playerId", playerId),
                ("updated", updated),
                ("message", "Player updated successfully")
            );
        }
        
        private object InspectPlayerData(Dictionary<string, object> payload)
        {
            var playerId = GetString(payload, "playerId");
            
            if (string.IsNullOrEmpty(playerId))
            {
                throw new InvalidOperationException("playerId is required");
            }
            
            // プレイヤーデータを取得（サンプル）
            var playerData = new Dictionary<string, object>
            {
                ["playerId"] = playerId,
                ["playerName"] = "SamplePlayer",
                ["level"] = 10,
                ["experience"] = 1500,
                ["health"] = 100
            };
            
            return CreateSuccessResponse(
                ("playerData", playerData)
            );
        }
        
        private object ListPlayerData(Dictionary<string, object> payload)
        {
            var maxResults = GetInt(payload, "maxResults", 100);
            
            // プレイヤーリストを取得（サンプル）
            var players = new List<Dictionary<string, object>>();
            
            for (int i = 0; i < Math.Min(maxResults, 5); i++)
            {
                players.Add(new Dictionary<string, object>
                {
                    ["playerId"] = Guid.NewGuid().ToString(),
                    ["playerName"] = $"Player{i + 1}",
                    ["level"] = i + 1
                });
            }
            
            return CreateSuccessResponse(
                ("players", players),
                ("count", players.Count)
            );
        }
        
        protected override bool RequiresCompilationWait(string operation)
        {
            // inspect と list は読み取り専用
            return operation != "inspect" && operation != "list";
        }
    }
}
```

### 例2: バッチ操作をサポートするハンドラー

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    public class TagCommandHandler : BaseCommandHandler
    {
        public override string Category => "tag";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "add",
            "remove",
            "setTag",
            "setTagMultiple",  // バッチ操作
            "list"
        };
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "add" => AddTag(payload),
                "remove" => RemoveTag(payload),
                "setTag" => SetTag(payload),
                "setTagMultiple" => SetTagMultiple(payload),
                "list" => ListTags(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }
        
        private object SetTagMultiple(Dictionary<string, object> payload)
        {
            var pattern = GetString(payload, "pattern");
            var tag = GetString(payload, "tag");
            var maxResults = GetInt(payload, "maxResults", 1000);
            var stopOnError = GetBool(payload, "stopOnError", false);
            
            if (string.IsNullOrEmpty(pattern))
            {
                throw new InvalidOperationException("pattern is required");
            }
            
            if (string.IsNullOrEmpty(tag))
            {
                throw new InvalidOperationException("tag is required");
            }
            
            // パターンマッチング（サンプル実装）
            var gameObjects = FindGameObjectsByPattern(pattern, maxResults);
            
            var results = new List<Dictionary<string, object>>();
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var go in gameObjects)
            {
                try
                {
                    go.tag = tag;
                    successCount++;
                    
                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["gameObjectPath"] = GetGameObjectPath(go),
                        ["tag"] = tag
                    });
                }
                catch (Exception ex)
                {
                    failureCount++;
                    
                    results.Add(new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["gameObjectPath"] = GetGameObjectPath(go),
                        ["error"] = ex.Message
                    });
                    
                    if (stopOnError)
                    {
                        throw;
                    }
                }
            }
            
            return CreateSuccessResponse(
                ("results", results),
                ("processed", results.Count),
                ("succeeded", successCount),
                ("failed", failureCount),
                ("message", $"Set tag on {successCount} GameObjects")
            );
        }
        
        // その他の操作メソッド...
        private object AddTag(Dictionary<string, object> payload) { /* ... */ return null; }
        private object RemoveTag(Dictionary<string, object> payload) { /* ... */ return null; }
        private object SetTag(Dictionary<string, object> payload) { /* ... */ return null; }
        private object ListTags(Dictionary<string, object> payload) { /* ... */ return null; }
        
        // ヘルパーメソッド
        private IEnumerable<GameObject> FindGameObjectsByPattern(string pattern, int maxResults)
        {
            // 実装: パターンマッチングロジック
            return GameObject.FindObjectsOfType<GameObject>().Take(maxResults);
        }
        
        private string GetGameObjectPath(GameObject go)
        {
            // 実装: GameObjectの階層パスを取得
            return go.name;
        }
    }
}
```

---

## ベストプラクティス

### 1. エラーハンドリング

```csharp
protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
{
    try
    {
        // 操作を実行
        return PerformOperation(operation, payload);
    }
    catch (ArgumentException ex)
    {
        // 引数エラー
        return CreateErrorResponse(ex);
    }
    catch (InvalidOperationException ex)
    {
        // 操作エラー
        return CreateErrorResponse(ex);
    }
    catch (Exception ex)
    {
        // 予期しないエラー
        Debug.LogException(ex);
        return CreateErrorResponse(ex);
    }
}
```

### 2. パラメータバリデーション

```csharp
private object CreateOperation(Dictionary<string, object> payload)
{
    // 必須パラメータのバリデーション
    var name = GetString(payload, "name");
    if (string.IsNullOrEmpty(name))
    {
        throw new ArgumentException("name parameter is required and cannot be empty");
    }
    
    // オプションパラメータのデフォルト値
    var type = GetString(payload, "type", "default");
    var count = GetInt(payload, "count", 1);
    
    // 値の範囲チェック
    if (count < 1 || count > 1000)
    {
        throw new ArgumentException("count must be between 1 and 1000");
    }
    
    // 処理...
}
```

### 3. ログ出力

```csharp
private object CreateOperation(Dictionary<string, object> payload)
{
    var name = GetString(payload, "name");
    
    Debug.Log($"[{Category}] Creating: {name}");
    
    try
    {
        // 処理...
        
        Debug.Log($"[{Category}] Successfully created: {name}");
        return CreateSuccessResponse(("name", name));
    }
    catch (Exception ex)
    {
        Debug.LogError($"[{Category}] Failed to create: {name}. Error: {ex.Message}");
        throw;
    }
}
```

### 4. パフォーマンス最適化

```csharp
// キャッシュの活用
private static Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();

private Type ResolveType(string typeName)
{
    if (_typeCache.TryGetValue(typeName, out var cachedType))
    {
        return cachedType;
    }
    
    // 型を検索
    var type = Type.GetType(typeName);
    if (type != null)
    {
        _typeCache[typeName] = type;
    }
    
    return type;
}

// 遅延初期化
private IMyService _service;
private IMyService Service => _service ?? (_service = new MyService());
```

---

## テスト

### ユニットテスト例

```csharp
using NUnit.Framework;
using MCP.Editor.Handlers;
using System.Collections.Generic;

public class PlayerDataCommandHandlerTests
{
    private PlayerDataCommandHandler _handler;
    
    [SetUp]
    public void SetUp()
    {
        _handler = new PlayerDataCommandHandler();
    }
    
    [Test]
    public void TestCreatePlayerData()
    {
        var payload = new Dictionary<string, object>
        {
            ["operation"] = "create",
            ["playerName"] = "TestPlayer",
            ["level"] = 5
        };
        
        var result = _handler.Execute(payload) as Dictionary<string, object>;
        
        Assert.IsNotNull(result);
        Assert.IsTrue((bool)result["success"]);
        Assert.AreEqual("TestPlayer", result["playerName"]);
        Assert.AreEqual(5, result["level"]);
    }
    
    [Test]
    public void TestCreatePlayerData_MissingName_ThrowsException()
    {
        var payload = new Dictionary<string, object>
        {
            ["operation"] = "create",
            ["level"] = 5
        };
        
        var result = _handler.Execute(payload) as Dictionary<string, object>;
        
        Assert.IsNotNull(result);
        Assert.IsFalse((bool)result["success"]);
        Assert.IsTrue(result.ContainsKey("error"));
    }
}
```

---

## トラブルシューティング

### 問題1: ハンドラーが見つからない

**症状**: `No handler registered for tool: myCommandManage`

**解決方法**:
```csharp
// ファクトリーに登録を追加
CommandHandlerFactory.Register("myCommandManage", new MyCommandHandler());
```

### 問題2: 操作がサポートされていない

**症状**: `Operation 'xyz' is not supported`

**解決方法**:
```csharp
public override IEnumerable<string> SupportedOperations => new[]
{
    "create",
    "delete",
    "update",
    "inspect",
    "xyz"  // 追加
};
```

### 問題3: パラメータが取得できない

**症状**: パラメータが常に null またはデフォルト値

**解決方法**:
```csharp
// デバッグログで確認
Debug.Log($"Payload keys: {string.Join(", ", payload.Keys)}");
Debug.Log($"Value for 'name': {payload.GetValueOrDefault("name")}");
```

---

## 関連ドキュメント

- [INTERFACE_EXTRACTION.md](./INTERFACE_EXTRACTION.md) - インターフェース抽出レポート
- [API.md](./API.md) - APIリファレンス
- [TOOLS_REVIEW.md](./TOOLS_REVIEW.md) - ツールレビュー


