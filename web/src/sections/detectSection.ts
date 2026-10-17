import type { TubeDLFinApi } from '../api/tubeDlFinApi';
import type { ProbeRequest } from '../api/types';
import { describeError, isAbortError } from '../errors';
import type { DetectStore } from '../state/detectStore';
import type { PageSection } from './pageSection';

/** Something that asks for a link to be detected: the form, a channel tab, the playlist switch. */
export interface DetectRequester {
    onDetect(handler: (request: ProbeRequest) => void): void;
}

/** Something that offers to cancel a running detection. */
export interface DetectCanceller {
    onCancel(handler: () => void): void;
}

/**
 * The "Detect" step: runs one detection at a time and records its progress in the store.
 * Starting a new detection, cancelling or leaving the page aborts the running one, which
 * stops yt-dlp on the server.
 */
export class DetectSection implements PageSection {
    private running: AbortController | undefined;

    constructor(
        private readonly api: TubeDLFinApi,
        private readonly store: DetectStore,
        requesters: readonly DetectRequester[],
        canceller: DetectCanceller,
    ) {
        for (const requester of requesters) {
            requester.onDetect((request) => {
                void this.detect(request);
            });
        }

        canceller.onCancel(() => this.cancel());
    }

    load(): Promise<void> {
        // Nothing to fetch; a previous result stays on screen when the admin comes back.
        return Promise.resolve();
    }

    hide(): void {
        this.cancel();
    }

    async detect(request: ProbeRequest): Promise<void> {
        this.running?.abort();
        const controller = new AbortController();
        this.running = controller;
        this.store.set({ status: 'detecting', request, startedAt: Date.now() });

        try {
            const result = await this.api.probe(request, controller.signal);
            if (this.running === controller) {
                this.store.set({ status: 'detected', request, result });
            }
        } catch (error) {
            if (this.running === controller && !isAbortError(error)) {
                this.store.set({ status: 'failed', request, error: describeError(error) });
            }
        } finally {
            if (this.running === controller) {
                this.running = undefined;
            }
        }
    }

    cancel(): void {
        if (!this.running) {
            return;
        }

        this.running.abort();
        this.running = undefined;
        this.store.set({ status: 'idle' });
    }
}
