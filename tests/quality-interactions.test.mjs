import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const helpers = await readFile(new URL('../src/AeterniUI/wwwroot/js/aeterni_floating.js', import.meta.url), 'utf8');
const data = source => `data:text/javascript;base64,${Buffer.from(source).toString('base64')}`;
async function moduleAt(component) {
    const source = await readFile(new URL(`../src/AeterniUI/Components/${component}.razor.js`, import.meta.url), 'utf8');
    return import(data(source.replace('../../js/aeterni_floating.js', data(helpers))));
}
const drawer = await moduleAt('Drawer/Drawer'), popover = await moduleAt('Popup/Popover');
const combo = await moduleAt('ComboBox/ComboBox'), rating = await moduleAt('Rating/Rating');
const tooltip = await moduleAt('Tooltip/Tooltip');
const tabs = await moduleAt('Tabs/Tabs');
const flush = async () => { for (let i = 0; i < 8; i++) await Promise.resolve(); };

class Events {
    listeners = new Map();
    addEventListener(name, callback) { if (!this.listeners.has(name)) this.listeners.set(name, new Set()); this.listeners.get(name).add(callback); }
    removeEventListener(name, callback) { this.listeners.get(name)?.delete(callback); }
    emit(name, event) { for (const callback of [...(this.listeners.get(name) ?? [])]) callback(event); }
}
class Element extends Events {
    attrs = new Map(); classes = new Set(); children = new Map(); animations = []; isConnected = true;
    constructor(parent = null) { super(); this.parentElement = parent; }
    classList = { contains: value => this.classes.has(value), add: (...values) => values.forEach(v => this.classes.add(v)), remove: (...values) => values.forEach(v => this.classes.delete(v)) };
    style = { overflow: '', paddingRight: '', setProperty() {} };
    getAttribute(name) { return this.attrs.get(name) ?? null; }
    setAttribute(name, value) { this.attrs.set(name, value); }
    removeAttribute(name) { this.attrs.delete(name); }
    querySelector(selector) { return this.children.get(selector) ?? null; }
    querySelectorAll() { return []; }
    getAnimations() { return this.animations; }
    getBoundingClientRect() { return { top: 100, bottom: 180, left: 100, right: 300, width: 200, height: 80 }; }
    contains(node) { for (; node; node = node.parentElement) if (node === this) return true; return false; }
    focus() { document.activeElement = this; }
    matches() { return !!this.disabled; }
    closest() { return null; }
}
function environment() {
    globalThis.Node = globalThis.HTMLElement = Element;
    globalThis.document = new Events(); document.body = new Element(); document.documentElement = { clientWidth: 1000 };
    document.getElementById = () => null;
    globalThis.window = new Events(); window.innerWidth = 1000; window.innerHeight = 800;
    globalThis.getComputedStyle = () => ({ paddingRight: '0px' });
}
function fixture(module, name) {
    environment();
    const root = new Element(document.body), panel = new Element(root), backdrop = new Element(root), opener = new Element(document.body);
    root.children.set('.aeterni-popover__surface', panel); root.children.set('.aeterni-popover__backdrop', backdrop);
    root.children.set('.aeterni-drawer__backdrop', backdrop);
    const calls = [];
    module.init({ invokeMethodAsync(method, ...args) { calls.push([method, ...args]); return Promise.resolve(); } }, name);
    const sync = (open, escape = true, closing = false, revision = 0) => name === 'drawer'
        ? module.setOpen(name, open, panel, true, escape, true, closing, revision)
        : module.setOpen(name, open, root, true, 'bottom-start', escape, true, false, false, closing, revision);
    opener.focus(); sync(true);
    const pressEscape = () => document.emit('keydown', { key: 'Escape', stopPropagation() {} });
    const animate = () => {
        let finish;
        panel.animations = [{ animationName: `aeterni-${name}-exit`, finished: new Promise(resolve => finish = resolve) }];
        return finish;
    };
    return { calls, sync, pressEscape, animate, opener, panel, root };
}

for (const [name, module] of [['drawer', drawer], ['popover', popover]]) {
    test(`${name}: modal Escape honors live policy; exits retain lock and cancel on reopen/dispose`, async () => {
        const f = fixture(module, name);
        try {
            f.sync(true, false); f.pressEscape(); assert.equal(f.calls.length, 0);
            f.sync(true, true); f.pressEscape(); assert.equal(f.calls[0][0], 'RequestCloseAsync');
            const finish = f.animate(); f.sync(false, true, true, 1);
            assert.equal(document.body.style.overflow, 'hidden', 'closing retains modal lock');
            f.sync(false, true, true, 1); f.pressEscape();
            assert.equal(f.calls.length, 1, 'no repeated close while exiting');
            f.sync(true); finish(); await flush();
            assert.equal(f.calls.length, 1, 'reopen cancels completion');
            const finishAgain = f.animate(); f.sync(false, true, true, 2); finishAgain(); await flush();
            assert.deepEqual(f.calls.at(-1), ['FinalizeCloseAsync', 2]);
            assert.equal(document.body.style.overflow, ''); assert.equal(document.activeElement, f.opener);
            f.sync(true); const finishDisposed = f.animate(); f.sync(false, true, true, 3);
            module.dispose(name); const count = f.calls.length; finishDisposed(); await flush();
            assert.equal(f.calls.length, count); assert.equal(document.body.style.overflow, '');
        } finally { module.dispose(name); }
    });
    test(`${name}: no CSS animation (reduced motion) finalizes without a fixed delay`, async () => {
        const f = fixture(module, name);
        try {
            f.sync(false, true, true, 4); await flush();
            assert.deepEqual(f.calls, [['FinalizeCloseAsync', 4]]);
            assert.equal(document.body.style.overflow, '');
        } finally { module.dispose(name); }
    });
}

test('ComboBox and Rating prevent only owned keys and release listeners', () => {
    environment();
    const trigger = new Element(), root = new Element(), star = new Element(root);
    star.setAttribute('role', 'radio');
    combo.init(null, 'combo'); rating.init(null, 'rating');
    combo.sync('combo', trigger); rating.sync('rating', root);
    const press = (element, key, target = element, extra = {}) => {
        const event = { key, target, prevented: false, preventDefault() { this.prevented = true; }, stopPropagation() {}, ...extra };
        element.emit('keydown', event); return event.prevented;
    };
    try {
        assert.equal(press(trigger, 'ArrowDown'), true);
        assert.equal(press(trigger, 'Tab'), false);
        assert.equal(press(trigger, 'Enter'), false, 'closed button uses native activation');
        trigger.setAttribute('aria-expanded', 'true');
        assert.equal(press(trigger, 'Enter'), true, 'open Enter must not synthesize another toggle click');
        assert.equal(press(trigger, ' '), true);
        assert.equal(press(trigger, 'Home', trigger, { ctrlKey: true }), false);
        assert.equal(press(root, 'ArrowRight', star), true);
        assert.equal(press(root, ' ', star), false, 'radio Space stays native');
        root.classList.add('is-readonly'); assert.equal(press(root, 'ArrowRight', star), false);
    } finally { combo.dispose('combo'); rating.dispose('rating'); }
    assert.equal(press(trigger, 'ArrowDown'), false); assert.equal(press(root, 'Home', star), false);
});

test('Tooltip refreshes placement/target and removes only its own description token', () => {
    environment();
    const root = new Element(), content = new Element(root), trigger = new Element(root), target = new Element(trigger);
    root.children.set('.aeterni-tooltip__content', content); root.children.set('.aeterni-tooltip__trigger', trigger);
    trigger.querySelector = () => target; target.setAttribute('aria-describedby', 'external');
    tooltip.attach('tip', root, 'first', 'top');
    target.setAttribute('aria-describedby', 'external first later');
    tooltip.attach('tip', root, 'second', 'bottom');
    assert.equal(root.classList.contains('aeterni-tooltip--bottom'), true);
    assert.equal(target.getAttribute('aria-describedby'), 'external later second');
    root.children.delete('.aeterni-tooltip__content'); tooltip.attach('tip', root, null, 'bottom');
    assert.equal(target.getAttribute('aria-describedby'), 'external later');
    root.children.set('.aeterni-tooltip__content', content); tooltip.attach('tip', root, 'third', 'end');
    assert.equal(target.getAttribute('aria-describedby'), 'external later third');
    tooltip.dispose('tip'); assert.equal(target.getAttribute('aria-describedby'), 'external later');
});

test('Tabs initializes, focuses visible-index buttons and releases navigation listener', () => {
    environment();
    const root = new Element(), first = new Element(root), second = new Element(root);
    first.setAttribute('role', 'tab'); second.setAttribute('role', 'tab');
    root.querySelectorAll = () => [first, second];
    tabs.init(null, 'tabs'); tabs.attach('tabs', root);
    tabs.focusTab('tabs', 1); assert.equal(document.activeElement, second);
    let prevented = false;
    root.emit('keydown', {target:second,key:'Home',preventDefault(){prevented=true;}});
    assert.equal(prevented,true); tabs.dispose('tabs');
    assert.equal(root.listeners.get('keydown').size,0);
});
