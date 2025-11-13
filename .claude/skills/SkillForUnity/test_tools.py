#!/usr/bin/env python3
"""Test script for SkillForUnity tools"""

import asyncio
import sys
from pathlib import Path

# Add src directory to path
src_path = Path(__file__).parent / "src"
sys.path.insert(0, str(src_path))

from bridge.bridge_connector import bridge_connector
from bridge.bridge_manager import bridge_manager


async def test_ping():
    """Test basic connectivity"""
    print("\n=== Testing unity.ping ===")
    try:
        result = await bridge_manager.send_command("ping", {})
        print(f"[OK] Ping successful: {result}")
        return True
    except Exception as e:
        print(f"[FAIL] Ping failed: {e}")
        return False


async def test_context_inspect():
    """Test context inspection"""
    print("\n=== Testing unity.context.inspect ===")
    try:
        result = await bridge_manager.send_command("contextInspect", {
            "includeHierarchy": True,
            "includeComponents": False,
            "maxDepth": 2
        })
        print(f"[OK] Context inspect successful")
        print(f"Scene: {result.get('sceneName', 'N/A')}")
        print(f"Hierarchy items: {len(result.get('hierarchy', []))}")
        return True
    except Exception as e:
        print(f"[FAIL] Context inspect failed: {e}")
        return False


async def test_scene_operations():
    """Test scene CRUD operations"""
    print("\n=== Testing unity.scene.crud ===")
    try:
        # List scenes
        result = await bridge_manager.send_command("sceneCrud", {
            "operation": "list"
        })
        print(f"[OK] Scene list successful: {len(result.get('scenes', []))} scenes found")
        return True
    except Exception as e:
        print(f"[FAIL] Scene operations failed: {e}")
        return False


async def test_quick_setup():
    """Test quick scene setup"""
    print("\n=== Testing unity.scene.quickSetup ===")
    try:
        result = await bridge_manager.send_command("sceneQuickSetup", {
            "setupType": "3D"
        })
        print(f"[OK] Quick setup successful")
        return True
    except Exception as e:
        print(f"[FAIL] Quick setup failed: {e}")
        return False


async def run_tests():
    """Run all tests"""
    print("Starting SkillForUnity tool tests...")
    print(f"Connecting to Unity Bridge at ws://127.0.0.1:7070/bridge")

    # Start bridge connector
    bridge_connector.start()

    # Wait for connection
    await asyncio.sleep(2)

    if not bridge_manager.is_connected():
        print("[FAIL] Failed to connect to Unity Bridge")
        print("Make sure Unity Editor is open and MCP Bridge is started (Tools > MCP Assistant)")
        return

    print("[OK] Connected to Unity Bridge")

    # Run tests
    tests = [
        ("Ping", test_ping),
        ("Context Inspect", test_context_inspect),
        ("Scene Operations", test_scene_operations),
        ("Quick Setup", test_quick_setup),
    ]

    results = {}
    for name, test_func in tests:
        try:
            results[name] = await test_func()
        except Exception as e:
            print(f"[FAIL] Test '{name}' crashed: {e}")
            results[name] = False

    # Summary
    print("\n" + "=" * 50)
    print("TEST SUMMARY")
    print("=" * 50)
    passed = sum(1 for r in results.values() if r)
    total = len(results)
    print(f"Passed: {passed}/{total}")
    for name, result in results.items():
        status = "[OK]" if result else "[FAIL]"
        print(f"{status} {name}")

    # Stop bridge connector
    await bridge_connector.stop()


if __name__ == "__main__":
    asyncio.run(run_tests())
