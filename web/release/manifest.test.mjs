import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
    addRelease,
    changelogSection,
    compareVersionsDescending,
    fullVersion,
    loadPublishedManifest,
    pluginEntry,
    unreleasedSection,
} from './manifest.mjs';

const meta = {
    guid: '0975c0da-6b11-4771-99fd-7d1176bf1f94',
    name: 'TubeDLFin',
    description: 'Long description.\n',
    overview: 'Short overview',
    owner: 'wqyeo',
    category: 'General',
    version: '1.0.0.0',
    changelog: 'From build.yaml.\n',
    targetAbi: '12.2.0.0',
    timestamp: '2026-10-17T14:34:56Z',
};

const release = {
    sourceUrl:
        'https://gitlab.com/api/v4/projects/1/packages/generic/tubedlfin/1.0.0.0/tubedlfin_1.0.0.0.zip',
    checksum: 'D41D8CD98F00B204E9800998ECF8427E',
};

/** @param {string} version */
const versionOf = (version) => ({
    version,
    changelog: '',
    targetAbi: '12.2.0.0',
    sourceUrl: `https://example.com/${version}.zip`,
    checksum: '0'.repeat(32),
    timestamp: '2026-01-01T00:00:00Z',
});

const changelog = `# Changelog

## [Unreleased]

## [1.0.0] - 2026-10-20

### Added
- Everything.

## [0.9.0] - 2026-10-17

First public build.
`;

describe('fullVersion', () => {
    it('pads to four parts and drops a leading v', () => {
        assert.equal(fullVersion('1.2'), '1.2.0.0');
        assert.equal(fullVersion('v0.9.0'), '0.9.0.0');
        assert.equal(fullVersion('1.0.0.0'), '1.0.0.0');
    });

    it('rejects anything else', () => {
        assert.throws(() => fullVersion('1.0.0-rc1'));
        assert.throws(() => fullVersion('1.0.0.0.0'));
        assert.throws(() => fullVersion(''));
    });
});

describe('compareVersionsDescending', () => {
    it('sorts numerically, newest first', () => {
        assert.deepEqual(['0.9.0.0', '1.10.0.0', '1.2.0.0'].sort(compareVersionsDescending), [
            '1.10.0.0',
            '1.2.0.0',
            '0.9.0.0',
        ]);
    });
});

describe('changelogSection', () => {
    it('returns the section of that version only', () => {
        assert.equal(changelogSection(changelog, '1.0.0.0'), '### Added\n- Everything.');
        assert.equal(changelogSection(changelog, 'v0.9.0'), 'First public build.');
    });

    it('fails for a missing or empty section', () => {
        assert.throws(() => changelogSection(changelog, '2.0.0'), /no "## \[2.0.0\]" section/);
        assert.throws(() => changelogSection('## [1.0.0]\n\n## [0.9.0]\nx', '1.0.0'), /empty/);
    });
});

describe('unreleasedSection', () => {
    it('returns the [Unreleased] notes', () => {
        assert.equal(unreleasedSection('## [Unreleased]\n\n- New.\n\n## [1.0.0] - 2026-10-20\nx'), '- New.');
    });

    it('fails when there are none', () => {
        assert.throws(() => unreleasedSection(changelog), /section for Unreleased is empty/);
    });
});

describe('pluginEntry', () => {
    it('turns JPRM metadata into a manifest entry', () => {
        const entry = pluginEntry(meta, {
            ...release,
            changelog: 'From CHANGELOG.md',
            imageUrl: 'https://x.test/logo.png',
        });

        assert.equal(entry.description, 'Long description.');
        assert.equal(entry.imageUrl, 'https://x.test/logo.png');
        assert.deepEqual(entry.versions, [
            {
                version: '1.0.0.0',
                changelog: 'From CHANGELOG.md',
                targetAbi: '12.2.0.0',
                sourceUrl: release.sourceUrl,
                checksum: 'd41d8cd98f00b204e9800998ecf8427e',
                timestamp: '2026-10-17T14:34:56Z',
            },
        ]);
    });

    it("falls back to build.yaml's changelog and leaves out a missing image", () => {
        const entry = pluginEntry(meta, release);

        assert.equal(entry.versions[0]?.changelog, 'From build.yaml.');
        assert.equal('imageUrl' in entry, false);
    });

    it('rejects a bad checksum or a non-https link', () => {
        assert.throws(() => pluginEntry(meta, { ...release, checksum: 'abc' }), /MD5/);
        assert.throws(() => pluginEntry(meta, { ...release, sourceUrl: 'http://x.test/a.zip' }), /https/);
    });
});

describe('addRelease', () => {
    const entry = pluginEntry(meta, release);

    it('starts an empty manifest', () => {
        assert.deepEqual(addRelease([], entry), [entry]);
    });

    it('keeps older versions, replaces the same version, newest first', () => {
        const old = {
            ...entry,
            overview: 'Old overview',
            versions: [versionOf('0.9.0.0'), versionOf('1.0.0')],
        };

        const [plugin] = addRelease([old], entry);

        assert.equal(plugin?.overview, 'Short overview');
        assert.deepEqual(
            plugin?.versions.map((v) => [v.version, v.sourceUrl]),
            [
                ['1.0.0.0', release.sourceUrl],
                ['0.9.0.0', 'https://example.com/0.9.0.0.zip'],
            ],
        );
    });

    it('leaves other plugins alone', () => {
        const other = {
            ...entry,
            guid: '11111111-1111-1111-1111-111111111111',
            versions: [versionOf('3.0.0.0')],
        };

        const manifest = addRelease([other], entry);

        assert.equal(manifest.length, 2);
        assert.equal(manifest[0], other);
    });
});

describe('loadPublishedManifest', () => {
    /** @param {number} status @param {string} [body] */
    const respond =
        (status, body = '') =>
        async () => ({
            ok: status >= 200 && status < 300,
            status,
            text: async () => body,
        });

    it('reads the published list', async () => {
        assert.deepEqual(await loadPublishedManifest('https://x.test/m.json', respond(200, '[]')), []);
    });

    it('starts empty when nothing is published yet', async () => {
        assert.deepEqual(await loadPublishedManifest('https://x.test/m.json', respond(404)), []);
    });

    it('fails on other errors instead of dropping older versions', async () => {
        await assert.rejects(loadPublishedManifest('https://x.test/m.json', respond(503)), /HTTP 503/);
        await assert.rejects(
            loadPublishedManifest('https://x.test/m.json', respond(200, '{}')),
            /not a list/,
        );
    });
});
