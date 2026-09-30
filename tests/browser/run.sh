#!/usr/bin/env bash
# Runs the browser checks against the published sample.
#
#   tests/browser/run.sh [Debug|Release]
#
# Prerequisites: a Chrome/Chromium binary (CHROME_BIN, or `google-chrome` /
# `chromium` / the macOS app bundle), Node 22+ (for the global WebSocket the CDP
# client uses), and a published sample. The script publishes one when dist/ is
# missing or when AETERNI_REPUBLISH=1 is set.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CONFIG="${1:-Debug}"
PORT="${AETERNI_PORT:-5099}"
CDP_PORT="${AETERNI_CDP_PORT:-9222}"
PROFILE="$(mktemp -d)"
HTTP_LOG="$(mktemp)"
CHROME_LOG="$(mktemp)"

find_chrome() {
    if [[ -n "${CHROME_BIN:-}" ]]; then echo "$CHROME_BIN"; return; fi
    for candidate in google-chrome chromium chromium-browser \
        "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"; do
        if command -v "$candidate" >/dev/null 2>&1; then echo "$candidate"; return; fi
    done
    echo "No Chrome/Chromium found. Set CHROME_BIN to the binary." >&2
    exit 2
}

if [[ ! -f "$ROOT/dist/index.html" || "${AETERNI_REPUBLISH:-0}" == "1" ]]; then
    echo "Publishing the sample ($CONFIG)…"
    bash "$ROOT/scripts/sample-publish.sh" "$CONFIG" >/dev/null
fi

node "$ROOT/tests/browser/serve.mjs" "$ROOT/dist" "$PORT" >"$HTTP_LOG" 2>&1 &
HTTP_PID=$!
"$(find_chrome)" --headless=new --remote-debugging-port="$CDP_PORT" \
    --no-first-run --no-default-browser-check --disable-gpu --no-sandbox \
    --user-data-dir="$PROFILE" about:blank >"$CHROME_LOG" 2>&1 &
CHROME_PID=$!

cleanup() {
    kill "$CHROME_PID" "$HTTP_PID" 2>/dev/null || true
    wait "$CHROME_PID" "$HTTP_PID" 2>/dev/null || true
    # Chrome keeps writing to its profile while shutting down; retry rather than
    # let a racing rm print an error over the real result.
    for _ in $(seq 1 10); do
        rm -rf "$PROFILE" "$HTTP_LOG" "$CHROME_LOG" 2>/dev/null && break
        sleep 0.3
    done
    return 0
}
trap cleanup EXIT

# Wait for both endpoints rather than sleeping a fixed amount.
for _ in $(seq 1 40); do
    if curl -sf "http://127.0.0.1:$PORT/" >/dev/null && curl -sf "http://127.0.0.1:$CDP_PORT/json/version" >/dev/null; then
        break
    fi
    sleep 0.25
done

AETERNI_BASE_URL="http://127.0.0.1:$PORT" \
AETERNI_CDP_URL="http://127.0.0.1:$CDP_PORT" \
node "$ROOT/tests/browser/verify.mjs"
