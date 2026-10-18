import { defaultIntervalHours, intervalChoices } from '../subscriptions/subscriptionRequest';
import { intervalLabel } from '../subscriptions/subscriptionText';
import { element } from './elements';

/** Elements of the "Subscribe" block under the download options. */
export interface SubscribeFormElements {
    container: HTMLElement;
    name: HTMLInputElement;
    interval: HTMLSelectElement;
    submit: HTMLButtonElement;
    status: HTMLElement;
    error: HTMLElement;
}

/** Name and check interval for subscribing to the detected channel or playlist. A view only. */
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

    onSubmit(handler: (name: string, intervalHours: number) => void): void {
        this.elements.submit.addEventListener('click', () => {
            handler(this.elements.name.value, Number.parseInt(this.elements.interval.value, 10));
        });
    }

    show(suggestedName: string): void {
        this.elements.name.value = suggestedName;
        this.elements.interval.value = String(defaultIntervalHours);
        this.elements.status.textContent = '';
        this.elements.error.hidden = true;
        this.elements.container.hidden = false;
    }

    hide(): void {
        this.elements.container.hidden = true;
    }

    setBusy(busy: boolean): void {
        this.elements.submit.disabled = busy;
        this.elements.submit.textContent = busy ? 'Subscribing…' : 'Subscribe';
    }

    showDone(message: string): void {
        this.elements.error.hidden = true;
        this.elements.status.textContent = message;
    }

    showError(message: string): void {
        this.elements.status.textContent = '';
        this.elements.error.textContent = message;
        this.elements.error.hidden = false;
    }
}

function capitalize(text: string): string {
    return text.charAt(0).toUpperCase() + text.slice(1);
}
