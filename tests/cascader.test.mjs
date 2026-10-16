// The Cascader keeps its state in C#; its module only owns two browser jobs —
// scrolling the active option into view (vertically inside its column, and the
// whole column horizontally into the panel) and cancelling the navigation keys
// the trigger already handles. These tests pin that contract with the same
// dependency-free DOM double the other module tests use.
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../src/AeterniUI/Components/Cascader/Cascader.razor.js', import.meta.url), 'utf8');
const cascader = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

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
    scrollLeft = 0;
    disabled = false;
    constructor(parent = null) { super(); this.parentElement = parent; }
    getAttribute(name) { return this.attrs.get(name) ?? null; }
    setAttribute(name, value) { this.attrs.set(name, value); }
    getBoundingClientRect() { return { top: 0, bottom: 100, left: 0, right: 100, width: 100, height: 100 }; }
}

function environment() {
    globalThis.document = new Events();
    globalThis.document.getElementById = id => globalThis.document.byId?.get(id) ?? null;
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

test('Cascader module binds one keydown listener per instance and releases it', () => {
    environment();
    const trigger = new Element();

    cascader.init({}, 'cascader-1');
    cascader.sync('cascader-1', trigger);
    assert.equal(trigger.listenerCount('keydown'), 1, 'sync binds the trigger');

    cascader.sync('cascader-1', trigger);
    assert.equal(trigger.listenerCount('keydown'), 1, 're-syncing the same trigger stays idempotent');

    cascader.sync('cascader-1', null);
    assert.equal(trigger.listenerCount('keydown'), 0, 'a detached trigger must not keep its listener');

    cascader.sync('cascader-1', trigger);
    cascader.dispose('cascader-1');
    assert.equal(trigger.listenerCount('keydown'), 0, 'dispose releases the listener');
});

test('Cascader module cancels only the keys the trigger owns', () => {
    environment();
    const trigger = new Element();
    trigger.setAttribute('aria-expanded', 'false');

    cascader.init({}, 'cascader-2');
    cascader.sync('cascader-2', trigger);

    for (const key of ['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'Escape']) {
        const event = keyEvent(trigger, key);
        trigger.emit('keydown', event);
        assert.equal(event.prevented, true, `${key} must be cancelled so the page does not scroll`);
    }

    const closedEnter = keyEvent(trigger, 'Enter');
    trigger.emit('keydown', closedEnter);
    assert.equal(closedEnter.prevented, false, 'Enter is the trigger\'s key only while the panel is open');

    trigger.setAttribute('aria-expanded', 'true');
    for (const key of ['Enter', ' ']) {
        const event = keyEvent(trigger, key);
        trigger.emit('keydown', event);
        assert.equal(event.prevented, true, `${key} must be cancelled while the panel is open`);
    }

    const tab = keyEvent(trigger, 'Tab');
    trigger.emit('keydown', tab);
    assert.equal(tab.prevented, false, 'unrelated keys keep their default behaviour');

    const shifted = keyEvent(trigger, 'ArrowDown', { shiftKey: true });
    trigger.emit('keydown', shifted);
    assert.equal(shifted.prevented, false, 'modified key presses are left to the browser');

    const composing = keyEvent(trigger, 'ArrowDown', { isComposing: true });
    trigger.emit('keydown', composing);
    assert.equal(composing.prevented, false, 'an IME composition keeps its keys');

    trigger.disabled = true;
    const disabled = keyEvent(trigger, 'ArrowDown');
    trigger.emit('keydown', disabled);
    assert.equal(disabled.prevented, false, 'a disabled trigger does not consume keys');
});

test('Cascader module scrolls the row inside its column and the column inside the panel', () => {
    environment();
    const panel = new Element();
    panel.getBoundingClientRect = () => ({ top: 0, bottom: 200, left: 0, right: 200, width: 200, height: 200 });

    const column = new Element(panel);
    column.getBoundingClientRect = () => ({ top: 0, bottom: 200, left: 250, right: 450, width: 200, height: 200 });

    const option = new Element(column);
    option.getBoundingClientRect = () => ({ top: 260, bottom: 300, left: 250, right: 450, width: 200, height: 40 });

    const trigger = new Element();
    trigger.setAttribute('aria-activedescendant', 'cascader-3-option');
    document.byId = new Map([['cascader-3-option', option]]);

    cascader.init({}, 'cascader-3');
    cascader.sync('cascader-3', trigger);

    assert.equal(column.scrollTop, 100, 'the row is scrolled to the bottom edge of its column');
    assert.equal(panel.scrollLeft, 250, 'the whole column is scrolled into the panel');
});

test('Cascader module leaves the panel alone while the active option is already visible', () => {
    environment();
    const panel = new Element();
    panel.getBoundingClientRect = () => ({ top: 0, bottom: 200, left: 0, right: 400, width: 400, height: 200 });

    const column = new Element(panel);
    column.getBoundingClientRect = () => ({ top: 0, bottom: 200, left: 10, right: 210, width: 200, height: 200 });

    const option = new Element(column);
    option.getBoundingClientRect = () => ({ top: 20, bottom: 60, left: 10, right: 210, width: 200, height: 40 });

    const trigger = new Element();
    trigger.setAttribute('aria-activedescendant', 'cascader-4-option');
    document.byId = new Map([['cascader-4-option', option]]);

    cascader.init({}, 'cascader-4');
    cascader.sync('cascader-4', trigger);

    assert.equal(column.scrollTop, 0, 'a visible row must not move its column');
    assert.equal(panel.scrollLeft, 0, 'a visible column must not move the panel');
});
