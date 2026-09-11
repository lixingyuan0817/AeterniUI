/*
   Drawer module.

   Geometry is CSS-only (the panel is pinned to a viewport edge by its placement
   class), so this module owns exactly the two things a stylesheet cannot do: the
   background scroll lock and the focus trap of the modal form. The shared
   floating-layer helpers provide both (see wwwroot/js/aeterni_floating.js).

   The non-modal form keeps focus where the consumer put it and only listens for
   Escape and outside pointer presses; the modal form hands Tab and Escape to the
   focus trap, because the trap is what owns "the topmost layer" while the drawer is
   open.
*/

import { createFocusTrap, lockScroll } from '../../js/aeterni_floating.js';

const instances = new Map();

export function init(reference, key) {
    dispose(key);
    instances.set(key, {
        reference,
        panel: null,
        modal: false,
        closeOnEscape: true,
        closeOnOutsideClick: true,
        active: false,
        trap: null,
        releaseScroll: null,
        keyHandler: null,
        pointerHandler: null
    });
}

export function dispose(key) {
    const instance = instances.get(key);
    if (!instance) {
        return;
    }

    deactivate(instance);
    instances.delete(key);
}

export function setOpen(key, open, panel, modal, closeOnEscape, closeOnOutsideClick) {
    const instance = instances.get(key);
    if (!instance) {
        return;
    }

    if (panel) {
        instance.panel = panel;
    }

    const nextModal = !!modal;
    const modalChanged = instance.modal !== nextModal;

    instance.modal = nextModal;
    instance.closeOnEscape = !!closeOnEscape;
    instance.closeOnOutsideClick = !!closeOnOutsideClick;

    if (!open) {
        deactivate(instance);
        return;
    }

    if (!instance.active) {
        activate(instance);
        return;
    }

    if (modalChanged) {
        deactivate(instance);
        activate(instance);
        return;
    }

    // Same mode, possibly different close options: refresh just those listeners.
    // Rebuilding the focus trap here would lose the element that had focus when the
    // drawer opened, so Escape would no longer hand focus back to the opener.
    uninstallEscape(instance);
    uninstallOutsidePointer(instance);
    installOptionalListeners(instance);
}

function activate(instance) {
    instance.active = true;

    installOptionalListeners(instance);

    if (instance.modal) {
        instance.releaseScroll = lockScroll();

        if (instance.panel) {
            instance.trap = createFocusTrap(instance.panel, { onEscape: () => requestClose(instance) });
        }
    }
}

function deactivate(instance) {
    instance.active = false;

    uninstallEscape(instance);
    uninstallOutsidePointer(instance);

    instance.trap?.release();
    instance.trap = null;
    instance.releaseScroll?.();
    instance.releaseScroll = null;
}

// Escape and outside-pointer are only installed for the non-modal form: a modal
// drawer gets Escape from its focus trap and closes on its backdrop, so it must not
// react to a press that landed on the page behind the backdrop.
function installOptionalListeners(instance) {
    if (instance.modal) {
        return;
    }

    if (instance.closeOnEscape) {
        instance.keyHandler = event => {
            if (event.key === 'Escape') {
                event.stopPropagation();
                requestClose(instance);
            }
        };

        document.addEventListener('keydown', instance.keyHandler, true);
    }

    if (instance.closeOnOutsideClick) {
        instance.pointerHandler = event => {
            const target = event.target;
            if (!instance.active || !instance.panel || !(target instanceof Node)) {
                return;
            }

            if (!instance.panel.contains(target)) {
                requestClose(instance);
            }
        };

        document.addEventListener('pointerdown', instance.pointerHandler, true);
    }
}

function uninstallEscape(instance) {
    if (instance.keyHandler) {
        document.removeEventListener('keydown', instance.keyHandler, true);
        instance.keyHandler = null;
    }
}

function uninstallOutsidePointer(instance) {
    if (instance.pointerHandler) {
        document.removeEventListener('pointerdown', instance.pointerHandler, true);
        instance.pointerHandler = null;
    }
}

// Closing is always the consumer's decision: the module only reports the request,
// so `@bind-Open` stays the single source of truth.
function requestClose(instance) {
    if (!instance.active) {
        return;
    }

    instance.reference?.invokeMethodAsync('RequestCloseAsync')?.catch(() => undefined);
}
