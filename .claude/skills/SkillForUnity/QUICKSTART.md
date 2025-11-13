# Unity MCP Skill - Quick Start Guide

**Get started with Unity MCP Skill in 5 minutes!**

## Prerequisites

- ✅ Unity Editor 2021.3 or higher
- ✅ Python 3.10 or higher
- ✅ uv package manager (will be installed automatically)

## Step 1: Install the Skill (2 minutes)

### Windows
```powershell
cd SkillPackage
.\setup\install.ps1
```

### Linux/macOS
```bash
cd SkillPackage
chmod +x setup/install.sh
./setup/install.sh
```

This will:
- Install Python dependencies
- Check Unity installation
- Generate MCP configuration files

## Step 2: Unity Setup (1 minute)

1. **Open Unity Editor** with your project
2. **Import MCPBridge** (if not already imported):
   - The bridge is located at `../Assets/Editor/MCPBridge/`
   - It should be automatically available if you opened the Unity project
3. **Start the Bridge**:
   - Go to **Tools > MCP Assistant**
   - Click **Start Bridge**
   - Status should show "Connected"

💡 The bridge listens on `ws://localhost:7077/bridge` by default.

## Step 3: Configure Your MCP Client (2 minutes)

### Option A: Claude Desktop

1. Open Claude Desktop configuration:
   - Windows: `%APPDATA%\Claude\claude_desktop_config.json`
   - macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - Linux: `~/.config/Claude/claude_desktop_config.json`

2. Add the Unity MCP server:
```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "uv",
      "args": [
        "run",
        "--directory",
        "C:/Projects/MCP/SkillPackage",
        "src/main.py"
      ],
      "env": {
        "MCP_SERVER_TRANSPORT": "stdio",
        "MCP_LOG_LEVEL": "info"
      }
    }
  }
}
```

3. Replace `C:/Projects/MCP/SkillPackage` with your actual path

4. Restart Claude Desktop

### Option B: Cursor IDE

1. Open Cursor settings
2. Add MCP server configuration (similar to Claude Desktop)
3. Use the generated `config/cursor.json` as a template

### Option C: Custom Client

Use the generated `config/mcp-config.json` as a starting point.

## Step 4: Test the Connection

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
unity_context_inspect({
    "includeHierarchy": True,
    "maxDepth": 2
})
```

## Next Steps

### Learn More

- 📖 **[README.md](README.md)** - Full documentation
- 🎮 **[examples/](examples/)** - Practical tutorials
- 📚 **[docs/](docs/)** - Detailed API reference

### Try These Examples

1. **[Basic Scene Setup](examples/01-basic-scene-setup.md)** - Create your first game scene
2. **[UI Creation](examples/02-ui-creation.md)** - Build a complete menu system
3. **[Game Level](examples/03-game-level.md)** - Design a game level

### Best Practices

✅ **DO:**
- Use templates when available (`createFromTemplate`)
- Check context before making changes (`context_inspect`)
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
1. Use `unity_context_inspect()` to see what exists
2. Check GameObject path is correct (case-sensitive)
3. Verify GameObject is in the active scene

## Getting Help

- 🐛 **Report Issues**: [GitHub Issues](https://github.com/yourusername/unity-mcp/issues)
- 📖 **Documentation**: [docs/troubleshooting.md](docs/troubleshooting.md)
- 💬 **Community**: Check discussions and examples

---

**Ready to build amazing Unity projects with AI assistance!** 🚀
