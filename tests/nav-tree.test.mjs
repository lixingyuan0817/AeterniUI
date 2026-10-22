import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../src/AeterniUI/Components/NavTree/NavTree.razor.js', import.meta.url), 'utf8');
const { init, attach, focus, dispose } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

class Element {
    constructor(kind, parent = null) {
        this.kind = kind;
        this.parent = parent;
        this.listeners = new Map();
        if (parent) parent.children.push(this);
        this.children = [];
    }

    closest(selector) {
        for (let node = this; node; node = node.parent) {
            if (selector === '.aeterni-nav-tree__button' && node.kind === 'node') {
                return node;
            }
        }
        return null;
    }

    addEventListener(name, listener) { this.listeners.set(name, listener); }
    removeEventListener(name) { this.listeners.delete(name); }
}

globalThis.Element = Element;
let focused = false;
globalThis.document = {
    getElementById(id) {
        return id === 'node-a' ? { focus() { focused = true; } } : null;
    }
};

test('NavTree keyboard module scrolls only tree buttons and owns focus', () => {
const host = new Element('host');
const nodeA = new Element('node', host);
const trailing = new Element('trailing', host);

init(null, 'nav-tree-test');
attach('nav-tree-test', host);
assert.equal(host.listeners.size, 1);

const keydown = host.listeners.get('keydown');
const arrowOnNode = { key: 'ArrowDown', target: nodeA, prevented: false, preventDefault() { this.prevented = true; } };
keydown(arrowOnNode);
assert.equal(arrowOnNode.prevented, true, 'tree node arrow keys suppress page scroll');

const arrowOnTrailing = { key: 'ArrowDown', target: trailing, prevented: false, preventDefault() { this.prevented = true; } };
keydown(arrowOnTrailing);
assert.equal(arrowOnTrailing.prevented, false, 'trailing action keys stay native');

const enterOnNode = { key: 'Enter', target: nodeA, prevented: false, preventDefault() { this.prevented = true; } };
keydown(enterOnNode);
assert.equal(enterOnNode.prevented, false, 'activation keys stay native');

focus('node-a');
assert.equal(focused, true);

dispose('nav-tree-test');
assert.equal(host.listeners.size, 0);
});
