# Unity-AI-Forge Documentation Index

<div align="center">

**📚 すべてのドキュメントへの総合索引**

Unity-AI-Forge を使いこなすための完全ガイド

[🚀 Getting Started](GETTING_STARTED.md) | [🇯🇵 日本語](README_ja.md) | [🇬🇧 English](README.md)

</div>

---

## 🎯 Quick Navigation

### 👤 初めての方

| ドキュメント | 目的 | 所要時間 |
|:---|:---|:---:|
| [**🚀 Getting Started**](GETTING_STARTED.md) | Unity-AI-Forge を始める | 15分 |
| [**⚙️ Quick Start**](Installation/QUICKSTART.md) | 最速インストール | 5分 |
| [**01. 基本シーン**](Examples/01-basic-scene-setup.md) | 最初のシーンを作る | 10分 |

### 🎮 GameKit を使う

| コンポーネント | 用途 | 難易度 |
|:---|:---|:---:|
| [**Actor**](GameKit/README.md#gamekit-actor) | プレイヤー/NPC | ⭐ |
| [**Manager**](GameKit/README.md#gamekit-manager) | リソース/状態管理 | ⭐⭐ |
| [**UICommand**](GameKit/GameKitUICommand.README.md) | UI → ロジック連携 | ⭐ |
| [**Machinations**](GameKit/GameKitMachinations.README.md) | 経済システム設計 | ⭐⭐⭐ |
| [**SceneFlow**](GameKit/GameKitSceneFlow.README.md) | シーン遷移管理 | ⭐⭐ |
| [**Interaction**](GameKit/GameKitInteraction.README.md) | トリガーアクション | ⭐ |

### 🤖 AI と連携

| ドキュメント | 内容 |
|:---|:---|
| [**MCP Server**](MCPServer/README.md) | MCP サーバーセットアップ |
| [**24 Tools**](MCPServer/SKILL.md) | 全ツールリファレンス |
| [**Batch Sequential**](MCPServer/BATCH_SEQUENTIAL.md) | バッチ逐次処理（レジューム対応） |
| [**GameKit Guide**](MCPServer/SKILL_GAMEKIT.md) | GameKit 完全ガイド |
| [**Claude AI**](CLAUDE.md) | Claude 連携情報 |

---

## 📖 Documentation Structure

```
Documentation/
├── 🚀 Getting Started ────────────── 初心者向けガイド
│
├── 📦 Installation/
│   ├── QUICKSTART.md ──────────── 5分でセットアップ
│   └── INSTALL_GUIDE.md ───────── 詳細インストール手順
│
├── 🎮 GameKit/
│   ├── README.md ──────────────── GameKit 概要
│   ├── GameKitActor ───────────── プレイヤー/NPC
│   ├── GameKitManager ─────────── リソース/状態管理
│   ├── GameKitMachinations ────── 経済システム
│   ├── GameKitSceneFlow ───────── シーン遷移
│   ├── GameKitUICommand ───────── UI コマンド
│   ├── GameKitInteraction ─────── トリガーシステム
│   ├── SplineMovement ─────────── スプライン移動
│   └── GraphNodeMovement ──────── グラフ移動
│
├── 🤖 MCPServer/
│   ├── README.md ──────────────── MCP サーバー概要
│   ├── SKILL.md ───────────────── 全24ツール解説
│   ├── BATCH_SEQUENTIAL.md ────── バッチ逐次処理
│   └── SKILL_GAMEKIT.md ───────── GameKit 完全ガイド
│
├── 💡 Examples/
│   ├── 01-basic-scene-setup.md ── 基本シーン
│   ├── 02-ui-creation.md ──────── UI 作成
│   ├── 03-game-level.md ───────── ゲームレベル
│   ├── 04-prefab-workflow.md ──── Prefab ワークフロー
│   └── 05-design-patterns.md ──── デザインパターン
│
├── 🛠️ Handlers/
│   └── CharacterControllerBundle ─ キャラクター制御
│
├── 🧪 Testing/
│   └── README.md ──────────────── テスト実行方法
│
├── 📝 CHANGELOG.md ────────────── 変更履歴
├── 🔮 GAMEKIT_ROADMAP.md ─────── 今後の開発計画
├── 🤖 CLAUDE.md ───────────────── Claude AI 連携
├── 🇯🇵 README_ja.md ──────────── プロジェクト概要（日本語）
└── 🇬🇧 README.md ─────────────── Project Overview (English)
```

---

## 🎓 Learning Path

### Level 1: 基礎を学ぶ (1-2時間)

1. [Getting Started](GETTING_STARTED.md) - セットアップと Hello World
2. [01. 基本シーン](Examples/01-basic-scene-setup.md) - シーン操作
3. [02. UI 作成](Examples/02-ui-creation.md) - UI 構築

### Level 2: GameKit を使う (2-4時間)

4. [GameKit 概要](GameKit/README.md) - フレームワーク理解
5. [Actor](GameKit/README.md#gamekit-actor) - キャラクター作成
6. [Manager](GameKit/README.md#gamekit-manager) - リソース管理
7. [UICommand](GameKit/GameKitUICommand.README.md) - UI 連携

### Level 3: 高度な機能 (4-8時間)

8. [Machinations](GameKit/GameKitMachinations.README.md) - 経済設計
9. [SceneFlow](GameKit/GameKitSceneFlow.README.md) - シーン遷移
10. [MCP Tools](MCPServer/SKILL.md) - AI 連携

### Level 4: プロジェクト開発 (実践)

11. [03. ゲームレベル](Examples/03-game-level.md) - レベル構築
12. [04. Prefab ワークフロー](Examples/04-prefab-workflow.md) - 再利用
13. [05. デザインパターン](Examples/05-design-patterns.md) - 設計

---

## 📚 Complete Documentation List

---

## 🔍 By Category

<details>
<summary><b>🚀 Getting Started & Installation</b></summary>

- [Getting Started Guide](GETTING_STARTED.md) ⭐ **Start Here!**
- [Quick Start (5 min)](Installation/QUICKSTART.md)
- [Installation Guide (詳細)](Installation/INSTALL_GUIDE.md)
- [Project README (English)](README.md)
- [プロジェクト README (日本語)](README_ja.md)

</details>

<details>
<summary><b>🎮 GameKit Framework</b></summary>

#### Core Components

- [GameKit Overview](GameKit/README.md)
- [GameKitActor](GameKit/README.md#gamekit-actor) - Player/NPC system
- [GameKitManager](GameKit/README.md#gamekit-manager) - Resource/State/Turn management

#### Resource & Economy

- [GameKitResourceManager](GameKit/GameKitResourceManager.README.md) - Resource pools and flows
- [GameKitMachinations](GameKit/GameKitMachinations.README.md) - Economic system design

#### UI & Interaction

- [GameKitUICommand](GameKit/GameKitUICommand.README.md) - UI button → Logic bridge
- [GameKitInteraction](GameKit/GameKitInteraction.README.md) - Trigger-based interactions

#### Scene & Navigation

- [GameKitSceneFlow](GameKit/GameKitSceneFlow.README.md) - Scene transition state machine
- [SplineMovement](GameKit/SplineMovement.README.md) - Spline-based movement
- [GraphNodeMovement](GameKit/GraphNodeMovement.README.md) - A* pathfinding

</details>

<details>
<summary><b>🤖 MCP & AI Integration</b></summary>

- [MCP Server Overview](MCPServer/README.md)
- [All 24 Tools Reference](MCPServer/SKILL.md)
- [Batch Sequential Tool](MCPServer/BATCH_SEQUENTIAL.md)
- [GameKit Complete Guide](MCPServer/SKILL_GAMEKIT.md)
- [Claude AI Integration](CLAUDE.md)

</details>

<details>
<summary><b>💡 Examples & Tutorials</b></summary>

- [Examples Overview](Examples/README.md)
- [01. Basic Scene Setup](Examples/01-basic-scene-setup.md)
- [02. UI Creation](Examples/02-ui-creation.md)
- [03. Game Level](Examples/03-game-level.md)
- [04. Prefab Workflow](Examples/04-prefab-workflow.md)
- [05. Design Patterns](Examples/05-design-patterns.md)

</details>

<details>
<summary><b>🛠️ Advanced & Tools</b></summary>

- [CharacterController Bundle](Handlers/CharacterControllerBundle.README.md)
- [Testing Guide](Testing/README.md)
- [Changelog](CHANGELOG.md)
- [GameKit Roadmap](GAMEKIT_ROADMAP.md) - 今後の開発計画

</details>

---

## 🎯 By Use Case

### "ゲームを作り始めたい"
→ [Getting Started](GETTING_STARTED.md) → [Basic Scene](Examples/01-basic-scene-setup.md)

### "経済システムを設計したい"
→ [Machinations](GameKit/GameKitMachinations.README.md) → [ResourceManager](GameKit/GameKitResourceManager.README.md)

### "プレイヤーキャラクターを作りたい"
→ [GameKitActor](GameKit/README.md#gamekit-actor) → [Example 03](Examples/03-game-level.md)

### "UIとロジックを分離したい"
→ [UICommand](GameKit/GameKitUICommand.README.md) → [UI Example](Examples/02-ui-creation.md)

### "シーン遷移を管理したい"
→ [SceneFlow](GameKit/GameKitSceneFlow.README.md)

### "AI でゲームを作りたい"
→ [MCP Server](MCPServer/README.md) → [All Tools](MCPServer/SKILL.md)

---

## 💬 Community & Support

| リソース | リンク |
|:---|:---|
| **質問・議論** | [GitHub Discussions](https://github.com/kuroyasouiti/Unity-AI-Forge/discussions) |
| **バグ報告** | [GitHub Issues](https://github.com/kuroyasouiti/Unity-AI-Forge/issues) |
| **貢献方法** | CONTRIBUTING.md |
| **ライセンス** | [MIT License](../../../LICENSE) |

---

## 📝 Document Status

| ドキュメント | 状態 | 最終更新 |
|:---|:---:|:---|
| Getting Started | ✅ 完成 | 2025-12-09 |
| GameKit Guide | ✅ 完成 | 2025-12-09 |
| MCP Tools | ✅ 完成 | 2025-12-09 |
| Examples | ✅ 完成 | 2025-12-03 |
| GameKit Roadmap | 📋 計画中 | 2025-12-29 |
| API Reference | 🚧 作成中 | - |

---

<div align="center">

**📚 Happy Reading & Coding! 🎮**

[⬅️ Back to Main README](../../../README.md) | [🚀 Getting Started](GETTING_STARTED.md)

</div>

