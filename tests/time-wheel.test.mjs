import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../src/AeterniUI/Components/TimePicker/TimeOptionList.razor.js', import.meta.url), 'utf8');
const { init, sync, dispose, pendingValues } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

for (const itemHeight of [20, 28, 32, 36, 44]) {
test(`time wheels measure ${itemHeight}px items and notify only after settling`, () => {
    const original = { requestAnimationFrame: globalThis.requestAnimationFrame, cancelAnimationFrame: globalThis.cancelAnimationFrame, setTimeout: globalThis.setTimeout, clearTimeout: globalThis.clearTimeout, ResizeObserver: globalThis.ResizeObserver };
    let resize;
    let disconnected = false;
    globalThis.ResizeObserver = class {
        constructor(callback) { resize = callback; }
        observe() {}
        disconnect() { disconnected = true; }
    };
    const frames = new Map();
    const timers = new Map();
    let id = 0;
    globalThis.requestAnimationFrame = callback => { frames.set(++id, callback); return id; };
    globalThis.cancelAnimationFrame = key => frames.delete(key);
    globalThis.setTimeout = callback => { timers.set(++id, callback); return id; };
    globalThis.clearTimeout = key => timers.delete(key);
    const flush = queue => { const callbacks = [...queue.values()]; queue.clear(); callbacks.forEach(callback => callback()); };
    const calls = [];
    const listeners = new Map();
    const wheel = {
        dataset: { timeUnit: 'minute' }, clientHeight: itemHeight * 5, scrollTop: 0, selected: 0,
        getBoundingClientRect: () => ({ top: 0 }),
        querySelector: () => options[wheel.selected],
        querySelectorAll: () => options,
        addEventListener: (name, callback) => listeners.set(name, callback),
        removeEventListener: name => listeners.delete(name),
        scrollTo: target => { calls.push(target); wheel.scrollTop = target.top; }
    };
    const options = Array.from({ length: 60 }, (_, value) => ({
        dataset: { timeValue: String(value) }, offsetTop: itemHeight * (2 + value), offsetHeight: itemHeight,
        getBoundingClientRect: () => ({ top: itemHeight * (2 + value) - wheel.scrollTop, height: itemHeight })
    }));
    const notifications = [];
    const host = { querySelectorAll: () => [wheel] };
    try {
        init({ invokeMethodAsync: (...args) => { notifications.push(args); return Promise.resolve(); } }, 'test');
        wheel.clientHeight = 0;
        wheel.selected = 25;
        sync('test', host, null, true, false);
        flush(frames);
        resize();
        wheel.clientHeight = itemHeight * 5;
        resize();
        assert.equal(wheel.scrollTop, itemHeight * 25, 'opening a hidden popup must center its existing value');
        wheel.selected = 0;
        sync('test', host, null, true, false);
        flush(frames);
        for (const top of [itemHeight, itemHeight * 2.25, itemHeight * 4]) {
            wheel.scrollTop = top;
            listeners.get('scroll')();
            flush(frames);
            assert.equal(notifications.length, 0);
        }
        assert.equal(timers.size, 1);
        assert.deepEqual(pendingValues('test'), { minute: 4 }, 'immediate confirmation reads the current center');
        flush(timers);
        assert.deepEqual(pendingValues('test'), {});
        assert.deepEqual(notifications, [['OnWheelChangedAsync', 'minute', 4]]);
        wheel.selected = 4;
        const count = calls.length;
        sync('test', host, null, false, false);
        assert.equal(calls.length, count, 'render echo must not restart scrolling');
        listeners.get('scroll')();
        flush(frames);
        flush(timers);
        assert.equal(notifications.length, 1, 'settling must not notify twice');
        wheel.scrollTop = itemHeight * 3.75;
        listeners.get('scroll')();
        flush(frames);
        flush(timers);
        wheel.scrollTop = itemHeight * 3.75;
        listeners.get('wheel')();
        assert.deepEqual(calls.at(-1), { top: itemHeight * 3.75, behavior: 'instant' }, 'reverse input cancels the old smooth snap');
        wheel.scrollTop = itemHeight * 2;
        listeners.get('scroll')();
        flush(frames);
        assert.equal(notifications.length, 1);
        flush(timers);
        assert.deepEqual(notifications.at(-1), ['OnWheelChangedAsync', 'minute', 2]);
        wheel.selected = 12;
        sync('test', host, null, false, false);
        assert.equal(wheel.scrollTop, itemHeight * 12, 'dependent selection must be centered');
        wheel.clientHeight = 0;
        resize();
        wheel.scrollTop = 0;
        wheel.clientHeight = itemHeight * 5;
        resize();
        assert.equal(wheel.scrollTop, itemHeight * 12, 'reopening must restore the current selection');
        flush(frames);
        listeners.get('scroll')();
        dispose('test');
        assert.equal(listeners.size, 0);
        assert.equal(disconnected, true);
        assert.equal(frames.size, 0);
        assert.equal(timers.size, 0);
    } finally {
        dispose('test');
        Object.assign(globalThis, original);
    }
});

}

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
