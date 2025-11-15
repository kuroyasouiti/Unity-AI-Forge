#!/usr/bin/env python3
"""
Detailed inspection of Canvas and TestButton hierarchy
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
        print("Inspecting Canvas with includeProperties=True")
        print("="*60)

        # Get full context
        context = await client.execute_command("contextInspect", {
            "includeHierarchy": True,
            "includeComponents": True,
            "maxDepth": 2
        })

        # Find Canvas in hierarchy
        hierarchy = context.get("hierarchy", {})
        canvas_found = False

        def find_canvas(node, path=""):
            nonlocal canvas_found
            if not isinstance(node, dict):
                return

            if node.get("name") == "Canvas":
                canvas_found = True
                print(f"\n[Canvas] Found at: {path}/Canvas")
                print(f"  Type: {node.get('type')}")
                print(f"  Components:")
                for comp in node.get("components", []):
                    if isinstance(comp, dict):
                        comp_type = comp.get("type", "Unknown")
                        enabled = comp.get("enabled")
                        print(f"    - {comp_type} (enabled={enabled})")
                    else:
                        print(f"    - {comp}")

                # Find TestButton
                for child in node.get("children", []):
                    if child.get("name") == "TestButton":
                        print(f"\n[TestButton] Found")
                        print(f"  Type: {child.get('type')}")
                        print(f"  Components:")
                        for comp in child.get("components", []):
                            if isinstance(comp, dict):
                                comp_type = comp.get("type", "Unknown")
                                enabled = comp.get("enabled")
                                print(f"    - {comp_type} (enabled={enabled})")
                            else:
                                print(f"    - {comp}")
                        break

            for child in node.get("children", []):
                find_canvas(child, f"{path}/{node.get('name', 'root')}")

        if isinstance(hierarchy, dict):
            find_canvas(hierarchy)
        elif isinstance(hierarchy, list):
            for item in hierarchy:
                find_canvas(item)

        if not canvas_found:
            print("\n[ERROR] Canvas not found in hierarchy!")

        # Also try direct gameObject inspect
        print("\n" + "="*60)
        print("Direct gameObjectManage inspect")
        print("="*60)

        try:
            canvas_inspect = await client.execute_command("gameObjectManage", {
                "operation": "inspect",
                "gameObjectPath": "Canvas"
            })
            print(f"\n[Canvas via gameObjectManage]:")
            print(f"  Name: {canvas_inspect.get('name')}")
            print(f"  Path: {canvas_inspect.get('path')}")
            print(f"  Components: {canvas_inspect.get('components', [])}")
        except Exception as e:
            print(f"[ERROR] Failed to inspect Canvas: {e}")

    except Exception as e:
        print(f"\n[ERROR] {e}")
        import traceback
        traceback.print_exc()
    finally:
        await client.close()

if __name__ == "__main__":
    asyncio.run(main())
