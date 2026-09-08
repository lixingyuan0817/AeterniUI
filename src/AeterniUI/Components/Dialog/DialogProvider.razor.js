const instances = new Map();

export function init(_reference, key) {
    dispose(key);

    const instance = {
        activeDialog: null,
        previousActiveElement: null,
        previousBodyOverflow: null,
        keydownHandler: null
    };

    instance.keydownHandler = event => handleTabKey(event, instance);
    document.addEventListener('keydown', instance.keydownHandler);
    instances.set(key, instance);
}

export function sync(root, activeDialogId, hasModal) {
    const instance = [...instances.values()][0];
    if (!instance || !root) {
        return;
    }

    instance.activeDialog = activeDialogId;

    if (hasModal) {
        if (instance.previousActiveElement === null) {
            instance.previousActiveElement = document.activeElement;
        }

        if (instance.previousBodyOverflow === null) {
            instance.previousBodyOverflow = document.body.style.overflow;
            document.body.style.overflow = 'hidden';
        }

        const dialog = root.querySelector(`[data-aeterni-dialog-id="${activeDialogId}"]`);
        if (dialog && !dialog.contains(document.activeElement)) {
            queueMicrotask(() => focusDialog(dialog));
        }

        return;
    }

    restoreBodyAndFocus(instance);
}

function focusDialog(dialog) {
    const target = dialog.querySelector(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
    ) || dialog;

    target.focus?.();
}

function handleTabKey(event, instance) {
    if (event.key !== 'Tab' || !instance.activeDialog) {
        return;
    }

    const dialog = document.querySelector(`[data-aeterni-dialog-id="${instance.activeDialog}"]`);
    if (!dialog) {
        return;
    }

    const focusable = [...dialog.querySelectorAll(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
    )];

    if (focusable.length === 0) {
        event.preventDefault();
        dialog.focus?.();
        return;
    }

    const first = focusable[0];
    const last = focusable[focusable.length - 1];

    if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
    }
}

function restoreBodyAndFocus(instance) {
    if (instance.previousBodyOverflow !== null) {
        document.body.style.overflow = instance.previousBodyOverflow;
        instance.previousBodyOverflow = null;
    }

    const previous = instance.previousActiveElement;
    instance.previousActiveElement = null;
    if (previous?.isConnected) {
        previous.focus?.();
    }
}

export function dispose(key) {
    const instance = instances.get(key);
    if (!instance) {
        return;
    }

    document.removeEventListener('keydown', instance.keydownHandler);
    restoreBodyAndFocus(instance);
    instances.delete(key);
}
