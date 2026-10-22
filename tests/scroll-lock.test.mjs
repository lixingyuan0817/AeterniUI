// 锁滚动会在根元素写 `--aeterni-scroll-lock-gutter`，全屏覆盖层靠它跨过预留的滚动条槽。
// 这条约定跨 JS 与四份组件 CSS，Node 侧只守 JS 的一半：写入、计数与恢复，以及
// 「覆盖式滚动条（没有槽）不写变量」这条必须在 0px 上退化的分支。
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../src/AeterniUI/wwwroot/js/aeterni_floating.js', import.meta.url), 'utf8');
const { lockScroll } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

function styleStub() {
    const values = new Map();
    return {
        values,
        scrollbarGutter: '',
        backgroundColor: '',
        getPropertyValue: name => values.get(name) ?? '',
        setProperty: (name, value) => values.set(name, String(value)),
        removeProperty: name => { values.delete(name); }
    };
}

function environment({
    innerWidth = 1000,
    clientWidth = 990,
    scrollHeight = 2000,
    clientHeight = 800,
    pageBackground = 'rgb(247, 247, 247)',
    overlay = 'rgba(0, 0, 0, .42)'
} = {}) {
    const body = { style: { overflow: '', paddingRight: '', backgroundColor: pageBackground } };
    const rootStyle = styleStub();
    const root = { style: rootStyle, clientWidth, scrollHeight, clientHeight, clientHeight: 800 };
    globalThis.window = { innerWidth };
    globalThis.document = { body, documentElement: root };
    globalThis.CSS = { supports: () => true };
    globalThis.getComputedStyle = element => ({
        backgroundColor: element === root ? pageBackground : body.style.backgroundColor,
        getPropertyValue: name => (name === '--aeterni-overlay' ? overlay : '')
    });
    return { body, root, rootStyle };
}

const GUTTER = '--aeterni-scroll-lock-gutter';

test('locks with a gutter variable so full-viewport overlays can cover the reserved strip', () => {
    const { body, rootStyle } = environment();
    const release = lockScroll();
    assert.equal(rootStyle.scrollbarGutter, 'stable');
    assert.equal(rootStyle.getPropertyValue(GUTTER), '10px');
    assert.equal(body.style.overflow, 'hidden');
    assert.equal(body.style.paddingRight, '', 'the gutter path must not pad the body');

    release();
    assert.equal(rootStyle.scrollbarGutter, '');
    assert.equal(rootStyle.getPropertyValue(GUTTER), '');
    assert.equal(body.style.overflow, '');
});

test('an overlay-scrollbar engine writes no gutter variable and pads nothing', () => {
    const { body, rootStyle } = environment({ innerWidth: 1000, clientWidth: 1000 });
    const release = lockScroll();
    assert.equal(rootStyle.scrollbarGutter, 'stable');
    assert.equal(rootStyle.getPropertyValue(GUTTER), '');
    assert.equal(body.style.overflow, 'hidden');
    assert.equal(body.style.paddingRight, '', 'a zero-width scrollbar has nothing to compensate');
    release();
});

test('a short page reserves no gutter even when the engine supports it', () => {
    const { rootStyle } = environment({ scrollHeight: 800, clientHeight: 800 });
    const release = lockScroll();
    assert.equal(rootStyle.scrollbarGutter, '');
    assert.equal(rootStyle.getPropertyValue(GUTTER), '');
    release();
});

test('overlapping locks keep the variable until the last release', () => {
    const { rootStyle } = environment();
    const first = lockScroll();
    const second = lockScroll();
    assert.equal(rootStyle.getPropertyValue(GUTTER), '10px');

    first();
    assert.equal(rootStyle.getPropertyValue(GUTTER), '10px', 'a second overlay still needs the cover');

    second();
    assert.equal(rootStyle.getPropertyValue(GUTTER), '');
    assert.equal(rootStyle.scrollbarGutter, '');
});

test('a host-provided gutter value is restored rather than cleared', () => {
    const { rootStyle } = environment();
    rootStyle.setProperty(GUTTER, '4px');
    const release = lockScroll();
    assert.equal(rootStyle.getPropertyValue(GUTTER), '10px');

    release();
    assert.equal(rootStyle.getPropertyValue(GUTTER), '4px');
});

test('paints the reserved gutter with the scrim overlay flattened over the page background', () => {
    const { rootStyle } = environment();
    const release = lockScroll();
    assert.equal(rootStyle.backgroundColor, 'rgb(143, 143, 143)', '0.42 black over 247 starts the strip dimmed');
    release();
    assert.equal(rootStyle.backgroundColor, '', 'the host background is restored');
});

test('a transparent page background is left alone so content is not dimmed twice', () => {
    const { rootStyle } = environment({ pageBackground: 'rgba(0, 0, 0, 0)' });
    const release = lockScroll();
    assert.equal(rootStyle.backgroundColor, '');
    release();
});

test('a host inline page background survives the lock', () => {
    const { rootStyle } = environment();
    rootStyle.backgroundColor = 'rgb(10, 20, 30)';
    const release = lockScroll();
    assert.equal(rootStyle.backgroundColor, 'rgb(143, 143, 143)');
    release();
    assert.equal(rootStyle.backgroundColor, 'rgb(10, 20, 30)');
});
