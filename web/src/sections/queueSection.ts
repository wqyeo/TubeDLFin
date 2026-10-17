import type { TubeDLFinApi } from '../api/tubeDlFinApi';
import { describeError } from '../errors';
import type { QueueView } from '../views/queueView';
import type { QueueRefresher } from './downloadSection';
import type { PageSection } from './pageSection';

/** How often the queue is refreshed while something is running, and while idle. */
const activeInterval = 1500;
const idleInterval = 10000;

/**
 * The "Queue" section: polls the download list while the page is shown, faster while a
 * download is running, and handles cancel, remove, clear and log requests.
 */
export class QueueSection implements PageSection, QueueRefresher {
    private timer: number | undefined;
    private visible = false;
    private refreshing: Promise<void> | undefined;

    constructor(
        private readonly api: TubeDLFinApi,
        private readonly view: QueueView,
    ) {
        view.onCancel((id) => void this.act(() => api.cancelDownload(id)));
        view.onRemove((id) => void this.act(() => api.removeDownload(id)));
        view.onClearFinished(() => void this.act(() => api.clearFinishedDownloads()));
        view.onShowLog((id) => this.showLog(id));
    }

    load(): Promise<void> {
        this.visible = true;
        return this.refresh();
    }

    hide(): void {
        this.visible = false;
        this.stopTimer();
    }

    /** Refreshes now (one refresh at a time) and schedules the next one. */
    refresh(): Promise<void> {
        this.refreshing ??= this.doRefresh().finally(() => {
            this.refreshing = undefined;
        });
        return this.refreshing;
    }

    private showLog(id: string): void {
        this.api.getDownloadLog(id).then(
            (lines) => this.view.showLog(id, lines),
            (error: unknown) => this.view.showLog(id, [describeError(error)]),
        );
    }

    private async doRefresh(): Promise<void> {
        this.stopTimer();
        let active = false;
        try {
            const jobs = await this.api.getDownloads();
            active = jobs.some((job) => !job.IsFinished);
            this.view.render(jobs);
        } catch (error) {
            this.view.showError(describeError(error));
        }

        if (this.visible) {
            this.timer = window.setTimeout(() => void this.refresh(), active ? activeInterval : idleInterval);
        }
    }

    private async act(action: () => Promise<unknown>): Promise<void> {
        try {
            await action();
        } catch (error) {
            this.view.showError(describeError(error));
        }

        await this.refresh();
    }

    private stopTimer(): void {
        if (this.timer !== undefined) {
            window.clearTimeout(this.timer);
            this.timer = undefined;
        }
    }
}
