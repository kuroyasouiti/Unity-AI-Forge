# SkillForUnity - Unity Editor Integration via Model Context Protocol

**Enable AI assistants to control Unity Editor in real-time through the Model Context Protocol.**

[![Python](https://img.shields.io/badge/Python-3.10%2B-blue)](https://www.python.org/)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black)](https://unity.com/)
[![MCP](https://img.shields.io/badge/MCP-0.9.0%2B-green)](https://modelcontextprotocol.io/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 📦 What's New: Skill Package Structure

SkillForUnity has been restructured as a **Claude Agent Skill** for easier setup and distribution!

```
SkillForUnity/
├── Assets/SkillForUnity/Editor/MCPBridge/    # Unity C# WebSocket Bridge + bundled Claude Skill zip
└── .claude/skills/SkillForUnity/             # ⭐ Claude Skill source (Python MCP server, docs, tools)
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

The Unity package already bundles the Claude Skill archive at `Assets/SkillForUnity/Editor/MCPBridge/SkillForUnity.zip`.

**Option A: Copy the bundled zip to Claude Desktop's skills folder**

```bash
# Copy the Claude Skill zip
cp Assets/SkillForUnity/Editor/MCPBridge/SkillForUnity.zip ~/.claude/skills/

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
      "args": ["run", "--directory", "/path/to/.claude/skills/SkillForUnity", "src/main.py"],
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

- **[Claude Skill QUICKSTART](.claude/skills/SkillForUnity/QUICKSTART.md)** - Get started in 5 minutes
- **[Claude Skill README](.claude/skills/SkillForUnity/README.md)** - Complete skill documentation
- **[Claude Skill examples](.claude/skills/SkillForUnity/examples/)** - Practical tutorials and walkthroughs

### For Developers

- **[Claude Skill docs](.claude/skills/SkillForUnity/docs/)** - API reference and guides
- **[CLAUDE.md](CLAUDE.md)** - Instructions for Claude Code integration
- **[Best Practices guide](.claude/skills/SkillForUnity/docs/guides/best-practices.md)** - Repository guidelines and tips

## 🏗️ Architecture

```
AI Client (Claude/Cursor) <--(MCP)--> Python MCP Server <--(WebSocket)--> Unity C# Bridge
                                      (.claude/skills/SkillForUnity/src/)   (Assets/SkillForUnity/Editor/)
```

### Components

| Component | Location | Description |
|-----------|----------|-------------|
| **Unity C# Bridge** | `Assets/SkillForUnity/Editor/MCPBridge/` | WebSocket server running inside Unity Editor |
| **Python MCP Server** | `.claude/skills/SkillForUnity/src/` | MCP protocol implementation |
| **Setup Scripts** | `.claude/skills/SkillForUnity/setup/` | Installation and configuration helpers |
| **Examples** | `.claude/skills/SkillForUnity/examples/` | Practical tutorials and guides |
| **Documentation** | `.claude/skills/SkillForUnity/docs/` | API reference and best practices |

## ✨ Features

### Core Capabilities

- **20+ Unity Tools** - Complete control over Unity Editor
- **Scene Management** - Create, load, save, delete scenes
- **GameObject Operations** - Full hierarchy manipulation
- **Component Editing** - Add, update, remove components
- **UI Creation** - Templates for buttons, panels, text, etc.
- **Asset Management** - Asset file operations
- **Script Management** - Batch C# script creation and compilation
- **Prefab Workflow** - Create and manage prefabs
- **Batch Operations** - Execute multiple commands efficiently

### Advanced Features

- **Project Settings** - Configure Unity settings
- **Render Pipeline** - Manage render pipeline settings
- **Tags & Layers** - Manage tags and layers
- **Automatic Compilation** - Detects and waits for Unity compilation

## 📝 Script Management

SkillForUnity provides a powerful **batch script management** system for creating and managing C# scripts efficiently.

### Key Features

- **Batch Operations** - Create, update, or delete multiple scripts in one atomic operation
- **Automatic Compilation** - Single consolidated compilation after all operations
- **10-20x Faster** - Compared to individual script operations
- **Error Handling** - Per-script error reporting with `stopOnError` control
- **Namespace Support** - Automatic namespace generation from folder structure

### Example: Create Multiple Scripts

```python
unity_script_batch_manage({
    "scripts": [
        {
            "operation": "create",
            "scriptPath": "Assets/Scripts/Player.cs",
            "content": "using UnityEngine;\n\npublic class Player : MonoBehaviour\n{\n    void Start()\n    {\n        Debug.Log(\"Player initialized\");\n    }\n}"
        },
        {
            "operation": "create",
            "scriptPath": "Assets/Scripts/Enemy.cs",
            "content": "using UnityEngine;\n\npublic class Enemy : MonoBehaviour\n{\n    public float health = 100f;\n}"
        },
        {
            "operation": "create",
            "scriptPath": "Assets/Scripts/GameManager.cs",
            "content": "using UnityEngine;\n\npublic class GameManager : MonoBehaviour\n{\n    public static GameManager Instance { get; private set; }\n}"
        }
    ],
    "stopOnError": False,
    "timeoutSeconds": 30
})
```

**Important**: Always use `unity_script_batch_manage()` for script operations - even for single scripts. This ensures proper compilation handling.

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

See [.claude/skills/SkillForUnity/examples/](.claude/skills/SkillForUnity/examples/) for more tutorials.

## 🛠️ Development

### Project Structure

```
SkillForUnity/
├── Assets/
│   └── SkillForUnity/
│       └── Editor/
│           └── MCPBridge/           # Unity C# Bridge + bundled Claude Skill zip
│               ├── McpBridgeService.cs
│               ├── McpCommandProcessor.cs
│               ├── McpContextCollector.cs
│               └── SkillForUnity.zip
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
cd .claude/skills/SkillForUnity
uv sync --dev
```

### Run Tests

```bash
cd .claude/skills/SkillForUnity
pytest
```

### Format Code

```bash
cd .claude/skills/SkillForUnity
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

See [.claude/skills/SkillForUnity/docs/guides/best-practices.md](.claude/skills/SkillForUnity/docs/guides/best-practices.md) for coding guidelines.

## 📄 License

MIT License - see [MIT License](https://opensource.org/licenses/MIT) for details.

## 🙏 Acknowledgments

- **Model Context Protocol** by Anthropic
- **Unity Technologies** for the amazing game engine
- All contributors and community members

## 🆘 Support

- **Quick Start**: [.claude/skills/SkillForUnity/QUICKSTART.md](.claude/skills/SkillForUnity/QUICKSTART.md)
- **Examples**: [.claude/skills/SkillForUnity/examples/](.claude/skills/SkillForUnity/examples/)
- **Troubleshooting**: [.claude/skills/SkillForUnity/docs/troubleshooting.md](.claude/skills/SkillForUnity/docs/troubleshooting.md)
- **Issues**: [GitHub Issues](https://github.com/yourusername/SkillForUnity/issues)

## 🔄 Migration from Old Structure

If you were using the old structure (`Assets/Runtime/MCPServer/` or `SkillPackage/`):

1. **Unity Side**: Install via Unity Package Manager (see installation instructions above)
   - The Unity Bridge remains at `Assets/SkillForUnity/Editor/MCPBridge/` (unchanged)
2. **Claude Skill Side**: Extract `Assets/SkillForUnity/Editor/MCPBridge/SkillForUnity.zip` into your Claude Desktop skills folder (creates `~/.claude/skills/SkillForUnity`)
   - Or configure via MCP Window by pointing to the extracted `skill.yml`
   - Or manually add to `claude_desktop_config.json`
3. Remove old installation files if desired

---

**Made with ❤️ for the Unity and AI community**

**Start building amazing Unity projects with AI assistance today!** 🚀
