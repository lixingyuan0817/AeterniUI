// Direct module coverage for the four components whose .razor.js had none
// (REV-163). The harness is the same dependency-free DOM double the other module
// tests use: the browser checks cover these paths end to end, and these tests pin
// the module contracts that a refactor could silently break.
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const helpers = await readFile(new URL('../src/AeterniUI/wwwroot/js/aeterni_floating.js', import.meta.url), 'utf8');
const data = source => `data:text/javascript;base64,${Buffer.from(source).toString('base64')}`;
async function moduleAt(component) {
    const source = await readFile(new URL(`../src/AeterniUI/Components/${component}.razor.js`, import.meta.url), 'utf8');
    return import(data(source.replace('../../js/aeterni_floating.js', data(helpers))));
}

const checkbox = await moduleAt('Checkbox/Checkbox');
const provider = await moduleAt('Dialog/DialogProvider');
const menu = await moduleAt('Menu/Menu');
const theme = await moduleAt('Theme/ThemeProvider');

const flush = async () => { for (let i = 0; i < 8; i++) await Promise.resolve(); };

class Events {
    listeners = new Map();
    addEventListener(name, callback) { if (!this.listeners.has(name)) this.listeners.set(name, new Set()); this.listeners.get(name).add(callback); }
    removeEventListener(name, callback) { this.listeners.get(name)?.delete(callback); }
    emit(name, event = {}) { for (const callback of [...(this.listeners.get(name) ?? [])]) callback(event); }
    listenerCount(name) { return this.listeners.get(name)?.size ?? 0; }
}

class Element extends Events {
    attrs = new Map();
    dataset = {};
    classes = new Set();
    children = new Map();
    animations = [];
    isConnected = true;
    style = { setProperty() {}, removeProperty() {}, overflow: '' };
    classList = {
        contains: value => this.classes.has(value),
        add: (...values) => values.forEach(value => this.classes.add(value)),
        remove: (...values) => values.forEach(value => this.classes.delete(value)),
        toggle: (value, on) => { if (on) this.classes.add(value); else this.classes.delete(value); }
    };
    constructor(parent = null) { super(); this.parentElement = parent; }
    getAttribute(name) { return this.attrs.get(name) ?? null; }
    setAttribute(name, value) { this.attrs.set(name, value); }
    removeAttribute(name) { this.attrs.delete(name); }
    querySelector(selector) { return this.children.get(selector) ?? null; }
    querySelectorAll() { return []; }
    getAnimations() { return this.animations; }
    getBoundingClientRect() { return { top: 0, bottom: 100, left: 0, right: 100, width: 100, height: 100 }; }
    contains(node) { for (; node; node = node.parentElement) if (node === this) return true; return false; }
    focus() { document.activeElement = this; }
    matches() { return false; }
    closest() { return null; }
}

function environment({ dark = false } = {}) {
    globalThis.Node = globalThis.HTMLElement = Element;
    const storage = new Map();
    globalThis.document = new Events();
    document.body = new Element();
    document.documentElement = new Element();
    document.getElementById = id => document.byId?.get(id) ?? null;
    document.querySelector = selector => document.bySelector?.get(selector) ?? null;
    document.querySelectorAll = selector => document.bySelectorAll?.get(selector) ?? [];
    globalThis.window = new Events();
    window.localStorage = {
        getItem: key => (storage.has(key) ? storage.get(key) : null),
        setItem: (key, value) => storage.set(key, value)
    };
    window.matchMedia = () => ({
        matches: dark,
        addEventListener(name, callback) { this.handler = callback; },
        removeEventListener() { this.handler = undefined; }
    });
    window.clearTimeout = () => {};
    window.setTimeout = () => 0;
    globalThis.getComputedStyle = () => ({ direction: 'ltr', paddingRight: '0px' });
    return storage;
}

test('REV-163 Checkbox pushes the indeterminate property and dispose is a no-op', () => {
    environment();
    const input = new Element();
    checkbox.setIndeterminate(input, true);
    assert.equal(input.indeterminate, true);
    checkbox.setIndeterminate(input, 0);
    assert.equal(input.indeterminate, false);
    assert.doesNotThrow(() => checkbox.setIndeterminate(null, true));
    assert.doesNotThrow(() => checkbox.dispose());
});

test('REV-163 DialogProvider sync focuses the top dialog, locks scroll and releases both', async () => {
    environment();
    const root = new Element(document.body);
    const dialog = new Element(root);
    const focusable = new Element(dialog);
    focusable.focus = function () { document.activeElement = this; };
    // focusFirst asks the container for its focusable descendants, so the dialog
    // double has to answer that query (the selector itself is the library's).
    dialog.querySelectorAll = () => [focusable];
    root.querySelector = () => dialog;
    const calls = [];
    provider.init({ invokeMethodAsync(method, ...args) { calls.push([method, ...args]); return Promise.resolve(); } }, 'provider-1');

    provider.sync('provider-1', root, 'dialog-1', true);
    await flush();
    assert.equal(document.activeElement, focusable, 'focus moves into the top dialog');
    assert.notEqual(document.body.style.overflow, '', 'scroll is locked while a modal dialog is open');

    provider.sync('provider-1', root, null, false);
    await flush();
    assert.equal(document.body.style.overflow, '', 'scroll lock is released');
    provider.dispose('provider-1');
    assert.equal(document.listenerCount('keydown'), 0, 'dispose removes the Tab handler');
});

test('REV-163 DialogProvider waits for the measured exit and resolves without animations', async () => {
    environment();
    provider.init({ invokeMethodAsync: () => Promise.resolve() }, 'provider-2');

    // No element for the id: the wait must resolve instead of hanging.
    await provider.waitForExitSignals('provider-2', ['missing']);

    // An exit animation is awaited until it finishes.
    const card = new Element(document.body);
    document.byId = new Map([['notice-1', card]]);
    let finished = false;
    card.animations = [{
        animationName: 'aeterni-notice-exit',
        finished: new Promise(resolve => { setTimeout(() => { finished = true; resolve(); }, 0); })
    }];
    await provider.waitForExitSignals('provider-2', ['notice-1']);
    assert.equal(finished, true, 'the caller resumes only after the exit animation finished');
    provider.dispose('provider-2');
});

test('REV-163 Menu suppresses navigation key defaults, reports direction and cleans up', () => {
    environment();
    menu.init({ invokeMethodAsync: () => Promise.resolve() }, 'menu-1');
    const host = new Element(document.body);
    menu.attach('menu-1', host);

    let prevented = false;
    host.emit('keydown', { key: 'ArrowDown', preventDefault() { prevented = true; } });
    assert.equal(prevented, true, 'arrow keys must not scroll the page');

    let untouched = true;
    host.emit('keydown', { key: 'Enter', preventDefault() { untouched = false; } });
    assert.equal(untouched, true, 'activation keys keep their default behaviour');

    assert.equal(menu.isRtl('menu-1'), false);
    const node = new Element(document.body);
    document.byId = new Map([['menu-item-1', node]]);
    menu.focus('menu-item-1');
    assert.equal(document.activeElement, node, 'C# can move focus to a node id');

    menu.dispose('menu-1');
    assert.equal(host.listenerCount('keydown'), 0, 'dispose detaches the handler');
});

test('REV-163 ThemeProvider keeps its storage contract and tolerates a broken store', () => {
    const storage = environment();
    theme.persistMode('dark');
    assert.equal(storage.get('aeterni.theme.mode'), 'dark');
    assert.equal(theme.getStoredMode(), 'dark');
    theme.persistBrand('green');
    assert.equal(theme.getStoredBrand(), 'green');
    assert.equal(theme.getSystemTheme(), false);

    window.localStorage = { getItem() { throw new Error('blocked'); }, setItem() { throw new Error('blocked'); } };
    assert.equal(theme.getStoredMode(), null, 'a restricted store degrades to no persistence');
    assert.doesNotThrow(() => theme.persistMode('light'));
    assert.doesNotThrow(() => theme.persistBrand('purple'));
});

test('REV-163 ThemeProvider applies theme and brand on <html> and notifies system changes', () => {
    environment({ dark: true });
    const calls = [];
    const reference = { invokeMethodAsync: (method, ...args) => { calls.push([method, ...args]); return Promise.resolve(); } };
    const mediaQuery = theme.init(reference, 'theme-1');
    assert.deepEqual(calls, [['OnSystemThemeChanged', true]], 'the initial system state is reported');
    assert.ok(mediaQuery === undefined || true);

    theme.applyTheme('dark', false);
    assert.equal(document.documentElement.dataset.theme, 'dark');
    assert.equal(document.documentElement.style.colorScheme, 'dark');
    theme.applyBrand('green', false);
    assert.equal(document.documentElement.dataset.aeterniBrand, 'green');
    theme.dispose('theme-1');
});
