# SkillForUnity - Unity Editor Integration via Model Context Protocol

**Enable AI assistants to control Unity Editor in real-time through the Model Context Protocol.**

[![Python](https://img.shields.io/badge/Python-3.10%2B-blue)](https://www.python.org/)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black)](https://unity.com/)
[![MCP](https://img.shields.io/badge/MCP-0.9.0%2B-green)](https://modelcontextprotocol.io/)
[![Version](https://img.shields.io/badge/Version-1.7.1-brightgreen)](https://github.com/kuroyasouiti/SkillForUnity/releases)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 🆕 What's New in v1.7.1

- **Critical Bug Fixes**: Template tools, constant conversion, and SerializedField support
  - Fixed template tools (scene quick setup, GameObject templates, UI templates, design patterns, script templates) to work correctly
  - Fixed enum type resolution in constant conversion (now supports Unity 2024.2+ module system)
  - Added support for `[SerializeField]` private fields in Component and ScriptableObject operations
  - 99%+ performance improvement in type resolution through caching

- **New Features**:
  - `listCommonEnums` operation: Lists commonly used Unity enum types by category (Input, Rendering, Physics, UI, Audio, Animation, Scripting)
  - Enhanced error messages with helpful debugging information

- **Documentation**: Comprehensive technical docs and test reports for all improvements

## 📦 Skill Package Structure

SkillForUnity is structured as a **Claude Agent Skill** for easier setup and distribution!

```
SkillForUnity/
├── Assets/SkillForUnity/Editor/MCPBridge/    # Unity C# WebSocket Bridge + bundled Claude Skill zip
└── SkillForUnity/                            # ⭐ Claude Skill source (Python MCP server, docs, tools)
    ├── src/                     # Python MCP Server
    ├── setup/                   # Installation scripts
    ├── examples/                # Practical tutorials
    ├── docs/                    # Comprehensive documentation
    └── config/                  # Configuration templates
```

## 🚀 Quick Start

### 1. Install Unity Package

**Option A: Via Unity Package Manager (Recommended)**

1. Open Unity Editor
2. Open **Window > Package Manager**
3. Click **+ (Plus)** button → **Add package from git URL...**
4. Enter: `https://github.com/kuroyasouiti/SkillForUnity.git?path=/Assets/SkillForUnity`
5. Click **Add**

**Option B: Manual Installation**

1. Download this repository
2. Copy `Assets/SkillForUnity` to your Unity project's `Assets/` folder

### 2. Install Claude Skill Package

The Unity package already bundles the Claude Skill archive at `Assets/SkillForUnity/SkillForUnity.zip`.

**Option A: Copy the bundled zip to Claude Desktop's skills folder**

```bash
# Copy the Claude Skill zip
cp Assets/SkillForUnity/SkillForUnity.zip ~/.claude/skills/

# Extract to create ~/.claude/skills/SkillForUnity
cd ~/.claude/skills
unzip -o SkillForUnity.zip
```

**Option B: Register via MCP Window**

1. Open Claude Desktop
2. Open MCP Settings Window
3. Add new MCP server with the skill configuration

**Option C: Manual Configuration**

Add to your Claude Desktop config (`~/.claude/claude_desktop_config.json`):
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

- **[Claude Skill QUICKSTART](SkillForUnity/QUICKSTART.md)** - Get started in 5 minutes
- **[Claude Skill README](SkillForUnity/README.md)** - Complete skill documentation
- **[Claude Skill examples](SkillForUnity/examples/)** - Practical tutorials and walkthroughs

### For Developers

- **[Claude Skill docs](SkillForUnity/docs/)** - API reference and guides
- **[CLAUDE.md](CLAUDE.md)** - Instructions for Claude Code integration
- **[Best Practices guide](SkillForUnity/docs/guides/best-practices.md)** - Repository guidelines and tips

## 🏗️ Architecture

```
AI Client (Claude/Cursor) <--(MCP)--> Python MCP Server <--(WebSocket)--> Unity C# Bridge
                                      (SkillForUnity/src/)   (Assets/SkillForUnity/Editor/)
```

### Components

| Component | Location | Description |
|-----------|----------|-------------|
| **Unity C# Bridge** | `Assets/SkillForUnity/Editor/MCPBridge/` | WebSocket server running inside Unity Editor |
| **Python MCP Server** | `SkillForUnity/src/` | MCP protocol implementation |
| **Setup Scripts** | `SkillForUnity/setup/` | Installation and configuration helpers |
| **Examples** | `SkillForUnity/examples/` | Practical tutorials and guides |
| **Documentation** | `SkillForUnity/docs/` | API reference and best practices |

## ✨ Features

### High-Level Tools (Recommended)

- **Quick Scene Setup** - Instant 3D/2D/UI/VR scene configuration
- **GameObject Templates** - Create primitives, lights, player, enemy with one command
- **UI Templates** - Complete UI elements (Button, Panel, ScrollView, etc.)
- **Vector Sprite Generation** - Create sprites from primitive shapes, SVG files, or solid colors for prototyping
- **Hierarchy Builder** - Build complex nested structures declaratively
- **Design Patterns** - Generate Singleton, ObjectPool, StateMachine, Observer, Command, Factory, ServiceLocator
- **Layout Management** - Configure UI layouts (Vertical, Horizontal, Grid)

### Core Tools (Low-Level)

- **Scene Management** - Create, load, save, delete, inspect scenes
- **GameObject CRUD** - Full hierarchy manipulation with batch operations
- **Component CRUD** - Add, update, remove components with batch support
- **Asset Operations** - Rename, duplicate, delete, inspect, update importer settings
- **ScriptableObject Management** - Create, inspect, update, delete, duplicate, find ScriptableObject assets
- **Script Template Generation** - Generate MonoBehaviour and ScriptableObject templates with proper structure
- **Prefab Workflow** - Create, instantiate, update, apply/revert overrides

### Advanced Features

- **Project Settings** - Configure player, quality, time, physics, audio, editor settings
- **Render Pipeline** - Manage Built-in/URP/HDRP pipeline settings
- **Tags & Layers** - Manage project tags and layers
- **Constants** - Convert between Unity constants and numeric values
- **Automatic Compilation** - Detects and waits for Unity compilation

## 📝 Script Template Generation

SkillForUnity provides a **script template generation** system for quickly creating MonoBehaviour and ScriptableObject scripts with proper Unity structure.

### Key Features

- **MonoBehaviour Templates** - Includes standard lifecycle methods (Awake, Start, Update, OnDestroy)
- **ScriptableObject Templates** - Data container classes with CreateAssetMenu attribute
- **Namespace Support** - Optional C# namespace wrapping
- **Fast Development** - Quickly scaffold scripts with proper structure

### Example: Generate MonoBehaviour Script

```python
unity_script_template_generate({
    "templateType": "MonoBehaviour",
    "className": "PlayerController",
    "scriptPath": "Assets/Scripts/PlayerController.cs",
    "namespace": "MyGame.Player"
})
```

### Example: Generate ScriptableObject Script

```python
unity_script_template_generate({
    "templateType": "ScriptableObject",
    "className": "GameConfig",
    "scriptPath": "Assets/ScriptableObjects/GameConfig.cs"
})
```

After generating the template, use `unity_asset_crud` with `update` operation to modify the script content:

```python
unity_asset_crud({
    "operation": "update",
    "assetPath": "Assets/Scripts/PlayerController.cs",
    "content": "using UnityEngine;\n\nnamespace MyGame.Player\n{\n    public class PlayerController : MonoBehaviour\n    {\n        public float speed = 5f;\n        \n        void Update()\n        {\n            // Movement code\n        }\n    }\n}"
})
```

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

See [SkillForUnity/examples/](SkillForUnity/examples/) for more tutorials.

## 🎨 Example: Vector Sprite Generation for Prototyping

```python
# Generate a red circle sprite
unity_vectorSprite_convert({
    "operation": "primitiveToSprite",
    "primitiveType": "circle",
    "width": 256,
    "height": 256,
    "color": {"r": 1.0, "g": 0.0, "b": 0.0, "a": 1.0},
    "outputPath": "Assets/Sprites/RedCircle.png"
})

# Generate a blue triangle
unity_vectorSprite_convert({
    "operation": "primitiveToSprite",
    "primitiveType": "triangle",
    "width": 256,
    "height": 256,
    "color": "#0000FF",
    "outputPath": "Assets/Sprites/BlueTriangle.png"
})

# Create a solid color sprite for UI placeholder
unity_vectorSprite_convert({
    "operation": "createColorSprite",
    "width": 64,
    "height": 64,
    "color": {"r": 0.5, "g": 0.5, "b": 0.5, "a": 1.0},
    "outputPath": "Assets/UI/Placeholder.png"
})
```

## 📦 ScriptableObject Management Example

```python
# Step 1: Generate ScriptableObject script template
unity_script_template_generate({
    "templateType": "ScriptableObject",
    "className": "GameConfig",
    "scriptPath": "Assets/Scripts/Data/GameConfig.cs",
    "namespace": "MyGame.Data"
})

# Step 2: Update script content
unity_asset_crud({
    "operation": "update",
    "assetPath": "Assets/Scripts/Data/GameConfig.cs",
    "content": """using UnityEngine;

namespace MyGame.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "MyGame/Game Config")]
    public class GameConfig : ScriptableObject
    {
        public string gameName = "My Awesome Game";
        public int maxPlayers = 4;
        public float gameSpeed = 1.0f;
        public bool enableDebugMode = false;
    }
}
"""
})

# Step 3: Wait for compilation
unity_await_compilation({"timeoutSeconds": 60})

# Step 4: Create ScriptableObject asset
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

# Step 5: Inspect ScriptableObject
config_info = unity_scriptableobject_manage({
    "operation": "inspect",
    "assetPath": "Assets/Data/DefaultConfig.asset",
    "includeProperties": True
})

# Step 6: Update properties
unity_scriptableobject_manage({
    "operation": "update",
    "assetPath": "Assets/Data/DefaultConfig.asset",
    "properties": {
        "maxPlayers": 16,
        "gameSpeed": 2.0
    }
})

# Step 7: Duplicate ScriptableObject
unity_scriptableobject_manage({
    "operation": "duplicate",
    "sourceAssetPath": "Assets/Data/DefaultConfig.asset",
    "destinationAssetPath": "Assets/Data/HighSpeedConfig.asset"
})

# Step 8: Find all GameConfig ScriptableObjects
all_configs = unity_scriptableobject_manage({
    "operation": "findByType",
    "typeName": "MyGame.Data.GameConfig",
    "searchPath": "Assets/Data",
    "includeProperties": True
})
```

## 🛠️ Development

### Project Structure

```
SkillForUnity/
├── Assets/
│   └── SkillForUnity/
│       ├── SkillForUnity.zip        # Bundled Claude Skill MCP server package
│       └── Editor/
│           └── MCPBridge/           # Unity C# Bridge
│               ├── McpBridgeService.cs
│               ├── McpCommandProcessor.cs
│               └── McpContextCollector.cs
│
├── .claude/
│   └── skills/
│       └── SkillForUnity/           # Claude Skill (Python MCP server)
│           ├── src/                 # Server source
│           │   ├── bridge/          # Unity Bridge communication
│           │   ├── tools/           # MCP tool definitions
│           │   ├── resources/       # MCP resources
│           │   └── main.py          # Entry point
│           ├── setup/               # Installation scripts
│           ├── examples/            # Tutorials
│           ├── docs/                # Documentation
│           ├── config/              # Configuration templates
│           ├── skill.yml            # Skill manifest
│           └── pyproject.toml       # Python package config
│
├── ProjectSettings/                 # Unity project settings
├── Packages/                        # Unity packages
└── README.md                        # This file
```

### Install Dev Dependencies

```bash
cd SkillForUnity
uv sync --dev
```

### Run Tests

```bash
cd SkillForUnity
pytest
```

### Format Code

```bash
cd SkillForUnity
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

See [SkillForUnity/docs/guides/best-practices.md](SkillForUnity/docs/guides/best-practices.md) for coding guidelines.

## 📄 License

MIT License - see [MIT License](https://opensource.org/licenses/MIT) for details.

## 🙏 Acknowledgments

- **Model Context Protocol** by Anthropic
- **Unity Technologies** for the amazing game engine
- All contributors and community members

## 🆘 Support

- **Quick Start**: [SkillForUnity/QUICKSTART.md](SkillForUnity/QUICKSTART.md)
- **Examples**: [SkillForUnity/examples/](SkillForUnity/examples/)
- **Troubleshooting**: [SkillForUnity/docs/troubleshooting.md](SkillForUnity/docs/troubleshooting.md)
- **Issues**: [GitHub Issues](https://github.com/yourusername/SkillForUnity/issues)

## 🔄 Migration from Old Structure

If you were using the old structure (`Assets/Runtime/MCPServer/` or `SkillPackage/`):

1. **Unity Side**: Install via Unity Package Manager (see installation instructions above)
   - The Unity Bridge remains at `Assets/SkillForUnity/Editor/MCPBridge/` (unchanged)
2. **Claude Skill Side**: Extract `Assets/SkillForUnity/SkillForUnity.zip` into your Claude Desktop skills folder (creates `~/.claude/skills/SkillForUnity`)
   - Or configure via MCP Window by pointing to the extracted `skill.yml`
   - Or manually add to `claude_desktop_config.json`
3. Remove old installation files if desired

---

**Made with ❤️ for the Unity and AI community**

**Start building amazing Unity projects with AI assistance today!** 🚀
