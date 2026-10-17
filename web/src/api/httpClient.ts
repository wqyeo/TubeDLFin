/** Sends authenticated requests to the Jellyfin server. */
export interface HttpClient {
    getJson<T>(path: string): Promise<T>;
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

    async getJson<T>(path: string): Promise<T> {
        try {
            return await this.apiClient.getJSON<T>(this.apiClient.getUrl(path));
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
