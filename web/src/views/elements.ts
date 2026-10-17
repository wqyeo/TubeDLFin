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
