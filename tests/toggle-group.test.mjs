import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';
const source = await readFile(new URL('../src/AeterniUI/Components/ToggleGroup/ToggleGroup.razor.js', import.meta.url), 'utf8');
const { init, sync, dispose } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

test('size styles follow Button tokens without changing joined geometry', async () => {
    const css = await readFile(new URL('../src/AeterniUI/Components/ToggleGroup/ToggleGroup.razor.css', import.meta.url), 'utf8');
    const rule = selector => {
        const start = css.indexOf(`${selector} {`);
        assert.notEqual(start, -1, `Missing ${selector}`);
        return css.slice(start, css.indexOf('}', start));
    };
    const item = rule('.aeterni-toggle-group__item');
    for (const declaration of [
        'display: inline-flex;', 'align-items: center;', 'justify-content: center;',
        'min-block-size: var(--aeterni-control-height-md);',
        'padding: 0 var(--aeterni-control-padding-x-md);',
        'font-size: var(--aeterni-font-size-sm);',
        'font-weight: var(--aeterni-font-weight-semibold);',
        'line-height: var(--aeterni-leading-none);'
    ]) assert.ok(item.includes(declaration), declaration);
    for (const [size, font] of [['sm', 'xs'], ['lg', 'base']]) {
        const sized = rule(`.aeterni-toggle-group--${size} .aeterni-toggle-group__item`);
        assert.ok(sized.includes(`min-block-size: var(--aeterni-control-height-${size});`));
        assert.ok(sized.includes(`padding-inline: var(--aeterni-control-padding-x-${size});`));
        assert.ok(sized.includes(`font-size: var(--aeterni-font-size-${font});`));
    }
    assert.ok(rule('.aeterni-toggle-group').includes('gap: 0;'));
    assert.ok(rule('.aeterni-toggle-group').includes('flex-wrap: nowrap;'));
    assert.ok(css.includes('margin-inline-start: calc(var(--aeterni-border-width) * -1);'));
    assert.ok(css.includes('margin-block-start: calc(var(--aeterni-border-width) * -1);'));
});

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
                if (selector === '.aeterni-toggle-group' && n.kind === 'toggle-group') return n;
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
    const host = new Element('toggle-group', doc.body);
    host.setAttribute('tabindex', '-1');
    const a = new Element('button', host), b = new Element('button', host), c = new Element('button', host);
    b.setAttribute('tabindex', '4');
    const popup = new Element('popup', host), menu = new Element('button', popup);
    const nested = new Element('toggle-group', host), nestedButton = new Element('button', nested);
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
        host.setAttribute('data-orientation', 'vertical');
        assert.equal(key('ArrowLeft').prevented, false); key('ArrowDown'); assert.equal(doc.activeElement, c);
        assert.equal(key('Home', menu).prevented, false); assert.equal(key('Home', nestedButton).prevented, false);
        assert.equal(key('Home', c, { ctrlKey: true }).prevented, false);
        sync('test', host); assert.equal(c.getAttribute('tabindex'), '0');
    } finally { dispose('test'); }
    assert.equal(a.getAttribute('tabindex'), null); assert.equal(b.getAttribute('tabindex'), '4');
    assert.equal(host.listeners.size, 0);
});

test('pressed state updates never activate an item or move focus', () => {
    const { host, a, b, c, doc, key } = fixture();
    try {
        a.setAttribute('aria-pressed', 'true');
        b.setAttribute('aria-pressed', 'false');
        a.focus(); key('ArrowRight');
        assert.equal(doc.activeElement, b);
        assert.equal(a.getAttribute('aria-pressed'), 'true');
        assert.equal(b.getAttribute('aria-pressed'), 'false');
        a.setAttribute('aria-pressed', 'false'); c.setAttribute('aria-pressed', 'true');
        sync('test', host);
        assert.equal(doc.activeElement, b);
        for (const k of ['Enter', ' ', 'Tab']) assert.equal(key(k).prevented, false);
    } finally { dispose('test'); }
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

test('a toggle-group inside a dialog owns its buttons and remembers focus across visibility', () => {
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
