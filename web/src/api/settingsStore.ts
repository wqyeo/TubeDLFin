import type { TubeDLFinSettings } from './types';

/** Loads and saves the plugin settings. */
export interface SettingsStore {
    load(): Promise<TubeDLFinSettings>;
    save(settings: TubeDLFinSettings): Promise<void>;
}

/** {@link SettingsStore} using Jellyfin's standard plugin configuration endpoints. */
export class JellyfinSettingsStore implements SettingsStore {
    constructor(
        private readonly apiClient: JellyfinApiClient,
        private readonly pluginId: string,
    ) {}

    async load(): Promise<TubeDLFinSettings> {
        const configuration = await this.apiClient.getPluginConfiguration(this.pluginId);
        return {
            YtDlpPath: stringOrEmpty(configuration.YtDlpPath),
            DenoPath: stringOrEmpty(configuration.DenoPath),
        };
    }

    async save(settings: TubeDLFinSettings): Promise<void> {
        // Re-read first so settings this page doesn't know about are kept.
        const configuration = await this.apiClient.getPluginConfiguration(this.pluginId);
        await this.apiClient.updatePluginConfiguration(this.pluginId, { ...configuration, ...settings });
    }
}

function stringOrEmpty(value: unknown): string {
    return typeof value === 'string' ? value : '';
}
