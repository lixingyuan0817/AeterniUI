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

if ! grep -q '<Version>[^<]*</Version>' Directory.Build.props; then
  echo "Directory.Build.props does not define the .NET Version." >&2
  exit 1
fi

if ! grep -q 'current-features.zh-CN.md' docs/component-roadmap.zh-CN.md; then
  echo "Roadmap does not link to the current feature source of truth." >&2
  exit 1
fi

if ! grep -q 'docs/component-design-guidelines.zh-CN.md' README.md; then
  echo "README documentation index is incomplete." >&2
  exit 1
fi

echo "Documentation consistency checks passed."
