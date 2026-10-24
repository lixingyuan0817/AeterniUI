// A splitter owns one pointer gesture at a time. The live width is written to a
// CSS custom property; Blazor only hears about committed changes.
//
// Keyboard handling lives here rather than in C#: only the keys this module owns
// may preventDefault() (an unhandled arrow key must still scroll the page), and
// the committed width is painted before the Blazor round-trip.
const instances = new Map();

// The second pane always keeps at least one touch target for the handle, so the
// seam can never be pushed outside the container and become unreachable.
// This mirrors --aeterni-touch-target-min; the computed token is preferred when
// the page provides it, this value only covers environments without CSS.
const DEFAULT_MIN_SECOND_PANE = 24;

export function init(dotNetRef, instanceId) {
    dispose(instanceId);
    instances.set(instanceId, { dotNetRef, state: null });
}

export function attach(instanceId, root, handle, options) {
    const instance = instances.get(instanceId);
    if (!instance || !root || !handle) {
        return;
    }

    detach(instance);

    const orientation = options?.orientation === 'vertical' ? 'vertical' : 'horizontal';
    const min = Math.max(0, numeric(options?.min, 0));
    const max = Math.max(min, numeric(options?.max, min));
    const persistKey = typeof options?.persistKey === 'string' ? options.persistKey : null;
    const state = {
        root,
        handle,
        dotNetRef: instance.dotNetRef,
        orientation,
        rtl: directionOf(root) === 'rtl',
        min,
        max,
        minSecondPane: DEFAULT_MIN_SECOND_PANE,
        width: 0,
        disabled: Boolean(options?.disabled),
        persistKey,
        dragging: false,
        detached: false,
        resizeObserver: null,
        pointerId: 0,
        start: 0,
        startSize: 0,
        bodyCursor: '',
        bodySelect: '',
        onPointerDown: null,
        onPointerMove: null,
        onPointerUp: null,
        onKeyDown: null,
        onDoubleClick: null
    };

    instance.state = state;
    state.minSecondPane = readMinSecondPane(root);
    const stored = readStored(state);
    const requested = stored ?? Math.round(numeric(options?.firstSize, min));
    state.width = constrain(state, requested);
    apply(state, state.width, false);
    applyDisabled(state);

    state.onPointerDown = event => startDrag(state, event);
    state.onPointerMove = event => moveDrag(state, event);
    state.onPointerUp = event => stopDrag(state, event);
    state.onKeyDown = event => handleKey(state, event);
    state.onDoubleClick = () => commit(state, state.min);

    handle.addEventListener('pointerdown', state.onPointerDown);
    handle.addEventListener('keydown', state.onKeyDown);
    handle.addEventListener('dblclick', state.onDoubleClick);

    // A restored width is the value the host must adopt, not just a painted width.
    if (stored !== null) {
        state.dotNetRef?.invokeMethodAsync('OnSizeRestoredAsync', state.width).catch(() => {});
    } else if (state.width !== requested) {
        // The configured FirstSize may exceed the space this container actually
        // offers; report the effective value so the host does not keep a width the
        // handle can never reach.
        state.dotNetRef?.invokeMethodAsync('OnSizeChangedAsync', state.width).catch(() => {});
    }

    observeContainer(state);
}

export function sync(instanceId, root, handle, options) {
    const instance = instances.get(instanceId);
    if (!instance) {
        return;
    }

    const state = instance.state;
    if (!state || state.root !== root || state.handle !== handle) {
        attach(instanceId, root, handle, options);
        return;
    }

    state.orientation = options?.orientation === 'vertical' ? 'vertical' : 'horizontal';
    state.rtl = directionOf(root) === 'rtl';
    state.min = Math.max(0, numeric(options?.min, 0));
    state.max = Math.max(state.min, numeric(options?.max, state.min));
    state.persistKey = typeof options?.persistKey === 'string' ? options.persistKey : null;
    state.disabled = Boolean(options?.disabled);
    state.minSecondPane = readMinSecondPane(root);
    applyDisabled(state);

    // While a gesture is in flight the live width wins; the host re-renders on
    // commit and would otherwise snap the pane back under the pointer.
    if (!state.dragging) {
        const requested = Math.round(numeric(options?.firstSize, state.width));
        state.width = constrain(state, requested);
        apply(state, state.width, false);
        if (state.width !== requested) {
            state.dotNetRef?.invokeMethodAsync('OnSizeChangedAsync', state.width).catch(() => {});
        }
    }
}

export function dispose(instanceId) {
    const instance = instances.get(instanceId);
    if (!instance) {
        return;
    }

    detach(instance);
    instances.delete(instanceId);
}

function detach(instance) {
    const state = instance.state;
    if (!state) {
        return;
    }

    state.detached = true;
    state.resizeObserver?.disconnect();
    state.resizeObserver = null;
    state.handle.removeEventListener('pointerdown', state.onPointerDown);
    state.handle.removeEventListener('keydown', state.onKeyDown);
    state.handle.removeEventListener('dblclick', state.onDoubleClick);
    stopDrag(state, false);
    instance.state = null;
}

function startDrag(state, event) {
    if (state.disabled || event.button !== 0) {
        return;
    }

    event.preventDefault();
    state.dragging = true;
    // Read the direction per gesture: a host can flip dir after the module attached.
    state.rtl = directionOf(state.root) === 'rtl';
    state.pointerId = event.pointerId;
    state.start = state.orientation === 'horizontal' ? event.clientX : event.clientY;
    state.startSize = state.width;
    state.handle.classList.add('is-dragging');
    state.handle.setPointerCapture?.(event.pointerId);
    window.addEventListener('pointermove', state.onPointerMove);
    window.addEventListener('pointerup', state.onPointerUp);
    window.addEventListener('pointercancel', state.onPointerUp);
    state.bodyCursor = document.body.style.cursor;
    state.bodySelect = document.body.style.userSelect;
    document.body.style.cursor = state.orientation === 'horizontal' ? 'col-resize' : 'row-resize';
    document.body.style.userSelect = 'none';
}

function moveDrag(state, event) {
    if (!state.dragging) {
        return;
    }

    event.preventDefault();
    const client = state.orientation === 'horizontal' ? event.clientX : event.clientY;
    // In RTL the first pane sits on the right, so dragging left grows it.
    const sign = state.orientation === 'horizontal' && state.rtl ? -1 : 1;
    apply(state, state.startSize + (client - state.start) * sign, false);
}

function stopDrag(state, commitChange = true) {
    if (!state.dragging) {
        return;
    }

    state.dragging = false;
    state.handle.classList.remove('is-dragging');
    try {
        state.handle.releasePointerCapture?.(state.pointerId);
    } catch {
        // The pointer may already have been released by the browser.
    }
    window.removeEventListener('pointermove', state.onPointerMove);
    window.removeEventListener('pointerup', state.onPointerUp);
    window.removeEventListener('pointercancel', state.onPointerUp);
    document.body.style.cursor = state.bodyCursor;
    document.body.style.userSelect = state.bodySelect;

    if (commitChange) {
        commit(state, state.width);
    }
}

function handleKey(state, event) {
    if (state.disabled || event.altKey || event.ctrlKey || event.metaKey) {
        return;
    }

    const step = event.shiftKey ? 32 : 8;
    const horizontal = state.orientation === 'horizontal';
    // Logical direction: ArrowRight grows the first pane in LTR, shrinks it in RTL.
    // Read it per key press for the same reason the drag reads it per gesture.
    const direction = horizontal && directionOf(state.root) === 'rtl' ? -1 : 1;
    let next = null;

    switch (event.key) {
        case 'ArrowLeft':
            next = horizontal ? state.width - step * direction : null;
            break;
        case 'ArrowRight':
            next = horizontal ? state.width + step * direction : null;
            break;
        case 'ArrowUp':
            next = horizontal ? null : state.width - step;
            break;
        case 'ArrowDown':
            next = horizontal ? null : state.width + step;
            break;
        case 'Home':
            next = state.min;
            break;
        case 'End':
            next = effectiveMax(state);
            break;
        default:
            return;
    }

    if (next === null) {
        return;
    }

    event.preventDefault();
    commit(state, next);
}

function commit(state, value) {
    apply(state, value, true);
    state.dotNetRef?.invokeMethodAsync('OnSizeChangedAsync', state.width).catch(() => {});
}

function apply(state, value, persist) {
    state.width = constrain(state, value);
    state.root.style.setProperty('--aeterni-splitter-first-size', `${state.width}px`);
    state.handle.setAttribute('aria-valuenow', String(state.width));

    if (persist) {
        writeStored(state);
    }
}

function applyDisabled(state) {
    if (state.disabled) {
        state.handle.setAttribute('aria-disabled', 'true');
    } else {
        state.handle.removeAttribute?.('aria-disabled');
    }
}

// 0 is a legal size (MinFirstSize may be 0), so a missing or malformed option must
// be told apart from a real 0 rather than collapsed by `||`.
function numeric(value, fallback) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : fallback;
}

// The configured MaxFirstSize is only an upper bound; a splitter that is smaller
// than that bound (a narrow preview, a resized window) must keep the seam inside
// the container. The second pane therefore reserves one touch target, otherwise
// the handle would sit past the clipped edge and could never be grabbed again.
function effectiveMax(state) {
    const container = containerSize(state);
    if (!(container > 0)) {
        // Unmeasured (or a detached layout): trust the configured maximum.
        return state.max;
    }

    const available = Math.round(container - state.minSecondPane);
    // When even the minimum does not fit, fall back to MinFirstSize: shrinking
    // below it would break a bound the host explicitly asked for.
    return Math.max(state.min, Math.min(state.max, available));
}

function constrain(state, value) {
    return Math.round(Math.max(state.min, Math.min(effectiveMax(state), value)));
}

function containerSize(state) {
    const element = state.root;
    if (!element) {
        return 0;
    }

    const horizontal = state.orientation === 'horizontal';
    const direct = horizontal ? element.clientWidth : element.clientHeight;
    if (Number.isFinite(direct) && direct > 0) {
        return direct;
    }

    try {
        const rect = element.getBoundingClientRect?.();
        const measured = horizontal ? rect?.width : rect?.height;
        if (Number.isFinite(measured) && measured > 0) {
            return measured;
        }
    } catch {
        // A detached element cannot be measured; the configured max still applies.
    }

    return 0;
}

// The second pane's reservation follows the same token the handle's hit area uses,
// so a host that themes the touch target keeps both in sync.
function readMinSecondPane(root) {
    if (typeof getComputedStyle !== 'function' || !root) {
        return DEFAULT_MIN_SECOND_PANE;
    }

    try {
        const parsed = parseFloat(getComputedStyle(root).getPropertyValue('--aeterni-touch-target-min'));
        return Number.isFinite(parsed) && parsed > 0 ? parsed : DEFAULT_MIN_SECOND_PANE;
    } catch {
        return DEFAULT_MIN_SECOND_PANE;
    }
}

function observeContainer(state) {
    // ResizeObserver is the only way to notice a shrink that does not come from a
    // re-render; without it a window resize would leave the same out-of-range width.
    if (typeof ResizeObserver !== 'function' || state.resizeObserver) {
        return;
    }

    const observer = new ResizeObserver(() => reclampToContainer(state));
    observer.observe(state.root);
    state.resizeObserver = observer;
}

function reclampToContainer(state) {
    if (state.detached) {
        return;
    }

    const max = effectiveMax(state);
    if (state.width <= max) {
        return;
    }

    if (state.dragging) {
        // The pointer is still down; paint the clamped width and let the pointerup
        // commit it through the normal path.
        apply(state, max, false);
        return;
    }

    commit(state, max);
}

function directionOf(element) {
    if (typeof getComputedStyle !== 'function' || !element) {
        return 'ltr';
    }

    try {
        return getComputedStyle(element).direction === 'rtl' ? 'rtl' : 'ltr';
    } catch {
        return 'ltr';
    }
}

function readStored(state) {
    if (!state.persistKey) {
        return null;
    }

    try {
        const raw = localStorage.getItem(state.persistKey);
        if (raw === null || raw === '') {
            return null;
        }

        const stored = Number(raw);
        return Number.isFinite(stored) ? constrain(state, stored) : null;
    } catch {
        return null;
    }
}

function writeStored(state) {
    if (!state.persistKey) {
        return;
    }

    try {
        localStorage.setItem(state.persistKey, String(state.width));
    } catch {
        // Private browsing can reject storage; the resize still works.
    }
}
