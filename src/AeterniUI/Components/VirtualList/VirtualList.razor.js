const instances = new Map();
const navigationKeys = new Set(['ArrowDown', 'ArrowUp', 'Home', 'End', ' ', 'Enter']);

function visible(element) {
    return !element.closest('[hidden], [inert]') && element.getClientRects().length > 0;
}

export function init(reference, key) {
    dispose(key);
    instances.set(key, {
        reference,
        root: null,
        viewport: null,
        handler: null,
        scrollHandler: null,
        pending: Promise.resolve(),
        loadMoreEnabled: false,
        loadMoreThreshold: 0,
        lastLoadMoreHeight: -1,
        disposed: false
    });
}

function tryLoadMore(state, root, viewport) {
    if (!state.loadMoreEnabled || state.disposed || state.root !== root || state.viewport !== viewport ||
        !visible(root) || root.getAttribute('aria-disabled') === 'true') return;

    const distance = viewport.scrollHeight - viewport.scrollTop - viewport.clientHeight;
    if (distance > state.loadMoreThreshold || viewport.scrollHeight === state.lastLoadMoreHeight) return;

    state.lastLoadMoreHeight = viewport.scrollHeight;
    state.loadMoreEnabled = false;
    state.pending = state.pending.then(async () => {
        if (!state.disposed && state.root === root) {
            await state.reference.invokeMethodAsync('HandleLoadMoreAsync');
        }
    }).catch(error => {
        if (!state.disposed) console.error('VirtualList load-more update failed.', error);
    });
}

export function sync(key, root, loadMoreEnabled = false, loadMoreThreshold = 0) {
    const state = instances.get(key);
    if (!state || !root) return;
    state.loadMoreEnabled = loadMoreEnabled === true;
    state.loadMoreThreshold = Number.isFinite(loadMoreThreshold) ? Math.max(0, loadMoreThreshold) : 0;
    const viewport = root.querySelector('.aeterni-virtual-list__viewport');
    if (!viewport) {
        state.root?.removeEventListener('keydown', state.handler);
        state.viewport?.removeEventListener('scroll', state.scrollHandler);
        state.root = null;
        state.viewport = null;
        return;
    }
    if (state.root === root && state.viewport === viewport) {
        queueMicrotask(() => tryLoadMore(state, root, viewport));
        return;
    }

    state.root?.removeEventListener('keydown', state.handler);
    state.viewport?.removeEventListener('scroll', state.scrollHandler);
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
    state.scrollHandler = () => tryLoadMore(state, root, viewport);
    viewport.addEventListener('scroll', state.scrollHandler, { passive: true });
    queueMicrotask(() => tryLoadMore(state, root, viewport));
}

export function scrollToIndex(key, index, itemSize) {
    const state = instances.get(key);
    if (!state?.viewport || !Number.isFinite(index) || !Number.isFinite(itemSize) || itemSize <= 0) return;

    const viewport = state.viewport;
    const itemStart = Math.max(0, index * itemSize);
    const itemEnd = itemStart + itemSize;
    const viewportStart = viewport.scrollTop;
    const viewportEnd = viewportStart + viewport.clientHeight;

    // Keep an already-visible row in place. Keyboard jumps still reveal rows
    // outside the viewport, but use the nearest edge instead of pinning every
    // newly active row to the top.
    let nextScrollTop = viewportStart;
    if (itemStart < viewportStart) nextScrollTop = itemStart;
    else if (itemEnd > viewportEnd) nextScrollTop = itemEnd - viewport.clientHeight;

    const maxScrollTop = Math.max(0, viewport.scrollHeight - viewport.clientHeight);
    viewport.scrollTop = Math.min(maxScrollTop, Math.max(0, nextScrollTop));
}

export function dispose(key) {
    const state = instances.get(key);
    if (!state) return;
    state.disposed = true;
    state.root?.removeEventListener('keydown', state.handler);
    state.viewport?.removeEventListener('scroll', state.scrollHandler);
    state.root = null;
    state.viewport = null;
    instances.delete(key);
}
