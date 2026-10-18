import type { TubeDLFinApi } from '../api/tubeDlFinApi';
import { describeError } from '../errors';
import type { SubscriptionDirectory } from '../subscriptions/subscriptionDirectory';
import type { SubscriptionsView } from '../views/subscriptionsView';
import type { QueueRefresher } from './downloadSection';
import type { PageSection } from './pageSection';
import type { SubscriptionsRefresher } from './subscribeSection';

/**
 * The Subscriptions tab: lists the subscriptions, keeps the queue's subscription names up to
 * date, and handles check, pause, edit and delete.
 */
export class SubscriptionsSection implements PageSection, SubscriptionsRefresher {
    constructor(
        private readonly api: TubeDLFinApi,
        private readonly view: SubscriptionsView,
        private readonly directory: SubscriptionDirectory,
        private readonly queue: QueueRefresher,
        private readonly confirm: (message: string) => boolean,
    ) {
        view.onCheck((id) => void this.check(id));
        view.onSetPaused((id, paused) => void this.act(() => api.setSubscriptionPaused(id, paused)));
        view.onSave(
            (id, name, hours) =>
                void this.act(() => api.updateSubscription(id, { Name: name, IntervalHours: hours })),
        );
        view.onDelete((id, name) => {
            if (this.confirm(`Delete the subscription "${name}"? Downloaded files are kept.`)) {
                void this.act(() => api.deleteSubscription(id));
            }
        });
    }

    load(): Promise<void> {
        return this.refresh();
    }

    async refresh(): Promise<void> {
        try {
            const subscriptions = await this.api.getSubscriptions();
            this.directory.update(subscriptions);
            this.view.render(subscriptions, new Date());
        } catch (error) {
            this.view.showError(describeError(error));
        }
    }

    private async check(id: string): Promise<void> {
        this.view.setBusy(id, 'Checking for new uploads…');
        await this.act(() => this.api.checkSubscription(id));
        await this.queue.refresh();
    }

    private async act(action: () => Promise<unknown>): Promise<void> {
        try {
            await action();
        } catch (error) {
            this.view.showError(describeError(error));
        }

        await this.refresh();
    }
}
