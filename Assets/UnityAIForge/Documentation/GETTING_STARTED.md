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

### Step 2: GameKitManager を追加

1. **空の GameObject を作成**
   - `Hierarchy` で右クリック > `Create Empty`
   - 名前を `GameManager` に変更

2. **GameKitManager コンポーネントを追加**
   - `Inspector` で `Add Component`
   - "GameKitManager" を検索して追加

3. **Manager を初期化**
   - `Manager Id`: `"MainManager"` を入力
   - `Is Persistent`: チェック（シーン遷移で保持）

### Step 3: リソースを追加

`GameKitManager` の `Mode Components` セクション：

1. **Resource Manager を追加**
   - `+` ボタン > `ResourcePool` を選択

2. **初期リソースを設定**
   - `Initial Resources` を展開
   - `+` で追加:
     - `health`: 100
     - `score`: 0
     - `coins`: 50

### Step 4: テスト用スクリプト作成

```csharp
using UnityEngine;
using UnityAIForge.GameKit;

public class HelloGameKit : MonoBehaviour
{
    void Start()
    {
        // GameKitManager を取得
        var manager = GameKitManager.FindManagerById("MainManager");
        
        // ResourceManager を取得
        var resourceManager = manager.GetComponent<GameKitResourceManager>();
        
        // リソースを表示
        Debug.Log($"Health: {resourceManager.GetResourceValue("health")}");
        Debug.Log($"Score: {resourceManager.GetResourceValue("score")}");
        Debug.Log($"Coins: {resourceManager.GetResourceValue("coins")}");
        
        // リソースを変更
        resourceManager.AddResource("score", 10);
        resourceManager.ConsumeResource("coins", 5);
        
        Debug.Log($"Score after +10: {resourceManager.GetResourceValue("score")}");
        Debug.Log($"Coins after -5: {resourceManager.GetResourceValue("coins")}");
    }
}
```

### Step 5: 実行

1. 新しい GameObject に `HelloGameKit` スクリプトをアタッチ
2. Play ボタンを押す
3. Console にリソース値が表示される！

```
Health: 100
Score: 0
Coins: 50
Score after +10: 10
Coins after -5: 45
```

🎉 **おめでとうございます！** 最初の GameKit シーンが動きました！

---

## 🎮 Try GameKit

### Example 1: プレイヤーキャラクターを作成

#### GameKitActor でキャラクターを作成

```csharp
using UnityEngine;
using UnityAIForge.GameKit;

public class CreatePlayer : MonoBehaviour
{
    void Start()
    {
        // プレイヤーActorを作成
        var playerGO = new GameObject("Player");
        var actor = playerGO.AddComponent<GameKitActor>();
        
        // Actor を設定
        actor.actorId = "Player1";
        actor.behaviorProfile = GameKitActor.BehaviorProfile.Actor2DLinear;
        actor.controlMode = GameKitActor.ControlMode.DirectController;
        
        // 2D Sprite を追加（オプション）
        var sprite = playerGO.AddComponent<SpriteRenderer>();
        sprite.color = Color.green;
        
        Debug.Log("Player created!");
    }
}
```

#### 移動を実装

`GameKitActor` は `OnMoveInput` イベントを提供：

```csharp
using UnityEngine;
using UnityAIForge.GameKit;

public class PlayerMovement : MonoBehaviour
{
    void Update()
    {
        // キー入力を取得
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        if (horizontal != 0 || vertical != 0)
        {
            // Actor の移動イベントを発火
            var actor = GetComponent<GameKitActor>();
            actor.OnMoveInput?.Invoke(new Vector3(horizontal, vertical, 0));
        }
    }
}
```

### Example 2: UI でリソースを表示

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityAIForge.GameKit;

public class ResourceUI : MonoBehaviour
{
    public Text healthText;
    public Text scoreText;
    
    private GameKitResourceManager resourceManager;
    
    void Start()
    {
        var manager = GameKitManager.FindManagerById("MainManager");
        resourceManager = manager.GetComponent<GameKitResourceManager>();
        
        // リソース変更イベントを登録
        resourceManager.OnResourceChanged.AddListener(OnResourceChanged);
        
        // 初期表示
        UpdateUI();
    }
    
    void OnResourceChanged(string resourceName, float oldValue, float newValue)
    {
        UpdateUI();
    }
    
    void UpdateUI()
    {
        healthText.text = $"HP: {resourceManager.GetResourceValue("health")}";
        scoreText.text = $"Score: {resourceManager.GetResourceValue("score")}";
    }
}
```

### Example 3: Machinations で経済を設計

1. **Machinations Asset を作成**
   ```
   Assets > Create > UnityAIForge > GameKit > Machinations Diagram
   ```

2. **リソースフローを定義**
   - `Initial Resources`: health, mana, gold
   - `Flows`: manaRegen (1.0/sec), goldIncome (5.0/sec)
   - `Converters`: castSpell (mana → damage)
   - `Triggers`: playerDied (health < 1)

3. **Manager に適用**
   ```csharp
   var resourceManager = manager.GetComponent<GameKitResourceManager>();
   resourceManager.machinationsAsset = myMachinationsAsset;
   resourceManager.autoProcessFlows = true;
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
| [**MCP Tools**](MCPServer/SKILL.md) | 全24ツールのリファレンス |
| [**Examples**](Examples/README.md) | 実践的なサンプル集 |
| [**API Docs**](GameKit/README.md) | GameKit API ドキュメント |

### プロジェクトアイデア

#### 初級

- [ ] **Clicker Game** - リソース管理を学ぶ
- [ ] **Quiz Game** - UI Command を学ぶ
- [ ] **Visual Novel** - SceneFlow を学ぶ

#### 中級

- [ ] **Tower Defense** - Machinations で経済設計
- [ ] **RPG** - Actor + Manager 統合
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

#### "GameKitManager が見つからない"

**解決策:**
```csharp
// 名前で検索
var manager = GameKitManager.FindManagerById("MainManager");

// またはシーン内の全Managerを取得
var allManagers = FindObjectsOfType<GameKitManager>();
```

#### "MCP Server が起動しない"

**チェックリスト:**
1. Python 3.11+ がインストールされている
2. `uv` がインストールされている (`pip install uv`)
3. ポート 6007 が使用されていない
4. Unity Editor の Console にエラーがない

#### "Assembly が見つからない"

**解決策:**
```csharp
// 正しい using を追加
using UnityAIForge.GameKit;
```

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

