// The Upload module exists for one reason: a drop delivers its files on the event
// and never in the input element that Blazor reads. These tests pin that bridge —
// the copy, the single-file truncation, the dragover state and the release — so a
// refactor cannot quietly stop reporting drops. The end-to-end path (real
// DataTransfer, real Blazor change handling) is covered by the browser checks;
// this file pins the module contract they depend on.
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../src/AeterniUI/Components/Upload/Upload.razor.js', import.meta.url), 'utf8');
const upload = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

class Events {
    listeners = new Map();
    addEventListener(name, callback) { if (!this.listeners.has(name)) this.listeners.set(name, new Set()); this.listeners.get(name).add(callback); }
    removeEventListener(name, callback) { this.listeners.get(name)?.delete(callback); }
    emit(name, event = {}) { for (const callback of [...(this.listeners.get(name) ?? [])]) callback(event); }
    listenerCount(name) { return this.listeners.get(name)?.size ?? 0; }
}

class Element extends Events {
    classes = new Set();
    changes = 0;
    value = '';
    files = [];
    isConnected = true;
    classList = {
        contains: value => this.classes.has(value),
        add: (...values) => values.forEach(value => this.classes.add(value)),
        remove: (...values) => values.forEach(value => this.classes.delete(value))
    };
    constructor(parent = null) { super(); this.parentElement = parent; }
    contains(node) { for (; node; node = node.parentElement) if (node === this) return true; return false; }
    dispatchEvent(event) { if (event.type === 'change') this.changes++; this.emit(event.type, event); return true; }
}

class DataTransferDouble {
    constructor(files = []) { this._files = [...files]; this.items = { add: file => this._files.push(file) }; }
    get files() { return this._files; }
}

globalThis.Event = class { constructor(type) { this.type = type; } };
globalThis.DataTransfer = DataTransferDouble;

function environment() {
    const zone = new Element();
    const input = new Element(zone);
    input.setAttribute = () => {};
    globalThis.document = undefined;
    upload.init({ invokeMethodAsync: () => Promise.resolve() }, 'upload-1');
    upload.sync('upload-1', zone, input, true, true);
    return { zone, input };
}

const drop = (zone, files) => {
    const event = {
        preventDefault() { this.prevented = true; },
        dataTransfer: { types: ['Files'], files }
    };
    zone.emit('drop', event);
    return event;
};

const dragOver = (zone, type = 'dragenter') => {
    const event = { preventDefault() { this.prevented = true; }, dataTransfer: { types: ['Files'] } };
    zone.emit(type, event);
    return event;
};

test('a drop lands in the input Blazor reads and raises the change event', () => {
    const { zone, input } = environment();
    const files = [{ name: 'report.pdf' }];

    const event = drop(zone, files);

    assert.equal(event.prevented, true, 'the browser must not navigate to the dropped file');
    assert.deepEqual(input.files, files, 'the dropped FileList is copied onto the input');
    assert.equal(input.changes, 1, 'the input reports the drop as its own change');
});

test('a single-file upload keeps the first dropped file only', () => {
    const { zone, input } = environment();
    upload.sync('upload-1', zone, input, true, false);

    drop(zone, [{ name: 'a.txt' }, { name: 'b.txt' }]);

    assert.deepEqual(input.files.map(file => file.name), ['a.txt']);
    assert.equal(input.changes, 1);
});

test('a refused assignment reports nothing instead of a file that cannot be read', () => {
    const { zone, input } = environment();
    Object.defineProperty(input, 'files', { set() { throw new Error('refused'); } });

    drop(zone, [{ name: 'a.txt' }]);

    assert.equal(input.changes, 0, 'an unsupported engine must not fake a selection');
});

test('the drop zone paints is-dragover only for a file drag and clears it on exit', () => {
    const { zone } = environment();

    zone.emit('dragenter', { preventDefault() {}, dataTransfer: { types: ['text/plain'] } });
    assert.equal(zone.classList.contains('is-dragover'), false, 'a text drag is not a file drag');

    const enter = dragOver(zone);
    assert.equal(enter.prevented, true, 'cancelling dragover is what makes the zone a drop target');
    assert.equal(zone.classList.contains('is-dragover'), true);

    // Moving onto a child of the zone fires dragleave; that is not an exit.
    const child = new Element(zone);
    zone.emit('dragleave', { target: child, relatedTarget: child });
    assert.equal(zone.classList.contains('is-dragover'), true, 'the highlight survives entering a child');

    const outside = new Element();
    zone.emit('dragleave', { target: zone, relatedTarget: outside });
    assert.equal(zone.classList.contains('is-dragover'), false);
});

test('a disabled zone stays inert but still swallows the drop', () => {
    const { zone, input } = environment();
    upload.sync('upload-1', zone, input, false, true);

    dragOver(zone);
    assert.equal(zone.classList.contains('is-dragover'), false, 'a disabled zone never highlights');

    const event = drop(zone, [{ name: 'a.txt' }]);

    assert.equal(event.prevented, true, 'dropping on a disabled zone must not navigate away');
    assert.deepEqual(input.files, [], 'nothing is reported as selected');
    assert.equal(input.changes, 0);
    assert.equal(zone.listenerCount('drop'), 1, 'the drop must still be swallowed');
});

test('reset clears the picker so the same file can be chosen again', () => {
    const { input } = environment();
    input.value = 'C:\\fakepath\\report.pdf';

    upload.reset('upload-1');

    assert.equal(input.value, '');
});

test('dispose releases every drag listener', () => {
    const { zone } = environment();
    assert.equal(zone.listenerCount('dragenter'), 1);
    assert.equal(zone.listenerCount('dragover'), 1);
    assert.equal(zone.listenerCount('dragleave'), 1);
    assert.equal(zone.listenerCount('drop'), 1);

    upload.dispose('upload-1');

    for (const name of ['dragenter', 'dragover', 'dragleave', 'drop']) {
        assert.equal(zone.listenerCount(name), 0, `${name} must be released`);
    }
});

test('disposing before init and syncing an unknown key are no-ops', () => {
    assert.doesNotThrow(() => upload.dispose('missing'));
    assert.doesNotThrow(() => upload.sync('missing', new Element(), new Element(), true, true));
    assert.doesNotThrow(() => upload.reset('missing'));
});
