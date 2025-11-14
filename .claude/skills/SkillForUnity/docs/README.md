# SkillForUnity Documentation

**Complete documentation for the Unity Editor MCP integration skill**

---

## 📚 Documentation Index

### Quick Start
- **[QUICKSTART.md](../QUICKSTART.md)** - Get started in 5 minutes
- **[README.md](../README.md)** - Project overview and setup

### Tool Documentation
- **[SKILL.md](../SKILL.md)** - Main skill documentation for AI assistants
- **[TOOLS_REFERENCE.md](TOOLS_REFERENCE.md)** - Complete reference for all 28 tools
- **[TOOL_SELECTION_GUIDE.md](TOOL_SELECTION_GUIDE.md)** - How to choose the right tool

### Advanced Guides
- **[CLAUDE.md](../../../../CLAUDE.md)** - Detailed guidance for Claude Code integration
- **Examples** - See [examples/](../examples/) directory for practical tutorials

---

## 🎯 What Should I Read?

### I want to...

#### ...get started quickly
→ Read **[QUICKSTART.md](../QUICKSTART.md)** (5 minutes)

#### ...understand all available tools
→ Read **[TOOLS_REFERENCE.md](TOOLS_REFERENCE.md)** (28 tools documented)

#### ...know which tool to use for my task
→ Read **[TOOL_SELECTION_GUIDE.md](TOOL_SELECTION_GUIDE.md)** (decision trees + workflows)

#### ...integrate with Claude Code
→ Read **[CLAUDE.md](../../../../CLAUDE.md)** (comprehensive guide)

#### ...see practical examples
→ Check **[examples/](../examples/)** directory

---

## 📖 Documentation Structure

```
SkillForUnity/
├── QUICKSTART.md              # 5-minute quick start
├── README.md                  # Project overview
├── SKILL.md                   # Main AI assistant guide
├── docs/
│   ├── README.md              # This file
│   ├── TOOLS_REFERENCE.md     # Complete tool reference (28 tools)
│   ├── TOOL_SELECTION_GUIDE.md # Tool selection guide
│   ├── api-reference/         # API documentation
│   ├── guides/                # Tutorial guides
│   └── troubleshooting.md     # Common issues
├── examples/
│   ├── 01-basic-scene-setup.md
│   ├── 02-ui-creation.md
│   ├── 03-game-level.md
│   └── 04-prefab-workflow.md
└── config/
    ├── mcp-config.json.template
    ├── claude-desktop.json.example
    └── cursor.json.example
```

---

## 🎮 Tool Categories Overview

SkillForUnity provides **28 MCP tools** organized into **9 categories**:

| Category | Tools | Documentation |
|----------|-------|---------------|
| **Core Tools** | 5 | [TOOLS_REFERENCE.md#core-tools](TOOLS_REFERENCE.md#core-tools) |
| **Scene Management** | 2 | [TOOLS_REFERENCE.md#scene-management](TOOLS_REFERENCE.md#scene-management) |
| **GameObject Operations** | 3 | [TOOLS_REFERENCE.md#gameobject-operations](TOOLS_REFERENCE.md#gameobject-operations) |
| **Component Management** | 1 | [TOOLS_REFERENCE.md#component-management](TOOLS_REFERENCE.md#component-management) |
| **Asset Management** | 2 | [TOOLS_REFERENCE.md#asset-management](TOOLS_REFERENCE.md#asset-management) |
| **UI (UGUI) Tools** | 6 | [TOOLS_REFERENCE.md#ui-ugui-tools](TOOLS_REFERENCE.md#ui-ugui-tools) |
| **Prefab Management** | 1 | [TOOLS_REFERENCE.md#prefab-management](TOOLS_REFERENCE.md#prefab-management) |
| **Advanced Features** | 7 | [TOOLS_REFERENCE.md#advanced-features](TOOLS_REFERENCE.md#advanced-features) |
| **Utility Tools** | 1 | [TOOLS_REFERENCE.md#utility-tools](TOOLS_REFERENCE.md#utility-tools) |

**Total: 28 Tools**

---

## 🚀 Common Tasks

### Task 1: Create a 3D Game Scene
**Time:** ~3 minutes
**Tools Used:** 3 tools, 5 operations
**Guide:** [TOOL_SELECTION_GUIDE.md - Workflow 2](TOOL_SELECTION_GUIDE.md#workflow-2-create-a-game-level)

1. Set up 3D scene with `unity_scene_quickSetup`
2. Create player hierarchy with `unity_hierarchy_builder`
3. Create ground with `unity_gameobject_createFromTemplate`
4. Create obstacles with `unity_batch_execute`
5. Add physics with `unity_component_crud`

---

### Task 2: Create a Main Menu UI
**Time:** ~2 minutes
**Tools Used:** 4 tools, 5 operations
**Guide:** [TOOL_SELECTION_GUIDE.md - Workflow 1](TOOL_SELECTION_GUIDE.md#workflow-1-create-a-main-menu)

1. Set up UI scene with `unity_scene_quickSetup`
2. Create menu structure with `unity_hierarchy_builder`
3. Add layout with `unity_ugui_layoutManage`
4. Create buttons with `unity_batch_execute`

---

### Task 3: Manage C# Scripts
**Time:** ~30 seconds (+ compilation time)
**Tools Used:** 2-3 tools
**Guide:** [TOOL_SELECTION_GUIDE.md - Workflow 3](TOOL_SELECTION_GUIDE.md#workflow-3-create-and-configure-scripts)

1. Create/update scripts with `unity_script_batch_manage` (ALWAYS!)
2. Wait for compilation with `unity_await_compilation` (if needed)
3. Add scripts to GameObjects with `unity_component_crud`

---

## 🔍 Finding Information

### Quick Reference Tables

| Topic | Location |
|-------|----------|
| **All tools list** | [TOOLS_REFERENCE.md](TOOLS_REFERENCE.md) |
| **Tool decision tree** | [TOOL_SELECTION_GUIDE.md#quick-decision-tree](TOOL_SELECTION_GUIDE.md#quick-decision-tree) |
| **Performance tips** | [TOOL_SELECTION_GUIDE.md#performance-optimization-guide](TOOL_SELECTION_GUIDE.md#performance-optimization-guide) |
| **Common workflows** | [TOOL_SELECTION_GUIDE.md#common-workflows](TOOL_SELECTION_GUIDE.md#common-workflows) |
| **Common mistakes** | [TOOL_SELECTION_GUIDE.md#common-mistakes-to-avoid](TOOL_SELECTION_GUIDE.md#common-mistakes-to-avoid) |
| **Tool comparison** | [TOOL_SELECTION_GUIDE.md#tool-comparison](TOOL_SELECTION_GUIDE.md#tool-comparison) |

---

## 💡 Best Practices Summary

### DO ✅

1. **Use templates** - 10x faster than manual creation
   - `unity_ugui_createFromTemplate` for UI
   - `unity_gameobject_createFromTemplate` for GameObjects
   - `unity_scene_quickSetup` for scenes

2. **Check context first** - Understand current state
   - `unity_context_inspect` before making changes

3. **Use hierarchy builder** - Create entire structures
   - `unity_hierarchy_builder` for complex hierarchies

4. **Batch script operations** - Always use script batch manager
   - `unity_script_batch_manage` for ALL C# scripts

5. **Optimize inspections** - Use fast modes
   - `includeProperties=false` for existence checks
   - `propertyFilter` for specific properties

6. **Limit batch operations** - Prevent timeouts
   - `maxResults` parameter (default: 1000)

### DON'T ❌

1. **Don't use asset_crud for C# scripts** - Use script batch manager!
2. **Don't create UI manually** - Use templates
3. **Don't skip context inspection** - Know what exists
4. **Don't use unlimited batch operations** - Set maxResults
5. **Don't read all properties** - Use includeProperties=false or propertyFilter

---

## 🎓 Learning Path

### Beginner (Day 1)
1. Read [QUICKSTART.md](../QUICKSTART.md) - 5 minutes
2. Try `unity_ping` to test connection
3. Use `unity_context_inspect` to explore scene
4. Create UI with `unity_ugui_createFromTemplate`
5. Create GameObjects with `unity_gameobject_createFromTemplate`

### Intermediate (Week 1)
1. Read [TOOL_SELECTION_GUIDE.md](TOOL_SELECTION_GUIDE.md)
2. Use `unity_hierarchy_builder` for complex structures
3. Use `unity_batch_execute` for multi-step operations
4. Manage components with `unity_component_crud`
5. Work with prefabs using `unity_prefab_crud`

### Advanced (Month 1)
1. Read [TOOLS_REFERENCE.md](TOOLS_REFERENCE.md) completely
2. Use advanced features (NavMesh, Input System, Tilemap)
3. Optimize performance with fast inspection modes
4. Create custom workflows with batch operations
5. Contribute examples and documentation

---

## 📊 Tool Usage Statistics

### Most Frequently Used (Top 10)

1. `unity_context_inspect` - Understand scene state
2. `unity_scene_quickSetup` - Quick scene initialization
3. `unity_gameobject_createFromTemplate` - Create common objects
4. `unity_ugui_createFromTemplate` - Create UI elements
5. `unity_component_crud` - Manage components
6. `unity_hierarchy_builder` - Build complex structures
7. `unity_script_batch_manage` - Manage C# scripts
8. `unity_batch_execute` - Execute multiple operations
9. `unity_prefab_crud` - Prefab workflow
10. `unity_ugui_layoutManage` - Organize UI layouts

### Most Powerful Tools

1. **`unity_hierarchy_builder`** - Create entire GameObject trees in one command
2. **`unity_batch_execute`** - Execute any combination of tools in sequence
3. **`unity_script_batch_manage`** - Manage multiple scripts with automatic compilation
4. **`unity_component_crud`** - Comprehensive component management with batch operations
5. **`unity_context_inspect`** - Complete scene understanding

---

## 🔧 Troubleshooting

### Common Issues

| Issue | Solution | Documentation |
|-------|----------|---------------|
| Unity bridge not connected | Start bridge in Unity Editor (Tools > MCP Assistant) | [QUICKSTART.md](../QUICKSTART.md#step-2-unity-setup-1-minute) |
| GameObject not found | Use `unity_context_inspect` to see hierarchy | [TOOLS_REFERENCE.md#2-unity_context_inspect](TOOLS_REFERENCE.md#2-unity_context_inspect) |
| Component type not found | Use fully qualified names (e.g., UnityEngine.UI.Button) | [SKILL.md#component-type-reference](../SKILL.md#component-type-reference) |
| Operation timeout | Use `maxResults` limit, `includeProperties=false` | [TOOL_SELECTION_GUIDE.md#performance-optimization-guide](TOOL_SELECTION_GUIDE.md#performance-optimization-guide) |
| Script compilation errors | Use `unity_console_log` to see errors | [TOOLS_REFERENCE.md#5-unity_console_log](TOOLS_REFERENCE.md#5-unity_console_log) |

For more troubleshooting, see [troubleshooting.md](troubleshooting.md)

---

## 🤝 Contributing

We welcome contributions to the documentation!

### How to Contribute

1. **Report Issues** - Found incorrect information? Open an issue!
2. **Suggest Improvements** - Have ideas for better documentation? Let us know!
3. **Add Examples** - Created a useful workflow? Share it!
4. **Fix Typos** - Even small fixes are appreciated!

---

## 📝 Documentation Maintenance

### Last Updated
- **Date:** 2025-01-14
- **Version:** 1.0.0
- **Total Tools:** 28

### Recent Changes
- Initial comprehensive documentation
- Added TOOLS_REFERENCE.md (complete tool reference)
- Added TOOL_SELECTION_GUIDE.md (decision trees and workflows)
- Updated SKILL.md with tool categories

---

## 📞 Support

- **GitHub Issues:** [Report bugs and request features](https://github.com/yourusername/SkillForUnity/issues)
- **Documentation:** You're here! 📚
- **Examples:** Check [examples/](../examples/) directory
- **Community:** Join discussions and share your workflows

---

**Happy Unity Development with AI! 🚀**
