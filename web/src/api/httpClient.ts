/** Sends authenticated requests to the Jellyfin server. */
export interface HttpClient {
    getJson<T>(path: string): Promise<T>;
    postJson<T>(path: string, body?: unknown): Promise<T>;
}

/** The server answered with an error status. */
export class HttpError extends Error {
    constructor(
        readonly status: number,
        message: string,
    ) {
        super(message);
        this.name = 'HttpError';
    }
}

/** {@link HttpClient} backed by jellyfin-web's global ApiClient, which adds the user's access token. */
export class JellyfinHttpClient implements HttpClient {
    constructor(private readonly apiClient: JellyfinApiClient) {}

    getJson<T>(path: string): Promise<T> {
        return this.send(() => this.apiClient.getJSON<T>(this.apiClient.getUrl(path)));
    }

    postJson<T>(path: string, body?: unknown): Promise<T> {
        return this.send(() =>
            this.apiClient.ajax<T>({
                type: 'POST',
                url: this.apiClient.getUrl(path),
                dataType: 'json',
                ...(body === undefined
                    ? {}
                    : { data: JSON.stringify(body), contentType: 'application/json' }),
            }),
        );
    }

    private async send<T>(request: () => Promise<T>): Promise<T> {
        try {
            return await request();
        } catch (error) {
            throw toError(error);
        }
    }
}

function toError(error: unknown): Error {
    if (error instanceof Response) {
        return new HttpError(error.status, `${error.status} ${error.statusText}`.trim());
    }

    return error instanceof Error ? error : new Error(String(error));
}
