/*
   AeterniUI — shared keyboard guard for menu trigger buttons.

   `MenuButton` and `IconMenuButton` open a menu from a real button. The C# side
   owns the open state and the menu's own keyboard model; this module only does
   what C# cannot: cancel the browser's default ArrowDown/ArrowUp page scroll
   before reporting the key, so opening the menu never moves the page.

   The module is shared instead of duplicated in both component modules because
   the behaviour is identical apart from the component that owns the instance.
   Every listener is released by `dispose(instanceId)`, which the component base
   calls when the component is torn down.
*/

const instances = new Map();

export function init(reference, key) {
    dispose(key);
    instances.set(key, { reference, trigger: null, handler: null });
}

export function attach(key, trigger) {
    const state = instances.get(key);
    if (!state || !trigger) {
        return;
    }

    if (state.trigger === trigger) {
        return;
    }

    state.trigger?.removeEventListener('keydown', state.handler);
    state.trigger = trigger;
    state.handler = event => {
        if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') {
            return;
        }

        // Keep the page from scrolling while the menu opens (or while moving
        // focus inside an already open menu).
        event.preventDefault();
        state.reference?.invokeMethodAsync('HandleTriggerArrowKeyAsync', event.key === 'ArrowDown')
            .catch(() => undefined);
    };
    trigger.addEventListener('keydown', state.handler);
}

export function dispose(key) {
    const state = instances.get(key);
    if (!state) {
        return;
    }

    if (state.trigger && state.handler) {
        state.trigger.removeEventListener('keydown', state.handler);
    }

    instances.delete(key);
}
