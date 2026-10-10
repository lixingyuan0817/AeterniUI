const instances = new Map();
const keys = new Set(['ArrowDown', 'ArrowUp', 'Home', 'End', ' ', 'Enter']);

function visible(element) {
    return !element.closest('[hidden], [inert]') && element.getClientRects().length > 0 &&
        !['hidden', 'collapse'].includes(getComputedStyle(element).visibility);
}

export function init(reference, key) {
    dispose(key);
    instances.set(key, { reference, host: null, pending: Promise.resolve(), disposed: false });
}

export function sync(key, host) {
    const state = instances.get(key);
    if (!state || !host) return;
    if (host.getAttribute('role') !== 'listbox') {
        detach(state);
        return;
    }
    if (state.host === host) return;
    detach(state);
    state.host = host;
    state.keydown = event => {
        if (event.target !== host || host.getAttribute('role') !== 'listbox' ||
            host.getAttribute('aria-disabled') === 'true' || !visible(host) ||
            event.altKey || event.ctrlKey || event.metaKey || event.shiftKey ||
            !keys.has(event.key)) return;
        if (event.isComposing) {
            event.stopPropagation();
            return;
        }
        event.preventDefault();
        // The list owns handled navigation/selection keys, not ancestor shortcuts.
        event.stopPropagation();
        const pressed = event.key;
        state.pending = state.pending.then(async () => {
            if (state.disposed || state.host !== host) return;
            const ids = [...host.querySelectorAll('[role="option"]')]
                .filter(option => option.closest('[role="listbox"]') === host &&
                    option.getAttribute('aria-disabled') !== 'true' && visible(option))
                .map(option => option.id);
            await state.reference.invokeMethodAsync('HandleKeyFromBrowserAsync', pressed, ids);
        }).catch(error => {
            if (!state.disposed) console.error('List keyboard update failed.', error);
        });
    };
    host.addEventListener('keydown', state.keydown);
}

function detach(state) {
    state.host?.removeEventListener('keydown', state.keydown);
    state.host = null;
}

export function dispose(key) {
    const state = instances.get(key);
    if (!state) return;
    state.disposed = true;
    detach(state);
    instances.delete(key);
}
