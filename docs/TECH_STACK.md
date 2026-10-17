# TubeDLFin: tech stack

*Decided 2026-10-17. The plugin was renamed from "Pullfin" to **TubeDLFin** the same day. Companion to `~/Documents/Pullfin/PLAN.md`; where the two differ, this file wins.*

## Decisions that change PLAN.md

| Topic | PLAN.md | Now |
|---|---|---|
| Name | Pullfin | **TubeDLFin** (Tube = YouTube, DL = download, Fin = Jellyfin). Repo `wqyeo/jellyfin-plugin-tubedlfin` |
| Hosting | GitHub repo, Actions, Pages | **GitLab only** (`gitlab.com/wqyeo/jellyfin-plugin-tubedlfin`): CI, releases, manifest. No GitHub for now |
| Prototype in `~/jellyfin-plugin-ytdlp` | Reuse or delete | **Ignored**: everything is written from scratch |
| Dashboard page | Embedded HTML + JS | **TypeScript bundled with esbuild** into one embedded JS file |
| Probe errors | 500 when tools can't be installed | **503** when yt-dlp isn't ready, **502** for unreadable output, **504** for timeouts; all as problem details (`detail` holds the message) |
| Thumbnails | (not covered) | Loaded by the admin's browser straight from the site (`https` only, `no-referrer`). Jellyfin 12.2 sends no CSP header, so no proxy is needed |

## Backend (the plugin DLL)

| Concern | Choice |
|---|---|
| Language / target | C# on `net10.0` (Jellyfin loads plugins as .NET DLLs in-process; other languages would need a sidecar or a separate app) |
| Jellyfin packages | `Jellyfin.Controller` + `Jellyfin.Model` **12.2.0**, `ExcludeAssets=runtime`; `targetAbi` `12.2.0.0` |
| Third-party runtime dependencies | **None.** Only the .NET base libraries plus what the Jellyfin host already loads (ASP.NET Core MVC, DI, logging, `IHttpClientFactory`, System.Text.Json). Extra DLLs risk version clashes with the server |
| Running yt-dlp / Deno | `System.Diagnostics.Process` with `ArgumentList` (never a shell); cancel with `Kill(entireProcessTree: true)` |
| Queue | `DownloadManager` (in memory, `System.Threading.Channels`) + `DownloadDispatcher` (`BackgroundService`, registered with `AddHostedService`; Jellyfin 12.2 starts it) running N jobs at once (setting, re-read every few seconds). `Enqueue(plan, source)` takes any source (`Manual`, later subscriptions). Each job: own temp folder `tmp/downloads/<id>` (`-P temp:`), yt-dlp moves finished files into the library, per-library `--download-archive <data>/archive/<libraryId>.txt`. Jobs and their snapshots are separate: only immutable `DownloadJobInfo` records leave the queue |
| Download options | One `IDownloadOptionSet` per concern (output, progress, archive, playlist, video format, audio format), applied in registration order; M4 adds subtitles, SponsorBlock, thumbnails as more sets. Validation (`DownloadRequestValidator`) allowlists every value and keeps the destination inside the chosen library folder |
| Progress | `--progress-template` / `--print before_dl|after_move` marker lines (`[tubedlfin:progress]` ...) read line by line from both streams (`IProcessRunner.StreamAsync`). `--print` implies quiet mode, so `--no-quiet` brings back the log (needed for "already in archive" messages and the job's Details) |
| New files in Jellyfin | Refresh the nearest folder Jellyfin already knows (`FindByPath` walking up inside the library) with `ChangedExternally()`; if none (library never scanned with content), `QueueLibraryScan()`. Not `ILibraryMonitor.ReportFileSystemChanged`: it waits for the "library monitor delay" (60 s default) and does nothing for never-scanned libraries. Measured: items appear 0–1.5 s after the download |
| JSON | System.Text.Json. yt-dlp's `-J` output is read with `JsonDocument` and lenient helpers (sites leave out or null whatever they lack), not mapped to fixed classes |
| `.nfo` files | `System.Xml.Linq` |
| Tool install | One `IToolDefinition` per tool (asset per platform, checksum file, version parsing); add a tool by registering another. Latest release read from the `github.com/<repo>/releases/latest` redirect (no REST API, so no 60/h limit). Download → SHA-256 check against the published list → unzip (Deno) → `chmod 755` → run `--version` on the new copy → atomic rename over the old one. A broken download never replaces a working tool |
| Tool updates | Scheduled task "Update yt-dlp and Deno" (startup + daily 04:00, editable under Scheduled Tasks) and `POST /TubeDLFin/Tools/Update`; one run at a time, later callers join it. Tools with a custom path are never touched |
| Running yt-dlp | `IYtDlpCommandFactory` adds what every run needs: `--ignore-config --no-color --cache-dir`, `--js-runtimes deno:<path>` (only when Deno works), `--ffmpeg-location` (Jellyfin's), then the run's options and `-- <url>`. Links are checked first by `MediaUrl` (absolute http(s), ≤ 2048 chars, no spaces or user info) |
| Detect | `yt-dlp --dump-single-json --flat-playlist --playlist-items 1:100 [--no-playlist]`, 2 min timeout, at most 2 at once (`ThrottledMediaProber` decorator). Aborting the HTTP request kills yt-dlp. "Video inside a playlist" links are recognised by `IPlaylistLinkInspector` implementations (YouTube for now). Parser tests use real yt-dlp output, cleaned by `tests/.../Detection/Samples/sanitize.mjs` (signed media URLs contain the server's IP) |
| Platform | `ProcessArchitecture` + musl detection (`/lib/ld-musl-*`) → `linux-arm64-glibc` etc. Deno has no musl builds, so Alpine needs a custom Deno path |
| Settings | `PluginConfiguration` (Jellyfin's XML config), read through `IPluginConfigurationProvider` (via `IPluginManager`, no static instance). M1: `YtDlpPath`, `DenoPath` (blank = managed) |
| Saved data | Versioned JSON / text in `<server data>/tubedlfin` (`/config/data/tubedlfin` in Docker), via `IPluginPaths`. Not `BasePlugin.DataFolderPath`: that is inside `plugins/`, where Jellyfin treats every folder as a plugin |
| Code quality | `Nullable` on, `TreatWarningsAsErrors`, `AnalysisMode=AllEnabledByDefault`, StyleCop / Serilog / multithreading analyzers as `GlobalPackageReference` (nothing ships). Package versions are pinned centrally in `Directory.Packages.props` |
| Design | SOLID: classes depend on interfaces injected by DI (no `Plugin.Instance` statics); `PluginServiceRegistrator` is the composition root; pure logic is kept apart from processes, files and HTTP |

The DLL is AnyCPU, so one build serves arm64 (the Pi) and x64. Only yt-dlp and Deno are
per-architecture, and they are downloaded at runtime.

## Dashboard page (TypeScript + esbuild)

```
web/                        # separate toolchain; output is embedded by src/.../Web.targets
├── package.json            # devDependencies only: typescript 7, esbuild, biome
├── tsconfig.json           # strict, noEmit (tsc only type-checks; esbuild emits)
├── biome.json              # lint + format
├── build.mjs               # esbuild -> dist/tubedlfin.js, copies configPage.html to dist/
├── types/jellyfin.d.ts     # ambient types for the ApiClient and Dashboard globals
└── src/
    ├── main.ts             # composition root: wires the classes below together
    ├── page.ts             # TubeDLFinPage: loads sections on "viewshow", stops them on "viewhide"
    ├── api/                # HttpClient (fetch + Jellyfin's auth header), TubeDLFinApi, DTO types
    ├── sections/           # one controller per page section (Detect, Tools, Settings, About)
    ├── state/              # DetectStore: the Detect step's state, observed by views (and M3's options)
    ├── views/              # rendering only: forms, result card, tables, tabs, format helpers
    ├── dom.ts, styles.css  # helpers; CSS is bundled as text and injected
    └── configPage.html     # markup only, uses Jellyfin's own classes
```

- **Output**: one bundled ES module, `tubedlfin.js`. jellyfin-web 12.2 imports the page's
  `data-controller="__plugin/tubedlfin.js"` and calls `new module.default(view, params)`, so the
  default export is a class. Confirmed working in M0 (headless browser against Jellyfin 12.2).
- **Embedding**: `web/dist/configPage.html` and `web/dist/tubedlfin.js` are `EmbeddedResource`s,
  served through the `PluginPageInfo` entries in `Web/PluginWebPages.cs`. A test checks that the
  page's `data-controller` and the script page name match.
- **Build hook**: `Web.targets` runs `npm ci` (only when the lock file changes) and
  `npm run build` (only when web sources change) before compiling. Skip it with
  `SkipWebBuild=true` when `web/dist` is already built.
- **The dist output is not committed**: it's always built from source.
- **No runtime JS libraries**: just the DOM and `fetch`. Requests carry the header from
  `ApiClient.setRequestHeaders()`, so the page uses Jellyfin's own auth, and an `AbortSignal`
  can cancel them (Cancel button, leaving the page).
- **Untrusted text**: titles and names from sites only ever go in through `textContent`; links
  must be http(s) and open with `rel="noopener noreferrer"`.
- **Queue updates**: poll `GET /TubeDLFin/Downloads` every 1.5 s while a job is active and every
  10 s when idle, only while the page is shown. Rows are updated in place (keyed by job id), so
  an open "Details" panel survives refreshes.
- **API types**: written by hand in `web/src/api/types.ts` for v1 (Jellyfin's JSON is PascalCase).

## Runtime tools (downloaded by the plugin, not shipped)

| Tool | Source | Location |
|---|---|---|
| yt-dlp | GitHub `yt-dlp/yt-dlp` latest release, standalone binary per arch | `<data>/bin/` |
| Deno | GitHub `denoland/deno` latest release zip (glibc only) | `<data>/bin/` |
| FFmpeg / ffprobe | jellyfin-ffmpeg via `IMediaEncoder.EncoderPath` | Jellyfin's own |

## Tests

- `Jellyfin.Plugin.TubeDLFin.Tests` uses **xUnit v3** (4.0.x) on Microsoft.Testing.Platform
  (opted in through `global.json`, which the .NET 10 SDK requires) with plain `Assert` and
  **NSubstitute** for fakes. FluentAssertions v8 needs a commercial licence, so it isn't used.
- Fixtures are real `yt-dlp -J` outputs saved as files: a video, a playlist, a channel, a
  YouTube Music album, and a video with subtitles.
- Argument building and validation are pure functions. Process execution sits behind
  `IProcessRunner`, so the queue can be tested with a fake.
- Integration tests that run real yt-dlp are marked `[Trait("Category", "Integration")]` and
  are run by hand or on a schedule, not on every push.
- Web: `tsc --noEmit` + Biome lint in CI. There are no browser tests in v1.

## Build, CI and release

| Piece | Choice |
|---|---|
| Packaging | **JPRM** 1.1.0 from `build.yaml`: runs `dotnet publish` on the solution (the test project is `IsPublishable=false`) and zips the DLL with `meta.json`. The output folder must exist beforehand |
| Version | Set in one place, `Directory.Build.props`; release tags are `vX.Y.Z` |
| CI | `.gitlab-ci.yml`, kept minimal because CI minutes are limited |
| `build` job | One job on the `dotnet/sdk:10.0` image: installs Node (tarball), `npm ci && npm run check`, `dotnet build` (also bundles web/), `dotnet test`. Runs only for `main`, merge requests and tags, and only when code changed (not docs) |
| `package` job | Only for `v*` tags: JPRM zip as an artifact |
| On a `v*` tag | JPRM package → upload the zip to the **GitLab Generic Package Registry** → create a GitLab Release → **GitLab Pages** regenerates and publishes `manifest.json` |
| Plugin repo URL | `https://wqyeo.gitlab.io/jellyfin-plugin-tubedlfin/manifest.json`. **Pages access must be changed from `private` to public** |

## Dev environment

- `mise.toml` + `global.json` pin .NET SDK 10.0.400, Node 24 and uv.
- `dev/jellyfin.sh up|deploy|logs|down` runs podman (or `CONTAINER_ENGINE=docker`)
  `jellyfin/jellyfin:12.2` on port 8096, with `dev/config`, `dev/cache` and read-write
  `dev/media/*` mounted with `:Z` for SELinux. `deploy` publishes the plugin into
  `dev/config/plugins/TubeDLFin_<version>/` and restarts the container.
- Use `http://127.0.0.1:8096` rather than `localhost`: podman publishes the port on IPv4 only.
