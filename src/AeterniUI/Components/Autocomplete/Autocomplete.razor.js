const instances = new Map();
const navigationKeys = new Set(['ArrowDown', 'ArrowUp', 'Home', 'End', 'Escape']);

export function init(reference, key) {
    dispose(key);
    instances.set(key, { reference, root: null, input: null, handler: null });
}

export function sync(key, input) {
    const state = instances.get(key);
    if (!state) return;

    // Unbind before any early return: leaving a stale listener attached to a
    // detached subtree leaks it for the lifetime of the page.
    if (state.input !== input) {
        state.input?.removeEventListener('keydown', state.handler);
        state.input = null;
        state.handler = null;
    }

    if (!input) return;

    const active = document.getElementById(input.getAttribute('aria-activedescendant'));
    if (active) {
        const viewport = active.parentElement;
        const row = active.getBoundingClientRect();
        const box = viewport.getBoundingClientRect();
        if (row.top < box.top) viewport.scrollTop -= box.top - row.top;
        else if (row.bottom > box.bottom) viewport.scrollTop += row.bottom - box.bottom;
    }

    if (state.input === input) return;
    state.input = input;
    state.handler = event => {
        if (event.target !== input || input.disabled || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;
        if (event.isComposing) {
            event.stopPropagation();
            return;
        }

        if (navigationKeys.has(event.key) ||
            (input.getAttribute('aria-expanded') === 'true' && event.key === 'Enter')) {
            event.preventDefault();
        }
    };
    input.addEventListener('keydown', state.handler);
}

export function dispose(key) {
    const state = instances.get(key);
    if (!state) return;
    state.input?.removeEventListener('keydown', state.handler);
    instances.delete(key);
}
