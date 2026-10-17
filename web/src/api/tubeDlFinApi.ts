import type { HttpClient } from './httpClient';
import type { TubeDLFinInfo } from './types';

const routePrefix = 'TubeDLFin';

/** Typed access to the plugin's endpoints. */
export class TubeDLFinApi {
    constructor(private readonly http: HttpClient) {}

    getInfo(): Promise<TubeDLFinInfo> {
        return this.http.getJson<TubeDLFinInfo>(`${routePrefix}/Info`);
    }
}
