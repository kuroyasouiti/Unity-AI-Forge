# GameKit Framework - Complete MCP Tool Guide

**High-level game development framework with GameKit UI, Systems, and Logic pillars.**

GameKit uses code generation to produce standalone C# scripts from templates, so user projects have zero runtime dependency on Unity-AI-Forge.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [UI Pillar](#ui-pillar)
   - [UICommand](#uicommand)
   - [UIBinding](#uibinding)
   - [UIList](#uilist)
   - [UISlot](#uislot)
   - [UISelection](#uiselection)
3. [Logic Pillar](#logic-pillar)
4. [Code Generation Workflow](#code-generation-workflow)
5. [Complete Game Example](#complete-game-example)
6. [Best Practices](#best-practices)

---

## Architecture Overview

GameKit is organized into pillars:

| Pillar | Tools | Purpose |
|--------|-------|---------|
| **UI** | 1 tool | `unity_gamekit_ui` (widgetType: command/binding/list/slot/selection) |
| **Logic** | 7 tools | Scene/code analysis and integrity validation |
| **Data** | 1 tool | `unity_gamekit_data` (dataType: pool/eventChannel/dataContainer/runtimeSet) |

All UI pillar tools use **code generation**:
- Templates in `Assets/UnityAIForge/Editor/CodeGen/Templates/`
- Generated scripts output to `Assets/Scripts/Generated/` by default
- Generated code uses only standard Unity APIs (zero package dependency)
- `create` operations require `unity_compilation_await` afterward

---

## UI Pillar

### UICommand

**MCP Tool:** `unity_gamekit_ui(widgetType='command')`

Creates button command panels using UI Toolkit. Generates UXML, USS, and a C# script with UnityEvent bindings.

#### Operations

| Operation | Description | Requires Compilation |
|-----------|-------------|---------------------|
| `createCommandPanel` | Generate panel with buttons | Yes |
| `addCommand` | Add button to existing panel | No |
| `inspect` | View panel configuration | No |
| `delete` | Remove panel and generated files | No |

#### Parameters (createCommandPanel)

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `panelId` | string | Yes | Unique identifier |
| `commands` | array | Yes | Array of command definitions |
| `layout` | string | No | `horizontal`, `vertical`, `grid` |
| `className` | string | No | Custom class name |
| `outputPath` | string | No | Output directory |

**Command object properties:**

| Property | Type | Description |
|----------|------|-------------|
| `name` | string | Command identifier |
| `label` | string | Button display text |
| `commandType` | string | `move`, `jump`, `action`, `look`, `custom` |
| `commandParameter` | string | Parameter for action commands |
| `moveDirection` | Vector3 | Direction for move commands |
| `lookDirection` | Vector2 | Direction for look commands |

#### Example

```python
# Create a mobile game control panel
unity_gamekit_ui(widgetType='command', {
    "operation": "createCommandPanel",
    "panelId": "mobileControls",
    "layout": "grid",
    "commands": [
        {"name": "moveUp", "label": "↑", "commandType": "move",
         "moveDirection": {"x": 0, "y": 0, "z": 1}},
        {"name": "moveDown", "label": "↓", "commandType": "move",
         "moveDirection": {"x": 0, "y": 0, "z": -1}},
        {"name": "moveLeft", "label": "←", "commandType": "move",
         "moveDirection": {"x": -1, "y": 0, "z": 0}},
        {"name": "moveRight", "label": "→", "commandType": "move",
         "moveDirection": {"x": 1, "y": 0, "z": 0}},
        {"name": "jump", "label": "Jump", "commandType": "jump"},
        {"name": "attack", "label": "Attack", "commandType": "action",
         "commandParameter": "melee"}
    ]
})
unity_compilation_await()
```

---

### UIBinding

**MCP Tool:** `unity_gamekit_ui(widgetType='binding')`

Declarative data binding from game state to UI elements. Supports ProgressBar, Label, Slider, and other UI Toolkit elements.

#### Operations

| Operation | Description | Requires Compilation |
|-----------|-------------|---------------------|
| `create` | Create data binding | Yes |
| `update` | Modify binding settings | No |
| `inspect` | View binding config | No |
| `delete` | Remove binding | No |
| `setRange` | Update min/max range | No |
| `refresh` | Force refresh (play mode) | No |
| `findByBindingId` | Find binding by ID | No |

#### Parameters (create)

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `bindingId` | string | Yes | Unique identifier |
| `sourceType` | string | Yes | `health`, `economy`, `timer`, `custom` |
| `sourceId` | string | Yes | Source component ID |
| `elementName` | string | Yes | UI element name |
| `targetProperty` | string | No | Property to bind |
| `format` | string | No | `raw`, `percent`, `formatted`, `ratio` |
| `formatString` | string | No | Custom format (e.g., `"HP: {0}/{1}"`) |
| `minValue` | number | No | Minimum value |
| `maxValue` | number | No | Maximum value |
| `updateInterval` | number | No | Polling interval (seconds) |
| `smoothTransition` | boolean | No | Enable smooth interpolation |
| `smoothSpeed` | number | No | Interpolation speed |

#### Example

```python
# Bind health bar to health component
unity_gamekit_ui(widgetType='binding', {
    "operation": "create",
    "bindingId": "playerHP",
    "sourceType": "health",
    "sourceId": "player_health",
    "elementName": "hp-progress",
    "targetProperty": "value",
    "format": "percent",
    "minValue": 0,
    "maxValue": 100,
    "smoothTransition": true,
    "smoothSpeed": 8.0
})
unity_compilation_await()

# Bind gold counter to economy manager
unity_gamekit_ui(widgetType='binding', {
    "operation": "create",
    "bindingId": "goldDisplay",
    "sourceType": "economy",
    "sourceId": "player_economy",
    "elementName": "gold-label",
    "targetProperty": "gold",
    "format": "formatted",
    "formatString": "Gold: {0}"
})
unity_compilation_await()
```

---

### UIList

**MCP Tool:** `unity_gamekit_ui(widgetType='list')`

Dynamic ScrollView-based list/grid for displaying item collections with selection support.

#### Operations

| Operation | Description | Requires Compilation |
|-----------|-------------|---------------------|
| `create` | Create list component | Yes |
| `update` | Modify layout/settings | No |
| `inspect` | View list state | No |
| `delete` | Remove list | No |
| `setItems` | Replace all items | No |
| `addItem` | Add single item | No |
| `removeItem` | Remove item | No |
| `clear` | Remove all items | No |
| `selectItem` | Select by index | No |
| `deselectItem` | Deselect by index | No |
| `clearSelection` | Clear all selections | No |
| `refreshFromSource` | Reload from source | No |
| `findByListId` | Find list by ID | No |

#### Parameters (create)

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `listId` | string | Yes | Unique identifier |
| `layout` | string | No | `vertical`, `horizontal`, `grid` |
| `columns` | integer | No | Column count (grid layout) |
| `cellSize` | Vector2 | No | Cell dimensions |
| `spacing` | Vector2 | No | Gap between items |
| `dataSource` | string | No | `custom`, `inventory`, `equipment` |
| `sourceId` | string | No | Data source ID |
| `selectable` | boolean | No | Enable selection |
| `multiSelect` | boolean | No | Allow multi-selection |

**Item object:**

| Property | Type | Description |
|----------|------|-------------|
| `id` | string | Item identifier |
| `name` | string | Display name |
| `description` | string | Item description |
| `iconPath` | string | Icon asset path |
| `quantity` | integer | Item count |
| `enabled` | boolean | Interactable |

#### Example

```python
# Create inventory grid
unity_gamekit_ui(widgetType='list', {
    "operation": "create",
    "listId": "inventory",
    "layout": "grid",
    "columns": 5,
    "cellSize": {"x": 80, "y": 80},
    "spacing": {"x": 4, "y": 4},
    "selectable": true,
    "multiSelect": false
})
unity_compilation_await()

# Populate with items
unity_gamekit_ui(widgetType='list', {
    "operation": "setItems",
    "listId": "inventory",
    "items": [
        {"id": "sword", "name": "Iron Sword", "quantity": 1,
         "iconPath": "Assets/Icons/sword.png"},
        {"id": "potion", "name": "Health Potion", "quantity": 5,
         "iconPath": "Assets/Icons/potion.png"},
        {"id": "shield", "name": "Wood Shield", "quantity": 1,
         "iconPath": "Assets/Icons/shield.png"}
    ]
})
```

---

### UISlot

**MCP Tool:** `unity_gamekit_ui(widgetType='slot')`

Slot-based UI for equipment, quickslots, and inventory management. Supports both individual slots and slot bars (grouped slots).

#### Operations

| Operation | Description | Requires Compilation |
|-----------|-------------|---------------------|
| `create` | Create single slot | Yes |
| `update` | Modify slot settings | No |
| `inspect` | View slot state | No |
| `delete` | Remove slot | No |
| `setItem` | Place item in slot | No |
| `clearSlot` | Remove item from slot | No |
| `setHighlight` | Toggle highlight visual | No |
| `createSlotBar` | Create slot bar | Yes |
| `updateSlotBar` | Modify bar settings | No |
| `inspectSlotBar` | View bar details | No |
| `deleteSlotBar` | Remove bar | No |
| `useSlot` | Use item (play mode) | No |
| `refreshFromInventory` | Reload from source | No |
| `findBySlotId` | Find slot by ID | No |
| `findByBarId` | Find bar by ID | No |

#### Parameters (createSlotBar)

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `barId` | string | Yes | Unique bar identifier |
| `slotCount` | integer | Yes | Number of slots |
| `slotType` | string | No | `storage`, `equipment`, `quickslot`, `trash` |
| `layout` | string | No | `horizontal`, `vertical`, `grid` |
| `dragDropEnabled` | boolean | No | Enable drag and drop |
| `acceptedCategories` | array | No | Allowed item categories |
| `slotSize` | Vector2 | No | Slot pixel dimensions |

#### Example

```python
# Create equipment slots
unity_gamekit_ui(widgetType='slot', {
    "operation": "create",
    "slotId": "helmet_slot",
    "slotType": "equipment",
    "equipmentSlot": "head",
    "acceptedCategories": ["helmet", "hat"]
})
unity_compilation_await()

# Create quickslot bar
unity_gamekit_ui(widgetType='slot', {
    "operation": "createSlotBar",
    "barId": "quickslots",
    "slotCount": 8,
    "slotType": "quickslot",
    "layout": "horizontal",
    "dragDropEnabled": true
})
unity_compilation_await()

# Place item in slot
unity_gamekit_ui(widgetType='slot', {
    "operation": "setItem",
    "slotId": "quickslots_slot_0",
    "itemId": "potion_hp",
    "itemName": "Health Potion",
    "quantity": 5,
    "iconPath": "Assets/Icons/potion_hp.png"
})
```

---

### UISelection

**MCP Tool:** `unity_gamekit_ui(widgetType='selection')`

Selection groups for radio buttons, toggles, checkboxes, and tabs. Supports SelectionActions to control panel visibility based on selection.

#### Operations

| Operation | Description | Requires Compilation |
|-----------|-------------|---------------------|
| `create` | Create selection group | Yes |
| `update` | Modify settings | No |
| `inspect` | View selection state | No |
| `delete` | Remove group | No |
| `setItems` | Replace all items | No |
| `addItem` | Add item | No |
| `removeItem` | Remove item | No |
| `clear` | Remove all items | No |
| `selectItem` | Select by index | No |
| `selectItemById` | Select by ID | No |
| `deselectItem` | Deselect by index | No |
| `clearSelection` | Clear all | No |
| `setSelectionActions` | Set visibility rules | No |
| `setItemEnabled` | Enable/disable item | No |
| `findBySelectionId` | Find by ID | No |

#### Parameters (create)

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `selectionId` | string | Yes | Unique identifier |
| `selectionType` | string | Yes | `radio`, `toggle`, `checkbox`, `tab` |
| `items` | array | Yes | Selection item objects |
| `layout` | string | No | `horizontal`, `vertical`, `grid` |
| `allowNone` | boolean | No | Allow empty selection |
| `defaultIndex` | integer | No | Initial selection |
| `spacing` | number | No | Gap between items (px) |

**Item object:**

| Property | Type | Description |
|----------|------|-------------|
| `id` | string | Item identifier |
| `label` | string | Display text |
| `iconPath` | string | Icon asset path |
| `enabled` | boolean | Interactable |
| `defaultSelected` | boolean | Selected by default |
| `associatedPanelPath` | string | Panel to show on select |

**SelectionAction object:**

| Property | Type | Description |
|----------|------|-------------|
| `selectedId` | string | Item ID that triggers this action |
| `showPaths` | array | Paths to show when selected |
| `hidePaths` | array | Paths to hide when selected |

#### Example

```python
# Create difficulty selection (radio)
unity_gamekit_ui(widgetType='selection', {
    "operation": "create",
    "selectionId": "difficulty",
    "selectionType": "radio",
    "layout": "vertical",
    "items": [
        {"id": "easy", "label": "Easy"},
        {"id": "normal", "label": "Normal", "defaultSelected": true},
        {"id": "hard", "label": "Hard"}
    ]
})
unity_compilation_await()

# Create tab navigation with panel switching
unity_gamekit_ui(widgetType='selection', {
    "operation": "create",
    "selectionId": "menuTabs",
    "selectionType": "tab",
    "layout": "horizontal",
    "items": [
        {"id": "status", "label": "Status"},
        {"id": "inventory", "label": "Inventory"},
        {"id": "skills", "label": "Skills"}
    ]
})
unity_compilation_await()

unity_gamekit_ui(widgetType='selection', {
    "operation": "setSelectionActions",
    "selectionId": "menuTabs",
    "actions": [
        {"selectedId": "status",
         "showPaths": ["StatusPanel"],
         "hidePaths": ["InventoryPanel", "SkillPanel"]},
        {"selectedId": "inventory",
         "showPaths": ["InventoryPanel"],
         "hidePaths": ["StatusPanel", "SkillPanel"]},
        {"selectedId": "skills",
         "showPaths": ["SkillPanel"],
         "hidePaths": ["StatusPanel", "InventoryPanel"]}
    ]
})
```

---

## Logic Pillar

The Logic pillar provides analysis and validation tools. These do not use code generation.

| Tool | Description |
|------|-------------|
| `unity_validate_integrity` | Check for missing scripts, null references, broken events/prefabs |
| `unity_class_catalog` | Enumerate and inspect types (classes, MonoBehaviours, enums) |
| `unity_class_dependency_graph` | Analyze C# class dependencies |
| `unity_scene_reference_graph` | Analyze references between GameObjects in scene |
| `unity_scene_relationship_graph` | Analyze scene transitions and relationships |
| `unity_scene_dependency` | Analyze scene asset dependencies (AssetDatabase) |
| `unity_script_syntax` | C# source code structure analysis with line numbers |

### Verifying Changes

Use logic pillar tools after making significant changes:

```python
# Check for broken references
unity_scene_relationship_graph({
    "operation": "analyze",
    "includeReferences": true,
    "includeEvents": true
})

# Check before deleting an object
unity_scene_reference_graph({
    "operation": "analyze",
    "rootPath": "TargetObject",
    "direction": "incoming"
})
```

---

## Code Generation Workflow

### Generated File Structure

```
Assets/Scripts/Generated/
├── PlayerControlsCommandPanel.cs     ← UICommand
├── PlayerHPBinding.cs                ← UIBinding
├── InventoryList.cs                  ← UIList
├── QuickSlots.cs                     ← UISlot
└── DifficultySelection.cs            ← UISelection

Assets/UI/Generated/                  ← UI Toolkit assets
├── PlayerControls.uxml
├── PlayerControls.uss
├── Inventory.uxml
└── Inventory.uss
```

### Registry Pattern

All generated components register themselves using a static dictionary:

```csharp
// From any script, find a generated component by its ID
var hpBar = PlayerHPBinding.FindById("playerHP");
var controls = PlayerControlsCommandPanel.FindById("mobileControls");
var inventory = InventoryList.FindById("inventory");
```

### Compilation Flow

```
1. Call create operation     → Script file generated
2. unity_compilation_await   → Wait for Unity to compile
3. Component auto-attached   → Ready to use
4. Subsequent operations     → No compilation needed
```

---

## Complete Game Example

### RPG with UI

```python
# === Step 1: Create UI ===

# Tab navigation
unity_gamekit_ui(widgetType='selection', {
    "operation": "create",
    "selectionId": "gameTabs",
    "selectionType": "tab",
    "items": [
        {"id": "game", "label": "Game"},
        {"id": "inventory", "label": "Inventory"},
        {"id": "status", "label": "Status"}
    ]
})
unity_compilation_await()

# HP bar binding
unity_gamekit_ui(widgetType='binding', {
    "operation": "create",
    "bindingId": "hpBar",
    "sourceType": "health",
    "sourceId": "player_hp",
    "elementName": "hp-progress",
    "format": "ratio",
    "formatString": "{0}/{1}",
    "smoothTransition": true,
    "smoothSpeed": 5.0
})
unity_compilation_await()

# Quickslot bar
unity_gamekit_ui(widgetType='slot', {
    "operation": "createSlotBar",
    "barId": "quickbar",
    "slotCount": 6,
    "slotType": "quickslot",
    "layout": "horizontal"
})
unity_compilation_await()

# === Step 2: Verify integrity ===
unity_validate_integrity({"operation": "checkAll"})
```

---

## Best Practices

### 1. Compilation Batching

Minimize compilation waits by creating multiple components before awaiting:

```python
# Create multiple components
unity_gamekit_ui(widgetType='command', {"operation": "createCommandPanel", ...})
# Don't await yet - create more components first if they don't depend on each other

# Await once for all
unity_compilation_await()
```

### 2. ID Naming Convention

Use consistent, descriptive IDs:
- UI: `playerHP`, `inventoryList`, `quickbar`
- Pool: `bulletPool`, `enemyPool`
- Data: `scoreChannel`, `playerConfig`

### 3. Use FindById for Cross-References

Generated components expose `FindById()` for easy access:
```csharp
// From any script
var hpBar = PlayerHPBinding.FindById("playerHP");
```

### 4. Inspect Before Modify

Always inspect existing components before making changes:
```python
unity_gamekit_ui(widgetType='list', {"operation": "inspect", "listId": "inventory"})
```

### 5. Verify After Major Changes

Use logic pillar tools to validate:
```python
unity_validate_integrity({"operation": "checkAll"})
unity_scene_reference_graph({"operation": "analyze", "rootPath": "Player"})
```

---

**GameKit provides a complete toolkit for AI-driven game development. Use the GameKit UI, Systems, and Logic pillars to build polished games efficiently.**
