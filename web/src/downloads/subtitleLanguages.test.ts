import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { parseLanguages, toggleLanguage } from './subtitleLanguages';

describe('subtitle languages', () => {
    it('parses a comma list', () => {
        assert.deepEqual(parseLanguages(' en, de ,,pt-BR '), ['en', 'de', 'pt-BR']);
        assert.deepEqual(parseLanguages(''), []);
    });

    it('toggles a code', () => {
        assert.equal(toggleLanguage('en', 'de'), 'en,de');
        assert.equal(toggleLanguage('en, de', 'en'), 'de');
        assert.equal(toggleLanguage('', 'en'), 'en');
    });
});
