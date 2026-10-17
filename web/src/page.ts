import { HttpError } from './api/httpClient';
import type { TubeDLFinApi } from './api/tubeDlFinApi';
import type { InfoPanel } from './views/infoPanel';
import type { LoadingIndicator } from './views/loadingIndicator';

/** Coordinates the page: loads data from the API when the page is shown and hands it to the views. */
export class TubeDLFinPage {
    constructor(
        private readonly view: HTMLElement,
        private readonly api: TubeDLFinApi,
        private readonly infoPanel: InfoPanel,
        private readonly loading: LoadingIndicator,
    ) {}

    attach(): void {
        this.view.addEventListener('viewshow', () => {
            void this.refresh();
        });
    }

    async refresh(): Promise<void> {
        this.loading.show();
        try {
            this.infoPanel.showInfo(await this.api.getInfo());
        } catch (error) {
            this.infoPanel.showError(describeError(error));
        } finally {
            this.loading.hide();
        }
    }
}

function describeError(error: unknown): string {
    if (error instanceof HttpError && error.status === 403) {
        return 'TubeDLFin is only available to administrators.';
    }

    const reason = error instanceof Error ? error.message : String(error);
    return `Could not reach the TubeDLFin API: ${reason}`;
}
