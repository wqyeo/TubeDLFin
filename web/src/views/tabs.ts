/**
 * Switches between the page's tabs: buttons marked `data-tubedlfin-tab="name"` show the
 * panel marked `data-tubedlfin-panel="name"` and hide the others.
 */
export class TabBar {
    private readonly buttons: HTMLButtonElement[];
    private readonly panels: HTMLElement[];

    constructor(root: ParentNode) {
        this.buttons = [...root.querySelectorAll<HTMLButtonElement>('[data-tubedlfin-tab]')];
        this.panels = [...root.querySelectorAll<HTMLElement>('[data-tubedlfin-panel]')];
        for (const tabButton of this.buttons) {
            tabButton.addEventListener('click', () => this.select(tabButton.dataset.tubedlfinTab ?? ''));
        }
    }

    select(name: string): void {
        for (const tabButton of this.buttons) {
            const selected = tabButton.dataset.tubedlfinTab === name;
            tabButton.classList.toggle('tubedlfin-tab-selected', selected);
            tabButton.setAttribute('aria-selected', String(selected));
        }

        for (const panel of this.panels) {
            panel.hidden = panel.dataset.tubedlfinPanel !== name;
        }
    }
}
