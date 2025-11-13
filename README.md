# UnityMCP - Unity Editor Integration via Model Context Protocol

**Enable AI assistants to control Unity Editor in real-time through the Model Context Protocol.**

[![Python](https://img.shields.io/badge/Python-3.10%2B-blue)](https://www.python.org/)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black)](https://unity.com/)
[![MCP](https://img.shields.io/badge/MCP-0.9.0%2B-green)](https://modelcontextprotocol.io/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## 📦 What's New: Skill Package Structure

UnityMCP has been restructured as a **Claude Agent Skill** for easier setup and distribution!

```
UnityMCP/
├── Assets/Editor/MCPBridge/    # Unity C# WebSocket Bridge
└── SkillPackage/                # ⭐ Standalone MCP Skill Package
    ├── src/                     # Python MCP Server
    ├── setup/                   # Installation scripts
    ├── examples/                # Practical tutorials
    ├── docs/                    # Comprehensive documentation
    └── config/                  # Configuration templates
```

## 🚀 Quick Start

### 1. Install the Skill Package

**Navigate to SkillPackage directory:**

```bash
cd SkillPackage
```

**Windows:**
```powershell
.\setup\install.ps1
```

**Linux/macOS:**
```bash
./setup/install.sh
```

### 2. Start Unity Bridge

1. Open Unity Editor with this project
2. Go to **Tools > MCP Assistant**
3. Click **Start Bridge**
4. Wait for "Connected" status

### 3. Configure Your MCP Client

**Claude Desktop** - Add to your config:
```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "uv",
      "args": ["run", "--directory", "/path/to/SkillPackage", "src/main.py"],
      "env": {
        "MCP_SERVER_TRANSPORT": "stdio",
        "MCP_LOG_LEVEL": "info"
      }
    }
  }
}
```

Or run the configuration helper:
```bash
cd SkillPackage
python setup/configure.py
```

### 4. Test Connection

```
Can you test the Unity MCP connection?
```

The AI should call `unity_ping()` and show Unity version information.

## 📚 Documentation

### For Users

- **[SkillPackage/QUICKSTART.md](SkillPackage/QUICKSTART.md)** - Get started in 5 minutes
- **[SkillPackage/README.md](SkillPackage/README.md)** - Complete skill documentation
- **[SkillPackage/examples/](SkillPackage/examples/)** - Practical examples and tutorials

### For Developers

- **[SkillPackage/docs/](SkillPackage/docs/)** - API reference and guides
- **[CLAUDE.md](CLAUDE.md)** - Instructions for Claude Code integration
- **[AGENTS.md](AGENTS.md)** - Repository guidelines

## 🏗️ Architecture

```
AI Client (Claude/Cursor) <--(MCP)--> Python MCP Server <--(WebSocket)--> Unity C# Bridge
                                      (SkillPackage/src/)                   (Assets/Editor/)
```

### Components

| Component | Location | Description |
|-----------|----------|-------------|
| **Unity C# Bridge** | `Assets/Editor/MCPBridge/` | WebSocket server running inside Unity Editor |
| **Python MCP Server** | `SkillPackage/src/` | MCP protocol implementation |
| **Setup Scripts** | `SkillPackage/setup/` | Installation and configuration helpers |
| **Examples** | `SkillPackage/examples/` | Practical tutorials and guides |
| **Documentation** | `SkillPackage/docs/` | API reference and best practices |

## ✨ Features

### Core Capabilities

- **30+ Unity Tools** - Complete control over Unity Editor
- **Scene Management** - Create, load, save, delete scenes
- **GameObject Operations** - Full hierarchy manipulation
- **Component Editing** - Add, update, remove components
- **UI Creation** - Templates for buttons, panels, text, etc.
- **Asset Management** - Asset file operations
- **Prefab Workflow** - Create and manage prefabs
- **Batch Operations** - Execute multiple commands efficiently

### Advanced Features

- **Tilemap Design** - 2D tilemap operations
- **NavMesh Operations** - Navigation mesh and agents
- **Input System** - New Input System management
- **Project Settings** - Configure Unity settings
- **Render Pipeline** - Manage render pipeline settings
- **Automatic Compilation** - Detects and waits for Unity compilation

## 🎮 Example: Create a 3D Game Scene

```python
# Set up a 3D scene
unity_scene_quickSetup({"setupType": "3D"})

# Create ground
unity_gameobject_createFromTemplate({
    "template": "Plane",
    "name": "Ground",
    "scale": {"x": 10, "y": 1, "z": 10}
})

# Create player
unity_gameobject_createFromTemplate({
    "template": "Player",
    "name": "Player",
    "position": {"x": 0, "y": 1, "z": 0}
})

# Add obstacles
unity_batch_execute({
    "operations": [
        {"tool": "gameObjectCreateFromTemplate",
         "payload": {"template": "Cube", "name": "Wall1",
                    "position": {"x": 5, "y": 0.5, "z": 0}}},
        {"tool": "gameObjectCreateFromTemplate",
         "payload": {"template": "Cube", "name": "Wall2",
                    "position": {"x": -5, "y": 0.5, "z": 0}}}
    ]
})
```

See [SkillPackage/examples/](SkillPackage/examples/) for more tutorials.

## 🛠️ Development

### Project Structure

```
UnityMCP/
├── Assets/
│   └── Editor/
│       └── MCPBridge/           # Unity C# Bridge
│           ├── McpBridgeService.cs
│           ├── McpCommandProcessor.cs
│           └── McpContextCollector.cs
│
├── SkillPackage/                # Python MCP Skill
│   ├── src/                     # MCP Server source
│   │   ├── bridge/              # Unity Bridge communication
│   │   ├── tools/               # MCP tool definitions
│   │   ├── resources/           # MCP resources
│   │   └── main.py              # Entry point
│   ├── setup/                   # Installation scripts
│   ├── examples/                # Tutorials
│   ├── docs/                    # Documentation
│   ├── config/                  # Configuration templates
│   ├── skill.yml                # Skill manifest
│   └── pyproject.toml           # Python package config
│
├── ProjectSettings/             # Unity project settings
├── Packages/                    # Unity packages
└── README.md                    # This file
```

### Install Dev Dependencies

```bash
cd SkillPackage
uv sync --dev
```

### Run Tests

```bash
cd SkillPackage
pytest
```

### Format Code

```bash
cd SkillPackage
black src/
ruff check src/
```

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests and documentation
5. Submit a pull request

See [AGENTS.md](AGENTS.md) for coding guidelines.

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

## 🙏 Acknowledgments

- **Model Context Protocol** by Anthropic
- **Unity Technologies** for the amazing game engine
- All contributors and community members

## 🆘 Support

- **Quick Start**: [SkillPackage/QUICKSTART.md](SkillPackage/QUICKSTART.md)
- **Examples**: [SkillPackage/examples/](SkillPackage/examples/)
- **Troubleshooting**: [SkillPackage/docs/troubleshooting.md](SkillPackage/docs/troubleshooting.md)
- **Issues**: [GitHub Issues](https://github.com/yourusername/unity-mcp/issues)

## 🔄 Migration from Old Structure

If you were using the old structure (`Assets/Runtime/MCPServer/`):

1. The MCP server has been moved to `SkillPackage/src/`
2. Update your MCP client configuration to point to the new location
3. Run `SkillPackage/setup/install.ps1` (Windows) or `setup/install.sh` (Linux/macOS)
4. The Unity Bridge (`Assets/Editor/MCPBridge/`) remains unchanged

---

**Made with ❤️ for the Unity and AI community**

**Start building amazing Unity projects with AI assistance today!** 🚀
