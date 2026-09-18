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
        const selectedKey = selected ? selectionKey(wheel, selected) : null;
        const selectionChanged = selectedKey !== state.lastNotifiedValue;
        state.lastNotifiedValue = selectedKey;

        // Align dependent columns, but never restart the originating wheel's momentum.
        if (shouldAlign || selectionChanged) {
            if (selected) {
                state.snapping = animateAlignment;
                if (animateAlignment) {
                    centerOption(wheel, selected, 'smooth');
                } else {
                    positionOptionImmediately(wheel, selected);
                }
                cancelAnimationFrame(state.frame);
                state.frame = requestAnimationFrame(() => {
                    updateWheel(wheel);
                });
            }
        } else {
            updateWheel(wheel);
        }
    }
}

export function pendingValues(key) {
    const values = {};
    const instance = instances.get(key);
    if (!instance) {
        return values;
    }

    for (const [wheel, state] of instance.wheels) {
        if (state.settleTimer) {
            const option = closestOption(wheel);
            if (option) {
                values[wheel.dataset.timeUnit] = Number.parseInt(option.dataset.timeValue, 10);
            }
        }
    }
    return values;
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
        scrollHandler: null,
        inputHandler: null,
        resizeObserver: null,
        height: 0,
        snapping: false
    };

    state.inputHandler = () => {
        // Cancel an in-flight snap before native wheel/touch input changes direction.
        if (state.snapping) {
            wheel.scrollTo({ top: wheel.scrollTop, behavior: 'instant' });
            state.snapping = false;
        }
        clearTimeout(state.settleTimer);
        state.settleTimer = setTimeout(() => settleWheel(instance, wheel, state), 140);
    };
    state.resizeObserver = new ResizeObserver(() => {
        const height = wheel.clientHeight;
        if (height > 0 && height !== state.height) {
            const selected = wheel.querySelector('[role="option"].is-selected');
            if (selected) {
                positionOptionImmediately(wheel, selected);
                updateWheel(wheel);
            }
        }
        state.height = height;
    });
    state.resizeObserver.observe(wheel);

    state.scrollHandler = () => {
        cancelAnimationFrame(state.frame);
        state.frame = requestAnimationFrame(() => {
            updateWheel(wheel);
        });
        clearTimeout(state.settleTimer);
        state.settleTimer = setTimeout(() => settleWheel(instance, wheel, state), 140);
    };

    wheel.addEventListener('wheel', state.inputHandler, { passive: true });
    wheel.addEventListener('touchstart', state.inputHandler, { passive: true });
    wheel.addEventListener('scroll', state.scrollHandler, { passive: true });
    updateWheel(wheel);
    return state;
}

function detachWheel(wheel, state) {
    state.resizeObserver.disconnect();
    wheel.removeEventListener('wheel', state.inputHandler);
    wheel.removeEventListener('touchstart', state.inputHandler);
    wheel.removeEventListener('scroll', state.scrollHandler);
    cancelAnimationFrame(state.frame);
    clearTimeout(state.settleTimer);
}

function settleWheel(instance, wheel, state) {
    state.settleTimer = 0;
    const option = closestOption(wheel);
    if (!option) {
        return;
    }

    const top = option.offsetTop - ((wheel.clientHeight - option.offsetHeight) / 2);
    state.snapping = Math.abs(wheel.scrollTop - top) > 0.5;
    if (state.snapping) {
        centerOption(wheel, option, 'smooth');
    }
    updateWheel(wheel);
    notifyCenteredOption(instance, wheel, state);
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
    const center = wheel.scrollTop + (wheel.clientHeight / 2);
    for (const option of wheel.querySelectorAll('[role="option"]')) {
        const optionCenter = option.offsetTop + (option.offsetHeight / 2);
        const distance = Math.min(3, Math.round(Math.abs(optionCenter - center) / Math.max(option.offsetHeight, 1)));
        option.dataset.wheelDistance = String(distance);
    }
}

function closestOption(wheel) {
    if (wheel.clientHeight === 0) {
        return null;
    }
    const center = wheel.scrollTop + (wheel.clientHeight / 2);
    let closest = null;
    let closestDistance = Number.POSITIVE_INFINITY;
    for (const option of wheel.querySelectorAll('[role="option"]')) {
        const distance = Math.abs((option.offsetTop + (option.offsetHeight / 2)) - center);
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
