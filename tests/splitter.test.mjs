import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../src/AeterniUI/Components/Splitter/Splitter.razor.js', import.meta.url), 'utf8');
const { init, attach, sync, dispose } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

class StubElement {
    constructor(kind) {
        this.kind = kind;
        this.attrs = new Map();
        this.listeners = new Map();
        this.classList = {
            values: new Set(),
            add: value => this.classList.values.add(value),
            remove: value => this.classList.values.delete(value)
        };
        this.style = {
            values: new Map(),
            setProperty: (name, value) => this.style.values.set(name, value),
            cursor: '',
            userSelect: ''
        };
        this.pointerId = 0;
    }

    getAttribute(name) { return this.attrs.get(name) ?? null; }
    setAttribute(name, value) { this.attrs.set(name, value); }
    removeAttribute(name) { this.attrs.delete(name); }
    addEventListener(name, listener) { this.listeners.set(name, listener); }
    removeEventListener(name) { this.listeners.delete(name); }
    setPointerCapture(id) { this.pointerId = id; }
    releasePointerCapture() { this.pointerId = 0; }
    dispatch(name, event = {}) {
        const handler = this.listeners.get(name);
        if (!handler) return false;
        handler({ preventDefault() {}, button: 0, pointerId: 1, ...event });
        return true;
    }
}

const storage = new Map();
globalThis.localStorage = {
    getItem: key => storage.get(key) ?? null,
    setItem: (key, value) => storage.set(key, String(value)),
    clear: () => storage.clear()
};
globalThis.window = {
    listeners: new Map(),
    addEventListener(name, listener) { this.listeners.set(name, listener); },
    removeEventListener(name) { this.listeners.delete(name); }
};
globalThis.document = {
    body: { style: { cursor: '', userSelect: '' } },
    addEventListener() {},
    removeEventListener() {}
};

function fixture(options = {}, container = {}) {
    const root = new StubElement('root');
    // A detached-style measurement: 0 means "not laid out yet", which the module
    // maps back to the configured maximum.
    root.clientWidth = container.width ?? 0;
    root.clientHeight = container.height ?? 0;
    const handle = new StubElement('handle');
    const calls = [];
    const dotNetRef = { invokeMethodAsync: async (name, value) => calls.push([name, value]) };
    init(dotNetRef, 'splitter-1');
    attach('splitter-1', root, handle, {
        orientation: 'horizontal',
        min: 200,
        max: 600,
        firstSize: 300,
        persistKey: 'splitter-test',
        disabled: false,
        ...options
    });
    return { root, handle, calls, dotNetRef };
}

test('dragging writes a clamped CSS width, persists it and notifies Blazor once', () => {
    storage.clear();
    const { root, handle, calls } = fixture();
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '300px');
    assert.equal(handle.getAttribute('aria-valuenow'), '300');

    handle.dispatch('pointerdown', { clientX: 300 });
    window.listeners.get('pointermove')({ clientX: 360, preventDefault() {} });
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '360px');
    assert.equal(calls.length, 0, 'pointer move must not round-trip through Blazor');

    window.listeners.get('pointerup')({ pointerId: 1, preventDefault() {} });
    assert.equal(storage.get('splitter-test'), '360');
    assert.deepEqual(calls, [['OnSizeChangedAsync', 360]]);
    assert.equal(handle.classList.values.has('is-dragging'), false);
    dispose('splitter-1');
});

test('keyboard, Home/End and double click use the same commit path and clamp', () => {
    storage.clear();
    const { root, handle, calls } = fixture({ firstSize: 300 });
    handle.dispatch('keydown', { key: 'ArrowRight', shiftKey: false });
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '308px');
    handle.dispatch('keydown', { key: 'ArrowRight', shiftKey: true });
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '340px');
    handle.dispatch('keydown', { key: 'End' });
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '600px');
    handle.dispatch('keydown', { key: 'Home' });
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '200px');
    handle.dispatch('dblclick');
    assert.equal(calls.at(-1)[1], 200);
    assert.equal(storage.get('splitter-test'), '200');
    dispose('splitter-1');
});

test('vertical orientation uses Y deltas and restores a stored width', () => {
    storage.clear();
    storage.set('splitter-test', '420');
    const { root, handle, calls } = fixture({ orientation: 'vertical' });
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '420px');
    assert.deepEqual(calls, [['OnSizeRestoredAsync', 420]]);

    handle.dispatch('pointerdown', { clientY: 100 });
    window.listeners.get('pointermove')({ clientY: 130, preventDefault() {} });
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '450px');
    window.listeners.get('pointerup')({ pointerId: 1, preventDefault() {} });
    assert.deepEqual(calls.at(-1), ['OnSizeChangedAsync', 450]);
    dispose('splitter-1');
});

test('sync applies an external size and clamp without re-registering listeners', () => {
    storage.clear();
    const { root, handle } = fixture();
    sync('splitter-1', root, handle, { orientation: 'horizontal', min: 200, max: 600, firstSize: 900 });
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '600px');
    assert.equal(handle.listeners.size, 3);
    dispose('splitter-1');
    assert.equal(handle.listeners.size, 0);
});

test('a legal zero size is not mistaken for "no size yet"', () => {
    storage.clear();
    const { root, handle } = fixture({ min: 0, firstSize: 0 });
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '0px');
    assert.equal(handle.getAttribute('aria-valuenow'), '0');

    sync('splitter-1', root, handle, { orientation: 'horizontal', min: 0, max: 600, firstSize: 0 });
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '0px');
    assert.equal(handle.getAttribute('aria-valuenow'), '0');
    dispose('splitter-1');
});

test('only the keys this module owns prevent the default action', () => {
    storage.clear();
    const { handle } = fixture();
    const prevented = [];
    const handler = handle.listeners.get('keydown');
    const send = (key, extra = {}) => handler({ key, preventDefault: () => prevented.push(key), ...extra });

    send('ArrowRight');
    send('ArrowLeft');
    send('Home');
    send('End');
    send('PageDown');
    assert.deepEqual(prevented, ['ArrowRight', 'ArrowLeft', 'Home', 'End']);

    // A horizontal splitter ignores the vertical arrows so the page can still scroll.
    prevented.length = 0;
    send('ArrowUp');
    send('ArrowDown');
    assert.deepEqual(prevented, []);
    dispose('splitter-1');
});

test('disposing mid-drag releases the window listeners, pointer capture and body cursor', () => {
    storage.clear();
    const { handle } = fixture();
    handle.dispatch('pointerdown', { clientX: 300 });
    assert.equal(window.listeners.size, 3);
    assert.equal(handle.pointerId, 1);
    assert.equal(document.body.style.cursor, 'col-resize');
    assert.equal(handle.classList.values.has('is-dragging'), true);

    dispose('splitter-1');
    assert.equal(window.listeners.size, 0);
    assert.equal(handle.listeners.size, 0);
    assert.equal(handle.pointerId, 0);
    assert.equal(handle.classList.values.has('is-dragging'), false);
    assert.equal(document.body.style.cursor, '');
    assert.equal(document.body.style.userSelect, '');
});

test('disabled splitter ignores pointer and keyboard input and reports aria-disabled', () => {
    storage.clear();
    const { root, handle, calls } = fixture({ disabled: true });
    assert.equal(handle.getAttribute('aria-disabled'), 'true');

    handle.dispatch('pointerdown', { clientX: 300 });
    assert.equal(window.listeners.size, 0, 'a disabled handle must not start a drag');
    handle.dispatch('keydown', { key: 'ArrowRight' });
    assert.equal(calls.length, 0, 'a disabled splitter must not commit');

    sync('splitter-1', root, handle, { orientation: 'horizontal', min: 200, max: 600, firstSize: 300, disabled: false });
    assert.equal(handle.getAttribute('aria-disabled'), null);
    handle.dispatch('keydown', { key: 'ArrowRight' });
    assert.deepEqual(calls.at(-1), ['OnSizeChangedAsync', 308]);
    dispose('splitter-1');
});

test('a stored width is clamped into the current range', () => {
    storage.clear();
    storage.set('splitter-test', '900');
    const { root, calls } = fixture();
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '600px');
    assert.deepEqual(calls, [['OnSizeRestoredAsync', 600]]);
    dispose('splitter-1');
});

test('RTL horizontal splitters mirror drag deltas and arrow keys', () => {
    storage.clear();
    globalThis.getComputedStyle = () => ({ direction: 'rtl' });
    try {
        const { root, handle } = fixture({ firstSize: 300 });

        handle.dispatch('pointerdown', { clientX: 500 });
        window.listeners.get('pointermove')({ clientX: 440, preventDefault() {} });
        assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '360px');
        window.listeners.get('pointerup')({ pointerId: 1, preventDefault() {} });

        // In RTL the first pane sits on the right, so ArrowRight shrinks it.
        handle.dispatch('keydown', { key: 'ArrowRight' });
        assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '352px');
        handle.dispatch('keydown', { key: 'ArrowLeft' });
        assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '360px');
        dispose('splitter-1');
    } finally {
        delete globalThis.getComputedStyle;
    }
});

test('the configured max is capped by the container so the seam never leaves it', () => {
    storage.clear();
    // 396px container with a 600px configured max: the reachable ceiling becomes
    // 396 - 24 (touch target) = 372, so the handle stays inside the clipped box.
    const { root, handle, calls } = fixture({ firstSize: 300 }, { width: 396 });
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '300px');

    handle.dispatch('keydown', { key: 'End' });
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '372px');
    assert.equal(handle.getAttribute('aria-valuenow'), '372');

    handle.dispatch('pointerdown', { clientX: 0 });
    window.listeners.get('pointermove')({ clientX: 500, preventDefault() {} });
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '372px',
        'a pointer drag is clamped by the effective ceiling too');
    window.listeners.get('pointerup')({ pointerId: 1, preventDefault() {} });
    assert.deepEqual(calls.at(-1), ['OnSizeChangedAsync', 372]);
    assert.equal(storage.get('splitter-test'), '372');
    dispose('splitter-1');
});

test('an unmeasured container keeps the configured maximum', () => {
    storage.clear();
    const { root, handle } = fixture({ firstSize: 300 });
    handle.dispatch('keydown', { key: 'End' });
    assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '600px');
    dispose('splitter-1');
});

test('the effective ceiling uses the container axis and the touch-target token', () => {
    storage.clear();
    const token = '--aeterni-touch-target-min';
    globalThis.getComputedStyle = () => ({
        direction: 'ltr',
        getPropertyValue: name => (name === token ? '32px' : '')
    });
    try {
        const { root, handle } = fixture({ orientation: 'vertical', firstSize: 200 }, { width: 900, height: 300 });
        handle.dispatch('keydown', { key: 'End' });
        // Vertical uses clientHeight (300) and the page's 32px touch target: 268.
        assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '268px');
        dispose('splitter-1');
    } finally {
        delete globalThis.getComputedStyle;
    }
});

test('a shrinking container re-clamps the committed width and disconnects on dispose', () => {
    storage.clear();
    const observers = [];
    globalThis.ResizeObserver = class {
        constructor(callback) { this.callback = callback; this.disconnected = false; observers.push(this); }
        observe() {}
        disconnect() { this.disconnected = true; }
    };
    try {
        const { root, handle, calls } = fixture({ firstSize: 300 }, { width: 600 });
        handle.dispatch('keydown', { key: 'End' });
        assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '576px');

        root.clientWidth = 400;
        observers[0].callback([]);
        assert.equal(root.style.values.get('--aeterni-splitter-first-size'), '376px');
        assert.deepEqual(calls.at(-1), ['OnSizeChangedAsync', 376]);
        assert.equal(storage.get('splitter-test'), '376');

        dispose('splitter-1');
        assert.equal(observers[0].disconnected, true, 'disposing must release the ResizeObserver');
    } finally {
        delete globalThis.ResizeObserver;
    }
});
