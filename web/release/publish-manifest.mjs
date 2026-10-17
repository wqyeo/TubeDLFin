// Adds a JPRM build to the published plugin repository manifest. Run by the "pages" CI job:
//
//   node release/publish-manifest.mjs --zip <tubedlfin_X.zip> --source-url <download link>
//     --changelog <CHANGELOG.md> --previous <published manifest.json URL>
//     [--image-url <logo URL>] --out <public/manifest.json>
//
// The zip's meta.json comes from JPRM's <zip>.meta.json next to it; the checksum from its .md5sum.
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname } from 'node:path';
import { parseArgs } from 'node:util';
import { addRelease, changelogSection, loadPublishedManifest, pluginEntry } from './manifest.mjs';

const { values } = parseArgs({
    options: {
        zip: { type: 'string' },
        'source-url': { type: 'string' },
        changelog: { type: 'string' },
        previous: { type: 'string' },
        'image-url': { type: 'string' },
        out: { type: 'string' },
    },
    strict: true,
});

for (const required of ['zip', 'source-url', 'changelog', 'previous', 'out']) {
    if (!values[required]) {
        throw new Error(`--${required} is required`);
    }
}

const meta = JSON.parse(await readFile(`${values.zip}.meta.json`, 'utf8'));
const checksum = (await readFile(`${values.zip}.md5sum`, 'utf8')).trim().split(/\s+/)[0];
const changelog = changelogSection(await readFile(values.changelog, 'utf8'), meta.version);

const entry = pluginEntry(meta, {
    sourceUrl: values['source-url'],
    checksum,
    changelog,
    imageUrl: values['image-url'],
});
const manifest = addRelease(await loadPublishedManifest(values.previous, fetch), entry);

await mkdir(dirname(values.out), { recursive: true });
await writeFile(values.out, `${JSON.stringify(manifest, null, 2)}\n`);

const versions = manifest.find((plugin) => plugin.guid === meta.guid)?.versions.map((v) => v.version);
console.log(`${values.out}: ${meta.name} ${versions?.join(', ')}`);
