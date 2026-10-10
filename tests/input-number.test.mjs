// InputNumber key-guard regression: the module only suppresses the browser's
// default action for the keys the C# handler steps on. It owns no state and no
// timers, so the assertions are about which keys are prevented on which root,
// and that every listener is released on dispose.

import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../src/AeterniUI/Components/InputNumber/InputNumber.razor.js', import.meta.url), 'utf8');
const inputNumber = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

class Events {
    listeners = new Map();
    addEventListener(name, callback) {
        if (!this.listeners.has(name)) this.listeners.set(name, new Set());
        this.listeners.get(name).add(callback);
    }
    removeEventListener(name, callback) { this.listeners.get(name)?.delete(callback); }
    emit(name, event) { for (const callback of [...(this.listeners.get(name) ?? [])]) callback(event); }
    listenerCount(name) { return this.listeners.get(name)?.size ?? 0; }
}

class Element extends Events {
    constructor(classes = []) {
        super();
        this.classes = new Set(classes);
        this.classList = { contains: value => this.classes.has(value) };
    }
}

function keyEvent(key, overrides = {}) {
    const event = { key, altKey: false, ctrlKey: false, metaKey: false, shiftKey: false, isComposing: false, prevented: 0, ...overrides };
    event.preventDefault = () => { event.prevented += 1; };
    return event;
}

test('input-number: stepped keys are prevented when the field is editable', () => {
    const root = new Element();
    inputNumber.init({}, 'field');
    inputNumber.sync('field', root);
    try {
        for (const key of ['ArrowUp', 'ArrowDown', 'PageUp', 'PageDown']) {
            const event = keyEvent(key);
            root.emit('keydown', event);
            assert.equal(event.prevented, 1, `${key} must not act on the page`);
        }

        // Keys the component does not step stay native: Left/Right move the
        // caret, Home/End remain caret navigation inside the field.
        for (const key of ['ArrowLeft', 'ArrowRight', 'Home', 'End', 'a']) {
            const event = keyEvent(key);
            root.emit('keydown', event);
            assert.equal(event.prevented, 0, `${key} must keep its native behaviour`);
        }
    } finally {
        inputNumber.dispose('field');
    }

    assert.equal(root.listenerCount('keydown'), 0, 'dispose must release the key guard');
});

test('input-number: modifier chords, IME composition and locked fields are left alone', () => {
    const root = new Element();
    inputNumber.init({}, 'field');
    inputNumber.sync('field', root);
    try {
        const chord = keyEvent('ArrowUp', { ctrlKey: true });
        root.emit('keydown', chord);
        assert.equal(chord.prevented, 0, 'a modified chord is a host shortcut, not a step');

        const composing = keyEvent('ArrowDown', { isComposing: true });
        root.emit('keydown', composing);
        assert.equal(composing.prevented, 0, 'IME composition must keep the key');

        root.classes.add('is-readonly');
        const readonly = keyEvent('PageUp');
        root.emit('keydown', readonly);
        assert.equal(readonly.prevented, 0);
        root.classes.delete('is-readonly');

        root.classes.add('is-disabled');
        const disabled = keyEvent('PageUp');
        root.emit('keydown', disabled);
        assert.equal(disabled.prevented, 0);
    } finally {
        inputNumber.dispose('field');
    }
});

test('input-number: every sync with a new root moves the guard instead of stacking it', () => {
    const first = new Element();
    const second = new Element();
    inputNumber.init({}, 'field');
    inputNumber.sync('field', first);
    inputNumber.sync('field', first);
    assert.equal(first.listenerCount('keydown'), 1, 'a repeated sync must not stack listeners');

    inputNumber.sync('field', second);
    assert.equal(first.listenerCount('keydown'), 0);

    try {
        const event = keyEvent('ArrowUp');
        second.emit('keydown', event);
        assert.equal(event.prevented, 1);
    } finally {
        inputNumber.dispose('field');
    }

    assert.equal(second.listenerCount('keydown'), 0);
});

test('input-number: re-init replaces the previous instance without leaking its listener', () => {
    const first = new Element();
    const second = new Element();
    inputNumber.init({}, 'field');
    inputNumber.sync('field', first);

    inputNumber.init({}, 'field');
    inputNumber.sync('field', second);
    try {
        assert.equal(first.listenerCount('keydown'), 0);
        assert.equal(second.listenerCount('keydown'), 1);
    } finally {
        inputNumber.dispose('field');
    }
});
