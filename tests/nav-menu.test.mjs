// NavMenu 的键盘模块只做三件事：移动焦点、抑制行上的方向键滚动、报告书写方向。
// 行按键由 C# 的导航模型消费，trailing 动作区里的按钮必须保留自己的方向键行为。
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../src/AeterniUI/Components/NavMenu/NavMenu.razor.js', import.meta.url), 'utf8');
const { init, attach, focus, isRtl, dispose } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

class Element {
    constructor(kind) {
        this.kind = kind;
        this.listeners = new Map();
    }

    addEventListener(name, listener) { this.listeners.set(name, listener); }
    removeEventListener(name, listener) { this.listeners.delete(name); }
}

class Target {
    constructor(row) {
        this.row = row;
    }

    closest(selector) {
        return this.row && selector === '.aeterni-nav-menu__button' ? {} : null;
    }
}

let focused = false;
globalThis.document = {
    getElementById(id) {
        return id === 'node-a' ? { focus() { focused = true; } } : null;
    }
};
globalThis.getComputedStyle = () => ({ direction: 'rtl' });

test('keyboard module only suppresses arrow keys on rows and owns focus', () => {
    const host = new Element('host');
    init(null, 'nav-menu-test');
    attach('nav-menu-test', host);
    assert.equal(host.listeners.size, 1);

    const keydown = host.listeners.get('keydown');
    const arrowOnRow = { key: 'ArrowDown', target: new Target(true), prevented: false, preventDefault() { this.prevented = true; } };
    keydown(arrowOnRow);
    assert.equal(arrowOnRow.prevented, true, 'row arrow keys suppress page scroll');

    const arrowOnTrailing = { key: 'ArrowDown', target: new Target(false), prevented: false, preventDefault() { this.prevented = true; } };
    keydown(arrowOnTrailing);
    assert.equal(arrowOnTrailing.prevented, false, 'trailing actions keep their own arrow keys');

    const enterOnRow = { key: 'Enter', target: new Target(true), prevented: false, preventDefault() { this.prevented = true; } };
    keydown(enterOnRow);
    assert.equal(enterOnRow.prevented, false, 'activation keys stay native');

    focus('node-a');
    assert.equal(focused, true);
    assert.equal(isRtl('nav-menu-test'), true, 'the writing direction is reported for RTL expand keys');

    dispose('nav-menu-test');
    assert.equal(host.listeners.size, 0);
});
