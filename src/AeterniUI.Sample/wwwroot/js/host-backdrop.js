// AeterniUI.Sample — keep the native Tauri window backdrop in sync with the
// resolved page theme.
//
// This intentionally lives at the sample level, not in the component library:
// the library must stay host-agnostic, and only this sample is hosted inside a
// Tauri window that owns a native (macOS vibrancy / Windows acrylic) backdrop.
//
// ThemeProvider writes `data-theme="light|dark"` on <html> for every resolved
// theme (System, Light and Dark modes alike), so observing that attribute is a
// library-free way to react to theme changes.
(function () {
    const internals = window.__TAURI_INTERNALS__;
    if (!internals || typeof internals.invoke !== 'function') {
        // Plain browser or any other non-Tauri host: nothing native to sync.
        return;
    }

    const root = document.documentElement;
    const COMMAND = 'apply_window_backdrop';
    const SETTLE_MS = 90;
    let timer = null;

    function resolveTheme() {
        const value = root.dataset.theme;
        if (value === 'light' || value === 'dark') {
            return value;
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function syncNow() {
        if (timer !== null) {
            window.clearTimeout(timer);
            timer = null;
        }
        // Failures are ignored: the native effect simply stays on its previous
        // tint, and the next theme change will retry.
        internals.invoke(COMMAND, { theme: resolveTheme() }).catch(() => undefined);
    }

    function syncSoon() {
        if (timer !== null) {
            window.clearTimeout(timer);
        }
        timer = window.setTimeout(() => {
            timer = null;
            syncNow();
        }, SETTLE_MS);
    }

    // Cover the window before the Blazor app mounts, then follow every theme
    // switch afterwards. Initial value was set by the bootstrap script in <head>.
    syncNow();

    const observer = new MutationObserver(syncSoon);
    observer.observe(root, { attributes: true, attributeFilter: ['data-theme'] });
})();
