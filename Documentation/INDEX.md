# Unity-AI-Forge Documentation Index

このドキュメントは Unity-AI-Forge プロジェクトのすべてのドキュメントへの索引です。

## 📚 目次

### 🚀 はじめに

- [**README (English)**](README.md) - プロジェクトの概要と基本情報
- [**README (日本語)**](README_ja.md) - プロジェクトの概要と基本情報（日本語版）
- [**CHANGELOG**](CHANGELOG.md) - 変更履歴
- [**CLAUDE.md**](CLAUDE.md) - Claude AI との連携に関する情報

### 📦 インストール

- [**クイックスタート**](Installation/QUICKSTART.md) - 最速で始めるためのガイド
- [**インストールガイド**](Installation/INSTALL_GUIDE.md) - 詳細なインストール手順

### 🔌 MCP サーバー

- [**MCP サーバー概要**](MCPServer/README.md) - MCP サーバーの詳細
- [**SKILL.md**](MCPServer/SKILL.md) - スキル定義と使用方法

### 💡 使用例

- [**Examples 概要**](Examples/README.md) - サンプルの一覧と使い方
- [01. 基本的なシーンセットアップ](Examples/01-basic-scene-setup.md)
- [02. UI作成](Examples/02-ui-creation.md)
- [03. ゲームレベル作成](Examples/03-game-level.md)
- [04. Prefabワークフロー](Examples/04-prefab-workflow.md)
- [05. デザインパターン](Examples/05-design-patterns.md)

### 🎮 GameKit

GameKit は、ゲーム開発でよく使われる機能を提供するフレームワークです。

- [**GameKit 概要**](GameKit/README.md) - GameKit 全体の説明

#### リソース管理

- [**GameKitResourceManager**](GameKit/GameKitResourceManager.README.md)  
  Machinations風のリソースフローシステム（HP、MP、Gold など）

- [**GameKitMachinations**](GameKit/GameKitMachinations.README.md)  
  マキネーションダイアグラムをScriptableObjectとして管理

#### ゲームシステム

- [**GameKitInteraction**](GameKit/GameKitInteraction.README.md)  
  インタラクションシステム（トリガー、条件、アクション）

- [**GameKitSceneFlow**](GameKit/GameKitSceneFlow.README.md)  
  シーン遷移管理システム

- [**GameKitUICommand**](GameKit/GameKitUICommand.README.md)  
  UIコマンドシステム（ボタンとアクターの連携）

#### 移動システム

- [**SplineMovement**](GameKit/SplineMovement.README.md)  
  スプライン（曲線）に沿った移動

- [**GraphNodeMovement**](GameKit/GraphNodeMovement.README.md)  
  ノードグラフベースの移動（パスファインディング）

### 🛠️ ハンドラー

- [**CharacterControllerBundle**](Handlers/CharacterControllerBundle.README.md)  
  キャラクターコントローラーのバンドルハンドラー

### 🧪 テスト

- [**Testing README**](Testing/README.md) - テストの実行方法と構造

---

## 📂 フォルダ構造

```
Documentation/
├── INDEX.md (このファイル)
├── README.md
├── README_ja.md
├── CHANGELOG.md
├── CLAUDE.md
├── Installation/
│   ├── QUICKSTART.md
│   └── INSTALL_GUIDE.md
├── MCPServer/
│   ├── README.md
│   └── SKILL.md
├── Examples/
│   ├── README.md
│   ├── 01-basic-scene-setup.md
│   ├── 02-ui-creation.md
│   ├── 03-game-level.md
│   ├── 04-prefab-workflow.md
│   └── 05-design-patterns.md
├── GameKit/
│   ├── README.md
│   ├── GameKitResourceManager.README.md
│   ├── GameKitMachinations.README.md
│   ├── GameKitInteraction.README.md
│   ├── GameKitSceneFlow.README.md
│   ├── GameKitUICommand.README.md
│   ├── SplineMovement.README.md
│   └── GraphNodeMovement.README.md
├── Handlers/
│   └── CharacterControllerBundle.README.md
└── Testing/
    └── README.md
```

---

## 🔍 カテゴリ別索引

### 初心者向け
1. [README (日本語)](README_ja.md)
2. [クイックスタート](Installation/QUICKSTART.md)
3. [基本的なシーンセットアップ](Examples/01-basic-scene-setup.md)

### インストール・セットアップ
1. [クイックスタート](Installation/QUICKSTART.md)
2. [インストールガイド](Installation/INSTALL_GUIDE.md)

### ゲームシステム設計
1. [GameKitResourceManager](GameKit/GameKitResourceManager.README.md) - 経済システム
2. [GameKitMachinations](GameKit/GameKitMachinations.README.md) - 経済システムのアセット化
3. [GameKitInteraction](GameKit/GameKitInteraction.README.md) - インタラクション
4. [GameKitSceneFlow](GameKit/GameKitSceneFlow.README.md) - シーン遷移

### UI・入力
1. [GameKitUICommand](GameKit/GameKitUICommand.README.md) - UIコマンド
2. [UI作成の例](Examples/02-ui-creation.md)

### 移動・ナビゲーション
1. [SplineMovement](GameKit/SplineMovement.README.md) - スプライン移動
2. [GraphNodeMovement](GameKit/GraphNodeMovement.README.md) - グラフベース移動
3. [CharacterControllerBundle](Handlers/CharacterControllerBundle.README.md) - キャラクター制御

### MCP・AI連携
1. [MCP サーバー概要](MCPServer/README.md)
2. [SKILL.md](MCPServer/SKILL.md)
3. [CLAUDE.md](CLAUDE.md)

---

## 💬 サポート

質問やフィードバックがある場合は、[GitHub Issues](https://github.com/kuroyasouiti/Unity-AI-Forge/issues) でお知らせください。

---

**最終更新**: 2025年11月30日

