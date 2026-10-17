import type { HttpClient } from './httpClient';
import type { ProbeRequest, ProbeResult, ToolsOverview, TubeDLFinInfo } from './types';

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

    /** Detects what a link points to. Aborting the signal stops yt-dlp on the server. */
    probe(request: ProbeRequest, signal?: AbortSignal): Promise<ProbeResult> {
        return this.http.postJson<ProbeResult>(`${routePrefix}/Probe`, request, signal ? { signal } : {});
    }
}
