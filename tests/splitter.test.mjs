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

function fixture(options = {}) {
    const root = new StubElement('root');
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
