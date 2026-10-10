import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';
const source = await readFile(new URL('../src/AeterniUI/Components/MultiSelect/MultiSelect.razor.js', import.meta.url), 'utf8');
const { init, sync, dispose } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

function element(extra = {}) {
    const listeners = new Map();
    return {
        attributes: {}, listeners,
        getAttribute(name) { return this.attributes[name] ?? null; },
        addEventListener(name, fn) { listeners.set(name, fn); },
        removeEventListener(name) { listeners.delete(name); },
        ...extra
    };
}

function fixture() {
    // The module reads the active option through the document and scrolls its row
    // into the options viewport, so both are stubbed here.
    const boxes = new Map();
    globalThis.document = { getElementById: id => boxes.get(id) ?? null };
    const trigger = element({ attributes: { 'aria-disabled': 'false', 'aria-expanded': 'false' } });
    init({ invokeMethodAsync: async () => {} }, 'test');
    function key(pressed, extra = {}) {
        const event = { key: pressed, target: trigger, prevented: false, stopped: false,
            preventDefault() { this.prevented = true; }, stopPropagation() { this.stopped = true; }, ...extra };
        trigger.listeners.get('keydown')?.(event);
        return event;
    }
    return { trigger, key, boxes, element };
}

test('consumes navigation keys and only consumes Enter/Space while expanded', () => {
    const f = fixture();
    try {
        sync('test', f.trigger);
        for (const pressed of ['ArrowDown', 'ArrowUp', 'Home', 'End', 'Escape', 'Backspace']) {
            assert.equal(f.key(pressed).prevented, true, `${pressed} must be consumed`);
        }
        assert.equal(f.key('Enter').prevented, false, 'a closed combobox must not swallow Enter');
        assert.equal(f.key(' ').prevented, false);

        f.trigger.attributes['aria-expanded'] = 'true';
        assert.equal(f.key('Enter').prevented, true);
        assert.equal(f.key(' ').prevented, true);
        assert.equal(f.key('a').prevented, false);
    } finally { dispose('test'); }
});

test('ignores modified, composing and unowned events', () => {
    const f = fixture();
    try {
        sync('test', f.trigger);
        for (const modifier of ['altKey', 'ctrlKey', 'metaKey', 'shiftKey']) {
            assert.equal(f.key('ArrowDown', { [modifier]: true }).prevented, false, `${modifier} must be ignored`);
        }
        assert.equal(f.key('ArrowDown', { target: {} }).prevented, false);

        const composing = f.key('ArrowDown', { isComposing: true });
        assert.equal(composing.prevented, false, 'IME composition must not be preventDefaulted');
        assert.equal(composing.stopped, true, 'IME composition must not bubble');

        f.trigger.attributes['aria-disabled'] = 'true';
        assert.equal(f.key('ArrowDown').prevented, false);
    } finally { dispose('test'); }
});

test('the active option is scrolled into its own viewport, never the page', () => {
    const f = fixture();
    try {
        const viewport = { scrollTop: 100, getBoundingClientRect: () => ({ top: 0, bottom: 200 }) };
        f.boxes.set('opt', { parentElement: viewport, getBoundingClientRect: () => ({ top: -30, bottom: 10 }) });
        f.trigger.attributes['aria-activedescendant'] = 'opt';
        sync('test', f.trigger);
        assert.equal(viewport.scrollTop, 70, 'a row above the viewport scrolls it up');

        f.boxes.set('opt', { parentElement: viewport, getBoundingClientRect: () => ({ top: 190, bottom: 260 }) });
        viewport.scrollTop = 100;
        sync('test', f.trigger);
        assert.equal(viewport.scrollTop, 160, 'a row below the viewport scrolls it down');
    } finally { dispose('test'); }
});

test('a replaced or missing trigger unbinds the previous listener', () => {
    const f = fixture();
    try {
        sync('test', f.trigger);
        assert.equal(f.trigger.listeners.has('keydown'), true);

        const replacement = f.element({ attributes: { 'aria-disabled': 'false' } });
        sync('test', replacement);
        assert.equal(f.trigger.listeners.size, 0, 'the replaced trigger must be unbound');
        assert.equal(replacement.listeners.has('keydown'), true);
        assert.equal(f.key('ArrowDown').prevented, false, 'the stale trigger must go inert');

        // Regression guard: an empty element reference used to return early and
        // leave the listener attached to a detached subtree for the page's life.
        sync('test', null);
        assert.equal(replacement.listeners.size, 0, 'a null trigger must still unbind');
    } finally { dispose('test'); }
});

test('disposal drops the listener and forgets the instance', () => {
    const f = fixture();
    sync('test', f.trigger);
    dispose('test');
    assert.equal(f.trigger.listeners.size, 0);
    sync('test', f.trigger);
    assert.equal(f.trigger.listeners.size, 0, 'a disposed instance must not rebind');
});
