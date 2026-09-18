import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../src/AeterniUI/Components/TimePicker/TimeOptionList.razor.js', import.meta.url), 'utf8');
const { init, sync, dispose, pendingValues } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

test('time wheels preserve continuous scrolling and notify only after settling', () => {
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
        dataset: { timeUnit: 'minute' }, clientHeight: 100, scrollTop: 0, selected: 0,
        getBoundingClientRect: () => ({ top: 0 }),
        querySelector: () => options[wheel.selected],
        querySelectorAll: () => options,
        addEventListener: (name, callback) => listeners.set(name, callback),
        removeEventListener: name => listeners.delete(name),
        scrollTo: target => { calls.push(target); wheel.scrollTop = target.top; }
    };
    const options = Array.from({ length: 60 }, (_, value) => ({
        dataset: { timeValue: String(value) }, offsetTop: 40 + value * 20, offsetHeight: 20,
        getBoundingClientRect: () => ({ top: 40 + value * 20 - wheel.scrollTop, height: 20 })
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
        wheel.clientHeight = 100;
        resize();
        assert.equal(wheel.scrollTop, 500, 'opening a hidden popup must center its existing value');
        wheel.selected = 0;
        sync('test', host, null, true, false);
        flush(frames);
        for (const top of [20, 45, 80]) {
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
        wheel.scrollTop = 75;
        listeners.get('scroll')();
        flush(frames);
        flush(timers);
        wheel.scrollTop = 75;
        listeners.get('wheel')();
        assert.deepEqual(calls.at(-1), { top: 75, behavior: 'instant' }, 'reverse input cancels the old smooth snap');
        wheel.scrollTop = 40;
        listeners.get('scroll')();
        flush(frames);
        assert.equal(notifications.length, 1);
        flush(timers);
        assert.deepEqual(notifications.at(-1), ['OnWheelChangedAsync', 'minute', 2]);
        wheel.selected = 12;
        sync('test', host, null, false, false);
        assert.equal(wheel.scrollTop, 240, 'dependent selection must be centered');
        wheel.clientHeight = 0;
        resize();
        wheel.scrollTop = 0;
        wheel.clientHeight = 100;
        resize();
        assert.equal(wheel.scrollTop, 240, 'reopening must restore the current selection');
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
