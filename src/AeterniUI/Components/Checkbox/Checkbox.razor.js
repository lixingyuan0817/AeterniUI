// The `indeterminate` state is a DOM property, not an HTML attribute, so it
// cannot be set from Blazor markup. This minimal module only pushes that state
// onto the checkbox input. All business state remains in C#.

export function setIndeterminate(element, value) {
    if (element) {
        element.indeterminate = !!value;
    }
}

export function dispose() {
    // No listeners or timers are created by this module.
}
