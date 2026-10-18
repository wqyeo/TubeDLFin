// Prints a release tag's notes (the GitLab Release description): its CHANGELOG.md section, or
// for a beta the [Unreleased] section.
//
//   node release/release-notes.mjs <CHANGELOG.md> <tag>
import { readFile } from 'node:fs/promises';
import { parseReleaseTag, releaseNotes } from './channels.mjs';

const [changelogPath, tag] = process.argv.slice(2);
if (!changelogPath || !tag) {
    throw new Error('Usage: node release/release-notes.mjs <CHANGELOG.md> <tag>');
}

console.log(releaseNotes(await readFile(changelogPath, 'utf8'), parseReleaseTag(tag)));
