/*
   Popover module.

   Position: by default the layer is absolutely positioned inside its PopupHost by
   the placement classes, so this module only has to flip the side when the viewport
   has no room and shift the layer on the cross axis. The shared floating-layer
   helpers own that maths (see wwwroot/js/aeterni_floating.js). When the consumer
   names an anchor element the layer is placed against instead (`setAnchor`), the
   layer is lifted out of that flow — `position: fixed` and viewport coordinates —
   so the host may live anywhere, including outside the container that holds the
   trigger.

   Modal chrome: Escape, outside pointer, background scroll lock and a focus trap.
   A non-modal layer keeps focus where the consumer put it, which is what a
   dropdown anchored to a trigger expects.
*/

import {
    clampAlignedShift,
    createFocusTrap,
    fitsSide,
    lockScroll,
    oppositeSide,
    waitForExit
} from '../../js/aeterni_floating.js';

const PLACEMENTS = ['bottom-start', 'bottom-end', 'top-start', 'top-end'];
const instances = new Map();

export function init(reference, key) {
    dispose(key);
    instances.set(key, {
        reference,
        layer: null,
        anchor: null,
        modal: false,
        preferred: 'bottom-start',
        closeOnEscape: true,
        closeOnOutsideClick: true,
        closeOnScroll: false,
        restoreFocusOnClose: false,
        opener: null,
        active: false,
        trap: null,
        releaseScroll: null,
        pointerHandler: null,
        keyHandler: null,
        resizeHandler: null,
        scrollHandler: null,
        cancelExit: null,
        closingRevision: null,
        // The placement `applyPlacement` resolved for the open layer (preferred or
        // flipped) and the cross-axis shift last written to the layer.
        resolvedPlacement: null,
        exitShiftX: '0px',
        // Final resolved placement of the open layer, captured after `place()`. Blazor
        // owns the root `class` attribute, so the render that starts the closing state
        // writes the preferred placement back over the one this module resolved; the
        // closing path restores this snapshot before the exit animation is measured so
        // the layer does not jump to the other side for the duration of the fade.
        exitPlacement: null
    });
}

/**
 * Points the instance at the element the layer is positioned against. Called after
 * every render, so the reference cannot stay stale across a caller re-render; a
 * missing or detached element falls back to the host the layer is rendered in.
 */
export function setAnchor(key, element) {
    const instance = instances.get(key);
    if (!instance) {
        return;
    }

    // `Node` rather than `Element`: it matches the other type checks in this file and
    // lets the Node-side DOM stand-in provide only `Node`.
    const next = element instanceof Node ? element : null;
    if (next === instance.anchor) {
        return;
    }

    instance.anchor = next;
    if (instance.active) {
        place(instance);
    }
}

/**
 * Synchronises the instance with the rendered state. Called after every render, so
 * it has to be idempotent: only a change of `open` or of the modal flag performs
 * work.
 */
export function setOpen(key, open, layer, modal, placement, closeOnEscape, closeOnOutsideClick, restoreFocusOnClose, closeOnScroll, closing = false, revision = 0) {
    const instance = instances.get(key);
    if (!instance) {
        return;
    }

    instance.layer = layer ?? instance.layer;
    instance.preferred = PLACEMENTS.includes(placement) ? placement : instance.preferred;
    instance.closeOnEscape = !!closeOnEscape;
    instance.closeOnOutsideClick = !!closeOnOutsideClick;
    instance.closeOnScroll = !!closeOnScroll;
    instance.restoreFocusOnClose = !!restoreFocusOnClose;

    if (open || !closing) {
        instance.cancelExit?.();
        instance.cancelExit = null;
        instance.closingRevision = null;
    }

    if (open && !instance.active) {
        activate(instance, !!modal);
        return;
    }

    if (!open) {
        if (closing) {
            const started = instance.closingRevision !== revision;
            if (started) {
                instance.cancelExit?.();
                instance.closingRevision = revision;
            }

            // Blazor just re-rendered the root with its preferred placement class (and
            // without the module-owned `is-anchored`/inline coordinates). Put the
            // resolved placement back *before* measuring the exit animation, otherwise
            // the layer animates out from the other side of the anchor. A parent render
            // during the exit can reset the root again, so this repeats on every
            // closing sync, not only when the revision starts.
            restoreExitPlacement(instance);

            if (started) {
                instance.cancelExit = waitForExit([
                    instance.layer?.querySelector('.aeterni-popover__surface'),
                    instance.layer?.querySelector('.aeterni-popover__backdrop')], () => {
                    instance.cancelExit = null;
                    deactivate(instance);
                    instance.reference.invokeMethodAsync('FinalizeCloseAsync', revision).catch(() => {});
                });
            }
        }
        if (!closing && instance.active) deactivate(instance);
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
    instance.opener = instance.restoreFocusOnClose ? document.activeElement : null;
    instance.active = true;

    installOutsidePointer(instance);
    installEscape(instance);

    instance.resizeHandler = () => place(instance);
    window.addEventListener('resize', instance.resizeHandler, { passive: true });

    instance.scrollHandler = event => {
        if (instance.closeOnScroll) {
            const target = event.target;
            if (!(target instanceof Node) || !instance.layer.contains(target)) {
                requestClose(instance);
                return;
            }
        }

        place(instance);
    };
    document.addEventListener('scroll', instance.scrollHandler, { capture: true, passive: true });

    if (modal) {
        instance.releaseScroll = lockScroll();
        // Focus restore is owned by `deactivate`, not by the trap: the trap's default
        // release would unconditionally focus the opener before `deactivate` runs its
        // `restoreFocusOnClose`-gated and guarded microtask, which both ignores the
        // flag (focus returns even when `false`) and defeats the "a consumer callback
        // deliberately focused another control" guard. One restore path, one owner.
        instance.trap = createFocusTrap(instance.layer, {
            restoreFocus: false,
            onEscape: () => {
                if (instance.closeOnEscape) requestClose(instance);
            }
        });
    }

    place(instance);
}

function deactivate(instance) {
    instance.cancelExit?.();
    instance.cancelExit = null;
    instance.active = false;
    instance.exitPlacement = null;

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

    const opener = instance.opener;
    instance.opener = null;
    if (instance.restoreFocusOnClose && opener instanceof HTMLElement && opener.isConnected) {
        queueMicrotask(() => {
            const active = document.activeElement;
            // Non-modal menus must not steal focus after an outside click, Tab,
            // or a consumer callback that deliberately focused another control.
            if (instance.modal || active === opener || active === document.body || instance.layer?.contains(active)) {
                if (!opener.matches(':disabled') && !opener.closest('[hidden], [inert]')) {
                    opener.focus({ preventScroll: true });
                }
            }
        });
    }
}

// A non-modal layer closes when the pointer goes down outside it.
//
// The anchor box counts as inside: with no anchor named it is the layer's own parent
// (the PopupHost) — that is what keeps a toggle button placed in the host from both
// closing and reopening the layer on the same press — and with an anchor it is the
// element the caller anchored to, which is where such a trigger lives when the layer
// renders elsewhere.
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

        const anchor = anchorOf(instance) ?? instance.layer.parentElement ?? instance.layer;
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
    if (!instance.active || instance.closingRevision !== null) {
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

    // add/remove rather than toggle: it matches applyPlacement and needs only the two
    // basic classList operations from the Node-side DOM stand-in.
    const anchor = anchorOf(instance);
    if (anchor) {
        layer.classList.add('is-anchored');
        placeAgainstAnchor(instance, layer, anchor, side, align);
        rememberPlacement(instance, true);
        return;
    }

    layer.classList.remove('is-anchored');
    clearAnchorPosition(layer);

    if (!fitsSide(layer.getBoundingClientRect(), side)) {
        side = oppositeSide(side);
        applyPlacement(instance, side, align);
    }

    layer.style.setProperty('--aeterni-popover-shift-x', '0px');

    const shift = clampAlignedShift(layer.getBoundingClientRect(), 'x');
    instance.exitShiftX = shift === 0 ? '0px' : `${shift}px`;
    if (shift !== 0) {
        layer.style.setProperty('--aeterni-popover-shift-x', instance.exitShiftX);
    }

    rememberPlacement(instance, false);
}

// Anchored placement: the layer is `position: fixed`, so it is measured in viewport
// space and no ancestor box decides where it lands — which is also what keeps it
// pinned beside a trigger inside a `position: fixed` header while the page scrolls.
// The side is picked from the room *beside the anchor* (the layer plus the gap), so
// the fit test runs on that projection rather than on the layer's current box.
function placeAgainstAnchor(instance, layer, anchor, side, align) {
    const anchorRect = anchor.getBoundingClientRect();
    const size = layer.getBoundingClientRect();
    const gap = gapOf(layer);

    const projected = side === 'bottom'
        ? { top: anchorRect.bottom + gap, bottom: anchorRect.bottom + gap + size.height }
        : { top: anchorRect.top - gap - size.height, bottom: anchorRect.top - gap };

    if (!fitsSide(projected, side)) {
        side = oppositeSide(side);
        applyPlacement(instance, side, align);
    }

    // The layer's own margins must stay out of the calibration read below: they
    // displace the box, and the write after it puts them back, so a margin measured
    // in would come out subtracted from the final offset.
    layer.style.marginLeft = '0px';
    layer.style.marginTop = '0px';
    layer.style.left = '0px';
    layer.style.top = '0px';

    // A transformed or filtered ancestor becomes the containing block of a fixed
    // box, so the origin is measured (offset 0/0) instead of assumed to be the
    // viewport corner. No paint happens in between: both steps run in one task.
    const origin = layer.getBoundingClientRect();

    const left = align === 'end' ? anchorRect.right - size.width : anchorRect.left;
    const top = side === 'bottom' ? anchorRect.bottom + gap : anchorRect.top - gap - size.height;

    layer.style.left = `${Math.round(left - origin.left)}px`;
    layer.style.top = `${Math.round(top - origin.top)}px`;

    // Clearing the inline margins hands the offset back to CSS: the consumer's own
    // margin still nudges the layer, and --aeterni-popover-shift-x takes over the
    // cross-axis clamp exactly as it does for a host-anchored layer.
    layer.style.marginLeft = '';
    layer.style.marginTop = '';
    layer.style.setProperty('--aeterni-popover-shift-x', '0px');

    const shift = clampAlignedShift(layer.getBoundingClientRect(), 'x');
    instance.exitShiftX = shift === 0 ? '0px' : `${shift}px`;
    if (shift !== 0) {
        layer.style.setProperty('--aeterni-popover-shift-x', instance.exitShiftX);
    }
}

/**
 * Snapshots the final placement of the open layer. Blazor writes the root `class`
 * attribute from its own render state, so the render that starts the closing state
 * replaces the placement the module resolved; the closing path restores this
 * snapshot before measuring the exit animation so the layer does not jump sides for
 * the duration of the fade. Coordinates are captured too because anchored mode owns
 * inline `left`/`top`/margins that the same render can overwrite.
 */
function rememberPlacement(instance, anchored) {
    const layer = instance.layer;
    const resolved = instance.resolvedPlacement;
    if (!layer || !resolved) {
        return;
    }

    instance.exitPlacement = {
        side: resolved.side,
        align: resolved.align,
        anchored,
        left: layer.style.left ?? '',
        top: layer.style.top ?? '',
        marginLeft: layer.style.marginLeft ?? '',
        marginTop: layer.style.marginTop ?? '',
        shiftX: instance.exitShiftX
    };
}

/** Restores the resolved placement over a Blazor re-render that reset the root. */
function restoreExitPlacement(instance) {
    const state = instance.exitPlacement;
    const layer = instance.layer;
    if (!state || !layer) {
        return;
    }

    applyPlacement(instance, state.side, state.align);

    if (state.anchored) {
        layer.classList.add('is-anchored');
    } else {
        layer.classList.remove('is-anchored');
    }

    layer.style.left = state.left;
    layer.style.top = state.top;
    layer.style.marginLeft = state.marginLeft;
    layer.style.marginTop = state.marginTop;
    layer.style.setProperty('--aeterni-popover-shift-x', state.shiftX);
}

/** The anchor element, when the consumer named one that is still in the document. */
function anchorOf(instance) {
    return instance.anchor instanceof Node && instance.anchor.isConnected ? instance.anchor : null;
}

/** Drops the coordinates of a previous anchored placement. */
function clearAnchorPosition(layer) {
    layer.style.left = '';
    layer.style.top = '';
}

/** The gap the placement classes keep between anchor and layer, in pixels. */
function gapOf(layer) {
    const raw = getComputedStyle(layer).getPropertyValue('--aeterni-popover-gap').trim();
    const value = parseFloat(raw);
    if (!Number.isFinite(value)) {
        return 0;
    }

    // Custom properties come back as the substituted token stream, so a rem/em
    // spacing token has to be resolved against the actual font size.
    if (raw.endsWith('rem')) {
        return value * parseFloat(getComputedStyle(document.documentElement).fontSize);
    }

    if (raw.endsWith('em')) {
        return value * parseFloat(getComputedStyle(layer).fontSize);
    }

    return value;
}

function applyPlacement(instance, side, align) {
    const resolved = `${side}-${align}`;
    instance.resolvedPlacement = { side, align };
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
