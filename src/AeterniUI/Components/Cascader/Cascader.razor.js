// The Cascader keeps focus on the trigger (a select-only combobox) and reports the
// active option with `aria-activedescendant`. Two things then need the browser:
//
//   1. the active option has to be scrolled into view — vertically inside its own
//      column, and the whole column horizontally into the panel when the viewport
//      cannot hold every expanded level;
//   2. the navigation keys the trigger handles must not also scroll the page.
//
// Business state (which column is active, what is expanded, the value) all stays in
// C#; this module only moves scroll offsets and cancels default actions.

const instances = new Map();

const navigationKeys = new Set([
    'ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'Escape'
]);

export function init(reference, key) {
    dispose(key);
    instances.set(key, { reference, trigger: null, handler: null });
}

export function sync(key, trigger) {
    const state = instances.get(key);
    if (!state) return;

    // Unbind before any early return: leaving a stale listener attached to a
    // detached subtree leaks it for the lifetime of the page.
    if (state.trigger !== trigger) {
        state.trigger?.removeEventListener('keydown', state.handler);
        state.trigger = null;
        state.handler = null;
    }

    if (!trigger) return;

    // Scroll only the option viewport, never the page (which closes the popup).
    const active = document.getElementById(trigger.getAttribute('aria-activedescendant'));
    if (active) scrollActiveIntoView(active);

    if (state.trigger === trigger) return;
    state.trigger = trigger;
    state.handler = event => {
        if (event.target !== trigger || trigger.disabled ||
            event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;
        if (event.isComposing) { event.stopPropagation(); return; }
        if (navigationKeys.has(event.key) ||
            (trigger.getAttribute('aria-expanded') === 'true' && ['Enter', ' '].includes(event.key))) {
            event.preventDefault();
        }
    };
    trigger.addEventListener('keydown', state.handler);
}

function scrollActiveIntoView(active) {
    // Vertical: keep the row inside its own column.
    const column = active.parentElement;
    if (!column) return;

    const row = active.getBoundingClientRect();
    const box = column.getBoundingClientRect();
    if (row.top < box.top) column.scrollTop -= box.top - row.top;
    else if (row.bottom > box.bottom) column.scrollTop += row.bottom - box.bottom;

    // Horizontal: keep the whole column inside the panel.
    const panel = column.parentElement;
    if (!panel) return;

    const col = column.getBoundingClientRect();
    const view = panel.getBoundingClientRect();
    if (col.left < view.left) panel.scrollLeft -= view.left - col.left;
    else if (col.right > view.right) panel.scrollLeft += col.right - view.right;
}

export function dispose(key) {
    const state = instances.get(key);
    if (!state) return;
    state.trigger?.removeEventListener('keydown', state.handler);
    instances.delete(key);
}
