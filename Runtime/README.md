# Unity MCP Server (Python)

This Python implementation exposes the Unity bridge to Model Context Protocol (MCP) clients over HTTP and WebSocket.

## Setup

```powershell
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -e .
Copy-Item .env.example .env
# edit .env as needed
```

## Run

```powershell
uv run unity-mcp-server
```

Alternatively start the Starlette application directly with uvicorn:

```powershell
uvicorn mcp_server.main:app --host %MCP_SERVER_HOST% --port %MCP_SERVER_PORT%
```

## Environment Variables

- `MCP_SERVER_PORT`: HTTP/WebSocket port exposed to MCP clients (default 6006)
- `MCP_SERVER_HOST`: Interface to listen on for MCP clients
- `UNITY_PROJECT_ROOT`: Absolute or relative path to the Unity project root
- `UNITY_EDITOR_LOG_PATH`: Optional override for the Unity Editor log
- `MCP_ENABLE_FILE_WATCHER`: Enable the editor log poller (`1` or `0`)
- `MCP_SERVER_LOG_LEVEL`: Log level (`fatal`, `error`, `warn`, `info`, `debug`, `trace`)
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
- `unity.scene.crud`
- `unity.hierarchy.crud`
- `unity.component.crud`
- `unity.asset.crud`
- `unity.ugui.rectAdjust`
- `unity.script.outline`
