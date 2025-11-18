# Skill for Unity

**Comprehensive Unity Editor integration through Model Context Protocol**

[![Python](https://img.shields.io/badge/Python-3.10%2B-blue)](https://www.python.org/)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black)](https://unity.com/)
[![MCP](https://img.shields.io/badge/MCP-0.9.0%2B-green)](https://modelcontextprotocol.io/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 🎯 What is Skill for Unity?

Skill for Unity enables AI assistants (Claude, Cursor, etc.) to interact with Unity Editor in real-time through the Model Context Protocol. Control scenes, GameObjects, components, UI, assets, and more—all through natural language commands.

## ✨ Key Features

- **30+ Unity Tools** - Complete control over Unity Editor
- **Real-time Bridge** - WebSocket-based bidirectional communication
- **Template System** - Quickly create GameObjects and UI with templates
- **Automatic Compilation** - Detects and waits for script compilation
- **Comprehensive Documentation** - Extensive guides and examples

## 🚀 Quick Start

### 1. Installation

**Windows (PowerShell):**
```powershell
cd SkillForUnity
.\setup\install.ps1
```

**Linux/macOS:**
```bash
cd SkillForUnity
./setup/install.sh
```

### 2. Unity Setup

1. Open your Unity project
2. Import the MCPBridge package from `Assets/SkillForUnity/Editor/MCPBridge/`
3. Go to **Tools > MCP Assistant**
4. Click **Start Bridge**

### 3. Configure MCP Client

Run the configuration helper:
```bash
python setup/configure.py
```

This generates configuration files in `config/` directory. Copy the appropriate one to your MCP client.

**For Claude Desktop:**
```json
{
  "mcpServers": {
    "skill-for-unity": {
      "command": "uv",
      "args": ["run", "--directory", "/path/to/SkillForUnity", "src/main.py"],
      "env": {
        "MCP_SERVER_TRANSPORT": "stdio",
        "MCP_LOG_LEVEL": "info"
      }
    }
  }
}
```

### 4. Test the Connection

```python
# Verify connection
unity_ping()

# Create a simple scene
unity_scene_quickSetup({"setupType": "3D"})
```

## 📚 Documentation

- **[QUICKSTART.md](QUICKSTART.md)** - Fast introduction with common commands
- **[examples/](examples/)** - Practical examples and tutorials
- **[docs/](docs/)** - Comprehensive documentation

### Documentation Structure

```
docs/
├── api-reference/       # Detailed API documentation
│   ├── scene-tools.md
│   ├── gameobject-tools.md
│   ├── component-tools.md
│   └── ui-tools.md
├── guides/              # How-to guides
│   ├── getting-started.md
│   ├── best-practices.md
│   └── performance-tips.md
└── troubleshooting.md   # Common issues and solutions
```

## 🎮 Example: Create a 3D Game Scene

```python
# Set up 3D scene with camera and lighting
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

# Add some obstacles
unity_gameobject_createFromTemplate({
    "template": "Cube",
    "name": "Wall1",
    "position": {"x": 5, "y": 0.5, "z": 0}
})

unity_gameobject_createFromTemplate({
    "template": "Cube",
    "name": "Wall2",
    "position": {"x": -5, "y": 0.5, "z": 0}
})
```

See [examples/01-basic-scene-setup.md](examples/01-basic-scene-setup.md) for full tutorial.

## 🛠️ Available Tools

### Core Operations

| Category | Tools | Description |
|----------|-------|-------------|
| **Scene** | `scene.crud` | Create, load, save, delete scenes |
| **GameObject** | `gameobject.crud`, `gameobject.createFromTemplate` | Hierarchy management |
| **Component** | `component.crud` | Add, update, remove components |
| **UI** | `ugui.createFromTemplate`, `ugui.layoutManage` | UI creation and layout |
| **Asset** | `asset.crud` | Asset file operations |
| **Prefab** | `prefab.crud` | Prefab workflow |

### Advanced Features

- **Tilemap Design** - 2D tilemap operations
- **NavMesh** - Navigation mesh baking and agents
- **Input System** - New Input System management
- **Project Settings** - Configure Unity project settings
- **Render Pipeline** - Manage render pipeline settings

## 🏗️ Architecture

```
AI Client (Claude/Cursor) <--(MCP)--> Python MCP Server <--(WebSocket)--> Unity C# Bridge
```

**Components:**
- **Python MCP Server** (`src/`) - Model Context Protocol implementation
- **Unity C# Bridge** (`Assets/SkillForUnity/Editor/MCPBridge/`) - WebSocket server in Unity Editor

## 💻 Development

### Install with Dev Dependencies

```bash
uv sync --dev
```

### Run Tests

```bash
pytest
```

### Format Code

```bash
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

## 📄 License

MIT License - see [MIT License](https://opensource.org/licenses/MIT) for details

## 🆘 Support

- **Issues**: Report bugs and request features on GitHub
- **Documentation**: Check [docs/troubleshooting.md](docs/troubleshooting.md)
- **Examples**: See [examples/](examples/) for practical guides

## 🙏 Acknowledgments

- Model Context Protocol by Anthropic
- Unity Technologies
- All contributors

---

**Made with ❤️ for the Unity and AI community**
