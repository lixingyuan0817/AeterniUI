// NavTree's model lives in Blazor. This module only suppresses native page
// scrolling for the keys the tree owns and moves focus to the node Blazor chose.
const NAVIGATION_KEYS = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End']);
const instances = new Map();

export function init(_reference, key) {
    dispose(key);
    instances.set(key, { host: null, keydown: null });
}

export function attach(key, host) {
    const instance = instances.get(key);
    if (!instance || !host) {
        return;
    }

    detach(instance);
    instance.host = host;
    instance.keydown = event => {
        if (NAVIGATION_KEYS.has(event.key) &&
            event.target instanceof Element &&
            event.target.closest('.aeterni-nav-tree__button')) {
            event.preventDefault();
        }
    };
    host.addEventListener('keydown', instance.keydown, true);
}

export function focus(nodeId) {
    document.getElementById(nodeId)?.focus();
}

export function dispose(key) {
    const instance = instances.get(key);
    if (!instance) {
        return;
    }

    detach(instance);
    instances.delete(key);
}

function detach(instance) {
    if (instance.host && instance.keydown) {
        instance.host.removeEventListener('keydown', instance.keydown, true);
    }
    instance.host = null;
    instance.keydown = null;
}
