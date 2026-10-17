import type { HttpClient } from './httpClient';
import type { ToolsOverview, TubeDLFinInfo } from './types';

const routePrefix = 'TubeDLFin';

/** Typed access to the plugin's endpoints. */
export class TubeDLFinApi {
    constructor(private readonly http: HttpClient) {}

    getInfo(): Promise<TubeDLFinInfo> {
        return this.http.getJson<TubeDLFinInfo>(`${routePrefix}/Info`);
    }

    getTools(): Promise<ToolsOverview> {
        return this.http.getJson<ToolsOverview>(`${routePrefix}/Tools`);
    }

    /** Installs missing tools and updates outdated ones; resolves when done. */
    updateTools(): Promise<ToolsOverview> {
        return this.http.postJson<ToolsOverview>(`${routePrefix}/Tools/Update`);
    }
}
