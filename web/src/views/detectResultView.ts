import type { ProbeRequest, ProbeResult } from '../api/types';
import type { DetectRequester } from '../sections/detectSection';
import type { DetectState } from '../state/detectStore';
import { button, element } from './elements';
import { entryList } from './entryList';
import { formatTables } from './formatTables';
import { mediaCard } from './mediaCard';
import { subtitleList } from './subtitleList';

/** Shows what was detected. Offers the video/playlist switch and opening channel tabs. */
export class DetectResultView implements DetectRequester {
    private handler: ((request: ProbeRequest) => void) | undefined;

    constructor(private readonly container: HTMLElement) {}

    onDetect(handler: (request: ProbeRequest) => void): void {
        this.handler = handler;
    }

    render(state: DetectState): void {
        switch (state.status) {
            case 'detected':
                this.container.replaceChildren(...this.build(state.request, state.result));
                this.container.classList.remove('tubedlfin-stale');
                this.container.hidden = false;
                break;
            case 'detecting':
                // Keep the previous result visible, dimmed, so the page doesn't jump.
                this.container.classList.add('tubedlfin-stale');
                break;
            default:
                this.container.replaceChildren();
                this.container.classList.remove('tubedlfin-stale');
                this.container.hidden = true;
        }
    }

    private build(request: ProbeRequest, result: ProbeResult): HTMLElement[] {
        const blocks: HTMLElement[] = [];
        if (result.HasPlaylistChoice) {
            blocks.push(this.playlistChoice(request));
        }

        blocks.push(mediaCard(result));

        if (result.Warnings.length > 0) {
            const warnings = element('ul', null, 'tubedlfin-warnings');
            warnings.append(...result.Warnings.map((warning) => element('li', warning)));
            blocks.push(warnings);
        }

        if (result.Kind === 'Video') {
            blocks.push(...formatTables(result.VideoFormats, result.AudioFormats));
            blocks.push(subtitleList(result.Subtitles, result.AutomaticCaptions));
        } else {
            blocks.push(
                entryList(
                    result,
                    (entry) => entry.Url && this.handler?.({ Url: entry.Url, NoPlaylist: false }),
                ),
            );
        }

        return blocks;
    }

    private playlistChoice(request: ProbeRequest): HTMLElement {
        const option = (label: string, noPlaylist: boolean) => {
            const selected = request.NoPlaylist === noPlaylist;
            const node = button(
                label,
                () => {
                    if (!selected) {
                        this.handler?.({ Url: request.Url, NoPlaylist: noPlaylist });
                    }
                },
                `tubedlfin-segment${selected ? ' tubedlfin-segment-selected' : ''}`,
            );
            node.setAttribute('aria-pressed', String(selected));
            return node;
        };

        const block = element('div', null, 'tubedlfin-choice');
        block.append(
            element('span', 'This link is a video inside a playlist:'),
            option('Only this video', true),
            option('Whole playlist', false),
        );
        return block;
    }
}
