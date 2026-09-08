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
    disposeProgress();
    restoreBodyAndFocus(instance);
    instances.delete(key);
}

/* SVG border progress ------------------------------------------------------
   The conic-gradient ring sweeps by angle, which is not uniform along a
   rounded-rectangle perimeter. These rings instead run a stroke-dashoffset
   animation over a pathLength-normalized rounded-rect path, so the progress
   tip advances at a constant rate along the border (like a native progress
   bar). Geometry is measured per card because SVG needs real pixel units. */

const progressNodes = new Map();
const progressObservers = new Map();
let progressResizeHandler = null;

function parseCssLength(value) {
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) ? parsed : 0;
}

function measureProgress(host) {
    const svg = host.querySelector('.aeterni-progress-svg');
    const base = host.querySelector('.aeterni-progress-ring--base');
    const accent = host.querySelector('.aeterni-progress-ring--accent');
    if (!svg || !base || !accent) {
        return;
    }

    const width = host.clientWidth;
    const height = host.clientHeight;
    if (width <= 0 || height <= 0) {
        return;
    }

    const card = host.parentElement;
    const radius = card ? parseCssLength(getComputedStyle(card).borderTopLeftRadius) : 0;
    const inset = 2; // stroke is 2px, centred on the overlay's inset edge.
    const stroke = 2;
    const rx = Math.max(0, radius - inset);
    const x = stroke / 2;
    const y = stroke / 2;
    const w = width - stroke;
    const h = height - stroke;

    svg.setAttribute('viewBox', `${0} ${0} ${width} ${height}`);
    base.setAttribute('x', String(x));
    base.setAttribute('y', String(y));
    base.setAttribute('width', String(w));
    base.setAttribute('height', String(h));
    base.setAttribute('rx', String(rx));
    base.setAttribute('pathLength', '1');
    accent.setAttribute('x', String(x));
    accent.setAttribute('y', String(y));
    accent.setAttribute('width', String(w));
    accent.setAttribute('height', String(h));
    accent.setAttribute('rx', String(rx));
    accent.setAttribute('pathLength', '1');
}

function observeProgressNode(host) {
    if (progressObservers.has(host)) {
        return;
    }

    measureProgress(host);
    const observer = new ResizeObserver(() => measureProgress(host));
    observer.observe(host);
    progressObservers.set(host, observer);
}

function disposeProgressNode(host) {
    const observer = progressObservers.get(host);
    if (observer) {
        observer.disconnect();
        progressObservers.delete(host);
    }
}

export function initProgress(root) {
    const nodes = [...root.querySelectorAll('[data-aeterni-notice-progress="true"]')];

    for (const [known, observer] of progressObservers) {
        if (!root.contains(known)) {
            observer.disconnect();
            progressObservers.delete(known);
        }
    }

    for (const node of nodes) {
        observeProgressNode(node);
    }

    progressNodes.set(root, nodes);

    if (!progressResizeHandler) {
        progressResizeHandler = () => {
            for (const [root] of progressNodes) {
                for (const node of root.querySelectorAll('[data-aeterni-notice-progress="true"]')) {
                    measureProgress(node);
                }
            }
        };
        window.addEventListener('resize', progressResizeHandler);
    }
}

function disposeProgress() {
    if (progressResizeHandler) {
        window.removeEventListener('resize', progressResizeHandler);
        progressResizeHandler = null;
    }

    for (const [, observer] of progressObservers) {
        observer.disconnect();
    }

    progressObservers.clear();
    progressNodes.clear();
}
