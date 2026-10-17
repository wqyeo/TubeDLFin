import type { ProbeRequest, ProbeResult } from '../api/types';

/** Where the Detect step is. */
export type DetectState =
    | { readonly status: 'idle' }
    | { readonly status: 'detecting'; readonly request: ProbeRequest; readonly startedAt: number }
    | { readonly status: 'detected'; readonly request: ProbeRequest; readonly result: ProbeResult }
    | { readonly status: 'failed'; readonly request: ProbeRequest; readonly error: string };

/** Called with the new state whenever it changes. */
export type DetectListener = (state: DetectState) => void;

/**
 * Holds the Detect step's state and tells subscribers when it changes. Views render from it,
 * and later steps (the download options) read the detected result from it.
 */
export class DetectStore {
    private state: DetectState = { status: 'idle' };
    private readonly listeners = new Set<DetectListener>();

    get current(): DetectState {
        return this.state;
    }

    set(state: DetectState): void {
        this.state = state;
        for (const listener of this.listeners) {
            listener(state);
        }
    }

    /** Subscribes and immediately reports the current state. Returns a function that unsubscribes. */
    subscribe(listener: DetectListener): () => void {
        this.listeners.add(listener);
        listener(this.state);
        return () => this.listeners.delete(listener);
    }
}
