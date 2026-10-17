import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import type { ProbeResult } from '../api/types';
import { buildDownloadRequest, type DownloadFormValues } from './downloadRequest';

const video: ProbeResult = {
    Kind: 'Video',
    Id: 'jNQXAC9IVRw',
    Title: 'Me at the zoo',
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
    videoQuality: '1080',
    container: 'Mp4',
    preferH264: true,
    audioStream: 'best',
    audioFormat: 'Original',
    audioQuality: '192',
    playlistItems: '1-3',
    libraryId: 'lib',
    libraryFolder: '/media/movies',
    subfolder: '  YouTube ',
    naming: 'MovieFolder',
    customTemplate: '%(title)s.%(ext)s',
    extras: {
        WriteNfo: true,
        SaveThumbnail: true,
        EmbedThumbnail: false,
        EmbedMetadata: true,
        EmbedChapters: true,
    },
    subtitleLanguages: ' en, de ',
    includeAutomaticSubtitles: true,
    embedSubtitles: false,
    sponsorBlockMode: 'Remove',
    sponsorBlockCategories: ['Sponsor'],
    music: {
        Enabled: true,
        SplitArtistTitle: true,
        CleanTitles: true,
        NumberTracks: true,
        PlaylistAsAlbum: false,
        SquareCover: true,
    },
    redownload: false,
};

const probe = { Url: 'https://www.youtube.com/watch?v=jNQXAC9IVRw', NoPlaylist: false };

describe('buildDownloadRequest', () => {
    it('maps a video download', () => {
        const request = buildDownloadRequest(values, probe, video);

        assert.equal(request.Url, probe.Url);
        assert.equal(request.FormatId, null);
        assert.equal(request.PlaylistItems, null, 'items only apply to lists');
        assert.equal(request.Title, 'Me at the zoo');
        assert.deepEqual(request.Profile, {
            Mode: 'Video',
            MaxHeight: 1080,
            Container: 'Mp4',
            PreferH264: true,
            AudioFormat: 'Original',
            AudioQuality: null,
            LibraryId: 'lib',
            LibraryFolder: '/media/movies',
            Subfolder: 'YouTube',
            Naming: 'MovieFolder',
            CustomTemplate: null,
            Extras: values.extras,
            Subtitles: { Languages: 'en,de', IncludeAutomatic: true, Embed: false },
            SponsorBlock: { Mode: 'Remove', Categories: ['Sponsor'] },
            Music: values.music,
        });
    });

    it('passes a chosen format and playlist items', () => {
        const request = buildDownloadRequest(
            { ...values, videoQuality: 'format:137' },
            { ...probe, NoPlaylist: true },
            { ...video, Kind: 'Playlist' },
        );

        assert.equal(request.FormatId, '137');
        assert.equal(request.NoPlaylist, true);
        assert.equal(request.PlaylistItems, '1-3');
        assert.equal(request.Profile.MaxHeight, null);
    });

    it('audio: bitrate only for lossy formats, format from the audio source', () => {
        const mp3 = buildDownloadRequest(
            { ...values, mode: 'Audio', audioFormat: 'Mp3', audioStream: 'format:251' },
            probe,
            video,
        );
        const flac = buildDownloadRequest({ ...values, mode: 'Audio', audioFormat: 'Flac' }, probe, video);

        assert.equal(mp3.Profile.AudioQuality, 192);
        assert.equal(mp3.FormatId, '251');
        assert.equal(mp3.Profile.MaxHeight, null);
        assert.equal(flac.Profile.AudioQuality, null);
    });

    it('sends the custom template only with the custom preset', () => {
        assert.equal(
            buildDownloadRequest({ ...values, naming: 'Custom' }, probe, video).Profile.CustomTemplate,
            '%(title)s.%(ext)s',
        );
    });

    it('turns SponsorBlock off when no category is picked, and subtitles off when blank', () => {
        const request = buildDownloadRequest(
            { ...values, sponsorBlockCategories: [], subtitleLanguages: ' ' },
            probe,
            video,
        );

        assert.equal(request.Profile.SponsorBlock.Mode, 'Off');
        assert.equal(request.Profile.Subtitles.Languages, null);
    });
});
