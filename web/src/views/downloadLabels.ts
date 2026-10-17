import type { DownloadState } from '../api/types';

/** How each job state is shown: its label and the colour class of its badge. */
export const stateLabels: Record<
    DownloadState,
    { label: string; tone: 'neutral' | 'busy' | 'good' | 'warn' | 'bad' }
> = {
    Queued: { label: 'Queued', tone: 'neutral' },
    Downloading: { label: 'Downloading', tone: 'busy' },
    Processing: { label: 'Processing', tone: 'busy' },
    Completed: { label: 'Completed', tone: 'good' },
    CompletedWithErrors: { label: 'Completed with errors', tone: 'warn' },
    AlreadyDownloaded: { label: 'Already downloaded', tone: 'neutral' },
    Failed: { label: 'Failed', tone: 'bad' },
    Cancelled: { label: 'Cancelled', tone: 'neutral' },
};

// yt-dlp's post-processor names.
const stageLabels: Record<string, string> = {
    Merger: 'Merging video and audio',
    ExtractAudio: 'Converting audio',
    VideoRemuxer: 'Remuxing',
    VideoConvertor: 'Converting video',
    MoveFiles: 'Moving into the library',
    Metadata: 'Writing tags',
    EmbedThumbnail: 'Embedding thumbnail',
    EmbedSubtitle: 'Embedding subtitles',
    FixupM3u8: 'Fixing up the file',
    FixupM4a: 'Fixing up the file',
    FixupStretched: 'Fixing up the file',
    FixupDuplicateMoov: 'Fixing up the file',
    FixupTimestamp: 'Fixing up the file',
    FixupDuration: 'Fixing up the file',
    Concat: 'Joining files',
};

/** "Merger" → "Merging video and audio"; unknown steps keep yt-dlp's name. */
export function stageLabel(stage: string): string {
    return stageLabels[stage] ?? stage;
}
