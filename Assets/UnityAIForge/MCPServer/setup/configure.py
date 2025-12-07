#!/usr/bin/env python3
"""
Unity MCP Skill - Configuration Helper
Generates MCP client configuration files based on user input
"""

import json
import os
import sys
from pathlib import Path
from typing import Any, Dict


def get_root_dir() -> Path:
    """Get the root directory of the skill package"""
    return Path(__file__).parent.parent


def get_config_dir() -> Path:
    """Get the config directory"""
    return get_root_dir() / "config"


def get_src_dir() -> Path:
    """Get the src directory"""
    return get_root_dir() / "src"


def prompt(message: str, default: str = "") -> str:
    """Prompt user for input with optional default"""
    if default:
        message = f"{message} [{default}]"
    result = input(f"{message}: ").strip()
    return result if result else default


def generate_mcp_config() -> Dict[str, Any]:
    """Generate generic MCP client configuration"""
    print("\n📝 Generating MCP Configuration\n")

    root_dir = get_root_dir()
    src_dir = get_src_dir()
    main_py = src_dir / "main.py"

    # Determine if using uv or python
    use_uv = prompt("Use 'uv' to run the server? (y/n)", "y").lower() == "y"

    if use_uv:
        command = "uv"
        args = ["run", "--directory", str(src_dir.parent), "src/main.py"]
    else:
        command = sys.executable or "python3"
        args = [str(main_py)]

    config = {
        "mcpServers": {
            "unity-mcp": {
                "command": command,
                "args": args,
                "env": {
                    "MCP_SERVER_TRANSPORT": "stdio",
                    "MCP_LOG_LEVEL": "info",
                    "MCP_BRIDGE_HOST": "localhost",
                    "MCP_BRIDGE_PORT": "7077",
                },
            }
        }
    }

    return config


def generate_claude_desktop_config() -> Dict[str, Any]:
    """Generate Claude Desktop specific configuration"""
    root_dir = get_root_dir()
    src_dir = get_src_dir()

    config = {
        "mcpServers": {
            "unity-mcp": {
                "command": "uv",
                "args": ["run", "--directory", str(src_dir.parent), "src/main.py"],
                "env": {
                    "MCP_SERVER_TRANSPORT": "stdio",
                    "MCP_LOG_LEVEL": "info",
                },
            }
        }
    }

    return config


def generate_cursor_config() -> Dict[str, Any]:
    """Generate Cursor IDE specific configuration"""
    root_dir = get_root_dir()
    src_dir = get_src_dir()

    config = {
        "mcp": {
            "servers": {
                "unity-mcp": {
                    "command": "uv",
                    "args": ["run", "--directory", str(src_dir.parent), "src/main.py"],
                    "env": {
                        "MCP_SERVER_TRANSPORT": "stdio",
                        "MCP_LOG_LEVEL": "info",
                    },
                }
            }
        }
    }

    return config


def generate_env_file() -> str:
    """Generate .env file content"""
    return """# Unity MCP Skill - Environment Configuration

# MCP Server Settings
MCP_SERVER_TRANSPORT=stdio
MCP_LOG_LEVEL=info

# Unity Bridge Settings
MCP_BRIDGE_HOST=localhost
MCP_BRIDGE_PORT=7077
MCP_BRIDGE_PATH=/bridge

# Server Settings (for websocket transport)
MCP_SERVER_HOST=127.0.0.1
MCP_SERVER_PORT=7070
"""


def save_config(filename: str, content: Dict[str, Any] | str) -> None:
    """Save configuration to file"""
    config_dir = get_config_dir()
    filepath = config_dir / filename

    try:
        with open(filepath, "w", encoding="utf-8") as f:
            if isinstance(content, dict):
                json.dump(content, f, indent=2)
            else:
                f.write(content)
        print(f"   ✓ Generated: {filename}")
    except Exception as e:
        print(f"   ❌ Failed to generate {filename}: {e}")


def main() -> None:
    """Main configuration generation"""
    print("\n⚙️  Unity MCP Skill - Configuration Generator")
    print("=" * 50)

    # Check if config directory exists
    config_dir = get_config_dir()
    if not config_dir.exists():
        print(f"\n❌ Config directory not found: {config_dir}")
        return

    print("\nGenerating configuration files...\n")

    # Generate configurations
    mcp_config = generate_mcp_config()
    claude_config = generate_claude_desktop_config()
    cursor_config = generate_cursor_config()
    env_content = generate_env_file()

    # Save configurations
    save_config("mcp-config.json", mcp_config)
    save_config("claude-desktop.json", claude_config)
    save_config("cursor.json", cursor_config)
    save_config(".env", env_content)

    print("\n✅ Configuration files generated successfully!")
    print("\n📝 Configuration files created in config/ directory:")
    print("   • mcp-config.json - Generic MCP client configuration")
    print("   • claude-desktop.json - Claude Desktop configuration")
    print("   • cursor.json - Cursor IDE configuration")
    print("   • .env - Environment variables")
    print("\n💡 Copy the appropriate configuration to your MCP client")


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\n❌ Configuration cancelled by user")
        sys.exit(1)
    except Exception as e:
        print(f"\n\n❌ Configuration failed: {e}")
        sys.exit(1)
