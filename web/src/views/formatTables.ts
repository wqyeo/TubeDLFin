import type { MediaFormat } from '../api/types';
import { disclosure, element, row, table } from './elements';
import { codecName, formatBitrate, formatResolution, formatSize } from './format';

/** The video and audio format lists, best first. */
export function formatTables(
    videoFormats: readonly MediaFormat[],
    audioFormats: readonly MediaFormat[],
): HTMLElement[] {
    const blocks: HTMLElement[] = [];

    if (videoFormats.length > 0) {
        blocks.push(
            disclosure(
                `Video formats (${videoFormats.length})`,
                false,
                table(
                    ['Quality', 'Codec', 'Audio', 'Bitrate', 'Size', 'Container', 'Format'],
                    videoFormats.map((format) =>
                        row(
                            quality(format),
                            codecName(format.VideoCodec),
                            format.HasAudio ? 'Included' : 'Added separately',
                            formatBitrate(format.Bitrate),
                            formatSize(format.FileSize, format.IsFileSizeApproximate),
                            format.Extension,
                            formatId(format),
                        ),
                    ),
                ),
            ),
        );
    }

    if (audioFormats.length > 0) {
        blocks.push(
            disclosure(
                `Audio formats (${audioFormats.length})`,
                videoFormats.length === 0,
                table(
                    ['Codec', 'Bitrate', 'Language', 'Size', 'Container', 'Note', 'Format'],
                    audioFormats.map((format) =>
                        row(
                            codecName(format.AudioCodec),
                            formatBitrate(format.AudioBitrate ?? format.Bitrate),
                            format.Language ?? '–',
                            formatSize(format.FileSize, format.IsFileSizeApproximate),
                            format.Extension,
                            format.Note ?? '–',
                            formatId(format),
                        ),
                    ),
                ),
            ),
        );
    }

    return blocks;
}

function quality(format: MediaFormat): string {
    const resolution = formatResolution(format.Height, format.Fps);
    return format.DynamicRange && format.DynamicRange !== 'SDR'
        ? `${resolution} ${format.DynamicRange}`
        : resolution;
}

function formatId(format: MediaFormat): HTMLElement {
    // Streamed (HLS/DASH) formats download in fragments; worth knowing when picking one.
    const streamed = format.Protocol?.startsWith('m3u8') || format.Protocol?.startsWith('http_dash');
    return element('code', streamed ? `${format.FormatId} (stream)` : format.FormatId);
}
