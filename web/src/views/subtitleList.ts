import type { SubtitleTrack } from '../api/types';
import { disclosure, element } from './elements';

/** Uploaded subtitles as chips; auto-generated captions folded away (YouTube lists ~150 translations). */
export function subtitleList(
    uploaded: readonly SubtitleTrack[],
    automatic: readonly SubtitleTrack[],
): HTMLElement {
    const block = element('div', null, 'tubedlfin-subtitles');
    block.append(element('h4', 'Subtitles'));

    if (uploaded.length === 0 && automatic.length === 0) {
        block.append(element('p', 'None available.', 'fieldDescription'));
        return block;
    }

    block.append(uploaded.length > 0 ? chips(uploaded) : element('p', 'None uploaded.', 'fieldDescription'));

    if (automatic.length > 0) {
        block.append(disclosure(`Auto-generated (${automatic.length})`, false, chips(automatic)));
    }

    return block;
}

function chips(tracks: readonly SubtitleTrack[]): HTMLElement {
    const list = element('ul', null, 'tubedlfin-chips');
    list.append(
        ...tracks.map((track) => {
            const chip = element('li', track.Name ?? track.Code, 'tubedlfin-chip');
            chip.title = track.Code;
            return chip;
        }),
    );
    return list;
}
