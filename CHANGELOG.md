# Changelog

All notable changes to TubeDLFin are listed here. Versions follow `MAJOR.MINOR.PATCH.0`
(Jellyfin plugin versions have four parts).

## [Unreleased]

### Added
- Detect (M2): paste a link on the new **Download** tab to see what it points to. Videos show
  title, thumbnail, uploader, length, date, music tags (artist, album, year), every video and
  audio format, and uploaded vs auto-generated subtitles. Playlists and channels list their
  first 100 items; a channel's tabs (Videos, Live, Shorts, Playlists) can be opened one by one.
  Links to a video inside a playlist (`watch?v=…&list=…`) offer "Only this video" or "Whole
  playlist". Detection can be cancelled, and leaving the page stops yt-dlp.
- `POST /TubeDLFin/Probe` (admin only). Errors come back as problem details: 400 invalid link,
  422 refused by yt-dlp (with its message), 502 unreadable output, 503 yt-dlp not ready,
  504 timeout. At most two detections run at once.
- The settings and tools moved to a **Tools & settings** tab.
- Tools (M1): TubeDLFin downloads yt-dlp and Deno for the server's platform (Linux x64/arm64
  glibc and musl, Windows, macOS), verifies their SHA-256 checksums, and checks that they run
  before installing. A scheduled task installs them at server start and updates them daily.
- Dashboard "Tools" section with yt-dlp, Deno and Jellyfin's FFmpeg status and "Update now";
  "Settings" section for custom yt-dlp / Deno paths.
- `GET /TubeDLFin/Tools` and `POST /TubeDLFin/Tools/Update` (admin only).
- Project setup (M0): plugin skeleton for Jellyfin 12.2, a "TubeDLFin" sidebar page built
  with TypeScript + esbuild, an admin-only `GET /TubeDLFin/Info` endpoint, unit tests, GitLab
  CI and a local Jellyfin dev container.
