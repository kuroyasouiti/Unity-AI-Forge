# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.0] - 2025-01-21

### Added
- **GameObject Update Operation**: New `update` operation for `unity_gameobject_crud`
  - Update GameObject tags: `{"operation": "update", "gameObjectPath": "Player", "tag": "Player"}`
  - Update GameObject layers by name or index: `{"layer": "UI"}` or `{"layer": 5}`
  - Toggle active state: `{"active": true}` or `{"active": false}`
  - Set static flag: `{"static": true}` or `{"static": false}`
  - Update multiple properties at once for efficient batch updates

### Fixed
- **JSON Number Type Handling**: Enhanced layer index handling to support JSON numeric types
  - Supports `int`, `long`, and `double` types from JSON deserialization
  - Resolves issues with MiniJson.cs returning numeric values as `double`
  - Ensures layer-by-index updates work correctly across all MCP clients

### Documentation
- Updated CLAUDE.md with comprehensive `update` operation examples
- Added usage examples for tag, layer, active, and static property updates

---

## [1.3.0] - 2025-01-21

### Changed
- **Skill Installation Method**: Changed from zip file copy to directory extraction
  - Unity Editor's MCP Assistant now extracts skill package to directories
  - Installation path changed from `.claude/skills/SkillForUnity.zip` to `.claude/skills/SkillForUnity/`
  - Supports source code installation for better development and debugging
  - Automatic cleanup of temporary extraction directories

### Improved
- **ServerInstallerUtility**: Enhanced installation process
  - Added support for zip file extraction using System.IO.Compression
  - Automatic detection of internal directory structure
  - Safe installation with temporary directory handling
  - Better error handling during installation

### Documentation
- Updated manual installation commands to use extraction instead of copy
  - Windows: `Expand-Archive` PowerShell command
  - macOS/Linux: `unzip` command

---

## [1.2.0] - 2025-01-20

### Added
- **Menu Creation System**: New `unity_menu_hierarchyCreate` tool for creating complete hierarchical menu systems
  - Automatically generates panels, buttons, and layout groups
  - Creates State pattern navigation script with keyboard/gamepad support
  - Supports nested submenus and customizable button dimensions
  - Perfect for main menus, pause menus, and settings menus

### Changed
- **Console Log Integration**: `unity_await_compilation` now returns console logs in results
  - Includes all, errors, warnings, and normal logs
  - Eliminates need for separate console log queries
  - Improved debugging workflow

- **Tool Count Optimization**: Reduced from 28 to 26 tools by consolidating functionality
  - More focused and easier to learn toolset
  - Better organized tool categories

### Removed
- **Deprecated Tools**:
  - `unity_hierarchy_builder`: Replaced by specialized tools
    - Use `unity_menu_hierarchyCreate` for menu systems
    - Use `unity_template_manage` for GameObject customization
  - `unity_console_log`: Functionality integrated into `unity_await_compilation`

### Documentation
- **Complete Documentation Update**: Updated all 15 documentation files
  - Root: README.md, README_ja.md, CLAUDE.md
  - Skill: README.md, QUICKSTART.md, SKILL.md
  - Examples: 03-game-level.md, 04-prefab-workflow.md
  - Docs: 7 files (README, troubleshooting, guides, API references)
  - Consistent examples and best practices across all documentation
  - Updated tool counts and categorizations

### Performance
- Menu creation is now ~80% faster with single command instead of manual hierarchy building
- Compilation debugging improved with automatic log collection

---

## [1.1.0] - 2025-01-XX

### Added
- Design pattern generation system
- Template customization with `unity_template_manage`
- Enhanced component management with batch operations
- SerializeField private field support

### Changed
- Improved compilation detection and waiting
- Enhanced WebSocket connection resilience
- Better error handling and timeout management

---

## [1.0.0] - 2024-XX-XX

### Added
- Initial release
- 28 Unity Editor tools via MCP
- WebSocket bridge architecture
- Real-time scene management
- GameObject and component CRUD operations
- UI creation with UGUI templates
- Asset and script management
- Prefab workflow support
- Project settings management

[1.4.0]: https://github.com/kuroyasouiti/SkillForUnity/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/kuroyasouiti/SkillForUnity/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/kuroyasouiti/SkillForUnity/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/kuroyasouiti/SkillForUnity/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/kuroyasouiti/SkillForUnity/releases/tag/v1.0.0
