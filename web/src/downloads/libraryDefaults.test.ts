import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import type { LibraryInfo } from '../api/types';
import { categoryOf, libraryDefaults } from './libraryDefaults';

const library = (Type: LibraryInfo['Type'], DefaultNaming: LibraryInfo['DefaultNaming']): LibraryInfo => ({
    Id: 'id',
    Name: 'Library',
    Type,
    DefaultNaming,
    Folders: [{ Path: '/media' }],
});

describe('libraryDefaults', () => {
    it('music: audio, music naming, embedded cover, no .nfo', () => {
        const defaults = libraryDefaults(library('Music', 'Music'));

        assert.equal(categoryOf(library('Music', 'Music')), 'music');
        assert.equal(defaults.mode, 'Audio');
        assert.equal(defaults.naming, 'Music');
        assert.deepEqual(defaults.extras, {
            WriteNfo: false,
            SaveThumbnail: false,
            EmbedThumbnail: true,
            EmbedMetadata: true,
            EmbedChapters: false,
        });
    });

    it('video libraries: video, the library type preset, .nfo and poster', () => {
        const defaults = libraryDefaults(library('Shows', 'TvShow'));

        assert.equal(categoryOf(library('Shows', 'TvShow')), 'video');
        assert.equal(defaults.mode, 'Video');
        assert.equal(defaults.naming, 'TvShow');
        assert.equal(defaults.extras.WriteNfo, true);
        assert.equal(defaults.extras.SaveThumbnail, true);
    });

    it('returns fresh objects', () => {
        const first = libraryDefaults(library('Movies', 'MovieFolder'));
        first.extras.WriteNfo = false;

        assert.equal(libraryDefaults(library('Movies', 'MovieFolder')).extras.WriteNfo, true);
    });
});
