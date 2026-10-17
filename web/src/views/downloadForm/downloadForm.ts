import type {
    AudioFormat,
    LibraryInfo,
    MediaFormat,
    NamingPreset,
    ProbeResult,
    VideoContainer,
} from '../../api/types';
import { type DownloadFormValues, formatValuePrefix, hasBitrate } from '../../downloads/downloadRequest';
import { estimateSize } from '../../downloads/freeSpace';
import type { RememberedOptions } from '../../downloads/optionsMemory';
import { element } from '../elements';
import { codecName, formatBitrate, formatResolution, formatSize } from '../format';
import type { DestinationFields } from './destinationFields';
import type { ExtrasFields } from './extrasFields';
import type { MusicFields } from './musicFields';
import type { NamingFields } from './namingFields';
import type { SponsorBlockFields } from './sponsorBlockFields';
import type { SubtitleFields } from './subtitleFields';

const heights = [2160, 1440, 1080, 720, 480, 360];

/** Elements of the download form itself (the field groups have their own). */
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
    redownload: HTMLInputElement;
    submit: HTMLButtonElement;
    status: HTMLElement;
    error: HTMLElement;
}

/** The field groups the form is made of. */
export interface DownloadFormGroups {
    destination: DestinationFields;
    naming: NamingFields;
    extras: ExtrasFields;
    subtitles: SubtitleFields;
    sponsorBlock: SponsorBlockFields;
    music: MusicFields;
}

/**
 * The options form under a detected link. A view only: it shows and reads values. Which
 * values to start from (library defaults, remembered options) is the section's decision.
 */
export class DownloadForm {
    private isList = false;

    constructor(
        private readonly elements: DownloadFormElements,
        private readonly groups: DownloadFormGroups,
    ) {
        elements.modeVideo.addEventListener('change', () => this.updateVisibility());
        elements.modeAudio.addEventListener('change', () => this.updateVisibility());
        elements.audioFormat.addEventListener('change', () => this.updateVisibility());
        groups.destination.onLibrarySelected(() => this.updateVisibility());
    }

    onSubmit(handler: (values: DownloadFormValues) => void): void {
        this.elements.form.addEventListener('submit', (event) => {
            event.preventDefault();
            handler(this.values());
        });
    }

    onLibraryChanged(handler: (library: LibraryInfo) => void): void {
        this.groups.destination.onLibraryChanged(handler);
    }

    setLibraries(libraries: LibraryInfo[]): void {
        this.groups.destination.setLibraries(libraries);
        this.setBusy(false);
        if (libraries.length === 0) {
            this.showError('There are no video or music libraries yet. Add one under Dashboard → Libraries.');
        }
    }

    setNamingPresets(presets: NamingPreset[]): void {
        this.groups.naming.setPresets(presets);
    }

    selectLibrary(id: string | undefined): LibraryInfo | undefined {
        return this.groups.destination.selectLibrary(id);
    }

    selectedLibrary(): LibraryInfo | undefined {
        return this.groups.destination.selectedLibrary();
    }

    /** Fills in what depends on the detected link and shows the form. */
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

        const isList = result.Kind !== 'Video';
        this.isList = isList;
        elements.playlistOptions.hidden = !isList;
        elements.playlistItems.value = '';
        elements.playlistHint.hidden = !(
            isList &&
            result.Entries.length > 0 &&
            result.Entries.every((entry) => entry.IsCollection)
        );
        elements.redownload.checked = false;

        this.groups.subtitles.showTracks(result.Subtitles, result.AutomaticCaptions);
        this.groups.destination.setEstimate(estimateSize(result));

        elements.status.textContent = '';
        elements.error.hidden = true;
        elements.form.hidden = false;
        this.updateVisibility();
    }

    hide(): void {
        this.elements.form.hidden = true;
    }

    /** Sets the given values; anything left out keeps its current value. */
    apply(values: Partial<RememberedOptions>): void {
        const elements = this.elements;
        if (values.mode) {
            elements.modeAudio.checked = values.mode === 'Audio';
            elements.modeVideo.checked = values.mode !== 'Audio';
        }

        setSelect(elements.videoQuality, values.videoQuality);
        setSelect(elements.container, values.container);
        setSelect(elements.audioStream, values.audioStream);
        setSelect(elements.audioFormat, values.audioFormat);
        setSelect(elements.audioQuality, values.audioQuality);
        if (values.preferH264 !== undefined) {
            elements.preferH264.checked = values.preferH264;
        }

        this.groups.naming.write(values);
        if (values.extras) {
            this.groups.extras.write(values.extras);
        }

        this.groups.subtitles.write(values);
        this.groups.sponsorBlock.write(values);
        if (values.music) {
            this.groups.music.write(values.music);
        }

        if (values.libraryFolder !== undefined || values.subfolder !== undefined) {
            this.groups.destination.write(values.libraryFolder, values.subfolder ?? '');
        }

        this.updateVisibility();
    }

    setBusy(busy: boolean): void {
        this.elements.submit.disabled = busy || !this.groups.destination.hasLibraries;
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

    values(): DownloadFormValues {
        const elements = this.elements;
        return {
            mode: elements.modeAudio.checked ? 'Audio' : 'Video',
            videoQuality: elements.videoQuality.value,
            container: elements.container.value as VideoContainer,
            preferH264: elements.preferH264.checked,
            audioStream: elements.audioStream.value,
            audioFormat: elements.audioFormat.value as AudioFormat,
            audioQuality: elements.audioQuality.value,
            playlistItems: elements.playlistItems.value,
            ...this.groups.destination.read(),
            ...this.groups.naming.read(),
            extras: this.groups.extras.read(),
            ...this.groups.subtitles.read(),
            ...this.groups.sponsorBlock.read(),
            music: { ...this.groups.music.read(), Enabled: this.tagsAsMusic() },
            redownload: elements.redownload.checked,
        };
    }

    // Audio downloads and music videos get music tags (artist, title, album, track).
    private tagsAsMusic(): boolean {
        return (
            this.elements.modeAudio.checked ||
            this.groups.destination.selectedLibrary()?.Type === 'MusicVideos'
        );
    }

    private updateVisibility(): void {
        const audio = this.elements.modeAudio.checked;
        this.elements.videoOptions.hidden = audio;
        this.elements.h264Option.hidden = audio;
        this.elements.audioOptions.hidden = !audio;
        this.elements.audioQuality.disabled = !hasBitrate(this.elements.audioFormat.value as AudioFormat);
        this.groups.music.show(this.tagsAsMusic(), this.isList);
    }
}

// Only values the list offers are set, so a remembered value that no longer exists is ignored.
function setSelect(select: HTMLSelectElement, value: string | undefined): void {
    if (value !== undefined && [...select.options].some((o) => o.value === value)) {
        select.value = value;
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
