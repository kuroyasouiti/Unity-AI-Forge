"""Schema definitions for visual and presentation MCP tools."""

from __future__ import annotations

from typing import Any

from tools.schemas.common import schema_with_required


def sprite2d_bundle_schema() -> dict[str, Any]:
    """Schema for the unity_sprite2d_bundle MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "createSprite",
                        "updateSprite",
                        "inspect",
                        "updateMultiple",
                        "setSortingLayer",
                        "setColor",
                        "sliceSpriteSheet",
                        "createSpriteAtlas",
                    ],
                    "description": "Sprite2D bundle operation.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                "gameObjectGlobalObjectId": {
                    "type": "string",
                    "description": "Target GameObject GlobalObjectId.",
                },
                "gameObjectPaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Multiple target GameObject paths for batch operations.",
                },
                "pattern": {"type": "string", "description": "Pattern for matching GameObjects."},
                "useRegex": {"type": "boolean", "description": "Use regex pattern matching."},
                "maxResults": {
                    "type": "integer",
                    "description": "Maximum results for batch operations.",
                },
                "name": {"type": "string", "description": "Name for new sprite GameObject."},
                "parentPath": {"type": "string", "description": "Parent GameObject path."},
                "spritePath": {"type": "string", "description": "Sprite asset path."},
                "sortingLayerName": {
                    "type": "string",
                    "description": "Sorting layer name (default: 'Default').",
                },
                "sortingOrder": {"type": "integer", "description": "Sorting order within layer."},
                "color": {
                    "type": "object",
                    "properties": {
                        "r": {"type": "number"},
                        "g": {"type": "number"},
                        "b": {"type": "number"},
                        "a": {"type": "number"},
                    },
                    "description": "Sprite color (RGBA 0-1).",
                },
                "flipX": {"type": "boolean", "description": "Flip sprite horizontally."},
                "flipY": {"type": "boolean", "description": "Flip sprite vertically."},
                "drawMode": {
                    "type": "string",
                    "enum": ["simple", "sliced", "tiled"],
                    "description": "Sprite draw mode.",
                },
                "size": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                    "description": "Sprite size (for sliced/tiled modes).",
                },
                "maskInteraction": {
                    "type": "string",
                    "enum": ["none", "visibleInsideMask", "visibleOutsideMask"],
                    "description": "Sprite mask interaction mode.",
                },
                "materialPath": {"type": "string", "description": "Material asset path."},
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Initial position for new sprite.",
                },
                "texturePath": {"type": "string", "description": "Texture path for slicing."},
                "sliceMode": {
                    "type": "string",
                    "enum": ["grid", "automatic"],
                    "description": "Sprite sheet slicing mode.",
                },
                "cellSizeX": {"type": "integer", "description": "Grid cell width in pixels."},
                "cellSizeY": {"type": "integer", "description": "Grid cell height in pixels."},
                "pivot": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                    "description": "Sprite pivot point (0-1).",
                },
                "pixelsPerUnit": {
                    "type": "number",
                    "description": "Pixels per unit for sprite import.",
                },
                "atlasPath": {"type": "string", "description": "Output path for sprite atlas."},
                "spritePaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Sprite paths to include in atlas.",
                },
            },
        },
        ["operation"],
    )


def animation2d_bundle_schema() -> dict[str, Any]:
    """Schema for the unity_animation2d_bundle MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "setupAnimator",
                        "updateAnimator",
                        "inspectAnimator",
                        "createController",
                        "addState",
                        "addTransition",
                        "addParameter",
                        "inspectController",
                        "createClipFromSprites",
                        "updateClip",
                        "inspectClip",
                    ],
                    "description": "Animation2D bundle operation.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                "gameObjectGlobalObjectId": {
                    "type": "string",
                    "description": "Target GameObject GlobalObjectId.",
                },
                "controllerPath": {
                    "type": "string",
                    "description": "AnimatorController asset path.",
                },
                "clipPath": {"type": "string", "description": "AnimationClip asset path."},
                "applyRootMotion": {"type": "boolean", "description": "Enable root motion."},
                "updateMode": {
                    "type": "string",
                    "enum": ["Normal", "AnimatePhysics", "UnscaledTime"],
                    "description": "Animator update mode.",
                },
                "cullingMode": {
                    "type": "string",
                    "enum": ["AlwaysAnimate", "CullCompletely", "CullUpdateTransforms"],
                    "description": "Animator culling mode.",
                },
                "speed": {"type": "number", "description": "Animator playback speed."},
                "parameters": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "type": {"type": "string", "enum": ["Bool", "Float", "Int", "Trigger"]},
                        },
                    },
                    "description": "Animator parameters to add.",
                },
                "stateName": {"type": "string", "description": "Animation state name."},
                "layerIndex": {
                    "type": "integer",
                    "description": "Animator layer index (default: 0).",
                },
                "isDefault": {"type": "boolean", "description": "Set as default state."},
                "fromState": {
                    "type": "string",
                    "description": "Source state for transition ('Any' for AnyState).",
                },
                "toState": {"type": "string", "description": "Destination state for transition."},
                "hasExitTime": {"type": "boolean", "description": "Transition has exit time."},
                "exitTime": {"type": "number", "description": "Exit time (0-1)."},
                "duration": {"type": "number", "description": "Transition duration in seconds."},
                "conditions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "parameter": {"type": "string"},
                            "mode": {
                                "type": "string",
                                "enum": ["If", "IfNot", "Greater", "Less", "Equals", "NotEqual"],
                            },
                            "threshold": {"type": "number"},
                        },
                    },
                    "description": "Transition conditions.",
                },
                "parameterName": {"type": "string", "description": "Parameter name."},
                "parameterType": {
                    "type": "string",
                    "enum": ["Bool", "Float", "Int", "Trigger"],
                    "description": "Parameter type.",
                },
                "spritePaths": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Sprite paths for animation clip creation.",
                },
                "frameRate": {
                    "type": "number",
                    "description": "Animation frame rate (default: 12).",
                },
                "loop": {"type": "boolean", "description": "Loop animation."},
            },
        },
        ["operation"],
    )


def material_bundle_schema() -> dict[str, Any]:
    """Schema for the unity_material_bundle MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "setTexture",
                        "setColor",
                        "applyPreset",
                        "inspect",
                        "applyToObjects",
                        "delete",
                        "duplicate",
                        "listPresets",
                    ],
                    "description": "Material bundle operation.",
                },
                "name": {"type": "string", "description": "Material name."},
                "savePath": {
                    "type": "string",
                    "description": "Save path for material asset (e.g., 'Assets/Materials/MyMat.mat').",
                },
                "materialPath": {"type": "string", "description": "Existing material asset path."},
                "preset": {
                    "type": "string",
                    "enum": [
                        "unlit",
                        "lit",
                        "transparent",
                        "cutout",
                        "fade",
                        "sprite",
                        "ui",
                        "emissive",
                        "metallic",
                        "glass",
                    ],
                    "description": "Material preset to apply.",
                },
                "shader": {
                    "type": "string",
                    "description": "Shader name override (e.g., 'Standard', 'Universal Render Pipeline/Lit').",
                },
                "color": {
                    "type": "string",
                    "description": "Main color (hex format: '#RRGGBB' or '#RRGGBBAA').",
                },
                "metallic": {"type": "number", "description": "Metallic value (0-1)."},
                "smoothness": {"type": "number", "description": "Smoothness value (0-1)."},
                "emission": {"type": "boolean", "description": "Enable emission."},
                "emissionColor": {"type": "string", "description": "Emission color (hex format)."},
                "emissionIntensity": {"type": "number", "description": "Emission intensity."},
                "texturePath": {"type": "string", "description": "Texture asset path."},
                "textureProperty": {
                    "type": "string",
                    "description": "Texture property name (e.g., '_MainTex', '_BumpMap').",
                },
                "tiling": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                    "description": "Texture tiling.",
                },
                "offset": {
                    "type": "object",
                    "properties": {"x": {"type": "number"}, "y": {"type": "number"}},
                    "description": "Texture offset.",
                },
                "pattern": {
                    "type": "string",
                    "description": "Pattern for applyToObjects (e.g., 'Cube*').",
                },
                "targetMaterialPath": {
                    "type": "string",
                    "description": "Target path for duplicate operation.",
                },
            },
        },
        ["operation"],
    )


def light_bundle_schema() -> dict[str, Any]:
    """Schema for the unity_light_bundle MCP tool."""
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
                        "applyPreset",
                        "createLightingSetup",
                        "listPresets",
                    ],
                    "description": "Light bundle operation.",
                },
                "name": {"type": "string", "description": "Light GameObject name."},
                "gameObjectPath": {
                    "type": "string",
                    "description": "Existing light GameObject path.",
                },
                "lightType": {
                    "type": "string",
                    "enum": ["Directional", "Point", "Spot", "Area"],
                    "description": "Light type.",
                },
                "preset": {
                    "type": "string",
                    "enum": [
                        "daylight",
                        "moonlight",
                        "warm",
                        "cool",
                        "spotlight",
                        "candle",
                        "neon",
                    ],
                    "description": "Light preset to apply.",
                },
                "setupPreset": {
                    "type": "string",
                    "enum": ["daylight", "nighttime", "indoor", "dramatic", "studio", "sunset"],
                    "description": "Complete lighting setup preset.",
                },
                "color": {"type": "string", "description": "Light color (hex format)."},
                "intensity": {"type": "number", "description": "Light intensity."},
                "range": {"type": "number", "description": "Light range (for Point/Spot)."},
                "spotAngle": {"type": "number", "description": "Spot light angle."},
                "shadows": {
                    "type": "string",
                    "enum": ["none", "hard", "soft"],
                    "description": "Shadow type.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Light position.",
                },
                "rotation": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Light rotation.",
                },
                "renderMode": {
                    "type": "string",
                    "enum": ["Auto", "ForcePixel", "ForceVertex"],
                    "description": "Light render mode.",
                },
                "bounceIntensity": {
                    "type": "number",
                    "description": "Bounce intensity for indirect lighting.",
                },
            },
        },
        ["operation"],
    )


def particle_bundle_schema() -> dict[str, Any]:
    """Schema for the unity_particle_bundle MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "applyPreset",
                        "play",
                        "stop",
                        "pause",
                        "inspect",
                        "delete",
                        "duplicate",
                        "listPresets",
                    ],
                    "description": "Particle bundle operation.",
                },
                "name": {"type": "string", "description": "ParticleSystem GameObject name."},
                "gameObjectPath": {
                    "type": "string",
                    "description": "Existing ParticleSystem GameObject path.",
                },
                "preset": {
                    "type": "string",
                    "enum": [
                        "explosion",
                        "fire",
                        "smoke",
                        "sparkle",
                        "rain",
                        "snow",
                        "dust",
                        "trail",
                        "hit",
                        "heal",
                        "magic",
                        "leaves",
                    ],
                    "description": "Particle preset to apply.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Particle system position.",
                },
                "startSize": {"type": "number", "description": "Start size."},
                "startLifetime": {"type": "number", "description": "Start lifetime."},
                "startSpeed": {"type": "number", "description": "Start speed."},
                "startColor": {"type": "string", "description": "Start color (hex format)."},
                "maxParticles": {"type": "integer", "description": "Maximum particles."},
                "emissionRate": {"type": "number", "description": "Emission rate over time."},
                "duration": {"type": "number", "description": "System duration."},
                "loop": {"type": "boolean", "description": "Loop playback."},
                "playOnAwake": {"type": "boolean", "description": "Play on awake."},
                "simulationSpace": {
                    "type": "string",
                    "enum": ["Local", "World", "Custom"],
                    "description": "Simulation space.",
                },
                "gravity": {"type": "number", "description": "Gravity modifier."},
                "shape": {
                    "type": "string",
                    "enum": ["Sphere", "Hemisphere", "Cone", "Box", "Circle", "Edge"],
                    "description": "Emission shape.",
                },
                "shapeRadius": {"type": "number", "description": "Shape radius."},
                "shapeAngle": {"type": "number", "description": "Shape angle (for Cone)."},
                "targetPath": {
                    "type": "string",
                    "description": "Target path for duplicate operation.",
                },
            },
        },
        ["operation"],
    )


def animation3d_bundle_schema() -> dict[str, Any]:
    """Schema for the unity_animation3d_bundle MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "setupAnimator",
                        "createController",
                        "addState",
                        "addTransition",
                        "setParameter",
                        "addBlendTree",
                        "createAvatarMask",
                        "inspect",
                        "delete",
                        "listParameters",
                        "listStates",
                    ],
                    "description": "Animation3D bundle operation.",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "Target GameObject for setupAnimator.",
                },
                "controllerPath": {
                    "type": "string",
                    "description": "AnimatorController asset path.",
                },
                "name": {"type": "string", "description": "Name for new controller/mask."},
                "savePath": {"type": "string", "description": "Save path for asset."},
                "applyRootMotion": {"type": "boolean", "description": "Apply root motion."},
                "updateMode": {
                    "type": "string",
                    "enum": ["Normal", "AnimatePhysics", "UnscaledTime"],
                    "description": "Animator update mode.",
                },
                "cullingMode": {
                    "type": "string",
                    "enum": ["AlwaysAnimate", "CullCompletely", "CullUpdateTransforms"],
                    "description": "Animator culling mode.",
                },
                "parameters": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "type": {"type": "string", "enum": ["float", "int", "bool", "trigger"]},
                            "defaultValue": {},
                        },
                    },
                    "description": "Animator parameters.",
                },
                "states": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "clip": {"type": "string"},
                            "isDefault": {"type": "boolean"},
                            "speed": {"type": "number"},
                        },
                    },
                    "description": "Animation states.",
                },
                "transitions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "from": {"type": "string"},
                            "to": {"type": "string"},
                            "hasExitTime": {"type": "boolean"},
                            "exitTime": {"type": "number"},
                            "duration": {"type": "number"},
                            "conditions": {"type": "array"},
                        },
                    },
                    "description": "State transitions.",
                },
                "stateName": {"type": "string", "description": "State name."},
                "clipPath": {"type": "string", "description": "Animation clip path."},
                "layerIndex": {"type": "integer", "description": "Layer index (default: 0)."},
                "isDefault": {"type": "boolean", "description": "Set as default state."},
                "speed": {"type": "number", "description": "State playback speed."},
                "fromState": {
                    "type": "string",
                    "description": "Source state for transition ('Any' for AnyState).",
                },
                "toState": {"type": "string", "description": "Destination state for transition."},
                "hasExitTime": {"type": "boolean", "description": "Transition has exit time."},
                "exitTime": {"type": "number", "description": "Exit time (0-1)."},
                "duration": {"type": "number", "description": "Transition duration."},
                "conditions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "param": {"type": "string"},
                            "mode": {
                                "type": "string",
                                "enum": ["if", "ifnot", "greater", "less", "equals", "notequal"],
                            },
                            "value": {"type": "number"},
                        },
                    },
                    "description": "Transition conditions.",
                },
                "parameterName": {"type": "string", "description": "Parameter name."},
                "parameterType": {
                    "type": "string",
                    "enum": ["float", "int", "bool", "trigger"],
                    "description": "Parameter type.",
                },
                "defaultValue": {"description": "Default parameter value."},
                "blendTreeName": {"type": "string", "description": "BlendTree name."},
                "blendType": {
                    "type": "string",
                    "enum": [
                        "Simple1D",
                        "SimpleDirectional2D",
                        "FreeformDirectional2D",
                        "FreeformCartesian2D",
                    ],
                    "description": "BlendTree type.",
                },
                "blendParameter": {"type": "string", "description": "Blend parameter name."},
                "blendParameterY": {"type": "string", "description": "Blend parameter Y (for 2D)."},
                "motions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "clip": {"type": "string"},
                            "threshold": {"type": "number"},
                            "positionX": {"type": "number"},
                            "positionY": {"type": "number"},
                        },
                    },
                    "description": "BlendTree motions.",
                },
                "enabledParts": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Enabled body parts for AvatarMask.",
                },
                "disabledParts": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Disabled body parts for AvatarMask.",
                },
            },
        },
        ["operation"],
    )


def event_wiring_schema() -> dict[str, Any]:
    """Schema for the unity_event_wiring MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "wire",
                        "unwire",
                        "inspect",
                        "listEvents",
                        "clearEvent",
                        "wireMultiple",
                    ],
                    "description": "Event wiring operation.",
                },
                "source": {
                    "type": "object",
                    "properties": {
                        "gameObject": {"type": "string", "description": "Source GameObject path."},
                        "component": {
                            "type": "string",
                            "description": "Source component type (e.g., 'Button', 'UnityEngine.UI.Button').",
                        },
                        "event": {
                            "type": "string",
                            "description": "Event name (e.g., 'onClick', 'm_OnClick').",
                        },
                    },
                    "description": "Event source.",
                },
                "target": {
                    "type": "object",
                    "properties": {
                        "gameObject": {"type": "string", "description": "Target GameObject path."},
                        "component": {
                            "type": "string",
                            "description": "Target component type (optional, defaults to searching GameObject).",
                        },
                        "method": {"type": "string", "description": "Target method name."},
                        "mode": {
                            "type": "string",
                            "enum": ["Void", "Int", "Float", "String", "Bool", "Object"],
                            "description": "Argument mode.",
                        },
                        "argument": {"description": "Argument value (type depends on mode)."},
                    },
                    "description": "Event target.",
                },
                "gameObjectPath": {"type": "string", "description": "GameObject for listEvents."},
                "componentType": {
                    "type": "string",
                    "description": "Component type for listEvents (optional).",
                },
                "targetGameObject": {
                    "type": "string",
                    "description": "Target GameObject for unwire filtering.",
                },
                "targetMethod": {
                    "type": "string",
                    "description": "Target method for unwire filtering.",
                },
                "listenerIndex": {
                    "type": "integer",
                    "description": "Specific listener index for unwire.",
                },
                "wirings": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "source": {"type": "object"},
                            "target": {"type": "object"},
                        },
                    },
                    "description": "Multiple wirings for wireMultiple.",
                },
            },
        },
        ["operation"],
    )
