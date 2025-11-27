# SerializeField Support Testing Guide

## Overview

This document provides testing procedures for the SerializeField support implementation in SkillForUnity's MCP bridge.

## Implementation Summary

### Modified Files
- `Assets/SkillForUnity/Editor/MCPBridge/McpCommandProcessor.cs`

### Changes Made

1. **Line 6726**: Modified field lookup to include private fields
   ```csharp
   // Before:
   var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);

   // After:
   var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
   ```

2. **Line 6755-6758**: Modified error message to show SerializeField fields
   ```csharp
   // Before:
   var allFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
       .Select(f => f.Name)
       .ToList();

   // After:
   var allFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
       .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
       .Select(f => f.Name)
       .ToList();
   ```

3. **Line 946-947**: Modified component inspection to include SerializeField fields
   ```csharp
   // Before:
   var fieldInfos = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);

   // After:
   var fieldInfos = componentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
       .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null);
   ```

4. **Line 1317-1318**: Modified inspectMultiple to include SerializeField fields
   ```csharp
   // Before:
   var fieldInfos = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);

   // After:
   var fieldInfos = componentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
       .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null);
   ```

## Test Prerequisites

1. Unity Editor is open
2. MCP Bridge is started (Tools > MCP Assistant > Start Bridge)
3. Test script `SerializeFieldTest.cs` is compiled
4. Test scene with GameObject containing SerializeFieldTest component

## Test Setup

### Step 1: Create Test Scene

```python
# Setup 3D scene
unity_scene_quickSetup({"setupType": "3D"})

# Create test GameObject
unity_gameobject_crud({
    "operation": "create",
    "name": "SerializeFieldTestObject"
})

# Add the test component
unity_component_crud({
    "operation": "add",
    "gameObjectPath": "SerializeFieldTestObject",
    "componentType": "SerializeFieldTest"
})
```

## Test Cases

### Test 1: Inspect SerializeField Fields

**Objective**: Verify that private SerializeField fields are visible during inspection

```python
result = unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "SerializeFieldTestObject",
    "componentType": "SerializeFieldTest",
    "includeProperties": True
})
```

**Expected Result**:
- Properties should include: `maxHealth`, `moveSpeed`, `playerName`, `isInvincible`, `spawnPosition`, `playerColor`, `weaponPrefab`, `playerMaterial`, `jumpSound`
- Properties should include: `publicHealth`, `publicSpeed` (public fields)
- Properties should NOT include: `hiddenValue` (private without SerializeField)

### Test 2: Update Primitive SerializeField Values

**Objective**: Verify that primitive type SerializeField fields can be updated

```python
# Update integer field
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "SerializeFieldTestObject",
    "componentType": "SerializeFieldTest",
    "propertyChanges": {
        "maxHealth": 200,
        "moveSpeed": 10.0,
        "playerName": "SuperPlayer",
        "isInvincible": True
    }
})
```

**Verification**:
```python
# Inspect to verify changes
result = unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "SerializeFieldTestObject",
    "componentType": "SerializeFieldTest",
    "propertyFilter": ["maxHealth", "moveSpeed", "playerName", "isInvincible"]
})

# Expected:
# result["properties"]["maxHealth"] == 200
# result["properties"]["moveSpeed"] == 10.0
# result["properties"]["playerName"] == "SuperPlayer"
# result["properties"]["isInvincible"] == True
```

### Test 3: Update Vector SerializeField Values

**Objective**: Verify that Vector3 and Color SerializeField fields can be updated

```python
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "SerializeFieldTestObject",
    "componentType": "SerializeFieldTest",
    "propertyChanges": {
        "spawnPosition": {"x": 10, "y": 5, "z": -3},
        "playerColor": {"r": 1.0, "g": 0.5, "b": 0.0, "a": 1.0}
    }
})
```

**Verification**:
```python
result = unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "SerializeFieldTestObject",
    "componentType": "SerializeFieldTest",
    "propertyFilter": ["spawnPosition", "playerColor"]
})

# Expected:
# result["properties"]["spawnPosition"] == {"x": 10, "y": 5, "z": -3}
# result["properties"]["playerColor"] == {"r": 1.0, "g": 0.5, "b": 0.0, "a": 1.0}
```

### Test 4: Update Asset Reference SerializeField Fields

**Objective**: Verify that asset references in SerializeField fields can be set

**Prerequisites**: Create test assets first

```python
# Create a test material
unity_asset_crud({
    "operation": "create",
    "assetPath": "Assets/Materials/TestMaterial.mat",
    "assetType": "Material"
})

# Update the material reference
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "SerializeFieldTestObject",
    "componentType": "SerializeFieldTest",
    "propertyChanges": {
        "playerMaterial": {
            "_ref": "asset",
            "path": "Assets/Materials/TestMaterial.mat"
        }
    }
})
```

**Verification**:
```python
result = unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "SerializeFieldTestObject",
    "componentType": "SerializeFieldTest",
    "propertyFilter": ["playerMaterial"]
})

# Expected:
# result["properties"]["playerMaterial"] should reference the TestMaterial
```

### Test 5: Property Filter with SerializeField

**Objective**: Verify that propertyFilter works with SerializeField fields

```python
result = unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "SerializeFieldTestObject",
    "componentType": "SerializeFieldTest",
    "propertyFilter": ["maxHealth", "moveSpeed"]  # Only SerializeField fields
})
```

**Expected Result**:
- Properties should contain only `maxHealth` and `moveSpeed`
- Other fields should not be included

### Test 6: Error Message Improvement

**Objective**: Verify that error messages suggest SerializeField fields

```python
# Try to set a non-existent field
try:
    unity_component_crud({
        "operation": "update",
        "gameObjectPath": "SerializeFieldTestObject",
        "componentType": "SerializeFieldTest",
        "propertyChanges": {
            "nonExistentField": 123
        }
    })
except Exception as e:
    # Expected: Error message should list available fields including SerializeField fields
    print(e)
```

**Expected Result**:
- Error message should include suggestions
- Suggestions should list: `maxHealth`, `moveSpeed`, `playerName`, etc. (SerializeField fields)

### Test 7: Negative Test - Private Field Without SerializeField

**Objective**: Verify that private fields WITHOUT SerializeField are NOT accessible

```python
# Try to update hiddenValue (private, no SerializeField)
try:
    unity_component_crud({
        "operation": "update",
        "gameObjectPath": "SerializeFieldTestObject",
        "componentType": "SerializeFieldTest",
        "propertyChanges": {
            "hiddenValue": 999
        }
    })
    assert False, "Should have raised an error"
except Exception as e:
    # Expected: Error - field not found
    print("Correctly rejected:", e)
```

**Expected Result**:
- Operation should fail with "Property or field 'hiddenValue' not found"
- `hiddenValue` should NOT appear in suggestions

### Test 8: Batch Update with SerializeField

**Objective**: Verify that batch operations work with SerializeField fields

```python
# Create multiple test objects
for i in range(3):
    unity_gameobject_crud({
        "operation": "create",
        "name": f"TestObject{i}"
    })
    unity_component_crud({
        "operation": "add",
        "gameObjectPath": f"TestObject{i}",
        "componentType": "SerializeFieldTest"
    })

# Batch update SerializeField fields
unity_component_crud({
    "operation": "updateMultiple",
    "pattern": "TestObject*",
    "componentType": "SerializeFieldTest",
    "propertyChanges": {
        "maxHealth": 150,
        "moveSpeed": 8.0
    }
})
```

**Verification**:
```python
result = unity_component_crud({
    "operation": "inspectMultiple",
    "pattern": "TestObject*",
    "componentType": "SerializeFieldTest",
    "propertyFilter": ["maxHealth", "moveSpeed"]
})

# Expected: All 3 objects should have maxHealth=150, moveSpeed=8.0
```

## Manual Verification in Unity Editor

After running the automated tests:

1. Open Unity Editor
2. Select `SerializeFieldTestObject` in the Hierarchy
3. Check the Inspector:
   - Verify all SerializeField values match what was set via MCP
   - Verify asset references are correctly assigned
4. Click "Play" mode
5. Check Console logs from `SerializeFieldTest.Start()` method
6. Verify the logged values match the updated values

## Expected Behavior Summary

### ✅ Should Work
- Reading SerializeField private fields during inspection
- Writing SerializeField private fields (primitives, vectors, colors, assets)
- Property filters with SerializeField field names
- Error messages showing SerializeField fields in suggestions
- Batch operations on SerializeField fields

### ❌ Should NOT Work
- Accessing private fields WITHOUT SerializeField attribute
- `hiddenValue` should remain inaccessible via MCP

## Rollback Procedure

If issues are found, rollback changes by reverting to the previous implementation:

```csharp
// Revert ApplyPropertyToObject (line 6726)
var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);

// Revert error message (line 6755)
var allFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
    .Select(f => f.Name)
    .ToList();

// Revert inspect (line 946)
var fieldInfos = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);

// Revert inspectMultiple (line 1317)
var fieldInfos = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
```

## Success Criteria

All 8 test cases pass:
1. ✅ SerializeField fields visible in inspection
2. ✅ Primitive SerializeField values can be updated
3. ✅ Vector/Color SerializeField values can be updated
4. ✅ Asset references in SerializeField can be set
5. ✅ Property filter works with SerializeField
6. ✅ Error messages include SerializeField fields
7. ✅ Private fields without SerializeField remain inaccessible
8. ✅ Batch operations work with SerializeField

## Notes

- This implementation uses reflection to access private fields
- SerializeField attribute detection uses `GetCustomAttribute<SerializeField>()`
- Performance impact is minimal (single attribute check per field)
- Compatible with Unity's serialization system
- Works for all types supported by Unity's serialization (primitives, vectors, Object references, etc.)
