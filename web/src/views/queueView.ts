import type { DownloadJobInfo } from '../api/types';
import { stageLabel, stateLabels } from './downloadLabels';
import { button, element, externalLink } from './elements';
import { formatDuration, formatSize } from './format';

/** What a job's buttons ask for. */
interface JobActions {
    cancel(id: string): void;
    remove(id: string): void;
    showLog(id: string): void;
}

/** Elements of the queue section. */
export interface QueueViewElements {
    list: HTMLElement;
    empty: HTMLElement;
    clearFinished: HTMLButtonElement;
    error: HTMLElement;
}

/**
 * The download queue, newest first. Rows are kept per job and updated in place, so an open
 * "Details" panel and button focus survive the regular refresh.
 */
export class QueueView {
    private readonly rows = new Map<string, JobRow>();
    private readonly handlers = {
        cancel: (_id: string) => {},
        remove: (_id: string) => {},
        showLog: (_id: string) => {},
    };

    // Rows call these; they forward to whatever handler is registered.
    private readonly actions: JobActions = {
        cancel: (id) => this.handlers.cancel(id),
        remove: (id) => this.handlers.remove(id),
        showLog: (id) => this.handlers.showLog(id),
    };

    constructor(private readonly elements: QueueViewElements) {}

    onCancel(handler: (id: string) => void): void {
        this.handlers.cancel = handler;
    }

    onRemove(handler: (id: string) => void): void {
        this.handlers.remove = handler;
    }

    /** Called when a job's "Details" is open and its log should be (re)loaded. */
    onShowLog(handler: (id: string) => void): void {
        this.handlers.showLog = handler;
    }

    onClearFinished(handler: () => void): void {
        this.elements.clearFinished.addEventListener('click', handler);
    }

    render(jobs: readonly DownloadJobInfo[]): void {
        const ids = new Set(jobs.map((job) => job.Id));
        for (const [id, row] of this.rows) {
            if (!ids.has(id)) {
                row.element.remove();
                this.rows.delete(id);
            }
        }

        const ordered = jobs.map((job) => {
            let row = this.rows.get(job.Id);
            if (!row) {
                row = new JobRow(job.Id, this.actions);
                this.rows.set(job.Id, row);
            }

            row.update(job);
            return row.element;
        });

        // Reorders only what moved; existing nodes are reused, not rebuilt.
        ordered.forEach((node, index) => {
            if (this.elements.list.children[index] !== node) {
                this.elements.list.insertBefore(node, this.elements.list.children[index] ?? null);
            }
        });

        this.elements.empty.hidden = jobs.length > 0;
        this.elements.clearFinished.hidden = !jobs.some((job) => job.IsFinished);
        this.elements.error.hidden = true;
    }

    showLog(id: string, lines: readonly string[]): void {
        this.rows.get(id)?.showLog(lines);
    }

    showError(message: string): void {
        this.elements.error.textContent = message;
        this.elements.error.hidden = false;
    }
}

/** One job in the queue. */
class JobRow {
    readonly element = element('div', null, 'tubedlfin-job');
    private readonly title = element('div', null, 'tubedlfin-job-title');
    private readonly badge = element('span', null, 'tubedlfin-badge');
    private readonly destination = element('div', null, 'tubedlfin-job-meta');
    private readonly bar = element('progress', null, 'tubedlfin-progress');
    private readonly status = element('div', null, 'tubedlfin-job-meta');
    private readonly error = element('div', null, 'tubedlfin-job-error');
    private readonly cancel: HTMLButtonElement;
    private readonly remove: HTMLButtonElement;
    private readonly details = element('details', null, 'tubedlfin-details');
    private readonly files = element('ul', null, 'tubedlfin-files');
    private readonly log = element('pre', 'Loading…', 'tubedlfin-log');
    private readonly requestLog: () => void;
    private url = '';

    constructor(id: string, actions: JobActions) {
        this.requestLog = () => actions.showLog(id);
        this.cancel = button('Cancel', () => actions.cancel(id), 'tubedlfin-small-button');
        this.remove = button('Remove', () => actions.remove(id), 'tubedlfin-small-button');
        this.remove.title = 'Remove from the list. The downloaded files are kept.';
        this.bar.max = 100;

        // The log is fetched when the panel opens, and again on every refresh while it's open.
        this.details.addEventListener('toggle', () => {
            if (this.details.open) {
                this.requestLog();
            }
        });
        this.details.append(element('summary', 'Details'), this.files, this.log);

        const header = element('div', null, 'tubedlfin-job-header');
        const buttons = element('div', null, 'tubedlfin-job-buttons');
        buttons.append(this.cancel, this.remove);
        header.append(this.badge, this.title, buttons);
        this.element.append(header, this.destination, this.bar, this.status, this.error, this.details);
    }

    update(job: DownloadJobInfo): void {
        const state = stateLabels[job.State];
        this.badge.textContent = state.label;
        this.badge.className = `tubedlfin-badge tubedlfin-badge-${state.tone}`;

        if (this.url !== job.Url || this.title.textContent !== (job.Title ?? job.Url)) {
            this.url = job.Url;
            this.title.replaceChildren(externalLink(job.Title ?? job.Url, job.Url));
        }

        this.destination.textContent = `${job.LibraryName} · ${job.Destination}`;

        const running = job.State === 'Downloading' || job.State === 'Processing';
        this.bar.hidden = !running && job.State !== 'Queued';
        if (job.Percent != null && running) {
            this.bar.value = job.Percent;
        } else {
            this.bar.removeAttribute('value'); // indeterminate while queued or starting
        }

        this.status.textContent = statusLine(job);
        this.error.textContent = job.Error ?? '';
        this.error.className = `tubedlfin-job-error${job.State === 'CompletedWithErrors' ? ' tubedlfin-warn' : ''}`;
        this.error.hidden = !job.Error;

        this.cancel.hidden = job.IsFinished;
        this.remove.hidden = !job.IsFinished;

        this.files.replaceChildren(...job.Files.map((file) => element('li', file, 'tubedlfin-path')));
        if (this.details.open) {
            this.requestLog();
        }
    }

    showLog(lines: readonly string[]): void {
        this.log.textContent = lines.length > 0 ? lines.join('\n') : 'No output yet.';
    }
}

function statusLine(job: DownloadJobInfo): string {
    const parts: string[] = [];
    const running = job.State === 'Downloading' || job.State === 'Processing';
    if (running && job.ItemCount != null && job.ItemCount > 1 && job.ItemIndex != null) {
        parts.push(`Item ${job.ItemIndex} of ${job.ItemCount}${job.ItemTitle ? `: ${job.ItemTitle}` : ''}`);
    }

    switch (job.State) {
        case 'Queued':
            return 'Waiting for a free slot…';
        case 'Processing':
            parts.push(job.Stage ? `${stageLabel(job.Stage)}…` : 'Processing…');
            break;
        case 'Downloading':
            if (job.Percent != null) {
                parts.push(`${Math.floor(job.Percent)}%`);
            }

            if (job.DownloadedBytes != null) {
                parts.push(
                    job.TotalBytes != null
                        ? `${formatSize(job.DownloadedBytes)} of ${formatSize(job.TotalBytes)}`
                        : formatSize(job.DownloadedBytes),
                );
            }

            if (job.Speed != null) {
                parts.push(`${formatSize(job.Speed)}/s`);
            }

            if (job.Eta != null) {
                parts.push(`${formatDuration(job.Eta)} left`);
            }

            break;
        default: {
            const files = job.Files.length;
            if (files > 0) {
                parts.push(`${files} file${files === 1 ? '' : 's'} added`);
            }

            if (job.SkippedItems > 0) {
                parts.push(`${job.SkippedItems} already downloaded before`);
            }

            if (job.FinishedAt) {
                parts.push(new Date(job.FinishedAt).toLocaleString());
            }
        }
    }

    return parts.join(' · ');
}
