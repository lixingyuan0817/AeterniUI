#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

required_files=(
  "AGENTS.md"
  "README.md"
  "docs/delivered-features.zh-CN.md"
  "docs/component-plan.zh-CN.md"
  "docs/review-plan.zh-CN.md"
  "docs/engineering-reference.zh-CN.md"
  "docs/release-history.zh-CN.md"
  "Directory.Build.props"
  "scripts/sample-publish.sh"
  "tests/AeterniUI.ContractChecks/AeterniUI.ContractChecks.csproj"
  "tests/AeterniUI.ContractChecks/Program.cs"
)

for file in "${required_files[@]}"; do
  [[ -f "$file" ]] || { echo "Missing required file: $file" >&2; exit 1; }
done

version=$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' Directory.Build.props | head -n 1)
if [[ -z "$version" ]]; then
  echo "Directory.Build.props does not define the .NET Version." >&2
  exit 1
fi

if [[ ! "$version" =~ ^10\.[0-9]+\.[0-9]+$ ]]; then
  echo "Version must use the 10.x.y format aligned with .NET 10; found: $version" >&2
  exit 1
fi

for doc in \
  docs/delivered-features.zh-CN.md \
  docs/component-plan.zh-CN.md \
  docs/review-plan.zh-CN.md \
  docs/engineering-reference.zh-CN.md \
  docs/release-history.zh-CN.md; do
  grep -q "文档版本：\`$version\`" "$doc" || {
    echo "Version mismatch in $doc; expected $version." >&2
    exit 1
  }
done

if ! grep -q 'delivered-features.zh-CN.md' docs/component-plan.zh-CN.md; then
  echo "Component plan does not link to the delivered feature source of truth." >&2
  exit 1
fi

if ! grep -q 'docs/delivered-features.zh-CN.md' README.md; then
  echo "README documentation index is incomplete." >&2
  exit 1
fi

for section in '## 1. 前言' '## 2. 开工必读' '## 3. 项目索引' '## 4. 开发规范'; do
  grep -Fq "$section" AGENTS.md || {
    echo "AGENTS.md is missing required section: $section" >&2
    exit 1
  }
done

grep -Fq "当前为 \`$version\`" AGENTS.md || {
  echo "AGENTS.md project index version mismatch; expected $version." >&2
  exit 1
}

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

grep -qE '^## [0-9]+\. Checkbox$' docs/delivered-features.zh-CN.md || { echo "Checkbox is missing from current features." >&2; exit 1; }
grep -qE '^## [0-9]+\. ComboBox$' docs/delivered-features.zh-CN.md || { echo "ComboBox is missing from current features." >&2; exit 1; }
grep -qE '^## [0-9]+\. Textarea$' docs/delivered-features.zh-CN.md || { echo "Textarea is missing from current features." >&2; exit 1; }

contract_command='dotnet run --project tests/AeterniUI.ContractChecks/AeterniUI.ContractChecks.csproj --no-build'
for file in AGENTS.md .github/workflows/build.yml; do
  grep -Fq "$contract_command" "$file" || {
    echo "Component contract check command is missing from $file." >&2
    exit 1
  }
done

echo "Documentation consistency checks passed."
