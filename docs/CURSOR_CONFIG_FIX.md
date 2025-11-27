# Cursor設定ファイル検出の改善 🔍

**実装日**: 2025-11-27  
**ステータス**: ✅ 完了  

---

## 📋 概要

Cursorの設定ファイルパスの検出を改善し、複数の可能性を自動的に探索するように修正しました。

---

## 🎯 問題

### Before (問題点)

❌ **単一パスのみ対応**:
```csharp
AITool.Cursor => Path.Combine(appData, "Cursor", "User", "globalStorage", 
    "saoudrizwan.claude-dev", "settings", "cline_mcp_settings.json")
```

**問題**:
- Cursorの設定ファイルパスは環境によって異なる
- インストールされている拡張機能によって変わる
- 固定パスでは見つからないことがある

### After (解決策)

✅ **複数パスの自動探索**:
```csharp
AITool.Cursor => FindCursorConfigPath(appData)
```

**改善点**:
- 複数の可能性を順番にチェック
- 既存ファイルを優先的に使用
- デフォルトパスへのフォールバック

---

## 🔧 実装内容

### 1. `FindCursorConfigPath()` メソッド

Cursorの設定ファイルを以下の順序で探索：

```csharp
private static string FindCursorConfigPath(string appData)
{
    // 可能性1: Cursor独自のMCP設定
    var path1 = Path.Combine(appData, "Cursor", "User", "globalStorage", "cursor", "mcp.json");
    if (File.Exists(path1)) return path1;
    
    // 可能性2: CursorのグローバルMCP設定
    var path2 = Path.Combine(appData, "Cursor", "User", "globalStorage", "cursor-mcp", "settings.json");
    if (File.Exists(path2)) return path2;
    
    // 可能性3: Cline統合（Roo Cline）
    var path3 = Path.Combine(appData, "Cursor", "User", "globalStorage", 
        "rooveterinaryinc.roo-cline", "settings", "cline_mcp_settings.json");
    if (File.Exists(path3)) return path3;
    
    // 可能性4: Cline統合（saoudrizwan）
    var path4 = Path.Combine(appData, "Cursor", "User", "globalStorage", 
        "saoudrizwan.claude-dev", "settings", "cline_mcp_settings.json");
    if (File.Exists(path4)) return path4;
    
    // 可能性5: Cursorのsettings.json（メイン設定ファイル）
    var path5 = Path.Combine(appData, "Cursor", "User", "settings.json");
    if (File.Exists(path5))
    {
        // settings.jsonがあれば、それを使用（mcpServersセクションがあるかチェック）
        try
        {
            var content = File.ReadAllText(path5);
            if (content.Contains("mcpServers"))
            {
                return path5;
            }
        }
        catch
        {
            // エラーは無視して次へ
        }
    }
    
    // デフォルト: Roo Clineのパスを返す（最も一般的）
    return path3;
}
```

### 2. 探索する設定ファイルパス

| 優先度 | パス | 説明 |
|-------|------|------|
| 1 | `Cursor\User\globalStorage\cursor\mcp.json` | Cursor独自のMCP設定 |
| 2 | `Cursor\User\globalStorage\cursor-mcp\settings.json` | グローバルMCP設定 |
| 3 | `Cursor\User\globalStorage\rooveterinaryinc.roo-cline\settings\cline_mcp_settings.json` | Roo Cline統合 ⭐ 最も一般的 |
| 4 | `Cursor\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json` | Cline統合（旧版） |
| 5 | `Cursor\User\settings.json` | メイン設定（mcpServersセクションがある場合） |

### 3. デバッグログの追加

設定ファイルの読み込み時に詳細ログを出力：

```csharp
public static JObject LoadConfig(AITool tool)
{
    var path = GetConfigPath(tool);
    
    Debug.Log($"[McpConfigManager] Loading config for {tool} from: {path}");
    
    if (!File.Exists(path))
    {
        Debug.Log($"[McpConfigManager] Config file not found for {tool}, creating new one.");
        return new JObject();
    }
    
    var json = File.ReadAllText(path);
    var config = JObject.Parse(json);
    
    Debug.Log($"[McpConfigManager] Config loaded successfully for {tool}");
    
    return config;
}
```

### 4. UI改善

#### 各ツール行に設定ファイルパスを表示

**Before**:
```
✅ Cursor    📄 Registered    [Unregister] [📦]
```

**After**:
```
┌────────────────────────────────────────────────┐
│ ✅ Cursor          📄 Registered               │
│    [Unregister] [📦] [📂]                      │
│ Path: C:\Users\...\cline_mcp_settings.json    │
└────────────────────────────────────────────────┘
```

#### 新しいボタン

| ボタン | 機能 |
|-------|------|
| 📦 | バックアップ作成 |
| 📂 | 設定ファイル/ディレクトリを開く |

#### `OpenConfigFile()` メソッド

```csharp
private void OpenConfigFile(AITool tool)
{
    var path = McpConfigManager.GetConfigPath(tool);
    var directory = Path.GetDirectoryName(path);
    
    if (File.Exists(path))
    {
        // ファイルが存在する場合は開く
        System.Diagnostics.Process.Start(path);
    }
    else if (Directory.Exists(directory))
    {
        // ファイルはないがディレクトリがある場合はディレクトリを開く
        System.Diagnostics.Process.Start(directory);
    }
    else
    {
        // どちらもない場合はダイアログを表示
        EditorUtility.DisplayDialog("Config Not Found", 
            $"Configuration file not found:\n{path}\n\n" +
            $"Please ensure {tool} is installed and has been run at least once.", 
            "OK");
    }
}
```

---

## 🎨 新しいUI

```
┌─────────────────────────────────────────────────────┐
│ ▼ AI Tool Registration                              │
│                                                      │
│ ┌─────────────────────────────────────────────────┐ │
│ │ ✅ Cursor          📄 Registered                │ │
│ │    [Unregister] [📦] [📂]                       │ │
│ │ Path: C:\Users\...\rooveterinaryinc.roo-cline\  │ │
│ │       settings\cline_mcp_settings.json          │ │
│ └─────────────────────────────────────────────────┘ │
│                                                      │
│ ┌─────────────────────────────────────────────────┐ │
│ │ ⭕ Claude Desktop  ❌ Config not found          │ │
│ │    [Register] [📂]                              │ │
│ │ Path: C:\Users\...\Claude\                      │ │
│ │       claude_desktop_config.json                │ │
│ └─────────────────────────────────────────────────┘ │
│                                                      │
│ ┌─────────────────────────────────────────────────┐ │
│ │ ⭕ Cline (VS Code) 📄 Not registered            │ │
│ │    [Register] [📦] [📂]                         │ │
│ │ Path: C:\Users\...\Code\User\globalStorage\     │ │
│ │       saoudrizwan.claude-dev\settings\          │ │
│ │       cline_mcp_settings.json                   │ │
│ └─────────────────────────────────────────────────┘ │
│                                                      │
│ ┌─────────────────────────────────────────────────┐ │
│ │ ⭕ Windsurf        📄 Not registered            │ │
│ │    [Register] [📦] [📂]                         │ │
│ │ Path: C:\Users\...\Windsurf\User\globalStorage\ │ │
│ │       windsurf.windsurf\settings\               │ │
│ │       mcp_settings.json                         │ │
│ └─────────────────────────────────────────────────┘ │
│                                                      │
│ [Register All] [Unregister All] [Refresh Status]    │
└─────────────────────────────────────────────────────┘
```

---

## 📊 統計

### コード変更

| ファイル | 変更内容 | 行数 |
|---------|---------|------|
| `McpConfigManager.cs` | `FindCursorConfigPath()` 追加 | +55行 |
| `McpConfigManager.cs` | デバッグログ追加 | +8行 |
| `McpBridgeWindow.cs` | UI改善（パス表示） | +40行 |
| `McpBridgeWindow.cs` | `OpenConfigFile()` 追加 | +35行 |

### 機能追加
- ✅ Cursor設定ファイルの自動検出（5つのパスをチェック）
- ✅ 設定ファイルパスの表示
- ✅ 設定ファイル/ディレクトリを開く機能
- ✅ 詳細なデバッグログ

---

## 🚀 使用方法

### 設定ファイルの確認

1. **MCP Assistantを開く**
   ```
   Tools > MCP Assistant
   ```

2. **AI Tool Registrationセクションを確認**
   - 各ツールのパスが表示されている

3. **設定ファイルを開く**
   - 📂ボタンをクリック
   - 設定ファイルまたはディレクトリが開く

4. **実際のパスを確認**
   - Unity Consoleでログを確認
   ```
   [McpConfigManager] Loading config for Cursor from: C:\Users\...\cline_mcp_settings.json
   ```

### トラブルシューティング

**Q: 設定ファイルが見つからない**

A: 
1. 📂ボタンをクリックして実際のパスを確認
2. Cursorを起動して、Cline拡張機能をインストール
3. Clineを一度使用して設定ファイルを生成
4. Refresh Statusをクリック

**Q: 複数の設定ファイルがある**

A: 
- 自動的に最も適切なファイルが選択されます
- 優先順位は実装内容を参照

**Q: カスタムパスを使いたい**

A: 
- 現在はサポートされていません
- 将来のバージョンで対応予定

---

## 🔍 技術的な詳細

### ファイル探索のロジック

```
1. Cursor独自のMCP設定を探す
   ↓ 見つからない
2. グローバルMCP設定を探す
   ↓ 見つからない
3. Roo Cline統合を探す ⭐ 最も一般的
   ↓ 見つからない
4. Cline統合（旧版）を探す
   ↓ 見つからない
5. settings.jsonにmcpServersセクションがあるかチェック
   ↓ 見つからない
6. デフォルトパス（Roo Cline）を返す
```

### エラーハンドリング

- ファイルが見つからない → 新規作成
- ディレクトリが存在しない → 自動作成
- JSON解析エラー → 例外をスロー
- ファイルアクセスエラー → ログ出力＆例外スロー

---

## 📈 改善ポイント

### 1. 柔軟性
- ✅ 複数のパスに対応
- ✅ 自動検出
- ✅ 環境に適応

### 2. デバッグ性
- ✅ 詳細なログ
- ✅ パスの可視化
- ✅ ファイルへのアクセス

### 3. ユーザー体験
- ✅ 自動で最適なパスを選択
- ✅ ワンクリックでファイルを開く
- ✅ 明確なエラーメッセージ

---

## ✅ 解決した問題

| 問題 | 解決策 | ステータス |
|------|--------|----------|
| Cursorの設定ファイルが見つからない | 複数パスの自動探索 | ✅ 完了 |
| どのパスが使われているか不明 | UIにパス表示 | ✅ 完了 |
| 設定ファイルの確認が困難 | 📂ボタンで直接開く | ✅ 完了 |
| デバッグが難しい | 詳細ログ追加 | ✅ 完了 |

---

## 🔮 今後の拡張

### Phase 1（完了）✅
- 複数パスの自動探索
- パスの可視化
- ファイルアクセス機能

### Phase 2（将来）
- ☐ カスタムパス設定
- ☐ パス優先度のカスタマイズ
- ☐ 設定ファイルのバリデーション
- ☐ 自動修復機能

### Phase 3（将来）
- ☐ 設定ファイルのエディタ
- ☐ ビジュアル設定UI
- ☐ プロファイル管理

---

## 🎉 結論

Cursorの設定ファイル検出を大幅に改善しました！

**メリット**:
- 🎯 **柔軟**: 複数のパスに対応
- 🔍 **明確**: パスが可視化
- 🚀 **簡単**: ワンクリックアクセス
- 💪 **堅牢**: 環境に適応

これで、Cursorへの登録がより確実になります！

---

## 📚 関連ドキュメント

- `JSON_CONFIG_REGISTRATION.md` - JSON設定ファイル登録方式
- `MCP_SERVER_MANAGEMENT_COMPLETED.md` - サーバー管理完了レポート
- `MCP_BRIDGE_INTEGRATION_REPORT.md` - Bridge統合レポート

---

**作成日**: 2025-11-27  
**最終更新**: 2025-11-27  
**ステータス**: ✅ 完了

