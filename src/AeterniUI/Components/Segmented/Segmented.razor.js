/*
   Segmented module.

   The control is a native radio group, so the browser already owns the arrow keys,
   the roving tab stop, the RTL arrow direction and the selection that follows them.
   The one thing a radio group does not have is Home/End, and the component adds it
   in C#. This module exists for the single part C# cannot cover: the default action
   of Home/End (scrolling the document) happens in the DOM event, while Blazor's
   synthetic handler runs after it, so a C#-only version would move the selection
   while the page jumped to the top or the bottom at the same time.

   Selection, focus and the option list all stay in C#; this module only calls
   preventDefault for the keys the component owns.
*/

const instances = new Map();

// Only the keys the component handles itself. The arrow keys must keep their
// default action or native radio navigation (focus + selection) would break.
const ownedKeys = new Set(['Home', 'End']);

/**
 * Wires the keydown guard to the component root. Re-renders pass the same root, so
 * this is a no-op unless the root element actually changed.
 */
export function sync(key, root) {
    if (!root) {
        return;
    }

    const state = instances.get(key) ?? { root: null, keydown: null };
    if (state.root === root) {
        return;
    }

    state.root?.removeEventListener('keydown', state.keydown);
    state.root = root;
    state.keydown = event => {
        if (!ownedKeys.has(event.key) || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) {
            return;
        }

        // A disabled option never receives key events, so the enabled check is the
        // target test alone: anything else inside the group keeps its own default.
        if (event.target?.matches?.('.aeterni-segmented__input:not(:disabled)')) {
            event.preventDefault();
        }
    };
    root.addEventListener('keydown', state.keydown);
    instances.set(key, state);
}

export function dispose(key) {
    const state = instances.get(key);
    if (!state) {
        return;
    }

    state.root?.removeEventListener('keydown', state.keydown);
    instances.delete(key);
}
