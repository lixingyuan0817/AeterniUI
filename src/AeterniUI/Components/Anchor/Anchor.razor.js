// Section navigation module.
//
// The Anchor only renders the link list; the browser has to answer two questions
// the DOM alone cannot:
//
//   1. which section is the reader looking at right now? The sections are observed
//      (IntersectionObserver with the same root as the scroll container) and their
//      positions are also compared against the container top, so the current item
//      is derived from the real scroll offset instead of trusting intersection
//      ratios alone. The result is reported back to C# through the DotNet
//      reference; `aria-current` and `is-active` stay C#-rendered.
//   2. where should a click scroll? The target lives in the document or in a
//      host-provided scroll container, and it has to land below the sticky-header
//      offset. The module scrolls that container and, when enabled, only replaces
//      the URL fragment (never routing).
//
// Everything here is an enhancement: the markup is a plain <nav> of href="#id"
// links, so without JavaScript the browser's native anchor behaviour still works.

const instances = new Map();

// Fractional scroll offsets and a section exactly on the offset line can round
// either way; a few pixels of tolerance keep the marker from flickering at the
// boundary.
const TOLERANCE = 8;

export function init(reference, key) {
    dispose(key);
    instances.set(key, {
        reference,
        host: null,
        options: { ids: [], container: null, offset: 0, smooth: true, updateLocation: true },
        container: null,
        observer: null,
        observerRoot: undefined,
        observed: new Map(),
        activeId: null,
        onclick: null,
        onscroll: null,
        scrollTarget: null
    });
}

/** Binds the module to the rendered root. Re-attaching the same root is idempotent. */
export function attach(key, host, options) {
    const state = instances.get(key);
    if (!state || !host) {
        return;
    }

    state.options = normalizeOptions(options);

    if (state.host === host) {
        // Every component render re-attaches: refresh the derived state and the
        // observed sections, but keep the listeners and the observer in place so
        // they never accumulate.
        state.container = resolveContainer(state.options.container);
        bindScrollTarget(state);
        observeSections(state);
        updateActive(state);
        return;
    }

    teardown(state);

    state.host = host;
    state.container = resolveContainer(state.options.container);
    state.activeId = null;

    state.onclick = event => handleClick(state, event);
    host.addEventListener('click', state.onclick);

    bindScrollTarget(state);
    observeSections(state);
    updateActive(state);
}

export function dispose(key) {
    const state = instances.get(key);
    if (!state) {
        return;
    }

    teardown(state);
    instances.delete(key);
}

function teardown(state) {
    state.observer?.disconnect();
    state.observer = null;
    state.observerRoot = undefined;
    state.observed.clear();

    state.host?.removeEventListener('click', state.onclick);
    state.scrollTarget?.removeEventListener('scroll', state.onscroll);

    state.host = null;
    state.scrollTarget = null;
    state.onclick = null;
    state.onscroll = null;
}

function normalizeOptions(options) {
    const source = options ?? {};
    return {
        ids: Array.isArray(source.ids) ? source.ids.filter(id => typeof id === 'string' && id) : [],
        container: typeof source.container === 'string' && source.container.trim() ? source.container.trim() : null,
        offset: Number.isFinite(source.offset) ? Math.max(0, source.offset) : 0,
        smooth: source.smooth !== false,
        updateLocation: source.updateLocation !== false
    };
}

/** An unknown container id degrades to the document scroll position. */
function resolveContainer(id) {
    return id ? document.getElementById(id) : null;
}

function bindScrollTarget(state) {
    const target = state.container ?? (typeof window === 'undefined' ? null : window);
    if (!target || state.scrollTarget === target) {
        return;
    }

    state.scrollTarget?.removeEventListener('scroll', state.onscroll);
    state.onscroll = state.onscroll ?? (() => updateActive(state));
    state.scrollTarget = target;
    target.addEventListener('scroll', state.onscroll, { passive: true });
}

/**
 * The observer is only a trigger: section-level comparisons are made from the real
 * scroll offset in `computeActiveId`, which also covers browsers without
 * IntersectionObserver and sections inserted after the first attach.
 */
function observeSections(state) {
    if (state.observerRoot !== state.container) {
        state.observer?.disconnect();
        state.observer = null;
        state.observerRoot = state.container;
        state.observed.clear();

        if (typeof IntersectionObserver === 'function') {
            state.observer = new IntersectionObserver(
                entries => {
                    if (entries.length > 0) {
                        updateActive(state);
                    }
                },
                {
                    root: state.container ?? null,
                    rootMargin: `-${state.options.offset + TOLERANCE}px 0px 0px 0px`,
                    threshold: 0
                });
        }
    }

    const observer = state.observer;
    const seen = new Set();

    for (const id of state.options.ids) {
        const section = document.getElementById(id);
        if (!section) {
            continue;
        }

        seen.add(id);
        if (state.observed.get(id) === section) {
            continue;
        }

        const previous = state.observed.get(id);
        if (previous) {
            observer?.unobserve(previous);
        }

        state.observed.set(id, section);
        observer?.observe(section);
    }

    for (const [id, section] of [...state.observed]) {
        if (seen.has(id)) {
            continue;
        }

        observer?.unobserve(section);
        state.observed.delete(id);
    }
}

/** Reports the current section id; unchanged ids are not sent twice. */
function updateActive(state) {
    const id = computeActiveId(state);
    if (id === state.activeId) {
        return;
    }

    state.activeId = id;
    report(state, id);
}

/**
 * Last section whose top has reached the offset line. Reaching the bottom of the
 * scroll range activates the last observable section, because a section shorter
 * than the viewport can never cross that line.
 */
function computeActiveId(state) {
    const { ids, offset } = state.options;
    const container = state.container;
    const containerTop = container ? container.getBoundingClientRect().top : 0;
    const threshold = containerTop + offset + TOLERANCE;

    let active = null;
    let lastObservable = null;

    for (const id of ids) {
        const section = document.getElementById(id);
        if (!section) {
            continue;
        }

        lastObservable = id;
        if (section.getBoundingClientRect().top <= threshold) {
            active = id;
        }
    }

    if (lastObservable && isAtBottom(container)) {
        // At the end of the scroll range the last section is what the reader is
        // looking at, even when its top never crosses the offset line (a section
        // shorter than the viewport) and an earlier section still does.
        return lastObservable;
    }

    return active;
}

function isAtBottom(container) {
    if (container) {
        const range = container.scrollHeight - container.clientHeight;
        return range > 0 && container.scrollTop >= range - 1;
    }

    const root = document.documentElement;
    const viewportWindow = typeof window === 'undefined' ? null : window;
    const viewport = viewportWindow?.innerHeight ?? root?.clientHeight ?? 0;
    const scrollTop = viewportWindow?.scrollY ?? root?.scrollTop ?? 0;
    const range = (root?.scrollHeight ?? 0) - viewport;
    return range > 0 && scrollTop >= range - 1;
}

function report(state, id) {
    const reference = state.reference;
    if (!reference || typeof reference.invokeMethodAsync !== 'function') {
        return;
    }

    try {
        // The component may already be disposed; a rejected callback is expected
        // then and must not surface as an unhandled promise rejection.
        const pending = reference.invokeMethodAsync('OnActiveSectionChangedAsync', id);
        if (pending && typeof pending.catch === 'function') {
            pending.catch(() => { });
        }
    } catch {
        // A disconnected circuit only costs the component its active marker.
    }
}

function handleClick(state, event) {
    if (event.defaultPrevented) {
        return;
    }

    // Modified clicks keep the browser's own behaviour (new tab / window / download).
    if ((event.button ?? 0) !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
        return;
    }

    const link = event.target?.closest?.('a[href]');
    if (!link || (state.host && !state.host.contains(link))) {
        return;
    }

    const href = link.getAttribute('href') ?? '';
    if (!href.startsWith('#')) {
        return;
    }

    const rawId = href.slice(1);
    let id = rawId;
    try {
        id = decodeURIComponent(rawId);
    } catch {
        // A malformed escape sequence is not a reason to break the click handler.
        id = rawId;
    }

    const section = id ? document.getElementById(id) : null;
    if (!section) {
        // The host may not render this section (or the JS state is stale); the
        // native anchor remains a truthful fallback.
        return;
    }

    event.preventDefault();

    scrollToSection(state, section);

    if (state.options.updateLocation) {
        replaceFragment(id);
    }
}

/**
 * Replaces only the fragment. The path and query are kept on purpose: hosts (the
 * sample included) commonly set `<base href="/">`, and a bare `#id` passed to
 * `replaceState` resolves against that base URL, which would rewrite the address
 * to the site root instead of the current page.
 */
function replaceFragment(id) {
    const view = typeof window === 'undefined' ? null : window;
    const location = view?.location;
    if (!view || !location || !view.history) {
        return;
    }

    const target = `${location.pathname ?? ''}${location.search ?? ''}#${id}`;

    try {
        // No history entry and no Blazor route change: this is not navigation.
        view.history.replaceState(null, '', target);
    } catch {
        // Some embedded documents block fragment updates; scrolling still worked.
    }
}

function scrollToSection(state, section) {
    const behavior = state.options.smooth && !prefersReducedMotion() ? 'smooth' : 'instant';
    const offset = state.options.offset;
    const container = state.container;

    if (container) {
        const top = section.getBoundingClientRect().top
            - container.getBoundingClientRect().top
            + container.scrollTop
            - offset;

        if (typeof container.scrollTo === 'function') {
            container.scrollTo({ top: Math.max(0, top), behavior });
        } else {
            container.scrollTop = Math.max(0, top);
        }

        return;
    }

    const viewportWindow = typeof window === 'undefined' ? null : window;
    const top = section.getBoundingClientRect().top + (viewportWindow?.scrollY ?? 0) - offset;
    if (typeof viewportWindow?.scrollTo === 'function') {
        viewportWindow.scrollTo({ top: Math.max(0, top), behavior });
    }
}

function prefersReducedMotion() {
    return typeof window !== 'undefined'
        && typeof window.matchMedia === 'function'
        && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}
