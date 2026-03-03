# Unity-AI-Forge Documentation Index

<div align="center">

**すべてのドキュメントへの総合索引**

Unity-AI-Forge を使いこなすための完全ガイド

[🚀 Getting Started](GETTING_STARTED.md) | [🇯🇵 日本語](README_ja.md) | [🇬🇧 English](README.md)

</div>

---

## Quick Navigation

### 初めての方

| ドキュメント | 目的 | 所要時間 |
|:---|:---|:---:|
| [**Getting Started**](GETTING_STARTED.md) | Unity-AI-Forge を始める | 15分 |
| [**Quick Start**](Installation/QUICKSTART.md) | 最速インストール | 5分 |
| [**01. 基本シーン**](Examples/01-basic-scene-setup.md) | 最初のシーンを作る | 10分 |

### GameKit を使う

#### UI ピラー

| コンポーネント | 用途 | MCP ツール |
|:---|:---|:---|
| [**UICommand**](GameKit/GameKitUICommand.README.md) | ボタンコマンドパネル | `unity_gamekit_ui_command` |
| **UIBinding** | データバインディング | `unity_gamekit_ui_binding` |
| **UIList** | リスト/グリッド | `unity_gamekit_ui_list` |
| **UISlot** | スロット（インベントリ等） | `unity_gamekit_ui_slot` |
| **UISelection** | 選択グループ（ラジオ/タブ等） | `unity_gamekit_ui_selection` |

#### ロジックピラー

| コンポーネント | 用途 | MCP ツール |
|:---|:---|:---|
| **SceneIntegrity** | シーン整合性チェック | `unity_validate_integrity` |
| **ClassCatalog** | クラス一覧 | `unity_class_catalog` |
| **ClassDependencyGraph** | クラス依存関係 | `unity_class_dependency_graph` |
| **SceneReferenceGraph** | シーン参照分析 | `unity_scene_reference_graph` |
| **SceneRelationshipGraph** | シーン関係分析 | `unity_scene_relationship_graph` |

### AI と連携

| ドキュメント | 内容 |
|:---|:---|
| [**MCP Server**](MCPServer/README.md) | MCP サーバーセットアップ |
| [**47 Tools**](MCPServer/SKILL.md) | 全ツールリファレンス |
| [**Batch Sequential**](MCPServer/BATCH_SEQUENTIAL.md) | バッチ逐次処理（レジューム対応） |
| [**GameKit Guide**](MCPServer/SKILL_GAMEKIT.md) | GameKit 完全ガイド |

---

## Documentation Structure

```
Documentation/
├── Getting Started ──────────── 初心者向けガイド
│
├── Installation/
│   ├── QUICKSTART.md ────────── 5分でセットアップ
│   └── INSTALL_GUIDE.md ─────── 詳細インストール手順
│
├── GameKit/
│   ├── README.md ────────────── GameKit 概要
│   └── GameKitUICommand ─────── UICommand 詳細ガイド
│
├── MCPServer/
│   ├── README.md ────────────── MCP サーバー概要
│   ├── SKILL.md ─────────────── 全47ツール解説
│   ├── BATCH_SEQUENTIAL.md ──── バッチ逐次処理
│   └── SKILL_GAMEKIT.md ─────── GameKit 完全ガイド
│
├── Examples/
│   ├── 01-basic-scene-setup.md ── 基本シーン
│   ├── 02-ui-creation.md ──────── UI 作成
│   ├── 03-game-level.md ───────── ゲームレベル
│   ├── 04-prefab-workflow.md ──── Prefab ワークフロー
│   └── 05-design-patterns.md ──── デザインパターン
│
├── Handlers/
│   ├── CharacterControllerBundle ─ キャラクター制御
│   └── TilemapBundle ──────────── タイルマップ
│
├── Testing/
│   └── README.md ────────────── テスト実行方法
│
├── CHANGELOG.md ──────────────── 変更履歴
├── GAMEKIT_ROADMAP.md ────────── 今後の開発計画
├── README_ja.md ──────────────── プロジェクト概要（日本語）
└── README.md ─────────────────── Project Overview (English)
```

---

## Learning Path

### Level 1: 基礎を学ぶ (1-2時間)

1. [Getting Started](GETTING_STARTED.md) - セットアップと Hello World
2. [01. 基本シーン](Examples/01-basic-scene-setup.md) - シーン操作
3. [02. UI 作成](Examples/02-ui-creation.md) - UI 構築

### Level 2: GameKit を使う (2-4時間)

4. [GameKit 概要](GameKit/README.md) - GameKit アーキテクチャの理解
5. [GameKit 完全ガイド](MCPServer/SKILL_GAMEKIT.md) - GameKit ツールの使い方
6. [UICommand](GameKit/GameKitUICommand.README.md) - UIコマンドパネル

### Level 3: 高度な機能 (4-8時間)

7. [全47ツール](MCPServer/SKILL.md) - Low/Mid/High レベルツール
8. [03. ゲームレベル](Examples/03-game-level.md) - レベル構築
9. [05. デザインパターン](Examples/05-design-patterns.md) - 設計

### Level 4: プロジェクト開発 (実践)

10. [04. Prefab ワークフロー](Examples/04-prefab-workflow.md) - 再利用
11. [Batch Sequential](MCPServer/BATCH_SEQUENTIAL.md) - バッチ処理

---

## Complete Documentation List

---

## By Category

<details>
<summary><b>Getting Started & Installation</b></summary>

- [Getting Started Guide](GETTING_STARTED.md) - **Start Here!**
- [Quick Start (5 min)](Installation/QUICKSTART.md)
- [Installation Guide](Installation/INSTALL_GUIDE.md)
- [Project README (English)](README.md)
- [プロジェクト README (日本語)](README_ja.md)

</details>

<details>
<summary><b>GameKit Framework</b></summary>

#### 概要

- [GameKit Overview](GameKit/README.md) - GameKit 概要

#### UI ピラー

- [UICommand](GameKit/GameKitUICommand.README.md) - ボタンコマンドパネル
- UIBinding - データバインディング（[完全ガイド](MCPServer/SKILL_GAMEKIT.md#uibinding)参照）
- UIList - リスト/グリッド（[完全ガイド](MCPServer/SKILL_GAMEKIT.md#uilist)参照）
- UISlot - スロット（[完全ガイド](MCPServer/SKILL_GAMEKIT.md#uislot)参照）
- UISelection - 選択グループ（[完全ガイド](MCPServer/SKILL_GAMEKIT.md#uiselection)参照）

#### ロジックピラー

- SceneIntegrity, ClassCatalog, ClassDependencyGraph, SceneReferenceGraph, SceneRelationshipGraph
- 詳細: [GameKit Overview](GameKit/README.md#ロジックピラー)

</details>

<details>
<summary><b>MCP & AI Integration</b></summary>

- [MCP Server Overview](MCPServer/README.md)
- [All 47 Tools Reference](MCPServer/SKILL.md)
- [Batch Sequential Tool](MCPServer/BATCH_SEQUENTIAL.md)
- [GameKit Complete Guide](MCPServer/SKILL_GAMEKIT.md)

</details>

<details>
<summary><b>Examples & Tutorials</b></summary>

- [Examples Overview](Examples/README.md)
- [01. Basic Scene Setup](Examples/01-basic-scene-setup.md)
- [02. UI Creation](Examples/02-ui-creation.md)
- [03. Game Level](Examples/03-game-level.md)
- [04. Prefab Workflow](Examples/04-prefab-workflow.md)
- [05. Design Patterns](Examples/05-design-patterns.md)

</details>

<details>
<summary><b>Advanced & Tools</b></summary>

- [CharacterController Bundle](Handlers/CharacterControllerBundle.README.md)
- [Tilemap Bundle](Handlers/TilemapBundle.README.md)
- [Testing Guide](Testing/README.md)
- [Changelog](CHANGELOG.md)
- [GameKit Roadmap](GAMEKIT_ROADMAP.md)

</details>

---

## By Use Case

### "ゲームの UI を作りたい"
→ [GameKit Overview](GameKit/README.md#ui-ピラー) → [UICommand](GameKit/GameKitUICommand.README.md)

### "シーンの整合性をチェックしたい"
→ [GameKit Overview](GameKit/README.md#ロジックピラー)

### "AI でゲームを作りたい"
→ [MCP Server](MCPServer/README.md) → [All Tools](MCPServer/SKILL.md)

---

## Community & Support

| リソース | リンク |
|:---|:---|
| **質問・議論** | [GitHub Discussions](https://github.com/kuroyasouiti/Unity-AI-Forge/discussions) |
| **バグ報告** | [GitHub Issues](https://github.com/kuroyasouiti/Unity-AI-Forge/issues) |
| **貢献方法** | CONTRIBUTING.md |
| **ライセンス** | [MIT License](../../../LICENSE) |

---

## Document Status

| ドキュメント | 状態 | 最終更新 |
|:---|:---:|:---|
| Getting Started | 完成 | 2025-12-09 |
| GameKit Guide | 完成 | 2026-02-20 |
| MCP Tools (47) | 完成 | 2026-02-20 |
| Examples | 完成 | 2025-12-03 |
| GameKit Roadmap | 計画中 | 2025-12-29 |
| API Reference | 作成中 | - |

---

<div align="center">

[Back to Main README](../../../README.md) | [Getting Started](GETTING_STARTED.md)

</div>
