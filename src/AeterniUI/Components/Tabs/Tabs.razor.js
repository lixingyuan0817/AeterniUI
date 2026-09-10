/*
   Tabs module.

   The strip is a roving-tabindex widget: selection lives in C#, and moving focus to
   the tab the keyboard just activated needs the DOM. That is the only browser
   behaviour here (plus reporting the writing direction so arrow keys follow the
   visual order in RTL), which keeps the component usable without JS.
*/

const instances = new Map();

export function init(_reference, key) {
    dispose(key);
    instances.set(key, { host: null });
}

/** Wires the module to the component root so focus and direction can be resolved. */
export function attach(key, host) {
    const instance = instances.get(key);
    if (!instance || !host) {
        return;
    }

    instance.host = host;
}

/**
 * Moves DOM focus to the tab button at the given registration index. `focus()`
 * also brings the button into view inside the horizontally scrollable strip.
 */
export function focusTab(key, index) {
    const host = instances.get(key)?.host;
    if (!host) {
        return;
    }

    const buttons = host.querySelectorAll('[role="tab"]');
    const button = index >= 0 && index < buttons.length ? buttons[index] : null;
    button?.focus?.();
}

/** True when the component renders right-to-left. */
export function isRtl(key) {
    const host = instances.get(key)?.host;
    return host ? getComputedStyle(host).direction === 'rtl' : false;
}

export function dispose(key) {
    instances.delete(key);
}
