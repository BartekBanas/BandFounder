import {useRef} from 'react';
import {Fade, type SelectProps} from '@mui/material';

/**
 * Lines a Select's menu up with its trigger, to the sub-pixel.
 *
 * MUI sizes the menu from the trigger's integer `clientWidth`, and it rounds
 * the menu's `left` to a whole pixel. Both leave the menu a fraction of a pixel
 * off the trigger, which shows a sliver of the field underneath at some zoom
 * levels.
 *
 * When the menu enters, this hook reads the trigger's exact box and sets the
 * menu's width and offset to match it. It runs before the first paint.
 *
 * Spread the result onto a Select, or onto a TextField's `SelectProps`.
 */
function alignMenuToTrigger(menu: HTMLElement, trigger: Element) {
    const box = trigger.getBoundingClientRect();
    const placed = getComputedStyle(menu);
    const marginLeft = parseFloat(placed.marginLeft) || 0;
    const placedLeft = parseFloat(placed.left) + marginLeft;

    menu.style.minWidth = `${box.width}px`;
    menu.style.marginLeft = `${marginLeft + box.left - placedLeft}px`;
}

export function useTriggerMatchedMenu(): Pick<SelectProps, 'onOpen' | 'MenuProps'> {
    const triggerRef = useRef<Element | null>(null);

    return {
        onOpen: (event) => {
            triggerRef.current = event.currentTarget;
        },
        MenuProps: {
            // Fade, not the default Grow. Grow scales the menu up from 75% width, so the field
            // underneath shows past its edges until the animation ends.
            slots: {transition: Fade},
            slotProps: {
                transition: {
                    onEnter: (menu: HTMLElement) => {
                        const trigger = triggerRef.current;
                        if (!trigger) {
                            return;
                        }
                        // MUI positions the menu after this callback runs. The microtask runs
                        // after that commit and before the browser paints, so nothing flashes.
                        queueMicrotask(() => alignMenuToTrigger(menu, trigger));
                    },
                },
            },
        },
    };
}
