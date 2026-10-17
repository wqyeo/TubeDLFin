# yt-dlp detect samples

Real `yt-dlp -J --flat-playlist -I 1:100` output, captured with yt-dlp 2026.08.19 on 2026-10-17
and cleaned with `sanitize.mjs` (signed media URLs removed, long lists shortened).

| File | Link |
|---|---|
| `video.json` | `https://www.youtube.com/watch?v=jNQXAC9IVRw` (uploaded subtitles) |
| `video-captions.json` | `https://www.youtube.com/watch?v=sO_lhW8av3U` (auto-generated captions, up to 2160p) |
| `music-track.json` | `https://music.youtube.com/watch?v=d7woYjGWs6k` (artist, album, track) |
| `playlist.json` | `https://www.youtube.com/watch?v=sO_lhW8av3U&list=PLa1F2ddGya_-Ymw4YlOjqrdQxiRMJql5x` |
| `album.json` | `https://music.youtube.com/browse/MPREb_DysYPhLwfgT` |
| `channel.json` | `https://www.youtube.com/@BlenderOfficial` (one entry per tab) |
| `channel-videos.json` | `https://www.youtube.com/@BlenderOfficial/videos` |
| `channel-playlists.json` | `https://www.youtube.com/@BlenderOfficial/playlists` |
| `soundcloud.json` | `https://soundcloud.com/forss/flickermood` (audio only, not YouTube) |

To refresh one:

```sh
yt-dlp --ignore-config --flat-playlist -J -I 1:100 '<link>' | node sanitize.mjs > <name>.json
```
