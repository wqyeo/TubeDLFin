# Releasing

A release is a `vX.Y.Z` tag on `main`. The tag pipeline publishes everything; nothing is
uploaded by hand.

## Steps

1. Set the version in **`Directory.Build.props`** (`Version`, `AssemblyVersion`, `FileVersion`)
   and **`build.yaml`**, as `X.Y.Z.0`. Raise `targetAbi` in `build.yaml` only when the plugin
   needs a newer Jellyfin.
2. In **`CHANGELOG.md`**, move the `[Unreleased]` notes into a new `## [X.Y.Z] - YYYY-MM-DD`
   section. That section becomes the release notes and the changelog shown in Jellyfin's
   catalog, so write it for users.
3. `dotnet test` and `cd web && npm run check`. A test fails if `build.yaml`,
   `Directory.Build.props` and `CHANGELOG.md` disagree.
4. Before a public release, install the build on x64 and arm64 (the Pi) Jellyfin with the
   `targetAbi` the release claims, and complete a video and a music download.
5. Commit, then tag and push:

   ```sh
   git tag -a vX.Y.Z -m "TubeDLFin X.Y.Z"
   git push origin main vX.Y.Z
   ```

## What the tag pipeline does

| Job | Does |
|---|---|
| `build` | Lint, type check, build and test, as on every push |
| `package` | Checks the tag matches `Directory.Build.props`, builds the zip with JPRM, uploads it to the project's package registry (`tubedlfin/X.Y.Z.0/tubedlfin_X.Y.Z.0.zip`) |
| `release` | Creates the GitLab Release with the changelog section and a link to the zip |
| `pages` | Downloads the published `manifest.json`, adds the new version (zip link, MD5, `targetAbi`, changelog) with `web/release/publish-manifest.mjs`, and publishes it with the logo on GitLab Pages |

The `pages` job fails, rather than publishing, if the old manifest can't be read (anything but
404), so older versions are never dropped by accident.

Plugin repository URL: `https://wqyeo.gitlab.io/jellyfin-plugin-tubedlfin/manifest.json`.
Requires the project's Pages access to be "Everyone" and "Use unique domain" to be off
(Settings → Pages).

## Checking a release

```sh
curl -s https://wqyeo.gitlab.io/jellyfin-plugin-tubedlfin/manifest.json | head -40
```

Then in a Jellyfin with the repository added: **Dashboard → Plugins → Catalog → TubeDLFin**
should offer the new version.

## Fixing a bad release

Published versions stay in the manifest, and servers may already have installed them. Don't
move or re-push a tag: release the fix as the next patch version. Re-running a tag's `pages`
job replaces that version's entry (same version number) rather than adding a second one.

## Trying the pipeline locally

```sh
VERSION=X.Y.Z.0
node web/release/release-notes.mjs CHANGELOG.md $VERSION
uvx --from "jprm==1.1.0" jprm plugin build . --output artifacts
node web/release/publish-manifest.mjs --zip artifacts/tubedlfin_$VERSION.zip \
  --source-url https://example.com/tubedlfin_$VERSION.zip --changelog CHANGELOG.md \
  --previous https://wqyeo.gitlab.io/jellyfin-plugin-tubedlfin/manifest.json --out public/manifest.json
```
