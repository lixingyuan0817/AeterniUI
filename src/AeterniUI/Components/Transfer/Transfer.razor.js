// The Transfer keeps every bit of business state in C#: which items are moved, twice
// which list is rendered, the transient checked set and the active item. The browser
// is needed for exactly two jobs:
//
//   1. the two listbox containers own focus (one Tab stop each, the active row is
//      reported through `aria-activedescendant`), so navigation/selection keys must
//      be cancelled and forwarded to C# instead of scrolling the page;
//   2. the active row has to be scrolled into its own column viewport, because
//      focus never leaves the listbox container.
//
// This module owns no state of its own: it binds per component instance, forwards
// `data-side` + key, and detaches every listener in dispose().

const instances = new Map();

const keys = new Set(['ArrowDown', 'ArrowUp', 'Home', 'End', ' ', 'Enter']);

function visible(element) {
    return !element.closest('[hidden], [inert]') && element.getClientRects().length > 0 &&
        !['hidden', 'collapse'].includes(getComputedStyle(element).visibility);
}

export function init(reference, key) {
    dispose(key);
    instances.set(key, {
        reference,
        root: null,
        hosts: [],
        handlers: [],
        pending: Promise.resolve(),
        disposed: false
    });
}

export function sync(key, root) {
    const state = instances.get(key);
    if (!state) return;

    // The two listboxes are looked up from the component root rather than captured
    // as ElementReference parameters: the pair is rendered unconditionally, so a
    // query is stable, and a consumer-supplied `Element` parameter replaces the root
    // without making one of the two columns silently lose its listener.
    const lists = root ? [...root.querySelectorAll('[role="listbox"]')] : [];

    if (state.root !== root || !sameHosts(state.hosts, lists)) {
        // Unbind before rebinding: leaving a stale listener attached to a detached
        // subtree leaks it for the lifetime of the page.
        detach(state);
        state.root = root;
        state.hosts = lists;

        for (const list of lists) {
            const handler = event => handleKeydown(state, list, event);
            list.addEventListener('keydown', handler);
            state.handlers.push({ list, handler });
        }
    }

    for (const list of lists) {
        scrollActiveIntoList(list);
    }
}

function handleKeydown(state, list, event) {
    if (event.target !== list || list.getAttribute('role') !== 'listbox' ||
        list.getAttribute('aria-disabled') === 'true' || !visible(list) ||
        event.altKey || event.ctrlKey || event.metaKey || event.shiftKey ||
        !keys.has(event.key)) {
        return;
    }

    if (event.isComposing) {
        // An IME candidate list owns these keys while composing.
        event.stopPropagation();
        return;
    }

    // The listbox owns the handled navigation/selection keys, not ancestor shortcuts,
    // and cancelling the default keeps arrow keys from scrolling the page.
    event.preventDefault();
    event.stopPropagation();

    const side = list.getAttribute('data-side');
    if (!side) return;

    const pressed = event.key;
    state.pending = state.pending.then(async () => {
        if (state.disposed || !state.hosts.includes(list)) return;
        await state.reference.invokeMethodAsync('HandleKeyFromBrowserAsync', side, pressed);
    }).catch(error => {
        if (!state.disposed) console.error('Transfer keyboard update failed.', error);
    });
}

// Keep the row inside its own column. The page must not move: the listbox container
// is the component's scroll viewport, so only its own scrollTop changes.
function scrollActiveIntoList(list) {
    const active = document.getElementById(list.getAttribute('aria-activedescendant'));
    if (!active) return;

    const row = active.getBoundingClientRect();
    const box = list.getBoundingClientRect();
    if (row.top < box.top) list.scrollTop -= box.top - row.top;
    else if (row.bottom > box.bottom) list.scrollTop += row.bottom - box.bottom;
}

function sameHosts(hosts, lists) {
    return hosts.length === lists.length && lists.every((list, index) => hosts[index] === list);
}

function detach(state) {
    for (const { list, handler } of state.handlers) {
        list.removeEventListener('keydown', handler);
    }

    state.handlers = [];
    state.hosts = [];
    state.root = null;
}

export function dispose(key) {
    const state = instances.get(key);
    if (!state) return;
    state.disposed = true;
    detach(state);
    instances.delete(key);
}
