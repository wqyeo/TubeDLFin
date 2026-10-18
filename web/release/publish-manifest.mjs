// Adds a JPRM build to the published plugin repository manifests: a stable release to
// manifest.json and manifest-beta.json, a beta to manifest-beta.json only (see channels.mjs).
// Run by the "pages" CI job, which republishes the whole Pages site, so both files are always
// written:
//
//   node release/publish-manifest.mjs --tag <vX.Y.Z[-beta.N]> --zip <tubedlfin_X.zip>
//     --source-url <download link> --changelog <CHANGELOG.md> --pages-url <GitLab Pages URL>
//     [--image-url <logo URL>] --out-dir <public>
//
// The zip's meta.json comes from JPRM's <zip>.meta.json next to it; the checksum from its .md5sum.
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { parseArgs } from 'node:util';
import {
    betaManifestFile,
    parseReleaseTag,
    publishRelease,
    releaseNotes,
    stableManifestFile,
} from './channels.mjs';
import { loadPublishedManifest, pluginEntry } from './manifest.mjs';

const { values } = parseArgs({
    options: {
        tag: { type: 'string' },
        zip: { type: 'string' },
        'source-url': { type: 'string' },
        changelog: { type: 'string' },
        'pages-url': { type: 'string' },
        'image-url': { type: 'string' },
        'out-dir': { type: 'string' },
    },
    strict: true,
});

for (const required of ['tag', 'zip', 'source-url', 'changelog', 'pages-url', 'out-dir']) {
    if (!values[required]) {
        throw new Error(`--${required} is required`);
    }
}

const release = parseReleaseTag(values.tag);
const meta = JSON.parse(await readFile(`${values.zip}.meta.json`, 'utf8'));
const checksum = (await readFile(`${values.zip}.md5sum`, 'utf8')).trim().split(/\s+/)[0];

const entry = pluginEntry(meta, {
    sourceUrl: values['source-url'],
    checksum,
    changelog: releaseNotes(await readFile(values.changelog, 'utf8'), release),
    imageUrl: values['image-url'],
});
const [stable, beta] = await Promise.all(
    [stableManifestFile, betaManifestFile].map((file) =>
        loadPublishedManifest(`${values['pages-url']}/${file}`, fetch),
    ),
);
const published = publishRelease(release, entry, stable, beta);

await mkdir(values['out-dir'], { recursive: true });
for (const [file, manifest] of [
    [stableManifestFile, published.stable],
    [betaManifestFile, published.beta],
]) {
    const out = join(values['out-dir'], file);
    await writeFile(out, `${JSON.stringify(manifest, null, 2)}\n`);
    const versions = manifest.find((plugin) => plugin.guid === meta.guid)?.versions.map((v) => v.version);
    console.log(`${out}: ${meta.name} ${versions?.join(', ')}`);
}
