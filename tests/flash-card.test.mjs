// FlashCard pointer following regression: the module owns two custom properties
// and one state class, and must release every listener it added. The component
// keeps the visual result (rotation, scale, flip) in its isolated stylesheet, so
// these tests only cover what the browser module is responsible for.

import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../src/AeterniUI/Components/FlashCard/FlashCard.razor.js', import.meta.url), 'utf8');
const card = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

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
    constructor(rect) {
        super();
        this.rect = rect;
        this.classes = new Set();
        this.properties = new Map();
        this.classList = {
            contains: value => this.classes.has(value),
            add: (...values) => values.forEach(value => this.classes.add(value)),
            remove: (...values) => values.forEach(value => this.classes.delete(value))
        };
        this.style = { setProperty: (name, value) => this.properties.set(name, value) };
    }
    getBoundingClientRect() { return this.rect; }
}

function environment({ reducedMotion = false } = {}) {
    globalThis.window = new Events();
    globalThis.window.matchMedia = () => ({ matches: reducedMotion });
}

const rect = { left: 100, top: 100, width: 200, height: 100 };

test('flash-card: the pointer drives the rotation and the highlight position', () => {
    environment();
    const root = new Element(rect);
    card.attach('card', root, true, 10, true);
    try {
        root.emit('pointerenter', {});
        root.emit('pointermove', { clientX: 300, clientY: 200 });

        assert.equal(root.properties.get('--aeterni-flash-card-rotate-y'), '-10.00deg');
        assert.equal(root.properties.get('--aeterni-flash-card-rotate-x'), '10.00deg');
        assert.equal(root.properties.get('--aeterni-flash-card-sheen-x'), '100.0%');
        assert.equal(root.properties.get('--aeterni-flash-card-sheen-y'), '100.0%');
        assert.equal(root.classes.has('is-tilting'), true);

        // Coordinates outside the card are clamped to the configured maximum.
        root.emit('pointermove', { clientX: 10000, clientY: -10000 });
        assert.equal(root.properties.get('--aeterni-flash-card-rotate-y'), '-10.00deg');
        assert.equal(root.properties.get('--aeterni-flash-card-rotate-x'), '-10.00deg');

        root.emit('pointerleave', {});
        assert.equal(root.properties.get('--aeterni-flash-card-rotate-x'), '0deg');
        assert.equal(root.properties.get('--aeterni-flash-card-rotate-y'), '0deg');
        assert.equal(root.classes.has('is-tilting'), false);
    } finally {
        card.dispose('card');
    }

    assert.equal(root.listenerCount('pointermove'), 0, 'dispose must release the pointer listeners');
    assert.equal(root.listenerCount('pointerleave'), 0);
    assert.equal(root.listenerCount('pointerenter'), 0);
});

test('flash-card: the sheen stays centred when the finish is off', () => {
    environment();
    const root = new Element(rect);
    card.attach('plain', root, true, 10, false);
    try {
        root.emit('pointermove', { clientX: 150, clientY: 120 });
        assert.equal(root.classes.has('is-tilting'), true);
        assert.equal(root.properties.has('--aeterni-flash-card-sheen-x'), false);
    } finally {
        card.dispose('plain');
    }
});

test('flash-card: flat, disabled and reduced-motion cards never register listeners', () => {
    environment();
    const root = new Element(rect);

    card.attach('disabled', root, false, 10, false);
    card.attach('flat', root, true, 0, false);

    environment({ reducedMotion: true });
    card.attach('reduced', root, true, 10, false);

    try {
        assert.equal(root.listenerCount('pointermove'), 0);
        assert.equal(root.listenerCount('pointerenter'), 0);
        assert.equal(root.classes.has('is-tilting'), false);
    } finally {
        card.dispose('disabled');
        card.dispose('flat');
        card.dispose('reduced');
    }
});

test('flash-card: re-attaching an instance replaces its listeners instead of stacking them', () => {
    environment();
    const root = new Element(rect);
    card.attach('card', root, true, 10, false);
    card.attach('card', root, true, 10, false);
    try {
        assert.equal(root.listenerCount('pointermove'), 1);
    } finally {
        card.dispose('card');
    }

    assert.equal(root.listenerCount('pointermove'), 0);
});
