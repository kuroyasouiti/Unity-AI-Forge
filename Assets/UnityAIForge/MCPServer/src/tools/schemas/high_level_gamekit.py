"""Schema definitions for High-Level GameKit MCP tools (UI + Data)."""

from __future__ import annotations

from typing import Any

from tools.schemas.common import schema_with_required


def gamekit_ui_schema() -> dict[str, Any]:
    """Schema for the unified unity_gamekit_ui MCP tool.

    Consolidates 5 widget types (command, binding, list, slot, selection)
    behind a single tool with a ``widgetType`` discriminator.
    """
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                # ── discriminator ──────────────────────────────────────
                "widgetType": {
                    "type": "string",
                    "enum": ["command", "binding", "list", "slot", "selection"],
                    "description": "Widget type to operate on.",
                },
                "operation": {
                    "type": "string",
                    "enum": [
                        # common
                        "create",
                        "createMultiple",
                        "inspect",
                        # command-specific
                        "createCommandPanel",
                        "addCommand",
                        # binding-specific
                        "setRange",
                        "refresh",
                        "findByBindingId",
                        # list-specific
                        "setItems",
                        "addItem",
                        "removeItem",
                        "clear",
                        "selectItem",
                        "deselectItem",
                        "clearSelection",
                        "refreshFromSource",
                        "findByListId",
                        # slot-specific
                        "setItem",
                        "clearSlot",
                        "setHighlight",
                        "createSlotBar",
                        "inspectSlotBar",
                        "useSlot",
                        "refreshFromInventory",
                        "findBySlotId",
                        "findByBarId",
                        # selection-specific
                        "selectItemById",
                        "setSelectionActions",
                        "setItemEnabled",
                        "findBySelectionId",
                    ],
                    "description": "Operation to perform. Available operations depend on widgetType.",
                },
                # ── shared identifiers ─────────────────────────────────
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject path.",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path (auto-creates UIDocument with UXML/USS).",
                },
                "name": {
                    "type": "string",
                    "description": "Name for the created GameObject when using parentPath.",
                },
                "uiOutputDir": {
                    "type": "string",
                    "description": "Output directory for generated UXML/USS files (default: 'Assets/UI/Generated').",
                },
                "className": {
                    "type": "string",
                    "description": "Custom class name for generated script.",
                },
                # ── command properties ─────────────────────────────────
                "panelId": {"type": "string", "description": "Unique command panel identifier."},
                "targetType": {
                    "type": "string",
                    "enum": ["actor", "manager"],
                    "description": "Target type: 'actor' for GameKitActor or 'manager' for GameKitManager.",
                },
                "targetActorId": {
                    "type": "string",
                    "description": "Target actor ID (when targetType is 'actor').",
                },
                "targetManagerId": {
                    "type": "string",
                    "description": "Target manager ID (when targetType is 'manager').",
                },
                "commands": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "label": {"type": "string"},
                            "commandType": {
                                "type": "string",
                                "enum": [
                                    "move",
                                    "jump",
                                    "action",
                                    "look",
                                    "custom",
                                    "addResource",
                                    "setResource",
                                    "consumeResource",
                                    "changeState",
                                    "nextTurn",
                                    "triggerScene",
                                ],
                                "description": "Command type: Actor commands (move/jump/action/look/custom) or Manager commands (addResource/setResource/consumeResource/changeState/nextTurn/triggerScene).",
                            },
                            "commandParameter": {
                                "type": "string",
                                "description": "Parameter for action/resource/state commands.",
                            },
                            "resourceAmount": {
                                "type": "number",
                                "description": "Amount for resource commands (addResource/setResource/consumeResource).",
                            },
                            "moveDirection": {
                                "type": "object",
                                "properties": {
                                    "x": {"type": "number"},
                                    "y": {"type": "number"},
                                    "z": {"type": "number"},
                                },
                                "description": "Direction vector for move commands.",
                            },
                            "lookDirection": {
                                "type": "object",
                                "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                                "description": "Direction vector for look commands.",
                            },
                        },
                    },
                    "description": "List of commands to create as buttons in UXML.",
                },
                # ── binding properties ─────────────────────────────────
                "bindingId": {"type": "string", "description": "Unique binding identifier."},
                "elementName": {
                    "type": "string",
                    "description": "VisualElement name to bind to within the UIDocument (e.g., 'hp-bar'). Queried via rootVisualElement.Q(name).",
                },
                "sourceType": {
                    "type": "string",
                    "enum": ["health", "economy", "timer", "custom"],
                    "description": "Data source type: 'health' (GameKitHealth), 'economy' (GameKitManager resource), 'timer' (GameKitTimer), 'custom' (manual updates).",
                },
                "sourceId": {
                    "type": "string",
                    "description": "Source component ID (healthId, managerId, or timerId).",
                },
                "targetProperty": {
                    "type": "string",
                    "description": "Resource name for economy source, or property for custom targets.",
                },
                "format": {
                    "type": "string",
                    "enum": ["raw", "percent", "formatted", "ratio"],
                    "description": "Value display format: 'raw' (75), 'percent' (75%), 'formatted' (custom), 'ratio' (75/100).",
                },
                "formatString": {
                    "type": "string",
                    "description": "Custom format string for 'formatted' mode (e.g., 'HP: {0}/{1}').",
                },
                "minValue": {"type": "number", "description": "Minimum value for range."},
                "maxValue": {"type": "number", "description": "Maximum value for range."},
                "updateInterval": {
                    "type": "number",
                    "description": "Polling interval in seconds (default: 0.1).",
                },
                "smoothTransition": {
                    "type": "boolean",
                    "description": "Enable smooth value transitions.",
                },
                "smoothSpeed": {
                    "type": "number",
                    "description": "Smooth transition speed (default: 5.0).",
                },
                "bindings": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": True,
                        "description": "Binding definition (same properties as single create: bindingId, sourceType, etc.).",
                    },
                    "description": "Array of bindings for createMultiple operation (widgetType='binding'). Each uses same properties as single create.",
                },
                # ── list properties ────────────────────────────────────
                "listId": {"type": "string", "description": "Unique list identifier."},
                "layout": {
                    "type": "string",
                    "enum": ["horizontal", "vertical", "grid"],
                    "description": "Layout direction for command/list/slot/selection widgets (default varies by widget).",
                },
                "columns": {
                    "type": "integer",
                    "description": "Number of columns for grid layout (default: 4).",
                },
                "cellSize": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                    "description": "Cell size for grid layout.",
                },
                "spacing": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                    "description": "Spacing between items (object for list, number for slot/selection).",
                },
                "dataSource": {
                    "type": "string",
                    "enum": ["custom", "inventory", "equipment"],
                    "description": "Data source type: 'custom' (manual), 'inventory' (GameKitInventory), 'equipment' (equipped items).",
                },
                "selectable": {
                    "type": "boolean",
                    "description": "Allow item selection (default: true).",
                },
                "multiSelect": {
                    "type": "boolean",
                    "description": "Allow multiple selection (default: false).",
                },
                "items": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "id": {"type": "string", "description": "Item unique ID."},
                            "name": {"type": "string", "description": "Item display name."},
                            "label": {"type": "string", "description": "Item display label."},
                            "description": {"type": "string", "description": "Item description."},
                            "iconPath": {"type": "string", "description": "Path to icon asset."},
                            "quantity": {"type": "integer", "description": "Item quantity."},
                            "enabled": {"type": "boolean", "description": "Item enabled state."},
                            "defaultSelected": {
                                "type": "boolean",
                                "description": "Default selected.",
                            },
                            "associatedPanelPath": {
                                "type": "string",
                                "description": "GameObject path of associated panel (selection widget tab type only).",
                            },
                        },
                    },
                    "description": "Items for setItems operation.",
                },
                "item": {
                    "type": "object",
                    "properties": {
                        "id": {"type": "string"},
                        "name": {"type": "string"},
                        "label": {"type": "string"},
                        "description": {"type": "string"},
                        "iconPath": {"type": "string"},
                        "quantity": {"type": "integer"},
                        "enabled": {"type": "boolean"},
                        "defaultSelected": {"type": "boolean"},
                        "associatedPanelPath": {
                            "type": "string",
                            "description": "Selection widget tab type only.",
                        },
                    },
                    "description": "Item data for addItem operation.",
                },
                "index": {
                    "type": "integer",
                    "description": "Item index for selection/removal operations.",
                },
                "itemId": {
                    "type": "string",
                    "description": "Item ID for selection/removal by ID.",
                },
                # ── slot properties ────────────────────────────────────
                "slotId": {"type": "string", "description": "Unique slot identifier."},
                "barId": {"type": "string", "description": "Unique slot bar identifier."},
                "size": {
                    "type": "number",
                    "description": "Slot size (width and height) when using parentPath (default: 64).",
                },
                "width": {
                    "type": "number",
                    "description": "Width of the slot UI (overrides size).",
                },
                "height": {
                    "type": "number",
                    "description": "Height of the slot UI (overrides size).",
                },
                "slotType": {
                    "type": "string",
                    "enum": ["storage", "equipment", "quickslot", "trash"],
                    "description": "Slot type: 'storage', 'equipment', 'quickslot', or 'trash'.",
                },
                "acceptedCategories": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Accepted item categories (empty = accept all).",
                },
                "equipmentSlot": {
                    "type": "string",
                    "description": "Equipment slot name (for equipment type).",
                },
                "slotIndex": {
                    "type": "integer",
                    "description": "Slot index in slot bar.",
                },
                "slotCount": {
                    "type": "integer",
                    "description": "Number of slots for createSlotBar.",
                },
                "slotSize": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                    "description": "Slot size.",
                },
                "inventoryId": {
                    "type": "string",
                    "description": "Inventory ID to bind to.",
                },
                "startIndex": {
                    "type": "integer",
                    "description": "Starting inventory slot index.",
                },
                "iconPath": {
                    "type": "string",
                    "description": "Icon asset path for setItem.",
                },
                "quantity": {
                    "type": "integer",
                    "description": "Item quantity for setItem.",
                },
                "highlighted": {
                    "type": "boolean",
                    "description": "Highlight state for setHighlight.",
                },
                # ── selection properties ───────────────────────────────
                "selectionId": {
                    "type": "string",
                    "description": "Unique selection group identifier.",
                },
                "selectionType": {
                    "type": "string",
                    "enum": ["radio", "toggle", "checkbox", "tab"],
                    "description": "Selection type: 'radio' (single), 'toggle' (single+off), 'checkbox' (multi), 'tab' (with panels).",
                },
                "allowNone": {
                    "type": "boolean",
                    "description": "Allow no selection (default: false).",
                },
                "defaultIndex": {
                    "type": "integer",
                    "description": "Default selected index.",
                },
                "fireEvents": {
                    "type": "boolean",
                    "description": "Fire selection events (default: true).",
                },
                "enabled": {
                    "type": "boolean",
                    "description": "Enabled state for setItemEnabled.",
                },
                "actions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "selectedId": {
                                "type": "string",
                                "description": "Selection ID that triggers this action.",
                            },
                            "showPaths": {
                                "type": "array",
                                "items": {"type": "string"},
                                "description": "GameObject paths to show.",
                            },
                            "hidePaths": {
                                "type": "array",
                                "items": {"type": "string"},
                                "description": "GameObject paths to hide.",
                            },
                        },
                    },
                    "description": "Selection actions for setSelectionActions.",
                },
            },
        },
        ["widgetType", "operation"],
    )


def gamekit_data_schema() -> dict[str, Any]:
    """Schema for the unified unity_gamekit_data MCP tool.

    Consolidates pool + data (eventChannel, dataContainer, runtimeSet)
    behind a single tool with a ``dataType`` discriminator.
    """
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                # ── discriminator ──────────────────────────────────────
                "dataType": {
                    "type": "string",
                    "enum": ["pool", "eventChannel", "dataContainer", "runtimeSet"],
                    "description": "Data type to operate on.",
                },
                "operation": {
                    "type": "string",
                    "enum": ["create", "createMultiple", "inspect", "find", "getIntegrationCode"],
                    "description": "Operation to perform. 'createMultiple' generates multiple scripts in one call (single compilation wait). 'getIntegrationCode' returns C# code snippets and MCP wiring commands for integrating a generated asset into game scripts.",
                },
                # ── shared ─────────────────────────────────────────────
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                # ── pool properties ────────────────────────────────────
                "poolId": {
                    "type": "string",
                    "description": "Unique object pool identifier.",
                },
                "prefabPath": {
                    "type": "string",
                    "description": "Prefab asset path for pooled objects (e.g., 'Assets/Prefabs/Bullet.prefab').",
                },
                "initialSize": {
                    "type": "integer",
                    "minimum": 0,
                    "description": "Number of objects to pre-instantiate (default: 10).",
                },
                "maxSize": {
                    "type": "integer",
                    "minimum": 1,
                    "description": "Maximum pool size (default: 100).",
                },
                "defaultParentPath": {
                    "type": "string",
                    "description": "Default parent Transform path for pooled objects.",
                },
                "collectionCheck": {
                    "type": "boolean",
                    "description": "Enable double-release detection (default: true).",
                },
                # ── data properties (eventChannel/dataContainer/runtimeSet) ──
                "dataId": {
                    "type": "string",
                    "description": "Unique data asset identifier.",
                },
                "assetPath": {
                    "type": "string",
                    "description": "Output asset path (e.g., 'Assets/Data/OnPlayerDeath.asset').",
                },
                "eventType": {
                    "type": "string",
                    "enum": ["void", "int", "float", "string", "Vector3", "GameObject"],
                    "description": "Event channel payload type (default: void).",
                },
                "createListener": {
                    "type": "boolean",
                    "description": "Also create an EventListener MonoBehaviour on targetPath (default: false).",
                },
                "fields": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {
                                "type": "string",
                                "description": "Field name.",
                            },
                            "fieldType": {
                                "type": "string",
                                "enum": [
                                    "int",
                                    "float",
                                    "string",
                                    "bool",
                                    "Vector2",
                                    "Vector3",
                                    "Color",
                                ],
                                "description": "Field type.",
                            },
                            "defaultValue": {
                                "description": "Default value for the field.",
                            },
                        },
                    },
                    "description": "Fields for DataContainer (name, fieldType, defaultValue).",
                },
                "resetOnPlay": {
                    "type": "boolean",
                    "description": "Reset DataContainer values on play mode entry (default: true).",
                },
                "elementType": {
                    "type": "string",
                    "description": "Fully qualified type name for RuntimeSet elements (default: GameObject).",
                },
                "autoCreateAsset": {
                    "type": "boolean",
                    "description": "Auto-create ScriptableObject .asset file after compilation (default: false). Requires assetPath.",
                },
                "items": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "additionalProperties": True,
                        "description": "Item definition (same properties as single create: dataId, eventType, fields, etc.).",
                    },
                    "description": "Array of items for createMultiple operation. Each item uses the same properties as single create.",
                },
                "scriptOutputDir": {
                    "type": "string",
                    "description": "Output directory for generated scripts (default: 'Assets/Scripts/Generated').",
                },
            },
        },
        ["dataType", "operation"],
    )
