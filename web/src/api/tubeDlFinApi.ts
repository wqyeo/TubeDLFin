import type { HttpClient } from './httpClient';
import type {
    ClearFinishedResult,
    DownloadJobInfo,
    DownloadRequest,
    LibraryInfo,
    NamingPreset,
    ProbeRequest,
    ProbeResult,
    SubscriptionInfo,
    SubscriptionRequest,
    SubscriptionUpdate,
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

    getNamingPresets(): Promise<NamingPreset[]> {
        return this.http.getJson<NamingPreset[]>(`${routePrefix}/NamingPresets`);
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

    getSubscriptions(): Promise<SubscriptionInfo[]> {
        return this.http.getJson<SubscriptionInfo[]>(`${routePrefix}/Subscriptions`);
    }

    /** Lists the channel or playlist (a few seconds) and subscribes to it. */
    addSubscription(request: SubscriptionRequest): Promise<SubscriptionInfo> {
        return this.http.postJson<SubscriptionInfo>(`${routePrefix}/Subscriptions`, request);
    }

    updateSubscription(id: string, update: SubscriptionUpdate): Promise<SubscriptionInfo> {
        return this.http.putJson<SubscriptionInfo>(subscriptionPath(id), update);
    }

    /** Deletes a subscription; downloaded files and queued downloads are kept. */
    deleteSubscription(id: string): Promise<void> {
        return this.http.delete(subscriptionPath(id));
    }

    /** Checks now and queues new uploads; resolves with the subscription after the check. */
    checkSubscription(id: string): Promise<SubscriptionInfo> {
        return this.http.postJson<SubscriptionInfo>(`${subscriptionPath(id)}/Check`);
    }

    setSubscriptionPaused(id: string, paused: boolean): Promise<SubscriptionInfo> {
        return this.http.postJson<SubscriptionInfo>(`${subscriptionPath(id)}/${paused ? 'Pause' : 'Resume'}`);
    }
}

function subscriptionPath(id: string): string {
    return `${routePrefix}/Subscriptions/${encodeURIComponent(id)}`;
}
