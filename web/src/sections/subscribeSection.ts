import type { TubeDLFinApi } from '../api/tubeDlFinApi';
import type { LibraryInfo } from '../api/types';
import type { DownloadFormValues } from '../downloads/downloadRequest';
import { categoryOf } from '../downloads/libraryDefaults';
import type { OptionsMemory } from '../downloads/optionsMemory';
import { describeError } from '../errors';
import type { DetectState, DetectStore } from '../state/detectStore';
import { buildSubscriptionRequest, suggestedName } from '../subscriptions/subscriptionRequest';
import { intervalLabel } from '../subscriptions/subscriptionText';
import type { SubscribeForm } from '../views/subscribeForm';
import type { PageSection } from './pageSection';

/** Where the download options are read from: the download form. */
export interface DownloadOptionsSource {
    values(): DownloadFormValues;
    selectedLibrary(): LibraryInfo | undefined;
}

/** Something that shows the subscriptions and can be told to refresh them. */
export interface SubscriptionsRefresher {
    refresh(): Promise<void>;
}

/**
 * The "Subscribe" step: offered for a detected channel or playlist, it subscribes with the
 * options set in the download form.
 */
export class SubscribeSection implements PageSection {
    constructor(
        private readonly api: TubeDLFinApi,
        private readonly detectStore: DetectStore,
        private readonly view: SubscribeForm,
        private readonly options: DownloadOptionsSource,
        private readonly memory: OptionsMemory,
        private readonly subscriptions: SubscriptionsRefresher,
    ) {
        detectStore.subscribe((state) => this.onDetectChanged(state));
        view.onSubmit((name, hours) => {
            void this.subscribe(name, hours);
        });
    }

    load(): Promise<void> {
        return Promise.resolve();
    }

    private onDetectChanged(state: DetectState): void {
        if (state.status === 'detected' && state.result.Kind !== 'Video') {
            this.view.show(suggestedName(state.result));
        } else {
            this.view.hide();
        }
    }

    private async subscribe(name: string, intervalHours: number): Promise<void> {
        const state = this.detectStore.current;
        if (state.status !== 'detected') {
            return;
        }

        this.view.setBusy(true);
        try {
            const values = this.options.values();
            const subscription = await this.api.addSubscription(
                buildSubscriptionRequest(values, state.request, state.result, name, intervalHours),
            );
            const library = this.options.selectedLibrary();
            if (library) {
                this.memory.save(categoryOf(library), values);
            }

            this.view.showDone(
                `Subscribed to "${subscription.Name}". New uploads will be checked ${intervalLabel(subscription.IntervalHours)}; see the Subscriptions tab.`,
            );
            await this.subscriptions.refresh();
        } catch (error) {
            this.view.showError(describeError(error));
        } finally {
            this.view.setBusy(false);
        }
    }
}
