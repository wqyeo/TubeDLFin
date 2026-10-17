import type { TubeDLFinInfo } from '../api/types';

/** Renders the "Status" section. Text is always set with textContent, never as HTML. */
export class InfoPanel {
    constructor(
        private readonly list: HTMLElement,
        private readonly error: HTMLElement,
    ) {}

    showInfo(info: TubeDLFinInfo): void {
        const rows: ReadonlyArray<readonly [string, string]> = [
            ['Plugin version', info.PluginVersion],
            ['Jellyfin version', info.ServerVersion],
            ['Operating system', info.OperatingSystem],
            ['Architecture', info.Architecture],
            ['Data folder', info.DataPath],
        ];

        this.list.replaceChildren(...rows.flatMap(([label, value]) => [term(label), definition(value)]));
        this.error.hidden = true;
    }

    showError(message: string): void {
        this.list.replaceChildren();
        this.error.textContent = message;
        this.error.hidden = false;
    }
}

function term(text: string): HTMLElement {
    const element = document.createElement('dt');
    element.textContent = text;
    return element;
}

function definition(text: string): HTMLElement {
    const element = document.createElement('dd');
    element.textContent = text;
    return element;
}
