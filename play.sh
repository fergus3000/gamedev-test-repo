#!/bin/bash
set -e

GODOT="$HOME/.local/share/godot/4.6.2/Godot_v4.6.2-stable_mono_linux.x86_64"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$SCRIPT_DIR/Game"

echo "Building..."
dotnet build "$PROJECT/cursorgame.csproj" --verbosity quiet

echo "Launching..."
"$GODOT" --path "$PROJECT/"
