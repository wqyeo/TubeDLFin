// Builds the Jellyfin plugin repository manifest (the list behind Dashboard → Plugins →
// Repositories). Pure functions; publish-manifest.mjs and release-notes.mjs do the I/O.
// JPRM's own "repo add" expects to host the zips next to the manifest, but the zips live in
// GitLab's package registry and the manifest on GitLab Pages, so this replaces it.

/**
 * @typedef {{ guid: string, name: string, description: string, overview: string, owner: string,
 *   category: string, version: string, changelog: string, targetAbi: string, timestamp: string }} PluginMeta
 *   JPRM's meta.json.
 * @typedef {{ version: string, changelog: string, targetAbi: string, sourceUrl: string,
 *   checksum: string, timestamp: string }} PluginVersion
 * @typedef {{ guid: string, name: string, description: string, overview: string, owner: string,
 *   category: string, imageUrl?: string, versions: PluginVersion[] }} PluginEntry
 */

/**
 * "1.2" → "1.2.0.0". Jellyfin compares four-part versions and ignores entries without them.
 * @param {string} version
 * @returns {string}
 */
export function fullVersion(version) {
    const parts = version.trim().replace(/^v/, '').split('.');
    if (parts.length > 4 || parts.some((part) => !/^\d+$/.test(part))) {
        throw new Error(`Not a version: "${version}"`);
    }

    while (parts.length < 4) {
        parts.push('0');
    }

    return parts.map(Number).join('.');
}

/**
 * Newest first.
 * @param {string} a
 * @param {string} b
 * @returns {number}
 */
export function compareVersionsDescending(a, b) {
    const left = fullVersion(a).split('.').map(Number);
    const right = fullVersion(b).split('.').map(Number);
    for (let i = 0; i < 4; i++) {
        const difference = (right[i] ?? 0) - (left[i] ?? 0);
        if (difference !== 0) {
            return difference;
        }
    }

    return 0;
}

/**
 * The body of the CHANGELOG.md section for a version (`## [1.0.0] - 2026-10-17`), trimmed.
 * @param {string} markdown
 * @param {string} version
 * @returns {string}
 */
export function changelogSection(markdown, version) {
    const wanted = fullVersion(version);
    const lines = markdown.split(/\r?\n/);
    const start = lines.findIndex((line) => {
        const heading = /^## \[(?<version>[\d.]+)\]/.exec(line);
        return heading?.groups?.version !== undefined && fullVersion(heading.groups.version) === wanted;
    });
    if (start < 0) {
        throw new Error(`CHANGELOG.md has no "## [${version}]" section`);
    }

    const end = lines.findIndex((line, index) => index > start && /^## /.test(line));
    const body = lines
        .slice(start + 1, end < 0 ? undefined : end)
        .join('\n')
        .trim();
    if (body === '') {
        throw new Error(`The CHANGELOG.md section for ${version} is empty`);
    }

    return body;
}

/**
 * One plugin with one version, from a JPRM build.
 * @param {PluginMeta} meta
 * @param {{ sourceUrl: string, checksum: string, changelog?: string, imageUrl?: string }} release
 * @returns {PluginEntry}
 */
export function pluginEntry(meta, release) {
    if (!/^[0-9a-f]{32}$/i.test(release.checksum)) {
        throw new Error(`Not an MD5 checksum: "${release.checksum}"`);
    }

    if (!/^https:\/\//.test(release.sourceUrl)) {
        throw new Error(`The zip must be downloadable over https: "${release.sourceUrl}"`);
    }

    /** @type {PluginEntry} */
    const entry = {
        guid: meta.guid,
        name: meta.name,
        description: meta.description.trim(),
        overview: meta.overview.trim(),
        owner: meta.owner,
        category: meta.category,
        versions: [
            {
                version: fullVersion(meta.version),
                changelog: (release.changelog ?? meta.changelog).trim(),
                targetAbi: meta.targetAbi,
                sourceUrl: release.sourceUrl,
                checksum: release.checksum.toLowerCase(),
                timestamp: meta.timestamp,
            },
        ],
    };
    if (release.imageUrl) {
        entry.imageUrl = release.imageUrl;
    }

    return entry;
}

/**
 * The manifest with the plugin's details updated and its versions merged: a version that was
 * published before is replaced, the others are kept, newest first. Other plugins are untouched.
 * @param {PluginEntry[]} manifest
 * @param {PluginEntry} entry
 * @returns {PluginEntry[]}
 */
export function addRelease(manifest, entry) {
    const guid = entry.guid.toLowerCase();
    const existing = manifest.find((plugin) => plugin.guid.toLowerCase() === guid);
    const added = new Set(entry.versions.map((v) => v.version));
    const versions = [
        ...entry.versions,
        ...(existing?.versions ?? []).filter((v) => !added.has(fullVersion(v.version))),
    ].sort((a, b) => compareVersionsDescending(a.version, b.version));

    const updated = { ...existing, ...entry, versions };
    return existing
        ? manifest.map((plugin) => (plugin === existing ? updated : plugin))
        : [...manifest, updated];
}

/**
 * The manifest published so far: empty if there is none yet (404). Anything else is an error,
 * so a failed download never publishes a manifest that forgot the older versions.
 * @param {string} url
 * @param {(url: string) => Promise<{ ok: boolean, status: number, text(): Promise<string> }>} fetchFn
 * @returns {Promise<PluginEntry[]>}
 */
export async function loadPublishedManifest(url, fetchFn) {
    const response = await fetchFn(url);
    if (response.status === 404) {
        return [];
    }

    if (!response.ok) {
        throw new Error(`Could not read the published manifest ${url}: HTTP ${response.status}`);
    }

    const manifest = JSON.parse(await response.text());
    if (!Array.isArray(manifest)) {
        throw new Error(`The published manifest ${url} is not a list`);
    }

    return manifest;
}
