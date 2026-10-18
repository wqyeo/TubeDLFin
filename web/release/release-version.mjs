// Prints the four-part version a release tag publishes, after checking it against
// Directory.Build.props and the published manifests (see channels.mjs). Run by the "package" CI job:
//
//   node release/release-version.mjs --tag <vX.Y.Z[-beta.N]> --props-version <Version>
//     --guid <plugin guid> --pages-url <GitLab Pages URL>
import { parseArgs } from 'node:util';
import {
    assertPublishable,
    betaManifestFile,
    parseReleaseTag,
    releaseVersion,
    stableManifestFile,
} from './channels.mjs';
import { loadPublishedManifest } from './manifest.mjs';

const { values } = parseArgs({
    options: {
        tag: { type: 'string' },
        'props-version': { type: 'string' },
        guid: { type: 'string' },
        'pages-url': { type: 'string' },
    },
    strict: true,
});

for (const required of ['tag', 'props-version', 'guid', 'pages-url']) {
    if (!values[required]) {
        throw new Error(`--${required} is required`);
    }
}

const release = parseReleaseTag(values.tag);
const version = releaseVersion(release, values['props-version']);
const [stable, beta] = await Promise.all(
    [stableManifestFile, betaManifestFile].map((file) =>
        loadPublishedManifest(`${values['pages-url']}/${file}`, fetch),
    ),
);
assertPublishable(release, version, values.guid, stable, beta);

console.log(version);
