// A splitter owns one pointer gesture at a time. The live width is written to a
// CSS custom property; Blazor only hears about committed changes.
const instances = new Map();

export function init(dotNetRef, instanceId) {
    dispose(instanceId);
    instances.set(instanceId, { dotNetRef, state: null, disposed: false });
}

export function attach(instanceId, root, handle, options) {
    const instance = instances.get(instanceId);
    if (!instance || !root || !handle) {
        return;
    }

    detach(instance);

    const orientation = options?.orientation === 'vertical' ? 'vertical' : 'horizontal';
    const min = Math.max(0, Number(options?.min) || 0);
    const max = Math.max(min, Number(options?.max) || min);
    const persistKey = typeof options?.persistKey === 'string' ? options.persistKey : null;
    const state = {
        root,
        handle,
        orientation,
        min,
        max,
        width: clamp(Number(options?.firstSize) || min, min, max),
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
    state.width = readStored(state) ?? state.width;
    apply(state, state.width, false);

    state.onPointerDown = event => startDrag(state, event);
    state.onPointerMove = event => moveDrag(state, event);
    state.onPointerUp = event => stopDrag(state, event);
    state.onKeyDown = event => handleKey(state, event);
    state.onDoubleClick = () => commit(state, state.min);

    handle.addEventListener('pointerdown', state.onPointerDown);
    handle.addEventListener('keydown', state.onKeyDown);
    handle.addEventListener('dblclick', state.onDoubleClick);

    if (persistKey && readStored(state) !== null) {
        instance.dotNetRef?.invokeMethodAsync('OnSizeRestoredAsync', state.width).catch(() => {});
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
    state.min = Math.max(0, Number(options?.min) || 0);
    state.max = Math.max(state.min, Number(options?.max) || state.min);
    state.persistKey = typeof options?.persistKey === 'string' ? options.persistKey : null;
    state.handle.setAttribute('aria-disabled', options?.disabled ? 'true' : 'false');

    if (!state.dragging) {
        state.width = clamp(Number(options?.firstSize) || state.width, state.min, state.max);
        apply(state, state.width, false);
    }
}

export function dispose(instanceId) {
    const instance = instances.get(instanceId);
    if (!instance) {
        return;
    }

    detach(instance);
    instance.disposed = true;
    instances.delete(instanceId);
}

function detach(instance) {
    const state = instance.state;
    if (!state) {
        return;
    }

    state.handle.removeEventListener('pointerdown', state.onPointerDown);
    state.handle.removeEventListener('pointermove', state.onPointerMove);
    state.handle.removeEventListener('pointerup', state.onPointerUp);
    state.handle.removeEventListener('pointercancel', state.onPointerUp);
    state.handle.removeEventListener('keydown', state.onKeyDown);
    state.handle.removeEventListener('dblclick', state.onDoubleClick);
    stopDrag(state, null, false);
    instance.state = null;
}

function startDrag(state, event) {
    if (state.handle.getAttribute('aria-disabled') === 'true' || event.button !== 0) {
        return;
    }

    event.preventDefault();
    state.dragging = true;
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
    const delta = (state.orientation === 'horizontal' ? event.clientX : event.clientY) - state.start;
    apply(state, state.startSize + delta, false);
}

function stopDrag(state, event, commitChange = true) {
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
    if (state.handle.getAttribute('aria-disabled') === 'true' || event.altKey || event.ctrlKey || event.metaKey) {
        return;
    }

    const step = event.shiftKey ? 32 : 8;
    const horizontal = state.orientation === 'horizontal';
    let next = null;

    switch (event.key) {
        case 'ArrowLeft':
            next = horizontal ? state.width - step : null;
            break;
        case 'ArrowRight':
            next = horizontal ? state.width + step : null;
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
    const instance = [...instances.values()].find(candidate => candidate.state === state);
    instance?.dotNetRef?.invokeMethodAsync('OnSizeChangedAsync', state.width).catch(() => {});
}

function apply(state, value, persist) {
    state.width = clamp(value, state.min, state.max);
    state.root.style.setProperty('--aeterni-splitter-first-size', `${state.width}px`);
    state.handle.setAttribute('aria-valuenow', String(state.width));

    if (persist) {
        writeStored(state);
    }
}

function clamp(value, min, max) {
    return Math.round(Math.max(min, Math.min(max, value)));
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
