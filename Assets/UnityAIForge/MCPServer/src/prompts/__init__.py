"""System prompts and MCP prompt templates for Unity-AI-Forge MCP Server.

This module provides functions to load system instructions from external
markdown files and register MCP prompt handlers for game development guides.
"""

from prompts.loader import load_system_prompt
from prompts.register_prompts import register_prompts

__all__ = ["load_system_prompt", "register_prompts"]
