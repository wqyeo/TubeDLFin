# TubeDLFin

**Tube** + **DL** + Jelly**fin**: a Jellyfin plugin that brings a yt-dlp download workflow into
the Jellyfin dashboard. Paste a video, playlist or channel URL, see what is available, pick the
options, and TubeDLFin downloads it straight into one of your libraries.

> **Status:** early development (M2, detect). Links can be detected (formats, subtitles,
> playlist and channel items); downloading media comes in M3.

- Jellyfin **12.2** (`targetAbi 12.2.0.0`, `net10.0`)
- Works for both **video** and **music** libraries
- yt-dlp and Deno are downloaded and kept up to date by the plugin; FFmpeg comes from Jellyfin

## Development

Requirements: .NET SDK 10.0.400+, Node 24+, podman (or Docker). With [mise](https://mise.jdx.dev),
`mise install` sets these up (except podman).

```sh
dotnet build                # also builds web/ (npm ci + esbuild) the first time
dotnet test

cd web && npm run check     # TypeScript type check + Biome lint

dev/jellyfin.sh deploy      # build, install into a local Jellyfin 12.2, (re)start it
dev/jellyfin.sh logs        # http://127.0.0.1:8096
dev/jellyfin.sh down
```

Layout:

| Path | What |
|---|---|
| `src/Jellyfin.Plugin.TubeDLFin/` | The plugin (C#). `PluginServiceRegistrator` is the composition root |
| `web/` | The dashboard page (TypeScript, bundled by esbuild into `web/dist/`, embedded in the DLL) |
| `tests/Jellyfin.Plugin.TubeDLFin.Tests/` | xUnit v3 tests |
| `build.yaml` | Plugin metadata for [JPRM](https://github.com/oddstr13/jellyfin-plugin-repository-manager) packaging |
| `dev/` | Local Jellyfin container script |
| `docs/` | Tech stack and design notes |

## Legal

TubeDLFin is not affiliated with YouTube, Google, the yt-dlp project or Jellyfin. You are
responsible for what you download; respect the terms of the sites you use and copyright law.

## Licence

[MIT](LICENSE)
