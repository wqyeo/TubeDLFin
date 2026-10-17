# Changelog

All notable changes to TubeDLFin are listed here. Versions follow `MAJOR.MINOR.PATCH.0`
(Jellyfin plugin versions have four parts).

## [Unreleased]

### Added
- Polish (M4): file naming presets chosen by library type: one folder per video for movies,
  "Channel / Season year / SYYYYEMMDDHHMM" for shows (episode numbers from the upload date and
  time, so same-day uploads don't merge), "Artist / Title" for music videos, "Artist / Album /
  Title" for music, plus "Channel / Title", "Playlist / 01 - Title" and a custom yt-dlp template.
- `.nfo` files for Movies, Home Videos, Mixed (`<movie>`), Shows (`<episodedetails>` plus the
  channel's `tvshow.nfo`) and Music Videos (`<musicvideo>`), with `<lockdata>` so Jellyfin keeps
  YouTube's title, description and date instead of guessing an online match. Thumbnail saved
  as the poster Jellyfin looks for; tags, chapters and cover art can be embedded.
- Subtitles as `.srt` next to the video (pick languages from the detected list or type codes,
  auto-generated captions, embed). YouTube's "-orig" captions are renamed so Jellyfin knows
  their language.
- SponsorBlock: cut out or mark as chapters, per category.
- The download form remembers the last options in the browser, separately for music and video;
  choosing a music library switches to audio, the music layout and embedded cover art.
- Free space of the destination folder, with a warning when it's low or smaller than the video.
- Settings: cookies file (for age-restricted videos and YouTube's "sign in to confirm you're not
  a bot"), extra yt-dlp arguments. Errors about signing in point to the cookies setting.
- `GET /TubeDLFin/NamingPresets`; `GET /TubeDLFin/Libraries` now includes default naming and free space.
- Frontend unit tests (Node's built-in test runner, no new dependencies), part of `npm run check`.
- Download (M3): after detecting a link, choose Video (quality up to 2160p or a specific
  format, MP4/MKV/WebM, prefer H.264) or Audio only (original, M4A, MP3, Opus, FLAC, WAV,
  bitrate), playlist items, a library, folder and subfolder, and press Download. Files go
  straight into the library as `Channel/Title [id].ext` and appear in Jellyfin within seconds,
  without a library scan, even in a library that was empty until then.
- Download queue on the Download tab: progress, speed, time left, "item 3 of 12", cancel,
  remove, clear finished, and per-job details (files and the yt-dlp log). States include
  "Completed with errors" (some playlist items failed) and "Already downloaded".
- Each library keeps a download archive, so nothing is downloaded into it twice; a checkbox
  downloads again anyway. Partial files stay in TubeDLFin's own temp folder until finished.
- Setting "Downloads at the same time" (default 1).
- `GET /TubeDLFin/Libraries`, `GET/POST /TubeDLFin/Downloads`, `GET /TubeDLFin/Downloads/{id}/Log`,
  `POST /TubeDLFin/Downloads/{id}/Cancel`, `DELETE /TubeDLFin/Downloads/{id}`,
  `POST /TubeDLFin/Downloads/ClearFinished` (admin only).
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
