const instances = new Map();

export function init(reference, key) {
    dispose(key);
    instances.set(key, { reference, wheels: new Map() });
}

export function sync(key, host, _activeOptionId, _shouldAlign, animateAlignment, alignmentRevision = 0) {
    const instance = instances.get(key);
    if (!instance || !host) return;
    const wheels = new Set(host.querySelectorAll('.aeterni-time-option-list__options'));
    for (const [wheel, state] of instance.wheels) {
        if (!wheels.has(wheel)) {
            detachWheel(wheel, state);
            instance.wheels.delete(wheel);
        }
    }
    for (const wheel of wheels) {
        let state = instance.wheels.get(wheel);
        const fresh = !state;
        if (fresh) {
            state = attachWheel(instance, wheel);
            instance.wheels.set(wheel, state);
        }
        const selected = wheel.querySelector('[role="option"].is-selected');
        const value = selected?.dataset.timeValue ?? null;
        const changed = state.selected !== value;
        const requested = state.revision !== undefined && state.revision !== alignmentRevision;
        state.revision = alignmentRevision;
        state.selected = value;
        const geometry = state.geometry;
        measure(wheel, state);
        const geometryChanged = geometry !== state.geometry;
        if (fresh) {
            state.height = wheel.clientHeight;
            state.rowHeight = state.rows[0]?.height ?? 0;
        }
        // A render echo acknowledges our callback; it must not interrupt new input.
        const echo = state.notified === value;
        if (changed && !echo) state.notified = value;
        const target = state.rows.find(row => row.option === selected && row.enabled);
        if (target && (fresh || requested || (changed && !echo) || (geometryChanged && !state.user))) {
            align(instance, wheel, state, target, !fresh && (animateAlignment || changed), false);
        } else if (state.target && !state.rows.some(row => row.option === state.target.option && row.enabled)) {
            cancel(state);
            settle(instance, wheel, state);
        }
        paint(wheel, state);
    }
}

export function pendingValues(key) {
    const values = {};
    for (const [wheel, state] of instances.get(key)?.wheels ?? []) {
        if (state.timer || state.target || state.user) {
            const row = state.target ?? closest(wheel, state);
            if (row) values[wheel.dataset.timeUnit] = Number(row.option.dataset.timeValue);
        }
    }
    return values;
}

export function dispose(key) {
    for (const [wheel, state] of instances.get(key)?.wheels ?? []) detachWheel(wheel, state);
    instances.delete(key);
}

function attachWheel(instance, wheel) {
    const state = { rows: [], visible: new Set(), frame: 0, timer: 0, target: null,
        selected: null, notified: null, user: false, height: 0, rowHeight: 0,
        reduced: window.matchMedia('(prefers-reduced-motion: reduce)') };
    state.input = () => {
        cancel(state);
        state.user = true;
        schedule(instance, wheel, state);
    };
    state.scroll = () => {
        if (!state.frame) state.frame = requestAnimationFrame(() => {
            state.frame = 0;
            paint(wheel, state);
        });
        if (!state.target) schedule(instance, wheel, state);
    };
    state.motion = () => {
        if (state.reduced.matches && state.target) {
            const { target, commit } = state;
            align(instance, wheel, state, target, false, commit);
        }
        paint(wheel, state);
    };
    state.resize = new ResizeObserver(() => {
        const height = wheel.clientHeight;
        const rowHeight = state.rows[0]?.option.offsetHeight ?? 0;
        if (height === state.height && rowHeight === state.rowHeight) return;
        state.height = height;
        state.rowHeight = rowHeight;
        measure(wheel, state);
        if (height > 0) {
            const selected = state.rows.find(row => row.option.dataset.timeValue === state.selected);
            const target = state.user ? closest(wheel, state) : selected;
            if (target) align(instance, wheel, state, target, false, state.user);
        }
    });
    state.resize.observe(wheel);
    state.reduced.addEventListener('change', state.motion);
    for (const event of ['wheel', 'touchstart', 'pointerdown']) wheel.addEventListener(event, state.input, { passive: true });
    wheel.addEventListener('scroll', state.scroll, { passive: true });
    return state;
}

function cancel(state) {
    cancelAnimationFrame(state.animation);
    clearTimeout(state.timer);
    state.animation = 0;
    state.timer = 0;
    state.target = null;
}

function detachWheel(wheel, state) {
    cancel(state);
    cancelAnimationFrame(state.frame);
    state.resize.disconnect();
    state.reduced.removeEventListener('change', state.motion);
    for (const event of ['wheel', 'touchstart', 'pointerdown']) wheel.removeEventListener(event, state.input);
    wheel.removeEventListener('scroll', state.scroll);
}

function measure(wheel, state) {
    // Batch geometry reads at render/resize boundaries, never inside the animation loop.
    state.rows = [...wheel.querySelectorAll('[role="option"]')].map(option => ({
        option, label: option.querySelector('.aeterni-time-option-list__label'),
        center: option.offsetTop + option.offsetHeight / 2, height: option.offsetHeight,
        enabled: option.getAttribute('aria-disabled') !== 'true'
    }));
    state.geometry = state.rows.map(row => `${row.option.dataset.timeValue}:${row.center}:${row.height}:${row.enabled}`).join('|');
}

function closest(wheel, state) {
    if (!wheel.clientHeight) return null;
    const center = wheel.scrollTop + wheel.clientHeight / 2;
    return state.rows.reduce((best, row) => row.enabled && (!best ||
        Math.abs(row.center - center) < Math.abs(best.center - center)) ? row : best, null);
}

function paint(wheel, state) {
    const center = wheel.scrollTop + wheel.clientHeight / 2;
    const visible = new Set();
    for (const row of state.rows) {
        const distance = (row.center - center) / Math.max(row.height, 1);
        if (Math.abs(distance) > 3 && !state.visible.has(row.option)) continue;
        if (Math.abs(distance) <= 3) visible.add(row.option);
        if (!row.label) continue;
        const depth = Math.min(Math.abs(distance), 3);
        // Local cylinder geometry; only the label transforms, not its hit area or scroll row.
        row.label.style.transform = state.reduced.matches ? 'none' :
            `perspective(${row.height * 8}px) rotateX(${Math.max(-3, Math.min(3, distance)) * -18}deg) scale(${1 - depth * 0.055})`;
        row.label.style.opacity = String(1 - depth * 0.18);
    }
    state.visible = visible;
}

function schedule(instance, wheel, state) {
    clearTimeout(state.timer);
    // Input quiet period, not an animation duration; momentum scroll resets it.
    state.timer = setTimeout(() => {
        state.timer = 0;
        settle(instance, wheel, state);
    }, 140);
}

function settle(instance, wheel, state) {
    const row = closest(wheel, state);
    if (row) align(instance, wheel, state, row, true, true);
}

function align(instance, wheel, state, row, animated, commit) {
    cancel(state);
    state.user = false;
    if (!wheel.clientHeight) return;
    const top = Math.max(0, row.center - wheel.clientHeight / 2);
    const start = wheel.scrollTop;
    const style = getComputedStyle(wheel);
    const token = style.getPropertyValue('--aeterni-duration-slow').trim();
    const duration = parseFloat(token) * (token.endsWith('ms') ? 1 : 1000) || 0;
    const ease = easing(style.getPropertyValue('--aeterni-ease-standard'));
    state.target = row;
    state.commit = commit;
    const finish = () => {
        wheel.scrollTop = top;
        state.target = null;
        state.animation = 0;
        paint(wheel, state);
        if (!commit) return;
        const value = row.option.dataset.timeValue;
        if (state.notified === value) return;
        state.notified = value;
        void instance.reference.invokeMethodAsync('OnWheelChangedAsync', wheel.dataset.timeUnit, Number(value)).catch(() => {});
    };
    if (!animated || state.reduced.matches || !duration || Math.abs(start - top) < 0.5) {
        finish();
        return;
    }
    const began = performance.now();
    const tick = now => {
        const progress = Math.min(1, (now - began) / duration);
        wheel.scrollTop = start + (top - start) * ease(progress);
        paint(wheel, state);
        if (progress < 1) state.animation = requestAnimationFrame(tick);
        else finish();
    };
    state.animation = requestAnimationFrame(tick);
}

function easing(token) {
    const values = /cubic-bezier\(([^)]+)\)/.exec(token)?.[1].split(',').map(Number);
    if (!values || values.length !== 4 || values.some(value => !Number.isFinite(value))) return x => x;
    const [x1, y1, x2, y2] = values;
    const curve = (t, a, b) => 3 * (1 - t) ** 2 * t * a + 3 * (1 - t) * t ** 2 * b + t ** 3;
    return x => {
        let low = 0, high = 1;
        for (let i = 0; i < 14; i++) {
            const mid = (low + high) / 2;
            if (curve(mid, x1, x2) < x) low = mid;
            else high = mid;
        }
        return curve((low + high) / 2, y1, y2);
    };
}
