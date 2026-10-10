// Page-level scroll lock and the focusable-element list come from the shared
// floating-layer helpers, so Dialog, Popover and (later) Drawer agree on what
// "lock the page" and "first focusable" mean. The Tab/Escape handling stays local:
// dialogs form a stack and only the topmost one may respond.
import { focusableWithin, focusFirst, lockScroll } from '../../js/aeterni_floating.js';

const instances = new Map();

export function init(_reference, key) {
    dispose(key);

    const instance = {
        activeDialog: null,
        previousActiveElement: null,
        releaseScroll: null,
        keydownHandler: null,
        progressRoot: null
    };

    instance.keydownHandler = event => handleTabKey(event, instance);
    document.addEventListener('keydown', instance.keydownHandler);
    instances.set(key, instance);
}

export function sync(key, root, activeDialogId, hasModal) {
    const instance = instances.get(key);
    if (!instance || !root) {
        return;
    }

    instance.activeDialog = activeDialogId;

    if (hasModal) {
        if (instance.previousActiveElement === null) {
            instance.previousActiveElement = document.activeElement;
        }

        if (instance.releaseScroll === null) {
            instance.releaseScroll = lockScroll();
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
    focusFirst(dialog);
}

function handleTabKey(event, instance) {
    if (event.key !== 'Tab' || !instance.activeDialog) {
        return;
    }

    const dialog = document.querySelector(`[data-aeterni-dialog-id="${instance.activeDialog}"]`);
    if (!dialog) {
        return;
    }

    const focusable = focusableWithin(dialog);

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
    instance.releaseScroll?.();
    instance.releaseScroll = null;

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

    // The resize listener and the observers are module-level state shared by every
    // provider on the page, so only the last one to go may tear them down.
    // Disposing them here unconditionally would let one provider unmounting strip
    // another provider's measurements.
    pruneProgressObservers();
    if (instances.size === 0) {
        disposeProgress();
    }
}

/* SVG border progress ------------------------------------------------------
   The conic-gradient ring sweeps by angle, which is not uniform along a
   rounded-rectangle perimeter. These rings instead run a stroke-dashoffset
   animation over a pathLength-normalized rounded-rect path, so the progress
   tip advances at a constant rate along the border (like a native progress
   bar). Geometry is measured per card because SVG needs real pixel units. */

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

// A host is still owned as long as any live provider's subtree contains it, so
// pruning is scoped across every instance rather than to the provider that
// happens to be syncing.
function pruneProgressObservers() {
    for (const [host, observer] of progressObservers) {
        const owned = [...instances.values()].some(instance => instance.progressRoot?.contains(host));
        if (!owned) {
            observer.disconnect();
            progressObservers.delete(host);
        }
    }
}

export function initProgress(key, root) {
    const instance = instances.get(key);
    if (!instance || !root) {
        return;
    }

    instance.progressRoot = root;
    pruneProgressObservers();

    for (const node of root.querySelectorAll('[data-aeterni-notice-progress="true"]')) {
        observeProgressNode(node);
    }

    if (!progressResizeHandler) {
        progressResizeHandler = () => {
            for (const instance of instances.values()) {
                const currentRoot = instance.progressRoot;
                if (!currentRoot) {
                    continue;
                }

                for (const node of currentRoot.querySelectorAll('[data-aeterni-notice-progress="true"]')) {
                    measureProgress(node);
                }
            }
        };
        window.addEventListener('resize', progressResizeHandler);
    }
}

/** Resolves once the dialog's shake animation has finished. The C# side awaits
 * this instead of guessing the CSS duration: a hand-tuned delay drifts whenever
 * the motion scale changes, and under `prefers-reduced-motion` the animation
 * completes instantly while a fixed delay would still block. */
export async function waitForShake(key, dialogId) {
    if (!instances.has(key)) {
        return;
    }

    const dialog = document.querySelector(`[data-aeterni-dialog-id="${dialogId}"]`);
    const shakes = (dialog?.getAnimations?.() ?? [])
        .filter(animation => /shake/.test(animation.animationName ?? ''));
    await Promise.allSettled(shakes.map(animation => animation.finished));
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
}
