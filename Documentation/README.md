# Unity-AI-Forge - AI-Powered Unity Development Toolkit

**Forge Unity games through AI collaboration. Model Context Protocol integration with GameKit framework.**

[![Python](https://img.shields.io/badge/Python-3.10%2B-blue)](https://www.python.org/)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black)](https://unity.com/)
[![MCP](https://img.shields.io/badge/MCP-0.9.0%2B-green)](https://modelcontextprotocol.io/)
[![Version](https://img.shields.io/badge/Version-2.1.0-brightgreen)](https://github.com/kuroyasouiti/Unity-AI-Forge/releases)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 🆕 What's New in v2.1.0

- **💾 State Persistence System**: Complete save/load functionality
  - Export/Import resource states as JSON
  - Save to files or PlayerPrefs
  - Automatic timestamp and metadata tracking
  - Cloud-save ready with serializable states
  - Manager convenience methods for easy integration

- **🎮 GameKit UICommand Extended**: Manager control support
  - **NEW Target Types**: Actor OR Manager
  - **11 Command Types**: Move/Jump/Action/Look/Custom + AddResource/SetResource/ConsumeResource/ChangeState/NextTurn/TriggerScene
  - UI buttons can now control game economy, states, and turns
  - Perfect for strategy games, shop UIs, and resource management

- **📊 GameKit Machinations Enhanced**: Diagram execution
  - ProcessDiagramFlows() - Execute automatic resource flows
  - CheckDiagramTriggers() - Monitor threshold events
  - ExecuteConverter() - Run specific resource conversions
  - SetFlowEnabled() - Dynamic flow control at runtime
  - Auto-execution modes for flows and triggers

- **🎯 Simplified ResourceManager**: Focus on core functionality
  - Pure resource storage and event management
  - Complex logic moved to external controllers or Machinations
  - Better performance (no Update() overhead by default)
  - Cleaner separation of concerns

### Previous Release (v2.0.0) Highlights

- **🎯 Hub-Based Architecture**: All GameKit components redesigned as intelligent hubs
- **🎮 GameKit Actor**: 8 behavior profiles, 4 control modes, UnityEvents
- **⚙️ GameKit Manager Hub**: Dynamic mode-specific components (TurnBased, ResourcePool, EventHub, StateManager, Realtime)
- **🎭 GameKit Interaction Hub**: Multi-trigger declarative system with specialized triggers
- **🎬 GameKit SceneFlow**: Scene-centric transition system with additive loading
- **🛤️ Spline Movement**: 2.5D spline-based movement with Catmull-Rom interpolation

- **🛤️ Spline Movement**: 2.5D spline-based movement
  - Catmull-Rom interpolation
  - Closed loops, lateral offsets, automatic rotation
  - Manual/automatic speed control

- **Mid-Level Tools**: Batch operations and presets
  - Transform/RectTransform batch operations (arrange, align, distribute)
  - Physics bundles (2D/3D presets: dynamic, kinematic, character, platformer, vehicle)
  - CharacterController bundle (fps, tps, platformer, child, large, narrow presets)
  - Camera rigs (follow, orbit, split-screen, fixed, dolly)
  - UI foundation (Canvas, Panel, Button, Text, Image, InputField)
  - Audio source bundles (music, sfx, ambient, voice, ui presets)
  - Input profiles (New Input System integration)

- **Compilation Wait Feature**: Automatic compilation handling
  - Operations execute first, then wait for compilation if triggered
  - Bridge reconnection detection for early wait release
  - Transparent wait information in responses

- **Comprehensive Test Suite**: 100+ unit tests
  - Unity Test Framework integration
  - 97.7% pass rate across all tool categories
  - CI/CD with GitHub Actions
  - Editor menu integration (`Tools > Unity-AI-Forge > Run All Tests`)

- **Documentation**: Complete overhaul
  - Test suite documentation and results
  - Tooling roadmap (Japanese)
  - Compilation wait feature guide
  - Legacy cleanup summary
  - [Full Release Notes](docs/Release_Notes_v1.8.0.md)
  - [Changelog](CHANGELOG.md)

## 📦 Package Structure

Unity-AI-Forge is a Unity package with integrated MCP server!

```
Unity-AI-Forge/
├── Assets/
│   └── UnityAIForge/                           # Unity Package
│       ├── Editor/
│       │   └── MCPBridge/                      # Unity C# WebSocket Bridge
│       ├── GameKit/                            # GameKit Framework Runtime
│       ├── MCPServer/                          # ⭐ MCP Server (Python, docs, tools)
│       │   ├── src/                            # Python MCP Server
│       │   ├── setup/                          # Installation scripts
│       │   ├── examples/                       # Practical tutorials
│       │   ├── config/                         # Configuration templates
│       │   └── docs/                           # Additional documentation
│       ├── Tests/                              # Test Suite
│       └── package.json                        # Unity Package definition
```

## 🚀 Quick Start

### 1. Install Unity Package

**Option A: Via Unity Package Manager (Recommended)**

1. Open Unity Editor
2. Open **Window > Package Manager**
3. Click **+ (Plus)** button → **Add package from git URL...**
4. Enter: `https://github.com/kuroyasouiti/Unity-AI-Forge.git?path=/Assets/UnityAIForge`
5. Click **Add**

**Option B: Manual Installation**

1. Download this repository
2. Copy `Assets/UnityAIForge` to your Unity project's `Assets/` folder

### 2. Install MCP Server

The MCP server is located in `Assets/UnityAIForge/MCPServer/`.

**Option A: Automatic Installation via Unity (Recommended)**

1. Open Unity Editor with the package installed
2. Go to **Tools > Unity-AI-Forge > MCP Server Manager**
3. Click **Install Server** (installs to `~/Unity-AI-Forge`)
4. Click **Register** for your AI tool (Cursor, Claude Desktop, etc.)
5. Restart your AI tool

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

**Option C: Manual Configuration**

Add to your Claude Desktop config (`~/.claude/claude_desktop_config.json`):
```json
{
  "mcpServers": {
    "unity-ai-forge": {
      "command": "uv",
      "args": ["--directory", "/path/to/Unity-AI-Forge", "run", "unity-ai-forge"],
      "env": {
        "MCP_SERVER_TRANSPORT": "stdio",
        "MCP_LOG_LEVEL": "info"
      }
    }
  }
}
```

Replace `/path/to/Unity-AI-Forge` with:
- Windows: `C:\Users\YOUR_USERNAME\Unity-AI-Forge`
- macOS/Linux: `/Users/YOUR_USERNAME/Unity-AI-Forge` or `/home/YOUR_USERNAME/Unity-AI-Forge`

### 3. Start Unity Bridge

1. Open Unity Editor with your project
2. Go to **Tools > MCP Assistant**
3. Click **Start Bridge**
4. Wait for "Connected" status

### 4. Test Connection

In Claude Desktop, ask:
```
Can you test the Unity MCP connection?
```

The AI should call `unity_ping()` and show Unity version information.

## 📚 Documentation

### For Users

- **[MCP Server QUICKSTART](Assets/UnityAIForge/MCPServer/QUICKSTART.md)** - Get started in 5 minutes
- **[MCP Server README](Assets/UnityAIForge/MCPServer/README.md)** - Complete MCP server documentation
- **[Installation Guide](Assets/UnityAIForge/MCPServer/INSTALL_GUIDE.md)** - Detailed installation instructions
- **[Examples](Assets/UnityAIForge/MCPServer/examples/)** - Practical tutorials and walkthroughs

### For Developers

- **[CLAUDE.md](CLAUDE.md)** - Instructions for Claude Code integration
- **[Test Suite](Assets/UnityAIForge/Tests/Editor/README.md)** - Comprehensive test suite for all tools
- **[Documentation Index](docs/)** - Additional guides and release notes

## 🏗️ Architecture

```
AI Client (Claude/Cursor) <--(MCP)--> Python MCP Server <--(WebSocket)--> Unity C# Bridge
                                      (MCPServer/src/)         (Editor/MCPBridge/)
```

### Components

| Component | Location | Description |
|-----------|----------|-------------|
| **Unity C# Bridge** | `Assets/UnityAIForge/Editor/MCPBridge/` | WebSocket server running inside Unity Editor |
| **Python MCP Server** | `Assets/UnityAIForge/MCPServer/src/` | MCP protocol implementation |
| **GameKit Framework** | `Assets/UnityAIForge/GameKit/Runtime/` | High-level game development components |
| **Setup Scripts** | `Assets/UnityAIForge/MCPServer/setup/` | Installation and configuration helpers |
| **Examples** | `Assets/UnityAIForge/MCPServer/examples/` | Practical tutorials and guides |
| **Tests** | `Assets/UnityAIForge/Tests/Editor/` | Comprehensive test suite |

## 🧪 Testing

Comprehensive test suite powered by Unity Test Framework:

- **100+ unit tests** covering all tool categories
- **Automated CI/CD** with GitHub Actions
- **Editor menu integration** for quick test execution
- **Command-line test runners** for batch testing

Run tests via:
- Unity Editor: `Tools > Unity-AI-Forge > Run All Tests`
- PowerShell: `.\run-tests.ps1`
- Bash: `./run-tests.sh`

See [Test Suite Documentation](Assets/Unity-AI-Forge/Tests/Editor/README.md) for details.

## ✨ Features

### Core Tools

- **Scene Management** - Create, load, save, delete, inspect scenes
- **GameObject CRUD** - Full hierarchy manipulation with batch operations
- **Component CRUD** - Add, update, remove components with batch support
- **Asset Operations** - Rename, duplicate, delete, inspect, update importer settings
- **ScriptableObject Management** - Create, inspect, update, delete, duplicate, find ScriptableObject assets
- **Prefab Management** (`unity_prefab_crud`) - Create prefabs from GameObjects, update, inspect, instantiate in scene, unpack, apply/revert overrides
- **Vector Sprite Conversion** (`unity_vector_sprite_convert`) - Generate sprites from primitives (square, circle, triangle, polygon), import SVG, convert textures, create solid color sprites
- **Project Settings** - Configure player, quality, time, physics, audio, and editor settings
- **Tags & Layers** - Add or remove tags and layers via the project settings tool

### Mid-Level Batch Tools

- **Transform Batch** (`unity_transform_batch`) - Arrange objects in circles/lines, sequential/list-based renaming, auto-generate menu hierarchies
- **RectTransform Batch** (`unity_rectTransform_batch`) - Set anchors/pivot/size/position, align to parent presets, distribute horizontally/vertically, match size from source
- **Physics Bundle** (`unity_physics_bundle`) - Apply 2D/3D Rigidbody + Collider presets (dynamic, kinematic, static, character, platformer, topDown, vehicle, projectile), update individual physics properties, inspect physics components
- **Camera Rig** (`unity_camera_rig`) - Create camera rigs (follow, orbit, split-screen, fixed, dolly) with target tracking, smooth movement, and viewport configuration
- **UI Foundation** (`unity_ui_foundation`) - Create UI elements (Canvas, Panel, Button, Text, Image, InputField) with anchor presets, TextMeshPro support, and automatic layout
- **Audio Source Bundle** (`unity_audio_source_bundle`) - Create and configure AudioSource with presets (music, sfx, ambient, voice, ui), 2D/3D spatial audio, and mixer group integration
- **Input Profile** (`unity_input_profile`) - Create PlayerInput with New Input System, configure action maps, set notification behaviors, and create InputActions assets

### High-Level GameKit Tools

- **GameKit Actor** (`unity_gamekit_actor`) - Create game actors as controller-to-behavior hubs that relay input via UnityEvents (OnMoveInput, OnJumpInput, OnActionInput, OnLookInput)
- **GameKit Manager** (`unity_gamekit_manager`) - Create game managers (turn-based, realtime, resource pool, event hub, state manager) with persistence, turn phases, and resource management
- **GameKit Interaction** (`unity_gamekit_interaction`) - Create interaction triggers (collision, raycast, proximity, input) with declarative actions (spawn, destroy, sound, message, scene change) and conditions
- **GameKit UI Command** (`unity_gamekit_ui_command`) - Create command panels with buttons that send commands to actors with UI command control mode, supporting horizontal/vertical/grid layouts
- **GameKit SceneFlow** (`unity_gamekit_sceneflow`) - Manage scene transitions with state machine, additive loading, persistent manager scene, shared scene groups (UI/Audio), and trigger-based transitions

## 📦 ScriptableObject Management Example

```python
# Create a ScriptableObject asset
unity_scriptableobject_manage({
    "operation": "create",
    "typeName": "MyGame.Data.GameConfig",
    "assetPath": "Assets/Data/DefaultConfig.asset",
    "properties": {
        "gameName": "Adventure Quest",
        "maxPlayers": 8,
        "gameSpeed": 1.5,
        "enableDebugMode": True
    }
})

# Inspect properties
config_info = unity_scriptableobject_manage({
    "operation": "inspect",
    "assetPath": "Assets/Data/DefaultConfig.asset",
    "includeProperties": True
})

# Update selected values
unity_scriptableobject_manage({
    "operation": "update",
    "assetPath": "Assets/Data/DefaultConfig.asset",
    "properties": {
        "maxPlayers": 16,
        "gameSpeed": 2.0
    }
})

# Duplicate for experimentation
unity_scriptableobject_manage({
    "operation": "duplicate",
    "sourceAssetPath": "Assets/Data/DefaultConfig.asset",
    "destinationAssetPath": "Assets/Data/HighSpeedConfig.asset"
})

# List all configs in a folder
all_configs = unity_scriptableobject_manage({
    "operation": "findByType",
    "typeName": "MyGame.Data.GameConfig",
    "searchPath": "Assets/Data",
    "includeProperties": False
})
```

## 🛠️ Development

### Project Structure

```
Unity-AI-Forge/
├── Assets/
│   └── UnityAIForge/                # Unity Package
│       ├── Editor/
│       │   └── MCPBridge/           # Unity C# Bridge
│       │       ├── McpBridgeService.cs
│       │       ├── McpCommandProcessor.cs
│       │       ├── McpContextCollector.cs
│       │       └── Handlers/        # Tool implementations
│       ├── GameKit/
│       │   └── Runtime/             # GameKit Framework
│       │       ├── Actor/
│       │       ├── Manager/
│       │       ├── Interaction/
│       │       └── SceneFlow/
│       ├── MCPServer/               # MCP Server (Python)
│       │   ├── src/                 # Server source
│       │   │   ├── bridge/          # Unity Bridge communication
│       │   │   ├── tools/           # MCP tool definitions
│       │   │   ├── resources/       # MCP resources
│       │   │   └── main.py          # Entry point
│       │   ├── setup/               # Installation scripts
│       │   ├── examples/            # Tutorials
│       │   ├── config/              # Configuration templates
│       │   ├── skill.yml            # MCP server manifest
│       │   └── pyproject.toml       # Python package config
│       ├── Tests/
│       │   └── Editor/              # Unity Test Framework tests
│       └── package.json             # Unity Package definition
│
├── ProjectSettings/                 # Unity project settings
├── Packages/                        # Unity packages
├── docs/                            # Project documentation
└── README.md                        # This file
```

### Install Dev Dependencies

```bash
cd Unity-AI-Forge
uv sync --dev
```

### Run Tests

```bash
cd Unity-AI-Forge
pytest
```

### Format Code

```bash
cd Unity-AI-Forge
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

See [CLAUDE.md](CLAUDE.md) for development guidelines.

## 📄 License

MIT License - see [MIT License](https://opensource.org/licenses/MIT) for details.

## 🙏 Acknowledgments

- **Model Context Protocol** by Anthropic
- **Unity Technologies** for the amazing game engine
- All contributors and community members

## 🆘 Support

- **Quick Start**: [Assets/UnityAIForge/MCPServer/QUICKSTART.md](Assets/UnityAIForge/MCPServer/QUICKSTART.md)
- **Examples**: [Assets/UnityAIForge/MCPServer/examples/](Assets/UnityAIForge/MCPServer/examples/)
- **Installation Guide**: [Assets/UnityAIForge/MCPServer/INSTALL_GUIDE.md](Assets/UnityAIForge/MCPServer/INSTALL_GUIDE.md)
- **Issues**: [GitHub Issues](https://github.com/kuroyasouiti/Unity-AI-Forge/issues)

## 🔄 Migration from Old Structure

If you were using the old structure:

1. **Unity Side**: Install via Unity Package Manager using the correct path:
   ```
   https://github.com/kuroyasouiti/Unity-AI-Forge.git?path=/Assets/UnityAIForge
   ```
2. **MCP Server Side**: Use Unity's MCP Server Manager:
   - Go to **Tools > Unity-AI-Forge > MCP Server Manager**
   - Click **Install Server** to copy files to `~/.claude/skills/Unity-AI-Forge`
   - Click **Register** for your AI tool
3. Remove old installation files if desired

---

**Made with ❤️ for the Unity and AI community**

**Start building amazing Unity projects with AI assistance today!** 🚀
