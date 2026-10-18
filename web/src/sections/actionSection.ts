import type { DownloadFormValues } from '../downloads/downloadRequest';
import { formLayout } from '../downloads/formLayout';
import type { DetectState, DetectStore } from '../state/detectStore';
import { suggestedName } from '../subscriptions/subscriptionRequest';
import type { ActionChoice } from '../views/actionChoice';
import type { DownloadForm } from '../views/downloadForm/downloadForm';
import type { SubscribeForm, SubscribeFormValues } from '../views/subscribeForm';
import type { PageSection } from './pageSection';

/** Queues a download with the form's options. */
export interface DownloadSubmitter {
    submit(values: DownloadFormValues): Promise<void>;
}

/** Subscribes with the form's options. */
export interface SubscribeSubmitter {
    submit(values: DownloadFormValues, subscription: SubscribeFormValues): Promise<void>;
}

/**
 * The "Download now / Subscribe" choice for a detected channel or playlist: lays the form out
 * for it, and hands the submitted form to the download or the subscribe step.
 */
export class ActionSection implements PageSection {
    private isList = false;

    constructor(
        detectStore: DetectStore,
        private readonly choice: ActionChoice,
        private readonly subscribeForm: SubscribeForm,
        private readonly form: DownloadForm,
        private readonly download: DownloadSubmitter,
        private readonly subscribe: SubscribeSubmitter,
    ) {
        detectStore.subscribe((state) => this.onDetectChanged(state));
        choice.onChange(() => this.updateLayout());
        subscribeForm.onIncludeExistingChanged(() => this.updateLayout());
        form.onSubmit((values) => {
            void (this.isSubscribing()
                ? this.subscribe.submit(values, this.subscribeForm.values())
                : this.download.submit(values));
        });
    }

    load(): Promise<void> {
        return Promise.resolve();
    }

    private onDetectChanged(state: DetectState): void {
        if (state.status !== 'detected') {
            return;
        }

        this.isList = state.result.Kind !== 'Video';
        this.choice.reset();
        this.subscribeForm.reset(suggestedName(state.result));
        this.updateLayout();
    }

    private isSubscribing(): boolean {
        return this.isList && this.choice.selected() === 'subscribe';
    }

    private updateLayout(): void {
        const layout = formLayout(
            this.isList,
            this.choice.selected(),
            this.subscribeForm.values().includeExisting,
        );
        this.choice.setVisible(layout.showChoice);
        this.subscribeForm.setVisible(layout.showSubscribe);
        this.form.setLayout(layout);
    }
}
