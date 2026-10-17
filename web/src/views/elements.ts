import { safeLinkUrl } from './format';

/** Small DOM builders. Text always goes through textContent, never innerHTML. */
export function element<K extends keyof HTMLElementTagNameMap>(
    tag: K,
    text?: string | null,
    className?: string,
): HTMLElementTagNameMap[K] {
    const node = document.createElement(tag);
    if (text != null) {
        node.textContent = text;
    }

    if (className) {
        node.className = className;
    }

    return node;
}

/** A link opening in a new tab, or plain text if the URL isn't an http(s) link. */
export function externalLink(text: string, url: string | null | undefined, className?: string): HTMLElement {
    const href = safeLinkUrl(url);
    if (!href) {
        return element('span', text, className);
    }

    const anchor = element('a', text, className);
    anchor.href = href;
    anchor.target = '_blank';
    anchor.rel = 'noopener noreferrer';
    return anchor;
}

/** A plain button that calls `onClick`. */
export function button(text: string, onClick: () => void, className = 'emby-button'): HTMLButtonElement {
    const node = element('button', text, className);
    node.type = 'button';
    node.addEventListener('click', onClick);
    return node;
}

/** A table with a header row. */
export function table(headers: readonly string[], rows: readonly HTMLTableRowElement[]): HTMLTableElement {
    const head = element('thead');
    const headRow = element('tr');
    headRow.append(...headers.map((header) => element('th', header)));
    head.append(headRow);

    const body = element('tbody');
    body.append(...rows);

    const node = element('table', null, 'tubedlfin-table');
    node.append(head, body);
    return node;
}

/** A table row of text cells (or ready-made nodes). */
export function row(...cells: ReadonlyArray<string | Node>): HTMLTableRowElement {
    const tr = element('tr');
    for (const cell of cells) {
        const td = element('td');
        td.append(cell);
        tr.append(td);
    }

    return tr;
}

/** A collapsible block. */
export function disclosure(summary: string, open: boolean, ...content: Node[]): HTMLDetailsElement {
    const details = element('details', null, 'tubedlfin-details');
    details.open = open;
    details.append(element('summary', summary), ...content);
    return details;
}
