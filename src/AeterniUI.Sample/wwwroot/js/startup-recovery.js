// AeterniUI.Sample — bounded startup recovery for the Tauri host.
//
// The Tauri window starts hidden and is revealed by the native host when the
// page loads and again when the Blazor app shell mounts (host_content_ready),
// which prevents the macOS transparent-window "blank until manual reload"
// first frame. In rare cold starts the WASM boot can still fail to mount; a
// manual reload always recovers it. This watchdog performs those reloads
// automatically instead of forcing the user to right-click → Reload.
//
// Guardrails:
//  - runs only inside the Tauri host (needs __TAURI_INTERNALS__),
//  - bounded: at most 3 reload attempts per cold start (sessionStorage),
//  - triggers on the Blazor error banner or after a short mount timeout,
//  - cancels immediately once the app shell actually renders.
(function () {
    const internals = window.__TAURI_INTERNALS__;
    if (!internals || typeof internals.invoke !== 'function') {
        return;
    }

    const FLAG = 'aeterni_startup_recovery';
    const MAX_ATTEMPTS = 3;
    const MOUNT_TIMEOUT_MS = 9000;
    const POLL_MS = 500;

    let attempts = 0;
    try {
        attempts = parseInt(sessionStorage.getItem(FLAG) || '0', 10) || 0;
    } catch {
        attempts = 0;
    }
    if (attempts >= MAX_ATTEMPTS) {
        // Already retried enough for this cold start; leave the built-in error
        // UI visible so the user can decide.
        return;
    }

    const startedAt = Date.now();
    let running = true;

    function appMounted() {
        // The first routed page renders the shared .sample-page shell as soon
        // as Blazor has booted and mounted the router.
        return Boolean(document.querySelector('.sample-page'));
    }

    function errorUiVisible() {
        const ui = document.getElementById('blazor-error-ui');
        if (!ui) {
            return false;
        }
        const style = window.getComputedStyle(ui);
        return style.display !== 'none' && style.visibility !== 'hidden' && style.opacity !== '0';
    }

    function finish(reload) {
        if (!running) {
            return;
        }
        running = false;
        window.clearInterval(timer);
        if (reload) {
            attempts += 1;
            try {
                sessionStorage.setItem(FLAG, String(attempts));
            } catch {
                /* non-fatal */
            }
            window.location.reload();
        } else {
            // Healthy boot: allow recovery again on the next cold start.
            try {
                sessionStorage.removeItem(FLAG);
            } catch {
                /* non-fatal */
            }
        }
    }

    const timer = window.setInterval(function poll() {
        if (appMounted()) {
            finish(false);
            return;
        }
        if (errorUiVisible() || Date.now() - startedAt > MOUNT_TIMEOUT_MS) {
            finish(true);
        }
    }, POLL_MS);
})();
