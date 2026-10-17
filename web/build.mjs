// Bundles the dashboard page into dist/, which the plugin project embeds (see Web.targets).
import { copyFile, mkdir, rm } from 'node:fs/promises';
import { build } from 'esbuild';

const outDir = 'dist';

await rm(outDir, { recursive: true, force: true });
await mkdir(outDir, { recursive: true });

await build({
    entryPoints: ['src/main.ts'],
    outfile: `${outDir}/tubedlfin.js`,
    bundle: true,
    // Jellyfin imports the page controller as an ES module and calls `new module.default(view, params)`.
    format: 'esm',
    target: 'es2020',
    minify: true,
    sourcemap: false,
    legalComments: 'none',
    loader: { '.css': 'text' },
    logLevel: 'info',
});

await copyFile('src/configPage.html', `${outDir}/configPage.html`);
