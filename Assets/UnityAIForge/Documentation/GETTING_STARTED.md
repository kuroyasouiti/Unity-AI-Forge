# Getting Started with Unity-AI-Forge

<div align="center">

**🚀 Welcome to Unity-AI-Forge!**

このガイドでは、Unity-AI-Forgeを使ったゲーム開発の始め方を
ステップバイステップで解説します。

</div>

---

## 📋 Table of Contents

1. [セットアップ](#-setup)
2. [Hello World - 最初のシーン](#-hello-world)
3. [GameKit を試す](#-try-gamekit)
4. [MCP で AI 連携](#-mcp-integration)
5. [次のステップ](#-next-steps)

---

## 🔧 Setup

### 1. プロジェクトを開く

```bash
# Clone the repository
git clone https://github.com/kuroyasouiti/Unity-AI-Forge.git

# Open in Unity Hub
# Unity Hub > Add > Select the 'Unity-AI-Forge' folder
```

**Requirements:**
- Unity 2022.3 LTS or later
- .NET Standard 2.1

### 2. 動作確認

Unity Editor を開いたら、以下を確認：

- [ ] **Tools > MCP Assistant** メニューが表示される
- [ ] **Assets/UnityAIForge** フォルダが存在する
- [ ] Console にエラーがない

✅ すべて OK なら、次に進みましょう！

---

## 👋 Hello World

最初のシーンを作成して、GameKit の基本を学びましょう。

### Step 1: 新しいシーンを作成

```
File > New Scene > Basic (Built-in)
または
Ctrl+N (Windows) / Cmd+N (Mac)
```

### Step 2: MCP Bridge を起動

1. **Tools > MCP Assistant** メニューを開く
2. **Start Bridge** ボタンをクリック
3. 接続ステータスが "Connected" になることを確認

### Step 3: AI で GameKit コンポーネントを作成

MCP ツールを使って、AIがコード生成でゲームシステムを構築します。

```python
# UIコマンドパネルを作成（コード生成）
unity_gamekit_ui_command({
    "operation": "createCommandPanel",
    "panelId": "gameControls",
    "layout": "horizontal",
    "commands": [
        {"name": "addScore", "label": "+10 Score", "commandType": "addResource",
         "resourceAmount": 10, "commandParameter": "score"},
        {"name": "useCoins", "label": "-5 Coins", "commandType": "consumeResource",
         "resourceAmount": 5, "commandParameter": "coins"}
    ]
})

# コンパイル完了を待機
unity_compilation_await({"operation": "await"})
```

GameKit は **コード生成** アーキテクチャを採用しています。上記のツール呼び出しにより：
- `Assets/` フォルダにスタンドアロン C# スクリプトが自動生成
- 生成されたスクリプトは Unity-AI-Forge への **依存なし**
- パッケージをアンインストールしても生成コードはそのまま動作

### Step 4: 実行

1. Play ボタンを押す
2. 生成された UI ボタンをクリック
3. Console にリソース変更ログが表示される！

🎉 **おめでとうございます！** 最初の GameKit シーンが動きました！

---

## 🎮 Try GameKit

### Example 1: プレイヤーキャラクターを作成（MCP ツール使用）

```python
# プレイヤー GameObject を作成
unity_gameobject_crud({
    "operation": "create",
    "name": "Player",
    "template": "Sphere"
})

# 2D 物理プリセットを適用
unity_physics_bundle({
    "operation": "applyPreset2D",
    "gameObjectPaths": ["Player"],
    "preset": "platformer"
})

# スプライトを設定
unity_sprite2d_bundle({
    "operation": "createSprite",
    "name": "Player",
    "spritePath": "Assets/Sprites/Player.png",
    "sortingLayerName": "Characters"
})

# 操作ボタンをコード生成
unity_gamekit_ui_command({
    "operation": "createCommandPanel",
    "panelId": "moveControls",
    "layout": "horizontal",
    "commands": [
        {"name": "moveLeft", "label": "Left", "commandType": "move",
         "moveDirection": {"x": -1, "y": 0, "z": 0}},
        {"name": "jump", "label": "Jump", "commandType": "jump"},
        {"name": "moveRight", "label": "Right", "commandType": "move",
         "moveDirection": {"x": 1, "y": 0, "z": 0}}
    ]
})
unity_compilation_await({"operation": "await"})
```

### Example 2: UI でデータバインディング

```python
# HPバーを UI Binding で自動同期
unity_gamekit_ui_binding({
    "operation": "create",
    "bindingId": "hpBar",
    "sourceType": "health",
    "sourceId": "playerHealth",
    "format": "percent",
    "smoothTransition": true,
    "smoothSpeed": 5.0
})
unity_compilation_await({"operation": "await"})
```

### Example 3: エフェクトとフィードバック

```python
# 爆発エフェクト（パーティクル + サウンド + カメラシェイク）
unity_gamekit_effect({
    "operation": "create",
    "effectId": "explosion",
    "components": [
        {"type": "particle", "prefabPath": "Assets/Prefabs/ExplosionVFX.prefab"},
        {"type": "sound", "clipPath": "Assets/Audio/explosion.wav", "volume": 0.8},
        {"type": "cameraShake", "intensity": 0.5, "shakeDuration": 0.3}
    ]
})
unity_compilation_await({"operation": "await"})

# シーン整合性の検証
unity_validate_integrity({"operation": "all"})
```

---

## 🤖 MCP Integration

Unity を AI から操作できるようにします。

### 1. MCP Server をインストール

#### Unity Editor から

1. **Tools > MCP Assistant** を開く
2. **Server Manager** タブに移動
3. **Install** ボタンをクリック

自動的に Python 環境がセットアップされます。

#### または手動で

```bash
cd Assets/UnityAIForge/MCPServer
uv sync
```

### 2. MCP Server を起動

**Unity Editor から:**
- Tools > MCP Assistant > **Start Server**

**または CLI から:**

```bash
cd Assets/UnityAIForge/MCPServer
uv run python src/main.py --transport websocket
```

### 3. AI クライアントを接続

#### Claude Desktop を使用

`claude_desktop_config.json` に追加：

```json
{
  "mcpServers": {
    "unity-ai-forge": {
      "command": "uvx",
      "args": [
        "--from",
        "path/to/Unity-AI-Forge/Assets/UnityAIForge/MCPServer",
        "unity-ai-forge"
      ]
    }
  }
}
```

#### Cursor を使用

`.cursorrules` に追加：

```
Unity-AI-Forge MCP server is available.
Use unity_gamekit_* tools to create game systems.
```

### 4. AI でゲームを作る

Claude や Cursor で以下のように指示：

```
Create a simple 2D platformer game with:
- Player character with jump and move
- Score system
- Coin collectibles
- Game over screen
```

AI が自動的に:
- GameObjects を作成
- Components を設定
- Scripts を生成
- UI を構築

してくれます！🎉

---

## 📚 Next Steps

おめでとうございます！Unity-AI-Forge の基本をマスターしました。

### 学習リソース

| リソース | 内容 |
|:---|:---|
| [**GameKit Guide**](MCPServer/SKILL_GAMEKIT.md) | GameKit 完全ガイド |
| [**MCP Tools**](MCPServer/SKILL.md) | 全49ツールのリファレンス |
| [**Examples**](Examples/README.md) | 実践的なサンプル集 |
| [**API Docs**](GameKit/README.md) | GameKit API ドキュメント |

### プロジェクトアイデア

#### 初級

- [ ] **Clicker Game** - リソース管理を学ぶ
- [ ] **Quiz Game** - UI Command を学ぶ
- [ ] **Visual Novel** - UI Selection + Effect を学ぶ

#### 中級

- [ ] **Tower Defense** - UI Slot + Effect で構築
- [ ] **RPG** - GameKit 3ピラー統合
- [ ] **Roguelike** - プロシージャル生成 + GameKit

#### 上級

- [ ] **Strategy Game** - ターンベース + AI
- [ ] **MMORPG** - リソース同期 + セーブシステム
- [ ] **Editor Extension** - カスタムツール開発

### コミュニティ

- **質問**: [GitHub Discussions](https://github.com/kuroyasouiti/Unity-AI-Forge/discussions)
- **バグ報告**: [GitHub Issues](https://github.com/kuroyasouiti/Unity-AI-Forge/issues)
- **貢献**: [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 🆘 Troubleshooting

### よくある問題

#### "コンパイルが終わらない"

**解決策:**
```python
# タイムアウトを延長
unity_compilation_await({"operation": "await", "timeoutSeconds": 120})

# コンパイルエラーを確認
unity_console_log({"operation": "getCompilationErrors"})
```

#### "MCP Server が起動しない"

**チェックリスト:**
1. Python 3.10+ がインストールされている
2. `uv` がインストールされている (`pip install uv`)
3. ポート 6007 が使用されていない
4. Unity Editor の Console にエラーがない

#### "生成されたスクリプトがアタッチされない"

**解決策:**
GameKit はコード生成後にコンパイルが必要です。`unity_compilation_await` を忘れずに呼んでください。
生成されたスクリプトは `Assets/` フォルダに配置されます。

---

## 📖 More Resources

- [📑 Documentation Index](INDEX.md)
- [🎮 GameKit Components](GameKit/README.md)
- [🔧 MCP Tools Reference](MCPServer/SKILL.md)
- [📝 Changelog](CHANGELOG.md)

---

<div align="center">

**Happy Game Development! 🎮✨**

[⬅️ Back to Main README](../../../README.md) | [📑 Documentation Index](INDEX.md)

</div>

