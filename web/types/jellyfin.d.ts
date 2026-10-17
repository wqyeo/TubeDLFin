// Minimal typings for the globals jellyfin-web 12.2 exposes to plugin pages.
// Only what TubeDLFin uses is declared; see jellyfin-web's src/apiclient.d.ts for the full surface.

interface JellyfinApiClient {
    /** Builds an absolute server URL for an API path such as "TubeDLFin/Info". */
    getUrl(name: string, params?: Record<string, unknown>): string;
    /** Adds the signed-in user's `Authorization` header (client, device and access token) to `headers`. */
    setRequestHeaders(headers: Record<string, string>): void;
    /** Reads a plugin's saved configuration object. */
    getPluginConfiguration(pluginId: string): Promise<Record<string, unknown>>;
    /** Replaces a plugin's saved configuration object. */
    updatePluginConfiguration(pluginId: string, configuration: Record<string, unknown>): Promise<void>;
}

interface JellyfinDashboard {
    showLoadingMsg(): void;
    hideLoadingMsg(): void;
    /** Hides the spinner and shows the standard "Settings saved" toast. */
    processPluginConfigurationUpdateResult(): void;
}

declare var ApiClient: JellyfinApiClient;
declare var Dashboard: JellyfinDashboard;

declare module '*.css' {
    const content: string;
    export default content;
}
