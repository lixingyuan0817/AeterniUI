import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';
const helpers = await readFile(new URL('../src/AeterniUI/wwwroot/js/aeterni_floating.js', import.meta.url), 'utf8');
const source = (await readFile(new URL('../src/AeterniUI/Components/Popup/Popover.razor.js', import.meta.url), 'utf8'))
    .replace('../../js/aeterni_floating.js', `data:text/javascript;base64,${Buffer.from(helpers).toString('base64')}`);
const { init, setOpen, dispose } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

function fixture() {
    const listeners = new Map();
    globalThis.window = { innerWidth: 1000, innerHeight: 800, addEventListener() {}, removeEventListener() {} };
    globalThis.document = { addEventListener: (name, fn) => listeners.set(name, fn), removeEventListener: name => listeners.delete(name) };
    class Element {
        constructor(parent = null) { this.parentElement = parent; this.isConnected = true; }
        contains(node) { for (let n = node; n; n = n.parentElement) if (n === this) return true; return false; }
        matches() { return !!this.disabled; }
        closest() { return this.hidden ? this : null; }
        focus() { document.activeElement = this; }
        getBoundingClientRect() { return { top: 100, bottom: 200, left: 100, right: 300, width: 200, height: 100 }; }
        classList = { add() {}, remove() {} };
        style = { setProperty() {} };
    }
    globalThis.Node = globalThis.HTMLElement = Element;
    const body = document.body = new Element();
    const host = new Element(body), trigger = new Element(host), layer = new Element(host), item = new Element(layer), outside = new Element(body);
    let closes = 0;
    const sync = open => setOpen('focus', open, layer, false, 'bottom-end', true, true, true, false);
    init({ invokeMethodAsync() { closes++; return Promise.resolve(); } }, 'focus');
    trigger.focus(); sync(true);
    return { trigger, layer, item, outside, listeners, sync, get closes() { return closes; } };
}

test('selection and Escape restore menu opener; outside click and Tab focus are preserved', async () => {
    for (const mode of ['selection', 'escape', 'outside', 'tab', 'callback', 'disabled']) {
        const f = fixture();
        try {
            f.item.focus();
            if (mode === 'escape') {
                f.listeners.get('keydown')({ key: 'Escape', stopPropagation() {} });
                assert.equal(f.closes, 1);
            }
            if (mode === 'outside') {
                f.listeners.get('pointerdown')({ target: f.outside });
                assert.equal(f.closes, 1);
            }
            if (['outside', 'tab', 'callback'].includes(mode)) f.outside.focus();
            if (mode === 'tab') {
                f.listeners.get('keydown')({ key: 'Tab', stopPropagation() { assert.fail('Tab must be native'); } });
                assert.equal(f.closes, 0, 'Tab alone does not close existing Popover');
            }
            if (mode === 'disabled') f.trigger.disabled = true;
            f.sync(false);
            await Promise.resolve();
            assert.equal(document.activeElement, ['outside', 'tab', 'callback'].includes(mode) ? f.outside : mode === 'disabled' ? f.item : f.trigger, mode);
        } finally { dispose('focus'); await Promise.resolve(); }
    }
});
