import type { SponsorBlockCategory, SponsorBlockMode } from '../../api/types';

/** Elements of the SponsorBlock fields. */
export interface SponsorBlockElements {
    mode: HTMLSelectElement;
    categories: HTMLElement;
}

/** The SponsorBlock choice, as the form holds it. */
export interface SponsorBlockValues {
    sponsorBlockMode: SponsorBlockMode;
    sponsorBlockCategories: SponsorBlockCategory[];
}

/** Cut out or mark, and which categories. */
export class SponsorBlockFields {
    constructor(private readonly elements: SponsorBlockElements) {}

    read(): SponsorBlockValues {
        return {
            sponsorBlockMode: this.elements.mode.value as SponsorBlockMode,
            sponsorBlockCategories: this.boxes()
                .filter((box) => box.checked)
                .map((box) => box.value as SponsorBlockCategory),
        };
    }

    write(values: Partial<SponsorBlockValues>): void {
        if (values.sponsorBlockMode && values.sponsorBlockMode !== 'Off') {
            this.elements.mode.value = values.sponsorBlockMode;
        }

        if (values.sponsorBlockCategories) {
            const selected = new Set<string>(values.sponsorBlockCategories);
            for (const box of this.boxes()) {
                box.checked = selected.has(box.value);
            }
        }
    }

    private boxes(): HTMLInputElement[] {
        return [...this.elements.categories.querySelectorAll<HTMLInputElement>('input[type="checkbox"]')];
    }
}
