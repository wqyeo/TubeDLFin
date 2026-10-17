import type {
    AudioFormat,
    DownloadExtras,
    DownloadMode,
    DownloadRequest,
    NamingPresetId,
    ProbeRequest,
    ProbeResult,
    SponsorBlockCategory,
    SponsorBlockMode,
    VideoContainer,
} from '../api/types';

/** Prefix of select values that name a specific format from the Detect step. */
export const formatValuePrefix = 'format:';

/** What the admin chose in the download form, as the form holds it. */
export interface DownloadFormValues {
    mode: DownloadMode;
    /** "best", a height such as "1080", or "format:<id>". */
    videoQuality: string;
    container: VideoContainer;
    preferH264: boolean;
    /** "best" or "format:<id>". */
    audioStream: string;
    audioFormat: AudioFormat;
    /** "best" or a bitrate such as "192". */
    audioQuality: string;
    playlistItems: string;
    libraryId: string;
    libraryFolder: string;
    subfolder: string;
    naming: NamingPresetId;
    customTemplate: string;
    extras: DownloadExtras;
    subtitleLanguages: string;
    includeAutomaticSubtitles: boolean;
    embedSubtitles: boolean;
    sponsorBlockMode: SponsorBlockMode;
    sponsorBlockCategories: SponsorBlockCategory[];
    redownload: boolean;
}

/** Lossy formats, where a bitrate can be chosen. */
export function hasBitrate(format: AudioFormat): boolean {
    return format === 'M4a' || format === 'Mp3' || format === 'Opus';
}

/** Turns the form's values and the detected link into the API request. Pure: no DOM. */
export function buildDownloadRequest(
    values: DownloadFormValues,
    probe: ProbeRequest,
    result: ProbeResult,
): DownloadRequest {
    const video = values.mode === 'Video';
    const choice = video ? values.videoQuality : values.audioStream;
    const height = Number.parseInt(values.videoQuality, 10);
    const bitrate = Number.parseInt(values.audioQuality, 10);
    const items = values.playlistItems.trim();
    const subfolder = values.subfolder.trim();
    const languages = values.subtitleLanguages.replace(/\s+/g, '');
    const custom = values.naming === 'Custom' ? values.customTemplate.trim() : '';

    return {
        Url: probe.Url,
        NoPlaylist: probe.NoPlaylist,
        FormatId: choice.startsWith(formatValuePrefix) ? choice.slice(formatValuePrefix.length) : null,
        PlaylistItems: result.Kind !== 'Video' && items ? items : null,
        Redownload: values.redownload,
        Title: result.Title,
        Profile: {
            Mode: values.mode,
            MaxHeight: video && Number.isFinite(height) ? height : null,
            Container: values.container,
            PreferH264: values.preferH264,
            AudioFormat: values.audioFormat,
            AudioQuality: hasBitrate(values.audioFormat) && Number.isFinite(bitrate) ? bitrate : null,
            LibraryId: values.libraryId,
            LibraryFolder: values.libraryFolder,
            Subfolder: subfolder || null,
            Naming: values.naming,
            CustomTemplate: custom || null,
            Extras: { ...values.extras },
            Subtitles: {
                Languages: languages || null,
                IncludeAutomatic: values.includeAutomaticSubtitles,
                Embed: values.embedSubtitles,
            },
            SponsorBlock: {
                Mode: values.sponsorBlockCategories.length > 0 ? values.sponsorBlockMode : 'Off',
                Categories: [...values.sponsorBlockCategories],
            },
        },
    };
}
