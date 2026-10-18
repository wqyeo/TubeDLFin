import type { SubscriptionInfo } from '../api/types';
import { intervalChoices } from '../subscriptions/subscriptionRequest';
import { checkSummary, countsSummary, intervalLabel } from '../subscriptions/subscriptionText';
import { button, element, externalLink } from './elements';

/** What a subscription's buttons ask for. */
interface SubscriptionActions {
    check(id: string): void;
    setPaused(id: string, paused: boolean): void;
    save(id: string, name: string, intervalHours: number): void;
    remove(id: string, name: string): void;
}

/** Elements of the Subscriptions tab. */
export interface SubscriptionsViewElements {
    list: HTMLElement;
    empty: HTMLElement;
    error: HTMLElement;
}

/**
 * The subscriptions, by name. Rows are kept per subscription and updated in place, so an open
 * edit form survives a refresh.
 */
export class SubscriptionsView {
    private readonly rows = new Map<string, SubscriptionRow>();
    private readonly handlers: SubscriptionActions = {
        check: () => {},
        setPaused: () => {},
        save: () => {},
        remove: () => {},
    };

    // Rows call these; they forward to whatever handler is registered.
    private readonly actions: SubscriptionActions = {
        check: (id) => this.handlers.check(id),
        setPaused: (id, paused) => this.handlers.setPaused(id, paused),
        save: (id, name, hours) => this.handlers.save(id, name, hours),
        remove: (id, name) => this.handlers.remove(id, name),
    };

    constructor(private readonly elements: SubscriptionsViewElements) {}

    onCheck(handler: (id: string) => void): void {
        this.handlers.check = handler;
    }

    onSetPaused(handler: (id: string, paused: boolean) => void): void {
        this.handlers.setPaused = handler;
    }

    onSave(handler: (id: string, name: string, intervalHours: number) => void): void {
        this.handlers.save = handler;
    }

    onDelete(handler: (id: string, name: string) => void): void {
        this.handlers.remove = handler;
    }

    render(subscriptions: readonly SubscriptionInfo[], now: Date): void {
        const ids = new Set(subscriptions.map((s) => s.Id));
        for (const [id, row] of this.rows) {
            if (!ids.has(id)) {
                row.element.remove();
                this.rows.delete(id);
            }
        }

        subscriptions.forEach((subscription, index) => {
            let row = this.rows.get(subscription.Id);
            if (!row) {
                row = new SubscriptionRow(subscription.Id, this.actions);
                this.rows.set(subscription.Id, row);
            }

            row.update(subscription, now);
            if (this.elements.list.children[index] !== row.element) {
                this.elements.list.insertBefore(row.element, this.elements.list.children[index] ?? null);
            }
        });

        this.elements.empty.hidden = subscriptions.length > 0;
        this.elements.error.hidden = true;
    }

    /** Shows that something is running for a subscription, e.g. "Checking…", and disables its buttons. */
    setBusy(id: string, label: string | undefined): void {
        this.rows.get(id)?.setBusy(label);
    }

    showError(message: string): void {
        this.elements.error.textContent = message;
        this.elements.error.hidden = false;
    }
}

/** One subscription. */
class SubscriptionRow {
    readonly element = element('div', null, 'tubedlfin-job');
    private readonly badge = element('span', null, 'tubedlfin-badge');
    private readonly title = element('div', null, 'tubedlfin-job-title');
    private readonly destination = element('div', null, 'tubedlfin-job-meta');
    private readonly checks = element('div', null, 'tubedlfin-job-meta');
    private readonly counts = element('div', null, 'tubedlfin-job-meta');
    private readonly error = element('div', null, 'tubedlfin-job-error');
    private readonly busy = element('div', null, 'tubedlfin-job-meta');
    private readonly buttons: HTMLButtonElement[];
    private readonly pause: HTMLButtonElement;
    private readonly editor = element('div', null, 'tubedlfin-subscription-edit');
    private readonly nameInput = element('input', null, 'tubedlfin-inline-input');
    private readonly intervalSelect = element('select', null, 'tubedlfin-inline-input');
    private current: SubscriptionInfo | undefined;

    constructor(id: string, actions: SubscriptionActions) {
        const check = button('Check now', () => actions.check(id), 'tubedlfin-small-button');
        check.title = 'Look for new uploads now and queue them.';
        this.pause = button(
            'Pause',
            () => actions.setPaused(id, !this.current?.Paused),
            'tubedlfin-small-button',
        );
        const edit = button('Edit', () => this.openEditor(), 'tubedlfin-small-button');
        const remove = button(
            'Delete',
            () => actions.remove(id, this.current?.Name ?? ''),
            'tubedlfin-small-button',
        );
        remove.title = 'Stop checking. Downloaded files are kept.';
        this.buttons = [check, this.pause, edit, remove];

        this.nameInput.type = 'text';
        this.nameInput.maxLength = 200;
        this.nameInput.setAttribute('aria-label', 'Name');
        this.intervalSelect.setAttribute('aria-label', 'Check interval');
        const save = button(
            'Save',
            () => {
                actions.save(id, this.nameInput.value, Number.parseInt(this.intervalSelect.value, 10));
                this.editor.hidden = true;
            },
            'tubedlfin-small-button',
        );
        const cancel = button(
            'Cancel',
            () => {
                this.editor.hidden = true;
            },
            'tubedlfin-small-button',
        );
        this.editor.append(this.nameInput, this.intervalSelect, save, cancel);
        this.editor.hidden = true;
        this.busy.hidden = true;

        const header = element('div', null, 'tubedlfin-job-header');
        const buttonBar = element('div', null, 'tubedlfin-job-buttons');
        buttonBar.append(...this.buttons);
        header.append(this.badge, this.title, buttonBar);
        this.element.append(
            header,
            this.destination,
            this.checks,
            this.counts,
            this.busy,
            this.error,
            this.editor,
        );
    }

    update(subscription: SubscriptionInfo, now: Date): void {
        this.current = subscription;
        const [label, tone] = subscription.Paused
            ? ['Paused', 'neutral']
            : subscription.LastError
              ? ['Problem', 'warn']
              : ['Active', 'good'];
        this.badge.textContent = label;
        this.badge.className = `tubedlfin-badge tubedlfin-badge-${tone}`;
        this.title.replaceChildren(externalLink(subscription.Name, subscription.Url));
        this.destination.textContent = `${subscription.LibraryName ?? 'The library no longer exists'} · checks ${intervalLabel(subscription.IntervalHours)}`;
        this.checks.textContent = checkSummary(subscription, now);
        this.counts.textContent = countsSummary(subscription);
        this.error.textContent = subscription.LastError ?? '';
        this.error.hidden = !subscription.LastError;
        this.pause.textContent = subscription.Paused ? 'Resume' : 'Pause';
        this.setBusy(undefined);
    }

    setBusy(label: string | undefined): void {
        this.busy.textContent = label ?? '';
        this.busy.hidden = !label;
        for (const node of this.buttons) {
            node.disabled = Boolean(label);
        }
    }

    private openEditor(): void {
        const subscription = this.current;
        if (!subscription) {
            return;
        }

        const choices = intervalChoices.includes(subscription.IntervalHours)
            ? intervalChoices
            : [...intervalChoices, subscription.IntervalHours].sort((a, b) => a - b);
        this.intervalSelect.replaceChildren(
            ...choices.map((hours) => {
                const node = element('option', intervalLabel(hours));
                node.value = String(hours);
                return node;
            }),
        );
        this.intervalSelect.value = String(subscription.IntervalHours);
        this.nameInput.value = subscription.Name;
        this.editor.hidden = false;
        this.nameInput.focus();
    }
}
