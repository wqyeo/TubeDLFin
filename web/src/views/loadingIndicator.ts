/** Shows that the page is busy. */
export interface LoadingIndicator {
    show(): void;
    hide(): void;
}

/** {@link LoadingIndicator} using the dashboard's own spinner. */
export class DashboardLoadingIndicator implements LoadingIndicator {
    constructor(private readonly dashboard: JellyfinDashboard) {}

    show(): void {
        this.dashboard.showLoadingMsg();
    }

    hide(): void {
        this.dashboard.hideLoadingMsg();
    }
}
