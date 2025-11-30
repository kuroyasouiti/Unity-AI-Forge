# GameKitResourceManager

## Overview

`GameKitResourceManager` is a Machinations-inspired resource flow system for managing game economies, health/mana pools, and resource production chains. It provides declarative resource management with automatic flows, converters, and threshold triggers.

## Core Concepts (Inspired by Machinations)

### 1. Resource Pool
Basic storage for resources with optional constraints.

```csharp
manager.SetResource("gold", 100f);
manager.SetResourceConstraints("health", 0f, 100f); // Min: 0, Max: 100
```

### 2. Resource Flow (Source/Drain)
Automatic resource generation or consumption over time.

```csharp
// Source: Generate 5 gold per second
resourceManager.AddFlow("gold", 5f, isSource: true);

// Drain: Consume 2 mana per second
resourceManager.AddFlow("mana", 2f, isSource: false);
```

### 3. Resource Converter
Transform one resource into another.

```csharp
// 1 wood → 4 planks
resourceManager.AddConverter("wood", "planks", conversionRate: 4f, inputCost: 1f);

// Execute conversion
bool success = resourceManager.Convert("wood", "planks", amount: 10f);
// Consumes 10 wood, produces 40 planks
```

### 4. Resource Trigger
Invoke events when resources cross thresholds.

```csharp
// Trigger when health drops below 30
resourceManager.AddTrigger("lowHealth", "health", ThresholdType.Below, 30f);

resourceManager.OnResourceTriggered.AddListener((triggerName, resourceName, value) => {
    if (triggerName == "lowHealth")
    {
        Debug.Log("Warning: Low health!");
    }
});
```

## Getting Started

### Basic Setup

```csharp
// Create manager hub
var managerGo = new GameObject("ResourceManager");
var manager = managerGo.AddComponent<GameKitManager>();
manager.Initialize("resourceMgr", GameKitManager.ManagerType.ResourcePool, false);

// Get direct access to ResourceManager
var resourceManager = manager.GetModeComponent<GameKitResourceManager>();
```

### Via Convenience Methods

```csharp
// Use manager hub convenience methods (automatically delegates)
manager.SetResource("gold", 1000f);
manager.AddResource("gold", 100f);
bool consumed = manager.ConsumeResource("gold", 50f);
```

## API Reference

### Resource Operations

#### SetResource
Set resource to specific value (clamped to min/max).

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
resourceManager.AddResource("gold", 50f);
```

#### ConsumeResource
Decrease resource if sufficient (clamped to min).

```csharp
bool success = resourceManager.ConsumeResource("mana", 25f);
if (success)
{
    // Cast spell
}
```

#### GetAllResources
Get dictionary of all resources.

```csharp
var allResources = resourceManager.GetAllResources();
foreach (var kvp in allResources)
{
    Debug.Log($"{kvp.Key}: {kvp.Value}");
}
```

#### HasResource
Check if resource meets minimum amount.

```csharp
bool canCraft = resourceManager.HasResource("iron", 5f);
```

#### ClearAllResources
Remove all resources.

```csharp
resourceManager.ClearAllResources();
```

### Resource Constraints

#### SetResourceConstraints
Set min/max boundaries for a resource.

```csharp
// Health between 0 and 100
resourceManager.SetResourceConstraints("health", 0f, 100f);

// Armor between 0 and 50
resourceManager.SetResourceConstraints("armor", 0f, 50f);

// Debt can go negative
resourceManager.SetResourceConstraints("gold", -1000f, 999999f);
```

### Resource Flows

#### AddFlow
Add automatic resource generation or consumption.

```csharp
// Generate 2 health per second (regeneration)
resourceManager.AddFlow("health", 2f, isSource: true);

// Consume 1 stamina per second (drain)
resourceManager.AddFlow("stamina", 1f, isSource: false);

// Passive gold income
resourceManager.AddFlow("gold", 10f, isSource: true);
```

#### SetFlowEnabled
Enable or disable a flow.

```csharp
resourceManager.SetFlowEnabled("health", false); // Stop regeneration
resourceManager.SetFlowEnabled("health", true);  // Resume regeneration
```

### Resource Converters

#### AddConverter
Define resource conversion rules.

```csharp
// Wood → Planks (1:4 ratio)
resourceManager.AddConverter("wood", "planks", conversionRate: 4f, inputCost: 1f);

// Iron + Coal → Steel (2 iron + 1 coal = 1 steel)
resourceManager.AddConverter("iron", "steel", conversionRate: 0.5f, inputCost: 2f);
// Then add coal cost via a second converter or custom logic
```

#### Convert
Execute a resource conversion.

```csharp
// Convert 10 wood to planks
bool success = resourceManager.Convert("wood", "planks", amount: 10f);
// Consumes: 10 wood (inputCost * amount)
// Produces: 40 planks (conversionRate * amount)
```

#### SetConverterEnabled
Enable or disable a converter.

```csharp
resourceManager.SetConverterEnabled("wood", "planks", false); // Disable sawmill
resourceManager.SetConverterEnabled("wood", "planks", true);  // Enable sawmill
```

### Resource Triggers

#### AddTrigger
Add threshold-based event trigger.

```csharp
// Warn when health drops below 30
resourceManager.AddTrigger("lowHealth", "health", ThresholdType.Below, 30f);

// Celebrate when gold reaches 1000
resourceManager.AddTrigger("richPlayer", "gold", ThresholdType.Above, 1000f);

// Alert when mana is depleted
resourceManager.AddTrigger("noMana", "mana", ThresholdType.Equal, 0f);
```

**Threshold Types:**
- `Above`: Triggers when crossing above threshold (e.g., level up)
- `Below`: Triggers when crossing below threshold (e.g., low health)
- `Equal`: Triggers when equal to threshold (e.g., exactly 0)
- `NotEqual`: Triggers when not equal (e.g., any change from 0)

#### OnResourceTriggered Event

```csharp
resourceManager.OnResourceTriggered.AddListener((triggerName, resourceName, currentValue) => {
    switch (triggerName)
    {
        case "lowHealth":
            ShowWarningUI();
            break;
        case "noMana":
            DisableSpells();
            break;
        case "richPlayer":
            UnlockPremiumShop();
            break;
    }
});
```

## Common Patterns

### RPG Health System

```csharp
// Setup health with constraints and regeneration
resourceManager.SetResource("health", 100f);
resourceManager.SetResourceConstraints("health", 0f, 100f);
resourceManager.AddFlow("health", 2f, isSource: true); // 2 HP/sec regen

// Low health warning
resourceManager.AddTrigger("lowHealth", "health", ThresholdType.Below, 30f);
resourceManager.OnResourceTriggered.AddListener((trigger, resource, value) => {
    if (trigger == "lowHealth")
    {
        PlayLowHealthSound();
        ShowHealthWarning();
    }
});

// Death trigger
resourceManager.AddTrigger("death", "health", ThresholdType.Equal, 0f);
```

### Strategy Game Economy

```csharp
// Resources
resourceManager.SetResource("gold", 500f);
resourceManager.SetResource("wood", 100f);
resourceManager.SetResource("stone", 50f);
resourceManager.SetResource("food", 200f);

// Passive income
resourceManager.AddFlow("gold", 10f, isSource: true); // 10 gold/sec from taxation
resourceManager.AddFlow("food", 5f, isSource: true);  // 5 food/sec from farms

// Passive consumption
resourceManager.AddFlow("food", 2f, isSource: false); // 2 food/sec for population

// Crafting conversions
resourceManager.AddConverter("wood", "planks", 4f, 1f);     // 1 wood → 4 planks
resourceManager.AddConverter("iron", "weapons", 2f, 3f);    // 3 iron → 2 weapons
resourceManager.AddConverter("gold", "soldiers", 1f, 100f); // 100 gold → 1 soldier

// Economic triggers
resourceManager.AddTrigger("recession", "gold", ThresholdType.Below, 100f);
resourceManager.AddTrigger("famine", "food", ThresholdType.Below, 50f);
```

### Crafting System

```csharp
// Raw materials
resourceManager.SetResource("iron_ore", 50f);
resourceManager.SetResource("coal", 30f);
resourceManager.SetResource("wood", 100f);

// Intermediate materials
resourceManager.SetResource("iron_bars", 0f);
resourceManager.SetResource("steel", 0f);
resourceManager.SetResource("planks", 0f);

// Smelting converters
resourceManager.AddConverter("iron_ore", "iron_bars", 1f, 1f);     // 1 ore → 1 bar
resourceManager.AddConverter("iron_bars", "steel", 1f, 2f);        // 2 bars + coal → 1 steel
resourceManager.AddConverter("wood", "planks", 4f, 1f);            // 1 wood → 4 planks

// Craft iron sword
public void CraftIronSword()
{
    if (resourceManager.Convert("iron_bars", "swords", 1f) && 
        resourceManager.Convert("wood", "sword_handles", 1f))
    {
        Debug.Log("Iron sword crafted!");
    }
}
```

### Idle Game Production Chain

```csharp
// Tier 1: Basic resources
resourceManager.SetResource("wood", 0f);
resourceManager.AddFlow("wood", 1f, isSource: true); // 1 wood/sec from trees

// Tier 2: Processed resources
resourceManager.SetResource("planks", 0f);
resourceManager.AddConverter("wood", "planks", 2f, 1f); // 1 wood → 2 planks

// Tier 3: Advanced resources
resourceManager.SetResource("furniture", 0f);
resourceManager.AddConverter("planks", "furniture", 0.1f, 10f); // 10 planks → 1 furniture

// Auto-craft when sufficient resources
void Update()
{
    if (resourceManager.GetResource("wood") >= 10f)
    {
        resourceManager.Convert("wood", "planks", 10f);
    }
    
    if (resourceManager.GetResource("planks") >= 20f)
    {
        resourceManager.Convert("planks", "furniture", 2f);
    }
}

// Milestones
resourceManager.AddTrigger("milestone_100_furniture", "furniture", ThresholdType.Above, 100f);
```

### Tower Defense Resource Economy

```csharp
// Player resources
resourceManager.SetResource("gold", 1000f);
resourceManager.SetResource("lives", 20f);
resourceManager.SetResourceConstraints("lives", 0f, 20f);

// Passive income from killing enemies
resourceManager.AddFlow("gold", 5f, isSource: true); // Base income

// Build tower (consume gold)
public void BuildTower(int cost)
{
    if (resourceManager.ConsumeResource("gold", cost))
    {
        SpawnTower();
    }
}

// Enemy reaches end
public void OnEnemyEscaped()
{
    resourceManager.ConsumeResource("lives", 1f);
}

// Game over trigger
resourceManager.AddTrigger("gameOver", "lives", ThresholdType.Equal, 0f);
resourceManager.OnResourceTriggered.AddListener((trigger, resource, value) => {
    if (trigger == "gameOver")
    {
        EndGame();
    }
});
```

### Survival Game Systems

```csharp
// Vitals
resourceManager.SetResource("health", 100f);
resourceManager.SetResource("hunger", 100f);
resourceManager.SetResource("thirst", 100f);
resourceManager.SetResource("stamina", 100f);

resourceManager.SetResourceConstraints("health", 0f, 100f);
resourceManager.SetResourceConstraints("hunger", 0f, 100f);
resourceManager.SetResourceConstraints("thirst", 0f, 100f);
resourceManager.SetResourceConstraints("stamina", 0f, 100f);

// Passive drains
resourceManager.AddFlow("hunger", 1f, isSource: false);  // 1/sec hunger drain
resourceManager.AddFlow("thirst", 2f, isSource: false);  // 2/sec thirst drain
resourceManager.AddFlow("stamina", 5f, isSource: true);  // 5/sec stamina regen

// Health affected by hunger/thirst
void Update()
{
    if (resourceManager.GetResource("hunger") == 0f)
    {
        resourceManager.ConsumeResource("health", Time.deltaTime * 2f); // Starving
    }
    
    if (resourceManager.GetResource("thirst") == 0f)
    {
        resourceManager.ConsumeResource("health", Time.deltaTime * 3f); // Dehydrating
    }
}

// Critical state triggers
resourceManager.AddTrigger("starving", "hunger", ThresholdType.Equal, 0f);
resourceManager.AddTrigger("dehydrated", "thirst", ThresholdType.Equal, 0f);
resourceManager.AddTrigger("critical", "health", ThresholdType.Below, 20f);
```

## Advanced Features

### Multi-Step Production Chains

```csharp
// Raw → Intermediate → Final
// Wood → Planks → Furniture
// Ore → Bars → Weapons

// Setup chain
resourceManager.AddConverter("wood", "planks", 2f, 1f);
resourceManager.AddConverter("planks", "furniture", 0.5f, 4f);
resourceManager.AddConverter("ore", "bars", 1f, 1f);
resourceManager.AddConverter("bars", "weapons", 1f, 3f);

// Auto-process chain
void ProcessProductionChain()
{
    // Process tier 1
    if (resourceManager.GetResource("wood") >= 10f)
        resourceManager.Convert("wood", "planks", 10f);
    
    if (resourceManager.GetResource("ore") >= 5f)
        resourceManager.Convert("ore", "bars", 5f);
    
    // Process tier 2
    if (resourceManager.GetResource("planks") >= 8f)
        resourceManager.Convert("planks", "furniture", 2f);
    
    if (resourceManager.GetResource("bars") >= 3f)
        resourceManager.Convert("bars", "weapons", 1f);
}
```

### Dynamic Flow Control

```csharp
// Start/stop flows based on game state

// Building provides gold income
public void OnBuildingConstructed()
{
    resourceManager.AddFlow("gold", 5f, isSource: true);
}

public void OnBuildingDestroyed()
{
    resourceManager.SetFlowEnabled("gold", false);
}

// Stamina drains while sprinting
public void OnSprintStart()
{
    resourceManager.AddFlow("stamina", 10f, isSource: false);
}

public void OnSprintEnd()
{
    resourceManager.SetFlowEnabled("stamina", false);
}
```

### Conditional Converters

```csharp
// Enable/disable converters based on upgrades or conditions

// Unlock advanced crafting
public void OnResearchComplete(string tech)
{
    if (tech == "steelForging")
    {
        resourceManager.AddConverter("iron", "steel", 1f, 2f);
    }
    else if (tech == "alchemy")
    {
        resourceManager.AddConverter("herbs", "potions", 3f, 1f);
    }
}

// Temporarily disable during events
public void OnPowerOutage()
{
    resourceManager.SetConverterEnabled("iron_ore", "iron_bars", false); // Furnace offline
}
```

### Cascading Triggers

```csharp
// Setup multiple threshold triggers
resourceManager.AddTrigger("healthFull", "health", ThresholdType.Equal, 100f);
resourceManager.AddTrigger("healthHigh", "health", ThresholdType.Above, 70f);
resourceManager.AddTrigger("healthLow", "health", ThresholdType.Below, 30f);
resourceManager.AddTrigger("healthCritical", "health", ThresholdType.Below, 10f);
resourceManager.AddTrigger("healthDepleted", "health", ThresholdType.Equal, 0f);

resourceManager.OnResourceTriggered.AddListener((trigger, resource, value) => {
    switch (trigger)
    {
        case "healthFull":
            StopHealthRegen();
            break;
        case "healthLow":
            ShowHealthWarning();
            PlayHeartbeatSound();
            break;
        case "healthCritical":
            ApplyScreenEffect();
            break;
        case "healthDepleted":
            PlayerDeath();
            break;
    }
});
```

## Use Cases

### 1. RPG Character Stats
Health, mana, stamina with regeneration and constraints.

### 2. Strategy Game Economy
Resource production, consumption, and conversion chains.

### 3. Crafting System
Material transformation with multi-tier recipes.

### 4. Idle/Incremental Games
Automatic resource generation, upgrades, prestige systems.

### 5. Tower Defense
Gold income, tower costs, life system.

### 6. Survival Games
Hunger, thirst, temperature, health interconnections.

### 7. Card Game Resources
Mana pools, card draw mechanics, energy systems.

### 8. City Builder
Population, food, happiness, production chains.

## Events

### OnResourceChanged
Invoked whenever a resource amount changes.

```csharp
resourceManager.OnResourceChanged.AddListener((resourceName, newAmount) => {
    UpdateResourceUI(resourceName, newAmount);
});
```

### OnResourceTriggered
Invoked when a resource crosses a threshold.

```csharp
resourceManager.OnResourceTriggered.AddListener((triggerName, resourceName, currentValue) => {
    Debug.Log($"Trigger '{triggerName}' fired: {resourceName} = {currentValue}");
});
```

## Inspector Configuration

### Resource Entry
- `name`: Resource identifier
- `amount`: Current value
- `minValue`: Minimum allowed (default: -∞)
- `maxValue`: Maximum allowed (default: +∞)

### Resource Flow
- `resourceName`: Target resource
- `ratePerSecond`: Flow rate (positive for source, negative for drain)
- `isSource`: True = generate, False = consume
- `enabled`: Enable/disable flow

### Resource Converter
- `fromResource`: Input resource
- `toResource`: Output resource
- `conversionRate`: Output per input (1:N ratio)
- `inputCost`: Input amount per conversion
- `enabled`: Enable/disable converter

### Resource Trigger
- `triggerName`: Unique trigger identifier
- `resourceName`: Resource to monitor
- `thresholdType`: Above/Below/Equal/NotEqual
- `thresholdValue`: Threshold value
- `enabled`: Enable/disable trigger

## Performance Considerations

### Flow Updates
Flows are processed every frame in `Update()`. For many flows, consider:
- Disabling unused flows with `SetFlowEnabled()`
- Using larger time intervals for non-critical flows

### Trigger Checks
Triggers are evaluated every frame. For optimization:
- Use threshold triggers sparingly for critical events
- Disable triggers when not needed
- Consider polling GetResource() instead for non-critical checks

## Best Practices

### 1. Define Constraints
Always set min/max for resources that have limits:
```csharp
resourceManager.SetResourceConstraints("health", 0f, 100f);
```

### 2. Use Flows for Passive Effects
Instead of manual updates:
```csharp
// Bad: Manual update every frame
void Update() { manager.AddResource("mana", 0.5f * Time.deltaTime); }

// Good: Define flow once
resourceManager.AddFlow("mana", 0.5f, isSource: true);
```

### 3. Centralize Conversion Logic
Define all conversions upfront:
```csharp
void SetupCrafting()
{
    resourceManager.AddConverter("wood", "planks", 4f, 1f);
    resourceManager.AddConverter("iron", "bars", 1f, 1f);
    resourceManager.AddConverter("gold", "coins", 100f, 1f);
}
```

### 4. Use Triggers for State Changes
Instead of polling:
```csharp
// Bad: Check every frame
void Update() { if (manager.GetResource("gold") > 1000f) UnlockShop(); }

// Good: Use trigger
resourceManager.AddTrigger("unlockShop", "gold", ThresholdType.Above, 1000f);
resourceManager.OnResourceTriggered.AddListener((trigger, resource, value) => {
    if (trigger == "unlockShop") UnlockShop();
});
```

## Machinations Equivalents

| Machinations | GameKitResourceManager |
|--------------|------------------------|
| Pool | ResourceEntry |
| Source | ResourceFlow (isSource=true) |
| Drain | ResourceFlow (isSource=false) |
| Converter | ResourceConverter |
| State Connection | ResourceTrigger |
| Gate | SetFlowEnabled() / SetConverterEnabled() |

## Debugging

Enable logging to track resource changes:

```csharp
resourceManager.OnResourceChanged.AddListener((name, amount) => {
    Debug.Log($"[Resource] {name} = {amount}");
});

resourceManager.OnResourceTriggered.AddListener((trigger, resource, value) => {
    Debug.Log($"[Trigger] {trigger}: {resource} = {value}");
});
```

## See Also

- [GameKit Manager](./README.md#manager-system)
- [GameKit Interaction](./GameKitInteraction.README.md)
- [Machinations.io](https://machinations.io) - Original inspiration

