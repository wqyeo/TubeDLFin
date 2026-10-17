# Changelog

All notable changes to TubeDLFin are listed here. Versions follow `MAJOR.MINOR.PATCH.0`
(Jellyfin plugin versions have four parts).

## [Unreleased]

## [0.9.0] - 2026-10-17

First public build, a release candidate for 1.0.

### Added
- **Download page** in the dashboard (admins only): paste a video, playlist or channel link,
  see its formats, subtitles, music tags and playlist items, choose the options and download
  it straight into a library. New files show up in Jellyfin within seconds, without a scan.
- **Video**: quality up to 2160p or a specific format, MP4/MKV/WebM, prefer H.264.
  **Audio only**: original, M4A, MP3, Opus, FLAC or WAV.
- **Layouts per library type**: one folder per video for Movies, "Channel / Season year /
  episode by upload date" for Shows, "Artist / Title" for Music Videos, "Artist / Album /
  01 - Title" for Music, or your own yt-dlp output template.
- **Metadata**: `.nfo` files Jellyfin reads (title, description, date, channel, kept from
  online lookups), the thumbnail as poster, embedded tags, chapters and cover art.
- **Music tags**: "Artist - Title" titles split, "(Official Video)" and similar removed,
  YouTube's " - Topic" dropped, playlists numbered as tracks, YouTube Music albums kept, a
  playlist can become one album, square cover art.
- **Subtitles** as `.srt` files with the right language, including auto-generated captions.
- **SponsorBlock**: cut out or mark sponsor segments, intros and more, per category.
- **Queue**: progress, speed, time left, playlist position, cancel, per-job log; several
  downloads at once (setting); a per-library archive so nothing is downloaded twice.
- **yt-dlp and Deno managed by the plugin**: downloaded for the server's platform (Linux x64 and
  arm64, Windows, macOS), checksum-verified and updated daily. FFmpeg comes from Jellyfin.
- **Settings**: cookies file (age-restricted videos, YouTube's "confirm you're not a bot"),
  extra yt-dlp arguments, custom tool paths. The page remembers the last options for music and
  video separately and warns when the destination is low on space.
- Admin-only API under `/TubeDLFin` (`Probe`, `Downloads`, `Libraries`, `NamingPresets`, `Tools`).
