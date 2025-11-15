#!/usr/bin/env python3
"""
Test updating RectTransform on an EXISTING GameObject (not just created)
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
        print("Testing: Update EXISTING GameObject (TestButton)")
        print("="*60)

        # Inspect existing TestButton
        inspect1 = await client.execute_command("componentManage", {
            "operation": "inspect",
            "gameObjectPath": "Canvas/TestButton",
            "componentType": "UnityEngine.RectTransform",
            "propertyFilter": ["anchoredPosition", "sizeDelta"]
        })

        print(f"\n[BEFORE UPDATE] TestButton RectTransform:")
        for key, value in inspect1.get("properties", {}).items():
            print(f"  {key}: {value}")

        # Update TestButton
        print(f"\n[UPDATE] Setting anchoredPosition to (200, -100), sizeDelta to (400, 80)...")
        update_result = await client.execute_command("componentManage", {
            "operation": "update",
            "gameObjectPath": "Canvas/TestButton",
            "componentType": "UnityEngine.RectTransform",
            "propertyChanges": {
                "anchoredPosition": {"x": 200, "y": -100},
                "sizeDelta": {"x": 400, "y": 80}
            }
        })

        print(f"[UPDATE] Success: {update_result.get('success', False)}")

        # Verify update
        inspect2 = await client.execute_command("componentManage", {
            "operation": "inspect",
            "gameObjectPath": "Canvas/TestButton",
            "componentType": "UnityEngine.RectTransform",
            "propertyFilter": ["anchoredPosition", "sizeDelta"]
        })

        print(f"\n[AFTER UPDATE] TestButton RectTransform:")
        for key, value in inspect2.get("properties", {}).items():
            print(f"  {key}: {value}")

        expected_pos = {"x": 200, "y": -100}
        expected_size = {"x": 400, "y": 80}
        actual_pos = inspect2.get("properties", {}).get("anchoredPosition")
        actual_size = inspect2.get("properties", {}).get("sizeDelta")

        if actual_pos == expected_pos and actual_size == expected_size:
            print("\n[SUCCESS] RectTransform update worked on existing object!")
        else:
            print(f"\n[FAILED] Expected pos={expected_pos}, size={expected_size}")
            print(f"[FAILED] Got pos={actual_pos}, size={actual_size}")

    except Exception as e:
        print(f"\n[ERROR] {e}")
        import traceback
        traceback.print_exc()
    finally:
        await client.close()

if __name__ == "__main__":
    asyncio.run(main())
