// The toggle group shares the roving-tabindex mechanism with Toolbar; only the
// item selector, the owning container and the orientation attribute differ.
import { createRovingFocus } from '../../js/aeterni_roving_focus.js';

const roving = createRovingFocus({
    selector: 'button.aeterni-toggle-group__item',
    hostSelector: '.aeterni-toggle-group',
    orientationAttribute: 'data-orientation'
});

export const init = roving.init;
export const sync = roving.sync;
export const dispose = roving.dispose;
