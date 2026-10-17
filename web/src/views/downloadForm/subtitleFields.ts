import type { SubtitleTrack } from '../../api/types';
import { parseLanguages, toggleLanguage } from '../../downloads/subtitleLanguages';
import { element } from '../elements';

/** Elements of the subtitle fields. */
export interface SubtitleElements {
    chips: HTMLElement;
    languages: HTMLInputElement;
    includeAutomatic: HTMLInputElement;
    embed: HTMLInputElement;
}

/** The subtitle choice, as the form holds it. */
export interface SubtitleValues {
    subtitleLanguages: string;
    includeAutomaticSubtitles: boolean;
    embedSubtitles: boolean;
}

/** Language chips from the detected video (click to add or remove) and the codes box. */
export class SubtitleFields {
    constructor(private readonly elements: SubtitleElements) {
        elements.languages.addEventListener('input', () => this.syncChips());
    }

    /** Offers the detected languages: uploaded ones, then the original-language auto captions. */
    showTracks(uploaded: readonly SubtitleTrack[], automatic: readonly SubtitleTrack[]): void {
        const offered = [...uploaded, ...automatic.filter((track) => track.Code.endsWith('-orig'))];
        this.elements.chips.replaceChildren(
            ...offered.map((track) => {
                const chip = element('button', track.Name ?? track.Code, 'tubedlfin-chip-button');
                chip.type = 'button';
                chip.dataset.code = track.Code;
                chip.title = track.Code;
                chip.addEventListener('click', () => {
                    this.elements.languages.value = toggleLanguage(this.elements.languages.value, track.Code);
                    if (track.Code.endsWith('-orig')) {
                        this.elements.includeAutomatic.checked = true; // "-orig" tracks are auto-generated.
                    }

                    this.syncChips();
                });
                const item = element('li');
                item.append(chip);
                return item;
            }),
        );
        this.elements.chips.hidden = offered.length === 0;
        this.syncChips();
    }

    read(): SubtitleValues {
        return {
            subtitleLanguages: this.elements.languages.value,
            includeAutomaticSubtitles: this.elements.includeAutomatic.checked,
            embedSubtitles: this.elements.embed.checked,
        };
    }

    write(values: Partial<SubtitleValues>): void {
        if (values.subtitleLanguages !== undefined) {
            this.elements.languages.value = values.subtitleLanguages;
        }

        if (values.includeAutomaticSubtitles !== undefined) {
            this.elements.includeAutomatic.checked = values.includeAutomaticSubtitles;
        }

        if (values.embedSubtitles !== undefined) {
            this.elements.embed.checked = values.embedSubtitles;
        }

        this.syncChips();
    }

    private syncChips(): void {
        const selected = new Set(parseLanguages(this.elements.languages.value));
        for (const chip of this.elements.chips.querySelectorAll<HTMLButtonElement>('button[data-code]')) {
            chip.setAttribute('aria-pressed', String(selected.has(chip.dataset.code ?? '')));
        }
    }
}
