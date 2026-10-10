// The toolbar shares the roving-tabindex mechanism with ToggleGroup; only the
// item selector, the owning container and the orientation attribute differ.
import { createRovingFocus } from '../../js/aeterni_roving_focus.js';

const roving = createRovingFocus({
    selector: 'button.aeterni-button, button.aeterni-icon-button',
    hostSelector: '[role="toolbar"]',
    orientationAttribute: 'aria-orientation'
});

export const init = roving.init;
export const sync = roving.sync;
export const dispose = roving.dispose;
