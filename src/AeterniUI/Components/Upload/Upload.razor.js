// A file drop reaches the browser as a FileList on the event, never as the input
// element's own files, and Blazor's <InputFile> only reads that element. So this
// module is the bridge: it copies `dataTransfer.files` onto the input and
// dispatches a synthetic `change`, after which C# sees exactly the same event as
// a picker selection. Validation, the file list and the transfer status all stay
// in C#.
//
// Copying is the only available route: writing a file input's value is forbidden,
// and `files` accepts an assignment only for a FileList the browser produced
// itself (a drop is such a case). When an engine refuses the assignment the drop
// is ignored instead of being reported as a selection that would stream nothing.
//
// Clearing happens when the picker is clicked, not after C# handled a change:
// a file input fires no `change` at all when the same file is chosen twice, so the
// value has to be gone *before* the dialog opens. Clearing afterwards instead
// races with the next interaction — the round trip back from C# can land after a
// following drop has already filled the input, and the selection is then wiped
// before the framework reads it.
//
// The listener set is attached to the drop zone, not to the root: the zone is the
// element that paints `is-dragover`, and a host-provided `Element` replaces the
// root without moving the zone.

const instances = new Map();

export function init(reference, key) {
    dispose(key);
    instances.set(key, {
        reference,
        zone: null,
        input: null,
        bound: false,
        enabled: false,
        multiple: false,
        inputBound: false,
        onDragEnter: null,
        onDragOver: null,
        onDragLeave: null,
        onDrop: null,
        onInputClick: null
    });
}

export function sync(key, zone, input, enabled, multiple) {
    const state = instances.get(key);
    if (!state) return;

    if (state.zone !== zone) {
        detach(state);
        state.zone = zone;
    }

    if (state.input !== input) {
        detachInput(state);
        state.input = input ?? null;
    }

    state.enabled = Boolean(enabled);
    state.multiple = Boolean(multiple);

    // Listeners stay attached while disabled: the zone still has to swallow a
    // dropped file, because the browser's default for an unhandled drop is to
    // navigate away to that file. `enabled` only decides whether the drop is
    // accepted and whether the highlight is painted.
    attach(state);
    attachInput(state);

    if (!state.enabled) {
        state.zone?.classList.remove('is-dragover');
    }
}

export function dispose(key) {
    const state = instances.get(key);
    if (!state) return;
    detach(state);
    detachInput(state);
    instances.delete(key);
}

function attach(state) {
    if (state.bound || !state.zone) return;

    state.onDragEnter = event => {
        if (!carriesFiles(event)) return;
        // Cancelling dragover is what makes the element a drop target at all, and
        // it also stops the browser from navigating to the dropped file.
        event.preventDefault();
        if (state.enabled) state.zone.classList.add('is-dragover');
    };
    state.onDragOver = event => {
        if (carriesFiles(event)) event.preventDefault();
    };
    state.onDragLeave = event => {
        // dragleave also fires when the pointer moves onto a child of the zone;
        // only leaving the zone's own subtree is an exit.
        if (event.target === state.zone || !state.zone.contains(event.relatedTarget)) {
            state.zone.classList.remove('is-dragover');
        }
    };
    state.onDrop = event => {
        if (carriesFiles(event)) event.preventDefault();
        state.zone.classList.remove('is-dragover');
        if (state.enabled) accept(state, event.dataTransfer);
    };

    state.bound = true;
    state.zone.addEventListener('dragenter', state.onDragEnter);
    state.zone.addEventListener('dragover', state.onDragOver);
    state.zone.addEventListener('dragleave', state.onDragLeave);
    state.zone.addEventListener('drop', state.onDrop);
}

function detach(state) {
    if (!state.bound || !state.zone) return;
    state.zone.removeEventListener('dragenter', state.onDragEnter);
    state.zone.removeEventListener('dragover', state.onDragOver);
    state.zone.removeEventListener('dragleave', state.onDragLeave);
    state.zone.removeEventListener('drop', state.onDrop);
    state.bound = false;
}

function attachInput(state) {
    if (state.inputBound || !state.input) return;
    state.onInputClick = () => {
        // Runs while the dialog is opening, so the picker always reports its
        // selection as a change even when it is the file already in the list.
        state.input.value = '';
    };
    state.inputBound = true;
    state.input.addEventListener('click', state.onInputClick);
}

function detachInput(state) {
    if (!state.inputBound || !state.input) return;
    state.input.removeEventListener('click', state.onInputClick);
    state.inputBound = false;
}

function accept(state, transfer) {
    const files = transfer?.files;
    if (!state.input || !files || files.length === 0) return;

    try {
        state.input.files = state.multiple ? files : firstOnly(files);
    } catch {
        // The engine refused a scripted assignment; the picker path stays
        // available and nothing is reported as selected.
        return;
    }

    state.input.dispatchEvent(new Event('change', { bubbles: true }));
}

function firstOnly(files) {
    if (files.length <= 1 || typeof DataTransfer !== 'function') return files;
    const single = new DataTransfer();
    single.items.add(files[0]);
    return single.files;
}

function carriesFiles(event) {
    const types = event?.dataTransfer?.types;
    if (!types) return false;
    return Array.from(types).includes('Files');
}
