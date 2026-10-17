import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
    codecName,
    formatCount,
    formatDuration,
    formatResolution,
    formatSize,
    safeImageUrl,
    safeLinkUrl,
} from './format';

describe('format', () => {
    it('codec names', () => {
        assert.equal(codecName('avc1.640028'), 'H.264');
        assert.equal(codecName('vp09.00.40.08'), 'VP9');
        assert.equal(codecName('mp4a.40.2'), 'AAC');
        assert.equal(codecName('weird'), 'weird');
        assert.equal(codecName(null), '–');
    });

    it('durations', () => {
        assert.equal(formatDuration(65), '1:05');
        assert.equal(formatDuration(3725), '1:02:05');
        assert.equal(formatDuration(undefined), '–');
    });

    it('sizes and counts', () => {
        assert.equal(formatSize(309288), '302 KB');
        assert.equal(formatSize(1536, true), '~1.5 KB');
        assert.equal(formatCount(1, 'item'), '1 item');
        assert.equal(formatCount(100, 'item', true), '100+ items');
        assert.equal(formatResolution(1080, 60), '1080p60');
    });

    it('only web links and https images', () => {
        assert.equal(safeLinkUrl('javascript:alert(1)'), undefined);
        assert.equal(safeLinkUrl('https://a.example/'), 'https://a.example/');
        assert.equal(safeImageUrl('http://a.example/i.jpg'), undefined);
    });
});
