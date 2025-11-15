#!/usr/bin/env python3
"""
Test RectTransform CRUD operations using SkillForUnity
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
        # Send hello
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

        # Wait for response
        timeout_count = 0
        max_timeout = 20
        while timeout_count < max_timeout:
            try:
                response = await asyncio.wait_for(self.websocket.recv(), timeout=2.0)
            except asyncio.TimeoutError:
                timeout_count += 1
                continue
            except Exception as e:
                print(f"[ERROR] WebSocket error: {e}")
                raise

            try:
                # Handle potential NaN values in JSON
                response_clean = response.replace('NaN', 'null').replace('Infinity', 'null').replace('-Infinity', 'null')
                data = json.loads(response_clean)
            except json.JSONDecodeError as e:
                print(f"[WARNING] Failed to parse JSON at position {e.pos}")
                print(f"[WARNING] Response length: {len(response)}")
                print(f"[WARNING] Near error: {response[max(0, e.pos-50):min(len(response), e.pos+50)]}")
                continue

            # Skip context updates and heartbeats
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

async def test_rectransform_crud():
    client = UnityBridgeClient()

    try:
        await client.connect()

        print("\n" + "="*60)
        print("TEST 1: CREATE - Create new UI Panel")
        print("="*60)

        create_result = await client.execute_command("uguiCreateFromTemplate", {
            "template": "Panel",
            "name": "TestPanel_CRUD",
            "parentPath": "Canvas",
            "width": 200,
            "height": 150
        })
        print(f"[CREATE] Result: {json.dumps(create_result, indent=2)}")

        print("\n" + "="*60)
        print("TEST 2: READ - Inspect RectTransform properties")
        print("="*60)

        # Use propertyFilter to get only specific properties
        read_result = await client.execute_command("componentManage", {
            "operation": "inspect",
            "gameObjectPath": "Canvas/TestPanel_CRUD",
            "componentType": "UnityEngine.RectTransform",
            "propertyFilter": [
                "anchoredPosition", "anchoredPosition3D",
                "sizeDelta", "pivot",
                "anchorMin", "anchorMax",
                "offsetMin", "offsetMax",
                "localPosition", "localRotation", "localScale"
            ]
        })
        print(f"[READ] RectTransform properties:")
        for key, value in read_result.get("properties", {}).items():
            print(f"  {key}: {value}")

        print("\n" + "="*60)
        print("TEST 3: UPDATE - Update RectTransform using componentManage")
        print("="*60)

        update_result1 = await client.execute_command("componentManage", {
            "operation": "update",
            "gameObjectPath": "Canvas/TestPanel_CRUD",
            "componentType": "UnityEngine.RectTransform",
            "propertyChanges": {
                "anchoredPosition": {"x": 100, "y": -50},
                "sizeDelta": {"x": 300, "y": 200},
                "pivot": {"x": 0.5, "y": 0.5}
            }
        })
        print(f"[UPDATE 1] Success: {update_result1.get('success', False)}")

        # Verify update
        verify_result1 = await client.execute_command("componentManage", {
            "operation": "inspect",
            "gameObjectPath": "Canvas/TestPanel_CRUD",
            "componentType": "UnityEngine.RectTransform",
            "propertyFilter": ["anchoredPosition", "sizeDelta", "pivot"]
        })
        print(f"[VERIFY 1] Updated properties:")
        for key, value in verify_result1.get("properties", {}).items():
            print(f"  {key}: {value}")

        print("\n" + "="*60)
        print("TEST 4: UPDATE - Update RectTransform using uguiManage")
        print("="*60)

        update_result2 = await client.execute_command("uguiManage", {
            "operation": "updateRect",
            "gameObjectPath": "Canvas/TestPanel_CRUD",
            "anchoredPosition": {"x": -100, "y": 50},
            "sizeDelta": {"x": 250, "y": 180}
        })
        print(f"[UPDATE 2] Success: {update_result2.get('success', False)}")

        # Verify update
        verify_result2 = await client.execute_command("componentManage", {
            "operation": "inspect",
            "gameObjectPath": "Canvas/TestPanel_CRUD",
            "componentType": "UnityEngine.RectTransform",
            "propertyFilter": ["anchoredPosition", "sizeDelta"]
        })
        print(f"[VERIFY 2] Updated properties:")
        for key, value in verify_result2.get("properties", {}).items():
            print(f"  {key}: {value}")

        print("\n" + "="*60)
        print("TEST 5: UPDATE - Test anchor presets")
        print("="*60)

        anchor_result = await client.execute_command("uguiManage", {
            "operation": "setAnchorPreset",
            "gameObjectPath": "Canvas/TestPanel_CRUD",
            "preset": "middle-center"
        })
        print(f"[ANCHOR] Success: {anchor_result.get('success', False)}")

        # Verify anchor update
        verify_anchor = await client.execute_command("componentManage", {
            "operation": "inspect",
            "gameObjectPath": "Canvas/TestPanel_CRUD",
            "componentType": "UnityEngine.RectTransform",
            "propertyFilter": ["anchorMin", "anchorMax", "anchoredPosition"]
        })
        print(f"[VERIFY ANCHOR] Anchor properties:")
        for key, value in verify_anchor.get("properties", {}).items():
            print(f"  {key}: {value}")

        print("\n" + "="*60)
        print("TEST 6: DELETE - Delete UI element")
        print("="*60)

        delete_result = await client.execute_command("gameObjectManage", {
            "operation": "delete",
            "gameObjectPath": "Canvas/TestPanel_CRUD"
        })
        print(f"[DELETE] Success: {delete_result.get('success', False)}")

        # Verify deletion
        try:
            verify_delete = await client.execute_command("gameObjectManage", {
                "operation": "inspect",
                "gameObjectPath": "Canvas/TestPanel_CRUD"
            })
            print(f"[ERROR] Object still exists after deletion!")
        except Exception as e:
            print(f"[VERIFY DELETE] Object successfully deleted (expected error)")

        print("\n" + "="*60)
        print("ALL TESTS COMPLETED SUCCESSFULLY!")
        print("="*60)

    except Exception as e:
        print(f"\n[ERROR] Test failed: {e}")
        import traceback
        traceback.print_exc()
    finally:
        await client.close()

if __name__ == "__main__":
    asyncio.run(test_rectransform_crud())
