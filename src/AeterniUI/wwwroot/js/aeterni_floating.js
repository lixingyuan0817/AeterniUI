/*
   AeterniUI — shared floating-layer helpers.

   One implementation of the browser behaviour that every anchored overlay needs:
   keeping a layer inside the viewport, locking page scroll, and trapping focus.
   Tooltip and Popover consume it today; Drawer and ComboBox are the next
   candidates (ComboBox still owns its trigger-anchored fixed positioning).

   This file lives in `wwwroot` because it is a shared foundation, not a
   component module: component modules are co-located `*.razor.js` files declared
   with [JsModule], and they import this one through a relative path.
*/

/** Distance kept between a layer and the viewport edge. */
export const VIEWPORT_MARGIN = 8;

/** Elements that participate in the Tab order inside a layer. */
export const FOCUSABLE =
    'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

const SIDE_OPPOSITE = { top: 'bottom', bottom: 'top', start: 'end', end: 'start' };

/** The opposite side of a placement's main axis (`bottom-start` -> `top-start`). */
export function oppositeSide(side) {
    return SIDE_OPPOSITE[side] ?? side;
}

/** True when the layer's box fits on the given side of the viewport. */
export function fitsSide(layerRect, side, margin = VIEWPORT_MARGIN) {
    switch (side) {
        case 'top':
            return layerRect.top >= margin;
        case 'bottom':
            return layerRect.bottom <= window.innerHeight - margin;
        case 'start':
            return layerRect.left >= margin;
        case 'end':
            return layerRect.right <= window.innerWidth - margin;
        default:
            return true;
    }
}

/**
 * Shift (px) that keeps a layer *centred on its anchor* inside the viewport —
 * the Tooltip case. Positive values move the layer towards the inline end.
 */
export function clampCenteredShift(anchorRect, layerRect, axis, margin = VIEWPORT_MARGIN) {
    const centered = axis === 'x';
    const viewportSize = centered ? window.innerWidth : window.innerHeight;
    const anchorCenter = centered
        ? anchorRect.left + anchorRect.width / 2
        : anchorRect.top + anchorRect.height / 2;
    const half = (centered ? layerRect.width : layerRect.height) / 2;
    const minimum = margin + half - anchorCenter;
    const maximum = viewportSize - margin - half - anchorCenter;
    return minimum > maximum ? minimum : Math.min(Math.max(0, minimum), maximum);
}

/**
 * Shift (px) that keeps a layer *aligned to its anchor's edge* inside the
 * viewport — the Popover case, where the layer is anchored to a container rather
 * than centred on a trigger.
 */
export function clampAlignedShift(layerRect, axis, margin = VIEWPORT_MARGIN) {
    const horizontal = axis === 'x';
    const viewportSize = horizontal ? window.innerWidth : window.innerHeight;
    const start = horizontal ? layerRect.left : layerRect.top;
    const end = horizontal ? layerRect.right : layerRect.bottom;

    if (start < margin) {
        return margin - start;
    }

    if (end > viewportSize - margin) {
        return viewportSize - margin - end;
    }

    return 0;
}

/** Tab-order candidates inside a layer. */
export function focusableWithin(container) {
    return container ? [...container.querySelectorAll(FOCUSABLE)] : [];
}

/** Focuses the first candidate, falling back to the container itself. */
export function focusFirst(container) {
    const target = focusableWithin(container)[0] ?? container;
    target?.focus?.();
    return target;
}

let scrollLockCount = 0;
let scrollLockState = null;

/**
 * Locks page scroll while keeping the background from reflowing by the scrollbar
 * width. Reference counted, so two overlapping overlays cannot unlock each other.
 * Returns an idempotent release function.
 */
export function lockScroll() {
    if (scrollLockCount === 0) {
        const body = document.body;
        const scrollbarWidth = window.innerWidth - document.documentElement.clientWidth;
        scrollLockState = {
            overflow: body.style.overflow,
            paddingRight: body.style.paddingRight
        };

        if (scrollbarWidth > 0) {
            body.style.paddingRight = `${scrollbarWidth}px`;
        }

        body.style.overflow = 'hidden';
    }

    scrollLockCount += 1;

    let released = false;
    return () => {
        if (released) {
            return;
        }

        released = true;
        scrollLockCount = Math.max(0, scrollLockCount - 1);

        if (scrollLockCount === 0 && scrollLockState) {
            document.body.style.overflow = scrollLockState.overflow;
            document.body.style.paddingRight = scrollLockState.paddingRight;
            scrollLockState = null;
        }
    };
}

/**
 * Keeps Tab inside `container`, reports Escape, and restores focus on release.
 * Returns `{ release, contains }`; `release` is idempotent.
 */
export function createFocusTrap(container, options = {}) {
    const { onEscape = null, initialFocus = true, restoreFocus = true } = options;
    const previous = document.activeElement;

    if (initialFocus) {
        focusFirst(container);
    }

    const onKeyDown = event => {
        if (event.key === 'Escape') {
            if (typeof onEscape === 'function') {
                event.stopPropagation();
                onEscape();
            }

            return;
        }

        if (event.key !== 'Tab') {
            return;
        }

        const items = focusableWithin(container);
        if (items.length === 0) {
            event.preventDefault();
            container.focus?.();
            return;
        }

        const first = items[0];
        const last = items[items.length - 1];

        if (!container.contains(document.activeElement)) {
            event.preventDefault();
            first.focus();
        } else if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    };

    document.addEventListener('keydown', onKeyDown, true);

    let released = false;
    return {
        contains: node => container.contains(node),
        release() {
            if (released) {
                return;
            }

            released = true;
            document.removeEventListener('keydown', onKeyDown, true);

            if (restoreFocus && previous?.isConnected) {
                previous.focus?.();
            }
        }
    };
}
