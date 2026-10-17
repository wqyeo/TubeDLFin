import type { MediaFormat, ProbeResult } from '../api/types';

/** Below this, the form warns even without a size estimate. */
export const lowSpaceBytes = 2 * 1024 ** 3;

/** The size of a single video's best formats (video + audio), if the site gave sizes. */
export function estimateSize(result: ProbeResult): number | undefined {
    if (result.Kind !== 'Video') {
        return undefined;
    }

    const largest = (formats: readonly MediaFormat[]) =>
        formats.reduce<number | undefined>((max, format) => {
            const size = format.FileSize ?? undefined;
            return size !== undefined && (max === undefined || size > max) ? size : max;
        }, undefined);

    const video = largest(result.VideoFormats);
    const audio = largest(result.AudioFormats);
    return video === undefined && audio === undefined ? undefined : (video ?? 0) + (audio ?? 0);
}

/** Why the free space is a problem, or undefined if it isn't (or isn't known). */
export function freeSpaceProblem(
    freeBytes: number | null | undefined,
    estimate: number | undefined,
): string | undefined {
    if (freeBytes == null) {
        return undefined;
    }

    if (estimate !== undefined && estimate > freeBytes) {
        return 'The download is probably larger than the free space.';
    }

    return freeBytes < lowSpaceBytes ? 'Less than 2 GB free.' : undefined;
}
