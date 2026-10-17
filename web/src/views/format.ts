// Turns API values into short display text. Pure functions, no DOM.

const codecNames: ReadonlyArray<readonly [string, string]> = [
    ['avc1', 'H.264'],
    ['avc3', 'H.264'],
    ['hev1', 'H.265'],
    ['hvc1', 'H.265'],
    ['vp09', 'VP9'],
    ['vp9', 'VP9'],
    ['vp8', 'VP8'],
    ['av01', 'AV1'],
    ['mp4a', 'AAC'],
    ['aac', 'AAC'],
    ['opus', 'Opus'],
    ['vorbis', 'Vorbis'],
    ['mp3', 'MP3'],
    ['flac', 'FLAC'],
    ['ac-3', 'AC-3'],
    ['ec-3', 'E-AC-3'],
];

/** "avc1.640028" → "H.264". Unknown codecs are shown as they are. */
export function codecName(codec: string | null | undefined): string {
    if (!codec) {
        return '–';
    }

    const lower = codec.toLowerCase();
    return codecNames.find(([prefix]) => lower.startsWith(prefix))?.[1] ?? codec;
}

/** 3725 → "1:02:05", 65 → "1:05". */
export function formatDuration(seconds: number | null | undefined): string {
    if (seconds == null || !Number.isFinite(seconds)) {
        return '–';
    }

    const total = Math.round(seconds);
    const hours = Math.floor(total / 3600);
    const minutes = Math.floor((total % 3600) / 60);
    const secs = String(total % 60).padStart(2, '0');
    return hours > 0 ? `${hours}:${String(minutes).padStart(2, '0')}:${secs}` : `${minutes}:${secs}`;
}

/** 309288 → "302 KB"; estimates get a "~". */
export function formatSize(bytes: number | null | undefined, approximate = false): string {
    if (bytes == null) {
        return '–';
    }

    const units = ['B', 'KB', 'MB', 'GB', 'TB'];
    let value = bytes;
    let unit = 0;
    while (value >= 1024 && unit < units.length - 1) {
        value /= 1024;
        unit++;
    }

    const digits = value >= 100 || unit === 0 ? 0 : 1;
    return `${approximate ? '~' : ''}${value.toFixed(digits)} ${units[unit]}`;
}

/** 129.8 → "130 kbit/s". */
export function formatBitrate(kbps: number | null | undefined): string {
    return kbps == null || kbps <= 0 ? '–' : `${Math.round(kbps)} kbit/s`;
}

/** 1080, 50 → "1080p50"; 1080, 25 → "1080p". */
export function formatResolution(height: number | null | undefined, fps: number | null | undefined): string {
    if (height == null) {
        return '–';
    }

    return fps != null && fps > 30 ? `${height}p${Math.round(fps)}` : `${height}p`;
}

/** "2005-04-24" → the admin's local date format. */
export function formatDate(isoDate: string | null | undefined): string | undefined {
    if (!isoDate) {
        return undefined;
    }

    const date = new Date(`${isoDate}T00:00:00`);
    return Number.isNaN(date.getTime()) ? isoDate : date.toLocaleDateString();
}

/** "1 item" / "5 items" / "100+ items". */
export function formatCount(count: number, noun: string, isMinimum = false): string {
    return `${count}${isMinimum ? '+' : ''} ${noun}${count === 1 && !isMinimum ? '' : 's'}`;
}

/** Only https images are shown, so the dashboard never loads mixed content. */
export function safeImageUrl(url: string | null | undefined): string | undefined {
    return url?.startsWith('https://') ? url : undefined;
}

/** Only web links become clickable. */
export function safeLinkUrl(url: string | null | undefined): string | undefined {
    return url && /^https?:\/\//i.test(url) ? url : undefined;
}
