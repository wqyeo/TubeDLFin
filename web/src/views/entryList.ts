import type { PlaylistEntry, ProbeResult } from '../api/types';
import { button, element, externalLink, row, table } from './elements';
import { formatCount, formatDuration } from './format';

/** The items of a playlist or channel. Tabs and nested playlists get an "Open" button. */
export function entryList(result: ProbeResult, open: (entry: PlaylistEntry) => void): HTMLElement {
    const block = element('div', null, 'tubedlfin-entries');
    const collectionsOnly = result.Entries.length > 0 && result.Entries.every((entry) => entry.IsCollection);
    block.append(
        element(
            'h4',
            collectionsOnly ? (result.Kind === 'Channel' ? 'Tabs and playlists' : 'Playlists') : 'Items',
        ),
    );

    if (result.Entries.length === 0) {
        block.append(element('p', 'This list is empty.', 'fieldDescription'));
        return block;
    }

    if (result.IsTruncated) {
        const total =
            result.ItemCount != null && result.ItemCount > result.Entries.length
                ? ` of ${result.ItemCount}`
                : '';
        block.append(
            element(
                'p',
                `Showing the first ${result.Entries.length}${total}. Downloading the whole list isn't limited to these.`,
                'fieldDescription',
            ),
        );
    }

    block.append(
        table(
            ['#', 'Title', 'Length', collectionsOnly ? 'Items' : 'Uploader'],
            result.Entries.map((entry) => entryRow(entry, open)),
        ),
    );
    return block;
}

function entryRow(entry: PlaylistEntry, open: (entry: PlaylistEntry) => void): HTMLTableRowElement {
    const title = element('div', null, 'tubedlfin-entry-title');
    title.append(externalLink(entry.Title, entry.Url));
    if (entry.IsCollection && entry.Url) {
        title.append(
            ' ',
            button('Open', () => open(entry), 'tubedlfin-small-button'),
        );
    }

    const last = entry.IsCollection
        ? entry.ItemCount != null
            ? formatCount(entry.ItemCount, 'item', entry.IsItemCountMinimum)
            : '–'
        : (entry.Uploader ?? '–');

    return row(String(entry.Index), title, entry.IsCollection ? '–' : formatDuration(entry.Duration), last);
}
