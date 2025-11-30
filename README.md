# Unity-AI-Forge

AI-powered Unity game development toolkit with Model Context Protocol (MCP) integration.

🎮 **Unity × AI** で、ゲーム開発を革新的に効率化するツールキットです。

---

## 📚 Documentation

**すべてのドキュメントは [Documentation](Documentation/) フォルダにまとめられています。**

### クイックリンク

| 📖 ドキュメント | 説明 |
|----------------|------|
| [📑 **ドキュメント索引**](Documentation/INDEX.md) | すべてのドキュメントへの索引 |
| [🇯🇵 **日本語 README**](Documentation/README_ja.md) | プロジェクト概要（日本語） |
| [🇬🇧 **English README**](Documentation/README.md) | Project overview (English) |
| [🚀 **クイックスタート**](Documentation/Installation/QUICKSTART.md) | 最速で始める |
| [📦 **インストールガイド**](Documentation/Installation/INSTALL_GUIDE.md) | 詳細な手順 |

### GameKit ドキュメント

| コンポーネント | 説明 |
|---------------|------|
| [💰 **ResourceManager**](Documentation/GameKit/GameKitResourceManager.README.md) | リソース管理システム |
| [📊 **Machinations**](Documentation/GameKit/GameKitMachinations.README.md) | 経済システムのアセット化 |
| [🔄 **SceneFlow**](Documentation/GameKit/GameKitSceneFlow.README.md) | シーン遷移管理 |
| [🎯 **Interaction**](Documentation/GameKit/GameKitInteraction.README.md) | インタラクションシステム |
| [🎨 **UICommand**](Documentation/GameKit/GameKitUICommand.README.md) | UIコマンドシステム |
| [🛤️ **SplineMovement**](Documentation/GameKit/SplineMovement.README.md) | スプライン移動 |
| [🗺️ **GraphNodeMovement**](Documentation/GameKit/GraphNodeMovement.README.md) | グラフベース移動 |

---

## ⚡ Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/kuroyasouiti/Unity-AI-Forge.git

# 2. Open in Unity (2022.3 or later)
# Unity Hub > Add > Select the cloned folder

# 3. Install MCP Server (optional, for AI integration)
# See: Documentation/Installation/QUICKSTART.md
```

**詳細は [クイックスタートガイド](Documentation/Installation/QUICKSTART.md) をご覧ください。**

---

## 🎯 Features

### 🤖 AI Integration (MCP)
- **Model Context Protocol** によるAIアシスタント連携
- Unity Editor を AI から直接操作
- 自然言語でゲームオブジェクト生成・編集

### 🎮 GameKit Framework
- **Resource Management** - Machinations風の経済システム
- **Scene Flow** - 柔軟なシーン遷移管理
- **Interaction System** - イベント駆動のインタラクション
- **Movement Systems** - スプライン、グラフベース移動
- **UI Command** - UIとゲームロジックの連携

### 📦 Asset Management
- ScriptableObject ベースの設定管理
- 再利用可能な経済システム・ステートマシン
- Git でバージョン管理可能

---

## 📖 Documentation Structure

```
Documentation/
├── INDEX.md                    # 📑 全ドキュメント索引
├── README.md & README_ja.md    # プロジェクト概要
├── CHANGELOG.md                # 変更履歴
├── Installation/               # インストール手順
├── MCPServer/                  # MCP サーバー
├── Examples/                   # 使用例
├── GameKit/                    # GameKit ドキュメント
├── Handlers/                   # ハンドラー
└── Testing/                    # テスト
```

**👉 [完全なドキュメント索引はこちら](Documentation/INDEX.md)**

---

## 🛠️ Requirements

- **Unity**: 2022.3 LTS or later
- **Python**: 3.11+ (for MCP Server)
- **UV**: Python package manager (optional)

---

## 📜 License

MIT License - 詳細は [LICENSE](LICENSE) ファイルをご覧ください。

---

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) first.

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/kuroyasouiti/Unity-AI-Forge/issues)
- **Discussions**: [GitHub Discussions](https://github.com/kuroyasouiti/Unity-AI-Forge/discussions)

---

## 🌟 Related Projects

- [Model Context Protocol](https://modelcontextprotocol.io/)
- [Unity ML-Agents](https://github.com/Unity-Technologies/ml-agents)

---

**Made with ❤️ by the Unity-AI-Forge community**

