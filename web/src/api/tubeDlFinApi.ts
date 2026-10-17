import type { HttpClient } from './httpClient';
import type {
    ClearFinishedResult,
    DownloadJobInfo,
    DownloadRequest,
    LibraryInfo,
    ProbeRequest,
    ProbeResult,
    ToolsOverview,
    TubeDLFinInfo,
} from './types';

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

    getLibraries(): Promise<LibraryInfo[]> {
        return this.http.getJson<LibraryInfo[]>(`${routePrefix}/Libraries`);
    }

    getDownloads(): Promise<DownloadJobInfo[]> {
        return this.http.getJson<DownloadJobInfo[]>(`${routePrefix}/Downloads`);
    }

    addDownload(request: DownloadRequest): Promise<DownloadJobInfo> {
        return this.http.postJson<DownloadJobInfo>(`${routePrefix}/Downloads`, request);
    }

    getDownloadLog(id: string): Promise<string[]> {
        return this.http.getJson<string[]>(`${routePrefix}/Downloads/${encodeURIComponent(id)}/Log`);
    }

    cancelDownload(id: string): Promise<DownloadJobInfo> {
        return this.http.postJson<DownloadJobInfo>(
            `${routePrefix}/Downloads/${encodeURIComponent(id)}/Cancel`,
        );
    }

    /** Removes a finished download from the list; its files are kept. */
    removeDownload(id: string): Promise<void> {
        return this.http.delete(`${routePrefix}/Downloads/${encodeURIComponent(id)}`);
    }

    clearFinishedDownloads(): Promise<ClearFinishedResult> {
        return this.http.postJson<ClearFinishedResult>(`${routePrefix}/Downloads/ClearFinished`);
    }
}
