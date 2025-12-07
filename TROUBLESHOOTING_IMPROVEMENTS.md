# TextMeshPro CRUD改善のトラブルシューティング

## 実装済みの改善内容

### 1. InspectMultipleComponents - propertyFilterの改善
**ファイル**: `Assets/UnityAIForge/Editor/MCPBridge/Component/McpCommandProcessor.Component.cs`

**改善内容**:
- propertyFilter指定時に内部フィールド（m_, _プレフィックス）を除外
- 指定されたプロパティのみを返すように改善

**期待される動作**:
```json
// リクエスト
{
  "operation": "inspectMultiple",
  "pattern": "Test*",
  "componentType": "TMPro.TextMeshPro",
  "propertyFilter": ["text", "fontSize", "enableAutoSizing"]
}

// 期待されるレスポンス
{
  "properties": {
    "text": "...",
    "fontSize": "36",
    "enableAutoSizing": "False"
    // m_プレフィックスフィールドは除外される
  }
}
```

### 2. UpdateComponent - エラーハンドリングの改善
**改善内容**:
- 個別プロパティの更新失敗を許容
- updatedPropertiesとfailedPropertiesを分けて返却

**期待される動作**:
```json
// リクエスト
{
  "operation": "update",
  "propertyChanges": {
    "text": "Valid",
    "invalidProperty": "Invalid"
  }
}

// 期待されるレスポンス
{
  "success": true,
  "updatedProperties": ["text"],
  "failedProperties": {
    "invalidProperty": "Property not found..."
  },
  "partialSuccess": true
}
```

### 3. AddMultipleComponents - 初期プロパティ適用の改善
**改善内容**:
- propertyChanges適用時のエラーハンドリング
- appliedPropertiesとfailedPropertiesの返却

**期待される動作**:
```json
// リクエスト
{
  "operation": "addMultiple",
  "pattern": "Test*",
  "componentType": "TMPro.TextMeshPro",
  "propertyChanges": {
    "text": "Initial Text",
    "fontSize": 28.0
  }
}

// 期待されるレスポンス (各結果に)
{
  "success": true,
  "appliedProperties": ["text", "fontSize"],
  "failedProperties": {}
}
```

### 4. UpdateMultipleComponents - バッチ更新の改善
同様に個別エラーハンドリングを実装。

## 現在の問題

### 症状
改善したコードが反映されず、以下の問題が継続：

1. **propertyFilter**: すべてのプロパティとm_フィールドが返される
2. **addMultiple**: propertyChangesが適用されない
3. **updateComponent**: 古いレスポンス形式（updatedのみ）

### 原因の可能性

#### 1. MCPサーバーがキャッシュされたDLLを使用
MCPサーバープロセスが起動したまま古いDLLを保持している可能性。

**確認方法**:
```bash
# MCPサーバープロセスを確認
tasklist | findstr python
tasklist | findstr uv
```

**対処方法**:
1. MCPサーバープロセスを停止
2. Unity Editorを再起動
3. MCPサーバーを再起動

#### 2. コンパイルエラーまたは警告
コードに構文エラーがある可能性。

**確認方法**:
Unity Editor の Console ウィンドウを確認:
- 赤いエラーメッセージがないか
- 黄色い警告メッセージがないか

**対処方法**:
1. Consoleウィンドウのエラーを解決
2. `Assets > Reimport All` を実行
3. Unity Editorを再起動

#### 3. アセンブリ定義の問題
McpCommandProcessor.Component.csが正しいアセンブリに含まれていない可能性。

**確認方法**:
```
Assets/UnityAIForge/Editor/UnityAIForge.Editor.asmdef
```
このアセンブリ定義ファイルを確認。

**対処方法**:
1. アセンブリ定義を再インポート
2. Unity Editorを再起動

#### 4. 部分クラスの問題
McpCommandProcessor.Component.csは部分クラス（partial class）として定義されている。
他のファイルとの整合性の問題。

**確認方法**:
```csharp
// McpCommandProcessor.Component.cs の先頭
internal static partial class McpCommandProcessor
```

**対処方法**:
コードが正しく保存されているか確認。

## デバッグ手順

### ステップ1: Unity Consoleの確認
1. Unity Editorを開く
2. Console ウィンドウを開く（Window > General > Console）
3. エラーや警告がないか確認

### ステップ2: コンパイル状態の確認
1. Unity Editor下部のステータスバーを確認
2. "Compiling" または "Compilation completed" を確認

### ステップ3: MCPサーバーの再起動
1. Cursor側でMCPサーバーを停止
2. Unity Editor側でBridgeを停止
3. Unity Editorを再起動
4. Unity Editor側でBridgeを起動
5. Cursor側でMCPサーバーを起動

### ステップ4: デバッグログの追加
改善されたメソッドにデバッグログを追加:

```csharp
Debug.Log($"[MCP] InspectMultiple: propertyFilter count = {propertyFilter?.Count ?? 0}");
Debug.Log($"[MCP] AddMultiple: propertyChanges count = {propertyChanges.Count}");
Debug.Log($"[MCP] UpdateComponent: updatedProperties count = {updatedProperties.Count}");
```

### ステップ5: 簡易テスト
Unity Editorで直接テスト:

```csharp
// Tools > Create Test Menu
[MenuItem("Tools/Test Component CRUD")]
public static void TestComponentCRUD()
{
    var go = new GameObject("TestObject");
    var component = go.AddComponent<TMPro.TextMeshPro>();
    
    var payload = new Dictionary<string, object>
    {
        ["operation"] = "inspect",
        ["gameObjectPath"] = "TestObject",
        ["componentType"] = "TMPro.TextMeshPro",
        ["propertyFilter"] = new List<object> { "text", "fontSize" }
    };
    
    var command = new McpIncomingCommand 
    { 
        ToolName = "componentManage", 
        Payload = payload 
    };
    
    var result = McpCommandProcessor.Execute(command);
    Debug.Log($"Result: {MiniJson.Serialize(result)}");
    
    Object.DestroyImmediate(go);
}
```

## 完全なテスト検証手順

### 1. Unity Editorの完全再起動
```
1. Unity Editorを閉じる
2. MCPサーバーを停止
3. Unity Editorを開く
4. MCP Bridgeを起動（Tools > MCP Bridge > Start Bridge）
5. MCPサーバーを起動（Cursor側）
```

### 2. 再コンパイルの強制実行
```
1. Assets > Reimport All を実行
2. Edit > Preferences > External Tools で Script Editor を確認
3. 再度 Assets > Reimport All を実行
```

### 3. テスト実行
改善された機能のテスト:

**Test A: propertyFilter**
```bash
unity_component_crud operation=inspectMultiple pattern="Test*" componentType="TMPro.TextMeshPro" propertyFilter=["text","fontSize"]
```
期待: text, fontSize のみが返される（m_フィールドなし）

**Test B: addMultiple with propertyChanges**
```bash
unity_component_crud operation=addMultiple pattern="Test*" componentType="TMPro.TextMeshPro" propertyChanges={"text":"Init","fontSize":25}
```
期待: appliedProperties が返され、実際に値が設定される

**Test C: update with errors**
```bash
unity_component_crud operation=update gameObjectPath="Test1" componentType="TMPro.TextMeshPro" propertyChanges={"text":"OK","invalid":"NG"}
```
期待: updatedProperties と failedProperties が分かれて返される

## 最終手段

上記すべてで解決しない場合:

1. **プロジェクトのバックアップ**
2. **Library フォルダの削除** (Unityが再生成)
3. **Unity Editorの再起動**
4. **MCPサーバーの再インストール**

## 期待される最終結果

すべての改善が正しく動作する場合:

```json
// InspectMultiple with propertyFilter
{
  "properties": {
    "text": "Hello",
    "fontSize": "36",
    "enableAutoSizing": "False"
    // Only requested properties, no m_ fields
  }
}

// AddMultiple with propertyChanges
{
  "results": [{
    "success": true,
    "appliedProperties": ["text", "fontSize"],
    "failedProperties": {}
  }]
}

// Update with partial errors
{
  "success": true,
  "updatedProperties": ["text", "fontSize"],
  "failedProperties": {
    "invalidProperty": "Property not found..."
  },
  "partialSuccess": true
}
```

## 連絡先・サポート

問題が解決しない場合は、以下の情報を提供してください:

1. Unity Editor Console のスクリーンショット
2. MCPサーバーのログ
3. 実行したテストコマンドと結果
4. Unityのバージョン情報

---

**最終更新**: 2025-12-06
**対象バージョン**: UnityAI-Forge 1.0.0
