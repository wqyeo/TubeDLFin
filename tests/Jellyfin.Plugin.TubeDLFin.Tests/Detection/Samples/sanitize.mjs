// Turns `yt-dlp -J` output into a sample file that is safe to commit.
//
// Media URLs in yt-dlp's JSON are signed for the machine that ran it and contain its IP
// address, so they are removed. Long lists are shortened to keep the samples small; the
// counts yt-dlp reported (playlist_count) are kept.
//
// Usage:
//   yt-dlp --ignore-config --flat-playlist -J -I 1:100 <url> | node sanitize.mjs [entries] > name.json
//
// The samples in this folder were captured with yt-dlp 2026.08.19 (see README.md here).

const maxEntries = Number(process.argv[2] ?? 5);
const maxNestedEntries = 3;
const maxThumbnails = 3;
const maxCaptionLanguages = 6;
const maxDescription = 200;

// Keys holding signed or machine-specific data.
const droppedKeys = new Set([
    'manifest_url',
    'fragment_base_url',
    'fragments',
    'http_headers',
    'downloader_options',
    'cookies',
    'requested_formats',
    'requested_downloads',
    'requested_subtitles',
    'heatmap',
    'formats_table',
    '_format_sort_fields',
    '_has_drm',
    'epoch',
]);

// Anything that still looks like a signed media URL fails the run instead of being committed.
const forbidden = [/googlevideo\.com/, /[/?&]ip[=/]/, /[?&]expire=/, /[?&](sig|signature)=/, /sndcdn\.com\/.*Policy=/];

const input = await new Promise((resolve) => {
    let text = '';
    process.stdin.setEncoding('utf8');
    process.stdin.on('data', (chunk) => {
        text += chunk;
    });
    process.stdin.on('end', () => resolve(text));
});

const info = JSON.parse(input);
const output = JSON.stringify(info === null ? null : sanitizeInfo(info, maxEntries), null, 1);

const leak = forbidden.find((pattern) => pattern.test(output));
if (leak) {
    console.error(`sanitize.mjs: output still matches ${leak}; not writing it.`);
    process.exit(1);
}

process.stdout.write(`${output}\n`);

function sanitizeInfo(item, entryLimit) {
    const result = {};
    for (const [key, value] of Object.entries(item)) {
        if (droppedKeys.has(key)) {
            continue;
        }

        switch (key) {
            case 'formats':
                result.formats = value.map(sanitizeFormat);
                break;
            case 'subtitles':
            case 'automatic_captions':
                result[key] = sanitizeCaptions(value, key === 'subtitles' ? Infinity : maxCaptionLanguages);
                break;
            case 'thumbnails':
                result.thumbnails = value.slice(-maxThumbnails);
                break;
            case 'description':
                result.description = typeof value === 'string' ? value.slice(0, maxDescription) : value;
                break;
            case 'entries':
                result.entries = value.slice(0, entryLimit).map((entry) => sanitizeInfo(entry, maxNestedEntries));
                break;
            case 'url':
                // An entry's url is its page link (keep); a media item's top-level url is a signed stream.
                if (item._type === 'url' || item._type === 'playlist') {
                    result.url = value;
                }

                break;
            default:
                result[key] = value;
        }
    }

    return result;
}

function sanitizeFormat(format) {
    const result = {};
    for (const [key, value] of Object.entries(format)) {
        if (key !== 'url' && !droppedKeys.has(key)) {
            result[key] = value;
        }
    }

    return result;
}

function sanitizeCaptions(captions, languageLimit) {
    const languages = Object.keys(captions);
    // Keep the original-language track (YouTube marks it "-orig") among the kept languages.
    const kept = [...languages.filter((code) => code.endsWith('-orig')), ...languages].filter(
        (code, index, all) => all.indexOf(code) === index,
    );

    return Object.fromEntries(
        kept.slice(0, languageLimit).map((code) => [code, captions[code].slice(0, 1).map(({ ext, name }) => ({ ext, name }))]),
    );
}
