// InputNumber's only browser behaviour is suppressing the default action of the
// keys the component already handles: PageUp/PageDown would scroll the document
// while stepping the value, and ArrowUp/ArrowDown would move the caret to either
// end of the input before the C# handler runs. Stepping itself stays in C#, this
// module never writes state and never calls back into .NET.
//
// The guarded key set is kept in step with HandleKeyDownAsync in
// InputNumber.razor.cs; the contract checks compare the two literals so they
// cannot drift apart.
const instances = new Map();
const handledKeys = new Set(['ArrowUp', 'ArrowDown', 'PageUp', 'PageDown']);

export function init(reference, key) {
    dispose(key);
    instances.set(key, { root: null, handler: null });
}

export function sync(key, root) {
    const state = instances.get(key);
    if (!state || state.root === root) return;
    state.root?.removeEventListener('keydown', state.handler);
    state.root = root;
    state.handler = event => {
        if (root?.classList.contains('is-disabled') || root?.classList.contains('is-readonly')) return;
        if (event.isComposing) return;
        if (handledKeys.has(event.key) && !event.altKey && !event.ctrlKey && !event.metaKey && !event.shiftKey) {
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
