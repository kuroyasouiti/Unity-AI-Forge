# GameKitResourceManager

## Overview

`GameKitResourceManager` is a simple, lightweight resource storage system for managing game resources (health, mana, gold, etc.) with event notifications. It focuses on **resource amount storage** and **change events**, leaving complex logic to external controllers or Machinations Assets.

## 🎯 Design Philosophy

- **Single Responsibility**: Only manages resource amounts and constraints
- **Event-Driven**: Fires events when resources change
- **Asset Integration**: Can initialize from GameKitMachinationsAsset
- **External Logic**: Complex behaviors (flows, converters, triggers) are implemented externally

## Core Features

### 1. Resource Storage
Basic key-value storage for resource amounts with min/max constraints.

```csharp
manager.SetResource("health", 100f);
manager.SetResource("mana", 50f);
manager.SetResource("gold", 1000f);
```

### 2. Resource Operations

#### SetResource
Set resource to a specific value (clamped to min/max).

```csharp
resourceManager.SetResource("health", 100f);
```

#### GetResource
Get current resource amount.

```csharp
float health = resourceManager.GetResource("health", defaultValue: 0f);
```

#### AddResource
Increase resource amount (clamped to max).

```csharp
resourceManager.AddResource("gold", 50f); // +50 gold
```

#### ConsumeResource
Decrease resource amount if sufficient (returns success).

```csharp
bool success = resourceManager.ConsumeResource("mana", 20f); // -20 mana
if (success)
{
    Debug.Log("Spell cast!");
}
```

### 3. Resource Constraints

Set minimum and maximum values for resources.

```csharp
resourceManager.SetResourceConstraints("health", 0f, 100f);
resourceManager.SetResourceConstraints("gold", 0f, 999999f);
```

### 4. Change Events

Listen for resource changes.

```csharp
resourceManager.OnResourceChanged.AddListener((resourceName, newAmount) => 
{
    Debug.Log($"{resourceName} changed to {newAmount}");
    UpdateUI(resourceName, newAmount);
});
```

## Getting Started

### Basic Setup

```csharp
// Create manager hub
var managerGo = new GameObject("EconomyManager");
var manager = managerGo.AddComponent<GameKitManager>();
manager.Initialize("economy", GameKitManager.ManagerType.ResourcePool, false);

// Get direct access to ResourceManager
var resourceManager = manager.GetModeComponent<GameKitResourceManager>();
```

### Via Convenience Methods

```csharp
// Use manager hub convenience methods
manager.SetResource("gold", 1000f);
manager.AddResource("gold", 100f);
bool consumed = manager.ConsumeResource("gold", 50f);
```

### With Machinations Asset

```csharp
// Load machinations asset
var asset = AssetDatabase.LoadAssetAtPath<GameKitMachinationsAsset>("Assets/Economy.asset");

// Apply resource pools from asset
resourceManager.ApplyMachinationsAsset(asset, resetExisting: true);
```

## MCP (AI Assistant) Usage

### Create Manager

```python
# Create resource manager
result = await call_tool("gamekitManager", "create", {
    "managerId": "PlayerEconomy",
    "managerType": "resourcepool",
    "initialResources": {
        "health": 100,
        "mana": 50,
        "gold": 1000
    }
})
```

### Update Resources

```python
# Set resource
await call_tool("gamekitManager", "update", {
    "managerId": "PlayerEconomy",
    "initialResources": {
        "health": 75
    }
})
```

### Apply Machinations Asset

```python
# Apply machinations asset to manager
await call_tool("gamekitMachinations", "apply", {
    "assetPath": "Assets/Economy/RPGEconomy.asset",
    "managerId": "PlayerEconomy",
    "resetExisting": True
})
```

## Advanced Patterns

### 1. Custom Resource Logic

Implement complex behaviors externally:

```csharp
public class ResourceFlowController : MonoBehaviour
{
    [SerializeField] private GameKitManager manager;
    [SerializeField] private float manaRegenRate = 1f;

    void Update()
    {
        // Custom mana regeneration
        float currentMana = manager.GetResource("mana");
        if (currentMana < 50f)
        {
            manager.AddResource("mana", manaRegenRate * Time.deltaTime);
        }
    }
}
```

### 2. Resource Conversion Logic

```csharp
public class PotionShop : MonoBehaviour
{
    [SerializeField] private GameKitManager economy;

    public void BuyHealthPotion()
    {
        if (economy.ConsumeResource("gold", 10))
        {
            economy.AddResource("health", 50);
            Debug.Log("Health potion purchased!");
        }
        else
        {
            Debug.Log("Not enough gold!");
        }
    }
}
```

### 3. Resource Threshold Monitoring

```csharp
public class HealthMonitor : MonoBehaviour
{
    [SerializeField] private GameKitManager economy;
    private float lastHealth;

    void Update()
    {
        float currentHealth = economy.GetResource("health");
        
        if (lastHealth > 20f && currentHealth <= 20f)
        {
            Debug.Log("Warning: Low health!");
            PlayLowHealthSound();
        }
        
        lastHealth = currentHealth;
    }
}
```

### 4. UI Integration

```csharp
public class ResourceUI : MonoBehaviour
{
    [SerializeField] private GameKitManager economy;
    [SerializeField] private Text healthText;
    [SerializeField] private Text goldText;

    void Start()
    {
        var resourceManager = economy.GetModeComponent<GameKitResourceManager>();
        resourceManager.OnResourceChanged.AddListener(OnResourceChanged);
    }

    void OnResourceChanged(string resourceName, float newAmount)
    {
        switch (resourceName)
        {
            case "health":
                healthText.text = $"HP: {newAmount:F0}";
                break;
            case "gold":
                goldText.text = $"Gold: {newAmount:F0}";
                break;
        }
    }
}
```

## Integration with GameKitUICommand

Use UI buttons to control resources:

```python
# Create resource management UI panel
await call_tool("gamekitUICommand", "createCommandPanel", {
    "panelId": "ResourcePanel",
    "canvasPath": "Canvas",
    "targetType": "manager",
    "targetManagerId": "PlayerEconomy",
    "commands": [
        {
            "name": "useHealthPotion",
            "label": "HP Potion (+50)",
            "commandType": "addResource",
            "commandParameter": "health",
            "resourceAmount": 50
        },
        {
            "name": "castFireball",
            "label": "Fireball (-20 MP)",
            "commandType": "consumeResource",
            "commandParameter": "mana",
            "resourceAmount": 20
        }
    ]
})
```

## API Reference

### Core Methods

| Method | Description | Returns |
|--------|-------------|---------|
| `SetResource(name, amount)` | Set resource to specific value | `void` |
| `GetResource(name, default)` | Get current resource amount | `float` |
| `AddResource(name, amount)` | Add to resource (clamped to max) | `void` |
| `ConsumeResource(name, amount)` | Consume resource if sufficient | `bool` |
| `HasResource(name, minAmount)` | Check if resource meets minimum | `bool` |
| `GetAllResources()` | Get all resources as dictionary | `Dictionary<string, float>` |
| `ClearAllResources()` | Clear all resources | `void` |
| `SetResourceConstraints(name, min, max)` | Set min/max constraints | `void` |

### Asset Management

| Method | Description |
|--------|-------------|
| `ApplyMachinationsAsset(asset, reset)` | Initialize from asset |
| `ExportToAsset(diagramId)` | Export current pools to asset |

### Events

| Event | Parameters | Description |
|-------|------------|-------------|
| `OnResourceChanged` | `(string resourceName, float newAmount)` | Fired when any resource changes |

## Best Practices

### 1. Resource Naming
- Use lowercase with underscores: `health`, `max_hp`, `gold_coins`
- Be consistent: `hp` / `mp` / `sp`

### 2. Constraints
- Always set constraints for UI resources: `SetResourceConstraints("health", 0, 100)`
- Use `-Infinity` / `+Infinity` for unconstrained resources like gold

### 3. Event Handling
- Subscribe to `OnResourceChanged` for UI updates
- Unsubscribe in `OnDestroy()` to prevent memory leaks

### 4. External Controllers
- Keep complex logic in separate MonoBehaviours
- Use ResourceManager as a simple data store
- Implement flows, converters, and triggers externally

### 5. Machinations Asset
- Use assets for initial configuration
- Don't rely on assets for runtime logic
- Export configurations for reusability

## Troubleshooting

### Q: Resources don't update in UI
**A**: Subscribe to `OnResourceChanged` event and update UI in the callback.

### Q: ConsumeResource returns false
**A**: Check if resource has sufficient amount. Use `GetResource()` to verify.

### Q: Resource goes below zero
**A**: Use `SetResourceConstraints()` to enforce minimum values.

### Q: How to implement automatic regeneration?
**A**: Create a custom controller script with `Update()` that calls `AddResource()`.

## Related Documentation

- [GameKitMachinations.README.md](./GameKitMachinations.README.md) - Machinations Asset system
- [GameKitManager README](./README.md) - GameKitManager overview
- [GameKitUICommand.README.md](./GameKitUICommand.README.md) - UI integration

## Migration from Old Version

If you were using built-in flows/converters/triggers:

### Before (Old)
```csharp
resourceManager.AddFlow("mana", 1f, true);
resourceManager.AddConverter("gold", "health", 5f, 10f);
resourceManager.AddTrigger("lowHealth", "health", ThresholdType.Below, 20f);
```

### After (New)
```csharp
// Implement flows externally
public class ManaRegenController : MonoBehaviour
{
    [SerializeField] private GameKitManager manager;
    void Update()
    {
        manager.AddResource("mana", 1f * Time.deltaTime);
    }
}

// Implement converters externally
public void BuyHealth()
{
    if (manager.ConsumeResource("gold", 10))
    {
        manager.AddResource("health", 50);
    }
}

// Implement triggers externally
float lastHealth;
void Update()
{
    float health = manager.GetResource("health");
    if (lastHealth > 20f && health <= 20f)
    {
        OnLowHealth();
    }
    lastHealth = health;
}
```

## Examples

### RPG Character Resources

```csharp
// Setup
manager.SetResource("hp", 100);
manager.SetResource("mp", 50);
manager.SetResourceConstraints("hp", 0, 100);
manager.SetResourceConstraints("mp", 0, 50);

// Combat
manager.ConsumeResource("hp", 15); // Take damage
manager.ConsumeResource("mp", 20); // Cast spell

// Recovery
manager.AddResource("hp", 10); // Heal
manager.AddResource("mp", 5);  // Mana potion
```

### Tower Defense Economy

```csharp
// Initial setup
manager.SetResource("gold", 200);
manager.SetResource("lives", 20);
manager.SetResourceConstraints("gold", 0, 999999);
manager.SetResourceConstraints("lives", 0, 20);

// Build tower
if (manager.ConsumeResource("gold", 50))
{
    SpawnTower("BasicTower");
}

// Lose life
manager.ConsumeResource("lives", 1);
if (manager.GetResource("lives") <= 0)
{
    GameOver();
}
```

---

**For complex economic systems with flows/converters/triggers, use [GameKitMachinationsAsset](./GameKitMachinations.README.md) with external controller scripts.**
