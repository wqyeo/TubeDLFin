import type { ProbeRequest } from '../api/types';
import type { DetectCanceller, DetectRequester } from '../sections/detectSection';
import type { DetectState } from '../state/detectStore';

/** Elements of the Detect form. */
export interface DetectFormElements {
    form: HTMLFormElement;
    url: HTMLInputElement;
    submit: HTMLButtonElement;
    cancel: HTMLButtonElement;
    status: HTMLElement;
    error: HTMLElement;
}

/** The link box with Detect and Cancel. Shows how long a detection has been running. */
export class DetectForm implements DetectRequester, DetectCanceller {
    private timer: number | undefined;

    constructor(private readonly elements: DetectFormElements) {}

    onDetect(handler: (request: ProbeRequest) => void): void {
        this.elements.form.addEventListener('submit', (event) => {
            event.preventDefault();
            handler({ Url: this.elements.url.value.trim(), NoPlaylist: false });
        });
    }

    onCancel(handler: () => void): void {
        this.elements.cancel.addEventListener('click', handler);
    }

    render(state: DetectState): void {
        this.stopTimer();
        const detecting = state.status === 'detecting';
        this.elements.submit.disabled = detecting;
        this.elements.cancel.hidden = !detecting;
        this.elements.status.textContent = '';
        this.elements.error.hidden = state.status !== 'failed';

        switch (state.status) {
            case 'detecting':
                // A channel tab or the playlist switch can start a detection: show its link.
                this.elements.url.value = state.request.Url;
                this.startTimer(state.startedAt);
                break;
            case 'failed':
                this.elements.error.textContent = state.error;
                break;
            default:
                break;
        }
    }

    private startTimer(startedAt: number): void {
        const update = () => {
            const seconds = Math.floor((Date.now() - startedAt) / 1000);
            this.elements.status.textContent =
                seconds < 3
                    ? 'Detecting…'
                    : `Detecting… ${seconds} s (long playlists and channels take a while)`;
        };

        update();
        this.timer = window.setInterval(update, 1000);
    }

    private stopTimer(): void {
        if (this.timer !== undefined) {
            window.clearInterval(this.timer);
            this.timer = undefined;
        }
    }
}
