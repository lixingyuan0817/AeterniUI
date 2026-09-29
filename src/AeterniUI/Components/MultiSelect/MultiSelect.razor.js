const instances = new Map();
const navigationKeys = new Set(['ArrowDown', 'ArrowUp', 'Home', 'End', 'Escape', 'Backspace']);

export function init(reference, key) {
    dispose(key);
    instances.set(key, { reference, trigger: null, handler: null });
}

export function sync(key, root) {
    const state = instances.get(key);
    if (!state || !root) return;

    const trigger = root.querySelector('[role="combobox"]');
    if (!trigger) return;

    const active = document.getElementById(trigger.getAttribute('aria-activedescendant'));
    if (active) {
        const viewport = active.parentElement;
        const row = active.getBoundingClientRect();
        const box = viewport.getBoundingClientRect();
        if (row.top < box.top) viewport.scrollTop -= box.top - row.top;
        else if (row.bottom > box.bottom) viewport.scrollTop += row.bottom - box.bottom;
    }

    if (state.trigger === trigger) return;
    state.trigger?.removeEventListener('keydown', state.handler);
    state.trigger = trigger;
    state.handler = event => {
        if (event.target !== trigger || trigger.getAttribute('aria-disabled') === 'true' || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;
        if (event.isComposing) { event.stopPropagation(); return; }
        if (navigationKeys.has(event.key) ||
            (trigger.getAttribute('aria-expanded') === 'true' && ['Enter', ' '].includes(event.key))) event.preventDefault();
    };
    trigger.addEventListener('keydown', state.handler);
}

export function dispose(key) {
    const state = instances.get(key);
    if (!state) return;
    state.trigger?.removeEventListener('keydown', state.handler);
    instances.delete(key);
}
