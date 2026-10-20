// The Anchor keeps its state in C#; its module only owns the browser side —
// observing which section is current and scrolling the document (or the
// host-provided container) on click, then replacing the URL fragment. These tests
// pin that contract with the same dependency-free DOM double the other module
// tests use.
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../src/AeterniUI/Components/Anchor/Anchor.razor.js', import.meta.url), 'utf8');
const anchor = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

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
    classes = new Set();
    scrollTop = 0;
    scrollHeight = 0;
    clientHeight = 0;
    rect = { top: 0, bottom: 0, left: 0, right: 0, width: 100, height: 100 };

    constructor(parent = null) {
        super();
        this.parentElement = parent;
    }

    getAttribute(name) { return this.attrs.get(name) ?? null; }
    setAttribute(name, value) { this.attrs.set(name, value); }
    getBoundingClientRect() { return this.rect; }
    contains(node) {
        for (let current = node; current; current = current.parentElement) {
            if (current === this) return true;
        }
        return false;
    }
    closest(selector) {
        for (let current = this; current; current = current.parentElement) {
            if (current.matches?.(selector)) return current;
        }
        return null;
    }
    matches(selector) { return selector === 'a[href]' && this.attrs.has('href'); }
    scrollTo(options) {
        this.scrolled = options;
        this.scrollTop = options.top;
    }
}

function section(top, parent = null) {
    const element = new Element(parent);
    element.rect = { top, bottom: top + 200, left: 0, right: 100, width: 100, height: 200 };
    return element;
}

function environment() {
    const documentDouble = new Events();
    documentDouble.byId = new Map();
    documentDouble.documentElement = new Element();
    documentDouble.documentElement.scrollHeight = 1000;
    documentDouble.documentElement.clientHeight = 600;
    documentDouble.getElementById = id => documentDouble.byId.get(id) ?? null;
    globalThis.document = documentDouble;

    const windowDouble = new Events();
    windowDouble.scrollY = 0;
    windowDouble.innerHeight = 600;
    windowDouble.reducedMotion = false;
    windowDouble.scrollTo = options => { windowDouble.scrolled = options; };
    windowDouble.matchMedia = query => ({ media: query, matches: windowDouble.reducedMotion });
    windowDouble.location = { pathname: '/components/anchor', search: '', hash: '' };
    globalThis.window = windowDouble;

    globalThis.history = {
        calls: [],
        replaceState(...args) { this.calls.push(args); }
    };
    windowDouble.history = globalThis.history;

    const observers = [];
    globalThis.IntersectionObserver = class {
        constructor(callback, options) {
            this.callback = callback;
            this.options = options;
            this.observed = new Set();
            this.disconnected = false;
            observers.push(this);
        }
        observe(element) { this.observed.add(element); }
        unobserve(element) { this.observed.delete(element); }
        disconnect() { this.disconnected = true; this.observed.clear(); }
    };

    return { doc: documentDouble, win: windowDouble, observers };
}

function recorder() {
    const calls = [];
    return {
        calls,
        invokeMethodAsync(method, id) {
            calls.push([method, id]);
            return Promise.resolve();
        }
    };
}

function clickEvent(target, overrides = {}) {
    return {
        target,
        button: 0,
        altKey: false,
        ctrlKey: false,
        metaKey: false,
        shiftKey: false,
        defaultPrevented: false,
        preventDefault() { this.defaultPrevented = true; },
        ...overrides
    };
}

const options = overrides => ({
    ids: ['a', 'b', 'c'],
    container: null,
    offset: 0,
    smooth: true,
    updateLocation: true,
    ...overrides
});

test('Anchor module observes every section and releases observer and listeners', () => {
    const env = environment();
    const reference = recorder();
    const host = new Element();
    env.doc.byId.set('a', section(100));
    env.doc.byId.set('b', section(400));
    env.doc.byId.set('c', section(800));

    anchor.init(reference, 'anchor-1');
    anchor.attach('anchor-1', host, options());

    assert.equal(env.observers.length, 1, 'attach creates one observer');
    assert.equal(env.observers[0].options.root, null, 'the document is the observation root without ContainerId');
    assert.equal(env.observers[0].observed.size, 3, 'every rendered section is observed');
    assert.equal(env.win.listenerCount('scroll'), 1, 'the document scroll position is tracked');
    assert.equal(host.listenerCount('click'), 1, 'link clicks are enhanced');
    assert.equal(reference.calls.length, 0, 'no section has been reached yet, so nothing is reported');

    anchor.attach('anchor-1', host, options());
    assert.equal(env.observers.length, 1, 're-attaching the same root reuses the observer');
    assert.equal(env.win.listenerCount('scroll'), 1, 're-attaching does not duplicate the scroll listener');
    assert.equal(host.listenerCount('click'), 1, 're-attaching does not duplicate the click listener');
    assert.equal(env.observers[0].observed.size, 3, 're-attaching does not duplicate observations');

    anchor.dispose('anchor-1');
    assert.equal(env.observers[0].disconnected, true, 'dispose disconnects the observer');
    assert.equal(env.win.listenerCount('scroll'), 0, 'dispose releases the scroll listener');
    assert.equal(host.listenerCount('click'), 0, 'dispose releases the click listener');
    assert.doesNotThrow(() => anchor.dispose('anchor-1'), 'dispose is idempotent');
    assert.doesNotThrow(() => anchor.attach('anchor-1', host, options()), 'attach after dispose is a no-op');
});

test('Anchor module reports the last section above the offset line and only reports changes', () => {
    const env = environment();
    const reference = recorder();
    const host = new Element();
    const a = section(100);
    const b = section(400);
    const c = section(800);
    env.doc.byId.set('a', a);
    env.doc.byId.set('b', b);
    env.doc.byId.set('c', c);

    anchor.init(reference, 'anchor-2');
    anchor.attach('anchor-2', host, options());

    a.rect = { ...a.rect, top: -40, bottom: 160 };
    env.win.emit('scroll');
    assert.deepEqual(reference.calls, [['OnActiveSectionChangedAsync', 'a']], 'a reached section becomes current');

    env.win.emit('scroll');
    assert.equal(reference.calls.length, 1, 'an unchanged section is not reported twice');

    b.rect = { ...b.rect, top: 0, bottom: 200 };
    env.win.emit('scroll');
    assert.deepEqual(reference.calls.at(-1), ['OnActiveSectionChangedAsync', 'b'], 'the last reached section wins');

    a.rect = { ...a.rect, top: 200, bottom: 400 };
    b.rect = { ...b.rect, top: 500, bottom: 700 };
    env.win.emit('scroll');
    assert.deepEqual(reference.calls.at(-1), ['OnActiveSectionChangedAsync', null], 'no reached section clears the marker');

    // A section shorter than the viewport never crosses the offset line, so the
    // bottom of the scroll range activates the last observable section.
    c.rect = { ...c.rect, top: 300, bottom: 400 };
    env.doc.documentElement.scrollHeight = 1000;
    env.win.innerHeight = 600;
    env.win.scrollY = 400;
    env.win.emit('scroll');
    assert.deepEqual(reference.calls.at(-1), ['OnActiveSectionChangedAsync', 'c'], 'the last section wins at the bottom');

    anchor.dispose('anchor-2');
});

test('Anchor module recomputes from the observer callback', () => {
    const env = environment();
    const reference = recorder();
    const host = new Element();
    const a = section(100);
    env.doc.byId.set('a', a);
    env.doc.byId.set('b', section(400));
    env.doc.byId.set('c', section(800));

    anchor.init(reference, 'anchor-3');
    anchor.attach('anchor-3', host, options());

    a.rect = { ...a.rect, top: -10, bottom: 190 };
    env.observers[0].callback([{ target: a, isIntersecting: false }]);

    assert.deepEqual(reference.calls, [['OnActiveSectionChangedAsync', 'a']],
        'an intersection change recomputes the current section from the scroll position');

    anchor.dispose('anchor-3');
});

test('Anchor module scrolls the container and replaces the URL fragment on click', () => {
    const env = environment();
    const host = new Element();

    const container = new Element();
    container.rect = { top: 100, bottom: 600, left: 0, right: 100, width: 100, height: 500 };
    container.scrollTop = 50;
    container.scrollHeight = 2000;
    container.clientHeight = 500;
    env.doc.byId.set('anchor-scroller', container);

    const target = section(360);
    env.doc.byId.set('b', target);

    const link = new Element(host);
    link.setAttribute('href', '#b');

    anchor.init(recorder(), 'anchor-4');
    anchor.attach('anchor-4', host, options({ container: 'anchor-scroller', offset: 20 }));

    const event = clickEvent(link);
    host.emit('click', event);

    assert.equal(event.defaultPrevented, true, 'the module takes over the click');
    assert.deepEqual(container.scrolled, { top: 290, behavior: 'smooth' },
        'the target is scrolled into the container below the offset');
    assert.equal(env.win.scrolled, undefined, 'the document is not scrolled when a container is configured');
    assert.deepEqual(globalThis.history.calls, [[null, '', '/components/anchor#b']],
        'only the fragment is replaced: the current path survives a <base href> host');
});

test('Anchor module scrolls the document and honours reduced motion', () => {
    const env = environment();
    const host = new Element();
    const target = section(40);
    env.doc.byId.set('b', target);

    const link = new Element(host);
    link.setAttribute('href', '#b');

    env.win.reducedMotion = true;
    env.win.scrollY = 10;

    anchor.init(recorder(), 'anchor-5');
    anchor.attach('anchor-5', host, options({ offset: 100, smooth: true }));

    const event = clickEvent(link);
    host.emit('click', event);

    assert.equal(event.defaultPrevented, true);
    assert.deepEqual(env.win.scrolled, { top: 0, behavior: 'instant' },
        'reduced motion wins over Smooth and a negative target clamps to the document top');

    anchor.dispose('anchor-5');
});

test('Anchor module leaves browser-owned clicks and fragment updates untouched', () => {
    const env = environment();
    const host = new Element();
    env.doc.byId.set('b', section(300));

    const link = new Element(host);
    link.setAttribute('href', '#b');
    const external = new Element(host);
    external.setAttribute('href', 'https://example.com');
    const missing = new Element(host);
    missing.setAttribute('href', '#missing');

    anchor.init(recorder(), 'anchor-6');
    anchor.attach('anchor-6', host, options({ updateLocation: false }));

    const modified = clickEvent(link, { ctrlKey: true });
    host.emit('click', modified);
    assert.equal(modified.defaultPrevented, false, 'modified clicks keep the browser behaviour');

    const secondary = clickEvent(link, { button: 1 });
    host.emit('click', secondary);
    assert.equal(secondary.defaultPrevented, false, 'non-primary buttons keep the browser behaviour');

    const externalClick = clickEvent(external);
    host.emit('click', externalClick);
    assert.equal(externalClick.defaultPrevented, false, 'non-fragment links keep the browser behaviour');

    const missingTarget = clickEvent(missing);
    host.emit('click', missingTarget);
    assert.equal(missingTarget.defaultPrevented, false, 'an unknown target keeps the native anchor fallback');

    const plain = clickEvent(link);
    host.emit('click', plain);
    assert.equal(plain.defaultPrevented, true, 'plain clicks are enhanced');
    assert.equal(globalThis.history.calls.length, 0, 'UpdateLocation=false does not touch the URL');

    anchor.dispose('anchor-6');
});

test('Anchor module degrades an unknown ContainerId to the document scroll', () => {
    const env = environment();
    const host = new Element();
    const target = section(300);
    env.doc.byId.set('b', target);

    const link = new Element(host);
    link.setAttribute('href', '#b');

    anchor.init(recorder(), 'anchor-7');
    anchor.attach('anchor-7', host, options({ container: 'not-rendered', smooth: false }));

    const event = clickEvent(link);
    host.emit('click', event);

    assert.equal(event.defaultPrevented, true);
    assert.deepEqual(env.win.scrolled, { top: 300, behavior: 'instant' },
        'a container id that resolves to nothing falls back to the document scroll');

    assert.equal(env.observers[0].options.root, null, 'the observer also falls back to the viewport');
    anchor.dispose('anchor-7');
});

test('Anchor module tolerates a browser without IntersectionObserver', () => {
    const env = environment();
    delete globalThis.IntersectionObserver;

    const reference = recorder();
    const host = new Element();
    const a = section(100);
    env.doc.byId.set('a', a);

    anchor.init(reference, 'anchor-8');
    assert.doesNotThrow(() => anchor.attach('anchor-8', host, options({ ids: ['a'] })));

    a.rect = { ...a.rect, top: -10, bottom: 190 };
    env.win.emit('scroll');
    assert.deepEqual(reference.calls, [['OnActiveSectionChangedAsync', 'a']],
        'the scroll fallback keeps the current marker working without IntersectionObserver');

    anchor.dispose('anchor-8');
});
