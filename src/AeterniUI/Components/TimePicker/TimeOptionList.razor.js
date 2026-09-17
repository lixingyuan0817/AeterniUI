const instances = new Map();

export function init(reference, key) {
    dispose(key);
    instances.set(key, {
        reference,
        host: null,
        wheels: new Map()
    });
}

export function sync(key, host, _activeOptionId, shouldAlign, animateAlignment) {
    const instance = instances.get(key);
    if (!instance || !host) {
        return;
    }

    instance.host = host;
    const currentWheels = new Set(host.querySelectorAll('.aeterni-time-option-list__options'));

    for (const [wheel, state] of instance.wheels) {
        if (!currentWheels.has(wheel)) {
            detachWheel(wheel, state);
            instance.wheels.delete(wheel);
        }
    }

    for (const wheel of currentWheels) {
        let state = instance.wheels.get(wheel);
        if (!state) {
            state = attachWheel(instance, wheel);
            instance.wheels.set(wheel, state);
        }

        const selected = wheel.querySelector('[role="option"].is-selected');
        if (selected) {
            state.lastNotifiedValue = selectionKey(wheel, selected);
        }

        if (shouldAlign) {
            if (selected) {
                if (animateAlignment) {
                    centerOption(wheel, selected, 'smooth');
                } else {
                    positionOptionImmediately(wheel, selected);
                }
                requestAnimationFrame(() => {
                    updateWheel(wheel);
                });
            }
        } else {
            updateWheel(wheel);
        }
    }
}

export function dispose(key) {
    const instance = instances.get(key);
    if (!instance) {
        return;
    }

    for (const [wheel, state] of instance.wheels) {
        detachWheel(wheel, state);
    }

    instances.delete(key);
}

function attachWheel(instance, wheel) {
    const state = {
        frame: 0,
        settleTimer: 0,
        lastNotifiedValue: null,
        scrollHandler: null
    };

    state.scrollHandler = () => {
        cancelAnimationFrame(state.frame);
        state.frame = requestAnimationFrame(() => {
            updateWheel(wheel);
            notifyCenteredOption(instance, wheel, state);
        });
        clearTimeout(state.settleTimer);
        state.settleTimer = setTimeout(() => settleWheel(instance, wheel), 140);
    };

    wheel.addEventListener('scroll', state.scrollHandler, { passive: true });
    updateWheel(wheel);
    return state;
}

function detachWheel(wheel, state) {
    wheel.removeEventListener('scroll', state.scrollHandler);
    cancelAnimationFrame(state.frame);
    clearTimeout(state.settleTimer);
}

function settleWheel(instance, wheel) {
    const option = closestOption(wheel);
    if (!option) {
        return;
    }

    centerOption(wheel, option, 'smooth');
    updateWheel(wheel);
}

function notifyCenteredOption(instance, wheel, state) {
    const option = closestOption(wheel);
    if (!option) {
        return;
    }

    const value = Number.parseInt(option.dataset.timeValue ?? '', 10);
    const unit = wheel.dataset.timeUnit;
    const key = selectionKey(wheel, option);
    if (!unit || !Number.isInteger(value) || state.lastNotifiedValue === key) {
        return;
    }

    state.lastNotifiedValue = key;
    void instance.reference.invokeMethodAsync('OnWheelChangedAsync', unit, value).catch(() => {});
}

function updateWheel(wheel) {
    const center = wheel.getBoundingClientRect().top + (wheel.clientHeight / 2);
    for (const option of wheel.querySelectorAll('[role="option"]')) {
        const rect = option.getBoundingClientRect();
        const optionCenter = rect.top + (rect.height / 2);
        const distance = Math.min(3, Math.round(Math.abs(optionCenter - center) / Math.max(rect.height, 1)));
        option.dataset.wheelDistance = String(distance);
    }
}

function closestOption(wheel) {
    const center = wheel.getBoundingClientRect().top + (wheel.clientHeight / 2);
    let closest = null;
    let closestDistance = Number.POSITIVE_INFINITY;
    for (const option of wheel.querySelectorAll('[role="option"]')) {
        const rect = option.getBoundingClientRect();
        const distance = Math.abs((rect.top + (rect.height / 2)) - center);
        if (distance < closestDistance) {
            closest = option;
            closestDistance = distance;
        }
    }

    return closest;
}

function centerOption(wheel, option, behavior) {
    const top = option.offsetTop - ((wheel.clientHeight - option.offsetHeight) / 2);
    wheel.scrollTo({ top, behavior });
}

function positionOptionImmediately(wheel, option) {
    const top = option.offsetTop - ((wheel.clientHeight - option.offsetHeight) / 2);
    wheel.scrollTop = top;
}

function selectionKey(wheel, option) {
    return `${wheel.dataset.timeUnit}:${option.dataset.timeValue}`;
}
