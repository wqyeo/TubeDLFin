import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import type { MediaFormat, ProbeResult } from '../api/types';
import { estimateSize, freeSpaceProblem, lowSpaceBytes } from './freeSpace';

const format = (FileSize: number | null): MediaFormat => ({
    FormatId: 'x',
    Extension: 'mp4',
    FileSize,
    IsFileSizeApproximate: false,
    HasVideo: true,
    HasAudio: false,
});

const result = (
    VideoFormats: MediaFormat[],
    AudioFormats: MediaFormat[],
    Kind: ProbeResult['Kind'] = 'Video',
): ProbeResult => ({
    Kind,
    Id: 'x',
    Title: 'x',
    VideoFormats,
    AudioFormats,
    Subtitles: [],
    AutomaticCaptions: [],
    Entries: [],
    IsTruncated: false,
    HasPlaylistChoice: false,
    Warnings: [],
});

describe('estimateSize', () => {
    it('largest video plus largest audio', () => {
        assert.equal(
            estimateSize(result([format(100), format(null), format(300)], [format(20), format(10)])),
            320,
        );
    });

    it('unknown for lists and when no sizes are given', () => {
        assert.equal(estimateSize(result([format(100)], [], 'Playlist')), undefined);
        assert.equal(estimateSize(result([format(null)], [])), undefined);
    });
});

describe('freeSpaceProblem', () => {
    it('warns when the estimate does not fit', () => {
        assert.equal(
            freeSpaceProblem(5 * lowSpaceBytes, 6 * lowSpaceBytes),
            'The download is probably larger than the free space.',
        );
    });

    it('warns when little is left', () => {
        assert.equal(freeSpaceProblem(lowSpaceBytes - 1, undefined), 'Less than 2 GB free.');
    });

    it('fine, or unknown', () => {
        assert.equal(freeSpaceProblem(10 * lowSpaceBytes, lowSpaceBytes), undefined);
        assert.equal(freeSpaceProblem(null, lowSpaceBytes), undefined);
    });
});
