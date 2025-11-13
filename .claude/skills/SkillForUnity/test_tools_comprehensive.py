#!/usr/bin/env python3
"""Comprehensive test script for SkillForUnity tools"""

import asyncio
import sys
from pathlib import Path

# Add src directory to path
src_path = Path(__file__).parent / "src"
sys.path.insert(0, str(src_path))

from bridge.bridge_connector import bridge_connector
from bridge.bridge_manager import bridge_manager


async def test_ping():
    """Test basic connectivity - pingUnityEditor"""
    print("\n=== Testing pingUnityEditor ===")
    try:
        result = await bridge_manager.send_command("pingUnityEditor", {})
        print(f"[OK] Ping successful")
        print(f"  Unity Editor: {result.get('editor', 'N/A')}")
        print(f"  Project: {result.get('project', 'N/A')}")
        return True
    except Exception as e:
        print(f"[FAIL] Ping failed: {e}")
        return False


async def test_context_inspect():
    """Test context inspection - contextInspect"""
    print("\n=== Testing contextInspect ===")
    try:
        result = await bridge_manager.send_command("contextInspect", {
            "includeHierarchy": True,
            "includeComponents": False,
            "maxDepth": 2
        })
        print(f"[OK] Context inspect successful")
        print(f"  Scene: {result.get('sceneName', 'N/A')}")
        print(f"  Hierarchy items: {len(result.get('hierarchy', []))}")
        print(f"  Selection: {result.get('selection', [])}")
        return True
    except Exception as e:
        print(f"[FAIL] Context inspect failed: {e}")
        return False


async def test_scene_manage():
    """Test scene management - sceneManage"""
    print("\n=== Testing sceneManage ===")
    try:
        # List scenes
        result = await bridge_manager.send_command("sceneManage", {
            "operation": "list"
        })
        print(f"[OK] Scene management successful")
        print(f"  Active scene: {result.get('activeScene', 'N/A')}")
        print(f"  Total scenes: {len(result.get('scenes', []))}")
        return True
    except Exception as e:
        print(f"[FAIL] Scene management failed: {e}")
        return False


async def test_scene_quick_setup():
    """Test quick scene setup - sceneQuickSetup"""
    print("\n=== Testing sceneQuickSetup ===")
    try:
        result = await bridge_manager.send_command("sceneQuickSetup", {
            "setupType": "3D"
        })
        print(f"[OK] Quick setup successful")
        print(f"  Objects created: {result.get('createdObjects', [])}")
        return True
    except Exception as e:
        print(f"[FAIL] Quick setup failed: {e}")
        return False


async def test_gameobject_manage():
    """Test GameObject management - gameObjectManage"""
    print("\n=== Testing gameObjectManage ===")
    try:
        # Create a test GameObject
        create_result = await bridge_manager.send_command("gameObjectManage", {
            "operation": "create",
            "name": "TestObject_SkillTest"
        })
        print(f"[OK] GameObject create successful: {create_result.get('path', 'N/A')}")

        # Delete the test GameObject
        await bridge_manager.send_command("gameObjectManage", {
            "operation": "delete",
            "gameObjectPath": "TestObject_SkillTest"
        })
        print(f"[OK] GameObject delete successful")
        return True
    except Exception as e:
        print(f"[FAIL] GameObject management failed: {e}")
        return False


async def test_gameobject_template():
    """Test GameObject templates - gameObjectCreateFromTemplate"""
    print("\n=== Testing gameObjectCreateFromTemplate ===")
    try:
        create_result = await bridge_manager.send_command("gameObjectCreateFromTemplate", {
            "template": "Cube",
            "name": "TestCube",
            "position": {"x": 0, "y": 1, "z": 0}
        })
        print(f"[OK] GameObject template successful: {create_result.get('path', 'N/A')}")

        # Clean up
        await bridge_manager.send_command("gameObjectManage", {
            "operation": "delete",
            "gameObjectPath": "TestCube"
        })
        print(f"[OK] Cleanup successful")
        return True
    except Exception as e:
        print(f"[FAIL] GameObject template failed: {e}")
        return False


async def test_component_manage():
    """Test component management - componentManage"""
    print("\n=== Testing componentManage ===")
    try:
        # Create a test object first
        await bridge_manager.send_command("gameObjectManage", {
            "operation": "create",
            "name": "ComponentTestObject"
        })

        # Add Rigidbody component
        add_result = await bridge_manager.send_command("componentManage", {
            "operation": "add",
            "gameObjectPath": "ComponentTestObject",
            "componentType": "UnityEngine.Rigidbody"
        })
        print(f"[OK] Component add successful")

        # Remove component
        await bridge_manager.send_command("componentManage", {
            "operation": "remove",
            "gameObjectPath": "ComponentTestObject",
            "componentType": "UnityEngine.Rigidbody"
        })
        print(f"[OK] Component remove successful")

        # Clean up
        await bridge_manager.send_command("gameObjectManage", {
            "operation": "delete",
            "gameObjectPath": "ComponentTestObject"
        })
        return True
    except Exception as e:
        print(f"[FAIL] Component management failed: {e}")
        return False


async def test_hierarchy_builder():
    """Test hierarchy builder - hierarchyBuilder"""
    print("\n=== Testing hierarchyBuilder ===")
    try:
        result = await bridge_manager.send_command("hierarchyBuilder", {
            "hierarchy": {
                "TestParent": {
                    "components": ["UnityEngine.BoxCollider"],
                    "children": {
                        "TestChild1": {},
                        "TestChild2": {}
                    }
                }
            }
        })
        print(f"[OK] Hierarchy builder successful")
        print(f"  Root objects: {result.get('rootObjects', [])}")

        # Clean up
        await bridge_manager.send_command("gameObjectManage", {
            "operation": "delete",
            "gameObjectPath": "TestParent"
        })
        return True
    except Exception as e:
        print(f"[FAIL] Hierarchy builder failed: {e}")
        return False


async def test_ugui_template():
    """Test UI templates - uguiCreateFromTemplate"""
    print("\n=== Testing uguiCreateFromTemplate ===")
    try:
        # First ensure we have a Canvas
        try:
            await bridge_manager.send_command("sceneQuickSetup", {
                "setupType": "UI"
            })
        except:
            pass  # Canvas might already exist

        # Create a Button
        result = await bridge_manager.send_command("uguiCreateFromTemplate", {
            "template": "Button",
            "text": "Test Button",
            "width": 160,
            "height": 30
        })
        print(f"[OK] UI template successful: {result.get('path', 'N/A')}")

        # Clean up
        button_path = result.get("path", "Canvas/Button")
        await bridge_manager.send_command("gameObjectManage", {
            "operation": "delete",
            "gameObjectPath": button_path
        })
        return True
    except Exception as e:
        print(f"[FAIL] UI template failed: {e}")
        return False


async def test_asset_manage():
    """Test asset management - assetManage"""
    print("\n=== Testing assetManage ===")
    try:
        # Find assets
        result = await bridge_manager.send_command("assetManage", {
            "operation": "findMultiple",
            "pattern": "Assets/*.unity"
        })
        print(f"[OK] Asset management successful")
        print(f"  Found {len(result.get('assets', []))} scene files")
        return True
    except Exception as e:
        print(f"[FAIL] Asset management failed: {e}")
        return False


async def run_tests():
    """Run all comprehensive tests"""
    print("Starting SkillForUnity Comprehensive Tool Tests...")
    print(f"Connecting to Unity Bridge at ws://127.0.0.1:7070/bridge")

    # Start bridge connector
    bridge_connector.start()

    # Wait for connection
    await asyncio.sleep(2)

    if not bridge_manager.is_connected():
        print("[FAIL] Failed to connect to Unity Bridge")
        print("Make sure Unity Editor is open and MCP Bridge is started (Tools > MCP Assistant)")
        return

    print("[OK] Connected to Unity Bridge\n")

    # Run tests
    tests = [
        ("Ping (pingUnityEditor)", test_ping),
        ("Context Inspect", test_context_inspect),
        ("Scene Management", test_scene_manage),
        ("Scene Quick Setup", test_scene_quick_setup),
        ("GameObject Management", test_gameobject_manage),
        ("GameObject Template", test_gameobject_template),
        ("Component Management", test_component_manage),
        ("Hierarchy Builder", test_hierarchy_builder),
        ("UI Template", test_ugui_template),
        ("Asset Management", test_asset_manage),
    ]

    results = {}
    for name, test_func in tests:
        try:
            results[name] = await test_func()
        except Exception as e:
            print(f"[FAIL] Test '{name}' crashed: {e}")
            results[name] = False

        # Small delay between tests
        await asyncio.sleep(0.5)

    # Summary
    print("\n" + "=" * 60)
    print("COMPREHENSIVE TEST SUMMARY")
    print("=" * 60)
    passed = sum(1 for r in results.values() if r)
    total = len(results)
    print(f"Passed: {passed}/{total}")
    print(f"Success Rate: {passed/total*100:.1f}%\n")

    for name, result in results.items():
        status = "[OK]  " if result else "[FAIL]"
        print(f"{status} {name}")

    # Stop bridge connector
    await bridge_connector.stop()


if __name__ == "__main__":
    asyncio.run(run_tests())
