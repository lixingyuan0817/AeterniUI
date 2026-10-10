import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';
const source = await readFile(new URL('../src/AeterniUI/Components/VirtualList/VirtualList.razor.js', import.meta.url), 'utf8');
const { init, sync, dispose, scrollToIndex } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

function element(extra = {}) {
    const listeners = new Map();
    return {
        hidden: false, attributes: {}, listeners,
        closest(selector) { return this.hidden && selector.includes('hidden') ? this : null; },
        getClientRects() { return this.hidden ? [] : [{}]; },
        getAttribute(name) { return this.attributes[name] ?? null; },
        addEventListener(name, fn) { listeners.set(name, fn); },
        removeEventListener(name) { listeners.delete(name); },
        ...extra
    };
}

function fixture() {
    const calls = [];
    const viewport = element({ scrollHeight: 1000, scrollTop: 0, clientHeight: 400 });
    const root = element({
        attributes: { role: 'listbox', 'aria-disabled': 'false' },
        querySelector(selector) { return selector.includes('viewport') ? this.viewport : null; }
    });
    root.viewport = viewport;
    init({ invokeMethodAsync: async (...args) => calls.push(args) }, 'test');
    function key(pressed, extra = {}) {
        const event = { key: pressed, target: root, prevented: false, stopped: false,
            preventDefault() { this.prevented = true; }, stopPropagation() { this.stopped = true; }, ...extra };
        root.listeners.get('keydown')?.(event);
        return event;
    }
    async function settle() { await new Promise(resolve => setImmediate(resolve)); }
    return { calls, root, viewport, key, settle, element };
}

test('only owned navigation keys are consumed, and they are queued serially', async () => {
    const f = fixture();
    try {
        sync('test', f.root, false, 0);
        for (const pressed of ['ArrowDown', 'ArrowUp', 'Home', 'End', ' ', 'Enter']) {
            const event = f.key(pressed);
            assert.equal(event.prevented, true, `${pressed} must be consumed`);
            assert.equal(event.stopped, true, `${pressed} must not bubble`);
        }
        await f.settle();
        assert.deepEqual(f.calls.map(call => call[1]), ['ArrowDown', 'ArrowUp', 'Home', 'End', ' ', 'Enter']);

        for (const pressed of ['Tab', 'Escape', 'a', 'Backspace']) assert.equal(f.key(pressed).prevented, false);
        assert.equal(f.key('Home', { ctrlKey: true }).prevented, false);
        assert.equal(f.key('Home', { shiftKey: true }).prevented, false);
        assert.equal(f.key('Home', { isComposing: true }).prevented, false);
        assert.equal(f.key('Home', { target: {} }).prevented, false);
    } finally { dispose('test'); }
});

test('a root that is not an enabled, visible listbox is ignored', async () => {
    const f = fixture();
    try {
        sync('test', f.root, false, 0);
        f.root.attributes.role = null;
        assert.equal(f.key('Home').prevented, false);
        f.root.attributes.role = 'listbox';

        f.root.attributes['aria-disabled'] = 'true';
        assert.equal(f.key('Home').prevented, false);
        f.root.attributes['aria-disabled'] = 'false';

        f.root.hidden = true;
        assert.equal(f.key('Home').prevented, false);
    } finally { dispose('test'); }
});

test('load-more fires once per scroll height and re-arms only after the list grows', async () => {
    const f = fixture();
    try {
        // At the bottom (distance 0) with threshold 0: the first sync reports it.
        f.viewport.scrollTop = 600;
        sync('test', f.root, true, 0);
        await f.settle();
        assert.deepEqual(f.calls, [['HandleLoadMoreAsync']]);

        // The trigger consumed loadMoreEnabled, so further scrolling is inert
        // until the component syncs again.
        f.viewport.scrollTop = 700;
        f.viewport.listeners.get('scroll')?.();
        await f.settle();
        assert.equal(f.calls.length, 1);

        // Re-armed at the same height: the height latch still holds it back.
        sync('test', f.root, true, 0);
        await f.settle();
        assert.equal(f.calls.length, 1, 'the same scroll height must not re-trigger');

        // New rows changed the scroll height, so the next bottom hit loads again.
        f.viewport.scrollHeight = 2000;
        f.viewport.scrollTop = 1600;
        sync('test', f.root, true, 0);
        await f.settle();
        assert.deepEqual(f.calls.map(call => call[0]), ['HandleLoadMoreAsync', 'HandleLoadMoreAsync']);
    } finally { dispose('test'); }
});

test('load-more respects the threshold, the enabled flag and the disabled root', async () => {
    const f = fixture();
    try {
        // 600px of distance left, threshold 100: too far to load.
        sync('test', f.root, true, 100);
        await f.settle();
        assert.equal(f.calls.length, 0);

        f.viewport.scrollTop = 600;
        f.viewport.listeners.get('scroll')?.();
        await f.settle();
        assert.deepEqual(f.calls, [['HandleLoadMoreAsync']]);

        // Disabled host stops reporting, and a re-sync re-arms it.
        f.root.attributes['aria-disabled'] = 'true';
        f.viewport.scrollHeight = 4000;
        f.viewport.scrollTop = 3600;
        sync('test', f.root, true, 100);
        await f.settle();
        assert.equal(f.calls.length, 1);

        // loadMoreEnabled=false (no OnLoadMore handler registered) stops it too.
        f.root.attributes['aria-disabled'] = 'false';
        sync('test', f.root, false, 100);
        await f.settle();
        assert.equal(f.calls.length, 1);
    } finally { dispose('test'); }
});

test('re-syncing with a replaced viewport moves the listeners and drops the stale ones', async () => {
    const f = fixture();
    try {
        sync('test', f.root, false, 0);
        const firstViewport = f.viewport;

        const replacement = element({ scrollHeight: 800, scrollTop: 0, clientHeight: 200 });
        f.root.viewport = replacement;
        sync('test', f.root, false, 0);

        assert.equal(firstViewport.listeners.size, 0, 'the old viewport must be unbound');
        assert.equal(replacement.listeners.has('scroll'), true, 'the new viewport must be bound');

        f.key('Home');
        await f.settle();
        assert.equal(f.calls.length, 1);
    } finally { dispose('test'); }
});

test('a root without a viewport still releases the previous bindings', async () => {
    const f = fixture();
    try {
        sync('test', f.root, false, 0);
        const firstViewport = f.viewport;
        f.root.viewport = null;
        sync('test', f.root, false, 0);
        assert.equal(f.root.listeners.size, 0);
        assert.equal(firstViewport.listeners.size, 0);
    } finally { dispose('test'); }
});

test('disposal cancels queued callbacks and drops every listener', async () => {
    const f = fixture();
    sync('test', f.root, false, 0);
    const firstViewport = f.viewport;
    f.key('Home');
    dispose('test');
    await f.settle();
    assert.equal(f.calls.length, 0, 'a queued key must not reach .NET after disposal');
    assert.equal(f.root.listeners.size, 0);
    assert.equal(firstViewport.listeners.size, 0);
});

test('scrollToIndex preserves visible rows, reveals by the nearest edge and clamps invalid targets', () => {
    const f = fixture();
    try {
        sync('test', f.root, false, 0);
        scrollToIndex('test', 4, 50);
        assert.equal(f.viewport.scrollTop, 0, 'an already-visible row must not move');
        scrollToIndex('test', 10, 50);
        assert.equal(f.viewport.scrollTop, 150, 'a row below the viewport must align to the nearest edge');
        f.viewport.scrollTop = 500;
        scrollToIndex('test', 4, 50);
        assert.equal(f.viewport.scrollTop, 200);
        scrollToIndex('test', -3, 50);
        assert.equal(f.viewport.scrollTop, 0);
        scrollToIndex('test', 100, 50);
        assert.equal(f.viewport.scrollTop, 600, 'targets beyond the data extent must clamp to the scroll range');
        scrollToIndex('test', Number.NaN, 50);
        assert.equal(f.viewport.scrollTop, 600);
        scrollToIndex('test', 4, 0);
        assert.equal(f.viewport.scrollTop, 600);
        scrollToIndex('missing', 4, 50);
        assert.equal(f.viewport.scrollTop, 600);
    } finally { dispose('test'); }
});
