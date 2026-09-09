#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

required_files=(
  "AGENTS.md"
  "README.md"
  "docs/agent-development.zh-CN.md"
  "docs/component-design-guidelines.zh-CN.md"
  "docs/component-roadmap.zh-CN.md"
  "docs/current-features.zh-CN.md"
  "docs/project-index.zh-CN.md"
  "Directory.Build.props"
  "src-tauri/tauri.conf.json"
  "scripts/sample-publish.sh"
)

for file in "${required_files[@]}"; do
  [[ -f "$file" ]] || { echo "Missing required file: $file" >&2; exit 1; }
done

if grep -RniE 'tauri/src-tauri|before(Dev|Build)Command[^\n]*\.\./scripts/sample-publish\.sh' \
  AGENTS.md README.md docs src-tauri --include='*.md' --include='*.json'; then
  echo "Found stale Tauri path reference." >&2
  exit 1
fi

if ! grep -q 'scripts/sample-publish.sh Debug' src-tauri/tauri.conf.json; then
  echo "Tauri beforeDevCommand is not aligned with the repository root command path." >&2
  exit 1
fi

version=$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' Directory.Build.props | head -n 1)
if [[ -z "$version" ]]; then
  echo "Directory.Build.props does not define the .NET Version." >&2
  exit 1
fi

if [[ ! "$version" =~ ^10\.[0-9]+\.[0-9]+$ ]]; then
  echo "Version must use the 10.x.y .NET version format; found: $version" >&2
  exit 1
fi

for doc in docs/current-features.zh-CN.md docs/component-design-guidelines.zh-CN.md docs/component-roadmap.zh-CN.md docs/project-index.zh-CN.md; do
  grep -q "文档版本：\`$version\`" "$doc" || {
    echo "Version mismatch in $doc; expected $version." >&2
    exit 1
  }
done

if ! grep -q 'current-features.zh-CN.md' docs/component-roadmap.zh-CN.md; then
  echo "Roadmap does not link to the current feature source of truth." >&2
  exit 1
fi

if ! grep -q 'docs/component-design-guidelines.zh-CN.md' README.md; then
  echo "README documentation index is incomplete." >&2
  exit 1
fi

component_paths=(
  "Button:src/AeterniUI/Components/Button"
  "Input:src/AeterniUI/Components/Input"
  "Checkbox:src/AeterniUI/Components/Checkbox"
  "Switch:src/AeterniUI/Components/Switch"
  "Rating:src/AeterniUI/Components/Rating"
  "ComboBox:src/AeterniUI/Components/ComboBox"
)
for entry in "${component_paths[@]}"; do
  name="${entry%%:*}"
  path="${entry#*:}"
  [[ -d "$path" ]] || { echo "Missing component directory for $name: $path" >&2; exit 1; }
done

grep -qE '^## [0-9]+\. Checkbox$' docs/current-features.zh-CN.md || { echo "Checkbox is missing from current features." >&2; exit 1; }
grep -qE '^## [0-9]+\. ComboBox$' docs/current-features.zh-CN.md || { echo "ComboBox is missing from current features." >&2; exit 1; }
grep -qE '^## [0-9]+\. Textarea$' docs/current-features.zh-CN.md || { echo "Textarea is missing from current features." >&2; exit 1; }

echo "Documentation consistency checks passed."
