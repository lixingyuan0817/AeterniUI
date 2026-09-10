/*
   Popover module.

   Position: the layer is absolutely positioned inside its PopupHost by the
   placement classes, so this module only has to flip the side when the viewport
   has no room and shift the layer on the cross axis. The shared floating-layer
   helpers own that maths (see wwwroot/js/aeterni_floating.js).

   Modal chrome: Escape, outside pointer, background scroll lock and a focus trap.
   A non-modal layer keeps focus where the consumer put it, which is what a
   dropdown anchored to a trigger expects.
*/

import {
    clampAlignedShift,
    createFocusTrap,
    fitsSide,
    lockScroll,
    oppositeSide
} from '../../js/aeterni_floating.js';

const PLACEMENTS = ['bottom-start', 'bottom-end', 'top-start', 'top-end'];
const instances = new Map();

export function init(reference, key) {
    dispose(key);
    instances.set(key, {
        reference,
        layer: null,
        modal: false,
        preferred: 'bottom-start',
        closeOnEscape: true,
        closeOnOutsideClick: true,
        active: false,
        trap: null,
        releaseScroll: null,
        pointerHandler: null,
        keyHandler: null,
        resizeHandler: null,
        scrollHandler: null
    });
}

/**
 * Synchronises the instance with the rendered state. Called after every render, so
 * it has to be idempotent: only a change of `open` or of the modal flag performs
 * work.
 */
export function setOpen(key, open, layer, modal, placement, closeOnEscape, closeOnOutsideClick) {
    const instance = instances.get(key);
    if (!instance) {
        return;
    }

    instance.layer = layer ?? instance.layer;
    instance.preferred = PLACEMENTS.includes(placement) ? placement : instance.preferred;
    instance.closeOnEscape = !!closeOnEscape;
    instance.closeOnOutsideClick = !!closeOnOutsideClick;

    if (open && !instance.active) {
        activate(instance, !!modal);
        return;
    }

    if (!open && instance.active) {
        deactivate(instance);
        return;
    }

    if (!open) {
        return;
    }

    if (instance.modal !== !!modal) {
        deactivate(instance);
        activate(instance, !!modal);
        return;
    }

    place(instance);
}

function activate(instance, modal) {
    instance.modal = modal;
    instance.active = true;

    installOutsidePointer(instance);
    installEscape(instance);

    instance.resizeHandler = () => place(instance);
    window.addEventListener('resize', instance.resizeHandler, { passive: true });

    instance.scrollHandler = () => place(instance);
    document.addEventListener('scroll', instance.scrollHandler, { capture: true, passive: true });

    if (modal) {
        instance.releaseScroll = lockScroll();
        instance.trap = createFocusTrap(instance.layer, { onEscape: () => requestClose(instance) });
    }

    place(instance);
}

function deactivate(instance) {
    instance.active = false;

    if (instance.pointerHandler) {
        document.removeEventListener('pointerdown', instance.pointerHandler, true);
        instance.pointerHandler = null;
    }

    if (instance.keyHandler) {
        document.removeEventListener('keydown', instance.keyHandler, true);
        instance.keyHandler = null;
    }

    if (instance.resizeHandler) {
        window.removeEventListener('resize', instance.resizeHandler);
        instance.resizeHandler = null;
    }

    if (instance.scrollHandler) {
        document.removeEventListener('scroll', instance.scrollHandler, { capture: true });
        instance.scrollHandler = null;
    }

    instance.trap?.release();
    instance.trap = null;
    instance.releaseScroll?.();
    instance.releaseScroll = null;
}

// A non-modal layer closes when the pointer goes down outside it.
//
// The anchor is the layer's own parent (the PopupHost), and a press inside it does
// not close: that is what keeps a toggle button placed in the host from both
// closing and reopening the layer on the same press. Put the trigger inside the
// PopupHost — the host *is* the anchor box, so this is also the only layout where
// the layer has something to align against.
function installOutsidePointer(instance) {
    if (instance.modal) {
        return;
    }

    instance.pointerHandler = event => {
        if (!instance.closeOnOutsideClick || !instance.active || !instance.layer) {
            return;
        }

        const target = event.target;
        if (!(target instanceof Node)) {
            return;
        }

        const anchor = instance.layer.parentElement ?? instance.layer;
        if (!instance.layer.contains(target) && !anchor.contains(target)) {
            requestClose(instance);
        }
    };

    document.addEventListener('pointerdown', instance.pointerHandler, true);
}

// Escape is only handled here for non-modal layers: a modal layer gets Escape from
// its focus trap, so the trap owns the key when a layer is on top.
function installEscape(instance) {
    if (instance.modal) {
        return;
    }

    instance.keyHandler = event => {
        if (event.key === 'Escape' && instance.closeOnEscape) {
            event.stopPropagation();
            requestClose(instance);
        }
    };

    document.addEventListener('keydown', instance.keyHandler, true);
}

function requestClose(instance) {
    if (!instance.active) {
        return;
    }

    instance.reference
        ?.invokeMethodAsync('RequestCloseAsync')
        .catch(() => undefined);
}

// Applies the preferred side, flips to the opposite side when the preferred one
// does not fit, then clamps the cross axis. The layer has to be measured after each
// class change, so the writes and reads are interleaved on purpose. Every pass
// starts from the preferred side so a viewport change can flip back.
function place(instance) {
    const layer = instance.layer;
    if (!layer || !instance.active) {
        return;
    }

    const [preferredSide, align] = splitPlacement(instance.preferred);
    let side = preferredSide;

    applyPlacement(instance, side, align);

    if (!fitsSide(layer.getBoundingClientRect(), side)) {
        side = oppositeSide(side);
        applyPlacement(instance, side, align);
    }

    layer.style.setProperty('--aeterni-popover-shift-x', '0px');

    const shift = clampAlignedShift(layer.getBoundingClientRect(), 'x');
    if (shift !== 0) {
        layer.style.setProperty('--aeterni-popover-shift-x', `${shift}px`);
    }
}

function applyPlacement(instance, side, align) {
    const resolved = `${side}-${align}`;
    instance.layer.classList.remove(...PLACEMENTS.map(value => `aeterni-popover--${value}`));
    instance.layer.classList.add(`aeterni-popover--${resolved}`);
}

function splitPlacement(placement) {
    const [side, align] = (placement || 'bottom-start').split('-');
    return [PLACEMENTS.some(value => value.startsWith(`${side}-`)) ? side : 'bottom', align || 'start'];
}

export function dispose(key) {
    const instance = instances.get(key);
    if (!instance) {
        return;
    }

    deactivate(instance);
    instances.delete(key);
}
