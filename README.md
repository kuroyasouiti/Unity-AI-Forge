# Unity MCP Server

The MCP server connects to the Unity Editor bridge over WebSocket (Unity hosts the bridge listener). MCP-compatible clients should connect to `ws://<host>:<port>/mcp`.

## Setup

```powershell
cd mcp-server
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -e .
Copy-Item .env.example .env
# edit .env as needed
```

## Run

```powershell
uvicorn mcp_server.main:app --host %MCP_SERVER_HOST% --port %MCP_SERVER_PORT%
```

Alternatively, launch through the console script:

```powershell
unity-mcp-server
```

## Environment Variables

- `MCP_SERVER_PORT`: HTTP/WebSocket port exposed to MCP clients (default 6006)
- `MCP_SERVER_HOST`: Interface to listen on for MCP clients
- `UNITY_PROJECT_ROOT`: Absolute or relative path to the Unity project root
- `UNITY_EDITOR_LOG_PATH`: Optional override for the Unity Editor log
- `MCP_ENABLE_FILE_WATCHER`: Enable the editor log poller (`1` or `0`)
- `MCP_SERVER_LOG_LEVEL`: Log level for the server
- `MCP_BRIDGE_TOKEN`: Optional shared secret that must match the Unity bridge token
- `UNITY_BRIDGE_HOST`: Hostname/IP of the Unity Editor bridge listener
- `UNITY_BRIDGE_PORT`: Port exposed by the Unity Editor bridge listener
- `MCP_BRIDGE_RECONNECT_MS`: Delay before attempting to reconnect to the bridge (ms)

## Available Resources

- `unity://project/structure`
- `unity://editor/log`
- `unity://scene/active`
- `unity://asset/{guid}`

## Available Tools

- `unity.ping`
- `unity.scene.manage`
- `unity_gameobject_manage`
- `unity.component.manage`
- `unity.asset.manage`
- `unity.ugui.rectAdjust`
- `unity.script.outline`
