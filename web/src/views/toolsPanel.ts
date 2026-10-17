import type {
    FfmpegStatus,
    ToolState,
    ToolStatus,
    ToolsOverview,
    ToolUpdateOutcome,
    ToolUpdateReport,
} from '../api/types';
import { element } from './elements';

const stateLabels: Record<ToolState, string> = {
    Ready: 'Ready',
    NotInstalled: 'Not installed',
    Broken: 'Not working',
    Unsupported: 'Not available for this system',
};

const outcomeLabels: Record<ToolUpdateOutcome, string> = {
    Installed: 'installed',
    Updated: 'updated',
    UpToDate: 'already up to date',
    SkippedCustom: 'skipped (custom path)',
    Unsupported: 'not available for this system',
    Failed: 'failed',
};

/** Elements of the "Tools" section. */
export interface ToolsPanelElements {
    rows: HTMLElement;
    platform: HTMLElement;
    updateButton: HTMLButtonElement;
    lastUpdate: HTMLElement;
    error: HTMLElement;
}

/** Renders the "Tools" section: one row per tool plus FFmpeg, and the last update's results. */
export class ToolsPanel {
    constructor(private readonly elements: ToolsPanelElements) {}

    onUpdateClicked(handler: () => void): void {
        this.elements.updateButton.addEventListener('click', handler);
    }

    show(overview: ToolsOverview): void {
        this.elements.platform.textContent = overview.Platform;
        this.elements.rows.replaceChildren(...overview.Tools.map(toolRow), ffmpegRow(overview.Ffmpeg));
        this.renderLastUpdate(overview.LastUpdate);
        this.setBusy(overview.IsUpdating);
        this.elements.error.hidden = true;
    }

    setBusy(busy: boolean): void {
        this.elements.updateButton.disabled = busy;
        this.elements.updateButton.textContent = busy ? 'Updating…' : 'Update now';
    }

    showError(message: string): void {
        this.elements.error.textContent = message;
        this.elements.error.hidden = false;
    }

    private renderLastUpdate(report: ToolUpdateReport | null | undefined): void {
        if (!report) {
            this.elements.lastUpdate.replaceChildren();
            return;
        }

        const finished = new Date(report.FinishedAt).toLocaleString();
        const items = report.Results.map((result) => {
            const version = result.Version ? ` ${result.Version}` : '';
            const message = result.Message ? ` – ${result.Message}` : '';
            const className = result.Outcome === 'Failed' ? 'tubedlfin-bad' : undefined;
            return element(
                'li',
                `${result.Name}${version}: ${outcomeLabels[result.Outcome]}${message}`,
                className,
            );
        });

        const list = element('ul');
        list.replaceChildren(...items);
        this.elements.lastUpdate.replaceChildren(element('p', `Last update: ${finished}`), list);
    }
}

function toolRow(tool: ToolStatus): HTMLTableRowElement {
    const status = stateLabels[tool.State] + (tool.IsCustom ? ' (custom path)' : '');
    return row(tool.Name, status, tool.State === 'Ready', tool.Version, tool.Path, tool.Message);
}

function ffmpegRow(ffmpeg: FfmpegStatus): HTMLTableRowElement {
    return ffmpeg.Found
        ? row('FFmpeg (from Jellyfin)', 'Ready', true, ffmpeg.Version, ffmpeg.EncoderPath)
        : row(
              'FFmpeg (from Jellyfin)',
              'Not found',
              false,
              null,
              null,
              'Set the FFmpeg path under Dashboard → Playback → Transcoding.',
          );
}

function row(
    name: string,
    status: string,
    good: boolean,
    version?: string | null,
    path?: string | null,
    message?: string | null,
): HTMLTableRowElement {
    const statusCell = element('td');
    statusCell.append(element('span', status, good ? 'tubedlfin-good' : 'tubedlfin-bad'));
    if (message) {
        statusCell.append(element('div', message, 'fieldDescription'));
    }

    const tr = element('tr');
    tr.append(
        element('td', name),
        statusCell,
        element('td', version ?? '–'),
        element('td', path ?? '–', 'tubedlfin-path'),
    );
    return tr;
}
