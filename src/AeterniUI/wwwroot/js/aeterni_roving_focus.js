/*
   AeterniUI — shared roving-tabindex behaviour.

   One implementation of "a container of buttons shares a single Tab stop and
   the arrow keys move between them", which ToggleGroup and Toolbar both need.
   The two modules were byte-identical apart from the item selector, how an
   owned item is recognised, and which attribute carries the orientation, so the
   mechanism lives here and each component passes those three facts in.

   This file lives in `wwwroot` because it is a shared foundation, not a
   component module: component modules are co-located `*.razor.js` files declared
   with [JsModule], and they import this one through a relative path.
*/

/** The popup surface a button inside a toolbar/toggle-group can open. Buttons
 * that live inside one are not part of the host's arrow-key sequence. */
const POPUP_SELECTOR = '[role="menu"], [role="dialog"], .aeterni-popover';

/**
 * Creates the roving-focus module for one component family.
 *
 * @param {object} options
 * @param {string} options.selector      CSS selector for the items in the ring.
 * @param {string} options.hostSelector  Selector the item's nearest ancestor must
 *                                       match for the item to belong to this host.
 * @param {string} options.orientationAttribute  Attribute holding `vertical`.
 */
export function createRovingFocus({ selector, hostSelector, orientationAttribute }) {
    const instances = new Map();

    function owned(state, button) {
        const popup = button.closest(POPUP_SELECTOR);
        return button.closest(hostSelector) === state.host && (!popup || !state.host.contains(popup));
    }

    function visible(element) {
        return !element.closest('[hidden], [inert]') && element.getClientRects().length > 0 &&
            !['hidden', 'collapse'].includes(getComputedStyle(element).visibility);
    }

    function available(state, button) {
        return visible(state.host) && visible(button) && !button.matches(':disabled') &&
            !button.closest('[aria-disabled="true"], [aria-busy="true"]');
    }

    function setTab(element, value) {
        if (element.getAttribute('tabindex') !== value) element.setAttribute('tabindex', value);
    }

    function restore(element, value) {
        if (value === null) element.removeAttribute('tabindex');
        else setTab(element, value);
    }

    function refresh(state) {
        const { host, saved } = state;
        for (const record of state.observer.takeRecords()) {
            if (record.attributeName === 'tabindex' && saved.has(record.target)) {
                saved.set(record.target, record.target.getAttribute('tabindex'));
            }
        }
        // Disconnect while writing so our roving tabindex is never mistaken for a host update.
        state.observer.disconnect();
        const buttons = [...host.querySelectorAll(selector)].filter(button => owned(state, button));
        for (const [button, value] of saved) {
            if (!buttons.includes(button)) {
                restore(button, value);
                saved.delete(button);
            }
        }
        for (const button of buttons) {
            if (!saved.has(button)) saved.set(button, button.getAttribute('tabindex'));
        }
        const enabled = buttons.filter(button => available(state, button));
        const previous = state.current;
        if (!enabled.includes(state.current)) {
            state.current = enabled.includes(state.remembered) ? state.remembered : enabled[0] ?? null;
        }
        if (state.current) state.remembered = state.current;
        for (const button of buttons) setTab(button, button === state.current ? '0' : '-1');
        const fallback = !state.current && visible(host) && host.getAttribute('aria-disabled') !== 'true';
        setTab(host, fallback ? '0' : '-1');
        const active = host.ownerDocument.activeElement;
        // Repair focus only when an owned focused item became unavailable, never from a menu or elsewhere.
        if (state.focused && !enabled.includes(state.focused) &&
            (active === state.focused || active === host.ownerDocument.body)) {
            state.focused = null;
            if (state.current) state.current.focus();
            else if (fallback) host.focus();
            else if (active === previous) active.blur();
        } else if (active === host && state.current) {
            state.current.focus();
        }
        state.observer.observe(host, { subtree: true, childList: true, attributes: true,
            attributeFilter: ['disabled', 'aria-disabled', 'aria-busy', 'hidden', 'inert', 'class', 'style', 'tabindex', 'role', orientationAttribute] });
        return enabled;
    }

    function detach(state) {
        state.observer?.disconnect();
        state.resize?.disconnect();
        if (state.host) {
            state.host.removeEventListener('keydown', state.keydown, true);
            state.host.removeEventListener('focusin', state.focusin);
            state.host.removeEventListener('focusout', state.focusout);
            state.host.ownerDocument.removeEventListener('pointerdown', state.pointerdown, true);
            restore(state.host, state.rootTab);
        }
        for (const [button, value] of state.saved) restore(button, value);
        state.saved.clear();
        state.current = null;
        state.remembered = null;
        state.focused = null;
    }

    // Plain local functions, not methods on the returned object: the component
    // modules re-export them by name, so `this` would be undefined at the call
    // site Blazor uses.
    function init(_reference, key) {
        dispose(key);
        instances.set(key, { host: null, saved: new Map(), current: null, remembered: null, focused: null });
    }

    function sync(key, host) {
            const state = instances.get(key);
            if (!state || !host) return;
            if (state.host !== host) {
                detach(state);
                state.host = host;
                state.rootTab = host.getAttribute('tabindex');
                state.observer = new MutationObserver(records => {
                    for (const record of records) {
                        if (record.attributeName === 'tabindex' && state.saved.has(record.target)) {
                            state.saved.set(record.target, record.target.getAttribute('tabindex'));
                        }
                    }
                    refresh(state);
                });
                state.keydown = event => {
                    if (event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;
                    const button = event.target.closest(selector);
                    if (!button || !owned(state, button)) return;
                    const vertical = host.getAttribute(orientationAttribute) === 'vertical';
                    const rtl = getComputedStyle(host).direction === 'rtl';
                    let delta = 0;
                    if (vertical) delta = event.key === 'ArrowDown' ? 1 : event.key === 'ArrowUp' ? -1 : 0;
                    else delta = event.key === 'ArrowRight' ? (rtl ? -1 : 1) : event.key === 'ArrowLeft' ? (rtl ? 1 : -1) : 0;
                    if (!delta && event.key !== 'Home' && event.key !== 'End') return;
                    const enabled = refresh(state);
                    if (!enabled.includes(button)) return;
                    event.preventDefault();
                    event.stopPropagation();
                    const index = event.key === 'Home' ? 0 : event.key === 'End' ? enabled.length - 1 :
                        (enabled.indexOf(button) + delta + enabled.length) % enabled.length;
                    state.current = enabled[index];
                    refresh(state);
                    state.current.focus();
                };
                state.focusin = event => {
                    const button = event.target.closest(selector);
                    state.focused = button && owned(state, button) ? button : null;
                    if (state.focused && available(state, button)) {
                        state.current = button;
                        refresh(state);
                    }
                };
                state.pointerdown = event => {
                    if (!host.contains(event.target)) state.focused = null;
                };
                state.focusout = event => {
                    if (event.relatedTarget && !host.contains(event.relatedTarget)) state.focused = null;
                };
                host.addEventListener('keydown', state.keydown, true);
                host.addEventListener('focusin', state.focusin);
                host.addEventListener('focusout', state.focusout);
                host.ownerDocument.addEventListener('pointerdown', state.pointerdown, true);
                state.resize = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(() => refresh(state));
                state.resize?.observe(host);
            }
        refresh(state);
    }

    function dispose(key) {
        const state = instances.get(key);
        if (!state) return;
        detach(state);
        instances.delete(key);
    }

    return { init, sync, dispose };
}
