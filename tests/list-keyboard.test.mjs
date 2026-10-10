import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';
const source = await readFile(new URL('../src/AeterniUI/Components/List/List.razor.js', import.meta.url), 'utf8');
const { init, sync, dispose } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

function fixture() {
    const calls = [];
    const listeners = new Map();
    const host = {
        role: 'listbox', disabled: false, hidden: false, options: [],
        getAttribute(name) { return name === 'role' ? this.role : name === 'aria-disabled' ? String(this.disabled) : null; },
        closest() { return this.hidden ? this : null; },
        getClientRects() { return [{}]; },
        querySelectorAll() { return this.options; },
        addEventListener(name, fn) { listeners.set(name, fn); },
        removeEventListener(name) { listeners.delete(name); }
    };
    globalThis.getComputedStyle = element => ({ visibility: element.visibility ?? 'visible' });
    function option(id, extra = {}) {
        return { id, owner: host, hidden: false, disabled: false,
            closest(selector) { return selector.includes('listbox') ? this.owner : this.hidden ? this : null; },
            getAttribute() { return String(this.disabled); }, getClientRects() { return [{}]; }, ...extra };
    }
    const a = option('a'), b = option('b'), c = option('c'); host.options = [a, b, c];
    init({ invokeMethodAsync: async (...args) => calls.push(args) }, 'test'); sync('test', host);
    function key(pressed, extra = {}) {
        const event = { key: pressed, target: host, prevented: false, stopped: false,
            preventDefault() { this.prevented = true; }, stopPropagation() { this.stopped = true; }, ...extra };
        listeners.get('keydown')?.(event); return event;
    }
    return { calls, host, a, b, c, option, key, listeners };
}

test('DOM order, visibility and ownership define candidates; only owned keys prevent scrolling', async () => {
    const f = fixture();
    try {
        f.host.options = [f.c, f.b, f.a, f.option('nested', { owner: {} })];
        f.b.hidden = true;
        const event = f.key('Home');
        assert.equal(event.prevented, true); assert.equal(event.stopped, true);
        await new Promise(resolve => setImmediate(resolve));
        assert.deepEqual(f.calls, [['HandleKeyFromBrowserAsync', 'Home', ['c', 'a']]]);
        for (const pressed of ['ArrowUp', 'ArrowDown', 'End', ' ', 'Enter']) assert.equal(f.key(pressed).prevented, true);
        for (const pressed of ['Tab', 'Escape', 'a']) assert.equal(f.key(pressed).prevented, false);
        assert.equal(f.key('Home', { ctrlKey: true }).prevented, false);
        assert.equal(f.key('Home', { target: f.a }).prevented, false);
        assert.equal(f.key('Home', { isComposing: true }).prevented, false);
        f.host.disabled = true; assert.equal(f.key('Home').prevented, false);
        f.host.disabled = false; f.host.role = null; assert.equal(f.key('Home').prevented, false);
    } finally { dispose('test'); }
    assert.equal(f.listeners.size, 0);
});

test('queued keys are serial and disposal cancels pending input', async () => {
    const f = fixture();
    f.key('Home'); f.key('End'); f.key('Enter');
    await new Promise(resolve => setImmediate(resolve));
    assert.deepEqual(f.calls.map(call => call[1]), ['Home', 'End', 'Enter']);
    f.key('Home'); dispose('test');
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(f.calls.length, 3);
});
