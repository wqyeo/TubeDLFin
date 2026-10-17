import type { TubeDLFinSettings } from '../api/types';

/** Elements of the "Settings" form. */
export interface SettingsFormElements {
    form: HTMLFormElement;
    ytDlpPath: HTMLInputElement;
    denoPath: HTMLInputElement;
    error: HTMLElement;
}

/** Renders the "Settings" form and reports what the admin submits. */
export class SettingsForm {
    constructor(private readonly elements: SettingsFormElements) {}

    onSubmit(handler: (settings: TubeDLFinSettings) => void): void {
        this.elements.form.addEventListener('submit', (event) => {
            event.preventDefault();
            handler({
                YtDlpPath: this.elements.ytDlpPath.value.trim(),
                DenoPath: this.elements.denoPath.value.trim(),
            });
        });
    }

    show(settings: TubeDLFinSettings): void {
        this.elements.ytDlpPath.value = settings.YtDlpPath;
        this.elements.denoPath.value = settings.DenoPath;
        this.elements.error.hidden = true;
    }

    showError(message: string): void {
        this.elements.error.textContent = message;
        this.elements.error.hidden = false;
    }
}
