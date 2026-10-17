import type { NamingPreset, NamingPresetId } from '../../api/types';
import { element } from '../elements';

/** Elements of the naming fields. */
export interface NamingElements {
    preset: HTMLSelectElement;
    customContainer: HTMLElement;
    custom: HTMLInputElement;
    example: HTMLElement;
}

/** How files are named. */
export interface Naming {
    naming: NamingPresetId;
    customTemplate: string;
}

/** The naming preset list (from the server), a custom template box, and an example. */
export class NamingFields {
    private presets: NamingPreset[] = [];

    constructor(private readonly elements: NamingElements) {
        elements.preset.addEventListener('change', () => this.update());
    }

    setPresets(presets: NamingPreset[]): void {
        this.presets = presets;
        const custom = element('option', 'Custom template');
        custom.value = 'Custom';
        this.elements.preset.replaceChildren(
            ...presets.map((preset) => {
                const node = element('option', preset.Name);
                node.value = preset.Id;
                return node;
            }),
            custom,
        );
        this.update();
    }

    read(): Naming {
        return {
            naming: this.elements.preset.value as NamingPresetId,
            customTemplate: this.elements.custom.value,
        };
    }

    write(values: Partial<Naming>): void {
        if (values.naming) {
            this.elements.preset.value = values.naming;
        }

        if (values.customTemplate !== undefined) {
            this.elements.custom.value = values.customTemplate;
        }

        this.update();
    }

    private update(): void {
        const custom = this.elements.preset.value === 'Custom';
        this.elements.customContainer.hidden = !custom;
        this.elements.example.textContent = custom
            ? 'as the template says'
            : (this.presets.find((preset) => preset.Id === this.elements.preset.value)?.Example ?? '');
    }
}
