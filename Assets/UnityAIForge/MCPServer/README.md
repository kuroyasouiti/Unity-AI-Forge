# Unity-AI-Forge MCP Server

Model Context Protocol (MCP) server for Unity-AI-Forge - AI-powered Unity development toolkit.

## About

This MCP server enables AI assistants to interact with Unity Editor through the Model Context Protocol, providing tools for scene manipulation, GameKit framework integration, and game development automation.

## Installation

This package is installed automatically via Unity Editor's MCP Server Manager.

For manual installation and detailed documentation, see:
- [Installation Guide](../Documentation/Installation/INSTALL_GUIDE.md)
- [Quick Start Guide](../Documentation/Installation/QUICKSTART.md)
- [Full Documentation](../Documentation/README.md)

## Features

### Core Unity Tools
- Scene hierarchy manipulation and build settings
- GameObject creation with templates and batch operations
- Component management with property updates
- Asset management and ScriptableObject CRUD
- Prefab workflow automation
- Material and shader control
- Project settings configuration

### High-Level Tools (14)
Analysis, GameKit UI, and Systems with zero runtime dependency:
- **Logic** (7): Integrity validation, ClassCatalog, ClassDependencyGraph, SceneReferenceGraph, SceneRelationshipGraph, SceneDependency, ScriptSyntax
- **GameKit UI** (5): UICommand, UIBinding, UIList, UISlot, UISelection
- **GameKit Systems** (2): ObjectPool, Data (EventChannel, DataContainer, RuntimeSet)

### Declarative UI System
- **UIHierarchy**: Create complex UI structures from JSON definitions
- **UIState**: Define, save, and transition between UI states
- **UINavigation**: Configure keyboard/gamepad navigation for UI elements

### Advanced Features
- Batch operations with pattern matching
- Physics presets and camera rigs
- Input system integration
- UI Toolkit (UXML/USS) support
- Compilation await for C# script operations

## Requirements

- Python 3.10 or higher
- Unity 2022.3 or higher
- MCP SDK 0.9.0 or higher

## Documentation

For complete documentation, visit the [Documentation](../Documentation) folder.

## License

MIT License - See [LICENSE](../../LICENSE) file for details.

## Repository

https://github.com/kuroyasouiti/Unity-AI-Forge

