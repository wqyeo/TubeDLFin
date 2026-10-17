// Prints a version's CHANGELOG.md section (the GitLab Release description).
//
//   node release/release-notes.mjs <CHANGELOG.md> <version>
import { readFile } from 'node:fs/promises';
import { changelogSection } from './manifest.mjs';

const [changelogPath, version] = process.argv.slice(2);
if (!changelogPath || !version) {
    throw new Error('Usage: node release/release-notes.mjs <CHANGELOG.md> <version>');
}

console.log(changelogSection(await readFile(changelogPath, 'utf8'), version));
