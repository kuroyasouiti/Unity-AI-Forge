#!/usr/bin/env bash
# Unity MCP Skill - Linux/macOS Setup Script
# Usage: ./setup/install.sh [--dev]

set -e

DEV_MODE=false
if [[ "$1" == "--dev" ]]; then
    DEV_MODE=true
fi

echo ""
echo "🚀 Unity MCP Skill - Installation"
echo "================================="
echo ""

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

# Change to root directory
cd "$ROOT_DIR"

# Check Python version
echo "🔍 Checking Python installation..."
if command -v python3 &> /dev/null; then
    PYTHON_VERSION=$(python3 --version)
    echo "   ✓ Found: $PYTHON_VERSION"

    # Extract version numbers
    VERSION_NUM=$(echo "$PYTHON_VERSION" | grep -oE '[0-9]+\.[0-9]+')
    MAJOR=$(echo "$VERSION_NUM" | cut -d. -f1)
    MINOR=$(echo "$VERSION_NUM" | cut -d. -f2)

    if [ "$MAJOR" -lt 3 ] || { [ "$MAJOR" -eq 3 ] && [ "$MINOR" -lt 10 ]; }; then
        echo "   ❌ Python 3.10 or higher is required"
        exit 1
    fi
else
    echo "   ❌ Python 3 not found. Please install Python 3.10 or higher"
    echo "   Download from: https://www.python.org/downloads/"
    exit 1
fi

# Check for uv
echo ""
echo "🔍 Checking for uv package manager..."
if ! command -v uv &> /dev/null; then
    echo "   ⚠️  uv not found. Installing..."
    if curl -LsSf https://astral.sh/uv/install.sh | sh; then
        echo "   ✓ uv installed successfully"
        # Source the environment to make uv available
        export PATH="$HOME/.cargo/bin:$PATH"
    else
        echo "   ❌ Failed to install uv"
        echo "   Please install manually: https://docs.astral.sh/uv/"
        exit 1
    fi
else
    echo "   ✓ uv is already installed"
fi

# Install dependencies
echo ""
echo "📦 Installing dependencies..."
if [ "$DEV_MODE" = true ]; then
    echo "   Installing with dev dependencies..."
    uv sync --dev
else
    uv sync
fi
echo "   ✓ Dependencies installed successfully"

# Check Unity Editor (Linux/macOS)
echo ""
echo "🔍 Checking Unity installation..."
UNITY_FOUND=false

# Check common Unity paths
if [ -d "/Applications/Unity/Hub/Editor" ] || [ -d "$HOME/Unity/Hub/Editor" ]; then
    UNITY_FOUND=true
    echo "   ✓ Unity Editor found"
elif command -v unity &> /dev/null; then
    UNITY_FOUND=true
    echo "   ✓ Unity Editor found in PATH"
fi

if [ "$UNITY_FOUND" = false ]; then
    echo "   ⚠️  Unity Editor not found in default locations"
    echo "   Make sure Unity 2021.3 or higher is installed"
fi

# Generate MCP configuration
echo ""
echo "⚙️  Generating MCP configuration..."
if python3 setup/configure.py; then
    echo "   ✓ Configuration generated"
else
    echo "   ⚠️  Configuration generation skipped"
    echo "   You can run 'python3 setup/configure.py' manually later"
fi

# Success message
echo ""
echo "✅ Installation complete!"
echo ""
echo "📖 Next steps:"
echo "   1. Open your Unity project"
echo "   2. Go to Tools > MCP Assistant"
echo "   3. Click 'Start Bridge'"
echo "   4. Configure your MCP client (see config/ folder)"
echo ""
echo "📚 Documentation:"
echo "   Quick Start: QUICKSTART.md"
echo "   API Reference: docs/api-reference/"
echo "   Examples: examples/"
echo ""
