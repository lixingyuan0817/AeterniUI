import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../src/AeterniUI/Components/TimePicker/TimeOptionList.razor.js', import.meta.url), 'utf8');
const { init, sync, dispose, pendingValues } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

function harness(height = 32, reduced = false) {
    const names = ['window', 'getComputedStyle', 'performance', 'requestAnimationFrame', 'cancelAnimationFrame', 'setTimeout', 'clearTimeout', 'ResizeObserver'];
    const original = Object.fromEntries(names.map(name => [name, globalThis[name]]));
    const frames = new Map(), timers = new Map(), listeners = new Map();
    let id = 0, clock = 0, resize, disconnected = false, motion;
    const media = { matches: reduced, addEventListener: (_, fn) => motion = fn, removeEventListener: () => motion = null };
    globalThis.window = { matchMedia: () => media };
    globalThis.getComputedStyle = () => ({ getPropertyValue: name => name.includes('duration') ? '300ms' : 'cubic-bezier(.2,.8,.2,1)' });
    globalThis.performance = { now: () => clock };
    globalThis.requestAnimationFrame = fn => { frames.set(++id, fn); return id; };
    globalThis.cancelAnimationFrame = id => frames.delete(id);
    globalThis.setTimeout = fn => { timers.set(++id, fn); return id; };
    globalThis.clearTimeout = id => timers.delete(id);
    globalThis.ResizeObserver = class { constructor(fn) { resize = fn; } observe() {} disconnect() { disconnected = true; } };
    const options = Array.from({ length: 60 }, (_, value) => ({
        dataset: { timeValue: String(value) }, offsetTop: height * (2 + value), offsetHeight: height,
        disabled: false, label: { style: {} },
        getAttribute() { return this.disabled ? 'true' : null; }, querySelector() { return this.label; }
    }));
    const wheel = { dataset: { timeUnit: 'minute' }, clientHeight: height * 5, scrollTop: 0, selected: 25,
        querySelector: () => options[wheel.selected], querySelectorAll: () => options,
        addEventListener: (name, fn) => listeners.set(name, fn), removeEventListener: name => listeners.delete(name) };
    const calls = [];
    init({ invokeMethodAsync: (...args) => { calls.push(args); return Promise.resolve(); } }, 'test');
    const render = (animate = true, revision = 0) => sync('test', { querySelectorAll: () => [wheel] }, null, true, animate, revision);
    const flush = queue => { const work = [...queue.values()]; queue.clear(); work.forEach(fn => fn(clock)); };
    return { wheel, options, calls, render, frames, timers, media,
        event: name => listeners.get(name)(), resize: () => resize(),
        tick: (ms = 300) => { clock += ms; flush(frames); }, quiet: () => flush(timers),
        motion: () => motion(),
        close: () => { dispose('test'); assert.equal(frames.size, 0); assert.equal(timers.size, 0); assert.equal(listeners.size, 0); assert.ok(disconnected); assert.equal(motion, null); Object.assign(globalThis, original); }
    };
}

for (const height of [20, 28, 32, 36, 44]) {
    test(`real module centers and animates ${height}px rows without intermediate callbacks`, () => {
        const h = harness(height);
        try {
            h.wheel.clientHeight = 0; h.render();
            h.wheel.clientHeight = height * 5; h.resize();
            assert.equal(h.wheel.scrollTop, height * 25);
            for (const value of [0, 59, 12]) {
                h.wheel.selected = value; h.render();
                assert.deepEqual(pendingValues('test'), { minute: value });
                h.tick(100); const top = h.wheel.scrollTop;
                h.render(); h.tick(200);
                assert.notEqual(top, h.wheel.scrollTop);
                assert.equal(h.wheel.scrollTop, height * value);
                assert.equal(h.calls.length, 0, 'click/key/external alignment must not echo a draft callback');
            }
            h.event('wheel'); h.wheel.scrollTop = height * 4.25; h.event('scroll');
            h.tick(16); h.quiet();
            assert.equal(h.calls.length, 0, 'snap must finish before publishing');
            h.tick(100); h.render(false); h.tick(200);
            assert.equal(h.wheel.scrollTop, height * 4);
            assert.deepEqual(h.calls, [['OnWheelChangedAsync', 'minute', 4]]);
            h.wheel.selected = 4; h.render(); h.event('scroll'); h.quiet(); h.tick();
            assert.equal(h.calls.length, 1);
            assert.match(h.options[4].label.style.transform, /rotateX\(0deg\) scale\(1\)/);
            assert.ok(Number(h.options[5].label.style.opacity) >= .8);
        } finally { h.close(); }
    });
}

test('rapid target replacement, user interruption, disabled rows and callback echoes', () => {
    const h = harness();
    try {
        h.render(); h.wheel.selected = 50; h.render(); h.tick(80);
        h.wheel.selected = 10; h.render(); h.tick();
        assert.equal(h.wheel.scrollTop, 320);
        h.wheel.selected = 40; h.render(); h.tick(80); h.event('touchstart');
        h.wheel.scrollTop = 32 * 7; h.options[7].disabled = true; h.render(false); h.event('scroll'); h.quiet(); h.tick();
        assert.equal(h.wheel.scrollTop, 32 * 6);
        assert.deepEqual(h.calls, [['OnWheelChangedAsync', 'minute', 6]]);
        h.event('wheel'); h.wheel.scrollTop = 32 * 9.2; h.event('scroll');
        h.wheel.selected = 6; h.render();
        assert.equal(h.wheel.scrollTop, 32 * 9.2, 'async callback render cannot reset fresh input');
        h.quiet(); h.tick();
        assert.deepEqual(h.calls.at(-1), ['OnWheelChangedAsync', 'minute', 9]);
    } finally { h.close(); }
});

test('availability changes recenter unchanged values after rows are removed', () => {
    const h = harness();
    try {
        h.render();
        for (const option of h.options) option.offsetTop -= 32;
        h.render(); h.tick();
        assert.equal(h.wheel.scrollTop, 24 * 32);
        assert.equal(h.calls.length, 0);
    } finally { h.close(); }
});

test('explicit same-value click cancels input while duplicate renders do not restart it', () => {
    const h = harness();
    try {
        h.render(); h.event('pointerdown'); h.wheel.scrollTop = 32 * 24.2; h.event('scroll');
        h.render(true, 1); h.tick(100); const intermediate = h.wheel.scrollTop;
        h.render(true, 1); h.tick(200);
        assert.notEqual(intermediate, h.wheel.scrollTop);
        assert.equal(h.wheel.scrollTop, 32 * 25);
        assert.equal(h.calls.length, 0);
    } finally { h.close(); }
});

test('reduced motion positions directly, commits once and cancels live animation', () => {
    const h = harness(32, true);
    try {
        h.render(); h.wheel.selected = 59; h.render();
        assert.equal(h.wheel.scrollTop, 59 * 32);
        assert.equal(h.frames.size, 0);
        assert.equal(h.options[59].label.style.transform, 'none');
        h.event('wheel'); h.wheel.scrollTop = 32 * 3.2; h.event('scroll'); h.quiet();
        assert.deepEqual(h.calls, [['OnWheelChangedAsync', 'minute', 3]]);
        h.media.matches = false; h.wheel.selected = 20; h.render(); h.tick(50);
        h.media.matches = true; h.motion();
        assert.equal(h.wheel.scrollTop, 20 * 32);
    } finally { h.close(); }
});

test('shared density tokens retain the spacing scale and wheel geometry', async () => {
    const css = await readFile(new URL('../src/AeterniUI/wwwroot/css/aeterni_ui.css', import.meta.url), 'utf8');
    const tokens = new Map([...css.matchAll(/(--aeterni-[\w-]+):\s*([^;]+);/g)].map(match => [match[1], match[2]]));
    const resolve = name => {
        const value = tokens.get(`--aeterni-${name}`);
        const alias = /^var\(--aeterni-([\w-]+)\)$/.exec(value);
        return alias ? resolve(alias[1]) : value;
    };
    for (const [name, value] of Object.entries({
        'control-height-sm': '28px', 'control-height-md': '36px', 'control-height-lg': '44px',
        'control-padding-x-sm': '8px', 'control-padding-x-md': '12px', 'control-padding-x-lg': '16px',
        'row-height-compact': '32px', 'touch-target-min': '24px', 'touch-target': '44px',
        'padding-sm': '8px', 'padding-lg': '12px', 'padding-xl': '20px', 'padding-2xl': '24px',
        'spacing-4': '16px', 'spacing-8': '32px', 'spacing-10': '40px', 'spacing-12': '48px',
        'font-size-base': '16px', 'line-height-normal': '1.5'
    })) assert.equal(resolve(name), value, name);
    const wheel = await readFile(new URL('../src/AeterniUI/Components/TimePicker/TimeOptionList.razor.css', import.meta.url), 'utf8');
    assert.match(wheel, /height: calc\(var\(--aeterni-row-height-compact\) \* 5\)/);
    for (const component of ['Card', 'Surface']) {
        const surface = await readFile(new URL(`../src/AeterniUI/Components/${component}/${component}.razor.css`, import.meta.url), 'utf8');
        for (const tier of ['sm', 'lg', 'xl', '2xl']) assert.ok(surface.includes(`var(--aeterni-padding-${tier})`));
    }
});
