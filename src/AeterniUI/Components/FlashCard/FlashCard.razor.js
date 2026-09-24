// FlashCard pointer following. The module only mirrors the pointer position into
// two rotation custom properties and the sheen position on the card root, and
// toggles `is-tilting`; the rotation, the hover scale, the flip, the sheen finish
// and every transition stay in the isolated stylesheet. The flipped state belongs
// to the component, not to the browser.
//
// The effect is skipped for `Tilt="false"`, for disabled cards and for the
// `prefers-reduced-motion: reduce` preference. Those cards keep the flip, the
// focus ring and the resting sheen, and simply never tilt or track the pointer.
//
// The pointer rectangle is cached on `pointerenter` instead of being measured on
// every move: a tilted card reports an inflated bounding box, which would feed
// the rotation back into itself and make the card jitter.

const instances = new Map();

export function attach(instanceId, root, enabled, maxAngle, sheen) {
    detach(instanceId);

    if (!instanceId || !root) {
        return;
    }

    const state = {
        root,
        angle: Number(maxAngle) || 0,
        sheen: !!sheen,
        rect: null,
        onEnter: null,
        onMove: null,
        onLeave: null
    };

    instances.set(instanceId, state);

    if (!enabled || state.angle <= 0 || prefersReducedMotion()) {
        return;
    }

    state.onEnter = () => { state.rect = root.getBoundingClientRect(); };
    state.onMove = event => track(state, event);
    state.onLeave = () => reset(state);

    root.addEventListener('pointerenter', state.onEnter);
    root.addEventListener('pointermove', state.onMove);
    root.addEventListener('pointerleave', state.onLeave);
    root.addEventListener('pointercancel', state.onLeave);
}

export function dispose(instanceId) {
    const state = instances.get(instanceId);
    if (!state) {
        return;
    }

    release(state);
    instances.delete(instanceId);
}

function detach(instanceId) {
    if (instanceId && instances.has(instanceId)) {
        dispose(instanceId);
    }
}

// Every render re-attaches the instance, so releasing the listeners and clearing
// the transform has to stay in one place: a detached card must not keep a stale
// rotation when its instance is replaced.
function release(state) {
    if (state.onEnter) {
        state.root.removeEventListener('pointerenter', state.onEnter);
    }
    if (state.onMove) {
        state.root.removeEventListener('pointermove', state.onMove);
    }
    if (state.onLeave) {
        state.root.removeEventListener('pointerleave', state.onLeave);
        state.root.removeEventListener('pointercancel', state.onLeave);
    }

    state.onEnter = null;
    state.onMove = null;
    state.onLeave = null;
    reset(state);
}

function track(state, event) {
    const rect = state.rect ?? state.root.getBoundingClientRect();
    if (!rect || !rect.width || !rect.height) {
        return;
    }

    const x = clamp((event.clientX - rect.left) / rect.width - 0.5);
    const y = clamp((event.clientY - rect.top) / rect.height - 0.5);

    // The side under the pointer leans towards the viewer, capped at ±MaxTiltAngle
    // on the edges.
    state.root.style.setProperty('--aeterni-flash-card-rotate-y', `${(-x * 2 * state.angle).toFixed(2)}deg`);
    state.root.style.setProperty('--aeterni-flash-card-rotate-x', `${(y * 2 * state.angle).toFixed(2)}deg`);

    if (state.sheen) {
        state.root.style.setProperty('--aeterni-flash-card-sheen-x', `${((x + 0.5) * 100).toFixed(1)}%`);
        state.root.style.setProperty('--aeterni-flash-card-sheen-y', `${((y + 0.5) * 100).toFixed(1)}%`);
    }

    state.root.classList.add('is-tilting');
}

function reset(state) {
    state.rect = null;
    state.root.classList.remove('is-tilting');
    state.root.style.setProperty('--aeterni-flash-card-rotate-x', '0deg');
    state.root.style.setProperty('--aeterni-flash-card-rotate-y', '0deg');
}

function clamp(value) {
    return Math.max(-0.5, Math.min(0.5, value));
}

function prefersReducedMotion() {
    return typeof window !== 'undefined'
        && typeof window.matchMedia === 'function'
        && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}
