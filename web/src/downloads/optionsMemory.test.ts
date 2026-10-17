import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import type { DownloadFormValues } from './downloadRequest';
import { BrowserOptionsMemory, type KeyValueStorage, rememberable } from './optionsMemory';

class MapStorage implements KeyValueStorage {
    readonly items = new Map<string, string>();
    getItem(key: string): string | null {
        return this.items.get(key) ?? null;
    }
    setItem(key: string, value: string): void {
        this.items.set(key, value);
    }
}

const values: DownloadFormValues = {
    mode: 'Video',
    videoQuality: 'format:137',
    container: 'Mp4',
    preferH264: false,
    audioStream: 'format:251',
    audioFormat: 'Original',
    audioQuality: 'best',
    playlistItems: '1-3',
    libraryId: 'movies',
    libraryFolder: '/media/movies',
    subfolder: 'YouTube',
    naming: 'MovieFolder',
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
    redownload: true,
};

describe('rememberable', () => {
    it('drops what only applies to one link', () => {
        const remembered = rememberable(values);

        assert.equal('playlistItems' in remembered, false);
        assert.equal('redownload' in remembered, false);
        assert.equal(remembered.videoQuality, 'best', 'a format id belongs to one video');
        assert.equal(remembered.audioStream, 'best');
        assert.equal(remembered.subfolder, 'YouTube');
    });

    it('keeps an "up to" quality', () => {
        assert.equal(rememberable({ ...values, videoQuality: '720' }).videoQuality, '720');
    });
});

describe('BrowserOptionsMemory', () => {
    it('keeps music and video apart and remembers the library', () => {
        const memory = new BrowserOptionsMemory(new MapStorage());

        memory.save('video', values);
        memory.save('music', { ...values, mode: 'Audio', libraryId: 'music' });

        assert.equal(memory.load('video')?.mode, 'Video');
        assert.equal(memory.load('music')?.mode, 'Audio');
        assert.equal(memory.lastLibraryId(), 'music');
    });

    it('nothing saved, or corrupt data: undefined', () => {
        const storage = new MapStorage();
        const memory = new BrowserOptionsMemory(storage);

        assert.equal(memory.load('video'), undefined);
        storage.setItem('tubedlfin.options.v1.video', '{not json');
        assert.equal(memory.load('video'), undefined);
        assert.equal(memory.lastLibraryId(), undefined);
    });

    it('storage that throws is ignored', () => {
        const memory = new BrowserOptionsMemory({
            getItem: () => {
                throw new Error('blocked');
            },
            setItem: () => {
                throw new Error('full');
            },
        });

        memory.save('video', values);
        assert.equal(memory.load('video'), undefined);
        assert.equal(memory.lastLibraryId(), undefined);
    });
});
