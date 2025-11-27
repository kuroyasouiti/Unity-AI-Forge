# ユーザー設定可能なインストールパス 🎯

**実装日**: 2025-11-27  
**ステータス**: ✅ 完了  

---

## 📋 概要

MCPサーバーのインストール先をユーザーが自由に変更できるようになりました。Unity Editorから簡単にパスを設定・変更できます。

---

## 🎯 変更の目的

### Before (固定パス)

❌ **ハードコーディング**:
```csharp
public static string UserInstallPath
{
    get
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".claude", "skills", "SkillForUnity");
    }
}
```

**問題**:
- パスが固定されている
- ユーザーが変更できない
- 特殊な環境に対応できない
- カスタムディレクトリが使えない

### After (ユーザー設定可能)

✅ **設定で変更可能**:
```csharp
public static string UserInstallPath
{
    get
    {
        // 設定から取得
        var settings = McpBridgeSettings.Instance;
        return settings.ServerInstallPath;
    }
}
```

**改善点**:
- ユーザーが自由にパスを設定
- UIから簡単に変更
- デフォルト値への復元が可能
- プロジェクト設定として保存

---

## 🔧 実装内容

### 1. `McpBridgeSettings` の利用

#### ServerInstallPath プロパティ

既存の`ServerInstallPath`プロパティを活用：

```csharp
public string ServerInstallPath
{
    get
    {
        var path = string.IsNullOrEmpty(serverInstallPath) 
            ? DefaultServerInstallPath 
            : serverInstallPath;
        return NormalizeInstallPath(path);
    }
    set
    {
        var normalized = NormalizeInstallPath(value);
        if (serverInstallPath == normalized)
        {
            return;
        }
        
        serverInstallPath = normalized;
        SaveSettings();
    }
}
```

#### DefaultServerInstallPath の更新

```csharp
public string DefaultServerInstallPath
{
    get
    {
        // Use McpServerManager's default path
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".claude", "skills", "SkillForUnity");
    }
}
```

### 2. `McpServerManager` の更新

リフレクションを使って設定を取得：

```csharp
public static string UserInstallPath
{
    get
    {
        // Try to get from settings (if available)
        try
        {
            // Use reflection to get McpBridgeSettings without direct reference
            var settingsType = System.Type.GetType("MCP.Editor.McpBridgeSettings, Assembly-CSharp-Editor");
            if (settingsType != null)
            {
                var instanceProp = settingsType.GetProperty("Instance", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProp != null)
                {
                    var settings = instanceProp.GetValue(null);
                    if (settings != null)
                    {
                        var pathProp = settingsType.GetProperty("ServerInstallPath");
                        if (pathProp != null)
                        {
                            var path = pathProp.GetValue(settings) as string;
                            if (!string.IsNullOrEmpty(path))
                            {
                                return path;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Fallback to default if settings not available
        }
        
        // Default path
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".claude", "skills", "SkillForUnity");
    }
}
```

**リフレクションを使う理由**:
- `McpServerManager`は`ServerManager`名前空間
- `McpBridgeSettings`は`MCP.Editor`名前空間
- 循環参照を避けるため

### 3. UI の更新

#### Install Path Settings セクション

```csharp
// Install Path Settings
var settings = McpBridgeSettings.Instance;
using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
{
    EditorGUILayout.LabelField("Install Path Settings", EditorStyles.boldLabel);
    
    using (new EditorGUILayout.HorizontalScope())
    {
        EditorGUILayout.LabelField("Install To:", GUILayout.Width(80));
        
        EditorGUI.BeginChangeCheck();
        var newPath = EditorGUILayout.TextField(settings.ServerInstallPath);
        if (EditorGUI.EndChangeCheck())
        {
            settings.ServerInstallPath = newPath;
            RefreshServerManagerStatus();
        }
    }
    
    using (new EditorGUILayout.HorizontalScope())
    {
        if (GUILayout.Button("Default", GUILayout.Width(70)))
        {
            settings.UseDefaultServerInstallPath();
            RefreshServerManagerStatus();
            AppendLog($"Reset to default path: {settings.ServerInstallPath}");
        }
        
        if (GUILayout.Button("Browse...", GUILayout.Width(70)))
        {
            var selected = EditorUtility.OpenFolderPanel("Select Install Directory", 
                Path.GetDirectoryName(settings.ServerInstallPath) ?? "", "");
            if (!string.IsNullOrEmpty(selected))
            {
                settings.ServerInstallPath = Path.Combine(selected, "SkillForUnity");
                RefreshServerManagerStatus();
                AppendLog($"Install path changed to: {settings.ServerInstallPath}");
            }
        }
        
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.LabelField($"Default: {settings.DefaultServerInstallPath}", 
            EditorStyles.miniLabel);
    }
}
```

---

## 🎨 新しいUI

```
┌─────────────────────────────────────────────────────┐
│ ▼ MCP Server Manager                                │
│                                                      │
│ Server Status                                        │
│ ✅ Status: Installed                                │
│ Install Path: C:\Users\user\.claude\skills\...      │
│ Version: 0.1.0                                       │
│ Python: ✅ Available                                │
│ UV: ✅ Available                                    │
│                                                      │
│ ┌───────────────────────────────────────────────┐   │
│ │ Install Path Settings                          │   │
│ │                                                │   │
│ │ Install To: [C:\Users\user\.claude\skills\... ]│   │
│ │                                                │   │
│ │ [Default] [Browse...] Default: C:\Users\...   │   │
│ └───────────────────────────────────────────────┘   │
│                                                      │
│ [Install Server] [Uninstall Server]                 │
│ [Reinstall Server] [Refresh Status]                 │
│                                                      │
│ [Open Install Folder] [Open Source Folder]          │
└─────────────────────────────────────────────────────┘
```

### UIコンポーネント

| コンポーネント | 機能 |
|-------------|------|
| **Install To** | テキストフィールドで直接編集可能 |
| **Default** | デフォルトパスに戻す |
| **Browse...** | フォルダ選択ダイアログを開く |
| **Default:** | デフォルトパスを表示（参考用） |

---

## 📊 統計

### コード変更

| ファイル | 変更内容 | 行数 |
|---------|---------|------|
| `McpBridgeSettings.cs` | `DefaultServerInstallPath`簡略化 | -10行 |
| `McpServerManager.cs` | リフレクションで設定取得 | +40行 |
| `McpBridgeWindow.cs` | Install Path Settings UI追加 | +40行 |

### 機能追加
- ✅ パス設定UI
- ✅ テキストフィールドで直接編集
- ✅ Browseボタンでフォルダ選択
- ✅ Defaultボタンで復元
- ✅ リアルタイム更新

---

## 🚀 使用方法

### 基本的な使い方

1. **MCP Assistantを開く**
   ```
   Tools > MCP Assistant
   ```

2. **MCP Server Managerセクションを確認**
   - "Install Path Settings"セクションが表示される

3. **パスを変更する方法**

#### 方法1: 直接入力
   - "Install To"フィールドに直接パスを入力
   - Enterキーで確定
   - 自動的に反映される

#### 方法2: Browse機能
   - "Browse..."ボタンをクリック
   - フォルダ選択ダイアログが開く
   - 任意のフォルダを選択
   - 自動的に`/SkillForUnity`が追加される

#### 方法3: デフォルトに戻す
   - "Default"ボタンをクリック
   - デフォルトパスに自動的に戻る

### 設定の保存

- **自動保存**: パスを変更すると自動的に`ProjectSettings/McpBridgeSettings.asset`に保存
- **プロジェクト固有**: 各Unityプロジェクトごとに設定が保存
- **Gitで共有可能**: プロジェクト設定なのでチームで共有可能

---

## 🔍 技術的な詳細

### 設定の保存場所

```
ProjectSettings/
  └─ McpBridgeSettings.asset
```

**保存内容**:
```json
{
  "serverInstallPath": "C:\\Users\\username\\CustomPath\\SkillForUnity"
}
```

### パスの正規化

```csharp
private static string NormalizeInstallPath(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return string.Empty;
    }
    
    var trimmed = value.Trim();
    
    try
    {
        // 絶対パスに変換
        trimmed = Path.GetFullPath(trimmed);
    }
    catch (Exception)
    {
        // keep trimmed fallback
    }
    
    // 末尾のスラッシュを削除
    return trimmed.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
```

### リフレクションの理由

**問題**:
- `McpServerManager`（`MCP.Editor.ServerManager`名前空間）
- `McpBridgeSettings`（`MCP.Editor`名前空間）
- 直接参照すると循環参照

**解決策**:
- リフレクションで動的に取得
- 疎結合を維持
- 依存関係を回避

### フォールバック動作

1. 設定から取得を試みる
2. 失敗した場合はデフォルトパスを使用
3. エラーをログに記録せず静かに処理

---

## 📈 改善ポイント

### 1. 柔軟性
- ✅ ユーザーが自由にパスを設定
- ✅ 特殊な環境に対応
- ✅ カスタムディレクトリが使用可能

### 2. 使いやすさ
- ✅ UIから簡単に変更
- ✅ Browseボタンで選択
- ✅ Defaultボタンで復元
- ✅ リアルタイムプレビュー

### 3. 保守性
- ✅ プロジェクト設定として保存
- ✅ 自動保存
- ✅ Git経由で共有可能

### 4. 互換性
- ✅ リフレクションで疎結合
- ✅ 循環参照を回避
- ✅ 既存コードへの影響最小

---

## 🎯 使用例

### 例1: デフォルトパス

```
C:\Users\username\.claude\skills\SkillForUnity
```

### 例2: カスタムパス

```
D:\MyMCPServers\UnitySkill
```

### 例3: プロジェクト内

```
D:\Projects\MyGame\.mcp\SkillForUnity
```

### 例4: ネットワークドライブ

```
\\NetworkShare\MCPServers\SkillForUnity
```

---

## ⚠️ 注意事項

### パスの要件

- ✅ 書き込み権限が必要
- ✅ 長すぎるパスは避ける（Windows制限）
- ✅ 特殊文字は使用可能だが推奨しない
- ⚠️ ネットワークドライブは遅い可能性

### 変更時の注意

1. **サーバーがインストール済みの場合**:
   - パスを変更してもすぐには移動しない
   - Reinstallで新しいパスに再インストール

2. **AI Tool登録**:
   - パス変更後はAI Toolsを再登録
   - 古いパスの登録は手動で削除が必要

3. **バックアップ**:
   - 重要なカスタム設定は事前にバックアップ
   - 設定ファイルもバックアップ推奨

---

## 🔮 今後の拡張

### Phase 1（完了）✅
- ユーザー設定可能なパス
- UI追加
- 自動保存

### Phase 2（将来）
- ☐ パス履歴機能
- ☐ クイック切り替え
- ☐ プロファイル管理
- ☐ 自動マイグレーション

### Phase 3（将来）
- ☐ マルチインスタンス対応
- ☐ パステンプレート
- ☐ 環境変数サポート

---

## ✅ 完了した目標

| 目標 | ステータス |
|------|----------|
| ユーザー設定可能なパス | ✅ 完了 |
| UI追加 | ✅ 完了 |
| Browse機能 | ✅ 完了 |
| Default復元 | ✅ 完了 |
| 自動保存 | ✅ 完了 |
| リフレクション連携 | ✅ 完了 |

---

## 🎉 結論

MCPサーバーのインストール先をユーザーが自由に設定できるようになりました！

**メリット**:
- 🎯 **柔軟**: 任意のパスに設定可能
- 🎨 **簡単**: UIから数クリックで変更
- 💾 **安全**: 自動保存＆復元可能
- 🔧 **保守**: プロジェクト設定として管理

次は、Unity Editorで新しいInstall Path Settings機能を試してみてください：

```
Tools > MCP Assistant
→ MCP Server Manager
→ Install Path Settings
→ [Browse...] or [Default]
```

すべてがカスタマイズ可能に！✨

---

## 📚 関連ドキュメント

- `JSON_CONFIG_REGISTRATION.md` - JSON設定ファイル登録方式
- `CURSOR_CONFIG_FIX.md` - Cursor設定ファイル検出
- `MCP_SERVER_MANAGEMENT_COMPLETED.md` - サーバー管理完了レポート

---

**作成日**: 2025-11-27  
**最終更新**: 2025-11-27  
**ステータス**: ✅ 完了

