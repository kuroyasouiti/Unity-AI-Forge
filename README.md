# Unity-AI-Forge

<div align="center">

🎮 **AI-powered Unity Development Toolkit**

Unity × AI で、ゲーム開発を革新的に効率化

[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity)](https://unity.com/)
[![MCP](https://img.shields.io/badge/MCP-Integration-blue)](https://modelcontextprotocol.io/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

[English](Assets/UnityAIForge/Documentation/README.md) | [日本語](Assets/UnityAIForge/Documentation/README_ja.md)

</div>

---

## 🚀 Quick Start

**Unity Package Manager からインストール（推奨）**

1. Unity Editor を開く
2. **Window > Package Manager** を開く
3. **+** ボタン → **Add package from git URL...**
4. 以下を入力:
   ```
   https://github.com/kuroyasouiti/Unity-AI-Forge.git?path=/Assets/UnityAIForge
   ```
5. **Add** をクリック

📖 **[Getting Started Guide →](Assets/UnityAIForge/Documentation/GETTING_STARTED.md)**

---

## ✨ Features

### 🤖 AI Integration (49 Tools)
Model Context Protocol で Unity を AI から操作
- Scene/GameObject/Component/Asset/Prefab/ScriptableObject の完全制御
- バッチ操作とパターンマッチング（Transform, RectTransform, Physics, Camera, Audio, etc.）
- UI Toolkit サポート（UXML/USS 生成, UIDocument 管理）
- レジューム機能付き逐次処理
- 宣言的UIシステム (Hierarchy/State/Navigation)
- シーン整合性検証・依存関係グラフ・参照解析

### 🎮 GameKit Framework
コード生成によるゼロ依存のゲームシステムフレームワーク
- **GameKit UI**: UICommand, UIBinding, UIList, UISlot, UISelection（UI Toolkit ベース）
- **GameKit Systems**: ObjectPool, EventChannel, DataContainer, RuntimeSet
- **Logic**: シーン整合性検証, クラス依存関係グラフ, 型カタログ, シーン参照解析

---

## 📚 Documentation

| 📖 ドキュメント | 説明 |
|:---|:---|
| [**📑 INDEX**](Assets/UnityAIForge/Documentation/INDEX.md) | 全ドキュメント索引 |
| [**🚀 Getting Started**](Assets/UnityAIForge/Documentation/GETTING_STARTED.md) | 初心者ガイド |
| [**⚙️ Installation**](Assets/UnityAIForge/Documentation/Installation/QUICKSTART.md) | インストール手順 |
| [**🎮 GameKit Guide**](Assets/UnityAIForge/Documentation/MCPServer/SKILL_GAMEKIT.md) | GameKit 完全ガイド |
| [**🔧 MCP Tools**](Assets/UnityAIForge/Documentation/MCPServer/SKILL.md) | 全49ツール解説 |
| [**📝 Examples**](Assets/UnityAIForge/Documentation/Examples/README.md) | 使用例集 |

---

## 🎯 Why Unity-AI-Forge?

| 従来の開発 | Unity-AI-Forge |
|:---|:---|
| 手動でコンポーネント設定 | AI が自然言語で 49 ツールを駆使し自動生成 |
| UI コードを一から手書き | GameKit UI ピラーで UI Toolkit コンポーネントをコード生成 |
| エフェクト・サウンドを個別設定 | Presentation ピラーで演出・音響を宣言的に構築 |
| 壊れた参照を目視で確認 | Logic ピラーでシーン整合性・依存関係を自動検証 |
| 生成コードがツールに依存 | コード生成はゼロ依存 — パッケージ削除後もそのまま動作 |

---

## 🛠️ Requirements

- **Unity**: 2022.3 LTS or later
- **Python**: 3.10+ (MCP Server 用)
- **.NET**: Standard 2.1

---

## 📖 Learn More

- [📚 Full Documentation](Assets/UnityAIForge/Documentation/INDEX.md)
- [📝 Changelog](Assets/UnityAIForge/Documentation/CHANGELOG.md)
- [🤖 MCP Integration Guide](Assets/UnityAIForge/Documentation/MCPServer/README.md)

---

## 💬 Community

- **Issues**: [GitHub Issues](https://github.com/kuroyasouiti/Unity-AI-Forge/issues)
- **Discussions**: [GitHub Discussions](https://github.com/kuroyasouiti/Unity-AI-Forge/discussions)

---

## 📜 License

MIT License - See [LICENSE](LICENSE)

---

<div align="center">

**Made with ❤️ by Unity-AI-Forge Team**

⭐ Star this repo if you find it useful!

</div>

