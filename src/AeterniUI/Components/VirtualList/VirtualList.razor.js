const instances = new Map();
const navigationKeys = new Set(['ArrowDown', 'ArrowUp', 'Home', 'End', ' ', 'Enter']);

function visible(element) {
    return !element.closest('[hidden], [inert]') && element.getClientRects().length > 0;
}

export function init(reference, key) {
    dispose(key);
    instances.set(key, { reference, root: null, viewport: null, handler: null, pending: Promise.resolve(), disposed: false });
}

export function sync(key, root) {
    const state = instances.get(key);
    if (!state || !root) return;
    const viewport = root.querySelector('.aeterni-virtual-list__viewport');
    if (!viewport) {
        state.root?.removeEventListener('keydown', state.handler);
        state.root = null;
        state.viewport = null;
        return;
    }
    if (state.root === root && state.viewport === viewport) return;

    state.root?.removeEventListener('keydown', state.handler);
    state.root = root;
    state.viewport = viewport;
    state.handler = event => {
        if (event.target !== root || root.getAttribute('role') !== 'listbox' ||
            root.getAttribute('aria-disabled') === 'true' || !visible(root) ||
            event.altKey || event.ctrlKey || event.metaKey || event.shiftKey ||
            event.isComposing || !navigationKeys.has(event.key)) return;

        event.preventDefault();
        event.stopPropagation();
        const keyPressed = event.key;
        state.pending = state.pending.then(async () => {
            if (!state.disposed && state.root === root) {
                await state.reference.invokeMethodAsync('HandleKeyFromBrowserAsync', keyPressed);
            }
        }).catch(error => {
            if (!state.disposed) console.error('VirtualList keyboard update failed.', error);
        });
    };
    root.addEventListener('keydown', state.handler);
}

export function scrollToIndex(key, index, itemSize) {
    const state = instances.get(key);
    if (!state?.viewport || !Number.isFinite(index) || !Number.isFinite(itemSize)) return;
    state.viewport.scrollTop = Math.max(0, index * itemSize);
}

export function dispose(key) {
    const state = instances.get(key);
    if (!state) return;
    state.disposed = true;
    state.root?.removeEventListener('keydown', state.handler);
    state.root = null;
    state.viewport = null;
    instances.delete(key);
}
