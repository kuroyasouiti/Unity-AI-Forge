# Unity-AI-Forge Examples

<div align="center">

**💡 実践的なサンプルで学ぶ Unity-AI-Forge**

ステップバイステップのチュートリアルでマスターしよう

[📚 Back to Index](../INDEX.md) | [🚀 Getting Started](../GETTING_STARTED.md)

</div>

---

## 🎯 Examples Overview

| # | タイトル | 内容 | 難易度 | 所要時間 |
|:---:|:---|:---|:---:|:---:|
| 01 | [**Basic Scene Setup**](01-basic-scene-setup.md) | シーン作成の基礎 | ⭐ | 10分 |
| 02 | [**UI Creation**](02-ui-creation.md) | UI システム構築 | ⭐⭐ | 15分 |
| 03 | [**Game Level**](03-game-level.md) | レベルデザイン | ⭐⭐ | 20分 |
| 04 | [**Prefab Workflow**](04-prefab-workflow.md) | Prefab 活用術 | ⭐⭐ | 15分 |
| 05 | [**Design Patterns**](05-design-patterns.md) | デザインパターン | ⭐⭐⭐ | 25分 |

**合計所要時間**: 約1.5時間

---

## 📖 Learning Path

### 🌱 初級者向け (Examples 01-02)

まずはこれらから始めましょう：

```
01. Basic Scene Setup
    ↓
    シーン操作、GameObject作成、Component追加の基礎
    ↓
02. UI Creation
    ↓
    Canvas、Button、Text などの UI 構築
```

**習得スキル:**
- シーン管理の基本
- GameObject と Component の理解
- UI システムの基礎

### 🌿 中級者向け (Examples 03-04)

ゲームを形にする：

```
03. Game Level
    ↓
    地形、敵、アイテムを配置してレベルを作る
    ↓
04. Prefab Workflow
    ↓
    再利用可能なアセットで効率化
```

**習得スキル:**
- レベルデザインの手法
- Prefab の活用
- アセット管理

### 🌲 上級者向け (Example 05)

設計パターンを学ぶ：

```
05. Design Patterns
    ↓
    Singleton、ObjectPool、StateMachine など
    プロフェッショナルな設計手法
```

**習得スキル:**
- デザインパターンの実装
- 拡張可能なコード設計
- パフォーマンス最適化

---

## 🎮 By Game Genre

### 🎲 Puzzle Game
→ [01. Basic Scene](01-basic-scene-setup.md) + [02. UI](02-ui-creation.md)

### 🏃 Platformer
→ [03. Game Level](03-game-level.md) + [04. Prefab Workflow](04-prefab-workflow.md)

### 🗡️ RPG / Adventure
→ [03. Game Level](03-game-level.md) + [05. Design Patterns](05-design-patterns.md)

### 🎯 Strategy / Tower Defense
→ [02. UI](02-ui-creation.md) + [04. Prefab Workflow](04-prefab-workflow.md)

---

## 🚀 Quick Start

### Prerequisites

開始する前に確認：

- [ ] Unity 2022.3 LTS 以上がインストール済み
- [ ] Unity-AI-Forge プロジェクトが開いている
- [ ] [Getting Started](../GETTING_STARTED.md) を読んだ

### Example の実行方法

#### 方法 1: MCP経由 (推奨)

```bash
# 1. MCP Server を起動
# Tools > MCP Assistant > Start Server

# 2. AI クライアント (Claude/Cursor) で指示
"Follow Example 01 from the documentation"
```

AI が自動的にシーンを構築してくれます！

#### 方法 2: 手動

1. 各 Example の Markdown ファイルを開く
2. Step-by-step の指示に従う
3. コードをコピー＆ペースト
4. Play ボタンで実行

---

## 📋 Example Structure

各 Example は統一されたフォーマット：

```markdown
## 🎯 Goal
何を学ぶか

## 📦 Prerequisites
必要な前提知識

## 🛠️ Step-by-Step Guide
詳細な手順

## ✅ Expected Result
完成イメージ

## 🐛 Troubleshooting
よくある問題と解決策

## 🎓 What You Learned
習得した内容

## 📚 Next Steps
次に読むべきドキュメント
```

---

## 💡 Tips & Best Practices

### 効率的な学習方法

1. **順番に進める**
   - Example 01 → 02 → 03 の順で
   - 前の Example の知識が次に活きる

2. **コードを理解する**
   - コピペだけでなく、なぜそうするか考える
   - コメントをしっかり読む

3. **自分でアレンジ**
   - Example を改造してみる
   - 違うゲームジャンルに応用

4. **エラーを恐れない**
   - エラーは学びのチャンス
   - Troubleshooting セクションを活用

### MCP を使った学習

```
# AI に質問する例

"Explain the code in Example 01 step 3"
"What happens if I change the value in Example 02?"
"Show me how to adapt Example 03 for a 2D game"
"Debug the error I'm getting in Example 04"
```

---

## 🔗 Related Documentation

### GameKit を使った Examples

Examples では MCP Tools を直接使いますが、
GameKit を使うとさらに効率的です：

- [**GameKit Overview**](../GameKit/README.md) - フレームワーク概要
- [**GameKit Guide**](../MCPServer/SKILL_GAMEKIT.md) - 完全ガイド

### より詳しい解説

- [**MCP Tools Reference**](../MCPServer/SKILL.md) - 全24ツール
- [**Installation Guide**](../Installation/INSTALL_GUIDE.md) - セットアップ詳細

---

## 🆘 Need Help?

### Stuck on an Example?

1. **Troubleshooting セクションを確認**
   - 各 Example に含まれています

2. **関連ドキュメントを読む**
   - より詳しい説明があります

3. **コミュニティに質問**
   - [GitHub Discussions](https://github.com/kuroyasouiti/Unity-AI-Forge/discussions)
   - [GitHub Issues](https://github.com/kuroyasouiti/Unity-AI-Forge/issues)

### よくある問題

#### "Example のコードが動かない"

**チェックリスト:**
- [ ] Unity のバージョンは 2022.3+ か
- [ ] 正しい namespace を using しているか
- [ ] Console にエラーが出ていないか

#### "MCP で Example が実行できない"

**解決策:**
1. MCP Server が起動しているか確認
2. Bridge が接続されているか確認 (Tools > MCP Assistant)
3. [MCP Setup Guide](../Installation/QUICKSTART.md) を再確認

---

## 🎓 After Completing Examples

Examples を全て完了したら：

### Next Steps

1. **[GameKit Guide](../MCPServer/SKILL_GAMEKIT.md)** で本格的なゲーム開発
2. **[Design Patterns](05-design-patterns.md)** を深掘り
3. **自分のゲームプロジェクトを開始**

### Project Ideas

Examples の知識を活かして作れるゲーム：

- **🎲 Tic-Tac-Toe** (Example 01 + 02)
- **🏃 Endless Runner** (Example 03 + 04)
- **🎯 Tower Defense** (Example 02 + 04 + 05)
- **🗡️ Simple RPG** (Example 03 + 05 + GameKit)

---

## 📝 Contributing Examples

新しい Example を追加したいですか？

### Example の要件

- [ ] 明確な学習目標がある
- [ ] Step-by-step で再現可能
- [ ] コードにコメントが付いている
- [ ] Troubleshooting セクションがある
- [ ] 15-30分で完了できる

### 提出方法

1. Fork this repository
2. 新しい Example を作成 (`06-your-example.md`)
3. このREADMEに追加
4. Pull Request を提出

---

<div align="center">

**💡 Happy Learning! 🎮**

[📚 Back to Index](../INDEX.md) | [🚀 Getting Started](../GETTING_STARTED.md) | [🎮 GameKit Guide](../GameKit/README.md)

</div>
