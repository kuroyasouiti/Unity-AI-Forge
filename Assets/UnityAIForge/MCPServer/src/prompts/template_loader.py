"""Template loader for MCP prompt templates.

Loads markdown template files from the templates/ directory
with version placeholder replacement.
"""

from __future__ import annotations

from pathlib import Path

from version import SERVER_VERSION

TEMPLATES_DIR = Path(__file__).parent / "templates"


def load_prompt_template(relative_path: str) -> str:
    """Load a prompt template file with version substitution.

    Args:
        relative_path: Path relative to the templates/ directory (e.g. "genre/platformer_2d.md")

    Returns:
        Template content with {VERSION} replaced by server version.

    Raises:
        FileNotFoundError: If the template file doesn't exist.
    """
    template_path = TEMPLATES_DIR / relative_path

    if not template_path.exists():
        raise FileNotFoundError(f"テンプレートファイルが見つかりません: {template_path}")

    content = template_path.read_text(encoding="utf-8")
    content = content.replace("{VERSION}", SERVER_VERSION)

    return content


def list_available_templates() -> list[str]:
    """List all available template files relative to the templates/ directory.

    Returns:
        Sorted list of relative paths to template files.
    """
    if not TEMPLATES_DIR.exists():
        return []

    return sorted(
        str(p.relative_to(TEMPLATES_DIR)).replace("\\", "/") for p in TEMPLATES_DIR.rglob("*.md")
    )
