// A splitter owns one pointer gesture at a time. The live width is written to a
// CSS custom property; Blazor only hears about committed changes.
//
// Keyboard handling lives here rather than in C#: only the keys this module owns
// may preventDefault() (an unhandled arrow key must still scroll the page), and
// the committed width is painted before the Blazor round-trip.
const instances = new Map();

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
        width: 0,
        disabled: Boolean(options?.disabled),
        persistKey,
        dragging: false,
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
    const stored = readStored(state);
    state.width = stored ?? clamp(numeric(options?.firstSize, min), min, max);
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
    }
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
    applyDisabled(state);

    // While a gesture is in flight the live width wins; the host re-renders on
    // commit and would otherwise snap the pane back under the pointer.
    if (!state.dragging) {
        state.width = clamp(numeric(options?.firstSize, state.width), state.min, state.max);
        apply(state, state.width, false);
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
            next = state.max;
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
    state.width = clamp(value, state.min, state.max);
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

function clamp(value, min, max) {
    return Math.round(Math.max(min, Math.min(max, value)));
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
        return Number.isFinite(stored) ? clamp(stored, state.min, state.max) : null;
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
