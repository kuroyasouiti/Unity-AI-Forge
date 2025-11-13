# MCP Server & Unity Bridge Setup

## Prerequisites

- Python 3.10 or later
- Unity Editor 2022.3 or later
- Git (optional, for diff summaries)

## Server Installation

1. `cd mcp-server`
2. `python -m venv .venv`
3. Activate the virtual environment  
   - Windows: `.venv\Scripts\Activate.ps1`  
   - macOS/Linux: `source .venv/bin/activate`
4. `python -m pip install -e .`
5. Copy `.env.example` to `.env` and adjust paths/hosts as needed

To run the server manually: `uvicorn mcp_server.main:app --host %MCP_SERVER_HOST% --port %MCP_SERVER_PORT%`  
Alternatively use the console script: `unity-mcp-server`

The server exposes MCP resources/tools on ws://<host>:<port>/mcp and dials into the Unity Editor bridge using the UNITY_BRIDGE_HOST and UNITY_BRIDGE_PORT settings.

## Unity Bridge Configuration

1. Open the Unity project in the Editor
2. Navigate to **Tools → MCP Assistant**
3. Set the listener host/port (must match the values in .env)
4. Optionally enable *Auto start on load*
5. Press **Start Bridge** (or rely on auto start)

## Assistant Window Highlights

- Configure the bridge listener, secret token, and context streaming interval
- Change the Python server install path (and restore the default) before running commands
- Install/verify the server template directly from Unity
- Register or remove the server via CLI helpers for Claude Code, Claude Desktop, Codex CLI, Gemini CLI, and Cursor CLI
- Manually push Unity context snapshots or emit bridge heartbeats on demand

## Streaming Context

When the MCP server connects, the bridge streams:

- Active scene hierarchy and current selection
- Indexed assets (scripts, prefabs, materials, scenes, shaders, ScriptableObjects)
- Git status summary (when the project is a Git repo)
- Unity Editor log tail (via the server-side watcher)

## Supported Tooling

- Scene Manage (create, load, save, delete, duplicate)
- GameObject Manage (create/move/delete/rename/duplicate GameObjects)
- Component Manage (add/remove/update components via reflection)
- Asset Manage (text asset create/update/delete/rename/duplicate)
- UGUI rect adjustment to align RectTransforms with pixel bounds
- Script read (outline + source) with brace-balance syntax check
- Bridge ping for health checks

All destructive actions surface inside the Unity Editor before they are persisted to source control.

