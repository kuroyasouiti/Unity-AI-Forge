# MCP Server Installation Guide

**Unity-AI-Forge** のMCPサーバーをAIツール（Cursor、Claude Desktop、Cline、Windsurf）に登録する方法を説明します。

---

## 📋 前提条件

1. **Unity Editor 2022.3以上**（2021.3+もサポート）
2. **Python 3.10以上**がインストールされていること
3. **UV**がインストールされていること（推奨）
   - インストール: `pip install uv` または https://github.com/astral-sh/uv
4. **Unity-AI-Forgeパッケージ**がUnityプロジェクトにインストールされていること

---

## 🚀 クイックスタート

### 方法1: Unity Editor から1クリックインストール（推奨）

1. Unity Editorを開く
2. メニューから **Tools > Unity-AI-Forge > MCP Server Manager** を選択
3. **Install Server** ボタンをクリック（`~/Unity-AI-Forge`にインストール）
4. 使用したいAIツールの **Register** ボタンをクリック
5. AIツールを再起動

これだけです！

### 方法2: 手動インストール

```bash
# Windows (PowerShell)
xcopy /E /I /Y "Assets\UnityAIForge\MCPServer" "%USERPROFILE%\Unity-AI-Forge"
cd %USERPROFILE%\Unity-AI-Forge
uv sync

# macOS/Linux
cp -r Assets/UnityAIForge/MCPServer ~/Unity-AI-Forge
cd ~/Unity-AI-Forge
uv sync
```

その後、手動でAIツールの設定ファイルを編集（下記参照）。

---

## 📖 詳細手順

### 1. MCP Server Manager を開く

Unity Editorのメニューから：
```
Tools > Unity-AI-Forge > MCP Server Manager
```

### 2. サーバーをインストール

**MCP Server Manager**ウィンドウで：

1. **Server Status**セクションで現在のステータスを確認
2. **Install Server**ボタンをクリック
3. インストールが完了するまで待機（通常10-30秒）

インストール先:
- Windows: `%USERPROFILE%\Unity-AI-Forge`
- macOS/Linux: `~/Unity-AI-Forge`

### 3. AIツールに登録

**AI Tool Registration**セクションで：

#### オプション1: 個別に登録

使用したいAIツールの **Register** ボタンをクリック：
- ✅ Cursor
- ✅ Claude Desktop
- ✅ Cline (VS Code)
- ✅ Windsurf

#### オプション2: 一括登録

**Register All** ボタンをクリックして、すべてのAIツールに一度に登録

### 4. AIツールを再起動

登録後、対象のAIツールを完全に再起動してください。

---

## 🔧 各AIツールの設定ファイル

### Cursor
**パス**: `%APPDATA%\Cursor\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json`

### Claude Desktop
**パス**: `%APPDATA%\Claude\claude_desktop_config.json`

### Cline (VS Code)
**パス**: `%APPDATA%\Code\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json`

### Windsurf
**パス**: `%APPDATA%\Windsurf\User\globalStorage\windsurf.windsurf\settings\mcp_settings.json`

---

## 🔄 更新手順

サーバーを最新版に更新するには：

1. **MCP Server Manager**を開く
2. **Reinstall Server**ボタンをクリック
3. AIツールを再起動

---

## 🗑️ アンインストール

### 完全アンインストール

1. **MCP Server Manager**で **Unregister All** をクリック
2. **Uninstall Server** をクリック
3. AIツールを再起動

### 特定のAIツールのみ解除

1. **MCP Server Manager**を開く
2. 解除したいツールの **Unregister** ボタンをクリック
3. 該当のAIツールを再起動

---

## 🐛 トラブルシューティング

### Unity Packageが見つからない

Unity Package Managerでインストールする場合、正しいパスを使用してください：
```
https://github.com/kuroyasouiti/Unity-AI-Forge.git?path=/Assets/UnityAIForge
```

### サーバーが起動しない

1. **Python**と**UV**がインストールされているか確認
   ```bash
   python --version
   uv --version
   ```

2. 手動でセットアップ
   ```bash
   # Windows
   cd %USERPROFILE%\Unity-AI-Forge
   uv sync
   
   # macOS/Linux
   cd ~/Unity-AI-Forge
   uv sync
   ```

### AIツールに表示されない

1. AIツールを完全に再起動（タスクマネージャーからプロセスを終了）
2. 設定ファイルが正しく作成されているか確認
3. **MCP Server Manager**で登録状態を確認

### エラーログの確認

1. **MCP Server Manager**の**Log**セクションを確認
2. Unity Consoleでエラーメッセージを確認
3. AIツールのログを確認（通常はツール内の設定から）

---

## 📚 追加情報

### 手動インストール（上級者向け）

MCP Server Managerを使わずに手動でインストールする場合：

1. サーバーファイルをコピー
   ```bash
   # Windows (PowerShell)
   xcopy /E /I /Y "Assets\UnityAIForge\MCPServer" "%USERPROFILE%\Unity-AI-Forge"
   
   # macOS/Linux
   cp -r Assets/UnityAIForge/MCPServer ~/Unity-AI-Forge
   ```

2. Python環境をセットアップ
   ```bash
   # Windows
   cd %USERPROFILE%\Unity-AI-Forge
   uv sync
   
   # macOS/Linux
   cd ~/Unity-AI-Forge
   uv sync
   ```

3. AIツールの設定ファイルを手動編集
   - 各ツールの設定ファイルに以下を追加：
   
   **Windows:**
   ```json
   {
     "mcpServers": {
       "unity-ai-forge": {
         "command": "uv",
         "args": [
           "--directory",
           "C:\\Users\\YOUR_USERNAME\\Unity-AI-Forge",
           "run",
           "unity-ai-forge"
         ]
       }
     }
   }
   ```
   
   **macOS/Linux:**
   ```json
   {
     "mcpServers": {
       "unity-ai-forge": {
         "command": "uv",
         "args": [
           "--directory",
           "/Users/YOUR_USERNAME/Unity-AI-Forge",
           "run",
           "unity-ai-forge"
         ]
       }
     }
   }
   ```
   
   **注意**: パスを実際のユーザー名に置き換えてください。

### バックアップと復元

設定ファイルは自動的にバックアップされます：
- バックアップ先: 元の設定ファイルと同じディレクトリ
- 命名規則: `{元のファイル名}.backup.{日時}`

復元が必要な場合は、**MCP Server Manager**の機能を使用するか、手動でバックアップファイルをリネームしてください。

---

## 📚 追加情報

### プロジェクト構造

```
Assets/UnityAIForge/
├── Editor/
│   └── MCPBridge/              # Unity C#ブリッジ
├── GameKit/                    # GameKitフレームワーク
├── MCPServer/                  # MCPサーバー（このフォルダ）
│   ├── src/                    # Pythonソースコード
│   ├── setup/                  # インストールスクリプト
│   ├── examples/               # サンプル
│   ├── config/                 # 設定テンプレート
│   ├── skill.yml               # MCPサーバーマニフェスト
│   └── pyproject.toml          # Pythonパッケージ定義
└── Tests/                      # テストスイート
```

### インストール先

MCP Server Managerは以下の場所にサーバーをインストールします：
- Windows: `%USERPROFILE%\Unity-AI-Forge` (例: `C:\Users\YourName\Unity-AI-Forge`)
- macOS: `~/Unity-AI-Forge` (例: `/Users/YourName/Unity-AI-Forge`)
- Linux: `~/Unity-AI-Forge` (例: `/home/YourName/Unity-AI-Forge`)

## ❓ ヘルプとサポート

- **GitHub Issues**: https://github.com/kuroyasouiti/Unity-AI-Forge/issues
- **ドキュメント**: 
  - [README.md](README.md) - MCPサーバードキュメント
  - [QUICKSTART.md](QUICKSTART.md) - クイックスタートガイド
  - [../../README.md](../../README.md) - プロジェクト全体のドキュメント
- **Unity Console**: エラーメッセージを確認

---

**最終更新**: 2025-11-29

