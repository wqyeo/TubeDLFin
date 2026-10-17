// Minimal typings for the globals jellyfin-web 12.2 exposes to plugin pages.
// Only what TubeDLFin uses is declared; see jellyfin-web's src/apiclient.d.ts for the full surface.

interface JellyfinAjaxRequest {
    type: 'GET' | 'POST' | 'PUT' | 'DELETE';
    url: string;
    data?: string;
    contentType?: string;
    dataType?: 'json' | 'text';
}

interface JellyfinApiClient {
    /** Builds an absolute server URL for an API path such as "TubeDLFin/Info". */
    getUrl(name: string, params?: Record<string, unknown>): string;
    /** Sends a request with the user's access token. Rejects with the Response on HTTP errors. */
    ajax<T = unknown>(request: JellyfinAjaxRequest): Promise<T>;
    /** GETs and parses JSON with the user's access token. Rejects with the Response on HTTP errors. */
    getJSON<T = unknown>(url: string): Promise<T>;
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
