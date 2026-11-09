#!/usr/bin/env bash
# Builds an installable zip for one addon from the files committed at HEAD.
#
# Usage: tools/package_addon.sh <addon> <version> [out_dir]
# Output: <out_dir>/<addon>-<version>.zip and <out_dir>/<addon>-<version>.notes.md
#
# Fails if <version> does not match plugin.cfg and the latest CHANGELOG entry.
set -euo pipefail

if [[ $# -lt 2 ]]; then
    echo "Usage: $0 <addon> <version> [out_dir]" >&2
    exit 2
fi

addon=$1
version=$2
root=$(git rev-parse --show-toplevel)
out=$(realpath -m "${3:-$root/dist}")
common=editor_tools_common

cd "$root"

if ! git cat-file -e "HEAD:addons/$addon/plugin.cfg" 2>/dev/null; then
    echo "error: addons/$addon/plugin.cfg is not committed at HEAD" >&2
    exit 1
fi

plugin_version=$(git show "HEAD:addons/$addon/plugin.cfg" | sed -n 's/^version="\(.*\)"[[:space:]]*$/\1/p')
if [[ "$plugin_version" != "$version" ]]; then
    echo "error: version $version does not match plugin.cfg ($plugin_version)" >&2
    exit 1
fi

changelog=$(git show "HEAD:addons/$addon/CHANGELOG.md")
changelog_version=$(sed -n 's/^## \[\([0-9][^]]*\)\].*/\1/p' <<<"$changelog" | head -n 1)
if [[ "$changelog_version" != "$version" ]]; then
    echo "error: version $version does not match latest CHANGELOG entry ($changelog_version)" >&2
    exit 1
fi

stage=$(mktemp -d)
trap 'rm -rf "$stage"' EXIT

git archive HEAD "addons/$addon" "addons/$common" LICENSE | tar -x -C "$stage"
cp "$stage/LICENSE" "$stage/addons/$addon/LICENSE"
cp "$stage/LICENSE" "$stage/addons/$common/LICENSE"

mkdir -p "$out"
zip_path="$out/$addon-$version.zip"
rm -f "$zip_path"
(cd "$stage" && zip -qrX "$zip_path" addons)

# The CHANGELOG section for this version, without its heading.
awk -v heading="## [$version]" '
    index($0, heading) == 1 { found = 1; next }
    found && /^## \[/ { exit }
    found { print }
' <<<"$changelog" >"$out/$addon-$version.notes.md"

echo "$zip_path"
