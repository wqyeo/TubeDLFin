import type { DownloadExtras, LibraryInfo } from '../api/types';
import type { DownloadFormValues } from './downloadRequest';

/** Options are remembered separately for music and for everything else. */
export type OptionsCategory = 'music' | 'video';

/** The values a library choice decides; the rest of the form keeps what it has. */
export type LibraryDrivenValues = Pick<DownloadFormValues, 'mode' | 'naming' | 'extras'>;

const videoExtras: DownloadExtras = {
    WriteNfo: true,
    SaveThumbnail: true,
    EmbedThumbnail: false,
    EmbedMetadata: true,
    EmbedChapters: true,
};

// Jellyfin reads music from the files' tags: no .nfo, the thumbnail becomes embedded cover art.
const musicExtras: DownloadExtras = {
    WriteNfo: false,
    SaveThumbnail: false,
    EmbedThumbnail: true,
    EmbedMetadata: true,
    EmbedChapters: false,
};

export function categoryOf(library: LibraryInfo): OptionsCategory {
    return library.Type === 'Music' ? 'music' : 'video';
}

/** What a library type suggests when nothing has been remembered for it yet. */
export function libraryDefaults(library: LibraryInfo): LibraryDrivenValues {
    return categoryOf(library) === 'music'
        ? { mode: 'Audio', naming: 'Music', extras: { ...musicExtras } }
        : { mode: 'Video', naming: library.DefaultNaming, extras: { ...videoExtras } };
}
