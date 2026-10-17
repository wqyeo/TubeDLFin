import type { TubeDLFinApi } from '../api/tubeDlFinApi';
import { buildDownloadRequest, type DownloadFormValues } from '../downloads/downloadRequest';
import { describeError } from '../errors';
import type { DetectState, DetectStore } from '../state/detectStore';
import type { DownloadForm } from '../views/downloadForm';
import type { PageSection } from './pageSection';

/** Something that shows the queue and can be told to refresh it. */
export interface QueueRefresher {
    refresh(): Promise<void>;
}

/**
 * The "Download" step: shows the options form for the detected link, and queues the download.
 */
export class DownloadSection implements PageSection {
    constructor(
        private readonly api: TubeDLFinApi,
        private readonly detectStore: DetectStore,
        private readonly form: DownloadForm,
        private readonly queue: QueueRefresher,
    ) {
        detectStore.subscribe((state) => this.onDetectChanged(state));
        form.onSubmit((values) => {
            void this.download(values);
        });
    }

    async load(): Promise<void> {
        try {
            this.form.setLibraries(await this.api.getLibraries());
        } catch (error) {
            this.form.showError(describeError(error));
        }
    }

    private onDetectChanged(state: DetectState): void {
        if (state.status === 'detected') {
            this.form.show(state.result);
        } else {
            this.form.hide();
        }
    }

    private async download(values: DownloadFormValues): Promise<void> {
        const state = this.detectStore.current;
        if (state.status !== 'detected') {
            return;
        }

        this.form.setBusy(true);
        try {
            const job = await this.api.addDownload(buildDownloadRequest(values, state.request, state.result));
            this.form.showAdded(`Added "${job.Title ?? state.result.Title}" to the queue.`);
            await this.queue.refresh();
        } catch (error) {
            this.form.showError(describeError(error));
        } finally {
            this.form.setBusy(false);
        }
    }
}
