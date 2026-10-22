// NavMenu keyboard support. Blazor owns the navigation model (which row comes
// next, when a group opens), while this module only does what C# cannot: moving
// focus, suppressing the browser's default arrow-key page scroll while focus is
// inside the menu, and reporting the writing direction so ArrowLeft/ArrowRight
// expand and collapse in RTL as well.

const NAVIGATION_KEYS = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End']);

const instances = new Map();

export function init(_reference, key) {
    dispose(key);
    instances.set(key, { host: null, keydownHandler: null });
}

// The root element is passed from C# after the first render; key suppression
// only applies while focus is inside the menu.
export function attach(key, host) {
    const instance = instances.get(key);
    if (!instance || !host) {
        return;
    }

    detachHandler(instance);

    instance.host = host;
    instance.keydownHandler = event => {
        // preventDefault keeps the page from scrolling; the event still
        // propagates, so the C# navigation model keeps receiving it. Trailing
        // actions are rendered outside the row button and keep their own keys.
        if (!NAVIGATION_KEYS.has(event.key)) {
            return;
        }

        const target = event.target;
        const insideRow = typeof target?.closest === 'function'
            ? !!target.closest('.aeterni-nav-menu__button')
            : true;
        if (insideRow) {
            event.preventDefault();
        }
    };

    host.addEventListener('keydown', instance.keydownHandler, true);
}

export function focus(nodeId) {
    const node = document.getElementById(nodeId);
    node?.focus();
}

export function isRtl(key) {
    const host = instances.get(key)?.host;
    return host ? getComputedStyle(host).direction === 'rtl' : false;
}

export function dispose(key) {
    const instance = instances.get(key);
    if (!instance) {
        return;
    }

    detachHandler(instance);
    instances.delete(key);
}

function detachHandler(instance) {
    if (instance.keydownHandler && instance.host) {
        instance.host.removeEventListener('keydown', instance.keydownHandler, true);
    }

    instance.keydownHandler = null;
}
