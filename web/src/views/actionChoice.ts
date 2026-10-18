import type { FormAction } from '../downloads/formLayout';

/** Elements of the "Download now / Subscribe" choice above the options. */
export interface ActionChoiceElements {
    container: HTMLElement;
    download: HTMLInputElement;
    subscribe: HTMLInputElement;
}

/** Whether the detected channel or playlist is downloaded now or subscribed to. A view only. */
export class ActionChoice {
    constructor(private readonly elements: ActionChoiceElements) {}

    onChange(handler: (action: FormAction) => void): void {
        for (const input of [this.elements.download, this.elements.subscribe]) {
            input.addEventListener('change', () => handler(this.selected()));
        }
    }

    selected(): FormAction {
        return this.elements.subscribe.checked ? 'subscribe' : 'download';
    }

    /** Back to "Download now", for a newly detected link. */
    reset(): void {
        this.elements.download.checked = true;
        this.elements.subscribe.checked = false;
    }

    setVisible(visible: boolean): void {
        this.elements.container.hidden = !visible;
    }
}
