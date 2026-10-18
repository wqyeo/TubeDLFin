import { defaultIntervalHours, intervalChoices } from '../subscriptions/subscriptionRequest';
import { intervalLabel } from '../subscriptions/subscriptionText';
import { element } from './elements';

/** Elements of the subscription fields, shown when "Subscribe" is chosen. */
export interface SubscribeFormElements {
    container: HTMLElement;
    name: HTMLInputElement;
    interval: HTMLSelectElement;
    includeExisting: HTMLInputElement;
}

/** What the subscription fields hold. */
export interface SubscribeFormValues {
    name: string;
    intervalHours: number;
    /** Also download what the channel or playlist has now. */
    includeExisting: boolean;
}

/** Name, check interval and "also download what it has now" of a new subscription. A view only. */
export class SubscribeForm {
    constructor(private readonly elements: SubscribeFormElements) {
        elements.interval.replaceChildren(
            ...intervalChoices.map((hours) => {
                const node = element('option', capitalize(intervalLabel(hours)));
                node.value = String(hours);
                return node;
            }),
        );
    }

    onIncludeExistingChanged(handler: (includeExisting: boolean) => void): void {
        this.elements.includeExisting.addEventListener('change', () => {
            handler(this.elements.includeExisting.checked);
        });
    }

    /** Starts over for a newly detected channel or playlist. */
    reset(suggestedName: string): void {
        this.elements.name.value = suggestedName;
        this.elements.interval.value = String(defaultIntervalHours);
        this.elements.includeExisting.checked = false;
    }

    setVisible(visible: boolean): void {
        this.elements.container.hidden = !visible;
    }

    values(): SubscribeFormValues {
        return {
            name: this.elements.name.value,
            intervalHours: Number.parseInt(this.elements.interval.value, 10),
            includeExisting: this.elements.includeExisting.checked,
        };
    }
}

function capitalize(text: string): string {
    return text.charAt(0).toUpperCase() + text.slice(1);
}
