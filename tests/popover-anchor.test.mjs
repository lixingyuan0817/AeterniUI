import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

// 浮层模块的锚定模式：渲染位置与定位锚点分开。用 Node 侧的 DOM 替身跑 placement 数学与"层内"
// 判定，比每次都在浏览器里跑一遍快得多，也不依赖真实排版。
const helpers = await readFile(new URL('../src/AeterniUI/wwwroot/js/aeterni_floating.js', import.meta.url), 'utf8');
const source = (await readFile(new URL('../src/AeterniUI/Components/Popup/Popover.razor.js', import.meta.url), 'utf8'))
    .replace('../../js/aeterni_floating.js', `data:text/javascript;base64,${Buffer.from(helpers).toString('base64')}`);
const { init, setAnchor, setOpen, dispose } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

const ANCHOR_RECT = { left: 400, top: 100, right: 600, bottom: 140, width: 200, height: 40 };
const LAYER_RECT = { left: 0, top: 0, right: 200, bottom: 100, width: 200, height: 100 };

function fixture() {
    const listeners = new Map();
    globalThis.window = { innerWidth: 1000, innerHeight: 800, addEventListener() {}, removeEventListener() {} };
    globalThis.document = { addEventListener: (name, fn) => listeners.set(name, fn), removeEventListener: name => listeners.delete(name) };
    // 间距来自组件自己的 CSS 自定义属性，替身只提供读取这一步需要的两个读数。
    globalThis.getComputedStyle = () => ({ getPropertyValue: () => '8px', fontSize: '16px' });

    class Element {
        constructor(parent = null, rect = LAYER_RECT) {
            this.parentElement = parent;
            this.isConnected = true;
            this.rect = rect;
            this.classes = new Set();
            this.classList = {
                add: name => this.classes.add(name),
                remove: name => this.classes.delete(name)
            };
            this.style = { setProperty: (name, value) => { this.style[name] = value; } };
        }

        contains(node) { for (let n = node; n; n = n.parentElement) if (n === this) return true; return false; }
        getBoundingClientRect() { return this.rect; }
    }

    globalThis.Node = globalThis.HTMLElement = Element;
    const body = document.body = new Element();
    const host = new Element(body);
    const layer = new Element(host);
    // 锚点与浮层分处两棵子树：触发器在页头里，浮层渲染在页面级宿主里。
    const triggerWrapper = new Element(new Element(body), ANCHOR_RECT);
    const outside = new Element(body);

    let closes = 0;
    init({ invokeMethodAsync() { closes++; return Promise.resolve(); } }, 'anchor');
    return {
        host, layer, triggerWrapper, outside, listeners,
        element: (parent, rect) => new Element(parent, rect),
        sync: (open, anchor) => {
            setAnchor('anchor', anchor);
            setOpen('anchor', open, layer, false, 'bottom-end', true, true, false, false);
        },
        get closes() { return closes; }
    };
}

test('an anchored layer is placed against the anchor, in viewport coordinates', () => {
    const f = fixture();
    f.sync(true, f.triggerWrapper);

    assert.ok(f.layer.classes.has('is-anchored'), 'the anchored state class reaches the layer');
    // bottom-end：右边缘对齐锚点右边缘（600 - 200），顶边留在锚点下方 8px。
    assert.equal(f.layer.style.left, '400px');
    assert.equal(f.layer.style.top, '148px');
});

test('outside press treats the anchor as part of the layer', () => {
    const f = fixture();
    f.sync(true, f.triggerWrapper);

    f.listeners.get('pointerdown')({ target: f.triggerWrapper });
    assert.equal(f.closes, 0, 'a press inside the anchor must not close the layer');

    f.listeners.get('pointerdown')({ target: f.outside });
    assert.equal(f.closes, 1, 'a press outside both the layer and the anchor requests a close');
});

test('without an anchor the layer keeps host-relative placement and its host as the anchor box', () => {
    const f = fixture();
    f.sync(true, null);

    assert.ok(!f.layer.classes.has('is-anchored'), 'no anchor means no anchored state class');
    assert.equal(f.layer.style.left, '', 'host-relative placement leaves the placement classes in charge');
    assert.equal(f.layer.style.top, '');

    // 宿主就是锚点盒：按在宿主里不关闭，按在宿主外才关闭。
    f.listeners.get('pointerdown')({ target: f.host });
    assert.equal(f.closes, 0, 'the host stays the anchor box when no anchor is named');

    f.listeners.get('pointerdown')({ target: f.outside });
    assert.equal(f.closes, 1);
});

test('a detached anchor falls back to the host', () => {
    const f = fixture();
    f.sync(true, f.triggerWrapper);
    assert.ok(f.layer.classes.has('is-anchored'));

    // 调用方重渲染把锚点元素换掉：旧引用已脱离文档，不能再拿它定位或当"层内"。
    f.triggerWrapper.isConnected = false;
    f.sync(true, f.triggerWrapper);

    assert.ok(!f.layer.classes.has('is-anchored'), 'a detached anchor is not a usable anchor');
    assert.equal(f.layer.style.left, '');
    assert.equal(f.layer.style.top, '');
});

test('the anchor survives being re-pointed while the layer is open', () => {
    const f = fixture();
    f.sync(true, f.triggerWrapper);

    const next = f.element(f.element(document.body), { left: 500, top: 20, right: 700, bottom: 60, width: 200, height: 40 });
    f.sync(true, next);

    assert.ok(f.layer.classes.has('is-anchored'));
    // 依旧是 bottom-end：右边缘跟到新锚点的右边缘（700 - 浮层宽 200），顶边落在它下方 8px。
    assert.equal(f.layer.style.left, '500px', 'the layer follows the new anchor immediately');
    assert.equal(f.layer.style.top, '68px');
});

test('dispose releases the anchored instance without leaving listeners behind', () => {
    const f = fixture();
    f.sync(true, f.triggerWrapper);
    dispose('anchor');

    assert.equal(f.listeners.get('pointerdown'), undefined, 'the outside-press listener is released');
});
