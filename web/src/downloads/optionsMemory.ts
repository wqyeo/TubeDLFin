import { type DownloadFormValues, formatValuePrefix } from './downloadRequest';
import type { OptionsCategory } from './libraryDefaults';

/** The remembered part of the form: everything that isn't about one particular link. */
export type RememberedOptions = Omit<DownloadFormValues, 'playlistItems' | 'redownload'>;

/** Remembers the last download options, separately for music and video, and the last library. */
export interface OptionsMemory {
    load(category: OptionsCategory): Partial<RememberedOptions> | undefined;
    save(category: OptionsCategory, values: DownloadFormValues): void;
    lastLibraryId(): string | undefined;
}

/** Strips what only applies to the link at hand: playlist items, "download again", chosen formats. */
export function rememberable(values: DownloadFormValues): RememberedOptions {
    const { playlistItems: _items, redownload: _redownload, ...rest } = values;
    return {
        ...rest,
        videoQuality: rest.videoQuality.startsWith(formatValuePrefix) ? 'best' : rest.videoQuality,
        audioStream: 'best',
        extras: { ...rest.extras },
        sponsorBlockCategories: [...rest.sponsorBlockCategories],
    };
}

/** Minimal storage, so tests can pass a Map-backed fake instead of localStorage. */
export interface KeyValueStorage {
    getItem(key: string): string | null;
    setItem(key: string, value: string): void;
}

const keyPrefix = 'tubedlfin.options.v1.';

/** {@link OptionsMemory} in the browser's localStorage (per browser, per server origin). */
export class BrowserOptionsMemory implements OptionsMemory {
    constructor(private readonly storage: KeyValueStorage) {}

    load(category: OptionsCategory): Partial<RememberedOptions> | undefined {
        try {
            const text = this.storage.getItem(keyPrefix + category);
            const parsed: unknown = text ? JSON.parse(text) : undefined;
            return typeof parsed === 'object' && parsed !== null
                ? (parsed as Partial<RememberedOptions>)
                : undefined;
        } catch {
            return undefined; // Corrupt or blocked storage: fall back to defaults.
        }
    }

    save(category: OptionsCategory, values: DownloadFormValues): void {
        try {
            this.storage.setItem(keyPrefix + category, JSON.stringify(rememberable(values)));
            this.storage.setItem(`${keyPrefix}library`, values.libraryId);
        } catch {
            // Storage full or disabled: remembering is a convenience, not required.
        }
    }

    lastLibraryId(): string | undefined {
        try {
            return this.storage.getItem(`${keyPrefix}library`) ?? undefined;
        } catch {
            return undefined;
        }
    }
}
