import type { TubeDLFinApi } from '../api/tubeDlFinApi';
import type { LibraryInfo } from '../api/types';
import { buildDownloadRequest, type DownloadFormValues } from '../downloads/downloadRequest';
import { categoryOf } from '../downloads/libraryDefaults';
import type { OptionsMemory } from '../downloads/optionsMemory';
import { describeError } from '../errors';
import type { DetectStore } from '../state/detectStore';
import { buildSubscriptionRequest } from '../subscriptions/subscriptionRequest';
import { subscribedMessage } from '../subscriptions/subscriptionText';
import type { SubscribeFormValues } from '../views/subscribeForm';
import type { QueueRefresher } from './downloadSection';

/** Where the outcome of submitting the form is shown: the download form's button and status. */
export interface SubmitFeedback {
    setBusy(busy: boolean): void;
    showDone(message: string): void;
    showError(message: string): void;
    selectedLibrary(): LibraryInfo | undefined;
}

/** Something that shows the subscriptions and can be told to refresh them. */
export interface SubscriptionsRefresher {
    refresh(): Promise<void>;
}

/**
 * Subscribing: when the form is submitted to subscribe (see ActionSection), subscribes to the
 * detected channel or playlist with the form's options, and if asked also queues what it has now.
 */
export class SubscribeSection {
    constructor(
        private readonly api: TubeDLFinApi,
        private readonly detectStore: DetectStore,
        private readonly feedback: SubmitFeedback,
        private readonly memory: OptionsMemory,
        private readonly queue: QueueRefresher,
        private readonly subscriptions: SubscriptionsRefresher,
    ) {}

    async submit(values: DownloadFormValues, subscription: SubscribeFormValues): Promise<void> {
        const state = this.detectStore.current;
        if (state.status !== 'detected') {
            return;
        }

        this.feedback.setBusy(true);
        try {
            const added = await this.api.addSubscription(
                buildSubscriptionRequest(
                    values,
                    state.request,
                    state.result,
                    subscription.name,
                    subscription.intervalHours,
                ),
            );
            const library = this.feedback.selectedLibrary();
            if (library) {
                this.memory.save(categoryOf(library), values);
            }

            await this.subscriptions.refresh();
            if (subscription.includeExisting) {
                // Subscribed first: if that fails (already subscribed), nothing is queued either.
                try {
                    await this.api.addDownload(
                        buildDownloadRequest({ ...values, redownload: false }, state.request, state.result),
                    );
                    await this.queue.refresh();
                } catch (error) {
                    this.feedback.showError(
                        `Subscribed to "${added.Name}", but the videos it has now could not be queued: ${describeError(error)}`,
                    );
                    return;
                }
            }

            this.feedback.showDone(
                subscribedMessage(added.Name, added.IntervalHours, subscription.includeExisting),
            );
        } catch (error) {
            this.feedback.showError(describeError(error));
        } finally {
            this.feedback.setBusy(false);
        }
    }
}
