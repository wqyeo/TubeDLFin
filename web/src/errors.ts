import { HttpError } from './api/httpClient';

/** Turns a failed API call into a sentence for the admin. */
export function describeError(error: unknown): string {
    if (error instanceof HttpError && error.status === 403) {
        return 'TubeDLFin is only available to administrators.';
    }

    const reason = error instanceof Error ? error.message : String(error);
    return `Could not reach the TubeDLFin API: ${reason}`;
}
