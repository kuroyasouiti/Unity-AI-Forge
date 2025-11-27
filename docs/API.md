# SkillForUnity API Documentation

Complete reference for all MCP tools available in SkillForUnity.

> This Claude Skill lives under `SkillForUnity` (project root) and is bundled with the Unity package as `Assets/SkillForUnity/SkillForUnity.zip`.

---

## Table of Contents

1. [Ping Tool](#ping-tool)
2. [Scene Manage](#scene-manage)
3. [GameObject Manage](#gameobject-manage)
4. [Component Manage](#component-manage)
5. [Asset Manage](#asset-manage)
6. [ScriptableObject Manage](#scriptableobject-manage)
7. [uGUI RectTransform Adjustment](#ugui-recttransform-adjustment)
8. [Script Management](#script-management)
9. [Error Handling](#error-handling)
10. [Best Practices](#best-practices)

---

## Ping Tool

### `pingUnityEditor`

Verifies Unity Editor connectivity and retrieves basic project information.

**Parameters**: None

**Returns**:
```json
{
  "editor": "6000.2.6f2",
  "project": "MCP",
  "time": 1761001784781
}
```

**Field Descriptions**:
- `editor` (string): Unity Editor version
- `project` (string): Project name from Application.productName
- `time` (number): Current UTC timestamp in milliseconds

**Example Usage** (from Claude Code):
```
Please ping Unity to verify connection
```

**Error Conditions**: None (always succeeds if bridge is connected)

---

## Scene Manage

### `sceneManage`

Performs create, load, save, delete, and duplicate operations on Unity scenes.

### Operations

#### Create Scene

Creates a new Unity scene at the specified path.

**Payload**:
```json
{
  "operation": "create",
  "scenePath": "Assets/Scenes/NewLevel.unity",
  "additive": false,
  "includeOpenScenes": false
}
```

**Parameters**:
- `operation` (string, required): `"create"`
- `scenePath` (string, required): Asset path where scene will be created
- `additive` (boolean, optional): If `true`, loads scene additively without closing existing scenes. Default: `false`
- `includeOpenScenes` (boolean, optional): If `true`, includes currently open scenes when creating. Default: `false`

**Returns**:
```json
{
  "path": "Assets/Scenes/NewLevel.unity",
  "name": "NewLevel"
}
```

**Example**:
```
Create a new scene at Assets/Scenes/Level2.unity
```

---

#### Load Scene

Loads an existing scene into the Unity Editor.

**Payload**:
```json
{
  "operation": "load",
  "scenePath": "Assets/Scenes/MainMenu.unity",
  "additive": true
}
```

**Parameters**:
- `operation` (string, required): `"load"`
- `scenePath` (string, required): Path to the scene to load
- `additive` (boolean, optional): If `true`, loads scene without unloading current scenes. Default: `false`

**Returns**:
```json
{
  "path": "Assets/Scenes/MainMenu.unity",
  "name": "MainMenu",
  "mode": "additive"
}
```

**Example**:
```
Load the MainMenu scene additively
```

---

#### Save Scene

Saves the active scene or all open scenes.

**Payload**:
```json
{
  "operation": "save",
  "scenePath": "Assets/Scenes/Level1.unity"
}
```

**Parameters**:
- `operation` (string, required): `"save"`
- `scenePath` (string, optional): Specific scene path to save. If omitted, saves all modified scenes

**Returns**:
```json
{
  "savedScenes": [
    "Assets/Scenes/Level1.unity",
    "Assets/Scenes/UI.unity"
  ]
}
```

**Example**:
```
Save the current scene
```

---

#### Delete Scene

Deletes a scene file from the project.

**Payload**:
```json
{
  "operation": "delete",
  "scenePath": "Assets/Scenes/OldLevel.unity"
}
```

**Parameters**:
- `operation` (string, required): `"delete"`
- `scenePath` (string, required): Path to the scene to delete

**Returns**:
```json
{
  "deleted": "Assets/Scenes/OldLevel.unity"
}
```

**Warning**: This permanently deletes the scene file. Cannot be undone via Unity's undo system.

**Example**:
```
Delete the scene at Assets/Scenes/TestScene.unity
```

---

#### Duplicate Scene

Creates a copy of an existing scene.

**Payload**:
```json
{
  "operation": "duplicate",
  "scenePath": "Assets/Scenes/Level1.unity",
  "newSceneName": "Level1_Copy"
}
```

**Parameters**:
- `operation` (string, required): `"duplicate"`
- `scenePath` (string, required): Path to the scene to duplicate
- `newSceneName` (string, optional): Name for the duplicated scene. If omitted, appends "_copy" to original name

**Returns**:
```json
{
  "source": "Assets/Scenes/Level1.unity",
  "destination": "Assets/Scenes/Level1_Copy.unity"
}
```

**Example**:
```
Duplicate Level1 scene as Level2
```

---

## GameObject Manage

### `gameObjectManage`

Manipulates GameObjects in the active scene hierarchy.

### Operations

#### Create GameObject

Creates a new GameObject in the scene hierarchy.

**Payload**:
```json
{
  "operation": "create",
  "name": "Player",
  "parentPath": "GameObjects/Characters",
  "template": "Assets/Prefabs/PlayerPrefab.prefab",
  "payload": {
    "position": [0, 1, 0],
    "rotation": [0, 90, 0]
  }
}
```

**Parameters**:
- `operation` (string, required): `"create"`
- `name` (string, optional): Name for the new GameObject. If using template, defaults to prefab name
- `parentPath` (string, optional): Hierarchy path to parent GameObject. Uses `/` as separator
- `template` (string, optional): Asset path to prefab to instantiate
- `payload` (object, optional): Additional properties (position, rotation, scale as arrays)

**Returns**:
```json
{
  "path": "GameObjects/Characters/Player",
  "name": "Player",
  "id": 12345
}
```

**Field Descriptions**:
- `path` (string): Full hierarchy path to the created GameObject
- `name` (string): GameObject name
- `id` (number): Unity instance ID

**Examples**:
```
Create an empty GameObject called "SpawnPoint"
Create a player GameObject from the prefab at Assets/Prefabs/Player.prefab
```

---

#### Delete GameObject

Removes a GameObject from the scene.

**Payload**:
```json
{
  "operation": "delete",
  "gameObjectPath": "Environment/Trees/Oak_01"
}
```

**Parameters**:
- `operation` (string, required): `"delete"`
- `gameObjectPath` (string, required): Hierarchy path to the GameObject

**Returns**:
```json
{
  "deleted": "Environment/Trees/Oak_01"
}
```

**Example**:
```
Delete the GameObject at Canvas/Panel/Button
```

---

#### Move GameObject

Changes a GameObject's parent in the hierarchy.

**Payload**:
```json
{
  "operation": "move",
  "gameObjectPath": "UI/HealthBar",
  "parentPath": "Canvas/HUD"
}
```

**Parameters**:
- `operation` (string, required): `"move"`
- `gameObjectPath` (string, required): Current hierarchy path
- `parentPath` (string, optional): New parent path. If `null` or empty, moves to scene root

**Returns**:
```json
{
  "path": "Canvas/HUD/HealthBar",
  "previousPath": "UI/HealthBar"
}
```

**Example**:
```
Move the Player GameObject under the Characters parent
```

---

#### Rename GameObject

Changes a GameObject's name.

**Payload**:
```json
{
  "operation": "rename",
  "gameObjectPath": "Cube",
  "name": "Platform"
}
```

**Parameters**:
- `operation` (string, required): `"rename"`
- `gameObjectPath` (string, required): Current hierarchy path
- `name` (string, required): New name

**Returns**:
```json
{
  "path": "Platform",
  "name": "Platform",
  "previousName": "Cube"
}
```

**Example**:
```
Rename the Main Camera to PlayerCamera
```

---

#### Duplicate GameObject

Creates a copy of a GameObject and its children.

**Payload**:
```json
{
  "operation": "duplicate",
  "gameObjectPath": "Enemies/Zombie"
}
```

**Parameters**:
- `operation` (string, required): `"duplicate"`
- `gameObjectPath` (string, required): Hierarchy path to GameObject to duplicate

**Returns**:
```json
{
  "path": "Enemies/Zombie (1)",
  "name": "Zombie (1)",
  "id": 67890
}
```

**Note**: Unity automatically appends `(1)`, `(2)`, etc. to duplicate names.

**Example**:
```
Duplicate the enemy GameObject
```

---

## Component Manage

### `componentManage`

Adds, removes, updates, or inspects components on GameObjects.

### Operations

#### Add Component

Adds a new component to a GameObject.

**Payload**:
```json
{
  "operation": "add",
  "gameObjectPath": "Player",
  "componentType": "UnityEngine.Rigidbody",
  "applyDefaults": true,
  "propertyChanges": {
    "mass": 70,
    "drag": 0.5,
    "useGravity": true
  }
}
```

**Parameters**:
- `operation` (string, required): `"add"`
- `gameObjectPath` (string, required): Hierarchy path to target GameObject
- `componentType` (string, required): Fully qualified type name (namespace + class)
- `applyDefaults` (boolean, optional): If `true`, applies default Unity values. Default: `true`
- `propertyChanges` (object, optional): Properties to set immediately after adding component

**Returns**:
```json
{
  "componentType": "UnityEngine.Rigidbody",
  "gameObjectPath": "Player"
}
```

**Common Component Types**:
- `UnityEngine.Rigidbody`
- `UnityEngine.BoxCollider`
- `UnityEngine.MeshRenderer`
- `UnityEngine.AudioSource`
- `UnityEngine.UI.Text`
- `UnityEngine.UI.Image`
- `UnityEngine.UI.Button`

**Example**:
```
Add a BoxCollider component to the Platform GameObject
Add a Rigidbody to Player with mass 80
```

---

#### Remove Component

Removes a component from a GameObject.

**Payload**:
```json
{
  "operation": "remove",
  "gameObjectPath": "Cube",
  "componentType": "UnityEngine.BoxCollider"
}
```

**Parameters**:
- `operation` (string, required): `"remove"`
- `gameObjectPath` (string, required): Hierarchy path to target GameObject
- `componentType` (string, required): Fully qualified type name

**Returns**:
```json
{
  "removed": "UnityEngine.BoxCollider",
  "gameObjectPath": "Cube"
}
```

**Note**: Cannot remove Transform component (Unity restriction).

**Example**:
```
Remove the Rigidbody from the Player
```

---

#### Update Component

Modifies properties of an existing component using reflection.

**Payload**:
```json
{
  "operation": "update",
  "gameObjectPath": "DirectionalLight",
  "componentType": "UnityEngine.Light",
  "propertyChanges": {
    "intensity": 1.5,
    "color": {
      "r": 1.0,
      "g": 0.95,
      "b": 0.8,
      "a": 1.0
    },
    "shadowType": "Soft"
  }
}
```

**Parameters**:
- `operation` (string, required): `"update"`
- `gameObjectPath` (string, required): Hierarchy path to target GameObject
- `componentType` (string, required): Fully qualified type name
- `propertyChanges` (object, required): Dictionary of property names and values

**Supported Property Types**:
- Primitives: `int`, `float`, `double`, `bool`, `string`
- Unity types: `Vector2`, `Vector3`, `Vector4`, `Color`, `Quaternion`
- Enums: Specify as string (e.g., `"Soft"` for ShadowType)
- Arrays: Specify as JSON arrays
- **Unity Object References**: GameObject, Component, and Asset references (see below)

**Vector/Color Format**:
```json
{
  "position": [1.5, 2.0, 3.0],
  "color": {"r": 1, "g": 0, "b": 0, "a": 1}
}
```

**Unity Object Reference Format**:

You can now reference GameObjects, Components, and Assets using special dictionary syntax:

1. **GameObject Reference** (by hierarchy path):
```json
{
  "propertyChanges": {
    "target": {
      "_ref": "gameObject",
      "path": "Player/Camera"
    }
  }
}
```

2. **Component Reference** (by GameObject path and type):
```json
{
  "propertyChanges": {
    "light": {
      "_ref": "component",
      "path": "Environment/DirectionalLight",
      "type": "UnityEngine.Light"
    }
  }
}
```

3. **Asset Reference** (by asset path or GUID):
```json
{
  "propertyChanges": {
    "material": {
      "_ref": "asset",
      "path": "Assets/Materials/PlayerMat.mat"
    },
    "prefab": {
      "_ref": "asset",
      "guid": "abc123def456"
    }
  }
}
```

4. **Instance Reference** (by Unity instance ID):
```json
{
  "propertyChanges": {
    "targetObject": {
      "_ref": "instance",
      "id": 12345
    }
  }
}
```

**Returns**:
```json
{
  "updated": "UnityEngine.Light",
  "gameObjectPath": "DirectionalLight",
  "properties": ["intensity", "color", "shadowType"]
}
```

**Examples**:
```
Set the Player's Rigidbody mass to 100
Change the Main Camera's field of view to 75
Update the UI Text component text to "Hello World" and fontSize to 24
```

**Advanced Example - Setting Object References**:

Assign a target GameObject to a follow camera script:
```json
{
  "operation": "update",
  "gameObjectPath": "Main Camera",
  "componentType": "FollowCamera",
  "propertyChanges": {
    "target": {
      "_ref": "gameObject",
      "path": "Player"
    },
    "offset": {"x": 0, "y": 2, "z": -5}
  }
}
```

Set a Material reference on a Renderer:
```json
{
  "operation": "update",
  "gameObjectPath": "Player/Body",
  "componentType": "UnityEngine.MeshRenderer",
  "propertyChanges": {
    "material": {
      "_ref": "asset",
      "path": "Assets/Materials/PlayerMaterial.mat"
    }
  }
}
```

Configure a Light to follow another Light's settings:
```json
{
  "operation": "update",
  "gameObjectPath": "Spotlight",
  "componentType": "UnityEngine.Light",
  "propertyChanges": {
    "cookie": {
      "_ref": "asset",
      "path": "Assets/Textures/SpotlightCookie.png"
    }
  }
}
```

#### Inspect Component

Returns the current state of a component without modifying it.

**Payload**:
```json
{
  "operation": "inspect",
  "gameObjectPath": "Player",
  "componentType": "UnityEngine.Light"
}
```

**Parameters**:
- `operation` (string, required): `"inspect"`
- `gameObjectPath` (string, required): Hierarchy path to target GameObject
- `componentType` (string, required): Fully qualified type name

**Returns**:
```json
{
  "gameObject": "Player",
  "type": "UnityEngine.Light",
  "properties": {
    "enabled": true,
    "intensity": 1.2,
    "range": 15.0
  }
}
```

**Notes**:
- Only public fields and properties with getters are serialized
- Collections larger than 100 entries are truncated
- Unity object references include name, type, instanceId, and assetPath (when available)

---

## Asset Manage

### `assetManage`

Creates, updates, deletes, duplicates, renames, or inspects asset files in the Unity project.

### Operations

#### Create Asset

Creates a new asset file with specified content.

**IMPORTANT**: C# script files (.cs) cannot be created using this tool. Use the following instead:
- `designPatternGenerate` for design pattern implementations
- `scriptTemplateGenerate` for MonoBehaviour/ScriptableObject templates
- Code editor tools (write, search_replace) for custom scripts

**Payload**:
```json
{
  "operation": "create",
  "assetPath": "Assets/Config/settings.json",
  "content": "{\"volume\": 1.0, \"difficulty\": \"normal\"}",
}
```

**Parameters**:
- `operation` (string, required): `"create"`
- `assetPath` (string, required): Full asset path including filename
- `content` (string, required): File contents
- `assetGuid` (string, optional): Asset GUID for precise identification

**Returns**:
```json
{
  "created": "Assets/Config/settings.json",
  "guid": "abc123...",
  "size": 42
}
```

**Supported File Types**:
- Data: `.json`, `.xml`, `.txt`, `.md`, `.yaml`, `.csv`
- Configuration: `.ini`, `.cfg`, `.conf`
- Shaders: `.shader`, `.cginc`, `.hlsl`
- Other text files (NOT .cs files)

**Example**:
```
Create a new JSON config file at Assets/Config/GameSettings.json with default settings
```

---

#### Update Asset

Modifies the contents of an existing asset file.

**IMPORTANT**: C# script files (.cs) cannot be updated using this tool. Use code editor tools (search_replace, write) instead for proper syntax highlighting and IDE integration.

**Payload**:
```json
{
  "operation": "update",
  "assetPath": "Assets/Config/settings.json",
  "content": "{\"volume\": 0.8, \"difficulty\": \"hard\"}"
}
```

**Parameters**:
- `operation` (string, required): `"update"`
- `assetPath` (string, required): Path to existing asset
- `assetGuid` (string, optional): Asset GUID (alternative to assetPath)
- `content` (string, required): New file contents

**Returns**:
```json
{
  "updated": "Assets/Config/settings.json",
  "guid": "abc123...",
  "size": 45
}
```

**Example**:
```
Update the settings.json file with new configuration values
```

---

#### Delete Asset

Removes an asset file from the project.

**Payload**:
```json
{
  "operation": "delete",
  "assetPath": "Assets/Temp/debug.log"
}
```

**Parameters**:
- `operation` (string, required): `"delete"`
- `assetPath` (string, required): Path to asset to delete

**Returns**:
```json
{
  "deleted": "Assets/Temp/debug.log"
}
```

**Warning**: Permanent deletion. Unity meta files are also removed.

**Example**:
```
Delete the old script at Assets/Scripts/Deprecated/OldController.cs
```

---

#### Rename Asset

Renames an asset file.

**Payload**:
```json
{
  "operation": "rename",
  "assetPath": "Assets/Materials/Mat_Old.mat",
  "destinationPath": "Assets/Materials/Mat_New.mat"
}
```

**Parameters**:
- `operation` (string, required): `"rename"`
- `assetPath` (string, required): Current asset path
- `destinationPath` (string, required): New asset path (can include directory change)

**Returns**:
```json
{
  "source": "Assets/Materials/Mat_Old.mat",
  "destination": "Assets/Materials/Mat_New.mat"
}
```

**Note**: Meta file is automatically renamed by Unity.

**Example**:
```
Rename PlayerMaterial to HeroMaterial
```

---

#### Duplicate Asset

Creates a copy of an existing asset.

**Payload**:
```json
{
  "operation": "duplicate",
  "assetPath": "Assets/Prefabs/Enemy.prefab",
  "destinationPath": "Assets/Prefabs/EnemyStrong.prefab"
}
```

**Parameters**:
- `operation` (string, required): `"duplicate"`
- `assetPath` (string, required): Source asset path
- `destinationPath` (string, optional): Destination path. If omitted, appends "_copy" to filename

**Returns**:
```json
{
  "source": "Assets/Prefabs/Enemy.prefab",
  "destination": "Assets/Prefabs/EnemyStrong.prefab"
}
```

**Example**:
```
Duplicate the Button prefab as ButtonHover
```

#### Inspect Asset

Returns metadata and public fields/properties for an asset.

**Payload**:
```json
{
  "operation": "inspect",
  "assetPath": "Assets/Prefabs/Enemy.prefab"
}
```

**Parameters**:
- `operation` (string, required): `"inspect"`
- `assetPath` (string, required): Target asset path

**Returns**:
```json
{
  "path": "Assets/Prefabs/Enemy.prefab",
  "guid": "2f8e9b...",
  "type": "UnityEngine.GameObject",
  "exists": true,
  "properties": {
    "name": "Enemy",
    "layer": 0
  }
}
```

**Notes**:
- Non-text or complex assets may expose limited information
- Unity object references include asset paths when available
- Throws if the specified path cannot be resolved to an asset

---

## ScriptableObject Manage

### `scriptableObjectManage`

Creates, inspects, updates, deletes, duplicates, and finds ScriptableObject assets in the Unity project.

### Operations

#### Create ScriptableObject

Creates a new ScriptableObject asset of the specified type.

**Payload**:
```json
{
  "operation": "create",
  "typeName": "MyGame.PlayerData",
  "assetPath": "Assets/Data/PlayerData.asset",
  "properties": {
    "playerName": "Hero",
    "maxHealth": 100,
    "speed": 5.5
  }
}
```

**Parameters**:
- `operation` (string, required): `"create"`
- `typeName` (string, required): Fully qualified type name of the ScriptableObject to create
- `assetPath` (string, required): Asset path where the ScriptableObject will be created (must start with `Assets/` and end with `.asset`)
- `properties` (object, optional): Initial property values to set on the ScriptableObject

**Returns**:
```json
{
  "assetPath": "Assets/Data/PlayerData.asset",
  "guid": "abc123...",
  "typeName": "MyGame.PlayerData",
  "message": "ScriptableObject created successfully at Assets/Data/PlayerData.asset"
}
```

**Example**:
```
Create a new ScriptableObject of type GameConfig at Assets/Data/Config.asset with properties: maxPlayers = 4, gameMode = "Competitive"
```

**Error Conditions**:
- `typeName` not found or not a ScriptableObject type
- `assetPath` already exists
- `assetPath` doesn't start with `Assets/` or doesn't end with `.asset`

---

#### Inspect ScriptableObject

Retrieves detailed information about a ScriptableObject asset.

**Payload**:
```json
{
  "operation": "inspect",
  "assetPath": "Assets/Data/PlayerData.asset",
  "includeProperties": true,
  "propertyFilter": ["playerName", "maxHealth"]
}
```

**Parameters**:
- `operation` (string, required): `"inspect"`
- `assetPath` (string, optional): Asset path to the ScriptableObject
- `assetGuid` (string, optional): Alternative to `assetPath`, GUID of the asset
- `includeProperties` (boolean, optional): Whether to include property values. Default: `true`
- `propertyFilter` (array of strings, optional): List of property names to include. If omitted, all properties are included

**Returns**:
```json
{
  "assetPath": "Assets/Data/PlayerData.asset",
  "guid": "abc123...",
  "typeName": "MyGame.PlayerData",
  "name": "PlayerData",
  "properties": {
    "playerName": "Hero",
    "maxHealth": 100,
    "speed": 5.5
  }
}
```

**Example**:
```
Inspect the ScriptableObject at Assets/Data/Config.asset and show all properties
```

---

#### Update ScriptableObject

Updates property values on an existing ScriptableObject asset.

**Payload**:
```json
{
  "operation": "update",
  "assetPath": "Assets/Data/PlayerData.asset",
  "properties": {
    "maxHealth": 150,
    "speed": 6.0
  }
}
```

**Parameters**:
- `operation` (string, required): `"update"`
- `assetPath` (string, optional): Asset path to the ScriptableObject
- `assetGuid` (string, optional): Alternative to `assetPath`, GUID of the asset
- `properties` (object, required): Dictionary of property names to new values

**Returns**:
```json
{
  "assetPath": "Assets/Data/PlayerData.asset",
  "guid": "abc123...",
  "changedProperties": ["maxHealth", "speed"],
  "message": "ScriptableObject updated successfully"
}
```

**Example**:
```
Update the ScriptableObject at Assets/Data/Config.asset: set maxPlayers to 8 and gameMode to "Casual"
```

---

#### Delete ScriptableObject

Deletes a ScriptableObject asset from the project.

**Payload**:
```json
{
  "operation": "delete",
  "assetPath": "Assets/Data/OldPlayerData.asset"
}
```

**Parameters**:
- `operation` (string, required): `"delete"`
- `assetPath` (string, optional): Asset path to the ScriptableObject
- `assetGuid` (string, optional): Alternative to `assetPath`, GUID of the asset

**Returns**:
```json
{
  "assetPath": "Assets/Data/OldPlayerData.asset",
  "message": "ScriptableObject deleted successfully"
}
```

**Example**:
```
Delete the ScriptableObject at Assets/Data/OldConfig.asset
```

---

#### Duplicate ScriptableObject

Creates a copy of an existing ScriptableObject asset.

**Payload**:
```json
{
  "operation": "duplicate",
  "sourceAssetPath": "Assets/Data/PlayerData.asset",
  "destinationAssetPath": "Assets/Data/PlayerData_Copy.asset"
}
```

**Parameters**:
- `operation` (string, required): `"duplicate"`
- `sourceAssetPath` (string, optional): Source asset path
- `sourceAssetGuid` (string, optional): Alternative to `sourceAssetPath`, GUID of the source asset
- `destinationAssetPath` (string, required): Destination asset path (must start with `Assets/` and end with `.asset`)

**Returns**:
```json
{
  "sourceAssetPath": "Assets/Data/PlayerData.asset",
  "destinationAssetPath": "Assets/Data/PlayerData_Copy.asset",
  "guid": "def456...",
  "message": "ScriptableObject duplicated successfully"
}
```

**Example**:
```
Duplicate the ScriptableObject at Assets/Data/Config.asset to Assets/Data/Config_Backup.asset
```

---

#### List ScriptableObjects

Lists all ScriptableObject assets in the project or a specific folder with pagination support.

**Payload**:
```json
{
  "operation": "list",
  "searchPath": "Assets/Data",
  "typeName": "MyGame.PlayerData",
  "maxResults": 100,
  "offset": 0
}
```

**Parameters**:
- `operation` (string, required): `"list"`
- `searchPath` (string, optional): Folder path to search in. Default: `"Assets"`
- `typeName` (string, optional): Filter by type name (exact match on full name or short name)
- `maxResults` (integer, optional): Maximum number of results to return. Default: `1000`
- `offset` (integer, optional): Number of results to skip (for pagination). Default: `0`

**Returns**:
```json
{
  "count": 2,
  "totalFound": 150,
  "offset": 0,
  "maxResults": 100,
  "hasMore": true,
  "scriptableObjects": [
    {
      "assetPath": "Assets/Data/PlayerData1.asset",
      "guid": "abc123...",
      "name": "PlayerData1",
      "typeName": "MyGame.PlayerData"
    },
    {
      "assetPath": "Assets/Data/PlayerData2.asset",
      "guid": "def456...",
      "name": "PlayerData2",
      "typeName": "MyGame.PlayerData"
    }
  ],
  "searchPath": "Assets/Data"
}
```

**Example**:
```
List all ScriptableObjects in the Assets/Data folder (first 100 results)
```

**Pagination Example**:
```python
# Get first page
result = unity_scriptableobject_crud({
    "operation": "list",
    "searchPath": "Assets/Data",
    "maxResults": 100,
    "offset": 0
})

# Get second page if there are more results
if result["hasMore"]:
    next_result = unity_scriptableobject_crud({
        "operation": "list",
        "searchPath": "Assets/Data",
        "maxResults": 100,
        "offset": 100
    })
```

---

#### Find ScriptableObjects by Type

Finds all ScriptableObject assets of a specific type, including derived types, with pagination support.

**Payload**:
```json
{
  "operation": "findByType",
  "typeName": "MyGame.GameConfig",
  "searchPath": "Assets",
  "includeProperties": true,
  "propertyFilter": ["configName", "version"],
  "maxResults": 50,
  "offset": 0
}
```

**Parameters**:
- `operation` (string, required): `"findByType"`
- `typeName` (string, required): Type name to search for (finds exact matches and derived types)
- `searchPath` (string, optional): Folder path to search in. Default: `"Assets"`
- `includeProperties` (boolean, optional): Whether to include property values. Default: `false`
- `propertyFilter` (array of strings, optional): List of property names to include. If omitted, all properties are included (when includeProperties is true)
- `maxResults` (integer, optional): Maximum number of results to return. Default: `1000`
- `offset` (integer, optional): Number of results to skip (for pagination). Default: `0`

**Returns**:
```json
{
  "count": 2,
  "totalMatched": 25,
  "offset": 0,
  "maxResults": 50,
  "hasMore": false,
  "typeName": "MyGame.GameConfig",
  "scriptableObjects": [
    {
      "assetPath": "Assets/Config/DefaultConfig.asset",
      "guid": "abc123...",
      "name": "DefaultConfig",
      "typeName": "MyGame.GameConfig",
      "properties": {
        "configName": "Default",
        "version": "1.0"
      }
    }
  ],
  "searchPath": "Assets",
  "note": "Searching for abstract type MyGame.GameConfig. Results include all derived types."
}
```

**Example**:
```
Find all ScriptableObjects of type GameConfig in the project, including their properties
```

**Notes**:
- `findByType` supports polymorphism - it will find instances of the specified type and any derived types
- Use `includeProperties: true` to get detailed information about each ScriptableObject
- Use `propertyFilter` to limit which properties are returned for better performance
- When searching for abstract types, a note is included in the response
- Use pagination with `maxResults` and `offset` for large datasets

---

## uGUI RectTransform Adjustment

### `uguiRectAdjust`

Automatically adjusts RectTransform size for UI elements using Unity's layout utilities.

**Payload**:
```json
{
  "gameObjectPath": "Canvas/Panel/Text",
  "matchMode": "widthOrHeight",
  "referenceResolution": [1920, 1080]
}
```

**Parameters**:
- `gameObjectPath` (string, required): Hierarchy path to UI GameObject
- `matchMode` (string, optional): Sizing mode. Options: `"widthOrHeight"`, `"expand"`, `"shrink"`. Default: `"widthOrHeight"`
- `referenceResolution` (array, optional): `[width, height]` for CanvasScaler reference. Default: `[1920, 1080]`

**Returns**:
```json
{
  "gameObjectPath": "Canvas/Panel/Text",
  "adjusted": true,
  "mode": "widthOrHeight"
}
```

**Match Modes**:
- `widthOrHeight`: Matches reference resolution proportionally
- `expand`: Expands to fit content
- `shrink`: Shrinks to fit content

**Example**:
```
Adjust the RectTransform of Canvas/HUD/HealthBar to fit content
```

---

## Script Management

### `scriptManage`

Unified C# script helper that can read existing files, generate new templates, apply incremental edits, or delete script assets. Choose the desired behavior via the `operation` parameter.

#### Read Operation

**Payload**:
```json
{
  "operation": "read",
  "assetPath": "Assets/Scripts/PlayerController.cs",
  "guid": "abc123def456",
  "includeMembers": true,
  "includeSource": true
}
```

**Parameters**:
- `operation` = `"read"` (required; `"outline"` still accepted for legacy clients)
- `assetPath` (string, optional): Asset path to C# script (either this or `guid` required)
- `guid` (string, optional): Asset GUID (either this or `assetPath` required)
- `includeMembers` (boolean, optional): Include method signatures and fields. Default: `true`
- `includeSource` (boolean, optional): Include the raw script text in the response. Default: `true`

**Returns**:
```json
{
  "assetPath": "Assets/Scripts/PlayerController.cs",
  "syntaxOk": true,
  "outline": [
    {
      "kind": "type",
      "name": "PlayerController",
      "members": [
        {"kind": "method", "name": "Start", "signature": "public void Start() { ... }"},
        {"kind": "method", "name": "Update", "signature": "private void Update() { ... }"}
      ]
    }
  ],
  "source": "using UnityEngine;\\n\\npublic class PlayerController : MonoBehaviour { ... }"
}
```

#### Create Operation

**Payload**:
```json
{
  "operation": "create",
  "scriptPath": "Assets/Scripts/PlayerController.cs",
  "scriptType": "monoBehaviour",
  "namespace": "Gameplay.Characters",
  "methods": ["Awake", "Update"],
  "fields": [
    "float speed = 5f",
    {"name": "jumpForce", "type": "float", "visibility": "private", "serialize": true}
  ],
  "attributes": ["RequireComponent(typeof(Rigidbody))"],
  "interfaces": ["IPointerClickHandler"],
  "includeUsings": ["UnityEngine.EventSystems"]
}
```

**Parameters**:
- `operation` = `"create"` (required)
- `scriptPath` (string, required): Target `.cs` file path under `Assets/`
- `scriptType` (string, optional): Template type (`monoBehaviour`, `scriptableObject`, `editor`, `class`, `interface`, `struct`). Default: `monoBehaviour`
- `namespace` (string, optional): Namespace to wrap the class in
- `methods` (array, optional): Method names to stub (common Unity callbacks auto-generated)
- `fields` (array, optional): Fields as strings or structured objects with `name`, `type`, `visibility`, `serialize`, `defaultValue`
- `attributes` (array, optional): Class-level attributes
- `baseClass` (string, optional): Override base class
- `interfaces` (array, optional): Interfaces to implement
- `includeUsings` (array, optional): Extra `using` directives

**Returns**:
```json
{
  "scriptPath": "Assets/Scripts/PlayerController.cs",
  "className": "PlayerController",
  "scriptType": "monoBehaviour",
  "success": true
}
```

#### Update Operation

Applies ordered text edits to an existing script. Perfect for inserting logs, tweaking constants, or removing obsolete blocks without rewriting the whole file.

**Payload**:
```json
{
  "operation": "update",
  "scriptPath": "Assets/Scripts/PlayerController.cs",
  "dryRun": false,
  "edits": [
    {
      "action": "insertAfter",
      "match": "void Awake()",
      "text": "\n    CacheComponents();",
      "count": 1
    },
    {
      "action": "replace",
      "match": "float speed = 5f;",
      "replacement": "float speed = 7.5f;"
    },
    {
      "action": "delete",
      "match": "// TODO: remove debug logs",
      "allowMissingMatch": true
    }
  ]
}
```

**Edit Object Fields**:
- `action` (required): `"replace"`, `"insertBefore"`, `"insertAfter"`, or `"delete"`
- `match` (required): Text fragment to search for
- `replacement`: Replacement text (replace/delete actions). Empty string removes the match.
- `text`: Content to insert (insert actions)
- `count`: Max occurrences to modify (0 = all matches)
- `caseSensitive`: Defaults to `true`
- `allowMissingMatch`: Skip errors when match text is absent

**Returns**:
```json
{
  "scriptPath": "Assets/Scripts/PlayerController.cs",
  "changesMade": 3,
  "appliedEdits": [
    {"action": "insertAfter", "match": "void Awake()", "appliedCount": 1},
    {"action": "replace", "match": "float speed = 5f;", "appliedCount": 1},
    {"action": "delete", "match": "// TODO: remove debug logs", "appliedCount": 0}
  ]
}
```

Use `dryRun: true` to preview the full `previewSource` without writing to disk.

#### Delete Operation

Removes a script asset (with optional dry-run confirmation).

```json
{
  "operation": "delete",
  "scriptPath": "Assets/Scripts/Legacy/OldController.cs",
  "dryRun": true
}
```

**Returns**:
```json
{
  "scriptPath": "Assets/Scripts/Legacy/OldController.cs",
  "deleted": false,
  "dryRun": true
}
```

Toggle `dryRun` off to permanently delete via `AssetDatabase.DeleteAsset`.

**Use Cases**:
- Quickly inspect script structure or raw source before editing
- Generate boilerplate MonoBehaviours, ScriptableObjects, or plain C# types
- Standardize namespaces/imports for newly created gameplay scripts
- Combine read + create inside batch workflows via a single tool name

**Example**:
```
Show me the structure of the GameManager script (operation=read)
Create a ScriptableObject template for ItemDefinition (operation=create)
```

---

## Error Handling

All tools follow a consistent error response format:

**Error Response**:
```json
{
  "ok": false,
  "errorMessage": "GameObject not found: InvalidPath/Object",
  "type": "command:result",
  "commandId": "cmd-123"
}
```

**Common Error Types**:

### InvalidOperationException
- Invalid operation name
- Missing required parameters
- GameObject/Asset not found
- Component type not found

### ArgumentException
- Invalid asset path format
- Invalid hierarchy path
- Type conversion failure

### System.IO Exceptions
- File already exists (create without overwrite)
- File not found (update/delete)
- Permission denied

**Best Practices**:
1. Always check if GameObject/Asset exists before operations
2. Use try-catch in client code for operation failures
3. Validate paths before sending commands
4. Handle `"ok": false` responses gracefully

---

## Best Practices

### Performance

1. **Asset Index Caching**:
   - Asset index is cached for 30 seconds
   - Automatically refreshes in background
   - Manual invalidation via `McpContextCollector.InvalidateAssetIndexCache()` (C# only)

2. **Batch Operations**:
   ```json
   // Instead of multiple single operations:
   // DO: Create parent, then create children with parentPath
   {
     "operation": "create",
     "name": "Characters",
     "children": [
       {"name": "Player"},
       {"name": "Enemy"}
     ]
   }
   ```

3. **Hierarchy Paths**:
   - Use full paths: `"Environment/Trees/Oak_01"`
   - Avoid partial matches (ambiguous results)
   - Cache frequently-used paths

### Security

1. **Bridge Token**:
   ```bash
   # Use environment variable in CI/CD
   export MCP_BRIDGE_TOKEN="your-secure-token-here"
   ```

2. **Validate Input**:
   - Always validate file paths server-side
   - Sanitize user-provided content
   - Use `.gitignore` for settings files

### Debugging

1. **Unity Console**:
   - All operations log to Unity Console
   - Look for `[MCP]` prefix in messages
   - Check for red error messages

2. **Bridge Window**:
   - `Tools > MCP Assistant`
   - "Command Output" section shows execution history
   - Check "Diagnostics" for connection status

3. **Test Commands**:
   ```
   # Verify connection
   Please ping Unity

   # Check context
   What scene is currently loaded?

   # List GameObjects
   What GameObjects are in the scene?
   ```

### Common Patterns

**Create UI Button**:
```json
[
  {"operation": "create", "name": "Button", "parentPath": "Canvas"},
  {"operation": "add", "gameObjectPath": "Canvas/Button", "componentType": "UnityEngine.UI.Button"},
  {"operation": "add", "gameObjectPath": "Canvas/Button", "componentType": "UnityEngine.UI.Image"}
]
```

**Setup Player with Physics**:
```json
[
  {"operation": "create", "name": "Player", "template": "Assets/Prefabs/Character.prefab"},
  {"operation": "add", "gameObjectPath": "Player", "componentType": "UnityEngine.Rigidbody", "propertyChanges": {"mass": 70}},
  {"operation": "add", "gameObjectPath": "Player", "componentType": "UnityEngine.CapsuleCollider"}
]
```

---

## Version History

- **v1.0.0** (2025-10): Initial API documentation
  - All manage operations documented
  - Environment variable token support
  - Async asset indexing

---

## Support

For issues and questions:
- GitHub Issues: [SkillForUnity Repository]
- Unity Console: Check for `[MCP]` prefixed logs
- Bridge Window: `Tools > MCP Assistant > Command Output`

**API Version**: 1.0
**Last Updated**: 2025-10-21
