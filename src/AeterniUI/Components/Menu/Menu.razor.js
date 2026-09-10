// Focus helper for the Menu keyboard model. Every actionable node stays a real
// <button> in the tab order, so this module only mirrors the roving focus that
// Blazor cannot express for a data-driven list without one component per node.

export function focus(nodeId) {
    const node = document.getElementById(nodeId);
    node?.focus();
}

// Stateless module: no listeners, observers or timers are registered. The
// component contract still expects an instance-level dispose export.
export function dispose() {
}
