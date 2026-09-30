import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';
const source = await readFile(new URL('../src/AeterniUI/Components/Autocomplete/Autocomplete.razor.js', import.meta.url), 'utf8');
const { init, sync, dispose } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

function element(extra = {}) {
    const listeners = new Map();
    return {
        disabled: false, attributes: {}, listeners,
        getAttribute(name) { return this.attributes[name] ?? null; },
        addEventListener(name, fn) { listeners.set(name, fn); },
        removeEventListener(name) { listeners.delete(name); },
        ...extra
    };
}

function fixture() {
    const boxes = new Map();
    globalThis.document = { getElementById: id => boxes.get(id) ?? null };
    const input = element({ attributes: { 'aria-expanded': 'false' } });
    init({ invokeMethodAsync: async () => {} }, 'test');
    function key(pressed, extra = {}) {
        const event = { key: pressed, target: input, prevented: false, stopped: false,
            preventDefault() { this.prevented = true; }, stopPropagation() { this.stopped = true; }, ...extra };
        input.listeners.get('keydown')?.(event);
        return event;
    }
    return { input, key, boxes, element };
}

test('consumes navigation keys and only consumes Enter while expanded', () => {
    const f = fixture();
    try {
        sync('test', f.input);
        for (const pressed of ['ArrowDown', 'ArrowUp', 'Home', 'End', 'Escape']) {
            assert.equal(f.key(pressed).prevented, true, `${pressed} must be consumed`);
        }
        assert.equal(f.key('Enter').prevented, false, 'a closed suggestion list must not swallow Enter');
        assert.equal(f.key('Backspace').prevented, false, 'free text editing must not be intercepted');

        f.input.attributes['aria-expanded'] = 'true';
        assert.equal(f.key('Enter').prevented, true);
        assert.equal(f.key('a').prevented, false);
    } finally { dispose('test'); }
});

test('ignores modified, composing, disabled and unowned events', () => {
    const f = fixture();
    try {
        sync('test', f.input);
        for (const modifier of ['altKey', 'ctrlKey', 'metaKey', 'shiftKey']) {
            assert.equal(f.key('ArrowDown', { [modifier]: true }).prevented, false, `${modifier} must be ignored`);
        }
        assert.equal(f.key('ArrowDown', { target: {} }).prevented, false);

        const composing = f.key('ArrowDown', { isComposing: true });
        assert.equal(composing.prevented, false, 'IME composition must not be preventDefaulted');
        assert.equal(composing.stopped, true, 'IME composition must not bubble');

        f.input.disabled = true;
        assert.equal(f.key('ArrowDown').prevented, false);
    } finally { dispose('test'); }
});

test('the active suggestion is scrolled into its own viewport, never the page', () => {
    const f = fixture();
    try {
        const viewport = { scrollTop: 100, getBoundingClientRect: () => ({ top: 0, bottom: 200 }) };
        f.boxes.set('opt', { parentElement: viewport, getBoundingClientRect: () => ({ top: -30, bottom: 10 }) });
        f.input.attributes['aria-activedescendant'] = 'opt';
        sync('test', f.input);
        assert.equal(viewport.scrollTop, 70, 'a row above the viewport scrolls it up');

        f.boxes.set('opt', { parentElement: viewport, getBoundingClientRect: () => ({ top: 190, bottom: 260 }) });
        viewport.scrollTop = 100;
        sync('test', f.input);
        assert.equal(viewport.scrollTop, 160, 'a row below the viewport scrolls it down');
    } finally { dispose('test'); }
});

test('a replaced or missing input unbinds the previous listener', () => {
    const f = fixture();
    try {
        sync('test', f.input);
        assert.equal(f.input.listeners.has('keydown'), true);

        const replacement = f.element({ attributes: { 'aria-expanded': 'false' } });
        sync('test', replacement);
        assert.equal(f.input.listeners.size, 0, 'the replaced input must be unbound');
        assert.equal(replacement.listeners.has('keydown'), true);
        assert.equal(f.key('ArrowDown').prevented, false, 'the stale input must go inert');

        // Regression guard: an empty element reference used to return early and
        // leave the listener attached to a detached subtree for the page's life.
        sync('test', null);
        assert.equal(replacement.listeners.size, 0, 'a null input must still unbind');
    } finally { dispose('test'); }
});

test('disposal drops the listener and forgets the instance', () => {
    const f = fixture();
    sync('test', f.input);
    dispose('test');
    assert.equal(f.input.listeners.size, 0);
    sync('test', f.input);
    assert.equal(f.input.listeners.size, 0, 'a disposed instance must not rebind');
});
