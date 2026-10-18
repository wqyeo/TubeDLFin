// Release channels. A `vX.Y.Z` tag is a stable release, published in both plugin repositories;
// a `vX.Y.Z-beta.N` tag is a beta, published only in the beta repository. The beta repository
// lists every version, so a server with it updates to whichever is newest, beta or stable.
//
// Jellyfin plugin versions are four numbers, so the fourth counts builds: beta N of 1.1.0 is
// 1.1.0.N, and the stable 1.1.0 after it must be higher still (1.1.0.N+1) for beta servers to
// update to it. A stable release without betas stays X.Y.Z.0. Pure functions; the scripts do
// the I/O.
import {
    addRelease,
    changelogSection,
    compareVersionsDescending,
    fullVersion,
    unreleasedSection,
} from './manifest.mjs';

/** @typedef {import('./manifest.mjs').PluginEntry} PluginEntry */

/**
 * @typedef {{ tag: string, version: string, beta: number | null }} ReleaseTag
 *   `version` is the tag's X.Y.Z; `beta` its N, or null for a stable release.
 */

export const stableManifestFile = 'manifest.json';
export const betaManifestFile = 'manifest-beta.json';

/**
 * @param {string} tag
 * @returns {ReleaseTag}
 */
export function parseReleaseTag(tag) {
    const match = /^v(?<version>\d+\.\d+\.\d+)(?:-beta\.(?<beta>[1-9]\d*))?$/.exec(tag);
    if (!match?.groups?.version) {
        throw new Error(`Not a release tag: "${tag}" (expected vX.Y.Z or vX.Y.Z-beta.N)`);
    }

    const beta = match.groups.beta;
    return { tag, version: match.groups.version, beta: beta === undefined ? null : Number(beta) };
}

/**
 * The four-part version the tag publishes: Directory.Build.props' version, checked against the tag.
 * @param {ReleaseTag} release
 * @param {string} propsVersion
 * @returns {string}
 */
export function releaseVersion(release, propsVersion) {
    const version = fullVersion(propsVersion);
    const [major, minor, patch, build] = version.split('.');
    if (`${major}.${minor}.${patch}` !== release.version) {
        throw new Error(`Tag ${release.tag} does not match version ${version} in Directory.Build.props`);
    }

    if (release.beta !== null && Number(build) !== release.beta) {
        throw new Error(
            `Tag ${release.tag} needs version ${release.version}.${release.beta} in Directory.Build.props, not ${version}`,
        );
    }

    return version;
}

/**
 * Stops a release that would strand beta servers: a stable version that isn't above its betas
 * (beta servers would never update to it), or a beta of a version that is already out.
 * @param {ReleaseTag} release
 * @param {string} version
 * @param {string} guid
 * @param {PluginEntry[]} stable The published stable manifest.
 * @param {PluginEntry[]} beta The published beta manifest.
 */
export function assertPublishable(release, version, guid, stable, beta) {
    const stableVersions = publishedVersions(stable, guid);
    const ofThisRelease = (/** @type {string} */ v) => v.split('.').slice(0, 3).join('.') === release.version;

    if (release.beta !== null) {
        const released = stableVersions.find(ofThisRelease);
        if (released) {
            throw new Error(
                `${release.version} is already released as ${released}: number the beta as the next version`,
            );
        }

        return;
    }

    const newestBeta = publishedVersions(beta, guid)
        .filter((v) => ofThisRelease(v) && !stableVersions.includes(v))
        .sort(compareVersionsDescending)[0];
    if (newestBeta && compareVersionsDescending(newestBeta, version) <= 0) {
        const next = Number(newestBeta.split('.')[3]) + 1;
        throw new Error(
            `Beta ${newestBeta} is published, so ${release.tag} needs a higher version for beta servers to update to it: set ${release.version}.${next}`,
        );
    }
}

/**
 * The release notes (GitLab Release and Jellyfin's catalog): the version's CHANGELOG.md section,
 * or for a beta the `[Unreleased]` section with a note that it is a beta.
 * @param {string} changelog
 * @param {ReleaseTag} release
 * @returns {string}
 */
export function releaseNotes(changelog, release) {
    if (release.beta === null) {
        return changelogSection(changelog, release.version);
    }

    return `Beta ${release.beta} of ${release.version}, for testing. Only servers with the TubeDLFin beta repository are offered it.\n\n${unreleasedSection(changelog)}`;
}

/**
 * Both manifests after the release. A beta leaves the stable one as it was; the beta one always
 * gets the stable versions too, so it works on its own or next to the stable repository. A beta
 * refuses an empty stable manifest: it rewrites manifest.json too, and must not empty it because
 * the old one couldn't be read.
 * @param {ReleaseTag} release
 * @param {PluginEntry} entry
 * @param {PluginEntry[]} stable The published stable manifest.
 * @param {PluginEntry[]} beta The published beta manifest.
 * @returns {{ stable: PluginEntry[], beta: PluginEntry[] }}
 */
export function publishRelease(release, entry, stable, beta) {
    if (release.beta !== null && stable.length === 0) {
        throw new Error('The stable manifest is empty or missing: publish a stable release before a beta');
    }

    const everything = stable.reduce(addRelease, beta);
    return {
        stable: release.beta === null ? addRelease(stable, entry) : stable,
        beta: addRelease(everything, entry),
    };
}

/**
 * @param {PluginEntry[]} manifest
 * @param {string} guid
 * @returns {string[]}
 */
function publishedVersions(manifest, guid) {
    const plugin = manifest.find((p) => p.guid.toLowerCase() === guid.toLowerCase());
    return plugin?.versions.map((v) => fullVersion(v.version)) ?? [];
}
