/** One independently loaded part of the page. A section shows its own errors. */
export interface PageSection {
    /** Called whenever the page is shown. */
    load(): Promise<void>;
    /** Called when the admin leaves the page: stop work nobody will see. */
    hide?(): void;
}
