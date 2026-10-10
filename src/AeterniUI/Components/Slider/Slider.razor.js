// Pointer dragging is the Slider's only browser behaviour: the keyboard model,
// the value maths and the ARIA state all live in C#. This module measures the
// track, converts a pointer position into a 0..1 fraction, optionally snaps that
// fraction to the configured step grid, writes the position custom property so
// the fill and the thumb follow the pointer without waiting for a render, and
// reports the same fraction back so C# commits exactly the value the user sees.
//
// The fraction is measured against the track element, not the root: the track is
// inset by half a thumb at either end, so track-relative coordinates are exactly
// the thumb centre's travel. That makes the first pixel of the track and the
// last pixel map to the two ends with no half-thumb offset to correct for.
//
// Echo suppression: while a drag is active this module owns the position custom
// property. Re-renders that arrive mid-drag must not overwrite it, or the thumb
// would jump back to the last committed value between pointer moves. Once the
// pointer is released, C# owns the property again through the style attribute.
//
// Pointer capture is the right primitive here because the pointer must keep
// steering the slider after it leaves the track; `setPointerCapture` retargets
// move/up events to the root, so the drag survives leaving the element and a
// lost capture can only happen when the browser takes the pointer away anyway.
//
// The guarded key set is kept in step with HandleKeyDownAsync in
// Slider.razor.cs; the contract checks compare the two literals so they cannot
// drift apart.

const instances = new Map();
const handledKeys = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageUp', 'PageDown']);
const EPSILON = 1e-6;

export function init(reference, key) {
    dispose(key);
    instances.set(key, {
        reference,
        root: null,
        track: null,
        bound: false,
        enabled: false,
        minimum: 0,
        maximum: 100,
        step: 0,
        vertical: false,
        rtl: false,
        dragging: false,
        reported: null,
        onPointerDown: null,
        onPointerMove: null,
        onPointerUp: null,
        onKeyDown: null
    });
}

export function sync(key, root, enabled, minimum, maximum, step, orientation) {
    const state = instances.get(key);
    if (!state) return;

    if (state.root !== root) {
        detach(state);
        releaseDrag(state);
        state.root = root;
        state.track = typeof root?.querySelector === 'function'
            ? root.querySelector('.aeterni-slider__track')
            : null;
    }

    state.minimum = Number.isFinite(minimum) ? minimum : 0;
    state.maximum = Number.isFinite(maximum) ? maximum : 100;
    state.step = Number.isFinite(step) && step > 0 ? step : 0;
    state.vertical = orientation === 'vertical';

    // The reading direction comes from the rendered element so it follows the
    // page's `dir` without the component having to know about it. C# keeps a
    // copy for its arrow-key mapping and is only told when the value flips.
    const rtl = !state.vertical && resolveDirection(state.root);
    if (rtl !== state.rtl) {
        state.rtl = rtl;
        state.reference?.invokeMethodAsync?.('HandleDirectionAsync', rtl)?.catch?.(() => {});
    }

    state.enabled = Boolean(enabled);

    if (state.enabled) {
        attach(state);
    } else {
        releaseDrag(state);
        detach(state);
    }
}

export function dispose(key) {
    const state = instances.get(key);
    if (!state) return;
    releaseDrag(state);
    detach(state);
    instances.delete(key);
}

function attach(state) {
    if (state.bound || !state.root) return;

    state.onPointerDown = event => handlePointerDown(state, event);
    state.onPointerMove = event => handlePointerMove(state, event);
    state.onPointerUp = event => handlePointerUp(state, event);
    state.onKeyDown = event => {
        if (event.isComposing) return;
        if (handledKeys.has(event.key) && !event.altKey && !event.ctrlKey && !event.metaKey && !event.shiftKey) {
            // Arrow/page keys would scroll the page and Home/End would jump to
            // the document edges; the C# key handler on the root performs the
            // actual stepping.
            event.preventDefault();
        }
    };

    state.bound = true;
    state.root.addEventListener('pointerdown', state.onPointerDown);
    state.root.addEventListener('keydown', state.onKeyDown);
}

function detach(state) {
    if (!state.bound || !state.root) return;
    state.root.removeEventListener('pointerdown', state.onPointerDown);
    state.root.removeEventListener('keydown', state.onKeyDown);
    state.bound = false;
}

function handlePointerDown(state, event) {
    if (!state.enabled || state.dragging) return;
    if (event.button > 0 || event.isPrimary === false) return;

    event.preventDefault();
    state.dragging = true;
    state.root.classList.add('is-dragging');
    state.root.focus?.({ preventScroll: true });

    if (typeof state.root.setPointerCapture === 'function') {
        try {
            state.root.setPointerCapture(event.pointerId);
        } catch {
            // Capture is best-effort; the drag below still works while the
            // pointer stays over the element.
        }
    }

    state.root.addEventListener('pointermove', state.onPointerMove);
    state.root.addEventListener('pointerup', state.onPointerUp);
    state.root.addEventListener('pointercancel', state.onPointerUp);

    applyPointer(state, event.clientX, event.clientY);
}

function handlePointerMove(state, event) {
    if (!state.dragging) return;
    applyPointer(state, event.clientX, event.clientY);
}

function handlePointerUp(state, event) {
    if (!state.dragging) return;

    if (typeof state.root.releasePointerCapture === 'function'
        && typeof state.root.hasPointerCapture === 'function'
        && state.root.hasPointerCapture(event.pointerId)) {
        try {
            state.root.releasePointerCapture(event.pointerId);
        } catch {
            // The browser may already have released the capture.
        }
    }

    releaseDrag(state);
}

function releaseDrag(state) {
    if (!state.dragging) return;
    state.dragging = false;
    state.root?.classList.remove('is-dragging');
    state.root?.removeEventListener('pointermove', state.onPointerMove);
    state.root?.removeEventListener('pointerup', state.onPointerUp);
    state.root?.removeEventListener('pointercancel', state.onPointerUp);
}

function applyPointer(state, clientX, clientY) {
    const measured = state.track ?? state.root;
    if (typeof measured?.getBoundingClientRect !== 'function') return;

    const rect = measured.getBoundingClientRect();
    let fraction;

    if (state.vertical) {
        fraction = rect.height > 0 ? (rect.bottom - clientY) / rect.height : 0;
    } else {
        fraction = rect.width > 0 ? (clientX - rect.left) / rect.width : 0;
        if (state.rtl) {
            fraction = 1 - fraction;
        }
    }

    const stepped = snap(state, Math.min(1, Math.max(0, fraction)));
    state.root.style.setProperty('--aeterni-slider-value', stepped.toFixed(4));

    if (state.reported === null || Math.abs(stepped - state.reported) >= EPSILON) {
        state.reported = stepped;
        state.reference?.invokeMethodAsync?.('HandlePointerFractionAsync', stepped)?.catch?.(() => {});
    }
}

function snap(state, fraction) {
    if (!(state.step > 0) || !(state.maximum > state.minimum)) return fraction;

    const span = state.maximum - state.minimum;
    const steps = Math.round((fraction * span) / state.step);
    const value = state.minimum + steps * state.step;
    return Math.min(1, Math.max(0, (value - state.minimum) / span));
}

function resolveDirection(root) {
    if (!root || typeof getComputedStyle !== 'function') return false;
    return getComputedStyle(root).direction === 'rtl';
}
