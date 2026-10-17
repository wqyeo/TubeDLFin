import type {
    AudioFormat,
    DownloadMode,
    LibraryInfo,
    MediaFormat,
    ProbeResult,
    VideoContainer,
} from '../api/types';
import { type DownloadFormValues, formatValuePrefix, hasBitrate } from '../downloads/downloadRequest';
import { element } from './elements';
import { codecName, formatBitrate, formatResolution, formatSize } from './format';

const heights = [2160, 1440, 1080, 720, 480, 360];

/** Elements of the download options form. */
export interface DownloadFormElements {
    form: HTMLFormElement;
    modeVideo: HTMLInputElement;
    modeAudio: HTMLInputElement;
    videoOptions: HTMLElement;
    videoQuality: HTMLSelectElement;
    container: HTMLSelectElement;
    h264Option: HTMLElement;
    preferH264: HTMLInputElement;
    audioOptions: HTMLElement;
    audioStream: HTMLSelectElement;
    audioFormat: HTMLSelectElement;
    audioQuality: HTMLSelectElement;
    playlistOptions: HTMLElement;
    playlistItems: HTMLInputElement;
    playlistHint: HTMLElement;
    library: HTMLSelectElement;
    folderContainer: HTMLElement;
    folder: HTMLSelectElement;
    subfolder: HTMLInputElement;
    redownload: HTMLInputElement;
    submit: HTMLButtonElement;
    status: HTMLElement;
    error: HTMLElement;
}

/** The options form shown under a detected link: what to keep, quality and destination. */
export class DownloadForm {
    private libraries: LibraryInfo[] = [];

    constructor(private readonly elements: DownloadFormElements) {
        elements.modeVideo.addEventListener('change', () => this.updateVisibility());
        elements.modeAudio.addEventListener('change', () => this.updateVisibility());
        elements.audioFormat.addEventListener('change', () => this.updateVisibility());
        elements.library.addEventListener('change', () => this.fillFolders());
    }

    onSubmit(handler: (values: DownloadFormValues) => void): void {
        this.elements.form.addEventListener('submit', (event) => {
            event.preventDefault();
            handler(this.values());
        });
    }

    setLibraries(libraries: LibraryInfo[]): void {
        const selected = this.elements.library.value;
        this.libraries = libraries;
        this.elements.library.replaceChildren(
            ...libraries.map((library) =>
                option(library.Id, `${library.Name} (${libraryTypeLabel(library)})`),
            ),
        );
        if (libraries.some((library) => library.Id === selected)) {
            this.elements.library.value = selected;
        }

        this.fillFolders();
        this.elements.submit.disabled = libraries.length === 0;
        if (libraries.length === 0) {
            this.showError('There are no video or music libraries yet. Add one under Dashboard → Libraries.');
        }
    }

    show(result: ProbeResult): void {
        const elements = this.elements;
        elements.videoQuality.replaceChildren(
            option('best', 'Best available'),
            ...heights.map((height) => option(String(height), `Up to ${height}p`)),
            ...formatGroup('Formats of this video', result.VideoFormats, videoFormatLabel),
        );
        elements.audioStream.replaceChildren(
            option('best', 'Best audio'),
            ...formatGroup('Audio formats of this video', result.AudioFormats, audioFormatLabel),
        );

        // Sites without video (SoundCloud, ...) start in audio mode.
        const audioOnly = result.VideoFormats.length === 0 && result.AudioFormats.length > 0;
        elements.modeAudio.checked = audioOnly;
        elements.modeVideo.checked = !audioOnly;

        const isList = result.Kind !== 'Video';
        elements.playlistOptions.hidden = !isList;
        elements.playlistItems.value = '';
        const onlyTabs =
            isList && result.Entries.length > 0 && result.Entries.every((entry) => entry.IsCollection);
        elements.playlistHint.hidden = !onlyTabs;

        elements.status.textContent = '';
        elements.error.hidden = true;
        elements.form.hidden = false;
        this.updateVisibility();
    }

    hide(): void {
        this.elements.form.hidden = true;
    }

    setBusy(busy: boolean): void {
        this.elements.submit.disabled = busy || this.libraries.length === 0;
        this.elements.submit.textContent = busy ? 'Adding…' : 'Download';
    }

    showAdded(message: string): void {
        this.elements.error.hidden = true;
        this.elements.status.textContent = message;
    }

    showError(message: string): void {
        this.elements.status.textContent = '';
        this.elements.error.textContent = message;
        this.elements.error.hidden = false;
    }

    private values(): DownloadFormValues {
        const elements = this.elements;
        return {
            mode: (elements.modeAudio.checked ? 'Audio' : 'Video') satisfies DownloadMode,
            videoQuality: elements.videoQuality.value,
            container: elements.container.value as VideoContainer,
            preferH264: elements.preferH264.checked,
            audioStream: elements.audioStream.value,
            audioFormat: elements.audioFormat.value as AudioFormat,
            audioQuality: elements.audioQuality.value,
            playlistItems: elements.playlistItems.value,
            libraryId: elements.library.value,
            libraryFolder: elements.folder.value,
            subfolder: elements.subfolder.value,
            redownload: elements.redownload.checked,
        };
    }

    private updateVisibility(): void {
        const audio = this.elements.modeAudio.checked;
        this.elements.videoOptions.hidden = audio;
        this.elements.h264Option.hidden = audio;
        this.elements.audioOptions.hidden = !audio;
        this.elements.audioQuality.disabled = !hasBitrate(this.elements.audioFormat.value as AudioFormat);
    }

    private fillFolders(): void {
        const library = this.libraries.find((candidate) => candidate.Id === this.elements.library.value);
        const folders = library?.Folders ?? [];
        this.elements.folder.replaceChildren(...folders.map((folder) => option(folder, folder)));
        this.elements.folderContainer.hidden = folders.length < 2;
    }
}

function option(value: string, label: string): HTMLOptionElement {
    const node = element('option', label);
    node.value = value;
    return node;
}

function formatGroup(
    label: string,
    formats: readonly MediaFormat[],
    describe: (format: MediaFormat) => string,
): HTMLOptGroupElement[] {
    if (formats.length === 0) {
        return [];
    }

    const group = element('optgroup');
    group.label = label;
    group.append(...formats.map((format) => option(formatValuePrefix + format.FormatId, describe(format))));
    return [group];
}

function videoFormatLabel(format: MediaFormat): string {
    return [
        formatResolution(format.Height, format.Fps),
        codecName(format.VideoCodec),
        format.Extension,
        format.HasAudio ? 'with audio' : null,
        format.FileSize != null ? formatSize(format.FileSize, format.IsFileSizeApproximate) : null,
        `#${format.FormatId}`,
    ]
        .filter(Boolean)
        .join(' · ');
}

function audioFormatLabel(format: MediaFormat): string {
    return [
        codecName(format.AudioCodec),
        formatBitrate(format.AudioBitrate ?? format.Bitrate),
        format.Language,
        format.Note,
        format.Extension,
        `#${format.FormatId}`,
    ]
        .filter(Boolean)
        .join(' · ');
}

function libraryTypeLabel(library: LibraryInfo): string {
    switch (library.Type) {
        case 'Shows':
            return 'shows';
        case 'MusicVideos':
            return 'music videos';
        case 'HomeVideos':
            return 'home videos';
        default:
            return library.Type.toLowerCase();
    }
}
