#!/usr/bin/env pwsh
# Unity MCP Skill - Windows Setup Script
# Usage: .\setup\install.ps1

param(
    [switch]$Dev = $false
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "🚀 Unity MCP Skill - Installation" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host ""

# Get script directory
$ScriptDir = Split-Path -Parent $PSCommandPath
$RootDir = Split-Path -Parent $ScriptDir

# Change to root directory
Set-Location $RootDir

# Check Python version
Write-Host "🔍 Checking Python installation..." -ForegroundColor Cyan
try {
    $PythonVersion = python --version 2>&1
    Write-Host "   ✓ Found: $PythonVersion" -ForegroundColor Green

    # Extract version number
    if ($PythonVersion -match "Python (\d+)\.(\d+)") {
        $Major = [int]$Matches[1]
        $Minor = [int]$Matches[2]

        if ($Major -lt 3 -or ($Major -eq 3 -and $Minor -lt 10)) {
            Write-Host "   ❌ Python 3.10 or higher is required" -ForegroundColor Red
            exit 1
        }
    }
} catch {
    Write-Host "   ❌ Python not found. Please install Python 3.10 or higher" -ForegroundColor Red
    Write-Host "   Download from: https://www.python.org/downloads/" -ForegroundColor Yellow
    exit 1
}

# Check for uv
Write-Host ""
Write-Host "🔍 Checking for uv package manager..." -ForegroundColor Cyan
$UvExists = Get-Command uv -ErrorAction SilentlyContinue

if (-not $UvExists) {
    Write-Host "   ⚠️  uv not found. Installing..." -ForegroundColor Yellow
    try {
        irm https://astral.sh/uv/install.ps1 | iex
        Write-Host "   ✓ uv installed successfully" -ForegroundColor Green
    } catch {
        Write-Host "   ❌ Failed to install uv" -ForegroundColor Red
        Write-Host "   Please install manually: https://docs.astral.sh/uv/" -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host "   ✓ uv is already installed" -ForegroundColor Green
}

# Install dependencies
Write-Host ""
Write-Host "📦 Installing dependencies..." -ForegroundColor Cyan
try {
    if ($Dev) {
        Write-Host "   Installing with dev dependencies..." -ForegroundColor Yellow
        uv sync --dev
    } else {
        uv sync
    }
    Write-Host "   ✓ Dependencies installed successfully" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Failed to install dependencies" -ForegroundColor Red
    exit 1
}

# Check Unity Editor
Write-Host ""
Write-Host "🔍 Checking Unity installation..." -ForegroundColor Cyan
$UnityPaths = @(
    "$env:ProgramFiles\Unity\Hub\Editor\*\Editor\Unity.exe",
    "$env:ProgramFiles(x86)\Unity\Hub\Editor\*\Editor\Unity.exe"
)

$UnityFound = $false
foreach ($Path in $UnityPaths) {
    if (Test-Path $Path) {
        $UnityFound = $true
        Write-Host "   ✓ Unity Editor found" -ForegroundColor Green
        break
    }
}

if (-not $UnityFound) {
    Write-Host "   ⚠️  Unity Editor not found in default locations" -ForegroundColor Yellow
    Write-Host "   Make sure Unity 2021.3 or higher is installed" -ForegroundColor Yellow
}

# Generate MCP configuration
Write-Host ""
Write-Host "⚙️  Generating MCP configuration..." -ForegroundColor Cyan
try {
    python setup/configure.py
    Write-Host "   ✓ Configuration generated" -ForegroundColor Green
} catch {
    Write-Host "   ⚠️  Configuration generation skipped" -ForegroundColor Yellow
    Write-Host "   You can run 'python setup/configure.py' manually later" -ForegroundColor Yellow
}

# Success message
Write-Host ""
Write-Host "✅ Installation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📖 Next steps:" -ForegroundColor Cyan
Write-Host "   1. Open your Unity project" -ForegroundColor White
Write-Host "   2. Go to Tools > MCP Assistant" -ForegroundColor White
Write-Host "   3. Click 'Start Bridge'" -ForegroundColor White
Write-Host "   4. Configure your MCP client (see config/ folder)" -ForegroundColor White
Write-Host ""
Write-Host "📚 Documentation:" -ForegroundColor Cyan
Write-Host "   Quick Start: QUICKSTART.md" -ForegroundColor White
Write-Host "   API Reference: docs/api-reference/" -ForegroundColor White
Write-Host "   Examples: examples/" -ForegroundColor White
Write-Host ""
