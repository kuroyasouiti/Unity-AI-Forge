# Unity-AI-Forge MCP Server

**AI-powered Unity development toolkit - Model Context Protocol integration**

[![Python](https://img.shields.io/badge/Python-3.10%2B-blue)](https://www.python.org/)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black)](https://unity.com/)
[![MCP](https://img.shields.io/badge/MCP-0.9.0%2B-green)](https://modelcontextprotocol.io/)
[![Version](https://img.shields.io/badge/Version-2.0.0-brightgreen)](https://github.com/kuroyasouiti/Unity-AI-Forge/releases)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 🆕 What's New in v2.0.0

- **Project Renamed** - Unity-AI-Forge → Unity-AI-Forge
- **Hub-Based Architecture** - All GameKit components redesigned as intelligent hubs
- **GameKit 2.0** - 8 movement modes, 5 manager types, Machinations support
- **Enhanced Interactions** - TilemapCell, GraphNode, SplineProgress triggers
- **Scene-Centric Flow** - Same trigger, different destinations per scene

## 🎯 What is Unity-AI-Forge?

Unity-AI-Forge enables AI assistants (Claude, Cursor, etc.) to forge Unity games through intelligent collaboration. Real-time Unity Editor control via Model Context Protocol with powerful GameKit framework for rapid game development.

## ✨ Key Features

- **31+ Unity Tools** - Complete control over Unity Editor
- **Real-time Bridge** - WebSocket-based bidirectional communication
- **ScriptableObject Management** - Create, inspect, update, and manage ScriptableObject assets
- **Template System** - Quickly create GameObjects and UI with templates
- **Automatic Compilation** - Detects and waits for script compilation
- **Comprehensive Documentation** - Extensive guides and examples

## 🚀 Quick Start

### 1. Install Unity Package

**Option A: Via Unity Package Manager (Recommended)**

1. Open Unity Editor
2. Open **Window > Package Manager**
3. Click **+ (Plus)** button → **Add package from git URL...**
4. Enter: `https://github.com/kuroyasouiti/Unity-AI-Forge.git?path=/Assets/UnityAIForge`
5. Click **Add**

**Option B: Manual Installation**

1. Download the repository
2. Copy `Assets/UnityAIForge` to your Unity project's `Assets/` folder

### 2. Install MCP Server

**Option A: Automatic (Recommended)**

1. In Unity Editor, go to **Tools > Unity-AI-Forge > MCP Server Manager**
2. Click **Install Server** (installs to `~/Unity-AI-Forge`)
3. Click **Register** for your AI tool (Cursor, Claude Desktop, etc.)
4. Restart your AI tool

**Option B: Manual Setup**

```bash
# Windows (PowerShell)
xcopy /E /I /Y "Assets\UnityAIForge\MCPServer" "%USERPROFILE%\Unity-AI-Forge"
cd %USERPROFILE%\Unity-AI-Forge
uv sync

# macOS/Linux
cp -r Assets/UnityAIForge/MCPServer ~/Unity-AI-Forge
cd ~/Unity-AI-Forge
uv sync
```

Then configure your AI tool's MCP settings (see [INSTALL_GUIDE.md](INSTALL_GUIDE.md)).

### 3. Start Unity Bridge

1. In Unity Editor, go to **Tools > Unity-AI-Forge > MCP Assistant**
2. Click **Start Bridge**
3. Wait for "Connected" status

### 4. Test the Connection

In your AI tool (Claude, Cursor, etc.), ask:

```
Can you test the Unity MCP connection?
```

The AI should call `unity_ping()` and show Unity version information.

## 📚 Documentation

- **[QUICKSTART.md](QUICKSTART.md)** - Fast introduction with common commands
- **[examples/](examples/)** - Practical examples and tutorials
- **[docs/](docs/)** - Comprehensive documentation

### Documentation Structure

```
Assets/UnityAIForge/MCPServer/
├── QUICKSTART.md           # Fast introduction with common commands
├── INSTALL_GUIDE.md        # Detailed installation instructions
├── README.md               # This file
├── examples/               # Practical examples and tutorials
│   ├── 01-basic-scene-setup.md
│   ├── 02-ui-creation.md
│   ├── 03-game-level.md
│   ├── 04-prefab-workflow.md
│   └── 05-design-patterns.md
└── config/                 # Configuration templates
    ├── claude-desktop.json.example
    ├── cursor.json.example
    └── mcp-config.json.template
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

### High-Level Tools (Recommended)

| Category | Tools | Description |
|----------|-------|-------------|
| **Quick Setup** | `scene_quickSetup` | Instant scene setup (3D, 2D, UI, VR) |
| **Templates** | `gameobject_createFromTemplate`, `ugui_createFromTemplate` | Create from templates |
| **Menu Systems** | `menu_hierarchyCreate` | Create complete hierarchical menu systems with navigation |
| **Layouts** | `ugui_layoutManage` | UI layout management |
| **Patterns** | `designPattern_generate` | Generate design pattern code |
| **Templates** | `template_manage` | Customize GameObjects and create prefabs |

### Low-Level Tools (Core)

| Category | Tools | Description |
|----------|-------|-------------|
| **Scene** | `scene_crud` | Create, load, save, delete, inspect scenes |
| **GameObject** | `gameobject_crud` | Full hierarchy CRUD operations |
| **Component** | `component_crud` | Add, update, remove components with batch support |
| **Asset** | `asset_crud` | Asset operations and importer settings |
| **Prefab** | `prefab_crud` | Complete prefab workflow |
| **Script** | `script_template_generate` | Generate MonoBehaviour/ScriptableObject templates |

### Advanced Features

- **Project Settings** - Read/write Unity project settings (player, quality, time, physics, audio, editor)
- **Render Pipeline** - Manage render pipeline settings (Built-in, URP, HDRP)
- **Tags & Layers** - Manage tags and layers
- **Constants** - Convert Unity constants and values

## 🏗️ Architecture

```
AI Client (Claude/Cursor) <--(MCP)--> Python MCP Server <--(WebSocket)--> Unity C# Bridge
```

**Components:**
- **Python MCP Server** (`Assets/UnityAIForge/MCPServer/src/`) - Model Context Protocol implementation
- **Unity C# Bridge** (`Assets/UnityAIForge/Editor/MCPBridge/`) - WebSocket server in Unity Editor
- **GameKit Framework** (`Assets/UnityAIForge/GameKit/Runtime/`) - High-level game development components

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
