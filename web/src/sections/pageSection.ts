/** One independently loaded part of the page. A section shows its own errors. */
export interface PageSection {
    load(): Promise<void>;
}
