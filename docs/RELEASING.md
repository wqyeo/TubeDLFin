# Releasing

A release is a tag on `main`. The tag pipeline publishes everything; nothing is uploaded by hand.

There are two plugin repositories (manifests on GitLab Pages):

| Repository | URL | Lists |
|---|---|---|
| Stable | `https://wqyeo.gitlab.io/jellyfin-plugin-tubedlfin/manifest.json` | `vX.Y.Z` releases |
| Beta | `https://wqyeo.gitlab.io/jellyfin-plugin-tubedlfin/manifest-beta.json` | `vX.Y.Z-beta.N` betas *and* every stable release |

Jellyfin offers the highest version among the repositories a server has, so servers with the
beta repository get the betas and then the stable release after them; the others never see a beta.

## Versions

Jellyfin plugin versions have four numbers, `X.Y.Z.B`. The fourth counts builds of `X.Y.Z`:

| Tag | Version in `Directory.Build.props` and `build.yaml` |
|---|---|
| `v1.2.0` with no betas before it | `1.2.0.0` |
| `v1.2.0-beta.1`, `v1.2.0-beta.2` | `1.2.0.1`, `1.2.0.2` (the fourth number is the beta's number) |
| `v1.2.0` after those betas | `1.2.0.3`: higher than the last beta, or beta servers would stay on it |

The `package` job checks this before uploading anything: the tag must match the version, a
stable release must be above its published betas, and there can't be a beta of a version that is
already released.

## A beta

1. Set the version to `X.Y.Z.N` (both files, as below) for `vX.Y.Z-beta.N`.
2. Keep the notes under `## [Unreleased]` in **`CHANGELOG.md`**: a beta's release notes are that
   section, with a line saying it is a beta.
3. Test, commit, then tag and push:

   ```sh
   git tag -a vX.Y.Z-beta.N -m "TubeDLFin X.Y.Z beta N"
   git push origin main vX.Y.Z-beta.N
   ```

Fixes go out as the next beta. When the betas hold up, release `vX.Y.Z` as below, with the
version one above the last beta.

## A stable release

1. Set the version in **`Directory.Build.props`** (`Version`, `AssemblyVersion`, `FileVersion`)
   and **`build.yaml`**, as `X.Y.Z.0`, or one above the last beta of `X.Y.Z`. Raise `targetAbi` in `build.yaml` only when the plugin
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
| `package` | Checks the tag against `Directory.Build.props` and the published manifests (`web/release/release-version.mjs`), builds the zip with JPRM, uploads it to the project's package registry (`tubedlfin/X.Y.Z.B/tubedlfin_X.Y.Z.B.zip`) |
| `release` | Creates the GitLab Release with the release notes and a link to the zip |
| `pages` | Downloads both published manifests, adds the new version (zip link, MD5, `targetAbi`, release notes) with `web/release/publish-manifest.mjs`: a stable release to both, a beta to the beta one only. Publishes both with the logo on GitLab Pages |

Each `pages` run replaces the whole Pages site, so it always writes both manifests. It fails,
rather than publishing, if an old manifest can't be read (anything but 404), and a beta refuses
an empty stable manifest, so older versions are never dropped by accident.

Pages requires the project's Pages access to be "Everyone" and "Use unique domain" to be off
(Settings → Pages).

## Checking a release

```sh
curl -s https://wqyeo.gitlab.io/jellyfin-plugin-tubedlfin/manifest.json | grep '"version"'
curl -s https://wqyeo.gitlab.io/jellyfin-plugin-tubedlfin/manifest-beta.json | grep '"version"'
```

Then in a Jellyfin with the repository added: **Dashboard → Plugins → Catalog → TubeDLFin**
should offer the new version.

## Fixing a bad release

Published versions stay in the manifest, and servers may already have installed them. Don't
move or re-push a tag: release the fix as the next patch version. Re-running a tag's `pages`
job replaces that version's entry (same version number) rather than adding a second one.

## Trying the pipeline locally

```sh
TAG=vX.Y.Z-beta.N
PAGES=https://wqyeo.gitlab.io/jellyfin-plugin-tubedlfin
VERSION=$(node web/release/release-version.mjs --tag $TAG --guid 0975c0da-6b11-4771-99fd-7d1176bf1f94 \
  --props-version "$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -n 1)" \
  --pages-url $PAGES)
node web/release/release-notes.mjs CHANGELOG.md $TAG
uvx --from "jprm==1.1.0" jprm plugin build . --output artifacts
node web/release/publish-manifest.mjs --tag $TAG --zip artifacts/tubedlfin_$VERSION.zip \
  --source-url https://example.com/tubedlfin_$VERSION.zip --changelog CHANGELOG.md \
  --pages-url $PAGES --out-dir public
```
