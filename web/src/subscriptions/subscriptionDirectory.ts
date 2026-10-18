import type { SubscriptionInfo } from '../api/types';

/** Describes who added a download, for the queue. */
export interface SourceDescriber {
    /** A label such as `Subscription "Blender"`, or undefined for downloads added by hand. */
    describe(source: string): string | undefined;
}

const subscriptionPrefix = 'Subscription:';

/** Ids compare without dashes or case: the server writes them as 32 hex digits. */
function normalizeId(id: string): string {
    return id.replace(/-/g, '').toLowerCase();
}

/** The subscriptions' names, kept up to date by the Subscriptions tab. */
export class SubscriptionDirectory implements SourceDescriber {
    private names = new Map<string, string>();
    private readonly listeners: Array<() => void> = [];

    /** Called after the names change, e.g. to redraw the queue's labels. */
    subscribe(listener: () => void): void {
        this.listeners.push(listener);
    }

    update(subscriptions: readonly SubscriptionInfo[]): void {
        this.names = new Map(subscriptions.map((s) => [normalizeId(s.Id), s.Name]));
        for (const listener of this.listeners) {
            listener();
        }
    }

    describe(source: string): string | undefined {
        if (!source.startsWith(subscriptionPrefix)) {
            return undefined;
        }

        const name = this.names.get(normalizeId(source.slice(subscriptionPrefix.length)));
        return name ? `Subscription "${name}"` : 'From a subscription';
    }
}
