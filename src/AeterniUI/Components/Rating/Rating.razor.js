const instances = new Map();
const navigationKeys = new Set(['ArrowRight', 'ArrowLeft', 'ArrowUp', 'ArrowDown', 'Home', 'End']);

export function init(reference, key) {
    dispose(key);
    instances.set(key, { reference, root: null, handler: null });
}

export function sync(key, root) {
    const state = instances.get(key);
    if (!state || state.root === root) return;
    state.root?.removeEventListener('keydown', state.handler);
    state.root = root;
    state.handler = event => {
        if (root.classList.contains('is-disabled') || root.classList.contains('is-readonly')) return;
        if (event.isComposing) { event.stopPropagation(); return; }
        if (event.target?.getAttribute?.('role') === 'radio' && navigationKeys.has(event.key) &&
            !event.altKey && !event.ctrlKey && !event.metaKey && !event.shiftKey) {
            event.preventDefault();
        }
    };
    root?.addEventListener('keydown', state.handler);
}

export function dispose(key) {
    const state = instances.get(key);
    if (!state) return;
    state.root?.removeEventListener('keydown', state.handler);
    instances.delete(key);
}
