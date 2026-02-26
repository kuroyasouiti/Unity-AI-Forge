"""Schema definitions for GameKit 3-pillar MCP tools (UI, Logic, Presentation)."""

from __future__ import annotations

from typing import Any

from tools.schemas.common import schema_with_required


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


def gamekit_feedback_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_feedback MCP tool."""
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
                        "addComponent",
                        "clearComponents",
                        "setIntensity",
                        "findByFeedbackId",
                    ],
                    "description": "Feedback operation.",
                },
                "feedbackId": {"type": "string", "description": "Unique feedback identifier."},
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject path for feedback component.",
                },
                "playOnEnable": {
                    "type": "boolean",
                    "description": "Play feedback when GameObject becomes active.",
                },
                "globalIntensityMultiplier": {
                    "type": "number",
                    "description": "Global intensity multiplier (default: 1.0).",
                },
                "intensity": {
                    "type": "number",
                    "description": "Intensity value for setIntensity operation.",
                },
                "components": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {
                                "type": "string",
                                "enum": [
                                    "hitstop",
                                    "screenShake",
                                    "flash",
                                    "colorFlash",
                                    "scale",
                                    "position",
                                    "rotation",
                                    "sound",
                                    "particle",
                                    "haptic",
                                ],
                                "description": "Feedback component type.",
                            },
                            "delay": {"type": "number", "description": "Delay before effect."},
                            "duration": {"type": "number", "description": "Effect duration."},
                            "intensity": {"type": "number", "description": "Effect intensity."},
                            "hitstopTimeScale": {
                                "type": "number",
                                "description": "Time scale during hitstop (0 = frozen).",
                            },
                            "shakeFrequency": {
                                "type": "number",
                                "description": "Shake frequency in Hz.",
                            },
                            "color": {
                                "type": "object",
                                "properties": {
                                    "r": {"type": "number", "minimum": 0, "maximum": 1},
                                    "g": {"type": "number", "minimum": 0, "maximum": 1},
                                    "b": {"type": "number", "minimum": 0, "maximum": 1},
                                    "a": {"type": "number", "minimum": 0, "maximum": 1},
                                },
                                "description": "Flash color (RGBA 0-1).",
                            },
                            "fadeTime": {"type": "number", "description": "Flash fade time."},
                            "scaleAmount": {
                                "type": "object",
                                "properties": {
                                    "x": {"type": "number"},
                                    "y": {"type": "number"},
                                    "z": {"type": "number"},
                                },
                                "description": "Scale punch amount.",
                            },
                            "positionAmount": {
                                "type": "object",
                                "properties": {
                                    "x": {"type": "number"},
                                    "y": {"type": "number"},
                                    "z": {"type": "number"},
                                },
                                "description": "Position shake amount.",
                            },
                            "soundVolume": {"type": "number", "description": "Sound volume."},
                            "hapticIntensity": {
                                "type": "number",
                                "description": "Controller haptic intensity.",
                            },
                        },
                    },
                    "description": "Feedback components for create operation.",
                },
                "component": {
                    "type": "object",
                    "description": "Single component for addComponent operation.",
                },
            },
        },
        ["operation"],
    )


def gamekit_vfx_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_vfx MCP tool."""
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
                        "setMultipliers",
                        "setColor",
                        "setLoop",
                        "findByVFXId",
                    ],
                    "description": "VFX operation.",
                },
                "vfxId": {"type": "string", "description": "Unique VFX identifier."},
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject path for VFX component.",
                },
                "autoPlay": {
                    "type": "boolean",
                    "description": "Auto-play on enable (default: false).",
                },
                "loop": {"type": "boolean", "description": "Loop the effect."},
                "usePooling": {
                    "type": "boolean",
                    "description": "Enable object pooling (default: true).",
                },
                "poolSize": {"type": "integer", "minimum": 1, "description": "Pool size (default: 5)."},
                "attachToParent": {
                    "type": "boolean",
                    "description": "Attach to parent transform when playing.",
                },
                "durationMultiplier": {
                    "type": "number",
                    "description": "Duration multiplier (default: 1.0).",
                },
                "sizeMultiplier": {
                    "type": "number",
                    "description": "Size multiplier (default: 1.0).",
                },
                "emissionMultiplier": {
                    "type": "number",
                    "description": "Emission rate multiplier (default: 1.0).",
                },
                "particlePrefabPath": {
                    "type": "string",
                    "description": "Particle prefab asset path.",
                },
                "duration": {
                    "type": "number",
                    "description": "Duration for setMultipliers operation.",
                },
                "size": {
                    "type": "number",
                    "description": "Size for setMultipliers operation.",
                },
                "emission": {
                    "type": "number",
                    "description": "Emission for setMultipliers operation.",
                },
            },
        },
        ["operation"],
    )


def gamekit_audio_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_audio MCP tool."""
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
                        "setVolume",
                        "setPitch",
                        "setLoop",
                        "setClip",
                        "findByAudioId",
                    ],
                    "description": "Audio operation.",
                },
                "audioId": {"type": "string", "description": "Unique audio identifier."},
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject path for audio component.",
                },
                "audioType": {
                    "type": "string",
                    "enum": ["sfx", "music", "ambient", "voice", "ui"],
                    "description": "Audio type category.",
                },
                "audioClipPath": {
                    "type": "string",
                    "description": "Audio clip asset path.",
                },
                "playOnEnable": {
                    "type": "boolean",
                    "description": "Auto-play on enable.",
                },
                "loop": {"type": "boolean", "description": "Loop playback."},
                "volume": {"type": "number", "minimum": 0, "maximum": 1, "description": "Volume (0-1)."},
                "pitch": {"type": "number", "description": "Pitch (default: 1.0)."},
                "pitchVariation": {
                    "type": "number",
                    "description": "Random pitch variation (+/-).",
                },
                "spatialBlend": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 1,
                    "description": "2D/3D blend (0=2D, 1=3D).",
                },
                "fadeInDuration": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Fade in duration in seconds.",
                },
                "fadeOutDuration": {
                    "type": "number",
                    "minimum": 0,
                    "description": "Fade out duration in seconds.",
                },
                "minDistance": {
                    "type": "number",
                    "minimum": 0,
                    "description": "3D audio min distance.",
                },
                "maxDistance": {
                    "type": "number",
                    "minimum": 0,
                    "description": "3D audio max distance.",
                },
            },
        },
        ["operation"],
    )
