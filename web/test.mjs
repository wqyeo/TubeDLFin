// Runs the unit tests with Node's built-in test runner: src/**/*.test.ts (the page) and
// release/*.test.mjs (the release scripts). esbuild bundles the page's test files first, because
// Node can't resolve the extensionless imports the page uses; the release scripts run as they are.
import { spawnSync } from 'node:child_process';
import { readdir, rm } from 'node:fs/promises';
import { join } from 'node:path';
import { build } from 'esbuild';

const outDir = '.test-dist';
const entryPoints = (await readdir('src', { recursive: true }))
    .filter((file) => file.endsWith('.test.ts'))
    .map((file) => join('src', file));

await rm(outDir, { recursive: true, force: true });
await build({
    entryPoints,
    outdir: outDir,
    outExtension: { '.js': '.mjs' },
    bundle: true,
    platform: 'node',
    format: 'esm',
    target: 'node24',
    logLevel: 'warning',
});

// src/a/b.test.ts → .test-dist/a/b.test.mjs
const testFiles = [
    ...entryPoints.map((file) => join(outDir, file.slice('src/'.length).replace(/\.ts$/, '.mjs'))),
    ...(await readdir('release'))
        .filter((file) => file.endsWith('.test.mjs'))
        .map((file) => join('release', file)),
];
const result = spawnSync(process.execPath, ['--test', '--test-reporter=dot', ...testFiles], {
    stdio: 'inherit',
});
process.exit(result.status ?? 1);
