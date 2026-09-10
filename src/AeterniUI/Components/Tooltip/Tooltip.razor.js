// Tooltip layer: links the tooltip text to the focusable trigger content for
// assistive technology and keeps the layer inside the viewport by flipping to the
// opposite side and shifting along the cross axis. Visibility itself stays
// CSS-driven (:hover / :focus-within), so the hint still shows without JS.
//
// The viewport maths lives in the shared floating-layer helpers, together with the
// Popover's focus trap and scroll lock.

import { clampCenteredShift, fitsSide, oppositeSide } from '../../js/aeterni_floating.js';

const instances = new Map();
const PLACEMENTS = ['top', 'bottom', 'start', 'end'];
const FOCUSABLE = 'a[href], button, input, select, textarea, [tabindex]:not([tabindex="-1"])';
let resizeHandler = null;

export function attach(instanceId, root, contentId, placement) {
    detach(instanceId);

    const content = root?.querySelector('.aeterni-tooltip__content');
    const trigger = root?.querySelector('.aeterni-tooltip__trigger');
    if (!instanceId || !root || !content || !trigger) {
        return;
    }

    const entry = {
        root,
        content,
        trigger,
        placement: PLACEMENTS.includes(placement) ? placement : 'top',
        preferred: PLACEMENTS.includes(placement) ? placement : 'top',
        describedByTarget: null,
        previousDescribedBy: null
    };

    instances.set(instanceId, entry);

    if (contentId) {
        linkDescription(entry, contentId);
    }

    entry.recompute = () => applyPlacement(entry);
    root.addEventListener('mouseenter', entry.recompute);
    root.addEventListener('focusin', entry.recompute);

    if (!resizeHandler) {
        resizeHandler = () => {
            for (const value of instances.values()) {
                applyPlacement(value);
            }
        };
        window.addEventListener('resize', resizeHandler, { passive: true });
    }

    applyPlacement(entry);
}

export function dispose(instanceId) {
    const entry = instances.get(instanceId);
    if (!entry) {
        return;
    }

    unlinkDescription(entry);
    entry.root.removeEventListener('mouseenter', entry.recompute);
    entry.root.removeEventListener('focusin', entry.recompute);
    instances.delete(instanceId);

    if (instances.size === 0 && resizeHandler) {
        window.removeEventListener('resize', resizeHandler);
        resizeHandler = null;
    }
}

function detach(instanceId) {
    if (instanceId && instances.has(instanceId)) {
        dispose(instanceId);
    }
}

// The tooltip text describes the control the user actually interacts with. The
// trigger wrapper is only used when the content exposes nothing focusable.
function linkDescription(entry, contentId) {
    const target = entry.trigger.querySelector(FOCUSABLE) ?? entry.trigger;
    entry.describedByTarget = target;
    entry.previousDescribedBy = target.getAttribute('aria-describedby');

    const ids = [entry.previousDescribedBy, contentId].filter(value => !!value && value.trim().length > 0);
    target.setAttribute('aria-describedby', ids.join(' '));
}

function unlinkDescription(entry) {
    const target = entry.describedByTarget;
    if (!target) {
        return;
    }

    if (entry.previousDescribedBy) {
        target.setAttribute('aria-describedby', entry.previousDescribedBy);
    } else {
        target.removeAttribute('aria-describedby');
    }

    entry.describedByTarget = null;
}

function applyPlacement(entry) {
    // Always start from the preferred side so a viewport change can flip back.
    entry.placement = entry.preferred;

    const horizontal = entry.placement === 'top' || entry.placement === 'bottom';
    const triggerRect = entry.trigger.getBoundingClientRect();
    if (triggerRect.width === 0 && triggerRect.height === 0) {
        return;
    }

    setPlacement(entry, entry.preferred);

    entry.content.style.setProperty('--aeterni-tooltip-shift-x', '0px');
    entry.content.style.setProperty('--aeterni-tooltip-shift-y', '0px');

    // Flip to the opposite side when the preferred side does not fit.
    if (!fitsSide(entry.content.getBoundingClientRect(), entry.placement)) {
        const opposite = oppositeSide(entry.placement);
        setPlacement(entry, opposite);
        entry.placement = opposite;
    }

    const shift = clampCenteredShift(triggerRect, entry.content.getBoundingClientRect(), horizontal ? 'x' : 'y');
    entry.content.style.setProperty(horizontal ? '--aeterni-tooltip-shift-x' : '--aeterni-tooltip-shift-y', `${shift}px`);
}

function setPlacement(entry, placement) {
    entry.root.classList.remove(...PLACEMENTS.map(value => `aeterni-tooltip--${value}`));
    entry.root.classList.add(`aeterni-tooltip--${placement}`);
}
