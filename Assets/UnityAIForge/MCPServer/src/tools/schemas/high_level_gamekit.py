"""Schema definitions for High-Level GameKit MCP tools (UI + Systems)."""

from __future__ import annotations

from typing import Any

from tools.schemas.common import schema_with_required


def gamekit_ui_command_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_ui_command MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["createCommandPanel", "addCommand", "inspect", "delete"],
                },
                "panelId": {"type": "string", "description": "Unique command panel identifier."},
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path for the UIDocument (optional, creates at scene root if omitted).",
                },
                "uiOutputDir": {
                    "type": "string",
                    "description": "Output directory for generated UXML/USS files (default: 'Assets/UI/Generated').",
                },
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
                            "icon": {"type": "string"},
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
                "layout": {
                    "type": "string",
                    "enum": ["horizontal", "vertical", "grid"],
                    "description": "Button layout style (maps to USS flex-direction).",
                },
            },
        },
        ["operation"],
    )


def gamekit_ui_binding_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_ui_binding MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "setRange",
                        "refresh",
                        "findByBindingId",
                    ],
                    "description": "UI binding operation.",
                },
                "bindingId": {"type": "string", "description": "Unique binding identifier."},
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject path with UIDocument (e.g., 'GameUI/HPBar').",
                },
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
            },
        },
        ["operation"],
    )


def gamekit_ui_list_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_ui_list MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "setItems",
                        "addItem",
                        "removeItem",
                        "clear",
                        "selectItem",
                        "deselectItem",
                        "clearSelection",
                        "refreshFromSource",
                        "findByListId",
                    ],
                    "description": "UI list operation.",
                },
                "listId": {"type": "string", "description": "Unique list identifier."},
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject path for list component (use existing GameObject).",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path to create new UI list under (auto-creates UIDocument with UXML/USS).",
                },
                "name": {
                    "type": "string",
                    "description": "Name for the created GameObject when using parentPath.",
                },
                "uiOutputDir": {
                    "type": "string",
                    "description": "Output directory for generated UXML/USS files (default: 'Assets/UI/Generated').",
                },
                "layout": {
                    "type": "string",
                    "enum": ["vertical", "horizontal", "grid"],
                    "description": "Layout type: 'vertical', 'horizontal', or 'grid'.",
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
                    "description": "Spacing between items.",
                },
                "dataSource": {
                    "type": "string",
                    "enum": ["custom", "inventory", "equipment"],
                    "description": "Data source type: 'custom' (manual), 'inventory' (GameKitInventory), 'equipment' (equipped items).",
                },
                "sourceId": {
                    "type": "string",
                    "description": "Source ID for inventory/equipment data source.",
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
                            "description": {"type": "string", "description": "Item description."},
                            "iconPath": {"type": "string", "description": "Path to icon asset."},
                            "quantity": {"type": "integer", "description": "Item quantity."},
                            "enabled": {"type": "boolean", "description": "Item enabled state."},
                        },
                    },
                    "description": "List items for setItems operation.",
                },
                "item": {
                    "type": "object",
                    "properties": {
                        "id": {"type": "string"},
                        "name": {"type": "string"},
                        "description": {"type": "string"},
                        "iconPath": {"type": "string"},
                        "quantity": {"type": "integer"},
                        "enabled": {"type": "boolean"},
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
            },
        },
        ["operation"],
    )


def gamekit_ui_slot_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_ui_slot MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "setItem",
                        "clearSlot",
                        "setHighlight",
                        "createSlotBar",
                        "updateSlotBar",
                        "inspectSlotBar",
                        "deleteSlotBar",
                        "useSlot",
                        "refreshFromInventory",
                        "findBySlotId",
                        "findByBarId",
                    ],
                    "description": "UI slot operation.",
                },
                "slotId": {"type": "string", "description": "Unique slot identifier."},
                "barId": {"type": "string", "description": "Unique slot bar identifier."},
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject path (use existing GameObject).",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path to create new UI slot under (auto-creates UIDocument with UXML/USS).",
                },
                "name": {
                    "type": "string",
                    "description": "Name for the created GameObject when using parentPath.",
                },
                "uiOutputDir": {
                    "type": "string",
                    "description": "Output directory for generated UXML/USS files (default: 'Assets/UI/Generated').",
                },
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
                "layout": {
                    "type": "string",
                    "enum": ["horizontal", "vertical", "grid"],
                    "description": "Layout type for slot bar.",
                },
                "spacing": {
                    "type": "number",
                    "description": "Spacing between slots.",
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
                "itemId": {"type": "string", "description": "Item ID for setItem."},
                "quantity": {
                    "type": "integer",
                    "description": "Item quantity for setItem.",
                },
                "iconPath": {
                    "type": "string",
                    "description": "Icon asset path for setItem.",
                },
                "highlighted": {
                    "type": "boolean",
                    "description": "Highlight state for setHighlight.",
                },
            },
        },
        ["operation"],
    )


def gamekit_ui_selection_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_ui_selection MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "setItems",
                        "addItem",
                        "removeItem",
                        "clear",
                        "selectItem",
                        "selectItemById",
                        "deselectItem",
                        "clearSelection",
                        "setSelectionActions",
                        "setItemEnabled",
                        "findBySelectionId",
                    ],
                    "description": "UI selection operation.",
                },
                "selectionId": {"type": "string", "description": "Unique selection group identifier."},
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject path (use existing GameObject).",
                },
                "parentPath": {
                    "type": "string",
                    "description": "Parent GameObject path to create new UI selection under (auto-creates UIDocument with UXML/USS).",
                },
                "name": {
                    "type": "string",
                    "description": "Name for the created GameObject when using parentPath.",
                },
                "uiOutputDir": {
                    "type": "string",
                    "description": "Output directory for generated UXML/USS files (default: 'Assets/UI/Generated').",
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
                "layout": {
                    "type": "string",
                    "enum": ["horizontal", "vertical", "grid"],
                    "description": "Layout type.",
                },
                "spacing": {
                    "type": "number",
                    "description": "Spacing between items.",
                },
                "items": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "id": {"type": "string", "description": "Item ID."},
                            "label": {"type": "string", "description": "Item display label."},
                            "iconPath": {"type": "string", "description": "Icon asset path."},
                            "enabled": {"type": "boolean", "description": "Item enabled state."},
                            "defaultSelected": {"type": "boolean", "description": "Default selected."},
                            "associatedPanelPath": {"type": "string", "description": "GameObject path of associated panel for tab type."},
                        },
                    },
                    "description": "Selection items for setItems.",
                },
                "item": {
                    "type": "object",
                    "properties": {
                        "id": {"type": "string"},
                        "label": {"type": "string"},
                        "iconPath": {"type": "string"},
                        "enabled": {"type": "boolean"},
                        "defaultSelected": {"type": "boolean"},
                        "associatedPanelPath": {"type": "string"},
                    },
                    "description": "Item data for addItem.",
                },
                "index": {
                    "type": "integer",
                    "description": "Item index.",
                },
                "itemId": {
                    "type": "string",
                    "description": "Item ID.",
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
                            "selectedId": {"type": "string", "description": "Selection ID that triggers this action."},
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
        ["operation"],
    )


def gamekit_pool_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_pool MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "findByPoolId",
                    ],
                    "description": "Object pool operation to perform.",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
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
            },
        },
        ["operation"],
    )


def gamekit_data_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_data MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "createEventChannel",
                        "createDataContainer",
                        "createRuntimeSet",
                        "inspect",
                        "delete",
                        "findByDataId",
                    ],
                    "description": "Data architecture operation to perform.",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path (for listener attachment).",
                },
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
                "scriptOutputDir": {
                    "type": "string",
                    "description": "Output directory for generated scripts (default: 'Assets/Scripts/Generated').",
                },
            },
        },
        ["operation"],
    )
