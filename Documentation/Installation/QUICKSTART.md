# Unity-AI-Forge - Quick Start Guide

**Get started with Unity-AI-Forge in 5 minutes!**

## Prerequisites

- ✅ Unity Editor 2022.3 or higher (2021.3+ supported)
- ✅ Python 3.10 or higher
- ✅ uv package manager (recommended)

## Step 1: Install Unity Package (1 minute)

### Option A: Via Unity Package Manager (Recommended)

1. Open Unity Editor
2. Open **Window > Package Manager**
3. Click **+ (Plus)** button → **Add package from git URL...**
4. Enter: `https://github.com/kuroyasouiti/Unity-AI-Forge.git?path=/Assets/UnityAIForge`
5. Click **Add**

### Option B: Manual Installation

1. Download the repository
2. Copy `Assets/UnityAIForge` to your Unity project's `Assets/` folder

## Step 2: Install MCP Server (2 minutes)

### Option A: Automatic (Recommended)

1. In Unity Editor, go to **Tools > Unity-AI-Forge > MCP Server Manager**
2. Click **Install Server** (installs to `~/Unity-AI-Forge`)
3. Click **Register** for your AI tool (Cursor, Claude Desktop, Cline, Windsurf)
4. Restart your AI tool

### Option B: Manual Setup

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

## Step 3: Start Unity Bridge (30 seconds)

1. In Unity Editor, go to **Tools > Unity-AI-Forge > MCP Assistant**
2. Click **Start Bridge**
3. Status should show "Connected"

💡 The bridge listens on `ws://localhost:7077/bridge` by default.

## Step 4: Configure Your MCP Client (if manual setup)

**Note:** If you used the automatic installation in Step 2, this is already done for you!

### For Claude Desktop

Configuration location:
- Windows: `%APPDATA%\Claude\claude_desktop_config.json`
- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
- Linux: `~/.config/Claude/claude_desktop_config.json`

Add this configuration:
```json
{
  "mcpServers": {
    "unity-ai-forge": {
      "command": "uv",
      "args": [
        "--directory",
        "C:/Users/YOUR_USERNAME/Unity-AI-Forge",
        "run",
        "unity-ai-forge"
      ]
    }
  }
}
```

Replace `C:/Users/YOUR_USERNAME` with your actual home directory path.

For macOS/Linux, use:
```json
{
  "mcpServers": {
    "unity-ai-forge": {
      "command": "uv",
      "args": [
        "--directory",
        "/Users/YOUR_USERNAME/Unity-AI-Forge",
        "run",
        "unity-ai-forge"
      ]
    }
  }
}
```

### For Cursor

Configuration is typically at: `%APPDATA%\Cursor\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json`

Use similar configuration as Claude Desktop.

### For Other Tools

See [INSTALL_GUIDE.md](INSTALL_GUIDE.md) for Cline and Windsurf configuration.

## Step 5: Test the Connection

Open your MCP client (Claude Desktop, Cursor, etc.) and try:

```
Can you test the Unity MCP connection?
```

The AI should respond by calling `unity_ping()` and showing Unity version information.

## Your First Commands

### Create a 3D Scene

```
Create a 3D game scene with a player and ground.
```

This will:
- Set up a 3D scene with camera and lighting
- Create a ground plane
- Add a player capsule

### Create a UI Menu

```
Create a main menu UI with Play, Settings, and Quit buttons.
```

This will:
- Set up a Canvas and EventSystem
- Create a menu panel
- Add three styled buttons

### Inspect the Scene

```
What GameObjects are in the current scene?
```

This will show the scene hierarchy and all GameObjects.

## Common Commands Reference

| Task | Example Command |
|------|-----------------|
| Create scene | "Set up a 3D scene" |
| Create GameObject | "Create a cube at position (0, 1, 0)" |
| Add component | "Add a Rigidbody to the Player" |
| Create UI | "Create a button with text 'Start Game'" |
| Create ScriptableObject | "Create a GameConfig ScriptableObject with maxPlayers=4" |
| List GameObjects | "Show me all GameObjects in the scene" |
| Batch operations | "Create 10 cubes in a line" |

## Tool Reference

### Most Used Tools

**Scene Management:**
```python
unity_scene_quickSetup({"setupType": "3D"})  # or "2D", "UI"
unity_scene_crud({"operation": "create", "scenePath": "Assets/Scenes/Level1.unity"})
```

**GameObject Creation:**
```python
unity_gameobject_createFromTemplate({
    "template": "Cube",  # or Sphere, Player, Enemy, etc.
    "name": "MyObject",
    "position": {"x": 0, "y": 1, "z": 0}
})
```

**UI Creation:**
```python
unity_ugui_createFromTemplate({
    "template": "Button",  # or Text, Panel, Image, etc.
    "text": "Click Me!",
    "width": 200,
    "height": 50
})
```

**Component Management:**
```python
unity_component_crud({
    "operation": "add",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Rigidbody"
})
```

**Scene Inspection:**
```python
unity_scene_crud({
    "operation": "inspect",
    "includeHierarchy": True,
    "includeComponents": True,
    "filter": "Player*"
})
```

**ScriptableObject Management:**
```python
# Create a ScriptableObject
unity_scriptableObject_crud({
    "operation": "create",
    "typeName": "MyGame.GameConfig",
    "assetPath": "Assets/Data/Config.asset",
    "properties": {
        "maxPlayers": 4,
        "gameSpeed": 1.0
    }
})

# Inspect existing ScriptableObject
unity_scriptableObject_crud({
    "operation": "inspect",
    "assetPath": "Assets/Data/Config.asset",
    "includeProperties": True
})

# Update properties
unity_scriptableObject_crud({
    "operation": "update",
    "assetPath": "Assets/Data/Config.asset",
    "properties": {
        "maxPlayers": 8
    }
})

# Find all ScriptableObjects of a type
unity_scriptableObject_crud({
    "operation": "findByType",
    "typeName": "MyGame.GameConfig",
    "includeProperties": True
})
```

## Next Steps

### Learn More

- 📖 **[README.md](README.md)** - Full MCP server documentation
- 📋 **[INSTALL_GUIDE.md](INSTALL_GUIDE.md)** - Detailed installation instructions
- 🎮 **[examples/](examples/)** - Practical tutorials
- 📚 **[Project README](../../README.md)** - Complete project documentation

### Try These Examples

1. **[Basic Scene Setup](examples/01-basic-scene-setup.md)** - Create your first game scene
2. **[UI Creation](examples/02-ui-creation.md)** - Build a complete menu system
3. **[Game Level](examples/03-game-level.md)** - Design a game level
4. **[Prefab Workflow](examples/04-prefab-workflow.md)** - Work with prefabs
5. **[Design Patterns](examples/05-design-patterns.md)** - Generate design pattern code

### Best Practices

✅ **DO:**
- Use templates when available (`createFromTemplate`)
- Check scene context before making changes (`unity_scene_crud` with `operation="inspect"`)
- Use batch operations for multiple similar tasks
- Specify full component type names (e.g., `UnityEngine.Rigidbody`)

❌ **DON'T:**
- Create GameObjects manually when templates exist
- Make many individual calls instead of batch operations
- Forget to start the Unity Bridge before using tools

## Troubleshooting

### Unity Bridge Not Connected

**Problem:** Tools fail with "Unity bridge is not connected"

**Solution:**
1. Open Unity Editor
2. Go to Tools > MCP Assistant
3. Click "Start Bridge"
4. Wait for "Connected" status

### Commands Time Out

**Problem:** Commands take too long and timeout

**Solution:**
- Increase timeout in MCP configuration
- Check Unity isn't compiling scripts
- Use lighter inspection operations (`includeProperties: false`)

### GameObject Not Found

**Problem:** "GameObject not found" error

**Solution:**
1. Use `unity_scene_crud({"operation": "inspect"})` to see what exists
2. Check GameObject path is correct (case-sensitive)
3. Verify GameObject is in the active scene

## Getting Help

- 🐛 **Report Issues**: [GitHub Issues](https://github.com/kuroyasouiti/Unity-AI-Forge/issues)
- 📖 **Documentation**: [README.md](README.md) and [INSTALL_GUIDE.md](INSTALL_GUIDE.md)
- 💬 **Examples**: Check [examples/](examples/) for practical guides

---

**Ready to build amazing Unity projects with AI assistance!** 🚀
