# Browser checks

Drives the **published sample** in headless Chrome over the DevTools Protocol and
asserts the behaviour that a rendered-HTML contract cannot see: real stacking
contexts, real CSS transition timing, real focus rings and real keyboard focus
movement. It exists because the rest of the suite runs on `HtmlRenderer`, which
has no layout, no cascade resolution against a live engine, and no focus model.

The harness has **no dependencies**: Node 22+ ships the global `WebSocket` the CDP
client uses, and the browser is whatever Chrome/Chromium is already installed.

```bash
tests/browser/run.sh                 # publishes the sample if dist/ is missing
AETERNI_REPUBLISH=1 tests/browser/run.sh Release
CHROME_BIN=/path/to/chrome tests/browser/run.sh
```

| File | Role |
| --- | --- |
| `run.sh` | Publishes if needed, starts the static server and Chrome, runs the checks, cleans up |
| `serve.mjs` | Static server for `dist/` with the MIME types Blazor WASM needs and an SPA fallback |
| `cdp.mjs` | Minimal DevTools Protocol client and helpers (navigate, evaluate, key, emulated media) |
| `verify.mjs` | The checks themselves; prints `PASS`/`FAIL` per assertion and exits non-zero on failure |

## What it covers

| Area | Assertion |
| --- | --- |
| `DialogProvider` stacking | The provider root is not a stacking context, and nothing between the notice region and `<html>` caps it — so toasts stay above popovers (1060) and tooltips (1070) |
| Theme transition | The transition class is applied on a real theme switch, dropped after the token-derived duration, and collapses to near-zero under `prefers-reduced-motion` |
| Focus ring | A repointed consumer still paints its ring, and the removed `--aeterni-control-focus-ring` alias resolves to nothing |
| Roving focus | Toolbar and ToggleGroup keep one tab stop, arrow keys move focus *and* the tab stop, and the ring wraps |

## What it does not cover

- **The inset focus ring** (Menu, MultiSelect options). Reaching it needs keyboard
  focus inside an open overlay; the trigger contract only promises Tab plus
  Enter/Space, so there is no supported path to focus a menu item from the
  keyboard alone. Covered by the contract assertion on the declared
  `outline-offset` instead, and the harness asserts the token it derives from
  resolves.
- **Anything requiring a real pointer device, touch, or a screen reader.** CDP can
  synthesise pointer and key events but not assistive-technology behaviour.
- **Visual regressions.** There are no screenshots here; every assertion is a
  computed-style or DOM-state claim.

## Notes for future assertions

Two things bit the first version of this harness, and both are easy to repeat:

- A programmatic `element.focus()` does **not** match `:focus-visible` in Chrome.
  Use a real `Tab` press (`tabUntil` in `verify.mjs`) when the assertion depends on
  a `:focus-visible` rule.
- An `Input.dispatchKeyEvent` without `text` does not fire the key's default
  action, so Enter on a button will not synthesise a click. Pass `text` (see the
  `press` helper) when the key is meant to activate something.
