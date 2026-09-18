import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';
const source = await readFile(new URL('../src/AeterniUI/Components/Toolbar/Toolbar.razor.js', import.meta.url), 'utf8');
const { init, sync, dispose } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

function fixture() {
    let observer;
    let records = [];
    globalThis.MutationObserver = class {
        constructor(callback) { observer = callback; }
        takeRecords() { const result = records; records = []; return result; }
        disconnect() {}
        observe() {}
    };
    globalThis.getComputedStyle = node => ({ visibility: node.visibility ?? 'visible', direction: node.direction ?? 'ltr' });
    const doc = { activeElement: null, addEventListener() {}, removeEventListener() {} };
    class Element {
        constructor(kind, parent = null) { this.kind = kind; this.parent = parent; this.attrs = new Map(); this.listeners = new Map(); this.ownerDocument = doc; this.children = []; if (parent) parent.children.push(this); }
        getAttribute(name) { return this.attrs.get(name) ?? null; }
        setAttribute(name, value) { this.attrs.set(name, value); }
        removeAttribute(name) { this.attrs.delete(name); }
        matches(selector) { return selector === ':disabled' ? this.attrs.has('disabled') : this.kind === 'button'; }
        closest(selector) {
            for (let n = this; n; n = n.parent) {
                if (selector === '[role="toolbar"]' && n.kind === 'toolbar') return n;
                if (selector.includes('.aeterni-popover') && n.kind === 'popup') return n;
                if (selector.includes('button.') && n.kind === 'button') return n;
                if (selector.includes('[hidden]') && (n.attrs.has('hidden') || n.attrs.has('inert'))) return n;
                if (selector.includes('aria-disabled') && (n.getAttribute('aria-disabled') === 'true' || n.getAttribute('aria-busy') === 'true')) return n;
            }
            return null;
        }
        getClientRects() { return this.closest('[hidden], [inert]') ? [] : [{}]; }
        querySelectorAll() { return this.children.flatMap(n => [ ...(n.kind === 'button' ? [n] : []), ...n.querySelectorAll() ]); }
        contains(node) { for (let n = node; n; n = n.parent) if (n === this) return true; return false; }
        addEventListener(name, fn) { this.listeners.set(name, fn); }
        removeEventListener(name) { this.listeners.delete(name); }
        focus() { doc.activeElement = this; host.listeners.get('focusin')?.({ target: this }); }
        blur() { doc.activeElement = doc.body; }
    }
    doc.body = new Element('body');
    const host = new Element('toolbar', doc.body);
    host.setAttribute('tabindex', '-1');
    const a = new Element('button', host), b = new Element('button', host), c = new Element('button', host);
    b.setAttribute('tabindex', '4');
    const popup = new Element('popup', host), menu = new Element('button', popup);
    const nested = new Element('toolbar', host), nestedButton = new Element('button', nested);
    const key = (key, target = doc.activeElement, extras = {}) => {
        const event = { key, target, prevented: false, stopped: false, preventDefault() { this.prevented = true; }, stopPropagation() { this.stopped = true; }, ...extras };
        host.listeners.get('keydown')(event); return event;
    };
    init(null, 'test'); sync('test', host);
    return { host, a, b, c, menu, nestedButton, doc, key, update: () => observer([]), authorTab: (node, value) => { node.setAttribute('tabindex', value); records.push({ attributeName: 'tabindex', target: node }); }, Element };
}

test('one tab stop, wrap, Home/End, RTL and orthogonal keys preserve activation', () => {
    const { host, a, b, c, menu, nestedButton, doc, key } = fixture();
    try {
        assert.equal(a.getAttribute('tabindex'), '0'); assert.equal(b.getAttribute('tabindex'), '-1');
        assert.equal(menu.getAttribute('tabindex'), null); assert.equal(nestedButton.getAttribute('tabindex'), null);
        a.focus(); assert.equal(key('ArrowRight').prevented, true); assert.equal(doc.activeElement, b);
        key('End'); assert.equal(doc.activeElement, c); key('ArrowRight'); assert.equal(doc.activeElement, a);
        key('Home'); assert.equal(doc.activeElement, a);
        host.direction = 'rtl'; key('ArrowLeft'); assert.equal(doc.activeElement, b);
        for (const k of ['ArrowDown', 'ArrowUp', 'Enter', ' ', 'Tab']) assert.equal(key(k).prevented, false);
        host.setAttribute('aria-orientation', 'vertical');
        assert.equal(key('ArrowLeft').prevented, false); key('ArrowDown'); assert.equal(doc.activeElement, c);
        assert.equal(key('Home', menu).prevented, false); assert.equal(key('Home', nestedButton).prevented, false);
        assert.equal(key('Home', c, { ctrlKey: true }).prevented, false);
        sync('test', host); assert.equal(c.getAttribute('tabindex'), '0');
    } finally { dispose('test'); }
    assert.equal(a.getAttribute('tabindex'), null); assert.equal(b.getAttribute('tabindex'), '4');
    assert.equal(host.listeners.size, 0);
});

test('disabled, busy, hidden, removed and reordered items repair only owned focus', () => {
    const { host, a, b, c, doc, key, update } = fixture();
    try {
        a.focus(); b.setAttribute('disabled', ''); key('ArrowRight'); assert.equal(doc.activeElement, c);
        c.setAttribute('aria-busy', 'true'); update(); assert.equal(doc.activeElement, a);
        a.setAttribute('hidden', ''); update(); assert.equal(doc.activeElement, host); assert.equal(host.getAttribute('tabindex'), '0');
        a.removeAttribute('hidden'); update(); assert.equal(doc.activeElement, a);
        b.removeAttribute('disabled'); c.removeAttribute('aria-busy'); update(); b.focus();
        host.children = [c, b, a]; update(); assert.equal(b.getAttribute('tabindex'), '0');
        key('ArrowRight'); assert.equal(doc.activeElement, a);
        host.children = [c, b]; a.parent = null; doc.activeElement = doc.body; update();
        assert.equal(doc.activeElement, c); assert.equal(a.getAttribute('tabindex'), null);
        doc.activeElement = doc.body; host.listeners.get('focusout')({ relatedTarget: doc.body });
        c.setAttribute('disabled', ''); update(); assert.equal(doc.activeElement, doc.body);
        host.setAttribute('inert', ''); host.setAttribute('aria-disabled', 'true'); update();
        assert.equal(host.getAttribute('tabindex'), '-1'); assert.equal(b.getAttribute('tabindex'), '-1');
    } finally { dispose('test'); }
});

test('a toolbar inside a dialog owns its buttons and remembers focus across visibility', () => {
    const { host, b, doc, update, Element } = fixture();
    try {
        const outer = new Element('popup', doc.body);
        host.parent = outer;
        b.focus();
        host.setAttribute('hidden', ''); update();
        assert.equal(b.getAttribute('tabindex'), '-1');
        host.removeAttribute('hidden'); update();
        assert.equal(b.getAttribute('tabindex'), '0');
        b.setAttribute('aria-disabled', 'true'); update();
        assert.equal(b.getAttribute('tabindex'), '-1');
    } finally { dispose('test'); }
});

test('host tabindex updates are restored and menu focus is never stolen', () => {
    const { host, a, b, menu, doc, update, authorTab, Element } = fixture();
    try {
        authorTab(b, '7'); sync('test', host);
        assert.equal(b.getAttribute('tabindex'), '-1');
        b.focus(); menu.focus(); b.setAttribute('disabled', ''); update();
        assert.equal(doc.activeElement, menu);
        const group = new Element('group', host);
        a.parent = group; group.children.push(a);
        host.children = host.children.filter(node => node !== a);
        group.setAttribute('inert', ''); update();
        assert.equal(a.getAttribute('tabindex'), '-1');
    } finally { dispose('test'); }
    assert.equal(b.getAttribute('tabindex'), '7');
});
