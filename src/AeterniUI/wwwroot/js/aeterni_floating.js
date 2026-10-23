/*
   AeterniUI — shared floating-layer helpers.

   One implementation of the browser behaviour that every anchored overlay needs:
   keeping a layer inside the viewport, locking page scroll, and trapping focus.
   Tooltip, Popover, Drawer and ComboBox consume it today.

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

/** Wait for the actual CSS exit animations, including theme/reduced-motion overrides.
 * Returns a cancellation function; reopening/disposal must cancel stale completion.
 * Only chrome elements are passed, never arbitrary animated child content.
 *
 * `Animation.finished` is not reliable when the animation already reached its end
 * before the promise is observed: the short 80ms popover exit can finish during
 * the Blazor interop round-trip, and some engines then never settle the promise,
 * which left the layer stuck in `is-closing` forever. A measured fallback timer
 * plus an immediate check for animations that are already finished keeps the
 * close path working in every case (this is the JS-failure fallback the component
 * rules require). */
export function waitForExit(elements, complete) {
    let cancelled = false;
    let completed = false;
    let timer = null;

    const animations = elements.filter(Boolean).flatMap(element => element.getAnimations?.() ?? [])
        .filter(animation => /-(exit|fade-out)(-|$)/.test(animation.animationName ?? ''));

    const completeOnce = () => {
        if (completed || cancelled) return;
        completed = true;
        if (timer !== null) clearTimeout(timer);
        complete();
    };

    if (animations.length === 0) {
        queueMicrotask(completeOnce);
        return () => { cancelled = true; if (timer !== null) clearTimeout(timer); };
    }

    // Longest measured exit timing: the fallback must outlive the animation even
    // when `finished` stays pending, but not by a fixed hand-tuned delay.
    const longest = Math.max(...animations.map(animation => {
        const timing = animation.effect?.getTiming?.();
        const duration = Number(timing?.duration) || 0;
        const delay = Number(timing?.delay) || 0;
        const endDelay = Number(timing?.endDelay) || 0;
        return Math.max(0, delay + duration + endDelay);
    }));
    timer = setTimeout(completeOnce, longest + 80);

    for (const animation of animations) {
        if (animation.playState === 'finished') {
            completeOnce();
            break;
        }
        // A rejected promise (cancelled animation) also settles the wait: the
        // chrome is going away either way.
        animation.finished.then(completeOnce, completeOnce);
    }

    return () => { cancelled = true; if (timer !== null) clearTimeout(timer); };
}

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
 * Inline custom property written on `<html>` while the scroll lock reserves a
 * gutter. Full-viewport overlays extend their inline-end edge by this value
 * (`inset-inline-end: calc(-1 * var(--aeterni-scroll-lock-gutter, 0px))`): fixed
 * layers are laid out in the reduced viewport, so without the extension the
 * reserved strip stays unpainted and a centred panel sits half a scrollbar off
 * the window centre. The value is 0 on overlay-scrollbar engines, where nothing
 * is reserved and the extension is a no-op.
 */
const SCROLL_LOCK_GUTTER = '--aeterni-scroll-lock-gutter';
const OVERLAY_TOKEN = '--aeterni-overlay';

function rgbaOf(value) {
    const match = /rgba?\(([^)]+)\)/.exec(String(value ?? ''));
    if (!match) {
        return null;
    }

    const parts = match[1].split(',').map(part => Number.parseFloat(part));
    if (parts.length < 3 || parts.slice(0, 3).some(part => Number.isNaN(part))) {
        return null;
    }

    return { r: parts[0], g: parts[1], b: parts[2], a: parts.length > 3 ? parts[3] : 1 };
}

/**
 * The reserved gutter is painted by the root element's background colour only:
 * fixed layers are clipped to the layout viewport, so extending them (which the
 * components still do for geometry) cannot paint the strip. Return the scrim's
 * overlay flattened over the page background so the canvas can paint the same
 * colour, or `null` when that would be wrong:
 *
 * - a transparent body would let the dimmed canvas show through the page content
 *   as a second dim layer, so only opaque page backgrounds qualify;
 * - without `getComputedStyle` (Node stubs) there is nothing to measure.
 */
function gutterPaintColor(root) {
    if (typeof getComputedStyle !== 'function') {
        return null;
    }

    const body = rgbaOf(getComputedStyle(document.body).backgroundColor);
    if (!body || body.a < 0.999) {
        return null;
    }

    // The gutter is painted from the canvas: the root's own colour when it is
    // opaque, otherwise the body colour that propagates to the canvas.
    const rootColor = rgbaOf(getComputedStyle(root).backgroundColor);
    const page = rootColor && rootColor.a >= 0.999 ? rootColor : body;

    const overlay = rgbaOf(getComputedStyle(root).getPropertyValue(OVERLAY_TOKEN));
    if (!overlay) {
        return null;
    }

    const mix = channel => Math.round(overlay[channel] * overlay.a + page[channel] * (1 - overlay.a));
    return `rgb(${mix('r')}, ${mix('g')}, ${mix('b')})`;
}

/**
 * Locks page scroll while keeping the background from reflowing by the scrollbar
 * width. Reference counted, so two overlapping overlays cannot unlock each other.
 * Returns an idempotent release function.
 *
 * The width is kept by reserving the scrollbar track with `scrollbar-gutter`,
 * not by padding the body. Padding only compensated in-flow content: fixed
 * chrome (a sticky header, a fixed toolbar) still measured against the wider
 * viewport and jumped by the scrollbar width when the track disappeared, then
 * jumped back on unlock. A reserved gutter keeps the layout viewport the same
 * width the whole time, so nothing moves. Browsers without `scrollbar-gutter`
 * keep the padding compensation.
 */
export function lockScroll() {
    if (scrollLockCount === 0) {
        const body = document.body;
        // DOM stubs used by the Node tests only model `document.body`; keep the
        // gutter path optional so the module stays loadable without a document
        // element, and fall back to the body padding compensation there.
        const root = document.documentElement ?? null;
        const rootStyle = root?.style ?? null;
        const scrollbarWidth = root ? window.innerWidth - root.clientWidth : 0;
        const reservesGutter = typeof CSS !== 'undefined'
            && typeof CSS.supports === 'function'
            && CSS.supports('scrollbar-gutter', 'stable');
        scrollLockState = {
            overflow: body.style.overflow,
            paddingRight: body.style.paddingRight,
            scrollbarGutter: rootStyle ? rootStyle.scrollbarGutter : null,
            gutter: rootStyle?.getPropertyValue?.(SCROLL_LOCK_GUTTER) ?? '',
            pageBackground: rootStyle ? (rootStyle.backgroundColor ?? '') : null
        };

        if (reservesGutter) {
            // Only lock pages that actually scroll: reserving a gutter on a short
            // page would introduce a stripe the page never had.
            if (rootStyle && root.scrollHeight > root.clientHeight) {
                rootStyle.scrollbarGutter = 'stable';
                // Measured before the lock hid the track, so it is the width the
                // full-viewport overlays have to extend by.
                if (scrollbarWidth > 0) {
                    rootStyle.setProperty?.(SCROLL_LOCK_GUTTER, `${scrollbarWidth}px`);
                    // Paint the strip the fixed layers cannot reach (see helper).
                    const paint = gutterPaintColor(root);
                    if (paint) {
                        rootStyle.backgroundColor = paint;
                    }
                }
            }
        } else if (scrollbarWidth > 0) {
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
            // Restore the scroller first so the track comes back before the gutter
            // reservation is dropped; the other order would expand the viewport for
            // one frame.
            document.body.style.overflow = scrollLockState.overflow;
            document.body.style.paddingRight = scrollLockState.paddingRight;
            const rootStyle = document.documentElement?.style;
            if (rootStyle && scrollLockState.scrollbarGutter !== null) {
                rootStyle.scrollbarGutter = scrollLockState.scrollbarGutter;
            }

            // Leave a host-provided value in place instead of clearing it.
            if (rootStyle) {
                if (scrollLockState.gutter) {
                    rootStyle.setProperty?.(SCROLL_LOCK_GUTTER, scrollLockState.gutter);
                } else {
                    rootStyle.removeProperty?.(SCROLL_LOCK_GUTTER);
                }

                rootStyle.backgroundColor = scrollLockState.pageBackground;
            }

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
