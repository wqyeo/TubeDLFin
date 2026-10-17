/** Finds a required element inside the page, failing loudly if the HTML and script disagree. */
export function requireElement<T extends HTMLElement = HTMLElement>(root: ParentNode, selector: string): T {
    const element = root.querySelector<T>(selector);
    if (!element) {
        throw new Error(`TubeDLFin: element "${selector}" is missing from configPage.html`);
    }

    return element;
}

/** Adds the page's stylesheet once per view. */
export function injectStyles(view: HTMLElement, css: string): void {
    if (view.querySelector('style[data-tubedlfin]')) {
        return;
    }

    const style = document.createElement('style');
    style.dataset.tubedlfin = '';
    style.textContent = css;
    view.prepend(style);
}
