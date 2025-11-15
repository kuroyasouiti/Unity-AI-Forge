#!/usr/bin/env python3
"""
Remove VerticalLayoutGroup from Canvas and test RectTransform update
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
        hello_msg = {"type": "hello", "version": "1.0.0", "client": "test-client"}
        await self.websocket.send(json.dumps(hello_msg))
        response = await self.websocket.recv()
        print(f"[CONNECTED] {json.loads(response)['unityVersion']}")

    async def execute_command(self, tool_name, payload):
        command_id = str(uuid.uuid4())
        cmd = {"type": "command:execute", "commandId": command_id, "toolName": tool_name, "payload": payload}
        await self.websocket.send(json.dumps(cmd))

        timeout_count = 0
        while timeout_count < 20:
            try:
                response = await asyncio.wait_for(self.websocket.recv(), timeout=2.0)
                response_clean = response.replace('NaN', 'null').replace('Infinity', 'null').replace('-Infinity', 'null')
                data = json.loads(response_clean)
                if data.get("type") in ["context:update", "heartbeat"]:
                    continue
                if data.get("type") == "command:result" and data.get("commandId") == command_id:
                    return data.get("result")
                elif data.get("type") == "command:error" and data.get("commandId") == command_id:
                    raise Exception(f"Command error: {data.get('error')}")
            except asyncio.TimeoutError:
                timeout_count += 1
            except json.JSONDecodeError:
                continue
        raise Exception(f"Command timeout")

    async def close(self):
        if self.websocket:
            await self.websocket.close()

async def main():
    client = UnityBridgeClient()

    try:
        await client.connect()

        print("\n" + "="*60)
        print("Step 1: Remove VerticalLayoutGroup from Canvas")
        print("="*60)

        try:
            remove_result = await client.execute_command("componentManage", {
                "operation": "remove",
                "gameObjectPath": "Canvas",
                "componentType": "UnityEngine.UI.VerticalLayoutGroup"
            })
            print(f"[REMOVE] VerticalLayoutGroup removed: {remove_result.get('success', False)}")
        except Exception as e:
            print(f"[INFO] VerticalLayoutGroup might not exist (or already removed): {e}")

        print("\n" + "="*60)
        print("Step 2: Test RectTransform update on TestButton")
        print("="*60)

        # Inspect before
        inspect1 = await client.execute_command("componentManage", {
            "operation": "inspect",
            "gameObjectPath": "Canvas/TestButton",
            "componentType": "UnityEngine.RectTransform",
            "propertyFilter": ["anchoredPosition", "sizeDelta"]
        })

        print(f"\n[BEFORE] TestButton RectTransform:")
        for key, value in inspect1.get("properties", {}).items():
            print(f"  {key}: {value}")

        # Update
        print(f"\n[UPDATE] Setting anchoredPosition to (250, -120), sizeDelta to (350, 90)...")
        update_result = await client.execute_command("componentManage", {
            "operation": "update",
            "gameObjectPath": "Canvas/TestButton",
            "componentType": "UnityEngine.RectTransform",
            "propertyChanges": {
                "anchoredPosition": {"x": 250, "y": -120},
                "sizeDelta": {"x": 350, "y": 90}
            }
        })

        print(f"[UPDATE] Success: {update_result.get('success', False)}")

        # Inspect after
        inspect2 = await client.execute_command("componentManage", {
            "operation": "inspect",
            "gameObjectPath": "Canvas/TestButton",
            "componentType": "UnityEngine.RectTransform",
            "propertyFilter": ["anchoredPosition", "sizeDelta"]
        })

        print(f"\n[AFTER] TestButton RectTransform:")
        for key, value in inspect2.get("properties", {}).items():
            print(f"  {key}: {value}")

        expected_pos = {"x": 250, "y": -120}
        expected_size = {"x": 350, "y": 90}
        actual_pos = inspect2.get("properties", {}).get("anchoredPosition")
        actual_size = inspect2.get("properties", {}).get("sizeDelta")

        print("\n" + "="*60)
        if actual_pos == expected_pos and actual_size == expected_size:
            print("[SUCCESS] RectTransform update worked correctly!")
            print("="*60)
        else:
            print(f"[FAILED] Expected pos={expected_pos}, size={expected_size}")
            print(f"[FAILED] Got pos={actual_pos}, size={actual_size}")
            print("="*60)

    except Exception as e:
        print(f"\n[ERROR] {e}")
        import traceback
        traceback.print_exc()
    finally:
        await client.close()

if __name__ == "__main__":
    asyncio.run(main())
