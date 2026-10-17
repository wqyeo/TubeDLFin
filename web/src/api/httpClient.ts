/** Options for a single request. */
export interface RequestOptions {
    /** Aborts the request; the server then stops whatever it was doing for it. */
    signal?: AbortSignal;
}

/** Sends authenticated requests to the Jellyfin server. */
export interface HttpClient {
    getJson<T>(path: string, options?: RequestOptions): Promise<T>;
    postJson<T>(path: string, body?: unknown, options?: RequestOptions): Promise<T>;
}

/** The server answered with an error status. `detail` is the server's explanation, if it gave one. */
export class HttpError extends Error {
    constructor(
        readonly status: number,
        message: string,
        readonly detail?: string,
    ) {
        super(message);
        this.name = 'HttpError';
    }
}

/**
 * {@link HttpClient} using `fetch`, so requests can be aborted, with the authorization header
 * jellyfin-web's global ApiClient builds for the signed-in user.
 */
export class JellyfinHttpClient implements HttpClient {
    constructor(private readonly apiClient: JellyfinApiClient) {}

    getJson<T>(path: string, options?: RequestOptions): Promise<T> {
        return this.send<T>('GET', path, undefined, options);
    }

    postJson<T>(path: string, body?: unknown, options?: RequestOptions): Promise<T> {
        return this.send<T>('POST', path, body, options);
    }

    private async send<T>(method: string, path: string, body: unknown, options?: RequestOptions): Promise<T> {
        const headers: Record<string, string> = { Accept: 'application/json' };
        this.apiClient.setRequestHeaders(headers);
        if (body !== undefined) {
            headers['Content-Type'] = 'application/json';
        }

        const response = await fetch(this.apiClient.getUrl(path), {
            method,
            headers,
            body: body === undefined ? null : JSON.stringify(body),
            signal: options?.signal ?? null,
        });

        if (!response.ok) {
            throw new HttpError(
                response.status,
                `${response.status} ${response.statusText}`.trim(),
                await readProblemDetail(response),
            );
        }

        return (await response.json()) as T;
    }
}

/** Reads the `detail` of an RFC 9457 problem response, which TubeDLFin uses for its error messages. */
async function readProblemDetail(response: Response): Promise<string | undefined> {
    try {
        const problem: unknown = await response.json();
        if (typeof problem === 'object' && problem !== null && 'detail' in problem) {
            return typeof problem.detail === 'string' ? problem.detail : undefined;
        }
    } catch {
        // Not JSON: fall back to the status line.
    }

    return undefined;
}
