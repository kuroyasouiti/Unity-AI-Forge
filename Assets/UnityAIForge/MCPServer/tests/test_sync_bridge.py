"""Sync tests: verify Python MCP Server definitions match C# MCP Bridge source.

These tests parse C# source files with regex to extract handler registrations
and SupportedOperations, then compare against the Python-side definitions in
``tool_registry`` and ``tool_definitions``.
"""

from __future__ import annotations

import re
from pathlib import Path

import pytest

from tools.tool_definitions import get_tool_definitions
from tools.tool_registry import TOOL_NAME_TO_BRIDGE

# ── Path constants ───────────────────────────────────────────────────────

_HANDLER_DIR = (
    Path(__file__).resolve().parents[2] / "Editor" / "MCPBridge" / "Handlers"
)
_INITIALIZER_PATH = (
    Path(__file__).resolve().parents[2]
    / "Editor"
    / "MCPBridge"
    / "Base"
    / "CommandHandlerInitializer.cs"
)

# ── Helper functions ─────────────────────────────────────────────────────


def parse_registered_bridge_names(initializer_path: Path) -> dict[str, str]:
    """Parse ``CommandHandlerInitializer.cs`` for handler registrations.

    Returns:
        ``{bridge_name: class_name}`` extracted from
        ``CommandHandlerFactory.Register("bridgeName", new ClassName());``
    """
    text = initializer_path.read_text(encoding="utf-8")
    # Match: Register("bridgeName", new Namespace.ClassName())
    pattern = re.compile(
        r'Register\(\s*"([^"]+)"\s*,\s*new\s+([\w.]+)\s*\(\s*\)\s*\)',
    )
    result: dict[str, str] = {}
    for m in pattern.finditer(text):
        bridge_name = m.group(1)
        full_class = m.group(2)
        # Extract simple class name (last segment after dots)
        class_name = full_class.rsplit(".", 1)[-1]
        result[bridge_name] = class_name
    return result


def parse_handler_operations(handler_dir: Path) -> dict[str, list[str]]:
    """Parse C# handler files for SupportedOperations.

    Supports two declaration patterns:

    - **Pattern A** (inline)::

        SupportedOperations => new[] { "op1", "op2" };

    - **Pattern B** (static field)::

        static readonly string[] Operations = { "op1", "op2" };
        ...
        SupportedOperations => Operations;

    Returns:
        ``{class_name: [operations]}``
    """
    result: dict[str, list[str]] = {}

    for cs_file in handler_dir.rglob("*.cs"):
        text = cs_file.read_text(encoding="utf-8")

        # Extract class name
        class_match = re.search(r"class\s+(\w+)\s*:", text)
        if not class_match:
            continue
        class_name = class_match.group(1)

        ops = _extract_operations(text)
        if ops is not None:
            result[class_name] = ops

    return result


def _extract_operations(text: str) -> list[str] | None:
    """Extract operation strings from handler source text."""
    # Pattern A: SupportedOperations => new[] { "op1", "op2", ... };
    # Match across multiple lines from SupportedOperations to the closing };
    pattern_a = re.compile(
        r"SupportedOperations\s*=>\s*new\s*\[\s*\]\s*\{([^}]+)\}",
        re.DOTALL,
    )
    m = pattern_a.search(text)
    if m:
        return _extract_strings(m.group(1))

    # Pattern B: static readonly string[] Xxx = { "op1", ... };
    #            SupportedOperations => Xxx;
    # First find what identifier SupportedOperations points to
    ref_match = re.search(
        r"SupportedOperations\s*=>\s*(\w+)\s*;",
        text,
    )
    if not ref_match:
        return None

    field_name = ref_match.group(1)
    if field_name == "new":
        # Already handled by Pattern A (shouldn't reach here)
        return None

    # Find the static field declaration: static readonly string[] FieldName = { ... };
    # or: private static readonly string[] FieldName = { ... };
    field_pattern = re.compile(
        rf"static\s+readonly\s+string\s*\[\s*\]\s+{re.escape(field_name)}\s*=\s*"
        r"(?:new\s*\[\s*\]\s*)?\{([^}}]+)\}",
        re.DOTALL,
    )
    fm = field_pattern.search(text)
    if fm:
        return _extract_strings(fm.group(1))

    return None


def _extract_strings(block: str) -> list[str]:
    """Extract all quoted strings from a code block."""
    return re.findall(r'"([^"]+)"', block)


def get_schema_operations() -> dict[str, list[str]]:
    """Extract ``operation`` enum values from all tool schemas.

    Returns:
        ``{mcp_tool_name: [operations]}`` for tools that have an
        ``operation`` property with ``enum``.
    """
    result: dict[str, list[str]] = {}
    for tool in get_tool_definitions():
        schema = tool.inputSchema
        if not isinstance(schema, dict):
            continue
        props = schema.get("properties", {})
        op_prop = props.get("operation", {})
        enum_vals = op_prop.get("enum")
        if enum_vals:
            result[tool.name] = list(enum_vals)
    return result


# ── Bridge name mapping for class resolution ─────────────────────────────

_csharp_registrations: dict[str, str] | None = None
_csharp_operations: dict[str, list[str]] | None = None


def _get_csharp_registrations() -> dict[str, str]:
    global _csharp_registrations
    if _csharp_registrations is None:
        _csharp_registrations = parse_registered_bridge_names(_INITIALIZER_PATH)
    return _csharp_registrations


def _get_csharp_operations() -> dict[str, list[str]]:
    global _csharp_operations
    if _csharp_operations is None:
        _csharp_operations = parse_handler_operations(_HANDLER_DIR)
    return _csharp_operations


# ── Tests: Bridge Name Sync ──────────────────────────────────────────────


class TestBridgeNameSync:
    """Verify that Python ``TOOL_NAME_TO_BRIDGE`` and C# ``CommandHandlerInitializer``
    are in sync."""

    def test_all_registry_bridge_names_are_registered_in_csharp(self) -> None:
        """Every bridge name in Python's TOOL_NAME_TO_BRIDGE must appear in
        CommandHandlerInitializer.cs."""
        csharp = _get_csharp_registrations()
        python_bridge_names = set(TOOL_NAME_TO_BRIDGE.values())
        csharp_bridge_names = set(csharp.keys())

        missing = python_bridge_names - csharp_bridge_names
        assert not missing, (
            f"Bridge names in Python TOOL_NAME_TO_BRIDGE but NOT registered in "
            f"CommandHandlerInitializer.cs: {sorted(missing)}"
        )

    def test_all_csharp_registrations_have_mcp_mappings(self) -> None:
        """Every bridge name registered in CommandHandlerInitializer.cs must
        have a corresponding entry in Python's TOOL_NAME_TO_BRIDGE."""
        csharp = _get_csharp_registrations()
        python_bridge_names = set(TOOL_NAME_TO_BRIDGE.values())
        csharp_bridge_names = set(csharp.keys())

        missing = csharp_bridge_names - python_bridge_names
        assert not missing, (
            f"Bridge names in CommandHandlerInitializer.cs but NOT in "
            f"Python TOOL_NAME_TO_BRIDGE: {sorted(missing)}"
        )

    def test_registration_count_matches(self) -> None:
        """Both sides should have the same number of registrations."""
        csharp = _get_csharp_registrations()
        assert len(TOOL_NAME_TO_BRIDGE) == len(csharp), (
            f"Registration count mismatch: "
            f"Python TOOL_NAME_TO_BRIDGE has {len(TOOL_NAME_TO_BRIDGE)}, "
            f"CommandHandlerInitializer.cs has {len(csharp)}"
        )


# ── Tests: Operation Sync ────────────────────────────────────────────────


def _build_operation_params() -> list[tuple[str, list[str], list[str]]]:
    """Build parametrized test cases for operation sync.

    Returns list of (mcp_tool_name, schema_ops, handler_ops) tuples.
    """
    schema_ops = get_schema_operations()
    csharp_regs = _get_csharp_registrations()
    csharp_ops = _get_csharp_operations()

    params: list[tuple[str, list[str], list[str]]] = []

    for mcp_name, bridge_name in TOOL_NAME_TO_BRIDGE.items():
        # Skip tools without operation enum in schema
        if mcp_name not in schema_ops:
            continue

        # Resolve C# class name from bridge name
        class_name = csharp_regs.get(bridge_name)
        if class_name is None:
            # Will be caught by TestBridgeNameSync
            continue

        handler_ops = csharp_ops.get(class_name)
        if handler_ops is None:
            # Handler file not parsed (shouldn't happen)
            continue

        params.append((mcp_name, schema_ops[mcp_name], handler_ops))

    return params


_OPERATION_PARAMS = _build_operation_params()


class TestOperationSync:
    """Verify that Python schema ``operation`` enums match C# handler
    ``SupportedOperations``."""

    @pytest.mark.parametrize(
        "mcp_name,schema_ops,handler_ops",
        _OPERATION_PARAMS,
        ids=[p[0] for p in _OPERATION_PARAMS],
    )
    def test_schema_operations_match_handler_operations(
        self,
        mcp_name: str,
        schema_ops: list[str],
        handler_ops: list[str],
    ) -> None:
        schema_set = set(schema_ops)
        handler_set = set(handler_ops)

        if schema_set != handler_set:
            only_in_schema = schema_set - handler_set
            only_in_handler = handler_set - schema_set
            parts = [f"{mcp_name} operations mismatch:"]
            parts.append(f"  Schema:  {sorted(schema_set)}")
            parts.append(f"  Handler: {sorted(handler_set)}")
            if only_in_schema:
                parts.append(f"  Only in schema (missing in handler): {sorted(only_in_schema)}")
            if only_in_handler:
                parts.append(f"  Only in handler (missing in schema): {sorted(only_in_handler)}")
            pytest.fail("\n".join(parts))

    def test_all_tools_with_operations_are_covered(self) -> None:
        """Every tool that has an operation enum in its schema should be
        tested by the parametrized test above."""
        schema_ops = get_schema_operations()
        covered = {p[0] for p in _OPERATION_PARAMS}
        uncovered = set(schema_ops.keys()) - covered

        # batch_sequential_execute is Python-only, no C# handler
        uncovered.discard("unity_batch_sequential_execute")

        assert not uncovered, (
            f"Tools with operation enums in schema but not covered by sync test: "
            f"{sorted(uncovered)}. Check that their bridge names are registered "
            f"in CommandHandlerInitializer.cs and that their handler files can be parsed."
        )
