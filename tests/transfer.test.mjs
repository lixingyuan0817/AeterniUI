// The Transfer keeps its state in C#; its module owns two browser jobs only — forwarding
// the navigation/selection keys handled by each listbox to `HandleKeyFromBrowserAsync`,
// and scrolling the active option into its own column viewport. These tests pin that
// contract with the same dependency-free DOM double the other module tests use.
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../src/AeterniUI/Components/Transfer/Transfer.razor.js', import.meta.url), 'utf8');
const transfer = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

class Events {
    listeners = new Map();
    addEventListener(name, callback) {
        if (!this.listeners.has(name)) this.listeners.set(name, new Set());
        this.listeners.get(name).add(callback);
    }
    removeEventListener(name, callback) { this.listeners.get(name)?.delete(callback); }
    emit(name, event = {}) { for (const callback of [...(this.listeners.get(name) ?? [])]) callback(event); }
    listenerCount(name) { return this.listeners.get(name)?.size ?? 0; }
}

class Element extends Events {
    attrs = new Map();
    scrollTop = 0;
    constructor(parent = null) { super(); this.parentElement = parent; }
    getAttribute(name) { return this.attrs.get(name) ?? null; }
    setAttribute(name, value) { this.attrs.set(name, value); }
    closest() { return null; }
    getClientRects() { return [{ top: 0, bottom: 10, left: 0, right: 10 }]; }
    getBoundingClientRect() { return { top: 0, bottom: 100, left: 0, right: 100, width: 100, height: 100 }; }
}

/// A component root: the module looks the two listboxes up with
/// querySelectorAll('[role="listbox"]'), so the double answers that one selector.
class Root extends Element {
    listboxes = [];
    querySelectorAll(selector) { return selector === '[role="listbox"]' ? this.listboxes : []; }
}

function environment() {
    globalThis.document = new Events();
    globalThis.document.getElementById = id => globalThis.document.byId?.get(id) ?? null;
    globalThis.getComputedStyle = () => ({ visibility: 'visible' });
}

function listbox(side) {
    const element = new Element();
    element.setAttribute('role', 'listbox');
    element.setAttribute('data-side', side);
    return element;
}

const keyEvent = (target, key, overrides = {}) => ({
    key,
    target,
    altKey: false,
    ctrlKey: false,
    metaKey: false,
    shiftKey: false,
    isComposing: false,
    prevented: false,
    propagationStopped: false,
    preventDefault() { this.prevented = true; },
    stopPropagation() { this.propagationStopped = true; },
    ...overrides
});

const flush = async () => { for (let index = 0; index < 64; index++) await Promise.resolve(); };

test('Transfer module binds one keydown listener per listbox and releases them', () => {
    environment();
    const root = new Root();
    const sourceList = listbox('source');
    const targetList = listbox('target');
    root.listboxes = [sourceList, targetList];

    transfer.init({}, 'transfer-1');
    transfer.sync('transfer-1', root);
    assert.equal(sourceList.listenerCount('keydown'), 1, 'sync binds the source listbox');
    assert.equal(targetList.listenerCount('keydown'), 1, 'sync binds the target listbox');

    transfer.sync('transfer-1', root);
    assert.equal(sourceList.listenerCount('keydown'), 1, 're-syncing the same root stays idempotent');
    assert.equal(targetList.listenerCount('keydown'), 1, 'the target listbox is not rebound either');

    const replacement = new Root();
    const replacementSource = listbox('source');
    const replacementTarget = listbox('target');
    replacement.listboxes = [replacementSource, replacementTarget];
    transfer.sync('transfer-1', replacement);
    assert.equal(sourceList.listenerCount('keydown'), 0, 'a replaced root must not keep its listeners');
    assert.equal(targetList.listenerCount('keydown'), 0, 'the replaced target must not keep its listener');
    assert.equal(replacementSource.listenerCount('keydown'), 1, 'the new source listbox is bound');

    transfer.sync('transfer-1', null);
    assert.equal(replacementSource.listenerCount('keydown'), 0, 'detaching the root releases every listener');

    transfer.sync('transfer-1', replacement);
    transfer.dispose('transfer-1');
    assert.equal(replacementSource.listenerCount('keydown'), 0, 'dispose releases the listeners');
    assert.equal(replacementTarget.listenerCount('keydown'), 0, 'dispose releases the target listener too');
});

test('Transfer module cancels only the keys each listbox owns and forwards side + key', async () => {
    environment();
    const root = new Root();
    const sourceList = listbox('source');
    const targetList = listbox('target');
    root.listboxes = [sourceList, targetList];

    const calls = [];
    transfer.init({ invokeMethodAsync: (...args) => { calls.push(args); return Promise.resolve(); } }, 'transfer-2');
    transfer.sync('transfer-2', root);

    for (const key of ['ArrowDown', 'ArrowUp', 'Home', 'End', ' ', 'Enter']) {
        const event = keyEvent(sourceList, key);
        sourceList.emit('keydown', event);
        assert.equal(event.prevented, true, `${key} must be cancelled so the page does not scroll`);
        assert.equal(event.propagationStopped, true, `${key} must not reach ancestor shortcuts`);
    }
    await flush();
    assert.deepEqual(calls, [
        ['HandleKeyFromBrowserAsync', 'source', 'ArrowDown'],
        ['HandleKeyFromBrowserAsync', 'source', 'ArrowUp'],
        ['HandleKeyFromBrowserAsync', 'source', 'Home'],
        ['HandleKeyFromBrowserAsync', 'source', 'End'],
        ['HandleKeyFromBrowserAsync', 'source', ' '],
        ['HandleKeyFromBrowserAsync', 'source', 'Enter']
    ], 'each handled key is forwarded with the listbox side');

    const targetEvent = keyEvent(targetList, 'ArrowDown');
    targetList.emit('keydown', targetEvent);
    await flush();
    assert.equal(calls.at(-1)[1], 'target', 'the target listbox forwards its own side');

    for (const key of ['Tab', 'a', 'PageDown']) {
        const event = keyEvent(sourceList, key);
        sourceList.emit('keydown', event);
        assert.equal(event.prevented, false, `${key} keeps its default behaviour`);
    }

    const shifted = keyEvent(sourceList, 'ArrowDown', { shiftKey: true });
    sourceList.emit('keydown', shifted);
    assert.equal(shifted.prevented, false, 'modified key presses are left to the browser');

    const composing = keyEvent(sourceList, 'ArrowDown', { isComposing: true });
    sourceList.emit('keydown', composing);
    assert.equal(composing.prevented, false, 'an IME composition keeps its keys');

    sourceList.setAttribute('aria-disabled', 'true');
    const disabled = keyEvent(sourceList, 'ArrowDown');
    sourceList.emit('keydown', disabled);
    assert.equal(disabled.prevented, false, 'a disabled listbox does not consume keys');

    const foreign = keyEvent(new Element(), 'ArrowDown');
    sourceList.emit('keydown', foreign);
    assert.equal(foreign.prevented, false, 'events from another target are ignored');
});

test('Transfer module scrolls the active row inside its own column only', () => {
    environment();
    const root = new Root();
    const sourceList = listbox('source');
    const targetList = listbox('target');
    root.listboxes = [sourceList, targetList];
    sourceList.getBoundingClientRect = () => ({ top: 0, bottom: 200, left: 0, right: 200, width: 200, height: 200 });
    targetList.getBoundingClientRect = () => ({ top: 0, bottom: 200, left: 0, right: 200, width: 200, height: 200 });

    const sourceRow = new Element(sourceList);
    sourceRow.getBoundingClientRect = () => ({ top: 260, bottom: 300, left: 0, right: 100, width: 100, height: 40 });
    sourceList.setAttribute('aria-activedescendant', 'transfer-3-source-option-1');

    const targetRow = new Element(targetList);
    targetRow.getBoundingClientRect = () => ({ top: -40, bottom: 20, left: 0, right: 100, width: 100, height: 40 });
    targetList.setAttribute('aria-activedescendant', 'transfer-3-target-option-2');

    document.byId = new Map([
        ['transfer-3-source-option-1', sourceRow],
        ['transfer-3-target-option-2', targetRow]
    ]);
    targetList.scrollTop = 60;

    transfer.init({}, 'transfer-3');
    transfer.sync('transfer-3', root);

    assert.equal(sourceList.scrollTop, 100, 'a row below the column edge scrolls the column by the overflow');
    assert.equal(targetList.scrollTop, 20, 'a row above the column edge scrolls back by the overflow');
});

test('Transfer module leaves a column alone while its active row is already visible', () => {
    environment();
    const root = new Root();
    const list = listbox('target');
    root.listboxes = [list];

    const row = new Element(list);
    row.getBoundingClientRect = () => ({ top: 20, bottom: 60, left: 0, right: 100, width: 100, height: 40 });
    list.setAttribute('aria-activedescendant', 'transfer-4-target-option-0');
    document.byId = new Map([['transfer-4-target-option-0', row]]);

    transfer.init({}, 'transfer-4');
    transfer.sync('transfer-4', root);

    assert.equal(list.scrollTop, 0, 'a visible row must not move its column');
});

test('Transfer module tolerates an instance that was disposed before it synced', async () => {
    environment();
    const root = new Root();
    const list = listbox('source');
    root.listboxes = [list];

    transfer.init({ invokeMethodAsync: () => Promise.resolve() }, 'transfer-5');
    transfer.dispose('transfer-5');

    assert.doesNotThrow(() => transfer.sync('transfer-5', root));
    assert.equal(list.listenerCount('keydown'), 0, 'a disposed instance must not bind listeners');

    transfer.init({ invokeMethodAsync: () => Promise.resolve() }, 'transfer-6');
    transfer.sync('transfer-6', root);
    const event = keyEvent(list, 'ArrowDown');
    list.emit('keydown', event);
    transfer.dispose('transfer-6');
    await flush();
    assert.doesNotThrow(() => transfer.dispose('transfer-6'), 'dispose stays idempotent');
});
