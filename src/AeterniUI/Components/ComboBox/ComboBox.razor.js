const instances = new Map();

export function init(reference, key) {
    dispose(key);
    instances.set(key, { reference, host: null, pointerHandler: null, scrollHandler: null, resizeHandler: null, active: false });
}

export function attach(key, host) {
    const instance = instances.get(key);
    if (!instance || !host) {
        return;
    }

    if (instance.pointerHandler) {
        document.removeEventListener('pointerdown', instance.pointerHandler);
    }

    instance.host = host;

    const pointerHandler = event => {
        if (instance.host && !instance.host.contains(event.target)) {
            instance.reference
                ?.invokeMethodAsync('OnOutsidePointerAsync')
                .catch(() => undefined);
        }
    };

    instance.pointerHandler = pointerHandler;
    document.addEventListener('pointerdown', pointerHandler);
}

// While open: closing on page scroll feels crisp (like a native select), so we
// do not try to follow the trigger while the page moves. Scrolling inside the
// popover's own option list is ignored. On resize the layer is repositioned.
export function setOpen(key, open) {
    const instance = instances.get(key);
    if (!instance) {
        return;
    }

    if (open && !instance.active) {
        instance.scrollHandler = event => {
            const popover = instance.host?.querySelector('.aeterni-combobox__popover');
            const target = event.target;

            // Scroll inside the option list (or the popover itself) keeps the
            // dropdown open — same as a native select.
            if (target && popover && (target === popover || popover.contains(target))) {
                return;
            }

            // Any other scroll (page body, an ancestor container) closes it.
            instance.reference
                ?.invokeMethodAsync('OnOutsidePointerAsync')
                .catch(() => undefined);
        };

        instance.resizeHandler = () => place(key);

        document.addEventListener('scroll', instance.scrollHandler, { capture: true, passive: true });
        window.addEventListener('resize', instance.resizeHandler);
        instance.active = true;
        place(key);
    } else if (!open && instance.active) {
        document.removeEventListener('scroll', instance.scrollHandler, { capture: true });
        window.removeEventListener('resize', instance.resizeHandler);
        instance.scrollHandler = null;
        instance.resizeHandler = null;
        instance.active = false;
    }
}

// Anchors the dropdown to the trigger with viewport coordinates so card
// containers with overflow:hidden cannot clip it. Also flips above the trigger
// when there is not enough room below, and keeps the layer inside the viewport.
export function place(key) {
    const instance = instances.get(key);
    const popover = instance?.host?.querySelector('.aeterni-combobox__popover');
    const trigger = instance?.host?.querySelector('.aeterni-combobox__trigger');
    if (!instance || !popover || !trigger) {
        return;
    }

    const rect = trigger.getBoundingClientRect();
    const gap = 6;
    const viewportMargin = 8;

    const maxLeft = window.innerWidth - rect.width - viewportMargin;
    popover.style.left = `${Math.min(rect.left, Math.max(viewportMargin, maxLeft))}px`;
    popover.style.width = `${rect.width}px`;
    popover.style.top = `${rect.bottom + gap}px`;

    popover.style.visibility = 'hidden';
    const height = popover.offsetHeight;
    popover.style.visibility = '';

    const spaceBelow = window.innerHeight - rect.bottom - viewportMargin;
    const spaceAbove = rect.top - viewportMargin;

    if (height > spaceBelow && spaceAbove > spaceBelow) {
        popover.style.top = `${Math.max(viewportMargin, rect.top - height - gap)}px`;
    }
}

export function dispose(key) {
    const instance = instances.get(key);
    if (!instance) {
        return;
    }

    if (instance.pointerHandler) {
        document.removeEventListener('pointerdown', instance.pointerHandler);
    }

    if (instance.scrollHandler) {
        document.removeEventListener('scroll', instance.scrollHandler, { capture: true });
    }

    if (instance.resizeHandler) {
        window.removeEventListener('resize', instance.resizeHandler);
    }

    instances.delete(key);
}
