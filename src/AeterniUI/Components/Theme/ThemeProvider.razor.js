const instances = new Map();
let transitionTimer;
const transitionClass = 'aeterni-theme-transitioning';

export function init(reference, key) {
    dispose(key);

    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    const changeHandler = event => notify(reference, event.matches);
    instances.set(key, { mediaQuery, changeHandler });

    if (mediaQuery.addEventListener) {
        mediaQuery.addEventListener('change', changeHandler);
    } else {
        mediaQuery.addListener(changeHandler);
    }

    return notify(reference, mediaQuery.matches);
}

export async function getSystemTheme() {
    // An explicit Tauri light/dark override can otherwise make the webview
    // keep reporting the previous effective theme.
    await syncNativeTheme('system');
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function notify(reference, isDark) {
    return reference.invokeMethodAsync('OnSystemThemeChanged', isDark).catch(() => undefined);
}

export function applyTheme(theme, animate = true, mode = 'explicit') {
    const normalizedTheme = theme === 'dark' ? 'dark' : 'light';
    const root = document.documentElement;

    if (transitionTimer) {
        window.clearTimeout(transitionTimer);
        transitionTimer = undefined;
    }

    root.classList.toggle(transitionClass, animate);
    if (animate) {
        // Ensure the transition rule is active before semantic tokens change.
        void root.offsetWidth;
    }

    root.dataset.theme = normalizedTheme;
    root.dataset.aeterniMode = normalizedTheme;
    root.style.colorScheme = normalizedTheme;
    syncNativeTheme(normalizedTheme, mode);

    if (animate) {
        transitionTimer = window.setTimeout(() => {
            root.classList.remove(transitionClass);
            transitionTimer = undefined;
        }, 620);
    }
}

async function syncNativeTheme(theme, mode = 'explicit') {
    const tauri = window.__TAURI__;
    const tauriApp = tauri?.app;
    const tauriWindow = tauri?.window?.getCurrentWindow?.();
    const invoke = window.__TAURI_INTERNALS__?.invoke;
    const nativeTheme = mode === 'system' ? null : theme;

    if (!tauriApp?.setTheme && !tauriWindow?.setTheme && !invoke) {
        return;
    }

    // macOS applies appearance at the app level. Keep the window call as a
    // fallback for platforms where the titlebar is window-scoped.
    const nativeThemeTask = tauriApp?.setTheme
        ? tauriApp.setTheme(nativeTheme)
        : tauriWindow?.setTheme
            ? tauriWindow.setTheme(nativeTheme)
            : invoke('plugin:app|set_app_theme', { theme: nativeTheme });

    await Promise.resolve(nativeThemeTask).catch(async () => {
        if (tauriWindow?.setTheme && tauriApp?.setTheme) {
            await tauriWindow.setTheme(nativeTheme).catch(() => undefined);
        }
    });
}

export function dispose(key) {
    const instance = instances.get(key);
    if (!instance) {
        return;
    }

    if (instance.mediaQuery.removeEventListener) {
        instance.mediaQuery.removeEventListener('change', instance.changeHandler);
    } else {
        instance.mediaQuery.removeListener(instance.changeHandler);
    }

    instances.delete(key);

    if (instances.size === 0) {
        if (transitionTimer) {
            window.clearTimeout(transitionTimer);
            transitionTimer = undefined;
        }
        document.documentElement.classList.remove(transitionClass);
    }
}
