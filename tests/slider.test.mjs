// Slider pointer-drag regression: the module converts pointer geometry into a
// optionally snapped 0..1 fraction, reports it to C#, and releases every listener, capture
// and state class it took. The value maths and keyboard model live in C#, so
// these tests cover exactly the browser half of the contract.

import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../src/AeterniUI/Components/Slider/Slider.razor.js', import.meta.url), 'utf8');
const slider = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

class Events {
    listeners = new Map();
    addEventListener(name, callback) {
        if (!this.listeners.has(name)) this.listeners.set(name, new Set());
        this.listeners.get(name).add(callback);
    }
    removeEventListener(name, callback) { this.listeners.get(name)?.delete(callback); }
    emit(name, event) { for (const callback of [...(this.listeners.get(name) ?? [])]) callback(event); }
    listenerCount(name) { return this.listeners.get(name)?.size ?? 0; }
}

class Element extends Events {
    constructor(rect) {
        super();
        this.rect = rect;
        this.classes = new Set();
        this.properties = new Map();
        this.focused = 0;
        this.captured = new Set();
        this.classList = {
            contains: value => this.classes.has(value),
            add: (...values) => values.forEach(value => this.classes.add(value)),
            remove: (...values) => values.forEach(value => this.classes.delete(value))
        };
        this.style = { setProperty: (name, value) => this.properties.set(name, value) };
    }
    getBoundingClientRect() { return this.rect; }
    querySelector() { return this.track ?? null; }
    focus() { this.focused += 1; }
    setPointerCapture(pointerId) { this.captured.add(pointerId); }
    releasePointerCapture(pointerId) { this.captured.delete(pointerId); }
    hasPointerCapture(pointerId) { return this.captured.has(pointerId); }
}

function environment({ direction = 'ltr' } = {}) {
    globalThis.getComputedStyle = () => ({ direction });
}

function reference() {
    return { calls: [], invokeMethodAsync(name, ...args) { this.calls.push([name, ...args]); return Promise.resolve(); } };
}

function pointerEvent(type, overrides = {}) {
    const event = { pointerId: 1, button: 0, clientX: 0, clientY: 0, prevented: 0, ...overrides };
    event.preventDefault = () => { event.prevented += 1; };
    return event;
}

function keyEvent(key, overrides = {}) {
    const event = { key, altKey: false, ctrlKey: false, metaKey: false, shiftKey: false, isComposing: false, prevented: 0, ...overrides };
    event.preventDefault = () => { event.prevented += 1; };
    return event;
}

function setup({ rect = { left: 100, top: 0, width: 200, height: 100, bottom: 100 }, minimum = 0, maximum = 100, step = 5, orientation = 'horizontal', enabled = true, direction = 'ltr' } = {}) {
    environment({ direction });
    const root = new Element(rect);
    const track = new Element(rect);
    root.track = track;
    const ref = reference();
    slider.init(ref, 'slider');
    slider.sync('slider', root, enabled, minimum, maximum, step, orientation);
    return { root, track, ref };
}

test('slider: the pointer fraction is measured on the track and snapped to the step grid', () => {
    const { root, ref } = setup();
    try {
        assert.equal(root.listenerCount('pointerdown'), 1);
        assert.equal(root.listenerCount('keydown'), 1);

        root.emit('pointerdown', pointerEvent('pointerdown', { clientX: 150, clientY: 50 }));
        assert.equal(root.properties.get('--aeterni-slider-value'), '0.2500');
        assert.deepEqual(ref.calls.at(-1), ['HandlePointerFractionAsync', 0.25]);
        assert.equal(root.classes.has('is-dragging'), true);
        assert.equal(root.focused, 1, 'pressing the track must focus the slider');
        assert.equal(root.captured.has(1), true, 'the drag must capture the pointer');

        root.emit('pointermove', pointerEvent('pointermove', { clientX: 166, clientY: 50 }));
        assert.equal(root.properties.get('--aeterni-slider-value'), '0.3500', '0.33 must snap to the 0.05 grid');
        assert.deepEqual(ref.calls.at(-1), ['HandlePointerFractionAsync', 0.35]);

        root.emit('pointermove', pointerEvent('pointermove', { clientX: 10000, clientY: 50 }));
        assert.equal(root.properties.get('--aeterni-slider-value'), '1.0000');

        root.emit('pointermove', pointerEvent('pointermove', { clientX: -1000, clientY: 50 }));
        assert.equal(root.properties.get('--aeterni-slider-value'), '0.0000');
    } finally {
        slider.dispose('slider');
    }

    assert.equal(root.listenerCount('pointerdown'), 0, 'dispose must release every drag listener');
    assert.equal(root.listenerCount('keydown'), 0);
});

test('slider: a released drag keeps no listeners and no state class', () => {
    const { root, ref } = setup();
    try {
        root.emit('pointerdown', pointerEvent('pointerdown', { clientX: 150, clientY: 50 }));
        const callsAfterPress = ref.calls.length;

        root.emit('pointerup', pointerEvent('pointerup', { clientX: 150, clientY: 50 }));
        assert.equal(root.classes.has('is-dragging'), false);
        assert.equal(root.captured.has(1), false, 'the capture must be released');
        assert.equal(root.listenerCount('pointermove'), 0);
        assert.equal(root.listenerCount('pointerup'), 0);
        assert.equal(root.listenerCount('pointercancel'), 0);

        root.emit('pointermove', pointerEvent('pointermove', { clientX: 200, clientY: 50 }));
        assert.equal(ref.calls.length, callsAfterPress, 'moves after the release must not commit');

        // A cancelled pointer behaves like a release.
        root.emit('pointerdown', pointerEvent('pointerdown', { clientX: 150, clientY: 50 }));
        root.emit('pointercancel', pointerEvent('pointercancel', { clientX: 150, clientY: 50 }));
        assert.equal(root.classes.has('is-dragging'), false);
        assert.equal(root.listenerCount('pointermove'), 0);
    } finally {
        slider.dispose('slider');
    }
});

test('slider: moves inside one step do not repeat the callback', () => {
    const { root, ref } = setup();
    try {
        root.emit('pointerdown', pointerEvent('pointerdown', { clientX: 150, clientY: 50 }));
        const calls = ref.calls.length;

        root.emit('pointermove', pointerEvent('pointermove', { clientX: 151, clientY: 50 }));
        root.emit('pointermove', pointerEvent('pointermove', { clientX: 152, clientY: 50 }));
        assert.equal(ref.calls.length, calls, 'the same snapped value must be reported once');
    } finally {
        slider.dispose('slider');
    }
});

test('slider: a zero transport step keeps pointer input continuous', () => {
    const { root, ref } = setup({ step: 0 });
    try {
        root.emit('pointerdown', pointerEvent('pointerdown', { clientX: 166, clientY: 50 }));
        assert.equal(root.properties.get('--aeterni-slider-value'), '0.3300');
        assert.deepEqual(ref.calls.at(-1), ['HandlePointerFractionAsync', 0.33]);
    } finally {
        slider.dispose('slider');
    }
});

test('slider: a render arriving mid-drag does not disturb the live position', () => {
    const { root, ref } = setup();
    try {
        root.emit('pointerdown', pointerEvent('pointerdown', { clientX: 150, clientY: 50 }));
        assert.equal(root.properties.get('--aeterni-slider-value'), '0.2500');

        // C# re-renders while the pointer is down (it just committed 0.25).
        slider.sync('slider', root, true, 0, 100, 5, 'horizontal');

        assert.equal(root.listenerCount('pointerdown'), 1, 'the drag listeners must survive a render');
        root.emit('pointermove', pointerEvent('pointermove', { clientX: 166, clientY: 50 }));
        assert.equal(root.properties.get('--aeterni-slider-value'), '0.3500');
    } finally {
        slider.dispose('slider');
    }
});

test('slider: RTL mirrors the fraction and is reported to C# once', () => {
    const { root, ref } = setup({ direction: 'rtl' });
    try {
        assert.deepEqual(ref.calls[0], ['HandleDirectionAsync', true]);

        root.emit('pointerdown', pointerEvent('pointerdown', { clientX: 150, clientY: 50 }));
        assert.equal(root.properties.get('--aeterni-slider-value'), '0.7500');
        assert.deepEqual(ref.calls.at(-1), ['HandlePointerFractionAsync', 0.75]);
    } finally {
        slider.dispose('slider');
    }
});

test('slider: the vertical layout measures from the bottom edge', () => {
    const { root, ref } = setup({ orientation: 'vertical' });
    try {
        assert.equal(ref.calls.some(call => call[0] === 'HandleDirectionAsync'), false, 'direction only applies to the horizontal track');

        root.emit('pointerdown', pointerEvent('pointerdown', { clientX: 110, clientY: 75 }));
        assert.equal(root.properties.get('--aeterni-slider-value'), '0.2500');
        assert.deepEqual(ref.calls.at(-1), ['HandlePointerFractionAsync', 0.25]);

        root.emit('pointermove', pointerEvent('pointermove', { clientX: 110, clientY: 0 }));
        assert.equal(root.properties.get('--aeterni-slider-value'), '1.0000');
    } finally {
        slider.dispose('slider');
    }
});

test('slider: the key guard prevents page scrolling for handled keys only', () => {
    const { root } = setup();
    try {
        const arrow = keyEvent('ArrowRight');
        root.emit('keydown', arrow);
        assert.equal(arrow.prevented, 1);

        const home = keyEvent('Home');
        root.emit('keydown', home);
        assert.equal(home.prevented, 1);

        const letter = keyEvent('a');
        root.emit('keydown', letter);
        assert.equal(letter.prevented, 0);

        const chord = keyEvent('ArrowUp', { shiftKey: true });
        root.emit('keydown', chord);
        assert.equal(chord.prevented, 0);
    } finally {
        slider.dispose('slider');
    }
});

test('slider: a disabled slider binds nothing and ignores pointers', () => {
    const { root, ref } = setup({ enabled: false });
    try {
        assert.equal(root.listenerCount('pointerdown'), 0);
        assert.equal(root.listenerCount('keydown'), 0);

        root.emit('pointerdown', pointerEvent('pointerdown', { clientX: 150, clientY: 50 }));
        assert.equal(root.properties.has('--aeterni-slider-value'), false);
        assert.equal(ref.calls.filter(call => call[0] === 'HandlePointerFractionAsync').length, 0);
        assert.equal(root.classes.has('is-dragging'), false);
    } finally {
        slider.dispose('slider');
    }
});

test('slider: disabling mid-drag ends the drag cleanly', () => {
    const { root } = setup();
    try {
        root.emit('pointerdown', pointerEvent('pointerdown', { clientX: 150, clientY: 50 }));
        assert.equal(root.classes.has('is-dragging'), true);

        slider.sync('slider', root, false, 0, 100, 5, 'horizontal');
        assert.equal(root.classes.has('is-dragging'), false);
        assert.equal(root.listenerCount('pointermove'), 0);
        assert.equal(root.listenerCount('pointerdown'), 0);
    } finally {
        slider.dispose('slider');
    }
});

test('slider: re-init replaces the previous instance without leaking listeners', () => {
    const first = setup();
    const second = setup();
    try {
        assert.equal(first.root.listenerCount('pointerdown'), 0, 're-init must release the old root');
        assert.equal(first.root.listenerCount('keydown'), 0);
        assert.equal(second.root.listenerCount('pointerdown'), 1);
    } finally {
        slider.dispose('slider');
    }

    assert.equal(second.root.listenerCount('pointerdown'), 0);
});
