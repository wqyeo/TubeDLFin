# Changelog

All notable changes to TubeDLFin are listed here. Versions follow `MAJOR.MINOR.PATCH.0`
(Jellyfin plugin versions have four parts).

## [Unreleased]

### Added
- Tools (M1): TubeDLFin downloads yt-dlp and Deno for the server's platform (Linux x64/arm64
  glibc and musl, Windows, macOS), verifies their SHA-256 checksums, and checks that they run
  before installing. A scheduled task installs them at server start and updates them daily.
- Dashboard "Tools" section with yt-dlp, Deno and Jellyfin's FFmpeg status and "Update now";
  "Settings" section for custom yt-dlp / Deno paths.
- `GET /TubeDLFin/Tools` and `POST /TubeDLFin/Tools/Update` (admin only).
- Project setup (M0): plugin skeleton for Jellyfin 12.2, a "TubeDLFin" sidebar page built
  with TypeScript + esbuild, an admin-only `GET /TubeDLFin/Info` endpoint, unit tests, GitLab
  CI and a local Jellyfin dev container.
