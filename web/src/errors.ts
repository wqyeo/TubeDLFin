import { HttpError } from './api/httpClient';

/** Turns a failed API call into a sentence for the admin. */
export function describeError(error: unknown): string {
    if (error instanceof HttpError) {
        if (error.status === 403) {
            return 'TubeDLFin is only available to administrators.';
        }

        if (error.detail) {
            return error.detail;
        }
    }

    const reason = error instanceof Error ? error.message : String(error);
    return `Could not reach the TubeDLFin API: ${reason}`;
}

/** Whether a request failed because it was aborted on purpose. */
export function isAbortError(error: unknown): boolean {
    return error instanceof DOMException && error.name === 'AbortError';
}
