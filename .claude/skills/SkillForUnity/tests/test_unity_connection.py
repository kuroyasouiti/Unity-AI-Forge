#!/usr/bin/env python3
"""
Simple test script to check Unity MCP Bridge connection
"""
import asyncio
import json
import websockets

async def test_unity_connection():
    uri = "ws://127.0.0.1:7070/bridge"

    try:
        print(f"Connecting to {uri}...")
        async with websockets.connect(uri) as websocket:
            print("[OK] Connected to Unity MCP Bridge!")

            # Send hello message
            hello_msg = {
                "type": "hello",
                "version": "1.0.0",
                "client": "test-client"
            }
            await websocket.send(json.dumps(hello_msg))
            print(f"[SENT] {hello_msg}")

            # Wait for response
            response = await websocket.recv()
            print(f"[RECV] {response}")

            # Send ping command
            ping_cmd = {
                "type": "command:execute",
                "commandId": "test-001",
                "toolName": "ping",
                "payload": {}
            }
            await websocket.send(json.dumps(ping_cmd))
            print(f"[SENT] Ping command")

            # Wait for ping response
            ping_response = await websocket.recv()
            print(f"[RECV] Ping response: {ping_response}")

            return True

    except ConnectionRefusedError:
        print("[ERROR] Connection refused - Unity MCP Bridge is not running")
        print("        Please start Unity Editor and go to Tools > MCP Assistant > Start Bridge")
        return False
    except Exception as e:
        print(f"[ERROR] {e}")
        return False

if __name__ == "__main__":
    result = asyncio.run(test_unity_connection())
    exit(0 if result else 1)
