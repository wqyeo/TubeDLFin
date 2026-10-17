import type { PageSection } from './sections/pageSection';
import type { LoadingIndicator } from './views/loadingIndicator';

/** The page: loads every section whenever it is shown. */
export class TubeDLFinPage {
    constructor(
        private readonly view: HTMLElement,
        private readonly sections: readonly PageSection[],
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
            // Sections handle their own errors, so one failing doesn't block the others.
            await Promise.all(this.sections.map((section) => section.load()));
        } finally {
            this.loading.hide();
        }
    }
}
