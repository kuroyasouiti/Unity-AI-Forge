#!/usr/bin/env python3
"""
Check if Canvas has Layout components that interfere with RectTransform updates
"""
import asyncio
import json
import websockets
import uuid

class UnityBridgeClient:
    def __init__(self, uri="ws://127.0.0.1:7070/bridge"):
        self.uri = uri
        self.websocket = None

    async def connect(self):
        self.websocket = await websockets.connect(self.uri)
        hello_msg = {
            "type": "hello",
            "version": "1.0.0",
            "client": "test-client"
        }
        await self.websocket.send(json.dumps(hello_msg))
        response = await self.websocket.recv()
        print(f"[CONNECTED] {json.loads(response)['unityVersion']}")

    async def execute_command(self, tool_name, payload):
        command_id = str(uuid.uuid4())
        cmd = {
            "type": "command:execute",
            "commandId": command_id,
            "toolName": tool_name,
            "payload": payload
        }
        await self.websocket.send(json.dumps(cmd))

        timeout_count = 0
        max_timeout = 20
        while timeout_count < max_timeout:
            try:
                response = await asyncio.wait_for(self.websocket.recv(), timeout=2.0)
            except asyncio.TimeoutError:
                timeout_count += 1
                continue

            try:
                response_clean = response.replace('NaN', 'null').replace('Infinity', 'null').replace('-Infinity', 'null')
                data = json.loads(response_clean)
            except json.JSONDecodeError:
                continue

            if data.get("type") in ["context:update", "heartbeat"]:
                continue

            if data.get("type") == "command:result" and data.get("commandId") == command_id:
                return data.get("result")
            elif data.get("type") == "command:error" and data.get("commandId") == command_id:
                raise Exception(f"Command error: {data.get('error')}")

        raise Exception(f"Command timeout after {max_timeout} attempts")

    async def close(self):
        if self.websocket:
            await self.websocket.close()

async def main():
    client = UnityBridgeClient()

    try:
        await client.connect()

        print("\n" + "="*60)
        print("Inspecting Canvas components")
        print("="*60)

        # Inspect Canvas GameObject
        canvas_inspect = await client.execute_command("gameObjectManage", {
            "operation": "inspect",
            "gameObjectPath": "Canvas"
        })

        print(f"\n[Canvas] Components:")
        for comp in canvas_inspect.get("components", []):
            print(f"  - {comp}")

        # Check if Canvas has Layout components
        layout_components = [
            "UnityEngine.UI.VerticalLayoutGroup",
            "UnityEngine.UI.HorizontalLayoutGroup",
            "UnityEngine.UI.GridLayoutGroup"
        ]

        has_layout = False
        for layout_type in layout_components:
            if layout_type in canvas_inspect.get("components", []):
                print(f"\n[WARNING] Canvas has {layout_type}!")
                has_layout = True

                # Inspect the layout component
                layout_info = await client.execute_command("componentManage", {
                    "operation": "inspect",
                    "gameObjectPath": "Canvas",
                    "componentType": layout_type,
                    "includeProperties": False
                })
                print(f"[INFO] Layout component found: {layout_info}")

        if not has_layout:
            print("\n[OK] Canvas has no Layout components")
        else:
            print("\n" + "="*60)
            print("Testing: Remove Layout component from Canvas")
            print("="*60)

            for layout_type in layout_components:
                if layout_type in canvas_inspect.get("components", []):
                    print(f"\n[ACTION] Removing {layout_type} from Canvas...")

                    remove_result = await client.execute_command("componentManage", {
                        "operation": "remove",
                        "gameObjectPath": "Canvas",
                        "componentType": layout_type
                    })

                    print(f"[RESULT] Removed: {remove_result.get('success', False)}")

            # Verify removal
            verify_inspect = await client.execute_command("gameObjectManage", {
                "operation": "inspect",
                "gameObjectPath": "Canvas"
            })

            print(f"\n[Canvas after cleanup] Components:")
            for comp in verify_inspect.get("components", []):
                print(f"  - {comp}")

        print("\n" + "="*60)
        print("Testing: Create TestPanel in Canvas (without Layout)")
        print("="*60)

        # Create test panel
        create_result = await client.execute_command("uguiCreateFromTemplate", {
            "template": "Panel",
            "name": "TestPanel_NoLayout",
            "parentPath": "Canvas",
            "width": 200,
            "height": 150
        })
        print(f"[CREATE] Panel: {create_result.get('gameObjectPath')}")

        # Update RectTransform
        print("\n[UPDATE] Setting anchoredPosition to (100, -50)...")
        update_result = await client.execute_command("componentManage", {
            "operation": "update",
            "gameObjectPath": "Canvas/TestPanel_NoLayout",
            "componentType": "UnityEngine.RectTransform",
            "propertyChanges": {
                "anchoredPosition": {"x": 100, "y": -50},
                "sizeDelta": {"x": 300, "y": 200}
            }
        })

        # Verify update
        verify_result = await client.execute_command("componentManage", {
            "operation": "inspect",
            "gameObjectPath": "Canvas/TestPanel_NoLayout",
            "componentType": "UnityEngine.RectTransform",
            "propertyFilter": ["anchoredPosition", "sizeDelta"]
        })

        print(f"\n[VERIFY] RectTransform properties after update:")
        for key, value in verify_result.get("properties", {}).items():
            print(f"  {key}: {value}")

        expected_pos = {"x": 100, "y": -50}
        expected_size = {"x": 300, "y": 200}
        actual_pos = verify_result.get("properties", {}).get("anchoredPosition")
        actual_size = verify_result.get("properties", {}).get("sizeDelta")

        if actual_pos == expected_pos and actual_size == expected_size:
            print("\n[SUCCESS] RectTransform update worked correctly!")
        else:
            print(f"\n[FAILED] Expected pos={expected_pos}, size={expected_size}")
            print(f"[FAILED] Got pos={actual_pos}, size={actual_size}")

        # Cleanup
        print("\n[CLEANUP] Deleting TestPanel_NoLayout...")
        delete_result = await client.execute_command("gameObjectManage", {
            "operation": "delete",
            "gameObjectPath": "Canvas/TestPanel_NoLayout"
        })
        print(f"[CLEANUP] Deleted: {delete_result.get('success', False)}")

    except Exception as e:
        print(f"\n[ERROR] {e}")
        import traceback
        traceback.print_exc()
    finally:
        await client.close()

if __name__ == "__main__":
    asyncio.run(main())
