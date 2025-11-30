# Example 2: UI Menu Creation

**Goal**: Create a complete main menu UI with buttons, title, and panel.

**Difficulty**: Beginner
**Time**: 10 minutes

## Prerequisites

- Unity Editor 2021.3 or higher
- MCP Bridge running
- Basic understanding of Unity UI (Canvas, RectTransform)

## What You'll Create

- A Canvas with proper settings
- A semi-transparent background panel
- A title text
- Three menu buttons (Play, Settings, Quit)
- Proper layout with spacing

## Step-by-Step Guide

### 1. Set Up UI Scene

Create a new scene with UI components:

```python
unity_scene_quickSetup({
    "setupType": "UI"
})
```

This creates:
- Canvas (with Canvas Scaler for responsive design)
- EventSystem (for handling UI input)

### 2. Create Background Panel

Add a semi-transparent panel as the background:

```python
unity_ugui_createFromTemplate({
    "template": "Panel",
    "name": "MenuPanel",
    "parentPath": "Canvas",
    "width": 400,
    "height": 600
})
```

### 3. Update Panel Color

Make the panel semi-transparent dark:

```python
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Canvas/MenuPanel",
    "componentType": "UnityEngine.UI.Image",
    "propertyChanges": {
        "color": {
            "r": 0.0,
            "g": 0.0,
            "b": 0.0,
            "a": 0.8
        }
    }
})
```

### 4. Add Title Text

Create the title at the top of the panel:

```python
unity_ugui_createFromTemplate({
    "template": "Text",
    "name": "TitleText",
    "parentPath": "Canvas/MenuPanel",
    "text": "Main Menu",
    "fontSize": 48,
    "width": 350,
    "height": 80,
    "anchorPreset": "top-center"
})
```

### 5. Position the Title

Adjust the title position:

```python
unity_ugui_manage({
    "operation": "updateRect",
    "gameObjectPath": "Canvas/MenuPanel/TitleText",
    "anchoredPositionX": 0,
    "anchoredPositionY": -50
})
```

### 6. Create Button Container

Add a vertical layout group for organizing buttons:

```python
unity_gameobject_crud({
    "operation": "create",
    "name": "ButtonContainer",
    "parentPath": "Canvas/MenuPanel"
})

unity_ugui_layoutManage({
    "operation": "add",
    "gameObjectPath": "Canvas/MenuPanel/ButtonContainer",
    "layoutType": "VerticalLayoutGroup",
    "spacing": 20,
    "padding": {
        "left": 50,
        "right": 50,
        "top": 150,
        "bottom": 50
    },
    "childControlWidth": True,
    "childControlHeight": False,
    "childForceExpandWidth": True
})
```

### 7. Create Menu Buttons

Add three buttons with proper styling:

```python
# Play Button
unity_ugui_createFromTemplate({
    "template": "Button",
    "name": "PlayButton",
    "parentPath": "Canvas/MenuPanel/ButtonContainer",
    "text": "Play",
    "width": 300,
    "height": 60
})

# Settings Button
unity_ugui_createFromTemplate({
    "template": "Button",
    "name": "SettingsButton",
    "parentPath": "Canvas/MenuPanel/ButtonContainer",
    "text": "Settings",
    "width": 300,
    "height": 60
})

# Quit Button
unity_ugui_createFromTemplate({
    "template": "Button",
    "name": "QuitButton",
    "parentPath": "Canvas/MenuPanel/ButtonContainer",
    "text": "Quit",
    "width": 300,
    "height": 60
})
```

### 8. Update Button Text Sizes

Make button text more readable:

```python
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Canvas/MenuPanel/ButtonContainer/PlayButton/Text",
    "componentType": "UnityEngine.UI.Text",
    "propertyChanges": {
        "fontSize": 24,
        "alignment": "MiddleCenter"
    }
})

unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Canvas/MenuPanel/ButtonContainer/SettingsButton/Text",
    "componentType": "UnityEngine.UI.Text",
    "propertyChanges": {
        "fontSize": 24,
        "alignment": "MiddleCenter"
    }
})

unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Canvas/MenuPanel/ButtonContainer/QuitButton/Text",
    "componentType": "UnityEngine.UI.Text",
    "propertyChanges": {
        "fontSize": 24,
        "alignment": "MiddleCenter"
    }
})
```

### 9. Center the Panel

Ensure the panel is centered on screen:

```python
unity_ugui_manage({
    "operation": "setAnchorPreset",
    "gameObjectPath": "Canvas/MenuPanel",
    "preset": "center"
})
```

## Expected Result

Your UI hierarchy should look like:

```
Canvas
└── MenuPanel
    ├── TitleText
    └── ButtonContainer
        ├── PlayButton
        │   └── Text
        ├── SettingsButton
        │   └── Text
        └── QuitButton
            └── Text
```

In Game view, you should see:
- A centered dark panel (400x600)
- "Main Menu" title at the top
- Three evenly-spaced buttons below
- Buttons expand to fill the width

## Next Steps - Add Functionality

### Option 1: Add Click Events (Manual)

In Unity, you can manually add click events:
1. Select a button
2. In Inspector, find the Button component
3. Click + in the OnClick section
4. Drag your script GameObject
5. Select the method to call

### Option 2: Create a Menu Manager Script

Create a script to handle button clicks:

```python
# Create MenuManager script using asset_crud
unity_asset_crud({
    "operation": "create",
    "assetPath": "Assets/Scripts/MenuManager.cs",
    "content": '''using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void OnPlayClicked()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnSettingsClicked()
    {
        Debug.Log("Settings clicked");
    }

    public void OnQuitClicked()
    {
        Application.Quit();
    }
}
'''
})
```

## Common Issues

**Issue**: Buttons don't respond to clicks
- **Solution**: Make sure EventSystem exists in the hierarchy

**Issue**: UI is too small/large on different screens
- **Solution**: Canvas Scaler is already configured for responsive design

**Issue**: Text is blurry
- **Solution**: Increase Canvas Scaler reference resolution or use TextMeshPro

## Enhancements

Try these improvements:

1. **Add Hover Effects**: Use button transitions in Inspector
2. **Add Icons**: Import sprites and add to buttons
3. **Animate Transitions**: Use Unity's Animation system
4. **Add Sound Effects**: Attach AudioSource to buttons

## Related Examples

- [01-basic-scene-setup.md](01-basic-scene-setup.md) - Add this UI to a game scene
- [04-prefab-workflow.md](04-prefab-workflow.md) - Turn this menu into a reusable prefab
