// Runs the unit tests (src/**/*.test.ts) with Node's built-in test runner. esbuild bundles each
// test file first, because Node can't resolve the extensionless imports the page uses.
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
const testFiles = entryPoints.map((file) => join(outDir, file.slice('src/'.length).replace(/\.ts$/, '.mjs')));
const result = spawnSync(process.execPath, ['--test', '--test-reporter=dot', ...testFiles], {
    stdio: 'inherit',
});
process.exit(result.status ?? 1);
