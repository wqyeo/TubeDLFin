import type { SubscriptionInfo } from '../api/types';

/** How long the scheduled task waits between runs; a due subscription is checked within this. */
const taskIntervalMinutes = 15;

/** "every hour", "every 6 hours", "every day", "every 2 days", "every week". */
export function intervalLabel(hours: number): string {
    if (hours === 1) {
        return 'every hour';
    }

    if (hours % 168 === 0) {
        return hours === 168 ? 'every week' : `every ${hours / 168} weeks`;
    }

    if (hours % 24 === 0) {
        return hours === 24 ? 'every day' : `every ${hours / 24} days`;
    }

    return `every ${hours} hours`;
}

/** When it was and will be checked, e.g. "Last checked … · next check …". */
export function checkSummary(
    subscription: SubscriptionInfo,
    now: Date,
    formatTime: (iso: string) => string = (iso) => new Date(iso).toLocaleString(),
): string {
    const last = subscription.LastCheckedAt
        ? `Last checked ${formatTime(subscription.LastCheckedAt)}`
        : 'Not checked yet';
    if (subscription.Paused) {
        return `${last} · paused`;
    }

    const next = new Date(subscription.NextCheckAt);
    const nextText =
        next.getTime() <= now.getTime()
            ? `next check within ${taskIntervalMinutes} minutes`
            : `next check ${formatTime(subscription.NextCheckAt)}`;
    return `${last} · ${nextText}`;
}

/** "3 downloaded · 1 in the queue · 1 failed 3 times", leaving out zeros. */
export function countsSummary(subscription: SubscriptionInfo): string {
    const parts = [`${subscription.Downloaded} downloaded`];
    if (subscription.Queued > 0) {
        parts.push(`${subscription.Queued} in the queue`);
    }

    if (subscription.GivenUp > 0) {
        parts.push(`${subscription.GivenUp} failed 3 times, not tried again`);
    }

    return parts.join(' · ');
}

/** What subscribing did, shown next to the form's button. */
export function subscribedMessage(name: string, intervalHours: number, queuedExisting: boolean): string {
    const queued = queuedExisting ? ' and added the videos it has now to the queue' : '';
    return `Subscribed to "${name}"${queued}. New uploads will be checked ${intervalLabel(intervalHours)}; see the Subscriptions tab.`;
}
