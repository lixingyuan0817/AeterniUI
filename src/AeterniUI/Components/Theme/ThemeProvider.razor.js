const instances = new Map();
let transitionTimer;
const transitionClass = 'aeterni-theme-transitioning';

// Theme-mode persistence uses browser localStorage; failures fall back to no
// persistence instead of breaking theme handling.
const storageKey = 'aeterni.theme.mode';
const brandStorageKey = 'aeterni.theme.brand';

export function getStoredMode() {
    try {
        return window.localStorage?.getItem(storageKey) ?? null;
    } catch {
        return null;
    }
}

export function persistMode(mode) {
    try {
        window.localStorage?.setItem(storageKey, mode ?? '');
    } catch {
        // Storage unavailable (private browsing, restricted context) — ignore.
    }
}

// Brand persistence lives in its own key so the two axes stay independently
// restorable: an absent brand falls back to the library default instead of
// resetting the stored mode.
export function getStoredBrand() {
    try {
        return window.localStorage?.getItem(brandStorageKey) ?? null;
    } catch {
        return null;
    }
}

export function persistBrand(brand) {
    try {
        window.localStorage?.setItem(brandStorageKey, brand ?? '');
    } catch {
        // Storage unavailable (private browsing, restricted context) — ignore.
    }
}

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

export function getSystemTheme() {
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function notify(reference, isDark) {
    return reference.invokeMethodAsync('OnSystemThemeChanged', isDark).catch(() => undefined);
}

export function applyTheme(theme, animate = true) {
    const normalizedTheme = theme === 'dark' ? 'dark' : 'light';
    const root = beginTransition(animate);

    root.dataset.theme = normalizedTheme;
    root.dataset.aeterniMode = normalizedTheme;
    root.style.colorScheme = normalizedTheme;

    endTransition(animate);
}

// Brand hue layer. The attribute has to sit on <html> next to the theme
// attribute, because every semantic alias resolves var(--aeterni-brand-*) on the
// element that declares it. An unrecognised value matches no [data-aeterni-brand]
// block, so the scale declared on :root keeps applying and the page stays violet
// instead of losing all brand colours.
export function applyBrand(brand, animate = true) {
    const root = document.documentElement;
    const value = typeof brand === 'string' && brand.length > 0 ? brand : 'purple';

    if (root.dataset.aeterniBrand === value) {
        return;
    }

    const target = beginTransition(animate);
    target.dataset.aeterniBrand = value;
    endTransition(animate);
}

function beginTransition(animate) {
    if (transitionTimer) {
        window.clearTimeout(transitionTimer);
        transitionTimer = undefined;
    }

    const root = document.documentElement;
    root.classList.toggle(transitionClass, animate);

    if (animate) {
        // Ensure the transition rule is active before semantic tokens change.
        void root.offsetWidth;
    }

    return root;
}

function endTransition(animate) {
    if (!animate) {
        return;
    }

    transitionTimer = window.setTimeout(() => {
        document.documentElement.classList.remove(transitionClass);
        transitionTimer = undefined;
    }, 620);
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
