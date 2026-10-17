import type { ProbeResult } from '../api/types';
import { element, externalLink } from './elements';
import { formatCount, formatDate, formatDuration, safeImageUrl } from './format';

/** The summary card: thumbnail, title, uploader, length, date, music tags or item count. */
export function mediaCard(result: ProbeResult): HTMLElement {
    const card = element('div', null, 'tubedlfin-card');

    const thumbnail = safeImageUrl(result.Thumbnail);
    if (thumbnail) {
        const image = element('img', null, 'tubedlfin-thumbnail');
        image.src = thumbnail;
        image.alt = '';
        image.loading = 'lazy';
        image.referrerPolicy = 'no-referrer';
        // Sites don't always have every size they list; drop the image rather than show a broken one.
        image.addEventListener('error', () => image.remove());
        card.append(image);
    }

    const body = element('div', null, 'tubedlfin-card-body');
    body.append(
        element('div', [kindLabel(result), siteName(result)].filter(Boolean).join(' · '), 'tubedlfin-kind'),
    );

    const title = element('h3', null, 'tubedlfin-title');
    title.append(externalLink(result.Title, result.Url));
    body.append(title);

    const meta = element('div', null, 'tubedlfin-meta');
    if (result.Uploader) {
        meta.append('by ', externalLink(result.Uploader, result.ChannelUrl));
    }

    const facts = [
        result.Duration != null ? formatDuration(result.Duration) : undefined,
        formatDate(result.UploadDate),
        result.ItemCount != null
            ? formatCount(
                  result.ItemCount,
                  itemNoun(result),
                  result.IsTruncated && result.ItemCount <= result.Entries.length,
              )
            : undefined,
    ].filter((fact): fact is string => Boolean(fact));
    if (facts.length > 0) {
        meta.append(`${result.Uploader ? ' · ' : ''}${facts.join(' · ')}`);
    }

    body.append(meta);

    const music = musicLine(result);
    if (music) {
        body.append(element('div', music, 'tubedlfin-meta'));
    }

    card.append(body);
    return card;
}

function kindLabel(result: ProbeResult): string {
    if (result.Kind !== 'Video') {
        return result.Kind;
    }

    return result.VideoFormats.length === 0 && result.AudioFormats.length > 0 ? 'Audio' : 'Video';
}

/** The link's site, e.g. "youtube.com"; yt-dlp's extractor name if there is no link. */
function siteName(result: ProbeResult): string | null | undefined {
    try {
        return result.Url ? new URL(result.Url).hostname.replace(/^www\./, '') : result.Site;
    } catch {
        return result.Site;
    }
}

/** A channel's front page lists its tabs; a playlists tab lists playlists. */
function itemNoun(result: ProbeResult): string {
    return result.Entries.length > 0 && result.Entries.every((entry) => entry.IsCollection) ? 'list' : 'item';
}

function musicLine(result: ProbeResult): string | undefined {
    if (!result.Artist && !result.Album) {
        return undefined;
    }

    const album = result.Album
        ? `${result.Album}${result.ReleaseYear ? ` (${result.ReleaseYear})` : ''}`
        : undefined;
    return `♪ ${[result.Artist, album].filter(Boolean).join(' — ')}`;
}
