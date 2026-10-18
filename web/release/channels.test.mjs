import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
    assertPublishable,
    parseReleaseTag,
    publishRelease,
    releaseNotes,
    releaseVersion,
} from './channels.mjs';

const guid = '0975c0da-6b11-4771-99fd-7d1176bf1f94';

/** @param {...string} versions */
const plugin = (...versions) => ({
    guid,
    name: 'TubeDLFin',
    description: '',
    overview: '',
    owner: 'wqyeo',
    category: 'General',
    versions: versions.map((version) => ({
        version,
        changelog: '',
        targetAbi: '12.2.0.0',
        sourceUrl: `https://example.com/${version}.zip`,
        checksum: '0'.repeat(32),
        timestamp: '2026-01-01T00:00:00Z',
    })),
});

/** @param {{ versions: { version: string }[] }[]} manifest */
const versionsIn = (manifest) => manifest[0]?.versions.map((v) => v.version);

const stable = parseReleaseTag('v1.1.0');
const beta2 = parseReleaseTag('v1.1.0-beta.2');

describe('parseReleaseTag', () => {
    it('reads stable and beta tags', () => {
        assert.deepEqual(stable, { tag: 'v1.1.0', version: '1.1.0', beta: null });
        assert.deepEqual(beta2, { tag: 'v1.1.0-beta.2', version: '1.1.0', beta: 2 });
    });

    it('rejects other tags', () => {
        for (const tag of ['1.1.0', 'v1.1', 'v1.1.0-rc.1', 'v1.1.0-beta.0', 'v1.1.0-beta', 'v1.1.0.1']) {
            assert.throws(() => parseReleaseTag(tag), /Not a release tag/, tag);
        }
    });
});

describe('releaseVersion', () => {
    it("takes Directory.Build.props' version when it matches the tag", () => {
        assert.equal(releaseVersion(stable, '1.1.0.0'), '1.1.0.0');
        assert.equal(releaseVersion(stable, '1.1.0.3'), '1.1.0.3');
        assert.equal(releaseVersion(beta2, '1.1.0.2'), '1.1.0.2');
    });

    it('rejects a version that does not match', () => {
        assert.throws(() => releaseVersion(stable, '1.0.0.0'), /does not match/);
        assert.throws(() => releaseVersion(beta2, '1.1.0.1'), /needs version 1\.1\.0\.2/);
    });
});

describe('assertPublishable', () => {
    it('lets a stable release out above its betas', () => {
        assertPublishable(
            stable,
            '1.1.0.3',
            guid,
            [plugin('1.0.0.0')],
            [plugin('1.1.0.2', '1.1.0.1', '1.0.0.0')],
        );
        assertPublishable(stable, '1.1.0.0', guid, [plugin('1.0.0.0')], [plugin('1.2.0.1', '1.0.0.0')]);
    });

    it('stops a stable release that beta servers would not update to', () => {
        assert.throws(
            () =>
                assertPublishable(
                    stable,
                    '1.1.0.0',
                    guid,
                    [plugin('1.0.0.0')],
                    [plugin('1.1.0.2', '1.1.0.1')],
                ),
            /Beta 1\.1\.0\.2 is published.*set 1\.1\.0\.3/,
        );
    });

    it('allows publishing the same stable release again', () => {
        assertPublishable(stable, '1.1.0.3', guid, [plugin('1.1.0.3')], [plugin('1.1.0.3', '1.1.0.2')]);
    });

    it('stops a beta of a version that is already out', () => {
        assertPublishable(beta2, '1.1.0.2', guid, [plugin('1.0.0.0')], [plugin('1.1.0.1')]);
        assert.throws(
            () => assertPublishable(beta2, '1.1.0.2', guid, [plugin('1.1.0.0')], []),
            /already released as 1\.1\.0\.0/,
        );
    });
});

describe('releaseNotes', () => {
    const changelog = '# Changelog\n\n## [Unreleased]\n\n- New.\n\n## [1.1.0] - 2026-10-20\n\n- Released.\n';

    it("uses the version's section for a stable release", () => {
        assert.equal(releaseNotes(changelog, stable), '- Released.');
    });

    it('uses [Unreleased] for a beta, saying it is one', () => {
        assert.equal(
            releaseNotes(changelog, beta2),
            'Beta 2 of 1.1.0, for testing. Only servers with the TubeDLFin beta repository are offered it.\n\n- New.',
        );
    });
});

describe('publishRelease', () => {
    it('adds a beta to the beta manifest only, with the stable versions', () => {
        const published = publishRelease(beta2, plugin('1.1.0.2'), [plugin('1.0.0.0')], [plugin('1.1.0.1')]);

        assert.deepEqual(versionsIn(published.stable), ['1.0.0.0']);
        assert.deepEqual(versionsIn(published.beta), ['1.1.0.2', '1.1.0.1', '1.0.0.0']);
    });

    it('starts the beta manifest from the stable one', () => {
        const published = publishRelease(beta2, plugin('1.1.0.2'), [plugin('1.0.0.0', '0.9.0.0')], []);

        assert.deepEqual(versionsIn(published.beta), ['1.1.0.2', '1.0.0.0', '0.9.0.0']);
    });

    it('adds a stable release to both', () => {
        const published = publishRelease(
            stable,
            plugin('1.1.0.3'),
            [plugin('1.0.0.0')],
            [plugin('1.1.0.2', '1.0.0.0')],
        );

        assert.deepEqual(versionsIn(published.stable), ['1.1.0.3', '1.0.0.0']);
        assert.deepEqual(versionsIn(published.beta), ['1.1.0.3', '1.1.0.2', '1.0.0.0']);
    });

    it('refuses a beta when the stable manifest is empty', () => {
        assert.throws(() => publishRelease(beta2, plugin('1.1.0.2'), [], []), /stable manifest is empty/);
    });
});
