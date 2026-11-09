#!/usr/bin/env bash
# Installs a packaged addon into a fresh C# project with default settings,
# builds it, and loads it in the headless editor with the plugin enabled.
#
# Usage: GODOT=/path/to/godot tools/smoke_test_addon.sh <zip> <addon>
set -euo pipefail

if [[ $# -ne 2 || -z "${GODOT:-}" ]]; then
    echo "Usage: GODOT=/path/to/godot $0 <zip> <addon>" >&2
    exit 2
fi

zip_path=$(realpath "$1")
addon=$2
sdk_version=${GODOT_SDK_VERSION:-4.7.0}

project=$(mktemp -d)
trap 'rm -rf "$project"' EXIT

unzip -q "$zip_path" -d "$project"

cat >"$project/project.godot" <<EOF
config_version=5

[application]

config/name="AddonSmokeTest"
config/features=PackedStringArray("4.7", "C#", "Forward Plus")

[dotnet]

project/assembly_name="AddonSmokeTest"

[editor_plugins]

enabled=PackedStringArray("res://addons/$addon/plugin.cfg")
EOF

# Matches the project file Godot generates, so the addon cannot rely on
# settings from this repository's csproj.
cat >"$project/AddonSmokeTest.csproj" <<EOF
<Project Sdk="Godot.NET.Sdk/$sdk_version">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>
</Project>
EOF

echo "== dotnet build"
dotnet build "$project/AddonSmokeTest.csproj" -nologo -v quiet

echo "== headless editor load"
log="$project/editor.log"
timeout 300 "$GODOT" --headless -e --path "$project" --quit-after 200 2>&1 | tee "$log"

# Godot exits 0 when a plugin fails to load, so the log is the only signal.
if grep -a "ERROR:" "$log" >/dev/null; then
    echo "error: the editor reported errors while loading $addon:" >&2
    grep -a "ERROR:" "$log" >&2
    exit 1
fi

echo "== $addon loaded cleanly"
