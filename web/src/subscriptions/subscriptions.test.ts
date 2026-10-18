import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import type { ProbeResult, SubscriptionInfo } from '../api/types';
import type { DownloadFormValues } from '../downloads/downloadRequest';
import { SubscriptionDirectory } from './subscriptionDirectory';
import { buildSubscriptionRequest, suggestedName } from './subscriptionRequest';
import { checkSummary, countsSummary, intervalLabel } from './subscriptionText';

const channel: ProbeResult = {
    Kind: 'Channel',
    Id: 'UCSMOQeBJ2RAnuFungnQOxLg',
    Title: 'Blender - Videos',
    Uploader: 'Blender',
    VideoFormats: [],
    AudioFormats: [],
    Subtitles: [],
    AutomaticCaptions: [],
    Entries: [],
    IsTruncated: false,
    HasPlaylistChoice: false,
    Warnings: [],
};

const values: DownloadFormValues = {
    mode: 'Video',
    videoQuality: 'format:137',
    container: 'Mkv',
    preferH264: false,
    audioStream: 'format:251',
    audioFormat: 'Original',
    audioQuality: 'best',
    playlistItems: '1-3',
    libraryId: 'lib',
    libraryFolder: '/media/shows',
    subfolder: '',
    naming: 'TvShow',
    customTemplate: '',
    extras: {
        WriteNfo: true,
        SaveThumbnail: true,
        EmbedThumbnail: false,
        EmbedMetadata: true,
        EmbedChapters: true,
    },
    subtitleLanguages: 'en',
    includeAutomaticSubtitles: false,
    embedSubtitles: false,
    sponsorBlockMode: 'Remove',
    sponsorBlockCategories: ['Sponsor'],
    music: {
        Enabled: false,
        SplitArtistTitle: true,
        CleanTitles: true,
        NumberTracks: true,
        PlaylistAsAlbum: false,
        SquareCover: true,
    },
    redownload: true,
};

const subscription: SubscriptionInfo = {
    Id: '8f1d2a3b4c5d4e6f8a9b0c1d2e3f4a5b',
    Name: 'Blender',
    Url: 'https://www.youtube.com/@BlenderOfficial/videos',
    LibraryName: 'Shows',
    IntervalHours: 6,
    Paused: false,
    CreatedAt: '2026-10-18T08:00:00Z',
    NextCheckAt: '2026-10-18T14:00:00Z',
    Downloaded: 0,
    Queued: 0,
    GivenUp: 0,
    Profile: {} as SubscriptionInfo['Profile'],
};

describe('buildSubscriptionRequest', () => {
    it('keeps the profile but drops one-off choices', () => {
        const request = buildSubscriptionRequest(
            values,
            { Url: 'https://www.youtube.com/@BlenderOfficial', NoPlaylist: false },
            channel,
            '  ',
            12,
        );

        assert.equal(request.Url, 'https://www.youtube.com/@BlenderOfficial');
        assert.equal(request.Name, null);
        assert.equal(request.IntervalHours, 12);
        assert.equal(request.Profile.Naming, 'TvShow');
        assert.equal(request.Profile.MaxHeight, null); // a format of one video → best
        assert.deepEqual(request.Profile.SponsorBlock, { Mode: 'Remove', Categories: ['Sponsor'] });
    });

    it('trims the name', () => {
        const request = buildSubscriptionRequest(
            values,
            { Url: 'x', NoPlaylist: false },
            channel,
            ' Talks ',
            6,
        );

        assert.equal(request.Name, 'Talks');
    });
});

describe('suggestedName', () => {
    it("uses the channel's name, or a playlist's title", () => {
        assert.equal(suggestedName(channel), 'Blender');
        assert.equal(suggestedName({ ...channel, Kind: 'Playlist', Title: 'Conference' }), 'Conference');
    });
});

describe('intervalLabel', () => {
    it('reads naturally', () => {
        assert.deepEqual([1, 6, 24, 48, 168, 12].map(intervalLabel), [
            'every hour',
            'every 6 hours',
            'every day',
            'every 2 days',
            'every week',
            'every 12 hours',
        ]);
    });
});

describe('checkSummary', () => {
    const now = new Date('2026-10-18T10:00:00Z');
    const time = (iso: string) => iso.slice(11, 16);

    it('shows the last and next check', () => {
        assert.equal(
            checkSummary({ ...subscription, LastCheckedAt: '2026-10-18T08:00:00Z' }, now, time),
            'Last checked 08:00 · next check 14:00',
        );
    });

    it('says when a check is overdue or paused', () => {
        assert.equal(
            checkSummary({ ...subscription, NextCheckAt: '2026-10-18T09:00:00Z' }, now, time),
            'Not checked yet · next check within 15 minutes',
        );
        assert.equal(checkSummary({ ...subscription, Paused: true }, now, time), 'Not checked yet · paused');
    });
});

describe('countsSummary', () => {
    it('leaves out zeros', () => {
        assert.equal(countsSummary(subscription), '0 downloaded');
        assert.equal(
            countsSummary({ ...subscription, Downloaded: 3, Queued: 1, GivenUp: 2 }),
            '3 downloaded · 1 in the queue · 2 failed 3 times, not tried again',
        );
    });
});

describe('SubscriptionDirectory', () => {
    it('names subscription jobs, whatever the id format', () => {
        const directory = new SubscriptionDirectory();
        directory.update([{ ...subscription, Id: '8f1d2a3b-4c5d-4e6f-8a9b-0c1d2e3f4a5b' }]);

        assert.equal(
            directory.describe('Subscription:8f1d2a3b4c5d4e6f8a9b0c1d2e3f4a5b'),
            'Subscription "Blender"',
        );
        assert.equal(
            directory.describe('Subscription:00000000000000000000000000000000'),
            'From a subscription',
        );
        assert.equal(directory.describe('Manual'), undefined);
    });
});
