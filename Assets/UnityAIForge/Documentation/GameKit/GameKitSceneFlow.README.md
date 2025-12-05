# GameKitSceneFlow - Scene-Centric State Machine with Auto-Loading

## Overview

`GameKitSceneFlow` manages scene transitions with a scene-centric state machine. Each scene defines its own transitions, allowing the same trigger to lead to different destinations depending on which scene is currently active. This is perfect for paginated interfaces, level progressions, and narrative-driven games.

**✨ NEW**: SceneFlow prefabs are now automatically loaded at startup! No need to manually place them in your initial scene.

## Key Concept: Scene-Centric Transitions

Unlike traditional global transition systems, `GameKitSceneFlow` integrates transitions directly into scene definitions. This means:

- **Same trigger, different destinations**: "nextPage" from Page1 goes to Page2, from Page2 goes to Page3
- **Context-aware navigation**: Current scene determines available transitions
- **Clear scene dependencies**: Each scene knows its own exit points

## Features

- **🎯 Automatic Loading**: Prefabs in `Resources/GameKitSceneFlows/` are loaded automatically
- **Scene-Centric Design**: Transitions defined per scene, not globally
- **Prefab-Based**: Configuration saved as prefabs for version control
- **Additive Scene Loading**: Support for both single and additive scene modes
- **Shared Scene Paths**: Each scene defines its own shared scenes (e.g., UI, Audio) directly
- **Smart Scene Management**: Only reload shared scenes when needed
- **Static API**: Trigger transitions from anywhere with `GameKitSceneFlow.Transition()`

## Automatic Loading System

GameKitSceneFlow now uses a prefab-based approach with automatic loading:

1. **Create via MCP Tool**: SceneFlow is saved as a prefab in `Resources/GameKitSceneFlows/`
2. **Automatic Loading**: 
   - **Play Mode**: `GameKitSceneFlowAutoLoader` (Editor) loads prefabs when entering play mode
   - **Runtime**: `GameKitSceneFlowRuntimeLoader` loads prefabs before first scene loads
3. **Persistent**: Prefabs are instantiated with `DontDestroyOnLoad()`
4. **Version Control**: Prefabs can be committed to Git for team collaboration

No need to manually place GameObjects in your initial scene!

## Basic Setup (MCP Tool - Recommended)

### 1. Create SceneFlow Prefab

```python
# Creates prefab at Resources/GameKitSceneFlows/MainGameFlow.prefab
unity_gamekit_sceneflow({
    "operation": "create",
    "flowId": "MainGameFlow"
})
```

### 2. Add Scenes One by One

```python
# Add main menu
unity_gamekit_sceneflow({
    "operation": "addScene",
    "flowId": "MainGameFlow",
    "sceneName": "MainMenu",
    "scenePath": "Assets/Scenes/MainMenu.unity",
    "loadMode": "single"
})

# Add Level1 with shared scenes
unity_gamekit_sceneflow({
    "operation": "addScene",
    "flowId": "MainGameFlow",
    "sceneName": "Level1",
    "scenePath": "Assets/Scenes/Level1.unity",
    "loadMode": "additive",
    "sharedScenePaths": [
        "Assets/Scenes/Shared/GameUI.unity",
        "Assets/Scenes/Shared/AudioManager.unity"
    ]
})

# Add Level2
unity_gamekit_sceneflow({
    "operation": "addScene",
    "flowId": "MainGameFlow",
    "sceneName": "Level2",
    "scenePath": "Assets/Scenes/Level2.unity",
    "loadMode": "additive",
    "sharedScenePaths": [
        "Assets/Scenes/Shared/GameUI.unity",
        "Assets/Scenes/Shared/AudioManager.unity"
    ]
})
```

### 3. Add Scene-Specific Transitions

```python
# Each scene defines its own transitions
unity_gamekit_sceneflow({
    "operation": "addTransition",
    "flowId": "MainGameFlow",
    "fromScene": "MainMenu",
    "trigger": "startGame",
    "toScene": "Level1"
})

unity_gamekit_sceneflow({
    "operation": "addTransition",
    "flowId": "MainGameFlow",
    "fromScene": "Level1",
    "trigger": "complete",
    "toScene": "Level2"
})

unity_gamekit_sceneflow({
    "operation": "addTransition",
    "flowId": "MainGameFlow",
    "fromScene": "Level1",
    "trigger": "mainMenu",
    "toScene": "MainMenu"
})
```

### 4. Start Play Mode

The prefab is automatically loaded! Trigger transitions from your scripts:

```csharp
// From UI buttons or game logic
GameKitSceneFlow.Transition("startGame");
GameKitSceneFlow.Transition("complete");
```

## Manual C# Setup (Advanced)

If you need to create SceneFlow programmatically:

```csharp
// This approach is less common now that prefabs auto-load
GameObject managerGo = new GameObject("SceneFlowManager");
GameKitSceneFlow sceneFlow = managerGo.AddComponent<GameKitSceneFlow>();
sceneFlow.Initialize("mainFlow");

// Add scenes and transitions
sceneFlow.AddScene("MainMenu", "Assets/Scenes/MainMenu.unity", 
    GameKitSceneFlow.SceneLoadMode.Single);
sceneFlow.AddTransition("MainMenu", "startGame", "Level1");

// Trigger transitions
sceneFlow.TriggerTransition("startGame");
```

## Common Patterns

### Paginated Book/Tutorial

```csharp
// Setup page scenes with shared UI scene
sceneFlow.AddScene("Page1", "Assets/Scenes/Tutorial/Page1.unity", SceneLoadMode.Single, 
    new string[] { "Assets/Scenes/Shared/TutorialUI.unity" });
sceneFlow.AddScene("Page2", "Assets/Scenes/Tutorial/Page2.unity", SceneLoadMode.Single, 
    new string[] { "Assets/Scenes/Shared/TutorialUI.unity" });
sceneFlow.AddScene("Page3", "Assets/Scenes/Tutorial/Page3.unity", SceneLoadMode.Single, 
    new string[] { "Assets/Scenes/Shared/TutorialUI.unity" });

// Same trigger "nextPage" goes to different destinations
sceneFlow.AddTransition("Page1", "nextPage", "Page2");
sceneFlow.AddTransition("Page2", "nextPage", "Page3");
sceneFlow.AddTransition("Page3", "nextPage", "Finished");

// Same trigger "prevPage" for going back
sceneFlow.AddTransition("Page2", "prevPage", "Page1");
sceneFlow.AddTransition("Page3", "prevPage", "Page2");

// Usage in UI
public void OnNextButtonClick()
{
    GameKitSceneFlow.Transition("nextPage"); // Destination depends on current page
}

public void OnPrevButtonClick()
{
    GameKitSceneFlow.Transition("prevPage");
}
```

### Level Progression

```csharp
// Setup levels with shared UI and Audio scenes
for (int i = 1; i <= 10; i++)
{
    string sceneName = $"Level{i}";
    string scenePath = $"Assets/Scenes/Levels/Level{i}.unity";
    sceneFlow.AddScene(sceneName, scenePath, SceneLoadMode.Additive, 
        new string[] { 
            "Assets/Scenes/Shared/GameUI.unity", 
            "Assets/Scenes/Shared/AudioManager.unity" 
        });
    
    // Each level can advance to next or return to hub
    if (i < 10)
    {
        sceneFlow.AddTransition(sceneName, "complete", $"Level{i + 1}");
    }
    sceneFlow.AddTransition(sceneName, "returnToHub", "LevelSelect");
}

// Level completion
public void OnLevelComplete()
{
    GameKitSceneFlow.Transition("complete"); // Goes to next level
}

public void OnQuit()
{
    GameKitSceneFlow.Transition("returnToHub"); // Returns to hub from any level
}
```

### Dialogue/Story System

```csharp
// Story scenes with branching, all sharing the same UI scene
sceneFlow.AddScene("Chapter1", "Assets/Scenes/Story/Chapter1.unity", SceneLoadMode.Single, 
    new string[] { "Assets/Scenes/Shared/StoryUI.unity" });
sceneFlow.AddScene("Chapter2A", "Assets/Scenes/Story/Chapter2A.unity", SceneLoadMode.Single, 
    new string[] { "Assets/Scenes/Shared/StoryUI.unity" });
sceneFlow.AddScene("Chapter2B", "Assets/Scenes/Story/Chapter2B.unity", SceneLoadMode.Single, 
    new string[] { "Assets/Scenes/Shared/StoryUI.unity" });

// Branching choices
sceneFlow.AddTransition("Chapter1", "choiceA", "Chapter2A");
sceneFlow.AddTransition("Chapter1", "choiceB", "Chapter2B");

// Both branches continue to same ending
sceneFlow.AddTransition("Chapter2A", "continue", "Chapter3");
sceneFlow.AddTransition("Chapter2B", "continue", "Chapter3");

// Usage
public void OnPlayerChoice(string choice)
{
    GameKitSceneFlow.Transition(choice);
}
```

### Mini-Game Integration

```csharp
// Main game and mini-games with different UI scenes
sceneFlow.AddScene("MainGame", "Assets/Scenes/MainGame.unity", SceneLoadMode.Single, 
    new string[] { "Assets/Scenes/Shared/MainUI.unity" });
sceneFlow.AddScene("Puzzle1", "Assets/Scenes/MiniGames/Puzzle1.unity", SceneLoadMode.Additive, 
    new string[] { "Assets/Scenes/Shared/MiniGameUI.unity" });
sceneFlow.AddScene("Puzzle2", "Assets/Scenes/MiniGames/Puzzle2.unity", SceneLoadMode.Additive, 
    new string[] { "Assets/Scenes/Shared/MiniGameUI.unity" });

// Return to main game with different triggers
sceneFlow.AddTransition("Puzzle1", "complete", "MainGame");
sceneFlow.AddTransition("Puzzle1", "failed", "MainGame");
sceneFlow.AddTransition("Puzzle2", "complete", "MainGame");
sceneFlow.AddTransition("Puzzle2", "failed", "MainGame");

// Enter mini-games
sceneFlow.AddTransition("MainGame", "enterPuzzle1", "Puzzle1");
sceneFlow.AddTransition("MainGame", "enterPuzzle2", "Puzzle2");
```

### Menu Navigation

```csharp
// Main menu system with shared menu UI
sceneFlow.AddScene("MainMenu", "Assets/Scenes/UI/MainMenu.unity", SceneLoadMode.Single, 
    new string[] { "Assets/Scenes/Shared/MenuUI.unity" });
sceneFlow.AddScene("Settings", "Assets/Scenes/UI/Settings.unity", SceneLoadMode.Single, 
    new string[] { "Assets/Scenes/Shared/MenuUI.unity" });
sceneFlow.AddScene("Credits", "Assets/Scenes/UI/Credits.unity", SceneLoadMode.Single, 
    new string[] { "Assets/Scenes/Shared/MenuUI.unity" });

// All scenes can return to main menu with same trigger
sceneFlow.AddTransition("MainMenu", "settings", "Settings");
sceneFlow.AddTransition("MainMenu", "credits", "Credits");

sceneFlow.AddTransition("Settings", "back", "MainMenu");
sceneFlow.AddTransition("Credits", "back", "MainMenu");

// UI button handler
public void OnBackButton()
{
    GameKitSceneFlow.Transition("back"); // Works from any sub-menu
}
```

## Advanced Features

### Dynamic Available Triggers

Get available triggers for current scene (useful for UI):

```csharp
List<string> triggers = sceneFlow.GetAvailableTriggers();
foreach (string trigger in triggers)
{
    Debug.Log($"Available: {trigger}");
    // Create UI button for each trigger
}
```

### Scene List

Get all defined scenes:

```csharp
List<string> scenes = sceneFlow.GetSceneNames();
Debug.Log($"Total scenes: {scenes.Count}");
```

### Shared Scene Optimization

Shared scenes are intelligently managed:
- Only loaded when needed
- Not reloaded if already present
- Unloaded when no longer needed

```csharp
// These share the same UI scene path
string gameUIPath = "Assets/Scenes/Shared/GameUI.unity";
sceneFlow.AddScene("Level1", "...", SceneLoadMode.Additive, new string[] { gameUIPath });
sceneFlow.AddScene("Level2", "...", SceneLoadMode.Additive, new string[] { gameUIPath });

// GameUI stays loaded when transitioning Level1 → Level2
// Only reloaded if transitioning to a scene without that path
```

## MCP Tool Operations

### Create SceneFlow

Initialize a new scene flow manager:

```python
unity_gamekit_sceneflow({
    "operation": "create",
    "flowId": "MainGameFlow"
})
```

### Add Scene (Granular Operation)

Add individual scenes one at a time:

```python
# Add main menu
unity_gamekit_sceneflow({
    "operation": "addScene",
    "flowId": "MainGameFlow",
    "sceneName": "MainMenu",
    "scenePath": "Assets/Scenes/MainMenu.unity",
    "loadMode": "single"
})

# Add level with shared scenes
unity_gamekit_sceneflow({
    "operation": "addScene",
    "flowId": "MainGameFlow",
    "sceneName": "Level1",
    "scenePath": "Assets/Scenes/Level1.unity",
    "loadMode": "additive",
    "sharedScenePaths": [
        "Assets/Scenes/Shared/GameUI.unity",
        "Assets/Scenes/Shared/AudioManager.unity"
    ]
})
```

### Remove Scene

```python
unity_gamekit_sceneflow({
    "operation": "removeScene",
    "flowId": "MainGameFlow",
    "sceneName": "Level1"
})
```

### Update Scene

Update scene properties (only specified properties are changed):

```python
unity_gamekit_sceneflow({
    "operation": "updateScene",
    "flowId": "MainGameFlow",
    "sceneName": "Level1",
    "loadMode": "single",
    "sharedScenePaths": ["Assets/Scenes/Shared/NewUI.unity"]
})
```

### Add Transition (Granular Operation)

Add individual transitions one at a time:

```python
unity_gamekit_sceneflow({
    "operation": "addTransition",
    "flowId": "MainGameFlow",
    "fromScene": "MainMenu",
    "trigger": "startGame",
    "toScene": "Level1"
})

unity_gamekit_sceneflow({
    "operation": "addTransition",
    "flowId": "MainGameFlow",
    "fromScene": "Level1",
    "trigger": "complete",
    "toScene": "Level2"
})
```

### Remove Transition

```python
unity_gamekit_sceneflow({
    "operation": "removeTransition",
    "flowId": "MainGameFlow",
    "fromScene": "MainMenu",
    "trigger": "startGame"
})
```

### Add Shared Scene

Add a shared scene to an existing scene definition:

```python
unity_gamekit_sceneflow({
    "operation": "addSharedScene",
    "flowId": "MainGameFlow",
    "sceneName": "Level1",
    "sharedScenePath": "Assets/Scenes/Shared/NewFeature.unity"
})
```

### Remove Shared Scene

```python
unity_gamekit_sceneflow({
    "operation": "removeSharedScene",
    "flowId": "MainGameFlow",
    "sceneName": "Level1",
    "sharedScenePath": "Assets/Scenes/Shared/OldFeature.unity"
})
```

### Inspect SceneFlow

```python
unity_gamekit_sceneflow({
    "operation": "inspect",
    "flowId": "MainGameFlow"
})
```

### Delete SceneFlow

```python
unity_gamekit_sceneflow({
    "operation": "delete",
    "flowId": "MainGameFlow"
})
```

### Complete MCP Example

Building a complete scene flow step by step:

```python
# 1. Create flow
unity_gamekit_sceneflow({
    "operation": "create",
    "flowId": "MainGameFlow"
})

# 2. Add scenes one by one
scenes = [
    {"name": "MainMenu", "path": "Assets/Scenes/MainMenu.unity", "mode": "single"},
    {"name": "Level1", "path": "Assets/Scenes/Level1.unity", "mode": "additive"},
    {"name": "Level2", "path": "Assets/Scenes/Level2.unity", "mode": "additive"},
    {"name": "Victory", "path": "Assets/Scenes/Victory.unity", "mode": "single"}
]

for scene in scenes:
    unity_gamekit_sceneflow({
        "operation": "addScene",
        "flowId": "MainGameFlow",
        "sceneName": scene["name"],
        "scenePath": scene["path"],
        "loadMode": scene["mode"],
        "sharedScenePaths": [
            "Assets/Scenes/Shared/GameUI.unity",
            "Assets/Scenes/Shared/AudioManager.unity"
        ] if scene["mode"] == "additive" else []
    })

# 3. Add transitions one by one
transitions = [
    {"from": "MainMenu", "trigger": "startGame", "to": "Level1"},
    {"from": "Level1", "trigger": "complete", "to": "Level2"},
    {"from": "Level1", "trigger": "quit", "to": "MainMenu"},
    {"from": "Level2", "trigger": "complete", "to": "Victory"},
    {"from": "Level2", "trigger": "quit", "to": "MainMenu"},
    {"from": "Victory", "trigger": "mainMenu", "to": "MainMenu"}
]

for trans in transitions:
    unity_gamekit_sceneflow({
        "operation": "addTransition",
        "flowId": "MainGameFlow",
        "fromScene": trans["from"],
        "trigger": trans["trigger"],
        "toScene": trans["to"]
    })
```

## C# API Reference

### Scene Management

- `void AddScene(string name, string scenePath, SceneLoadMode loadMode, string[] sharedScenePaths = null)` - Define a scene with optional shared scene paths
- `bool RemoveScene(string name)` - Remove a scene definition
- `bool UpdateScene(string name, string scenePath = null, SceneLoadMode? loadMode = null, string[] sharedScenePaths = null)` - Update scene properties
- `void AddSharedScenesToScene(string sceneName, string[] sharedScenePaths)` - Add shared scenes to existing scene definition
- `bool RemoveSharedSceneFromScene(string sceneName, string sharedScenePath)` - Remove a shared scene from a scene definition
- `void SetCurrentScene(string sceneName)` - Set current scene without loading

### Transitions

- `void AddTransition(string fromScene, string trigger, string toScene)` - Add transition from specific scene
- `bool RemoveTransition(string fromSceneName, string trigger)` - Remove a transition
- `void TriggerTransition(string trigger)` - Trigger transition from current scene
- `static void Transition(string trigger)` - Static method to trigger from anywhere

### Queries

- `List<string> GetAvailableTriggers()` - Get triggers available from current scene
- `List<string> GetSceneNames()` - Get all scene names
- `string CurrentScene` - Get current scene name
- `string FlowId` - Get flow identifier

### Enums

```csharp
public enum SceneLoadMode
{
    Single,    // Replace all scenes
    Additive   // Add to existing scenes
}
```

## Prefab-Based Approach Benefits

### Why Prefabs?

1. **No Manual Placement**: No need to add GameObjects to your initial scene
2. **Version Control Friendly**: Prefabs can be committed to Git
3. **Team Collaboration**: Team members share the same SceneFlow configuration
4. **Automatic Loading**: Works in both Editor and builds without setup
5. **Persistent**: Survives scene transitions automatically
6. **Inspector Editable**: Can view/edit prefabs directly in Project window

### How It Works

```
1. MCP Tool creates prefab → Resources/GameKitSceneFlows/MainGameFlow.prefab
2. Unity Editor starts or Play Mode begins
3. GameKitSceneFlowAutoLoader (Editor) or RuntimeLoader loads all prefabs
4. Prefabs instantiated with DontDestroyOnLoad()
5. SceneFlow ready to use from any scene
```

### Directory Structure

```
Assets/
  Resources/
    GameKitSceneFlows/
      MainGameFlow.prefab       # Your SceneFlow prefabs
      TutorialFlow.prefab
      BossRushFlow.prefab
```

### Creating Directory Manually

If needed, you can create the directory manually:

**Unity Editor Menu**:
```
Tools → Unity-AI-Forge → GameKit → Create SceneFlows Directory
```

**Or via MCP**:
Just create a SceneFlow - the directory is created automatically!

## Best Practices

### 1. Scene Naming

Use clear, hierarchical names:
```csharp
// Good
"MainMenu"
"Level1_Forest"
"Level2_Cave"
"UI_Settings"
"UI_Credits"

// Avoid
"Scene1"
"Untitled"
"Test"
```

### 2. Trigger Naming

Use consistent, semantic trigger names:
```csharp
// Good - Context-independent
"next", "previous", "back", "complete", "failed", "start", "quit"

// Avoid - Too specific
"goToLevel2", "openSettingsMenu", "clickNextButton"
```

### 3. Shared Scene Paths

Define shared scenes directly in scene definitions:
```csharp
// Define common shared scenes as constants for reuse
const string HUD_PATH = "Assets/Scenes/Shared/HUD.unity";
const string MINIMAP_PATH = "Assets/Scenes/Shared/Minimap.unity";
const string AUDIO_PATH = "Assets/Scenes/Shared/AudioManager.unity";
const string MUSIC_PATH = "Assets/Scenes/Shared/MusicController.unity";

// Use in scene definitions
sceneFlow.AddScene("Level1", "...", SceneLoadMode.Additive, 
    new string[] { HUD_PATH, MINIMAP_PATH, AUDIO_PATH, MUSIC_PATH });
```

### 4. Scene Load Modes

- Use `Single` for distinct scenes (menus, levels that don't overlap)
- Use `Additive` for content scenes with shared persistent scenes

### 5. Initialization

Initialize in a persistent manager scene:
```csharp
void Awake()
{
    // SceneFlow persists across scene loads
    sceneFlow = GetComponent<GameKitSceneFlow>();
    sceneFlow.Initialize("mainFlow");
    SetupScenes();
    SetupTransitions();
    sceneFlow.SetCurrentScene(SceneManager.GetActiveScene().name);
}
```

## Troubleshooting

**"No transition found" warning:**
- Ensure transition is added FROM the current scene
- Check current scene is set: `sceneFlow.SetCurrentScene()`
- Verify trigger name matches exactly

**"Scene not found" error:**
- Verify scene path is correct and scene exists
- Check scene is added to Build Settings
- Ensure scene name matches definition

**Shared scenes not loading:**
- Check group name matches in `AddScene` and `AddSharedGroup`
- Verify shared scene paths are correct
- Ensure shared scenes are in Build Settings

**Transitions from wrong scene:**
- Remember: transitions are per-scene, not global
- Use `GetAvailableTriggers()` to debug which triggers are available

## Migration from Global Transitions

If migrating from a global transition system:

**Old way (global):**
```csharp
transitions.Add("StartGame", "Title", "Level1");
transitions.Add("StartGame", "Level1", "Level2"); // Conflict!
```

**New way (scene-centric):**
```csharp
sceneFlow.AddTransition("Title", "StartGame", "Level1");
sceneFlow.AddTransition("Level1", "StartGame", "Level2"); // No conflict!
```

## See Also

- [GameKit Actor](./README.md#gamekit-actor)
- [GameKit Manager](./README.md#gamekit-manager)
- [GameKit Interaction](./README.md#gamekit-interaction)

