#!/usr/bin/env bash
# Publishes the AeterniUI Blazor sample so the repo-root "dist" folder directly
# contains the deployable site (index.html at its root). Tauri loads ../dist
# from the root-level src-tauri configuration.
#
# Usage: sample-publish.sh [Debug|Release]
#
# Blazor WASM's `dotnet publish` places the deployable site under <out>/wwwroot.
# We move that wwwroot content up to <repo>/dist and discard the runtime/transport
# metadata files, so `dist/index.html` is the web root Tauri serves directly.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIG="${1:-Debug}"
STAGE="$ROOT/.sample-publish"

case "$CONFIG" in
  Debug|Release) ;;
  *) echo "Unknown config '$CONFIG'. Use Debug or Release." >&2; exit 2 ;;
esac

echo "Publishing AeterniUI.Sample ($CONFIG) -> $ROOT/dist"

rm -rf "$ROOT/dist" "$STAGE"

dotnet publish \
  "$ROOT/src/AeterniUI.Sample/AeterniUI.Sample.csproj" \
  -c "$CONFIG" \
  -o "$STAGE"

if [ -d "$STAGE/wwwroot" ]; then
  mv "$STAGE/wwwroot" "$ROOT/dist"
fi

rm -rf "$STAGE"

# index.html references the library's scoped stylesheet by its logical name
# (_content/AeterniUI/AeterniUI.styles.css), but dotnet publish only emits the
# fingerprinted AeterniUI.<hash>.bundle.scp.css. Plain static hosts (Tauri
# custom protocol, python/npx preview servers) do not apply the .NET static-web-
# asset manifest, so a missing alias would 404 -> the browser then rejects the
# response as "non CSS MIME type". Mirror the current bundle under the logical
# name so any static host can serve it.
LIB_BUNDLE="$(ls "$ROOT/dist/_content/AeterniUI/"AeterniUI.*.bundle.scp.css 2>/dev/null | head -n 1 || true)"
if [ -n "$LIB_BUNDLE" ]; then
  cp "$LIB_BUNDLE" "$ROOT/dist/_content/AeterniUI/AeterniUI.styles.css"
  echo "Linked scoped CSS: $(basename "$LIB_BUNDLE") -> AeterniUI.styles.css"
fi

if [ ! -f "$ROOT/dist/index.html" ]; then
  echo "Expected $ROOT/dist/index.html but it was not produced." >&2
  exit 1
fi

echo "Done. dist/index.html is ready for Tauri."
