/** Tells the admin that something happened. */
export interface Notifier {
    settingsSaved(): void;
}

/** {@link Notifier} using the dashboard's own toast. */
export class DashboardNotifier implements Notifier {
    constructor(private readonly dashboard: JellyfinDashboard) {}

    settingsSaved(): void {
        this.dashboard.processPluginConfigurationUpdateResult();
    }
}
